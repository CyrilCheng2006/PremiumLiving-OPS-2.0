using System.Collections.Generic;

namespace PremiumLivingOPS.Models.Entities
{
    // ── Order domain entities (map directly to DB tables) ──────────────────────────────

    public class OrderEntity
    {
        public string           OrderID          { get; set; }
        public string           QuotationID      { get; set; }
        public string           CustomerID       { get; set; }
        public string           CustomerName     { get; set; }
        public string           AddressID        { get; set; }
        public string           SalesID          { get; set; }
        public string           SalesName        { get; set; }
        public System.DateTime  IssuedTime       { get; set; }
        // Nullable: DB column DeliveryDate allows NULL
        public System.DateTime? DeliveryDate     { get; set; }
        public string           ShippingAddress  { get; set; }
        public string           BillingAddress   { get; set; }
        public double           SubTotal         { get; set; }
        public string           DiscountType     { get; set; }
        public double           DiscountValue    { get; set; }
        public double           DiscountAmount   { get; set; }
        public double           GrandTotal       { get; set; }
        public string           OrderContactName { get; set; }
        public string           OrderStatus      { get; set; }
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

    /// <summary>
    /// Maps directly to the Quotation table in schema.sql.
    /// Columns: QuotationID, CustomerID, ExpiryDate, TotalAmount,
    ///          DepositRequired (DOUBLE DEFAULT NULL), LeadTimeEstimated,
    ///          TermsandCondition, QuotationStatus.
    ///
    /// DepositRequired is double? (nullable) because the DB column is DEFAULT NULL.
    /// SalesStaffName is NOT a Quotation column; populated by controller JOIN.
    /// There is no QuotationItem table in the schema.
    /// </summary>
    public class QuotationEntity
    {
        public string          QuotationID       { get; set; }
        public string          CustomerID        { get; set; }
        public string          CustomerName      { get; set; }
        public System.DateTime IssuedDate        { get; set; }
        public System.DateTime ExpiryDate        { get; set; }
        public double          TotalAmount       { get; set; }

        /// <summary>
        /// Schema: DepositRequired DOUBLE(10,2) DEFAULT NULL.
        /// Nullable because a quotation may not require any deposit.
        /// </summary>
        public double?         DepositRequired   { get; set; }

        public string          LeadTimeEstimated { get; set; }
        public string          TermsandCondition { get; set; }
        public string          QuotationStatus   { get; set; }
        public string          Status            => QuotationStatus;

        /// <summary>Display helper — populated by controller JOIN. Not a DB column.</summary>
        public string          SalesStaffName    { get; set; }

        /// <summary>Line items — populated only by GetQuotationDetail via in-memory cache.</summary>
        public List<QuotationItemEntity> Items   { get; set; }
    }

    /// <summary>
    /// One line item held in the in-memory quotation cache.
    /// Unit and DiscountPercent are in-memory display helpers only.
    /// NOT persisted to DB.
    /// </summary>
    public class QuotationItemEntity
    {
        public string QuotationID      { get; set; }
        public string ItemID           { get; set; }
        public string ProductName      { get; set; }
        public int    Quantity         { get; set; }
        public double UnitPrice        { get; set; }
        public double Subtotal         => Quantity * UnitPrice;

        /// <summary>In-memory display helper (e.g. "pcs"). NOT persisted to DB.</summary>
        public string Unit             { get; set; } = string.Empty;

        /// <summary>In-memory display helper (0-100). NOT persisted to DB.</summary>
        public double DiscountPercent  { get; set; } = 0;
    }

    public class ProductLookup
    {
        public string ItemID      { get; set; }
        public string ItemName    { get; set; }
        public double SalesPrice  { get; set; }
        public string Category    { get; set; }
        public string DisplayText => $"{ItemID}  -  {ItemName}  (HK$ {SalesPrice:N2})";
    }

    /// <summary>
    /// Schema: Address (AddressID, CustomerID, AddressName, AddressType, isDefault)
    /// </summary>
    public class AddressLookup
    {
        public string AddressId   { get; set; }
        public string CustomerId  { get; set; }
        public string FullAddress { get; set; }
        public string Label       { get; set; }
        public bool   IsDefault   { get; set; }
        public string DisplayText =>
            $"{AddressId}  -  {Label}  -  {FullAddress}"
            + (IsDefault ? "  [Default]" : "");
    }

    // ── ViewModels ──────────────────────────────────────────────────────────────────────────────────

    public class ViewOrderViewModel
    {
        public UserBarViewModel  UserBar      { get; set; }
        public string[]          AllowedMenus { get; set; }
        public List<OrderEntity> Orders       { get; set; }
    }

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

    public class CreateQuotationViewModel
    {
        public UserBarViewModel     UserBar         { get; set; }
        public string[]             AllowedMenus    { get; set; }
        public List<CustomerEntity> Customers       { get; set; }
        public List<ProductLookup>  Products        { get; set; }
        public string               NextQuotationId { get; set; }
        public string               SalesStaffName  { get; set; }
        public string               SalesStaffId    { get; set; }
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
        public string                NextOrderId       { get; set; }
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
        public List<QuotationEntity> Quotations    { get; set; }
    }
}
