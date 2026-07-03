using MySql.Data.MySqlClient;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PremiumLivingOPS.Models.DAL
{
    /// <summary>
    /// Data-access layer for Logistics Processing.
    /// All SQL lives here — no UI, no business logic.
    /// </summary>
    public class LogisticsProcessingRepo
    {
        // ── Shipment ────────────────────────────────────────────────
        public List<ShipmentEntity> SearchShipments(
            string statusFilter, string keyword, DateTime? dateFrom)
        {
            var list = new List<ShipmentEntity>();
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            var sql = @"
                SELECT s.ShipmentID, s.OrderID, s.TrackingNumber,
                       s.ShipDate, s.DeliveryMethod, s.ShipmentStatus,
                       s.ShipmentType, s.TotalAmount,
                       COALESCE(o.ShippingAddress,'') AS ShippingAddress,
                       o.DeliveryDate,
                       COALESCE(c.CustomerName,'')   AS CustomerName
                FROM   Shipment s
                JOIN   `Order`   o ON o.OrderID    = s.OrderID
                JOIN   Customer  c ON c.CustomerID = o.CustomerID
                WHERE  (@status IS NULL OR s.ShipmentStatus = @status)
                  AND  (@kw IS NULL OR s.ShipmentID LIKE @kw OR s.OrderID LIKE @kw
                        OR c.CustomerName LIKE @kw)
                  AND  (@from IS NULL OR s.ShipDate >= @from)
                ORDER  BY s.ShipDate DESC";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@status", (object)statusFilter ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@kw",     string.IsNullOrEmpty(keyword) ? (object)DBNull.Value : "%"+keyword+"%");
            cmd.Parameters.AddWithValue("@from",   (object)dateFrom ?? DBNull.Value);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                list.Add(MapShipment(rd));
            return list;
        }

        public ShipmentEntity GetShipmentById(string shipmentId)
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            var sql = @"
                SELECT s.ShipmentID, s.OrderID, s.TrackingNumber,
                       s.ShipDate, s.DeliveryMethod, s.ShipmentStatus,
                       s.ShipmentType, s.TotalAmount,
                       COALESCE(o.ShippingAddress,'') AS ShippingAddress,
                       o.DeliveryDate,
                       COALESCE(c.CustomerName,'')   AS CustomerName
                FROM   Shipment s
                JOIN   `Order`   o ON o.OrderID    = s.OrderID
                JOIN   Customer  c ON c.CustomerID = o.CustomerID
                WHERE  s.ShipmentID = @id";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", shipmentId);
            using var rd = cmd.ExecuteReader();
            if (!rd.Read()) return null;
            return MapShipment(rd);
        }

        private static ShipmentEntity MapShipment(MySqlDataReader rd) => new ShipmentEntity
        {
            ShipmentID      = rd.GetString("ShipmentID"),
            OrderID         = rd.GetString("OrderID"),
            TrackingNumber  = rd.GetString("TrackingNumber"),
            ShipDate        = rd.GetDateTime("ShipDate"),
            DeliveryMethod  = rd.GetString("DeliveryMethod"),
            ShipmentStatus  = rd.GetString("ShipmentStatus"),
            ShipmentType    = rd.GetString("ShipmentType"),
            TotalAmount     = rd.GetDouble("TotalAmount"),
            ShippingAddress = rd.GetString("ShippingAddress"),
            DeliveryDate    = rd["DeliveryDate"] as DateTime?,
            CustomerName    = rd.GetString("CustomerName")
        };

        public List<ShipmentLineEntity> GetShipmentLines(string shipmentId)
        {
            var list = new List<ShipmentLineEntity>();
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            var sql = @"
                SELECT sl.ShipmentLineID, sl.ShipmentID, sl.OrderID, sl.ItemID,
                       sl.QtyShipped, sl.QtyOutstanding,
                       COALESCE(i.ItemName,'') AS ItemName
                FROM   ShipmentLine sl
                LEFT JOIN Item i ON i.ItemID = sl.ItemID
                WHERE  sl.ShipmentID = @id";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", shipmentId);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                list.Add(new ShipmentLineEntity
                {
                    ShipmentLineID  = rd.GetString("ShipmentLineID"),
                    ShipmentID      = rd.GetString("ShipmentID"),
                    OrderID         = rd.GetString("OrderID"),
                    ItemID          = rd.GetString("ItemID"),
                    ItemName        = rd.GetString("ItemName"),
                    QtyShipped      = rd.GetInt32("QtyShipped"),
                    QtyOutstanding  = rd["QtyOutstanding"] as int?
                });
            return list;
        }

        public void UpdateShipment(string shipmentId, string newStatus)
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            var sql = "UPDATE Shipment SET ShipmentStatus=@s WHERE ShipmentID=@id";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@s",  newStatus);
            cmd.Parameters.AddWithValue("@id", shipmentId);
            cmd.ExecuteNonQuery();
        }

        /// <summary>Updates DeliveryMethod and ShipDate for an existing shipment.</summary>
        public void ScheduleShipment(
            string   shipmentId,
            DateTime scheduledDate,
            string   deliveryMethod)
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            var sql = @"UPDATE Shipment
                        SET    DeliveryMethod = @method,
                               ShipDate       = @date
                        WHERE  ShipmentID     = @id";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@method", deliveryMethod);
            cmd.Parameters.AddWithValue("@date",   scheduledDate);
            cmd.Parameters.AddWithValue("@id",     shipmentId);
            cmd.ExecuteNonQuery();
        }

        public void DeleteShipment(string shipmentId)
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();
            new MySqlCommand($"DELETE FROM ReplySlip WHERE DeliveryID IN (SELECT DeliveryID FROM DeliveryNote WHERE ShipmentID='{shipmentId}')", conn, tx).ExecuteNonQuery();
            new MySqlCommand($"DELETE FROM DeliveryNote WHERE ShipmentID='{shipmentId}'", conn, tx).ExecuteNonQuery();
            new MySqlCommand($"DELETE FROM ShipmentLine WHERE ShipmentID='{shipmentId}'", conn, tx).ExecuteNonQuery();
            new MySqlCommand($"DELETE FROM Shipment WHERE ShipmentID='{shipmentId}'", conn, tx).ExecuteNonQuery();
            tx.Commit();
        }

        // ── Schedule Shipment Wizard — new shipment creation ─────────

        public List<OrderSummary> GetSchedulableOrders()
        {
            var list = new List<OrderSummary>();
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            var sql = @"
                SELECT o.OrderID,
                       COALESCE(c.CustomerName,'')    AS CustomerName,
                       o.OrderStatus,
                       COALESCE(o.ShippingAddress,'') AS ShippingAddress,
                       COALESCE(c.PhoneNumber,'')     AS ContactName,
                       o.DeliveryDate,
                       COALESCE(o.GrandTotal, 0)      AS GrandTotal
                FROM   `Order` o
                JOIN   Customer c ON c.CustomerID = o.CustomerID
                WHERE  o.OrderStatus IN ('Processing','Partially Delivered','Pending')
                ORDER  BY o.DeliveryDate ASC, o.OrderID ASC";
            using var cmd = new MySqlCommand(sql, conn);
            using var rd  = cmd.ExecuteReader();
            while (rd.Read())
                list.Add(new OrderSummary
                {
                    OrderID         = rd.GetString("OrderID"),
                    CustomerName    = rd.GetString("CustomerName"),
                    OrderStatus     = rd.GetString("OrderStatus"),
                    ShippingAddress = rd.GetString("ShippingAddress"),
                    ContactName     = rd.GetString("ContactName"),
                    DeliveryDate    = rd.GetDateTime("DeliveryDate"),
                    GrandTotal      = rd.GetDouble("GrandTotal")
                });
            return list;
        }

        public List<OrderLineDetail> GetOrderLinesWithShipmentStatus(string orderId)
        {
            var list = new List<OrderLineDetail>();
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            var sql = @"
                SELECT ol.ItemID,
                       COALESCE(i.ItemName,'') AS ItemName,
                       ol.Quantity,
                       ol.Price                AS UnitPrice,
                       COALESCE(shipped.TotalShipped, 0) AS QtyAlreadyShipped
                FROM   OrderLine ol
                LEFT JOIN Item i ON i.ItemID = ol.ItemID
                LEFT JOIN (
                    SELECT sl.ItemID,
                           SUM(sl.QtyShipped) AS TotalShipped
                    FROM   ShipmentLine sl
                    JOIN   Shipment     s  ON s.ShipmentID = sl.ShipmentID
                    WHERE  s.OrderID = @oid
                    GROUP  BY sl.ItemID
                ) shipped ON shipped.ItemID = ol.ItemID
                WHERE  ol.OrderID = @oid
                ORDER  BY ol.ItemID";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@oid", orderId);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                list.Add(new OrderLineDetail
                {
                    ItemID            = rd.GetString("ItemID"),
                    ItemName          = rd.GetString("ItemName"),
                    Quantity          = rd.GetInt32("Quantity"),
                    UnitPrice         = rd.GetDouble("UnitPrice"),
                    QtyAlreadyShipped = rd.GetInt32("QtyAlreadyShipped")
                });
            return list;
        }

        public List<string> GetExistingShipmentSuffixes(string orderId)
        {
            var list = new List<string>();
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            var sql = "SELECT ShipmentID FROM Shipment WHERE OrderID = @oid";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@oid", orderId);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                string sid = rd.GetString("ShipmentID");
                int thirdDash = -1, dashCount = 0;
                for (int i = 0; i < sid.Length; i++)
                {
                    if (sid[i] == '-') { dashCount++; if (dashCount == 3) { thirdDash = i; break; } }
                }
                if (thirdDash < 0)
                {
                    int secondDash = sid.LastIndexOf('-');
                    if (secondDash >= 0 && secondDash < sid.Length - 1)
                        list.Add(sid.Substring(secondDash + 1));
                }
                else
                {
                    list.Add(sid.Substring(thirdDash + 1));
                }
            }
            return list;
        }

        public double ComputeShipmentTotal(string orderId, List<ShipmentLineRequest> lines)
        {
            if (lines == null || lines.Count == 0) return 0.0;
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            double total = 0.0;
            foreach (var ln in lines)
            {
                var cmd = new MySqlCommand(
                    "SELECT COALESCE(Price, 0) FROM OrderLine WHERE OrderID=@oid AND ItemID=@iid LIMIT 1",
                    conn);
                cmd.Parameters.AddWithValue("@oid", orderId);
                cmd.Parameters.AddWithValue("@iid", ln.ItemID);
                var result = cmd.ExecuteScalar();
                double unitPrice = result == null || result == DBNull.Value ? 0.0 : Convert.ToDouble(result);
                total += unitPrice * ln.QtyShip;
            }
            return total;
        }

        private int GetMaxShipmentLineSeq(MySqlConnection conn, MySqlTransaction tx, string dateStr)
        {
            var cmd = new MySqlCommand(
                @"SELECT COALESCE(MAX(CAST(RIGHT(ShipmentLineID, 4) AS UNSIGNED)), 0)
                  FROM   ShipmentLine
                  WHERE  ShipmentLineID LIKE @prefix",
                conn, tx);
            cmd.Parameters.AddWithValue("@prefix", $"SHPL-{dateStr}-%");
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void CreateScheduledShipment(
            string                   shipmentId,
            string                   orderId,
            DateTime                 shipDate,
            string                   deliveryMethod,
            string                   shipmentType,
            double                   totalAmount,
            List<ShipmentLineRequest> lines)
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            var insShip = new MySqlCommand(@"
                INSERT INTO Shipment
                    (ShipmentID, OrderID, TrackingNumber, ShipDate,
                     DeliveryMethod, ShipmentStatus, ShipmentType, TotalAmount)
                VALUES
                    (@sid, @oid, '', @sd, @dm, 'Pending', @st, @amt)",
                conn, tx);
            insShip.Parameters.AddWithValue("@sid", shipmentId);
            insShip.Parameters.AddWithValue("@oid", orderId);
            insShip.Parameters.AddWithValue("@sd",  shipDate);
            insShip.Parameters.AddWithValue("@dm",  deliveryMethod);
            insShip.Parameters.AddWithValue("@st",  shipmentType);
            insShip.Parameters.AddWithValue("@amt", totalAmount);
            insShip.ExecuteNonQuery();

            string dateStr = shipDate.ToString("yyyyMMdd");
            int seq = GetMaxShipmentLineSeq(conn, tx, dateStr);

            for (int i = 0; i < lines.Count; i++)
            {
                seq++;
                var ln     = lines[i];
                string slId = $"SHPL-{dateStr}-{seq:D4}";

                var insLine = new MySqlCommand(@"
                    INSERT INTO ShipmentLine
                        (ShipmentLineID, ShipmentID, OrderID, ItemID,
                         QtyShipped, QtyOutstanding)
                    VALUES
                        (@slid, @sid, @oid, @iid, @qty, @out)",
                    conn, tx);
                insLine.Parameters.AddWithValue("@slid", slId);
                insLine.Parameters.AddWithValue("@sid",  shipmentId);
                insLine.Parameters.AddWithValue("@oid",  orderId);
                insLine.Parameters.AddWithValue("@iid",  ln.ItemID);
                insLine.Parameters.AddWithValue("@qty",  ln.QtyShip);
                insLine.Parameters.AddWithValue("@out",  ln.Remain);
                insLine.ExecuteNonQuery();
            }

            var updOrder = new MySqlCommand(@"
                UPDATE `Order`
                SET    OrderStatus = 'Partially Delivered'
                WHERE  OrderID     = @oid
                  AND  OrderStatus NOT IN ('Completed','Cancelled')",
                conn, tx);
            updOrder.Parameters.AddWithValue("@oid", orderId);
            updOrder.ExecuteNonQuery();

            tx.Commit();
        }

        // ── Delivery Note / Reply Slip ───────────────────────────────
        public DeliveryNoteEntity GetDeliveryNoteByShipment(string shipmentId)
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            var sql = "SELECT * FROM DeliveryNote WHERE ShipmentID=@id LIMIT 1";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", shipmentId);
            using var rd = cmd.ExecuteReader();
            if (!rd.Read()) return null;
            return new DeliveryNoteEntity
            {
                DeliveryID      = rd.GetString("DeliveryID"),
                ShipmentID      = rd.GetString("ShipmentID"),
                DeliveryDate    = rd.GetDateTime("DeliveryDate"),
                OutstandingQty  = rd["Outstanding_qty"] as int?,
                ShippingAddress = rd.GetString("ShippingAddress"),
                ShipToName      = rd.GetString("ShipToName")
            };
        }

        public string InsertDeliveryNote(string shipmentId, DateTime deliveryDate,
                                         int outstandingQty, string address, string shipToName)
        {
            string newId = "DN-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            var sql = @"INSERT INTO DeliveryNote
                        (DeliveryID,ShipmentID,DeliveryDate,Outstanding_qty,ShippingAddress,ShipToName)
                        VALUES (@id,@sid,@dd,@oq,@addr,@stn)";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id",   newId);
            cmd.Parameters.AddWithValue("@sid",  shipmentId);
            cmd.Parameters.AddWithValue("@dd",   deliveryDate);
            cmd.Parameters.AddWithValue("@oq",   outstandingQty);
            cmd.Parameters.AddWithValue("@addr", address);
            cmd.Parameters.AddWithValue("@stn",  shipToName);
            cmd.ExecuteNonQuery();
            return newId;
        }

        public ReplySlipEntity GetReplySlipByDelivery(string deliveryId)
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            var sql = "SELECT * FROM ReplySlip WHERE DeliveryID=@id LIMIT 1";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", deliveryId);
            using var rd = cmd.ExecuteReader();
            if (!rd.Read()) return null;
            return new ReplySlipEntity
            {
                SlipID          = rd.GetString("SlipID"),
                DeliveryID      = rd.GetString("DeliveryID"),
                ActualRecipient = rd.GetString("actualRecipient"),
                ReceivedDate    = rd.GetDateTime("ReceivedDate"),
                RecipientRemark = rd["RecipientRemark"] as string
            };
        }

        public void UpsertReplySlip(string deliveryId, string actualRecipient, string remark)
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            var check = new MySqlCommand("SELECT COUNT(*) FROM ReplySlip WHERE DeliveryID=@id", conn);
            check.Parameters.AddWithValue("@id", deliveryId);
            bool exists = Convert.ToInt32(check.ExecuteScalar()) > 0;
            if (exists)
            {
                var upd = new MySqlCommand("UPDATE ReplySlip SET actualRecipient=@r,RecipientRemark=@rm WHERE DeliveryID=@id", conn);
                upd.Parameters.AddWithValue("@r",  actualRecipient);
                upd.Parameters.AddWithValue("@rm", (object)remark ?? DBNull.Value);
                upd.Parameters.AddWithValue("@id", deliveryId);
                upd.ExecuteNonQuery();
            }
            else
            {
                string newId = "RS-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                var ins = new MySqlCommand(@"INSERT INTO ReplySlip(SlipID,DeliveryID,actualRecipient,ReceivedDate,RecipientRemark)
                                            VALUES(@sid,@did,@r,@rd,@rm)", conn);
                ins.Parameters.AddWithValue("@sid", newId);
                ins.Parameters.AddWithValue("@did", deliveryId);
                ins.Parameters.AddWithValue("@r",   actualRecipient);
                ins.Parameters.AddWithValue("@rd",  DateTime.Today);
                ins.Parameters.AddWithValue("@rm",  (object)remark ?? DBNull.Value);
                ins.ExecuteNonQuery();
            }
        }

        public string InsertReplySlip(string deliveryId, string actualRecipient,
                                      string remark, DateTime receivedDate)
        {
            string newId = "RS-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            var ins = new MySqlCommand(@"INSERT INTO ReplySlip(SlipID,DeliveryID,actualRecipient,ReceivedDate,RecipientRemark)
                                        VALUES(@sid,@did,@r,@rd,@rm)", conn);
            ins.Parameters.AddWithValue("@sid", newId);
            ins.Parameters.AddWithValue("@did", deliveryId);
            ins.Parameters.AddWithValue("@r",   actualRecipient);
            ins.Parameters.AddWithValue("@rd",  receivedDate);
            ins.Parameters.AddWithValue("@rm",  (object)remark ?? DBNull.Value);
            ins.ExecuteNonQuery();
            return newId;
        }

        // ── Handling Goods Received — Receipt list ───────────────────
        public List<GoodsReceivedEntity> SearchReceipts(
            string statusFilter, string keyword, DateTime? dateFrom)
        {
            var list = new List<GoodsReceivedEntity>();
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            var sql = @"
                SELECT r.ReceiptID, r.PurchaseID, r.POLineID,
                       r.QtyReceived, r.ReceiptDate, r.Outstanding_QTY AS OutstandingQty,
                       po.PurchaseStatus,
                       COALESCE(s.SupplierName,'') AS SupplierName,
                       pol.RawMaterialItemID, COALESCE(i.ItemName,'') AS ItemName,
                       pol.WarehouseID,
                       COALESCE(w.WarehouseLocation,'') AS WarehouseLocation,
                       pol.UnitPrice
                FROM   Receipt r
                JOIN   PurchaseOrder     po  ON po.PurchaseID  = r.PurchaseID
                JOIN   Supplier          s   ON s.SupplierID   = po.SupplierID
                JOIN   PurchaseOrderLine pol ON pol.POLineID    = r.POLineID
                JOIN   Item              i   ON i.ItemID        = pol.RawMaterialItemID
                JOIN   Warehouse         w   ON w.WarehouseID   = pol.WarehouseID
                WHERE  (@status IS NULL OR po.PurchaseStatus = @status)
                  AND  (@kw IS NULL OR r.ReceiptID LIKE @kw OR r.PurchaseID LIKE @kw
                        OR s.SupplierName LIKE @kw)
                  AND  (@from IS NULL OR r.ReceiptDate >= @from)
                ORDER  BY r.ReceiptDate DESC";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@status", (object)statusFilter ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@kw",     string.IsNullOrEmpty(keyword) ? (object)DBNull.Value : "%"+keyword+"%");
            cmd.Parameters.AddWithValue("@from",   (object)dateFrom ?? DBNull.Value);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                list.Add(MapReceipt(rd));
            return list;
        }

        public List<GoodsReceivedEntity> GetReceiptsByPurchaseID(string purchaseId)
        {
            var list = new List<GoodsReceivedEntity>();
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            var sql = @"
                SELECT r.ReceiptID, r.PurchaseID, r.POLineID,
                       r.QtyReceived, r.ReceiptDate, r.Outstanding_QTY AS OutstandingQty,
                       po.PurchaseStatus,
                       COALESCE(s.SupplierName,'') AS SupplierName,
                       pol.RawMaterialItemID, COALESCE(i.ItemName,'') AS ItemName,
                       pol.WarehouseID,
                       COALESCE(w.WarehouseLocation,'') AS WarehouseLocation,
                       pol.UnitPrice
                FROM   Receipt r
                JOIN   PurchaseOrder     po  ON po.PurchaseID  = r.PurchaseID
                JOIN   Supplier          s   ON s.SupplierID   = po.SupplierID
                JOIN   PurchaseOrderLine pol ON pol.POLineID    = r.POLineID
                JOIN   Item              i   ON i.ItemID        = pol.RawMaterialItemID
                JOIN   Warehouse         w   ON w.WarehouseID   = pol.WarehouseID
                WHERE  r.PurchaseID = @pid
                ORDER  BY r.ReceiptDate, r.ReceiptID";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@pid", purchaseId);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                list.Add(MapReceipt(rd));
            return list;
        }

        private static GoodsReceivedEntity MapReceipt(MySqlDataReader rd) => new GoodsReceivedEntity
        {
            ReceiptID         = rd.GetString("ReceiptID"),
            PurchaseID        = rd.GetString("PurchaseID"),
            POLineID          = rd.GetString("POLineID"),
            QtyReceived       = rd.GetInt32("QtyReceived"),
            ReceiptDate       = rd.GetDateTime("ReceiptDate"),
            OutstandingQty    = rd["OutstandingQty"] as int?,
            PurchaseStatus    = rd.GetString("PurchaseStatus"),
            SupplierName      = rd.GetString("SupplierName"),
            RawMaterialItemID = rd.GetString("RawMaterialItemID"),
            ItemName          = rd.GetString("ItemName"),
            WarehouseID       = rd.GetString("WarehouseID"),
            WarehouseLocation = rd.GetString("WarehouseLocation"),
            UnitPrice         = rd.GetDouble("UnitPrice")
        };

        // ── PurchaseOrder ────────────────────────────────────────────
        public List<PurchaseOrderEntity> GetAllPurchaseOrders()
        {
            var list = new List<PurchaseOrderEntity>();
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            var sql = @"
                SELECT po.PurchaseID, po.RequestID, po.SupplierID,
                       COALESCE(s.SupplierName,'') AS SupplierName,
                       po.POTotalAmount, po.OrderDate, po.PurchaseStatus
                FROM   PurchaseOrder po
                LEFT JOIN Supplier s ON s.SupplierID = po.SupplierID
                ORDER  BY po.OrderDate DESC";
            using var cmd = new MySqlCommand(sql, conn);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                list.Add(MapPO(rd));
            return list;
        }

        public List<PurchaseOrderLineEntity> GetPODetailLines(string purchaseId)
        {
            var list = new List<PurchaseOrderLineEntity>();
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            var sql = @"
                SELECT pol.POLineID, pol.PurchaseID, pol.RawMaterialItemID,
                       COALESCE(i.ItemName,'') AS MaterialName,
                       COALESCE(rm.MaterialType,'') AS MaterialType,
                       pol.WarehouseID,
                       COALESCE(w.WarehouseLocation,'') AS WarehouseLocation,
                       pol.OrderQty, pol.UnitPrice
                FROM   PurchaseOrderLine pol
                JOIN   Item              i   ON i.ItemID       = pol.RawMaterialItemID
                JOIN   RawMaterial       rm  ON rm.ItemID      = pol.RawMaterialItemID
                JOIN   Warehouse         w   ON w.WarehouseID  = pol.WarehouseID
                WHERE  pol.PurchaseID = @pid
                ORDER  BY pol.POLineID";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@pid", purchaseId);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                list.Add(new PurchaseOrderLineEntity
                {
                    POLineID          = rd.GetString("POLineID"),
                    PurchaseID        = rd.GetString("PurchaseID"),
                    RawMaterialItemID = rd.GetString("RawMaterialItemID"),
                    MaterialName      = rd.GetString("MaterialName"),
                    MaterialType      = rd.GetString("MaterialType"),
                    WarehouseID       = rd.GetString("WarehouseID"),
                    WarehouseLocation = rd.GetString("WarehouseLocation"),
                    OrderQty          = rd.GetInt32("OrderQty"),
                    UnitPrice         = rd.GetDouble("UnitPrice")
                });
            return list;
        }

        public (PurchaseOrderEntity po, string supplierPhone, string supplierAddress,
                string invoiceStatus) GetPOHeaderFull(string purchaseId)
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            var sql = @"
                SELECT po.PurchaseID, po.RequestID, po.SupplierID,
                       COALESCE(s.SupplierName,'')    AS SupplierName,
                       COALESCE(s.PhoneNumber,'')     AS PhoneNumber,
                       COALESCE(s.SupplierAddress,'') AS SupplierAddress,
                       po.POTotalAmount, po.OrderDate, po.PurchaseStatus,
                       COALESCE(pi.PaymentStatus,'N/A') AS InvoiceStatus
                FROM   PurchaseOrder po
                JOIN   Supplier s ON s.SupplierID = po.SupplierID
                LEFT JOIN PurchaseInvoice pi ON pi.PurchaseID = po.PurchaseID
                WHERE  po.PurchaseID = @pid
                LIMIT  1";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@pid", purchaseId);
            using var rd = cmd.ExecuteReader();
            if (!rd.Read()) return (null, "", "", "");
            var po = MapPO(rd);
            return (po,
                    rd.GetString("PhoneNumber"),
                    rd.GetString("SupplierAddress"),
                    rd.GetString("InvoiceStatus"));
        }

        private static PurchaseOrderEntity MapPO(MySqlDataReader rd) => new PurchaseOrderEntity
        {
            PurchaseID     = rd.GetString("PurchaseID"),
            RequestID      = rd["RequestID"] as string,
            SupplierID     = rd.GetString("SupplierID"),
            SupplierName   = rd.GetString("SupplierName"),
            POTotalAmount  = rd.GetDouble("POTotalAmount"),
            OrderDate      = rd.GetDateTime("OrderDate"),
            PurchaseStatus = rd.GetString("PurchaseStatus")
        };

        // ── PurchaseInvoice ──────────────────────────────────────────
        public List<PurchaseInvoiceEntity> GetAllPurchaseInvoices()
        {
            var list = new List<PurchaseInvoiceEntity>();
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            var sql = @"
                SELECT pi.PurInvoiceID, pi.PurchaseID, pi.TotalAmount,
                       pi.PaymentStatus, pi.ExpectedDate,
                       COALESCE(s.SupplierName,'') AS SupplierName
                FROM   PurchaseInvoice pi
                JOIN   PurchaseOrder   po ON po.PurchaseID = pi.PurchaseID
                JOIN   Supplier        s  ON s.SupplierID  = po.SupplierID
                ORDER  BY pi.ExpectedDate DESC";
            using var cmd = new MySqlCommand(sql, conn);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                list.Add(new PurchaseInvoiceEntity
                {
                    PurInvoiceID  = rd.GetString("PurInvoiceID"),
                    PurchaseID    = rd.GetString("PurchaseID"),
                    TotalAmount   = rd.GetDouble("TotalAmount"),
                    PaymentStatus = rd.GetString("PaymentStatus"),
                    ExpectedDate  = rd.GetDateTime("ExpectedDate"),
                    SupplierName  = rd.GetString("SupplierName")
                });
            return list;
        }

        public PurchaseInvoiceEntity GetPurchaseInvoiceByPO(string purchaseId)
        {
            if (string.IsNullOrEmpty(purchaseId)) return null;
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            var sql = @"
                SELECT pi.PurInvoiceID, pi.PurchaseID, pi.TotalAmount,
                       pi.PaymentStatus, pi.ExpectedDate,
                       COALESCE(s.SupplierName,'') AS SupplierName
                FROM   PurchaseInvoice pi
                JOIN   PurchaseOrder   po ON po.PurchaseID = pi.PurchaseID
                JOIN   Supplier        s  ON s.SupplierID  = po.SupplierID
                WHERE  pi.PurchaseID = @pid LIMIT 1";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@pid", purchaseId);
            using var rd = cmd.ExecuteReader();
            if (!rd.Read()) return null;
            return new PurchaseInvoiceEntity
            {
                PurInvoiceID  = rd.GetString("PurInvoiceID"),
                PurchaseID    = rd.GetString("PurchaseID"),
                TotalAmount   = rd.GetDouble("TotalAmount"),
                PaymentStatus = rd.GetString("PaymentStatus"),
                ExpectedDate  = rd.GetDateTime("ExpectedDate"),
                SupplierName  = rd.GetString("SupplierName")
            };
        }

        public string InsertPurchaseInvoice(RecordPurchaseInvoiceVM vm)
        {
            string newId = "PINV-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            var sql = @"INSERT INTO PurchaseInvoice
                        (PurInvoiceID,PurchaseID,TotalAmount,PaymentStatus,ExpectedDate)
                        VALUES(@id,@pid,@amt,@ps,@ed)";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id",  newId);
            cmd.Parameters.AddWithValue("@pid", vm.PurchaseID);
            cmd.Parameters.AddWithValue("@amt", vm.TotalAmount);
            cmd.Parameters.AddWithValue("@ps",  vm.PaymentStatus);
            cmd.Parameters.AddWithValue("@ed",  vm.ExpectedDate);
            cmd.ExecuteNonQuery();
            return newId;
        }

        // ── CSV Import helpers ───────────────────────────────────────
        public bool PurchaseOrderExists(string purchaseId)
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            var cmd = new MySqlCommand("SELECT COUNT(*) FROM PurchaseOrder WHERE PurchaseID=@id", conn);
            cmd.Parameters.AddWithValue("@id", purchaseId);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        public bool POLineExists(string poLineId, string purchaseId)
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            var cmd = new MySqlCommand(
                "SELECT COUNT(*) FROM PurchaseOrderLine WHERE POLineID=@lid AND PurchaseID=@pid", conn);
            cmd.Parameters.AddWithValue("@lid", poLineId);
            cmd.Parameters.AddWithValue("@pid", purchaseId);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        /// <summary>
        /// Queries the max existing sequence number for a given receipt date string (yyyyMMdd)
        /// within the same transaction, so the new ReceiptID is globally unique even when
        /// multiple CSV uploads happen on the same calendar day.
        /// ReceiptID format: REC-{yyyyMMdd}-{seq:D4}  e.g. REC-20260704-0003
        /// </summary>
        private int GetMaxReceiptSeq(MySqlConnection conn, MySqlTransaction tx, string dateStr)
        {
            // ReceiptID: REC-20260704-0001  → last 4 chars after final '-'
            var cmd = new MySqlCommand(
                @"SELECT COALESCE(MAX(CAST(RIGHT(ReceiptID, 4) AS UNSIGNED)), 0)
                  FROM   Receipt
                  WHERE  ReceiptID LIKE @prefix",
                conn, tx);
            cmd.Parameters.AddWithValue("@prefix", $"REC-{dateStr}-%");
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        /// <summary>
        /// Bulk-inserts validated receipt rows and auto-updates PurchaseOrder.PurchaseStatus.
        ///
        /// FIX 1 (revised) — ReceiptID uniqueness across multiple uploads:
        ///   Old approach used CSV RowNumber as suffix → duplicate key on second upload
        ///   because RowNumber resets to 1 for every new file.
        ///   New approach: query MAX(seq) for today's date from the DB at the START of
        ///   the transaction, then increment per inserted row.
        ///   REC-{ReceiptDate:yyyyMMdd}-{dbSeq:D4}  e.g. REC-20260704-0003
        ///   This is safe under concurrent inserts because the query runs inside
        ///   the same transaction with a table-level intention lock.
        ///
        /// FIX 2 — PurchaseStatus auto-update (unchanged).
        /// </summary>
        public int BulkInsertReceipts(List<ReceiptImportRow> rows)
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();
            int count = 0;

            // Pre-fetch the current max sequence per distinct receipt date in this batch.
            // Keyed by dateStr (yyyyMMdd) so multi-date batches each get their own counter.
            var seqByDate = new System.Collections.Generic.Dictionary<string, int>();

            foreach (var row in rows)
            {
                string dateStr = row.ReceiptDate.ToString("yyyyMMdd");

                // Initialise counter for this date on first encounter
                if (!seqByDate.ContainsKey(dateStr))
                    seqByDate[dateStr] = GetMaxReceiptSeq(conn, tx, dateStr);

                // Increment and build the new ID
                seqByDate[dateStr]++;
                string newId = $"REC-{dateStr}-{seqByDate[dateStr]:D4}";

                var ins = new MySqlCommand(@"
                    INSERT INTO Receipt(ReceiptID,PurchaseID,POLineID,QtyReceived,ReceiptDate,Outstanding_QTY)
                    VALUES(@rid,@pid,@lid,@qty,@dt,@out)", conn, tx);
                ins.Parameters.AddWithValue("@rid", newId);
                ins.Parameters.AddWithValue("@pid", row.PurchaseID);
                ins.Parameters.AddWithValue("@lid", row.POLineID);
                ins.Parameters.AddWithValue("@qty", row.QtyReceived);
                ins.Parameters.AddWithValue("@dt",  row.ReceiptDate);
                ins.Parameters.AddWithValue("@out", (object)row.OutstandingQty ?? DBNull.Value);
                ins.ExecuteNonQuery();
                count++;
            }

            // ── FIX 2: auto-update PurchaseOrder.PurchaseStatus ──────
            var purchaseIds = rows.Select(r => r.PurchaseID).Distinct();
            foreach (var pid in purchaseIds)
            {
                var checkCmd = new MySqlCommand(@"
                    SELECT
                        COALESCE(SUM(pol.OrderQty), 0)    AS TotalOrdered,
                        COALESCE(SUM(r.QtyReceived), 0)   AS TotalReceived
                    FROM  PurchaseOrderLine pol
                    LEFT JOIN Receipt r ON r.POLineID = pol.POLineID
                    WHERE pol.PurchaseID = @pid", conn, tx);
                checkCmd.Parameters.AddWithValue("@pid", pid);

                using var crd = checkCmd.ExecuteReader();
                if (crd.Read())
                {
                    int totalOrdered  = crd.GetInt32("TotalOrdered");
                    int totalReceived = crd.GetInt32("TotalReceived");
                    crd.Close();

                    string newStatus = (totalOrdered > 0 && totalReceived >= totalOrdered)
                        ? "Completed"
                        : "Partially Received";

                    var updCmd = new MySqlCommand(@"
                        UPDATE PurchaseOrder
                        SET    PurchaseStatus = @s
                        WHERE  PurchaseID     = @pid
                          AND  PurchaseStatus NOT IN ('Cancelled')",
                        conn, tx);
                    updCmd.Parameters.AddWithValue("@s",   newStatus);
                    updCmd.Parameters.AddWithValue("@pid", pid);
                    updCmd.ExecuteNonQuery();
                }
            }

            tx.Commit();
            return count;
        }
    }
}
