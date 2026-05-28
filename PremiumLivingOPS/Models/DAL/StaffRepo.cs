using MySql.Data.MySqlClient;
using PremiumLivingOPS.Models.Entities;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.DAL
{
    /// <summary>
    /// Repository class for Staff table.
    /// SQL columns aligned with schema.sql:
    ///   StaffID, StaffName, StaffRole, Department, Email, StaffPassword
    /// Note: schema.sql has NO Status column.
    /// </summary>
    public class StaffRepo
    {
        // ── Helper: map a reader row to a Staff object ────────────────
        private Staff MapRow(MySqlDataReader r)
        {
            Staff s        = new Staff();
            s.StaffId      = r.GetString("StaffID");
            s.StaffName    = r.GetString("StaffName");
            s.Role         = r.GetString("StaffRole");      // DB col: StaffRole
            s.Department   = r.GetString("Department");
            s.Email        = r.GetString("Email");
            s.Password     = r.GetString("StaffPassword");  // DB col: StaffPassword
            return s;
        }

        // ── READ ─────────────────────────────────────────────────────

        /// <summary>
        /// Authenticates a staff member by StaffID and StaffPassword.
        /// Returns the Staff object on success, or null on failure.
        /// Used by UC-019 Login Account.
        /// </summary>
        public Staff Login(string staffId, string password)
        {
            Staff staff = null;

            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql = "SELECT StaffID, StaffName, StaffRole, Department, Email, StaffPassword " +
                             "FROM Staff " +
                             "WHERE StaffID = @staffId AND StaffPassword = @password";

                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@staffId",  staffId);
                    cmd.Parameters.AddWithValue("@password", password);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            staff = MapRow(reader);
                    }
                }
            }

            return staff;
        }

        /// <summary>Returns all staff members.</summary>
        public List<Staff> GetAll()
        {
            List<Staff> list = new List<Staff>();

            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql = "SELECT StaffID, StaffName, StaffRole, Department, Email, StaffPassword " +
                             "FROM Staff ORDER BY StaffID";

                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        list.Add(MapRow(reader));
                }
            }

            return list;
        }

        /// <summary>Returns a staff member by StaffID, or null if not found.</summary>
        public Staff GetById(string staffId)
        {
            Staff staff = null;

            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql = "SELECT StaffID, StaffName, StaffRole, Department, Email, StaffPassword " +
                             "FROM Staff WHERE StaffID = @staffId";

                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@staffId", staffId);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            staff = MapRow(reader);
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
                string sql = "INSERT INTO Staff (StaffID, StaffName, StaffRole, Department, Email, StaffPassword) " +
                             "VALUES (@staffId, @staffName, @role, @department, @email, @password)";

                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@staffId",    staff.StaffId);
                    cmd.Parameters.AddWithValue("@staffName",  staff.StaffName);
                    cmd.Parameters.AddWithValue("@role",       staff.Role);
                    cmd.Parameters.AddWithValue("@department", staff.Department);
                    cmd.Parameters.AddWithValue("@email",      staff.Email);
                    cmd.Parameters.AddWithValue("@password",   staff.Password);

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
                string sql = "UPDATE Staff " +
                             "SET StaffName = @staffName, StaffRole = @role, " +
                             "    Department = @department, Email = @email, " +
                             "    StaffPassword = @password " +
                             "WHERE StaffID = @staffId";

                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@staffId",    staff.StaffId);
                    cmd.Parameters.AddWithValue("@staffName",  staff.StaffName);
                    cmd.Parameters.AddWithValue("@role",       staff.Role);
                    cmd.Parameters.AddWithValue("@department", staff.Department);
                    cmd.Parameters.AddWithValue("@email",      staff.Email);
                    cmd.Parameters.AddWithValue("@password",   staff.Password);

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        // ── DELETE ───────────────────────────────────────────────────

        /// <summary>
        /// Hard-deletes a Staff record.
        /// Note: schema.sql has no Status column, so soft-delete is not supported.
        /// </summary>
        public bool Delete(string staffId)
        {
            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql = "DELETE FROM Staff WHERE StaffID = @staffId";

                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@staffId", staffId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
    }
}
