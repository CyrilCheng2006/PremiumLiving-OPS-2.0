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

        public List<SupplierEntity> SearchSuppliers(string keyword = null)
        {
            var list = new List<SupplierEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var sql = @"SELECT SupplierID, SupplierName, PhoneNumber, SupplierAddress
                            FROM Supplier WHERE 1=1";
                if (!string.IsNullOrWhiteSpace(keyword))
                    sql += " AND (SupplierID LIKE @kw OR SupplierName LIKE @kw)";
                sql += " ORDER BY SupplierName ASC";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrWhiteSpace(keyword))
                        cmd.Parameters.AddWithValue("@kw", "%" + keyword.Trim() + "%");
                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read()) list.Add(MapSupplier(rdr));
                }
            }
            return list;
        }

        public List<SupplierEntity> GetAllSuppliers() => SearchSuppliers();

        /// <summary>
        /// Generates the next SupplierID in SUP-YYYYMMDD-XXX format.
        /// Sequence is scoped to the current date; fills the lowest unused slot.
        /// </summary>
        public string GetNextSupplierID()
        {
            string dateTag = DateTime.Today.ToString("yyyyMMdd");
            string prefix  = $"SUP-{dateTag}-";
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var used = new HashSet<int>();
                using (var cmd = new MySqlCommand(
                    "SELECT SupplierID FROM Supplier WHERE SupplierID LIKE @p ORDER BY SupplierID", conn))
                {
                    cmd.Parameters.AddWithValue("@p", prefix + "%");
                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                        {
                            string id  = rdr.GetString(0);
                            string seq = id.Length > prefix.Length ? id.Substring(prefix.Length) : "";
                            if (int.TryParse(seq, out int n)) used.Add(n);
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

        public bool UpdateSupplier(string supplierId, string name, string phone, string address)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    "UPDATE Supplier SET SupplierName=@name, PhoneNumber=@phone, SupplierAddress=@addr " +
                    "WHERE SupplierID=@id";
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
        //  CUSTOMER — READ
        // ════════════════════════════════════════════════════════════════

        public List<CustomerEntity> SearchCustomers(string keyword = null)
        {
            var list = new List<CustomerEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var sql = @"SELECT CustomerID, CustomerName, EmailAddress, PhoneNumber
                            FROM Customer WHERE 1=1";
                if (!string.IsNullOrWhiteSpace(keyword))
                    sql += " AND (CustomerID LIKE @kw OR CustomerName LIKE @kw OR EmailAddress LIKE @kw)";
                sql += " ORDER BY CustomerName ASC";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrWhiteSpace(keyword))
                        cmd.Parameters.AddWithValue("@kw", "%" + keyword.Trim() + "%");
                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read()) list.Add(MapCustomer(rdr));
                }
            }
            return list;
        }

        public List<CustomerEntity> GetAllCustomers() => SearchCustomers();

        /// <summary>
        /// Generates the next available CustomerID in C-XXXX format.
        /// Fills the lowest unused 4-digit sequence number.
        /// </summary>
        public string GetNextCustomerID()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    "SELECT CustomerID FROM Customer " +
                    "WHERE CustomerID REGEXP '^C-[0-9]+$' " +
                    "ORDER BY CAST(SUBSTRING(CustomerID, 3) AS UNSIGNED)";

                var used = new HashSet<int>();
                using (var cmd = new MySqlCommand(sql, conn))
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                    {
                        string id  = rdr.GetString(0);
                        string seq = id.Length > 2 ? id.Substring(2) : "";
                        if (int.TryParse(seq, out int n)) used.Add(n);
                    }

                int next = 1;
                while (used.Contains(next)) next++;
                return $"C-{next:D4}";
            }
        }

        /// <summary>
        /// Returns all Order header rows for the specified customer, newest-first.
        /// Columns: OrderID, IssuedTime, DeliveryDate, GrandTotal, OrderStatus.
        /// </summary>
        public List<OrderEntity> GetOrdersByCustomerID(string customerId)
        {
            var list = new List<OrderEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT o.OrderID, o.CustomerID, c.CustomerName,
                             o.OrderContactName, o.IssuedTime, o.DeliveryDate,
                             o.GrandTotal, o.OrderStatus
                      FROM `Order` o
                      JOIN Customer c ON c.CustomerID = o.CustomerID
                      WHERE o.CustomerID = @cid
                      ORDER BY o.IssuedTime DESC";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@cid", customerId);
                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                            list.Add(new OrderEntity
                            {
                                OrderID         = rdr["OrderID"]?.ToString(),
                                CustomerID      = rdr["CustomerID"]?.ToString(),
                                CustomerName    = rdr["CustomerName"]?.ToString(),
                                OrderContactName = rdr["OrderContactName"]?.ToString(),
                                IssuedTime      = rdr["IssuedTime"] != DBNull.Value
                                                    ? Convert.ToDateTime(rdr["IssuedTime"]) : DateTime.MinValue,
                                DeliveryDate    = rdr["DeliveryDate"] != DBNull.Value
                                                    ? (DateTime?)Convert.ToDateTime(rdr["DeliveryDate"]) : null,
                                GrandTotal      = rdr["GrandTotal"] != DBNull.Value
                                                    ? Convert.ToDouble(rdr["GrandTotal"]) : 0,
                                OrderStatus     = rdr["OrderStatus"]?.ToString()
                            });
                }
            }
            return list;
        }

        private static CustomerEntity MapCustomer(MySqlDataReader rdr) => new CustomerEntity
        {
            CustomerID   = rdr["CustomerID"]?.ToString(),
            CustomerName = rdr["CustomerName"]?.ToString(),
            EmailAddress = rdr["EmailAddress"]?.ToString(),
            PhoneNumber  = rdr["PhoneNumber"]?.ToString()
        };

        // ════════════════════════════════════════════════════════════════
        //  CUSTOMER — WRITE
        // ════════════════════════════════════════════════════════════════

        /// <summary>Inserts a new customer. Returns true if exactly one row inserted.</summary>
        public bool InsertCustomer(CustomerEntity c)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    "INSERT INTO Customer (CustomerID, CustomerName, EmailAddress, PhoneNumber) " +
                    "VALUES (@id, @name, @email, @phone)";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id",    c.CustomerID);
                    cmd.Parameters.AddWithValue("@name",  c.CustomerName);
                    cmd.Parameters.AddWithValue("@email", c.EmailAddress);
                    cmd.Parameters.AddWithValue("@phone", c.PhoneNumber);
                    return cmd.ExecuteNonQuery() == 1;
                }
            }
        }

        /// <summary>
        /// Updates an existing customer's name, email, and phone.
        /// Returns true if exactly one row updated.
        /// </summary>
        public bool UpdateCustomer(string customerId, string name, string email, string phone)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    "UPDATE Customer " +
                    "SET CustomerName=@name, EmailAddress=@email, PhoneNumber=@phone " +
                    "WHERE CustomerID=@id";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@name",  name);
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@phone", phone);
                    cmd.Parameters.AddWithValue("@id",    customerId);
                    return cmd.ExecuteNonQuery() == 1;
                }
            }
        }
    }
}
