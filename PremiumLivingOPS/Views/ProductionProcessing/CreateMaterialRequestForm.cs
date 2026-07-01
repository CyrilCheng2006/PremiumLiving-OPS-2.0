using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.DAL;
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
    /// View — Create Raw Material Request (multi-line, Plan-A Batch Prefix).
    ///
    /// What the user sees
    ///   Request ID label  →  Batch Prefix  e.g. MRQ-260701-003
    ///                         (no -NN suffix shown)
    ///
    /// What goes into the DB
    ///   Line 1  RequestID = MRQ-260701-003-01
    ///   Line 2  RequestID = MRQ-260701-003-02
    ///   …
    ///   All lines share the same OrderID / UrgencyLevel / TriggerType.
    /// </summary>
    public partial class CreateMaterialRequestForm : Form
    {
        private readonly ProductionProcessingController _ctrl =
            new ProductionProcessingController();

        // Lookup data
        private List<RawMaterialLookup>   _rawMaterials   = new List<RawMaterialLookup>();
        private List<WarehouseItemLookup> _warehouseItems = new List<WarehouseItemLookup>();
        private List<OrderLookup>         _orders         = new List<OrderLookup>();

        // Staged lines
        private readonly List<MaterialRequestLineStaging> _requestLines =
            new List<MaterialRequestLineStaging>();

        // Batch prefix shown to the user  (e.g. "MRQ-260701-003")
        // DB RequestID per line           (e.g. "MRQ-260701-003-01", "MRQ-260701-003-02", …)
        private string _batchPrefix = string.Empty;

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
            _shell.SetBreadcrumb("Production Processing  ›  Create Raw Material Request");

            // _batchPrefix is what the user sees.  The DB PKs will be prefix-01, prefix-02 …
            _batchPrefix = vm.NextRequestID;   // e.g. "MRQ-260701-003"
            lblBatchRef.Text = _batchPrefix;   // shown in Card 1 — NO -NN suffix

            cboUrgency.SelectedIndex = 0;
            cboTrigger.SelectedIndex = 0;

            _rawMaterials = vm.RawMaterials;
            cboRawMaterial.Items.Clear();
            cboRawMaterial.Items.Add("-- Select Raw Material --");
            foreach (var m in _rawMaterials)
                cboRawMaterial.Items.Add(m);
            cboRawMaterial.SelectedIndex = 0;

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
            if (!isOrderDemand) cboOrder.SelectedIndex = 0;
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

            // Merge same material + warehouse combination
            var existing = _requestLines.FirstOrDefault(
                l => l.RawMaterialItemID == mat.ItemID &&
                     l.WarehouseItemID   == wh.WarehouseItemID);

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
                // Column "Request ID" shows batch prefix only — no -NN suffix
                dgvLines.Rows.Add(
                    i + 1,
                    _batchPrefix,          // displayed Request ID = batch prefix
                    l.MaterialName,
                    l.MaterialType,
                    l.WarehouseDisplay,
                    l.RequestedQty);
            }
            lblLineCount.Text = $"{_requestLines.Count} line(s) staged  —  Request ID: {_batchPrefix}";
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

            for (int i = 0; i < _requestLines.Count; i++)
            {
                var line = _requestLines[i];
                int lineNo = i + 1;   // 1-based

                // DB PK = batch prefix + "-NN"  e.g. MRQ-260701-003-01
                // User never sees the -NN part.
                string dbRequestId = ProductionProcessingRepo.BuildLineRequestId(_batchPrefix, lineNo);

                try
                {
                    _ctrl.SubmitCreateMaterialRequest(
                        dbRequestId, orderId,
                        line.RawMaterialItemID, line.WarehouseItemID,
                        line.RequestedQty, urgency, trigger);
                    created++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Line {lineNo} ({line.MaterialName}): {ex.Message}");
                }
            }

            if (errors.Count == 0)
            {
                MessageBox.Show(
                    $"{created} item(s) saved under Request ID  {_batchPrefix}.",
                    "Request Created",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadForm();
            }
            else
            {
                string msg = $"{created} item(s) saved.\n\nErrors:\n" + string.Join("\n", errors);
                MessageBox.Show(msg, "Partial Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                LoadForm();
            }
        }

        private void BtnReset_Click(object sender, EventArgs e) => LoadForm();

        private static void ShowWarning(string msg)
            => MessageBox.Show(msg, "Validation",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);

        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to log out?",
                "Confirm Logout",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            SessionManager.Clear();
            Application.Restart();
        }
    }

    // ── Staging entity (View layer only, no DB mapping) ──────────────────────────────
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
