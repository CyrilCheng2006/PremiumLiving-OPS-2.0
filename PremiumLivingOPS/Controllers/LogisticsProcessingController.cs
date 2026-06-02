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

        // ── View Shipment ──────────────────────────────────────────────────
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
            return new ShipmentDetailVM
            {
                Shipment     = _repo.GetShipmentById(shipmentId),
                Lines        = _repo.GetShipmentLines(shipmentId),
                DeliveryNote = _repo.GetDeliveryNoteByShipment(shipmentId)
            };
        }

        // ── Handling Goods Received ───────────────────────────────────────
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
                PurchaseOrders = _repo.GetAllPurchaseOrders()
            };
        }
    }
}
