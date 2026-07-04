using System;

namespace PremiumLivingOPS.Models.Entities
{
    /// <summary>
    /// Represents one row from the MySQL Log table (or a parsed TXT line).
    /// </summary>
    public class AuditLogEntity
    {
        public string   LogID       { get; set; }  // UUID primary key
        public string   StaffID     { get; set; }
        public string   StaffName   { get; set; }  // joined from Staff table
        public string   LogType     { get; set; }  // Login | Create | Edit | Delete
        public string   TargetTable { get; set; }
        public DateTime Timestamp   { get; set; }
        public string   OldValue    { get; set; }
        public string   NewValue    { get; set; }
        public string   RawLine     { get; set; }  // kept for backward compat (empty for DB rows)
    }
}
