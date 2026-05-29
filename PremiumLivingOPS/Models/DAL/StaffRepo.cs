using MySql.Data.MySqlClient;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.Helpers;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.DAL
{
    /// <summary>
    /// Repository class for Staff table.
    /// SQL columns aligned with schema.sql:
    ///   StaffID, StaffName, StaffRole, Department, Email, StaffPassword
    ///
    /// Password policy:
    ///   Passwords are stored as PBKDF2-HMACSHA256 hashes via PasswordHelper.
    ///   The Login() method supports a one-time migration path:
    ///   if the stored value is still plain-text it is verified directly and
    ///   immediately re-hashed, so existing accounts migrate transparently.
    /// </summary>
    public class StaffRepo
    {
        // ── Helper: map a reader row to a Staff object ────────────────
        private Staff MapRow(MySqlDataReader r)
        {
            Staff s      = new Staff();
            s.StaffId    = r.GetString("StaffID");
            s.StaffName  = r.GetString("StaffName");
            s.Role       = r.GetString("StaffRole");
            s.Department = r.GetString("Department");
            s.Email      = r.GetString("Email");
            s.Password   = r.GetString("StaffPassword");
            return s;
        }

        // ── LOGIN ────────────────────────────────────────────────────

        /// <summary>
        /// Authenticates a staff member.
        /// 1. Fetch the stored hash by StaffID only (never compare in SQL).
        /// 2. Verify the plain-text password against the stored hash.
        /// 3. Migration path: if the stored value is still plain-text,
        ///    accept it on this first login and re-hash it transparently.
        /// Returns the Staff object on success, or null on failure.
        /// </summary>
        public Staff Login(string staffId, string plainPassword)
        {
            Staff staff = null;

            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                // Step 1: fetch by StaffID only
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

                if (staff == null) return null;  // StaffID not found

                string stored = staff.Password;

                // Step 2: verify password
                bool passwordOk;

                if (PasswordHelper.IsHashed(stored))
                {
                    // Normal path — verify against stored hash
                    passwordOk = PasswordHelper.Verify(plainPassword, stored);
                }
                else
                {
                    // Migration path — stored value is still plain-text
                    passwordOk = (plainPassword == stored);

                    if (passwordOk)
                    {
                        // Re-hash transparently on first successful login
                        string newHash = PasswordHelper.Hash(plainPassword);
                        UpdatePasswordHash(staffId, newHash, conn);
                        staff.Password = newHash;
                    }
                }

                return passwordOk ? staff : null;
            }
        }

        /// <summary>Updates only the StaffPassword column (used during migration).</summary>
        private void UpdatePasswordHash(string staffId, string hash, MySqlConnection conn)
        {
            string sql = "UPDATE Staff SET StaffPassword = @hash WHERE StaffID = @staffId";
            using (MySqlCommand cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@hash",    hash);
                cmd.Parameters.AddWithValue("@staffId", staffId);
                cmd.ExecuteNonQuery();
            }
        }

        // ── READ ─────────────────────────────────────────────────────

        /// <summary>Returns all staff members.</summary>
        public System.Collections.Generic.List<Staff> GetAll()
        {
            var list = new System.Collections.Generic.List<Staff>();

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
                        if (reader.Read()) staff = MapRow(reader);
                }
            }
            return staff;
        }

        // ── CREATE ───────────────────────────────────────────────────

        /// <summary>
        /// Inserts a new Staff record.
        /// The password in staff.Password is expected to be plain-text;
        /// it is hashed before being stored in the database.
        /// </summary>
        public bool Add(Staff staff)
        {
            string hashedPassword = PasswordHelper.Hash(staff.Password);

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
                    cmd.Parameters.AddWithValue("@password",   hashedPassword);  // store hash
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        // ── UPDATE ───────────────────────────────────────────────────

        /// <summary>
        /// Updates an existing Staff record.
        /// If staff.Password is plain-text (not yet hashed), it is hashed first.
        /// </summary>
        public bool Edit(Staff staff)
        {
            // Hash the password only if it is not already hashed
            string passwordToStore = PasswordHelper.IsHashed(staff.Password)
                ? staff.Password
                : PasswordHelper.Hash(staff.Password);

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
                    cmd.Parameters.AddWithValue("@password",   passwordToStore);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        // ── DELETE ───────────────────────────────────────────────────

        /// <summary>Hard-deletes a Staff record.</summary>
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
