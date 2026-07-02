using System;
using System.Collections.Generic;

// ============================================================
//  FILE: Models/Entities/ProcurementViewModels.cs
// ============================================================

namespace PremiumLivingOPS.Models.Entities
{
    // ── PROCUREMENT — Domain Entities ─────────────────────────────

    /// <summary>
    /// Flat projection for the Search Procurement grid.
    /// </summary>
    public class ProcurementOrderEntity
    {
        public string   PurchaseID     { get; set; }
        public string   RequestID      { get; set; }
        public string   SupplierID     { get; set; }
        public string   SupplierName   { get; set; }
        public double   POTotalAmount  { get; set; }
        public DateTime OrderDate      { get; set; }
        public string   PurchaseStatus { get; set; }
        public string RawMaterialItemID { get; set; }
        public string RawMaterialName   { get; set; }
        public int    RequestedQty      { get; set; }
        public string UrgencyLevel      { get; set; }
        public string TriggerType       { get; set; }
        public string OrderDateStr => OrderDate.ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// One line item inside a PurchaseOrder.
    /// </summary>
    public class PurchaseOrderLineEntity
    {
        public string POLineID          { get; set; }
        public string PurchaseID        { get; set; }
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
        public string BatchPrefix  { get; set; }   // e.g. MRQ-260702-001
        public string UrgencyLevel { get; set; }
        public string TriggerType  { get; set; }
        public int    LineCount    { get; set; }   // how many -NN items exist
        public override string ToString() =>
            $"{BatchPrefix}  ({LineCount} item(s), {UrgencyLevel})";
    }

    /// <summary>
    /// One -NN line inside a batch prefix, shown in the procurement grid.
    /// Each line becomes one PurchaseOrder + one PurchaseOrderLine in DB.
    /// </summary>
    public class MaterialRequestLineItem
    {
        public string RequestID        { get; set; }   // full ID e.g. MRQ-260702-001-01
        public string RawMaterialItemID{ get; set; }
        public string MaterialName     { get; set; }
        public string MaterialType     { get; set; }
        public string WarehouseItemID  { get; set; }   // from MaterialRequest
        public string WarehouseID      { get; set; }   // resolved from WarehouseItem
        public string WarehouseDisplay { get; set; }   // e.g. "WH-001 — Kowloon Bay"
        public int    RequestedQty     { get; set; }
        public int    OrderQty         { get; set; }   // user-editable, defaults to RequestedQty
        public double UnitPrice        { get; set; }   // user-editable
        public double LineTotal        => OrderQty * UnitPrice;
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

    /// <summary>Legacy single-item lookup — kept for SearchProcurementForm compatibility.</summary>
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

    /// <summary>ViewModel for Search Procurement page.</summary>
    public class SearchProcurementViewModel
    {
        public UserBarViewModel              UserBar      { get; set; }
        public string[]                      AllowedMenus { get; set; }
        public List<ProcurementOrderEntity>  Orders       { get; set; }
    }

    /// <summary>ViewModel for Create Procurement page (batch-prefix model).</summary>
    public class CreateProcurementViewModel
    {
        public UserBarViewModel                   UserBar        { get; set; }
        public string[]                           AllowedMenus   { get; set; }
        /// <summary>Distinct batch prefixes available for procurement.</summary>
        public List<MaterialRequestBatchLookup>   BatchPrefixes  { get; set; }
        public List<SupplierLookup>               Suppliers      { get; set; }
        /// <summary>Auto-generated next PurchaseID prefix for display only.</summary>
        public string                             NextPurchaseID { get; set; }
    }

    /// <summary>Detail ViewModel for Search Procurement detail dialog.</summary>
    public class ProcurementDetailViewModel
    {
        public UserBarViewModel              UserBar      { get; set; }
        public string[]                      AllowedMenus { get; set; }
        public ProcurementOrderEntity        Order        { get; set; }
        public List<PurchaseOrderLineEntity> Lines        { get; set; }
    }
}
