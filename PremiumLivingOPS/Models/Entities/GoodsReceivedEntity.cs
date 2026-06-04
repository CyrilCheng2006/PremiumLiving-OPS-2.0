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

    /// <summary>
    /// Maps to the PurchaseInvoice table.
    /// PurInvoiceID, PurchaseID, TotalAmount, PaymentStatus ENUM('Partial','Full'), ExpectedDate
    /// </summary>
    public class PurchaseInvoiceEntity
    {
        public string   PurInvoiceID   { get; set; }
        public string   PurchaseID     { get; set; }
        public double   TotalAmount    { get; set; }
        public string   PaymentStatus  { get; set; }  // 'Partial' | 'Full'
        public DateTime ExpectedDate   { get; set; }

        // Joined fields (populated by Repo query)
        public string   SupplierName   { get; set; }
    }

    /// <summary>
    /// Represents an uploaded supplier receipt document record (stored as file path / reference).
    /// Uses the Receipt table — this entity carries the file-path attachment field.
    /// </summary>
    public class SupplierReceiptUploadEntity
    {
        public string   ReceiptID      { get; set; }   // FK → Receipt.ReceiptID
        public string   PurchaseID     { get; set; }   // FK → PurchaseOrder
        public string   SupplierName   { get; set; }   // joined
        public string   FilePath       { get; set; }   // local path selected by user
        public string   FileName       { get; set; }   // display name
        public DateTime UploadDate     { get; set; }
        public string   UploadedBy     { get; set; }   // StaffName
    }
}
