using PremiumLivingOPS.Models;
using MySql.Data.MySqlClient;
using PremiumLivingOPS.Views.Shared;
using System;
using System.IO;
using System.Text;

namespace PremiumLivingOPS.Services
{
    /// <summary>
    /// Thread-safe audit logging service.
    /// Writes every Add / Modify / Delete operation to:
    ///   1. MySQL Log table  (schema: LogID, StaffID, LogType, TargetTable, LogTimeStamp, OldValue, NewValue)
    ///   2. TXT file         (logs/audit_YYYY-MM-DD.txt — one line per entry)
    ///
    /// Usage (in any Controller after a successful DB operation):
    ///   AuditLogger.Write("Create", "Supplier", null, "S-0042 | Premium Co. | +852 1234 5678");
    ///   AuditLogger.Write("Edit",   "Customer", oldSnapshot, newSnapshot);
    ///   AuditLogger.Write("Delete", "Staff",    oldSnapshot, null);
    /// </summary>
    public static class AuditLogger
    {
        // ── Log folder: <AppBase>/logs/
        private static readonly string _logDir =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

        private static readonly object _fileLock = new object();

        // ── Log types matching schema ENUM('Login','Create','Edit','Delete')
        public const string TYPE_LOGIN  = "Login";
        public const string TYPE_CREATE = "Create";
        public const string TYPE_EDIT   = "Edit";
        public const string TYPE_DELETE = "Delete";

        /// <summary>
        /// Write one audit entry.  StaffID is read from SessionManager.CurrentStaffID.
        /// </summary>
        /// <param name="logType">"Create" | "Edit" | "Delete" | "Login"</param>
        /// <param name="targetTable">Affected table name, e.g. "Supplier"</param>
        /// <param name="oldValue">Snapshot before change (null for Create)</param>
        /// <param name="newValue">Snapshot after change  (null for Delete)</param>
        public static void Write(
            string logType,
            string targetTable,
            string oldValue = null,
            string newValue = null)
        {
            string logId    = Guid.NewGuid().ToString();
            string staffId  = SessionManager.CurrentStaffID ?? "SYSTEM";
            DateTime stamp  = DateTime.Now;

            // 1. Write to MySQL
            try { WriteToDb(logId, staffId, logType, targetTable, stamp, oldValue, newValue); }
            catch { /* DB failure must not crash the UI operation */ }

            // 2. Write to TXT
            try { WriteToFile(logId, staffId, logType, targetTable, stamp, oldValue, newValue); }
            catch { /* File failure must not crash the UI operation */ }
        }

        // ── MySQL insert ──────────────────────────────────────────────────────
        private static void WriteToDb(
            string logId, string staffId, string logType,
            string targetTable, DateTime stamp,
            string oldValue, string newValue)
        {
            using var conn = new MySqlConnection(DbConfig.ConnectionString);
            conn.Open();
            const string sql =
                @"INSERT INTO Log (LogID, StaffID, LogType, TargetTable, LogTimeStamp, OldValue, NewValue)
                  VALUES (@logId, @staffId, @logType, @targetTable, @stamp, @old, @new)";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@logId",       logId);
            cmd.Parameters.AddWithValue("@staffId",     staffId);
            cmd.Parameters.AddWithValue("@logType",     logType);
            cmd.Parameters.AddWithValue("@targetTable", targetTable);
            cmd.Parameters.AddWithValue("@stamp",       stamp);
            cmd.Parameters.AddWithValue("@old",         (object)oldValue ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@new",         (object)newValue ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        // ── TXT append ────────────────────────────────────────────────────────
        private static void WriteToFile(
            string logId, string staffId, string logType,
            string targetTable, DateTime stamp,
            string oldValue, string newValue)
        {
            if (!Directory.Exists(_logDir))
                Directory.CreateDirectory(_logDir);

            string fileName = Path.Combine(_logDir,
                $"audit_{stamp:yyyy-MM-dd}.txt");

            // Format:
            // [2026-06-22 04:39:00] [Create] Staff=S001 Table=Supplier
            //   NEW: S-0042 | Premium Co. | +852 1234 5678
            var sb = new StringBuilder();
            sb.AppendLine($"[{stamp:yyyy-MM-dd HH:mm:ss}] [{logType.ToUpper()}] " +
                          $"LogID={logId} Staff={staffId} Table={targetTable}");
            if (!string.IsNullOrWhiteSpace(oldValue))
                sb.AppendLine($"  OLD: {oldValue}");
            if (!string.IsNullOrWhiteSpace(newValue))
                sb.AppendLine($"  NEW: {newValue}");
            sb.AppendLine(new string('-', 80));

            lock (_fileLock)
                File.AppendAllText(fileName, sb.ToString(), Encoding.UTF8);
        }

        // ── Convenience: build a snapshot string from key=value pairs ─────────
        /// <summary>
        /// Build a human-readable snapshot string.
        /// Example: Snapshot(("Name","Premium Co."),("Phone","+852 1234 5678"))
        ///          => "Name=Premium Co. | Phone=+852 1234 5678"
        /// </summary>
        public static string Snapshot(params (string key, string value)[] fields)
        {
            var sb = new StringBuilder();
            foreach (var (k, v) in fields)
            {
                if (sb.Length > 0) sb.Append(" | ");
                sb.Append($"{k}={v ?? "null"}");
            }
            return sb.ToString();
        }
    }
}
