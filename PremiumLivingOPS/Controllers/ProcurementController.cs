using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;   // SearchProcurementViewModel, CreateProcurementViewModel, ProcurementDetailViewModel
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Controller (middle layer) for Raw Material → Procurement module.
    /// The View never accesses ProcurementRepo or the DB directly.
    /// </summary>
    public class ProcurementController
    {
        private readonly ProcurementRepo       _repo    = new ProcurementRepo();
        private readonly InventoryControlRepo  _invRepo = new InventoryControlRepo();

        // ════════════════════════════════════════════════════════════════
        //  SEARCH PROCUREMENT
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns a ViewModel for Search Procurement with optional filters.
        /// </summary>
        public SearchProcurementViewModel GetSearchProcurementVM(
            string keyword    = null,
            string status     = null,
            DateTime? dateFrom = null,
            DateTime? dateTo   = null)
        {
            var user = SessionManager.CurrentUser;
            return new SearchProcurementViewModel
            {
                UserBar      = new UserBarViewModel
                {
                    DisplayName = user?.StaffName  ?? "Unknown",
                    Department  = user?.Department ?? ""
                },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department),
                Orders       = _repo.SearchPurchaseOrders(keyword, status, dateFrom, dateTo)
            };
        }

        /// <summary>
        /// Returns a detail ViewModel for viewing a single PurchaseOrder + its lines.
        /// </summary>
        public ProcurementDetailViewModel GetProcurementDetailVM(string purchaseId)
        {
            var user = SessionManager.CurrentUser;
            return new ProcurementDetailViewModel
            {
                UserBar      = new UserBarViewModel
                {
                    DisplayName = user?.StaffName  ?? "Unknown",
                    Department  = user?.Department ?? ""
                },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department),
                Order        = _repo.GetPurchaseOrderById(purchaseId),
                Lines        = _repo.GetLinesByPurchaseId(purchaseId)
            };
        }

        // ════════════════════════════════════════════════════════════════
        //  CREATE PROCUREMENT
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns a ViewModel for Create Procurement (with all dropdown data).
        /// </summary>
        public CreateProcurementViewModel GetCreateProcurementVM()
        {
            var user = SessionManager.CurrentUser;
            return new CreateProcurementViewModel
            {
                UserBar          = new UserBarViewModel
                {
                    DisplayName = user?.StaffName  ?? "Unknown",
                    Department  = user?.Department ?? ""
                },
                AllowedMenus     = NavAccessPolicy.GetAllowedMenus(user?.Department),
                MaterialRequests = _repo.GetUnlinkedMaterialRequests(),
                Suppliers        = _repo.GetAllSuppliers(),
                Warehouses       = _invRepo.GetAllWarehouses(),
                NextPurchaseID   = _repo.GenerateNextPurchaseId()
            };
        }

        /// <summary>
        /// Validates and submits a new PurchaseOrder.
        /// Throws ArgumentException on validation failure.
        /// </summary>
        public void SubmitCreateProcurement(
            string purchaseId,
            string requestId,
            string supplierId,
            DateTime orderDate,
            string status,
            string rawMaterialItemId,
            string warehouseId,
            int    orderQty,
            double unitPrice)
        {
            // ── Validation ────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(requestId))
                throw new ArgumentException("Please select a Material Request.");
            if (string.IsNullOrWhiteSpace(supplierId))
                throw new ArgumentException("Please select a Supplier.");
            if (string.IsNullOrWhiteSpace(rawMaterialItemId))
                throw new ArgumentException("Raw Material could not be resolved from the selected Request.");
            if (string.IsNullOrWhiteSpace(warehouseId))
                throw new ArgumentException("Please select a Delivery Warehouse.");
            if (orderQty <= 0)
                throw new ArgumentException("Order Quantity must be greater than 0.");
            if (unitPrice <= 0)
                throw new ArgumentException("Unit Price must be greater than 0.");

            double poTotal = orderQty * unitPrice;
            string staffId = SessionManager.CurrentUser?.StaffID ?? "SYSTEM";

            _repo.CreatePurchaseOrder(
                purchaseId,
                requestId,
                supplierId,
                poTotal,
                orderDate,
                status,
                rawMaterialItemId,
                warehouseId,
                orderQty,
                unitPrice,
                staffId);
        }

        // ════════════════════════════════════════════════════════════════
        //  HELPERS
        // ════════════════════════════════════════════════════════════════

        /// <summary>Returns all Warehouses (used by dropdowns).</summary>
        public List<WarehouseEntity> GetAllWarehouses()
            => _invRepo.GetAllWarehouses();

        /// <summary>Returns all Suppliers.</summary>
        public List<SupplierLookup> GetAllSuppliers()
            => _repo.GetAllSuppliers();

        /// <summary>Generates a fresh PurchaseID (used after a successful save).</summary>
        public string GenerateNextPurchaseId()
            => _repo.GenerateNextPurchaseId();
    }
}
