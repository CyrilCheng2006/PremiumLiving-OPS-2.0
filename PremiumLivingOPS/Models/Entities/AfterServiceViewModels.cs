using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.Entities
{
    // ── UserBarViewModel is already defined in OrderProcessingViewModel.cs ──────
    // Do NOT re-declare UserBarViewModel here — use the one from that file.

    // ══════════════════════════════════════════════════════════════════════════════
    //  After-Service Domain Entities
    // ══════════════════════════════════════════════════════════════════════════════

    // ── Create Invoice ────────────────────────────────────────────────────────────

    /// <summary>
    /// A lightweight Order row shown in the Create Invoice picker.
    /// Replaces the generic OrderEntity for this narrowly-scoped use case.
    /// Maps to: Order JOIN Customer WHERE OrderStatus IN ('Delivered','Completed')
    ///           AND no existing Invoice.
    /// </summary>
    public class OrderForInvoiceEntity
    {
        public string   OrderID      { get; set; }
        public string   CustomerID   { get; set; }
        public string   CustomerName { get; set; }
        public double   GrandTotal   { get; set; }
        public DateTime IssuedTime   { get; set; }
        public string   OrderStatus  { get; set; }
    }

    /// <summary>
    /// Maps to the Invoice table row on INSERT.
    /// On SELECT the Repo populates CustomerName from a JOIN.
    /// </summary>
    public class InvoiceEntity
    {
        public string   InvoiceID        { get; set; }
        public string   OrderID          { get; set; }
        public string   CustomerID       { get; set; }   // FK — required on INSERT
        public string   CustomerName     { get; set; }   // JOIN result — SELECT only
        public string   StaffID          { get; set; }   // FK — required on INSERT
        public double   TotalAmount      { get; set; }
        public double   PaidAmount       { get; set; }
        public double   RemainingBalance { get; set; }
        public string   PaymentStatus    { get; set; }   // 'Partial' | 'Full'
        public string   PaymentMethod    { get; set; }   // nullable
        public DateTime IssuedDate       { get; set; }   // maps to Invoice.IssuedDate column
    }

    // ── Complaint ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Maps to Complaint table (JOINed with Staff).
    /// AssignedStaffID — used on INSERT (FK to Staff).
    /// AssignedStaffName — resolved via JOIN, populated on SELECT only.
    /// </summary>
    public class ComplaintEntity
    {
        public string   ComplaintID       { get; set; }
        public string   OrderID           { get; set; }   // nullable
        public string   AssignedStaffID   { get; set; }   // FK — nullable
        public string   AssignedStaffName { get; set; }   // JOIN result
        public string   CustomerName      { get; set; }   // JOIN result
        public string   ComplaintType     { get; set; }
        public string   ComplaintStatus   { get; set; }   // Pending|Processing|Escalated|Completed
        public string   Description       { get; set; }   // nullable
        public DateTime CreatedDate       { get; set; }
    }

    // ── Return Order ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Maps to ReturnOrder table (JOINed with Order + Customer).
    /// </summary>
    public class ReturnOrderEntity
    {
        public string   ReturnID     { get; set; }
        public string   OrderID      { get; set; }
        public string   CustomerName { get; set; }   // JOIN result
        public DateTime ReturnDate   { get; set; }
        public string   Reason       { get; set; }
        public double   RefundAmount { get; set; }
        public string   ReturnStatus { get; set; }   // Pending|Approved|Processing|Rejected|Completed
    }

    // ── Account Receivable ────────────────────────────────────────────────────────

    /// <summary>
    /// Accounts Receivable view: Invoice JOIN Order + Customer.
    /// IsOverdue = RemainingBalance &gt; 0 AND DueDate &lt; TODAY.
    /// </summary>
    public class AccountReceivableEntity
    {
        public string   InvoiceID        { get; set; }
        public string   OrderID          { get; set; }
        public string   CustomerName     { get; set; }
        public double   TotalAmount      { get; set; }
        public double   PaidAmount       { get; set; }
        public double   RemainingBalance { get; set; }
        public string   PaymentStatus    { get; set; }
        public DateTime IssuedDate       { get; set; }
        public bool     IsOverdue        { get; set; }
    }

    // ── Account Payable ───────────────────────────────────────────────────────────

    /// <summary>
    /// Accounts Payable view: PurchaseInvoice JOIN Supplier.
    /// IsOverdue = PaymentStatus != 'Full' AND ExpectedPaymentDate &lt; TODAY.
    /// </summary>
    public class AccountPayableEntity
    {
        public string   PurInvoiceID  { get; set; }
        public string   PurchaseID    { get; set; }
        public string   SupplierName  { get; set; }
        public double   TotalAmount   { get; set; }
        public string   PaymentStatus { get; set; }
        public DateTime ExpectedDate  { get; set; }   // maps to ExpectedPaymentDate column
        public bool     IsOverdue     { get; set; }
    }

    // ── AP 3-Way Verification ─────────────────────────────────────────────────────

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
        public string   PurchaseID           { get; set; }
        public string   SupplierID           { get; set; }
        public string   SupplierName         { get; set; }
        public string   SupplierPhone        { get; set; }
        public string   SupplierAddress      { get; set; }
        public DateTime OrderDate            { get; set; }
        public string   PurchaseStatus       { get; set; }
        public double   POTotalAmount        { get; set; }   // PurchaseOrder.POTotalAmount

        public string   PurInvoiceID         { get; set; }
        public double   InvTotalAmount       { get; set; }   // PurchaseInvoice.TotalAmount
        public string   InvPayStatus         { get; set; }   // PurchaseInvoice.PaymentStatus
        public DateTime ExpectedDate         { get; set; }   // PurchaseInvoice.ExpectedPaymentDate

        /// <summary>SUM of Receipt.QtyReceived × PurchaseOrderLine.UnitPrice.</summary>
        public double   SupplierReceiptTotal { get; set; }

        public List<APVerificationLineEntity> Lines { get; set; } = new List<APVerificationLineEntity>();

        /// <summary>
        /// True when all three amounts agree within ±0.005 tolerance.
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
        public string[]                    AllowedMenus { get; set; }
        public UserBarViewModel            UserBar      { get; set; }
        public List<OrderForInvoiceEntity> Orders       { get; set; }
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
        public string[]                   AllowedMenus { get; set; }
        public UserBarViewModel           UserBar      { get; set; }
        public List<AccountPayableEntity> Items        { get; set; }
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
