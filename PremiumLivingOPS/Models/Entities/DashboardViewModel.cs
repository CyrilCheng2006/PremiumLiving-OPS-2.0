using System.Collections.Generic;

namespace PremiumLivingOPS.Models.Entities
{
    // ── Top-level ViewModel ──────────────────────────────────────────────────
    public class DashboardViewModel
    {
        public UserBarInfo        UserBar       { get; set; } = new UserBarInfo();
        public string[]           AllowedMenus  { get; set; } = new string[0];

        /// <summary>
        /// Controls which Dashboard UI blocks are rendered.
        /// Populated by DashboardController based on the current user's department
        /// (mirrors the NavAccessPolicy menu-access matrix).
        /// </summary>
        public DashboardSections  Sections      { get; set; } = new DashboardSections();

        public List<DashboardKpi>        Kpis        { get; set; } = new List<DashboardKpi>();
        public List<OrderSummaryRow>     Orders      { get; set; } = new List<OrderSummaryRow>();
        public List<LowStockRow>         LowStock    { get; set; } = new List<LowStockRow>();
        public List<QuotationSummaryRow> Quotations  { get; set; } = new List<QuotationSummaryRow>();
        public List<ShipmentSummaryRow>  Shipments   { get; set; } = new List<ShipmentSummaryRow>();
        public List<SupplierPaymentRow>  Suppliers   { get; set; } = new List<SupplierPaymentRow>();
        public List<ActivityRow>         Activities  { get; set; } = new List<ActivityRow>();
    }

    // ── Section-visibility flags ─────────────────────────────────────────────
    /// <summary>
    /// Each boolean flag maps 1-to-1 to a UI panel/card on the Dashboard.
    /// The Controller sets these based on department; the View reads them.
    /// </summary>
    public class DashboardSections
    {
        // ── KPI Row 1 ────────────────────────────────────────────────────────
        public bool ShowKpiOrders      { get; set; }   // Total Orders
        public bool ShowKpiDelivered   { get; set; }   // Delivered This Month
        public bool ShowKpiQuotations  { get; set; }   // Pending Quotations
        public bool ShowKpiLowStock    { get; set; }   // Low Stock Alerts

        // ── KPI Row 2 ────────────────────────────────────────────────────────
        public bool ShowKpiRevenue     { get; set; }   // Revenue This Month
        public bool ShowKpiAR          { get; set; }   // Outstanding AR
        public bool ShowKpiSuppliers   { get; set; }   // Total Suppliers
        public bool ShowKpiCustomers   { get; set; }   // Total Customers

        // ── Section Cards ────────────────────────────────────────────────────
        public bool ShowRecentOrders      { get; set; }   // Recent Orders grid
        public bool ShowLowStock          { get; set; }   // Low Stock Alerts grid
        public bool ShowPendingQuotations { get; set; }   // Pending Quotations grid
        public bool ShowActiveShipments   { get; set; }   // Active Shipments grid
        public bool ShowSupplierPayments  { get; set; }   // Supplier Payment Status grid
        public bool ShowRecentActivity    { get; set; }   // Recent Activity feed (always shown if any other section is shown)
    }

    // ── KPI card DTO ─────────────────────────────────────────────────────────
    public class DashboardKpi
    {
        public string Label     { get; set; } = string.Empty;
        public string Value     { get; set; } = "–";
        public string SubText   { get; set; } = string.Empty;
        public string AccentKey { get; set; } = "Primary";
    }

    // ── User bar DTO ─────────────────────────────────────────────────────────
    public class UserBarInfo
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Department  { get; set; } = string.Empty;
    }

    // ── Tabular row DTOs ─────────────────────────────────────────────────────
    public class OrderSummaryRow
    {
        public string OrderId  { get; set; } = string.Empty;
        public string Customer { get; set; } = string.Empty;
        public string Total    { get; set; } = string.Empty;
        public string Status   { get; set; } = string.Empty;
    }

    public class LowStockRow
    {
        public string ItemName   { get; set; } = string.Empty;
        public int    OnHand     { get; set; }
        public int    MinimumQty { get; set; }
        public string Status     { get; set; } = "Low";
    }

    public class QuotationSummaryRow
    {
        public string QuotationId { get; set; } = string.Empty;
        public string Customer    { get; set; } = string.Empty;
        public string Amount      { get; set; } = string.Empty;
        public string ValidUntil  { get; set; } = string.Empty;
    }

    public class ShipmentSummaryRow
    {
        public string ShipmentId { get; set; } = string.Empty;
        public string Customer   { get; set; } = string.Empty;
        public string SchedDate  { get; set; } = string.Empty;
        public string Status     { get; set; } = string.Empty;
    }

    public class SupplierPaymentRow
    {
        public string Supplier  { get; set; } = string.Empty;
        public string InvoiceId { get; set; } = string.Empty;
        public string Amount    { get; set; } = string.Empty;
        public string Status    { get; set; } = string.Empty;
    }

    public class ActivityRow
    {
        public string CategoryKey { get; set; } = "Primary";
        public string BoldText    { get; set; } = string.Empty;
        public string NormalText  { get; set; } = string.Empty;
        public string TimeLabel   { get; set; } = string.Empty;
    }
}
