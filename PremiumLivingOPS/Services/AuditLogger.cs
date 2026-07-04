using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;

namespace PremiumLivingOPS.Services
{
    /// <summary>
    /// Thread-safe audit logger.
    /// Writes every audit event directly to the MySQL 'Log' table.
    ///   LogType: LOGIN | LOGOUT | CREATE | EDIT | DELETE
    ///   TargetTable: e.g. "Staff", "Supplier", ...
    /// </summary>
    public static class AuditLogger
    {
        // ── Public operation-type constants ───────────────────────────────────────
        public const string TYPE_CREATE = "Create";
        public const string TYPE_EDIT   = "Edit";
        public const string TYPE_DELETE = "Delete";
        public const string TYPE_LOGIN  = "Login";
        public const string TYPE_LOGOUT = "Login"; // schema ENUM uses 'Login' for both; distinguish via NewValue

        private static readonly object _lock = new object();

        // ── Core write method ─────────────────────────────────────────────────────

        /// <summary>
        /// Inserts one audit row into the MySQL Log table.
        /// </summary>
        public static void Write(string logType, string targetTable,
                                 string oldValue, string newValue)
        {
            try
            {
                var    user     = SessionManager.CurrentUser;
                string staffId  = user?.StaffID ?? null;
                string old      = string.IsNullOrWhiteSpace(oldValue) ? null : oldValue;
                string @new     = string.IsNullOrWhiteSpace(newValue) ? null : newValue;

                const string sql = @"
                    INSERT INTO Log (LogID, StaffID, LogType, TargetTable, LogTimeStamp, OldValue, NewValue)
                    VALUES (@id, @staffId, @logType, @targetTable, @ts, @old, @new)";

                lock (_lock)
                {
                    using var conn = DatabaseHelper.GetConnection();
                    conn.Open();
                    using var cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@id",          Guid.NewGuid().ToString());
                    cmd.Parameters.AddWithValue("@staffId",     (object)staffId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@logType",     logType);
                    cmd.Parameters.AddWithValue("@targetTable", (object)targetTable ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ts",          DateTime.Now);
                    cmd.Parameters.AddWithValue("@old",         (object)old  ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@new",         (object)@new ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // Logging must never crash the application.
            }
        }

        /// <summary>
        /// Overload that accepts an explicit staffId (used before SessionManager is populated,
        /// e.g. immediately after login before SetUser is called).
        /// </summary>
        public static void WriteAs(string staffId, string logType, string targetTable,
                                   string oldValue, string newValue)
        {
            try
            {
                string old  = string.IsNullOrWhiteSpace(oldValue) ? null : oldValue;
                string @new = string.IsNullOrWhiteSpace(newValue) ? null : newValue;

                const string sql = @"
                    INSERT INTO Log (LogID, StaffID, LogType, TargetTable, LogTimeStamp, OldValue, NewValue)
                    VALUES (@id, @staffId, @logType, @targetTable, @ts, @old, @new)";

                lock (_lock)
                {
                    using var conn = DatabaseHelper.GetConnection();
                    conn.Open();
                    using var cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@id",          Guid.NewGuid().ToString());
                    cmd.Parameters.AddWithValue("@staffId",     (object)staffId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@logType",     logType);
                    cmd.Parameters.AddWithValue("@targetTable", (object)targetTable ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ts",          DateTime.Now);
                    cmd.Parameters.AddWithValue("@old",         (object)old  ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@new",         (object)@new ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        // ── Snapshot helper ───────────────────────────────────────────────────────

        /// <summary>
        /// Builds a compact, semicolon-separated field snapshot string.
        /// Example: Snapshot(("ID","S001"),("Name","Alice")) -> "ID=S001; Name=Alice"
        /// </summary>
        public static string Snapshot(params (string Field, string Value)[] fields)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var (f, v) in fields)
            {
                if (sb.Length > 0) sb.Append("; ");
                sb.Append(f).Append('=').Append(v ?? "(null)");
            }
            return sb.ToString();
        }

        // ── Load helpers (used by LogListForm / SystemControlRepo) ────────────────

        /// <summary>
        /// Loads log rows from MySQL Log table, joined with Staff for name,
        /// filtered by optional keyword.
        /// </summary>
        public static List<AuditLogEntity> LoadAllLogs(string keyword = null)
        {
            var result = new List<AuditLogEntity>();
            try
            {
                string kw = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();

                const string sql = @"
                    SELECT l.LogID, l.StaffID, s.StaffName, l.LogType,
                           l.TargetTable, l.LogTimeStamp, l.OldValue, l.NewValue
                    FROM   Log l
                    LEFT JOIN Staff s ON s.StaffID = l.StaffID
                    WHERE  (@kw IS NULL
                            OR l.StaffID     LIKE CONCAT('%',@kw,'%')
                            OR s.StaffName   LIKE CONCAT('%',@kw,'%')
                            OR l.LogType     LIKE CONCAT('%',@kw,'%')
                            OR l.TargetTable LIKE CONCAT('%',@kw,'%'))
                    ORDER  BY l.LogTimeStamp DESC";

                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@kw", (object)kw ?? DBNull.Value);
                using var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    result.Add(new AuditLogEntity
                    {
                        LogID       = rdr.IsDBNull(rdr.GetOrdinal("LogID"))       ? "" : rdr.GetString("LogID"),
                        StaffID     = rdr.IsDBNull(rdr.GetOrdinal("StaffID"))     ? "" : rdr.GetString("StaffID"),
                        StaffName   = rdr.IsDBNull(rdr.GetOrdinal("StaffName"))   ? "" : rdr.GetString("StaffName"),
                        LogType     = rdr.IsDBNull(rdr.GetOrdinal("LogType"))     ? "" : rdr.GetString("LogType"),
                        TargetTable = rdr.IsDBNull(rdr.GetOrdinal("TargetTable")) ? "" : rdr.GetString("TargetTable"),
                        Timestamp   = rdr.GetDateTime("LogTimeStamp"),
                        OldValue    = rdr.IsDBNull(rdr.GetOrdinal("OldValue"))    ? "" : rdr.GetString("OldValue"),
                        NewValue    = rdr.IsDBNull(rdr.GetOrdinal("NewValue"))    ? "" : rdr.GetString("NewValue"),
                        RawLine     = ""
                    });
                }
            }
            catch { }
            return result;
        }
    }
}
