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
                Groups       = _repo.SearchGroupedPurchaseOrders(keyword, status, dateFrom, dateTo)
            };
        }

        // ══ DETAIL (GROUPED BY BASE ID) ══════════════════════════════════

        /// <summary>
        /// Returns a grouped detail ViewModel for the Detail Dialog.
        /// baseId = "PO-YYYYMMDD-NNNN" (without the -NN suffix).
        /// Includes all child POs (PO-YYYYMMDD-NNNN-NN) and their line items.
        /// </summary>
        public GroupedProcurementDetailViewModel GetGroupedProcurementDetailVM(string baseId)
        {
            if (string.IsNullOrWhiteSpace(baseId)) return null;
            var user = SessionManager.CurrentUser;
            var vm   = _repo.GetGroupedDetailByBaseId(baseId);
            if (vm != null)
            {
                vm.UserBar      = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" };
                vm.AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department);
            }
            return vm;
        }

        // ══ LEGACY SINGLE-PO DETAIL ══════════════════════════════════════

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

        public List<MaterialRequestLineItem> GetLinesByBatchPrefix(string batchPrefix)
        {
            if (string.IsNullOrWhiteSpace(batchPrefix))
                return new List<MaterialRequestLineItem>();
            return _repo.GetLineItemsByBatchPrefix(batchPrefix);
        }

        /// <summary>
        /// Creates one PurchaseOrder (+ one PurchaseOrderLine) per MRQ line.
        /// Each PO gets its own unique PurchaseID generated from the DB sequence.
        /// </summary>
        public void SubmitCreateProcurement(
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

            foreach (var ln in lines)
            {
                string poId    = _repo.GenerateNextPurchaseId();
                double poTotal = ln.OrderQty * ln.UnitPrice;

                _repo.CreatePurchaseOrder(
                    poId, ln.RequestID, supplierId,
                    poTotal, orderDate, status,
                    ln.RawMaterialItemID, ln.WarehouseID,
                    ln.OrderQty, ln.UnitPrice, staffId);
            }
        }

        // ══ HELPERS ═════════════════════════════════════════════════════
        public List<SupplierLookup> GetAllSuppliers()  => _repo.GetAllSuppliers();
        public string GenerateNextPurchaseId()         => _repo.GenerateNextPurchaseId();
    }
}
