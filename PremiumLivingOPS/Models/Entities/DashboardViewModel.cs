using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.Entities
{
    // ── User-bar info (set by Controller from SessionManager) ────────
    /// <summary>
    /// Carries the display strings for the User Bar so the View never
    /// needs to touch SessionManager or the Staff entity directly.
    /// </summary>
    public class UserBarInfo
    {
        /// <summary>Staff member's full name, e.g. "Chan Ho Yuen".</summary>
        public string DisplayName  { get; set; } = string.Empty;

        /// <summary>Department label shown in parentheses, e.g. "Sales".</summary>
        public string Department   { get; set; } = string.Empty;
    }

    // ── KPI summary row ──────────────────────────────────────────
    public class DashboardKpi
    {
        public string Label     { get; set; }
        public string Value     { get; set; }
        public string SubText   { get; set; }
        /// <summary>Values: Primary | Success | Warning | Danger | Info</summary>
        public string AccentKey { get; set; }
    }

    // ── Recent Orders ───────────────────────────────────────────
    public class OrderSummaryRow
    {
        public string OrderId  { get; set; }
        public string Customer { get; set; }
        public string Total    { get; set; }
        public string Status   { get; set; }
    }

    // ── Pending Quotations ────────────────────────────────────
    public class QuotationSummaryRow
    {
        public string QuotationId { get; set; }
        public string Customer    { get; set; }
        public string Amount      { get; set; }
        public string ValidUntil  { get; set; }
    }

    // ── Active Shipments ───────────────────────────────────────
    public class ShipmentSummaryRow
    {
        public string ShipmentId { get; set; }
        public string Customer   { get; set; }
        public string SchedDate  { get; set; }
        public string Status     { get; set; }
    }

    // ── Supplier Payments ──────────────────────────────────────
    public class SupplierPaymentRow
    {
        public string Supplier  { get; set; }
        public string InvoiceId { get; set; }
        public string Amount    { get; set; }
        public string Status    { get; set; }
    }

    // ── Activity Feed ──────────────────────────────────────────
    public class ActivityRow
    {
        /// <summary>Values: Primary | Success | Warning | Danger</summary>
        public string CategoryKey { get; set; }
        public string BoldText    { get; set; }
        public string NormalText  { get; set; }
        public string TimeLabel   { get; set; }
    }

    // ── Low-Stock Alerts ────────────────────────────────────────
    public class LowStockRow
    {
        public string ItemName   { get; set; }
        public int    OnHand     { get; set; }
        public int    MinimumQty { get; set; }
        /// <summary>Critical (onHand &lt; min/2) or Low (onHand &lt; min)</summary>
        public string Status     { get; set; }
    }

    // ── Top-level ViewModel returned by DashboardController ──────────
    public class DashboardViewModel
    {
        // — User Bar ——————————————————————————————————
        /// <summary>
        /// Display data for the User Bar.
        /// Populated by DashboardController from SessionManager.
        /// The View binds directly from here — it never reads SessionManager.
        /// </summary>
        public UserBarInfo UserBar { get; set; } = new UserBarInfo();

        // — KPI cards ————————————————————————————————
        public List<DashboardKpi> Kpis { get; set; } = new List<DashboardKpi>();

        // — Row 1 —————————————————————————————————————
        public List<OrderSummaryRow>     Orders     { get; set; } = new List<OrderSummaryRow>();
        public List<LowStockRow>         LowStock   { get; set; } = new List<LowStockRow>();

        // — Row 2 —————————————————————————————————————
        public List<QuotationSummaryRow> Quotations { get; set; } = new List<QuotationSummaryRow>();
        public List<ShipmentSummaryRow>  Shipments  { get; set; } = new List<ShipmentSummaryRow>();

        // — Row 3 —————————————————————————————————————
        public List<SupplierPaymentRow>  Suppliers  { get; set; } = new List<SupplierPaymentRow>();
        public List<ActivityRow>         Activities { get; set; } = new List<ActivityRow>();
    }
}
