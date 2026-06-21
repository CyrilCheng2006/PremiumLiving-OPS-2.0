using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Services;

namespace PremiumLivingOPS.Models.DAL
{
    /// <summary>
    /// Data Access Layer for System Control module.
    /// Staff CRUD -> MySQL 'Staff' table (via DatabaseHelper.GetConnection).
    /// Log search  -> AuditLogger.LoadAllLogs() (reads TXT files, no DB needed).
    /// Uses the canonical Staff entity (Staff.cs).
    /// </summary>
    public class SystemControlRepo
    {
        // Matches pattern used across the project (DatabaseHelper.GetConnection)
        private MySqlConnection GetConn() => DatabaseHelper.GetConnection();

        // ════════════════════════════════════════════════════════════════
        // STAFF  (StaffID, StaffName, StaffRole, Department, Email, StaffPassword)
        // ════════════════════════════════════════════════════════════════

        public List<Staff> SearchStaff(string keyword = null)
        {
            var list = new List<Staff>();
            const string sql = @"
                SELECT StaffID, StaffName, StaffRole, Department, Email, StaffPassword
                FROM   Staff
                WHERE  (@kw IS NULL OR @kw = ''
                        OR StaffID     LIKE CONCAT('%',@kw,'%')
                        OR StaffName   LIKE CONCAT('%',@kw,'%')
                        OR Department  LIKE CONCAT('%',@kw,'%'))
                ORDER  BY StaffID";

            using var conn = GetConn(); conn.Open();
            using var cmd  = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@kw", keyword ?? "");
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
                list.Add(new Staff
                {
                    StaffID    = rdr.GetString("StaffID"),
                    StaffName  = rdr.GetString("StaffName"),
                    Role       = rdr.GetString("StaffRole"),
                    Department = rdr.GetString("Department"),
                    Email      = rdr.IsDBNull(rdr.GetOrdinal("Email")) ? "" : rdr.GetString("Email"),
                    Password   = rdr.IsDBNull(rdr.GetOrdinal("StaffPassword")) ? "" : rdr.GetString("StaffPassword")
                });
            return list;
        }

        public string GetNextStaffId()
        {
            const string sql = "SELECT MAX(CAST(SUBSTRING(StaffID,2) AS UNSIGNED)) FROM Staff WHERE StaffID LIKE 'S%'";
            using var conn = GetConn(); conn.Open();
            using var cmd  = new MySqlCommand(sql, conn);
            var val  = cmd.ExecuteScalar();
            int next = (val == DBNull.Value || val == null) ? 1 : Convert.ToInt32(val) + 1;
            return $"S{next:D3}";
        }

        public bool InsertStaff(Staff s)
        {
            const string sql = @"
                INSERT INTO Staff (StaffID, StaffName, StaffRole, Department, Email, StaffPassword)
                VALUES (@id, @name, @role, @dept, @email, @pwd)";
            using var conn = GetConn(); conn.Open();
            using var cmd  = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id",   s.StaffID);
            cmd.Parameters.AddWithValue("@name", s.StaffName);
            cmd.Parameters.AddWithValue("@role", s.Role);
            cmd.Parameters.AddWithValue("@dept", s.Department);
            cmd.Parameters.AddWithValue("@email",s.Email   ?? "");
            cmd.Parameters.AddWithValue("@pwd",  s.Password ?? "");
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool UpdateStaff(string staffId, string name, string role,
                                string email, string dept, string password = null)
        {
            string sql = password == null
                ? "UPDATE Staff SET StaffName=@name, StaffRole=@role, Email=@email, Department=@dept WHERE StaffID=@id"
                : "UPDATE Staff SET StaffName=@name, StaffRole=@role, Email=@email, Department=@dept, StaffPassword=@pwd WHERE StaffID=@id";
            using var conn = GetConn(); conn.Open();
            using var cmd  = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id",   staffId);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@role", role);
            cmd.Parameters.AddWithValue("@email",email ?? "");
            cmd.Parameters.AddWithValue("@dept", dept);
            if (password != null) cmd.Parameters.AddWithValue("@pwd", password);
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool DeleteStaff(string staffId)
        {
            const string sql = "DELETE FROM Staff WHERE StaffID=@id";
            using var conn = GetConn(); conn.Open();
            using var cmd  = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", staffId);
            return cmd.ExecuteNonQuery() > 0;
        }

        // ════════════════════════════════════════════════════════════════
        // LOG  (reads TXT files via AuditLogger -- no MySQL table needed)
        // ════════════════════════════════════════════════════════════════

        public List<AuditLogEntity> SearchLogs(string keyword = null)
            => AuditLogger.LoadAllLogs(keyword);
    }
}
