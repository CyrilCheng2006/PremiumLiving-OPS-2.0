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

        // ── Supplier ───────────────────────────────────────────────────────────────

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

        public string GetNextSupplierID() => _repo.GetNextSupplierID();

        public bool AddSupplier(SupplierEntity supplier)
        {
            try   { return _repo.InsertSupplier(supplier); }
            catch { return false; }
        }

        public bool UpdateSupplier(string supplierId, string name, string phone, string address)
            => _repo.UpdateSupplier(supplierId, name, phone, address);

        // ── Customer ───────────────────────────────────────────────────────────────

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

        /// <summary>Returns the next available CustomerID in C-XXXX format.</summary>
        public string GetNextCustomerID() => _repo.GetNextCustomerID();

        /// <summary>Inserts a new customer. Returns true on success.</summary>
        public bool AddCustomer(CustomerEntity customer)
        {
            try   { return _repo.InsertCustomer(customer); }
            catch { return false; }
        }

        /// <summary>Updates an existing customer's editable fields. Returns true on success.</summary>
        public bool UpdateCustomer(string customerId, string name, string email, string phone)
            => _repo.UpdateCustomer(customerId, name, email, phone);
    }
}
