using MySql.Data.MySqlClient;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.DAL
{
    /// <summary>
    /// Repository (DAL layer) for Master Data Maintenance module.
    /// Covers Supplier and Customer tables.
    /// All queries are parameterised via DatabaseHelper.
    /// </summary>
    public class MasterDataRepo
    {
        // ════════════════════════════════════════════════════════════════
        //  SUPPLIER — READ
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns all suppliers, optionally filtered by a keyword that matches
        /// SupplierID or SupplierName (case-insensitive LIKE).
        /// </summary>
        public List<SupplierEntity> SearchSuppliers(string keyword = null)
        {
            var list = new List<SupplierEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var sql = @"SELECT SupplierID, SupplierName, PhoneNumber, SupplierAddress
                            FROM Supplier
                            WHERE 1=1";

                if (!string.IsNullOrWhiteSpace(keyword))
                    sql += @" AND (SupplierID   LIKE @kw
                               OR  SupplierName LIKE @kw)";

                sql += " ORDER BY SupplierName ASC";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrWhiteSpace(keyword))
                        cmd.Parameters.AddWithValue("@kw", "%" + keyword.Trim() + "%");

                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                            list.Add(MapSupplier(rdr));
                }
            }
            return list;
        }

        /// <summary>Returns all suppliers (no filter).</summary>
        public List<SupplierEntity> GetAllSuppliers() => SearchSuppliers();

        /// <summary>
        /// Generates the next available SupplierID in SUP-YYYYMMDD-XXX format.
        /// XXX is a zero-padded 3-digit sequence number scoped to the current date.
        /// Finds the lowest unused sequence number for today.
        /// </summary>
        public string GetNextSupplierID()
        {
            string dateTag = DateTime.Today.ToString("yyyyMMdd");
            string prefix  = $"SUP-{dateTag}-";

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                // Fetch all IDs that match today's prefix
                string sql =
                    "SELECT SupplierID FROM Supplier " +
                    "WHERE SupplierID LIKE @prefix " +
                    "ORDER BY SupplierID ASC";

                var used = new HashSet<int>();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@prefix", prefix + "%");
                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                        {
                            string id  = rdr.GetString(0);
                            string seq = id.Length > prefix.Length
                                ? id.Substring(prefix.Length)
                                : string.Empty;
                            if (int.TryParse(seq, out int n))
                                used.Add(n);
                        }
                }

                int next = 1;
                while (used.Contains(next)) next++;
                return $"{prefix}{next:D3}";
            }
        }

        private static SupplierEntity MapSupplier(MySqlDataReader rdr) => new SupplierEntity
        {
            SupplierID      = rdr["SupplierID"]?.ToString(),
            SupplierName    = rdr["SupplierName"]?.ToString(),
            PhoneNumber     = rdr["PhoneNumber"]?.ToString(),
            SupplierAddress = rdr["SupplierAddress"]?.ToString()
        };

        // ════════════════════════════════════════════════════════════════
        //  SUPPLIER — WRITE
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Inserts a new supplier. Returns true if exactly one row inserted.
        /// </summary>
        public bool InsertSupplier(SupplierEntity s)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    "INSERT INTO Supplier (SupplierID, SupplierName, PhoneNumber, SupplierAddress) " +
                    "VALUES (@id, @name, @phone, @addr)";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id",    s.SupplierID);
                    cmd.Parameters.AddWithValue("@name",  s.SupplierName);
                    cmd.Parameters.AddWithValue("@phone", s.PhoneNumber);
                    cmd.Parameters.AddWithValue("@addr",  s.SupplierAddress);
                    return cmd.ExecuteNonQuery() == 1;
                }
            }
        }

        /// <summary>
        /// Updates an existing supplier's name, phone, and address.
        /// Returns true if exactly one row updated.
        /// </summary>
        public bool UpdateSupplier(string supplierId, string name, string phone, string address)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    "UPDATE Supplier " +
                    "SET SupplierName = @name, PhoneNumber = @phone, SupplierAddress = @addr " +
                    "WHERE SupplierID = @id";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@name",  name);
                    cmd.Parameters.AddWithValue("@phone", phone);
                    cmd.Parameters.AddWithValue("@addr",  address);
                    cmd.Parameters.AddWithValue("@id",    supplierId);
                    return cmd.ExecuteNonQuery() == 1;
                }
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  CUSTOMER
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns all customers, optionally filtered by a keyword that matches
        /// CustomerID, CustomerName, or EmailAddress (case-insensitive LIKE).
        /// </summary>
        public List<CustomerEntity> SearchCustomers(string keyword = null)
        {
            var list = new List<CustomerEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var sql = @"SELECT CustomerID, CustomerName, EmailAddress, PhoneNumber
                            FROM Customer
                            WHERE 1=1";

                if (!string.IsNullOrWhiteSpace(keyword))
                    sql += @" AND (CustomerID   LIKE @kw
                               OR  CustomerName LIKE @kw
                               OR  EmailAddress LIKE @kw)";

                sql += " ORDER BY CustomerName ASC";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrWhiteSpace(keyword))
                        cmd.Parameters.AddWithValue("@kw", "%" + keyword.Trim() + "%");

                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                            list.Add(MapCustomer(rdr));
                }
            }
            return list;
        }

        /// <summary>Returns all customers (no filter).</summary>
        public List<CustomerEntity> GetAllCustomers() => SearchCustomers();

        private static CustomerEntity MapCustomer(MySqlDataReader rdr) => new CustomerEntity
        {
            CustomerID   = rdr["CustomerID"]?.ToString(),
            CustomerName = rdr["CustomerName"]?.ToString(),
            EmailAddress = rdr["EmailAddress"]?.ToString(),
            PhoneNumber  = rdr["PhoneNumber"]?.ToString()
        };
    }
}
