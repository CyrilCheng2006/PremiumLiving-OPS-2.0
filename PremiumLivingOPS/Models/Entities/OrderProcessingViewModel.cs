using System.Collections.Generic;

namespace PremiumLivingOPS.Models.Entities
{
    // ── Shared sub-models ────────────────────────────────────────────────────

    public class UserBarViewModel
    {
        public string DisplayName { get; set; }
        public string Department  { get; set; }
    }

    // ── Domain entities (map directly to DB tables) ──────────────────────────

    public class OrderEntity
    {
        public string   OrderID          { get; set; }
        public string   QuotationID      { get; set; }
        public string   CustomerID       { get; set; }
        public string   CustomerName     { get; set; }
        public string   AddressID        { get; set; }
        public string   SalesID          { get; set; }
        public string   SalesName        { get; set; }
        public System.DateTime IssuedTime   { get; set; }
        public System.DateTime DeliveryDate { get; set; }
        public string   ShippingAddress  { get; set; }
        public string   BillingAddress   { get; set; }
        public double   SubTotal         { get; set; }
        public string   DiscountType     { get; set; }
        public double   DiscountValue    { get; set; }
        public double   DiscountAmount   { get; set; }
        public double   GrandTotal       { get; set; }
        public string   OrderContactName { get; set; }
        public string   OrderStatus      { get; set; }
    }

    public class OrderLineEntity
    {
        public string OrderID  { get; set; }
        public string ItemID   { get; set; }
        public string ItemName { get; set; }
        public int    Quantity { get; set; }
        public double Price    { get; set; }
        public double LineTotal => Quantity * Price;
    }

    public class QuotationEntity
    {
        public string   QuotationID       { get; set; }
        public string   CustomerID        { get; set; }
        public string   CustomerName      { get; set; }
        public System.DateTime ExpiryDate { get; set; }
        public double   TotalAmount       { get; set; }
        public double   DepositRequired   { get; set; }
        public string   LeadTimeEstimated { get; set; }
        public string   TermsandCondition { get; set; }
        public string   QuotationStatus   { get; set; }
    }

    public class CustomerEntity
    {
        public string CustomerID   { get; set; }
        public string CustomerName { get; set; }
        public string Email        { get; set; }
        public string Phone        { get; set; }
    }

    public class ProductLookup
    {
        public string ItemID     { get; set; }
        public string ItemName   { get; set; }
        public double SalesPrice { get; set; }
        public string Category   { get; set; }
    }

    // ── ViewModels (passed from Controller → View) ───────────────────────────

    public class ViewOrderViewModel
    {
        public UserBarViewModel      UserBar      { get; set; }
        public List<string>          AllowedMenus { get; set; }
        public List<OrderEntity>     Orders       { get; set; }
    }

    /// <summary>Full detail of one order including its line items.</summary>
    public class OrderDetailViewModel
    {
        public OrderEntity           Order { get; set; }
        public List<OrderLineEntity> Lines { get; set; }
    }

    public class QuotationViewModel
    {
        public UserBarViewModel         UserBar      { get; set; }
        public List<string>             AllowedMenus { get; set; }
        public List<QuotationEntity>    Quotations   { get; set; }
    }

    public class CreateOrderViewModel
    {
        public UserBarViewModel      UserBar      { get; set; }
        public List<string>          AllowedMenus { get; set; }
        public List<CustomerEntity>  Customers    { get; set; }
        public List<ProductLookup>   Products     { get; set; }
        public List<QuotationEntity> Quotations   { get; set; }
    }

    public class ModifyOrderViewModel
    {
        public UserBarViewModel      UserBar       { get; set; }
        public List<string>          AllowedMenus  { get; set; }
        public OrderEntity           SelectedOrder { get; set; }
        public List<OrderLineEntity> Lines         { get; set; }
        public List<ProductLookup>   Products      { get; set; }
    }
}
