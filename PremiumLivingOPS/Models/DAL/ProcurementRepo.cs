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
    /// PurchaseID format : PO-YYYYMMDD-NNNN       (one header per batch)
    /// POLineID   format : PO-YYYYMMDD-NNNN-NN    (one per item line in that batch)
    /// </summary>
    public class ProcurementRepo
    {
        // ══ SEARCH ═══════════════════════════════════════════════════════════════

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

        // ══ DETAIL ─────────────────────────────────────────────────────────────────═

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
        /// that key (e.g. PO-20260702-0001-01) and return it as a synthetic header,
        /// replacing its PurchaseID with the clean header key.
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
                        // Override the stored -NN ID with the clean header key
                        entity.PurchaseID = headerKey;
                        return entity;
                    }
                }
            }
        }

        /// <summary>
        /// Returns all PurchaseOrderLine rows for a given PurchaseID, ordered by POLineID.
        /// Note: PurchaseOrderLine has NO RequestID column; the MRQ link lives on PurchaseOrder.
        /// </summary>
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

        /// <summary>
        /// Legacy fallback: fetches PurchaseOrderLine rows where PurchaseID starts with
        /// <paramref name="headerKey"/> (catches -01/-02/-03 stored as PurchaseID).
        /// Also synthesises a POLineID sequence if the stored POLineID itself is the
        /// same as the PurchaseID (edge case from very early data).
        /// </summary>
        public List<PurchaseOrderLineEntity> GetLinesByPurchaseIdPrefix(string headerKey)
        {
            var list = new List<PurchaseOrderLineEntity>();
            if (string.IsNullOrWhiteSpace(headerKey)) return list;
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                // Match rows whose PurchaseID starts with the header key (e.g. PO-20260702-0001-)
                // OR equals the header key exactly (covers both old and new data in one query).
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

        // ══ CREATE ─ BATCH PREFIX LOOKUPS ══════════════════════════════════════════════════════

        /// <summary>
        /// Returns MRQ batch prefixes that have NOT yet been linked to a PurchaseOrder.
        /// "Linked" means PurchaseOrder.RequestID starts with the same batch prefix.
        /// Uses PurchaseOrder.RequestID (NOT PurchaseOrderLine) for the existence check.
        /// </summary>
        public List<MaterialRequestBatchLookup> GetUnlinkedBatchPrefixes()
        {
            var list = new List<MaterialRequestBatchLookup>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                // A batch prefix is "linked" if any PurchaseOrder.RequestID starts with it.
                // We derive the prefix by stripping the last 3 chars (-NN) from mr.RequestID.
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

        /// <summary>
        /// Returns all MaterialRequest line items for a given batch prefix
        /// that have not yet been ordered (no matching PurchaseOrder.RequestID).
        /// </summary>
        public List<MaterialRequestLineItem> GetLineItemsByBatchPrefix(string batchPrefix)
        {
            var list = new List<MaterialRequestLineItem>();
            if (string.IsNullOrWhiteSpace(batchPrefix)) return list;
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                // Exclude MRQ lines whose RequestID already appears in PurchaseOrder.RequestID.
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

        // ══ CREATE ─ WRITE ════════════════════════════════════════════════════════════════
        //
        // Actual PurchaseOrder columns    : PurchaseID, RequestID, SupplierID, POTotalAmount, OrderDate, PurchaseStatus
        // Actual PurchaseOrderLine columns: POLineID, RawMaterialItemID, PurchaseID, WarehouseID, OrderQty, UnitPrice
        //
        // PurchaseOrder.RequestID = the representative MRQ RequestID for the whole batch.
        // PurchaseOrderLine has NO RequestID column — MRQ traceability is via PurchaseOrder.

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
            string urgencyLevel,    // caller compat only — not stored on PurchaseOrder
            string triggerType,     // caller compat only — not stored on PurchaseOrder
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
                        // Representative RequestID = first MRQ line's RequestID
                        string firstRequestId = lines.Count > 0 ? lines[0].RequestID : null;

                        // 1. PurchaseOrder header — only actual schema columns
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

                        // 2. PurchaseOrderLine rows — only actual schema columns (NO RequestID)
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

                        // 3. Audit log
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

        // ── Legacy single-line overload — kept so old callers compile ──────────────────
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

        // ══ ID GENERATORS ══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Generates the next available PurchaseID: PO-YYYYMMDD-NNNN.
        /// Counts only exact-length 17-char IDs so -NN suffixed POLineIDs are excluded.
        /// </summary>
        public string GenerateNextPurchaseId()
        {
            string prefix = $"PO-{DateTime.Today:yyyyMMdd}-";
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT COALESCE(MAX(CAST(SUBSTRING(PurchaseID, 14, 4) AS UNSIGNED)), 0) + 1
                      FROM   PurchaseOrder
                      WHERE  PurchaseID LIKE @prefix
                        AND  LENGTH(PurchaseID) = 17";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@prefix", prefix + "%");
                    var next = Convert.ToInt32(cmd.ExecuteScalar());
                    return $"{prefix}{next:D4}";
                }
            }
        }

        // ══ MAPPERS ════════════════════════════════════════════════════════════════════════

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

        /// <summary>
        /// Maps a PurchaseOrderLine row.
        /// PurchaseOrderLine has no RequestID column — RequestID on the entity is left empty.
        /// </summary>
        private static PurchaseOrderLineEntity MapPOLine(MySqlDataReader r)
            => new PurchaseOrderLineEntity
            {
                POLineID          = r["POLineID"].ToString(),
                PurchaseID        = r["PurchaseID"].ToString(),
                RequestID         = "",   // PurchaseOrderLine has no RequestID column
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
