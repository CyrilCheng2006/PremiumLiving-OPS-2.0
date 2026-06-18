using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public DateTime ExpectedDate    { get; set; }
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
        /// <summary>PurchaseInvoice.ExpectedDate — payment/delivery deadline. Null when no invoice exists.</summary>
        public DateTime? ExpectedDate { get; set; }

        // ── Derived receipt-progress fields (populated by controller) ──
        /// <summary>Sum of Receipt.QtyReceived across all lines of this PO.</summary>
        public int TotalQtyReceived { get; set; }
        /// <summary>Sum of PurchaseOrderLine.OrderQty across all lines of this PO.</summary>
        public int TotalQtyOrdered  { get; set; }
        /// <summary>"3 / 10 units received" style summary label.</summary>
        public string ReceiptProgressLabel =>
            TotalQtyOrdered > 0
                ? $"{TotalQtyReceived} / {TotalQtyOrdered} units received"
                : "No order lines";
    }

    /// <summary>
    /// View model for ReceiptDetailDialog.
    /// Receipt = the selected row (used for header).
    /// AllReceipts = all Receipt rows sharing the same PurchaseID (grid), sorted by ReceiptDate ASC.
    /// Populated by LogisticsProcessingController.GetReceiptDetailVM().
    /// </summary>
    public class ReceiptDetailVM
    {
        private List<GoodsReceivedEntity> _allReceipts;

        public GoodsReceivedEntity       Receipt     { get; set; }

        /// <summary>All receipts under the same PO, sorted by ReceiptDate ASC then ReceiptID.</summary>
        public List<GoodsReceivedEntity> AllReceipts
        {
            get => _allReceipts;
            set => _allReceipts = value
                       ?.OrderBy(r => r.ReceiptDate)
                       .ThenBy(r => r.ReceiptID)
                       .ToList();
        }

        // ── Derived grid-footer aggregates ──
        public int   TotalQtyReceived  => AllReceipts?.Sum(r => r.QtyReceived)  ?? 0;
        public int   TotalOutstanding  => AllReceipts?.Sum(r => r.OutstandingQty ?? 0) ?? 0;
        public double TotalLineAmount  => AllReceipts?.Sum(r => r.QtyReceived * r.UnitPrice) ?? 0;
    }
}
