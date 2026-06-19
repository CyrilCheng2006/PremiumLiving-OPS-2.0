using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;
using System.Collections.Generic;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Controller (middle layer) for Inventory Control module.
    /// The View never touches the Repo or DB directly.
    /// </summary>
    public class InventoryControlController
    {
        private readonly InventoryControlRepo _repo = new InventoryControlRepo();

        // ════════════════════════════════════════════════════════════════
        //  PRODUCT — read
        // ════════════════════════════════════════════════════════════════

        public ViewProductViewModel GetViewProductVM(string keyword = null, string category = null)
        {
            var user = SessionManager.CurrentUser;
            return new ViewProductViewModel
            {
                UserBar      = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department),
                Products     = _repo.SearchProducts(keyword, category)
            };
        }

        public List<string> GetProductCategories() => _repo.GetProductCategories();

        public ModifyProductViewModel GetModifyProductVM(string itemId)
        {
            var user = SessionManager.CurrentUser;
            return new ModifyProductViewModel
            {
                UserBar            = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus       = NavAccessPolicy.GetAllowedMenus(user?.Department),
                Product            = _repo.GetProductById(itemId),
                WarehouseBreakdown = _repo.GetWarehouseItemsByItemId(itemId),
                Warehouses         = _repo.GetAllWarehouses()
            };
        }

        public AddProductViewModel GetAddProductVM()
        {
            var user = SessionManager.CurrentUser;
            return new AddProductViewModel
            {
                UserBar      = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department),
                Categories   = new System.Collections.Generic.List<string> { "Sofa", "Bed", "Table", "Chair", "Cabinet" },
                Warehouses   = _repo.GetAllWarehouses()
            };
        }

        // ════════════════════════════════════════════════════════════════
        //  PRODUCT — write
        // ════════════════════════════════════════════════════════════════

        public void SubmitAddProduct(string itemId, string itemName, string itemDesc,
                                     string category, double salesPrice,
                                     string warehouseId, int initialQty, int reorderLevel)
            => _repo.AddProduct(itemId, itemName, itemDesc, category, salesPrice, warehouseId, initialQty, reorderLevel);

        public void SubmitUpdateProduct(string itemId, string itemName, string itemDesc,
                                        string category, double salesPrice)
            => _repo.UpdateProduct(itemId, itemName, itemDesc, category, salesPrice);

        public void DeleteProduct(string itemId)
            => _repo.DeleteProduct(itemId);

        /// <summary>
        /// Returns the next auto-generated Product Item ID (IID-P-XXXX).
        /// Delegates to the Repo which queries MAX suffix from the DB.
        /// </summary>
        public string GenerateNextProductItemId()
            => _repo.GenerateNextProductItemId();

        // ════════════════════════════════════════════════════════════════
        //  RAW MATERIAL — read
        // ════════════════════════════════════════════════════════════════

        public ViewRawMaterialViewModel GetViewRawMaterialVM(string keyword = null, string category = null)
        {
            var user = SessionManager.CurrentUser;
            return new ViewRawMaterialViewModel
            {
                UserBar      = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department),
                Materials    = _repo.SearchRawMaterials(keyword, category)
            };
        }

        public List<string> GetRawMaterialCategories() => _repo.GetRawMaterialCategories();

        public ModifyRawMaterialViewModel GetModifyRawMaterialVM(string itemId)
        {
            var user = SessionManager.CurrentUser;
            return new ModifyRawMaterialViewModel
            {
                UserBar            = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus       = NavAccessPolicy.GetAllowedMenus(user?.Department),
                Material           = _repo.GetRawMaterialById(itemId),
                WarehouseBreakdown = _repo.GetWarehouseItemsByItemId(itemId),
                Warehouses         = _repo.GetAllWarehouses()
            };
        }

        public AddRawMaterialViewModel GetAddRawMaterialVM()
        {
            var user = SessionManager.CurrentUser;
            return new AddRawMaterialViewModel
            {
                UserBar      = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department),
                Categories   = new System.Collections.Generic.List<string> { "Wood", "Metal", "Fabric", "Foam", "Glass", "Paint" },
                Warehouses   = _repo.GetAllWarehouses()
            };
        }

        // ════════════════════════════════════════════════════════════════
        //  RAW MATERIAL — write
        // ════════════════════════════════════════════════════════════════

        public void SubmitAddRawMaterial(string itemId, string itemName, string itemDesc,
                                         string materialType, double purchasePrice,
                                         string warehouseId, int initialQty, int reorderLevel)
            => _repo.AddRawMaterial(itemId, itemName, itemDesc, materialType, purchasePrice, warehouseId, initialQty, reorderLevel);

        public void SubmitUpdateRawMaterial(string itemId, string itemName, string itemDesc,
                                            string materialType, double purchasePrice)
            => _repo.UpdateRawMaterial(itemId, itemName, itemDesc, materialType, purchasePrice);

        public void DeleteRawMaterial(string itemId)
            => _repo.DeleteRawMaterial(itemId);

        // ════════════════════════════════════════════════════════════════
        //  INWARD GOODS
        // ════════════════════════════════════════════════════════════════

        public InwardGoodsViewModel GetInwardGoodsVM()
        {
            var user = SessionManager.CurrentUser;
            return new InwardGoodsViewModel
            {
                UserBar      = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department),
                Warehouses   = _repo.GetAllWarehouses(),
                Items        = _repo.GetAllItemsLookup()
            };
        }

        public void SubmitInwardGoods(string itemId, string warehouseId, int qty)
            => _repo.RecordInwardGoods(itemId, warehouseId, qty);

        // ════════════════════════════════════════════════════════════════
        //  WAREHOUSE TRANSFER
        // ════════════════════════════════════════════════════════════════

        public WarehouseTransferViewModel GetWarehouseTransferVM()
        {
            var user = SessionManager.CurrentUser;
            return new WarehouseTransferViewModel
            {
                UserBar         = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus    = NavAccessPolicy.GetAllowedMenus(user?.Department),
                Warehouses      = _repo.GetAllWarehouses(),
                WarehouseItems  = _repo.GetAllWarehouseItems(),
                NextTransferID  = _repo.GenerateNextTransferId()
            };
        }

        public void SubmitWarehouseTransfer(string transferId, string fromWarehouseItemId,
                                            string toWarehouseId, int qty)
            => _repo.RecordWarehouseTransfer(transferId, fromWarehouseItemId, toWarehouseId, qty);

        // Warehouse list used by dialogs
        public List<WarehouseEntity> GetAllWarehouses() => _repo.GetAllWarehouses();

        public List<WarehouseItemEntity> GetWarehouseItemsByItem(string itemId)
            => _repo.GetWarehouseItemsByItemId(itemId);
    }
}
