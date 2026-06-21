using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.Entities
{
    // ── UserBarViewModel is already defined in OrderProcessingViewModel.cs ──────
    // Do NOT re-declare UserBarViewModel here — use the one from that file.

    // ══════════════════════════════════════════════════════════════════════════════
    //  After-Service Domain Entities
    // ══════════════════════════════════════════════════════════════════════════════

    /// <summary>Maps to Invoice table (JOINed with Order + Customer).</summary>
    public class InvoiceEntity
    {
        public string   InvoiceID        { get; set; }
        public string   OrderID          { get; set; }
        public string   CustomerName     { get; set; }
        public DateTime InvoiceDate      { get; set; }
        public double   DepositAmount    { get; set; }
        public double   PaidAmount       { get; set; }
        public double   RemainingBalance { get; set; }
        public double   TotalAmount      { get; set; }
        public string   PaymentStatus    { get; set; }   // 'Partial' | 'Full'
        public DateTime DueDate          { get; set; }
    }

    /// <summary>
    /// Maps to Complaint table (JOINed with Staff).
    /// StaffID — used on INSERT (FK to Staff).
    /// StaffName — resolved via JOIN, populated on SELECT only.
    /// </summary>
    public class ComplaintEntity
    {
        public string ComplaintID          { get; set; }
        public string OrderID              { get; set; }   // nullable
        public string StaffID              { get; set; }   // FK — required on INSERT
        public string StaffName            { get; set; }   // JOIN result — used on SELECT
        public string ComplaintDescription { get; set; }   // nullable
        public string ComplaintStatus      { get; set; }   // Pending|Processing|Escalated|Completed
    }

    /// <summary>
    /// Maps to ReturnOrder table (JOINed with Order + Customer).
    /// StaffID — FK, used on INSERT.
    /// CustomerName — JOIN result, used on SELECT.
    /// </summary>
    public class ReturnOrderEntity
    {
        public string   ReturnID     { get; set; }
        public string   OrderID      { get; set; }
        public string   StaffID      { get; set; }   // FK — required on INSERT, nullable in DB
        public string   CustomerName { get; set; }   // JOIN result — used on SELECT
        public DateTime ReturnDate   { get; set; }
        public string   Reason       { get; set; }
        public double   RefundAmount { get; set; }
        public string   ReturnStatus { get; set; }   // Pending|Approved|Processing|Rejected|Completed
    }

    /// <summary>
    /// Accounts Receivable view: Invoice JOIN Order + Customer.
    /// IsOverdue = RemainingBalance &gt; 0 AND DueDate &lt; TODAY.
    /// </summary>
    public class AccountReceivableEntity
    {
        public string   InvoiceID        { get; set; }
        public string   OrderID          { get; set; }
        public string   CustomerName     { get; set; }
        public DateTime InvoiceDate      { get; set; }
        public double   TotalAmount      { get; set; }
        public double   PaidAmount       { get; set; }
        public double   RemainingBalance { get; set; }
        public string   PaymentStatus    { get; set; }
        public DateTime DueDate          { get; set; }
        public bool     IsOverdue        { get; set; }
    }

    /// <summary>
    /// Accounts Payable view: PurchaseInvoice JOIN Supplier.
    /// IsOverdue = PaymentStatus != 'Full' AND DueDate &lt; TODAY.
    /// DueDate is the canonical backing field.
    /// ExpectedDate is a View-layer alias for DueDate (AccountPayableForm.cs).
    /// </summary>
    public class AccountPayableEntity
    {
        public string   PurInvoiceID     { get; set; }
        public string   PurchaseID       { get; set; }   // alias for PurInvoiceID display
        public string   SupplierID       { get; set; }   // FK — used in Mapper
        public string   SupplierName     { get; set; }
        public DateTime PurInvoiceDate   { get; set; }   // invoice issue date
        public double   TotalAmount      { get; set; }
        public double   PaidAmount       { get; set; }
        public double   RemainingBalance { get; set; }
        public string   PaymentStatus    { get; set; }
        public DateTime DueDate          { get; set; }   // maps to DB DueDate column
        /// <summary>
        /// Alias for DueDate used by AccountPayableForm (View layer).
        /// Both properties read/write the same backing value.
        /// </summary>
        public DateTime ExpectedDate     { get => DueDate; set => DueDate = value; }
        public bool     IsOverdue        { get; set; }
    }

    // ══════════════════════════════════════════════════════════════════════════════
    //  AP 3-Way Verification Entity
    // ══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// PurchaseOrderLine row used in the AP Verification dialog item grid.
    /// </summary>
    public class APVerificationLineEntity
    {
        public string POLineID          { get; set; }
        public string RawMaterialItemID { get; set; }
        public string ItemName          { get; set; }
        public int    OrderQty          { get; set; }
        public double UnitPrice         { get; set; }
        public double LineTotal         => OrderQty * UnitPrice;
        public int    QtyReceived       { get; set; }   // SUM from Receipt table
    }

    /// <summary>
    /// Full detail for the AP 3-Way Match Verification dialog.
    ///
    /// 3-way match rule:
    ///   PurchaseOrder.POTotalAmount
    ///   == SUM(Receipt rows for this PO)   [SupplierReceiptTotal]
    ///   == PurchaseInvoice.TotalAmount
    ///   → All three equal  → IsMatched = true → ready to record as Account Payable
    /// </summary>
    public class APVerificationDetailVM
    {
        // ── PurchaseOrder fields ─────────────────────────────────────────
        public string   PurchaseID      { get; set; }
        public string   SupplierID      { get; set; }
        public string   SupplierName    { get; set; }
        public string   SupplierPhone   { get; set; }
        public string   SupplierAddress { get; set; }
        public DateTime OrderDate       { get; set; }
        public string   PurchaseStatus  { get; set; }
        public double   POTotalAmount   { get; set; }   // PurchaseOrder.POTotalAmount

        // ── PurchaseInvoice fields ───────────────────────────────────────
        public string   PurInvoiceID    { get; set; }
        public double   InvTotalAmount  { get; set; }   // PurchaseInvoice.TotalAmount
        public string   InvPayStatus    { get; set; }   // PurchaseInvoice.PaymentStatus
        public DateTime ExpectedDate    { get; set; }   // PurchaseInvoice.ExpectedDate

        // ── Supplier Receipt aggregate ───────────────────────────────────
        /// <summary>
        /// SUM of all Receipt.QtyReceived × PurchaseOrderLine.UnitPrice
        /// for this PurchaseOrder.
        /// </summary>
        public double SupplierReceiptTotal { get; set; }

        // ── Line items ───────────────────────────────────────────────────
        public List<APVerificationLineEntity> Lines { get; set; } = new List<APVerificationLineEntity>();

        // ── 3-Way Match result ───────────────────────────────────────────
        /// <summary>
        /// True when POTotalAmount == SupplierReceiptTotal == InvTotalAmount.
        /// Only matched records are eligible to be recorded as Account Payable.
        /// </summary>
        public bool IsMatched =>
            Math.Abs(POTotalAmount - SupplierReceiptTotal) < 0.005 &&
            Math.Abs(POTotalAmount - InvTotalAmount)       < 0.005;
    }

    // ══════════════════════════════════════════════════════════════════════════════
    //  After-Service ViewModels  (Controller → View)
    // ══════════════════════════════════════════════════════════════════════════════

    /// <summary>ViewModel for the Create Invoice page.</summary>
    public class CreateInvoiceViewModel
    {
        public string[]          AllowedMenus { get; set; }
        public UserBarViewModel  UserBar      { get; set; }
        public List<OrderEntity> Orders       { get; set; }
    }

    /// <summary>ViewModel for the Complaint List page.</summary>
    public class ComplaintListViewModel
    {
        public string[]              AllowedMenus { get; set; }
        public UserBarViewModel      UserBar      { get; set; }
        public List<ComplaintEntity> Complaints   { get; set; }
    }

    /// <summary>ViewModel for the Return Order List page.</summary>
    public class ReturnOrderListViewModel
    {
        public string[]                AllowedMenus { get; set; }
        public UserBarViewModel        UserBar      { get; set; }
        public List<ReturnOrderEntity> ReturnOrders { get; set; }
    }

    /// <summary>ViewModel for the Accounts Receivable page.</summary>
    public class AccountReceivableViewModel
    {
        public string[]                      AllowedMenus { get; set; }
        public UserBarViewModel              UserBar      { get; set; }
        public List<AccountReceivableEntity> Items        { get; set; }
    }

    /// <summary>ViewModel for the Accounts Payable page.</summary>
    public class AccountPayableViewModel
    {
        public string[]                    AllowedMenus { get; set; }
        public UserBarViewModel            UserBar      { get; set; }
        public List<AccountPayableEntity>  Items        { get; set; }
    }

    /// <summary>
    /// ViewModel for the Invoice List + Record Payment popup dialog
    /// (Account Receivable → [📋 Invoice List] button).
    /// </summary>
    public class InvoiceListViewModel
    {
        public string[]                  AllowedMenus { get; set; }
        public UserBarViewModel          UserBar      { get; set; }
        public List<InvoiceDetailEntity> Invoices     { get; set; } = new List<InvoiceDetailEntity>();
    }
}
