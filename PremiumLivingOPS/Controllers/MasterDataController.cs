using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Controller (MVC middle layer) for Master Data Maintenance.
    /// Accepts requests from View, delegates to MasterDataRepo, returns ViewModels.
    /// Contains NO UI code.
    /// </summary>
    public class MasterDataController
    {
        private readonly MasterDataRepo _repo = new MasterDataRepo();

        // ── Supplier List ──────────────────────────────────────────────

        /// <summary>
        /// Returns ViewModel for the Supplier List page.
        /// Supports optional keyword search (SupplierID or SupplierName).
        /// </summary>
        public SupplierListViewModel GetSupplierListVM(string keyword = null)
        {
            var user = SessionManager.CurrentUser;
            return new SupplierListViewModel
            {
                UserBar = new UserBarViewModel
                {
                    DisplayName = user?.StaffName ?? "Unknown",
                    Department  = user?.Department ?? ""
                },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                Suppliers    = _repo.SearchSuppliers(keyword)
            };
        }

        // ── Customer List ──────────────────────────────────────────────

        /// <summary>
        /// Returns ViewModel for the Customer List page.
        /// Supports optional keyword search (CustomerID, CustomerName, or EmailAddress).
        /// </summary>
        public CustomerListViewModel GetCustomerListVM(string keyword = null)
        {
            var user = SessionManager.CurrentUser;
            return new CustomerListViewModel
            {
                UserBar = new UserBarViewModel
                {
                    DisplayName = user?.StaffName ?? "Unknown",
                    Department  = user?.Department ?? ""
                },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                Customers    = _repo.SearchCustomers(keyword)
            };
        }
    }
}
