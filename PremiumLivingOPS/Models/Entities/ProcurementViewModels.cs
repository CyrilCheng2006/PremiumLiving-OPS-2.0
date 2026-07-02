using System;
using System.Collections.Generic;

// ============================================================
//  FILE: Models/Entities/ProcurementViewModels.cs
// ============================================================

namespace PremiumLivingOPS.Models.Entities
{
    // ── PROCUREMENT — Domain Entities ─────────────────────────────

    /// <summary>
    /// Flat projection for a single PurchaseOrder row (includes -NN suffix).
    /// Used internally and in the Detail dialog.
    /// </summary>
    public class ProcurementOrderEntity
    {
        public string   PurchaseID     { get; set; }   // e.g. PO-20260701-0001-01
        public string   RequestID      { get; set; }   // e.g. MRQ-260702-001-01
        public string   SupplierID     { get; set; }
        public string   SupplierName   { get; set; }
        public double   POTotalAmount  { get; set; }
        public DateTime OrderDate      { get; set; }
        public string   PurchaseStatus { get; set; }
        public string   RawMaterialItemID { get; set; }
        public string   RawMaterialName   { get; set; }
        public int      RequestedQty      { get; set; }
        public string   UrgencyLevel      { get; set; }
        public string   TriggerType       { get; set; }
        public string   OrderDateStr => OrderDate.ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// Grouped projection shown in the main Search Procurement grid.
    /// One row = one base PO-ID (PO-YYYYMMDD-NNNN), aggregating all -NN sub-orders.
    /// </summary>
    public class ProcurementOrderGroup
    {
        /// <summary>Base purchase ID without -NN suffix, e.g. PO-20260701-0001</summary>
        public string   BasePurchaseID { get; set; }
        public string   SupplierID     { get; set; }
        public string   SupplierName   { get; set; }
        public DateTime OrderDate      { get; set; }
        /// <summary>Aggregated status: "Mixed" if sub-orders differ, otherwise the single status.</summary>
        public string   PurchaseStatus { get; set; }
        /// <summary>Sum of POTotalAmount across all -NN sub-orders.</summary>
        public double   TotalAmount    { get; set; }
        /// <summary>Number of -NN sub-orders in this group.</summary>
        public int      ItemCount      { get; set; }
        public string   UrgencyLevel   { get; set; }
        public string   OrderDateStr   => OrderDate.ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// One line item inside a PurchaseOrder.
    /// </summary>
    public class PurchaseOrderLineEntity
    {
        public string POLineID          { get; set; }
        public string PurchaseID        { get; set; }   // full ID incl. -NN
        public string RawMaterialItemID { get; set; }
        public string MaterialName      { get; set; }
        public string MaterialType      { get; set; }
        public string WarehouseID       { get; set; }
        public string WarehouseLocation { get; set; }
        public int    OrderQty          { get; set; }
        public double UnitPrice         { get; set; }
        public double LineTotal         => OrderQty * UnitPrice;
    }

    /// <summary>
    /// Dropdown item: one unique MRQ batch prefix  e.g. "MRQ-260702-001"
    /// (groups all -NN line records that share the same prefix).
    /// </summary>
    public class MaterialRequestBatchLookup
    {
        public string BatchPrefix  { get; set; }
        public string UrgencyLevel { get; set; }
        public string TriggerType  { get; set; }
        public int    LineCount    { get; set; }
        public override string ToString() =>
            $"{BatchPrefix}  ({LineCount} item(s), {UrgencyLevel})";
    }

    /// <summary>
    /// One -NN line inside a batch prefix, shown in the procurement grid.
    /// Each line becomes one PurchaseOrder + one PurchaseOrderLine in DB.
    /// </summary>
    public class MaterialRequestLineItem
    {
        public string RequestID         { get; set; }
        public string RawMaterialItemID { get; set; }
        public string MaterialName      { get; set; }
        public string MaterialType      { get; set; }
        public string WarehouseItemID   { get; set; }
        public string WarehouseID       { get; set; }
        public string WarehouseDisplay  { get; set; }
        public int    RequestedQty      { get; set; }
        public int    OrderQty          { get; set; }
        public double UnitPrice         { get; set; }
        public double LineTotal         => OrderQty * UnitPrice;
    }

    /// <summary>
    /// Supplier lookup row for Create Procurement.
    /// </summary>
    public class SupplierLookup
    {
        public string SupplierID      { get; set; }
        public string SupplierName    { get; set; }
        public string PhoneNumber     { get; set; }
        public string SupplierAddress { get; set; }
        public override string ToString() => $"{SupplierName}  ({SupplierID})";
    }

    /// <summary>Legacy single-item lookup — kept for compatibility.</summary>
    public class MaterialRequestLookup
    {
        public string RequestID      { get; set; }
        public string RawMaterialID  { get; set; }
        public string MaterialName   { get; set; }
        public int    RequestedQty   { get; set; }
        public string UrgencyLevel   { get; set; }
        public string TriggerType    { get; set; }
        public override string ToString() =>
            $"{RequestID}  —  {MaterialName}  ({RequestedQty} units, {UrgencyLevel})";
    }
}

namespace PremiumLivingOPS.Models.ViewModels
{
    using PremiumLivingOPS.Models.Entities;

    /// <summary>ViewModel for Search Procurement page (grouped view).</summary>
    public class SearchProcurementViewModel
    {
        public UserBarViewModel                  UserBar      { get; set; }
        public string[]                          AllowedMenus { get; set; }
        public List<ProcurementOrderGroup>       Groups       { get; set; }
    }

    /// <summary>ViewModel for Create Procurement page (batch-prefix model).</summary>
    public class CreateProcurementViewModel
    {
        public UserBarViewModel                   UserBar        { get; set; }
        public string[]                           AllowedMenus   { get; set; }
        public List<MaterialRequestBatchLookup>   BatchPrefixes  { get; set; }
        public List<SupplierLookup>               Suppliers      { get; set; }
        public string                             NextPurchaseID { get; set; }
    }

    /// <summary>
    /// Detail ViewModel for the grouped PO detail dialog.
    /// Contains all -NN sub-orders and their lines.
    /// </summary>
    public class ProcurementDetailViewModel
    {
        public UserBarViewModel                  UserBar      { get; set; }
        public string[]                          AllowedMenus { get; set; }
        /// <summary>All -NN PurchaseOrder rows for the given base ID.</summary>
        public List<ProcurementOrderEntity>      Orders       { get; set; }
        /// <summary>All PurchaseOrderLine rows across all -NN orders.</summary>
        public List<PurchaseOrderLineEntity>     Lines        { get; set; }
    }
}
