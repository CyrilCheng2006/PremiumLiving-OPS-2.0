using System.Collections.Generic;

namespace PremiumLivingOPS.Models.Entities
{
    // ── Order domain entities (map directly to DB tables) ──────────────────────

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
        public string OrderID   { get; set; }
        public string ItemID    { get; set; }
        public string ItemName  { get; set; }
        public int    Quantity  { get; set; }
        public double Price     { get; set; }
        public double LineTotal => Quantity * Price;
    }

    public class QuotationEntity
    {
        public string          QuotationID       { get; set; }
        public string          CustomerID        { get; set; }
        public string          CustomerName      { get; set; }
        public System.DateTime IssuedDate        { get; set; }
        public System.DateTime ExpiryDate        { get; set; }
        public double          TotalAmount       { get; set; }
        public double          DepositRequired   { get; set; }
        public string          LeadTimeEstimated { get; set; }
        public string          TermsandCondition { get; set; }
        public string          QuotationStatus   { get; set; }
        public string          Status            => QuotationStatus;  // picker-friendly alias
        public string          SalesStaffName    { get; set; }
        public string          Notes             { get; set; }

        /// <summary>Line items — populated only by GetQuotationDetail, null in list queries.</summary>
        public List<QuotationItemEntity> Items   { get; set; }
    }

    /// <summary>
    /// One line item inside a Quotation.
    /// ItemID references Item(ItemID) / Product(ItemID) in the DB.
    /// ProductName is the display name carried from Item.ItemName.
    /// </summary>
    public class QuotationItemEntity
    {
        public string QuotationID      { get; set; }
        /// <summary>FK → Item.ItemID (also Product.ItemID). Used when converting to OrderLine.</summary>
        public string ItemID           { get; set; }
        public string ProductName      { get; set; }
        public int    Quantity         { get; set; }
        public string Unit             { get; set; }
        public double UnitPrice        { get; set; }
        public double DiscountPercent  { get; set; }
        public double Subtotal         => Quantity * UnitPrice * (1 - DiscountPercent / 100.0);
        public string ItemNote         { get; set; }
    }

    public class ProductLookup
    {
        public string ItemID     { get; set; }
        public string ItemName   { get; set; }
        public double SalesPrice { get; set; }
        public string Category   { get; set; }

        /// <summary>Convenience display string for ComboBox items.</summary>
        public string DisplayText => $"{ItemID}  \u2013  {ItemName}  (HK$ {SalesPrice:N2})";
    }

    /// <summary>
    /// Represents one saved address record from the Address table.
    /// Schema: Address (AddressID, CustomerID, AddressName, AddressType, isDefault)
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
        /// </summary>
        public string DisplayText =>
            $"{AddressId}  \u2013  {Label}  \u2013  {FullAddress}"
            + (IsDefault ? "  [\u2605 Default]" : "");
    }

    // ── ViewModels (passed from Controller → View) ─────────────────────────────
    // NOTE: UserBarViewModel is defined in AfterServiceViewModels.cs (shared).
    // NOTE: CustomerEntity is defined in CustomerEntity.cs (canonical).

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
        public List<AddressLookup>   Addresses         { get; set; }
        public List<ProductLookup>   Products          { get; set; }
        public List<QuotationEntity> Quotations        { get; set; }
        public List<QuotationEntity> PendingQuotations { get; set; }

        /// <summary>
        /// Pre-generated OrderID in ORD-YYYYMMDD-NNNN format.
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
        public List<CustomerEntity>  Customers     { get; set; }
        public List<AddressLookup>   Addresses     { get; set; }

        /// <summary>All quotations available for linking (used by Modify Order picker).</summary>
        public List<QuotationEntity> Quotations    { get; set; }
    }
}
