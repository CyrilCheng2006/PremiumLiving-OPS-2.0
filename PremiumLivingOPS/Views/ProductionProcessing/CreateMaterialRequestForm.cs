using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.ProductionProcessing
{
    /// <summary>
    /// View — Create Raw Material Request.
    ///
    /// MVC role : View only.  All data access goes through ProductionProcessingController.
    /// AppShell  : mandatory chrome (TopNavBar + UserBar).
    /// CardPanel : all content wrapped in 3-layer nested cards.
    ///
    /// Schema coverage:
    ///   MaterialRequest — record being created
    ///   RawMaterial     — lookup (material dropdown)
    ///   WarehouseItem   — lookup (stock location dropdown, filtered per material)
    ///   Order           — lookup (only for OrderDemand trigger type)
    /// </summary>
    public partial class CreateMaterialRequestForm : Form
    {
        private readonly ProductionProcessingController _ctrl = new ProductionProcessingController();

        private List<RawMaterialLookup>   _rawMaterials;
        private List<WarehouseItemLookup> _warehouseItems;
        private List<OrderLookup>         _orders;

        public CreateMaterialRequestForm()
        {
            InitializeComponent();
            this.Load += CreateMaterialRequestForm_Load;
        }

        // ════════════════════════════════════════════════════════════════
        //  Load
        // ════════════════════════════════════════════════════════════════

        private void CreateMaterialRequestForm_Load(object sender, EventArgs e)
        {
            cboRawMaterial.SelectedIndexChanged += CboRawMaterial_Changed;
            cboTrigger.SelectedIndexChanged     += CboTrigger_Changed;
            btnSubmit.Click += BtnSubmit_Click;
            btnReset.Click  += BtnReset_Click;

            LoadForm();
        }

        // ════════════════════════════════════════════════════════════════
        //  Data load
        // ════════════════════════════════════════════════════════════════

        private void LoadForm()
        {
            var vm = _ctrl.GetCreateMaterialRequestVM();

            // AppShell
            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Production Processing  \u203a  Create Raw Material Request");

            // Auto-generated ID
            txtRequestID.Text = vm.NextRequestID;

            // Raw Material dropdown
            _rawMaterials = vm.RawMaterials;
            cboRawMaterial.Items.Clear();
            cboRawMaterial.Items.Add("-- Select Raw Material --");
            foreach (var m in _rawMaterials)
                cboRawMaterial.Items.Add(m);
            cboRawMaterial.SelectedIndex = 0;

            // Warehouse dropdown (empty until material is selected)
            _warehouseItems = new List<WarehouseItemLookup>();
            cboWarehouse.Items.Clear();
            cboWarehouse.Items.Add("-- Select Material First --");
            cboWarehouse.SelectedIndex = 0;
            cboWarehouse.Enabled = false;

            // Order dropdown
            _orders = vm.Orders;
            cboOrder.Items.Clear();
            cboOrder.Items.Add("-- None (Reorder) --");
            foreach (var o in _orders)
                cboOrder.Items.Add(o);
            cboOrder.SelectedIndex = 0;

            // Fields
            txtMaterialType.Text   = string.Empty;
            txtCurrentStock.Text   = string.Empty;
            txtReorderLevel.Text   = string.Empty;

            nudRequestedQty.Minimum = 1;
            nudRequestedQty.Maximum = 99999;
            nudRequestedQty.Value   = 1;

            cboUrgency.SelectedIndex = 0;
            cboTrigger.SelectedIndex = 0;

            // Show/hide order row based on trigger
            RefreshOrderVisibility();
        }

        // ════════════════════════════════════════════════════════════════
        //  Event handlers
        // ════════════════════════════════════════════════════════════════

        private void CboRawMaterial_Changed(object sender, EventArgs e)
        {
            if (cboRawMaterial.SelectedItem is RawMaterialLookup mat)
            {
                txtMaterialType.Text = mat.MaterialType;

                // Reload warehouse items for this material
                _warehouseItems = _ctrl.GetWarehouseItemsForMaterial(mat.ItemID);
                cboWarehouse.Items.Clear();
                cboWarehouse.Items.Add("-- Select Warehouse --");
                foreach (var w in _warehouseItems)
                    cboWarehouse.Items.Add(w);
                cboWarehouse.SelectedIndex = 0;
                cboWarehouse.Enabled = _warehouseItems.Count > 0;

                txtCurrentStock.Text = string.Empty;
                txtReorderLevel.Text = string.Empty;
            }
            else
            {
                txtMaterialType.Text = string.Empty;
                cboWarehouse.Items.Clear();
                cboWarehouse.Items.Add("-- Select Material First --");
                cboWarehouse.SelectedIndex = 0;
                cboWarehouse.Enabled = false;
                txtCurrentStock.Text = string.Empty;
                txtReorderLevel.Text = string.Empty;
            }
        }

        // Update stock/reorder display when warehouse selection changes
        internal void CboWarehouse_Changed(object sender, EventArgs e)
        {
            if (cboWarehouse.SelectedIndex > 0 && cboWarehouse.SelectedIndex <= _warehouseItems.Count)
            {
                var w = _warehouseItems[cboWarehouse.SelectedIndex - 1];
                txtCurrentStock.Text = w.CurrentStock.ToString();
                txtReorderLevel.Text = w.ReorderLevel.ToString();
            }
            else
            {
                txtCurrentStock.Text = string.Empty;
                txtReorderLevel.Text = string.Empty;
            }
        }

        private void CboTrigger_Changed(object sender, EventArgs e)
        {
            RefreshOrderVisibility();
        }

        private void RefreshOrderVisibility()
        {
            bool isOrderDemand = cboTrigger.SelectedItem?.ToString() == "OrderDemand";
            pnlOrderRow.Visible = isOrderDemand;
            if (!isOrderDemand)
                cboOrder.SelectedIndex = 0;
        }

        private void BtnSubmit_Click(object sender, EventArgs e)
        {
            string requestId      = txtRequestID.Text.Trim();
            string rawMaterialId  = (cboRawMaterial.SelectedItem as RawMaterialLookup)?.ItemID;
            string urgency        = cboUrgency.SelectedItem?.ToString() ?? "Medium";
            string trigger        = cboTrigger.SelectedItem?.ToString() ?? "Reorder";
            int    requestedQty   = (int)nudRequestedQty.Value;

            string warehouseItemId = null;
            if (cboWarehouse.SelectedIndex > 0 && cboWarehouse.SelectedIndex <= _warehouseItems.Count)
                warehouseItemId = _warehouseItems[cboWarehouse.SelectedIndex - 1].WarehouseItemID;

            string orderId = null;
            if (trigger == "OrderDemand" &&
                cboOrder.SelectedIndex > 0 && cboOrder.SelectedIndex <= _orders.Count)
                orderId = _orders[cboOrder.SelectedIndex - 1].OrderID;

            try
            {
                _ctrl.SubmitCreateMaterialRequest(
                    requestId, orderId, rawMaterialId,
                    warehouseItemId, requestedQty,
                    urgency, trigger);

                MessageBox.Show(
                    $"Material Request  {requestId}  has been created successfully.",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoadForm();
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message, "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An error occurred while creating the Material Request:\n\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            LoadForm();
        }

        // ════════════════════════════════════════════════════════════════
        //  Navigation / session
        // ════════════════════════════════════════════════════════════════

        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }
    }
}
