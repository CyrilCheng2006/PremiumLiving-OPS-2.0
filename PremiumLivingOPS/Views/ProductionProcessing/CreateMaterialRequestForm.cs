using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.ProductionProcessing
{
    /// <summary>
    /// View — Create Raw Material Request (multi-line).
    ///
    /// MVC role : View only.  All data access goes through ProductionProcessingController.
    /// AppShell  : mandatory chrome (TopNavBar + UserBar).
    /// CardPanel : all content wrapped in 3-layer nested cards.
    ///
    /// Schema coverage:
    ///   MaterialRequest — one DB record per staged line (each gets its own RequestID).
    ///   All lines in the same submit batch share a BatchRef label shown in the header
    ///   (UI grouping only — no extra DB column required).
    ///   RawMaterial     — lookup (material dropdown in Add-line picker)
    ///   WarehouseItem   — lookup (stock location dropdown, filtered per material)
    ///   Order           — lookup (only for OrderDemand trigger type)
    /// </summary>
    public partial class CreateMaterialRequestForm : Form
    {
        private readonly ProductionProcessingController _ctrl = new ProductionProcessingController();

        // Lookup data
        private List<RawMaterialLookup>   _rawMaterials   = new List<RawMaterialLookup>();
        private List<WarehouseItemLookup> _warehouseItems = new List<WarehouseItemLookup>();
        private List<OrderLookup>         _orders         = new List<OrderLookup>();

        // Staged request lines
        private readonly List<MaterialRequestLineStaging> _requestLines = new List<MaterialRequestLineStaging>();

        // Batch reference (shared UI label; each line still gets its own DB RequestID)
        private string _batchRef = string.Empty;

        public CreateMaterialRequestForm()
        {
            InitializeComponent();
            this.Load += CreateMaterialRequestForm_Load;
        }

        // ── Load ────────────────────────────────────────────────────────────────────
        private void CreateMaterialRequestForm_Load(object sender, EventArgs e)
        {
            cboRawMaterial.SelectedIndexChanged += CboRawMaterial_Changed;
            cboWarehouse.SelectedIndexChanged   += CboWarehouse_Changed;
            cboTrigger.SelectedIndexChanged     += CboTrigger_Changed;
            btnAddLine.Click    += BtnAddLine_Click;
            btnRemoveLine.Click += BtnRemoveLine_Click;
            btnSubmit.Click     += BtnSubmit_Click;
            btnReset.Click      += BtnReset_Click;

            LoadForm();
        }

        // ── Data load ───────────────────────────────────────────────────────────────
        private void LoadForm()
        {
            var vm = _ctrl.GetCreateMaterialRequestVM();

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Production Processing  \u203a  Create Raw Material Request");

            // Generate a batch reference for this session (first auto-ID from repo)
            _batchRef = vm.NextRequestID;
            lblBatchRef.Text = _batchRef;

            cboUrgency.SelectedIndex = 0;
            cboTrigger.SelectedIndex = 0;

            _rawMaterials = vm.RawMaterials;
            cboRawMaterial.Items.Clear();
            cboRawMaterial.Items.Add("-- Select Raw Material --");
            foreach (var m in _rawMaterials)
                cboRawMaterial.Items.Add(m);
            cboRawMaterial.SelectedIndex = 0;

            _warehouseItems = new List<WarehouseItemLookup>();
            cboWarehouse.Items.Clear();
            cboWarehouse.Items.Add("-- Select Material First --");
            cboWarehouse.SelectedIndex = 0;
            cboWarehouse.Enabled = false;

            txtMaterialType.Text = string.Empty;
            txtCurrentStock.Text = string.Empty;
            txtReorderLevel.Text = string.Empty;
            nudRequestedQty.Value = 1;

            _orders = vm.Orders;
            cboOrder.Items.Clear();
            cboOrder.Items.Add("-- None (Reorder) --");
            foreach (var o in _orders)
                cboOrder.Items.Add(o);
            cboOrder.SelectedIndex = 0;

            _requestLines.Clear();
            RefreshLinesGrid();
            RefreshOrderVisibility();
        }

        // ── Raw Material changed ─────────────────────────────────────────────────
        private void CboRawMaterial_Changed(object sender, EventArgs e)
        {
            if (cboRawMaterial.SelectedItem is RawMaterialLookup mat)
            {
                txtMaterialType.Text = mat.MaterialType;

                _warehouseItems = _ctrl.GetWarehouseItemsForMaterial(mat.ItemID);
                cboWarehouse.Items.Clear();
                cboWarehouse.Items.Add("-- Select Warehouse --");
                foreach (var w in _warehouseItems)
                    cboWarehouse.Items.Add(w);
                cboWarehouse.SelectedIndex = 0;
                cboWarehouse.Enabled = _warehouseItems.Count > 0;
            }
            else
            {
                txtMaterialType.Text = string.Empty;
                cboWarehouse.Items.Clear();
                cboWarehouse.Items.Add("-- Select Material First --");
                cboWarehouse.SelectedIndex = 0;
                cboWarehouse.Enabled = false;
            }
            txtCurrentStock.Text = string.Empty;
            txtReorderLevel.Text = string.Empty;
        }

        // ── Warehouse changed ────────────────────────────────────────────────────
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

        // ── Trigger type changed ─────────────────────────────────────────────────
        private void CboTrigger_Changed(object sender, EventArgs e)
            => RefreshOrderVisibility();

        private void RefreshOrderVisibility()
        {
            bool isOrderDemand = cboTrigger.SelectedItem?.ToString() == "OrderDemand";
            pnlOrderRow.Visible = isOrderDemand;
            if (!isOrderDemand)
                cboOrder.SelectedIndex = 0;
        }

        // ── Add line ─────────────────────────────────────────────────────────────
        private void BtnAddLine_Click(object sender, EventArgs e)
        {
            var mat = cboRawMaterial.SelectedItem as RawMaterialLookup;
            if (mat == null)
            { ShowWarning("Please select a Raw Material."); return; }

            if (cboWarehouse.SelectedIndex < 1 || cboWarehouse.SelectedIndex > _warehouseItems.Count)
            { ShowWarning("Please select a Warehouse / Stock Location."); return; }

            var wh  = _warehouseItems[cboWarehouse.SelectedIndex - 1];
            int qty = (int)nudRequestedQty.Value;

            // Merge duplicate material+warehouse combination
            var existing = _requestLines.FirstOrDefault(
                l => l.RawMaterialItemID == mat.ItemID && l.WarehouseItemID == wh.WarehouseItemID);

            if (existing != null)
                existing.RequestedQty += qty;
            else
                _requestLines.Add(new MaterialRequestLineStaging
                {
                    RawMaterialItemID = mat.ItemID,
                    MaterialName      = mat.ToString(),
                    MaterialType      = mat.MaterialType,
                    WarehouseItemID   = wh.WarehouseItemID,
                    WarehouseDisplay  = wh.ToString(),
                    RequestedQty      = qty
                });

            RefreshLinesGrid();

            // Reset picker for next entry
            cboRawMaterial.SelectedIndex = 0;
            nudRequestedQty.Value = 1;
        }

        // ── Remove line ──────────────────────────────────────────────────────────
        private void BtnRemoveLine_Click(object sender, EventArgs e)
        {
            if (dgvLines.SelectedRows.Count == 0)
            { ShowWarning("Please select a line to delete."); return; }

            int idx = dgvLines.SelectedRows[0].Index;
            if (idx >= 0 && idx < _requestLines.Count)
            {
                _requestLines.RemoveAt(idx);
                RefreshLinesGrid();
            }
        }

        // ── Refresh DataGridView ─────────────────────────────────────────────────
        private void RefreshLinesGrid()
        {
            dgvLines.Rows.Clear();
            for (int i = 0; i < _requestLines.Count; i++)
            {
                var l = _requestLines[i];
                dgvLines.Rows.Add(
                    i + 1,
                    l.MaterialName,
                    l.MaterialType,
                    l.WarehouseDisplay,
                    l.RequestedQty);
            }
            lblLineCount.Text = $"{_requestLines.Count} line(s) staged";
        }

        // ── Submit ───────────────────────────────────────────────────────────────
        private void BtnSubmit_Click(object sender, EventArgs e)
        {
            if (_requestLines.Count == 0)
            { ShowWarning("Please add at least one Raw Material line before submitting."); return; }

            string urgency = cboUrgency.SelectedItem?.ToString() ?? "Medium";
            string trigger = cboTrigger.SelectedItem?.ToString() ?? "Reorder";

            string orderId = null;
            if (trigger == "OrderDemand")
            {
                if (cboOrder.SelectedIndex < 1 || cboOrder.SelectedIndex > _orders.Count)
                { ShowWarning("An Order must be selected when Trigger Type is 'OrderDemand'."); return; }
                orderId = _orders[cboOrder.SelectedIndex - 1].OrderID;
            }

            int created = 0;
            var errors  = new List<string>();

            // Each line generates its own DB record with a unique RequestID
            foreach (var line in _requestLines)
            {
                string requestId = _ctrl.GenerateNextRequestId();
                try
                {
                    _ctrl.SubmitCreateMaterialRequest(
                        requestId, orderId,
                        line.RawMaterialItemID, line.WarehouseItemID,
                        line.RequestedQty, urgency, trigger);
                    created++;
                }
                catch (Exception ex)
                {
                    errors.Add($"{line.MaterialName}: {ex.Message}");
                }
            }

            if (errors.Count == 0)
            {
                MessageBox.Show(
                    $"{created} Material Request(s) created successfully under batch {_batchRef}.",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadForm();
            }
            else
            {
                string msg = $"{created} created.\n\nErrors:\n" + string.Join("\n", errors);
                MessageBox.Show(msg, "Partial Success", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                LoadForm();
            }
        }

        private void BtnReset_Click(object sender, EventArgs e) => LoadForm();

        private static void ShowWarning(string msg)
            => MessageBox.Show(msg, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        // ── Navigation / session ─────────────────────────────────────────────────
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }
    }

    // ── View-layer staging entity (no DB mapping) ────────────────────────────────
    internal sealed class MaterialRequestLineStaging
    {
        public string RawMaterialItemID { get; set; }
        public string MaterialName      { get; set; }
        public string MaterialType      { get; set; }
        public string WarehouseItemID   { get; set; }
        public string WarehouseDisplay  { get; set; }
        public int    RequestedQty      { get; set; }
    }
}
