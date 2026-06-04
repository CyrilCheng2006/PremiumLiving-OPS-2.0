using MySql.Data.MySqlClient;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.DAL
{
    /// <summary>
    /// Data Access Layer for Logistics Processing.
    /// All SQL queries are isolated here; no UI or business logic.
    /// </summary>
    public class LogisticsProcessingRepo
    {
        // ── Connection helper ────────────────────────────────────────────
        private MySqlConnection OpenConnection()
        {
            var conn = DatabaseHelper.GetConnection();
            conn.Open();
            return conn;
        }

        // ── Shipments ──────────────────────────────────────────────────
        public List<ShipmentEntity> SearchShipments(
            string statusFilter, string keyword, DateTime? dateFrom)
        {
            var list = new List<ShipmentEntity>();
            using (var conn = OpenConnection())
            {
                string sql = @"
                    SELECT s.ShipmentID, s.OrderID, s.TrackingNumber, s.ShipDate,
                           s.DeliveryMethod, s.ShipmentStatus, s.ShipmentType, s.TotalAmount,
                           c.CustomerName, o.ShippingAddress, o.DeliveryDate
                    FROM Shipment s
                    JOIN `Order` o ON s.OrderID = o.OrderID
                    JOIN Customer c ON o.CustomerID = c.CustomerID
                    WHERE 1=1";

                if (!string.IsNullOrEmpty(statusFilter))
                    sql += " AND s.ShipmentStatus = @status";
                if (!string.IsNullOrEmpty(keyword))
                    sql += " AND (s.ShipmentID LIKE @kw OR s.OrderID LIKE @kw OR c.CustomerName LIKE @kw)";
                if (dateFrom.HasValue)
                    sql += " AND s.ShipDate >= @dateFrom";
                sql += " ORDER BY s.ShipDate DESC";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(statusFilter))
                        cmd.Parameters.AddWithValue("@status", statusFilter);
                    if (!string.IsNullOrEmpty(keyword))
                        cmd.Parameters.AddWithValue("@kw", $"%{keyword}%");
                    if (dateFrom.HasValue)
                        cmd.Parameters.AddWithValue("@dateFrom", dateFrom.Value);

                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add(MapShipment(r));
                }
            }
            return list;
        }

        public ShipmentEntity GetShipmentById(string shipmentId)
        {
            using (var conn = OpenConnection())
            {
                const string sql = @"
                    SELECT s.ShipmentID, s.OrderID, s.TrackingNumber, s.ShipDate,
                           s.DeliveryMethod, s.ShipmentStatus, s.ShipmentType, s.TotalAmount,
                           c.CustomerName, o.ShippingAddress, o.DeliveryDate
                    FROM Shipment s
                    JOIN `Order` o ON s.OrderID = o.OrderID
                    JOIN Customer c ON o.CustomerID = c.CustomerID
                    WHERE s.ShipmentID = @id";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", shipmentId);
                    using (var r = cmd.ExecuteReader())
                        return r.Read() ? MapShipment(r) : null;
                }
            }
        }

        public List<ShipmentLineEntity> GetShipmentLines(string shipmentId)
        {
            var list = new List<ShipmentLineEntity>();
            using (var conn = OpenConnection())
            {
                const string sql = @"
                    SELECT sl.ShipmentLineID, sl.ShipmentID, sl.OrderID,
                           sl.ItemID, i.ItemName, sl.QtyShipped, sl.QtyOutstanding
                    FROM ShipmentLine sl
                    JOIN Item i ON sl.ItemID = i.ItemID
                    WHERE sl.ShipmentID = @id";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", shipmentId);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add(MapShipmentLine(r));
                }
            }
            return list;
        }

        public DeliveryNoteEntity GetDeliveryNoteByShipment(string shipmentId)
        {
            using (var conn = OpenConnection())
            {
                const string sql = @"
                    SELECT dn.DeliveryID, dn.ShipmentID, dn.DeliveryDate,
                           dn.Outstanding_qty, dn.ShippingAddress, dn.ShipToName
                    FROM DeliveryNote dn
                    WHERE dn.ShipmentID = @id
                    LIMIT 1";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", shipmentId);
                    using (var r = cmd.ExecuteReader())
                        return r.Read() ? MapDeliveryNote(r) : null;
                }
            }
        }

        /// <summary>
        /// Returns the ReplySlip for a given DeliveryID, or null if not yet received.
        /// </summary>
        public ReplySlipEntity GetReplySlipByDelivery(string deliveryId)
        {
            if (string.IsNullOrEmpty(deliveryId)) return null;
            using (var conn = OpenConnection())
            {
                const string sql = @"
                    SELECT rs.SlipID, rs.DeliveryID, rs.actualRecipient,
                           rs.ReceivedDate, rs.RecipientRemark
                    FROM ReplySlip rs
                    WHERE rs.DeliveryID = @id
                    LIMIT 1";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", deliveryId);
                    using (var r = cmd.ExecuteReader())
                        return r.Read() ? MapReplySlip(r) : null;
                }
            }
        }

        // ── Goods Received ───────────────────────────────────────────
        public List<GoodsReceivedEntity> SearchReceipts(
            string statusFilter, string keyword, DateTime? dateFrom)
        {
            var list = new List<GoodsReceivedEntity>();
            using (var conn = OpenConnection())
            {
                string sql = @"
                    SELECT r.ReceiptID, r.PurchaseID, r.POLineID, r.QtyReceived,
                           r.ReceiptDate, r.Outstanding_QTY,
                           sup.SupplierName,
                           pol.RawMaterialItemID, i.ItemName,
                           pol.WarehouseID, w.WarehouseLocation,
                           po.PurchaseStatus, pol.UnitPrice
                    FROM Receipt r
                    JOIN PurchaseOrderLine pol ON r.POLineID = pol.POLineID
                    JOIN PurchaseOrder po      ON r.PurchaseID = po.PurchaseID
                    JOIN Supplier sup          ON po.SupplierID = sup.SupplierID
                    JOIN Item i                ON pol.RawMaterialItemID = i.ItemID
                    JOIN Warehouse w           ON pol.WarehouseID = w.WarehouseID
                    WHERE 1=1";

                if (!string.IsNullOrEmpty(statusFilter))
                    sql += " AND po.PurchaseStatus = @status";
                if (!string.IsNullOrEmpty(keyword))
                    sql += " AND (r.ReceiptID LIKE @kw OR r.PurchaseID LIKE @kw OR sup.SupplierName LIKE @kw)";
                if (dateFrom.HasValue)
                    sql += " AND r.ReceiptDate >= @dateFrom";
                sql += " ORDER BY r.ReceiptDate DESC";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(statusFilter))
                        cmd.Parameters.AddWithValue("@status", statusFilter);
                    if (!string.IsNullOrEmpty(keyword))
                        cmd.Parameters.AddWithValue("@kw", $"%{keyword}%");
                    if (dateFrom.HasValue)
                        cmd.Parameters.AddWithValue("@dateFrom", dateFrom.Value);

                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add(MapReceipt(r));
                }
            }
            return list;
        }

        public List<PurchaseOrderEntity> GetAllPurchaseOrders()
        {
            var list = new List<PurchaseOrderEntity>();
            using (var conn = OpenConnection())
            {
                const string sql = @"
                    SELECT po.PurchaseID, po.RequestID, po.SupplierID,
                           sup.SupplierName, po.POTotalAmount, po.OrderDate, po.PurchaseStatus
                    FROM PurchaseOrder po
                    JOIN Supplier sup ON po.SupplierID = sup.SupplierID
                    ORDER BY po.OrderDate DESC";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new PurchaseOrderEntity
                        {
                            PurchaseID     = r["PurchaseID"].ToString(),
                            RequestID      = r["RequestID"].ToString(),
                            SupplierID     = r["SupplierID"].ToString(),
                            SupplierName   = r["SupplierName"].ToString(),
                            POTotalAmount  = Convert.ToDouble(r["POTotalAmount"]),
                            OrderDate      = Convert.ToDateTime(r["OrderDate"]),
                            PurchaseStatus = r["PurchaseStatus"].ToString()
                        });
            }
            return list;
        }

        // ── Purchase Invoices ────────────────────────────────────────────

        /// <summary>Returns all PurchaseInvoice rows joined to Supplier for display.</summary>
        public List<PurchaseInvoiceEntity> GetAllPurchaseInvoices()
        {
            var list = new List<PurchaseInvoiceEntity>();
            using (var conn = OpenConnection())
            {
                const string sql = @"
                    SELECT pi.PurInvoiceID, pi.PurchaseID, pi.TotalAmount,
                           pi.PaymentStatus, pi.ExpectedDate,
                           sup.SupplierName
                    FROM PurchaseInvoice pi
                    JOIN PurchaseOrder po  ON pi.PurchaseID = po.PurchaseID
                    JOIN Supplier sup      ON po.SupplierID = sup.SupplierID
                    ORDER BY pi.ExpectedDate DESC";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) list.Add(MapPurchaseInvoice(r));
            }
            return list;
        }

        /// <summary>Returns the PurchaseInvoice (if any) linked to a specific PurchaseOrder.</summary>
        public PurchaseInvoiceEntity GetPurchaseInvoiceByPO(string purchaseId)
        {
            if (string.IsNullOrEmpty(purchaseId)) return null;
            using (var conn = OpenConnection())
            {
                const string sql = @"
                    SELECT pi.PurInvoiceID, pi.PurchaseID, pi.TotalAmount,
                           pi.PaymentStatus, pi.ExpectedDate,
                           sup.SupplierName
                    FROM PurchaseInvoice pi
                    JOIN PurchaseOrder po  ON pi.PurchaseID = po.PurchaseID
                    JOIN Supplier sup      ON po.SupplierID = sup.SupplierID
                    WHERE pi.PurchaseID = @pid
                    LIMIT 1";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@pid", purchaseId);
                    using (var r = cmd.ExecuteReader())
                        return r.Read() ? MapPurchaseInvoice(r) : null;
                }
            }
        }

        /// <summary>
        /// Inserts a new PurchaseInvoice row.
        /// Returns the generated PurInvoiceID on success, or throws on failure.
        /// </summary>
        public string InsertPurchaseInvoice(RecordPurchaseInvoiceVM vm)
        {
            // Auto-generate ID: PURINV-yyyyMMdd-NNNN
            string newId = $"PURINV-{DateTime.Today:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper()}";
            using (var conn = OpenConnection())
            {
                const string sql = @"
                    INSERT INTO PurchaseInvoice
                        (PurInvoiceID, PurchaseID, TotalAmount, PaymentStatus, ExpectedDate)
                    VALUES
                        (@id, @po, @total, @status, @expected)";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id",       newId);
                    cmd.Parameters.AddWithValue("@po",       vm.PurchaseID);
                    cmd.Parameters.AddWithValue("@total",    vm.TotalAmount);
                    cmd.Parameters.AddWithValue("@status",   vm.PaymentStatus);
                    cmd.Parameters.AddWithValue("@expected", vm.ExpectedDate.Date);
                    cmd.ExecuteNonQuery();
                }
            }
            return newId;
        }

        // ── Private mappers ─────────────────────────────────────────────
        private static ShipmentEntity MapShipment(MySqlDataReader r) => new ShipmentEntity
        {
            ShipmentID      = r["ShipmentID"].ToString(),
            OrderID         = r["OrderID"].ToString(),
            TrackingNumber  = r["TrackingNumber"].ToString(),
            ShipDate        = Convert.ToDateTime(r["ShipDate"]),
            DeliveryMethod  = r["DeliveryMethod"].ToString(),
            ShipmentStatus  = r["ShipmentStatus"].ToString(),
            ShipmentType    = r["ShipmentType"].ToString(),
            TotalAmount     = Convert.ToDouble(r["TotalAmount"]),
            CustomerName    = r["CustomerName"].ToString(),
            ShippingAddress = r["ShippingAddress"].ToString(),
            DeliveryDate    = r["DeliveryDate"] == DBNull.Value
                                ? (DateTime?)null
                                : Convert.ToDateTime(r["DeliveryDate"])
        };

        private static ShipmentLineEntity MapShipmentLine(MySqlDataReader r) => new ShipmentLineEntity
        {
            ShipmentLineID = r["ShipmentLineID"].ToString(),
            ShipmentID     = r["ShipmentID"].ToString(),
            OrderID        = r["OrderID"].ToString(),
            ItemID         = r["ItemID"].ToString(),
            ItemName       = r["ItemName"].ToString(),
            QtyShipped     = Convert.ToInt32(r["QtyShipped"]),
            QtyOutstanding = r["QtyOutstanding"] == DBNull.Value
                               ? (int?)null
                               : Convert.ToInt32(r["QtyOutstanding"])
        };

        private static DeliveryNoteEntity MapDeliveryNote(MySqlDataReader r) => new DeliveryNoteEntity
        {
            DeliveryID      = r["DeliveryID"].ToString(),
            ShipmentID      = r["ShipmentID"].ToString(),
            DeliveryDate    = Convert.ToDateTime(r["DeliveryDate"]),
            OutstandingQty  = r["Outstanding_qty"] == DBNull.Value
                                ? (int?)null
                                : Convert.ToInt32(r["Outstanding_qty"]),
            ShippingAddress = r["ShippingAddress"].ToString(),
            ShipToName      = r["ShipToName"].ToString()
        };

        private static ReplySlipEntity MapReplySlip(MySqlDataReader r) => new ReplySlipEntity
        {
            SlipID          = r["SlipID"].ToString(),
            DeliveryID      = r["DeliveryID"].ToString(),
            ActualRecipient = r["actualRecipient"].ToString(),
            ReceivedDate    = Convert.ToDateTime(r["ReceivedDate"]),
            RecipientRemark = r["RecipientRemark"] == DBNull.Value
                                ? null
                                : r["RecipientRemark"].ToString()
        };

        private static GoodsReceivedEntity MapReceipt(MySqlDataReader r) => new GoodsReceivedEntity
        {
            ReceiptID         = r["ReceiptID"].ToString(),
            PurchaseID        = r["PurchaseID"].ToString(),
            POLineID          = r["POLineID"].ToString(),
            QtyReceived       = Convert.ToInt32(r["QtyReceived"]),
            ReceiptDate       = Convert.ToDateTime(r["ReceiptDate"]),
            OutstandingQty    = r["Outstanding_QTY"] == DBNull.Value
                                  ? (int?)null
                                  : Convert.ToInt32(r["Outstanding_QTY"]),
            SupplierName      = r["SupplierName"].ToString(),
            RawMaterialItemID = r["RawMaterialItemID"].ToString(),
            ItemName          = r["ItemName"].ToString(),
            WarehouseID       = r["WarehouseID"].ToString(),
            WarehouseLocation = r["WarehouseLocation"].ToString(),
            PurchaseStatus    = r["PurchaseStatus"].ToString(),
            UnitPrice         = Convert.ToDouble(r["UnitPrice"])
        };

        private static PurchaseInvoiceEntity MapPurchaseInvoice(MySqlDataReader r) => new PurchaseInvoiceEntity
        {
            PurInvoiceID  = r["PurInvoiceID"].ToString(),
            PurchaseID    = r["PurchaseID"].ToString(),
            TotalAmount   = Convert.ToDouble(r["TotalAmount"]),
            PaymentStatus = r["PaymentStatus"].ToString(),
            ExpectedDate  = Convert.ToDateTime(r["ExpectedDate"]),
            SupplierName  = r["SupplierName"].ToString()
        };
    }
}
