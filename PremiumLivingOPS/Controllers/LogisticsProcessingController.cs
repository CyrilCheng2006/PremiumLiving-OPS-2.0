using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using PremiumLivingOPS.Services;
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Controller (MVC middle layer) for Logistics Processing.
    /// All DB-write operations (Shipment create / status update) are audit-logged.
    /// Contains NO UI code.
    /// </summary>
    public class LogisticsProcessingController
    {
        private readonly LogisticsProcessingRepo _repo = new LogisticsProcessingRepo();

        // ── View Shipment ──────────────────────────────────────────────

        public ViewShipmentViewModel GetViewShipmentVM(
            string    status   = null,
            string    keyword  = null,
            DateTime? dateFrom = null,
            DateTime? dateTo   = null)
        {
            var user = SessionManager.CurrentUser;
            return new ViewShipmentViewModel
            {
                UserBar      = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                Shipments    = _repo.SearchShipments(status, keyword, dateFrom, dateTo)
            };
        }

        public ShipmentDetailViewModel GetShipmentDetailVM(string shipmentId)
        {
            var user = SessionManager.CurrentUser;
            return new ShipmentDetailViewModel
            {
                UserBar      = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                Shipment     = _repo.GetShipmentById(shipmentId),
                Items        = _repo.GetShipmentItems(shipmentId)
            };
        }

        // ── Create Shipment ────────────────────────────────────────────

        public CreateShipmentViewModel GetCreateShipmentVM()
        {
            var user = SessionManager.CurrentUser;
            return new CreateShipmentViewModel
            {
                UserBar        = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus   = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                PendingOrders  = _repo.GetUnshippedOrders(),
                NextShipmentId = _repo.GenerateNextShipmentId()
            };
        }

        public string GenerateNextShipmentId() => _repo.GenerateNextShipmentId();

        /// <summary>Creates a new shipment record and logs the CREATE.</summary>
        public bool CreateShipment(ShipmentEntity shipment, List<ShipmentItemEntity> items)
        {
            bool ok = _repo.CreateShipment(shipment, items);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_CREATE, "Shipment",
                    oldValue: null,
                    newValue: AuditLogger.Snapshot(
                        ("ID",      shipment.ShipmentID),
                        ("Order",   shipment.OrderID ?? ""),
                        ("Status",  shipment.ShipmentStatus ?? ""),
                        ("Carrier", shipment.Carrier ?? ""),
                        ("Items",   (items?.Count ?? 0).ToString())));
            return ok;
        }

        // ── Update Shipment Status ─────────────────────────────────────

        /// <summary>Updates shipment status and logs the EDIT.</summary>
        public bool UpdateShipmentStatus(string shipmentId, string newStatus)
        {
            var old = _repo.GetShipmentById(shipmentId);
            string oldSnap = old == null ? shipmentId
                : AuditLogger.Snapshot(
                    ("ID",     old.ShipmentID),
                    ("Status", old.ShipmentStatus ?? ""),
                    ("Order",  old.OrderID ?? ""));

            bool ok = _repo.UpdateShipmentStatus(shipmentId, newStatus);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_EDIT, "Shipment",
                    oldValue: oldSnap,
                    newValue: AuditLogger.Snapshot(
                        ("ID",     shipmentId),
                        ("Status", newStatus)));
            return ok;
        }

        // ── Update Delivery Date ───────────────────────────────────────

        /// <summary>Updates the actual delivery date and logs the EDIT.</summary>
        public bool UpdateDeliveryDate(string shipmentId, DateTime deliveryDate)
        {
            var old = _repo.GetShipmentById(shipmentId);
            string oldSnap = old == null ? shipmentId
                : AuditLogger.Snapshot(
                    ("ID",          old.ShipmentID),
                    ("DeliveryDate", old.DeliveryDate?.ToString("yyyy-MM-dd") ?? "-"));

            bool ok = _repo.UpdateDeliveryDate(shipmentId, deliveryDate);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_EDIT, "Shipment",
                    oldValue: oldSnap,
                    newValue: AuditLogger.Snapshot(
                        ("ID",          shipmentId),
                        ("DeliveryDate", deliveryDate.ToString("yyyy-MM-dd"))));
            return ok;
        }

        // ── Read helpers ───────────────────────────────────────────────

        public List<ShipmentEntity> GetUnshippedOrders()                     => _repo.GetUnshippedOrders();
        public ShipmentEntity       GetShipmentById(string id)               => _repo.GetShipmentById(id);
        public List<ShipmentItemEntity> GetShipmentItems(string shipmentId)  => _repo.GetShipmentItems(shipmentId);
    }
}
