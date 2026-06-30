using System;
using System.Collections.Generic;
using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;

namespace PremiumLivingOPS.Controllers
{
    public class StatisticalReportsController
    {
        private readonly StatisticalReportsRepo _repo = new StatisticalReportsRepo();

        // ─── Shared helpers ───────────────────────────────────────────────────

        private ViewReportViewModel BaseVM()
        {
            return new ViewReportViewModel
            {
                UserBar      = new UserBarEntity  { DisplayName = "Admin", Department = "Management" },
                AllowedMenus = new List<string>   { "Dashboard", "Sales", "Procurement", "Logistics", "Inventory", "Staff", "After-Service", "Finance", "Statistical Reports" }
            };
        }

        // ─── 1. Sales ─────────────────────────────────────────────────────────

        public ViewReportViewModel GetSalesReportVM(DateTime? from = null, DateTime? to = null)
        {
            var vm = BaseVM();
            vm.SalesKpi     = _repo.GetSalesKpi(from, to);
            vm.SalesRows    = _repo.GetSalesRows(from, to);
            vm.TopProducts  = _repo.GetTopProducts(from, to);
            return vm;
        }

        // ─── 2. Inventory ─────────────────────────────────────────────────────

        public ViewReportViewModel GetInventoryReportVM()
        {
            var vm = BaseVM();
            vm.InventoryKpi  = _repo.GetInventoryKpi();
            vm.InventoryRows = _repo.GetInventoryRows();
            return vm;
        }

        // ─── 3. Procurement ───────────────────────────────────────────────────

        public ViewReportViewModel GetProcurementReportVM(
            DateTime? from         = null,
            DateTime? to           = null,
            string    statusFilter = null)
        {
            var vm = BaseVM();
            vm.ProcKpi         = _repo.GetProcurementKpi(from, to);
            vm.ProcurementRows = _repo.GetProcurementRows(from, to, statusFilter);
            return vm;
        }

        // ─── 4. Logistics ─────────────────────────────────────────────────────

        public ViewReportViewModel GetLogisticsReportVM(
            DateTime? from         = null,
            DateTime? to           = null,
            string    statusFilter = null)
        {
            var vm = BaseVM();
            vm.LogKpi         = _repo.GetLogisticsKpi(from, to);
            vm.LogisticsRows  = _repo.GetLogisticsRows(from, to, statusFilter);
            return vm;
        }

        // ─── 5. After-Service ─────────────────────────────────────────────────

        public ViewReportViewModel GetAfterServiceReportVM(
            DateTime? from              = null,
            DateTime? to                = null,
            string    complaintFilter   = null)
        {
            var vm = BaseVM();
            vm.AfterKpi      = _repo.GetAfterServiceKpi(from, to);
            vm.ComplaintRows = _repo.GetComplaintRows(from, to, complaintFilter);
            vm.ReturnRows    = _repo.GetReturnOrderRows(from, to);
            return vm;
        }

        // ─── 6. Finance ───────────────────────────────────────────────────────

        public ViewReportViewModel GetFinanceReportVM(
            DateTime? from         = null,
            DateTime? to           = null,
            string    typeFilter   = null)
        {
            var vm = BaseVM();
            vm.FinanceKpi  = _repo.GetFinanceKpi(from, to);
            vm.FinanceRows = _repo.GetFinanceRows(from, to, typeFilter);
            return vm;
        }
    }
}
