using System;
using System.Collections.Generic;

// ============================================================
//  FILE: Models/Entities/ProcurementViewModels.cs
// ============================================================

namespace PremiumLivingOPS.Models.Entities
{
    // ── PROCUREMENT — Domain Entities ───────────────────────────────────────────

    /// <summary>
    /// Flat projection for a single PurchaseOrder row.
    /// PurchaseID format: PO-YYYYMMDD-NNNN  (no -NN suffix).
    /// </summary>
    public class ProcurementOrderEntity
    {
        public string   PurchaseID        { get; set; }   // e.g. PO-20260702-0001
        public string   RequestID         { get; set; }   // first linked MRQ RequestID (for display)
        public string   SupplierID        { get; set; }
        public string   SupplierName      { get; set; }
        public double   POTotalAmount     { get; set; }
        public DateTime OrderDate         { get; set; }
        public string   PurchaseStatus    { get; set; }
        // RawMaterial fields are now per-line; kept here for backward compat (first line)
        public string   RawMaterialItemID { get; set; }
        public string   RawMaterialName   { get; set; }
        public int      RequestedQty      { get; set; }
        public string   UrgencyLevel      { get; set; }
        public string   TriggerType       { get; set; }
        public string   OrderDateStr      => OrderDate.ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// One row shown in the Search Procurement grid.
    /// Corresponds 1-to-1 with a PurchaseOrder header row (PO-YYYYMMDD-NNNN).
    /// ItemCount = number of PurchaseOrderLine rows under this PO.
    /// </summary>
    public class ProcurementOrderGroup
    {
        /// <summary>Header PurchaseID, e.g. PO-20260702-0001 — shown in grid.</summary>
        public string   PurchaseID     { get; set; }
        public string   SupplierID     { get; set; }
        public string   SupplierName   { get; set; }
        public DateTime OrderDate      { get; set; }
        public string   PurchaseStatus { get; set; }
        public double   TotalAmount    { get; set; }
        /// <summary>Number of PurchaseOrderLine items for this PO.</summary>
        public int      ItemCount      { get; set; }
        public string   UrgencyLevel   { get; set; }
        public string   OrderDateStr   => OrderDate.ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// One line item inside a PurchaseOrder (from PurchaseOrderLine table).
    /// POLineID  format: PO-YYYYMMDD-NNNN-01, -02, -03…
    /// RequestID links back to the originating MaterialRequest row for full traceability.
    /// </summary>
    public class PurchaseOrderLineEntity
    {
        public string POLineID          { get; set; }   // e.g. PO-20260702-0001-01
        public string PurchaseID        { get; set; }   // e.g. PO-20260702-0001
        public string RequestID         { get; set; }   // originating MRQ e.g. MRQ-260702-001-01
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
    /// Dropdown item: one unique MRQ batch prefix e.g. "MRQ-260702-001"
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
    /// One -NN line inside a batch prefix, shown in the Create Procurement grid.
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

    /// <summary>Supplier lookup row for Create Procurement.</summary>
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

    /// <summary>ViewModel for Search Procurement page.</summary>
    public class SearchProcurementViewModel
    {
        public UserBarViewModel             UserBar      { get; set; }
        public string[]                     AllowedMenus { get; set; }
        public List<ProcurementOrderGroup>  Groups       { get; set; }
    }

    /// <summary>ViewModel for Create Procurement page.</summary>
    public class CreateProcurementViewModel
    {
        public UserBarViewModel                  UserBar        { get; set; }
        public string[]                          AllowedMenus   { get; set; }
        public List<MaterialRequestBatchLookup>  BatchPrefixes  { get; set; }
        public List<SupplierLookup>              Suppliers      { get; set; }
        public string                            NextPurchaseID { get; set; }
    }

    /// <summary>
    /// Detail ViewModel for the PO detail dialog.
    /// Order  = the single PurchaseOrder header (PO-YYYYMMDD-NNNN).
    /// Lines  = all PurchaseOrderLine rows (POLineID = PO-YYYYMMDD-NNNN-01, -02…).
    /// </summary>
    public class ProcurementDetailViewModel
    {
        public UserBarViewModel              UserBar      { get; set; }
        public string[]                      AllowedMenus { get; set; }
        /// <summary>The PurchaseOrder header row.</summary>
        public ProcurementOrderEntity        Order        { get; set; }
        /// <summary>All PurchaseOrderLine rows for this PO, ordered by POLineID.</summary>
        public List<PurchaseOrderLineEntity> Lines        { get; set; }
    }
}
