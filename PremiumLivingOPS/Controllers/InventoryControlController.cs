using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Services;
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Controller (MVC middle layer) for Inventory Control.
    /// All DB-write operations are audit-logged via AuditLogger.
    /// </summary>
    public class InventoryControlController
    {
        private readonly InventoryControlRepo _repo = new InventoryControlRepo();

        // ═══════════════════════════════════════════════════════════════
        //  WAREHOUSE
        // ═══════════════════════════════════════════════════════════════

        public WarehouseListViewModel GetWarehouseListVM(string keyword = null)
        {
            var user = SessionManager.CurrentUser;
            return new WarehouseListViewModel
            {
                UserBar      = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                Warehouses   = _repo.SearchWarehouses(keyword)
            };
        }

        public string GetNextWarehouseId() => _repo.GetNextWarehouseId();

        public bool AddWarehouse(WarehouseEntity wh)
        {
            bool ok = _repo.InsertWarehouse(wh);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_CREATE, "Warehouse",
                    oldValue: null,
                    newValue: AuditLogger.Snapshot(
                        ("ID",       wh.WarehouseID),
                        ("Name",     wh.WarehouseName),
                        ("Location", wh.Location ?? ""),
                        ("Capacity", wh.Capacity.ToString())));
            return ok;
        }

        public bool UpdateWarehouse(WarehouseEntity wh)
        {
            var old = _repo.GetWarehouseById(wh.WarehouseID);
            string oldSnap = old == null ? wh.WarehouseID
                : AuditLogger.Snapshot(
                    ("ID",       old.WarehouseID),
                    ("Name",     old.WarehouseName),
                    ("Location", old.Location ?? ""),
                    ("Capacity", old.Capacity.ToString()));

            bool ok = _repo.UpdateWarehouse(wh);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_EDIT, "Warehouse",
                    oldValue: oldSnap,
                    newValue: AuditLogger.Snapshot(
                        ("ID",       wh.WarehouseID),
                        ("Name",     wh.WarehouseName),
                        ("Location", wh.Location ?? ""),
                        ("Capacity", wh.Capacity.ToString())));
            return ok;
        }

        public bool DeleteWarehouse(string warehouseId)
        {
            var old = _repo.GetWarehouseById(warehouseId);
            string oldSnap = old == null ? warehouseId
                : AuditLogger.Snapshot(
                    ("ID",   old.WarehouseID),
                    ("Name", old.WarehouseName));

            bool ok = _repo.DeleteWarehouse(warehouseId);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_DELETE, "Warehouse",
                    oldValue: oldSnap,
                    newValue: null);
            return ok;
        }

        // ═══════════════════════════════════════════════════════════════
        //  INVENTORY ITEM / STOCK
        // ═══════════════════════════════════════════════════════════════

        public InventoryListViewModel GetInventoryListVM(string keyword = null)
        {
            var user = SessionManager.CurrentUser;
            return new InventoryListViewModel
            {
                UserBar      = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                Items        = _repo.SearchInventoryItems(keyword)
            };
        }

        public string GetNextItemId() => _repo.GetNextItemId();

        public bool AddItem(InventoryItemEntity item)
        {
            bool ok = _repo.InsertItem(item);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_CREATE, "InventoryItem",
                    oldValue: null,
                    newValue: AuditLogger.Snapshot(
                        ("ID",       item.ItemID),
                        ("Name",     item.ItemName),
                        ("Type",     item.ItemType ?? ""),
                        ("Unit",     item.Unit ?? ""),
                        ("ReorderPt",item.ReorderPoint.ToString())));
            return ok;
        }

        public bool UpdateItem(InventoryItemEntity item)
        {
            var old = _repo.GetItemById(item.ItemID);
            string oldSnap = old == null ? item.ItemID
                : AuditLogger.Snapshot(
                    ("ID",   old.ItemID),
                    ("Name", old.ItemName),
                    ("Type", old.ItemType ?? ""));

            bool ok = _repo.UpdateItem(item);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_EDIT, "InventoryItem",
                    oldValue: oldSnap,
                    newValue: AuditLogger.Snapshot(
                        ("ID",   item.ItemID),
                        ("Name", item.ItemName),
                        ("Type", item.ItemType ?? "")));
            return ok;
        }

        public bool DeleteItem(string itemId)
        {
            var old = _repo.GetItemById(itemId);
            string oldSnap = old == null ? itemId
                : AuditLogger.Snapshot(("ID", old.ItemID), ("Name", old.ItemName));

            bool ok = _repo.DeleteItem(itemId);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_DELETE, "InventoryItem",
                    oldValue: oldSnap,
                    newValue: null);
            return ok;
        }

        // ── Stock Adjustment ───────────────────────────────────────────

        public bool AdjustStock(string itemId, string warehouseId, int qtyDelta, string reason)
        {
            bool ok = _repo.AdjustStock(itemId, warehouseId, qtyDelta);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_EDIT, "Stock",
                    oldValue: AuditLogger.Snapshot(("Item", itemId), ("WH", warehouseId)),
                    newValue: AuditLogger.Snapshot(
                        ("Item",   itemId),
                        ("WH",     warehouseId),
                        ("Delta",  qtyDelta.ToString()),
                        ("Reason", reason ?? "")));
            return ok;
        }

        // ── Read helpers (no logging needed) ───────────────────────────

        public List<WarehouseEntity>      GetAllWarehouses()             => _repo.GetAllWarehouses();
        public List<InventoryItemEntity>  SearchInventoryItems(string kw) => _repo.SearchInventoryItems(kw);
        public WarehouseEntity            GetWarehouseById(string id)    => _repo.GetWarehouseById(id);
        public InventoryItemEntity        GetItemById(string id)         => _repo.GetItemById(id);

        public StockListViewModel GetStockListVM(string keyword = null)
        {
            var user = SessionManager.CurrentUser;
            return new StockListViewModel
            {
                UserBar      = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                Stocks       = _repo.SearchStock(keyword)
            };
        }

        public List<MaterialRequestEntity> GetPendingMaterialRequests() => _repo.GetPendingMaterialRequests();

        public bool CreateMaterialRequest(MaterialRequestEntity req)
        {
            bool ok = _repo.InsertMaterialRequest(req);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_CREATE, "MaterialRequest",
                    oldValue: null,
                    newValue: AuditLogger.Snapshot(
                        ("ID",   req.RequestID),
                        ("Item", req.ItemID ?? ""),
                        ("Qty",  req.RequestedQuantity.ToString()),
                        ("Dept", req.Department ?? "")));
            return ok;
        }

        public bool UpdateMaterialRequestStatus(string reqId, string status)
        {
            bool ok = _repo.UpdateMaterialRequestStatus(reqId, status);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_EDIT, "MaterialRequest",
                    oldValue: AuditLogger.Snapshot(("ID", reqId)),
                    newValue: AuditLogger.Snapshot(("ID", reqId), ("Status", status)));
            return ok;
        }
    }
}
