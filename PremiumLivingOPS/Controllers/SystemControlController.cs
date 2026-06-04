using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Controller (MVC middle layer) for the System Control module.
    /// Handles Staff List and Log List pages.
    /// Contains NO UI code — returns ViewModels to the View.
    /// </summary>
    public class SystemControlController
    {
        private readonly SystemControlRepo _repo = new SystemControlRepo();

        // ── Staff List ────────────────────────────────────────────────

        /// <summary>
        /// Returns ViewModel for the Staff List page.
        /// Supports optional keyword search (StaffID, StaffName, Role, Department, Email).
        /// </summary>
        public StaffListViewModel GetStaffListVM(string keyword = null)
        {
            var user = SessionManager.CurrentUser;
            return new StaffListViewModel
            {
                UserBar = new UserBarViewModel
                {
                    DisplayName = user?.StaffName ?? "Unknown",
                    Department  = user?.Department ?? ""
                },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                Staffs       = _repo.SearchStaff(keyword)
            };
        }

        // ── Log List ──────────────────────────────────────────────────

        /// <summary>
        /// Returns ViewModel for the Log List page.
        /// Supports optional keyword search (StaffID, LogType, TargetTable).
        /// </summary>
        public LogListViewModel GetLogListVM(string keyword = null)
        {
            var user = SessionManager.CurrentUser;
            return new LogListViewModel
            {
                UserBar = new UserBarViewModel
                {
                    DisplayName = user?.StaffName ?? "Unknown",
                    Department  = user?.Department ?? ""
                },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                Logs         = _repo.SearchLogs(keyword)
            };
        }
    }
}
