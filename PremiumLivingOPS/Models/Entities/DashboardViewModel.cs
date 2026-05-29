using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.Entities
{
    // ── KPI summary row ──────────────────────────────────────────────
    public class DashboardKpi
    {
        public string Label       { get; set; }
        public string Value       { get; set; }
        public string SubText     { get; set; }
        /// <summary>Category key used by the View to map accent colour.
        /// Values: Primary | Success | Warning | Danger | Info</summary>
        public string AccentKey   { get; set; }
    }

    // ── Recent Orders ────────────────────────────────────────────────
    public class OrderSummaryRow
    {
        public string OrderId    { get; set; }
        public string Customer   { get; set; }
        public string Total      { get; set; }   // formatted, e.g. "HK$21,300"
        public string Status     { get; set; }   // Processing | Shipped | Pending | Delivered
    }

    // ── Pending Quotations ───────────────────────────────────────────
    public class QuotationSummaryRow
    {
        public string QuotationId { get; set; }
        public string Customer    { get; set; }
        public string Amount      { get; set; }
        public string ValidUntil  { get; set; }  // formatted date string
    }

    // ── Active Shipments ─────────────────────────────────────────────
    public class ShipmentSummaryRow
    {
        public string ShipmentId  { get; set; }
        public string Customer    { get; set; }
        public string SchedDate   { get; set; }  // formatted date string
        public string Status      { get; set; }  // Scheduled | In Transit | Delivered
    }

    // ── Supplier Payments ────────────────────────────────────────────
    public class SupplierPaymentRow
    {
        public string Supplier    { get; set; }
        public string InvoiceId   { get; set; }
        public string Amount      { get; set; }
        public string Status      { get; set; }  // Pending | Overdue | Paid
    }

    // ── Activity Feed ────────────────────────────────────────────────
    public class ActivityRow
    {
        /// <summary>Category key used by the View to map dot colour.
        /// Values: Primary | Success | Warning | Danger</summary>
        public string CategoryKey { get; set; }
        public string BoldText    { get; set; }
        public string NormalText  { get; set; }
        public string TimeLabel   { get; set; }
    }

    // ── Low-Stock Alerts ─────────────────────────────────────────────
    public class LowStockRow
    {
        public string ItemName    { get; set; }
        public int    OnHand      { get; set; }
        public int    MinimumQty  { get; set; }
        /// <summary>Critical (onHand &lt; min/2) or Low (onHand &lt; min)</summary>
        public string Status      { get; set; }
    }

    // ── Top-level ViewModel returned by DashboardController ──────────
    public class DashboardViewModel
    {
        // Row 0 — KPI cards
        public List<DashboardKpi>        Kpis        { get; set; } = new List<DashboardKpi>();

        // Row 1
        public List<OrderSummaryRow>     Orders      { get; set; } = new List<OrderSummaryRow>();
        public List<LowStockRow>         LowStock    { get; set; } = new List<LowStockRow>();

        // Row 2
        public List<QuotationSummaryRow> Quotations  { get; set; } = new List<QuotationSummaryRow>();
        public List<ShipmentSummaryRow>  Shipments   { get; set; } = new List<ShipmentSummaryRow>();

        // Row 3
        public List<SupplierPaymentRow>  Suppliers   { get; set; } = new List<SupplierPaymentRow>();
        public List<ActivityRow>         Activities  { get; set; } = new List<ActivityRow>();
    }
}
