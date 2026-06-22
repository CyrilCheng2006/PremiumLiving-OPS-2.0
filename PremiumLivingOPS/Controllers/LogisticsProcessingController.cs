using System;
using System.Collections.Generic;
using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using PremiumLivingOPS.Services;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Controller (MVC middle layer) for Logistics Processing.
    /// Delegates ALL DB work to LogisticsProcessingRepo — contains NO SQL.
    /// Methods mirror ONLY what LogisticsProcessingRepo actually exposes.
    /// </summary>
    public class LogisticsProcessingController
    {
        private readonly LogisticsProcessingRepo _repo = new LogisticsProcessingRepo();

        // ── Shipment READ ─────────────────────────────────────────────────

        /// <summary>Search shipments. Repo signature: (statusFilter, keyword, dateFrom).</summary>
        public List<ShipmentEntity> SearchShipments(
            string    status   = null,
            string    keyword  = null,
            DateTime? dateFrom = null)
            => _repo.SearchShipments(status, keyword, dateFrom);

        public ShipmentEntity GetShipmentById(string shipmentId)
            => _repo.GetShipmentById(shipmentId);

        /// <summary>Returns ShipmentLine rows for a given shipment.</summary>
        public List<ShipmentLineEntity> GetShipmentLines(string shipmentId)
            => _repo.GetShipmentLines(shipmentId);

        // ── Shipment WRITE ───────────────────────────────────────────────

        /// <summary>Updates shipment status and logs the EDIT.</summary>
        public void UpdateShipment(string shipmentId, string newStatus)
        {
            _repo.UpdateShipment(shipmentId, newStatus);
            AuditLogger.Write(AuditLogger.TYPE_EDIT, "Shipment",
                oldValue: AuditLogger.Snapshot(("ID", shipmentId)),
                newValue: AuditLogger.Snapshot(
                    ("ID",     shipmentId),
                    ("Status", newStatus)));
        }

        /// <summary>Deletes a shipment and all child records. Logs DELETE.</summary>
        public void DeleteShipment(string shipmentId)
        {
            _repo.DeleteShipment(shipmentId);
            AuditLogger.Write(AuditLogger.TYPE_DELETE, "Shipment",
                oldValue: AuditLogger.Snapshot(("ID", shipmentId)),
                newValue: null);
        }

        // ── Delivery Note ──────────────────────────────────────────────

        public DeliveryNoteEntity GetDeliveryNoteByShipment(string shipmentId)
            => _repo.GetDeliveryNoteByShipment(shipmentId);

        public string InsertDeliveryNote(string shipmentId, DateTime deliveryDate,
                                         int outstandingQty, string address, string shipToName)
        {
            string id = _repo.InsertDeliveryNote(shipmentId, deliveryDate, outstandingQty, address, shipToName);
            AuditLogger.Write(AuditLogger.TYPE_CREATE, "DeliveryNote",
                oldValue: null,
                newValue: AuditLogger.Snapshot(
                    ("ID",      id),
                    ("Shipment",shipmentId),
                    ("Date",    deliveryDate.ToString("yyyy-MM-dd"))));
            return id;
        }

        // ── Reply Slip ────────────────────────────────────────────────

        public ReplySlipEntity GetReplySlipByDelivery(string deliveryId)
            => _repo.GetReplySlipByDelivery(deliveryId);

        public void UpsertReplySlip(string deliveryId, string actualRecipient, string remark)
        {
            _repo.UpsertReplySlip(deliveryId, actualRecipient, remark);
            AuditLogger.Write(AuditLogger.TYPE_EDIT, "ReplySlip",
                oldValue: AuditLogger.Snapshot(("DeliveryID", deliveryId)),
                newValue: AuditLogger.Snapshot(
                    ("DeliveryID", deliveryId),
                    ("Recipient",  actualRecipient ?? "")));
        }

        public string InsertReplySlip(string deliveryId, string actualRecipient,
                                      string remark, DateTime receivedDate)
        {
            string id = _repo.InsertReplySlip(deliveryId, actualRecipient, remark, receivedDate);
            AuditLogger.Write(AuditLogger.TYPE_CREATE, "ReplySlip",
                oldValue: null,
                newValue: AuditLogger.Snapshot(
                    ("ID",        id),
                    ("DeliveryID",deliveryId),
                    ("Recipient", actualRecipient ?? "")));
            return id;
        }

        // ── Goods Received (Receipt) ──────────────────────────────────

        public List<GoodsReceivedEntity> SearchReceipts(
            string status = null, string keyword = null, DateTime? dateFrom = null)
            => _repo.SearchReceipts(status, keyword, dateFrom);

        public List<GoodsReceivedEntity> GetReceiptsByPurchaseID(string purchaseId)
            => _repo.GetReceiptsByPurchaseID(purchaseId);

        // ── Purchase Order ────────────────────────────────────────────

        public List<PurchaseOrderEntity> GetAllPurchaseOrders()
            => _repo.GetAllPurchaseOrders();

        public List<PurchaseOrderLineEntity> GetPODetailLines(string purchaseId)
            => _repo.GetPODetailLines(purchaseId);

        // ── Purchase Invoice ──────────────────────────────────────────

        public List<PurchaseInvoiceEntity> GetAllPurchaseInvoices()
            => _repo.GetAllPurchaseInvoices();

        public PurchaseInvoiceEntity GetPurchaseInvoiceByPO(string purchaseId)
            => _repo.GetPurchaseInvoiceByPO(purchaseId);

        public string InsertPurchaseInvoice(RecordPurchaseInvoiceVM vm)
        {
            string id = _repo.InsertPurchaseInvoice(vm);
            AuditLogger.Write(AuditLogger.TYPE_CREATE, "PurchaseInvoice",
                oldValue: null,
                newValue: AuditLogger.Snapshot(
                    ("ID",      id),
                    ("PO",      vm.PurchaseID ?? ""),
                    ("Total",   vm.TotalAmount.ToString("F2")),
                    ("Status",  vm.PaymentStatus ?? "")));
            return id;
        }

        // ── ViewModel Assembly (View layer shortcut) ─────────────────

        /// <summary>
        /// Assembles ShipmentDetailVM: Shipment + Lines + DeliveryNote + ReplySlip.
        /// </summary>
        public ShipmentDetailVM GetShipmentDetailVM(string shipmentId)
        {
            var shipment     = _repo.GetShipmentById(shipmentId);
            if (shipment == null) return null;
            var lines        = _repo.GetShipmentLines(shipmentId);
            var deliveryNote = _repo.GetDeliveryNoteByShipment(shipmentId);
            var replySlip    = deliveryNote != null
                               ? _repo.GetReplySlipByDelivery(deliveryNote.DeliveryID)
                               : null;
            return new ShipmentDetailVM
            {
                Shipment     = shipment,
                Lines        = lines,
                DeliveryNote = deliveryNote,
                ReplySlip    = replySlip
            };
        }

        /// <summary>Assembles PODetailVM for PODetailDialog.</summary>
        public PODetailVM GetPODetailVM(string purchaseId)
        {
            var (po, phone, address, invoiceStatus) = _repo.GetPOHeaderFull(purchaseId);
            if (po == null) return null;
            var lines    = _repo.GetPODetailLines(purchaseId);
            var receipts = _repo.GetReceiptsByPurchaseID(purchaseId);
            int totalOrdered  = 0;
            int totalReceived = 0;
            foreach (var l in lines)   totalOrdered  += l.OrderQty;
            foreach (var r in receipts) totalReceived += r.QtyReceived;
            return new PODetailVM
            {
                PurchaseOrder   = po,
                Lines           = lines,
                SupplierPhone   = phone,
                SupplierAddress = address,
                InvoiceStatus   = invoiceStatus,
                TotalQtyOrdered = totalOrdered,
                TotalQtyReceived= totalReceived
            };
        }

        /// <summary>Assembles ReceiptDetailVM for ReceiptDetailDialog.</summary>
        public ReceiptDetailVM GetReceiptDetailVM(string purchaseId, string receiptId)
        {
            var receipts = _repo.GetReceiptsByPurchaseID(purchaseId);
            var selected = receipts.Find(r => r.ReceiptID == receiptId);
            return new ReceiptDetailVM
            {
                Receipt    = selected,
                AllReceipts = receipts
            };
        }

        /// <summary>Assembles ViewShipmentVM for the View Shipment page.</summary>
        public ViewShipmentVM GetViewShipmentVM(string status = null, string keyword = null, DateTime? dateFrom = null)
        {
            var user = SessionManager.CurrentUser;
            return new ViewShipmentVM
            {
                UserBar      = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                Shipments    = _repo.SearchShipments(status, keyword, dateFrom)
            };
        }

        /// <summary>Assembles HandlingGoodsReceivedVM.</summary>
        public HandlingGoodsReceivedVM GetHandlingGoodsReceivedVM(string status = null, string keyword = null, DateTime? dateFrom = null)
        {
            var user = SessionManager.CurrentUser;
            return new HandlingGoodsReceivedVM
            {
                UserBar        = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus   = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                Receipts       = _repo.SearchReceipts(status, keyword, dateFrom),
                PurchaseOrders = _repo.GetAllPurchaseOrders(),
                Invoices       = _repo.GetAllPurchaseInvoices()
            };
        }

        // ── CSV Bulk Import ───────────────────────────────────────────

        public bool PurchaseOrderExists(string purchaseId) => _repo.PurchaseOrderExists(purchaseId);
        public bool POLineExists(string poLineId, string purchaseId) => _repo.POLineExists(poLineId, purchaseId);
        public int BulkInsertReceipts(List<ReceiptImportRow> rows)
        {
            int count = _repo.BulkInsertReceipts(rows);
            AuditLogger.Write(AuditLogger.TYPE_CREATE, "Receipt",
                oldValue: null,
                newValue: AuditLogger.Snapshot(("BulkCount", count.ToString())));
            return count;
        }
    }
}
