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
        public UserBarViewModel UserBar      { get; set; }
        public List<string>     AllowedMenus { get; set; }
        /// <summary>Uses the canonical Staff entity (Staff.cs).</summary>
        public List<Staff>      Staffs       { get; set; } = new List<Staff>();
    }
}
