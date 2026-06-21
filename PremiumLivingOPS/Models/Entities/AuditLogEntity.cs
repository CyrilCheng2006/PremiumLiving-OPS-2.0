using System;

namespace PremiumLivingOPS.Models.Entities
{
    /// <summary>
    /// In-memory representation of one parsed audit log line.
    /// Populated by AuditLogger.LoadAllLogs() and displayed in LogListForm.
    /// </summary>
    public class AuditLogEntity
    {
        public DateTime Timestamp   { get; set; }
        public string   LogType     { get; set; }   // CREATE | EDIT | DELETE | LOGIN
        public string   StaffID     { get; set; }
        public string   StaffName   { get; set; }
        public string   TargetTable { get; set; }
        public string   OldValue    { get; set; }
        public string   NewValue    { get; set; }
        public string   RawLine     { get; set; }   // original raw text for export
    }
}
