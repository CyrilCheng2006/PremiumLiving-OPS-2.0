using System.Collections.Generic;

namespace PremiumLivingOPS.Models.Entities
{
    // ── Shared sub-models ──────────────────────────────────────────────────────

    public class UserBarViewModel
    {
        public string DisplayName { get; set; }
        public string Department  { get; set; }
    }

    // ── Supplier List ──────────────────────────────────────────────────────────

    /// <summary>
    /// ViewModel for the Supplier List page.
    /// </summary>
    public class SupplierListViewModel
    {
        public UserBarViewModel     UserBar      { get; set; }
        public string[]             AllowedMenus { get; set; }
        public List<SupplierEntity> Suppliers    { get; set; } = new List<SupplierEntity>();
    }

    // ── Customer List ──────────────────────────────────────────────────────────

    /// <summary>
    /// ViewModel for the Customer List page.
    /// </summary>
    public class CustomerListViewModel
    {
        public UserBarViewModel     UserBar      { get; set; }
        public string[]             AllowedMenus { get; set; }
        public List<CustomerEntity> Customers    { get; set; } = new List<CustomerEntity>();
    }
}
