using System;
using System.Collections.Generic;
using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using PremiumLivingOPS.Services;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Controller (MVC middle layer) for Production Processing.
    /// Covers: Raw Material Requests + (future) Production Order management.
    /// All DB-write operations are audit-logged via AuditLogger.
    /// Contains NO UI code.
    /// </summary>
    public class ProductionProcessingController
    {
        private readonly ProductionProcessingRepo _repo = new ProductionProcessingRepo();

        // ── Material Request ──────────────────────────────────────────────

        public SearchMaterialRequestViewModel GetSearchMaterialRequestVM(
            string keyword = null,
            string status  = null)
        {
            var user = SessionManager.CurrentUser;
            return new SearchMaterialRequestViewModel
            {
                UserBar      = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                Requests     = _repo.SearchMaterialRequests(keyword, status)
            };
        }

        public CreateMaterialRequestViewModel GetCreateMaterialRequestVM()
        {
            var user = SessionManager.CurrentUser;
            return new CreateMaterialRequestViewModel
            {
                UserBar        = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus   = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                RawMaterials   = _repo.GetRawMaterialLookups(),
                WarehouseItems = _repo.GetWarehouseItemLookups(),
                Orders         = _repo.GetOrderLookups(),
                NextRequestID  = _repo.GenerateNextRequestId()
            };
        }

        public string GenerateNextRequestId() => _repo.GenerateNextRequestId();

        /// <summary>Creates a Material Request and logs the CREATE.</summary>
        public bool CreateMaterialRequest(MaterialRequestEntity req)
        {
            bool ok = _repo.InsertMaterialRequest(req);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_CREATE, "MaterialRequest",
                    oldValue: null,
                    newValue: AuditLogger.Snapshot(
                        ("ID",       req.RequestID),
                        ("Material", req.RawMaterialItemID ?? ""),
                        ("Qty",      req.RequestedQty.ToString()),
                        ("Urgency",  req.UrgencyLevel ?? "")));
            return ok;
        }

        // ── Update Material Request Status ─────────────────────────────

        // MaterialRequestEntity has no explicit Status column in the file;
        // status updates delegate entirely to Repo.
        public bool UpdateMaterialRequestStatus(string requestId, string newStatus)
        {
            bool ok = _repo.UpdateMaterialRequestStatus(requestId, newStatus);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_EDIT, "MaterialRequest",
                    oldValue: AuditLogger.Snapshot(("ID", requestId)),
                    newValue: AuditLogger.Snapshot(
                        ("ID",     requestId),
                        ("Status", newStatus)));
            return ok;
        }

        // ── Read helpers ──────────────────────────────────────────────

        public List<MaterialRequestEntity> SearchMaterialRequests(string kw, string status)
            => _repo.SearchMaterialRequests(kw, status);

        public MaterialRequestDetailEntity GetMaterialRequestDetail(string requestId)
            => _repo.GetMaterialRequestDetail(requestId);

        public List<RawMaterialLookup>   GetRawMaterialLookups()   => _repo.GetRawMaterialLookups();
        public List<WarehouseItemLookup> GetWarehouseItemLookups() => _repo.GetWarehouseItemLookups();
        public List<OrderLookup>         GetOrderLookups()         => _repo.GetOrderLookups();
    }
}
