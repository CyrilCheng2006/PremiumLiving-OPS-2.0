using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.Entities
{
    // ════════════════════════════════════════════════════════════════════
    //  ENTITY: PurchaseOrder header row
    // ════════════════════════════════════════════════════════════════════

    public class ProcurementOrderEntity
    {
        public string   PurchaseID        { get; set; }
        public string   RequestID         { get; set; }
        public string   SupplierID        { get; set; }
        public string   SupplierName      { get; set; }
        public double   POTotalAmount     { get; set; }
        public DateTime OrderDate         { get; set; }
        public string   PurchaseStatus    { get; set; }
        public string   RawMaterialItemID { get; set; }
        public string   RawMaterialName   { get; set; }
        public int      RequestedQty      { get; set; }
        public string   UrgencyLevel      { get; set; }
        public string   TriggerType       { get; set; }

        public string OrderDateStr => OrderDate.ToString("yyyy-MM-dd");
    }

    // Alias used by LogisticsProcessing module
    public class PurchaseOrderEntity : ProcurementOrderEntity { }

    // ════════════════════════════════════════════════════════════════════
    //  ENTITY: PurchaseOrderLine
    // ════════════════════════════════════════════════════════════════════

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

    // ════════════════════════════════════════════════════════════════════
    //  LOOKUP: Supplier
    // ════════════════════════════════════════════════════════════════════

    public class SupplierLookup
    {
        public string SupplierID      { get; set; }
        public string SupplierName    { get; set; }
        public string PhoneNumber     { get; set; }
        public string SupplierAddress { get; set; }

        public string DisplayText => $"{SupplierID}  —  {SupplierName}";
    }

    // ════════════════════════════════════════════════════════════════════
    //  LOOKUP: MaterialRequest batch prefix (for Create Procurement)
    // ════════════════════════════════════════════════════════════════════

    public class MaterialRequestBatchLookup
    {
        /// <summary>e.g. "MR-20260701-0001" (without the -NN line suffix)</summary>
        public string BatchPrefix  { get; set; }
        public string UrgencyLevel { get; set; }
        public string TriggerType  { get; set; }
        public int    LineCount    { get; set; }

        public string DisplayText =>
            $"{BatchPrefix}  ({LineCount} line{(LineCount == 1 ? "" : "s")})  [{UrgencyLevel}]";
    }

    // ════════════════════════════════════════════════════════════════════
    //  LOOKUP: One MaterialRequest line item (for Create Procurement grid)
    // ════════════════════════════════════════════════════════════════════

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

    // ════════════════════════════════════════════════════════════════════
    //  SEARCH
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// One row in the Search Procurement DataGridView.
    /// Represents a BASE Purchase Order group (PO-YYYYMMDD-NNNN),
    /// aggregating all child line-orders (PO-YYYYMMDD-NNNN-NN) beneath it.
    /// </summary>
    public class ProcurementOrderGroup
    {
        /// <summary>The base PO ID without the trailing -NN suffix, e.g. "PO-20260701-0001".</summary>
        public string BasePurchaseID  { get; set; }

        /// <summary>All child PurchaseIDs that share this BasePurchaseID.</summary>
        public List<string> ChildPurchaseIDs { get; set; } = new List<string>();

        public string   SupplierID     { get; set; }
        public string   SupplierName   { get; set; }
        public DateTime OrderDate      { get; set; }
        public string   PurchaseStatus { get; set; }
        public double   TotalAmount    { get; set; }
        public int      ItemCount      { get; set; }
        public string   UrgencyLevel   { get; set; }

        public string OrderDateStr => OrderDate.ToString("yyyy-MM-dd");
    }

    // ════════════════════════════════════════════════════════════════════
    //  DETAIL (grouped – one dialog for all child POs under a BaseID)
    // ════════════════════════════════════════════════════════════════════

    public class GroupedProcurementDetailViewModel
    {
        public UserBarViewModel  UserBar      { get; set; }
        public List<string>      AllowedMenus { get; set; }

        public string BasePurchaseID  { get; set; }
        public string SupplierDisplay { get; set; }
        public List<ProcurementChildGroup> Children { get; set; } = new List<ProcurementChildGroup>();

        public string PurchaseStatus  { get; set; }
        public double TotalAmount     { get; set; }
        public string OrderDateStr    { get; set; }
    }

    /// <summary>One child PO and its lines for the detail dialog.</summary>
    public class ProcurementChildGroup
    {
        public string   PurchaseID      { get; set; }
        public string   RequestID       { get; set; }
        public string   UrgencyLevel    { get; set; }
        public string   TriggerType     { get; set; }
        public string   PurchaseStatus  { get; set; }
        public double   SubTotal        { get; set; }
        public List<PurchaseOrderLineEntity> Lines { get; set; } = new List<PurchaseOrderLineEntity>();
    }

    // ════════════════════════════════════════════════════════════════════
    //  LEGACY SINGLE-PO DETAIL
    // ════════════════════════════════════════════════════════════════════
    public class ProcurementDetailViewModel
    {
        public UserBarViewModel              UserBar      { get; set; }
        public List<string>                  AllowedMenus { get; set; }
        public ProcurementOrderEntity        Order        { get; set; }
        public List<PurchaseOrderLineEntity> Lines        { get; set; }
    }

    // ════════════════════════════════════════════════════════════════════
    //  SEARCH PAGE VM
    // ════════════════════════════════════════════════════════════════════
    public class SearchProcurementViewModel
    {
        public UserBarViewModel            UserBar      { get; set; }
        public List<string>                AllowedMenus { get; set; }
        public List<ProcurementOrderGroup> Groups       { get; set; }
    }

    // ════════════════════════════════════════════════════════════════════
    //  CREATE PAGE VM
    // ════════════════════════════════════════════════════════════════════
    public class CreateProcurementViewModel
    {
        public UserBarViewModel                UserBar        { get; set; }
        public List<string>                    AllowedMenus   { get; set; }
        public List<MaterialRequestBatchLookup> BatchPrefixes { get; set; }
        public List<SupplierLookup>            Suppliers      { get; set; }
        public string                          NextPurchaseID { get; set; }
    }
}
