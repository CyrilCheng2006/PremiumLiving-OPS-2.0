using MySql.Data.MySqlClient;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PremiumLivingOPS.Models.DAL
{
    /// <summary>
    /// DAL for Raw Material → Procurement module.
    /// PurchaseID format in DB: PO-YYYYMMDD-NNNN  (base)
    ///                       or PO-YYYYMMDD-NNNN-NN  (child with suffix)
    /// The Search grid groups by BaseID = LEFT(PurchaseID, 18), i.e. "PO-YYYYMMDD-NNNN".
    /// </summary>
    public class ProcurementRepo
    {
        // ══ SEARCH ══════════════════════════════════════════════════════════

        /// <summary>
        /// Returns one ProcurementOrderGroup per BASE Purchase Order group.
        /// BaseID = first 18 chars of PurchaseID  ("PO-YYYYMMDD-NNNN").
        /// Child POs (suffix -NN) are aggregated: ItemCount = SUM of lines,
        /// TotalAmount = SUM of POTotalAmount, Status/Supplier from any child.
        /// </summary>
        public List<ProcurementOrderGroup> SearchGroupedPurchaseOrders(
            string keyword = null, string status = null,
            DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            // Step 1: pull all matching raw PO rows with line counts
            var rawRows = new List<(string purchaseId, string supplierId, string supplierName,
                                    DateTime orderDate, string purchaseStatus,
                                    double totalAmount, int lineCount, string urgencyLevel)>();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var sql =
                    @"SELECT
                          po.PurchaseID,
                          po.SupplierID,
                          COALESCE(s.SupplierName, po.SupplierID)      AS SupplierName,
                          po.OrderDate,
                          po.PurchaseStatus,
                          po.POTotalAmount                             AS TotalAmount,
                          COALESCE(lc.LineCount, 0)                   AS ItemCount,
                          COALESCE(mr.UrgencyLevel, '')               AS UrgencyLevel
                      FROM  PurchaseOrder po
                      LEFT JOIN Supplier        s  ON po.SupplierID  = s.SupplierID
                      LEFT JOIN MaterialRequest mr ON po.RequestID   = mr.RequestID
                      LEFT JOIN (
                          SELECT PurchaseID, COUNT(*) AS LineCount
                          FROM   PurchaseOrderLine
                          GROUP  BY PurchaseID
                      ) lc ON lc.PurchaseID = po.PurchaseID
                      WHERE 1=1";

                if (!string.IsNullOrEmpty(keyword))
                    sql += " AND (po.PurchaseID LIKE @kw OR s.SupplierName LIKE @kw OR po.SupplierID LIKE @kw)";
                if (!string.IsNullOrEmpty(status) && status != "All")
                    sql += " AND po.PurchaseStatus = @status";
                if (dateFrom.HasValue)
                    sql += " AND po.OrderDate >= @dateFrom";
                if (dateTo.HasValue)
                    sql += " AND po.OrderDate <= @dateTo";

                sql += " ORDER BY po.OrderDate DESC, po.PurchaseID DESC";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(keyword))                    cmd.Parameters.AddWithValue("@kw",       "%" + keyword + "%");
                    if (!string.IsNullOrEmpty(status) && status != "All") cmd.Parameters.AddWithValue("@status",   status);
                    if (dateFrom.HasValue)                                 cmd.Parameters.AddWithValue("@dateFrom", dateFrom.Value.ToString("yyyy-MM-dd"));
                    if (dateTo.HasValue)                                   cmd.Parameters.AddWithValue("@dateTo",   dateTo.Value.ToString("yyyy-MM-dd"));

                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            rawRows.Add((
                                r["PurchaseID"].ToString(),
                                r["SupplierID"].ToString(),
                                r["SupplierName"].ToString(),
                                Convert.ToDateTime(r["OrderDate"]),
                                r["PurchaseStatus"].ToString(),
                                Convert.ToDouble(r["TotalAmount"]),
                                Convert.ToInt32(r["ItemCount"]),
                                r["UrgencyLevel"].ToString()
                            ));
                }
            }

            // Step 2: group by BaseID in C# (BaseID = first 18 chars "PO-YYYYMMDD-NNNN")
            //         A PurchaseID of exactly 18 chars is itself the base (no suffix).
            //         A PurchaseID of  21 chars ends with -NN suffix.
            var dict = new Dictionary<string, ProcurementOrderGroup>(StringComparer.Ordinal);
            foreach (var row in rawRows)
            {
                string baseId = ExtractBaseId(row.purchaseId);
                if (!dict.TryGetValue(baseId, out var grp))
                {
                    grp = new ProcurementOrderGroup
                    {
                        BasePurchaseID = baseId,
                        SupplierID     = row.supplierId,
                        SupplierName   = row.supplierName,
                        OrderDate      = row.orderDate,
                        PurchaseStatus = row.purchaseStatus,
                        UrgencyLevel   = row.urgencyLevel
                    };
                    dict[baseId] = grp;
                }
                grp.ChildPurchaseIDs.Add(row.purchaseId);
                grp.TotalAmount += row.totalAmount;
                grp.ItemCount   += row.lineCount;
                // prefer the most-severe urgency
                grp.UrgencyLevel = HigherUrgency(grp.UrgencyLevel, row.urgencyLevel);
            }

            return dict.Values
                       .OrderByDescending(g => g.OrderDate)
                       .ThenByDescending(g => g.BasePurchaseID)
                       .ToList();
        }

        // ══ GROUPED DETAIL (by BaseID) ══════════════════════════════════

        /// <summary>
        /// Returns header info + all child POs + their lines for the detail dialog.
        /// baseId = "PO-YYYYMMDD-NNNN" (no -NN suffix).
        /// Handles both plain POs (no suffix) and multi-line batches.
        /// </summary>
        public GroupedProcurementDetailViewModel GetGroupedDetailByBaseId(string baseId)
        {
            if (string.IsNullOrWhiteSpace(baseId)) return null;

            // Pattern: exact match OR starts with baseId + '-'
            string likePattern = baseId + "%";

            var children = new List<ProcurementChildGroup>();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                // 1. Fetch all child PO headers
                const string sqlHeaders =
                    @"SELECT po.PurchaseID,
                             po.RequestID,
                             po.SupplierID,
                             COALESCE(s.SupplierName, po.SupplierID)  AS SupplierName,
                             po.POTotalAmount,
                             po.OrderDate,
                             po.PurchaseStatus,
                             COALESCE(mr.UrgencyLevel, '')            AS UrgencyLevel,
                             COALESCE(mr.TriggerType,  '')            AS TriggerType
                      FROM   PurchaseOrder   po
                      LEFT JOIN Supplier       s  ON po.SupplierID = s.SupplierID
                      LEFT JOIN MaterialRequest mr ON po.RequestID  = mr.RequestID
                      WHERE  po.PurchaseID = @exact
                          OR po.PurchaseID LIKE @prefix
                      ORDER  BY po.PurchaseID";

                string supplierDisplay = string.Empty;
                string purchaseStatus  = string.Empty;
                double totalAmount     = 0;
                string orderDateStr    = string.Empty;

                using (var cmd = new MySqlCommand(sqlHeaders, conn))
                {
                    cmd.Parameters.AddWithValue("@exact",  baseId);
                    cmd.Parameters.AddWithValue("@prefix", baseId + "-%");

                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                        {
                            string pid = r["PurchaseID"].ToString();
                            double sub  = Convert.ToDouble(r["POTotalAmount"]);
                            totalAmount += sub;

                            if (string.IsNullOrEmpty(supplierDisplay))
                            {
                                string sid  = r["SupplierID"].ToString();
                                string sn   = r["SupplierName"].ToString();
                                supplierDisplay = string.IsNullOrEmpty(sid) ? sn : $"{sid}  —  {sn}";
                                purchaseStatus  = r["PurchaseStatus"].ToString();
                                orderDateStr    = Convert.ToDateTime(r["OrderDate"]).ToString("yyyy-MM-dd");
                            }

                            children.Add(new ProcurementChildGroup
                            {
                                PurchaseID     = pid,
                                RequestID      = r["RequestID"].ToString(),
                                UrgencyLevel   = r["UrgencyLevel"].ToString(),
                                TriggerType    = r["TriggerType"].ToString(),
                                PurchaseStatus = r["PurchaseStatus"].ToString(),
                                SubTotal       = sub
                            });
                        }
                }

                if (children.Count == 0) return null;

                // 2. Fetch lines for every child PO in one query
                var ids = children.ConvertAll(c => c.PurchaseID);
                string inClause = string.Join(",", ids.ConvertAll(id => "'" + id.Replace("'", "''") + "'"));

                string sqlLines =
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
                      WHERE  pol.PurchaseID IN (" + inClause + @")
                      ORDER  BY pol.PurchaseID, pol.POLineID";

                // group lines by PurchaseID
                var lineMap = new Dictionary<string, List<PurchaseOrderLineEntity>>(StringComparer.Ordinal);
                using (var cmd2 = new MySqlCommand(sqlLines, conn))
                using (var r2   = cmd2.ExecuteReader())
                    while (r2.Read())
                    {
                        string pid = r2["PurchaseID"].ToString();
                        if (!lineMap.ContainsKey(pid)) lineMap[pid] = new List<PurchaseOrderLineEntity>();
                        lineMap[pid].Add(MapPOLine(r2));
                    }

                foreach (var ch in children)
                    ch.Lines = lineMap.ContainsKey(ch.PurchaseID)
                             ? lineMap[ch.PurchaseID]
                             : new List<PurchaseOrderLineEntity>();

                return new GroupedProcurementDetailViewModel
                {
                    BasePurchaseID  = baseId,
                    SupplierDisplay = supplierDisplay,
                    PurchaseStatus  = purchaseStatus,
                    TotalAmount     = totalAmount,
                    OrderDateStr    = orderDateStr,
                    Children        = children
                };
            }
        }

        // ══ LEGACY SINGLE-PO DETAIL ══════════════════════════════════════

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
                             COALESCE(s.SupplierName, po.SupplierID)   AS SupplierName,
                             po.POTotalAmount,
                             po.OrderDate,
                             po.PurchaseStatus,
                             COALESCE(mr.RawMaterialItemID, '')        AS RawMaterialItemID,
                             COALESCE(i.ItemName, '')                  AS RawMaterialName,
                             COALESCE(mr.RequestedQty, 0)              AS RequestedQty,
                             COALESCE(mr.UrgencyLevel, '')             AS UrgencyLevel,
                             COALESCE(mr.TriggerType,  '')             AS TriggerType
                      FROM   PurchaseOrder   po
                      LEFT JOIN Supplier       s  ON po.SupplierID          = s.SupplierID
                      LEFT JOIN MaterialRequest mr ON po.RequestID           = mr.RequestID
                      LEFT JOIN RawMaterial    rm  ON mr.RawMaterialItemID   = rm.ItemID
                      LEFT JOIN Item           i   ON rm.ItemID              = i.ItemID
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
                    @"SELECT pol.POLineID,
                             pol.PurchaseID,
                             pol.RawMaterialItemID,
                             COALESCE(i.ItemName, pol.RawMaterialItemID)     AS MaterialName,
                             COALESCE(rm.MaterialType, '')                   AS MaterialType,
                             pol.WarehouseID,
                             COALESCE(w.WarehouseLocation, pol.WarehouseID)  AS WarehouseLocation,
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

        // ══ CREATE ─ BATCH PREFIX LOOKUPS ════════════════════════════════

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

        // ══ CREATE ─ WRITE ══════════════════════════════════════════

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

        // ══ ID GENERATORS ══════════════════════════════════════════════

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

        // ══ HELPERS ══════════════════════════════════════════════════

        /// <summary>
        /// Extracts the base ID from a PurchaseID.
        /// "PO-20260701-0001"    → "PO-20260701-0001"   (length 18, no suffix)
        /// "PO-20260701-0001-01" → "PO-20260701-0001"   (length 21, strip last 3)
        /// </summary>
        public static string ExtractBaseId(string purchaseId)
        {
            if (string.IsNullOrEmpty(purchaseId)) return purchaseId;
            // Base part is always 18 chars: PO- (3) + YYYYMMDD (8) + - (1) + NNNN (4) = 16... 
            // Actually: "PO-" = 3, "20260701" = 8, "-" = 1, "0001" = 4  → total 16
            // Wait: P-O-hyphen = 3 chars, YYYYMMDD = 8 chars, hyphen = 1 char, NNNN = 4 chars → 16 chars
            // "-NN" suffix = 3 more chars → 19 total for child
            // Let's use the actual string: find the 3rd hyphen position
            int h1 = purchaseId.IndexOf('-');             // after PO
            if (h1 < 0) return purchaseId;
            int h2 = purchaseId.IndexOf('-', h1 + 1);    // after YYYYMMDD
            if (h2 < 0) return purchaseId;
            int h3 = purchaseId.IndexOf('-', h2 + 1);    // after NNNN (base ends here)
            if (h3 < 0) return purchaseId;                // no 4th hyphen → IS the base
            // h3 points to the hyphen before NNNN – that IS the end of the base
            // A 4th hyphen would be the suffix separator
            int h4 = purchaseId.IndexOf('-', h3 + 1);    // suffix hyphen?
            return h4 >= 0 ? purchaseId.Substring(0, h4) : purchaseId;
        }

        private static string HigherUrgency(string a, string b)
        {
            int Rank(string u) => u == "Critical" ? 3 : u == "High" ? 2 : u == "Medium" ? 1 : 0;
            return Rank(a) >= Rank(b) ? a : b;
        }

        // ══ MAPPERS ══════════════════════════════════════════════════

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
