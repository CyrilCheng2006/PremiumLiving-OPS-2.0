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
        /// <summary>Reply Slip linked to the DeliveryNote (may be null if not yet received).</summary>
        public ReplySlipEntity          ReplySlip    { get; set; }
    }

    // ── Handling Goods Received ─────────────────────────────────────────────────
    public class HandlingGoodsReceivedVM
    {
        /// <summary>Shared user bar — uses the same UserBarViewModel as Order Processing.</summary>
        public UserBarViewModel              UserBar        { get; set; }
        public string[]                      AllowedMenus   { get; set; }
        public List<GoodsReceivedEntity>     Receipts       { get; set; }
        public List<PurchaseOrderEntity>     PurchaseOrders { get; set; }
        public List<PurchaseInvoiceEntity>   Invoices       { get; set; }
    }

    /// <summary>
    /// View model for the Upload Supplier Receipt dialog.
    /// Carries the selected Receipt row + file path chosen by the user.
    /// </summary>
    public class UploadSupplierReceiptVM
    {
        public string ReceiptID    { get; set; }
        public string PurchaseID   { get; set; }
        public string SupplierName { get; set; }
        public string FilePath     { get; set; }   // full local path
        public string FileName     { get; set; }   // display name (basename)
    }

    /// <summary>
    /// View model for the Record Purchase Invoice dialog.
    /// Maps to PurchaseInvoice table: PurInvoiceID, PurchaseID, TotalAmount, PaymentStatus, ExpectedDate.
    /// </summary>
    public class RecordPurchaseInvoiceVM
    {
        public string   PurInvoiceID   { get; set; }  // auto-generated or user-supplied
        public string   PurchaseID     { get; set; }  // FK → PurchaseOrder
        public string   SupplierName   { get; set; }  // display only
        public double   TotalAmount    { get; set; }
        public string   PaymentStatus  { get; set; }  // 'Partial' | 'Full'
        public DateTime ExpectedDate   { get; set; }
        // Existing invoice (null if none yet)
        public PurchaseInvoiceEntity ExistingInvoice { get; set; }
    }

    /// <summary>
    /// View model for PODetailDialog.
    /// Carries the PurchaseOrder header and its line items.
    /// Populated by LogisticsProcessingController.GetPODetailVM().
    /// </summary>
    public class PODetailVM
    {
        public PurchaseOrderEntity           PurchaseOrder { get; set; }
        public List<PurchaseOrderLineEntity> Lines         { get; set; }
    }
}
