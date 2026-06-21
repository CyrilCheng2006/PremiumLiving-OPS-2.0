using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Services;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Controller (MVC middle layer) for Master Data Maintenance.
    /// Accepts requests from View, delegates to MasterDataRepo, returns ViewModels.
    /// All Add / Update operations are audit-logged via AuditLogger.
    /// Contains NO UI code.
    /// </summary>
    public class MasterDataController
    {
        private readonly MasterDataRepo _repo = new MasterDataRepo();

        // ═ Supplier ═══════════════════════════════════════════════════════════════
        public SupplierListViewModel GetSupplierListVM(string keyword = null)
        {
            var user = SessionManager.CurrentUser;
            return new SupplierListViewModel
            {
                UserBar = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                Suppliers    = _repo.SearchSuppliers(keyword)
            };
        }

        public string GetNextSupplierID() => _repo.GetNextSupplierID();

        public bool AddSupplier(SupplierEntity supplier)
        {
            try
            {
                bool ok = _repo.InsertSupplier(supplier);
                if (ok)
                    AuditLogger.Write(
                        AuditLogger.TYPE_CREATE, "Supplier",
                        oldValue: null,
                        newValue: AuditLogger.Snapshot(
                            ("ID",      supplier.SupplierID),
                            ("Name",    supplier.SupplierName),
                            ("Phone",   supplier.PhoneNumber),
                            ("Address", supplier.SupplierAddress)));
                return ok;
            }
            catch { return false; }
        }

        public bool UpdateSupplier(string supplierId, string name, string phone, string address)
        {
            // Snapshot OLD before overwriting
            var old = _repo.SearchSuppliers(supplierId).Find(s => s.SupplierID == supplierId);
            string oldSnap = old == null ? null :
                AuditLogger.Snapshot(("ID", old.SupplierID), ("Name", old.SupplierName), ("Phone", old.PhoneNumber), ("Address", old.SupplierAddress));

            bool ok = _repo.UpdateSupplier(supplierId, name, phone, address);
            if (ok)
                AuditLogger.Write(
                    AuditLogger.TYPE_EDIT, "Supplier",
                    oldValue: oldSnap,
                    newValue: AuditLogger.Snapshot(("ID", supplierId), ("Name", name), ("Phone", phone), ("Address", address)));
            return ok;
        }

        // ═ Customer ═══════════════════════════════════════════════════════════════
        public CustomerListViewModel GetCustomerListVM(string keyword = null)
        {
            var user = SessionManager.CurrentUser;
            return new CustomerListViewModel
            {
                UserBar = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                Customers    = _repo.SearchCustomers(keyword)
            };
        }

        public string GetNextCustomerID() => _repo.GetNextCustomerID();

        public bool AddCustomer(CustomerEntity customer)
        {
            try
            {
                bool ok = _repo.InsertCustomer(customer);
                if (ok)
                    AuditLogger.Write(
                        AuditLogger.TYPE_CREATE, "Customer",
                        oldValue: null,
                        newValue: AuditLogger.Snapshot(
                            ("ID",    customer.CustomerID),
                            ("Name",  customer.CustomerName),
                            ("Email", customer.EmailAddress),
                            ("Phone", customer.PhoneNumber)));
                return ok;
            }
            catch { return false; }
        }

        public bool UpdateCustomer(string customerId, string name, string email, string phone)
        {
            var old = _repo.SearchCustomers(customerId).Find(c => c.CustomerID == customerId);
            string oldSnap = old == null ? null :
                AuditLogger.Snapshot(("ID", old.CustomerID), ("Name", old.CustomerName), ("Email", old.EmailAddress), ("Phone", old.PhoneNumber));

            bool ok = _repo.UpdateCustomer(customerId, name, email, phone);
            if (ok)
                AuditLogger.Write(
                    AuditLogger.TYPE_EDIT, "Customer",
                    oldValue: oldSnap,
                    newValue: AuditLogger.Snapshot(("ID", customerId), ("Name", name), ("Email", email), ("Phone", phone)));
            return ok;
        }
    }
}
