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
    /// View — Create Procurement  (batch-prefix, multi-item model).
    ///
    /// Interaction flow
    ///   1. Form loads: all unlinked MRQ batch prefixes appear in dropdown.
    ///   2. User selects MRQ-260702-001.
    ///   3. Grid immediately shows all -NN line items under that prefix.
    ///   4. User reviews / edits OrderQty + UnitPrice per line, selects Supplier.
    ///   5. Submit → one PurchaseOrder + PurchaseOrderLine per line.
    /// </summary>
    public partial class CreateProcurementForm : Form
    {
        private readonly ProcurementController _ctrl = new ProcurementController();

        private List<MaterialRequestBatchLookup> _batches   = new List<MaterialRequestBatchLookup>();
        private List<SupplierLookup>             _suppliers = new List<SupplierLookup>();
        private List<MaterialRequestLineItem>    _lines     = new List<MaterialRequestLineItem>();

        public CreateProcurementForm()
        {
            InitializeComponent();
            this.Load += CreateProcurementForm_Load;
        }

        // ══ Load ═════════════════════════════════════════════════════

        private void CreateProcurementForm_Load(object sender, EventArgs e)
        {
            cboBatchPrefix.SelectedIndexChanged += CboBatchPrefix_Changed;
            dgvLines.CellValueChanged           += DgvLines_CellValueChanged;
            btnSubmit.Click -= BtnSubmit_Click;
            btnReset.Click  -= BtnReset_Click;
            btnSubmit.Click += BtnSubmit_Click;
            btnReset.Click  += BtnReset_Click;
            LoadForm();
        }

        // ══ Data load ═══════════════════════════════════════════════

        private void LoadForm()
        {
            var vm = _ctrl.GetCreateProcurementVM();

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Raw Material  ›  Create Procurement");

            // PO ID chip
            lblPurchaseIDValue.Text = vm.NextPurchaseID;

            // Batch prefix dropdown
            _batches = vm.BatchPrefixes ?? new List<MaterialRequestBatchLookup>();
            cboBatchPrefix.Items.Clear();
            cboBatchPrefix.Items.Add("-- Select Material Request --");
            foreach (var b in _batches)
                cboBatchPrefix.Items.Add(b);
            cboBatchPrefix.SelectedIndex = 0;

            // Supplier dropdown
            _suppliers = vm.Suppliers ?? new List<SupplierLookup>();
            cboSupplier.Items.Clear();
            cboSupplier.Items.Add("-- Select Supplier --");
            foreach (var s in _suppliers)
                cboSupplier.Items.Add(s);
            cboSupplier.SelectedIndex = 0;

            // Reset header fields
            dtpOrderDate.Value      = DateTime.Today;
            cboStatus.SelectedIndex = 0;

            // Clear lines
            _lines = new List<MaterialRequestLineItem>();
            RefreshLinesGrid();
        }

        // ══ Batch prefix selection ────────────────────────────────────

        private void CboBatchPrefix_Changed(object sender, EventArgs e)
        {
            if (cboBatchPrefix.SelectedItem is MaterialRequestBatchLookup batch)
            {
                _lines = _ctrl.GetLinesByBatchPrefix(batch.BatchPrefix);
                lblBatchInfo.Text      = $"Urgency: {batch.UrgencyLevel}   Trigger: {batch.TriggerType}   —   {batch.LineCount} item(s)";
                lblBatchInfo.ForeColor = Color.FromArgb(47, 111, 237);
            }
            else
            {
                _lines = new List<MaterialRequestLineItem>();
                lblBatchInfo.Text      = string.Empty;
                lblBatchInfo.ForeColor = Color.FromArgb(98, 112, 135);
            }
            RefreshLinesGrid();
            RecalcGrandTotal();
        }

        // ══ Grid ────────────────────────────────────────────────

        private void RefreshLinesGrid()
        {
            dgvLines.CellValueChanged -= DgvLines_CellValueChanged;
            dgvLines.Rows.Clear();

            for (int i = 0; i < _lines.Count; i++)
            {
                var ln = _lines[i];
                int idx = dgvLines.Rows.Add(
                    i + 1,
                    ln.RequestID,
                    ln.MaterialName,
                    ln.MaterialType,
                    ln.WarehouseDisplay,
                    ln.RequestedQty,
                    ln.OrderQty,
                    ln.UnitPrice,
                    $"HK$ {ln.LineTotal:N2}");
                dgvLines.Rows[idx].Tag = ln;
            }

            lblLineCount.Text = $"{_lines.Count} line(s) loaded";
            dgvLines.CellValueChanged += DgvLines_CellValueChanged;
        }

        private void DgvLines_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _lines.Count) return;

            var ln  = _lines[e.RowIndex];
            var row = dgvLines.Rows[e.RowIndex];

            // colOrderQty (index 6)
            if (e.ColumnIndex == 6)
            {
                if (int.TryParse(row.Cells[6].Value?.ToString(), out int qty) && qty > 0)
                    ln.OrderQty = qty;
                else
                    row.Cells[6].Value = ln.OrderQty;
            }
            // colUnitPrice (index 7)
            else if (e.ColumnIndex == 7)
            {
                if (double.TryParse(row.Cells[7].Value?.ToString(), out double price) && price > 0)
                    ln.UnitPrice = price;
                else
                    row.Cells[7].Value = ln.UnitPrice;
            }

            row.Cells[8].Value = $"HK$ {ln.LineTotal:N2}";
            RecalcGrandTotal();
        }

        private void RecalcGrandTotal()
        {
            double total = 0;
            foreach (var ln in _lines) total += ln.LineTotal;
            lblGrandTotal.Text = $"HK$ {total:N2}";
        }

        // ══ Submit ──────────────────────────────────────────────

        private void BtnSubmit_Click(object sender, EventArgs e)
        {
            if (!(cboBatchPrefix.SelectedItem is MaterialRequestBatchLookup))
            { ShowWarning("Please select a Material Request batch prefix."); return; }
            if (_lines.Count == 0)
            { ShowWarning("No line items are loaded for the selected Material Request."); return; }

            string supplierId  = (cboSupplier.SelectedItem as SupplierLookup)?.SupplierID;
            string purchaseBase = lblPurchaseIDValue.Text.Trim();
            DateTime orderDate = dtpOrderDate.Value.Date;
            string status      = cboStatus.SelectedItem?.ToString() ?? "Sent";

            try
            {
                _ctrl.SubmitCreateProcurement(
                    purchaseBase, supplierId, orderDate, status, _lines);

                int count = _lines.Count;
                MessageBox.Show(
                    $"{count} Purchase Order(s) created successfully under base ID  {purchaseBase}.",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadForm();
            }
            catch (ArgumentException ex)
            {
                ShowWarning(ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An error occurred:\n\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnReset_Click(object sender, EventArgs e) => LoadForm();

        private static void ShowWarning(string msg)
            => MessageBox.Show(msg, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        // ══ Navigation ─────────────────────────────────────────

        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }
    }
}
