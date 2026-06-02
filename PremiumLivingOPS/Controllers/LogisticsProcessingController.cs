using MySql.Data.MySqlClient;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Controllers
{
    public class LogisticsProcessingController
    {
        // ── Connection helper ──────────────────────────────────────────
        private MySqlConnection OpenConnection()
        {
            var conn = new MySqlConnection(DbConfig.ConnectionString);
            conn.Open();
            return conn;
        }

        // ── GetViewShipmentVM ──────────────────────────────────────────
        public ViewShipmentVM GetViewShipmentVM(
            string statusFilter = null,
            string keyword      = null,
            DateTime? dateFrom  = null)
        {
            var session = SessionManager.Current;
            return new ViewShipmentVM
            {
                UserBar = new LogisticsUserBarVM
                {
                    DisplayName = session?.StaffName ?? "User",
                    Department  = session?.Department ?? ""
                },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(session),
                Shipments    = GetShipments(statusFilter, keyword, dateFrom)
            };
        }

        // ── GetShipmentDetail ──────────────────────────────────────────
        public ShipmentDetailVM GetShipmentDetail(string shipmentId)
        {
            ShipmentEntity    shipment = null;
            var lines = new List<ShipmentLineEntity>();
            DeliveryNoteEntity note   = null;

            using (var conn = OpenConnection())
            {
                const string sqlS = @"
                    SELECT s.ShipmentID, s.OrderID, s.TrackingNumber, s.ShipDate,
                           s.DeliveryMethod, s.ShipmentStatus, s.ShipmentType, s.TotalAmount,
                           c.CustomerName, o.ShippingAddress, o.DeliveryDate
                    FROM Shipment s
                    JOIN `Order` o  ON s.OrderID = o.OrderID
                    JOIN Customer c ON o.CustomerID = c.CustomerID
                    WHERE s.ShipmentID = @id";
                using (var cmd = new MySqlCommand(sqlS, conn))
                {
                    cmd.Parameters.AddWithValue("@id", shipmentId);
                    using (var r = cmd.ExecuteReader())
                        if (r.Read()) shipment = ReadShipment(r);
                }

                const string sqlL = @"
                    SELECT sl.ShipmentLineID, sl.ShipmentID, sl.OrderID,
                           sl.ItemID, i.ItemName, sl.QtyShipped, sl.QtyOutstanding
                    FROM ShipmentLine sl
                    JOIN Item i ON sl.ItemID = i.ItemID
                    WHERE sl.ShipmentID = @id";
                using (var cmd = new MySqlCommand(sqlL, conn))
                {
                    cmd.Parameters.AddWithValue("@id", shipmentId);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) lines.Add(ReadShipmentLine(r));
                }

                const string sqlD = @"
                    SELECT dn.DeliveryID, dn.ShipmentID, dn.DeliveryDate,
                           dn.Outstanding_qty, dn.ShippingAddress, dn.ShipToName
                    FROM DeliveryNote dn
                    WHERE dn.ShipmentID = @id
                    LIMIT 1";
                using (var cmd = new MySqlCommand(sqlD, conn))
                {
                    cmd.Parameters.AddWithValue("@id", shipmentId);
                    using (var r = cmd.ExecuteReader())
                        if (r.Read()) note = ReadDeliveryNote(r);
                }
            }

            return new ShipmentDetailVM { Shipment = shipment, Lines = lines, DeliveryNote = note };
        }

        // ── GetHandlingGoodsReceivedVM ──────────────────────────────────
        public HandlingGoodsReceivedVM GetHandlingGoodsReceivedVM(
            string statusFilter = null,
            string keyword      = null,
            DateTime? dateFrom  = null)
        {
            var session = SessionManager.Current;
            return new HandlingGoodsReceivedVM
            {
                UserBar = new LogisticsUserBarVM
                {
                    DisplayName = session?.StaffName ?? "User",
                    Department  = session?.Department ?? ""
                },
                AllowedMenus   = NavAccessPolicy.GetAllowedMenus(session),
                Receipts       = GetReceipts(statusFilter, keyword, dateFrom),
                PurchaseOrders = GetPurchaseOrders()
            };
        }

        // ── Private: GetShipments ────────────────────────────────────────
        private List<ShipmentEntity> GetShipments(
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
                    JOIN `Order` o  ON s.OrderID = o.OrderID
                    JOIN Customer c ON o.CustomerID = c.CustomerID
                    WHERE 1=1";

                if (!string.IsNullOrEmpty(statusFilter)) sql += " AND s.ShipmentStatus = @status";
                if (!string.IsNullOrEmpty(keyword))      sql += " AND (s.ShipmentID LIKE @kw OR s.OrderID LIKE @kw OR c.CustomerName LIKE @kw)";
                if (dateFrom.HasValue)                   sql += " AND s.ShipDate >= @dateFrom";
                sql += " ORDER BY s.ShipDate DESC";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(statusFilter)) cmd.Parameters.AddWithValue("@status",   statusFilter);
                    if (!string.IsNullOrEmpty(keyword))      cmd.Parameters.AddWithValue("@kw",        $"%{keyword}%");
                    if (dateFrom.HasValue)                   cmd.Parameters.AddWithValue("@dateFrom",  dateFrom.Value);

                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add(ReadShipment(r));
                }
            }
            return list;
        }

        // ── Private: GetReceipts ─────────────────────────────────────────
        private List<GoodsReceivedEntity> GetReceipts(
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
                    JOIN PurchaseOrderLine pol ON r.POLineID   = pol.POLineID
                    JOIN PurchaseOrder po      ON r.PurchaseID = po.PurchaseID
                    JOIN Supplier sup          ON po.SupplierID = sup.SupplierID
                    JOIN Item i                ON pol.RawMaterialItemID = i.ItemID
                    JOIN Warehouse w           ON pol.WarehouseID = w.WarehouseID
                    WHERE 1=1";

                if (!string.IsNullOrEmpty(statusFilter)) sql += " AND po.PurchaseStatus = @status";
                if (!string.IsNullOrEmpty(keyword))      sql += " AND (r.ReceiptID LIKE @kw OR r.PurchaseID LIKE @kw OR sup.SupplierName LIKE @kw)";
                if (dateFrom.HasValue)                   sql += " AND r.ReceiptDate >= @dateFrom";
                sql += " ORDER BY r.ReceiptDate DESC";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(statusFilter)) cmd.Parameters.AddWithValue("@status",  statusFilter);
                    if (!string.IsNullOrEmpty(keyword))      cmd.Parameters.AddWithValue("@kw",       $"%{keyword}%");
                    if (dateFrom.HasValue)                   cmd.Parameters.AddWithValue("@dateFrom", dateFrom.Value);

                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add(ReadReceipt(r));
                }
            }
            return list;
        }

        // ── Private: GetPurchaseOrders ───────────────────────────────────
        private List<PurchaseOrderEntity> GetPurchaseOrders()
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

        // ── Private readers ─────────────────────────────────────────────
        private static ShipmentEntity ReadShipment(MySqlDataReader r) => new ShipmentEntity
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
            DeliveryDate    = r["DeliveryDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["DeliveryDate"])
        };

        private static ShipmentLineEntity ReadShipmentLine(MySqlDataReader r) => new ShipmentLineEntity
        {
            ShipmentLineID = r["ShipmentLineID"].ToString(),
            ShipmentID     = r["ShipmentID"].ToString(),
            OrderID        = r["OrderID"].ToString(),
            ItemID         = r["ItemID"].ToString(),
            ItemName       = r["ItemName"].ToString(),
            QtyShipped     = Convert.ToInt32(r["QtyShipped"]),
            QtyOutstanding = r["QtyOutstanding"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["QtyOutstanding"])
        };

        private static DeliveryNoteEntity ReadDeliveryNote(MySqlDataReader r) => new DeliveryNoteEntity
        {
            DeliveryID      = r["DeliveryID"].ToString(),
            ShipmentID      = r["ShipmentID"].ToString(),
            DeliveryDate    = Convert.ToDateTime(r["DeliveryDate"]),
            OutstandingQty  = r["Outstanding_qty"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["Outstanding_qty"]),
            ShippingAddress = r["ShippingAddress"].ToString(),
            ShipToName      = r["ShipToName"].ToString()
        };

        private static GoodsReceivedEntity ReadReceipt(MySqlDataReader r) => new GoodsReceivedEntity
        {
            ReceiptID         = r["ReceiptID"].ToString(),
            PurchaseID        = r["PurchaseID"].ToString(),
            POLineID          = r["POLineID"].ToString(),
            QtyReceived       = Convert.ToInt32(r["QtyReceived"]),
            ReceiptDate       = Convert.ToDateTime(r["ReceiptDate"]),
            OutstandingQty    = r["Outstanding_QTY"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["Outstanding_QTY"]),
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
