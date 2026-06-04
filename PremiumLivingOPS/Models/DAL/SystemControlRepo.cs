using MySql.Data.MySqlClient;
using PremiumLivingOPS.Models.Entities;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.DAL
{
    /// <summary>
    /// Repository for the System Control module.
    /// Covers: Staff table and Log table (schema.sql).
    /// </summary>
    public class SystemControlRepo
    {
        // ═══════════════════════════════════════════════════════════════
        //  STAFF
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns all staff members, ordered by StaffID.
        /// Supports optional keyword search on StaffID, StaffName, StaffRole,
        /// Department, or Email.
        /// </summary>
        public List<Staff> SearchStaff(string keyword = null)
        {
            var list = new List<Staff>();

            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string sql;
                MySqlCommand cmd;

                if (string.IsNullOrWhiteSpace(keyword))
                {
                    sql = "SELECT StaffID, StaffName, StaffRole, Department, Email, StaffPassword " +
                          "FROM Staff ORDER BY StaffID";
                    cmd = new MySqlCommand(sql, conn);
                }
                else
                {
                    sql = "SELECT StaffID, StaffName, StaffRole, Department, Email, StaffPassword " +
                          "FROM Staff " +
                          "WHERE StaffID    LIKE @kw " +
                          "   OR StaffName  LIKE @kw " +
                          "   OR StaffRole  LIKE @kw " +
                          "   OR Department LIKE @kw " +
                          "   OR Email      LIKE @kw " +
                          "ORDER BY StaffID";
                    cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@kw", $"%{keyword}%");
                }

                using (cmd)
                using (MySqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new Staff
                        {
                            StaffId    = r.GetString("StaffID"),
                            StaffName  = r.GetString("StaffName"),
                            Role       = r.GetString("StaffRole"),
                            Department = r.GetString("Department"),
                            Email      = r.GetString("Email"),
                            Password   = r.GetString("StaffPassword")
                        });
                    }
                }
            }
            return list;
        }

        // ═══════════════════════════════════════════════════════════════
        //  LOG
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns all log entries, newest first.
        /// Supports optional keyword search on StaffID, LogType, or TargetTable.
        /// </summary>
        public List<LogEntry> SearchLogs(string keyword = null)
        {
            var list = new List<LogEntry>();

            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string sql;
                MySqlCommand cmd;

                if (string.IsNullOrWhiteSpace(keyword))
                {
                    sql = "SELECT LogID, StaffID, LogType, TargetTable, " +
                          "       DATE_FORMAT(LogTimeStamp,'%Y-%m-%d %H:%i:%s') AS LogTimeStamp, " +
                          "       OldValue, NewValue " +
                          "FROM Log ORDER BY LogTimeStamp DESC";
                    cmd = new MySqlCommand(sql, conn);
                }
                else
                {
                    sql = "SELECT LogID, StaffID, LogType, TargetTable, " +
                          "       DATE_FORMAT(LogTimeStamp,'%Y-%m-%d %H:%i:%s') AS LogTimeStamp, " +
                          "       OldValue, NewValue " +
                          "FROM Log " +
                          "WHERE StaffID     LIKE @kw " +
                          "   OR LogType     LIKE @kw " +
                          "   OR TargetTable LIKE @kw " +
                          "ORDER BY LogTimeStamp DESC";
                    cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@kw", $"%{keyword}%");
                }

                using (cmd)
                using (MySqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new LogEntry
                        {
                            LogId       = r.GetString("LogID"),
                            StaffId     = r.IsDBNull(r.GetOrdinal("StaffID"))     ? "—" : r.GetString("StaffID"),
                            LogType     = r.GetString("LogType"),
                            TargetTable = r.IsDBNull(r.GetOrdinal("TargetTable")) ? "—" : r.GetString("TargetTable"),
                            TimeStamp   = r.GetString("LogTimeStamp"),
                            OldValue    = r.IsDBNull(r.GetOrdinal("OldValue"))    ? "" : r.GetString("OldValue"),
                            NewValue    = r.IsDBNull(r.GetOrdinal("NewValue"))    ? "" : r.GetString("NewValue")
                        });
                    }
                }
            }
            return list;
        }
    }
}
