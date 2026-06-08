using MySql.Data.MySqlClient;
using PremiumLivingOPS.Models.Entities;
using System;
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
        //  STAFF — READ
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

        /// <summary>
        /// Generates the next available StaffID in the format S-XXX.
        /// Finds all existing IDs matching S-\d+, picks the lowest unused number,
        /// and returns it zero-padded to 3 digits (e.g. S-011).
        /// If no matching IDs exist, returns S-001.
        /// 
        /// FIX: use Substring(3) to skip the full "S-" prefix (indices 0-1 = 'S','-')
        ///      so "S-007" → "007" → 7, not "-007" → -7 (negative) as Substring(2) produced.
        /// </summary>
        public string GetNextStaffId()
        {
            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    "SELECT StaffID FROM Staff " +
                    "WHERE StaffID REGEXP '^S-[0-9]+$' " +
                    "ORDER BY CAST(SUBSTRING(StaffID, 3) AS UNSIGNED)";

                var usedNumbers = new HashSet<int>();
                using (var cmd = new MySqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string id = reader.GetString(0);           // e.g. "S-007"
                        // Substring(3): skip 'S'(0), '-'(1), '0'... → "007"
                        if (int.TryParse(id.Substring(3), out int n))
                            usedNumbers.Add(n);
                    }
                }

                int next = 1;
                while (usedNumbers.Contains(next))
                    next++;

                return $"S-{next:D3}";
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  STAFF — WRITE
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Inserts a new staff member into the Staff table.
        /// StaffPassword defaults to "changeme" if not supplied.
        /// Returns true if exactly one row was inserted.
        /// </summary>
        public bool InsertStaff(Staff staff)
        {
            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    "INSERT INTO Staff (StaffID, StaffName, StaffRole, Department, Email, StaffPassword) " +
                    "VALUES (@id, @name, @role, @dept, @email, @pwd)";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id",   staff.StaffId);
                    cmd.Parameters.AddWithValue("@name", staff.StaffName);
                    cmd.Parameters.AddWithValue("@role", staff.Role);
                    cmd.Parameters.AddWithValue("@dept", staff.Department);
                    cmd.Parameters.AddWithValue("@email",staff.Email);
                    cmd.Parameters.AddWithValue("@pwd",
                        string.IsNullOrWhiteSpace(staff.Password) ? "changeme" : staff.Password);
                    return cmd.ExecuteNonQuery() == 1;
                }
            }
        }

        /// <summary>
        /// Updates the StaffPassword for the given staffId.
        /// Returns true if exactly one row was affected.
        /// </summary>
        public bool UpdateStaffPassword(string staffId, string newPassword)
        {
            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    "UPDATE Staff SET StaffPassword = @pwd WHERE StaffID = @id";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@pwd", newPassword);
                    cmd.Parameters.AddWithValue("@id",  staffId);
                    return cmd.ExecuteNonQuery() == 1;
                }
            }
        }

        /// <summary>
        /// Updates the Department for the given staffId.
        /// Returns true if exactly one row was affected.
        /// </summary>
        public bool UpdateStaffDepartment(string staffId, string newDepartment)
        {
            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    "UPDATE Staff SET Department = @dept WHERE StaffID = @id";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@dept", newDepartment);
                    cmd.Parameters.AddWithValue("@id",   staffId);
                    return cmd.ExecuteNonQuery() == 1;
                }
            }
        }

        /// <summary>
        /// Updates the StaffRole for the given staffId.
        /// Returns true if exactly one row was affected.
        /// </summary>
        public bool UpdateStaffRole(string staffId, string newRole)
        {
            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    "UPDATE Staff SET StaffRole = @role WHERE StaffID = @id";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@role", newRole);
                    cmd.Parameters.AddWithValue("@id",   staffId);
                    return cmd.ExecuteNonQuery() == 1;
                }
            }
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
