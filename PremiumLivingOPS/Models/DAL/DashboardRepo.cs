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
    /// All table names are backtick-quoted so MySQL case-sensitivity on
    /// Linux/macOS hosts (lower_case_table_names = 0) does not cause errors.
    ///
    /// Column mapping (verified against Database/schema.sql):
    ///   Order      : GrandTotal (not TotalAmount), IssuedTime (not OrderDate)
    ///   Quotation  : ExpiryDate (not ValidUntil)
    ///   Shipment   : ShipmentID, ShipDate, ShipmentStatus  (no Delivery table)
    ///   WarehouseItem: WarehouseItemQuantity, ReorderLevel  (no InventoryItem table)
    ///   Outstanding AR: Invoice.RemainingBalance WHERE PaymentStatus = 'Partial'
    ///   Supplier payments: PurchaseInvoice JOIN PurchaseOrder JOIN Supplier
    ///   Active suppliers: Supplier (no SupplierStatus column — COUNT all)
    /// </summary>
    public class DashboardRepo
    {
        // ── Orders ───────────────────────────────────────────────────

        /// <summary>Returns the <paramref name="top"/> most recent orders.</summary>
        public List<OrderSummaryRow> GetRecentOrders(int top = 5)
        {
            var list = new List<OrderSummaryRow>();
            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                // GrandTotal replaces TotalAmount; IssuedTime replaces OrderDate
                string sql =
                    "SELECT o.OrderID, c.CustomerName, o.GrandTotal, o.OrderStatus " +
                    "FROM `Order` o " +
                    "JOIN `Customer` c ON o.CustomerID = c.CustomerID " +
                    "ORDER BY o.IssuedTime DESC " +
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
                                Total    = r.GetDouble("GrandTotal").ToString("N0"),
                                Status   = r.GetString("OrderStatus")
                            });
                }
            }
            return list;
        }

        /// <summary>Returns a count per OrderStatus value for the current month.</summary>
        public Dictionary<string, int> GetOrderStatusCounts()
        {
            var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                // IssuedTime replaces OrderDate; filter to current month for the KPI
                string sql =
                    "SELECT OrderStatus, COUNT(*) AS Cnt " +
                    "FROM `Order` " +
                    "WHERE MONTH(IssuedTime) = MONTH(CURDATE()) " +
                    "  AND YEAR(IssuedTime)  = YEAR(CURDATE()) " +
                    "GROUP BY OrderStatus";
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                using (MySqlDataReader r = cmd.ExecuteReader())
                    while (r.Read())
                        dict[r.GetString("OrderStatus")] = r.GetInt32("Cnt");
            }
            return dict;
        }

        // ── Quotations ───────────────────────────────────────────────

        /// <summary>Returns up to <paramref name="top"/> pending quotations, soonest expiry first.</summary>
        public List<QuotationSummaryRow> GetPendingQuotations(int top = 5)
        {
            var list = new List<QuotationSummaryRow>();
            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                // ExpiryDate replaces ValidUntil (schema column name)
                string sql =
                    "SELECT q.QuotationID, c.CustomerName, q.TotalAmount, q.ExpiryDate " +
                    "FROM `Quotation` q " +
                    "JOIN `Customer` c ON q.CustomerID = c.CustomerID " +
                    "WHERE q.QuotationStatus = 'Pending' " +
                    "ORDER BY q.ExpiryDate ASC " +
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
                                Amount      = r.GetDouble("TotalAmount").ToString("N0"),
                                ValidUntil  = r.GetDateTime("ExpiryDate").ToString("d MMM yyyy")
                            });
                }
            }
            return list;
        }

        // ── Active Shipments ─────────────────────────────────────────
        // Schema: Shipment table (ShipmentID, OrderID, ShipDate, ShipmentStatus)
        // There is no "Delivery" table — deliveries are tracked via DeliveryNote
        // which links to Shipment.  We show Shipments in Pending / In Transit.

        /// <summary>Returns up to <paramref name="top"/> active shipments (Pending or In Transit).</summary>
        public List<ShipmentSummaryRow> GetActiveShipments(int top = 5)
        {
            var list = new List<ShipmentSummaryRow>();
            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql =
                    "SELECT s.ShipmentID, c.CustomerName, s.ShipDate, s.ShipmentStatus " +
                    "FROM `Shipment` s " +
                    "JOIN `Order`    o ON s.OrderID    = o.OrderID " +
                    "JOIN `Customer` c ON o.CustomerID = c.CustomerID " +
                    "WHERE s.ShipmentStatus IN ('Pending','In Transit') " +
                    "ORDER BY s.ShipDate ASC " +
                    "LIMIT @top";
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@top", top);
                    using (MySqlDataReader r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new ShipmentSummaryRow
                            {
                                ShipmentId = r.GetString("ShipmentID"),
                                Customer   = r.GetString("CustomerName"),
                                SchedDate  = r.GetDateTime("ShipDate").ToString("d MMM yyyy"),
                                Status     = r.GetString("ShipmentStatus")
                            });
                }
            }
            return list;
        }

        // ── Supplier Payments ────────────────────────────────────────
        // Schema: PurchaseInvoice (PurInvoiceID, PurchaseID, TotalAmount, PaymentStatus, ExpectedDate)
        //         PurchaseOrder   (PurchaseID, SupplierID, …)
        //         Supplier        (SupplierID, SupplierName, …)
        // PaymentStatus ENUM: 'Partial' | 'Full'
        // We surface 'Partial' as "Pending" and overdue (ExpectedDate < TODAY) as "Overdue".

        /// <summary>Returns up to <paramref name="top"/> supplier purchase invoices, overdue first.</summary>
        public List<SupplierPaymentRow> GetSupplierPayments(int top = 5)
        {
            var list = new List<SupplierPaymentRow>();
            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql =
                    "SELECT s.SupplierName, " +
                    "       pi.PurInvoiceID, " +
                    "       pi.TotalAmount, " +
                    "       CASE " +
                    "           WHEN pi.PaymentStatus = 'Full'                    THEN 'Paid' " +
                    "           WHEN pi.ExpectedDate  < CURDATE()                 THEN 'Overdue' " +
                    "           ELSE 'Pending' " +
                    "       END AS DerivedStatus " +
                    "FROM `PurchaseInvoice` pi " +
                    "JOIN `PurchaseOrder`   po ON pi.PurchaseID  = po.PurchaseID " +
                    "JOIN `Supplier`        s  ON po.SupplierID  = s.SupplierID " +
                    "ORDER BY FIELD(DerivedStatus,'Overdue','Pending','Paid'), pi.ExpectedDate ASC " +
                    "LIMIT @top";
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@top", top);
                    using (MySqlDataReader r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new SupplierPaymentRow
                            {
                                Supplier  = r.GetString("SupplierName"),
                                InvoiceId = r.GetString("PurInvoiceID"),
                                Amount    = r.GetDouble("TotalAmount").ToString("N0"),
                                Status    = r.GetString("DerivedStatus")
                            });
                }
            }
            return list;
        }

        // ── Low Stock ────────────────────────────────────────────────
        // Schema: WarehouseItem (WarehouseItemQuantity, ReorderLevel)
        //         Item          (ItemName)
        // "Low stock" = WarehouseItemQuantity < ReorderLevel.
        // We aggregate per item across all warehouses to get total on-hand.

        /// <summary>Returns all items where total on-hand quantity is below the reorder level.</summary>
        public List<LowStockRow> GetLowStockItems()
        {
            var list = new List<LowStockRow>();
            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql =
                    "SELECT i.ItemName, " +
                    "       SUM(wi.WarehouseItemQuantity) AS TotalOnHand, " +
                    "       MIN(wi.ReorderLevel)          AS MinReorder " +
                    "FROM `WarehouseItem` wi " +
                    "JOIN `Item`          i  ON wi.ItemID = i.ItemID " +
                    "GROUP BY wi.ItemID, i.ItemName " +
                    "HAVING TotalOnHand < MinReorder " +
                    "ORDER BY (TotalOnHand / MinReorder) ASC";
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                using (MySqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        int onHand = Convert.ToInt32(r["TotalOnHand"]);
                        int minQty = Convert.ToInt32(r["MinReorder"]);
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

        /// <summary>
        /// Monthly revenue = sum of GrandTotal for Delivered orders issued this month.
        /// </summary>
        public decimal GetMonthlyRevenue()
        {
            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                // GrandTotal replaces TotalAmount; IssuedTime replaces OrderDate
                string sql =
                    "SELECT IFNULL(SUM(GrandTotal), 0) " +
                    "FROM `Order` " +
                    "WHERE OrderStatus IN ('Delivered','Completed') " +
                    "  AND MONTH(IssuedTime) = MONTH(CURDATE()) " +
                    "  AND YEAR(IssuedTime)  = YEAR(CURDATE())";
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    return Convert.ToDecimal(cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Outstanding AR = sum of RemainingBalance on customer invoices not fully paid.
        /// Schema: Invoice (RemainingBalance, PaymentStatus ENUM 'Partial'|'Full').
        /// </summary>
        public decimal GetOutstandingAR()
        {
            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql =
                    "SELECT IFNULL(SUM(RemainingBalance), 0) " +
                    "FROM `Invoice` " +
                    "WHERE PaymentStatus = 'Partial'";
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    return Convert.ToDecimal(cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Active supplier count.
        /// Schema: Supplier has no SupplierStatus column — count all rows.
        /// </summary>
        public int GetActiveSupplierCount()
        {
            using (MySqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql = "SELECT COUNT(*) FROM `Supplier`";
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        /// <summary>Total number of customers in the system.</summary>
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
