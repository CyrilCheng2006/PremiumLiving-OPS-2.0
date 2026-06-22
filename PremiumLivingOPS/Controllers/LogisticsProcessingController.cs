using System;
using System.Collections.Generic;
using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Services;

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

        // ── Shipment READ ─────────────────────────────────────────────────

        public List<ShipmentEntity> SearchShipments(
            string    status   = null,
            string    keyword  = null,
            DateTime? dateFrom = null,
            DateTime? dateTo   = null)
            => _repo.SearchShipments(status, keyword, dateFrom, dateTo);

        public ShipmentEntity GetShipmentById(string shipmentId)
            => _repo.GetShipmentById(shipmentId);

        /// <summary>Returns line items (ShipmentLine rows) for the given shipment.</summary>
        public List<ShipmentLineEntity> GetShipmentLines(string shipmentId)
            => _repo.GetShipmentLines(shipmentId);

        public string GenerateNextShipmentId() => _repo.GenerateNextShipmentId();

        // ── Shipment WRITE ───────────────────────────────────────────────

        /// <summary>Creates a new shipment record and logs the CREATE.</summary>
        public bool CreateShipment(ShipmentEntity shipment, List<ShipmentLineEntity> lines)
        {
            bool ok = _repo.CreateShipment(shipment, lines);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_CREATE, "Shipment",
                    oldValue: null,
                    newValue: AuditLogger.Snapshot(
                        ("ID",      shipment.ShipmentID),
                        ("Order",   shipment.OrderID ?? ""),
                        ("Status",  shipment.ShipmentStatus ?? ""),
                        ("Method",  shipment.DeliveryMethod ?? ""),
                        ("Lines",   (lines?.Count ?? 0).ToString())));
            return ok;
        }

        /// <summary>Updates shipment status and logs the EDIT.</summary>
        public bool UpdateShipmentStatus(string shipmentId, string newStatus)
        {
            bool ok = _repo.UpdateShipmentStatus(shipmentId, newStatus);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_EDIT, "Shipment",
                    oldValue: AuditLogger.Snapshot(("ID", shipmentId)),
                    newValue: AuditLogger.Snapshot(
                        ("ID",     shipmentId),
                        ("Status", newStatus)));
            return ok;
        }

        /// <summary>Updates the actual delivery date and logs the EDIT.</summary>
        public bool UpdateDeliveryDate(string shipmentId, DateTime deliveryDate)
        {
            bool ok = _repo.UpdateDeliveryDate(shipmentId, deliveryDate);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_EDIT, "Shipment",
                    oldValue: AuditLogger.Snapshot(("ID", shipmentId)),
                    newValue: AuditLogger.Snapshot(
                        ("ID",          shipmentId),
                        ("DeliveryDate", deliveryDate.ToString("yyyy-MM-dd"))));
            return ok;
        }

        // ── Order lookup (for Create Shipment form) ────────────────────

        public List<ShipmentEntity> GetUnshippedOrders() => _repo.GetUnshippedOrders();
    }
}
