using MySql.Data.MySqlClient;

namespace PremiumLivingOPS.Models.DAL
{
    /// <summary>
    /// Manages MySQL database connection for the entire application.
    /// Phase 1 — Step 2: MySQL Connection Manager
    /// </summary>
    public static class DatabaseHelper
    {
        private static string host     = "127.0.0.1";
        private static string port     = "3306";
        private static string database = "premiumliving_ops";
        private static string userId   = "root";
        private static string password = "";

        /// <summary>
        /// Returns a new (unopened) MySqlConnection using the configured credentials.
        /// </summary>
        public static MySqlConnection GetConnection()
        {
            string connStr = $"Server={host};Port={port};Database={database};" +
                             $"User ID={userId};Password={password};Pooling=true;";
            return new MySqlConnection(connStr);
        }

        /// <summary>
        /// Tests connectivity to the database.
        /// Returns true if connection opens successfully.
        /// </summary>
        public static bool TestConnection()
        {
            try
            {
                using (MySqlConnection conn = GetConnection())
                {
                    conn.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
