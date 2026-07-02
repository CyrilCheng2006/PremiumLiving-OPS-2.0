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

        // ══ SEARCH PROCUREMENT ════════════════════════════════════════════════════════════════════════════════════

        public SearchProcurementViewModel GetSearchProcurementVM(
            string keyword     = null,
            string status      = null,
            DateTime? dateFrom = null,
            DateTime? dateTo   = null)
        {
            var user   = SessionManager.CurrentUser;
            var raw    = _repo.SearchGroupedPurchaseOrders(keyword, status, dateFrom, dateTo);

            // ── C# grouping layer ────────────────────────────────────────────────────────────────────
            // PO header format: PO-YYYYMMDD-NNNN  (16 characters)
            //   e.g.  PO-20260702-0001   length = 16
            // PO line format:   PO-YYYYMMDD-NNNN-NN  (19 characters)
            //   e.g.  PO-20260702-0001-01  length = 19
            //
            // Rules:
            //   • length == 16  → already a proper header; use as-is.
            //   • length  > 16  → strip everything after char 16 to get the
            //     header key, then merge all rows sharing the same key.
            //     Merged group sums ItemCount & TotalAmount, picks the most
            //     recent OrderDate and the first non-empty Status/UrgencyLevel.
            // ──────────────────────────────────────────────────────────────────────────
            const int HEADER_LEN = 16; // PO-YYYYMMDD-NNNN

            // Step 1: normalise every row to its 16-char header key
            var keyed = raw.Select(g => new
            {
                HeaderKey = g.PurchaseID.Length > HEADER_LEN
                    ? g.PurchaseID.Substring(0, HEADER_LEN)
                    : g.PurchaseID,
                Group = g
            }).ToList();

            // Step 2: group by HeaderKey and merge
            var groups = keyed
                .GroupBy(x => x.HeaderKey)
                .Select(grp =>
                {
                    var first = grp.First().Group;
                    return new ProcurementOrderGroup
                    {
                        PurchaseID     = grp.Key,
                        SupplierID     = first.SupplierID,
                        SupplierName   = first.SupplierName,
                        OrderDate      = grp.Max(x => x.Group.OrderDate),
                        PurchaseStatus = grp.First(x => !string.IsNullOrEmpty(x.Group.PurchaseStatus)).Group.PurchaseStatus,
                        TotalAmount    = grp.Sum(x => x.Group.TotalAmount),
                        ItemCount      = grp.Sum(x => x.Group.ItemCount > 0 ? x.Group.ItemCount : 1),
                        UrgencyLevel   = grp.FirstOrDefault(x => !string.IsNullOrEmpty(x.Group.UrgencyLevel))?.Group.UrgencyLevel ?? string.Empty
                    };
                })
                .OrderByDescending(g => g.OrderDate)
                .ThenByDescending(g => g.PurchaseID)
                .ToList();

            return new SearchProcurementViewModel
            {
                UserBar      = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department),
                Groups       = groups
            };
        }

        // ══ DETAIL ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns the PO header + all its PurchaseOrderLine items for the Detail dialog.
        /// purchaseId = the 16-char header key shown in the grid (PO-YYYYMMDD-NNNN).
        /// For legacy rows whose PurchaseOrder.PurchaseID has a -NN suffix, we fall back
        /// to querying PurchaseOrderLine directly so all items appear in the detail view.
        /// </summary>
        public ProcurementDetailViewModel GetProcurementDetailVM(string purchaseId)
        {
            if (string.IsNullOrWhiteSpace(purchaseId)) return null;
            var user = SessionManager.CurrentUser;

            // Try exact header lookup first (new data)
            var order = _repo.GetPurchaseOrderById(purchaseId);
            List<PurchaseOrderLineEntity> lines;

            if (order != null)
            {
                // Proper header exists — fetch lines normally
                lines = _repo.GetLinesByPurchaseId(purchaseId);
            }
            else
            {
                // Legacy data: no exact header row exists for this key.
                // Build a synthetic header from the first -NN row found,
                // and collect lines from all -NN variants.
                order = _repo.GetPurchaseOrderByPrefix(purchaseId);   // finds PO-YYYYMMDD-NNNN-01
                lines = _repo.GetLinesByPurchaseIdPrefix(purchaseId); // finds all -NN lines
            }

            return new ProcurementDetailViewModel
            {
                UserBar      = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department),
                Order        = order,
                Lines        = lines
            };
        }

        // ══ CREATE PROCUREMENT ══════════════════════════════════════════════════════════════════════════════════

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
        /// (POLineID = PO-YYYYMMDD-NNNN-01, -02 …), one per MRQ line.
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
            string purchaseId = _repo.GenerateNextPurchaseId();
            double poTotal    = lines.Sum(ln => ln.OrderQty * ln.UnitPrice);

            string urgencyLevel = string.Empty;
            string triggerType  = string.Empty;

            _repo.CreatePurchaseOrderBatch(
                purchaseId, supplierId, poTotal,
                orderDate, status,
                urgencyLevel, triggerType,
                lines, staffId);
        }

        // ══ HELPERS ═════════════════════════════════════════════════════════════════════════════════════════
        public List<SupplierLookup> GetAllSuppliers()  => _repo.GetAllSuppliers();
        public string GenerateNextPurchaseId()         => _repo.GenerateNextPurchaseId();
    }
}
