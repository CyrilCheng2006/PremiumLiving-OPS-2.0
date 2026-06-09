using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using System;
using System.Collections.Generic;

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

        /// <summary>
        /// Updates ShipmentStatus. Validates that status is a known value.
        /// Also upserts ReplySlip if actualRecipient is supplied and a DeliveryNote exists.
        /// </summary>
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

            // Update status
            _repo.UpdateShipment(shipmentId, newStatus);

            // Upsert ReplySlip if a recipient was provided and a DeliveryNote exists
            if (!string.IsNullOrWhiteSpace(actualRecipient))
            {
                var dn = _repo.GetDeliveryNoteByShipment(shipmentId);
                if (dn != null)
                    _repo.UpsertReplySlip(dn.DeliveryID, actualRecipient, remark);
            }
        }

        // ── Delete Shipment ───────────────────────────────────────────

        /// <summary>
        /// Permanently deletes a shipment and all its child records.
        /// Throws if shipmentId is null/empty.
        /// </summary>
        public void DeleteShipment(string shipmentId)
        {
            if (string.IsNullOrWhiteSpace(shipmentId))
                throw new ArgumentException("Shipment ID is required.");
            _repo.DeleteShipment(shipmentId);
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

        /// <summary>
        /// Returns a pre-filled RecordPurchaseInvoiceVM for the given PO,
        /// including the existing invoice if one already exists.
        /// </summary>
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

        /// <summary>
        /// Saves a new PurchaseInvoice row to the database.
        /// Returns the generated PurInvoiceID.
        /// </summary>
        public string SavePurchaseInvoice(RecordPurchaseInvoiceVM vm)
        {
            if (string.IsNullOrWhiteSpace(vm.PurchaseID))
                throw new ArgumentException("PurchaseID is required.");
            if (vm.TotalAmount <= 0)
                throw new ArgumentException("Total Amount must be greater than zero.");
            return _repo.InsertPurchaseInvoice(vm);
        }
    }
}
