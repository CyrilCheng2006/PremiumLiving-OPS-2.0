using MySql.Data.MySqlClient;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.DAL
{
    /// <summary>
    /// Data Access Layer for the After-Service module.
    /// All SQL lives here; no UI references.
    /// </summary>
    public partial class AfterServiceRepo
    {
        // ════════════════════════════════════════════════════════════════════
        //  Create Invoice
        // ════════════════════════════════════════════════════════════════════

        public List<OrderForInvoiceEntity> GetOrdersWithoutInvoice()
        {
            var list = new List<OrderForInvoiceEntity>();
            using var conn = DbConnection.GetConnection();
            conn.Open();
            const string sql = @"
                SELECT o.OrderID, o.CustomerID, c.CustomerName,
                       o.GrandTotal, o.IssuedTime, o.OrderStatus
                FROM   `Order` o
                JOIN   Customer c ON c.CustomerID = o.CustomerID
                WHERE  o.OrderStatus IN ('Delivered','Completed')
                  AND  o.OrderID NOT IN (SELECT OrderID FROM Invoice WHERE OrderID IS NOT NULL)
                ORDER BY o.IssuedTime DESC";
            using var cmd = new MySqlCommand(sql, conn);
            using var r   = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new OrderForInvoiceEntity
                {
                    OrderID      = r.GetString("OrderID"),
                    CustomerID   = r.GetString("CustomerID"),
                    CustomerName = r.GetString("CustomerName"),
                    GrandTotal   = r.GetDouble("GrandTotal"),
                    IssuedTime   = r.GetDateTime("IssuedTime"),
                    OrderStatus  = r.GetString("OrderStatus")
                });
            return list;
        }

        public List<string> GetInvoiceIdsByPrefix(string prefix)
        {
            var list = new List<string>();
            using var conn = DbConnection.GetConnection();
            conn.Open();
            const string sql = "SELECT InvoiceID FROM Invoice WHERE InvoiceID LIKE @p ORDER BY InvoiceID";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@p", prefix + "%");
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(r.GetString(0));
            return list;
        }

        public bool CreateInvoice(InvoiceEntity inv)
        {
            using var conn = DbConnection.GetConnection();
            conn.Open();
            const string sql = @"
                INSERT INTO Invoice
                    (InvoiceID, OrderID, CustomerID, StaffID,
                     TotalAmount, PaidAmount, RemainingBalance,
                     PaymentStatus, PaymentMethod, IssuedDate)
                VALUES
                    (@id, @order, @cust, @staff,
                     @total, @paid, @remaining,
                     @status, @method, @issued)";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id",        inv.InvoiceID);
            cmd.Parameters.AddWithValue("@order",     inv.OrderID);
            cmd.Parameters.AddWithValue("@cust",      inv.CustomerID);
            cmd.Parameters.AddWithValue("@staff",     inv.StaffID);
            cmd.Parameters.AddWithValue("@total",     inv.TotalAmount);
            cmd.Parameters.AddWithValue("@paid",      inv.PaidAmount);
            cmd.Parameters.AddWithValue("@remaining", inv.RemainingBalance);
            cmd.Parameters.AddWithValue("@status",    inv.PaymentStatus);
            cmd.Parameters.AddWithValue("@method",    inv.PaymentMethod);
            cmd.Parameters.AddWithValue("@issued",    inv.IssuedDate);
            return cmd.ExecuteNonQuery() > 0;
        }

        // ════════════════════════════════════════════════════════════════════
        //  Complaint
        // ════════════════════════════════════════════════════════════════════

        public List<ComplaintEntity> SearchComplaints(string status, string keyword)
        {
            var list = new List<ComplaintEntity>();
            using var conn = DbConnection.GetConnection();
            conn.Open();
            var sql = @"
                SELECT c.ComplaintID, c.OrderID, cu.CustomerName,
                       c.ComplaintType, c.ComplaintStatus,
                       c.Description, c.CreatedDate,
                       COALESCE(s.StaffName,'') AS AssignedStaffName
                FROM   Complaint c
                JOIN   `Order` o  ON o.OrderID    = c.OrderID
                JOIN   Customer cu ON cu.CustomerID = o.CustomerID
                LEFT JOIN Staff s ON s.StaffID = c.AssignedStaffID
                WHERE  1=1";
            if (!string.IsNullOrEmpty(status))  sql += " AND c.ComplaintStatus = @status";
            if (!string.IsNullOrEmpty(keyword)) sql += " AND (c.ComplaintID LIKE @kw OR c.OrderID LIKE @kw OR cu.CustomerName LIKE @kw)";
            sql += " ORDER BY c.CreatedDate DESC";
            using var cmd = new MySqlCommand(sql, conn);
            if (!string.IsNullOrEmpty(status))  cmd.Parameters.AddWithValue("@status", status);
            if (!string.IsNullOrEmpty(keyword)) cmd.Parameters.AddWithValue("@kw",     "%" + keyword + "%");
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new ComplaintEntity
                {
                    ComplaintID       = r.GetString("ComplaintID"),
                    OrderID           = r.GetString("OrderID"),
                    CustomerName      = r.GetString("CustomerName"),
                    ComplaintType     = r.GetString("ComplaintType"),
                    ComplaintStatus   = r.GetString("ComplaintStatus"),
                    Description       = r.IsDBNull(r.GetOrdinal("Description")) ? "" : r.GetString("Description"),
                    CreatedDate       = r.GetDateTime("CreatedDate"),
                    AssignedStaffName = r.GetString("AssignedStaffName")
                });
            return list;
        }

        public bool UpdateComplaintStatus(string complaintId, string newStatus)
        {
            using var conn = DbConnection.GetConnection();
            conn.Open();
            const string sql = "UPDATE Complaint SET ComplaintStatus=@s WHERE ComplaintID=@id";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@s",  newStatus);
            cmd.Parameters.AddWithValue("@id", complaintId);
            return cmd.ExecuteNonQuery() > 0;
        }

        public List<(string StaffID, string StaffName)> GetStaffList()
        {
            var list = new List<(string, string)>();
            using var conn = DbConnection.GetConnection();
            conn.Open();
            const string sql = "SELECT StaffID, StaffName FROM Staff ORDER BY StaffName";
            using var cmd = new MySqlCommand(sql, conn);
            using var r   = cmd.ExecuteReader();
            while (r.Read()) list.Add((r.GetString(0), r.GetString(1)));
            return list;
        }

        public bool CreateComplaint(ComplaintEntity c)
        {
            using var conn = DbConnection.GetConnection();
            conn.Open();
            const string sql = @"
                INSERT INTO Complaint
                    (ComplaintID, OrderID, AssignedStaffID,
                     ComplaintType, ComplaintStatus, Description, CreatedDate)
                VALUES
                    (@id, @order, @staff, @type, @status, @desc, @date)";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id",     c.ComplaintID);
            cmd.Parameters.AddWithValue("@order",  c.OrderID);
            cmd.Parameters.AddWithValue("@staff",  c.AssignedStaffID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@type",   c.ComplaintType);
            cmd.Parameters.AddWithValue("@status", c.ComplaintStatus);
            cmd.Parameters.AddWithValue("@desc",   c.Description ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@date",   c.CreatedDate);
            return cmd.ExecuteNonQuery() > 0;
        }

        public string GenerateComplaintId()
        {
            using var conn = DbConnection.GetConnection();
            conn.Open();
            const string sql = "SELECT ComplaintID FROM Complaint ORDER BY ComplaintID DESC LIMIT 1";
            using var cmd = new MySqlCommand(sql, conn);
            var last = cmd.ExecuteScalar()?.ToString();
            if (last != null && last.StartsWith("CMP-") && int.TryParse(last.Substring(4), out int n))
                return $"CMP-{n + 1:D4}";
            return "CMP-0001";
        }

        // ════════════════════════════════════════════════════════════════════
        //  Return Order
        // ════════════════════════════════════════════════════════════════════

        public List<ReturnOrderEntity> SearchReturnOrders(string status, string keyword)
        {
            var list = new List<ReturnOrderEntity>();
            using var conn = DbConnection.GetConnection();
            conn.Open();
            var sql = @"
                SELECT ro.ReturnID, ro.OrderID, c.CustomerName,
                       ro.ReturnStatus, ro.ReturnDate, ro.Reason,
                       ro.RefundAmount
                FROM   ReturnOrder ro
                JOIN   `Order` o ON o.OrderID = ro.OrderID
                JOIN   Customer c ON c.CustomerID = o.CustomerID
                WHERE  1=1";
            if (!string.IsNullOrEmpty(status))  sql += " AND ro.ReturnStatus = @status";
            if (!string.IsNullOrEmpty(keyword)) sql += " AND (ro.ReturnID LIKE @kw OR ro.OrderID LIKE @kw OR c.CustomerName LIKE @kw)";
            sql += " ORDER BY ro.ReturnDate DESC";
            using var cmd = new MySqlCommand(sql, conn);
            if (!string.IsNullOrEmpty(status))  cmd.Parameters.AddWithValue("@status", status);
            if (!string.IsNullOrEmpty(keyword)) cmd.Parameters.AddWithValue("@kw",     "%" + keyword + "%");
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new ReturnOrderEntity
                {
                    ReturnID     = r.GetString("ReturnID"),
                    OrderID      = r.GetString("OrderID"),
                    CustomerName = r.GetString("CustomerName"),
                    ReturnStatus = r.GetString("ReturnStatus"),
                    ReturnDate   = r.GetDateTime("ReturnDate"),
                    Reason       = r.IsDBNull(r.GetOrdinal("Reason")) ? "" : r.GetString("Reason"),
                    RefundAmount = r.IsDBNull(r.GetOrdinal("RefundAmount")) ? 0 : r.GetDouble("RefundAmount")
                });
            return list;
        }

        public bool UpdateReturnOrderStatus(string returnId, string newStatus)
        {
            using var conn = DbConnection.GetConnection();
            conn.Open();
            const string sql = "UPDATE ReturnOrder SET ReturnStatus=@s WHERE ReturnID=@id";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@s",  newStatus);
            cmd.Parameters.AddWithValue("@id", returnId);
            return cmd.ExecuteNonQuery() > 0;
        }

        // ════════════════════════════════════════════════════════════════════
        //  Account Receivable
        // ════════════════════════════════════════════════════════════════════

        public List<AccountReceivableEntity> SearchAccountReceivables(string status, string keyword)
        {
            var list = new List<AccountReceivableEntity>();
            using var conn = DbConnection.GetConnection();
            conn.Open();
            var sql = @"
                SELECT i.InvoiceID, i.OrderID, c.CustomerName,
                       i.TotalAmount, i.PaidAmount, i.RemainingBalance,
                       i.PaymentStatus, i.IssuedDate
                FROM   Invoice i
                JOIN   `Order`   o ON o.OrderID    = i.OrderID
                JOIN   Customer  c ON c.CustomerID = o.CustomerID
                WHERE  1=1";
            if (!string.IsNullOrEmpty(status))  sql += " AND i.PaymentStatus = @status";
            if (!string.IsNullOrEmpty(keyword)) sql += " AND (i.InvoiceID LIKE @kw OR i.OrderID LIKE @kw OR c.CustomerName LIKE @kw)";
            sql += " ORDER BY i.IssuedDate DESC";
            using var cmd = new MySqlCommand(sql, conn);
            if (!string.IsNullOrEmpty(status))  cmd.Parameters.AddWithValue("@status", status);
            if (!string.IsNullOrEmpty(keyword)) cmd.Parameters.AddWithValue("@kw",     "%" + keyword + "%");
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new AccountReceivableEntity
                {
                    InvoiceID        = r.GetString("InvoiceID"),
                    OrderID          = r.GetString("OrderID"),
                    CustomerName     = r.GetString("CustomerName"),
                    TotalAmount      = r.GetDouble("TotalAmount"),
                    PaidAmount       = r.GetDouble("PaidAmount"),
                    RemainingBalance = r.GetDouble("RemainingBalance"),
                    PaymentStatus    = r.GetString("PaymentStatus"),
                    IssuedDate       = r.GetDateTime("IssuedDate")
                });
            return list;
        }

        // ════════════════════════════════════════════════════════════════════
        //  Invoice Details + Payment
        // ════════════════════════════════════════════════════════════════════

        public List<InvoiceDetailEntity> GetInvoiceDetails(string keyword)
        {
            var list = new List<InvoiceDetailEntity>();
            using var conn = DbConnection.GetConnection();
            conn.Open();
            var sql = @"
                SELECT i.InvoiceID, i.OrderID, c.CustomerName,
                       i.TotalAmount, i.PaidAmount, i.RemainingBalance,
                       i.PaymentStatus, i.PaymentMethod, i.IssuedDate
                FROM   Invoice i
                JOIN   `Order`  o ON o.OrderID    = i.OrderID
                JOIN   Customer c ON c.CustomerID = o.CustomerID
                WHERE  1=1";
            if (!string.IsNullOrEmpty(keyword))
                sql += " AND (i.InvoiceID LIKE @kw OR i.OrderID LIKE @kw OR c.CustomerName LIKE @kw)";
            sql += " ORDER BY i.IssuedDate DESC";
            using var cmd = new MySqlCommand(sql, conn);
            if (!string.IsNullOrEmpty(keyword))
                cmd.Parameters.AddWithValue("@kw", "%" + keyword + "%");
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new InvoiceDetailEntity
                {
                    InvoiceID        = r.GetString("InvoiceID"),
                    OrderID          = r.GetString("OrderID"),
                    CustomerName     = r.GetString("CustomerName"),
                    TotalAmount      = r.GetDouble("TotalAmount"),
                    PaidAmount       = r.GetDouble("PaidAmount"),
                    RemainingBalance = r.GetDouble("RemainingBalance"),
                    PaymentStatus    = r.GetString("PaymentStatus"),
                    PaymentMethod    = r.IsDBNull(r.GetOrdinal("PaymentMethod")) ? "" : r.GetString("PaymentMethod"),
                    IssuedDate       = r.GetDateTime("IssuedDate")
                });
            return list;
        }

        public string GenerateTransactionId()
        {
            using var conn = DbConnection.GetConnection();
            conn.Open();
            const string sql = "SELECT TransactionID FROM PaymentTransaction ORDER BY TransactionID DESC LIMIT 1";
            using var cmd = new MySqlCommand(sql, conn);
            var last = cmd.ExecuteScalar()?.ToString();
            if (last != null && last.StartsWith("TXN-") && int.TryParse(last.Substring(4), out int n))
                return $"TXN-{n + 1:D4}";
            return "TXN-0001";
        }

        public bool RecordPayment(TransactionEntity txn)
        {
            using var conn = DbConnection.GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                const string ins = @"
                    INSERT INTO PaymentTransaction
                        (TransactionID, InvoiceID, Amount, TransactionDate, TransactionType)
                    VALUES (@tid, @inv, @amt, @date, @type)";
                using var cmd1 = new MySqlCommand(ins, conn, tx);
                cmd1.Parameters.AddWithValue("@tid",  txn.TransactionID);
                cmd1.Parameters.AddWithValue("@inv",  txn.InvoiceID);
                cmd1.Parameters.AddWithValue("@amt",  txn.Amount);
                cmd1.Parameters.AddWithValue("@date", txn.TransactionDate);
                cmd1.Parameters.AddWithValue("@type", txn.TransactionType);
                cmd1.ExecuteNonQuery();

                const string upd = @"
                    UPDATE Invoice
                    SET    PaidAmount       = PaidAmount + @amt,
                           RemainingBalance = GREATEST(0, RemainingBalance - @amt),
                           PaymentStatus    = CASE
                               WHEN GREATEST(0, RemainingBalance - @amt) <= 0 THEN 'Full'
                               ELSE 'Partial' END
                    WHERE  InvoiceID = @inv";
                using var cmd2 = new MySqlCommand(upd, conn, tx);
                cmd2.Parameters.AddWithValue("@amt", txn.Amount);
                cmd2.Parameters.AddWithValue("@inv", txn.InvoiceID);
                cmd2.ExecuteNonQuery();

                tx.Commit();
                return true;
            }
            catch { tx.Rollback(); throw; }
        }

        // ════════════════════════════════════════════════════════════════════
        //  Account Payable
        //  Supports optional status / keyword / dateFrom filters.
        //  dateFrom filters on pi.ExpectedPaymentDate >= @dateFrom.
        // ════════════════════════════════════════════════════════════════════

        public List<AccountPayableEntity> SearchAccountPayables(
            string    status   = null,
            string    keyword  = null,
            DateTime? dateFrom = null)
        {
            var list = new List<AccountPayableEntity>();
            using var conn = DbConnection.GetConnection();
            conn.Open();

            var sql = @"
                SELECT pi.PurInvoiceID, pi.PurchaseID, s.SupplierName,
                       pi.TotalAmount, pi.PaymentStatus,
                       pi.ExpectedPaymentDate,
                       CASE WHEN pi.ExpectedPaymentDate < CURDATE()
                            AND  pi.PaymentStatus <> 'Full' THEN 1 ELSE 0 END AS IsOverdue
                FROM   PurchaseInvoice pi
                JOIN   PurchaseOrder   po ON po.PurchaseID   = pi.PurchaseID
                JOIN   Supplier        s  ON s.SupplierID    = po.SupplierID
                WHERE  1=1";

            if (!string.IsNullOrEmpty(status))
                sql += " AND pi.PaymentStatus = @status";
            if (!string.IsNullOrEmpty(keyword))
                sql += " AND (pi.PurInvoiceID LIKE @kw OR pi.PurchaseID LIKE @kw OR s.SupplierName LIKE @kw)";
            if (dateFrom.HasValue)
                sql += " AND pi.ExpectedPaymentDate >= @dateFrom";

            sql += " ORDER BY pi.ExpectedPaymentDate ASC";

            using var cmd = new MySqlCommand(sql, conn);
            if (!string.IsNullOrEmpty(status))
                cmd.Parameters.AddWithValue("@status",   status);
            if (!string.IsNullOrEmpty(keyword))
                cmd.Parameters.AddWithValue("@kw",       "%" + keyword + "%");
            if (dateFrom.HasValue)
                cmd.Parameters.AddWithValue("@dateFrom", dateFrom.Value);

            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new AccountPayableEntity
                {
                    PurInvoiceID  = r.GetString("PurInvoiceID"),
                    PurchaseID    = r.GetString("PurchaseID"),
                    SupplierName  = r.GetString("SupplierName"),
                    TotalAmount   = r.GetDouble("TotalAmount"),
                    PaymentStatus = r.GetString("PaymentStatus"),
                    ExpectedDate  = r.GetDateTime("ExpectedPaymentDate"),
                    IsOverdue     = r.GetInt32("IsOverdue") == 1
                });
            return list;
        }
    }
}
