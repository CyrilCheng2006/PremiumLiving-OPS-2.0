using System;
using System.Collections.Generic;
using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Services;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Controller (MVC middle layer) for Inventory Control.
    /// Covers: Products, Raw Materials, Warehouse Items, Inward Goods, Warehouse Transfer.
    /// All DB-write operations are audit-logged via AuditLogger.
    /// Contains NO UI code.
    /// </summary>
    public class InventoryControlController
    {
        private readonly InventoryControlRepo _repo = new InventoryControlRepo();

        // ── Product ────────────────────────────────────────────────────

        public List<ProductEntity> SearchProducts(string keyword = null)
            => _repo.SearchProducts(keyword);

        public ProductEntity GetProductById(string itemId)
            => _repo.GetProductById(itemId);

        public bool AddProduct(ProductEntity product)
        {
            bool ok = _repo.InsertProduct(product);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_CREATE, "Product",
                    oldValue: null,
                    newValue: AuditLogger.Snapshot(
                        ("ID",       product.ItemID),
                        ("Name",     product.ItemName),
                        ("Category", product.Category ?? ""),
                        ("Price",    product.SalesPrice.ToString("F2"))));
            return ok;
        }

        public bool UpdateProduct(ProductEntity product)
        {
            var old = _repo.GetProductById(product.ItemID);
            string oldSnap = old == null ? product.ItemID
                : AuditLogger.Snapshot(
                    ("ID",   old.ItemID),
                    ("Name", old.ItemName),
                    ("Cat",  old.Category ?? ""));

            bool ok = _repo.UpdateProduct(product);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_EDIT, "Product",
                    oldValue: oldSnap,
                    newValue: AuditLogger.Snapshot(
                        ("ID",   product.ItemID),
                        ("Name", product.ItemName),
                        ("Cat",  product.Category ?? "")));
            return ok;
        }

        public bool DeleteProduct(string itemId)
        {
            var old = _repo.GetProductById(itemId);
            string oldSnap = old == null ? itemId
                : AuditLogger.Snapshot(("ID", old.ItemID), ("Name", old.ItemName));

            bool ok = _repo.DeleteProduct(itemId);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_DELETE, "Product",
                    oldValue: oldSnap, newValue: null);
            return ok;
        }

        // ── Raw Material ───────────────────────────────────────────────

        public List<RawMaterialEntity> SearchRawMaterials(string keyword = null)
            => _repo.SearchRawMaterials(keyword);

        public RawMaterialEntity GetRawMaterialById(string itemId)
            => _repo.GetRawMaterialById(itemId);

        public bool AddRawMaterial(RawMaterialEntity material)
        {
            bool ok = _repo.InsertRawMaterial(material);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_CREATE, "RawMaterial",
                    oldValue: null,
                    newValue: AuditLogger.Snapshot(
                        ("ID",   material.MaterialID),
                        ("Name", material.MaterialName),
                        ("Type", material.Category ?? "")));
            return ok;
        }

        public bool UpdateRawMaterial(RawMaterialEntity material)
        {
            var old = _repo.GetRawMaterialById(material.MaterialID);
            string oldSnap = old == null ? material.MaterialID
                : AuditLogger.Snapshot(
                    ("ID",   old.MaterialID),
                    ("Name", old.MaterialName),
                    ("Type", old.Category ?? ""));

            bool ok = _repo.UpdateRawMaterial(material);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_EDIT, "RawMaterial",
                    oldValue: oldSnap,
                    newValue: AuditLogger.Snapshot(
                        ("ID",   material.MaterialID),
                        ("Name", material.MaterialName),
                        ("Type", material.Category ?? "")));
            return ok;
        }

        public bool DeleteRawMaterial(string itemId)
        {
            var old = _repo.GetRawMaterialById(itemId);
            string oldSnap = old == null ? itemId
                : AuditLogger.Snapshot(("ID", old.MaterialID), ("Name", old.MaterialName));

            bool ok = _repo.DeleteRawMaterial(itemId);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_DELETE, "RawMaterial",
                    oldValue: oldSnap, newValue: null);
            return ok;
        }

        // ── Warehouse ─────────────────────────────────────────────────

        public List<WarehouseEntity> GetAllWarehouses() => _repo.GetAllWarehouses();

        // ── Warehouse Item ──────────────────────────────────────────────

        public List<WarehouseItemEntity> GetWarehouseItems(string itemId = null, string warehouseId = null)
            => _repo.GetWarehouseItems(itemId, warehouseId);

        /// <summary>Inward Goods: adds stock to a WarehouseItem and logs EDIT.</summary>
        public bool InwardGoods(string warehouseItemId, int qty)
        {
            bool ok = _repo.AddStock(warehouseItemId, qty);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_EDIT, "WarehouseItem",
                    oldValue: AuditLogger.Snapshot(("WI", warehouseItemId)),
                    newValue: AuditLogger.Snapshot(
                        ("WI",    warehouseItemId),
                        ("Delta", "+" + qty)));
            return ok;
        }

        /// <summary>Warehouse Transfer: moves qty between two WarehouseItems and logs EDIT.</summary>
        public bool TransferStock(string fromWarehouseItemId, string toWarehouseItemId, int qty)
        {
            bool ok = _repo.TransferStock(fromWarehouseItemId, toWarehouseItemId, qty);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_EDIT, "WarehouseItem",
                    oldValue: AuditLogger.Snapshot(
                        ("From", fromWarehouseItemId),
                        ("To",   toWarehouseItemId)),
                    newValue: AuditLogger.Snapshot(
                        ("From",  fromWarehouseItemId),
                        ("To",    toWarehouseItemId),
                        ("Qty",   qty.ToString())));
            return ok;
        }

        // ── Lookup helpers (no logging needed) ────────────────────────

        public List<ItemLookup> GetItemLookups() => _repo.GetItemLookups();
    }
}
