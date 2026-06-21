using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Services;
using System.Collections.Generic;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Controller (MVC middle layer) for System Control module.
    /// Covers: Staff List, Log List.
    /// All Add / Update / Delete staff operations are audit-logged.
    /// Contains NO UI code.
    /// </summary>
    public class SystemControlController
    {
        private readonly SystemControlRepo _repo = new SystemControlRepo();

        // ═ Log List ════════════════════════════════════════════════════════════
        public LogListViewModel GetLogListVM(string keyword = null)
        {
            var user = SessionManager.CurrentUser;
            return new LogListViewModel
            {
                UserBar = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                Logs = _repo.SearchLogs(keyword)
            };
        }

        // ═ Staff List ════════════════════════════════════════════════════════════
        public StaffListViewModel GetStaffListVM(string keyword = null)
        {
            var user = SessionManager.CurrentUser;
            return new StaffListViewModel
            {
                UserBar = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                Staffs = _repo.SearchStaff(keyword)
            };
        }

        public string GetNextStaffID() => _repo.GetNextStaffID();

        /// <summary>Add new staff and log the Create operation.</summary>
        public bool AddStaff(StaffEntity staff)
        {
            try
            {
                bool ok = _repo.InsertStaff(staff);
                if (ok)
                    AuditLogger.Write(
                        AuditLogger.TYPE_CREATE, "Staff",
                        oldValue: null,
                        newValue: AuditLogger.Snapshot(
                            ("ID",         staff.StaffID),
                            ("Name",       staff.StaffName),
                            ("Role",       staff.StaffRole),
                            ("Dept",       staff.Department),
                            ("Email",      staff.Email)));
                return ok;
            }
            catch { return false; }
        }

        /// <summary>Update staff and log the Edit operation.</summary>
        public bool UpdateStaff(string staffId, string name, string role,
                                string email, string department, string password = null)
        {
            var old = _repo.SearchStaff(staffId).Find(s => s.StaffID == staffId);
            string oldSnap = old == null ? null :
                AuditLogger.Snapshot(
                    ("ID", old.StaffID), ("Name", old.StaffName),
                    ("Role", old.StaffRole), ("Dept", old.Department), ("Email", old.Email));

            bool ok = _repo.UpdateStaff(staffId, name, role, email, department, password);
            if (ok)
                AuditLogger.Write(
                    AuditLogger.TYPE_EDIT, "Staff",
                    oldValue: oldSnap,
                    newValue: AuditLogger.Snapshot(
                        ("ID", staffId), ("Name", name),
                        ("Role", role), ("Dept", department), ("Email", email)));
            return ok;
        }

        /// <summary>Delete staff and log the Delete operation.</summary>
        public bool DeleteStaff(string staffId)
        {
            var target = _repo.SearchStaff(staffId).Find(s => s.StaffID == staffId);
            string oldSnap = target == null ? staffId :
                AuditLogger.Snapshot(
                    ("ID", target.StaffID), ("Name", target.StaffName),
                    ("Role", target.StaffRole), ("Dept", target.Department), ("Email", target.Email));

            bool ok = _repo.DeleteStaff(staffId);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_DELETE, "Staff", oldValue: oldSnap, newValue: null);
            return ok;
        }
    }
}
