using MySql.Data.MySqlClient;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.DAL
{
    /// <summary>
    /// DAL for Raw Material → Procurement module.
    /// </summary>
    public class ProcurementRepo
    {
        // ══ SEARCH — GROUPED ════════════════════════════════════════════════

        /// <summary>
        /// Returns one row per base PO-ID (LEFT(PurchaseID, 17) = "PO-YYYYMMDD-NNNN"),
        /// aggregating status, total amount, and item count across all -NN sub-orders.
        /// </summary>
        public List<ProcurementOrderGroup> SearchGroupedPurchaseOrders(
            string keyword = null, string status = null,
            DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            var list = new List<ProcurementOrderGroup>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var sql =
                    @"SELECT
                          LEFT(po.PurchaseID, 17)                       AS BasePurchaseID,
                          po.SupplierID,
                          s.SupplierName,
                          MIN(po.OrderDate)                             AS OrderDate,
                          CASE WHEN COUNT(DISTINCT po.PurchaseStatus) > 1
                               THEN 'Mixed'
                               ELSE MAX(po.PurchaseStatus) END          AS PurchaseStatus,
                          SUM(po.POTotalAmount)                         AS TotalAmount,
                          COUNT(po.PurchaseID)                          AS ItemCount,
                          MAX(mr.UrgencyLevel)                          AS UrgencyLevel
                      FROM  PurchaseOrder po
                      JOIN  Supplier       s  ON po.SupplierID = s.SupplierID
                      JOIN  MaterialRequest mr ON po.RequestID = mr.RequestID
                      WHERE 1=1";

                if (!string.IsNullOrEmpty(keyword))
                    sql += " AND (po.PurchaseID LIKE @kw OR s.SupplierName LIKE @kw)";
                if (!string.IsNullOrEmpty(status) && status != "All")
                    sql += " AND po.PurchaseStatus = @status";
                if (dateFrom.HasValue)
                    sql += " AND po.OrderDate >= @dateFrom";
                if (dateTo.HasValue)
                    sql += " AND po.OrderDate <= @dateTo";

                sql += " GROUP BY LEFT(po.PurchaseID, 17), po.SupplierID, s.SupplierName";
                sql += " ORDER BY OrderDate DESC, BasePurchaseID DESC";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(keyword))                    cmd.Parameters.AddWithValue("@kw",       "%" + keyword + "%");
                    if (!string.IsNullOrEmpty(status) && status != "All") cmd.Parameters.AddWithValue("@status",   status);
                    if (dateFrom.HasValue)                                 cmd.Parameters.AddWithValue("@dateFrom", dateFrom.Value.ToString("yyyy-MM-dd"));
                    if (dateTo.HasValue)                                   cmd.Parameters.AddWithValue("@dateTo",   dateTo.Value.ToString("yyyy-MM-dd"));

                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new ProcurementOrderGroup
                            {
                                BasePurchaseID = r["BasePurchaseID"].ToString(),
                                SupplierID     = r["SupplierID"].ToString(),
                                SupplierName   = r["SupplierName"].ToString(),
                                OrderDate      = Convert.ToDateTime(r["OrderDate"]),
                                PurchaseStatus = r["PurchaseStatus"].ToString(),
                                TotalAmount    = Convert.ToDouble(r["TotalAmount"]),
                                ItemCount      = Convert.ToInt32(r["ItemCount"]),
                                UrgencyLevel   = r["UrgencyLevel"].ToString()
                            });
                }
            }
            return list;
        }

        /// <summary>
        /// Returns all -NN PurchaseOrder rows for a given base ID.
        /// Uses LEFT JOIN for MR/RM/Item so missing FK data never produces an empty result.
        /// </summary>
        public List<ProcurementOrderEntity> GetPurchaseOrdersByBaseId(string basePurchaseId)
        {
            var list = new List<ProcurementOrderEntity>();
            if (string.IsNullOrWhiteSpace(basePurchaseId)) return list;
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT po.PurchaseID,
                             po.RequestID,
                             po.SupplierID,
                             COALESCE(s.SupplierName, po.SupplierID)   AS SupplierName,
                             po.POTotalAmount,
                             po.OrderDate,
                             po.PurchaseStatus,
                             COALESCE(mr.RawMaterialItemID, '')        AS RawMaterialItemID,
                             COALESCE(i.ItemName, '')                  AS RawMaterialName,
                             COALESCE(mr.RequestedQty, 0)              AS RequestedQty,
                             COALESCE(mr.UrgencyLevel, '')             AS UrgencyLevel,
                             COALESCE(mr.TriggerType, '')              AS TriggerType
                      FROM   PurchaseOrder   po
                      LEFT JOIN Supplier       s  ON po.SupplierID          = s.SupplierID
                      LEFT JOIN MaterialRequest mr ON po.RequestID           = mr.RequestID
                      LEFT JOIN RawMaterial    rm  ON mr.RawMaterialItemID   = rm.ItemID
                      LEFT JOIN Item           i   ON rm.ItemID              = i.ItemID
                      WHERE  po.PurchaseID LIKE @prefix
                      ORDER  BY po.PurchaseID";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@prefix", basePurchaseId + "-%");
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add(MapProcurementOrder(r));
                }
            }
            return list;
        }

        /// <summary>
        /// Returns all PurchaseOrderLine rows for every -NN sub-order of a base ID.
        /// Uses LEFT JOIN defensively so missing RM/Item/Warehouse rows are not excluded.
        /// </summary>
        public List<PurchaseOrderLineEntity> GetAllLinesByBaseId(string basePurchaseId)
        {
            var list = new List<PurchaseOrderLineEntity>();
            if (string.IsNullOrWhiteSpace(basePurchaseId)) return list;
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT pol.POLineID,
                             pol.PurchaseID,
                             pol.RawMaterialItemID,
                             COALESCE(i.ItemName, pol.RawMaterialItemID)      AS MaterialName,
                             COALESCE(rm.MaterialType, '')                    AS MaterialType,
                             pol.WarehouseID,
                             COALESCE(w.WarehouseLocation, pol.WarehouseID)   AS WarehouseLocation,
                             pol.OrderQty,
                             pol.UnitPrice
                      FROM   PurchaseOrderLine pol
                      LEFT JOIN RawMaterial rm ON pol.RawMaterialItemID = rm.ItemID
                      LEFT JOIN Item        i  ON rm.ItemID             = i.ItemID
                      LEFT JOIN Warehouse   w  ON pol.WarehouseID       = w.WarehouseID
                      WHERE  pol.PurchaseID LIKE @prefix
                      ORDER  BY pol.PurchaseID, pol.POLineID";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@prefix", basePurchaseId + "-%");
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add(MapPOLine(r));
                }
            }
            return list;
        }

        // ══ Legacy single-row getter ═════════════════════════════════════════
        public ProcurementOrderEntity GetPurchaseOrderById(string purchaseId)
        {
            if (string.IsNullOrWhiteSpace(purchaseId)) return null;
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT po.PurchaseID, po.RequestID, po.SupplierID,
                             COALESCE(s.SupplierName, po.SupplierID)   AS SupplierName,
                             po.POTotalAmount, po.OrderDate, po.PurchaseStatus,
                             COALESCE(mr.RawMaterialItemID, '')        AS RawMaterialItemID,
                             COALESCE(i.ItemName, '')                  AS RawMaterialName,
                             COALESCE(mr.RequestedQty, 0)             AS RequestedQty,
                             COALESCE(mr.UrgencyLevel, '')            AS UrgencyLevel,
                             COALESCE(mr.TriggerType, '')             AS TriggerType
                      FROM   PurchaseOrder   po
                      LEFT JOIN Supplier       s  ON po.SupplierID        = s.SupplierID
                      LEFT JOIN MaterialRequest mr ON po.RequestID         = mr.RequestID
                      LEFT JOIN RawMaterial    rm  ON mr.RawMaterialItemID = rm.ItemID
                      LEFT JOIN Item           i   ON rm.ItemID            = i.ItemID
                      WHERE  po.PurchaseID = @id";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", purchaseId);
                    using (var r = cmd.ExecuteReader())
                        if (r.Read()) return MapProcurementOrder(r);
                }
            }
            return null;
        }

        public List<PurchaseOrderLineEntity> GetLinesByPurchaseId(string purchaseId)
        {
            var list = new List<PurchaseOrderLineEntity>();
            if (string.IsNullOrWhiteSpace(purchaseId)) return list;
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT pol.POLineID, pol.PurchaseID,
                             pol.RawMaterialItemID,
                             COALESCE(i.ItemName, pol.RawMaterialItemID)     AS MaterialName,
                             COALESCE(rm.MaterialType, '')                   AS MaterialType,
                             pol.WarehouseID,
                             COALESCE(w.WarehouseLocation, pol.WarehouseID)  AS WarehouseLocation,
                             pol.OrderQty, pol.UnitPrice
                      FROM   PurchaseOrderLine pol
                      LEFT JOIN RawMaterial rm ON pol.RawMaterialItemID = rm.ItemID
                      LEFT JOIN Item        i  ON rm.ItemID             = i.ItemID
                      LEFT JOIN Warehouse   w  ON pol.WarehouseID       = w.WarehouseID
                      WHERE  pol.PurchaseID = @id
                      ORDER  BY pol.POLineID";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", purchaseId);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add(MapPOLine(r));
                }
            }
            return list;
        }

        // ══ CREATE — BATCH PREFIX LOOKUPS ════════════════════════════════════

        public List<MaterialRequestBatchLookup> GetUnlinkedBatchPrefixes()
        {
            var list = new List<MaterialRequestBatchLookup>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT
                          LEFT(mr.RequestID, LENGTH(mr.RequestID) - 3)  AS BatchPrefix,
                          mr.UrgencyLevel,
                          mr.TriggerType,
                          COUNT(*)                                       AS LineCount
                      FROM  MaterialRequest mr
                      WHERE mr.RequestID NOT IN (
                                SELECT po.RequestID FROM PurchaseOrder po
                            )
                        AND mr.RequestID REGEXP '-[0-9]{2}$'
                      GROUP BY BatchPrefix, mr.UrgencyLevel, mr.TriggerType
                      ORDER BY BatchPrefix";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new MaterialRequestBatchLookup
                        {
                            BatchPrefix  = r["BatchPrefix"].ToString(),
                            UrgencyLevel = r["UrgencyLevel"].ToString(),
                            TriggerType  = r["TriggerType"].ToString(),
                            LineCount    = Convert.ToInt32(r["LineCount"])
                        });
            }
            return list;
        }

        public List<MaterialRequestLineItem> GetLineItemsByBatchPrefix(string batchPrefix)
        {
            var list = new List<MaterialRequestLineItem>();
            if (string.IsNullOrWhiteSpace(batchPrefix)) return list;
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT mr.RequestID,
                             mr.RawMaterialItemID,
                             i.ItemName   AS MaterialName,
                             rm.MaterialType,
                             mr.WarehouseItemID,
                             wi.WarehouseID,
                             w.WarehouseLocation,
                             mr.RequestedQty
                      FROM   MaterialRequest mr
                      JOIN   RawMaterial    rm ON mr.RawMaterialItemID = rm.ItemID
                      JOIN   Item           i  ON rm.ItemID            = i.ItemID
                      JOIN   WarehouseItem  wi ON mr.WarehouseItemID   = wi.WarehouseItemID
                      JOIN   Warehouse      w  ON wi.WarehouseID       = w.WarehouseID
                      WHERE  mr.RequestID LIKE @prefix
                        AND  mr.RequestID NOT IN (SELECT po.RequestID FROM PurchaseOrder po)
                      ORDER  BY mr.RequestID";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@prefix", batchPrefix + "-%");
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                        {
                            int qty = Convert.ToInt32(r["RequestedQty"]);
                            list.Add(new MaterialRequestLineItem
                            {
                                RequestID         = r["RequestID"].ToString(),
                                RawMaterialItemID = r["RawMaterialItemID"].ToString(),
                                MaterialName      = r["MaterialName"].ToString(),
                                MaterialType      = r["MaterialType"].ToString(),
                                WarehouseItemID   = r["WarehouseItemID"].ToString(),
                                WarehouseID       = r["WarehouseID"].ToString(),
                                WarehouseDisplay  = $"{r["WarehouseID"]}  —  {r["WarehouseLocation"]}",
                                RequestedQty      = qty,
                                OrderQty          = qty,
                                UnitPrice         = 0
                            });
                        }
                }
            }
            return list;
        }

        public List<SupplierLookup> GetAllSuppliers()
        {
            var list = new List<SupplierLookup>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT SupplierID, SupplierName, PhoneNumber, SupplierAddress
                      FROM   Supplier ORDER BY SupplierName";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new SupplierLookup
                        {
                            SupplierID      = r["SupplierID"].ToString(),
                            SupplierName    = r["SupplierName"].ToString(),
                            PhoneNumber     = r["PhoneNumber"].ToString(),
                            SupplierAddress = r["SupplierAddress"].ToString()
                        });
            }
            return list;
        }

        // ══ CREATE — WRITE ════════════════════════════════════════════════

        public void CreatePurchaseOrder(
            string purchaseId, string requestId, string supplierId,
            double poTotalAmount, DateTime orderDate, string purchaseStatus,
            string rawMaterialItemId, string warehouseId,
            int orderQty, double unitPrice, string staffId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var trx = conn.BeginTransaction())
                {
                    try
                    {
                        string poLineId = GenerateNextPoLineId(conn, trx);

                        const string insertPO =
                            @"INSERT INTO PurchaseOrder
                                (PurchaseID, RequestID, SupplierID, POTotalAmount, OrderDate, PurchaseStatus)
                              VALUES
                                (@purchaseId, @requestId, @supplierId, @amount, @date, @status)";
                        using (var cmd = new MySqlCommand(insertPO, conn, trx))
                        {
                            cmd.Parameters.AddWithValue("@purchaseId", purchaseId);
                            cmd.Parameters.AddWithValue("@requestId",  requestId);
                            cmd.Parameters.AddWithValue("@supplierId", supplierId);
                            cmd.Parameters.AddWithValue("@amount",     poTotalAmount);
                            cmd.Parameters.AddWithValue("@date",       orderDate.ToString("yyyy-MM-dd"));
                            cmd.Parameters.AddWithValue("@status",     purchaseStatus);
                            cmd.ExecuteNonQuery();
                        }

                        const string insertLine =
                            @"INSERT INTO PurchaseOrderLine
                                (POLineID, RawMaterialItemID, PurchaseID, WarehouseID, OrderQty, UnitPrice)
                              VALUES
                                (@lineId, @matId, @purchaseId, @whId, @qty, @price)";
                        using (var cmd = new MySqlCommand(insertLine, conn, trx))
                        {
                            cmd.Parameters.AddWithValue("@lineId",     poLineId);
                            cmd.Parameters.AddWithValue("@matId",      rawMaterialItemId);
                            cmd.Parameters.AddWithValue("@purchaseId", purchaseId);
                            cmd.Parameters.AddWithValue("@whId",       warehouseId);
                            cmd.Parameters.AddWithValue("@qty",        orderQty);
                            cmd.Parameters.AddWithValue("@price",      unitPrice);
                            cmd.ExecuteNonQuery();
                        }

                        const string insertLog =
                            @"INSERT INTO Log (LogID, StaffID, LogType, TargetTable, NewValue)
                              VALUES (@logId, @staffId, 'Create', 'PurchaseOrder', @newVal)";
                        using (var cmd = new MySqlCommand(insertLog, conn, trx))
                        {
                            cmd.Parameters.AddWithValue("@logId",   Guid.NewGuid().ToString());
                            cmd.Parameters.AddWithValue("@staffId", staffId);
                            cmd.Parameters.AddWithValue("@newVal",  purchaseId);
                            cmd.ExecuteNonQuery();
                        }

                        trx.Commit();
                    }
                    catch { trx.Rollback(); throw; }
                }
            }
        }

        // ══ ID GENERATORS ═════════════════════════════════════════════════

        public string GenerateNextPurchaseId()
        {
            string prefix = $"PO-{DateTime.Today:yyyyMMdd}-";
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT COALESCE(MAX(CAST(SUBSTRING(PurchaseID, 14, 4) AS UNSIGNED)), 0) + 1
                      FROM   PurchaseOrder
                      WHERE  PurchaseID LIKE @prefix";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@prefix", prefix + "%");
                    var next = Convert.ToInt32(cmd.ExecuteScalar());
                    return $"{prefix}{next:D4}";
                }
            }
        }

        private string GenerateNextPoLineId(MySqlConnection conn, MySqlTransaction trx)
        {
            string prefix = $"POL-{DateTime.Today:yyyyMMdd}-";
            const string sql =
                @"SELECT COALESCE(MAX(CAST(SUBSTRING(POLineID, 15) AS UNSIGNED)), 0) + 1
                  FROM   PurchaseOrderLine
                  WHERE  POLineID LIKE @prefix";
            using (var cmd = new MySqlCommand(sql, conn, trx))
            {
                cmd.Parameters.AddWithValue("@prefix", prefix + "%");
                var next = Convert.ToInt32(cmd.ExecuteScalar());
                return $"{prefix}{next:D4}";
            }
        }

        // ══ MAPPERS ═══════════════════════════════════════════════════════

        private static ProcurementOrderEntity MapProcurementOrder(MySqlDataReader r)
            => new ProcurementOrderEntity
            {
                PurchaseID        = r["PurchaseID"].ToString(),
                RequestID         = r["RequestID"].ToString(),
                SupplierID        = r["SupplierID"].ToString(),
                SupplierName      = r["SupplierName"].ToString(),
                POTotalAmount     = Convert.ToDouble(r["POTotalAmount"]),
                OrderDate         = Convert.ToDateTime(r["OrderDate"]),
                PurchaseStatus    = r["PurchaseStatus"].ToString(),
                RawMaterialItemID = r["RawMaterialItemID"].ToString(),
                RawMaterialName   = r["RawMaterialName"].ToString(),
                RequestedQty      = Convert.ToInt32(r["RequestedQty"]),
                UrgencyLevel      = r["UrgencyLevel"].ToString(),
                TriggerType       = r["TriggerType"].ToString()
            };

        private static PurchaseOrderLineEntity MapPOLine(MySqlDataReader r)
            => new PurchaseOrderLineEntity
            {
                POLineID          = r["POLineID"].ToString(),
                PurchaseID        = r["PurchaseID"].ToString(),
                RawMaterialItemID = r["RawMaterialItemID"].ToString(),
                MaterialName      = r["MaterialName"].ToString(),
                MaterialType      = r["MaterialType"].ToString(),
                WarehouseID       = r["WarehouseID"].ToString(),
                WarehouseLocation = r["WarehouseLocation"].ToString(),
                OrderQty          = Convert.ToInt32(r["OrderQty"]),
                UnitPrice         = Convert.ToDouble(r["UnitPrice"])
            };
    }
}
