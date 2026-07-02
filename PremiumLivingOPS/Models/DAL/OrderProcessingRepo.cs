using MySql.Data.MySqlClient;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.DAL
{
    /// <summary>
    /// Repository (DAL layer) for Order Processing module.
    /// All methods use parameterised queries via DatabaseHelper.
    /// Schema reference: Database/schema.sql
    /// </summary>
    public class OrderProcessingRepo
    {
        // ╔════════════════════════════════════════════════════════════════
        //  ORDER queries
        //  NOTE: ALL order queries exclude staging rows (OrderID LIKE 'STG-%').
        //        Staging rows are internal workaround rows; they must never
        //        appear in order lists or Modify guards.
        // ╔════════════════════════════════════════════════════════════════

        public List<OrderEntity> SearchOrders(
            string    status   = null,
            string    keyword  = null,
            DateTime? dateFrom = null,
            DateTime? dateTo   = null)
        {
            var list = new List<OrderEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                // Always exclude STG- staging rows from real order lists
                var sql =
                    @"SELECT o.OrderID, o.QuotationID, o.CustomerID, c.CustomerName,
                             o.AddressID, o.SalesID, s.StaffName AS SalesName,
                             o.IssuedTime, o.DeliveryDate, o.ShippingAddress, o.BillingAddress,
                             o.SubTotal, o.DiscountType, o.DiscountValue, o.DiscountAmount,
                             o.GrandTotal, o.OrderContactName, o.OrderStatus
                      FROM `Order` o
                      JOIN Customer c ON o.CustomerID = c.CustomerID
                      JOIN Staff    s ON o.SalesID    = s.StaffID
                      WHERE o.OrderID NOT LIKE 'STG-%'";

                if (!string.IsNullOrEmpty(status))
                    sql += " AND o.OrderStatus = @status";
                if (!string.IsNullOrEmpty(keyword))
                    sql += @" AND (o.OrderID          LIKE @kw
                                OR c.CustomerName     LIKE @kw
                                OR s.StaffName        LIKE @kw
                                OR o.OrderContactName LIKE @kw)";
                if (dateFrom.HasValue)
                    sql += " AND DATE(o.IssuedTime) >= @dateFrom";
                if (dateTo.HasValue)
                    sql += " AND DATE(o.IssuedTime) <= @dateTo";

                sql += " ORDER BY o.IssuedTime DESC";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(status))
                        cmd.Parameters.AddWithValue("@status",   status);
                    if (!string.IsNullOrEmpty(keyword))
                        cmd.Parameters.AddWithValue("@kw",       "%" + keyword + "%");
                    if (dateFrom.HasValue)
                        cmd.Parameters.AddWithValue("@dateFrom", dateFrom.Value.ToString("yyyy-MM-dd"));
                    if (dateTo.HasValue)
                        cmd.Parameters.AddWithValue("@dateTo",   dateTo.Value.ToString("yyyy-MM-dd"));

                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read()) list.Add(MapOrder(rdr));
                }
            }
            return list;
        }

        public List<OrderEntity> GetAllOrders()                      => SearchOrders();
        public List<OrderEntity> GetOrdersByStatus(string status)    => SearchOrders(status);

        public OrderEntity GetOrderById(string orderId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT o.OrderID, o.QuotationID, o.CustomerID, c.CustomerName,
                             o.AddressID, o.SalesID, s.StaffName AS SalesName,
                             o.IssuedTime, o.DeliveryDate, o.ShippingAddress, o.BillingAddress,
                             o.SubTotal, o.DiscountType, o.DiscountValue, o.DiscountAmount,
                             o.GrandTotal, o.OrderContactName, o.OrderStatus
                      FROM `Order` o
                      JOIN Customer c ON o.CustomerID = c.CustomerID
                      JOIN Staff    s ON o.SalesID    = s.StaffID
                      WHERE o.OrderID = @orderId";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@orderId", orderId);
                    using (var rdr = cmd.ExecuteReader())
                        if (rdr.Read()) return MapOrder(rdr);
                }
            }
            return null;
        }

        public List<string> GetOrderIdsByPrefix(string prefix)
        {
            var list = new List<string>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                // Exclude STG- rows so they don’t interfere with ID generation
                const string sql = "SELECT OrderID FROM `Order` WHERE OrderID LIKE @prefix AND OrderID NOT LIKE 'STG-%'";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@prefix", prefix + "%");
                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read()) list.Add(rdr.GetString(0));
                }
            }
            return list;
        }

        public List<OrderLineEntity> GetOrderLines(string orderId)
        {
            var list = new List<OrderLineEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT ol.OrderID, ol.ItemID, i.ItemName, ol.Quantity, ol.Price
                      FROM OrderLine ol
                      JOIN Item i ON ol.ItemID = i.ItemID
                      WHERE ol.OrderID = @orderId";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@orderId", orderId);
                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                            list.Add(new OrderLineEntity
                            {
                                OrderID  = rdr.GetString("OrderID"),
                                ItemID   = rdr.GetString("ItemID"),
                                ItemName = rdr.GetString("ItemName"),
                                Quantity = rdr.GetInt32("Quantity"),
                                Price    = Convert.ToDouble(rdr["Price"])
                            });
                }
            }
            return list;
        }

        /// <summary>
        /// Returns QuotationItemEntity rows synthesised from OrderLine data.
        /// Includes BOTH staging rows (STG-) and real converted Order rows so
        /// that Quotation Detail can always show the items regardless of state.
        /// </summary>
        public List<QuotationItemEntity> GetOrderLinesByQuotationId(string quotationId)
        {
            var list = new List<QuotationItemEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT ol.ItemID,
                             i.ItemName  AS ProductName,
                             SUM(ol.Quantity)         AS TotalQty,
                             AVG(ol.Price)            AS AvgPrice
                      FROM OrderLine ol
                      JOIN `Order`   o  ON ol.OrderID = o.OrderID
                      JOIN Item      i  ON ol.ItemID  = i.ItemID
                      WHERE o.QuotationID = @qid
                      GROUP BY ol.ItemID, i.ItemName
                      ORDER BY i.ItemName";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@qid", quotationId);
                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                            list.Add(new QuotationItemEntity
                            {
                                QuotationID     = quotationId,
                                ItemID          = rdr.GetString("ItemID"),
                                ProductName     = rdr.GetString("ProductName"),
                                Quantity        = Convert.ToInt32(rdr["TotalQty"]),
                                Unit            = "",
                                UnitPrice       = Convert.ToDouble(rdr["AvgPrice"]),
                                DiscountPercent = 0
                            });
                }
            }
            return list;
        }

        /// <summary>
        /// Checks whether a REAL (non-staging) Order references this QuotationID.
        /// Used by IsQuotationLinkedToOrder to decide whether Modify is allowed.
        /// Staging rows (OrderID LIKE 'STG-%') are explicitly excluded so a
        /// Pending quotation with only a staging row is NOT blocked from Modify.
        /// </summary>
        public bool HasRealOrderLinkedToQuotation(string quotationId)
        {
            if (string.IsNullOrEmpty(quotationId)) return false;
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT COUNT(*) FROM `Order`
                      WHERE QuotationID = @qid
                        AND OrderID NOT LIKE 'STG-%'";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@qid", quotationId);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        public bool CreateOrder(OrderEntity order)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"INSERT INTO `Order`
                        (OrderID, QuotationID, CustomerID, AddressID, SalesID,
                         IssuedTime, DeliveryDate, ShippingAddress, BillingAddress,
                         SubTotal, DiscountType, DiscountValue, DiscountAmount,
                         GrandTotal, OrderContactName, OrderStatus)
                      VALUES
                        (@OrderID, @QuotationID, @CustomerID, @AddressID, @SalesID,
                         @IssuedTime, @DeliveryDate, @ShippingAddress, @BillingAddress,
                         @SubTotal, @DiscountType, @DiscountValue, @DiscountAmount,
                         @GrandTotal, @OrderContactName, @OrderStatus)";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@OrderID",          order.OrderID);
                    cmd.Parameters.AddWithValue("@QuotationID",      string.IsNullOrEmpty(order.QuotationID)      ? (object)DBNull.Value : order.QuotationID);
                    cmd.Parameters.AddWithValue("@CustomerID",       order.CustomerID);
                    cmd.Parameters.AddWithValue("@AddressID",        string.IsNullOrEmpty(order.AddressID)        ? (object)DBNull.Value : order.AddressID);
                    cmd.Parameters.AddWithValue("@SalesID",          order.SalesID);
                    cmd.Parameters.AddWithValue("@IssuedTime",       order.IssuedTime);
                    cmd.Parameters.AddWithValue("@DeliveryDate",     order.DeliveryDate);
                    cmd.Parameters.AddWithValue("@ShippingAddress",  order.ShippingAddress);
                    cmd.Parameters.AddWithValue("@BillingAddress",   order.BillingAddress);
                    cmd.Parameters.AddWithValue("@SubTotal",         order.SubTotal);
                    cmd.Parameters.AddWithValue("@DiscountType",     string.IsNullOrEmpty(order.DiscountType)     ? (object)DBNull.Value : order.DiscountType);
                    cmd.Parameters.AddWithValue("@DiscountValue",    order.DiscountValue);
                    cmd.Parameters.AddWithValue("@DiscountAmount",   order.DiscountAmount);
                    cmd.Parameters.AddWithValue("@GrandTotal",       order.GrandTotal);
                    cmd.Parameters.AddWithValue("@OrderContactName", string.IsNullOrEmpty(order.OrderContactName) ? (object)DBNull.Value : order.OrderContactName);
                    cmd.Parameters.AddWithValue("@OrderStatus",      order.OrderStatus);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool CreateOrderLine(OrderLineEntity line)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    "INSERT INTO OrderLine (OrderID, ItemID, Quantity, Price) VALUES (@OrderID, @ItemID, @Qty, @Price)";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@OrderID", line.OrderID);
                    cmd.Parameters.AddWithValue("@ItemID",  line.ItemID);
                    cmd.Parameters.AddWithValue("@Qty",     line.Quantity);
                    cmd.Parameters.AddWithValue("@Price",   line.Price);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool UpdateOrder(OrderEntity order)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"UPDATE `Order` SET
                        DeliveryDate     = @DeliveryDate,
                        ShippingAddress  = @ShippingAddress,
                        BillingAddress   = @BillingAddress,
                        SubTotal         = @SubTotal,
                        DiscountType     = @DiscountType,
                        DiscountValue    = @DiscountValue,
                        DiscountAmount   = @DiscountAmount,
                        GrandTotal       = @GrandTotal,
                        OrderContactName = @OrderContactName,
                        OrderStatus      = @OrderStatus
                      WHERE OrderID = @OrderID";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@DeliveryDate",     order.DeliveryDate);
                    cmd.Parameters.AddWithValue("@ShippingAddress",  order.ShippingAddress);
                    cmd.Parameters.AddWithValue("@BillingAddress",   order.BillingAddress);
                    cmd.Parameters.AddWithValue("@SubTotal",         order.SubTotal);
                    cmd.Parameters.AddWithValue("@DiscountType",     string.IsNullOrEmpty(order.DiscountType) ? (object)DBNull.Value : order.DiscountType);
                    cmd.Parameters.AddWithValue("@DiscountValue",    order.DiscountValue);
                    cmd.Parameters.AddWithValue("@DiscountAmount",   order.DiscountAmount);
                    cmd.Parameters.AddWithValue("@GrandTotal",       order.GrandTotal);
                    cmd.Parameters.AddWithValue("@OrderContactName", order.OrderContactName);
                    cmd.Parameters.AddWithValue("@OrderStatus",      order.OrderStatus);
                    cmd.Parameters.AddWithValue("@OrderID",          order.OrderID);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool UpdateOrderStatus(string orderId, string newStatus)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql = "UPDATE `Order` SET OrderStatus = @status WHERE OrderID = @id";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@status", newStatus);
                    cmd.Parameters.AddWithValue("@id",     orderId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool ReplaceOrderLines(string orderId, List<OrderLineEntity> lines)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        using (var del = new MySqlCommand("DELETE FROM OrderLine WHERE OrderID = @id", conn, tx))
                        {
                            del.Parameters.AddWithValue("@id", orderId);
                            del.ExecuteNonQuery();
                        }
                        foreach (var l in lines)
                        {
                            using (var ins = new MySqlCommand(
                                "INSERT INTO OrderLine (OrderID,ItemID,Quantity,Price) VALUES (@oid,@iid,@qty,@price)",
                                conn, tx))
                            {
                                ins.Parameters.AddWithValue("@oid",   orderId);
                                ins.Parameters.AddWithValue("@iid",   l.ItemID);
                                ins.Parameters.AddWithValue("@qty",   l.Quantity);
                                ins.Parameters.AddWithValue("@price", l.Price);
                                ins.ExecuteNonQuery();
                            }
                        }
                        tx.Commit();
                        return true;
                    }
                    catch { tx.Rollback(); return false; }
                }
            }
        }

        // ── Quotation Item Staging helpers ────────────────────────────────
        // Schema has no QuotationItem table. Quotation items are persisted by
        // creating a shadow Order (OrderStatus = 'Pending', QuotationID = this
        // quotation) so they survive application restarts.
        //
        // Naming convention for staging OrderID:
        //   "STG-" + QuotationID  (e.g. "STG-QT-20260702-0001")
        //
        // IMPORTANT: every real-order query in this file includes
        //   AND o.OrderID NOT LIKE 'STG-%'
        // so staging rows are completely invisible to normal operations.
        // ─────────────────────────────────────────────────────────────────

        private static string StagingOrderId(string quotationId) => "STG-" + quotationId;

        /// <summary>
        /// Atomically creates (or replaces) a staging Order + OrderLine rows for
        /// the given Quotation so that items are persisted to the DB.
        /// </summary>
        public bool CreateStagingOrderForQuotation(
            string                    quotationId,
            string                    customerId,
            string                    salesStaffId,
            double                    totalAmount,
            List<QuotationItemEntity> items)
        {
            if (string.IsNullOrEmpty(quotationId) || string.IsNullOrEmpty(customerId)
                || string.IsNullOrEmpty(salesStaffId) || items == null || items.Count == 0)
                return false;

            string stagingId = StagingOrderId(quotationId);

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        using (var del1 = new MySqlCommand(
                            "DELETE FROM OrderLine WHERE OrderID = @sid", conn, tx))
                        {
                            del1.Parameters.AddWithValue("@sid", stagingId);
                            del1.ExecuteNonQuery();
                        }
                        using (var del2 = new MySqlCommand(
                            "DELETE FROM `Order` WHERE OrderID = @sid", conn, tx))
                        {
                            del2.Parameters.AddWithValue("@sid", stagingId);
                            del2.ExecuteNonQuery();
                        }

                        const string insOrder =
                            @"INSERT INTO `Order`
                                (OrderID, QuotationID, CustomerID, AddressID, SalesID,
                                 IssuedTime, DeliveryDate, ShippingAddress, BillingAddress,
                                 SubTotal, DiscountType, DiscountValue, DiscountAmount,
                                 GrandTotal, OrderContactName, OrderStatus)
                              VALUES
                                (@OrderID, @QuotationID, @CustomerID, NULL, @SalesID,
                                 @IssuedTime, @DeliveryDate, @ShippingAddress, @BillingAddress,
                                 @SubTotal, NULL, 0, 0,
                                 @GrandTotal, @OrderContactName, 'Pending')";

                        using (var ins = new MySqlCommand(insOrder, conn, tx))
                        {
                            ins.Parameters.AddWithValue("@OrderID",          stagingId);
                            ins.Parameters.AddWithValue("@QuotationID",      quotationId);
                            ins.Parameters.AddWithValue("@CustomerID",       customerId);
                            ins.Parameters.AddWithValue("@SalesID",          salesStaffId);
                            ins.Parameters.AddWithValue("@IssuedTime",       DateTime.Today.ToString("yyyy-MM-dd"));
                            ins.Parameters.AddWithValue("@DeliveryDate",     DateTime.Today.ToString("yyyy-MM-dd"));
                            ins.Parameters.AddWithValue("@ShippingAddress",  "[Quotation Staging]");
                            ins.Parameters.AddWithValue("@BillingAddress",   "[Quotation Staging]");
                            ins.Parameters.AddWithValue("@SubTotal",         totalAmount);
                            ins.Parameters.AddWithValue("@GrandTotal",       totalAmount);
                            ins.Parameters.AddWithValue("@OrderContactName", "[Quotation Staging]");
                            ins.ExecuteNonQuery();
                        }

                        const string insLine =
                            "INSERT INTO OrderLine (OrderID, ItemID, Quantity, Price) VALUES (@oid, @iid, @qty, @price)";
                        foreach (var item in items)
                        {
                            using (var insL = new MySqlCommand(insLine, conn, tx))
                            {
                                insL.Parameters.AddWithValue("@oid",   stagingId);
                                insL.Parameters.AddWithValue("@iid",   item.ItemID);
                                insL.Parameters.AddWithValue("@qty",   item.Quantity);
                                insL.Parameters.AddWithValue("@price", item.UnitPrice);
                                insL.ExecuteNonQuery();
                            }
                        }

                        tx.Commit();
                        return true;
                    }
                    catch { tx.Rollback(); return false; }
                }
            }
        }

        /// <summary>
        /// Removes the staging Order + its OrderLine rows.
        /// Called when the Quotation is converted to a real Order.
        /// </summary>
        public bool DeleteStagingOrderByQuotationId(string quotationId)
        {
            if (string.IsNullOrEmpty(quotationId)) return false;
            string stagingId = StagingOrderId(quotationId);

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        using (var del1 = new MySqlCommand(
                            "DELETE FROM OrderLine WHERE OrderID = @sid", conn, tx))
                        {
                            del1.Parameters.AddWithValue("@sid", stagingId);
                            del1.ExecuteNonQuery();
                        }
                        using (var del2 = new MySqlCommand(
                            "DELETE FROM `Order` WHERE OrderID = @sid", conn, tx))
                        {
                            del2.Parameters.AddWithValue("@sid", stagingId);
                            del2.ExecuteNonQuery();
                        }
                        tx.Commit();
                        return true;
                    }
                    catch { tx.Rollback(); return false; }
                }
            }
        }

        // ╔════════════════════════════════════════════════════════════════
        //  QUOTATION queries
        // ╔════════════════════════════════════════════════════════════════

        public List<QuotationEntity> GetAllQuotations()
        {
            var list = new List<QuotationEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT q.QuotationID, q.CustomerID, c.CustomerName,
                             q.ExpiryDate, q.TotalAmount, q.DepositRequired,
                             q.LeadTimeEstimated, q.TermsandCondition, q.QuotationStatus
                      FROM Quotation q
                      JOIN Customer c ON q.CustomerID = c.CustomerID
                      ORDER BY q.ExpiryDate DESC";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read()) list.Add(MapQuotation(rdr));
            }
            return list;
        }

        public QuotationEntity GetQuotationById(string quotationId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT q.QuotationID, q.CustomerID, c.CustomerName,
                             q.ExpiryDate, q.TotalAmount, q.DepositRequired,
                             q.LeadTimeEstimated, q.TermsandCondition, q.QuotationStatus
                      FROM Quotation q
                      JOIN Customer c ON q.CustomerID = c.CustomerID
                      WHERE q.QuotationID = @qid";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@qid", quotationId);
                    using (var rdr = cmd.ExecuteReader())
                        if (rdr.Read()) return MapQuotation(rdr);
                }
            }
            return null;
        }

        public bool UpdateQuotationStatus(string quotationId, string newStatus)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql = "UPDATE Quotation SET QuotationStatus = @status WHERE QuotationID = @id";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@status", newStatus);
                    cmd.Parameters.AddWithValue("@id",     quotationId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool UpdateQuotationTotalAmount(string quotationId, double newTotal)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql = "UPDATE Quotation SET TotalAmount = @total WHERE QuotationID = @id";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@total", newTotal);
                    cmd.Parameters.AddWithValue("@id",    quotationId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public List<string> GetQuotationIdsByPrefix(string prefix)
        {
            var list = new List<string>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql = "SELECT QuotationID FROM Quotation WHERE QuotationID LIKE @prefix";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@prefix", prefix + "%");
                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read()) list.Add(rdr.GetString(0));
                }
            }
            return list;
        }

        public bool CreateQuotation(QuotationEntity q)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"INSERT INTO Quotation
                        (QuotationID, CustomerID, ExpiryDate,
                         TotalAmount, DepositRequired, LeadTimeEstimated,
                         TermsandCondition, QuotationStatus)
                      VALUES
                        (@QuotationID, @CustomerID, @ExpiryDate,
                         @TotalAmount, @DepositRequired, @LeadTimeEstimated,
                         @TermsandCondition, @QuotationStatus)";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@QuotationID",       q.QuotationID);
                    cmd.Parameters.AddWithValue("@CustomerID",        q.CustomerID);
                    cmd.Parameters.AddWithValue("@ExpiryDate",        q.ExpiryDate.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@TotalAmount",       q.TotalAmount);
                    cmd.Parameters.AddWithValue("@DepositRequired",   q.DepositRequired);
                    cmd.Parameters.AddWithValue("@LeadTimeEstimated", string.IsNullOrEmpty(q.LeadTimeEstimated) ? (object)DBNull.Value : q.LeadTimeEstimated);
                    cmd.Parameters.AddWithValue("@TermsandCondition", string.IsNullOrEmpty(q.TermsandCondition) ? (object)DBNull.Value : q.TermsandCondition);
                    cmd.Parameters.AddWithValue("@QuotationStatus",   q.QuotationStatus ?? "Pending");
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        // ╔════════════════════════════════════════════════════════════════
        //  LOOKUP queries
        // ╔════════════════════════════════════════════════════════════════

        public List<CustomerEntity> GetAllCustomers()
        {
            var list = new List<CustomerEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    "SELECT CustomerID, CustomerName, EmailAddress, PhoneNumber FROM Customer ORDER BY CustomerName";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        list.Add(new CustomerEntity
                        {
                            CustomerID   = rdr.GetString("CustomerID"),
                            CustomerName = rdr.GetString("CustomerName"),
                            EmailAddress = rdr.IsDBNull(rdr.GetOrdinal("EmailAddress")) ? "" : rdr.GetString("EmailAddress"),
                            PhoneNumber  = rdr.IsDBNull(rdr.GetOrdinal("PhoneNumber"))  ? "" : rdr.GetString("PhoneNumber")
                        });
            }
            return list;
        }

        public List<AddressLookup> GetAllAddresses()
        {
            var list = new List<AddressLookup>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    "SELECT AddressID, CustomerID, AddressName, AddressType, isDefault FROM Address ORDER BY CustomerID";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        list.Add(new AddressLookup
                        {
                            AddressId   = rdr.GetString("AddressID"),
                            CustomerId  = rdr.GetString("CustomerID"),
                            FullAddress = rdr.GetString("AddressName"),
                            Label       = rdr.IsDBNull(rdr.GetOrdinal("AddressType")) ? "" : rdr.GetString("AddressType"),
                            IsDefault   = !rdr.IsDBNull(rdr.GetOrdinal("isDefault")) && Convert.ToBoolean(rdr["isDefault"])
                        });
            }
            return list;
        }

        public List<ProductLookup> GetAllProducts()
        {
            var list = new List<ProductLookup>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT i.ItemID, i.ItemName, p.SalesPrice, p.Category
                      FROM Item i
                      JOIN Product p ON i.ItemID = p.ItemID
                      ORDER BY p.Category, i.ItemName";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        list.Add(new ProductLookup
                        {
                            ItemID     = rdr.GetString("ItemID"),
                            ItemName   = rdr.GetString("ItemName"),
                            SalesPrice = Convert.ToDouble(rdr["SalesPrice"]),
                            Category   = rdr.IsDBNull(rdr.GetOrdinal("Category")) ? "" : rdr.GetString("Category")
                        });
            }
            return list;
        }

        // ╔════════════════════════════════════════════════════════════════
        //  MAPPING helpers (private)
        // ╔════════════════════════════════════════════════════════════════

        private static double ToDouble(MySqlDataReader r, string col)
            => r.IsDBNull(r.GetOrdinal(col)) ? 0.0 : Convert.ToDouble(r[col]);

        private static OrderEntity MapOrder(MySqlDataReader r) => new OrderEntity
        {
            OrderID          = r.GetString("OrderID"),
            QuotationID      = r.IsDBNull(r.GetOrdinal("QuotationID"))      ? null : r.GetString("QuotationID"),
            CustomerID       = r.GetString("CustomerID"),
            CustomerName     = r.GetString("CustomerName"),
            AddressID        = r.IsDBNull(r.GetOrdinal("AddressID"))        ? null : r.GetString("AddressID"),
            SalesID          = r.GetString("SalesID"),
            SalesName        = r.GetString("SalesName"),
            IssuedTime       = r.GetDateTime("IssuedTime"),
            DeliveryDate     = r.GetDateTime("DeliveryDate"),
            ShippingAddress  = r.IsDBNull(r.GetOrdinal("ShippingAddress"))  ? null : r.GetString("ShippingAddress"),
            BillingAddress   = r.IsDBNull(r.GetOrdinal("BillingAddress"))   ? null : r.GetString("BillingAddress"),
            SubTotal         = ToDouble(r, "SubTotal"),
            DiscountType     = r.IsDBNull(r.GetOrdinal("DiscountType"))     ? null : r.GetString("DiscountType"),
            DiscountValue    = ToDouble(r, "DiscountValue"),
            DiscountAmount   = ToDouble(r, "DiscountAmount"),
            GrandTotal       = ToDouble(r, "GrandTotal"),
            OrderContactName = r.IsDBNull(r.GetOrdinal("OrderContactName")) ? null : r.GetString("OrderContactName"),
            OrderStatus      = r.GetString("OrderStatus")
        };

        private static QuotationEntity MapQuotation(MySqlDataReader r) => new QuotationEntity
        {
            QuotationID       = r.GetString("QuotationID"),
            CustomerID        = r.GetString("CustomerID"),
            CustomerName      = r.GetString("CustomerName"),
            ExpiryDate        = r.GetDateTime("ExpiryDate"),
            TotalAmount       = ToDouble(r, "TotalAmount"),
            DepositRequired   = ToDouble(r, "DepositRequired"),
            LeadTimeEstimated = r.IsDBNull(r.GetOrdinal("LeadTimeEstimated")) ? null : r.GetString("LeadTimeEstimated"),
            TermsandCondition = r.IsDBNull(r.GetOrdinal("TermsandCondition")) ? null : r.GetString("TermsandCondition"),
            QuotationStatus   = r.GetString("QuotationStatus")
        };
    }
}
