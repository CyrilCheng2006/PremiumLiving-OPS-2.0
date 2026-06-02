using PremiumLivingOPS.Models.Entities;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.ViewModels
{
    // ── View Shipment ─────────────────────────────────────────────────────────────────
    public class ViewShipmentVM
    {
        /// <summary>Shared user bar — uses the same UserBarViewModel as Order Processing.</summary>
        public UserBarViewModel     UserBar      { get; set; }
        public string[]             AllowedMenus { get; set; }
        public List<ShipmentEntity> Shipments    { get; set; }
    }

    public class ShipmentDetailVM
    {
        public ShipmentEntity           Shipment     { get; set; }
        public List<ShipmentLineEntity> Lines        { get; set; }
        public DeliveryNoteEntity       DeliveryNote { get; set; }
    }

    // ── Handling Goods Received ─────────────────────────────────────────────────
    public class HandlingGoodsReceivedVM
    {
        /// <summary>Shared user bar — uses the same UserBarViewModel as Order Processing.</summary>
        public UserBarViewModel          UserBar        { get; set; }
        public string[]                  AllowedMenus   { get; set; }
        public List<GoodsReceivedEntity> Receipts       { get; set; }
        public List<PurchaseOrderEntity> PurchaseOrders { get; set; }
    }
}
