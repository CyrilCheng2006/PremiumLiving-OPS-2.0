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
        public int    TotalOrders       { get; set; }
        public double TotalRevenue      { get; set; }
        public double AverageOrderValue { get; set; }
        public int    DeliveredOrders   { get; set; }
        public int    PendingOrders     { get; set; }
        public int    ProcessingOrders  { get; set; }
        public int    CancelledOrders   { get; set; }
    }

    /// <summary>One detail row in the Sales Performance grid.</summary>
    public class SalesOrderRowEntity
    {
        public string   OrderID      { get; set; }
        public string   CustomerName { get; set; }
        public string   OrderStatus  { get; set; }
        public DateTime IssuedTime   { get; set; }
        public double   GrandTotal   { get; set; }
        public int      LineCount    { get; set; }
    }

    /// <summary>Top product by revenue for the sales breakdown panel.</summary>
    public class TopProductEntity
    {
        public string ItemID       { get; set; }
        public string ItemName     { get; set; }
        public string Category     { get; set; }
        public int    TotalQty     { get; set; }
        public double TotalRevenue { get; set; }
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

        // ── Aliases for ViewReportForm.cs compatibility ──────────────
        /// <summary>Alias for ItemCategory — used by ViewReportForm.cs</summary>
        public string Category    => ItemCategory;
        /// <summary>Alias for WarehouseLocation — used by ViewReportForm.cs</summary>
        public string WarehouseName => WarehouseLocation;
    }

    /// <summary>KPI summary for Inventory report.</summary>
    public class InventoryKpiEntity
    {
        public int TotalSKUs         { get; set; }
        public int BelowReorderCount { get; set; }
        public int ProductCount      { get; set; }
        public int RawMaterialCount  { get; set; }
    }

    // ════════════════════════════════════════════════════════════════════
    //  3. PROCUREMENT SUMMARY
    // ════════════════════════════════════════════════════════════════════

    /// <summary>One row in the Procurement Summary grid.</summary>
    public class ProcurementRowEntity
    {
        public string   PurchaseOrderID  { get; set; }
        public string   SupplierName     { get; set; }
        public string   PurchaseStatus   { get; set; }
        public string   ReceiptStatus    { get; set; }
        public DateTime OrderDate        { get; set; }
        public double   TotalAmount      { get; set; }
        public int      ItemCount        { get; set; }
        public string   RequestID        { get; set; }

        // ── Aliases for ViewReportForm.cs compatibility ──────────────
        /// <summary>Alias for PurchaseOrderID — used by ViewReportForm.cs</summary>
        public string POID     => PurchaseOrderID;
        /// <summary>Alias for PurchaseStatus — used by ViewReportForm.cs</summary>
        public string POStatus => PurchaseStatus;
        /// <summary>Alias for ItemCount — used by ViewReportForm.cs</summary>
        public int    LineCount => ItemCount;
    }

    /// <summary>KPI summary for Procurement report.</summary>
    public class ProcurementKpiEntity
    {
        public int    TotalPOs        { get; set; }
        public double TotalSpend      { get; set; }
        public int    CompletedPOs    { get; set; }
        public int    PendingPOs      { get; set; }
        public int    UniqueSuppliers { get; set; }
    }

    // ════════════════════════════════════════════════════════════════════
    //  4. LOGISTICS OVERVIEW
    // ════════════════════════════════════════════════════════════════════

    /// <summary>One row in the Logistics Overview grid.</summary>
    public class LogisticsRowEntity
    {
        public string   DeliveryOrderID  { get; set; }
        public string   SalesOrderID     { get; set; }
        public string   CustomerName     { get; set; }
        public string   DeliveryStatus   { get; set; }
        public string   DriverName       { get; set; }
        public DateTime DeliveryDate     { get; set; }
        public bool     HasDeliveryNote  { get; set; }
        public bool     HasReplySlip     { get; set; }
        /// <summary>Shipment/carrier tracking reference number.</summary>
        public string   TrackingNumber   { get; set; }

        // ── Aliases for ViewReportForm.cs compatibility ──────────────
        /// <summary>Alias for DeliveryDate — used by ViewReportForm.cs</summary>
        public DateTime CreatedDate  => DeliveryDate;
        /// <summary>Alias for DriverName — used by ViewReportForm.cs</summary>
        public string   CarrierName  => DriverName;
    }

    /// <summary>KPI summary for Logistics report.</summary>
    public class LogisticsKpiEntity
    {
        public int TotalShipments { get; set; }
        public int Completed      { get; set; }
        public int InTransit      { get; set; }
        public int Pending        { get; set; }
        public int WithReplySlip  { get; set; }
    }

    // ════════════════════════════════════════════════════════════════════
    //  5. AFTER-SERVICE SUMMARY
    // ════════════════════════════════════════════════════════════════════

    /// <summary>One complaint row (original entity, retained for backward compatibility).</summary>
    public class ComplaintRowEntity
    {
        public string   ComplaintID     { get; set; }
        public string   CustomerName    { get; set; }
        public string   Subject         { get; set; }
        public string   ComplaintStatus { get; set; }
        public DateTime ComplaintDate   { get; set; }
        public string   OrderID         { get; set; }
    }

    /// <summary>One return order row (original entity, retained for backward compatibility).</summary>
    public class ReturnOrderRowEntity
    {
        public string   ReturnOrderID { get; set; }
        public string   SalesOrderID  { get; set; }
        public string   CustomerName  { get; set; }
        public string   Reason        { get; set; }
        public double   RefundAmount  { get; set; }
        public string   ReturnStatus  { get; set; }
        public DateTime ReturnDate    { get; set; }
    }

    /// <summary>
    /// Service request row — used by ViewReportForm.cs (vm.ServiceRows).
    /// Maps to the after-service request / complaint table in the DB.
    /// </summary>
    public class ServiceRequestRowEntity
    {
        public string   RequestID     { get; set; }
        public string   CustomerName  { get; set; }
        public string   RequestStatus { get; set; }
        public DateTime RequestDate   { get; set; }
        public string   ServiceType   { get; set; }
        public string   Description   { get; set; }
    }

    /// <summary>
    /// Follow-up row — used by ViewReportForm.cs (vm.FollowUpRows).
    /// Maps to the after-service follow-up / note table in the DB.
    /// </summary>
    public class ServiceFollowUpRowEntity
    {
        public string   FollowUpID   { get; set; }
        public string   RequestID    { get; set; }
        public DateTime FollowUpDate { get; set; }
        public string   StaffName    { get; set; }
        public string   Note         { get; set; }
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

    /// <summary>One finance document / transaction row.</summary>
    public class FinanceTransactionRowEntity
    {
        public string   TransactionID   { get; set; }
        public string   TransactionType { get; set; }
        public double   Amount          { get; set; }
        public DateTime TransactionDate { get; set; }
        public string   DocumentType    { get; set; }
        public string   PaymentMethod   { get; set; }
        public string   ApprovalStatus  { get; set; }
        public string   LinkedDocument  { get; set; }
        /// <summary>Due / settlement date of the document.</summary>
        public DateTime DueDate         { get; set; }
        /// <summary>Currency code, e.g. "HKD", "USD".</summary>
        public string   Currency        { get; set; }

        // ── Aliases for ViewReportForm.cs compatibility ──────────────
        /// <summary>Alias for TransactionID — used by ViewReportForm.cs</summary>
        public string   DocumentID    => TransactionID;
        /// <summary>Alias for ApprovalStatus — used by ViewReportForm.cs</summary>
        public string   PaymentStatus => ApprovalStatus;
        /// <summary>Alias for TransactionDate — used by ViewReportForm.cs</summary>
        public DateTime IssueDate     => TransactionDate;
    }

    /// <summary>KPI summary for Finance report.</summary>
    public class FinanceKpiEntity
    {
        public double TotalSalesRevenue     { get; set; }
        public double TotalProcurementSpend { get; set; }
        public double TotalRefunds          { get; set; }
        public double AROutstanding         { get; set; }
        public double APOutstanding         { get; set; }
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

        // ── 1. Sales ─────────────────────────────────────────────────
        public SalesKpiEntity                    SalesKpi     { get; set; }
        public List<SalesOrderRowEntity>         SalesRows    { get; set; }
        public List<TopProductEntity>            TopProducts  { get; set; }

        // ── 2. Inventory ─────────────────────────────────────────────
        public InventoryKpiEntity                InventoryKpi  { get; set; }
        public List<InventoryStatusRowEntity>    InventoryRows { get; set; }

        // ── 3. Procurement ───────────────────────────────────────────
        public ProcurementKpiEntity              ProcKpi         { get; set; }
        public List<ProcurementRowEntity>        ProcurementRows { get; set; }

        // ── 4. Logistics ─────────────────────────────────────────────
        public LogisticsKpiEntity                LogKpi        { get; set; }
        public List<LogisticsRowEntity>          LogisticsRows { get; set; }

        // ── 5. After-Service ─────────────────────────────────────────
        public AfterServiceKpiEntity             AfterKpi      { get; set; }
        /// <summary>
        /// Service request rows — used by ViewReportForm.cs as vm.ServiceRows.
        /// Populated by StatisticalReportsController.GetAfterServiceReportVM().
        /// </summary>
        public List<ServiceRequestRowEntity>     ServiceRows   { get; set; }
        /// <summary>
        /// Follow-up note rows — used by ViewReportForm.cs as vm.FollowUpRows.
        /// Populated by StatisticalReportsController.GetAfterServiceReportVM().
        /// </summary>
        public List<ServiceFollowUpRowEntity>    FollowUpRows  { get; set; }

        // Retained for backward compatibility with existing controller code
        public List<ComplaintRowEntity>          ComplaintRows { get; set; }
        public List<ReturnOrderRowEntity>        ReturnRows    { get; set; }

        // ── 6. Finance ───────────────────────────────────────────────
        public FinanceKpiEntity                  FinanceKpi  { get; set; }
        public List<FinanceTransactionRowEntity> FinanceRows { get; set; }
    }
}
