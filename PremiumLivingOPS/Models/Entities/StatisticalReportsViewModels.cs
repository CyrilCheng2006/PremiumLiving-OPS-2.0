using System;
using System.Collections.Generic;

// ============================================================
//  FILE: Models/Entities/StatisticalReportsViewModels.cs
//
//  Contains all data projections and page-level ViewModels
//  required by the Statistical Reports module.
//
//  Report types surfaced in the UI:
//    1. Sales Performance     — Orders / Revenue by period
//    2. Inventory Status      — Stock levels + reorder alerts
//    3. Procurement Summary   — PO spend + supplier breakdown
//    4. Logistics Overview    — Shipment status distribution
//    5. After-Service Summary — Complaints + Returns + Refunds
//    6. Finance Overview      — Revenue / AP / AR / Transactions
// ============================================================

namespace PremiumLivingOPS.Models.Entities
{
    // ── Report type catalogue ───────────────────────────────────────────
    public enum ReportType
    {
        SalesPerformance    = 0,
        InventoryStatus     = 1,
        ProcurementSummary  = 2,
        LogisticsOverview   = 3,
        AfterServiceSummary = 4,
        FinanceOverview     = 5
    }

    // ════════════════════════════════════════════════════════════════════
    //  1. SALES PERFORMANCE
    // ════════════════════════════════════════════════════════════════════

    /// <summary>KPI summary row for Sales Performance report.</summary>
    public class SalesKpiEntity
    {
        public int    TotalOrders      { get; set; }
        public double TotalRevenue     { get; set; }
        public double AverageOrderValue{ get; set; }
        public int    DeliveredOrders  { get; set; }
        public int    PendingOrders    { get; set; }
        public int    ProcessingOrders { get; set; }
        public int    CancelledOrders  { get; set; }
    }

    /// <summary>One detail row in the Sales Performance grid.</summary>
    public class SalesOrderRowEntity
    {
        public string OrderID      { get; set; }
        public string CustomerName { get; set; }
        public string OrderStatus  { get; set; }
        public DateTime IssuedTime { get; set; }
        public double GrandTotal   { get; set; }
        public int    LineCount    { get; set; }  // number of order lines
    }

    /// <summary>Top product by revenue for the sales breakdown panel.</summary>
    public class TopProductEntity
    {
        public string ItemID      { get; set; }
        public string ItemName    { get; set; }
        public string Category    { get; set; }
        public int    TotalQty    { get; set; }
        public double TotalRevenue{ get; set; }
    }

    // ════════════════════════════════════════════════════════════════════
    //  2. INVENTORY STATUS
    // ════════════════════════════════════════════════════════════════════

    /// <summary>One row in the Inventory Status grid.</summary>
    public class InventoryStatusRowEntity
    {
        public string WarehouseItemID   { get; set; }
        public string ItemID            { get; set; }
        public string ItemName          { get; set; }
        public string ItemCategory      { get; set; }  // "Product" or "Raw Material"
        public string MaterialType      { get; set; }  // for raw materials
        public string WarehouseID       { get; set; }
        public string WarehouseLocation { get; set; }
        public int    CurrentStock      { get; set; }
        public int    ReorderLevel      { get; set; }
        public bool   BelowReorder      => CurrentStock <= ReorderLevel;
    }

    /// <summary>KPI summary for Inventory report.</summary>
    public class InventoryKpiEntity
    {
        public int TotalSKUs        { get; set; }
        public int BelowReorderCount{ get; set; }
        public int ProductCount     { get; set; }
        public int RawMaterialCount { get; set; }
    }

    // ════════════════════════════════════════════════════════════════════
    //  3. PROCUREMENT SUMMARY
    // ════════════════════════════════════════════════════════════════════

    /// <summary>One row in the Procurement Summary grid.</summary>
    public class ProcurementRowEntity
    {
        public string PurchaseID     { get; set; }
        public string SupplierName   { get; set; }
        public string PurchaseStatus { get; set; }
        public DateTime OrderDate    { get; set; }
        public double POTotalAmount  { get; set; }
        public string MaterialNames  { get; set; }  // comma-joined from POLines
        public string RequestID      { get; set; }
    }

    /// <summary>KPI summary for Procurement report.</summary>
    public class ProcurementKpiEntity
    {
        public int    TotalPOs       { get; set; }
        public double TotalSpend     { get; set; }
        public int    CompletedPOs   { get; set; }
        public int    PendingPOs     { get; set; }
        public int    UniqueSuppliers{ get; set; }
    }

    // ════════════════════════════════════════════════════════════════════
    //  4. LOGISTICS OVERVIEW
    // ════════════════════════════════════════════════════════════════════

    /// <summary>One row in the Logistics Overview grid.</summary>
    public class ShipmentRowEntity
    {
        public string ShipmentID     { get; set; }
        public string OrderID        { get; set; }
        public string CustomerName   { get; set; }
        public string ShipmentStatus { get; set; }
        public string ShipmentType   { get; set; }
        public string DeliveryMethod { get; set; }
        public DateTime ShipDate     { get; set; }
        public double TotalAmount    { get; set; }
        public bool   HasDeliveryNote{ get; set; }
        public bool   HasReplySlip   { get; set; }
    }

    /// <summary>KPI summary for Logistics report.</summary>
    public class LogisticsKpiEntity
    {
        public int TotalShipments   { get; set; }
        public int Completed        { get; set; }
        public int InTransit        { get; set; }
        public int Pending          { get; set; }
        public int WithReplySlip    { get; set; }
    }

    // ════════════════════════════════════════════════════════════════════
    //  5. AFTER-SERVICE SUMMARY
    // ════════════════════════════════════════════════════════════════════

    /// <summary>One complaint row.</summary>
    public class ComplaintRowEntity
    {
        public string   ComplaintID          { get; set; }
        public string   OrderID              { get; set; }
        public string   CustomerName         { get; set; }
        public string   ComplaintDescription { get; set; }
        public string   ComplaintStatus      { get; set; }
    }

    /// <summary>One return order row.</summary>
    public class ReturnOrderRowEntity
    {
        public string   ReturnID      { get; set; }
        public string   OrderID       { get; set; }
        public string   CustomerName  { get; set; }
        public string   Reason        { get; set; }
        public double   RefundAmount  { get; set; }
        public string   ReturnStatus  { get; set; }
        public DateTime ReturnDate    { get; set; }
    }

    /// <summary>KPI summary for After-Service report.</summary>
    public class AfterServiceKpiEntity
    {
        public int    TotalComplaints { get; set; }
        public int    OpenComplaints  { get; set; }
        public int    TotalReturns    { get; set; }
        public double TotalRefunded   { get; set; }
    }

    // ════════════════════════════════════════════════════════════════════
    //  6. FINANCE OVERVIEW
    // ════════════════════════════════════════════════════════════════════

    /// <summary>One transaction row.</summary>
    public class FinanceTransactionRowEntity
    {
        public string   TransactionID   { get; set; }
        public string   TransactionType { get; set; }
        public double   Amount          { get; set; }
        public DateTime TransactionDate { get; set; }
        public string   LinkedDocument  { get; set; }  // InvoiceID or PurInvoiceID or ReturnID
        public string   DocumentType    { get; set; }  // "Sales Invoice" / "Purchase Invoice" / "Return"
    }

    /// <summary>KPI summary for Finance report.</summary>
    public class FinanceKpiEntity
    {
        public double TotalSalesRevenue  { get; set; }
        public double TotalProcurementSpend { get; set; }
        public double TotalRefunds       { get; set; }
        public double AROutstanding      { get; set; }
        public double APOutstanding      { get; set; }
    }

    // ════════════════════════════════════════════════════════════════════
    //  PAGE-LEVEL VIEWMODEL
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Passed from StatisticalReportsController to ViewReportForm.
    /// Only the active-report's list will be populated; others are null.
    /// </summary>
    public class ViewReportViewModel
    {
        public UserBarViewModel UserBar      { get; set; }
        public string[]         AllowedMenus { get; set; }
        public ReportType       ActiveReport { get; set; }

        // ── Per-report data payloads ─────────────────────────────────
        // 1. Sales
        public SalesKpiEntity                  SalesKpi      { get; set; }
        public List<SalesOrderRowEntity>       SalesRows     { get; set; }
        public List<TopProductEntity>          TopProducts   { get; set; }

        // 2. Inventory
        public InventoryKpiEntity              InventoryKpi  { get; set; }
        public List<InventoryStatusRowEntity>  InventoryRows { get; set; }

        // 3. Procurement
        public ProcurementKpiEntity            ProcKpi       { get; set; }
        public List<ProcurementRowEntity>      ProcRows      { get; set; }

        // 4. Logistics
        public LogisticsKpiEntity              LogKpi        { get; set; }
        public List<ShipmentRowEntity>         LogRows       { get; set; }

        // 5. After-Service
        public AfterServiceKpiEntity           AfterKpi      { get; set; }
        public List<ComplaintRowEntity>        Complaints    { get; set; }
        public List<ReturnOrderRowEntity>      Returns       { get; set; }

        // 6. Finance
        public FinanceKpiEntity                FinanceKpi    { get; set; }
        public List<FinanceTransactionRowEntity> FinanceRows { get; set; }
    }
}
