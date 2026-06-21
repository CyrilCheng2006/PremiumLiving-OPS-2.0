using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;

namespace PremiumLivingOPS.Services
{
    /// <summary>
    /// Thread-safe audit logger.
    /// Writes to TWO append-only TXT files simultaneously:
    ///   1. Daily  : ./Logs/audit_YYYY-MM-DD.txt   (one per calendar day)
    ///   2. Master : ./Logs/audit_master.txt        (all-time, never rotated)
    ///
    /// Line format:
    ///   [2026-06-22 04:30:00] [CREATE] [S001|Alice] [Supplier] | OLD: - | NEW: ID=SUP010; Name=ABC Ltd
    ///
    /// Every Add / Modify / Delete on any database table must call AuditLogger.Write()
    /// through its owning Controller so the Log List page always shows a complete picture.
    /// </summary>
    public static class AuditLogger
    {
        // ── Public operation-type constants ───────────────────────────────────────
        public const string TYPE_CREATE = "CREATE";
        public const string TYPE_EDIT   = "EDIT";
        public const string TYPE_DELETE = "DELETE";
        public const string TYPE_LOGIN  = "LOGIN";

        private static readonly object _lock = new object();
        private static readonly string _logDir;
        private static readonly string _masterPath;

        static AuditLogger()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _logDir    = Path.Combine(baseDir, "Logs");
            _masterPath = Path.Combine(_logDir, "audit_master.txt");
            Directory.CreateDirectory(_logDir);

            // Write a session-start separator into master log on each application launch
            try
            {
                lock (_lock)
                {
                    string sep = $"{Environment.NewLine}{'=',0}".PadRight(1);
                    string header =
                        Environment.NewLine +
                        "================================================================" + Environment.NewLine +
                        $"  SESSION STARTED  {DateTime.Now:yyyy-MM-dd HH:mm:ss}" + Environment.NewLine +
                        "================================================================";
                    File.AppendAllText(_masterPath, header + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch { /* must never crash */ }
        }

        // ── Core write method ─────────────────────────────────────────────────────

        /// <summary>
        /// Appends one audit line to BOTH today's daily log and the master log.
        /// </summary>
        /// <param name="logType">TYPE_CREATE | TYPE_EDIT | TYPE_DELETE | TYPE_LOGIN</param>
        /// <param name="targetTable">e.g. "Supplier", "Customer", "SalesOrder"</param>
        /// <param name="oldValue">Snapshot before-state (null for Create/Login)</param>
        /// <param name="newValue">Snapshot after-state  (null for Delete)</param>
        public static void Write(string logType, string targetTable,
                                 string oldValue, string newValue)
        {
            try
            {
                var    user      = SessionManager.CurrentUser;
                string staffTag  = user != null
                                   ? $"{user.StaffID}|{user.StaffName}"
                                   : "SYSTEM";
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string old       = string.IsNullOrWhiteSpace(oldValue) ? "-" : oldValue;
                string @new      = string.IsNullOrWhiteSpace(newValue) ? "-" : newValue;

                string line = $"[{timestamp}] [{logType}] [{staffTag}] [{targetTable}] | OLD: {old} | NEW: {@new}";

                string dailyPath  = Path.Combine(_logDir, $"audit_{DateTime.Today:yyyy-MM-dd}.txt");

                lock (_lock)
                {
                    // Write to daily log
                    File.AppendAllText(dailyPath,   line + Environment.NewLine, Encoding.UTF8);
                    // Write to master log (all-time, never deleted)
                    File.AppendAllText(_masterPath, line + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch
            {
                // Logging must never crash the application.
            }
        }

        // ── Snapshot helper ───────────────────────────────────────────────────────

        /// <summary>
        /// Builds a compact semicolon-separated snapshot string.
        /// Example: Snapshot(("ID","S001"),("Name","Alice")) -> "ID=S001; Name=Alice"
        /// </summary>
        public static string Snapshot(params (string Field, string Value)[] fields)
        {
            var sb = new StringBuilder();
            foreach (var (f, v) in fields)
            {
                if (sb.Length > 0) sb.Append("; ");
                sb.Append(f).Append('=').Append(v ?? "(null)");
            }
            return sb.ToString();
        }

        // ── Convenience wrappers used by Repo classes ─────────────────────────────

        /// <summary>Logs a CREATE operation with a pre-built new-value snapshot.</summary>
        public static void LogCreate(string table, string newSnap)
            => Write(TYPE_CREATE, table, null, newSnap);

        /// <summary>Logs an EDIT operation with pre-built old/new snapshots.</summary>
        public static void LogEdit(string table, string oldSnap, string newSnap)
            => Write(TYPE_EDIT, table, oldSnap, newSnap);

        /// <summary>Logs a DELETE operation with a pre-built old-value snapshot.</summary>
        public static void LogDelete(string table, string oldSnap)
            => Write(TYPE_DELETE, table, oldSnap, null);

        // ── Load helpers (used by LogListForm / SystemControlRepo) ────────────────

        /// <summary>
        /// Loads and parses every audit_*.txt file in ./Logs/ (daily files only),
        /// filtered by an optional keyword.  Results are sorted newest-first.
        /// </summary>
        public static List<AuditLogEntity> LoadAllLogs(string keyword = null)
        {
            var result = new List<AuditLogEntity>();
            if (!Directory.Exists(_logDir)) return result;

            string kw = keyword?.ToLowerInvariant();

            foreach (string file in Directory.GetFiles(_logDir, "audit_????-??-??.txt"))
            {
                string[] lines;
                lock (_lock) { lines = File.ReadAllLines(file, Encoding.UTF8); }

                foreach (string raw in lines)
                {
                    var entity = ParseLine(raw);
                    if (entity == null) continue;
                    if (!string.IsNullOrEmpty(kw) &&
                        !raw.ToLowerInvariant().Contains(kw)) continue;
                    result.Add(entity);
                }
            }

            result.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
            return result;
        }

        /// <summary>Returns the absolute path of the master log file.</summary>
        public static string MasterLogPath => _masterPath;

        /// <summary>Returns the absolute path of today's daily log file.</summary>
        public static string TodayLogPath  => Path.Combine(_logDir, $"audit_{DateTime.Today:yyyy-MM-dd}.txt");

        /// <summary>Returns the Logs directory path.</summary>
        public static string LogDirectory  => _logDir;

        // ── Private parser ────────────────────────────────────────────────────────

        // Expected format:
        // [2026-06-22 04:30:00] [CREATE] [S001|Alice] [Supplier] | OLD: - | NEW: ID=SUP010; Name=ABC
        private static AuditLogEntity ParseLine(string raw)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(raw)) return null;
                if (raw.TrimStart().StartsWith("=") || raw.TrimStart().StartsWith("SESSION")) return null;

                int p0 = raw.IndexOf('[');
                int p1 = raw.IndexOf(']', p0 + 1);
                int p2 = raw.IndexOf('[', p1 + 1);
                int p3 = raw.IndexOf(']', p2 + 1);
                int p4 = raw.IndexOf('[', p3 + 1);
                int p5 = raw.IndexOf(']', p4 + 1);
                int p6 = raw.IndexOf('[', p5 + 1);
                int p7 = raw.IndexOf(']', p6 + 1);

                if (p0 < 0 || p7 < 0) return null;

                string ts          = raw.Substring(p0 + 1, p1 - p0 - 1).Trim();
                string logType     = raw.Substring(p2 + 1, p3 - p2 - 1).Trim();
                string staffTag    = raw.Substring(p4 + 1, p5 - p4 - 1).Trim();
                string targetTable = raw.Substring(p6 + 1, p7 - p6 - 1).Trim();

                string remainder = raw.Substring(p7 + 1);
                string oldVal = ""; string newVal = "";
                int oidx = remainder.IndexOf("OLD:", StringComparison.Ordinal);
                int nidx = remainder.IndexOf("NEW:", StringComparison.Ordinal);
                if (oidx >= 0 && nidx > oidx)
                {
                    oldVal = remainder.Substring(oidx + 4, nidx - oidx - 4).Trim().TrimEnd('|').Trim();
                    newVal = remainder.Substring(nidx + 4).Trim();
                }

                string staffId = staffTag; string staffName = "";
                int pipe = staffTag.IndexOf('|');
                if (pipe >= 0) { staffId = staffTag.Substring(0, pipe); staffName = staffTag.Substring(pipe + 1); }

                return new AuditLogEntity
                {
                    Timestamp   = DateTime.TryParse(ts, out var dt) ? dt : DateTime.MinValue,
                    LogType     = logType,
                    StaffID     = staffId,
                    StaffName   = staffName,
                    TargetTable = targetTable,
                    OldValue    = oldVal == "-" ? "" : oldVal,
                    NewValue    = newVal == "-" ? "" : newVal,
                    RawLine     = raw
                };
            }
            catch { return null; }
        }
    }
}
