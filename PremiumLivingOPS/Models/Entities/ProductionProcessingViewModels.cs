using System;
using System.Collections.Generic;

// ============================================================
//  FILE: Models/Entities/ProductionProcessingViewModels.cs
//
//  Block 1  ─ namespace PremiumLivingOPS.Models.Entities
//             Pure data projections (Entity / Lookup classes)
//
//  Block 2  ─ namespace PremiumLivingOPS.Models.ViewModels
//             Page-level ViewModels (Controller → View)
// ============================================================

// ──────────────────────────────────────────────────────
namespace PremiumLivingOPS.Models.Entities
{
    // ── PRODUCTION PROCESSING — Domain Entities ──────────────────

    /// <summary>
    /// One line (DB row) of a MaterialRequest batch.
    /// Used in the Detail dialog’s line-items table.
    /// </summary>
    public class MaterialRequestLineEntity
    {
        public string RequestID         { get; set; }   // full PK e.g. MRQ-260215-001-01
        public string RawMaterialItemID { get; set; }
        public string RawMaterialName   { get; set; }
        public string MaterialType      { get; set; }
        public string WarehouseItemID   { get; set; }
        public string WarehouseID       { get; set; }
        public string WarehouseLocation { get; set; }
        public int    RequestedQty      { get; set; }
        public int    CurrentStock      { get; set; }
        public int    ReorderLevel      { get; set; }
    }

    /// <summary>
    /// Batch-level summary for the Search Raw Material Request grid.
    /// One row per BatchPrefix (e.g. MRQ-260215-001).
    /// Aggregates all DB lines sharing that prefix.
    /// </summary>
    public class MaterialRequestBatchEntity
    {
        // Batch prefix shown to user (no -NN suffix)
        public string BatchPrefix       { get; set; }   // e.g. MRQ-260215-001
        public string OrderID           { get; set; }   // shared across all lines in batch
        public string UrgencyLevel      { get; set; }   // taken from first line
        public string TriggerType       { get; set; }   // taken from first line
        public int    TotalLines        { get; set; }   // count of -NN lines
        public int    TotalRequestedQty { get; set; }   // SUM of RequestedQty
        public string WarehouseLocation { get; set; }   // taken from first line
        public int    CurrentStock      { get; set; }   // taken from first line
        public int    ReorderLevel      { get; set; }   // taken from first line
        public bool   IsLinkedToPO      { get; set; }   // any line linked to PO
    }

    /// <summary>
    /// Full detail for the Detail dialog: batch header + all line items.
    /// </summary>
    public class MaterialRequestBatchDetailEntity
    {
        // Batch header fields
        public string   BatchPrefix     { get; set; }
        public string   OrderID         { get; set; }
        public string   UrgencyLevel    { get; set; }
        public string   TriggerType     { get; set; }
        public int      TotalLines      { get; set; }

        // All line items
        public List<MaterialRequestLineEntity> Lines { get; set; } = new List<MaterialRequestLineEntity>();

        // Linked Purchase Order (from first line that has one)
        public string   PurchaseID      { get; set; }
        public string   PurchaseStatus  { get; set; }
        public decimal? POTotalAmount   { get; set; }
    }

    // ---- kept for backward compat (Create form still uses these) ----

    /// <summary>
    /// Flat projection for the Search Raw Material Request grid.
    /// Combines MaterialRequest + RawMaterial + Item + WarehouseItem + Warehouse + Order.
    /// </summary>
    public class MaterialRequestEntity
    {
        public string RequestID         { get; set; }
        public string OrderID           { get; set; }
        public string RawMaterialItemID { get; set; }
        public string RawMaterialName   { get; set; }
        public string MaterialType      { get; set; }
        public string WarehouseItemID   { get; set; }
        public int    RequestedQty      { get; set; }
        public string UrgencyLevel      { get; set; }
        public string TriggerType       { get; set; }
        public string WarehouseID       { get; set; }
        public string WarehouseLocation { get; set; }
        public int    CurrentStock      { get; set; }
        public int    ReorderLevel      { get; set; }
        public bool   IsLinkedToPO      { get; set; }
    }

    /// <summary>
    /// Full detail projection for the Material Request Detail dialog.
    /// </summary>
    public class MaterialRequestDetailEntity
    {
        public string   RequestID         { get; set; }
        public string   OrderID           { get; set; }
        public string   RawMaterialItemID { get; set; }
        public string   RawMaterialName   { get; set; }
        public string   MaterialType      { get; set; }
        public string   WarehouseItemID   { get; set; }
        public string   WarehouseID       { get; set; }
        public string   WarehouseLocation { get; set; }
        public int      RequestedQty      { get; set; }
        public string   UrgencyLevel      { get; set; }
        public string   TriggerType       { get; set; }
        public int      CurrentStock      { get; set; }
        public int      ReorderLevel      { get; set; }
        public string   PurchaseID        { get; set; }
        public string   PurchaseStatus    { get; set; }
        public decimal? POTotalAmount     { get; set; }
    }

    /// <summary>
    /// Lookup for Raw Material dropdown in Create Raw Material Request.
    /// </summary>
    public class RawMaterialLookup
    {
        public string  ItemID        { get; set; }
        public string  ItemName      { get; set; }
        public string  MaterialType  { get; set; }
        public decimal PurchasePrice { get; set; }
        public override string ToString() =>
            $"{ItemID}  —  {ItemName}  ({MaterialType})";
    }

    /// <summary>
    /// Lookup for WarehouseItem dropdown in Create Raw Material Request.
    /// </summary>
    public class WarehouseItemLookup
    {
        public string WarehouseItemID   { get; set; }
        public string WarehouseID       { get; set; }
        public string WarehouseLocation { get; set; }
        public string ItemID            { get; set; }
        public int    CurrentStock      { get; set; }
        public int    ReorderLevel      { get; set; }
        public override string ToString() =>
            $"{WarehouseID}  —  {WarehouseLocation}  (Stock: {CurrentStock})";
    }

    /// <summary>
    /// Lookup for Order dropdown in Create Raw Material Request.
    /// </summary>
    public class OrderLookup
    {
        public string OrderID     { get; set; }
        public string CustomerID  { get; set; }
        public string OrderStatus { get; set; }
        public override string ToString() =>
            $"{OrderID}  ({OrderStatus})";
    }
}

// ──────────────────────────────────────────────────────
namespace PremiumLivingOPS.Models.ViewModels
{
    using PremiumLivingOPS.Models.Entities;

    /// <summary>ViewModel for Search Raw Material Request page.</summary>
    public class SearchMaterialRequestViewModel
    {
        public UserBarViewModel                                             UserBar      { get; set; }
        public string[]                                                     AllowedMenus { get; set; }
        /// <summary>One row per BatchPrefix for the grid.</summary>
        public System.Collections.Generic.List<MaterialRequestBatchEntity> Batches      { get; set; }
        /// <summary>Kept for KPI counts (all raw lines).</summary>
        public System.Collections.Generic.List<MaterialRequestEntity>      Requests     { get; set; }
    }

    /// <summary>ViewModel for Create Raw Material Request page.</summary>
    public class CreateMaterialRequestViewModel
    {
        public UserBarViewModel                                             UserBar        { get; set; }
        public string[]                                                     AllowedMenus   { get; set; }
        public System.Collections.Generic.List<RawMaterialLookup>          RawMaterials   { get; set; }
        public System.Collections.Generic.List<WarehouseItemLookup>        WarehouseItems { get; set; }
        public System.Collections.Generic.List<OrderLookup>                Orders         { get; set; }
        public string                                                       NextRequestID  { get; set; }
    }
}
