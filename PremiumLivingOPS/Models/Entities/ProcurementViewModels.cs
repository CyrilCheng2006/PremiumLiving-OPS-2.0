using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.Entities
{
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

        /// <summary>All child PurchaseIDs that share this BasePurchaseID (may be empty if PO has no suffix).</summary>
        public List<string> ChildPurchaseIDs { get; set; } = new List<string>();

        public string   SupplierID     { get; set; }
        public string   SupplierName   { get; set; }
        public DateTime OrderDate      { get; set; }
        public string   PurchaseStatus { get; set; }
        public double   TotalAmount    { get; set; }
        public int      ItemCount      { get; set; }   // total line count across all child POs
        public string   UrgencyLevel   { get; set; }

        public string OrderDateStr => OrderDate.ToString("yyyy-MM-dd");
    }

    // ════════════════════════════════════════════════════════════════════
    //  DETAIL (grouped – one dialog for all child POs under a BaseID)
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// ViewModel passed to the Detail Dialog.
    /// Contains one GroupHeader per child PO (PO-YYYYMMDD-NNNN-NN),
    /// each with its own line items.
    /// </summary>
    public class GroupedProcurementDetailViewModel
    {
        public UserBarViewModel  UserBar      { get; set; }
        public List<string>      AllowedMenus { get; set; }

        /// <summary>Base PO ID shown in the dialog title, e.g. "PO-20260701-0001".</summary>
        public string BasePurchaseID { get; set; }

        /// <summary>Supplier of the first child PO (all children share the same supplier).</summary>
        public string SupplierDisplay { get; set; }

        /// <summary>Ordered list of child PO sections to render in the dialog.</summary>
        public List<ProcurementChildGroup> Children { get; set; } = new List<ProcurementChildGroup>();

        public string PurchaseStatus  { get; set; }
        public double TotalAmount     { get; set; }
        public string OrderDateStr    { get; set; }
    }

    /// <summary>One child PO and its lines for the detail dialog.</summary>
    public class ProcurementChildGroup
    {
        public string   PurchaseID      { get; set; }  // e.g. "PO-20260701-0001-01"
        public string   RequestID       { get; set; }
        public string   UrgencyLevel    { get; set; }
        public string   TriggerType     { get; set; }
        public string   PurchaseStatus  { get; set; }
        public double   SubTotal        { get; set; }
        public List<PurchaseOrderLineEntity> Lines { get; set; } = new List<PurchaseOrderLineEntity>();
    }

    // ════════════════════════════════════════════════════════════════════
    //  LEGACY SINGLE-PO DETAIL (kept for backward-compat if needed)
    // ════════════════════════════════════════════════════════════════════
    public class ProcurementDetailViewModel
    {
        public UserBarViewModel            UserBar      { get; set; }
        public List<string>                AllowedMenus { get; set; }
        public ProcurementOrderEntity      Order        { get; set; }
        public List<PurchaseOrderLineEntity> Lines      { get; set; }
    }

    // ════════════════════════════════════════════════════════════════════
    //  SEARCH PAGE VM
    // ════════════════════════════════════════════════════════════════════
    public class SearchProcurementViewModel
    {
        public UserBarViewModel           UserBar      { get; set; }
        public List<string>               AllowedMenus { get; set; }
        public List<ProcurementOrderGroup> Groups      { get; set; }
    }

    // ════════════════════════════════════════════════════════════════════
    //  CREATE PAGE VM
    // ════════════════════════════════════════════════════════════════════
    public class CreateProcurementViewModel
    {
        public UserBarViewModel              UserBar        { get; set; }
        public List<string>                  AllowedMenus   { get; set; }
        public List<MaterialRequestBatchLookup> BatchPrefixes { get; set; }
        public List<SupplierLookup>          Suppliers      { get; set; }
        public string                        NextPurchaseID { get; set; }
    }
}
