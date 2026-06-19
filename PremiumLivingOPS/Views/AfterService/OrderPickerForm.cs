using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.AfterService
{
    /// <summary>
    /// Popup picker for selecting an Order ID in Create Return Order.
    /// Uses CardPanel.Create() / CardPanel.CreateFill() — CardPanel is a static class.
    /// </summary>
    public class OrderPickerForm : Form
    {
        public string SelectedOrderID    { get; private set; }
        public string SelectedCustomer   { get; private set; }
        public double SelectedGrandTotal { get; private set; }

        private readonly List<OrderEntity> _allOrders;

        private TextBox      txtSearch;
        private DataGridView dgv;
        private Button       btnSelect;
        private Button       btnCancel;

        public OrderPickerForm(List<OrderEntity> orders)
        {
            _allOrders = orders ?? new List<OrderEntity>();
            InitUI();
            PopulateGrid(_allOrders);
        }

        private void InitUI()
        {
            Text            = "Select Order";
            Size            = new Size(860, 520);
            MinimumSize     = new Size(700, 420);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = Color.FromArgb(240, 244, 249);
            Font            = new Font("Segoe UI", 9.5f);

            // ── outer layout panel (fills the form) ──────────────────────────
            var layout = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0) };
            Controls.Add(layout);

            // ── search bar card ──────────────────────────────────────────────
            var (searchOuter, searchInner) = CardPanel.Create(outerHeight: 70,
                outerPadding: new Padding(12, 8, 12, 4));
            searchOuter.Dock = DockStyle.Top;
            layout.Controls.Add(searchOuter);

            var lblTitle = new Label
            {
                Text      = "Select Order ID",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30),
                AutoSize  = true,
                Location  = new Point(10, 8)
            };
            searchInner.Controls.Add(lblTitle);

            var lblSearch = new Label
            {
                Text      = "Search:",
                AutoSize  = true,
                Location  = new Point(10, 38),
                ForeColor = Color.FromArgb(80, 80, 80)
            };
            searchInner.Controls.Add(lblSearch);

            txtSearch = new TextBox
            {
                Location        = new Point(66, 35),
                Size            = new Size(320, 26),
                PlaceholderText = "Order ID, Customer name..."
            };
            txtSearch.TextChanged += (s, e) => FilterGrid(txtSearch.Text.Trim());
            searchInner.Controls.Add(txtSearch);

            // ── button panel ─────────────────────────────────────────────────
            var (btnOuter, btnInner) = CardPanel.Create(outerHeight: 56,
                outerPadding: new Padding(12, 6, 12, 6));
            btnOuter.Dock = DockStyle.Bottom;
            layout.Controls.Add(btnOuter);

            btnSelect = new Button
            {
                Text      = "Select",
                Size      = new Size(90, 34),
                Anchor    = AnchorStyles.Right | AnchorStyles.Top,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnSelect.FlatAppearance.BorderSize = 0;
            btnSelect.Click += (s, e) => ConfirmSelection();

            btnCancel = new Button
            {
                Text      = "Cancel",
                Size      = new Size(90, 34),
                Anchor    = AnchorStyles.Right | AnchorStyles.Top,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9.5f)
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var btnFlow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents  = false,
                Padding       = new Padding(0)
            };
            btnFlow.Controls.Add(btnCancel);
            btnFlow.Controls.Add(btnSelect);
            btnInner.Controls.Add(btnFlow);

            // ── grid card (fills remaining space) ────────────────────────────
            var (gridOuter, gridInner) = CardPanel.CreateFill(
                outerPadding: new Padding(12, 4, 12, 4));
            layout.Controls.Add(gridOuter);

            dgv = new DataGridView
            {
                Dock                = DockStyle.Fill,
                ReadOnly            = true,
                AllowUserToAddRows  = false,
                SelectionMode       = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect         = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                BackgroundColor     = Color.White,
                BorderStyle         = BorderStyle.None,
                RowHeadersVisible   = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgv.DoubleClick += (s, e) => ConfirmSelection();
            gridInner.Controls.Add(dgv);

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "OrderID",      HeaderText = "Order ID",    FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "CustomerName", HeaderText = "Customer",    FillWeight = 30 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "IssuedTime",   HeaderText = "Issued Date", FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "OrderStatus",  HeaderText = "Status",      FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "GrandTotal",   HeaderText = "Grand Total", FillWeight = 16 });
        }

        private void PopulateGrid(IEnumerable<OrderEntity> source)
        {
            dgv.Rows.Clear();
            foreach (var o in source)
                dgv.Rows.Add(o.OrderID, o.CustomerName,
                             o.IssuedTime.ToString("yyyy-MM-dd"),
                             o.OrderStatus, o.GrandTotal.ToString("N2"));
        }

        private void FilterGrid(string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) { PopulateGrid(_allOrders); return; }
            var filtered = _allOrders.Where(o =>
                o.OrderID.IndexOf(keyword,      StringComparison.OrdinalIgnoreCase) >= 0 ||
                o.CustomerName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                o.OrderStatus.IndexOf(keyword,  StringComparison.OrdinalIgnoreCase) >= 0);
            PopulateGrid(filtered);
        }

        private void ConfirmSelection()
        {
            if (dgv.CurrentRow == null) return;
            SelectedOrderID  = dgv.CurrentRow.Cells["OrderID"].Value?.ToString();
            SelectedCustomer = dgv.CurrentRow.Cells["CustomerName"].Value?.ToString();
            if (double.TryParse(
                    dgv.CurrentRow.Cells["GrandTotal"].Value?.ToString(),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double gt))
                SelectedGrandTotal = gt;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
