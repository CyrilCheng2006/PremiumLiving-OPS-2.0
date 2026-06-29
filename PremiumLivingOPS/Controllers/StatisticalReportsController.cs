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
            string categoryFilter   = null,
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

        // Signature: (from, to, statusFilter) — matches ViewReportForm.cs call order
        /// <param name="statusFilter">null/"All"/"Sent"/"Partially Received"/"Received"/"Completed"/"Cancelled"</param>
        public ViewReportViewModel GetProcurementReportVM(
            DateTime? from        = null,
            DateTime? to          = null,
            string    statusFilter = null)
        {
            return new ViewReportViewModel
            {
                UserBar         = MakeUserBar(),
                AllowedMenus    = GetMenus(),
                ActiveReport    = ReportType.ProcurementSummary,
                ProcKpi         = _repo.GetProcurementKpi(),
                ProcurementRows = _repo.GetProcurementRows(statusFilter, from, to)
            };
        }

        // ════════════════════════════════════════════════════════════════
        //  4. LOGISTICS OVERVIEW
        // ════════════════════════════════════════════════════════════════

        // Signature: (from, to, statusFilter) — matches ViewReportForm.cs call order
        /// <param name="statusFilter">null/"All"/"Pending"/"In Transit"/"Delivered"/"Partially Delivered"/"Cancelled"</param>
        public ViewReportViewModel GetLogisticsReportVM(
            DateTime? from        = null,
            DateTime? to          = null,
            string    statusFilter = null)
        {
            return new ViewReportViewModel
            {
                UserBar       = MakeUserBar(),
                AllowedMenus  = GetMenus(),
                ActiveReport  = ReportType.LogisticsOverview,
                LogKpi        = _repo.GetLogisticsKpi(),
                LogisticsRows = _repo.GetLogisticsRows(statusFilter, from, to)
            };
        }

        // ════════════════════════════════════════════════════════════════
        //  5. AFTER-SERVICE SUMMARY
        // ════════════════════════════════════════════════════════════════

        // Signature: (from, to, complaintStatusFilter) — matches ViewReportForm.cs call order
        /// <param name="complaintStatusFilter">null/"All"/"Open"/"In Progress"/"Resolved"/"Escalated"/"Closed"</param>
        public ViewReportViewModel GetAfterServiceReportVM(
            DateTime? from                 = null,
            DateTime? to                   = null,
            string    complaintStatusFilter = null)
        {
            return new ViewReportViewModel
            {
                UserBar       = MakeUserBar(),
                AllowedMenus  = GetMenus(),
                ActiveReport  = ReportType.AfterServiceSummary,
                AfterKpi      = _repo.GetAfterServiceKpi(),
                ComplaintRows = _repo.GetComplaintRows(complaintStatusFilter, from, to),
                ReturnRows    = _repo.GetReturnOrderRows(null, from, to)
            };
        }

        // ════════════════════════════════════════════════════════════════
        //  6. FINANCE OVERVIEW
        // ════════════════════════════════════════════════════════════════

        // Signature: (from, to, docTypeFilter) — matches ViewReportForm.cs call order
        /// <param name="docTypeFilter">null/"All"/"Revenue"/"Expense"/"Refund"/"Deposit"/"Installment"/"Full"</param>
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
