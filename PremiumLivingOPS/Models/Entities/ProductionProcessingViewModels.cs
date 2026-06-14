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

// ──────────────────────────────────────────────────────────────
namespace PremiumLivingOPS.Models.Entities
{
    // ── PRODUCTION PROCESSING — Domain Entities ──────────────────

    /// <summary>
    /// Flat projection for the Search Raw Material Request grid.
    /// Combines MaterialRequest + RawMaterial + Item + WarehouseItem + Warehouse + Order.
    /// </summary>
    public class MaterialRequestEntity
    {
        // MaterialRequest columns
        public string RequestID         { get; set; }
        public string OrderID           { get; set; }
        public string RawMaterialItemID { get; set; }
        public string RawMaterialName   { get; set; }
        public string MaterialType      { get; set; }
        public string WarehouseItemID   { get; set; }
        public int    RequestedQty      { get; set; }
        public string UrgencyLevel      { get; set; }
        public string TriggerType       { get; set; }

        // Warehouse info (joined via WarehouseItem)
        public string WarehouseID       { get; set; }
        public string WarehouseLocation { get; set; }

        // Current stock from WarehouseItem
        public int    CurrentStock      { get; set; }
        public int    ReorderLevel      { get; set; }

        // Derived display
        public bool   IsLinkedToPO      { get; set; }  // true if a PurchaseOrder references this RequestID
    }

    /// <summary>
    /// Full detail projection for the Material Request Detail dialog.
    /// Combines MaterialRequest + RawMaterial + Item + WarehouseItem + Warehouse
    /// + LEFT JOIN PurchaseOrder (if any).
    /// </summary>
    public class MaterialRequestDetailEntity
    {
        // Core request fields
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

        // Linked Purchase Order (null if none)
        public string   PurchaseID        { get; set; }
        public string   PurchaseStatus    { get; set; }
        public decimal? POTotalAmount     { get; set; }
    }

    /// <summary>
    /// Lookup for Raw Material dropdown in Create Raw Material Request.
    /// Maps to RawMaterial JOIN Item.
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
    /// Maps to WarehouseItem JOIN Warehouse, filtered to raw material items.
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
    /// Maps to Order table (for OrderDemand trigger type).
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

// ──────────────────────────────────────────────────────────────
namespace PremiumLivingOPS.Models.ViewModels
{
    using PremiumLivingOPS.Models.Entities;

    // ── PRODUCTION PROCESSING — Page ViewModels ──────────────────

    /// <summary>ViewModel for Search Raw Material Request page.</summary>
    public class SearchMaterialRequestViewModel
    {
        public UserBarViewModel                                        UserBar      { get; set; }
        public string[]                                                AllowedMenus { get; set; }
        public System.Collections.Generic.List<MaterialRequestEntity> Requests     { get; set; }
    }

    /// <summary>ViewModel for Create Raw Material Request page.</summary>
    public class CreateMaterialRequestViewModel
    {
        public UserBarViewModel                                        UserBar        { get; set; }
        public string[]                                                AllowedMenus   { get; set; }
        public System.Collections.Generic.List<RawMaterialLookup>     RawMaterials   { get; set; }
        public System.Collections.Generic.List<WarehouseItemLookup>   WarehouseItems { get; set; }
        public System.Collections.Generic.List<OrderLookup>           Orders         { get; set; }
        /// <summary>Auto-generated next RequestID (e.g. MRQ-20260604-0025).</summary>
        public string                                                  NextRequestID  { get; set; }
    }
}
