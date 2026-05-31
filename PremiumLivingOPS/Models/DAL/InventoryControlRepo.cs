using MySql.Data.MySqlClient;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.DAL
{
    /// <summary>
    /// Repository (DAL layer) for Inventory Control module.
    /// All methods use parameterised queries via DatabaseHelper.
    /// </summary>
    public class InventoryControlRepo
    {
        // ════════════════════════════════════════════════════════════════
        //  PRODUCT queries
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns all products with stock info, filtered by optional keyword and category.
        /// Schema join: Product (ItemID, SalesPrice, Category, StockQty, ReorderLevel)
        ///              Item    (ItemID, ItemName)
        /// </summary>
        public List<ProductEntity> SearchProducts(
            string keyword  = null,
            string category = null)
        {
            var list = new List<ProductEntity>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var sql =
                    @"SELECT p.ItemID, i.ItemName, p.Category,
                             p.SalesPrice, p.StockQty, p.ReorderLevel
                      FROM Product p
                      JOIN Item i ON p.ItemID = i.ItemID
                      WHERE 1=1";

                if (!string.IsNullOrEmpty(keyword))
                    sql += @" AND (p.ItemID   LIKE @kw
                               OR i.ItemName LIKE @kw
                               OR p.Category LIKE @kw)";

                if (!string.IsNullOrEmpty(category) && category != "All")
                    sql += " AND p.Category = @category";

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
        /// Schema: RawMaterial (MaterialID, MaterialName, Category, Unit, UnitCost, StockQty, ReorderLevel)
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
                    @"SELECT MaterialID, MaterialName, Category, Unit,
                             UnitCost, StockQty, ReorderLevel
                      FROM RawMaterial
                      WHERE 1=1";

                if (!string.IsNullOrEmpty(keyword))
                    sql += @" AND (MaterialID   LIKE @kw
                               OR MaterialName LIKE @kw
                               OR Category     LIKE @kw)";

                if (!string.IsNullOrEmpty(category) && category != "All")
                    sql += " AND Category = @category";

                sql += " ORDER BY MaterialName";

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
        /// Returns distinct raw material categories for the filter ComboBox.
        /// </summary>
        public List<string> GetRawMaterialCategories()
        {
            var list = new List<string> { "All" };
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql =
                    "SELECT DISTINCT Category FROM RawMaterial ORDER BY Category";
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
                StockQty     = rdr.IsDBNull(rdr.GetOrdinal("StockQty"))     ? 0  : rdr.GetInt32("StockQty"),
                ReorderLevel = rdr.IsDBNull(rdr.GetOrdinal("ReorderLevel")) ? 0  : rdr.GetInt32("ReorderLevel")
            };
        }

        private static RawMaterialEntity MapRawMaterial(MySqlDataReader rdr)
        {
            return new RawMaterialEntity
            {
                MaterialID   = rdr.GetString("MaterialID"),
                MaterialName = rdr.GetString("MaterialName"),
                Category     = rdr.IsDBNull(rdr.GetOrdinal("Category"))     ? "" : rdr.GetString("Category"),
                Unit         = rdr.IsDBNull(rdr.GetOrdinal("Unit"))         ? "" : rdr.GetString("Unit"),
                UnitCost     = Convert.ToDouble(rdr["UnitCost"]),
                StockQty     = rdr.IsDBNull(rdr.GetOrdinal("StockQty"))     ? 0  : rdr.GetInt32("StockQty"),
                ReorderLevel = rdr.IsDBNull(rdr.GetOrdinal("ReorderLevel")) ? 0  : rdr.GetInt32("ReorderLevel")
            };
        }
    }
}
