using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using PremiumLivingOPS.Views.LogisticsProcessing;
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

        // ── View Shipment ────────────────────────────────────────────
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

        // ── Edit Shipment ───────────────────────────────────────────
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

        // ── Schedule Shipment (update existing record) ──────────────
        /// <summary>
        /// Updates DeliveryMethod and ShipDate for the specified shipment.
        /// Called from ScheduleShipmentDialog when editing an existing shipment.
        /// </summary>
        public void ScheduleShipment(
            string   shipmentId,
            DateTime scheduledDate,
            string   deliveryMethod,
            string   contactPerson,
            string   notes)
        {
            if (string.IsNullOrWhiteSpace(shipmentId))
                throw new ArgumentException("Shipment ID is required.");
            if (scheduledDate < DateTime.Today)
                throw new ArgumentException("Scheduled date cannot be in the past.");

            var validMethods = new[] { "Courier", "SelfPickup" };
            if (System.Array.IndexOf(validMethods, deliveryMethod) < 0)
                throw new ArgumentException($"Invalid delivery method '{deliveryMethod}'.");

            _repo.ScheduleShipment(shipmentId, scheduledDate, deliveryMethod);
        }

        // ── Schedule Shipment Wizard — Step 1: list orders ──────────
        /// <summary>
        /// Returns orders eligible for shipment scheduling:
        /// OrderStatus IN ('Processing', 'Partially Delivered', 'Pending').
        /// Each row is mapped to the lightweight OrderSummary DTO used by
        /// ScheduleShipmentDialog (Step 1 order-picker).
        /// </summary>
        public List<OrderSummary> GetSchedulableOrders()
            => _repo.GetSchedulableOrders();

        // ── Schedule Shipment Wizard — Step 2: lines with qty status ─
        /// <summary>
        /// Returns all OrderLines for the given order together with the
        /// total qty already shipped across all existing ShipmentLines.
        /// Used to populate the Step-2 grid in ScheduleShipmentDialog.
        /// </summary>
        public List<OrderLineDetail> GetOrderLinesWithShipmentStatus(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
                throw new ArgumentException("Order ID is required.");
            return _repo.GetOrderLinesWithShipmentStatus(orderId);
        }

        // ── Schedule Shipment Wizard — duplicate-batch guard ─────────
        /// <summary>
        /// Returns the list of trailing suffixes (e.g. "0029A", "0029B") from
        /// ShipmentIDs that already exist for the given order.
        /// ScheduleShipmentDialog uses this to block duplicate Batch letters.
        /// </summary>
        public List<string> GetExistingShipmentSuffixes(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
                return new List<string>();
            return _repo.GetExistingShipmentSuffixes(orderId);
        }

        // ── Schedule Shipment Wizard — create shipment(s) ────────────
        /// <summary>
        /// Creates one Shipment record + its ShipmentLines from a
        /// CreateShipmentRequest produced by ScheduleShipmentDialog.
        /// ShipmentID format : SHP-YYYYMMDD-{orderSuffix}{batchLetter}
        ///   e.g. SHP-20260309-0029A
        /// After insertion the parent Order's status is updated:
        ///   all items covered -> 'Partially Delivered' (caller may upgrade to Completed
        ///   on the last batch); partial coverage -> 'Partially Delivered'.
        /// </summary>
        public void CreateScheduledShipment(CreateShipmentRequest req)
        {
            if (req == null)        throw new ArgumentNullException(nameof(req));
            if (req.Lines == null || req.Lines.Count == 0)
                throw new ArgumentException("At least one shipment line is required.");

            string shipmentId = "SHP-"
                + req.ShipDate.ToString("yyyyMMdd")
                + "-" + req.OrderSuffix + req.Batch;

            double totalAmount = _repo.ComputeShipmentTotal(req.OrderID, req.Lines);

            _repo.CreateScheduledShipment(
                shipmentId,
                req.OrderID,
                req.ShipDate,
                req.DeliveryMethod,
                req.ShipmentType,
                totalAmount,
                req.Lines);
        }

        // ── Delete Shipment ─────────────────────────────────────────
        public void DeleteShipment(string shipmentId)
        {
            if (string.IsNullOrWhiteSpace(shipmentId))
                throw new ArgumentException("Shipment ID is required.");
            _repo.DeleteShipment(shipmentId);
        }

        // ── Generate Delivery Note ──────────────────────────────────
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

        // ── Generate Reply Slip ────────────────────────────────────
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

        // ── Handling Goods Received ──────────────────────────────────
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

        public PODetailVM GetPODetailVM(string purchaseId)
        {
            var (po, phone, address, invoiceStatus) = _repo.GetPOHeaderFull(purchaseId);
            return new PODetailVM
            {
                PurchaseOrder   = po,
                Lines           = _repo.GetPODetailLines(purchaseId),
                SupplierPhone   = phone,
                SupplierAddress = address,
                InvoiceStatus   = invoiceStatus
            };
        }

        public ReceiptDetailVM GetReceiptDetailVM(GoodsReceivedEntity selectedReceipt)
        {
            if (selectedReceipt == null) throw new ArgumentNullException(nameof(selectedReceipt));
            return new ReceiptDetailVM
            {
                Receipt     = selectedReceipt,
                AllReceipts = _repo.GetReceiptsByPurchaseID(selectedReceipt.PurchaseID)
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

        // ── CSV Import: Receipt ──────────────────────────────────────
        public ReceiptImportResult ImportReceiptsFromCsv(string filePath)
        {
            var result    = new ReceiptImportResult();
            var validRows = new List<ReceiptImportRow>();

            if (!File.Exists(filePath))
            { result.Errors.Add("File not found: " + filePath); return result; }

            string[] lines;
            try   { lines = File.ReadAllLines(filePath); }
            catch (Exception ex)
            { result.Errors.Add("Cannot read file: " + ex.Message); return result; }

            if (lines.Length < 2)
            { result.Errors.Add("CSV file has no data rows."); return result; }

            var header = lines[0].Split(',');
            int idxPurchaseID  = FindCol(header, "PurchaseID");
            int idxPOLineID    = FindCol(header, "POLineID");
            int idxQtyReceived = FindCol(header, "QtyReceived");
            int idxReceiptDate = FindCol(header, "ReceiptDate");
            int idxOutstanding = FindCol(header, "Outstanding_QTY");

            if (idxPurchaseID < 0 || idxPOLineID < 0 ||
                idxQtyReceived < 0 || idxReceiptDate < 0)
            {
                result.Errors.Add("CSV header must contain: PurchaseID, POLineID, QtyReceived, ReceiptDate");
                return result;
            }

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                int rowNum = i;
                var cols = line.Split(',');

                string purchaseId = GetCol(cols, idxPurchaseID);
                string poLineId   = GetCol(cols, idxPOLineID);
                string qtyStr     = GetCol(cols, idxQtyReceived);
                string dateStr    = GetCol(cols, idxReceiptDate);
                string outStr     = idxOutstanding >= 0 ? GetCol(cols, idxOutstanding) : "";

                if (string.IsNullOrEmpty(purchaseId))
                { result.Errors.Add($"Row {rowNum}: PurchaseID is empty."); continue; }
                if (string.IsNullOrEmpty(poLineId))
                { result.Errors.Add($"Row {rowNum}: POLineID is empty."); continue; }
                if (!int.TryParse(qtyStr, out int qty) || qty <= 0)
                { result.Errors.Add($"Row {rowNum}: QtyReceived '{qtyStr}' must be a positive integer."); continue; }
                if (!DateTime.TryParse(dateStr, out DateTime receiptDate))
                { result.Errors.Add($"Row {rowNum}: ReceiptDate '{dateStr}' is not a valid date."); continue; }

                int? outstanding = null;
                if (!string.IsNullOrEmpty(outStr))
                {
                    if (!int.TryParse(outStr, out int outVal) || outVal < 0)
                    { result.Errors.Add($"Row {rowNum}: Outstanding_QTY '{outStr}' must be non-negative or blank."); continue; }
                    outstanding = outVal;
                }

                if (!_repo.PurchaseOrderExists(purchaseId))
                { result.Errors.Add($"Row {rowNum}: PurchaseID '{purchaseId}' not found."); continue; }
                if (!_repo.POLineExists(poLineId, purchaseId))
                { result.Errors.Add($"Row {rowNum}: POLineID '{poLineId}' does not belong to '{purchaseId}'."); continue; }

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

            if (validRows.Count > 0)
            {
                try   { result.SuccessCount = _repo.BulkInsertReceipts(validRows); }
                catch (Exception ex) { result.Errors.Add("Database error: " + ex.Message); }
            }
            return result;
        }

        private static int FindCol(string[] header, string name)
        {
            for (int i = 0; i < header.Length; i++)
                if (header[i].Trim().Equals(name, StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }
        private static string GetCol(string[] cols, int idx)
            => idx < cols.Length ? cols[idx].Trim() : string.Empty;
    }
}
