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
    public partial class AfterServiceRepo
    {
        // ══════════════════════════════════════════════════════════════════
        //  INVOICE queries
        // ══════════════════════════════════════════════════════════════════

        public List<InvoiceEntity> SearchInvoices(string status = null, string keyword = null)
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
                    sql += " AND (i.InvoiceID LIKE @kw OR c.CustomerName LIKE @kw OR i.OrderID LIKE @kw)";
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

        public List<InvoiceEntity> GetAllInvoices() => SearchInvoices();

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
                    sql += " AND (i.InvoiceID LIKE @kw OR i.OrderID LIKE @kw OR c.CustomerName LIKE @kw)";
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
            return string.Format("{0}{1:D4}", prefix, next);
        }

        public bool RecordPayment(TransactionEntity txn)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        const string insertSql =
                            @"INSERT INTO `Transaction`
                                (TransactionID, InvoiceID, Amount, TransactionDate, TransactionType)
                              VALUES (@tid, @iid, @amount, @date, @type)";
                        using (var cmd = new MySqlCommand(insertSql, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@tid",    txn.TransactionID);
                            cmd.Parameters.AddWithValue("@iid",    txn.InvoiceID);
                            cmd.Parameters.AddWithValue("@amount", txn.Amount);
                            cmd.Parameters.AddWithValue("@date",   txn.TransactionDate.ToString("yyyy-MM-dd"));
                            cmd.Parameters.AddWithValue("@type",   txn.TransactionType);
                            cmd.ExecuteNonQuery();
                        }

                        double newPaid;
                        using (var cmd = new MySqlCommand(
                            "SELECT COALESCE(SUM(Amount),0) FROM `Transaction` WHERE InvoiceID = @iid",
                            conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@iid", txn.InvoiceID);
                            newPaid = Convert.ToDouble(cmd.ExecuteScalar());
                        }

                        double total;
                        using (var cmd = new MySqlCommand(
                            "SELECT TotalAmount FROM Invoice WHERE InvoiceID = @iid", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@iid", txn.InvoiceID);
                            total = Convert.ToDouble(cmd.ExecuteScalar());
                        }

                        double newBalance = Math.Max(0, total - newPaid);
                        string newStatus  = newBalance <= 0 ? "Full" : "Partial";

                        const string updateSql =
                            @"UPDATE Invoice
                              SET PaidAmount = @paid, RemainingBalance = @balance, PaymentStatus = @status
                              WHERE InvoiceID = @iid";
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
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  COMPLAINT queries
        // ══════════════════════════════════════════════════════════════════

        public List<ComplaintEntity> SearchComplaints(string status = null, string keyword = null)
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
                    sql += " AND (c.ComplaintID LIKE @kw OR c.OrderID LIKE @kw OR s.StaffName LIKE @kw OR c.ComplaintDescription LIKE @kw)";
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

        public List<ComplaintEntity> GetAllComplaints() => SearchComplaints();

        public bool UpdateComplaintStatus(string complaintId, string newStatus)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql = "UPDATE Complaint SET ComplaintStatus = @status WHERE ComplaintID = @id";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@status", newStatus);
                    cmd.Parameters.AddWithValue("@id",     complaintId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public List<(string StaffID, string StaffName)> GetStaffList()
        {
            var list = new List<(string, string)>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql = "SELECT StaffID, StaffName FROM Staff ORDER BY StaffName ASC";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        list.Add((rdr.GetString("StaffID"), rdr.GetString("StaffName")));
            }
            return list;
        }

        public List<(string StaffID, string StaffName, string Department, string StaffRole)> GetStaffListForPicker()
        {
            var list = new List<(string, string, string, string)>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql = "SELECT StaffID, StaffName, Department, StaffRole FROM Staff ORDER BY StaffName ASC";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        list.Add((rdr.GetString("StaffID"), rdr.GetString("StaffName"),
                                  rdr.GetString("Department"), rdr.GetString("StaffRole")));
            }
            return list;
        }

        public string GenerateComplaintId()
        {
            string prefix = "CMP-" + DateTime.Today.ToString("yyyyMMdd") + "-";
            var    list   = new List<string>();
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
            return string.Format("{0}{1:D4}", prefix, next);
        }

        public bool CreateComplaint(ComplaintEntity c)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"INSERT INTO Complaint
                        (ComplaintID, OrderID, StaffID, ComplaintDescription, ComplaintStatus)
                      VALUES (@cid, @oid, @sid, @desc, @status)";
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
                      JOIN `Order`  o ON r.OrderID    = o.OrderID
                      JOIN Customer c ON o.CustomerID = c.CustomerID
                      WHERE 1=1";

                if (!string.IsNullOrEmpty(status))
                    sql += " AND r.ReturnStatus = @status";
                if (!string.IsNullOrEmpty(keyword))
                    sql += " AND (r.ReturnID LIKE @kw OR r.OrderID LIKE @kw OR c.CustomerName LIKE @kw OR r.Reason LIKE @kw)";
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

        public List<ReturnOrderEntity> GetAllReturnOrders() => SearchReturnOrders();

        public bool UpdateReturnOrderStatus(string returnId, string newStatus)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql = "UPDATE ReturnOrder SET ReturnStatus = @status WHERE ReturnID = @id";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@status", newStatus);
                    cmd.Parameters.AddWithValue("@id",     returnId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

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

        public List<OrderEntity> GetOrdersForReturnPicker(string keyword = null)
        {
            var list = new List<OrderEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var sql =
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
                      WHERE o.OrderStatus IN ('Delivered','Completed','Partially Delivered')";

                if (!string.IsNullOrEmpty(keyword))
                    sql += " AND (o.OrderID LIKE @kw OR c.CustomerName LIKE @kw)";
                sql += " ORDER BY o.IssuedTime DESC";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(keyword))
                        cmd.Parameters.AddWithValue("@kw", "%" + keyword + "%");
                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read()) list.Add(MapOrder(rdr));
                }
            }
            return list;
        }

        public bool CreateReturnOrder(ReturnOrderEntity r)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"INSERT INTO ReturnOrder
                        (ReturnID, OrderID, StaffID, ReturnDate, Reason, RefundAmount, ReturnStatus)
                      VALUES (@rid, @oid, @sid, @date, @reason, @refund, @status)";
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

        public List<AccountReceivableEntity> SearchAccountReceivables(string status = null, string keyword = null)
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
                    sql += " AND (i.InvoiceID LIKE @kw OR c.CustomerName LIKE @kw OR i.OrderID LIKE @kw)";
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
        //
        //  PurchaseInvoice schema:
        //    PurInvoiceID  VARCHAR(20) PK
        //    PurchaseID    VARCHAR(20) FK -> PurchaseOrder.PurchaseID
        //    TotalAmount   DECIMAL
        //    PaymentStatus ENUM('Partial','Full')
        //    ExpectedDate  DATE
        //
        //  PaidAmount / RemainingBalance computed from Transaction rows.
        // ══════════════════════════════════════════════════════════════════

        public List<AccountPayableEntity> SearchAccountPayables(string status = null, string keyword = null)
        {
            var list = new List<AccountPayableEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var sql =
                    @"SELECT p.PurInvoiceID,
                             po.PurchaseID,
                             su.SupplierID,
                             su.SupplierName,
                             p.ExpectedDate                                           AS DueDate,
                             p.TotalAmount,
                             COALESCE(tx.PaidAmount, 0)                              AS PaidAmount,
                             GREATEST(p.TotalAmount - COALESCE(tx.PaidAmount, 0), 0) AS RemainingBalance,
                             p.PaymentStatus,
                             p.ExpectedDate                                           AS PurInvoiceDate
                      FROM PurchaseInvoice p
                      JOIN PurchaseOrder   po ON p.PurchaseID  = po.PurchaseID
                      JOIN Supplier        su ON po.SupplierID = su.SupplierID
                      LEFT JOIN (
                          SELECT PurInvoiceID, SUM(Amount) AS PaidAmount
                          FROM `Transaction`
                          WHERE PurInvoiceID IS NOT NULL
                          GROUP BY PurInvoiceID
                      ) tx ON tx.PurInvoiceID = p.PurInvoiceID
                      WHERE GREATEST(p.TotalAmount - COALESCE(tx.PaidAmount, 0), 0) > 0";

                if (!string.IsNullOrEmpty(status))
                    sql += " AND p.PaymentStatus = @status";
                if (!string.IsNullOrEmpty(keyword))
                    sql += " AND (p.PurInvoiceID LIKE @kw OR su.SupplierName LIKE @kw OR po.PurchaseID LIKE @kw)";
                sql += " ORDER BY p.ExpectedDate ASC";

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
        //  PRIVATE MAP HELPERS
        //  One static method per entity — reads an open MySqlDataReader row.
        //  Keeps all Search/Get methods clean and DRY.
        // ══════════════════════════════════════════════════════════════════

        private static InvoiceEntity MapInvoice(MySqlDataReader r)
        {
            return new InvoiceEntity
            {
                InvoiceID        = r.GetString("InvoiceID"),
                OrderID          = r.GetString("OrderID"),
                CustomerName     = r.IsDBNull(r.GetOrdinal("CustomerName"))     ? null : r.GetString("CustomerName"),
                InvoiceDate      = r.GetDateTime("InvoiceDate"),
                DepositAmount    = r.IsDBNull(r.GetOrdinal("DepositAmount"))    ? 0    : r.GetDouble("DepositAmount"),
                PaidAmount       = r.GetDouble("PaidAmount"),
                RemainingBalance = r.GetDouble("RemainingBalance"),
                TotalAmount      = r.GetDouble("TotalAmount"),
                PaymentStatus    = r.GetString("PaymentStatus"),
                DueDate          = r.GetDateTime("DueDate")
            };
        }

        private static OrderEntity MapOrder(MySqlDataReader r)
        {
            return new OrderEntity
            {
                OrderID          = r.GetString("OrderID"),
                CustomerID       = r.GetString("CustomerID"),
                CustomerName     = r.IsDBNull(r.GetOrdinal("CustomerName"))      ? null : r.GetString("CustomerName"),
                IssuedTime       = r.GetDateTime("IssuedTime"),
                DeliveryDate     = r.IsDBNull(r.GetOrdinal("DeliveryDate"))      ? (DateTime?)null : r.GetDateTime("DeliveryDate"),
                GrandTotal       = r.IsDBNull(r.GetOrdinal("GrandTotal"))        ? 0    : r.GetDouble("GrandTotal"),
                OrderStatus      = r.GetString("OrderStatus"),
                OrderContactName = r.IsDBNull(r.GetOrdinal("OrderContactName"))  ? null : r.GetString("OrderContactName"),
                SalesID          = r.GetString("SalesID"),
                SalesName        = r.IsDBNull(r.GetOrdinal("SalesName"))         ? null : r.GetString("SalesName"),
                QuotationID      = r.IsDBNull(r.GetOrdinal("QuotationID"))       ? null : r.GetString("QuotationID"),
                AddressID        = r.IsDBNull(r.GetOrdinal("AddressID"))         ? null : r.GetString("AddressID"),
                ShippingAddress  = r.IsDBNull(r.GetOrdinal("ShippingAddress"))   ? null : r.GetString("ShippingAddress"),
                BillingAddress   = r.IsDBNull(r.GetOrdinal("BillingAddress"))    ? null : r.GetString("BillingAddress"),
                SubTotal         = r.IsDBNull(r.GetOrdinal("SubTotal"))          ? 0    : r.GetDouble("SubTotal"),
                DiscountType     = r.IsDBNull(r.GetOrdinal("DiscountType"))      ? null : r.GetString("DiscountType"),
                DiscountValue    = r.IsDBNull(r.GetOrdinal("DiscountValue"))     ? 0    : r.GetDouble("DiscountValue"),
                DiscountAmount   = r.IsDBNull(r.GetOrdinal("DiscountAmount"))    ? 0    : r.GetDouble("DiscountAmount")
            };
        }

        private static ComplaintEntity MapComplaint(MySqlDataReader r)
        {
            return new ComplaintEntity
            {
                ComplaintID          = r.GetString("ComplaintID"),
                OrderID              = r.IsDBNull(r.GetOrdinal("OrderID"))              ? null : r.GetString("OrderID"),
                StaffName            = r.IsDBNull(r.GetOrdinal("StaffName"))            ? null : r.GetString("StaffName"),
                ComplaintDescription = r.IsDBNull(r.GetOrdinal("ComplaintDescription")) ? null : r.GetString("ComplaintDescription"),
                ComplaintStatus      = r.GetString("ComplaintStatus")
            };
        }

        private static ReturnOrderEntity MapReturnOrder(MySqlDataReader r)
        {
            return new ReturnOrderEntity
            {
                ReturnID      = r.GetString("ReturnID"),
                OrderID       = r.GetString("OrderID"),
                CustomerName  = r.IsDBNull(r.GetOrdinal("CustomerName")) ? null : r.GetString("CustomerName"),
                ReturnDate    = r.GetDateTime("ReturnDate"),
                Reason        = r.IsDBNull(r.GetOrdinal("Reason"))       ? null : r.GetString("Reason"),
                RefundAmount  = r.IsDBNull(r.GetOrdinal("RefundAmount")) ? 0    : r.GetDouble("RefundAmount"),
                ReturnStatus  = r.GetString("ReturnStatus")
            };
        }

        private static AccountReceivableEntity MapAccountReceivable(MySqlDataReader r)
        {
            return new AccountReceivableEntity
            {
                InvoiceID        = r.GetString("InvoiceID"),
                OrderID          = r.GetString("OrderID"),
                CustomerName     = r.IsDBNull(r.GetOrdinal("CustomerName"))  ? null : r.GetString("CustomerName"),
                InvoiceDate      = r.GetDateTime("InvoiceDate"),
                TotalAmount      = r.GetDouble("TotalAmount"),
                PaidAmount       = r.GetDouble("PaidAmount"),
                RemainingBalance = r.GetDouble("RemainingBalance"),
                PaymentStatus    = r.GetString("PaymentStatus"),
                DueDate          = r.GetDateTime("DueDate")
            };
        }

        private static AccountPayableEntity MapAccountPayable(MySqlDataReader r)
        {
            return new AccountPayableEntity
            {
                PurInvoiceID     = r.GetString("PurInvoiceID"),
                PurchaseID       = r.GetString("PurchaseID"),
                SupplierID       = r.GetString("SupplierID"),
                SupplierName     = r.IsDBNull(r.GetOrdinal("SupplierName"))  ? null : r.GetString("SupplierName"),
                DueDate          = r.GetDateTime("DueDate"),
                TotalAmount      = r.GetDouble("TotalAmount"),
                PaidAmount       = r.GetDouble("PaidAmount"),
                RemainingBalance = r.GetDouble("RemainingBalance"),
                PaymentStatus    = r.GetString("PaymentStatus"),
                PurInvoiceDate   = r.GetDateTime("PurInvoiceDate")
            };
        }
    }
}
