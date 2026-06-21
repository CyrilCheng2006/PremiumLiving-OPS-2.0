using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Services;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Controller (MVC middle layer) for System Control module.
    /// Covers: Staff List, Log List.
    /// All Add / Update / Delete staff operations are audit-logged.
    /// Contains NO UI code. Uses the canonical Staff entity (Staff.cs).
    /// AllowedMenus is string[] to match NavAccessPolicy.GetAllowedMenus() return type.
    /// </summary>
    public class SystemControlController
    {
        private readonly SystemControlRepo _repo = new SystemControlRepo();

        // ═ Log List ═══════════════════════════════════════════════════════════════
        public LogListViewModel GetLogListVM(string keyword = null)
        {
            var user = SessionManager.CurrentUser;
            return new LogListViewModel
            {
                UserBar      = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                Logs         = _repo.SearchLogs(keyword)
            };
        }

        // ═ Staff List ══════════════════════════════════════════════════════════════
        public StaffListViewModel GetStaffListVM(string keyword = null)
        {
            var user = SessionManager.CurrentUser;
            return new StaffListViewModel
            {
                UserBar      = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                Staffs       = _repo.SearchStaff(keyword)
            };
        }

        /// <summary>Returns the next auto-generated StaffID (e.g. S004).</summary>
        public string GetNextStaffId() => _repo.GetNextStaffId();

        /// <summary>Add new staff and log the Create operation.</summary>
        public bool AddStaff(Staff staff)
        {
            bool ok = _repo.InsertStaff(staff);
            if (ok)
                AuditLogger.Write(
                    AuditLogger.TYPE_CREATE, "Staff",
                    oldValue: null,
                    newValue: AuditLogger.Snapshot(
                        ("ID",   staff.StaffID),
                        ("Name", staff.StaffName),
                        ("Role", staff.Role),
                        ("Dept", staff.Department),
                        ("Email",staff.Email)));
            return ok;
        }

        /// <summary>
        /// AddStaffWithException — called by StaffListForm which expects exceptions to bubble up.
        /// </summary>
        public bool AddStaffWithException(Staff staff) => AddStaff(staff);

        /// <summary>Update staff password and log the Edit operation.</summary>
        public bool ChangeStaffPassword(string staffId, string newPassword)
        {
            var list = _repo.SearchStaff(staffId);
            var old  = list.Find(s => s.StaffID == staffId);
            string oldSnap = old == null ? staffId
                : AuditLogger.Snapshot(("ID", old.StaffID), ("Name", old.StaffName), ("Role", old.Role));

            bool ok = _repo.UpdateStaff(staffId, old?.StaffName ?? "", old?.Role ?? "",
                                        old?.Email ?? "", old?.Department ?? "", newPassword);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_EDIT, "Staff",
                    oldValue: oldSnap,
                    newValue: AuditLogger.Snapshot(("ID", staffId), ("PasswordChanged", "true")));
            return ok;
        }

        /// <summary>Update staff role and log the Edit operation.</summary>
        public bool ChangeStaffRole(string staffId, string newRole)
        {
            var list = _repo.SearchStaff(staffId);
            var old  = list.Find(s => s.StaffID == staffId);
            string oldSnap = old == null ? staffId
                : AuditLogger.Snapshot(("ID", old.StaffID), ("Role", old.Role));

            bool ok = _repo.UpdateStaff(staffId, old?.StaffName ?? "", newRole,
                                        old?.Email ?? "", old?.Department ?? "");
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_EDIT, "Staff",
                    oldValue: oldSnap,
                    newValue: AuditLogger.Snapshot(("ID", staffId), ("Role", newRole)));
            return ok;
        }

        /// <summary>Update staff department and log the Edit operation.</summary>
        public bool ChangeStaffDepartment(string staffId, string newDept)
        {
            var list = _repo.SearchStaff(staffId);
            var old  = list.Find(s => s.StaffID == staffId);
            string oldSnap = old == null ? staffId
                : AuditLogger.Snapshot(("ID", old.StaffID), ("Dept", old.Department));

            bool ok = _repo.UpdateStaff(staffId, old?.StaffName ?? "", old?.Role ?? "",
                                        old?.Email ?? "", newDept);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_EDIT, "Staff",
                    oldValue: oldSnap,
                    newValue: AuditLogger.Snapshot(("ID", staffId), ("Dept", newDept)));
            return ok;
        }

        /// <summary>Delete staff and log the Delete operation.</summary>
        public bool DeleteStaff(string staffId)
        {
            var list   = _repo.SearchStaff(staffId);
            var target = list.Find(s => s.StaffID == staffId);
            string oldSnap = target == null ? staffId
                : AuditLogger.Snapshot(
                    ("ID",   target.StaffID),
                    ("Name", target.StaffName),
                    ("Role", target.Role),
                    ("Dept", target.Department),
                    ("Email",target.Email));

            bool ok = _repo.DeleteStaff(staffId);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_DELETE, "Staff", oldValue: oldSnap, newValue: null);
            return ok;
        }
    }
}
