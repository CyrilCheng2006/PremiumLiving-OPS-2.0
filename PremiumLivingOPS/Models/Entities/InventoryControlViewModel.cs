using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.Entities
{
    // ════════════════════════════════════════════════════════════════
    //  INVENTORY CONTROL — Domain Entities
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Maps to Product JOIN Item JOIN WarehouseItem aggregation.
    /// </summary>
    public class ProductEntity
    {
        public string ItemID          { get; set; }
        public string ItemName        { get; set; }
        public string ItemDescription { get; set; }
        public string Category        { get; set; }
        public double SalesPrice      { get; set; }
        public int    StockQty        { get; set; }
        public int    ReorderLevel    { get; set; }

        public string StockStatus
        {
            get
            {
                if (StockQty == 0)              return "Out of Stock";
                if (StockQty <= ReorderLevel)   return "Low Stock";
                return "In Stock";
            }
        }
    }

    /// <summary>
    /// Maps to RawMaterial JOIN Item JOIN WarehouseItem aggregation.
    /// </summary>
    public class RawMaterialEntity
    {
        public string MaterialID      { get; set; }
        public string MaterialName    { get; set; }
        public string ItemDescription { get; set; }
        public string Category        { get; set; }  // MaterialType
        public string Unit            { get; set; }
        public double UnitCost        { get; set; }  // purchasePrice
        public int    StockQty        { get; set; }
        public int    ReorderLevel    { get; set; }

        public string StockStatus
        {
            get
            {
                if (StockQty == 0)              return "Out of Stock";
                if (StockQty <= ReorderLevel)   return "Low Stock";
                return "In Stock";
            }
        }
    }

    /// <summary>One WarehouseItem row — used by Inward / Transfer dialogs.</summary>
    public class WarehouseItemEntity
    {
        public string WarehouseItemID { get; set; }
        public string ItemID          { get; set; }
        public string ItemName        { get; set; }
        public string WarehouseID     { get; set; }
        public string WarehouseName   { get; set; }  // WarehouseLocation for display
        public int    Quantity        { get; set; }
        public int    ReorderLevel    { get; set; }
    }

    /// <summary>Warehouse lookup row.</summary>
    public class WarehouseEntity
    {
        public string WarehouseID       { get; set; }
        public string WarehouseLocation { get; set; }
        public string ManagerID         { get; set; }
        public string ContactNumber     { get; set; }
        public int    Capacity          { get; set; }
    }

    // ════════════════════════════════════════════════════════════════
    //  INVENTORY CONTROL — ViewModels (Controller → View)
    // ════════════════════════════════════════════════════════════════

    public class ViewProductViewModel
    {
        public UserBarViewModel    UserBar      { get; set; }
        public string[]            AllowedMenus { get; set; }
        public List<ProductEntity> Products     { get; set; }
    }

    public class ViewRawMaterialViewModel
    {
        public UserBarViewModel        UserBar      { get; set; }
        public string[]                AllowedMenus { get; set; }
        public List<RawMaterialEntity> Materials    { get; set; }
    }

    // ── Add / Modify Product ─────────────────────────────────────────

    public class AddProductViewModel
    {
        public UserBarViewModel  UserBar      { get; set; }
        public string[]          AllowedMenus { get; set; }
        public List<string>      Categories   { get; set; }  // ENUM values
        public List<WarehouseEntity> Warehouses { get; set; }
    }

    public class ModifyProductViewModel
    {
        public UserBarViewModel  UserBar      { get; set; }
        public string[]          AllowedMenus { get; set; }
        public ProductEntity     Product      { get; set; }
        public List<WarehouseItemEntity> WarehouseBreakdown { get; set; }
        public List<WarehouseEntity>     Warehouses         { get; set; }
    }

    // ── Add / Modify Raw Material ─────────────────────────────────────

    public class AddRawMaterialViewModel
    {
        public UserBarViewModel  UserBar      { get; set; }
        public string[]          AllowedMenus { get; set; }
        public List<string>      Categories   { get; set; }  // MaterialType ENUM
        public List<WarehouseEntity> Warehouses { get; set; }
    }

    public class ModifyRawMaterialViewModel
    {
        public UserBarViewModel  UserBar      { get; set; }
        public string[]          AllowedMenus { get; set; }
        public RawMaterialEntity Material     { get; set; }
        public List<WarehouseItemEntity> WarehouseBreakdown { get; set; }
        public List<WarehouseEntity>     Warehouses         { get; set; }
    }

    // ── Inward Goods ─────────────────────────────────────────────────

    public class InwardGoodsViewModel
    {
        public UserBarViewModel  UserBar      { get; set; }
        public string[]          AllowedMenus { get; set; }
        public List<WarehouseEntity> Warehouses { get; set; }
        // Items dropdown: all products + all raw materials labelled
        public List<ItemLookup>  Items        { get; set; }
    }

    public class ItemLookup
    {
        public string ItemID   { get; set; }
        public string ItemName { get; set; }
        public string ItemType { get; set; }  // "Product" or "Raw Material"
        public override string ToString() => $"[{ItemType}] {ItemName} ({ItemID})";
    }

    // ── Warehouse Transfer ────────────────────────────────────────────

    public class WarehouseTransferViewModel
    {
        public UserBarViewModel  UserBar      { get; set; }
        public string[]          AllowedMenus { get; set; }
        public List<WarehouseEntity>     Warehouses     { get; set; }
        public List<WarehouseItemEntity> WarehouseItems { get; set; }
        public string NextTransferID { get; set; }
    }
}
