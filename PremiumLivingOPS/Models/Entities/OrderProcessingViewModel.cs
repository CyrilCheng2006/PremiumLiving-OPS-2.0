using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.Entities
{
    // ── Shared shell info (reused across modules) ────────────────────────────
    // UserBarInfo is already declared in DashboardViewModel.cs

    // ══════════════════════════════════════════════════════════════════════════
    //  ORDER entity
    // ══════════════════════════════════════════════════════════════════════════
    public class OrderEntity
    {
        public string OrderID          { get; set; }
        public string QuotationID      { get; set; }
        public string CustomerID       { get; set; }
        public string CustomerName     { get; set; }
        public string AddressID        { get; set; }
        public string SalesID          { get; set; }
        public string SalesName        { get; set; }
        public DateTime IssuedTime     { get; set; }
        public DateTime DeliveryDate   { get; set; }
        public string ShippingAddress  { get; set; }
        public string BillingAddress   { get; set; }
        public double SubTotal         { get; set; }
        public string DiscountType     { get; set; }   // "Amount" | "Rate" | null
        public double DiscountValue    { get; set; }
        public double DiscountAmount   { get; set; }
        public double GrandTotal       { get; set; }
        public string OrderContactName { get; set; }
        public string OrderStatus      { get; set; }
    }

    // ── Order line item ────────────────────────────────────────────────────
    public class OrderLineEntity
    {
        public string OrderID  { get; set; }
        public string ItemID   { get; set; }
        public string ItemName { get; set; }
        public int    Quantity { get; set; }
        public double Price    { get; set; }
        public double LineTotal => Quantity * Price;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  QUOTATION entity
    // ══════════════════════════════════════════════════════════════════════════
    public class QuotationEntity
    {
        public string   QuotationID       { get; set; }
        public string   CustomerID        { get; set; }
        public string   CustomerName      { get; set; }
        public DateTime ExpiryDate        { get; set; }
        public double   TotalAmount       { get; set; }
        public double   DepositRequired   { get; set; }
        public string   LeadTimeEstimated { get; set; }
        public string   TermsandCondition { get; set; }
        public string   QuotationStatus   { get; set; }  // Converted | Rejected | Pending
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  CUSTOMER lookup
    // ══════════════════════════════════════════════════════════════════════════
    public class CustomerEntity
    {
        public string CustomerID   { get; set; }
        public string CustomerName { get; set; }
        public string Email        { get; set; }
        public string Phone        { get; set; }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  PRODUCT lookup (for order lines)
    // ══════════════════════════════════════════════════════════════════════════
    public class ProductLookup
    {
        public string ItemID      { get; set; }
        public string ItemName    { get; set; }
        public double SalesPrice  { get; set; }
        public string Category    { get; set; }
        public string DisplayText => $"{ItemID} – {ItemName}";
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  VIEW-ORDER tab ViewModel
    // ══════════════════════════════════════════════════════════════════════════
    public class ViewOrderViewModel
    {
        public UserBarInfo          UserBar      { get; set; } = new UserBarInfo();
        public string[]             AllowedMenus { get; set; } = new string[0];
        public List<OrderEntity>    Orders       { get; set; } = new List<OrderEntity>();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  QUOTATION tab ViewModel
    // ══════════════════════════════════════════════════════════════════════════
    public class QuotationViewModel
    {
        public UserBarInfo              UserBar      { get; set; } = new UserBarInfo();
        public string[]                 AllowedMenus { get; set; } = new string[0];
        public List<QuotationEntity>    Quotations   { get; set; } = new List<QuotationEntity>();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  CREATE-ORDER tab ViewModel
    // ══════════════════════════════════════════════════════════════════════════
    public class CreateOrderViewModel
    {
        public UserBarInfo           UserBar      { get; set; } = new UserBarInfo();
        public string[]              AllowedMenus { get; set; } = new string[0];
        public List<CustomerEntity>  Customers    { get; set; } = new List<CustomerEntity>();
        public List<ProductLookup>   Products     { get; set; } = new List<ProductLookup>();
        public List<QuotationEntity> PendingQuotations { get; set; } = new List<QuotationEntity>();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  MODIFY-ORDER tab ViewModel
    // ══════════════════════════════════════════════════════════════════════════
    public class ModifyOrderViewModel
    {
        public UserBarInfo          UserBar      { get; set; } = new UserBarInfo();
        public string[]             AllowedMenus { get; set; } = new string[0];
        public List<OrderEntity>    Orders       { get; set; } = new List<OrderEntity>();
        public List<CustomerEntity> Customers    { get; set; } = new List<CustomerEntity>();
        public List<ProductLookup>  Products     { get; set; } = new List<ProductLookup>();
    }
}
