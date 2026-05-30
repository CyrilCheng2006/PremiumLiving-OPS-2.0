using MySql.Data.MySqlClient;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.DAL
{
    /// <summary>
    /// Repository (DAL layer) for Order Processing module.
    /// Encapsulates all SQL queries for Order, OrderLine, Quotation,
    /// Customer, and Product tables.
    /// All methods use parameterised queries via DatabaseHelper.
    /// </summary>
    public class OrderProcessingRepo
    {
        // ════════════════════════════════════════════════════════════════
        //  ORDER queries
        // ════════════════════════════════════════════════════════════════

        /// <summary>Returns all orders with customer and sales-staff names.</summary>
        public List<OrderEntity> GetAllOrders()
        {
            var list = new List<OrderEntity>();
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
                      ORDER BY o.IssuedTime DESC";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read()) list.Add(MapOrder(rdr));
            }
            return list;
        }

        /// <summary>Returns orders filtered by status.</summary>
        public List<OrderEntity> GetOrdersByStatus(string status)
        {
            var list = new List<OrderEntity>();
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
                      WHERE o.OrderStatus = @status
                      ORDER BY o.IssuedTime DESC";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@status", status);
                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read()) list.Add(MapOrder(rdr));
                }
            }
            return list;
        }

        /// <summary>Returns a single order by OrderID.</summary>
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

        /// <summary>Returns all OrderLine rows for a given Order.</summary>
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
                                Price    = rdr.GetDouble("Price")
                            });
                }
            }
            return list;
        }

        /// <summary>Inserts a new Order header. Returns true on success.</summary>
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
                    cmd.Parameters.AddWithValue("@QuotationID",      string.IsNullOrEmpty(order.QuotationID) ? (object)DBNull.Value : order.QuotationID);
                    cmd.Parameters.AddWithValue("@CustomerID",       order.CustomerID);
                    cmd.Parameters.AddWithValue("@AddressID",        string.IsNullOrEmpty(order.AddressID)   ? (object)DBNull.Value : order.AddressID);
                    cmd.Parameters.AddWithValue("@SalesID",          order.SalesID);
                    cmd.Parameters.AddWithValue("@IssuedTime",       order.IssuedTime);
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
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        /// <summary>Inserts a single OrderLine row.</summary>
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

        /// <summary>Updates Order header fields (status, delivery date, addresses, contact).</summary>
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

        /// <summary>Deletes all OrderLine rows for an order, then re-inserts the new set.</summary>
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
                        foreach (var line in lines)
                        {
                            const string ins =
                                "INSERT INTO OrderLine (OrderID, ItemID, Quantity, Price) VALUES (@OrderID, @ItemID, @Qty, @Price)";
                            using (var ins_cmd = new MySqlCommand(ins, conn, tx))
                            {
                                ins_cmd.Parameters.AddWithValue("@OrderID", line.OrderID);
                                ins_cmd.Parameters.AddWithValue("@ItemID",  line.ItemID);
                                ins_cmd.Parameters.AddWithValue("@Qty",     line.Quantity);
                                ins_cmd.Parameters.AddWithValue("@Price",   line.Price);
                                ins_cmd.ExecuteNonQuery();
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

        // ════════════════════════════════════════════════════════════════
        //  QUOTATION queries
        // ════════════════════════════════════════════════════════════════

        /// <summary>Returns all quotations with customer names.</summary>
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

        /// <summary>Returns only Pending quotations.</summary>
        public List<QuotationEntity> GetPendingQuotations()
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
                      WHERE q.QuotationStatus = 'Pending'
                      ORDER BY q.ExpiryDate ASC";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read()) list.Add(MapQuotation(rdr));
            }
            return list;
        }

        /// <summary>Updates the status of a Quotation.</summary>
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

        // ════════════════════════════════════════════════════════════════
        //  CUSTOMER queries
        // ════════════════════════════════════════════════════════════════

        /// <summary>Returns all customers for drop-down population.</summary>
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
                            Email        = rdr.GetString("EmailAddress"),
                            Phone        = rdr.GetString("PhoneNumber")
                        });
            }
            return list;
        }

        // ════════════════════════════════════════════════════════════════
        //  PRODUCT queries
        // ════════════════════════════════════════════════════════════════

        /// <summary>Returns all products (Item JOIN Product) for order-line entry.</summary>
        public List<ProductLookup> GetAllProducts()
        {
            var list = new List<ProductLookup>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT p.ItemID, i.ItemName, p.SalesPrice, p.Category
                      FROM Product p
                      JOIN Item i ON p.ItemID = i.ItemID
                      ORDER BY i.ItemName";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        list.Add(new ProductLookup
                        {
                            ItemID     = rdr.GetString("ItemID"),
                            ItemName   = rdr.GetString("ItemName"),
                            SalesPrice = rdr.GetDouble("SalesPrice"),
                            Category   = rdr.GetString("Category")
                        });
            }
            return list;
        }

        // ════════════════════════════════════════════════════════════════
        //  PRIVATE MAPPERS
        // ════════════════════════════════════════════════════════════════

        private static OrderEntity MapOrder(MySqlDataReader rdr)
        {
            return new OrderEntity
            {
                OrderID          = rdr.GetString("OrderID"),
                QuotationID      = rdr.IsDBNull(rdr.GetOrdinal("QuotationID"))  ? null : rdr.GetString("QuotationID"),
                CustomerID       = rdr.GetString("CustomerID"),
                CustomerName     = rdr.GetString("CustomerName"),
                AddressID        = rdr.IsDBNull(rdr.GetOrdinal("AddressID"))     ? null : rdr.GetString("AddressID"),
                SalesID          = rdr.GetString("SalesID"),
                SalesName        = rdr.GetString("SalesName"),
                IssuedTime       = rdr.GetDateTime("IssuedTime"),
                DeliveryDate     = rdr.GetDateTime("DeliveryDate"),
                ShippingAddress  = rdr.GetString("ShippingAddress"),
                BillingAddress   = rdr.GetString("BillingAddress"),
                SubTotal         = rdr.IsDBNull(rdr.GetOrdinal("SubTotal"))      ? 0   : rdr.GetDouble("SubTotal"),
                DiscountType     = rdr.IsDBNull(rdr.GetOrdinal("DiscountType"))  ? null : rdr.GetString("DiscountType"),
                DiscountValue    = rdr.IsDBNull(rdr.GetOrdinal("DiscountValue")) ? 0   : rdr.GetDouble("DiscountValue"),
                DiscountAmount   = rdr.IsDBNull(rdr.GetOrdinal("DiscountAmount"))? 0   : rdr.GetDouble("DiscountAmount"),
                GrandTotal       = rdr.GetDouble("GrandTotal"),
                OrderContactName = rdr.GetString("OrderContactName"),
                OrderStatus      = rdr.GetString("OrderStatus")
            };
        }

        private static QuotationEntity MapQuotation(MySqlDataReader rdr)
        {
            return new QuotationEntity
            {
                QuotationID       = rdr.GetString("QuotationID"),
                CustomerID        = rdr.GetString("CustomerID"),
                CustomerName      = rdr.GetString("CustomerName"),
                ExpiryDate        = rdr.GetDateTime("ExpiryDate"),
                TotalAmount       = rdr.GetDouble("TotalAmount"),
                DepositRequired   = rdr.IsDBNull(rdr.GetOrdinal("DepositRequired")) ? 0 : rdr.GetDouble("DepositRequired"),
                LeadTimeEstimated = rdr.IsDBNull(rdr.GetOrdinal("LeadTimeEstimated")) ? null : rdr.GetString("LeadTimeEstimated"),
                TermsandCondition = rdr.IsDBNull(rdr.GetOrdinal("TermsandCondition")) ? null : rdr.GetString("TermsandCondition"),
                QuotationStatus   = rdr.GetString("QuotationStatus")
            };
        }
    }
}
