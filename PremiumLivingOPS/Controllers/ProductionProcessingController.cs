using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using PremiumLivingOPS.Services;
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Controller (MVC middle layer) for Production Processing.
    /// All DB-write operations are audit-logged via AuditLogger.
    /// Contains NO UI code.
    /// </summary>
    public class ProductionProcessingController
    {
        private readonly ProductionProcessingRepo _repo = new ProductionProcessingRepo();

        // ── Search Production Orders ───────────────────────────────────

        public ProductionListViewModel GetProductionListVM(
            string keyword = null,
            string status  = null)
        {
            var user = SessionManager.CurrentUser;
            return new ProductionListViewModel
            {
                UserBar      = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                Orders       = _repo.SearchProductionOrders(keyword, status)
            };
        }

        public ProductionDetailViewModel GetProductionDetailVM(string productionId)
        {
            var user = SessionManager.CurrentUser;
            return new ProductionDetailViewModel
            {
                UserBar      = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                Order        = _repo.GetProductionOrderById(productionId),
                Lines        = _repo.GetProductionLines(productionId)
            };
        }

        // ── Create Production Order ────────────────────────────────────

        public CreateProductionViewModel GetCreateProductionVM()
        {
            var user = SessionManager.CurrentUser;
            return new CreateProductionViewModel
            {
                UserBar      = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                NextID       = _repo.GenerateNextProductionId(),
                Products     = _repo.GetFinishedGoodItems(),
                RawMaterials = _repo.GetRawMaterialItems()
            };
        }

        public string GenerateNextProductionId() => _repo.GenerateNextProductionId();

        /// <summary>Creates a production order and logs the CREATE.</summary>
        public bool CreateProductionOrder(ProductionOrderEntity order, List<ProductionLineEntity> lines)
        {
            bool ok = _repo.CreateProductionOrder(order, lines);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_CREATE, "ProductionOrder",
                    oldValue: null,
                    newValue: AuditLogger.Snapshot(
                        ("ID",      order.ProductionID),
                        ("Product", order.ItemID ?? ""),
                        ("Qty",     order.PlannedQty.ToString()),
                        ("Status",  order.Status ?? ""),
                        ("Lines",   (lines?.Count ?? 0).ToString())));
            return ok;
        }

        // ── Update Production Status ───────────────────────────────────

        /// <summary>Updates production order status and logs the EDIT.</summary>
        public bool UpdateProductionStatus(string productionId, string newStatus)
        {
            var old = _repo.GetProductionOrderById(productionId);
            string oldSnap = old == null ? productionId
                : AuditLogger.Snapshot(
                    ("ID",     old.ProductionID),
                    ("Status", old.Status ?? ""),
                    ("Item",   old.ItemID ?? ""));

            bool ok = _repo.UpdateProductionStatus(productionId, newStatus);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_EDIT, "ProductionOrder",
                    oldValue: oldSnap,
                    newValue: AuditLogger.Snapshot(
                        ("ID",     productionId),
                        ("Status", newStatus)));
            return ok;
        }

        // ── Read helpers ───────────────────────────────────────────────

        public List<ProductionOrderEntity> SearchProductionOrders(string kw, string status)
            => _repo.SearchProductionOrders(kw, status);

        public ProductionOrderEntity GetProductionOrderById(string id)
            => _repo.GetProductionOrderById(id);

        public List<ProductionLineEntity> GetProductionLines(string id)
            => _repo.GetProductionLines(id);

        public List<InventoryItemEntity> GetFinishedGoodItems() => _repo.GetFinishedGoodItems();
        public List<InventoryItemEntity> GetRawMaterialItems()  => _repo.GetRawMaterialItems();
    }
}
