using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.AfterService
{
    /// <summary>
    /// Popup picker for selecting an Order ID in Create Return Order.
    /// Displays a searchable DataGridView of eligible orders.
    /// </summary>
    public class OrderPickerForm : Form
    {
        // ── public result ─────────────────────────────────────────────────
        public string SelectedOrderID   { get; private set; }
        public string SelectedCustomer  { get; private set; }
        public double SelectedGrandTotal { get; private set; }

        // ── injected data source ──────────────────────────────────────────
        private readonly List<OrderEntity> _allOrders;

        // ── controls ─────────────────────────────────────────────────────
        private TextBox      txtSearch;
        private DataGridView dgv;
        private Button       btnSelect;
        private Button       btnCancel;
        private CardPanel    card;

        public OrderPickerForm(List<OrderEntity> orders)
        {
            _allOrders = orders ?? new List<OrderEntity>();
            InitUI();
            PopulateGrid(_allOrders);
        }

        // ─────────────────────────────────────────────────────────────────
        //  UI Construction
        // ─────────────────────────────────────────────────────────────────
        private void InitUI()
        {
            Text            = "Select Order";
            Size            = new Size(860, 520);
            MinimumSize     = new Size(700, 420);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = Color.FromArgb(243, 244, 246);
            Font            = new Font("Segoe UI", 9.5f);

            // ── outer card ───────────────────────────────────────────────
            card = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(16) };
            Controls.Add(card);

            // ── title ────────────────────────────────────────────────────
            var lblTitle = new Label
            {
                Text      = "Select Order ID",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30),
                AutoSize  = true,
                Location  = new Point(16, 14)
            };
            card.Controls.Add(lblTitle);

            // ── search bar ───────────────────────────────────────────────
            var lblSearch = new Label
            {
                Text     = "Search:",
                AutoSize = true,
                Location = new Point(16, 50),
                ForeColor = Color.FromArgb(80, 80, 80)
            };
            card.Controls.Add(lblSearch);

            txtSearch = new TextBox
            {
                Location    = new Point(70, 47),
                Size        = new Size(300, 26),
                PlaceholderText = "Order ID, Customer name..."
            };
            txtSearch.TextChanged += (s, e) => FilterGrid(txtSearch.Text.Trim());
            card.Controls.Add(txtSearch);

            // ── grid ─────────────────────────────────────────────────────
            dgv = new DataGridView
            {
                Location          = new Point(16, 84),
                Size              = new Size(810, 340),
                ReadOnly          = true,
                AllowUserToAddRows    = false,
                SelectionMode     = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect       = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                BackgroundColor   = Color.White,
                BorderStyle       = BorderStyle.None,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgv.DoubleClick += (s, e) => ConfirmSelection();
            card.Controls.Add(dgv);

            // ── columns ──────────────────────────────────────────────────
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "OrderID",       HeaderText = "Order ID",       FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "CustomerName",  HeaderText = "Customer",       FillWeight = 28 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "IssuedTime",    HeaderText = "Issued Date",    FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "OrderStatus",   HeaderText = "Status",         FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "GrandTotal",    HeaderText = "Grand Total",    FillWeight = 16 });

            // ── buttons ──────────────────────────────────────────────────
            btnSelect = new Button
            {
                Text      = "Select",
                Size      = new Size(90, 34),
                Location  = new Point(652, 434),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnSelect.FlatAppearance.BorderSize = 0;
            btnSelect.Click += (s, e) => ConfirmSelection();
            card.Controls.Add(btnSelect);

            btnCancel = new Button
            {
                Text      = "Cancel",
                Size      = new Size(90, 34),
                Location  = new Point(748, 434),
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9.5f)
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            card.Controls.Add(btnCancel);
        }

        // ─────────────────────────────────────────────────────────────────
        //  Data helpers
        // ─────────────────────────────────────────────────────────────────
        private void PopulateGrid(IEnumerable<OrderEntity> source)
        {
            dgv.Rows.Clear();
            foreach (var o in source)
            {
                dgv.Rows.Add(
                    o.OrderID,
                    o.CustomerName,
                    o.IssuedTime.ToString("yyyy-MM-dd"),
                    o.OrderStatus,
                    o.GrandTotal.ToString("N2"));
            }
        }

        private void FilterGrid(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                PopulateGrid(_allOrders);
                return;
            }
            var filtered = _allOrders.Where(o =>
                o.OrderID.IndexOf(keyword,      StringComparison.OrdinalIgnoreCase) >= 0 ||
                o.CustomerName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                o.OrderStatus.IndexOf(keyword,  StringComparison.OrdinalIgnoreCase) >= 0);
            PopulateGrid(filtered);
        }

        private void ConfirmSelection()
        {
            if (dgv.CurrentRow == null) return;
            SelectedOrderID    = dgv.CurrentRow.Cells["OrderID"].Value?.ToString();
            SelectedCustomer   = dgv.CurrentRow.Cells["CustomerName"].Value?.ToString();
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
