using MySql.Data.MySqlClient;
using PremiumLivingOPS.Models.Entities;
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
        //  SUPPLIER
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

        private static SupplierEntity MapSupplier(MySqlDataReader rdr) => new SupplierEntity
        {
            SupplierID      = rdr["SupplierID"]?.ToString(),
            SupplierName    = rdr["SupplierName"]?.ToString(),
            PhoneNumber     = rdr["PhoneNumber"]?.ToString(),
            SupplierAddress = rdr["SupplierAddress"]?.ToString()
        };

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
