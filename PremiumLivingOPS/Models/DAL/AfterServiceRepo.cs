using MySql.Data.MySqlClient;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.DAL
{
    /// <summary>
    /// Repository (DAL layer) for After-Service module.
    /// All methods use parameterised queries via DatabaseHelper.GetConnection().
    /// Contains NO business logic — pure SQL access only.
    /// </summary>
    public class AfterServiceRepo
    {
        // ════════════════════════════════════════════════════════════════════════
        //  INVOICE queries
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>Returns all invoices, JOINed with Order and Customer.</summary>
        public List<InvoiceEntity> GetAllInvoices()
            => SearchInvoices();

        /// <summary>
        /// Returns invoices filtered by optional PaymentStatus and/or keyword
        /// (matches InvoiceID or CustomerName).
        /// </summary>
        public List<InvoiceEntity> SearchInvoices(string status = null, string keyword = null)
        {
            var list = new List<InvoiceEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var sql =
                    @"SELECT i.InvoiceID, i.OrderID, c.CustomerName,
                             i.InvoiceDate, i.DepositAmount, i.PaidAmount,
                             i.RemainingBalance, i.TotalAmount, i.PaymentStatus, i.DueDate
                      FROM Invoice i
                      JOIN `Order`   o ON i.OrderID    = o.OrderID
                      JOIN Customer  c ON o.CustomerID = c.CustomerID
                      WHERE 1=1";

                if (!string.IsNullOrEmpty(status))
                    sql += " AND i.PaymentStatus = @status";
                if (!string.IsNullOrEmpty(keyword))
                    sql += " AND (i.InvoiceID LIKE @kw OR c.CustomerName LIKE @kw)";
                sql += " ORDER BY i.InvoiceDate DESC";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(status))
                        cmd.Parameters.AddWithValue("@status", status);
                    if (!string.IsNullOrEmpty(keyword))
                        cmd.Parameters.AddWithValue("@kw", "%" + keyword + "%");

                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                            list.Add(MapInvoice(rdr));
                }
            }
            return list;
        }

        /// <summary>Inserts a new Invoice row. Returns true on success.</summary>
        public bool CreateInvoice(InvoiceEntity inv)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"INSERT INTO Invoice
                        (InvoiceID, OrderID, InvoiceDate, DepositAmount, PaidAmount,
                         RemainingBalance, TotalAmount, PaymentStatus, DueDate)
                      VALUES
                        (@invoiceId, @orderId, @invoiceDate, @depositAmount, @paidAmount,
                         @remainingBalance, @totalAmount, @paymentStatus, @dueDate)";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@invoiceId",        inv.InvoiceID);
                    cmd.Parameters.AddWithValue("@orderId",          inv.OrderID);
                    cmd.Parameters.AddWithValue("@invoiceDate",      inv.InvoiceDate.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@depositAmount",    inv.DepositAmount);
                    cmd.Parameters.AddWithValue("@paidAmount",       inv.PaidAmount);
                    cmd.Parameters.AddWithValue("@remainingBalance", inv.RemainingBalance);
                    cmd.Parameters.AddWithValue("@totalAmount",      inv.TotalAmount);
                    cmd.Parameters.AddWithValue("@paymentStatus",    inv.PaymentStatus);
                    cmd.Parameters.AddWithValue("@dueDate",          inv.DueDate.ToString("yyyy-MM-dd"));
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        /// <summary>
        /// Returns orders that have NO Invoice row yet (candidates for invoice creation).
        /// Columns: OrderID, CustomerName, GrandTotal.
        /// </summary>
        public List<OrderEntity> GetOrdersWithoutInvoice()
        {
            var list = new List<OrderEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT o.OrderID, c.CustomerName, o.GrandTotal, o.OrderStatus, o.IssuedTime
                      FROM `Order` o
                      JOIN Customer c ON o.CustomerID = c.CustomerID
                      LEFT JOIN Invoice i ON o.OrderID = i.OrderID
                      WHERE i.InvoiceID IS NULL
                      ORDER BY o.IssuedTime DESC";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        list.Add(new OrderEntity
                        {
                            OrderID      = rdr.GetString("OrderID"),
                            CustomerName = rdr.GetString("CustomerName"),
                            GrandTotal   = rdr.IsDBNull(rdr.GetOrdinal("GrandTotal"))   ? 0 : rdr.GetDouble("GrandTotal"),
                            OrderStatus  = rdr.IsDBNull(rdr.GetOrdinal("OrderStatus"))  ? "" : rdr.GetString("OrderStatus"),
                            IssuedTime   = rdr.IsDBNull(rdr.GetOrdinal("IssuedTime"))   ? DateTime.MinValue : rdr.GetDateTime("IssuedTime")
                        });
            }
            return list;
        }

        /// <summary>Returns a single Invoice by ID (with CustomerName from JOIN).</summary>
        public InvoiceEntity GetInvoiceById(string invoiceId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT i.InvoiceID, i.OrderID, c.CustomerName,
                             i.InvoiceDate, i.DepositAmount, i.PaidAmount,
                             i.RemainingBalance, i.TotalAmount, i.PaymentStatus, i.DueDate
                      FROM Invoice i
                      JOIN `Order`   o ON i.OrderID    = o.OrderID
                      JOIN Customer  c ON o.CustomerID = c.CustomerID
                      WHERE i.InvoiceID = @invoiceId";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@invoiceId", invoiceId);
                    using (var rdr = cmd.ExecuteReader())
                        if (rdr.Read()) return MapInvoice(rdr);
                }
            }
            return null;
        }

        /// <summary>
        /// Generates the next InvoiceID in format INV-YYYYMMDD-NNNN.
        /// Thread-safe within a single call (reads MAX from DB).
        /// </summary>
        public string GenerateInvoiceId()
        {
            string today  = DateTime.Today.ToString("yyyyMMdd");
            string prefix = $"INV-{today}-";
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var sql = "SELECT MAX(InvoiceID) FROM Invoice WHERE InvoiceID LIKE @prefix";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@prefix", prefix + "%");
                    var result = cmd.ExecuteScalar();
                    if (result == DBNull.Value || result == null)
                        return prefix + "0001";
                    string last = result.ToString();
                    if (int.TryParse(last.Substring(last.Length - 4), out int seq))
                        return prefix + (seq + 1).ToString("D4");
                    return prefix + "0001";
                }
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        //  COMPLAINT queries
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>Returns all complaints, JOINed with Order and Staff.</summary>
        public List<ComplaintEntity> GetAllComplaints()
            => SearchComplaints();

        /// <summary>
        /// Returns complaints filtered by optional ComplaintStatus and/or keyword
        /// (matches ComplaintID or OrderID).
        /// </summary>
        public List<ComplaintEntity> SearchComplaints(string status = null, string keyword = null)
        {
            var list = new List<ComplaintEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var sql =
                    @"SELECT c.ComplaintID, c.OrderID, s.StaffName,
                             c.ComplaintDescription, c.ComplaintStatus
                      FROM Complaint c
                      JOIN Staff s ON c.StaffID = s.StaffID
                      WHERE 1=1";

                if (!string.IsNullOrEmpty(status))
                    sql += " AND c.ComplaintStatus = @status";
                if (!string.IsNullOrEmpty(keyword))
                    sql += " AND (c.ComplaintID LIKE @kw OR c.OrderID LIKE @kw OR s.StaffName LIKE @kw)";
                sql += " ORDER BY c.ComplaintID DESC";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(status))
                        cmd.Parameters.AddWithValue("@status", status);
                    if (!string.IsNullOrEmpty(keyword))
                        cmd.Parameters.AddWithValue("@kw", "%" + keyword + "%");

                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                            list.Add(MapComplaint(rdr));
                }
            }
            return list;
        }

        /// <summary>Updates ComplaintStatus for the given ComplaintID. Returns true on success.</summary>
        public bool UpdateComplaintStatus(string complaintId, string newStatus)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    "UPDATE Complaint SET ComplaintStatus = @status WHERE ComplaintID = @id";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@status", newStatus);
                    cmd.Parameters.AddWithValue("@id",     complaintId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        //  RETURN ORDER queries
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>Returns all return orders, JOINed with Order and Customer.</summary>
        public List<ReturnOrderEntity> GetAllReturnOrders()
            => SearchReturnOrders();

        /// <summary>
        /// Returns return orders filtered by optional ReturnStatus and/or keyword
        /// (matches ReturnID, OrderID, or CustomerName).
        /// </summary>
        public List<ReturnOrderEntity> SearchReturnOrders(string status = null, string keyword = null)
        {
            var list = new List<ReturnOrderEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var sql =
                    @"SELECT r.ReturnID, r.OrderID, c.CustomerName,
                             r.ReturnDate, r.Reason, r.RefundAmount, r.ReturnStatus
                      FROM ReturnOrder r
                      JOIN `Order`   o ON r.OrderID    = o.OrderID
                      JOIN Customer  c ON o.CustomerID = c.CustomerID
                      WHERE 1=1";

                if (!string.IsNullOrEmpty(status))
                    sql += " AND r.ReturnStatus = @status";
                if (!string.IsNullOrEmpty(keyword))
                    sql += " AND (r.ReturnID LIKE @kw OR r.OrderID LIKE @kw OR c.CustomerName LIKE @kw)";
                sql += " ORDER BY r.ReturnDate DESC";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(status))
                        cmd.Parameters.AddWithValue("@status", status);
                    if (!string.IsNullOrEmpty(keyword))
                        cmd.Parameters.AddWithValue("@kw", "%" + keyword + "%");

                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                            list.Add(MapReturnOrder(rdr));
                }
            }
            return list;
        }

        /// <summary>Updates ReturnStatus for the given ReturnID. Returns true on success.</summary>
        public bool UpdateReturnOrderStatus(string returnId, string newStatus)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    "UPDATE ReturnOrder SET ReturnStatus = @status WHERE ReturnID = @id";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@status", newStatus);
                    cmd.Parameters.AddWithValue("@id",     returnId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        //  ACCOUNTS RECEIVABLE queries
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns Accounts Receivable items from Invoice JOIN Order+Customer.
        /// Optional status filter: "Partial" | "Full" | "Overdue".
        /// IsOverdue = RemainingBalance &gt; 0 AND DueDate &lt; TODAY.
        /// </summary>
        public List<AccountReceivableEntity> GetAccountReceivables(string status = null)
        {
            var list = new List<AccountReceivableEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var sql =
                    @"SELECT i.InvoiceID, i.OrderID, c.CustomerName,
                             i.TotalAmount, i.PaidAmount, i.RemainingBalance,
                             i.PaymentStatus, i.DueDate,
                             (i.RemainingBalance > 0 AND i.DueDate < CURDATE()) AS IsOverdue
                      FROM Invoice i
                      JOIN `Order`   o ON i.OrderID    = o.OrderID
                      JOIN Customer  c ON o.CustomerID = c.CustomerID
                      WHERE 1=1";

                if (status == "Overdue")
                    sql += " AND i.RemainingBalance > 0 AND i.DueDate < CURDATE()";
                else if (!string.IsNullOrEmpty(status))
                    sql += " AND i.PaymentStatus = @status";
                sql += " ORDER BY i.DueDate ASC";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(status) && status != "Overdue")
                        cmd.Parameters.AddWithValue("@status", status);

                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                        {
                            int overdueOrd = rdr.GetOrdinal("IsOverdue");
                            list.Add(new AccountReceivableEntity
                            {
                                InvoiceID        = rdr.GetString("InvoiceID"),
                                OrderID          = rdr.GetString("OrderID"),
                                CustomerName     = rdr.GetString("CustomerName"),
                                TotalAmount      = rdr.IsDBNull(rdr.GetOrdinal("TotalAmount"))      ? 0 : rdr.GetDouble("TotalAmount"),
                                PaidAmount       = rdr.IsDBNull(rdr.GetOrdinal("PaidAmount"))       ? 0 : rdr.GetDouble("PaidAmount"),
                                RemainingBalance = rdr.IsDBNull(rdr.GetOrdinal("RemainingBalance")) ? 0 : rdr.GetDouble("RemainingBalance"),
                                PaymentStatus    = rdr.IsDBNull(rdr.GetOrdinal("PaymentStatus"))    ? "" : rdr.GetString("PaymentStatus"),
                                DueDate          = rdr.IsDBNull(rdr.GetOrdinal("DueDate"))          ? DateTime.MinValue : rdr.GetDateTime("DueDate"),
                                IsOverdue        = !rdr.IsDBNull(overdueOrd) && rdr.GetBoolean(overdueOrd)
                            });
                        }
                }
            }
            return list;
        }

        // ════════════════════════════════════════════════════════════════════════
        //  ACCOUNTS PAYABLE queries
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns Accounts Payable items from PurchaseInvoice JOIN PurchaseOrder+Supplier.
        /// Optional status filter: "Partial" | "Full" | "Overdue".
        /// IsOverdue = PaymentStatus != 'Full' AND ExpectedDate &lt; TODAY.
        /// </summary>
        public List<AccountPayableEntity> GetAccountPayables(string status = null)
        {
            var list = new List<AccountPayableEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var sql =
                    @"SELECT pi.PurInvoiceID, pi.PurchaseID, s.SupplierName,
                             pi.TotalAmount, pi.PaymentStatus, pi.ExpectedDate,
                             (pi.PaymentStatus != 'Full' AND pi.ExpectedDate < CURDATE()) AS IsOverdue
                      FROM PurchaseInvoice pi
                      JOIN PurchaseOrder  po ON pi.PurchaseID = po.PurchaseID
                      JOIN Supplier        s ON po.SupplierID = s.SupplierID
                      WHERE 1=1";

                if (status == "Overdue")
                    sql += " AND pi.PaymentStatus != 'Full' AND pi.ExpectedDate < CURDATE()";
                else if (!string.IsNullOrEmpty(status))
                    sql += " AND pi.PaymentStatus = @status";
                sql += " ORDER BY pi.ExpectedDate ASC";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(status) && status != "Overdue")
                        cmd.Parameters.AddWithValue("@status", status);

                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                        {
                            int overdueOrd = rdr.GetOrdinal("IsOverdue");
                            list.Add(new AccountPayableEntity
                            {
                                PurInvoiceID  = rdr.GetString("PurInvoiceID"),
                                PurchaseID    = rdr.GetString("PurchaseID"),
                                SupplierName  = rdr.GetString("SupplierName"),
                                TotalAmount   = rdr.IsDBNull(rdr.GetOrdinal("TotalAmount"))   ? 0 : rdr.GetDouble("TotalAmount"),
                                PaymentStatus = rdr.IsDBNull(rdr.GetOrdinal("PaymentStatus")) ? "" : rdr.GetString("PaymentStatus"),
                                ExpectedDate  = rdr.IsDBNull(rdr.GetOrdinal("ExpectedDate"))  ? DateTime.MinValue : rdr.GetDateTime("ExpectedDate"),
                                IsOverdue     = !rdr.IsDBNull(overdueOrd) && rdr.GetBoolean(overdueOrd)
                            });
                        }
                }
            }
            return list;
        }

        // ════════════════════════════════════════════════════════════════════════
        //  Private mapping helpers
        // ════════════════════════════════════════════════════════════════════════

        private static InvoiceEntity MapInvoice(MySqlDataReader rdr)
            => new InvoiceEntity
            {
                InvoiceID        = rdr.GetString("InvoiceID"),
                OrderID          = rdr.GetString("OrderID"),
                CustomerName     = rdr.IsDBNull(rdr.GetOrdinal("CustomerName"))     ? "" : rdr.GetString("CustomerName"),
                InvoiceDate      = rdr.IsDBNull(rdr.GetOrdinal("InvoiceDate"))      ? DateTime.MinValue : rdr.GetDateTime("InvoiceDate"),
                DepositAmount    = rdr.IsDBNull(rdr.GetOrdinal("DepositAmount"))    ? 0 : rdr.GetDouble("DepositAmount"),
                PaidAmount       = rdr.IsDBNull(rdr.GetOrdinal("PaidAmount"))       ? 0 : rdr.GetDouble("PaidAmount"),
                RemainingBalance = rdr.IsDBNull(rdr.GetOrdinal("RemainingBalance")) ? 0 : rdr.GetDouble("RemainingBalance"),
                TotalAmount      = rdr.IsDBNull(rdr.GetOrdinal("TotalAmount"))      ? 0 : rdr.GetDouble("TotalAmount"),
                PaymentStatus    = rdr.IsDBNull(rdr.GetOrdinal("PaymentStatus"))    ? "" : rdr.GetString("PaymentStatus"),
                DueDate          = rdr.IsDBNull(rdr.GetOrdinal("DueDate"))          ? DateTime.MinValue : rdr.GetDateTime("DueDate")
            };

        private static ComplaintEntity MapComplaint(MySqlDataReader rdr)
            => new ComplaintEntity
            {
                ComplaintID          = rdr.GetString("ComplaintID"),
                OrderID              = rdr.IsDBNull(rdr.GetOrdinal("OrderID"))              ? "" : rdr.GetString("OrderID"),
                StaffName            = rdr.IsDBNull(rdr.GetOrdinal("StaffName"))            ? "" : rdr.GetString("StaffName"),
                ComplaintDescription = rdr.IsDBNull(rdr.GetOrdinal("ComplaintDescription")) ? "" : rdr.GetString("ComplaintDescription"),
                ComplaintStatus      = rdr.IsDBNull(rdr.GetOrdinal("ComplaintStatus"))      ? "" : rdr.GetString("ComplaintStatus")
            };

        private static ReturnOrderEntity MapReturnOrder(MySqlDataReader rdr)
            => new ReturnOrderEntity
            {
                ReturnID     = rdr.GetString("ReturnID"),
                OrderID      = rdr.IsDBNull(rdr.GetOrdinal("OrderID"))      ? "" : rdr.GetString("OrderID"),
                CustomerName = rdr.IsDBNull(rdr.GetOrdinal("CustomerName")) ? "" : rdr.GetString("CustomerName"),
                ReturnDate   = rdr.IsDBNull(rdr.GetOrdinal("ReturnDate"))   ? DateTime.MinValue : rdr.GetDateTime("ReturnDate"),
                Reason       = rdr.IsDBNull(rdr.GetOrdinal("Reason"))       ? "" : rdr.GetString("Reason"),
                RefundAmount = rdr.IsDBNull(rdr.GetOrdinal("RefundAmount")) ? 0 : rdr.GetDouble("RefundAmount"),
                ReturnStatus = rdr.IsDBNull(rdr.GetOrdinal("ReturnStatus")) ? "" : rdr.GetString("ReturnStatus")
            };
    }
}
