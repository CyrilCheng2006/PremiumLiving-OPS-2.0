using MySql.Data.MySqlClient;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.DAL
{
    /// <summary>
    /// DAL for Raw Material → Procurement module.
    /// Covers: PurchaseOrder, PurchaseOrderLine, MaterialRequest (read-only lookup),
    ///         Supplier (read-only lookup).
    /// </summary>
    public class ProcurementRepo
    {
        // ════════════════════════════════════════════════════════════════
        //  SEARCH PROCUREMENT — read
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns a flat list of PurchaseOrders joined with Supplier and
        /// MaterialRequest for display in the Search Procurement grid.
        /// </summary>
        public List<ProcurementOrderEntity> SearchPurchaseOrders(
            string keyword    = null,
            string status     = null,
            DateTime? dateFrom = null,
            DateTime? dateTo   = null)
        {
            var list = new List<ProcurementOrderEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var sql =
                    @"SELECT po.PurchaseID, po.RequestID, po.SupplierID,
                             s.SupplierName,
                             po.POTotalAmount, po.OrderDate, po.PurchaseStatus,
                             mr.RawMaterialItemID,
                             i.ItemName          AS RawMaterialName,
                             mr.RequestedQty, mr.UrgencyLevel, mr.TriggerType
                      FROM   PurchaseOrder po
                      JOIN   Supplier       s  ON po.SupplierID  = s.SupplierID
                      JOIN   MaterialRequest mr ON po.RequestID  = mr.RequestID
                      JOIN   RawMaterial    rm  ON mr.RawMaterialItemID = rm.ItemID
                      JOIN   Item           i   ON rm.ItemID     = i.ItemID
                      WHERE  1=1";

                if (!string.IsNullOrEmpty(keyword))
                    sql += " AND (po.PurchaseID LIKE @kw OR s.SupplierName LIKE @kw OR i.ItemName LIKE @kw)";
                if (!string.IsNullOrEmpty(status) && status != "All")
                    sql += " AND po.PurchaseStatus = @status";
                if (dateFrom.HasValue)
                    sql += " AND po.OrderDate >= @dateFrom";
                if (dateTo.HasValue)
                    sql += " AND po.OrderDate <= @dateTo";

                sql += " ORDER BY po.OrderDate DESC";

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
                            list.Add(MapProcurementOrder(r));
                }
            }
            return list;
        }

        /// <summary>Get a single PurchaseOrder by PurchaseID.</summary>
        public ProcurementOrderEntity GetPurchaseOrderById(string purchaseId)
        {
            if (string.IsNullOrWhiteSpace(purchaseId)) return null;
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT po.PurchaseID, po.RequestID, po.SupplierID,
                             s.SupplierName,
                             po.POTotalAmount, po.OrderDate, po.PurchaseStatus,
                             mr.RawMaterialItemID,
                             i.ItemName          AS RawMaterialName,
                             mr.RequestedQty, mr.UrgencyLevel, mr.TriggerType
                      FROM   PurchaseOrder po
                      JOIN   Supplier       s  ON po.SupplierID  = s.SupplierID
                      JOIN   MaterialRequest mr ON po.RequestID  = mr.RequestID
                      JOIN   RawMaterial    rm  ON mr.RawMaterialItemID = rm.ItemID
                      JOIN   Item           i   ON rm.ItemID     = i.ItemID
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

        /// <summary>Get all PurchaseOrderLines for a given PurchaseOrder.</summary>
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
                             i.ItemName   AS MaterialName,
                             rm.MaterialType,
                             pol.WarehouseID,
                             w.WarehouseLocation,
                             pol.OrderQty, pol.UnitPrice
                      FROM   PurchaseOrderLine pol
                      JOIN   RawMaterial rm ON pol.RawMaterialItemID = rm.ItemID
                      JOIN   Item        i  ON rm.ItemID             = i.ItemID
                      JOIN   Warehouse   w  ON pol.WarehouseID       = w.WarehouseID
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

        // ════════════════════════════════════════════════════════════════
        //  CREATE PROCUREMENT — lookups
        // ════════════════════════════════════════════════════════════════

        public List<MaterialRequestLookup> GetUnlinkedMaterialRequests()
        {
            var list = new List<MaterialRequestLookup>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT mr.RequestID, mr.RawMaterialItemID,
                             i.ItemName AS MaterialName,
                             mr.RequestedQty, mr.UrgencyLevel, mr.TriggerType
                      FROM   MaterialRequest mr
                      JOIN   RawMaterial rm  ON mr.RawMaterialItemID = rm.ItemID
                      JOIN   Item        i   ON rm.ItemID            = i.ItemID
                      WHERE  mr.RequestID NOT IN (
                                 SELECT po.RequestID FROM PurchaseOrder po
                             )
                      ORDER  BY mr.RequestID";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new MaterialRequestLookup
                        {
                            RequestID     = r["RequestID"].ToString(),
                            RawMaterialID = r["RawMaterialItemID"].ToString(),
                            MaterialName  = r["MaterialName"].ToString(),
                            RequestedQty  = Convert.ToInt32(r["RequestedQty"]),
                            UrgencyLevel  = r["UrgencyLevel"].ToString(),
                            TriggerType   = r["TriggerType"].ToString()
                        });
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
                      FROM   Supplier
                      ORDER  BY SupplierName";
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

        // ════════════════════════════════════════════════════════════════
        //  CREATE PROCUREMENT — write
        // ════════════════════════════════════════════════════════════════

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

        // ════════════════════════════════════════════════════════════════
        //  ID GENERATORS
        // ════════════════════════════════════════════════════════════════

        public string GenerateNextPurchaseId()
        {
            string prefix = $"PO-{DateTime.Today:yyyyMMdd}-";
            // prefix length = 3 + 1 + 8 + 1 = 13 chars  =>  SUBSTRING(PurchaseID, 14)
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT COALESCE(MAX(CAST(SUBSTRING(PurchaseID, 14) AS UNSIGNED)), 0) + 1
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
            // prefix length = 4 + 1 + 8 + 1 = 14 chars  =>  SUBSTRING(POLineID, 15)
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

        // ════════════════════════════════════════════════════════════════
        //  MAPPERS
        // ════════════════════════════════════════════════════════════════

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
