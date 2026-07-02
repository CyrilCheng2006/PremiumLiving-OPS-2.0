using MySql.Data.MySqlClient;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.DAL
{
    /// <summary>
    /// DAL for Raw Material → Procurement module.
    ///
    /// Actual DB schema (do NOT add columns that are not here):
    ///   PurchaseOrder    : PurchaseID, RequestID, SupplierID, POTotalAmount, OrderDate, PurchaseStatus
    ///   PurchaseOrderLine: POLineID, RawMaterialItemID, PurchaseID, WarehouseID, OrderQty, UnitPrice
    ///   MaterialRequest  : RequestID, UrgencyLevel, TriggerType, RawMaterialItemID, WarehouseItemID, RequestedQty, ...
    ///
    /// MRQ linkage chain: PurchaseOrderLine.PurchaseID → PurchaseOrder.RequestID → MaterialRequest
    ///
    /// PurchaseID format : PO-YYYYMMDD-NNNN        length = 16  (e.g. PO-20260702-0001)
    /// POLineID   format : PO-YYYYMMDD-NNNN-NN     length = 19  (e.g. PO-20260702-0001-01)
    /// </summary>
    public class ProcurementRepo
    {
        // ══ SEARCH ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns one <see cref="ProcurementOrderGroup"/> per PurchaseOrder header.
        /// UrgencyLevel is resolved via PurchaseOrder.RequestID → MaterialRequest.
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
                          po.PurchaseID,
                          po.SupplierID,
                          s.SupplierName,
                          po.OrderDate,
                          po.PurchaseStatus,
                          po.POTotalAmount                   AS TotalAmount,
                          COALESCE(lc.LineCount, 0)         AS ItemCount,
                          COALESCE(mr.UrgencyLevel, '')     AS UrgencyLevel
                      FROM  PurchaseOrder po
                      JOIN  Supplier         s  ON po.SupplierID = s.SupplierID
                      LEFT JOIN MaterialRequest mr ON po.RequestID = mr.RequestID
                      LEFT JOIN (
                          SELECT PurchaseID, COUNT(*) AS LineCount
                          FROM   PurchaseOrderLine
                          GROUP  BY PurchaseID
                      ) lc ON lc.PurchaseID = po.PurchaseID
                      WHERE 1=1";

                if (!string.IsNullOrEmpty(keyword))
                    sql += " AND (po.PurchaseID LIKE @kw OR s.SupplierName LIKE @kw)";
                if (!string.IsNullOrEmpty(status) && status != "All")
                    sql += " AND po.PurchaseStatus = @status";
                if (dateFrom.HasValue)
                    sql += " AND po.OrderDate >= @dateFrom";
                if (dateTo.HasValue)
                    sql += " AND po.OrderDate <= @dateTo";

                sql += " ORDER BY po.OrderDate DESC, po.PurchaseID DESC";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(keyword))
                        cmd.Parameters.AddWithValue("@kw", "%" + keyword + "%");
                    if (!string.IsNullOrEmpty(status) && status != "All")
                        cmd.Parameters.AddWithValue("@status", status);
                    if (dateFrom.HasValue)
                        cmd.Parameters.AddWithValue("@dateFrom", dateFrom.Value.ToString("yyyy-MM-dd"));
                    if (dateTo.HasValue)
                        cmd.Parameters.AddWithValue("@dateTo", dateTo.Value.ToString("yyyy-MM-dd"));

                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new ProcurementOrderGroup
                            {
                                PurchaseID     = r["PurchaseID"].ToString(),
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

        // ══ DETAIL ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns the PurchaseOrder header for a given PurchaseID.
        /// UrgencyLevel / TriggerType resolved via PurchaseOrder.RequestID → MaterialRequest.
        /// </summary>
        public ProcurementOrderEntity GetPurchaseOrderById(string purchaseId)
        {
            if (string.IsNullOrWhiteSpace(purchaseId)) return null;
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT po.PurchaseID,
                             po.RequestID,
                             po.SupplierID,
                             COALESCE(s.SupplierName, po.SupplierID)  AS SupplierName,
                             po.POTotalAmount,
                             po.OrderDate,
                             po.PurchaseStatus,
                             COALESCE(mr.UrgencyLevel, '')            AS UrgencyLevel,
                             COALESCE(mr.TriggerType,  '')            AS TriggerType,
                             '' AS RawMaterialItemID,
                             '' AS RawMaterialName,
                             0  AS RequestedQty
                      FROM   PurchaseOrder po
                      LEFT JOIN Supplier        s  ON po.SupplierID = s.SupplierID
                      LEFT JOIN MaterialRequest mr ON po.RequestID  = mr.RequestID
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

        /// <summary>
        /// Legacy fallback: when no exact header row exists for <paramref name="headerKey"/>
        /// (PO-YYYYMMDD-NNNN), find the first PurchaseOrder whose PurchaseID starts with
        /// that key and return it as a synthetic header.
        /// </summary>
        public ProcurementOrderEntity GetPurchaseOrderByPrefix(string headerKey)
        {
            if (string.IsNullOrWhiteSpace(headerKey)) return null;
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT po.PurchaseID,
                             po.RequestID,
                             po.SupplierID,
                             COALESCE(s.SupplierName, po.SupplierID)  AS SupplierName,
                             po.POTotalAmount,
                             po.OrderDate,
                             po.PurchaseStatus,
                             COALESCE(mr.UrgencyLevel, '')            AS UrgencyLevel,
                             COALESCE(mr.TriggerType,  '')            AS TriggerType,
                             '' AS RawMaterialItemID,
                             '' AS RawMaterialName,
                             0  AS RequestedQty
                      FROM   PurchaseOrder po
                      LEFT JOIN Supplier        s  ON po.SupplierID = s.SupplierID
                      LEFT JOIN MaterialRequest mr ON po.RequestID  = mr.RequestID
                      WHERE  po.PurchaseID LIKE @prefix
                      ORDER  BY po.PurchaseID
                      LIMIT  1";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@prefix", headerKey + "%");
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read()) return null;
                        var entity = MapProcurementOrder(r);
                        entity.PurchaseID = headerKey;
                        return entity;
                    }
                }
            }
        }

        public List<PurchaseOrderLineEntity> GetLinesByPurchaseId(string purchaseId)
        {
            var list = new List<PurchaseOrderLineEntity>();
            if (string.IsNullOrWhiteSpace(purchaseId)) return list;
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT pol.POLineID,
                             pol.PurchaseID,
                             pol.RawMaterialItemID,
                             COALESCE(i.ItemName, pol.RawMaterialItemID)    AS MaterialName,
                             COALESCE(rm.MaterialType, '')                  AS MaterialType,
                             pol.WarehouseID,
                             COALESCE(w.WarehouseLocation, pol.WarehouseID) AS WarehouseLocation,
                             pol.OrderQty,
                             pol.UnitPrice
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

        public List<PurchaseOrderLineEntity> GetLinesByPurchaseIdPrefix(string headerKey)
        {
            var list = new List<PurchaseOrderLineEntity>();
            if (string.IsNullOrWhiteSpace(headerKey)) return list;
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT pol.POLineID,
                             pol.PurchaseID,
                             pol.RawMaterialItemID,
                             COALESCE(i.ItemName, pol.RawMaterialItemID)    AS MaterialName,
                             COALESCE(rm.MaterialType, '')                  AS MaterialType,
                             pol.WarehouseID,
                             COALESCE(w.WarehouseLocation, pol.WarehouseID) AS WarehouseLocation,
                             pol.OrderQty,
                             pol.UnitPrice
                      FROM   PurchaseOrderLine pol
                      LEFT JOIN RawMaterial rm ON pol.RawMaterialItemID = rm.ItemID
                      LEFT JOIN Item        i  ON rm.ItemID             = i.ItemID
                      LEFT JOIN Warehouse   w  ON pol.WarehouseID       = w.WarehouseID
                      WHERE  pol.PurchaseID LIKE @prefix
                      ORDER  BY pol.POLineID";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@prefix", headerKey + "%");
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add(MapPOLine(r));
                }
            }
            return list;
        }

        // ══ CREATE ─ BATCH PREFIX LOOKUPS ═════════════════════════════════════════════════════════════

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
                      WHERE mr.RequestID REGEXP '-[0-9]{2}$'
                        AND LEFT(mr.RequestID, LENGTH(mr.RequestID) - 3) NOT IN (
                                SELECT LEFT(po.RequestID, LENGTH(po.RequestID) - 3)
                                FROM   PurchaseOrder po
                                WHERE  po.RequestID IS NOT NULL
                                  AND  po.RequestID REGEXP '-[0-9]{2}$'
                            )
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
                        AND  mr.RequestID NOT IN (
                                 SELECT po.RequestID
                                 FROM   PurchaseOrder po
                                 WHERE  po.RequestID IS NOT NULL
                             )
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

        // ══ CREATE ─ WRITE ═════════════════════════════════════════════════════════════════════════════════════
        //
        // Actual PurchaseOrder columns    : PurchaseID, RequestID, SupplierID, POTotalAmount, OrderDate, PurchaseStatus
        // Actual PurchaseOrderLine columns: POLineID, RawMaterialItemID, PurchaseID, WarehouseID, OrderQty, UnitPrice

        /// <summary>
        /// Creates one PurchaseOrder header + one PurchaseOrderLine per entry in
        /// <paramref name="lines"/>.
        /// POLineID format: {purchaseId}-{seq:D2}  e.g. PO-20260702-0001-01
        /// </summary>
        public void CreatePurchaseOrderBatch(
            string purchaseId,
            string supplierId,
            double poTotalAmount,
            DateTime orderDate,
            string purchaseStatus,
            string urgencyLevel,
            string triggerType,
            List<MaterialRequestLineItem> lines,
            string staffId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var trx = conn.BeginTransaction())
                {
                    try
                    {
                        string firstRequestId = lines.Count > 0 ? lines[0].RequestID : null;

                        const string insertPO =
                            @"INSERT INTO PurchaseOrder
                                (PurchaseID, RequestID, SupplierID,
                                 POTotalAmount, OrderDate, PurchaseStatus)
                              VALUES
                                (@purchaseId, @requestId, @supplierId,
                                 @amount, @date, @status)";
                        using (var cmd = new MySqlCommand(insertPO, conn, trx))
                        {
                            cmd.Parameters.AddWithValue("@purchaseId", purchaseId);
                            cmd.Parameters.AddWithValue("@requestId",  firstRequestId ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@supplierId", supplierId);
                            cmd.Parameters.AddWithValue("@amount",     poTotalAmount);
                            cmd.Parameters.AddWithValue("@date",       orderDate.ToString("yyyy-MM-dd"));
                            cmd.Parameters.AddWithValue("@status",     purchaseStatus);
                            cmd.ExecuteNonQuery();
                        }

                        const string insertLine =
                            @"INSERT INTO PurchaseOrderLine
                                (POLineID, PurchaseID, RawMaterialItemID, WarehouseID, OrderQty, UnitPrice)
                              VALUES
                                (@lineId, @purchaseId, @matId, @whId, @qty, @price)";

                        for (int seq = 0; seq < lines.Count; seq++)
                        {
                            var    ln     = lines[seq];
                            string lineId = $"{purchaseId}-{(seq + 1):D2}";

                            using (var cmd = new MySqlCommand(insertLine, conn, trx))
                            {
                                cmd.Parameters.AddWithValue("@lineId",     lineId);
                                cmd.Parameters.AddWithValue("@purchaseId", purchaseId);
                                cmd.Parameters.AddWithValue("@matId",      ln.RawMaterialItemID);
                                cmd.Parameters.AddWithValue("@whId",       ln.WarehouseID);
                                cmd.Parameters.AddWithValue("@qty",        ln.OrderQty);
                                cmd.Parameters.AddWithValue("@price",      ln.UnitPrice);
                                cmd.ExecuteNonQuery();
                            }
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

        public void CreatePurchaseOrder(
            string purchaseId, string requestId, string supplierId,
            double poTotalAmount, DateTime orderDate, string purchaseStatus,
            string rawMaterialItemId, string warehouseId,
            int orderQty, double unitPrice, string staffId)
        {
            var singleLine = new List<MaterialRequestLineItem>
            {
                new MaterialRequestLineItem
                {
                    RequestID         = requestId,
                    RawMaterialItemID = rawMaterialItemId,
                    WarehouseID       = warehouseId,
                    OrderQty          = orderQty,
                    UnitPrice         = unitPrice
                }
            };
            CreatePurchaseOrderBatch(
                purchaseId, supplierId, poTotalAmount,
                orderDate, purchaseStatus, null, null,
                singleLine, staffId);
        }

        // ══ ID GENERATOR ════════════════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Generates the next available PurchaseID in PO-YYYYMMDD-NNNN format (16 chars).
        ///
        /// PO-YYYYMMDD-NNNN breakdown (1-based MySQL positions):
        ///   P O -  Y  Y  Y  Y  M  M  D  D  -  N  N  N  N
        ///   1 2 3  4  5  6  7  8  9 10 11 12 13 14 15 16
        ///
        /// SUBSTRING(PurchaseID, 13, 4) extracts chars 13-16 → the NNNN sequence.
        /// LENGTH = 16 ensures only proper header IDs are considered
        /// (POLineIDs are 19 chars: PO-YYYYMMDD-NNNN-NN).
        /// </summary>
        public string GenerateNextPurchaseId()
        {
            string prefix = $"PO-{DateTime.Today:yyyyMMdd}-";
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT COALESCE(MAX(CAST(SUBSTRING(PurchaseID, 13, 4) AS UNSIGNED)), 0) + 1
                      FROM   PurchaseOrder
                      WHERE  PurchaseID LIKE @prefix
                        AND  LENGTH(PurchaseID) = 16";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@prefix", prefix + "%");
                    var next = Convert.ToInt32(cmd.ExecuteScalar());
                    return $"{prefix}{next:D4}";
                }
            }
        }

        // ══ MAPPERS ════════════════════════════════════════════════════════════════════════════════════════════

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
                RequestID         = "",
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
