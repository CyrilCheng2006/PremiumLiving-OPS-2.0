using MySql.Data.MySqlClient;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.DAL
{
    /// <summary>
    /// Repository (DAL layer) for Order Processing module.
    /// All methods use parameterised queries via DatabaseHelper.
    /// </summary>
    public class OrderProcessingRepo
    {
        // ════════════════════════════════════════════════════════════════
        //  ORDER queries
        // ════════════════════════════════════════════════════════════════

        public List<OrderEntity> SearchOrders(
            string   status   = null,
            string   keyword  = null,
            DateTime? dateFrom = null,
            DateTime? dateTo   = null)
        {
            var list = new List<OrderEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var sql =
                    @"SELECT o.OrderID, o.QuotationID, o.CustomerID, c.CustomerName,
                             o.AddressID, o.SalesID, s.StaffName AS SalesName,
                             o.IssuedTime, o.DeliveryDate, o.ShippingAddress, o.BillingAddress,
                             o.SubTotal, o.DiscountType, o.DiscountValue, o.DiscountAmount,
                             o.GrandTotal, o.OrderContactName, o.OrderStatus
                      FROM `Order` o
                      JOIN Customer c ON o.CustomerID = c.CustomerID
                      JOIN Staff   s ON o.SalesID     = s.StaffID
                      WHERE 1=1";

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

        public List<OrderEntity> GetAllOrders() => SearchOrders();
        public List<OrderEntity> GetOrdersByStatus(string status) => SearchOrders(status);

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
                      JOIN Staff   s ON o.SalesID     = s.StaffID
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
                const string sql = "SELECT OrderID FROM `Order` WHERE OrderID LIKE @prefix";
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

        // ════════════════════════════════════════════════════════════════
        //  QUOTATION queries
        // ════════════════════════════════════════════════════════════════

        public List<QuotationEntity> GetAllQuotations()
        {
            var list = new List<QuotationEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT q.QuotationID, q.CustomerID, c.CustomerName,
                             q.IssuedDate, q.ExpiryDate,
                             q.TotalAmount, q.DepositRequired, q.LeadTimeEstimated,
                             q.TermsandCondition, q.QuotationStatus,
                             s.StaffName AS SalesStaffName, q.Notes
                      FROM Quotation q
                      JOIN Customer c ON q.CustomerID   = c.CustomerID
                      JOIN Staff    s ON q.SalesStaffID = s.StaffID
                      ORDER BY q.IssuedDate DESC";
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
                             q.IssuedDate, q.ExpiryDate,
                             q.TotalAmount, q.DepositRequired, q.LeadTimeEstimated,
                             q.TermsandCondition, q.QuotationStatus,
                             s.StaffName AS SalesStaffName, q.Notes
                      FROM Quotation q
                      JOIN Customer c ON q.CustomerID   = c.CustomerID
                      JOIN Staff    s ON q.SalesStaffID = s.StaffID
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

        public List<QuotationItemEntity> GetQuotationItems(string quotationId)
        {
            var list = new List<QuotationItemEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT qi.QuotationID, qi.ItemID, i.ItemName AS ProductName,
                             qi.Quantity, qi.Unit, qi.UnitPrice, qi.DiscountPercent, qi.ItemNote
                      FROM QuotationItem qi
                      JOIN Item i ON qi.ItemID = i.ItemID
                      WHERE qi.QuotationID = @qid";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@qid", quotationId);
                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                            list.Add(new QuotationItemEntity
                            {
                                QuotationID     = rdr.GetString("QuotationID"),
                                ItemID          = rdr.GetString("ItemID"),
                                ProductName     = rdr.GetString("ProductName"),
                                Quantity        = rdr.GetInt32("Quantity"),
                                Unit            = rdr.IsDBNull(rdr.GetOrdinal("Unit"))           ? "" : rdr.GetString("Unit"),
                                UnitPrice       = Convert.ToDouble(rdr["UnitPrice"]),
                                DiscountPercent = rdr.IsDBNull(rdr.GetOrdinal("DiscountPercent")) ? 0  : Convert.ToDouble(rdr["DiscountPercent"]),
                                ItemNote        = rdr.IsDBNull(rdr.GetOrdinal("ItemNote"))        ? "" : rdr.GetString("ItemNote")
                            });
                }
            }
            return list;
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

        public bool CreateQuotation(QuotationEntity q, string salesStaffId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"INSERT INTO Quotation
                        (QuotationID, CustomerID, SalesStaffID,
                         IssuedDate, ExpiryDate,
                         TotalAmount, DepositRequired, LeadTimeEstimated,
                         TermsandCondition, QuotationStatus, Notes)
                      VALUES
                        (@QuotationID, @CustomerID, @SalesStaffID,
                         @IssuedDate, @ExpiryDate,
                         @TotalAmount, @DepositRequired, @LeadTimeEstimated,
                         @TermsandCondition, @QuotationStatus, @Notes)";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@QuotationID",       q.QuotationID);
                    cmd.Parameters.AddWithValue("@CustomerID",        q.CustomerID);
                    cmd.Parameters.AddWithValue("@SalesStaffID",      salesStaffId);
                    cmd.Parameters.AddWithValue("@IssuedDate",        q.IssuedDate.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@ExpiryDate",        q.ExpiryDate.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@TotalAmount",       q.TotalAmount);
                    cmd.Parameters.AddWithValue("@DepositRequired",   q.DepositRequired);
                    cmd.Parameters.AddWithValue("@LeadTimeEstimated", string.IsNullOrEmpty(q.LeadTimeEstimated) ? (object)DBNull.Value : q.LeadTimeEstimated);
                    cmd.Parameters.AddWithValue("@TermsandCondition", string.IsNullOrEmpty(q.TermsandCondition) ? (object)DBNull.Value : q.TermsandCondition);
                    cmd.Parameters.AddWithValue("@QuotationStatus",   q.QuotationStatus ?? "Pending");
                    cmd.Parameters.AddWithValue("@Notes",             string.IsNullOrEmpty(q.Notes)             ? (object)DBNull.Value : q.Notes);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool CreateQuotationItem(QuotationItemEntity item)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"INSERT INTO QuotationItem
                        (QuotationID, ItemID, Quantity, Unit, UnitPrice, DiscountPercent, ItemNote)
                      VALUES
                        (@QuotationID, @ItemID, @Quantity, @Unit, @UnitPrice, @DiscountPercent, @ItemNote)";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@QuotationID",     item.QuotationID);
                    cmd.Parameters.AddWithValue("@ItemID",          item.ItemID);
                    cmd.Parameters.AddWithValue("@Quantity",        item.Quantity);
                    cmd.Parameters.AddWithValue("@Unit",            string.IsNullOrEmpty(item.Unit)     ? (object)DBNull.Value : item.Unit);
                    cmd.Parameters.AddWithValue("@UnitPrice",       item.UnitPrice);
                    cmd.Parameters.AddWithValue("@DiscountPercent", item.DiscountPercent);
                    cmd.Parameters.AddWithValue("@ItemNote",        string.IsNullOrEmpty(item.ItemNote) ? (object)DBNull.Value : item.ItemNote);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  LOOKUP queries  (shared by Order + Quotation forms)
        // ════════════════════════════════════════════════════════════════

        public List<CustomerEntity> GetAllCustomers()
        {
            var list = new List<CustomerEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    "SELECT CustomerID, CustomerName, ContactPhone, ContactEmail FROM Customer ORDER BY CustomerName";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        list.Add(new CustomerEntity
                        {
                            CustomerID   = rdr.GetString("CustomerID"),
                            CustomerName = rdr.GetString("CustomerName"),
                            ContactPhone = rdr.IsDBNull(rdr.GetOrdinal("ContactPhone")) ? "" : rdr.GetString("ContactPhone"),
                            ContactEmail = rdr.IsDBNull(rdr.GetOrdinal("ContactEmail")) ? "" : rdr.GetString("ContactEmail")
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
                    "SELECT ItemID, ItemName, SalesPrice, Category FROM Item ORDER BY Category, ItemName";
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

        // ════════════════════════════════════════════════════════════════
        //  MAPPING helpers (private)
        // ════════════════════════════════════════════════════════════════

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
            SubTotal         = Convert.ToDouble(r["SubTotal"]),
            DiscountType     = r.IsDBNull(r.GetOrdinal("DiscountType"))     ? null : r.GetString("DiscountType"),
            DiscountValue    = Convert.ToDouble(r["DiscountValue"]),
            DiscountAmount   = Convert.ToDouble(r["DiscountAmount"]),
            GrandTotal       = Convert.ToDouble(r["GrandTotal"]),
            OrderContactName = r.IsDBNull(r.GetOrdinal("OrderContactName")) ? null : r.GetString("OrderContactName"),
            OrderStatus      = r.GetString("OrderStatus")
        };

        private static QuotationEntity MapQuotation(MySqlDataReader r) => new QuotationEntity
        {
            QuotationID       = r.GetString("QuotationID"),
            CustomerID        = r.GetString("CustomerID"),
            CustomerName      = r.GetString("CustomerName"),
            IssuedDate        = r.GetDateTime("IssuedDate"),
            ExpiryDate        = r.GetDateTime("ExpiryDate"),
            TotalAmount       = Convert.ToDouble(r["TotalAmount"]),
            DepositRequired   = Convert.ToDouble(r["DepositRequired"]),
            LeadTimeEstimated = r.IsDBNull(r.GetOrdinal("LeadTimeEstimated")) ? null : r.GetString("LeadTimeEstimated"),
            TermsandCondition = r.IsDBNull(r.GetOrdinal("TermsandCondition")) ? null : r.GetString("TermsandCondition"),
            QuotationStatus   = r.GetString("QuotationStatus"),
            SalesStaffName    = r.IsDBNull(r.GetOrdinal("SalesStaffName"))    ? null : r.GetString("SalesStaffName"),
            Notes             = r.IsDBNull(r.GetOrdinal("Notes"))             ? null : r.GetString("Notes")
        };
    }
}
