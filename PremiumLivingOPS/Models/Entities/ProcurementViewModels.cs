using System;
using System.Collections.Generic;

// ============================================================
//  FILE: Models/Entities/ProcurementViewModels.cs
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
    // ── PROCUREMENT — Domain Entities ───────────────────────────────

    /// <summary>
    /// Flat projection for the Search Procurement grid.
    /// Combines PurchaseOrder + Supplier + MaterialRequest.
    /// NOTE: The lightweight version in GoodsReceivedEntity.cs is kept
    ///       for LogisticsProcessing. This richer version is used by
    ///       the Procurement module only.
    /// </summary>
    public class ProcurementOrderEntity
    {
        // PurchaseOrder columns
        public string   PurchaseID     { get; set; }
        public string   RequestID      { get; set; }
        public string   SupplierID     { get; set; }
        public string   SupplierName   { get; set; }
        public double   POTotalAmount  { get; set; }
        public DateTime OrderDate      { get; set; }
        public string   PurchaseStatus { get; set; }

        // MaterialRequest columns (joined)
        public string RawMaterialItemID { get; set; }
        public string RawMaterialName   { get; set; }
        public int    RequestedQty      { get; set; }
        public string UrgencyLevel      { get; set; }
        public string TriggerType       { get; set; }

        // Derived display
        public string OrderDateStr => OrderDate.ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// One line item inside a PurchaseOrder.
    /// Maps to PurchaseOrderLine + RawMaterial + Item + Warehouse.
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
    /// Lookup row for Material Request dropdown in Create Procurement.
    /// Maps to MaterialRequest JOIN RawMaterial JOIN Item.
    /// </summary>
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
}

// ──────────────────────────────────────────────────────────────
namespace PremiumLivingOPS.Models.ViewModels
{
    using PremiumLivingOPS.Models.Entities;

    // ── PROCUREMENT — Page ViewModels ───────────────────────────────

    /// <summary>ViewModel for Search Procurement page.</summary>
    public class SearchProcurementViewModel
    {
        public UserBarViewModel              UserBar      { get; set; }
        public string[]                      AllowedMenus { get; set; }
        public List<ProcurementOrderEntity>  Orders       { get; set; }
    }

    /// <summary>ViewModel for Create Procurement page.</summary>
    public class CreateProcurementViewModel
    {
        public UserBarViewModel            UserBar          { get; set; }
        public string[]                    AllowedMenus     { get; set; }
        public List<MaterialRequestLookup> MaterialRequests { get; set; }
        public List<SupplierLookup>        Suppliers        { get; set; }
        public List<WarehouseEntity>       Warehouses       { get; set; }
        /// <summary>Auto-generated next PurchaseID (e.g. PO-20260604-0025).</summary>
        public string                      NextPurchaseID   { get; set; }
    }

    /// <summary>
    /// Detail ViewModel for Create Procurement review panel
    /// and Search Procurement detail dialog.
    /// </summary>
    public class ProcurementDetailViewModel
    {
        public UserBarViewModel              UserBar      { get; set; }
        public string[]                      AllowedMenus { get; set; }
        public ProcurementOrderEntity        Order        { get; set; }
        public List<PurchaseOrderLineEntity> Lines        { get; set; }
    }
}
