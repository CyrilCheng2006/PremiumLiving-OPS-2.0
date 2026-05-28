using MySql.Data.MySqlClient;
using PremiumLivingOPS.Models.Entities;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.DAL
{
    /// <summary>
    /// Repository class for Staff table.
    /// Phase 1 — Step 2: DAL / Repository
    /// </summary>
    public class StaffRepo
    {
        // ── READ ─────────────────────────────────────────────────────

        /// <summary>
        /// Authenticates a staff member by StaffId and Password.
        /// Returns the Staff object on success, or null on failure.
        /// Used by UC-019 Login Account.
        /// </summary>
        public Staff Login(string staffId, string password)
        {
            Staff staff = null;

            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql = "SELECT StaffId, StaffName, Role, Department, Email, Status " +
                             "FROM Staff " +
                             "WHERE StaffId = @staffId AND Password = @password AND Status = 'Active'";

                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@staffId",  staffId);
                    cmd.Parameters.AddWithValue("@password", password);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            staff = new Staff();
                            staff.StaffId    = reader.GetString("StaffId");
                            staff.StaffName  = reader.GetString("StaffName");
                            staff.Role       = reader.GetString("Role");
                            staff.Department = reader.GetString("Department");
                            staff.Email      = reader.GetString("Email");
                            staff.Status     = reader.GetString("Status");
                        }
                    }
                }
            }

            return staff;
        }

        /// <summary>Returns all active staff members.</summary>
        public List<Staff> GetAll()
        {
            List<Staff> list = new List<Staff>();

            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql = "SELECT StaffId, StaffName, Role, Department, Email, Status " +
                             "FROM Staff ORDER BY StaffId";

                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Staff s = new Staff();
                        s.StaffId    = reader.GetString("StaffId");
                        s.StaffName  = reader.GetString("StaffName");
                        s.Role       = reader.GetString("Role");
                        s.Department = reader.GetString("Department");
                        s.Email      = reader.GetString("Email");
                        s.Status     = reader.GetString("Status");
                        list.Add(s);
                    }
                }
            }

            return list;
        }

        /// <summary>Returns a staff member by StaffId, or null if not found.</summary>
        public Staff GetById(string staffId)
        {
            Staff staff = null;

            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql = "SELECT StaffId, StaffName, Role, Department, Email, Status " +
                             "FROM Staff WHERE StaffId = @staffId";

                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@staffId", staffId);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            staff = new Staff();
                            staff.StaffId    = reader.GetString("StaffId");
                            staff.StaffName  = reader.GetString("StaffName");
                            staff.Role       = reader.GetString("Role");
                            staff.Department = reader.GetString("Department");
                            staff.Email      = reader.GetString("Email");
                            staff.Status     = reader.GetString("Status");
                        }
                    }
                }
            }

            return staff;
        }

        // ── CREATE ───────────────────────────────────────────────────

        /// <summary>Inserts a new Staff record. Returns true on success.</summary>
        public bool Add(Staff staff)
        {
            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql = "INSERT INTO Staff (StaffId, StaffName, Role, Department, Email, Password, Status) " +
                             "VALUES (@staffId, @staffName, @role, @department, @email, @password, @status)";

                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@staffId",    staff.StaffId);
                    cmd.Parameters.AddWithValue("@staffName",  staff.StaffName);
                    cmd.Parameters.AddWithValue("@role",       staff.Role);
                    cmd.Parameters.AddWithValue("@department", staff.Department);
                    cmd.Parameters.AddWithValue("@email",      staff.Email);
                    cmd.Parameters.AddWithValue("@password",   staff.Password);
                    cmd.Parameters.AddWithValue("@status",     staff.Status);

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        // ── UPDATE ───────────────────────────────────────────────────

        /// <summary>Updates an existing Staff record. Returns true on success.</summary>
        public bool Edit(Staff staff)
        {
            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql = "UPDATE Staff SET StaffName = @staffName, Role = @role, " +
                             "Department = @department, Email = @email, " +
                             "Password = @password, Status = @status " +
                             "WHERE StaffId = @staffId";

                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@staffId",    staff.StaffId);
                    cmd.Parameters.AddWithValue("@staffName",  staff.StaffName);
                    cmd.Parameters.AddWithValue("@role",       staff.Role);
                    cmd.Parameters.AddWithValue("@department", staff.Department);
                    cmd.Parameters.AddWithValue("@email",      staff.Email);
                    cmd.Parameters.AddWithValue("@password",   staff.Password);
                    cmd.Parameters.AddWithValue("@status",     staff.Status);

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        // ── DELETE ───────────────────────────────────────────────────

        /// <summary>Soft-deletes a Staff record by setting Status to Inactive.</summary>
        public bool Delete(string staffId)
        {
            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql = "UPDATE Staff SET Status = 'Inactive' WHERE StaffId = @staffId";

                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@staffId", staffId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
    }
}
