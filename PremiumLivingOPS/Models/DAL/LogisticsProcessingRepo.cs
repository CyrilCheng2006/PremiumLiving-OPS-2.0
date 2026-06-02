using MySql.Data.MySqlClient;
using PremiumLivingOPS.Models.Entities;
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
    }
}
