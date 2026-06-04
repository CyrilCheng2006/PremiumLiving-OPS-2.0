using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.RawMaterial
{
    /// <summary>
    /// View — Create Procurement (Create Purchase Order).
    ///
    /// MVC role : View only. All data access goes through ProcurementController.
    /// AppShell  : mandatory chrome (TopNavBar + UserBar).
    /// CardPanel : all content wrapped in 3-layer nested cards.
    ///
    /// Schema coverage:
    ///   PurchaseOrder     — header record
    ///   PurchaseOrderLine — one line per order (single-line in this form)
    ///   MaterialRequest   — lookup (unlinked requests only)
    ///   Supplier          — lookup
    ///   Warehouse         — lookup (delivery destination)
    /// </summary>
    public partial class CreateProcurementForm : Form
    {
        private readonly ProcurementController _ctrl = new ProcurementController();

        private List<MaterialRequestLookup> _requests;
        private List<SupplierLookup>        _suppliers;
        private List<WarehouseEntity>       _warehouses;

        public CreateProcurementForm()
        {
            InitializeComponent();
            this.Load += CreateProcurementForm_Load;
        }

        // ════════════════════════════════════════════════════════════════
        //  Load
        // ════════════════════════════════════════════════════════════════

        private void CreateProcurementForm_Load(object sender, EventArgs e)
        {
            nudOrderQty.ValueChanged   += RecalcTotal;
            nudUnitPrice.ValueChanged  += RecalcTotal;
            cboMaterialRequest.SelectedIndexChanged += CboMaterialRequest_Changed;
            btnSubmit.Click += BtnSubmit_Click;
            btnReset.Click  += BtnReset_Click;

            LoadForm();
        }

        // ════════════════════════════════════════════════════════════════
        //  Data load
        // ════════════════════════════════════════════════════════════════

        private void LoadForm()
        {
            var vm = _ctrl.GetCreateProcurementVM();

            // AppShell
            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Raw Material  \u203a  Create Procurement");

            // Auto-generated ID
            txtPurchaseID.Text = vm.NextPurchaseID;

            // Material Request dropdown
            _requests = vm.MaterialRequests;
            cboMaterialRequest.Items.Clear();
            cboMaterialRequest.Items.Add("-- Select Material Request --");
            foreach (var r in _requests)
                cboMaterialRequest.Items.Add(r);
            cboMaterialRequest.SelectedIndex = 0;

            // Supplier dropdown
            _suppliers = vm.Suppliers;
            cboSupplier.Items.Clear();
            cboSupplier.Items.Add("-- Select Supplier --");
            foreach (var s in _suppliers)
                cboSupplier.Items.Add(s);
            cboSupplier.SelectedIndex = 0;

            // Warehouse dropdown
            _warehouses = vm.Warehouses;
            cboWarehouse.Items.Clear();
            cboWarehouse.Items.Add("-- Select Warehouse --");
            foreach (var w in _warehouses)
                cboWarehouse.Items.Add($"{w.WarehouseID}  —  {w.WarehouseLocation}");
            cboWarehouse.SelectedIndex = 0;

            // ── Reset line fields ────────────────────────────────────────
            // IMPORTANT: always set Minimum BEFORE Value to avoid
            // ArgumentOutOfRangeException when the designer has Minimum > 0.

            // Order Qty — valid range 1..9999
            nudOrderQty.Minimum = 1;
            nudOrderQty.Maximum = 9999;
            nudOrderQty.Value   = 1;

            // Unit Price — valid range 0..9,999,999  (0 = blank / not yet entered)
            nudUnitPrice.Minimum       = 0m;
            nudUnitPrice.Maximum       = 9_999_999m;
            nudUnitPrice.DecimalPlaces = 2;
            nudUnitPrice.Value         = 0m;

            txtRawMaterialID.Text  = string.Empty;
            txtRequestedQty.Text   = string.Empty;
            txtLineTotal.Text      = "HK$ 0.00";
            dtpOrderDate.Value     = DateTime.Today;
            cboStatus.SelectedIndex = 0;
        }

        // ════════════════════════════════════════════════════════════════
        //  Event handlers
        // ════════════════════════════════════════════════════════════════

        private void CboMaterialRequest_Changed(object sender, EventArgs e)
        {
            if (cboMaterialRequest.SelectedItem is MaterialRequestLookup req)
            {
                txtRawMaterialID.Text = req.RawMaterialID;
                txtRequestedQty.Text  = req.RequestedQty.ToString();
            }
            else
            {
                txtRawMaterialID.Text = string.Empty;
                txtRequestedQty.Text  = string.Empty;
            }
        }

        private void RecalcTotal(object sender, EventArgs e)
        {
            double total = (double)nudOrderQty.Value * (double)nudUnitPrice.Value;
            txtLineTotal.Text = $"HK$ {total:N2}";
        }

        private void BtnSubmit_Click(object sender, EventArgs e)
        {
            string purchaseId    = txtPurchaseID.Text.Trim();
            string requestId     = (cboMaterialRequest.SelectedItem as MaterialRequestLookup)?.RequestID;
            string supplierId    = (cboSupplier.SelectedItem as SupplierLookup)?.SupplierID;
            string rawMaterialId = txtRawMaterialID.Text.Trim();
            string status        = cboStatus.SelectedItem?.ToString() ?? "Sent";
            DateTime orderDate   = dtpOrderDate.Value.Date;

            string warehouseId = null;
            if (cboWarehouse.SelectedIndex > 0 && cboWarehouse.SelectedIndex <= _warehouses.Count)
                warehouseId = _warehouses[cboWarehouse.SelectedIndex - 1].WarehouseID;

            int    orderQty  = (int)nudOrderQty.Value;
            double unitPrice = (double)nudUnitPrice.Value;

            try
            {
                _ctrl.SubmitCreateProcurement(
                    purchaseId, requestId, supplierId,
                    orderDate, status,
                    rawMaterialId, warehouseId,
                    orderQty, unitPrice);

                MessageBox.Show(
                    $"Purchase Order  {purchaseId}  has been created successfully.",
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
                    $"An error occurred while creating the Purchase Order:\n\n{ex.Message}",
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
