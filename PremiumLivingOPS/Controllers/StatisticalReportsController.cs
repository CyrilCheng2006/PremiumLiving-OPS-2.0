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

        // ── Common helpers ──────────────────────────────────────────────────────────────
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

        /// <param name="categoryFilter">null/"All" / "Product" / "Raw Material"</param>
        /// <param name="keyword">Searches ItemID and ItemName (partial match).</param>
        public ViewReportViewModel GetInventoryReportVM(
            string categoryFilter  = null,
            bool   belowReorderOnly = false,
            string keyword          = null)
        {
            return new ViewReportViewModel
            {
                UserBar       = MakeUserBar(),
                AllowedMenus  = GetMenus(),
                ActiveReport  = ReportType.InventoryStatus,
                InventoryKpi  = _repo.GetInventoryKpi(),
                InventoryRows = _repo.GetInventoryRows(categoryFilter, belowReorderOnly, keyword)
            };
        }

        // ════════════════════════════════════════════════════════════════
        //  3. PROCUREMENT SUMMARY
        // ════════════════════════════════════════════════════════════════

        /// <param name="statusFilter">null/"All"/"Sent"/"Partially Received"/"Received"/"Completed"/"Cancelled"</param>
        public ViewReportViewModel GetProcurementReportVM(
            string   statusFilter = null,
            DateTime? from        = null,
            DateTime? to          = null)
        {
            return new ViewReportViewModel
            {
                UserBar      = MakeUserBar(),
                AllowedMenus = GetMenus(),
                ActiveReport = ReportType.ProcurementSummary,
                ProcKpi      = _repo.GetProcurementKpi(),
                ProcRows     = _repo.GetProcurementRows(statusFilter, from, to)
            };
        }

        // ════════════════════════════════════════════════════════════════
        //  4. LOGISTICS OVERVIEW
        // ════════════════════════════════════════════════════════════════

        /// <param name="statusFilter">null/"All"/"Pending"/"In Transit"/"Completed"</param>
        public ViewReportViewModel GetLogisticsReportVM(
            string   statusFilter = null,
            DateTime? from        = null,
            DateTime? to          = null)
        {
            return new ViewReportViewModel
            {
                UserBar      = MakeUserBar(),
                AllowedMenus = GetMenus(),
                ActiveReport = ReportType.LogisticsOverview,
                LogKpi       = _repo.GetLogisticsKpi(),
                LogRows      = _repo.GetLogisticsRows(statusFilter, from, to)
            };
        }

        // ════════════════════════════════════════════════════════════════
        //  5. AFTER-SERVICE SUMMARY
        // ════════════════════════════════════════════════════════════════

        /// <param name="complaintStatusFilter">null/"All"/"Pending"/"Processing"/"Escalated"/"Completed"</param>
        /// <param name="returnStatusFilter">null/"All"/"Pending"/"Processing"/"Completed"</param>
        public ViewReportViewModel GetAfterServiceReportVM(
            string   complaintStatusFilter = null,
            string   returnStatusFilter    = null,
            DateTime? from                 = null,
            DateTime? to                   = null)
        {
            return new ViewReportViewModel
            {
                UserBar      = MakeUserBar(),
                AllowedMenus = GetMenus(),
                ActiveReport = ReportType.AfterServiceSummary,
                AfterKpi     = _repo.GetAfterServiceKpi(),
                Complaints   = _repo.GetComplaintRows(complaintStatusFilter, from, to),
                Returns      = _repo.GetReturnOrderRows(returnStatusFilter, from, to)
            };
        }

        // ════════════════════════════════════════════════════════════════
        //  6. FINANCE OVERVIEW
        // ════════════════════════════════════════════════════════════════

        /// <param name="docTypeFilter">null/"All"/"Revenue"/"Expense"/"Refund"</param>
        public ViewReportViewModel GetFinanceReportVM(
            DateTime? from          = null,
            DateTime? to            = null,
            string    docTypeFilter = null)
        {
            return new ViewReportViewModel
            {
                UserBar      = MakeUserBar(),
                AllowedMenus = GetMenus(),
                ActiveReport = ReportType.FinanceOverview,
                FinanceKpi   = _repo.GetFinanceKpi(),
                FinanceRows  = _repo.GetFinanceTransactionRows(from, to, docTypeFilter)
            };
        }
    }
}
