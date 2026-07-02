using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Controller for Raw Material → Procurement module.
    ///
    /// PO lifecycle:
    ///   Create  → one PurchaseOrder header (PO-YYYYMMDD-NNNN)
    ///             + N PurchaseOrderLine rows (POLineID = PO-YYYYMMDD-NNNN-01/-02…)
    ///   Search  → grid shows one row per PurchaseOrder header
    ///   Detail  → dialog shows header + all N lines
    /// </summary>
    public class ProcurementController
    {
        private readonly ProcurementRepo      _repo    = new ProcurementRepo();
        private readonly InventoryControlRepo _invRepo = new InventoryControlRepo();

        // ══ SEARCH PROCUREMENT ═══════════════════════════════════════════════════════════════

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

        // ══ DETAIL ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns the PO header + all its PurchaseOrderLine items for the Detail dialog.
        /// purchaseId = exact PurchaseID from DB, e.g. "PO-20260702-0001" (no -NN suffix).
        /// </summary>
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

        // ══ CREATE PROCUREMENT ═══════════════════════════════════════════════════════════════

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
        /// Creates ONE PurchaseOrder header (PO-YYYYMMDD-NNNN) with N PurchaseOrderLine rows
        /// (POLineID = PO-YYYYMMDD-NNNN-01, -02 …), one per MRQ line in <paramref name="lines"/>.
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

            string staffId    = SessionManager.CurrentUser?.StaffId ?? "SYSTEM";
            string purchaseId = _repo.GenerateNextPurchaseId();   // one ID for the whole batch
            double poTotal    = lines.Sum(ln => ln.OrderQty * ln.UnitPrice);

            // Derive urgency / trigger from the first line's associated MRQ
            // (all lines in a batch share the same MRQ batch → same urgency/trigger).
            // If the caller hasn't pre-populated these, pass empty strings.
            string urgencyLevel = string.Empty;
            string triggerType  = string.Empty;

            _repo.CreatePurchaseOrderBatch(
                purchaseId, supplierId, poTotal,
                orderDate, status,
                urgencyLevel, triggerType,
                lines, staffId);
        }

        // ══ HELPERS ═══════════════════════════════════════════════════════════════════════════
        public List<SupplierLookup> GetAllSuppliers()  => _repo.GetAllSuppliers();
        public string GenerateNextPurchaseId()         => _repo.GenerateNextPurchaseId();
    }
}
