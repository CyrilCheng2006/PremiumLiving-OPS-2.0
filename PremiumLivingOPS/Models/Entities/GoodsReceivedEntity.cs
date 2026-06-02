using System;

namespace PremiumLivingOPS.Models.Entities
{
    public class GoodsReceivedEntity
    {
        public string   ReceiptID         { get; set; }
        public string   PurchaseID        { get; set; }
        public string   POLineID          { get; set; }
        public int      QtyReceived       { get; set; }
        public DateTime ReceiptDate       { get; set; }
        public int?     OutstandingQty    { get; set; }

        // Joined fields
        public string SupplierName       { get; set; }
        public string RawMaterialItemID  { get; set; }
        public string ItemName           { get; set; }
        public string WarehouseID        { get; set; }
        public string WarehouseLocation  { get; set; }
        public string PurchaseStatus     { get; set; }
        public double UnitPrice          { get; set; }
    }

    public class PurchaseOrderEntity
    {
        public string   PurchaseID     { get; set; }
        public string   RequestID      { get; set; }
        public string   SupplierID     { get; set; }
        public string   SupplierName   { get; set; }
        public double   POTotalAmount  { get; set; }
        public DateTime OrderDate      { get; set; }
        public string   PurchaseStatus { get; set; }
    }
}
