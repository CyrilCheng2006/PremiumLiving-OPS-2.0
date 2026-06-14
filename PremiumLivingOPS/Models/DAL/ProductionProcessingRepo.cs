using MySql.Data.MySqlClient;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.DAL
{
    /// <summary>
    /// DAL for Production Processing module.
    /// Covers: MaterialRequest (read + write), RawMaterial (lookup),
    ///         WarehouseItem (lookup), Order (lookup).
    /// </summary>
    public class ProductionProcessingRepo
    {
        // ════════════════════════════════════════════════════════════════
        //  SEARCH RAW MATERIAL REQUEST — read
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns a flat list of MaterialRequests joined with RawMaterial,
        /// Item, WarehouseItem and Warehouse for the Search grid.
        /// </summary>
        public List<MaterialRequestEntity> SearchMaterialRequests(
            string keyword      = null,
            string urgency      = null,
            string triggerType  = null,
            bool   linkedToPOOnly = false)
        {
            var list = new List<MaterialRequestEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var sql =
                    @"SELECT mr.RequestID, mr.OrderID,
                             mr.RawMaterialItemID,
                             i.ItemName           AS RawMaterialName,
                             rm.MaterialType,
                             mr.WarehouseItemID,
                             mr.RequestedQty, mr.UrgencyLevel, mr.TriggerType,
                             wi.WarehouseID, wi.WarehouseItemQuantity AS CurrentStock, wi.ReorderLevel,
                             w.WarehouseLocation,
                             (SELECT COUNT(1) FROM PurchaseOrder po WHERE po.RequestID = mr.RequestID) AS IsLinkedToPO
                      FROM   MaterialRequest mr
                      JOIN   RawMaterial  rm  ON mr.RawMaterialItemID = rm.ItemID
                      JOIN   Item         i   ON rm.ItemID            = i.ItemID
                      JOIN   WarehouseItem wi  ON mr.WarehouseItemID  = wi.WarehouseItemID
                      JOIN   Warehouse    w   ON wi.WarehouseID       = w.WarehouseID
                      WHERE  1=1";

                if (!string.IsNullOrEmpty(keyword))
                    sql += " AND (mr.RequestID LIKE @kw OR i.ItemName LIKE @kw OR mr.RawMaterialItemID LIKE @kw)";
                if (!string.IsNullOrEmpty(urgency) && urgency != "All")
                    sql += " AND mr.UrgencyLevel = @urgency";
                if (!string.IsNullOrEmpty(triggerType) && triggerType != "All")
                    sql += " AND mr.TriggerType = @trigger";
                if (linkedToPOOnly)
                    sql += " AND EXISTS (SELECT 1 FROM PurchaseOrder po WHERE po.RequestID = mr.RequestID)";

                sql += " ORDER BY mr.RequestID DESC";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(keyword))
                        cmd.Parameters.AddWithValue("@kw", "%" + keyword + "%");
                    if (!string.IsNullOrEmpty(urgency) && urgency != "All")
                        cmd.Parameters.AddWithValue("@urgency", urgency);
                    if (!string.IsNullOrEmpty(triggerType) && triggerType != "All")
                        cmd.Parameters.AddWithValue("@trigger", triggerType);

                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(MapMaterialRequest(r));
                }
            }
            return list;
        }

        // ════════════════════════════════════════════════════════════════
        //  GET MATERIAL REQUEST DETAIL — single record for detail dialog
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns full detail for a single MaterialRequest,
        /// including linked PurchaseOrder (LEFT JOIN — nullable).
        /// </summary>
        public MaterialRequestDetailEntity GetMaterialRequestDetail(string requestId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT mr.RequestID, mr.OrderID,
                             mr.RawMaterialItemID,
                             i.ItemName           AS RawMaterialName,
                             rm.MaterialType,
                             mr.WarehouseItemID,
                             wi.WarehouseID,
                             w.WarehouseLocation,
                             mr.RequestedQty, mr.UrgencyLevel, mr.TriggerType,
                             wi.WarehouseItemQuantity AS CurrentStock,
                             wi.ReorderLevel,
                             po.PurchaseID,
                             po.PurchaseStatus,
                             po.POTotalAmount
                      FROM   MaterialRequest mr
                      JOIN   RawMaterial  rm  ON mr.RawMaterialItemID = rm.ItemID
                      JOIN   Item         i   ON rm.ItemID            = i.ItemID
                      JOIN   WarehouseItem wi  ON mr.WarehouseItemID  = wi.WarehouseItemID
                      JOIN   Warehouse    w   ON wi.WarehouseID       = w.WarehouseID
                      LEFT JOIN PurchaseOrder po ON po.RequestID      = mr.RequestID
                      WHERE  mr.RequestID = @id
                      LIMIT  1";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", requestId);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read()) return null;
                        return new MaterialRequestDetailEntity
                        {
                            RequestID         = r["RequestID"].ToString(),
                            OrderID           = r["OrderID"]        == DBNull.Value ? null : r["OrderID"].ToString(),
                            RawMaterialItemID = r["RawMaterialItemID"].ToString(),
                            RawMaterialName   = r["RawMaterialName"].ToString(),
                            MaterialType      = r["MaterialType"].ToString(),
                            WarehouseItemID   = r["WarehouseItemID"].ToString(),
                            WarehouseID       = r["WarehouseID"].ToString(),
                            WarehouseLocation = r["WarehouseLocation"].ToString(),
                            RequestedQty      = Convert.ToInt32(r["RequestedQty"]),
                            UrgencyLevel      = r["UrgencyLevel"].ToString(),
                            TriggerType       = r["TriggerType"].ToString(),
                            CurrentStock      = Convert.ToInt32(r["CurrentStock"]),
                            ReorderLevel      = Convert.ToInt32(r["ReorderLevel"]),
                            PurchaseID        = r["PurchaseID"]     == DBNull.Value ? null : r["PurchaseID"].ToString(),
                            PurchaseStatus    = r["PurchaseStatus"] == DBNull.Value ? null : r["PurchaseStatus"].ToString(),
                            POTotalAmount     = r["POTotalAmount"]  == DBNull.Value ? (decimal?)null
                                                                                    : Convert.ToDecimal(r["POTotalAmount"])
                        };
                    }
                }
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  CREATE RAW MATERIAL REQUEST — lookups
        // ════════════════════════════════════════════════════════════════

        /// <summary>All raw materials for the dropdown.</summary>
        public List<RawMaterialLookup> GetAllRawMaterials()
        {
            var list = new List<RawMaterialLookup>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT rm.ItemID, i.ItemName, rm.MaterialType, rm.purchasePrice
                      FROM   RawMaterial rm
                      JOIN   Item i ON rm.ItemID = i.ItemID
                      ORDER  BY i.ItemName";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new RawMaterialLookup
                        {
                            ItemID        = r["ItemID"].ToString(),
                            ItemName      = r["ItemName"].ToString(),
                            MaterialType  = r["MaterialType"].ToString(),
                            PurchasePrice = Convert.ToDecimal(r["purchasePrice"])
                        });
            }
            return list;
        }

        /// <summary>
        /// Returns WarehouseItems that hold a specific RawMaterial.
        /// Used to populate the warehouse dropdown after a raw material is chosen.
        /// </summary>
        public List<WarehouseItemLookup> GetWarehouseItemsByMaterial(string rawMaterialItemId)
        {
            var list = new List<WarehouseItemLookup>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT wi.WarehouseItemID, wi.WarehouseID,
                             w.WarehouseLocation,
                             wi.ItemID,
                             wi.WarehouseItemQuantity AS CurrentStock,
                             wi.ReorderLevel
                      FROM   WarehouseItem wi
                      JOIN   Warehouse w ON wi.WarehouseID = w.WarehouseID
                      WHERE  wi.ItemID = @itemId
                      ORDER  BY w.WarehouseLocation";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@itemId", rawMaterialItemId);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new WarehouseItemLookup
                            {
                                WarehouseItemID   = r["WarehouseItemID"].ToString(),
                                WarehouseID       = r["WarehouseID"].ToString(),
                                WarehouseLocation = r["WarehouseLocation"].ToString(),
                                ItemID            = r["ItemID"].ToString(),
                                CurrentStock      = Convert.ToInt32(r["CurrentStock"]),
                                ReorderLevel      = Convert.ToInt32(r["ReorderLevel"])
                            });
                }
            }
            return list;
        }

        /// <summary>All active orders for the OrderDemand dropdown.</summary>
        public List<OrderLookup> GetActiveOrders()
        {
            var list = new List<OrderLookup>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT OrderID, CustomerID, OrderStatus
                      FROM   `Order`
                      WHERE  OrderStatus IN ('Processing','Pending','Partially Delivered')
                      ORDER  BY OrderID DESC";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new OrderLookup
                        {
                            OrderID     = r["OrderID"].ToString(),
                            CustomerID  = r["CustomerID"].ToString(),
                            OrderStatus = r["OrderStatus"].ToString()
                        });
            }
            return list;
        }

        // ════════════════════════════════════════════════════════════════
        //  CREATE RAW MATERIAL REQUEST — write
        // ════════════════════════════════════════════════════════════════

        public void CreateMaterialRequest(
            string requestId, string orderId, string rawMaterialItemId,
            string warehouseItemId, int requestedQty,
            string urgencyLevel, string triggerType, string staffId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var trx = conn.BeginTransaction())
                {
                    try
                    {
                        const string insertMR =
                            @"INSERT INTO MaterialRequest
                                (RequestID, OrderID, RawMaterialItemID, WarehouseItemID,
                                 RequestedQty, UrgencyLevel, TriggerType)
                              VALUES
                                (@requestId, @orderId, @matId, @whItemId,
                                 @qty, @urgency, @trigger)";

                        using (var cmd = new MySqlCommand(insertMR, conn, trx))
                        {
                            cmd.Parameters.AddWithValue("@requestId", requestId);
                            cmd.Parameters.AddWithValue("@orderId",   string.IsNullOrEmpty(orderId) ? (object)DBNull.Value : orderId);
                            cmd.Parameters.AddWithValue("@matId",     rawMaterialItemId);
                            cmd.Parameters.AddWithValue("@whItemId",  warehouseItemId);
                            cmd.Parameters.AddWithValue("@qty",       requestedQty);
                            cmd.Parameters.AddWithValue("@urgency",   urgencyLevel);
                            cmd.Parameters.AddWithValue("@trigger",   triggerType);
                            cmd.ExecuteNonQuery();
                        }
                        trx.Commit();
                    }
                    catch
                    {
                        trx.Rollback();
                        throw;
                    }
                }
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  HELPERS
        // ════════════════════════════════════════════════════════════════

        public string GenerateNextRequestId()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string today = DateTime.Now.ToString("yyyyMMdd");
                string prefix = $"MRQ-{today}-";
                const string sql =
                    @"SELECT RequestID FROM MaterialRequest
                      WHERE  RequestID LIKE @prefix
                      ORDER  BY RequestID DESC LIMIT 1";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@prefix", prefix + "%");
                    var last = cmd.ExecuteScalar()?.ToString();
                    if (string.IsNullOrEmpty(last))
                        return prefix + "0001";
                    int seq = int.Parse(last.Substring(last.LastIndexOf('-') + 1)) + 1;
                    return prefix + seq.ToString("D4");
                }
            }
        }

        private static MaterialRequestEntity MapMaterialRequest(MySqlDataReader r)
            => new MaterialRequestEntity
            {
                RequestID         = r["RequestID"].ToString(),
                OrderID           = r["OrderID"]        == DBNull.Value ? null : r["OrderID"].ToString(),
                RawMaterialItemID = r["RawMaterialItemID"].ToString(),
                RawMaterialName   = r["RawMaterialName"].ToString(),
                MaterialType      = r["MaterialType"].ToString(),
                WarehouseItemID   = r["WarehouseItemID"].ToString(),
                RequestedQty      = Convert.ToInt32(r["RequestedQty"]),
                UrgencyLevel      = r["UrgencyLevel"].ToString(),
                TriggerType       = r["TriggerType"].ToString(),
                WarehouseID       = r["WarehouseID"].ToString(),
                WarehouseLocation = r["WarehouseLocation"].ToString(),
                CurrentStock      = Convert.ToInt32(r["CurrentStock"]),
                ReorderLevel      = Convert.ToInt32(r["ReorderLevel"]),
                IsLinkedToPO      = Convert.ToInt32(r["IsLinkedToPO"]) > 0
            };
    }
}
