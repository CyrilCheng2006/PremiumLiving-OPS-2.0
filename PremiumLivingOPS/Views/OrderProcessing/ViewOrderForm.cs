using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.OrderProcessing
{
    public partial class ViewOrderForm : Form
    {
        private readonly OrderProcessingController _ctrl = new OrderProcessingController();
        private List<OrderEntity> _currentOrders = new List<OrderEntity>();

        public ViewOrderForm()
        {
            InitializeComponent();
            this.Load += ViewOrderForm_Load;
        }

        // ── Load ──────────────────────────────────────────────────────────────
        private void ViewOrderForm_Load(object sender, EventArgs e) => RefreshGrid();

        // ── Refresh / Search ──────────────────────────────────────────────────
        private void RefreshGrid()
        {
            string status  = cboStatus.SelectedItem?.ToString();
            string keyword = txtSearch.Text.Trim();

            var vm = _ctrl.GetViewOrderVM(
                status  == "All" || string.IsNullOrEmpty(status)  ? null : status,
                string.IsNullOrEmpty(keyword) ? null : keyword);

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Order Processing  ›  View Order");

            _currentOrders = vm.Orders;
            dgvOrders.Rows.Clear();
            foreach (var o in _currentOrders)
                dgvOrders.Rows.Add(
                    o.OrderID, o.CustomerName, o.SalesName,
                    o.IssuedTime.ToString("yyyy-MM-dd"),
                    o.DeliveryDate.ToString("yyyy-MM-dd"),
                    $"HK$ {o.GrandTotal:N2}",
                    o.OrderStatus);

            UpdateActionButtons();
        }

        private void UpdateActionButtons()
        {
            bool sel = dgvOrders.SelectedRows.Count > 0;
            btnViewDetail.Enabled  = sel;
            btnModifyOrder.Enabled = sel;
        }

        // ── Toolbar events ────────────────────────────────────────────────────
        private void btnSearch_Click(object sender, EventArgs e)   => RefreshGrid();
        private void btnRefresh_Click(object sender, EventArgs e)  => RefreshGrid();
        private void cboStatus_Changed(object sender, EventArgs e) => RefreshGrid();
        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) RefreshGrid();
        }

        // ── DGV events ────────────────────────────────────────────────────────
        private void dgvOrders_SelectionChanged(object sender, EventArgs e)
            => UpdateActionButtons();

        private void dgvOrders_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvOrders.Columns[e.ColumnIndex].Name != "colStatus" || e.Value == null) return;
            switch (e.Value.ToString())
            {
                case "Pending":    e.CellStyle.ForeColor = Color.FromArgb(200, 130,  0); break;
                case "Processing": e.CellStyle.ForeColor = Color.FromArgb(  0, 100, 200); break;
                case "Delivered":  e.CellStyle.ForeColor = Color.FromArgb( 30, 140,  60); break;
                case "Cancelled":  e.CellStyle.ForeColor = Color.FromArgb(180,  40,  40); break;
            }
        }

        // ── Helper: get selected Order ID ─────────────────────────────────────
        private string SelectedOrderId()
        {
            if (dgvOrders.SelectedRows.Count == 0) return null;
            return dgvOrders.SelectedRows[0].Cells["colOrderID"].Value?.ToString();
        }

        // ── View Details button ───────────────────────────────────────────────
        private void btnViewDetail_Click(object sender, EventArgs e)
        {
            string id = SelectedOrderId();
            if (id == null) return;

            var detail = _ctrl.GetOrderDetail(id);
            if (detail?.Order == null)
            {
                MessageBox.Show("Order not found.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            ShowDetailDialog(detail);
        }

        // ── Modify Order button ───────────────────────────────────────────────
        private void btnModifyOrder_Click(object sender, EventArgs e)
        {
            string id = SelectedOrderId();
            if (id == null) return;
            // Pass the selected OrderID to ModifyOrderForm via static property
            ModifyOrderForm.PendingOrderId = id;
            FormNavigator.NavigateTo(this, "Order Processing", "Modify Order");
        }

        // ── Detail dialog (read-only) ─────────────────────────────────────────
        private void ShowDetailDialog(OrderDetailViewModel detail)
        {
            var o = detail.Order;
            using (var dlg = new Form())
            {
                dlg.Text            = $"Order Detail — {o.OrderID}";
                dlg.Size            = new Size(820, 640);
                dlg.StartPosition   = FormStartPosition.CenterParent;
                dlg.BackColor       = Palette.BgPage;
                dlg.Font            = new Font("Segoe UI", 13f);
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox     = false;
                dlg.MinimizeBox     = false;

                // Info section
                var pnlInfo = new Panel { Dock = DockStyle.Top, Height = 244, Padding = new Padding(28, 18, 28, 0) };
                int row = 0;
                void AddRow(string label, string value)
                {
                    int y = row++ * 28;
                    var p = new Panel { Location = new Point(0, y), Height = 28, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
                    p.Controls.Add(new Label { Text = label + ":", Width = 150, Location = new Point(0, 4), Font = new Font("Segoe UI", 12f), ForeColor = Palette.TextMuted });
                    p.Controls.Add(new Label { Text = value ?? "—", AutoSize = true, Location = new Point(154, 4), Font = new Font("Segoe UI", 12f), ForeColor = Palette.TextMain });
                    pnlInfo.Controls.Add(p);
                }
                AddRow("Order ID",      o.OrderID);
                AddRow("Customer",      o.CustomerName);
                AddRow("Sales Staff",   o.SalesName);
                AddRow("Contact",       o.OrderContactName);
                AddRow("Issued Date",   o.IssuedTime.ToString("yyyy-MM-dd"));
                AddRow("Delivery Date", o.DeliveryDate.ToString("yyyy-MM-dd"));
                AddRow("Grand Total",   $"HK$ {o.GrandTotal:N2}");
                AddRow("Status",        o.OrderStatus);

                // Line items label
                var lblLines = new Label
                {
                    Text = "Line Items", Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                    ForeColor = Palette.TextMain, Dock = DockStyle.Top, Height = 36,
                    Padding = new Padding(28, 8, 0, 0)
                };

                // Line items grid
                var dgv = new DataGridView
                {
                    ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    BackgroundColor = Palette.BgCard, BorderStyle = BorderStyle.None,
                    Dock = DockStyle.Fill, Font = new Font("Segoe UI", 13f),
                    RowTemplate = { Height = 40 },
                    CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                    ColumnHeadersHeight = 44,
                    ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                    {
                        BackColor = Color.FromArgb(240, 245, 255), ForeColor = Palette.TextMuted,
                        Font = new Font("Segoe UI", 12f, FontStyle.Bold)
                    },
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        BackColor = Palette.BgCard, ForeColor = Palette.TextMain,
                        SelectionBackColor = Color.FromArgb(230, 240, 255),
                        SelectionForeColor = Palette.TextMain
                    }
                };
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cItem",  HeaderText = "Item ID",    FillWeight = 18 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cName",  HeaderText = "Item Name",  FillWeight = 40 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cQty",   HeaderText = "Qty",        FillWeight = 10 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cPrice", HeaderText = "Unit Price", FillWeight = 16 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cTotal", HeaderText = "Line Total", FillWeight = 16 });
                foreach (var l in detail.Lines)
                    dgv.Rows.Add(l.ItemID, l.ItemName, l.Quantity,
                                 $"HK$ {l.Price:N2}", $"HK$ {l.LineTotal:N2}");

                // Close button
                var btnClose = new Button
                {
                    Text = "Close", Font = new Font("Segoe UI", 13f),
                    FlatStyle = FlatStyle.Flat, ForeColor = Palette.TextMuted,
                    Dock = DockStyle.Bottom, Height = 48
                };
                btnClose.FlatAppearance.BorderColor = Palette.BorderColor;
                btnClose.Click += (s, ev) => dlg.Close();

                // Add in DockStyle order: Fill first, then Top items
                dlg.Controls.Add(dgv);
                dlg.Controls.Add(lblLines);
                dlg.Controls.Add(pnlInfo);
                dlg.Controls.Add(btnClose);

                dlg.ShowDialog(this);
            }
        }

        // ── Nav / Logout ──────────────────────────────────────────────────────
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }
    }
}
