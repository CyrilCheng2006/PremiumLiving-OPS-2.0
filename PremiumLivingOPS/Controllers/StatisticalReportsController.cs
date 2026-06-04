using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using System;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Controller (middle layer) for Statistical Reports module.
    /// Views never access StatisticalReportsRepo or the DB directly.
    /// </summary>
    public class StatisticalReportsController
    {
        private readonly StatisticalReportsRepo _repo = new StatisticalReportsRepo();

        // ── Common helpers ──────────────────────────────────────────────
        private UserBarViewModel MakeUserBar()
        {
            var u = SessionManager.CurrentUser;
            return new UserBarViewModel
            {
                DisplayName = u?.StaffName  ?? "Unknown",
                Department  = u?.Department ?? ""
            };
        }
        private string[] GetMenus()
            => NavAccessPolicy.GetAllowedMenus(SessionManager.CurrentUser?.Department);

        // ════════════════════════════════════════════════════════════════
        //  1. SALES PERFORMANCE
        // ════════════════════════════════════════════════════════════════

        public ViewReportViewModel GetSalesReportVM(DateTime? from = null, DateTime? to = null)
        {
            return new ViewReportViewModel
            {
                UserBar      = MakeUserBar(),
                AllowedMenus = GetMenus(),
                ActiveReport = ReportType.SalesPerformance,
                SalesKpi     = _repo.GetSalesKpi(from, to),
                SalesRows    = _repo.GetSalesRows(from, to),
                TopProducts  = _repo.GetTopProducts(from, to)
            };
        }

        // ════════════════════════════════════════════════════════════════
        //  2. INVENTORY STATUS
        // ════════════════════════════════════════════════════════════════

        public ViewReportViewModel GetInventoryReportVM(string categoryFilter = null, bool belowReorderOnly = false)
        {
            return new ViewReportViewModel
            {
                UserBar       = MakeUserBar(),
                AllowedMenus  = GetMenus(),
                ActiveReport  = ReportType.InventoryStatus,
                InventoryKpi  = _repo.GetInventoryKpi(),
                InventoryRows = _repo.GetInventoryRows(categoryFilter, belowReorderOnly)
            };
        }

        // ════════════════════════════════════════════════════════════════
        //  3. PROCUREMENT SUMMARY
        // ════════════════════════════════════════════════════════════════

        public ViewReportViewModel GetProcurementReportVM(string statusFilter = null)
        {
            return new ViewReportViewModel
            {
                UserBar      = MakeUserBar(),
                AllowedMenus = GetMenus(),
                ActiveReport = ReportType.ProcurementSummary,
                ProcKpi      = _repo.GetProcurementKpi(),
                ProcRows     = _repo.GetProcurementRows(statusFilter)
            };
        }

        // ════════════════════════════════════════════════════════════════
        //  4. LOGISTICS OVERVIEW
        // ════════════════════════════════════════════════════════════════

        public ViewReportViewModel GetLogisticsReportVM(string statusFilter = null)
        {
            return new ViewReportViewModel
            {
                UserBar      = MakeUserBar(),
                AllowedMenus = GetMenus(),
                ActiveReport = ReportType.LogisticsOverview,
                LogKpi       = _repo.GetLogisticsKpi(),
                LogRows      = _repo.GetLogisticsRows(statusFilter)
            };
        }

        // ════════════════════════════════════════════════════════════════
        //  5. AFTER-SERVICE SUMMARY
        // ════════════════════════════════════════════════════════════════

        public ViewReportViewModel GetAfterServiceReportVM(
            string complaintStatusFilter = null,
            string returnStatusFilter    = null)
        {
            return new ViewReportViewModel
            {
                UserBar      = MakeUserBar(),
                AllowedMenus = GetMenus(),
                ActiveReport = ReportType.AfterServiceSummary,
                AfterKpi     = _repo.GetAfterServiceKpi(),
                Complaints   = _repo.GetComplaintRows(complaintStatusFilter),
                Returns      = _repo.GetReturnOrderRows(returnStatusFilter)
            };
        }

        // ════════════════════════════════════════════════════════════════
        //  6. FINANCE OVERVIEW
        // ════════════════════════════════════════════════════════════════

        public ViewReportViewModel GetFinanceReportVM(DateTime? from = null, DateTime? to = null)
        {
            return new ViewReportViewModel
            {
                UserBar      = MakeUserBar(),
                AllowedMenus = GetMenus(),
                ActiveReport = ReportType.FinanceOverview,
                FinanceKpi   = _repo.GetFinanceKpi(),
                FinanceRows  = _repo.GetFinanceTransactionRows(from, to)
            };
        }
    }
}
