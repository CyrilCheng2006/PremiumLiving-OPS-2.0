using System.Collections.Generic;

namespace PremiumLivingOPS.Models.Entities
{
    // ── Log List ──────────────────────────────────────────────────────────────────
    public class LogListViewModel
    {
        public UserBarViewModel     UserBar      { get; set; }
        public List<string>         AllowedMenus { get; set; }
        public List<AuditLogEntity> Logs         { get; set; } = new List<AuditLogEntity>();
    }

    // ── Staff List ────────────────────────────────────────────────────────────────
    public class StaffListViewModel
    {
        public UserBarViewModel    UserBar      { get; set; }
        public List<string>        AllowedMenus { get; set; }
        /// <summary>
        /// Uses StaffEntity (SystemControl MVC stack), which maps to the DB Staff table
        /// with columns StaffID, StaffName, StaffRole, Department, Email, Password.
        /// </summary>
        public List<StaffEntity>   Staffs       { get; set; } = new List<StaffEntity>();
    }
}
