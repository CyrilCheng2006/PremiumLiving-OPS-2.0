using System.Collections.Generic;

namespace PremiumLivingOPS.Models.Entities
{
    // ── Staff List ─────────────────────────────────────────────────────────────

    /// <summary>
    /// ViewModel for the Staff List page (System Control module).
    /// </summary>
    public class StaffListViewModel
    {
        public UserBarViewModel UserBar      { get; set; }
        public string[]         AllowedMenus { get; set; }
        public List<Staff>      Staffs       { get; set; } = new List<Staff>();
    }

    // ── Log List ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Represents a single row from the Log table (schema.sql).
    /// Columns: LogID, StaffID, LogType, TargetTable, LogTimeStamp, OldValue, NewValue
    /// </summary>
    public class LogEntry
    {
        public string LogId       { get; set; }
        public string StaffId     { get; set; }
        public string LogType     { get; set; }   // ENUM: Login | Create | Edit | Delete
        public string TargetTable { get; set; }
        public string TimeStamp   { get; set; }
        public string OldValue    { get; set; }
        public string NewValue    { get; set; }
    }

    /// <summary>
    /// ViewModel for the Log List page (System Control module).
    /// </summary>
    public class LogListViewModel
    {
        public UserBarViewModel UserBar      { get; set; }
        public string[]         AllowedMenus { get; set; }
        public List<LogEntry>   Logs         { get; set; } = new List<LogEntry>();
    }
}
