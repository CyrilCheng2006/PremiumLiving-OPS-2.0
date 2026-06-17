using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Controller (MVC middle layer) for Logistics Processing.
    /// Accepts requests from View layer, delegates to LogisticsProcessingRepo, returns ViewModels.
    /// Contains NO UI code and NO direct SQL.
    /// </summary>
    public class LogisticsProcessingController
    {
        private readonly LogisticsProcessingRepo _repo = new LogisticsProcessingRepo();

        // ── View Shipment ─────────────────────────────────────────────
        public ViewShipmentVM GetViewShipmentVM(
            string statusFilter = null,
            string keyword      = null,
            DateTime? dateFrom  = null)
        {
            var user = SessionManager.CurrentUser;
            return new ViewShipmentVM
            {
                UserBar = new UserBarViewModel
                {
                    DisplayName = user?.StaffName   ?? "Unknown",
                    Department  = user?.Department  ?? ""
                },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                Shipments    = _repo.SearchShipments(statusFilter, keyword, dateFrom)
            };
        }

        public ShipmentDetailVM GetShipmentDetail(string shipmentId)
        {
            var dn = _repo.GetDeliveryNoteByShipment(shipmentId);
            return new ShipmentDetailVM
            {
                Shipment     = _repo.GetShipmentById(shipmentId),
                Lines        = _repo.GetShipmentLines(shipmentId),
                DeliveryNote = dn,
                ReplySlip    = dn != null ? _repo.GetReplySlipByDelivery(dn.DeliveryID) : null
            };
        }

        // ── Edit Shipment ─────────────────────────────────────────────
        public void UpdateShipment(string shipmentId,
                                   string newStatus,
                                   string actualRecipient,
                                   string remark)
        {
            if (string.IsNullOrWhiteSpace(shipmentId))
                throw new ArgumentException("Shipment ID is required.");

            var validStatuses = new[] { "Pending", "In Transit", "Completed" };
            if (System.Array.IndexOf(validStatuses, newStatus) < 0)
                throw new ArgumentException($"Invalid status '{newStatus}'.");

            _repo.UpdateShipment(shipmentId, newStatus);

            if (!string.IsNullOrWhiteSpace(actualRecipient))
            {
                var dn = _repo.GetDeliveryNoteByShipment(shipmentId);
                if (dn != null)
                    _repo.UpsertReplySlip(dn.DeliveryID, actualRecipient, remark);
            }
        }

        // ── Delete Shipment ───────────────────────────────────────────
        public void DeleteShipment(string shipmentId)
        {
            if (string.IsNullOrWhiteSpace(shipmentId))
                throw new ArgumentException("Shipment ID is required.");
            _repo.DeleteShipment(shipmentId);
        }

        // ── Generate Delivery Note ────────────────────────────────────
        public string GenerateDeliveryNote(string shipmentId)
        {
            if (string.IsNullOrWhiteSpace(shipmentId))
                throw new ArgumentException("Shipment ID is required.");

            var existing = _repo.GetDeliveryNoteByShipment(shipmentId);
            if (existing != null)
                throw new InvalidOperationException(
                    $"A Delivery Note ({existing.DeliveryID}) already exists for shipment {shipmentId}.");

            var shipment = _repo.GetShipmentById(shipmentId);
            if (shipment == null)
                throw new InvalidOperationException($"Shipment {shipmentId} not found.");

            var lines = _repo.GetShipmentLines(shipmentId);
            int outstandingQty = 0;
            foreach (var line in lines)
                outstandingQty += line.QtyOutstanding ?? 0;

            return _repo.InsertDeliveryNote(
                shipmentId,
                shipment.ShipDate,
                outstandingQty,
                shipment.ShippingAddress,
                shipment.CustomerName);
        }

        // ── Generate Reply Slip ───────────────────────────────────────
        public string GenerateReplySlip(string shipmentId,
                                        string actualRecipient,
                                        string remark)
        {
            if (string.IsNullOrWhiteSpace(shipmentId))
                throw new ArgumentException("Shipment ID is required.");
            if (string.IsNullOrWhiteSpace(actualRecipient))
                throw new ArgumentException("Actual Recipient is required.");

            var dn = _repo.GetDeliveryNoteByShipment(shipmentId);
            if (dn == null)
                throw new InvalidOperationException(
                    $"No Delivery Note found for shipment {shipmentId}. Please generate one first.");

            var existing = _repo.GetReplySlipByDelivery(dn.DeliveryID);
            if (existing != null)
                throw new InvalidOperationException(
                    $"A Reply Slip ({existing.SlipID}) already exists for Delivery Note {dn.DeliveryID}.");

            return _repo.InsertReplySlip(dn.DeliveryID, actualRecipient, remark, DateTime.Today);
        }

        // ── Handling Goods Received ────────────────────────────────────
        public HandlingGoodsReceivedVM GetHandlingGoodsReceivedVM(
            string statusFilter = null,
            string keyword      = null,
            DateTime? dateFrom  = null)
        {
            var user = SessionManager.CurrentUser;
            return new HandlingGoodsReceivedVM
            {
                UserBar = new UserBarViewModel
                {
                    DisplayName = user?.StaffName  ?? "Unknown",
                    Department  = user?.Department ?? ""
                },
                AllowedMenus   = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                Receipts       = _repo.SearchReceipts(statusFilter, keyword, dateFrom),
                PurchaseOrders = _repo.GetAllPurchaseOrders(),
                Invoices       = _repo.GetAllPurchaseInvoices()
            };
        }

        public RecordPurchaseInvoiceVM GetRecordPurchaseInvoiceVM(PurchaseOrderEntity po)
        {
            var existing = _repo.GetPurchaseInvoiceByPO(po?.PurchaseID);
            return new RecordPurchaseInvoiceVM
            {
                PurchaseID      = po?.PurchaseID    ?? "",
                SupplierName    = po?.SupplierName  ?? "",
                TotalAmount     = po?.POTotalAmount ?? 0,
                PaymentStatus   = "Full",
                ExpectedDate    = DateTime.Today.AddDays(30),
                ExistingInvoice = existing
            };
        }

        public string SavePurchaseInvoice(RecordPurchaseInvoiceVM vm)
        {
            if (string.IsNullOrWhiteSpace(vm.PurchaseID))
                throw new ArgumentException("PurchaseID is required.");
            if (vm.TotalAmount <= 0)
                throw new ArgumentException("Total Amount must be greater than zero.");
            return _repo.InsertPurchaseInvoice(vm);
        }

        // ── CSV Import: Receipt ───────────────────────────────────────
        /// <summary>
        /// Parses a CSV file, validates each row against the DB, then bulk-inserts
        /// all valid rows inside a single transaction.
        ///
        /// Expected CSV header (case-insensitive):
        ///   PurchaseID, POLineID, QtyReceived, ReceiptDate, Outstanding_QTY
        ///
        /// Rules:
        ///   • PurchaseID must exist in PurchaseOrder table.
        ///   • POLineID must exist in PurchaseOrderLine AND belong to that PurchaseID.
        ///   • QtyReceived must be a positive integer.
        ///   • ReceiptDate must be a valid date (yyyy-MM-dd preferred).
        ///   • Outstanding_QTY is optional; blank = NULL.
        ///
        /// Returns a ReceiptImportResult with success count and per-row error messages.
        /// </summary>
        public ReceiptImportResult ImportReceiptsFromCsv(string filePath)
        {
            var result = new ReceiptImportResult();
            var validRows = new List<ReceiptImportRow>();

            if (!File.Exists(filePath))
            {
                result.Errors.Add("File not found: " + filePath);
                return result;
            }

            string[] lines;
            try   { lines = File.ReadAllLines(filePath); }
            catch (Exception ex)
            {
                result.Errors.Add("Cannot read file: " + ex.Message);
                return result;
            }

            if (lines.Length < 2)
            {
                result.Errors.Add("CSV file has no data rows (only a header or is empty).");
                return result;
            }

            // ── Parse header ─────────────────────────────────────────
            var header = lines[0].Split(',');
            int idxPurchaseID   = FindCol(header, "PurchaseID");
            int idxPOLineID     = FindCol(header, "POLineID");
            int idxQtyReceived  = FindCol(header, "QtyReceived");
            int idxReceiptDate  = FindCol(header, "ReceiptDate");
            int idxOutstanding  = FindCol(header, "Outstanding_QTY");

            if (idxPurchaseID < 0 || idxPOLineID < 0 ||
                idxQtyReceived < 0 || idxReceiptDate < 0)
            {
                result.Errors.Add(
                    "CSV header must contain: PurchaseID, POLineID, QtyReceived, ReceiptDate");
                return result;
            }

            // ── Parse + validate rows ─────────────────────────────────
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;   // skip blank lines

                int rowNum = i;  // 1-based data row number equals line index
                var cols   = line.Split(',');

                string purchaseId  = GetCol(cols, idxPurchaseID);
                string poLineId    = GetCol(cols, idxPOLineID);
                string qtyStr      = GetCol(cols, idxQtyReceived);
                string dateStr     = GetCol(cols, idxReceiptDate);
                string outStr      = idxOutstanding >= 0 ? GetCol(cols, idxOutstanding) : "";

                // ── Field presence checks
                if (string.IsNullOrEmpty(purchaseId))
                { result.Errors.Add($"Row {rowNum}: PurchaseID is empty."); continue; }
                if (string.IsNullOrEmpty(poLineId))
                { result.Errors.Add($"Row {rowNum}: POLineID is empty."); continue; }

                // ── QtyReceived
                if (!int.TryParse(qtyStr, out int qty) || qty <= 0)
                { result.Errors.Add($"Row {rowNum}: QtyReceived '{qtyStr}' must be a positive integer."); continue; }

                // ── ReceiptDate
                if (!DateTime.TryParse(dateStr, out DateTime receiptDate))
                { result.Errors.Add($"Row {rowNum}: ReceiptDate '{dateStr}' is not a valid date."); continue; }

                // ── Outstanding_QTY (optional)
                int? outstanding = null;
                if (!string.IsNullOrEmpty(outStr))
                {
                    if (!int.TryParse(outStr, out int outVal) || outVal < 0)
                    { result.Errors.Add($"Row {rowNum}: Outstanding_QTY '{outStr}' must be a non-negative integer or blank."); continue; }
                    outstanding = outVal;
                }

                // ── DB FK checks
                if (!_repo.PurchaseOrderExists(purchaseId))
                { result.Errors.Add($"Row {rowNum}: PurchaseID '{purchaseId}' not found in database."); continue; }

                if (!_repo.POLineExists(poLineId, purchaseId))
                { result.Errors.Add($"Row {rowNum}: POLineID '{poLineId}' not found or does not belong to PurchaseID '{purchaseId}'."); continue; }

                validRows.Add(new ReceiptImportRow
                {
                    RowNumber      = rowNum,
                    PurchaseID     = purchaseId,
                    POLineID       = poLineId,
                    QtyReceived    = qty,
                    ReceiptDate    = receiptDate,
                    OutstandingQty = outstanding
                });
            }

            // ── Bulk insert all valid rows in one transaction ─────────
            if (validRows.Count > 0)
            {
                try
                {
                    result.SuccessCount = _repo.BulkInsertReceipts(validRows);
                }
                catch (Exception ex)
                {
                    result.Errors.Add("Database error during insert: " + ex.Message);
                }
            }

            return result;
        }

        // ── Private CSV helpers ───────────────────────────────────────
        private static int FindCol(string[] header, string name)
        {
            for (int i = 0; i < header.Length; i++)
                if (header[i].Trim().Equals(name, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        private static string GetCol(string[] cols, int idx)
            => idx < cols.Length ? cols[idx].Trim() : string.Empty;
    }
}
