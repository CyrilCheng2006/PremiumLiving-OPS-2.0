// ViewOrderForm.cs — View layer for Order Processing > View Order tab
// Follows MVC: all data retrieval delegated to OrderProcessingController.
// UI only: bind ViewModel data, handle user events.
using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;          // AppShell, FormNavigator
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.OrderProcessing
{
    public partial class ViewOrderForm : Form
    {
        // ── Fields ──────────────────────────────────────────────────────────
        private readonly OrderProcessingController _ctrl = new OrderProcessingController();
        private AppShell _shell;

        private readonly Dictionary<string, Button> _kpiButtons = new Dictionary<string, Button>();

        private static readonly Dictionary<string, (Color bg, Color fg)> StatusColors =
            new Dictionary<string, (Color, Color)>(StringComparer.OrdinalIgnoreCase)
            {
                { "Pending",    (Color.FromArgb(254, 243, 199), Color.FromArgb(146, 64,  14)) },
                { "Processing", (Color.FromArgb(219, 234, 254), Color.FromArgb( 30, 64, 175)) },
                { "Delivered",  (Color.FromArgb(209, 250, 229), Color.FromArgb( 22,101,  52)) },
                { "Cancelled",  (Color.FromArgb(254, 226, 226), Color.FromArgb(185, 28,  28)) }
            };

        // ── Constructor ─────────────────────────────────────────────────────
        public ViewOrderForm()
        {
            InitializeComponent();
            WireEvents();
        }

        // ── Load ───────────────────────────────────────────────────────────────
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            AttachShell();
            BuildKpiBar();
            ExecuteSearch();
        }

        private void AttachShell()
        {
            _shell = new AppShell();
            Controls.Add(_shell);
            _shell.BringToFront();

            var vm = _ctrl.GetViewOrderVM();
            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Order Processing  ›  View Orders");
            _shell.SetPopupContainer(this);

            // Nav menu clicks — delegate to FormNavigator (requires current Form)
            _shell.MenuItemClicked += (menu, sub) =>
                FormNavigator.NavigateTo(this, menu, sub);

            // Logout: clear session then navigate to Login via FormNavigator
            _shell.LogoutClicked += (s, ev) =>
            {
                SessionManager.Clear();                          // ✔ correct API
                FormNavigator.NavigateTo(this, "Login", "");    // ✔ passes current Form
            };
        }

        private void BuildKpiBar()
        {
            pnlKpi.Controls.Clear();
            _kpiButtons.Clear();

            var pills = new[]
            {
                ("All",        Color.FromArgb(229, 231, 235), Color.FromArgb( 55,  65,  81)),
                ("Pending",    Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)),
                ("Processing", Color.FromArgb(219, 234, 254), Color.FromArgb( 30,  64, 175)),
                ("Delivered",  Color.FromArgb(209, 250, 229), Color.FromArgb( 22, 101,  52)),
                ("Cancelled",  Color.FromArgb(254, 226, 226), Color.FromArgb(185,  28,  28))
            };

            foreach (var (label, bg, fg) in pills)
            {
                var btn = new Button
                {
                    Text      = label + "  0",
                    Tag       = label,
                    BackColor = bg,
                    ForeColor = fg,
                    FlatStyle = FlatStyle.Flat,
                    Font      = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Height    = 32,
                    Width     = 110,
                    Margin    = new Padding(0, 0, 8, 0),
                    Cursor    = Cursors.Hand
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += KpiPill_Click;
                _kpiButtons[label] = btn;
                pnlKpi.Controls.Add(btn);
            }
        }

        // ── Wire events ─────────────────────────────────────────────────────
        private void WireEvents()
        {
            btnSearch.Click      += (s, e) => ExecuteSearch();
            btnClear.Click       += (s, e) => ClearSearch();
            btnCreateOrder.Click += (s, e) => OpenCreateOrder();

            chkDateFrom.CheckedChanged += (s, e) => dtpDateFrom.Enabled = chkDateFrom.Checked;
            chkDateTo.CheckedChanged   += (s, e) => dtpDateTo.Enabled   = chkDateTo.Checked;

            txtOrderNo.KeyDown  += SearchBox_KeyDown;
            txtCustomer.KeyDown += SearchBox_KeyDown;

            dgvOrders.CellClick      += dgvOrders_CellClick;
            dgvOrders.CellFormatting += dgvOrders_CellFormatting;
            dgvOrders.DoubleClick    += (s, e) => OpenDetailFromSelection();
            dgvOrders.CellPainting   += dgvOrders_CellPainting;
        }

        // ── Search ─────────────────────────────────────────────────────────────
        private void ExecuteSearch()
        {
            string keyword = BuildKeyword();
            string status  = cboStatus.SelectedItem?.ToString() == "All"
                             ? null : cboStatus.SelectedItem?.ToString();

            DateTime? dateFrom = chkDateFrom.Checked ? (DateTime?)dtpDateFrom.Value.Date : null;
            DateTime? dateTo   = chkDateTo.Checked   ? (DateTime?)dtpDateTo.Value.Date   : null;

            if (dateFrom.HasValue && dateTo.HasValue && dateFrom.Value > dateTo.Value)
            {
                MessageBox.Show("Date From cannot be later than Date To.",
                                "Invalid Date Range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var vm = _ctrl.GetViewOrderVM(status, keyword, dateFrom, dateTo);
            BindGrid(vm.Orders);
            RefreshKpiCounts(vm.Orders);
        }

        private string BuildKeyword()
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(txtOrderNo.Text))  parts.Add(txtOrderNo.Text.Trim());
            if (!string.IsNullOrWhiteSpace(txtCustomer.Text)) parts.Add(txtCustomer.Text.Trim());
            return parts.Count > 0 ? string.Join(" ", parts) : null;
        }

        private void ClearSearch()
        {
            txtOrderNo.Clear();
            txtCustomer.Clear();
            cboStatus.SelectedIndex = 0;
            chkDateFrom.Checked     = false;
            chkDateTo.Checked       = false;
            dtpDateFrom.Value       = DateTime.Today;
            dtpDateTo.Value         = DateTime.Today;
            ExecuteSearch();
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { ExecuteSearch(); e.SuppressKeyPress = true; }
        }

        // ── KPI pills ───────────────────────────────────────────────────────────
        private void KpiPill_Click(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is string status)
            {
                cboStatus.SelectedItem = status;
                ExecuteSearch();
            }
        }

        private void RefreshKpiCounts(List<OrderEntity> orders)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "All",        orders.Count },
                { "Pending",    0 },
                { "Processing", 0 },
                { "Delivered",  0 },
                { "Cancelled",  0 }
            };
            foreach (var o in orders)
                if (counts.ContainsKey(o.OrderStatus))
                    counts[o.OrderStatus]++;

            foreach (var kvp in _kpiButtons)
                kvp.Value.Text = kvp.Key + "  " + counts[kvp.Key];

            lblResultCount.Text = $"{orders.Count} order{(orders.Count != 1 ? "s" : "")} found";
        }

        // ── Grid binding ────────────────────────────────────────────────────
        private void BindGrid(List<OrderEntity> orders)
        {
            dgvOrders.Rows.Clear();
            foreach (var o in orders)
            {
                int idx = dgvOrders.Rows.Add(
                    o.OrderID,
                    o.CustomerName,
                    o.SalesName,
                    o.IssuedTime.ToString("dd MMM yyyy"),
                    o.DeliveryDate.ToString("dd MMM yyyy"),
                    $"HK$ {o.GrandTotal:N2}",
                    o.OrderStatus
                );
                dgvOrders.Rows[idx].Tag = o.OrderID;
            }
        }

        // ── Grid events ────────────────────────────────────────────────────
        private void dgvOrders_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvOrders.Columns[e.ColumnIndex].Name == "colAction")
                ShowDetailDialog(dgvOrders.Rows[e.RowIndex].Tag?.ToString());
        }

        private void OpenDetailFromSelection()
        {
            if (dgvOrders.CurrentRow?.Tag is string id)
                ShowDetailDialog(id);
        }

        private void dgvOrders_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var col = dgvOrders.Columns[e.ColumnIndex];

            if (col.Name == "colStatus" && e.Value != null)
            {
                if (StatusColors.TryGetValue(e.Value.ToString(), out var c))
                {
                    e.CellStyle.BackColor = c.bg;
                    e.CellStyle.ForeColor = c.fg;
                    e.CellStyle.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
                    e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
            }

            if (col.Name != "colStatus" && e.RowIndex % 2 == 1 && !dgvOrders.Rows[e.RowIndex].Selected)
                e.CellStyle.BackColor = Color.FromArgb(249, 250, 252);
        }

        private void dgvOrders_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || dgvOrders.Columns[e.ColumnIndex].Name != "colAction") return;

            e.Paint(e.ClipBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);

            var btnRect = new Rectangle(
                e.CellBounds.X + 8, e.CellBounds.Y + 8,
                e.CellBounds.Width - 16, e.CellBounds.Height - 16);

            using (var brush = new SolidBrush(Color.FromArgb(37, 99, 235)))
            using (var fnt   = new Font("Segoe UI", 8.5F, FontStyle.Bold))
            using (var path  = RoundedRect(btnRect, 6))
            {
                e.Graphics.FillPath(brush, path);
                var sf = new StringFormat
                {
                    Alignment     = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                e.Graphics.DrawString("View Details", fnt, Brushes.White, btnRect, sf);
            }
            e.Handled = true;
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(r.X,                   r.Y,                   radius * 2, radius * 2, 180, 90);
            path.AddArc(r.Right - radius * 2,  r.Y,                   radius * 2, radius * 2, 270, 90);
            path.AddArc(r.Right - radius * 2,  r.Bottom - radius * 2, radius * 2, radius * 2,   0, 90);
            path.AddArc(r.X,                   r.Bottom - radius * 2, radius * 2, radius * 2,  90, 90);
            path.CloseFigure();
            return path;
        }

        // ── Detail Dialog ───────────────────────────────────────────────────
        private void ShowDetailDialog(string orderId)
        {
            if (string.IsNullOrEmpty(orderId)) return;
            var detail = _ctrl.GetOrderDetail(orderId);
            if (detail?.Order == null) return;
            var o = detail.Order;

            using (var dlg = new Form())
            {
                dlg.Text            = "Order Details";
                dlg.Size            = new Size(720, 620);
                dlg.StartPosition   = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox     = false;
                dlg.MinimizeBox     = false;
                dlg.BackColor       = Color.White;

                var header = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.FromArgb(15, 23, 42) };
                StatusColors.TryGetValue(o.OrderStatus, out var sc);
                header.Controls.Add(new Label
                {
                    Text = $"Order Details  —  {o.OrderID}",
                    ForeColor = Color.White, Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                    Dock = DockStyle.Left, AutoSize = false, Width = 420,
                    TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(20, 0, 0, 0)
                });
                var badge = new Label
                {
                    Text = o.OrderStatus,
                    BackColor = sc.bg != default ? sc.bg : Color.Gray,
                    ForeColor = sc.fg != default ? sc.fg : Color.White,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    AutoSize = false, Width = 90, Height = 28,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right
                };
                badge.Location = new Point(dlg.ClientSize.Width - 120, 14);
                header.Controls.Add(badge);

                var tbl = new TableLayoutPanel
                {
                    Dock = DockStyle.Top, Height = 160, ColumnCount = 4, RowCount = 6,
                    BackColor = Color.White, Padding = new Padding(20, 12, 20, 4)
                };
                for (int i = 0; i < 4; i++) tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
                for (int i = 0; i < 6; i++) tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, i % 2 == 0 ? 18F : 26F));

                void AddInfo(int col, int row, string lbl, string val)
                {
                    tbl.Controls.Add(new Label { Text = lbl, Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                        ForeColor = Color.FromArgb(107, 114, 128), Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.BottomLeft }, col, row * 2);
                    tbl.Controls.Add(new Label { Text = val, Font = new Font("Segoe UI", 9.5F),
                        ForeColor = Color.FromArgb(17, 24, 39), Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.TopLeft }, col, row * 2 + 1);
                }
                AddInfo(0, 0, "ORDER NO.",     o.OrderID);
                AddInfo(1, 0, "CUSTOMER",      o.CustomerName);
                AddInfo(2, 0, "SALES REP",     o.SalesName);
                AddInfo(3, 0, "ORDER DATE",    o.IssuedTime.ToString("dd MMM yyyy"));
                AddInfo(0, 1, "DELIVERY DATE", o.DeliveryDate.ToString("dd MMM yyyy"));
                AddInfo(1, 1, "CONTACT",       o.OrderContactName);
                AddInfo(2, 1, "SHIPPING ADDR", o.ShippingAddress);
                AddInfo(3, 1, "BILLING ADDR",  o.BillingAddress);

                var div      = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(229, 231, 235) };
                var lblItems = new Label  { Text = "ORDER ITEMS", Dock = DockStyle.Top, Height = 32,
                                            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                                            ForeColor = Color.FromArgb(37, 99, 235),
                                            Padding = new Padding(20, 8, 0, 0) };

                var dgv = new DataGridView
                {
                    Dock = DockStyle.Fill, BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                    RowHeadersVisible = false, AllowUserToAddRows = false, ReadOnly = true,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    EnableHeadersVisualStyles = false
                };
                dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(246, 249, 255);
                dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(107, 114, 128);
                dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 8F, FontStyle.Bold);
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cItemID",   HeaderText = "ITEM ID",    FillWeight = 20 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cItemName", HeaderText = "ITEM NAME",  FillWeight = 40 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cQty",      HeaderText = "QTY",        FillWeight = 15 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cPrice",    HeaderText = "UNIT PRICE", FillWeight = 15 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cTotal",    HeaderText = "TOTAL",      FillWeight = 15 });

                foreach (var line in detail.Lines)
                    dgv.Rows.Add(line.ItemID, line.ItemName, line.Quantity,
                                 $"HK$ {line.Price:N2}", $"HK$ {line.Quantity * line.Price:N2}");

                var pnlGT = new Panel { Dock = DockStyle.Bottom, Height = 36, BackColor = Color.FromArgb(249, 250, 252) };
                pnlGT.Controls.Add(new Label
                {
                    Text = $"Grand Total:    HK$ {o.GrandTotal:N2}",
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(17, 24, 39),
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 0, 20, 0)
                });

                var pnlFoot = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.White, Padding = new Padding(0, 8, 20, 0) };
                var btnClose = new Button
                {
                    Text = "Close", Width = 90, Height = 32, BackColor = Color.FromArgb(243, 244, 246),
                    ForeColor = Color.FromArgb(55, 65, 81), FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right, Cursor = Cursors.Hand
                };
                btnClose.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);
                btnClose.FlatAppearance.BorderSize  = 1;
                btnClose.Location = new Point(dlg.ClientSize.Width - 110, 8);
                btnClose.Click   += (_, __) => dlg.Close();
                pnlFoot.Controls.Add(btnClose);

                dlg.Controls.Add(dgv);
                dlg.Controls.Add(pnlGT);
                dlg.Controls.Add(pnlFoot);
                dlg.Controls.Add(lblItems);
                dlg.Controls.Add(div);
                dlg.Controls.Add(tbl);
                dlg.Controls.Add(header);
                dlg.ShowDialog(this);
            }
        }

        // ── Navigation ───────────────────────────────────────────────────────
        private void OpenCreateOrder()
        {
            new CreateOrderForm().ShowDialog(this);
            ExecuteSearch();
        }
    }
}
