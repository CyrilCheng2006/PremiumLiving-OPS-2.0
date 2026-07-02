using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Controller for Raw Material → Procurement module.
    /// </summary>
    public class ProcurementController
    {
        private readonly ProcurementRepo      _repo    = new ProcurementRepo();
        private readonly InventoryControlRepo _invRepo = new InventoryControlRepo();

        // ══ SEARCH PROCUREMENT ══════════════════════════════════════════

        public SearchProcurementViewModel GetSearchProcurementVM(
            string keyword     = null,
            string status      = null,
            DateTime? dateFrom = null,
            DateTime? dateTo   = null)
        {
            var user = SessionManager.CurrentUser;
            return new SearchProcurementViewModel
            {
                UserBar      = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department),
                Orders       = _repo.SearchPurchaseOrders(keyword, status, dateFrom, dateTo)
            };
        }

        public ProcurementDetailViewModel GetProcurementDetailVM(string purchaseId)
        {
            if (string.IsNullOrWhiteSpace(purchaseId)) return null;
            var user = SessionManager.CurrentUser;
            return new ProcurementDetailViewModel
            {
                UserBar      = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department),
                Order        = _repo.GetPurchaseOrderById(purchaseId),
                Lines        = _repo.GetLinesByPurchaseId(purchaseId)
            };
        }

        // ══ CREATE PROCUREMENT ══════════════════════════════════════════

        public CreateProcurementViewModel GetCreateProcurementVM()
        {
            var user = SessionManager.CurrentUser;
            return new CreateProcurementViewModel
            {
                UserBar        = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus   = NavAccessPolicy.GetAllowedMenus(user?.Department),
                BatchPrefixes  = _repo.GetUnlinkedBatchPrefixes(),
                Suppliers      = _repo.GetAllSuppliers(),
                NextPurchaseID = _repo.GenerateNextPurchaseId()
            };
        }

        /// <summary>
        /// Returns all -NN line items for a given batch prefix.
        /// Called when user selects a batch prefix in the dropdown.
        /// </summary>
        public List<MaterialRequestLineItem> GetLinesByBatchPrefix(string batchPrefix)
        {
            if (string.IsNullOrWhiteSpace(batchPrefix))
                return new List<MaterialRequestLineItem>();
            return _repo.GetLineItemsByBatchPrefix(batchPrefix);
        }

        /// <summary>
        /// Submits one PurchaseOrder + one PurchaseOrderLine per staged line.
        /// purchaseIdBase is the base ID; each PO gets purchaseIdBase-NN suffix.
        /// </summary>
        public void SubmitCreateProcurement(
            string purchaseIdBase,
            string supplierId,
            DateTime orderDate,
            string status,
            List<MaterialRequestLineItem> lines)
        {
            if (string.IsNullOrWhiteSpace(supplierId))
                throw new ArgumentException("Please select a Supplier.");
            if (lines == null || lines.Count == 0)
                throw new ArgumentException("No Material Request lines are loaded.");

            for (int i = 0; i < lines.Count; i++)
            {
                var ln = lines[i];
                if (ln.OrderQty <= 0)
                    throw new ArgumentException($"Line {i + 1} ({ln.MaterialName}): Order Quantity must be > 0.");
                if (ln.UnitPrice <= 0)
                    throw new ArgumentException($"Line {i + 1} ({ln.MaterialName}): Unit Price must be > 0.");
                if (string.IsNullOrWhiteSpace(ln.WarehouseID))
                    throw new ArgumentException($"Line {i + 1} ({ln.MaterialName}): Warehouse not resolved.");
            }

            string staffId = SessionManager.CurrentUser?.StaffId ?? "SYSTEM";

            for (int i = 0; i < lines.Count; i++)
            {
                var ln         = lines[i];
                int lineNo     = i + 1;
                string poId    = $"{purchaseIdBase}-{lineNo:D2}";
                double poTotal = ln.OrderQty * ln.UnitPrice;

                _repo.CreatePurchaseOrder(
                    poId, ln.RequestID, supplierId,
                    poTotal, orderDate, status,
                    ln.RawMaterialItemID, ln.WarehouseID,
                    ln.OrderQty, ln.UnitPrice, staffId);
            }
        }

        // ══ HELPERS ═══════════════════════════════════════════════
        public List<SupplierLookup> GetAllSuppliers()  => _repo.GetAllSuppliers();
        public string GenerateNextPurchaseId()         => _repo.GenerateNextPurchaseId();
    }
}
