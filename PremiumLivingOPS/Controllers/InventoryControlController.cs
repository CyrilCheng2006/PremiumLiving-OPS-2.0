using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;
using System.Collections.Generic;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Controller (middle layer) for Inventory Control module.
    /// Receives raw requests from the View, orchestrates the Repo (DAL),
    /// and returns fully-populated ViewModels.
    /// The View never touches the Repo or DB directly.
    /// </summary>
    public class InventoryControlController
    {
        private readonly InventoryControlRepo _repo = new InventoryControlRepo();

        // ════════════════════════════════════════════════════════════════
        //  PRODUCT
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns a fully-populated ViewProductViewModel.
        /// Applies optional keyword and category filters.
        /// </summary>
        public ViewProductViewModel GetViewProductVM(
            string keyword  = null,
            string category = null)
        {
            // SessionManager.CurrentUser mirrors OrderProcessingController usage
            var user = SessionManager.CurrentUser;

            return new ViewProductViewModel
            {
                UserBar = new UserBarViewModel
                {
                    DisplayName = user?.StaffName ?? "Unknown",
                    Department  = user?.Department ?? ""
                },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department),
                Products     = _repo.SearchProducts(keyword, category)
            };
        }

        /// <summary>
        /// Returns distinct product categories for the Category filter ComboBox.
        /// Includes "All" as the first item.
        /// </summary>
        public List<string> GetProductCategories()
            => _repo.GetProductCategories();

        // ════════════════════════════════════════════════════════════════
        //  RAW MATERIAL
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns a fully-populated ViewRawMaterialViewModel.
        /// Applies optional keyword and category filters.
        /// </summary>
        public ViewRawMaterialViewModel GetViewRawMaterialVM(
            string keyword  = null,
            string category = null)
        {
            var user = SessionManager.CurrentUser;

            return new ViewRawMaterialViewModel
            {
                UserBar = new UserBarViewModel
                {
                    DisplayName = user?.StaffName ?? "Unknown",
                    Department  = user?.Department ?? ""
                },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department),
                Materials    = _repo.SearchRawMaterials(keyword, category)
            };
        }

        /// <summary>
        /// Returns distinct raw material categories for the Category filter ComboBox.
        /// Includes "All" as the first item.
        /// </summary>
        public List<string> GetRawMaterialCategories()
            => _repo.GetRawMaterialCategories();
    }
}
