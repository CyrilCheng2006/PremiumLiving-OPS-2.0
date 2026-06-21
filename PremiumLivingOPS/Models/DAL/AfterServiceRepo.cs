using MySql.Data.MySqlClient;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.DAL
{
    /// <summary>
    /// Repository (DAL layer) for the After-Service module.
    /// All methods use parameterised queries via DatabaseHelper.
    /// Contains NO business logic and NO UI code.
    /// </summary>
    public class AfterServiceRepo
    {
        // ══════════════════════════════════════════════════════════════════
        //  INVOICE queries
        // ══════════════════════════════════════════════════════════════════

        /// <summary>Returns all invoices, with optional status / keyword filter.</summary>
        public List<InvoiceEntity> SearchInvoices(
            string status  = null,
            string keyword = null)
        {
            var list = new List<InvoiceEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var sql =
                    @"SELECT i.InvoiceID, i.OrderID, c.CustomerName,
                             i.InvoiceDate, i.DepositAmount, i.PaidAmount,
                             i.RemainingBalance, i.TotalAmount,
                             i.PaymentStatus, i.DueDate
                      FROM Invoice i
                      JOIN `Order`  o ON i.OrderID    = o.OrderID
                      JOIN Customer c ON o.CustomerID = c.CustomerID
                      WHERE 1=1";

                if (!string.IsNullOrEmpty(status))
                    sql += " AND i.PaymentStatus = @status";
                if (!string.IsNullOrEmpty(keyword))
                    sql += @" AND (i.InvoiceID    LIKE @kw
                               OR c.CustomerName  LIKE @kw
                               OR i.OrderID       LIKE @kw)";

                sql += " ORDER BY i.InvoiceDate DESC";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(status))  cmd.Parameters.AddWithValue("@status", status);
                    if (!string.IsNullOrEmpty(keyword)) cmd.Parameters.AddWithValue("@kw", "%" + keyword + "%");

                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read()) list.Add(MapInvoice(rdr));
                }
            }
            return list;
        }

        /// <summary>Returns all invoices (no filter).</summary>
        public List<InvoiceEntity> GetAllInvoices() => SearchInvoices();

        /// <summary>
        /// Returns orders that have NO Invoice row yet (LEFT JOIN WHERE InvoiceID IS NULL),
        /// ordered by IssuedTime DESC.
        /// </summary>
        public List<OrderEntity> GetOrdersWithoutInvoice()
        {
            var list = new List<OrderEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT o.OrderID, o.CustomerID, c.CustomerName,
                             o.IssuedTime, o.DeliveryDate, o.GrandTotal,
                             o.OrderStatus, o.OrderContactName,
                             o.SalesID, s.StaffName AS SalesName,
                             o.QuotationID, o.AddressID,
                             o.ShippingAddress, o.BillingAddress,
                             o.SubTotal, o.DiscountType, o.DiscountValue, o.DiscountAmount
                      FROM `Order` o
                      JOIN Customer c ON o.CustomerID = c.CustomerID
                      JOIN Staff    s ON o.SalesID    = s.StaffID
                      LEFT JOIN Invoice i ON o.OrderID = i.OrderID
                      WHERE i.InvoiceID IS NULL
                      ORDER BY o.IssuedTime DESC";

                using (var cmd = new MySqlCommand(sql, conn))
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read()) list.Add(MapOrder(rdr));
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
                        (InvoiceID, OrderID, InvoiceDate, DepositAmount,
                         PaidAmount, RemainingBalance, TotalAmount, PaymentStatus, DueDate)
                      VALUES
                        (@id, @orderID, @date, @deposit,
                         @paid, @remaining, @total, @status, @due)";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id",        inv.InvoiceID);
                    cmd.Parameters.AddWithValue("@orderID",   inv.OrderID);
                    cmd.Parameters.AddWithValue("@date",      inv.InvoiceDate.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@deposit",   inv.DepositAmount);
                    cmd.Parameters.AddWithValue("@paid",      inv.PaidAmount);
                    cmd.Parameters.AddWithValue("@remaining", inv.RemainingBalance);
                    cmd.Parameters.AddWithValue("@total",     inv.TotalAmount);
                    cmd.Parameters.AddWithValue("@status",    inv.PaymentStatus);
                    cmd.Parameters.AddWithValue("@due",       inv.DueDate.ToString("yyyy-MM-dd"));
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        /// <summary>Returns existing InvoiceIDs that start with the given date prefix.</summary>
        public List<string> GetInvoiceIdsByPrefix(string prefix)
        {
            var list = new List<string>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql = "SELECT InvoiceID FROM Invoice WHERE InvoiceID LIKE @prefix";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@prefix", prefix + "%");
                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read()) list.Add(rdr.GetString(0));
                }
            }
            return list;
        }

        // ══════════════════════════════════════════════════════════════════
        //  INVOICE DETAIL queries  (Invoice List + Record Payment dialog)
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns all invoices with JOIN CustomerName,
        /// each enriched with its Transaction history.
        /// </summary>
        public List<InvoiceDetailEntity> GetInvoiceDetails(string keyword = null)
        {
            var list = new List<InvoiceDetailEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var sql =
                    @"SELECT i.InvoiceID, i.OrderID, c.CustomerName,
                             i.InvoiceDate, i.DepositAmount, i.PaidAmount,
                             i.RemainingBalance, i.TotalAmount,
                             i.PaymentStatus, i.DueDate,
                             (i.RemainingBalance > 0 AND i.DueDate < CURDATE()) AS IsOverdue
                      FROM Invoice i
                      JOIN `Order`  o ON i.OrderID    = o.OrderID
                      JOIN Customer c ON o.CustomerID = c.CustomerID
                      WHERE 1=1";

                if (!string.IsNullOrEmpty(keyword))
                    sql += @" AND (i.InvoiceID   LIKE @kw
                               OR i.OrderID      LIKE @kw
                               OR c.CustomerName LIKE @kw)";

                sql += " ORDER BY i.DueDate ASC";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(keyword))
                        cmd.Parameters.AddWithValue("@kw", "%" + keyword + "%");

                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            list.Add(new InvoiceDetailEntity
                            {
                                InvoiceID        = rdr.GetString("InvoiceID"),
                                OrderID          = rdr.GetString("OrderID"),
                                CustomerName     = rdr.GetString("CustomerName"),
                                InvoiceDate      = rdr.GetDateTime("InvoiceDate"),
                                DepositAmount    = rdr.IsDBNull(rdr.GetOrdinal("DepositAmount")) ? 0 : rdr.GetDouble("DepositAmount"),
                                PaidAmount       = rdr.GetDouble("PaidAmount"),
                                RemainingBalance = rdr.GetDouble("RemainingBalance"),
                                TotalAmount      = rdr.GetDouble("TotalAmount"),
                                PaymentStatus    = rdr.GetString("PaymentStatus"),
                                DueDate          = rdr.GetDateTime("DueDate"),
                                IsOverdue        = rdr.GetBoolean("IsOverdue")
                            });
                        }
                    }
                }
            }

            foreach (var inv in list)
                inv.Transactions = GetTransactionsByInvoice(inv.InvoiceID);

            return list;
        }

        /// <summary>Returns all Transaction rows linked to a given InvoiceID.</summary>
        public List<TransactionEntity> GetTransactionsByInvoice(string invoiceId)
        {
            var list = new List<TransactionEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT TransactionID, InvoiceID, PurInvoiceID, ReturnID,
                             Amount, TransactionDate, TransactionType
                      FROM `Transaction`
                      WHERE InvoiceID = @id
                      ORDER BY TransactionDate ASC";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", invoiceId);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            list.Add(new TransactionEntity
                            {
                                TransactionID   = rdr.GetString("TransactionID"),
                                InvoiceID       = rdr.IsDBNull(rdr.GetOrdinal("InvoiceID"))    ? null : rdr.GetString("InvoiceID"),
                                PurInvoiceID    = rdr.IsDBNull(rdr.GetOrdinal("PurInvoiceID")) ? null : rdr.GetString("PurInvoiceID"),
                                ReturnID        = rdr.IsDBNull(rdr.GetOrdinal("ReturnID"))      ? null : rdr.GetString("ReturnID"),
                                Amount          = rdr.GetDouble("Amount"),
                                TransactionDate = rdr.GetDateTime("TransactionDate"),
                                TransactionType = rdr.GetString("TransactionType")
                            });
                        }
                    }
                }
            }
            return list;
        }

        /// <summary>Generates the next TransactionID in format TXN-YYYYMMDD-NNNN.</summary>
        public string GenerateTransactionId()
        {
            string prefix = "TXN-" + DateTime.Today.ToString("yyyyMMdd") + "-";
            var    list   = new List<string>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql = "SELECT TransactionID FROM `Transaction` WHERE TransactionID LIKE @prefix";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@prefix", prefix + "%");
                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read()) list.Add(rdr.GetString(0));
                }
            }
            int next = 1;
            foreach (var id in list)
            {
                if (id.Length >= prefix.Length + 4 &&
                    int.TryParse(id.Substring(prefix.Length, 4), out int seq) && seq >= next)
                    next = seq + 1;
            }
            return $"{prefix}{next:D4}";
        }

        /// <summary>
        /// Inserts a Transaction row and updates Invoice.PaidAmount,
        /// RemainingBalance, PaymentStatus atomically.
        /// Returns true on full success.
        /// </summary>
        public bool RecordPayment(TransactionEntity txn)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Insert Transaction row
                        const string insertSql =
                            @"INSERT INTO `Transaction`
                                (TransactionID, InvoiceID, Amount, TransactionDate, TransactionType)
                              VALUES
                                (@tid, @iid, @amount, @date, @type)";

                        using (var cmd = new MySqlCommand(insertSql, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@tid",    txn.TransactionID);
                            cmd.Parameters.AddWithValue("@iid",    txn.InvoiceID);
                            cmd.Parameters.AddWithValue("@amount", txn.Amount);
                            cmd.Parameters.AddWithValue("@date",   txn.TransactionDate.ToString("yyyy-MM-dd"));
                            cmd.Parameters.AddWithValue("@type",   txn.TransactionType);
                            cmd.ExecuteNonQuery();
                        }

                        // 2. Re-aggregate paid amount from all transactions for this invoice
                        double newPaid;
                        using (var cmd = new MySqlCommand(
                            "SELECT COALESCE(SUM(Amount),0) FROM `Transaction` WHERE InvoiceID = @iid",
                            conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@iid", txn.InvoiceID);
                            newPaid = Convert.ToDouble(cmd.ExecuteScalar());
                        }

                        // 3. Fetch TotalAmount for this invoice
                        double total;
                        using (var cmd = new MySqlCommand(
                            "SELECT TotalAmount FROM Invoice WHERE InvoiceID = @iid", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@iid", txn.InvoiceID);
                            total = Convert.ToDouble(cmd.ExecuteScalar());
                        }

                        double newBalance = Math.Max(0, total - newPaid);
                        string newStatus  = newBalance <= 0 ? "Full" : "Partial";

                        // 4. Update Invoice row
                        const string updateSql =
                            @"UPDATE Invoice
                              SET PaidAmount       = @paid,
                                  RemainingBalance = @balance,
                                  PaymentStatus    = @status
                              WHERE InvoiceID      = @iid";

                        using (var cmd = new MySqlCommand(updateSql, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@paid",    newPaid);
                            cmd.Parameters.AddWithValue("@balance", newBalance);
                            cmd.Parameters.AddWithValue("@status",  newStatus);
                            cmd.Parameters.AddWithValue("@iid",     txn.InvoiceID);
                            cmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                        return true;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  COMPLAINT queries
        // ══════════════════════════════════════════════════════════════════

        /// <summary>Returns complaints with optional status / keyword filter.</summary>
        public List<ComplaintEntity> SearchComplaints(
            string status  = null,
            string keyword = null)
        {
            var list = new List<ComplaintEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var sql =
                    @"SELECT c.ComplaintID, c.OrderID,
                             s.StaffName, c.ComplaintDescription, c.ComplaintStatus
                      FROM Complaint c
                      JOIN Staff s ON c.StaffID = s.StaffID
                      WHERE 1=1";

                if (!string.IsNullOrEmpty(status))
                    sql += " AND c.ComplaintStatus = @status";
                if (!string.IsNullOrEmpty(keyword))
                    sql += @" AND (c.ComplaintID          LIKE @kw
                               OR c.OrderID              LIKE @kw
                               OR s.StaffName            LIKE @kw
                               OR c.ComplaintDescription LIKE @kw)";

                sql += " ORDER BY c.ComplaintID DESC";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(status))  cmd.Parameters.AddWithValue("@status", status);
                    if (!string.IsNullOrEmpty(keyword)) cmd.Parameters.AddWithValue("@kw", "%" + keyword + "%");

                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read()) list.Add(MapComplaint(rdr));
                }
            }
            return list;
        }

        /// <summary>Returns all complaints (no filter).</summary>
        public List<ComplaintEntity> GetAllComplaints() => SearchComplaints();

        /// <summary>Updates the ComplaintStatus of a single complaint.</summary>
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

        /// <summary>
        /// Returns a list of (StaffID, StaffName) tuples for all active staff.
        /// </summary>
        public List<(string StaffID, string StaffName)> GetStaffList()
        {
            var list = new List<(string, string)>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    "SELECT StaffID, StaffName FROM Staff ORDER BY StaffName ASC";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        list.Add((rdr.GetString("StaffID"), rdr.GetString("StaffName")));
            }
            return list;
        }

        /// <summary>
        /// Returns a list of (StaffID, StaffName, Department, StaffRole) tuples for all staff.
        /// </summary>
        public List<(string StaffID, string StaffName, string Department, string StaffRole)> GetStaffListForPicker()
        {
            var list = new List<(string, string, string, string)>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    "SELECT StaffID, StaffName, Department, StaffRole FROM Staff ORDER BY StaffName ASC";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        list.Add((
                            rdr.GetString("StaffID"),
                            rdr.GetString("StaffName"),
                            rdr.GetString("Department"),
                            rdr.GetString("StaffRole")));
            }
            return list;
        }

        /// <summary>Generates the next ComplaintID in the format CMP-YYYYMMDD-NNNN.</summary>
        public string GenerateComplaintId()
        {
            string prefix   = "CMP-" + DateTime.Today.ToString("yyyyMMdd") + "-";
            var    list     = new List<string>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql = "SELECT ComplaintID FROM Complaint WHERE ComplaintID LIKE @prefix";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@prefix", prefix + "%");
                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read()) list.Add(rdr.GetString(0));
                }
            }
            int next = 1;
            foreach (var id in list)
            {
                if (id.Length >= prefix.Length + 4 &&
                    int.TryParse(id.Substring(prefix.Length, 4), out int seq) && seq >= next)
                    next = seq + 1;
            }
            return $"{prefix}{next:D4}";
        }

        /// <summary>Inserts a new Complaint row. Returns true on success.</summary>
        public bool CreateComplaint(ComplaintEntity c)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"INSERT INTO Complaint
                        (ComplaintID, OrderID, StaffID, ComplaintDescription, ComplaintStatus)
                      VALUES
                        (@cid, @oid, @sid, @desc, @status)";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@cid",    c.ComplaintID);
                    cmd.Parameters.AddWithValue("@oid",    string.IsNullOrWhiteSpace(c.OrderID) ? (object)DBNull.Value : c.OrderID);
                    cmd.Parameters.AddWithValue("@sid",    c.StaffID);
                    cmd.Parameters.AddWithValue("@desc",   c.ComplaintDescription ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@status", c.ComplaintStatus);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  RETURN ORDER queries
        // ══════════════════════════════════════════════════════════════════

        /// <summary>Returns return orders with optional status / keyword filter.</summary>
        public List<ReturnOrderEntity> SearchReturnOrders(
            string status  = null,
            string keyword = null)
        {
            var list = new List<ReturnOrderEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var sql =
                    @"SELECT r.ReturnID, r.OrderID, c.CustomerName,
                             r.ReturnDate, r.Reason, r.RefundAmount, r.ReturnStatus
                      FROM ReturnOrder r
                      JOIN `Order`  o ON r.OrderID    = o.OrderID
                      JOIN Customer c ON o.CustomerID = c.CustomerID
                      WHERE 1=1";

                if (!string.IsNullOrEmpty(status))
                    sql += " AND r.ReturnStatus = @status";
                if (!string.IsNullOrEmpty(keyword))
                    sql += @" AND (r.ReturnID      LIKE @kw
                               OR r.OrderID        LIKE @kw
                               OR c.CustomerName   LIKE @kw
                               OR r.Reason         LIKE @kw)";

                sql += " ORDER BY r.ReturnDate DESC";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(status))  cmd.Parameters.AddWithValue("@status", status);
                    if (!string.IsNullOrEmpty(keyword)) cmd.Parameters.AddWithValue("@kw", "%" + keyword + "%");

                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read()) list.Add(MapReturnOrder(rdr));
                }
            }
            return list;
        }

        /// <summary>Returns all return orders (no filter).</summary>
        public List<ReturnOrderEntity> GetAllReturnOrders() => SearchReturnOrders();

        /// <summary>Updates the ReturnStatus of a single return order.</summary>
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

        /// <summary>Returns existing ReturnIDs that start with the given prefix.</summary>
        public List<string> GetReturnIdsByPrefix(string prefix)
        {
            var list = new List<string>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql = "SELECT ReturnID FROM ReturnOrder WHERE ReturnID LIKE @prefix";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@prefix", prefix + "%");
                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read()) list.Add(rdr.GetString(0));
                }
            }
            return list;
        }

        /// <summary>
        /// Returns all completed/delivered orders available for return.
        /// </summary>
        public List<OrderEntity> GetOrdersForReturn()
        {
            var list = new List<OrderEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT o.OrderID, o.CustomerID, c.CustomerName,
                             o.IssuedTime, o.DeliveryDate, o.GrandTotal,
                             o.OrderStatus, o.OrderContactName,
                             o.SalesID, s.StaffName AS SalesName,
                             o.QuotationID, o.AddressID,
                             o.ShippingAddress, o.BillingAddress,
                             o.SubTotal, o.DiscountType, o.DiscountValue, o.DiscountAmount
                      FROM `Order` o
                      JOIN Customer c ON o.CustomerID = c.CustomerID
                      JOIN Staff    s ON o.SalesID    = s.StaffID
                      WHERE o.OrderStatus IN ('Delivered','Completed')
                      ORDER BY o.IssuedTime DESC";

                using (var cmd = new MySqlCommand(sql, conn))
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read()) list.Add(MapOrder(rdr));
            }
            return list;
        }

        /// <summary>Inserts a new ReturnOrder row. Returns true on success.</summary>
        public bool CreateReturnOrder(ReturnOrderEntity r)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"INSERT INTO ReturnOrder
                        (ReturnID, OrderID, StaffID, ReturnDate, Reason, RefundAmount, ReturnStatus)
                      VALUES
                        (@rid, @oid, @sid, @date, @reason, @refund, @status)";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@rid",    r.ReturnID);
                    cmd.Parameters.AddWithValue("@oid",    r.OrderID);
                    cmd.Parameters.AddWithValue("@sid",    r.StaffID ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@date",   r.ReturnDate.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@reason", r.Reason   ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@refund", r.RefundAmount);
                    cmd.Parameters.AddWithValue("@status", r.ReturnStatus);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  ACCOUNT RECEIVABLE queries
        // ══════════════════════════════════════════════════════════════════

        /// <summary>Returns account-receivable rows with optional status / keyword filter.</summary>
        public List<AccountReceivableEntity> SearchAccountReceivables(
            string status  = null,
            string keyword = null)
        {
            var list = new List<AccountReceivableEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var sql =
                    @"SELECT i.InvoiceID, i.OrderID, c.CustomerName,
                             i.InvoiceDate, i.TotalAmount, i.PaidAmount,
                             i.RemainingBalance, i.PaymentStatus, i.DueDate
                      FROM Invoice i
                      JOIN `Order`  o ON i.OrderID    = o.OrderID
                      JOIN Customer c ON o.CustomerID = c.CustomerID
                      WHERE i.RemainingBalance > 0";

                if (!string.IsNullOrEmpty(status))
                    sql += " AND i.PaymentStatus = @status";
                if (!string.IsNullOrEmpty(keyword))
                    sql += @" AND (i.InvoiceID    LIKE @kw
                               OR c.CustomerName  LIKE @kw
                               OR i.OrderID       LIKE @kw)";

                sql += " ORDER BY i.DueDate ASC";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(status))  cmd.Parameters.AddWithValue("@status", status);
                    if (!string.IsNullOrEmpty(keyword)) cmd.Parameters.AddWithValue("@kw", "%" + keyword + "%");

                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read()) list.Add(MapAccountReceivable(rdr));
                }
            }
            return list;
        }

        // ══════════════════════════════════════════════════════════════════
        //  ACCOUNT PAYABLE queries
        // ══════════════════════════════════════════════════════════════════

        /// <summary>Returns account-payable rows with optional status / keyword filter.</summary>
        public List<AccountPayableEntity> SearchAccountPayables(
            string status  = null,
            string keyword = null)
        {
            var list = new List<AccountPayableEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var sql =
                    @"SELECT p.PurInvoiceID, p.SupplierID, s.SupplierName,
                             p.PurInvoiceDate, p.TotalAmount, p.PaidAmount,
                             p.RemainingBalance, p.PaymentStatus, p.DueDate
                      FROM PurchaseInvoice p
                      JOIN Supplier s ON p.SupplierID = s.SupplierID
                      WHERE p.RemainingBalance > 0";

                if (!string.IsNullOrEmpty(status))
                    sql += " AND p.PaymentStatus = @status";
                if (!string.IsNullOrEmpty(keyword))
                    sql += @" AND (p.PurInvoiceID  LIKE @kw
                               OR s.SupplierName   LIKE @kw
                               OR p.SupplierID     LIKE @kw)";

                sql += " ORDER BY p.DueDate ASC";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(status))  cmd.Parameters.AddWithValue("@status", status);
                    if (!string.IsNullOrEmpty(keyword)) cmd.Parameters.AddWithValue("@kw", "%" + keyword + "%");

                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read()) list.Add(MapAccountPayable(rdr));
                }
            }
            return list;
        }

        // ══════════════════════════════════════════════════════════════════
        //  MAPPERS
        // ══════════════════════════════════════════════════════════════════

        private static InvoiceEntity MapInvoice(MySqlDataReader r) => new InvoiceEntity
        {
            InvoiceID        = r.GetString("InvoiceID"),
            OrderID          = r.GetString("OrderID"),
            CustomerName     = r.GetString("CustomerName"),
            InvoiceDate      = r.GetDateTime("InvoiceDate"),
            DepositAmount    = r.IsDBNull(r.GetOrdinal("DepositAmount")) ? 0 : r.GetDouble("DepositAmount"),
            PaidAmount       = r.GetDouble("PaidAmount"),
            RemainingBalance = r.GetDouble("RemainingBalance"),
            TotalAmount      = r.GetDouble("TotalAmount"),
            PaymentStatus    = r.GetString("PaymentStatus"),
            DueDate          = r.GetDateTime("DueDate")
        };

        private static OrderEntity MapOrder(MySqlDataReader r) => new OrderEntity
        {
            OrderID          = r.GetString("OrderID"),
            CustomerID       = r.GetString("CustomerID"),
            CustomerName     = r.GetString("CustomerName"),
            IssuedTime       = r.GetDateTime("IssuedTime"),
            DeliveryDate     = r.IsDBNull(r.GetOrdinal("DeliveryDate"))     ? (DateTime?)null : r.GetDateTime("DeliveryDate"),
            GrandTotal       = r.IsDBNull(r.GetOrdinal("GrandTotal"))       ? 0 : r.GetDouble("GrandTotal"),
            OrderStatus      = r.GetString("OrderStatus"),
            OrderContactName = r.IsDBNull(r.GetOrdinal("OrderContactName")) ? null : r.GetString("OrderContactName"),
            SalesID          = r.GetString("SalesID"),
            SalesName        = r.GetString("SalesName"),
            QuotationID      = r.IsDBNull(r.GetOrdinal("QuotationID"))      ? null : r.GetString("QuotationID"),
            AddressID        = r.IsDBNull(r.GetOrdinal("AddressID"))        ? null : r.GetString("AddressID"),
            ShippingAddress  = r.IsDBNull(r.GetOrdinal("ShippingAddress"))  ? null : r.GetString("ShippingAddress"),
            BillingAddress   = r.IsDBNull(r.GetOrdinal("BillingAddress"))   ? null : r.GetString("BillingAddress"),
            SubTotal         = r.IsDBNull(r.GetOrdinal("SubTotal"))         ? 0 : r.GetDouble("SubTotal"),
            DiscountType     = r.IsDBNull(r.GetOrdinal("DiscountType"))     ? null : r.GetString("DiscountType"),
            DiscountValue    = r.IsDBNull(r.GetOrdinal("DiscountValue"))    ? 0 : r.GetDouble("DiscountValue"),
            DiscountAmount   = r.IsDBNull(r.GetOrdinal("DiscountAmount"))   ? 0 : r.GetDouble("DiscountAmount")
        };

        private static ComplaintEntity MapComplaint(MySqlDataReader r) => new ComplaintEntity
        {
            ComplaintID          = r.GetString("ComplaintID"),
            OrderID              = r.IsDBNull(r.GetOrdinal("OrderID")) ? null : r.GetString("OrderID"),
            StaffName            = r.GetString("StaffName"),
            ComplaintDescription = r.IsDBNull(r.GetOrdinal("ComplaintDescription")) ? null : r.GetString("ComplaintDescription"),
            ComplaintStatus      = r.GetString("ComplaintStatus")
        };

        private static ReturnOrderEntity MapReturnOrder(MySqlDataReader r) => new ReturnOrderEntity
        {
            ReturnID      = r.GetString("ReturnID"),
            OrderID       = r.GetString("OrderID"),
            CustomerName  = r.GetString("CustomerName"),
            ReturnDate    = r.GetDateTime("ReturnDate"),
            Reason        = r.IsDBNull(r.GetOrdinal("Reason"))       ? null  : r.GetString("Reason"),
            RefundAmount  = r.IsDBNull(r.GetOrdinal("RefundAmount"))  ? 0     : r.GetDouble("RefundAmount"),
            ReturnStatus  = r.GetString("ReturnStatus")
        };

        private static AccountReceivableEntity MapAccountReceivable(MySqlDataReader r) => new AccountReceivableEntity
        {
            InvoiceID        = r.GetString("InvoiceID"),
            OrderID          = r.GetString("OrderID"),
            CustomerName     = r.GetString("CustomerName"),
            InvoiceDate      = r.GetDateTime("InvoiceDate"),
            TotalAmount      = r.GetDouble("TotalAmount"),
            PaidAmount       = r.GetDouble("PaidAmount"),
            RemainingBalance = r.GetDouble("RemainingBalance"),
            PaymentStatus    = r.GetString("PaymentStatus"),
            DueDate          = r.GetDateTime("DueDate")
        };

        private static AccountPayableEntity MapAccountPayable(MySqlDataReader r) => new AccountPayableEntity
        {
            PurInvoiceID     = r.GetString("PurInvoiceID"),
            SupplierID       = r.GetString("SupplierID"),
            SupplierName     = r.GetString("SupplierName"),
            PurInvoiceDate   = r.GetDateTime("PurInvoiceDate"),
            TotalAmount      = r.GetDouble("TotalAmount"),
            PaidAmount       = r.GetDouble("PaidAmount"),
            RemainingBalance = r.GetDouble("RemainingBalance"),
            PaymentStatus    = r.GetString("PaymentStatus"),
            DueDate          = r.GetDateTime("DueDate")
        };
    }
}
