using MySql.Data.MySqlClient;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.DAL
{
    /// <summary>
    /// Repository (DAL layer) for Inventory Control module.
    /// All methods use parameterised queries via DatabaseHelper.
    ///
    /// Actual schema (see Database/schema.sql):
    ///   Product       (ItemID, SalesPrice, Category)
    ///   Item          (ItemID, ItemName, ItemDescription)
    ///   WarehouseItem (WarehouseItemID, ItemID, WarehouseID,
    ///                  WarehouseItemQuantity, ReorderLevel)
    ///   RawMaterial   (ItemID [FK Item], purchasePrice, MaterialType)
    /// </summary>
    public class InventoryControlRepo
    {
        // ════════════════════════════════════════════════════════════════
        //  PRODUCT queries
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns all products with aggregated stock info across all warehouses.
        /// StockQty   = SUM(WarehouseItemQuantity) per ItemID (NULL → 0)
        /// ReorderLevel = MIN(ReorderLevel) from WarehouseItem (NULL → 0)
        /// </summary>
        public List<ProductEntity> SearchProducts(
            string keyword  = null,
            string category = null)
        {
            var list = new List<ProductEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                // WarehouseItem stores stock per warehouse; aggregate for total.
                var sql =
                    @"SELECT  p.ItemID,
                             i.ItemName,
                             p.Category,
                             p.SalesPrice,
                             COALESCE(SUM(wi.WarehouseItemQuantity), 0) AS StockQty,
                             COALESCE(MIN(wi.ReorderLevel), 0)          AS ReorderLevel
                      FROM   Product p
                      JOIN   Item i         ON p.ItemID = i.ItemID
                      LEFT JOIN WarehouseItem wi ON wi.ItemID = p.ItemID
                      WHERE  1=1";

                if (!string.IsNullOrEmpty(keyword))
                    sql += @" AND (p.ItemID   LIKE @kw
                               OR i.ItemName LIKE @kw
                               OR p.Category LIKE @kw)";

                if (!string.IsNullOrEmpty(category) && category != "All")
                    sql += " AND p.Category = @category";

                sql += " GROUP BY p.ItemID, i.ItemName, p.Category, p.SalesPrice";
                sql += " ORDER BY i.ItemName";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(keyword))
                        cmd.Parameters.AddWithValue("@kw", "%" + keyword + "%");
                    if (!string.IsNullOrEmpty(category) && category != "All")
                        cmd.Parameters.AddWithValue("@category", category);

                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read()) list.Add(MapProduct(rdr));
                }
            }
            return list;
        }

        /// <summary>Returns all products.</summary>
        public List<ProductEntity> GetAllProducts() => SearchProducts();

        /// <summary>
        /// Returns distinct product categories for the filter ComboBox.
        /// </summary>
        public List<string> GetProductCategories()
        {
            var list = new List<string> { "All" };
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    "SELECT DISTINCT Category FROM Product ORDER BY Category";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                    {
                        string cat = rdr.IsDBNull(0) ? null : rdr.GetString(0);
                        if (!string.IsNullOrEmpty(cat)) list.Add(cat);
                    }
            }
            return list;
        }

        // ════════════════════════════════════════════════════════════════
        //  RAW MATERIAL queries
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns all raw materials filtered by optional keyword and category.
        /// Schema: RawMaterial (ItemID, purchasePrice, MaterialType) + Item (ItemID, ItemName)
        ///         WarehouseItem provides stock quantities (same pattern as Product).
        /// </summary>
        public List<RawMaterialEntity> SearchRawMaterials(
            string keyword  = null,
            string category = null)
        {
            var list = new List<RawMaterialEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var sql =
                    @"SELECT  rm.ItemID          AS MaterialID,
                             i.ItemName          AS MaterialName,
                             rm.MaterialType     AS Category,
                             rm.purchasePrice    AS UnitCost,
                             COALESCE(SUM(wi.WarehouseItemQuantity), 0) AS StockQty,
                             COALESCE(MIN(wi.ReorderLevel), 0)          AS ReorderLevel
                      FROM   RawMaterial rm
                      JOIN   Item i          ON rm.ItemID = i.ItemID
                      LEFT JOIN WarehouseItem wi ON wi.ItemID = rm.ItemID
                      WHERE  1=1";

                if (!string.IsNullOrEmpty(keyword))
                    sql += @" AND (rm.ItemID      LIKE @kw
                               OR i.ItemName     LIKE @kw
                               OR rm.MaterialType LIKE @kw)";

                if (!string.IsNullOrEmpty(category) && category != "All")
                    sql += " AND rm.MaterialType = @category";

                sql += " GROUP BY rm.ItemID, i.ItemName, rm.MaterialType, rm.purchasePrice";
                sql += " ORDER BY i.ItemName";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(keyword))
                        cmd.Parameters.AddWithValue("@kw", "%" + keyword + "%");
                    if (!string.IsNullOrEmpty(category) && category != "All")
                        cmd.Parameters.AddWithValue("@category", category);

                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read()) list.Add(MapRawMaterial(rdr));
                }
            }
            return list;
        }

        /// <summary>Returns all raw materials.</summary>
        public List<RawMaterialEntity> GetAllRawMaterials() => SearchRawMaterials();

        /// <summary>
        /// Returns distinct raw material types (MaterialType) for the filter ComboBox.
        /// </summary>
        public List<string> GetRawMaterialCategories()
        {
            var list = new List<string> { "All" };
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    "SELECT DISTINCT MaterialType FROM RawMaterial ORDER BY MaterialType";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                    {
                        string cat = rdr.IsDBNull(0) ? null : rdr.GetString(0);
                        if (!string.IsNullOrEmpty(cat)) list.Add(cat);
                    }
            }
            return list;
        }

        // ════════════════════════════════════════════════════════════════
        //  PRIVATE MAPPERS
        // ════════════════════════════════════════════════════════════════

        private static ProductEntity MapProduct(MySqlDataReader rdr)
        {
            return new ProductEntity
            {
                ItemID       = rdr.GetString("ItemID"),
                ItemName     = rdr.GetString("ItemName"),
                Category     = rdr.IsDBNull(rdr.GetOrdinal("Category"))     ? "" : rdr.GetString("Category"),
                SalesPrice   = Convert.ToDouble(rdr["SalesPrice"]),
                StockQty     = rdr.IsDBNull(rdr.GetOrdinal("StockQty"))     ? 0  : Convert.ToInt32(rdr["StockQty"]),
                ReorderLevel = rdr.IsDBNull(rdr.GetOrdinal("ReorderLevel")) ? 0  : Convert.ToInt32(rdr["ReorderLevel"])
            };
        }

        private static RawMaterialEntity MapRawMaterial(MySqlDataReader rdr)
        {
            return new RawMaterialEntity
            {
                MaterialID   = rdr.GetString("MaterialID"),
                MaterialName = rdr.GetString("MaterialName"),
                Category     = rdr.IsDBNull(rdr.GetOrdinal("Category"))     ? "" : rdr.GetString("Category"),
                Unit         = "",   // not stored in schema; kept for ViewModel compatibility
                UnitCost     = Convert.ToDouble(rdr["UnitCost"]),
                StockQty     = rdr.IsDBNull(rdr.GetOrdinal("StockQty"))     ? 0  : Convert.ToInt32(rdr["StockQty"]),
                ReorderLevel = rdr.IsDBNull(rdr.GetOrdinal("ReorderLevel")) ? 0  : Convert.ToInt32(rdr["ReorderLevel"])
            };
        }
    }
}
