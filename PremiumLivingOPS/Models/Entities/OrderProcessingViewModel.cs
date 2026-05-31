using System.Collections.Generic;

namespace PremiumLivingOPS.Models.Entities
{
    // ── Shared sub-models ─────────────────────────────────────────────────

    public class UserBarViewModel
    {
        public string DisplayName { get; set; }
        public string Department  { get; set; }
    }

    // ── Domain entities (map directly to DB tables) ─────────────────────────

    public class OrderEntity
    {
        public string          OrderID          { get; set; }
        public string          QuotationID      { get; set; }
        public string          CustomerID       { get; set; }
        public string          CustomerName     { get; set; }
        public string          AddressID        { get; set; }
        public string          SalesID          { get; set; }
        public string          SalesName        { get; set; }
        public System.DateTime IssuedTime       { get; set; }
        public System.DateTime DeliveryDate     { get; set; }
        public string          ShippingAddress  { get; set; }
        public string          BillingAddress   { get; set; }
        public double          SubTotal         { get; set; }
        public string          DiscountType     { get; set; }
        public double          DiscountValue    { get; set; }
        public double          DiscountAmount   { get; set; }
        public double          GrandTotal       { get; set; }
        public string          OrderContactName { get; set; }
        public string          OrderStatus      { get; set; }
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
        public string          QuotationID       { get; set; }
        public string          CustomerID        { get; set; }
        public string          CustomerName      { get; set; }
        public System.DateTime ExpiryDate        { get; set; }
        public double          TotalAmount       { get; set; }
        public double          DepositRequired   { get; set; }
        public string          LeadTimeEstimated { get; set; }
        public string          TermsandCondition { get; set; }
        public string          QuotationStatus   { get; set; }
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

        /// <summary>Convenience display string for ComboBox items.</summary>
        public string DisplayText => $"{ItemID}  –  {ItemName}  (HK$ {SalesPrice:N2})";
    }

    /// <summary>
    /// Represents one saved address record from the Address table.
    /// Schema: Address (AddressID, CustomerID, AddressName, AddressType, isDefault)
    ///
    /// Mapping:
    ///   AddressName  → FullAddress  (the actual address text)
    ///   AddressType  → Label        (Residential / Office / Mailing)
    ///   isDefault    → IsDefault    (marks the default shipping address)
    ///
    /// Selecting one in the AddressID ComboBox auto-fills the Shipping Address
    /// TextBox and stores AddressID on the Order.
    /// </summary>
    public class AddressLookup
    {
        /// <summary>PK from Address table (e.g. ADDR-0001).</summary>
        public string AddressId   { get; set; }

        /// <summary>FK — which customer owns this address.</summary>
        public string CustomerId  { get; set; }

        /// <summary>Address text as stored in Address.AddressName.</summary>
        public string FullAddress { get; set; }

        /// <summary>Address type from Address.AddressType (Residential / Office / Mailing).</summary>
        public string Label       { get; set; }

        /// <summary>Whether this is the customer's default address (Address.isDefault).</summary>
        public bool   IsDefault   { get; set; }

        /// <summary>
        /// ComboBox display text.
        /// Format: "ADDR-0001  –  Residential  –  123 Main St  [★ Default]"
        /// The ★ marker only appears when IsDefault is true.
        /// </summary>
        public string DisplayText =>
            $"{AddressId}  –  {Label}  –  {FullAddress}"
            + (IsDefault ? "  [★ Default]" : "");
    }

    // ── ViewModels (passed from Controller → View) ───────────────────────────

    public class ViewOrderViewModel
    {
        public UserBarViewModel  UserBar      { get; set; }
        public string[]          AllowedMenus { get; set; }
        public List<OrderEntity> Orders       { get; set; }
    }

    /// <summary>Full detail of one order including its line items.</summary>
    public class OrderDetailViewModel
    {
        public OrderEntity           Order { get; set; }
        public List<OrderLineEntity> Lines { get; set; }
    }

    public class QuotationViewModel
    {
        public UserBarViewModel      UserBar      { get; set; }
        public string[]              AllowedMenus { get; set; }
        public List<QuotationEntity> Quotations   { get; set; }
    }

    public class CreateOrderViewModel
    {
        public UserBarViewModel      UserBar           { get; set; }
        public string[]              AllowedMenus      { get; set; }
        public List<CustomerEntity>  Customers         { get; set; }
        public List<AddressLookup>   Addresses         { get; set; }  // all addresses; filtered in View by CustomerID
        public List<ProductLookup>   Products          { get; set; }
        public List<QuotationEntity> Quotations        { get; set; }
        public List<QuotationEntity> PendingQuotations { get; set; }

        /// <summary>
        /// Pre-generated OrderID in ORD-YYYYMMDD-NNNN format.
        /// Generated by Controller via GenerateOrderId() and displayed read-only in the View.
        /// </summary>
        public string NextOrderId { get; set; }
    }

    public class ModifyOrderViewModel
    {
        public UserBarViewModel      UserBar       { get; set; }
        public string[]              AllowedMenus  { get; set; }
        public OrderEntity           SelectedOrder { get; set; }
        public List<OrderLineEntity> Lines         { get; set; }
        public List<ProductLookup>   Products      { get; set; }

        /// <summary>All customers — for the Customer ComboBox in Order Details.</summary>
        public List<CustomerEntity>  Customers     { get; set; }

        /// <summary>All addresses — filtered by CustomerID when Customer changes.</summary>
        public List<AddressLookup>   Addresses     { get; set; }
    }
}
