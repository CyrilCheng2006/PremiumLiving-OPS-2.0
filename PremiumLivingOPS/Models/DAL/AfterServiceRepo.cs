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
        /// Used to populate the Handled By ComboBox in Create Complaint.
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
        /// Used to populate the Handed By Picker in Create Return Order.
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

        /// <summary>
        /// Generates the next ComplaintID in the format CMP-YYYYMMDD-NNNN.
        /// </summary>
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

        /// <summary>
        /// Inserts a new Complaint row. Returns true on success.
        /// </summary>
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

        /// <summary>
        /// Returns existing ReturnIDs that start with the given prefix.
        /// Used by AfterServiceController.GenerateReturnId() to compute
        /// the next daily sequence number in RTN-YYYYMMDD-XXXX format.
        /// </summary>
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
        /// Returns all completed/delivered orders that are available for return,
        /// with CustomerName, OrderStatus, GrandTotal, IssuedTime.
        /// Used to populate the Order ID Picker in Create Return Order.
        /// </summary>
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
                    sql += @" AND (o.OrderID       LIKE @kw
                               OR c.CustomerName  LIKE @kw
                               OR o.OrderStatus   LIKE @kw)";

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

        /// <summary>
        /// Generates the next ReturnID in the format RTN-YYYYMMDD-NNNN.
        /// Kept for backward compatibility; prefer AfterServiceController.GenerateReturnId().
        /// </summary>
        public string GenerateReturnId()
        {
            string prefix = "RTN-" + DateTime.Today.ToString("yyyyMMdd") + "-";
            var    list   = GetReturnIdsByPrefix(prefix);
            int    next   = 1;
            foreach (var id in list)
            {
                if (id.Length >= prefix.Length + 4 &&
                    int.TryParse(id.Substring(prefix.Length, 4), out int seq) && seq >= next)
                    next = seq + 1;
            }
            return $"{prefix}{next:D4}";
        }

        /// <summary>
        /// Inserts a new ReturnOrder row. Returns true on success.
        /// HandedByID is stored only at UI level (not persisted — schema has no HandedBy column).
        /// </summary>
        public bool CreateReturnOrder(ReturnOrderEntity r)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"INSERT INTO ReturnOrder
                        (ReturnID, OrderID, ReturnDate, Reason, RefundAmount, ReturnStatus)
                      VALUES
                        (@rid, @oid, @date, @reason, @amount, @status)";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@rid",    r.ReturnID);
                    cmd.Parameters.AddWithValue("@oid",    r.OrderID);
                    cmd.Parameters.AddWithValue("@date",   r.ReturnDate.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@reason", string.IsNullOrWhiteSpace(r.Reason) ? (object)DBNull.Value : r.Reason);
                    cmd.Parameters.AddWithValue("@amount", r.RefundAmount);
                    cmd.Parameters.AddWithValue("@status", r.ReturnStatus ?? "Pending");
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  ACCOUNTS RECEIVABLE queries
        // ══════════════════════════════════════════════════════════════════

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
                             i.TotalAmount, i.PaidAmount, i.RemainingBalance,
                             i.PaymentStatus, i.DueDate,
                             (i.RemainingBalance > 0 AND i.DueDate < CURDATE()) AS IsOverdue
                      FROM Invoice i
                      JOIN `Order`  o ON i.OrderID    = o.OrderID
                      JOIN Customer c ON o.CustomerID = c.CustomerID
                      WHERE 1=1";

                if (status == "Overdue")
                    sql += " AND i.RemainingBalance > 0 AND i.DueDate < CURDATE()";
                else if (!string.IsNullOrEmpty(status))
                    sql += " AND i.PaymentStatus = @status";

                if (!string.IsNullOrEmpty(keyword))
                    sql += @" AND (i.InvoiceID   LIKE @kw
                               OR i.OrderID      LIKE @kw
                               OR c.CustomerName LIKE @kw)";

                sql += " ORDER BY i.DueDate ASC";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(status) && status != "Overdue")
                        cmd.Parameters.AddWithValue("@status", status);
                    if (!string.IsNullOrEmpty(keyword))
                        cmd.Parameters.AddWithValue("@kw", "%" + keyword + "%");

                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            list.Add(new AccountReceivableEntity
                            {
                                InvoiceID        = rdr.GetString("InvoiceID"),
                                OrderID          = rdr.GetString("OrderID"),
                                CustomerName     = rdr.GetString("CustomerName"),
                                TotalAmount      = rdr.GetDouble("TotalAmount"),
                                PaidAmount       = rdr.GetDouble("PaidAmount"),
                                RemainingBalance = rdr.GetDouble("RemainingBalance"),
                                PaymentStatus    = rdr.GetString("PaymentStatus"),
                                DueDate          = rdr.GetDateTime("DueDate"),
                                IsOverdue        = rdr.GetBoolean("IsOverdue")
                            });
                        }
                    }
                }
            }
            return list;
        }

        public List<AccountReceivableEntity> GetAccountReceivables(string status = null)
            => SearchAccountReceivables(status);

        // ══════════════════════════════════════════════════════════════════
        //  ACCOUNTS PAYABLE queries
        // ══════════════════════════════════════════════════════════════════

        public List<AccountPayableEntity> SearchAccountPayables(
            string status  = null,
            string keyword = null)
        {
            var list = new List<AccountPayableEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var sql =
                    @"SELECT pi.PurInvoiceID, pi.PurchaseID, sup.SupplierName,
                             pi.TotalAmount, pi.PaymentStatus, pi.ExpectedDate,
                             (pi.PaymentStatus != 'Full' AND pi.ExpectedDate < CURDATE()) AS IsOverdue
                      FROM PurchaseInvoice pi
                      JOIN PurchaseOrder po  ON pi.PurchaseID = po.PurchaseID
                      JOIN Supplier     sup  ON po.SupplierID = sup.SupplierID
                      WHERE 1=1";

                if (status == "Overdue")
                    sql += " AND pi.PaymentStatus != 'Full' AND pi.ExpectedDate < CURDATE()";
                else if (!string.IsNullOrEmpty(status))
                    sql += " AND pi.PaymentStatus = @status";

                if (!string.IsNullOrEmpty(keyword))
                    sql += @" AND (pi.PurInvoiceID LIKE @kw
                               OR pi.PurchaseID   LIKE @kw
                               OR sup.SupplierName LIKE @kw)";

                sql += " ORDER BY pi.ExpectedDate ASC";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(status) && status != "Overdue")
                        cmd.Parameters.AddWithValue("@status", status);
                    if (!string.IsNullOrEmpty(keyword))
                        cmd.Parameters.AddWithValue("@kw", "%" + keyword + "%");

                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            list.Add(new AccountPayableEntity
                            {
                                PurInvoiceID  = rdr.GetString("PurInvoiceID"),
                                PurchaseID    = rdr.GetString("PurchaseID"),
                                SupplierName  = rdr.GetString("SupplierName"),
                                TotalAmount   = rdr.GetDouble("TotalAmount"),
                                PaymentStatus = rdr.GetString("PaymentStatus"),
                                ExpectedDate  = rdr.GetDateTime("ExpectedDate"),
                                IsOverdue     = rdr.GetBoolean("IsOverdue")
                            });
                        }
                    }
                }
            }
            return list;
        }

        public List<AccountPayableEntity> GetAccountPayables(string status = null)
            => SearchAccountPayables(status);

        // ══════════════════════════════════════════════════════════════════
        //  Mapping helpers (private)
        // ══════════════════════════════════════════════════════════════════

        private static InvoiceEntity MapInvoice(MySqlDataReader rdr) =>
            new InvoiceEntity
            {
                InvoiceID        = rdr.GetString("InvoiceID"),
                OrderID          = rdr.GetString("OrderID"),
                CustomerName     = rdr.GetString("CustomerName"),
                InvoiceDate      = rdr.GetDateTime("InvoiceDate"),
                DepositAmount    = rdr.IsDBNull(rdr.GetOrdinal("DepositAmount"))  ? 0 : rdr.GetDouble("DepositAmount"),
                PaidAmount       = rdr.GetDouble("PaidAmount"),
                RemainingBalance = rdr.GetDouble("RemainingBalance"),
                TotalAmount      = rdr.GetDouble("TotalAmount"),
                PaymentStatus    = rdr.GetString("PaymentStatus"),
                DueDate          = rdr.GetDateTime("DueDate")
            };

        private static OrderEntity MapOrder(MySqlDataReader rdr) =>
            new OrderEntity
            {
                OrderID          = rdr.GetString("OrderID"),
                CustomerID       = rdr.GetString("CustomerID"),
                CustomerName     = rdr.GetString("CustomerName"),
                IssuedTime       = rdr.GetDateTime("IssuedTime"),
                DeliveryDate     = rdr.GetDateTime("DeliveryDate"),
                GrandTotal       = rdr.GetDouble("GrandTotal"),
                OrderStatus      = rdr.GetString("OrderStatus"),
                OrderContactName = rdr.GetString("OrderContactName"),
                SalesID          = rdr.GetString("SalesID"),
                SalesName        = rdr.GetString("SalesName"),
                QuotationID      = rdr.IsDBNull(rdr.GetOrdinal("QuotationID"))    ? null : rdr.GetString("QuotationID"),
                AddressID        = rdr.IsDBNull(rdr.GetOrdinal("AddressID"))      ? null : rdr.GetString("AddressID"),
                ShippingAddress  = rdr.GetString("ShippingAddress"),
                BillingAddress   = rdr.GetString("BillingAddress"),
                SubTotal         = rdr.IsDBNull(rdr.GetOrdinal("SubTotal"))       ? 0    : rdr.GetDouble("SubTotal"),
                DiscountType     = rdr.IsDBNull(rdr.GetOrdinal("DiscountType"))   ? null : rdr.GetString("DiscountType"),
                DiscountValue    = rdr.IsDBNull(rdr.GetOrdinal("DiscountValue"))  ? 0    : rdr.GetDouble("DiscountValue"),
                DiscountAmount   = rdr.IsDBNull(rdr.GetOrdinal("DiscountAmount")) ? 0    : rdr.GetDouble("DiscountAmount")
            };

        private static ComplaintEntity MapComplaint(MySqlDataReader rdr) =>
            new ComplaintEntity
            {
                ComplaintID          = rdr.GetString("ComplaintID"),
                OrderID              = rdr.IsDBNull(rdr.GetOrdinal("OrderID")) ? null : rdr.GetString("OrderID"),
                StaffName            = rdr.GetString("StaffName"),
                ComplaintDescription = rdr.IsDBNull(rdr.GetOrdinal("ComplaintDescription")) ? null : rdr.GetString("ComplaintDescription"),
                ComplaintStatus      = rdr.GetString("ComplaintStatus")
            };

        private static ReturnOrderEntity MapReturnOrder(MySqlDataReader rdr) =>
            new ReturnOrderEntity
            {
                ReturnID     = rdr.GetString("ReturnID"),
                OrderID      = rdr.GetString("OrderID"),
                CustomerName = rdr.GetString("CustomerName"),
                ReturnDate   = rdr.GetDateTime("ReturnDate"),
                Reason       = rdr.IsDBNull(rdr.GetOrdinal("Reason")) ? null : rdr.GetString("Reason"),
                RefundAmount = rdr.GetDouble("RefundAmount"),
                ReturnStatus = rdr.GetString("ReturnStatus")
            };
    }
}
