using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.OrderProcessing
{
    /// <summary>
    /// View Order — Tab 1 of Order Processing Management.
    ///
    /// Search is triggered ONLY by btnSearch (or Enter in a text box).
    /// Reset clears all search fields and reloads all orders.
    /// </summary>
    public partial class ViewOrderForm : Form
    {
        private readonly OrderProcessingController _ctrl = new OrderProcessingController();
        private List<OrderEntity> _currentOrders        = new List<OrderEntity>();

        // ── Status colour map (bg, fg) ────────────────────────────────────────────
        private static readonly Dictionary<string, (Color bg, Color fg)> StatusColors =
            new Dictionary<string, (Color, Color)>
            {
                //  Status               bg                                fg
                { "Pending",             (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "Processing",          (Color.FromArgb(219, 234, 254), Color.FromArgb( 29,  78, 216)) },
                { "Shipped",             (Color.FromArgb(224, 242, 254), Color.FromArgb(  3,  96, 170)) },
                { "Delivered",           (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
                { "Partially Delivered", (Color.FromArgb(237, 233, 254), Color.FromArgb( 91,  33, 182)) },
            };

        public ViewOrderForm()
        {
            InitializeComponent();
            this.Load += ViewOrderForm_Load;
        }

        // ── Load ───────────────────────────────────────────────────────────────────────
        private void ViewOrderForm_Load(object sender, EventArgs e)
        {
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;
            RefreshGrid();
        }

        // ── Search ──────────────────────────────────────────────────────────────────────
        private void RefreshGrid()
        {
            string orderNo  = txtSearchOrderNo.Text.Trim();
            string customer = txtSearchCustomer.Text.Trim();
            string status   = cboStatus.SelectedItem?.ToString();
            DateTime? dateFrom = chkDateFrom.Checked ? (DateTime?)dtpDateFrom.Value.Date : null;

            string keyword = !string.IsNullOrEmpty(orderNo)  ? orderNo
                           : !string.IsNullOrEmpty(customer) ? customer
                           : null;

            var vm = _ctrl.GetViewOrderVM(
                status == "All" || string.IsNullOrEmpty(status) ? null : status,
                keyword,
                dateFrom);

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Order Processing  ›  View Order");

            _currentOrders = vm.Orders;

            dgvOrders.Rows.Clear();
            foreach (var o in _currentOrders)
                dgvOrders.Rows.Add(
                    o.OrderID,
                    o.CustomerName,
                    o.SalesName,
                    o.IssuedTime.ToString("yyyy-MM-dd"),
                    o.DeliveryDate.ToString("yyyy-MM-dd"),
                    $"HK$ {o.GrandTotal:N2}",
                    o.OrderStatus);

            RefreshKpi();
            UpdateActionButtons();
        }

        // ── Reset ────────────────────────────────────────────────────────────────────────
        private void ResetFilters()
        {
            txtSearchOrderNo.Text  = string.Empty;
            txtSearchCustomer.Text = string.Empty;
            cboStatus.SelectedIndex = 0;
            chkDateFrom.Checked    = false;
            dtpDateFrom.Value      = DateTime.Today.AddMonths(-1);
            dtpDateFrom.Enabled    = false;
            RefreshGrid();
        }

        // ── KPI bar ──────────────────────────────────────────────────────────────────────
        private void RefreshKpi()
        {
            pnlKpi.Controls.Clear();

            int total            = _currentOrders.Count;
            int pending          = _currentOrders.FindAll(o => o.OrderStatus == "Pending").Count;
            int processing       = _currentOrders.FindAll(o => o.OrderStatus == "Processing").Count;
            int delivered        = _currentOrders.FindAll(o => o.OrderStatus == "Delivered").Count;
            int shipped          = _currentOrders.FindAll(o => o.OrderStatus == "Shipped").Count;
            int partialDelivered = _currentOrders.FindAll(o => o.OrderStatus == "Partially Delivered").Count;

            var pills = new[]
            {
                ("Total",               total.ToString(),            Color.FromArgb( 47, 111, 237), Color.FromArgb(219, 234, 254)),
                ("Pending",             pending.ToString(),          Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199)),
                ("Processing",          processing.ToString(),       Color.FromArgb( 29,  78, 216), Color.FromArgb(219, 234, 254)),
                ("Delivered",           delivered.ToString(),        Color.FromArgb(  6,  95,  70), Color.FromArgb(209, 250, 229)),
                ("Shipped",             shipped.ToString(),          Color.FromArgb(  3,  96, 170), Color.FromArgb(224, 242, 254)),
                ("Partially Delivered", partialDelivered.ToString(), Color.FromArgb( 91,  33, 182), Color.FromArgb(237, 233, 254)),
            };

            const int PillW = 280;
            const int PillH = 50;
            const int Gap   = 14;
            const int CountW = 52;   // left column: number

            int x = 0;
            foreach (var (label, count, fg, bg) in pills)
            {
                // ── Pill container
                var pill = new Panel
                {
                    BackColor = bg,
                    Location  = new Point(x, 0),
                    Size      = new Size(PillW, PillH),
                    Cursor    = Cursors.Hand
                };
                pill.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path  = RoundedRect(((Panel)s).ClientRectangle, 8);
                    using var brush = new SolidBrush(((Panel)s).BackColor);
                    e.Graphics.FillPath(brush, path);
                };

                // ── Left: large number, vertically centred
                var lblCount = new Label
                {
                    Text      = count,
                    Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                    ForeColor = fg,
                    BackColor = Color.Transparent,
                    Size      = new Size(CountW, PillH),
                    Location  = new Point(8, 0),
                    TextAlign = ContentAlignment.MiddleCenter
                };

                // ── Right: status name, vertically centred
                var lblName = new Label
                {
                    Text      = label,
                    Font      = new Font("Segoe UI", 12f),
                    ForeColor = fg,
                    BackColor = Color.Transparent,
                    Size      = new Size(PillW - CountW - 8, PillH),
                    Location  = new Point(CountW + 8, 0),
                    TextAlign = ContentAlignment.MiddleLeft
                };

                // Forward clicks from child labels to the pill handler
                string filterLabel = label == "Total" ? "All" : label;
                EventHandler clickHandler = (s, e) =>
                {
                    cboStatus.SelectedItem = filterLabel;
                    RefreshGrid();
                };
                pill.Click    += clickHandler;
                lblCount.Click += clickHandler;
                lblName.Click  += clickHandler;

                pill.Controls.Add(lblCount);
                pill.Controls.Add(lblName);
                pnlKpi.Controls.Add(pill);
                x += PillW + Gap;
            }
        }

        // ── Button state ─────────────────────────────────────────────────────────────────
        private void UpdateActionButtons()
        {
            bool sel = dgvOrders.SelectedRows.Count > 0;
            btnViewDetail.Enabled  = sel;
            btnModifyOrder.Enabled = sel;
        }

        // ── DGV events ───────────────────────────────────────────────────────────────────
        private void dgvOrders_SelectionChanged(object sender, EventArgs e)
            => UpdateActionButtons();

        private void dgvOrders_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvOrders.Columns[e.ColumnIndex].Name != "colStatus" || e.Value == null) return;
            string status = e.Value.ToString();
            if (StatusColors.TryGetValue(status, out var colors))
            {
                e.CellStyle.ForeColor          = colors.fg;
                e.CellStyle.BackColor          = colors.bg;
                e.CellStyle.SelectionForeColor = colors.fg;
                e.CellStyle.SelectionBackColor = colors.bg;
                e.CellStyle.Font      = new Font("Segoe UI", 11f, FontStyle.Bold);
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }

        private void dgvOrders_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            OpenDetailDialog();
        }

        // ── Helper ───────────────────────────────────────────────────────────────────────
        private string SelectedOrderId()
        {
            if (dgvOrders.SelectedRows.Count == 0) return null;
            return dgvOrders.SelectedRows[0].Cells["colOrderID"].Value?.ToString();
        }

        // ── View Details ─────────────────────────────────────────────────────────────────
        private void btnViewDetail_Click(object sender, EventArgs e) => OpenDetailDialog();

        private void OpenDetailDialog()
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

        // ── Modify Order ─────────────────────────────────────────────────────────────────
        private void btnModifyOrder_Click(object sender, EventArgs e)
        {
            string id = SelectedOrderId();
            if (id == null) return;
            ModifyOrderForm.PendingOrderId = id;
            FormNavigator.NavigateTo(this, "Order Processing", "Modify Order");
        }

        // ── Detail dialog ─────────────────────────────────────────────────────────────────
        private void ShowDetailDialog(OrderDetailViewModel detail)
        {
            var o = detail.Order;

            using var dlg = new Form
            {
                Text            = $"Order Detail — {o.OrderID}",
                Size            = new Size(860, 680),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 13f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false
            };

            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Color.FromArgb(19, 35, 61) };
            var lblDialogTitle = new Label
            {
                Text      = $"Order Details  —  {o.OrderID}",
                Font      = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White, AutoSize = true, Location = new Point(24, 20)
            };
            StatusColors.TryGetValue(o.OrderStatus ?? "", out var sc);
            var lblStatusBadge = new Label
            {
                Text      = o.OrderStatus ?? "Unknown",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = sc.fg != default ? sc.fg : Color.White,
                BackColor = sc.bg != default ? sc.bg : Color.FromArgb(80, 80, 80),
                AutoSize  = true, Location = new Point(440, 22), Padding = new Padding(8, 4, 8, 4)
            };
            pnlHeader.Controls.Add(lblDialogTitle);
            pnlHeader.Controls.Add(lblStatusBadge);

            var pnlInfo = new Panel { Dock = DockStyle.Top, Height = 210, Padding = new Padding(24, 16, 24, 0), BackColor = Color.White };
            pnlInfo.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawLine(pen, 24, ((Panel)s).Height - 1, ((Panel)s).Width - 24, ((Panel)s).Height - 1);
            };
            var fields = new[]
            {
                ("Order No.",     o.OrderID),       ("Customer",      o.CustomerName),
                ("Sales Staff",   o.SalesName),     ("Contact",       o.OrderContactName),
                ("Issued Date",   o.IssuedTime.ToString("yyyy-MM-dd")),
                ("Delivery Date", o.DeliveryDate.ToString("yyyy-MM-dd")),
                ("Grand Total",   $"HK$ {o.GrandTotal:N2}"),
                ("Shipping Addr", o.ShippingAddress),
                ("Billing Addr",  o.BillingAddress), ("Status", o.OrderStatus),
            };
            for (int i = 0; i < fields.Length; i++)
            {
                int col = i % 2; int row = i / 2;
                int fx = col == 0 ? 0 : 390;
                int fy = row * 38;
                var pRow = new Panel { Location = new Point(fx, fy), Width = 360, Height = 38 };
                pRow.Controls.Add(new Label { Text = fields[i].Item1, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(98, 112, 135), Width = 120, Location = new Point(0, 10), TextAlign = ContentAlignment.MiddleLeft });
                pRow.Controls.Add(new Label { Text = fields[i].Item2 ?? "—", Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(15, 31, 53), AutoSize = true, Location = new Point(124, 10) });
                pnlInfo.Controls.Add(pRow);
            }

            var pnlLineLabel = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = Color.FromArgb(246, 249, 255), Padding = new Padding(24, 0, 0, 0) };
            pnlLineLabel.Controls.Add(new Label { Text = "ORDER ITEMS", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(98, 112, 135), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft });
            pnlLineLabel.Paint += PaintBottomBorderStatic;

            var dgv = new DataGridView
            {
                ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(221, 227, 236), Font = new Font("Segoe UI", 12f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate = { Height = 40 }, Dock = DockStyle.Fill,
                ColumnHeadersHeight = 38, EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(246, 249, 255), ForeColor = Color.FromArgb(98, 112, 135), Font = new Font("Segoe UI", 10f, FontStyle.Bold), Padding = new Padding(10, 0, 0, 0) },
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.White, ForeColor = Color.FromArgb(15, 31, 53), SelectionBackColor = Color.FromArgb(219, 234, 254), SelectionForeColor = Color.FromArgb(15, 31, 53), Padding = new Padding(10, 4, 10, 4) }
            };
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cItem",  HeaderText = "ITEM ID",    FillWeight = 18 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cName",  HeaderText = "ITEM NAME",  FillWeight = 42 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cQty",   HeaderText = "QTY",        FillWeight = 10 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cPrice", HeaderText = "UNIT PRICE", FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cTotal", HeaderText = "LINE TOTAL", FillWeight = 15 });
            foreach (var l in detail.Lines)
                dgv.Rows.Add(l.ItemID, l.ItemName, l.Quantity, $"HK$ {l.Price:N2}", $"HK$ {l.LineTotal:N2}");

            var pnlTotalRow = new Panel { Dock = DockStyle.Bottom, Height = 46, BackColor = Color.FromArgb(246, 249, 255), Padding = new Padding(0, 0, 24, 0) };
            pnlTotalRow.Controls.Add(new Label { Text = $"Grand Total:   HK$ {o.GrandTotal:N2}", Font = new Font("Segoe UI", 13f, FontStyle.Bold), ForeColor = Color.FromArgb(15, 31, 53), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight });

            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.White, Padding = new Padding(0, 10, 24, 10) };
            pnlFooter.Paint += PaintTopBorderStatic;
            var btnClose = new Button { Text = "Close", Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White, FlatStyle = FlatStyle.Flat, Dock = DockStyle.Right, Width = 110, Cursor = Cursors.Hand };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnClose.Click += (s, ev) => dlg.Close();
            pnlFooter.Controls.Add(btnClose);

            dlg.Controls.Add(dgv);
            dlg.Controls.Add(pnlTotalRow);
            dlg.Controls.Add(pnlLineLabel);
            dlg.Controls.Add(pnlInfo);
            dlg.Controls.Add(pnlHeader);
            dlg.Controls.Add(pnlFooter);
            dlg.ShowDialog(this);
        }

        // ── Static border painters ────────────────────────────────────────────────────────
        private static void PaintBottomBorderStatic(object s, PaintEventArgs e)
        { var p = (Panel)s; using var pen = new Pen(Color.FromArgb(221, 227, 236), 1); e.Graphics.DrawLine(pen, 0, p.Height-1, p.Width, p.Height-1); }
        private static void PaintTopBorderStatic(object s, PaintEventArgs e)
        { var p = (Panel)s; using var pen = new Pen(Color.FromArgb(221, 227, 236), 1); e.Graphics.DrawLine(pen, 0, 0, p.Width, 0); }

        // ── Geometry helper ───────────────────────────────────────────────────────────────
        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // ── Nav / Logout ──────────────────────────────────────────────────────────────────
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }
    }
}
