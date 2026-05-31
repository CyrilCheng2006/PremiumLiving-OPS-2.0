using System.Collections.Generic;

namespace PremiumLivingOPS.Models.Entities
{
    // ════════════════════════════════════════════════════════════════
    //  INVENTORY CONTROL — Domain Entities
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Maps to the Product table joined with Item.
    /// Schema: Product (ItemID, SalesPrice, Category, StockQty, ReorderLevel)
    ///         Item    (ItemID, ItemName)
    /// </summary>
    public class ProductEntity
    {
        public string ItemID       { get; set; }
        public string ItemName     { get; set; }
        public string Category     { get; set; }
        public double SalesPrice   { get; set; }
        public int    StockQty     { get; set; }
        public int    ReorderLevel { get; set; }

        /// <summary>
        /// Derived stock status based on StockQty vs ReorderLevel.
        /// Out of Stock  → StockQty == 0
        /// Low Stock     → 0 < StockQty &lt;= ReorderLevel
        /// In Stock      → StockQty > ReorderLevel
        /// </summary>
        public string StockStatus
        {
            get
            {
                if (StockQty == 0)                      return "Out of Stock";
                if (StockQty <= ReorderLevel)           return "Low Stock";
                return "In Stock";
            }
        }
    }

    /// <summary>
    /// Maps to the RawMaterial table.
    /// Schema: RawMaterial (MaterialID, MaterialName, Category, Unit, UnitCost, StockQty, ReorderLevel)
    /// </summary>
    public class RawMaterialEntity
    {
        public string MaterialID   { get; set; }
        public string MaterialName { get; set; }
        public string Category     { get; set; }
        public string Unit         { get; set; }
        public double UnitCost     { get; set; }
        public int    StockQty     { get; set; }
        public int    ReorderLevel { get; set; }

        /// <summary>Derived stock status (same logic as ProductEntity.StockStatus).</summary>
        public string StockStatus
        {
            get
            {
                if (StockQty == 0)            return "Out of Stock";
                if (StockQty <= ReorderLevel) return "Low Stock";
                return "In Stock";
            }
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  INVENTORY CONTROL — ViewModels (Controller → View)
    // ════════════════════════════════════════════════════════════════

    /// <summary>ViewModel for the View Product tab.</summary>
    public class ViewProductViewModel
    {
        public UserBarViewModel      UserBar      { get; set; }
        public string[]              AllowedMenus { get; set; }
        public List<ProductEntity>   Products     { get; set; }
    }

    /// <summary>ViewModel for the View Raw Material tab.</summary>
    public class ViewRawMaterialViewModel
    {
        public UserBarViewModel         UserBar      { get; set; }
        public string[]                 AllowedMenus { get; set; }
        public List<RawMaterialEntity>  Materials    { get; set; }
    }
}
