using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Controller (middle layer) for Production Processing module.
    /// The View never accesses ProductionProcessingRepo or the DB directly.
    /// </summary>
    public class ProductionProcessingController
    {
        private readonly ProductionProcessingRepo _repo = new ProductionProcessingRepo();

        // ════════════════════════════════════════════════════════════════
        //  SEARCH RAW MATERIAL REQUEST
        // ════════════════════════════════════════════════════════════════

        public SearchMaterialRequestViewModel GetSearchMaterialRequestVM(
            string keyword     = null,
            string urgency     = null,
            string triggerType = null,
            bool   linkedOnly  = false)
        {
            var user = SessionManager.CurrentUser;
            return new SearchMaterialRequestViewModel
            {
                UserBar      = new UserBarViewModel
                {
                    DisplayName = user?.StaffName  ?? "Unknown",
                    Department  = user?.Department ?? ""
                },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department),
                Requests     = _repo.SearchMaterialRequests(keyword, urgency, triggerType, linkedOnly)
            };
        }

        // ════════════════════════════════════════════════════════════════
        //  MATERIAL REQUEST DETAIL
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns full detail for a single Material Request (used by the detail dialog).
        /// </summary>
        public MaterialRequestDetailEntity GetMaterialRequestDetail(string requestId)
            => _repo.GetMaterialRequestDetail(requestId);

        // ════════════════════════════════════════════════════════════════
        //  CREATE RAW MATERIAL REQUEST
        // ════════════════════════════════════════════════════════════════

        public CreateMaterialRequestViewModel GetCreateMaterialRequestVM()
        {
            var user = SessionManager.CurrentUser;
            return new CreateMaterialRequestViewModel
            {
                UserBar        = new UserBarViewModel
                {
                    DisplayName = user?.StaffName  ?? "Unknown",
                    Department  = user?.Department ?? ""
                },
                AllowedMenus   = NavAccessPolicy.GetAllowedMenus(user?.Department),
                RawMaterials   = _repo.GetAllRawMaterials(),
                WarehouseItems = new List<WarehouseItemLookup>(), // populated after material is selected
                Orders         = _repo.GetActiveOrders(),
                NextRequestID  = _repo.GenerateNextRequestId()
            };
        }

        /// <summary>
        /// Returns WarehouseItems for the given raw material.
        /// Called when the user selects a material in the form.
        /// </summary>
        public List<WarehouseItemLookup> GetWarehouseItemsForMaterial(string rawMaterialItemId)
            => _repo.GetWarehouseItemsByMaterial(rawMaterialItemId);

        public void SubmitCreateMaterialRequest(
            string requestId, string orderId, string rawMaterialItemId,
            string warehouseItemId, int requestedQty,
            string urgencyLevel, string triggerType)
        {
            if (string.IsNullOrWhiteSpace(rawMaterialItemId))
                throw new ArgumentException("Please select a Raw Material.");
            if (string.IsNullOrWhiteSpace(warehouseItemId))
                throw new ArgumentException("Please select a Warehouse / Stock Location.");
            if (requestedQty <= 0)
                throw new ArgumentException("Requested Quantity must be greater than 0.");
            if (triggerType == "OrderDemand" && string.IsNullOrWhiteSpace(orderId))
                throw new ArgumentException("An Order must be selected when Trigger Type is 'Order Demand'.");

            string staffId = SessionManager.CurrentUser?.StaffId ?? "SYSTEM";

            _repo.CreateMaterialRequest(
                requestId, orderId, rawMaterialItemId,
                warehouseItemId, requestedQty,
                urgencyLevel, triggerType, staffId);
        }

        // ════════════════════════════════════════════════════════════════
        //  HELPERS
        // ════════════════════════════════════════════════════════════════

        public string GenerateNextRequestId() => _repo.GenerateNextRequestId();
    }
}
