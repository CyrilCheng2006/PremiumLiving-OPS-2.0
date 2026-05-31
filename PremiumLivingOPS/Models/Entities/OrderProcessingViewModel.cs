using System.Collections.Generic;

namespace PremiumLivingOPS.Models.Entities
{
    // ── Shared sub-models ────────────────────────────────────────────────────

    public class UserBarViewModel
    {
        public string DisplayName { get; set; }
        public string Department  { get; set; }
    }

    // ── Domain entities (map directly to DB tables) ────────────────────────────

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
    /// Represents one saved address record for a customer.
    /// Used to populate the Shipping / Billing address ComboBoxes
    /// in CreateOrderForm without exposing raw address strings in the View.
    /// AddressId is stored back into OrderEntity.ShippingAddress /
    /// BillingAddress as the resolved full-text address.
    /// </summary>
    public class AddressLookup
    {
        /// <summary>Surrogate key (e.g. ADDR-0001 or a DB auto-ID string).</summary>
        public string AddressId   { get; set; }

        /// <summary>Full address text as stored in the DB.</summary>
        public string FullAddress { get; set; }

        /// <summary>Short label shown in the ComboBox drop-down.</summary>
        public string Label       { get; set; }

        /// <summary>ComboBox display text: ID + label snippet.</summary>
        public string DisplayText =>
            string.IsNullOrEmpty(Label) ? FullAddress : $"{AddressId}  –  {Label}";
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
    }
}
