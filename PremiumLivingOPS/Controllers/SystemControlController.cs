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

        // ── Staff List ───────────────────────────────────────────────────────────

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

        /// <summary>
        /// Returns the next available StaffID in S-XXX format,
        /// filling the lowest unused number (no gaps left behind).
        /// </summary>
        public string GetNextStaffId()
            => _repo.GetNextStaffId();

        /// <summary>
        /// Inserts a new staff member.
        /// Returns true on success, false if the StaffID already exists or a DB error occurs.
        /// </summary>
        public bool AddStaff(Staff staff)
        {
            try   { return _repo.InsertStaff(staff); }
            catch { return false; }
        }

        /// <summary>
        /// Inserts a new staff member.
        /// Throws the underlying exception so the caller can display the real error message.
        /// </summary>
        public bool AddStaffWithException(Staff staff)
            => _repo.InsertStaff(staff);

        /// <summary>
        /// Updates the password for a staff member.
        /// Returns true on success.
        /// </summary>
        public bool ChangeStaffPassword(string staffId, string newPassword)
            => _repo.UpdateStaffPassword(staffId, newPassword);

        /// <summary>
        /// Updates the department for a staff member.
        /// Returns true on success.
        /// </summary>
        public bool ChangeStaffDepartment(string staffId, string newDepartment)
            => _repo.UpdateStaffDepartment(staffId, newDepartment);

        /// <summary>
        /// Updates the role (StaffRole) for a staff member.
        /// Returns true on success.
        /// </summary>
        public bool ChangeStaffRole(string staffId, string newRole)
            => _repo.UpdateStaffRole(staffId, newRole);

        // ── Log List ───────────────────────────────────────────────────────────

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
