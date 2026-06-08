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

        // ── Supplier List ────────────────────────────────────────────

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

        /// <summary>
        /// Returns the next available SupplierID in SP-XXX format.
        /// </summary>
        public string GetNextSupplierID()
            => _repo.GetNextSupplierID();

        /// <summary>
        /// Inserts a new supplier.
        /// Returns true on success.
        /// </summary>
        public bool AddSupplier(SupplierEntity supplier)
        {
            try   { return _repo.InsertSupplier(supplier); }
            catch { return false; }
        }

        /// <summary>
        /// Updates an existing supplier's editable fields.
        /// Returns true on success.
        /// </summary>
        public bool UpdateSupplier(string supplierId, string name, string phone, string address)
            => _repo.UpdateSupplier(supplierId, name, phone, address);

        // ── Customer List ────────────────────────────────────────────

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
