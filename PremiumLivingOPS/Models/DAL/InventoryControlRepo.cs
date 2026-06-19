using MySql.Data.MySqlClient;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.DAL
{
    /// <summary>
    /// DAL for Inventory Control module.
    /// Covers: Product, RawMaterial, WarehouseItem, Warehouse, TransferForm.
    /// </summary>
    public class InventoryControlRepo
    {
        // ════════════════════════════════════════════════════════════════
        //  PRODUCT — read
        // ════════════════════════════════════════════════════════════════

        public List<ProductEntity> SearchProducts(string keyword = null, string category = null)
        {
            var list = new List<ProductEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var sql =
                    @"SELECT p.ItemID, i.ItemName, i.ItemDescription,
                             p.Category, p.SalesPrice,
                             COALESCE(SUM(wi.WarehouseItemQuantity),0) AS StockQty,
                             COALESCE(MIN(wi.ReorderLevel),0)          AS ReorderLevel
                      FROM   Product p
                      JOIN   Item i              ON p.ItemID  = i.ItemID
                      LEFT JOIN WarehouseItem wi ON wi.ItemID = p.ItemID
                      WHERE  1=1";
                if (!string.IsNullOrEmpty(keyword))
                    sql += " AND (p.ItemID LIKE @kw OR i.ItemName LIKE @kw OR p.Category LIKE @kw)";
                if (!string.IsNullOrEmpty(category) && category != "All")
                    sql += " AND p.Category = @category";
                sql += " GROUP BY p.ItemID, i.ItemName, i.ItemDescription, p.Category, p.SalesPrice ORDER BY i.ItemName";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(keyword))  cmd.Parameters.AddWithValue("@kw", "%" + keyword + "%");
                    if (!string.IsNullOrEmpty(category) && category != "All") cmd.Parameters.AddWithValue("@category", category);
                    using (var r = cmd.ExecuteReader()) while (r.Read()) list.Add(MapProduct(r));
                }
            }
            return list;
        }

        public List<ProductEntity> GetAllProducts() => SearchProducts();

        public ProductEntity GetProductById(string itemId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT p.ItemID, i.ItemName, i.ItemDescription,
                             p.Category, p.SalesPrice,
                             COALESCE(SUM(wi.WarehouseItemQuantity),0) AS StockQty,
                             COALESCE(MIN(wi.ReorderLevel),0)          AS ReorderLevel
                      FROM   Product p
                      JOIN   Item i              ON p.ItemID  = i.ItemID
                      LEFT JOIN WarehouseItem wi ON wi.ItemID = p.ItemID
                      WHERE  p.ItemID = @id
                      GROUP BY p.ItemID, i.ItemName, i.ItemDescription, p.Category, p.SalesPrice";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", itemId);
                    using (var r = cmd.ExecuteReader())
                        if (r.Read()) return MapProduct(r);
                }
            }
            return null;
        }

        public List<string> GetProductCategories()
        {
            var list = new List<string> { "All" };
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand("SELECT DISTINCT Category FROM Product ORDER BY Category", conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) { string c = r.IsDBNull(0) ? null : r.GetString(0); if (!string.IsNullOrEmpty(c)) list.Add(c); }
            }
            return list;
        }

        // ════════════════════════════════════════════════════════════════
        //  PRODUCT — write
        // ════════════════════════════════════════════════════════════════

        /// <summary>Insert Item + Product rows and an initial WarehouseItem row.</summary>
        public void AddProduct(string itemId, string itemName, string itemDesc,
                               string category, double salesPrice,
                               string warehouseId, int initialQty, int reorderLevel)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Item
                        Run(conn, tx,
                            "INSERT INTO Item (ItemID,ItemName,ItemDescription) VALUES (@id,@name,@desc)",
                            ("@id", itemId), ("@name", itemName), ("@desc", (object)itemDesc ?? DBNull.Value));

                        // 2. Product
                        Run(conn, tx,
                            "INSERT INTO Product (ItemID,SalesPrice,Category) VALUES (@id,@price,@cat)",
                            ("@id", itemId), ("@price", salesPrice), ("@cat", category));

                        // 3. WarehouseItem
                        string wiId = GenerateWarehouseItemId(conn, tx);
                        Run(conn, tx,
                            "INSERT INTO WarehouseItem (WarehouseItemID,ItemID,WarehouseID,WarehouseItemQuantity,ReorderLevel) VALUES (@wid,@iid,@whid,@qty,@rl)",
                            ("@wid", wiId), ("@iid", itemId), ("@whid", warehouseId),
                            ("@qty", initialQty), ("@rl", reorderLevel));

                        tx.Commit();
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        /// <summary>Update Item + Product master data.</summary>
        public void UpdateProduct(string itemId, string itemName, string itemDesc,
                                  string category, double salesPrice)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        Run(conn, tx,
                            "UPDATE Item SET ItemName=@name, ItemDescription=@desc WHERE ItemID=@id",
                            ("@name", itemName), ("@desc", (object)itemDesc ?? DBNull.Value), ("@id", itemId));
                        Run(conn, tx,
                            "UPDATE Product SET SalesPrice=@price, Category=@cat WHERE ItemID=@id",
                            ("@price", salesPrice), ("@cat", category), ("@id", itemId));
                        tx.Commit();
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        /// <summary>Delete Product + Item (cascade: WarehouseItem rows deleted first).</summary>
        public void DeleteProduct(string itemId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        Run(conn, tx, "DELETE FROM WarehouseItem WHERE ItemID=@id", ("@id", itemId));
                        Run(conn, tx, "DELETE FROM Product WHERE ItemID=@id",       ("@id", itemId));
                        Run(conn, tx, "DELETE FROM Item WHERE ItemID=@id",           ("@id", itemId));
                        tx.Commit();
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  RAW MATERIAL — read
        // ════════════════════════════════════════════════════════════════

        public List<RawMaterialEntity> SearchRawMaterials(string keyword = null, string category = null)
        {
            var list = new List<RawMaterialEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var sql =
                    @"SELECT rm.ItemID AS MaterialID, i.ItemName AS MaterialName,
                             i.ItemDescription,
                             rm.MaterialType AS Category, rm.purchasePrice AS UnitCost,
                             COALESCE(SUM(wi.WarehouseItemQuantity),0) AS StockQty,
                             COALESCE(MIN(wi.ReorderLevel),0)          AS ReorderLevel
                      FROM   RawMaterial rm
                      JOIN   Item i              ON rm.ItemID = i.ItemID
                      LEFT JOIN WarehouseItem wi ON wi.ItemID = rm.ItemID
                      WHERE  1=1";
                if (!string.IsNullOrEmpty(keyword))
                    sql += " AND (rm.ItemID LIKE @kw OR i.ItemName LIKE @kw OR rm.MaterialType LIKE @kw)";
                if (!string.IsNullOrEmpty(category) && category != "All")
                    sql += " AND rm.MaterialType = @category";
                sql += " GROUP BY rm.ItemID, i.ItemName, i.ItemDescription, rm.MaterialType, rm.purchasePrice ORDER BY i.ItemName";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(keyword))  cmd.Parameters.AddWithValue("@kw", "%" + keyword + "%");
                    if (!string.IsNullOrEmpty(category) && category != "All") cmd.Parameters.AddWithValue("@category", category);
                    using (var r = cmd.ExecuteReader()) while (r.Read()) list.Add(MapRawMaterial(r));
                }
            }
            return list;
        }

        public List<RawMaterialEntity> GetAllRawMaterials() => SearchRawMaterials();

        public RawMaterialEntity GetRawMaterialById(string itemId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT rm.ItemID AS MaterialID, i.ItemName AS MaterialName,
                             i.ItemDescription,
                             rm.MaterialType AS Category, rm.purchasePrice AS UnitCost,
                             COALESCE(SUM(wi.WarehouseItemQuantity),0) AS StockQty,
                             COALESCE(MIN(wi.ReorderLevel),0)          AS ReorderLevel
                      FROM   RawMaterial rm
                      JOIN   Item i              ON rm.ItemID = i.ItemID
                      LEFT JOIN WarehouseItem wi ON wi.ItemID = rm.ItemID
                      WHERE  rm.ItemID = @id
                      GROUP BY rm.ItemID, i.ItemName, i.ItemDescription, rm.MaterialType, rm.purchasePrice";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", itemId);
                    using (var r = cmd.ExecuteReader())
                        if (r.Read()) return MapRawMaterial(r);
                }
            }
            return null;
        }

        public List<string> GetRawMaterialCategories()
        {
            var list = new List<string> { "All" };
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand("SELECT DISTINCT MaterialType FROM RawMaterial ORDER BY MaterialType", conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) { string c = r.IsDBNull(0) ? null : r.GetString(0); if (!string.IsNullOrEmpty(c)) list.Add(c); }
            }
            return list;
        }

        // ════════════════════════════════════════════════════════════════
        //  RAW MATERIAL — write
        // ════════════════════════════════════════════════════════════════

        public void AddRawMaterial(string itemId, string itemName, string itemDesc,
                                   string materialType, double purchasePrice,
                                   string warehouseId, int initialQty, int reorderLevel)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        Run(conn, tx,
                            "INSERT INTO Item (ItemID,ItemName,ItemDescription) VALUES (@id,@name,@desc)",
                            ("@id", itemId), ("@name", itemName), ("@desc", (object)itemDesc ?? DBNull.Value));
                        Run(conn, tx,
                            "INSERT INTO RawMaterial (ItemID,purchasePrice,MaterialType) VALUES (@id,@price,@type)",
                            ("@id", itemId), ("@price", purchasePrice), ("@type", materialType));
                        string wiId = GenerateWarehouseItemId(conn, tx);
                        Run(conn, tx,
                            "INSERT INTO WarehouseItem (WarehouseItemID,ItemID,WarehouseID,WarehouseItemQuantity,ReorderLevel) VALUES (@wid,@iid,@whid,@qty,@rl)",
                            ("@wid", wiId), ("@iid", itemId), ("@whid", warehouseId),
                            ("@qty", initialQty), ("@rl", reorderLevel));
                        tx.Commit();
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        public void UpdateRawMaterial(string itemId, string itemName, string itemDesc,
                                      string materialType, double purchasePrice)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        Run(conn, tx,
                            "UPDATE Item SET ItemName=@name, ItemDescription=@desc WHERE ItemID=@id",
                            ("@name", itemName), ("@desc", (object)itemDesc ?? DBNull.Value), ("@id", itemId));
                        Run(conn, tx,
                            "UPDATE RawMaterial SET purchasePrice=@price, MaterialType=@type WHERE ItemID=@id",
                            ("@price", purchasePrice), ("@type", materialType), ("@id", itemId));
                        tx.Commit();
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        public void DeleteRawMaterial(string itemId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        Run(conn, tx, "DELETE FROM WarehouseItem WHERE ItemID=@id",   ("@id", itemId));
                        Run(conn, tx, "DELETE FROM RawMaterial WHERE ItemID=@id",     ("@id", itemId));
                        Run(conn, tx, "DELETE FROM Item WHERE ItemID=@id",            ("@id", itemId));
                        tx.Commit();
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  WAREHOUSE
        // ════════════════════════════════════════════════════════════════

        public List<WarehouseEntity> GetAllWarehouses()
        {
            var list = new List<WarehouseEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql = "SELECT WarehouseID, ManagerID, WarehouseLocation, ContactNumber, Capacity FROM Warehouse ORDER BY WarehouseLocation";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new WarehouseEntity
                        {
                            WarehouseID       = r.GetString("WarehouseID"),
                            ManagerID         = r.GetString("ManagerID"),
                            WarehouseLocation = r.GetString("WarehouseLocation"),
                            ContactNumber     = r.GetString("ContactNumber"),
                            Capacity          = r.GetInt32("Capacity")
                        });
            }
            return list;
        }

        // ════════════════════════════════════════════════════════════════
        //  WAREHOUSE ITEM — per-warehouse breakdown
        // ════════════════════════════════════════════════════════════════

        public List<WarehouseItemEntity> GetWarehouseItemsByItemId(string itemId)
        {
            var list = new List<WarehouseItemEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT wi.WarehouseItemID, wi.ItemID, i.ItemName,
                             wi.WarehouseID, w.WarehouseLocation,
                             wi.WarehouseItemQuantity AS Quantity,
                             wi.ReorderLevel
                      FROM   WarehouseItem wi
                      JOIN   Item i      ON i.ItemID      = wi.ItemID
                      JOIN   Warehouse w ON w.WarehouseID = wi.WarehouseID
                      WHERE  wi.ItemID = @id
                      ORDER BY w.WarehouseLocation";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", itemId);
                    using (var r = cmd.ExecuteReader()) while (r.Read()) list.Add(MapWarehouseItem(r));
                }
            }
            return list;
        }

        public List<WarehouseItemEntity> GetAllWarehouseItems()
        {
            var list = new List<WarehouseItemEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT wi.WarehouseItemID, wi.ItemID, i.ItemName,
                             wi.WarehouseID, w.WarehouseLocation,
                             wi.WarehouseItemQuantity AS Quantity,
                             wi.ReorderLevel
                      FROM   WarehouseItem wi
                      JOIN   Item i      ON i.ItemID      = wi.ItemID
                      JOIN   Warehouse w ON w.WarehouseID = wi.WarehouseID
                      ORDER BY i.ItemName, w.WarehouseLocation";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) list.Add(MapWarehouseItem(r));
            }
            return list;
        }

        // ════════════════════════════════════════════════════════════════
        //  INWARD GOODS — add stock qty to an existing WarehouseItem row
        //  (or create a new WarehouseItem if the item isn't in that warehouse)
        // ════════════════════════════════════════════════════════════════

        public void RecordInwardGoods(string itemId, string warehouseId, int qtyReceived)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        // Check if WarehouseItem row exists
                        string wiId = null;
                        using (var cmd = new MySqlCommand(
                            "SELECT WarehouseItemID FROM WarehouseItem WHERE ItemID=@iid AND WarehouseID=@whid LIMIT 1", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@iid",  itemId);
                            cmd.Parameters.AddWithValue("@whid", warehouseId);
                            var obj = cmd.ExecuteScalar();
                            if (obj != null) wiId = obj.ToString();
                        }

                        if (wiId != null)
                        {
                            // Update existing
                            Run(conn, tx,
                                "UPDATE WarehouseItem SET WarehouseItemQuantity = WarehouseItemQuantity + @qty WHERE WarehouseItemID=@wid",
                                ("@qty", qtyReceived), ("@wid", wiId));
                        }
                        else
                        {
                            // Create new row with default ReorderLevel = 0
                            wiId = GenerateWarehouseItemId(conn, tx);
                            Run(conn, tx,
                                "INSERT INTO WarehouseItem (WarehouseItemID,ItemID,WarehouseID,WarehouseItemQuantity,ReorderLevel) VALUES (@wid,@iid,@whid,@qty,0)",
                                ("@wid", wiId), ("@iid", itemId), ("@whid", warehouseId), ("@qty", qtyReceived));
                        }
                        tx.Commit();
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  WAREHOUSE TRANSFER
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Creates a TransferForm + TransferForm_WarehouseItem line,
        /// deducts qty from source WarehouseItem and adds to destination.
        /// Destination WarehouseItem row is auto-created if absent.
        /// </summary>
        public void RecordWarehouseTransfer(
            string transferId,
            string fromWarehouseItemId,
            string toWarehouseId,
            int    transferQty)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        // Get source row details
                        string fromItemId = null;
                        int    fromQty    = 0;
                        using (var cmd = new MySqlCommand(
                            "SELECT ItemID, WarehouseItemQuantity FROM WarehouseItem WHERE WarehouseItemID=@id", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@id", fromWarehouseItemId);
                            using (var r = cmd.ExecuteReader())
                            {
                                if (r.Read()) { fromItemId = r.GetString(0); fromQty = r.GetInt32(1); }
                            }
                        }
                        if (fromItemId == null) throw new Exception("Source warehouse item not found.");
                        if (fromQty < transferQty) throw new Exception($"Insufficient stock. Available: {fromQty}");

                        // Find or create destination WarehouseItem
                        string toWarehouseItemId = null;
                        using (var cmd = new MySqlCommand(
                            "SELECT WarehouseItemID FROM WarehouseItem WHERE ItemID=@iid AND WarehouseID=@whid LIMIT 1", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@iid",  fromItemId);
                            cmd.Parameters.AddWithValue("@whid", toWarehouseId);
                            var obj = cmd.ExecuteScalar();
                            if (obj != null) toWarehouseItemId = obj.ToString();
                        }
                        if (toWarehouseItemId == null)
                        {
                            toWarehouseItemId = GenerateWarehouseItemId(conn, tx);
                            Run(conn, tx,
                                "INSERT INTO WarehouseItem (WarehouseItemID,ItemID,WarehouseID,WarehouseItemQuantity,ReorderLevel) VALUES (@wid,@iid,@whid,0,0)",
                                ("@wid", toWarehouseItemId), ("@iid", fromItemId), ("@whid", toWarehouseId));
                        }

                        // Deduct source
                        Run(conn, tx,
                            "UPDATE WarehouseItem SET WarehouseItemQuantity=WarehouseItemQuantity-@qty WHERE WarehouseItemID=@id",
                            ("@qty", transferQty), ("@id", fromWarehouseItemId));

                        // Add to destination
                        Run(conn, tx,
                            "UPDATE WarehouseItem SET WarehouseItemQuantity=WarehouseItemQuantity+@qty WHERE WarehouseItemID=@id",
                            ("@qty", transferQty), ("@id", toWarehouseItemId));

                        // Insert TransferForm header
                        Run(conn, tx,
                            "INSERT INTO TransferForm (TransferID,TransferDate,TransferStatus) VALUES (@tid,@date,'Completed')",
                            ("@tid", transferId), ("@date", DateTime.Today.ToString("yyyy-MM-dd")));

                        // Insert TransferForm_WarehouseItem line
                        string lineId = "TL-" + transferId.Substring(3);
                        Run(conn, tx,
                            "INSERT INTO TransferForm_WarehouseItem (TransferLineID,TransferID,FromWarehouseItemID,ToWarehouseItemID,TransferQuantity) VALUES (@lid,@tid,@from,@to,@qty)",
                            ("@lid", lineId), ("@tid", transferId),
                            ("@from", fromWarehouseItemId), ("@to", toWarehouseItemId), ("@qty", transferQty));

                        tx.Commit();
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  ALL ITEMS LOOKUP (products + raw materials combined)
        // ════════════════════════════════════════════════════════════════

        public List<ItemLookup> GetAllItemsLookup()
        {
            var list = new List<ItemLookup>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    @"SELECT i.ItemID, i.ItemName, 'Product' AS ItemType
                      FROM   Item i JOIN Product p ON p.ItemID=i.ItemID
                      UNION ALL
                      SELECT i.ItemID, i.ItemName, 'Raw Material' AS ItemType
                      FROM   Item i JOIN RawMaterial rm ON rm.ItemID=i.ItemID
                      ORDER BY ItemName";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new ItemLookup
                        {
                            ItemID   = r.GetString("ItemID"),
                            ItemName = r.GetString("ItemName"),
                            ItemType = r.GetString("ItemType")
                        });
            }
            return list;
        }

        // ════════════════════════════════════════════════════════════════
        //  ID GENERATORS
        // ════════════════════════════════════════════════════════════════

        public string GenerateNextTransferId()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand(
                    "SELECT COUNT(*) FROM TransferForm", conn))
                {
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    return $"TRF-{(count + 1):D4}";
                }
            }
        }

        /// <summary>
        /// Returns the next available Product Item ID in the format IID-P-XXXX.
        /// Extracts the MAX numeric suffix from existing IID-P-* rows so that
        /// gaps caused by deletions never cause a collision.
        /// </summary>
        public string GenerateNextProductItemId()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                // CAST the 7-char suffix to UNSIGNED to get a true numeric MAX,
                // avoiding lexicographic ordering issues (e.g. '9' > '10').
                const string sql =
                    @"SELECT COALESCE(
                          MAX(CAST(SUBSTRING(ItemID, 7) AS UNSIGNED)),
                          0
                      )
                      FROM Product
                      WHERE ItemID LIKE 'IID-P-%'
                        AND LENGTH(ItemID) = 11
                        AND SUBSTRING(ItemID, 7) REGEXP '^[0-9]{4}$'";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    int maxSeq = Convert.ToInt32(cmd.ExecuteScalar());
                    return $"IID-P-{(maxSeq + 1):D4}";
                }
            }
        }

        private static string GenerateWarehouseItemId(MySqlConnection conn, MySqlTransaction tx)
        {
            using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM WarehouseItem", conn, tx))
            {
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return $"WI-{(count + 1):D4}";
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  HELPERS
        // ════════════════════════════════════════════════════════════════

        private static void Run(MySqlConnection conn, MySqlTransaction tx,
            string sql, params (string name, object value)[] ps)
        {
            using (var cmd = new MySqlCommand(sql, conn, tx))
            {
                foreach (var (n, v) in ps)
                    cmd.Parameters.AddWithValue(n, v ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        private static ProductEntity MapProduct(MySqlDataReader r) => new ProductEntity
        {
            ItemID          = r.GetString("ItemID"),
            ItemName        = r.GetString("ItemName"),
            ItemDescription = r.IsDBNull(r.GetOrdinal("ItemDescription")) ? "" : r.GetString("ItemDescription"),
            Category        = r.IsDBNull(r.GetOrdinal("Category"))        ? "" : r.GetString("Category"),
            SalesPrice      = Convert.ToDouble(r["SalesPrice"]),
            StockQty        = r.IsDBNull(r.GetOrdinal("StockQty"))        ? 0  : Convert.ToInt32(r["StockQty"]),
            ReorderLevel    = r.IsDBNull(r.GetOrdinal("ReorderLevel"))    ? 0  : Convert.ToInt32(r["ReorderLevel"])
        };

        private static RawMaterialEntity MapRawMaterial(MySqlDataReader r) => new RawMaterialEntity
        {
            MaterialID      = r.GetString("MaterialID"),
            MaterialName    = r.GetString("MaterialName"),
            ItemDescription = r.IsDBNull(r.GetOrdinal("ItemDescription")) ? "" : r.GetString("ItemDescription"),
            Category        = r.IsDBNull(r.GetOrdinal("Category"))        ? "" : r.GetString("Category"),
            Unit            = "",
            UnitCost        = Convert.ToDouble(r["UnitCost"]),
            StockQty        = r.IsDBNull(r.GetOrdinal("StockQty"))        ? 0  : Convert.ToInt32(r["StockQty"]),
            ReorderLevel    = r.IsDBNull(r.GetOrdinal("ReorderLevel"))    ? 0  : Convert.ToInt32(r["ReorderLevel"])
        };

        private static WarehouseItemEntity MapWarehouseItem(MySqlDataReader r) => new WarehouseItemEntity
        {
            WarehouseItemID = r.GetString("WarehouseItemID"),
            ItemID          = r.GetString("ItemID"),
            ItemName        = r.GetString("ItemName"),
            WarehouseID     = r.GetString("WarehouseID"),
            WarehouseName   = r.GetString("WarehouseLocation"),
            Quantity        = r.GetInt32("Quantity"),
            ReorderLevel    = r.GetInt32("ReorderLevel")
        };
    }
}
