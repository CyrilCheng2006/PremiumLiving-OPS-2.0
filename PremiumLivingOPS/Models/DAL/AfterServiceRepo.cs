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

        // ══════════════════════════════════════════════════════════════════
        //  ACCOUNTS RECEIVABLE queries
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns Account Receivable records with optional status / keyword filter.
        /// IsOverdue = RemainingBalance &gt; 0 AND DueDate &lt; CURDATE().
        /// status filter: 'Partial' | 'Full' | 'Overdue' (computed).
        /// keyword: searches InvoiceID, OrderID, CustomerName.
        /// </summary>
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

        /// <summary>Returns all AR records (no filter). Used by KPI panel.</summary>
        public List<AccountReceivableEntity> GetAccountReceivables(string status = null)
            => SearchAccountReceivables(status);

        // ══════════════════════════════════════════════════════════════════
        //  ACCOUNTS PAYABLE queries
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns Account Payable records with optional status / keyword filter.
        /// IsOverdue = PaymentStatus != 'Full' AND ExpectedDate &lt; CURDATE().
        /// status filter: 'Partial' | 'Full' | 'Overdue' (computed).
        /// keyword: searches PurInvoiceID, PurchaseID, SupplierName.
        /// </summary>
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

        /// <summary>Returns all AP records (no filter). Used by KPI panel.</summary>
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
