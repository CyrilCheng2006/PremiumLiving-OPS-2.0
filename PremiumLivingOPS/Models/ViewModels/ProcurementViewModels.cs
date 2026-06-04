using System;
using System.Collections.Generic;
using PremiumLivingOPS.Models.Entities;          // GoodsReceivedEntity, PurchaseOrderEntity

namespace PremiumLivingOPS.Models.ViewModels
{
    // ── Procurement Module — Page-level ViewModels ───────────────────────────

    /// <summary>
    /// ViewModel for the Procurement Overview / Purchase Order list page.
    /// Follows AppShell contract: UserBar + AllowedMenus must always be populated.
    /// </summary>
    public class ProcurementPageViewModel
    {
        // AppShell requirements
        public UserBarViewModel          UserBar      { get; set; }
        public string[]                  AllowedMenus { get; set; }

        // Page data — PurchaseOrderEntity already defined in GoodsReceivedEntity.cs
        public List<PurchaseOrderEntity> PurchaseOrders { get; set; } = new List<PurchaseOrderEntity>();
    }

    /// <summary>
    /// ViewModel for the Create / Edit Purchase Order form.
    /// </summary>
    public class ProcurementFormViewModel
    {
        // AppShell requirements
        public UserBarViewModel UserBar      { get; set; }
        public string[]         AllowedMenus { get; set; }

        // Form data
        public PurchaseOrderEntity PurchaseOrder { get; set; } = new PurchaseOrderEntity();

        // Dropdown sources
        public List<SupplierDropdownItem> Suppliers { get; set; } = new List<SupplierDropdownItem>();
    }

    /// <summary>
    /// Lightweight supplier item used for dropdown lists in procurement forms.
    /// </summary>
    public class SupplierDropdownItem
    {
        public string SupplierID   { get; set; }
        public string SupplierName { get; set; }
    }
}
