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
