using MySql.Data.MySqlClient;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.DAL
{
    /// <summary>
    /// Data Access Layer for Statistical Reports module.
    /// One public method per report section; each returns a plain data object.
    /// The Controller is the only caller — Views never touch this class.
    /// </summary>
    public class StatisticalReportsRepo
    {
        // ════════════════════════════════════════════════════════════════
        //  1. SALES PERFORMANCE
        // ════════════════════════════════════════════════════════════════

        public SalesKpiEntity GetSalesKpi(DateTime? from, DateTime? to)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string where  = BuildDateWhere("o.IssuedTime", from, to, prefix: "WHERE");
                string sql    =
                    $@"SELECT
                         COUNT(*) AS TotalOrders,
                         COALESCE(SUM(o.GrandTotal),0) AS TotalRevenue,
                         COALESCE(AVG(o.GrandTotal),0) AS AvgOrder,
                         SUM(o.OrderStatus = 'Delivered') AS Delivered,
                         SUM(o.OrderStatus = 'Pending')   AS Pending,
                         SUM(o.OrderStatus IN ('Processing','Partially Delivered')) AS Processing,
                         SUM(o.OrderStatus = 'Cancelled') AS Cancelled
                       FROM `Order` o
                       {where}";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    AddDateParams(cmd, from, to);
                    using (var r = cmd.ExecuteReader())
                    {
                        r.Read();
                        return new SalesKpiEntity
                        {
                            TotalOrders       = Convert.ToInt32(r["TotalOrders"]),
                            TotalRevenue      = Convert.ToDouble(r["TotalRevenue"]),
                            AverageOrderValue = Convert.ToDouble(r["AvgOrder"]),
                            DeliveredOrders   = Convert.ToInt32(r["Delivered"]),
                            PendingOrders     = Convert.ToInt32(r["Pending"]),
                            ProcessingOrders  = Convert.ToInt32(r["Processing"]),
                            CancelledOrders   = Convert.ToInt32(r["Cancelled"])
                        };
                    }
                }
            }
        }

        public List<SalesOrderRowEntity> GetSalesRows(DateTime? from, DateTime? to)
        {
            var list = new List<SalesOrderRowEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string where = BuildDateWhere("o.IssuedTime", from, to, prefix: "WHERE");
                string sql   =
                    $@"SELECT o.OrderID, c.CustomerName, o.OrderStatus,
                              o.IssuedTime, o.GrandTotal,
                              (SELECT COUNT(*) FROM OrderLine ol WHERE ol.OrderID = o.OrderID) AS LineCount
                       FROM `Order` o
                       JOIN Customer c ON o.CustomerID = c.CustomerID
                       {where}
                       ORDER BY o.IssuedTime DESC";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    AddDateParams(cmd, from, to);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new SalesOrderRowEntity
                            {
                                OrderID      = r["OrderID"].ToString(),
                                CustomerName = r["CustomerName"].ToString(),
                                OrderStatus  = r["OrderStatus"].ToString(),
                                IssuedTime   = Convert.ToDateTime(r["IssuedTime"]),
                                GrandTotal   = Convert.ToDouble(r["GrandTotal"]),
                                LineCount    = Convert.ToInt32(r["LineCount"])
                            });
                }
            }
            return list;
        }

        public List<TopProductEntity> GetTopProducts(DateTime? from, DateTime? to, int topN = 5)
        {
            var list = new List<TopProductEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string dateJoin = (from.HasValue || to.HasValue)
                    ? "JOIN `Order` o2 ON ol.OrderID = o2.OrderID" : "";
                string where    = BuildDateWhere("o2.IssuedTime", from, to,
                                                 prefix: dateJoin.Length > 0 ? "WHERE" : "");
                string sql =
                    $@"SELECT ol.ItemID, i.ItemName, p.Category,
                              SUM(ol.Quantity) AS TotalQty,
                              SUM(ol.Quantity * ol.Price) AS TotalRevenue
                       FROM OrderLine ol
                       JOIN Item i    ON ol.ItemID = i.ItemID
                       JOIN Product p ON ol.ItemID = p.ItemID
                       {dateJoin}
                       {where}
                       GROUP BY ol.ItemID, i.ItemName, p.Category
                       ORDER BY TotalRevenue DESC
                       LIMIT @topN";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@topN", topN);
                    AddDateParams(cmd, from, to);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new TopProductEntity
                            {
                                ItemID       = r["ItemID"].ToString(),
                                ItemName     = r["ItemName"].ToString(),
                                Category     = r["Category"].ToString(),
                                TotalQty     = Convert.ToInt32(r["TotalQty"]),
                                TotalRevenue = Convert.ToDouble(r["TotalRevenue"])
                            });
                }
            }
            return list;
        }

        // ════════════════════════════════════════════════════════════════
        //  2. INVENTORY STATUS
        // ════════════════════════════════════════════════════════════════

        public InventoryKpiEntity GetInventoryKpi()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT
                        COUNT(*) AS TotalSKUs,
                        SUM(wi.WarehouseItemQuantity <= wi.ReorderLevel) AS BelowReorder,
                        SUM(p.ItemID IS NOT NULL) AS ProductCount,
                        SUM(rm.ItemID IS NOT NULL) AS RawMaterialCount
                      FROM WarehouseItem wi
                      LEFT JOIN Product     p  ON wi.ItemID = p.ItemID
                      LEFT JOIN RawMaterial rm ON wi.ItemID = rm.ItemID";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var r   = cmd.ExecuteReader())
                {
                    r.Read();
                    return new InventoryKpiEntity
                    {
                        TotalSKUs         = Convert.ToInt32(r["TotalSKUs"]),
                        BelowReorderCount = Convert.ToInt32(r["BelowReorder"]),
                        ProductCount      = Convert.ToInt32(r["ProductCount"]),
                        RawMaterialCount  = Convert.ToInt32(r["RawMaterialCount"])
                    };
                }
            }
        }

        /// <summary>
        /// Returns inventory rows.
        /// <paramref name="categoryFilter"/>: null/"All" = all; "Product" / "Raw Material" = filtered.
        /// <paramref name="keyword"/>: searches ItemID and ItemName (case-insensitive LIKE).
        /// </summary>
        public List<InventoryStatusRowEntity> GetInventoryRows(
            string categoryFilter  = null,
            bool   belowReorderOnly = false,
            string keyword          = null)
        {
            var list = new List<InventoryStatusRowEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string extra = belowReorderOnly ? " AND wi.WarehouseItemQuantity <= wi.ReorderLevel" : "";
                if (!string.IsNullOrEmpty(categoryFilter) && categoryFilter != "All")
                    extra += categoryFilter == "Product"
                        ? " AND p.ItemID IS NOT NULL"
                        : " AND rm.ItemID IS NOT NULL";
                if (!string.IsNullOrWhiteSpace(keyword))
                    extra += " AND (wi.ItemID LIKE @kw OR i.ItemName LIKE @kw)";

                string sql =
                    $@"SELECT wi.WarehouseItemID, wi.ItemID, i.ItemName,
                              CASE WHEN p.ItemID  IS NOT NULL THEN 'Product'
                                   WHEN rm.ItemID IS NOT NULL THEN 'Raw Material'
                                   ELSE 'Unknown' END AS ItemCategory,
                              COALESCE(rm.MaterialType,'') AS MaterialType,
                              wi.WarehouseID, w.WarehouseLocation,
                              wi.WarehouseItemQuantity AS CurrentStock,
                              wi.ReorderLevel
                       FROM   WarehouseItem wi
                       JOIN   Item i     ON wi.ItemID      = i.ItemID
                       JOIN   Warehouse w ON wi.WarehouseID = w.WarehouseID
                       LEFT JOIN Product     p  ON wi.ItemID = p.ItemID
                       LEFT JOIN RawMaterial rm ON wi.ItemID = rm.ItemID
                       WHERE  1=1 {extra}
                       ORDER  BY wi.WarehouseItemQuantity ASC";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrWhiteSpace(keyword))
                        cmd.Parameters.AddWithValue("@kw", $"%{keyword.Trim()}%");
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new InventoryStatusRowEntity
                            {
                                WarehouseItemID   = r["WarehouseItemID"].ToString(),
                                ItemID            = r["ItemID"].ToString(),
                                ItemName          = r["ItemName"].ToString(),
                                ItemCategory      = r["ItemCategory"].ToString(),
                                MaterialType      = r["MaterialType"].ToString(),
                                WarehouseID       = r["WarehouseID"].ToString(),
                                WarehouseLocation = r["WarehouseLocation"].ToString(),
                                CurrentStock      = Convert.ToInt32(r["CurrentStock"]),
                                ReorderLevel      = Convert.ToInt32(r["ReorderLevel"])
                            });
                }
            }
            return list;
        }

        // ════════════════════════════════════════════════════════════════
        //  3. PROCUREMENT SUMMARY
        // ════════════════════════════════════════════════════════════════

        public ProcurementKpiEntity GetProcurementKpi()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT COUNT(*) AS TotalPOs,
                             COALESCE(SUM(POTotalAmount),0) AS TotalSpend,
                             SUM(PurchaseStatus = 'Completed') AS Completed,
                             SUM(PurchaseStatus IN ('Sent','Partially Received')) AS Pending,
                             COUNT(DISTINCT SupplierID) AS UniqueSuppliers
                      FROM PurchaseOrder";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var r   = cmd.ExecuteReader())
                {
                    r.Read();
                    return new ProcurementKpiEntity
                    {
                        TotalPOs        = Convert.ToInt32(r["TotalPOs"]),
                        TotalSpend      = Convert.ToDouble(r["TotalSpend"]),
                        CompletedPOs    = Convert.ToInt32(r["Completed"]),
                        PendingPOs      = Convert.ToInt32(r["Pending"]),
                        UniqueSuppliers = Convert.ToInt32(r["UniqueSuppliers"])
                    };
                }
            }
        }

        /// <summary>
        /// Valid statusFilter values: null / "All" / "Sent" / "Partially Received" /
        /// "Received" / "Completed" / "Cancelled"  (matches PurchaseOrder.PurchaseStatus ENUM).
        /// </summary>
        public List<ProcurementRowEntity> GetProcurementRows(
            DateTime? from        = null,
            DateTime? to          = null,
            string   statusFilter = null)
        {
            var list = new List<ProcurementRowEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string where = "WHERE 1=1";
                if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
                    where += " AND po.PurchaseStatus = @status";
                string dateExtra = BuildDateWhere("po.OrderDate", from, to, prefix: "AND");
                where += dateExtra;

                string sql =
                    $@"SELECT po.PurchaseID, s.SupplierName, po.PurchaseStatus,
                              po.OrderDate, po.POTotalAmount, po.RequestID,
                              (SELECT COUNT(*)
                               FROM   PurchaseOrderLine pol
                               WHERE  pol.PurchaseID = po.PurchaseID) AS ItemCount,
                              (SELECT MAX(gr.ReceiptStatus)
                               FROM   GoodsReceived gr
                               WHERE  gr.PurchaseID = po.PurchaseID) AS ReceiptStatus
                       FROM   PurchaseOrder po
                       JOIN   Supplier s ON po.SupplierID = s.SupplierID
                       {where}
                       ORDER  BY po.OrderDate DESC";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
                        cmd.Parameters.AddWithValue("@status", statusFilter);
                    AddDateParams(cmd, from, to);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new ProcurementRowEntity
                            {
                                PurchaseOrderID = r["PurchaseID"].ToString(),
                                SupplierName    = r["SupplierName"].ToString(),
                                PurchaseStatus  = r["PurchaseStatus"].ToString(),
                                ReceiptStatus   = r["ReceiptStatus"] == DBNull.Value ? "—" : r["ReceiptStatus"].ToString(),
                                OrderDate       = Convert.ToDateTime(r["OrderDate"]),
                                TotalAmount     = Convert.ToDouble(r["POTotalAmount"]),
                                ItemCount       = Convert.ToInt32(r["ItemCount"]),
                                RequestID       = r["RequestID"] == DBNull.Value ? "—" : r["RequestID"].ToString()
                            });
                }
            }
            return list;
        }

        // ════════════════════════════════════════════════════════════════
        //  4. LOGISTICS OVERVIEW
        // ════════════════════════════════════════════════════════════════

        public LogisticsKpiEntity GetLogisticsKpi()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT COUNT(*) AS Total,
                             SUM(ShipmentStatus = 'Completed')  AS Completed,
                             SUM(ShipmentStatus = 'In Transit') AS InTransit,
                             SUM(ShipmentStatus = 'Pending')    AS Pending,
                             (SELECT COUNT(DISTINCT dn.ShipmentID)
                              FROM DeliveryNote dn
                              JOIN ReplySlip rs ON rs.DeliveryID = dn.DeliveryID) AS WithReplySlip
                      FROM Shipment";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var r   = cmd.ExecuteReader())
                {
                    r.Read();
                    return new LogisticsKpiEntity
                    {
                        TotalShipments = Convert.ToInt32(r["Total"]),
                        Completed      = Convert.ToInt32(r["Completed"]),
                        InTransit      = Convert.ToInt32(r["InTransit"]),
                        Pending        = Convert.ToInt32(r["Pending"]),
                        WithReplySlip  = Convert.ToInt32(r["WithReplySlip"])
                    };
                }
            }
        }

        /// <summary>
        /// Valid statusFilter values: null / "All" / "Pending" / "In Transit" / "Completed"
        /// (matches Shipment.ShipmentStatus ENUM).
        /// Returns LogisticsRowEntity — aligned to ViewReportForm / LogisticsRowEntity fields.
        /// </summary>
        public List<LogisticsRowEntity> GetLogisticsRows(
            DateTime? from        = null,
            DateTime? to          = null,
            string   statusFilter = null)
        {
            var list = new List<LogisticsRowEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string where = "WHERE 1=1";
                if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
                    where += " AND sh.ShipmentStatus = @status";
                string dateExtra = BuildDateWhere("sh.ShipDate", from, to, prefix: "AND");
                where += dateExtra;

                // DriverName: Shipment may reference a Driver via DeliveryNote or directly.
                // We attempt LEFT JOIN DeliveryNote → Driver; fall back to empty string if absent.
                string sql =
                    $@"SELECT sh.ShipmentID, sh.OrderID, c.CustomerName,
                              sh.ShipmentStatus, sh.ShipDate,
                              COALESCE(d.DriverName, '') AS DriverName,
                              (SELECT COUNT(1) FROM DeliveryNote dn WHERE dn.ShipmentID = sh.ShipmentID) AS HasDN,
                              (SELECT COUNT(1)
                               FROM DeliveryNote dn2
                               JOIN ReplySlip rs ON rs.DeliveryID = dn2.DeliveryID
                               WHERE dn2.ShipmentID = sh.ShipmentID) AS HasRS
                       FROM   Shipment sh
                       JOIN   `Order`  o ON sh.OrderID = o.OrderID
                       JOIN   Customer c ON o.CustomerID = c.CustomerID
                       LEFT JOIN DeliveryNote dn3 ON dn3.ShipmentID = sh.ShipmentID
                       LEFT JOIN Driver d ON d.DriverID = dn3.DriverID
                       {where}
                       GROUP BY sh.ShipmentID, sh.OrderID, c.CustomerName,
                                sh.ShipmentStatus, sh.ShipDate, d.DriverName
                       ORDER  BY sh.ShipDate DESC";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
                        cmd.Parameters.AddWithValue("@status", statusFilter);
                    AddDateParams(cmd, from, to);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new LogisticsRowEntity
                            {
                                DeliveryOrderID = r["ShipmentID"].ToString(),
                                SalesOrderID    = r["OrderID"].ToString(),
                                CustomerName    = r["CustomerName"].ToString(),
                                DeliveryStatus  = r["ShipmentStatus"].ToString(),
                                DriverName      = r["DriverName"].ToString(),
                                DeliveryDate    = Convert.ToDateTime(r["ShipDate"]),
                                HasDeliveryNote = Convert.ToInt32(r["HasDN"]) > 0,
                                HasReplySlip    = Convert.ToInt32(r["HasRS"]) > 0
                            });
                }
            }
            return list;
        }

        // ════════════════════════════════════════════════════════════════
        //  5. AFTER-SERVICE SUMMARY
        // ════════════════════════════════════════════════════════════════

        public AfterServiceKpiEntity GetAfterServiceKpi()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT
                        (SELECT COUNT(*) FROM Complaint) AS TotalComplaints,
                        (SELECT COUNT(*) FROM Complaint WHERE ComplaintStatus NOT IN ('Completed')) AS OpenComplaints,
                        (SELECT COUNT(*) FROM ReturnOrder) AS TotalReturns,
                        (SELECT COALESCE(SUM(RefundAmount),0) FROM ReturnOrder WHERE ReturnStatus = 'Completed') AS TotalRefunded";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var r   = cmd.ExecuteReader())
                {
                    r.Read();
                    return new AfterServiceKpiEntity
                    {
                        TotalComplaints = Convert.ToInt32(r["TotalComplaints"]),
                        OpenComplaints  = Convert.ToInt32(r["OpenComplaints"]),
                        TotalReturns    = Convert.ToInt32(r["TotalReturns"]),
                        TotalRefunded   = Convert.ToDouble(r["TotalRefunded"])
                    };
                }
            }
        }

        /// <summary>
        /// Valid complaintStatusFilter: null/"All"/"Pending"/"Processing"/"Escalated"/"Completed".
        /// </summary>
        public List<ComplaintRowEntity> GetComplaintRows(
            DateTime? from        = null,
            DateTime? to          = null,
            string   statusFilter = null)
        {
            var list = new List<ComplaintRowEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string where = "WHERE 1=1";
                if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
                    where += " AND cp.ComplaintStatus = @status";
                // Filter by associated Order.IssuedTime if date range supplied
                string dateExtra = BuildDateWhere("o.IssuedTime", from, to, prefix: "AND");
                where += dateExtra;

                string sql =
                    $@"SELECT cp.ComplaintID, cp.OrderID,
                              COALESCE(c.CustomerName,'—') AS CustomerName,
                              cp.ComplaintDescription, cp.ComplaintStatus,
                              o.IssuedTime AS ComplaintDate
                       FROM   Complaint cp
                       LEFT JOIN `Order`  o  ON cp.OrderID    = o.OrderID
                       LEFT JOIN Customer c  ON o.CustomerID  = c.CustomerID
                       {where}
                       ORDER  BY cp.ComplaintID DESC";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
                        cmd.Parameters.AddWithValue("@status", statusFilter);
                    AddDateParams(cmd, from, to);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new ComplaintRowEntity
                            {
                                ComplaintID     = r["ComplaintID"].ToString(),
                                OrderID         = r["OrderID"] == DBNull.Value ? "—" : r["OrderID"].ToString(),
                                CustomerName    = r["CustomerName"].ToString(),
                                Subject         = r["ComplaintDescription"] == DBNull.Value ? "—" : r["ComplaintDescription"].ToString(),
                                ComplaintStatus = r["ComplaintStatus"].ToString(),
                                ComplaintDate   = r["ComplaintDate"] == DBNull.Value
                                                    ? DateTime.MinValue
                                                    : Convert.ToDateTime(r["ComplaintDate"])
                            });
                }
            }
            return list;
        }

        /// <summary>
        /// Valid returnStatusFilter: null/"All"/"Pending"/"Processing"/"Completed".
        /// </summary>
        public List<ReturnOrderRowEntity> GetReturnOrderRows(
            DateTime? from        = null,
            DateTime? to          = null,
            string   statusFilter = null)
        {
            var list = new List<ReturnOrderRowEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string where = "WHERE 1=1";
                if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
                    where += " AND rt.ReturnStatus = @status";
                string dateExtra = BuildDateWhere("rt.ReturnDate", from, to, prefix: "AND");
                where += dateExtra;

                string sql =
                    $@"SELECT rt.ReturnID, rt.OrderID, c.CustomerName,
                              rt.Reason, rt.RefundAmount, rt.ReturnStatus, rt.ReturnDate
                       FROM   ReturnOrder rt
                       JOIN   `Order`   o  ON rt.OrderID   = o.OrderID
                       JOIN   Customer  c  ON o.CustomerID = c.CustomerID
                       {where}
                       ORDER  BY rt.ReturnDate DESC";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
                        cmd.Parameters.AddWithValue("@status", statusFilter);
                    AddDateParams(cmd, from, to);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new ReturnOrderRowEntity
                            {
                                ReturnOrderID = r["ReturnID"].ToString(),
                                SalesOrderID  = r["OrderID"].ToString(),
                                CustomerName  = r["CustomerName"].ToString(),
                                Reason        = r["Reason"] == DBNull.Value ? "—" : r["Reason"].ToString(),
                                RefundAmount  = Convert.ToDouble(r["RefundAmount"]),
                                ReturnStatus  = r["ReturnStatus"].ToString(),
                                ReturnDate    = Convert.ToDateTime(r["ReturnDate"])
                            });
                }
            }
            return list;
        }

        // ════════════════════════════════════════════════════════════════
        //  6. FINANCE OVERVIEW
        // ════════════════════════════════════════════════════════════════

        public FinanceKpiEntity GetFinanceKpi()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT
                        (SELECT COALESCE(SUM(t.Amount),0) FROM `Transaction` t WHERE t.InvoiceID IS NOT NULL AND t.ReturnID IS NULL)  AS SalesRevenue,
                        (SELECT COALESCE(SUM(t.Amount),0) FROM `Transaction` t WHERE t.PurInvoiceID IS NOT NULL)                      AS ProcSpend,
                        (SELECT COALESCE(SUM(t.Amount),0) FROM `Transaction` t WHERE t.ReturnID IS NOT NULL)                          AS Refunds,
                        (SELECT COALESCE(SUM(i.RemainingBalance),0) FROM Invoice i WHERE i.PaymentStatus = 'Partial')                  AS AROutstanding,
                        (SELECT COALESCE(SUM(
                            pi.TotalAmount - COALESCE((
                                SELECT SUM(t2.Amount)
                                FROM `Transaction` t2
                                WHERE t2.PurInvoiceID = pi.PurInvoiceID
                            ),0)
                        ),0)
                         FROM PurchaseInvoice pi WHERE pi.PaymentStatus = 'Partial')                                                   AS APOutstanding";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var r   = cmd.ExecuteReader())
                {
                    r.Read();
                    return new FinanceKpiEntity
                    {
                        TotalSalesRevenue       = Convert.ToDouble(r["SalesRevenue"]),
                        TotalProcurementSpend   = Convert.ToDouble(r["ProcSpend"]),
                        TotalRefunds            = Convert.ToDouble(r["Refunds"]),
                        AROutstanding           = Convert.ToDouble(r["AROutstanding"]),
                        APOutstanding           = Convert.ToDouble(r["APOutstanding"])
                    };
                }
            }
        }

        /// <summary>
        /// Returns finance transaction rows filtered by date range and optionally by docType group.
        /// </summary>
        public List<FinanceTransactionRowEntity> GetFinanceTransactionRows(
            DateTime? from           = null,
            DateTime? to             = null,
            string    docTypeFilter  = null)
        {
            var list = new List<FinanceTransactionRowEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string where = BuildDateWhere("t.TransactionDate", from, to, prefix: "WHERE");
                if (string.IsNullOrEmpty(where)) where = "WHERE 1=1";

                if (!string.IsNullOrEmpty(docTypeFilter) && docTypeFilter != "All")
                {
                    switch (docTypeFilter)
                    {
                        case "Revenue": where += " AND t.InvoiceID IS NOT NULL AND t.ReturnID IS NULL"; break;
                        case "Expense": where += " AND t.PurInvoiceID IS NOT NULL"; break;
                        case "Refund":  where += " AND t.ReturnID IS NOT NULL";    break;
                    }
                }

                string sql =
                    $@"SELECT t.TransactionID, t.TransactionType, t.Amount, t.TransactionDate,
                              COALESCE(t.InvoiceID, t.PurInvoiceID, t.ReturnID, '—') AS LinkedDoc,
                              CASE
                                WHEN t.InvoiceID    IS NOT NULL AND t.ReturnID IS NULL THEN 'Sales Invoice'
                                WHEN t.PurInvoiceID IS NOT NULL THEN 'Purchase Invoice'
                                WHEN t.ReturnID     IS NOT NULL THEN 'Return Refund'
                                ELSE '—' END AS DocType
                       FROM   `Transaction` t
                       {where}
                       ORDER  BY t.TransactionDate DESC";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    AddDateParams(cmd, from, to);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new FinanceTransactionRowEntity
                            {
                                TransactionID   = r["TransactionID"].ToString(),
                                TransactionType = r["TransactionType"].ToString(),
                                Amount          = Convert.ToDouble(r["Amount"]),
                                TransactionDate = Convert.ToDateTime(r["TransactionDate"]),
                                LinkedDocument  = r["LinkedDoc"].ToString(),
                                DocumentType    = r["DocType"].ToString()
                            });
                }
            }
            return list;
        }

        // ════════════════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ════════════════════════════════════════════════════════════════

        private static string BuildDateWhere(string col, DateTime? from, DateTime? to, string prefix)
        {
            if (!from.HasValue && !to.HasValue) return "";
            var parts = new List<string>();
            if (from.HasValue) parts.Add($"{col} >= @dateFrom");
            if (to.HasValue)   parts.Add($"{col} <= @dateTo");
            return $" {prefix} {string.Join(" AND ", parts)}";
        }

        private static void AddDateParams(MySqlCommand cmd, DateTime? from, DateTime? to)
        {
            if (from.HasValue) cmd.Parameters.AddWithValue("@dateFrom", from.Value.Date);
            if (to.HasValue)   cmd.Parameters.AddWithValue("@dateTo",   to.Value.Date.AddDays(1).AddSeconds(-1));
        }
    }
}
