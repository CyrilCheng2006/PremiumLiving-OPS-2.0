using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Controller (middle layer) for Production Processing module.
    ///
    /// RequestID Plan A — Batch Prefix Grouping
    /// ───────────────────────────────────────────
    ///   UI shows         : MRQ-YYMMDD-NNN          (batch prefix, one grid row)
    ///   DB PK per line   : MRQ-YYMMDD-NNN-NN       (-NN suffix hidden, shown in detail)
    /// </summary>
    public class ProductionProcessingController
    {
        private readonly ProductionProcessingRepo _repo = new ProductionProcessingRepo();

        // ════════════════════════════════════════════════════════════════
        //  SEARCH RAW MATERIAL REQUEST
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns one batch row per BatchPrefix for the search grid,
        /// plus the flat request list for KPI pill counts.
        /// </summary>
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
                Batches      = _repo.SearchMaterialRequestBatches(keyword, urgency, triggerType),
                Requests     = _repo.SearchMaterialRequests(keyword, urgency, triggerType, linkedOnly)
            };
        }

        // ════════════════════════════════════════════════════════════════
        //  MATERIAL REQUEST BATCH DETAIL
        // ════════════════════════════════════════════════════════════════

        /// <summary>Returns the batch detail (header + all line items) for View Detail.</summary>
        public MaterialRequestBatchDetailEntity GetMaterialRequestBatchDetail(string batchPrefix)
            => _repo.GetMaterialRequestBatchDetail(batchPrefix);

        // kept for backward compat
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
                NextRequestID  = _repo.GenerateNextBatchPrefix()
            };
        }

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

        public string GenerateNextBatchPrefix() => _repo.GenerateNextBatchPrefix();
        public string GenerateNextRequestId()   => _repo.GenerateNextBatchPrefix();
    }
}
