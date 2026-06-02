using PremiumLivingOPS.Models.Entities;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.ViewModels
{
    // ── Shared user bar ──────────────────────────────────────────────
    public class LogisticsUserBarVM
    {
        public string DisplayName { get; set; }
        public string Department  { get; set; }
    }

    // ── View Shipment ────────────────────────────────────────────────
    public class ViewShipmentVM
    {
        public LogisticsUserBarVM   UserBar      { get; set; }
        public string[]             AllowedMenus { get; set; }
        public List<ShipmentEntity> Shipments    { get; set; }
    }

    public class ShipmentDetailVM
    {
        public ShipmentEntity           Shipment     { get; set; }
        public List<ShipmentLineEntity> Lines        { get; set; }
        public DeliveryNoteEntity       DeliveryNote { get; set; }
    }

    // ── Handling Goods Received ──────────────────────────────────────
    public class HandlingGoodsReceivedVM
    {
        public LogisticsUserBarVM        UserBar        { get; set; }
        public string[]                  AllowedMenus   { get; set; }
        public List<GoodsReceivedEntity> Receipts       { get; set; }
        public List<PurchaseOrderEntity> PurchaseOrders { get; set; }
    }
}
