using MySql.Data.MySqlClient;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.DAL
{
    /// <summary>
    /// Data-access layer for the Dashboard screen.
    /// Each method issues one focused SQL query and returns typed entity lists.
    /// No business logic or formatting lives here — that is the Controller's job.
    ///
    /// Table names are always backtick-quoted so MySQL case-sensitivity on
    /// Linux/macOS hosts does not cause "table doesn't exist" errors.
    /// </summary>
    public class DashboardRepo
    {
        // ── Orders ───────────────────────────────────────────────────

        public List<OrderSummaryRow> GetRecentOrders(int top = 5)
        {
            var list = new List<OrderSummaryRow>();
            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql =
                    "SELECT o.OrderID, c.CustomerName, o.TotalAmount, o.OrderStatus " +
                    "FROM `Order` o " +
                    "JOIN `Customer` c ON o.CustomerID = c.CustomerID " +
                    "ORDER BY o.OrderDate DESC " +
                    "LIMIT @top";
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@top", top);
                    using (MySqlDataReader r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new OrderSummaryRow
                            {
                                OrderId  = r.GetString("OrderID"),
                                Customer = r.GetString("CustomerName"),
                                Total    = r.GetDecimal("TotalAmount").ToString("N0"),
                                Status   = r.GetString("OrderStatus")
                            });
                }
            }
            return list;
        }

        public Dictionary<string, int> GetOrderStatusCounts()
        {
            var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql = "SELECT OrderStatus, COUNT(*) AS Cnt FROM `Order` GROUP BY OrderStatus";
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                using (MySqlDataReader r = cmd.ExecuteReader())
                    while (r.Read())
                        dict[r.GetString("OrderStatus")] = r.GetInt32("Cnt");
            }
            return dict;
        }

        // ── Quotations ───────────────────────────────────────────────

        public List<QuotationSummaryRow> GetPendingQuotations(int top = 5)
        {
            var list = new List<QuotationSummaryRow>();
            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql =
                    "SELECT q.QuotationID, c.CustomerName, q.TotalAmount, q.ValidUntil " +
                    "FROM `Quotation` q " +
                    "JOIN `Customer` c ON q.CustomerID = c.CustomerID " +
                    "WHERE q.QuotationStatus = 'Pending' " +
                    "ORDER BY q.ValidUntil ASC " +
                    "LIMIT @top";
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@top", top);
                    using (MySqlDataReader r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new QuotationSummaryRow
                            {
                                QuotationId = r.GetString("QuotationID"),
                                Customer    = r.GetString("CustomerName"),
                                Amount      = r.GetDecimal("TotalAmount").ToString("N0"),
                                ValidUntil  = r.GetDateTime("ValidUntil").ToString("d MMM yyyy")
                            });
                }
            }
            return list;
        }

        // ── Shipments ────────────────────────────────────────────────

        public List<ShipmentSummaryRow> GetActiveShipments(int top = 5)
        {
            var list = new List<ShipmentSummaryRow>();
            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql =
                    "SELECT d.DeliveryID, c.CustomerName, d.ScheduledDate, d.DeliveryStatus " +
                    "FROM `Delivery` d " +
                    "JOIN `Order` o  ON d.OrderID   = o.OrderID " +
                    "JOIN `Customer` c ON o.CustomerID = c.CustomerID " +
                    "WHERE d.DeliveryStatus IN ('Scheduled','In Transit') " +
                    "ORDER BY d.ScheduledDate ASC " +
                    "LIMIT @top";
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@top", top);
                    using (MySqlDataReader r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new ShipmentSummaryRow
                            {
                                ShipmentId = r.GetString("DeliveryID"),
                                Customer   = r.GetString("CustomerName"),
                                SchedDate  = r.GetDateTime("ScheduledDate").ToString("d MMM yyyy"),
                                Status     = r.GetString("DeliveryStatus")
                            });
                }
            }
            return list;
        }

        // ── Supplier Payments ────────────────────────────────────────

        public List<SupplierPaymentRow> GetSupplierPayments(int top = 5)
        {
            var list = new List<SupplierPaymentRow>();
            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql =
                    "SELECT s.SupplierName, sp.PurchaseOrderID, sp.TotalAmount, sp.PaymentStatus " +
                    "FROM `SupplierPayment` sp " +
                    "JOIN `Supplier` s ON sp.SupplierID = s.SupplierID " +
                    "ORDER BY FIELD(sp.PaymentStatus,'Overdue','Pending','Paid'), sp.DueDate ASC " +
                    "LIMIT @top";
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@top", top);
                    using (MySqlDataReader r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new SupplierPaymentRow
                            {
                                Supplier  = r.GetString("SupplierName"),
                                InvoiceId = r.GetString("PurchaseOrderID"),
                                Amount    = r.GetDecimal("TotalAmount").ToString("N0"),
                                Status    = r.GetString("PaymentStatus")
                            });
                }
            }
            return list;
        }

        // ── Low Stock ────────────────────────────────────────────────

        /// <summary>
        /// Returns inventory items where QuantityOnHand &lt; MinimumQuantity.
        /// The table is quoted with backticks to prevent MySQL case-sensitivity
        /// errors on Linux/macOS hosts (lower_case_table_names = 0).
        /// Actual table name verified against premiumlivingfurniture schema:
        ///   SHOW TABLES LIKE '%nventor%'  →  InventoryItem
        /// If the schema uses a different casing, update the backtick name below.
        /// </summary>
        public List<LowStockRow> GetLowStockItems()
        {
            var list = new List<LowStockRow>();
            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                // Use backtick-quoting so the exact identifier is sent to MySQL,
                // bypassing any client-side case folding.
                string sql =
                    "SELECT ItemName, QuantityOnHand, MinimumQuantity " +
                    "FROM `InventoryItem` " +
                    "WHERE QuantityOnHand < MinimumQuantity " +
                    "ORDER BY (QuantityOnHand / MinimumQuantity) ASC";
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                using (MySqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        int onHand = r.GetInt32("QuantityOnHand");
                        int minQty = r.GetInt32("MinimumQuantity");
                        list.Add(new LowStockRow
                        {
                            ItemName   = r.GetString("ItemName"),
                            OnHand     = onHand,
                            MinimumQty = minQty,
                            Status     = (onHand < minQty / 2) ? "Critical" : "Low"
                        });
                    }
                }
            }
            return list;
        }

        // ── Revenue / AR ─────────────────────────────────────────────

        public decimal GetMonthlyRevenue()
        {
            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql =
                    "SELECT IFNULL(SUM(TotalAmount), 0) " +
                    "FROM `Order` " +
                    "WHERE OrderStatus = 'Delivered' " +
                    "  AND MONTH(OrderDate) = MONTH(CURDATE()) " +
                    "  AND YEAR(OrderDate)  = YEAR(CURDATE())";
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    return Convert.ToDecimal(cmd.ExecuteScalar());
            }
        }

        public decimal GetOutstandingAR()
        {
            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql =
                    "SELECT IFNULL(SUM(TotalAmount), 0) " +
                    "FROM `SupplierPayment` " +
                    "WHERE PaymentStatus IN ('Pending','Overdue')";
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    return Convert.ToDecimal(cmd.ExecuteScalar());
            }
        }

        public int GetActiveSupplierCount()
        {
            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql = "SELECT COUNT(*) FROM `Supplier` WHERE SupplierStatus = 'Active'";
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public int GetCustomerCount()
        {
            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql = "SELECT COUNT(*) FROM `Customer`";
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
    }
}
