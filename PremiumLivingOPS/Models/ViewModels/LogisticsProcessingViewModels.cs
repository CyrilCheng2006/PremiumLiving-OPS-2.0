using PremiumLivingOPS.Models.Entities;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.ViewModels
{
    // ── View Shipment ─────────────────────────────────────────────────────────────────
    public class ViewShipmentVM
    {
        public UserBarViewModel     UserBar      { get; set; }
        public string[]             AllowedMenus { get; set; }
        public List<ShipmentEntity> Shipments    { get; set; }
    }

    public class ShipmentDetailVM
    {
        public ShipmentEntity           Shipment     { get; set; }
        public List<ShipmentLineEntity> Lines        { get; set; }
        public DeliveryNoteEntity       DeliveryNote { get; set; }
        /// <summary>Reply Slip linked to the DeliveryNote (may be null if not yet received).</summary>
        public ReplySlipEntity          ReplySlip    { get; set; }
    }

    // ── Handling Goods Received ─────────────────────────────────────────────────
    public class HandlingGoodsReceivedVM
    {
        public UserBarViewModel            UserBar        { get; set; }
        public string[]                    AllowedMenus   { get; set; }
        public List<GoodsReceivedEntity>   Receipts       { get; set; }
        public List<PurchaseOrderEntity>   PurchaseOrders { get; set; }
        public List<PurchaseInvoiceEntity> Invoices       { get; set; }
    }

    /// <summary>
    /// View model for the Upload Supplier Receipt dialog.
    /// </summary>
    public class UploadSupplierReceiptVM
    {
        public string ReceiptID    { get; set; }
        public string PurchaseID   { get; set; }
        public string SupplierName { get; set; }
        public string FilePath     { get; set; }
        public string FileName     { get; set; }
    }

    /// <summary>
    /// View model for the Record Purchase Invoice dialog.
    /// Maps to PurchaseInvoice: PurInvoiceID, PurchaseID, TotalAmount, PaymentStatus, ExpectedDate.
    /// </summary>
    public class RecordPurchaseInvoiceVM
    {
        public string   PurInvoiceID    { get; set; }
        public string   PurchaseID      { get; set; }
        public string   SupplierName    { get; set; }
        public double   TotalAmount     { get; set; }
        public string   PaymentStatus   { get; set; }   // 'Partial' | 'Full'
        public System.DateTime ExpectedDate { get; set; }
        public PurchaseInvoiceEntity ExistingInvoice { get; set; }
    }

    /// <summary>
    /// View model for PODetailDialog.
    /// Carries the PurchaseOrder header (with supplier contact + invoice status)
    /// and all line items (with WarehouseLocation).
    /// Populated by LogisticsProcessingController.GetPODetailVM().
    /// </summary>
    public class PODetailVM
    {
        public PurchaseOrderEntity           PurchaseOrder   { get; set; }
        public List<PurchaseOrderLineEntity> Lines           { get; set; }

        // Extra header fields from Supplier + PurchaseInvoice JOIN
        /// <summary>Supplier.PhoneNumber</summary>
        public string SupplierPhone   { get; set; }
        /// <summary>Supplier.SupplierAddress</summary>
        public string SupplierAddress { get; set; }
        /// <summary>PurchaseInvoice.PaymentStatus  ('Partial' | 'Full' | 'N/A')</summary>
        public string InvoiceStatus   { get; set; }
    }

    /// <summary>
    /// View model for ReceiptDetailDialog.
    /// Receipt = the selected row (used for header).
    /// AllReceipts = all Receipt rows sharing the same PurchaseID (grid).
    /// Populated by LogisticsProcessingController.GetReceiptDetailVM().
    /// </summary>
    public class ReceiptDetailVM
    {
        public GoodsReceivedEntity       Receipt     { get; set; }
        public List<GoodsReceivedEntity> AllReceipts { get; set; }
    }
}
