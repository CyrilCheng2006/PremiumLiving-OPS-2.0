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
    ///
    /// RequestID Plan A — Batch Prefix Grouping
    /// ───────────────────────────────────────────
    ///   UI shows         : MRQ-YYMMDD-NNN          (batch prefix)
    ///   DB PK per line   : MRQ-YYMMDD-NNN-NN       (-NN suffix hidden from user)
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
                WarehouseItems = new List<WarehouseItemLookup>(),
                Orders         = _repo.GetActiveOrders(),
                // NextRequestID now returns the Batch Prefix (no -NN suffix)
                NextRequestID  = _repo.GenerateNextBatchPrefix()
            };
        }

        public List<WarehouseItemLookup> GetWarehouseItemsForMaterial(string rawMaterialItemId)
            => _repo.GetWarehouseItemsByMaterial(rawMaterialItemId);

        /// <summary>
        /// Submits one material-request line.
        /// requestId must already be the full DB PK, i.e. batchPrefix + "-NN".
        /// </summary>
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

        /// <summary>
        /// Returns the next Batch Prefix for today (UI display value).
        /// e.g. "MRQ-260701-001"
        /// </summary>
        public string GenerateNextBatchPrefix() => _repo.GenerateNextBatchPrefix();

        /// <summary>Legacy alias kept for backward compatibility.</summary>
        public string GenerateNextRequestId() => _repo.GenerateNextBatchPrefix();
    }
}
