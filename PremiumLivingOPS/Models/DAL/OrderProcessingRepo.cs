using MySql.Data.MySqlClient;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.DAL
{
    /// <summary>
    /// Data-access layer for the Order Processing module.
    /// All SQL runs against the PremiumLiving schema.
    /// </summary>
    public class OrderProcessingRepo
    {
        // ── Connection string ───────────────────────────────────────────────────
        private readonly string _connStr = DBConfig.ConnectionString;

        // ═══════════════════════════════════════════════════
        // CUSTOMER
        // ═══════════════════════════════════════════════════

        public List<CustomerEntity> GetAllCustomers()
        {
            var list = new List<CustomerEntity>();
            const string sql = @"
                SELECT CustomerID, CustomerName, ContactPhone, ContactEmail,
                       Address, MemberTier, JoinDate
                FROM   Customer
                ORDER  BY CustomerName";
            using (var conn = new MySqlConnection(_connStr))
            using (var cmd  = new MySqlCommand(sql, conn))
            {
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        list.Add(new CustomerEntity
                        {
                            CustomerID   = rdr.GetString("CustomerID"),
                            CustomerName = rdr.GetString("CustomerName"),
                            ContactPhone = rdr["ContactPhone"] as string ?? string.Empty,  // L493 fix
                            ContactEmail = rdr["ContactEmail"] as string ?? string.Empty,  // L494 fix
                            Address      = rdr["Address"]      as string ?? string.Empty,
                            MemberTier   = rdr["MemberTier"]   as string ?? string.Empty,
                            JoinDate     = rdr["JoinDate"]     as string ?? string.Empty
                        });
            }
            return list;
        }

        public List<AddressLookup> GetAddressesByCustomer(string customerId)
        {
            var list = new List<AddressLookup>();
            const string sql = @"
                SELECT AddressID, CustomerID, AddressName, AddressType, isDefault
                FROM   Address
                WHERE  CustomerID = @cid
                ORDER  BY isDefault DESC, AddressName";
            using (var conn = new MySqlConnection(_connStr))
            using (var cmd  = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@cid", customerId);
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        list.Add(new AddressLookup
                        {
                            AddressId   = rdr.GetString("AddressID"),
                            CustomerId  = rdr.GetString("CustomerID"),
                            FullAddress = rdr["AddressName"] as string ?? string.Empty,
                            Label       = rdr["AddressType"] as string ?? string.Empty,
                            IsDefault   = Convert.ToBoolean(rdr["isDefault"])
                        });
            }
            return list;
        }

        // ═══════════════════════════════════════════════════
        // PRODUCT
        // ═══════════════════════════════════════════════════

        public List<ProductLookup> GetAllProducts()
        {
            var list = new List<ProductLookup>();
            const string sql = @"
                SELECT   i.ItemID, i.ItemName, i.SalesPrice, c.CategoryName
                FROM     Item i
                LEFT JOIN Category c ON c.CategoryID = i.CategoryID
                WHERE    i.IsActive = 1
                ORDER    BY i.ItemName";
            using (var conn = new MySqlConnection(_connStr))
            using (var cmd  = new MySqlCommand(sql, conn))
            {
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        list.Add(new ProductLookup
                        {
                            ItemID     = rdr.GetString("ItemID"),
                            ItemName   = rdr.GetString("ItemName"),
                            SalesPrice = Convert.ToDouble(rdr["SalesPrice"]),
                            Category   = rdr["CategoryName"] as string ?? string.Empty
                        });
            }
            return list;
        }

        // ═══════════════════════════════════════════════════
        // QUOTATION
        // ═══════════════════════════════════════════════════

        public List<QuotationEntity> GetAllQuotations()
        {
            var list = new List<QuotationEntity>();
            const string sql = @"
                SELECT q.QuotationID, q.CustomerID, c.CustomerName,
                       q.IssuedDate, q.ExpiryDate, q.TotalAmount,
                       q.DepositRequired, q.LeadTimeEstimated,
                       q.TermsandCondition, q.QuotationStatus,
                       s.StaffName AS SalesStaffName, q.Notes
                FROM   Quotation q
                JOIN   Customer  c ON c.CustomerID = q.CustomerID
                LEFT JOIN Staff  s ON s.StaffID    = q.SalesStaffID
                ORDER  BY q.IssuedDate DESC";
            using (var conn = new MySqlConnection(_connStr))
            using (var cmd  = new MySqlCommand(sql, conn))
            {
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        list.Add(MapQuotationRow(rdr));
            }
            return list;
        }

        public List<QuotationEntity> GetPendingQuotations()
        {
            var list = new List<QuotationEntity>();
            const string sql = @"
                SELECT q.QuotationID, q.CustomerID, c.CustomerName,
                       q.IssuedDate, q.ExpiryDate, q.TotalAmount,
                       q.DepositRequired, q.LeadTimeEstimated,
                       q.TermsandCondition, q.QuotationStatus,
                       s.StaffName AS SalesStaffName, q.Notes
                FROM   Quotation q
                JOIN   Customer  c ON c.CustomerID = q.CustomerID
                LEFT JOIN Staff  s ON s.StaffID    = q.SalesStaffID
                WHERE  q.QuotationStatus = 'Pending'
                ORDER  BY q.IssuedDate DESC";
            using (var conn = new MySqlConnection(_connStr))
            using (var cmd  = new MySqlCommand(sql, conn))
            {
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        list.Add(MapQuotationRow(rdr));
            }
            return list;
        }

        public QuotationEntity GetQuotationDetail(string quotationId)
        {
            QuotationEntity q = null;
            const string hdrSql = @"
                SELECT q.QuotationID, q.CustomerID, c.CustomerName,
                       q.IssuedDate, q.ExpiryDate, q.TotalAmount,
                       q.DepositRequired, q.LeadTimeEstimated,
                       q.TermsandCondition, q.QuotationStatus,
                       s.StaffName AS SalesStaffName, q.Notes
                FROM   Quotation q
                JOIN   Customer  c ON c.CustomerID = q.CustomerID
                LEFT JOIN Staff  s ON s.StaffID    = q.SalesStaffID
                WHERE  q.QuotationID = @qid";
            const string lineSql = @"
                SELECT qi.QuotationID, qi.ItemID, i.ItemName AS ProductName,
                       qi.Quantity, qi.Unit, qi.UnitPrice,
                       qi.DiscountPercent, qi.ItemNote
                FROM   QuotationItem qi
                JOIN   Item i ON i.ItemID = qi.ItemID
                WHERE  qi.QuotationID = @qid
                ORDER  BY qi.ItemID";
            using (var conn = new MySqlConnection(_connStr))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(hdrSql, conn))
                {
                    cmd.Parameters.AddWithValue("@qid", quotationId);
                    using (var rdr = cmd.ExecuteReader())
                        if (rdr.Read()) q = MapQuotationRow(rdr);
                }
                if (q == null) return null;
                q.Items = new List<QuotationItemEntity>();
                using (var cmd = new MySqlCommand(lineSql, conn))
                {
                    cmd.Parameters.AddWithValue("@qid", quotationId);
                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                            q.Items.Add(new QuotationItemEntity
                            {
                                QuotationID     = rdr.GetString("QuotationID"),
                                ItemID          = rdr.GetString("ItemID"),
                                ProductName     = rdr.GetString("ProductName"),
                                Quantity        = rdr.GetInt32("Quantity"),
                                Unit            = rdr["Unit"]            as string ?? string.Empty,
                                UnitPrice       = Convert.ToDouble(rdr["UnitPrice"]),
                                DiscountPercent = Convert.ToDouble(rdr["DiscountPercent"]),
                                ItemNote        = rdr["ItemNote"]        as string ?? string.Empty
                            });
                }
            }
            return q;
        }

        public bool InsertQuotation(QuotationEntity q, List<QuotationItemEntity> items, string salesStaffId)
        {
            const string hdrSql = @"
                INSERT INTO Quotation
                    (QuotationID, CustomerID, IssuedDate, ExpiryDate,
                     TotalAmount, DepositRequired, LeadTimeEstimated,
                     TermsandCondition, QuotationStatus, SalesStaffID, Notes)
                VALUES
                    (@qid, @cid, @iss, @exp,
                     @tot, @dep, @lead,
                     @terms, @status, @sid, @notes)";
            const string lineSql = @"
                INSERT INTO QuotationItem
                    (QuotationID, ItemID, Quantity, Unit, UnitPrice, DiscountPercent, ItemNote)
                VALUES
                    (@qid, @iid, @qty, @unit, @price, @disc, @note)";
            using (var conn = new MySqlConnection(_connStr))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = new MySqlCommand(hdrSql, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@qid",    q.QuotationID);
                            cmd.Parameters.AddWithValue("@cid",    q.CustomerID);
                            cmd.Parameters.AddWithValue("@iss",    q.IssuedDate);
                            cmd.Parameters.AddWithValue("@exp",    q.ExpiryDate);
                            cmd.Parameters.AddWithValue("@tot",    q.TotalAmount);
                            cmd.Parameters.AddWithValue("@dep",    q.DepositRequired);
                            cmd.Parameters.AddWithValue("@lead",   q.LeadTimeEstimated  ?? string.Empty);
                            cmd.Parameters.AddWithValue("@terms",  q.TermsandCondition  ?? string.Empty);
                            cmd.Parameters.AddWithValue("@status", q.QuotationStatus    ?? "Pending");
                            cmd.Parameters.AddWithValue("@sid",    salesStaffId         ?? string.Empty);
                            cmd.Parameters.AddWithValue("@notes",  q.Notes              ?? string.Empty);
                            cmd.ExecuteNonQuery();
                        }
                        foreach (var li in items)
                        {
                            using (var cmd = new MySqlCommand(lineSql, conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@qid",   q.QuotationID);
                                cmd.Parameters.AddWithValue("@iid",   li.ItemID);
                                cmd.Parameters.AddWithValue("@qty",   li.Quantity);
                                cmd.Parameters.AddWithValue("@unit",  li.Unit            ?? string.Empty);
                                cmd.Parameters.AddWithValue("@price", li.UnitPrice);
                                cmd.Parameters.AddWithValue("@disc",  li.DiscountPercent);
                                cmd.Parameters.AddWithValue("@note",  li.ItemNote        ?? string.Empty);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        tx.Commit();
                        return true;
                    }
                    catch
                    {
                        tx.Rollback();
                        return false;
                    }
                }
            }
        }

        public bool UpdateQuotationStatus(string quotationId, string newStatus)
        {
            const string sql = "UPDATE Quotation SET QuotationStatus = @s WHERE QuotationID = @q";
            using (var conn = new MySqlConnection(_connStr))
            using (var cmd  = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@s", newStatus);
                cmd.Parameters.AddWithValue("@q", quotationId);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public string GenerateNextQuotationId()
        {
            string prefix = "QUO-" + DateTime.Today.ToString("yyyyMMdd") + "-";
            const string sql = @"
                SELECT COUNT(*) FROM Quotation
                WHERE  QuotationID LIKE @p";
            using (var conn = new MySqlConnection(_connStr))
            using (var cmd  = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@p", prefix + "%");
                conn.Open();
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return string.Format("{0}{1:D4}", prefix, count + 1);
            }
        }

        // ═══════════════════════════════════════════════════
        // ORDER
        // ═══════════════════════════════════════════════════

        public List<OrderEntity> GetAllOrders()
        {
            var list = new List<OrderEntity>();
            const string sql = @"
                SELECT o.OrderID, o.QuotationID, o.CustomerID, c.CustomerName,
                       o.AddressID, o.SalesID, s.StaffName AS SalesName,
                       o.IssuedTime, o.DeliveryDate, o.ShippingAddress, o.BillingAddress,
                       o.SubTotal, o.DiscountType, o.DiscountValue, o.DiscountAmount,
                       o.GrandTotal, o.OrderContactName, o.OrderStatus
                FROM   `Order` o
                JOIN   Customer c ON c.CustomerID = o.CustomerID
                LEFT JOIN Staff s ON s.StaffID    = o.SalesID
                ORDER  BY o.IssuedTime DESC";
            using (var conn = new MySqlConnection(_connStr))
            using (var cmd  = new MySqlCommand(sql, conn))
            {
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        list.Add(MapOrderRow(rdr));
            }
            return list;
        }

        public OrderEntity GetOrderById(string orderId)
        {
            const string sql = @"
                SELECT o.OrderID, o.QuotationID, o.CustomerID, c.CustomerName,
                       o.AddressID, o.SalesID, s.StaffName AS SalesName,
                       o.IssuedTime, o.DeliveryDate, o.ShippingAddress, o.BillingAddress,
                       o.SubTotal, o.DiscountType, o.DiscountValue, o.DiscountAmount,
                       o.GrandTotal, o.OrderContactName, o.OrderStatus
                FROM   `Order` o
                JOIN   Customer c ON c.CustomerID = o.CustomerID
                LEFT JOIN Staff s ON s.StaffID    = o.SalesID
                WHERE  o.OrderID = @oid";
            using (var conn = new MySqlConnection(_connStr))
            using (var cmd  = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@oid", orderId);
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                    return rdr.Read() ? MapOrderRow(rdr) : null;
            }
        }

        public List<OrderLineEntity> GetOrderLines(string orderId)
        {
            var list = new List<OrderLineEntity>();
            const string sql = @"
                SELECT ol.OrderID, ol.ItemID, i.ItemName,
                       ol.Quantity, ol.Price
                FROM   OrderLine ol
                JOIN   Item i ON i.ItemID = ol.ItemID
                WHERE  ol.OrderID = @oid
                ORDER  BY ol.ItemID";
            using (var conn = new MySqlConnection(_connStr))
            using (var cmd  = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@oid", orderId);
                conn.Open();
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
            return list;
        }

        public bool InsertOrder(OrderEntity o, List<OrderLineEntity> lines)
        {
            const string hdrSql = @"
                INSERT INTO `Order`
                    (OrderID, QuotationID, CustomerID, AddressID, SalesID,
                     IssuedTime, DeliveryDate, ShippingAddress, BillingAddress,
                     SubTotal, DiscountType, DiscountValue, DiscountAmount,
                     GrandTotal, OrderContactName, OrderStatus)
                VALUES
                    (@oid, @qid, @cid, @aid, @sid,
                     @iss, @del, @ship, @bill,
                     @sub, @dtype, @dval, @damt,
                     @grand, @contact, @status)";
            const string lineSql = @"
                INSERT INTO OrderLine (OrderID, ItemID, Quantity, Price)
                VALUES (@oid, @iid, @qty, @price)";
            using (var conn = new MySqlConnection(_connStr))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = new MySqlCommand(hdrSql, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@oid",     o.OrderID);
                            cmd.Parameters.AddWithValue("@qid",     o.QuotationID    ?? string.Empty);
                            cmd.Parameters.AddWithValue("@cid",     o.CustomerID);
                            cmd.Parameters.AddWithValue("@aid",     o.AddressID      ?? string.Empty);
                            cmd.Parameters.AddWithValue("@sid",     o.SalesID        ?? string.Empty);
                            cmd.Parameters.AddWithValue("@iss",     o.IssuedTime);
                            cmd.Parameters.AddWithValue("@del",     o.DeliveryDate);
                            cmd.Parameters.AddWithValue("@ship",    o.ShippingAddress  ?? string.Empty);
                            cmd.Parameters.AddWithValue("@bill",    o.BillingAddress   ?? string.Empty);
                            cmd.Parameters.AddWithValue("@sub",     o.SubTotal);
                            cmd.Parameters.AddWithValue("@dtype",   o.DiscountType   ?? string.Empty);
                            cmd.Parameters.AddWithValue("@dval",    o.DiscountValue);
                            cmd.Parameters.AddWithValue("@damt",    o.DiscountAmount);
                            cmd.Parameters.AddWithValue("@grand",   o.GrandTotal);
                            cmd.Parameters.AddWithValue("@contact", o.OrderContactName ?? string.Empty);
                            cmd.Parameters.AddWithValue("@status",  o.OrderStatus      ?? "Pending");
                            cmd.ExecuteNonQuery();
                        }
                        foreach (var li in lines)
                        {
                            using (var cmd = new MySqlCommand(lineSql, conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@oid",   o.OrderID);
                                cmd.Parameters.AddWithValue("@iid",   li.ItemID);
                                cmd.Parameters.AddWithValue("@qty",   li.Quantity);
                                cmd.Parameters.AddWithValue("@price", li.Price);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        tx.Commit();
                        return true;
                    }
                    catch
                    {
                        tx.Rollback();
                        return false;
                    }
                }
            }
        }

        public bool UpdateOrderStatus(string orderId, string newStatus)
        {
            const string sql = "UPDATE `Order` SET OrderStatus = @s WHERE OrderID = @o";
            using (var conn = new MySqlConnection(_connStr))
            using (var cmd  = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@s", newStatus);
                cmd.Parameters.AddWithValue("@o", orderId);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public string GenerateNextOrderId()
        {
            string prefix = "ORD-" + DateTime.Today.ToString("yyyyMMdd") + "-";
            const string sql = "SELECT COUNT(*) FROM `Order` WHERE OrderID LIKE @p";
            using (var conn = new MySqlConnection(_connStr))
            using (var cmd  = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@p", prefix + "%");
                conn.Open();
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return string.Format("{0}{1:D4}", prefix, count + 1);
            }
        }

        // ═══════════════════════════════════════════════════
        // PRIVATE MAPPERS
        // ═══════════════════════════════════════════════════

        private static QuotationEntity MapQuotationRow(MySqlDataReader rdr)
            => new QuotationEntity
            {
                QuotationID       = rdr.GetString("QuotationID"),
                CustomerID        = rdr.GetString("CustomerID"),
                CustomerName      = rdr.GetString("CustomerName"),
                IssuedDate        = Convert.ToDateTime(rdr["IssuedDate"]),
                ExpiryDate        = Convert.ToDateTime(rdr["ExpiryDate"]),
                TotalAmount       = Convert.ToDouble(rdr["TotalAmount"]),
                DepositRequired   = Convert.ToDouble(rdr["DepositRequired"]),
                LeadTimeEstimated = rdr["LeadTimeEstimated"] as string ?? string.Empty,
                TermsandCondition = rdr["TermsandCondition"] as string ?? string.Empty,
                QuotationStatus   = rdr["QuotationStatus"]   as string ?? string.Empty,
                SalesStaffName    = rdr["SalesStaffName"]    as string ?? string.Empty,
                Notes             = rdr["Notes"]             as string ?? string.Empty
            };

        private static OrderEntity MapOrderRow(MySqlDataReader rdr)
            => new OrderEntity
            {
                OrderID          = rdr.GetString("OrderID"),
                QuotationID      = rdr["QuotationID"]      as string ?? string.Empty,
                CustomerID       = rdr.GetString("CustomerID"),
                CustomerName     = rdr.GetString("CustomerName"),
                AddressID        = rdr["AddressID"]        as string ?? string.Empty,
                SalesID          = rdr["SalesID"]          as string ?? string.Empty,
                SalesName        = rdr["SalesName"]        as string ?? string.Empty,
                IssuedTime       = Convert.ToDateTime(rdr["IssuedTime"]),
                DeliveryDate     = Convert.ToDateTime(rdr["DeliveryDate"]),
                ShippingAddress  = rdr["ShippingAddress"]  as string ?? string.Empty,
                BillingAddress   = rdr["BillingAddress"]   as string ?? string.Empty,
                SubTotal         = Convert.ToDouble(rdr["SubTotal"]),
                DiscountType     = rdr["DiscountType"]     as string ?? string.Empty,
                DiscountValue    = Convert.ToDouble(rdr["DiscountValue"]),
                DiscountAmount   = Convert.ToDouble(rdr["DiscountAmount"]),
                GrandTotal       = Convert.ToDouble(rdr["GrandTotal"]),
                OrderContactName = rdr["OrderContactName"] as string ?? string.Empty,
                OrderStatus      = rdr["OrderStatus"]      as string ?? string.Empty
            };
    }
}
