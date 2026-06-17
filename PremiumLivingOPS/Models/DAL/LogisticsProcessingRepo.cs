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

        // ── Shipments ────────────────────────────────────────────
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

        // ── Edit Shipment ───────────────────────────────────────────
        public void UpdateShipment(string shipmentId, string newStatus)
        {
            using (var conn = OpenConnection())
            {
                const string sql = @"
                    UPDATE Shipment
                    SET    ShipmentStatus = @status
                    WHERE  ShipmentID    = @id";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@status", newStatus);
                    cmd.Parameters.AddWithValue("@id",     shipmentId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpsertReplySlip(string deliveryId, string actualRecipient, string remark)
        {
            using (var conn = OpenConnection())
            {
                string slipId = null;
                using (var chk = new MySqlCommand(
                    "SELECT SlipID FROM ReplySlip WHERE DeliveryID = @did LIMIT 1", conn))
                {
                    chk.Parameters.AddWithValue("@did", deliveryId);
                    var result = chk.ExecuteScalar();
                    if (result != null) slipId = result.ToString();
                }

                if (slipId != null)
                {
                    const string updSql = @"
                        UPDATE ReplySlip
                        SET    actualRecipient = @recip,
                               RecipientRemark = @remark,
                               ReceivedDate    = @rdate
                        WHERE  SlipID = @sid";
                    using (var cmd = new MySqlCommand(updSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@recip",  actualRecipient ?? "");
                        cmd.Parameters.AddWithValue("@remark", string.IsNullOrWhiteSpace(remark) ? (object)DBNull.Value : remark);
                        cmd.Parameters.AddWithValue("@rdate",  DateTime.Today);
                        cmd.Parameters.AddWithValue("@sid",    slipId);
                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    string newSlipId = $"RS-{DateTime.Today:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper()}";
                    const string insSql = @"
                        INSERT INTO ReplySlip
                            (SlipID, DeliveryID, actualRecipient, ReceivedDate, RecipientRemark)
                        VALUES
                            (@sid, @did, @recip, @rdate, @remark)";
                    using (var cmd = new MySqlCommand(insSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@sid",    newSlipId);
                        cmd.Parameters.AddWithValue("@did",    deliveryId);
                        cmd.Parameters.AddWithValue("@recip",  actualRecipient ?? "");
                        cmd.Parameters.AddWithValue("@rdate",  DateTime.Today);
                        cmd.Parameters.AddWithValue("@remark", string.IsNullOrWhiteSpace(remark) ? (object)DBNull.Value : remark);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        // ── Generate Delivery Note ──────────────────────────────────
        public string InsertDeliveryNote(
            string shipmentId, DateTime deliveryDate, int outstandingQty,
            string shippingAddress, string shipToName)
        {
            string newId = $"DN-{DateTime.Today:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper()}";
            using (var conn = OpenConnection())
            {
                const string sql = @"
                    INSERT INTO DeliveryNote
                        (DeliveryID, ShipmentID, DeliveryDate,
                         Outstanding_qty, ShippingAddress, ShipToName)
                    VALUES
                        (@did, @sid, @ddate, @qty, @addr, @name)";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@did",   newId);
                    cmd.Parameters.AddWithValue("@sid",   shipmentId);
                    cmd.Parameters.AddWithValue("@ddate", deliveryDate.Date);
                    cmd.Parameters.AddWithValue("@qty",   outstandingQty);
                    cmd.Parameters.AddWithValue("@addr",  shippingAddress ?? "");
                    cmd.Parameters.AddWithValue("@name",  shipToName      ?? "");
                    cmd.ExecuteNonQuery();
                }
            }
            return newId;
        }

        // ── Generate Reply Slip ──────────────────────────────────────
        public string InsertReplySlip(
            string deliveryId, string actualRecipient, string remark, DateTime receivedDate)
        {
            string newId = $"RS-{DateTime.Today:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper()}";
            using (var conn = OpenConnection())
            {
                const string sql = @"
                    INSERT INTO ReplySlip
                        (SlipID, DeliveryID, actualRecipient, ReceivedDate, RecipientRemark)
                    VALUES
                        (@sid, @did, @recip, @rdate, @remark)";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@sid",    newId);
                    cmd.Parameters.AddWithValue("@did",    deliveryId);
                    cmd.Parameters.AddWithValue("@recip",  actualRecipient);
                    cmd.Parameters.AddWithValue("@rdate",  receivedDate.Date);
                    cmd.Parameters.AddWithValue("@remark", string.IsNullOrWhiteSpace(remark) ? (object)DBNull.Value : remark);
                    cmd.ExecuteNonQuery();
                }
            }
            return newId;
        }

        // ── Delete Shipment ───────────────────────────────────────────
        public void DeleteShipment(string shipmentId)
        {
            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                try
                {
                    ExecuteNonQuery(conn, tx, @"
                        DELETE rs FROM ReplySlip rs
                        JOIN DeliveryNote dn ON rs.DeliveryID = dn.DeliveryID
                        WHERE dn.ShipmentID = @id", shipmentId);
                    ExecuteNonQuery(conn, tx, @"
                        DELETE FROM DeliveryNote WHERE ShipmentID = @id", shipmentId);
                    ExecuteNonQuery(conn, tx, @"
                        DELETE FROM ShipmentLine WHERE ShipmentID = @id", shipmentId);
                    ExecuteNonQuery(conn, tx, @"
                        DELETE FROM Shipment WHERE ShipmentID = @id", shipmentId);
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
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

        public string InsertPurchaseInvoice(RecordPurchaseInvoiceVM vm)
        {
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

        // ── CSV Import: Receipt ──────────────────────────────────────
        /// <summary>
        /// Checks whether a PurchaseID exists in PurchaseOrder.
        /// Used by the CSV import validator.
        /// </summary>
        public bool PurchaseOrderExists(string purchaseId)
        {
            using (var conn = OpenConnection())
            using (var cmd = new MySqlCommand(
                "SELECT COUNT(1) FROM PurchaseOrder WHERE PurchaseID = @id", conn))
            {
                cmd.Parameters.AddWithValue("@id", purchaseId);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        /// <summary>
        /// Checks whether a POLineID exists AND belongs to the given PurchaseID.
        /// </summary>
        public bool POLineExists(string poLineId, string purchaseId)
        {
            using (var conn = OpenConnection())
            using (var cmd = new MySqlCommand(
                "SELECT COUNT(1) FROM PurchaseOrderLine WHERE POLineID = @lid AND PurchaseID = @pid", conn))
            {
                cmd.Parameters.AddWithValue("@lid", poLineId);
                cmd.Parameters.AddWithValue("@pid", purchaseId);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        /// <summary>
        /// Inserts one Receipt row inside an existing transaction.
        /// ReceiptID is auto-generated as RCPT-yyyyMMdd-XXXX.
        /// </summary>
        public void InsertReceipt(
            MySqlConnection conn,
            MySqlTransaction tx,
            ReceiptImportRow row)
        {
            string newId = $"RCPT-{DateTime.Today:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper()}";
            const string sql = @"
                INSERT INTO Receipt
                    (ReceiptID, PurchaseID, POLineID,
                     QtyReceived, ReceiptDate, Outstanding_QTY)
                VALUES
                    (@rid, @pid, @lid, @qty, @rdate, @oqty)";
            using (var cmd = new MySqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@rid",   newId);
                cmd.Parameters.AddWithValue("@pid",   row.PurchaseID);
                cmd.Parameters.AddWithValue("@lid",   row.POLineID);
                cmd.Parameters.AddWithValue("@qty",   row.QtyReceived);
                cmd.Parameters.AddWithValue("@rdate", row.ReceiptDate.Date);
                cmd.Parameters.AddWithValue("@oqty",  row.OutstandingQty.HasValue
                                                        ? (object)row.OutstandingQty.Value
                                                        : DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Opens a single transaction and inserts all validated rows.
        /// Returns the number of rows actually inserted.
        /// </summary>
        public int BulkInsertReceipts(List<ReceiptImportRow> rows)
        {
            int count = 0;
            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                try
                {
                    foreach (var row in rows)
                    {
                        InsertReceipt(conn, tx, row);
                        count++;
                    }
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
            return count;
        }

        // ── Private helpers ────────────────────────────────────────────
        private static void ExecuteNonQuery(
            MySqlConnection conn, MySqlTransaction tx, string sql, string shipmentId)
        {
            using var cmd = new MySqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@id", shipmentId);
            cmd.ExecuteNonQuery();
        }

        // ── Private mappers ────────────────────────────────────────────
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
