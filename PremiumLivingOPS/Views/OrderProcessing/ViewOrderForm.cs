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
    public partial class ViewOrderForm : Form
    {
        private readonly OrderProcessingController _ctrl = new OrderProcessingController();
        private List<OrderEntity> _currentOrders        = new List<OrderEntity>();

        private static readonly Dictionary<string, (Color bg, Color fg)> StatusColors =
            new Dictionary<string, (Color, Color)>
            {
                { "Pending",            (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "Processing",         (Color.FromArgb(219, 234, 254), Color.FromArgb( 29,  78, 216)) },
                { "Shipped",            (Color.FromArgb(224, 242, 254), Color.FromArgb(  3,  96, 170)) },
                { "Delivered",          (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
                { "Partially Delivered",(Color.FromArgb(237, 233, 254), Color.FromArgb( 91,  33, 182)) },
            };

        public ViewOrderForm()
        {
            InitializeComponent();
            this.Load += ViewOrderForm_Load;
        }

        private void ViewOrderForm_Load(object sender, EventArgs e)
        {
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            string orderNo      = txtSearchOrderNo.Text.Trim();
            string customer     = txtSearchCustomer.Text.Trim();
            string statusSelect = cboStatus.SelectedItem?.ToString();

            string statusFilter = (statusSelect == "All" || string.IsNullOrEmpty(statusSelect))
                                  ? null : statusSelect;

            DateTime? dateFrom = chkDateFrom.Checked ? (DateTime?)dtpDateFrom.Value.Date : null;

            string keyword = !string.IsNullOrEmpty(orderNo)  ? orderNo
                           : !string.IsNullOrEmpty(customer) ? customer
                           : null;

            var vm = _ctrl.GetViewOrderVM(statusFilter, keyword, dateFrom);

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

        private void ResetFilters()
        {
            txtSearchOrderNo.Text   = string.Empty;
            txtSearchCustomer.Text  = string.Empty;
            cboStatus.SelectedIndex = 0;
            chkDateFrom.Checked     = false;
            dtpDateFrom.Value       = DateTime.Today.AddMonths(-1);
            dtpDateFrom.Enabled     = false;
            RefreshGrid();
        }

        private void RefreshKpi()
        {
            pnlKpi.Controls.Clear();

            var allOrders = _ctrl.GetViewOrderVM().Orders;

            int total      = allOrders.Count;
            int pending    = allOrders.FindAll(o => o.OrderStatus == "Pending").Count;
            int processing = allOrders.FindAll(o => o.OrderStatus == "Processing").Count;
            int delivered  = allOrders.FindAll(o => o.OrderStatus == "Delivered").Count;
            int shipped    = allOrders.FindAll(o => o.OrderStatus == "Shipped").Count;
            int partially  = allOrders.FindAll(o => o.OrderStatus == "Partially Delivered").Count;

            var pills = new[]
            {
                ("Total",     total.ToString(),      Color.FromArgb( 47, 111, 237), Color.FromArgb(219, 234, 254), "All"),
                ("Pending",   pending.ToString(),    Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199), "Pending"),
                ("Processing",processing.ToString(), Color.FromArgb( 29,  78, 216), Color.FromArgb(219, 234, 254), "Processing"),
                ("Delivered", delivered.ToString(),  Color.FromArgb(  6,  95,  70), Color.FromArgb(209, 250, 229), "Delivered"),
                ("Shipped",   shipped.ToString(),    Color.FromArgb(  3,  96, 170), Color.FromArgb(224, 242, 254), "Shipped"),
                ("Partially", partially.ToString(),  Color.FromArgb( 91,  33, 182), Color.FromArgb(237, 233, 254), "Partially Delivered"),
            };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent,
                Padding = new Padding(0), AutoScroll = false
            };

            const int PillW = 270, PillH = 60, Gap = 8, NumColW = 65;

            foreach (var (label, count, fg, bg, filterItem) in pills)
            {
                var pill = new Panel { BackColor = bg, Size = new Size(PillW, PillH), Margin = new Padding(0, 0, Gap, 0), Cursor = Cursors.Hand };
                pill.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path  = RoundedRect(((Panel)s).ClientRectangle, 8);
                    using var brush = new SolidBrush(((Panel)s).BackColor);
                    e.Graphics.FillPath(brush, path);
                };

                var tlp = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                    BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding = new Padding(10, 0, 8, 0)
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, NumColW));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                tlp.Controls.Add(new Label { Text = count, Font = new Font("Segoe UI", 14f, FontStyle.Bold), ForeColor = fg, BackColor = Color.Transparent, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, AutoSize = false }, 0, 0);
                tlp.Controls.Add(new Label { Text = label, Font = new Font("Segoe UI", 12f), ForeColor = fg, BackColor = Color.Transparent, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false }, 1, 0);

                string localFilterItem = filterItem;
                EventHandler clickHandler = (s, e) =>
                {
                    int idx = cboStatus.FindStringExact(localFilterItem);
                    if (idx >= 0) cboStatus.SelectedIndex = idx;
                    RefreshGrid();
                };
                pill.Click += clickHandler; tlp.Click += clickHandler;
                foreach (Control c in tlp.Controls) c.Click += clickHandler;

                pill.Controls.Add(tlp);
                flow.Controls.Add(pill);
            }
            pnlKpi.Controls.Add(flow);
        }

        private void UpdateActionButtons()
        {
            bool sel = dgvOrders.SelectedRows.Count > 0;
            btnViewDetail.Enabled  = sel;
            btnModifyOrder.Enabled = sel;
        }

        private void dgvOrders_SelectionChanged(object sender, EventArgs e) => UpdateActionButtons();

        private void dgvOrders_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvOrders.Columns[e.ColumnIndex].Name != "colStatus" || e.Value == null) return;
            string dbValue = e.Value.ToString();
            e.FormattingApplied = true;
            if (StatusColors.TryGetValue(dbValue, out var colors))
            {
                e.CellStyle.ForeColor = colors.fg; e.CellStyle.BackColor = colors.bg;
                e.CellStyle.SelectionForeColor = colors.fg; e.CellStyle.SelectionBackColor = colors.bg;
                e.CellStyle.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }

        private void dgvOrders_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        { if (e.RowIndex >= 0) OpenDetailDialog(); }

        private string SelectedOrderId()
        {
            if (dgvOrders.SelectedRows.Count == 0) return null;
            return dgvOrders.SelectedRows[0].Cells["colOrderID"].Value?.ToString();
        }

        private void btnViewDetail_Click(object sender, EventArgs e) => OpenDetailDialog();

        private void OpenDetailDialog()
        {
            string id = SelectedOrderId();
            if (id == null) return;
            var detail = _ctrl.GetOrderDetail(id);
            if (detail?.Order == null)
            {
                MessageBox.Show("Order not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            ShowDetailDialog(detail);
        }

        private void btnModifyOrder_Click(object sender, EventArgs e)
        {
            string id = SelectedOrderId();
            if (id == null) return;
            ModifyOrderForm.PendingOrderId = id;
            FormNavigator.NavigateTo(this, "Order Processing", "Modify Order");
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Order Details dialog
        // ─────────────────────────────────────────────────────────────────────
        private void ShowDetailDialog(OrderDetailViewModel detail)
        {
            var o = detail.Order;

            using var dlg = new Form
            {
                Text            = $"Order Detail — {o.OrderID}",
                Size            = new Size(2200, 900),        // fixed at requested size
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 13f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false
            };

            // ── Header bar
            // Use a TLP so title takes all remaining space and badge sits on the right
            // without ever overlapping the title text.
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };

            var tblHeader = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                RowCount    = 1,
                BackColor   = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding     = new Padding(24, 0, 24, 0)
            };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));  // title — expands
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f)); // badge — fixed
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var lblDialogTitle = new Label
            {
                Text      = $"Order Details  —  {o.OrderID}",
                Font      = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false
            };

            StatusColors.TryGetValue(o.OrderStatus ?? "", out var sc);
            var lblStatusBadge = new Label
            {
                Text      = o.OrderStatus ?? "Unknown",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = sc.fg != default ? sc.fg : Color.White,
                BackColor = sc.bg != default ? sc.bg : Color.FromArgb(80, 80, 80),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize  = false,
                Padding   = new Padding(8, 4, 8, 4)
            };

            tblHeader.Controls.Add(lblDialogTitle, 0, 0);
            tblHeader.Controls.Add(lblStatusBadge, 1, 0);
            pnlHeader.Controls.Add(tblHeader);

            // ── Info panel  (5 rows x 2 col pairs)
            var pnlInfo = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 260,
                Padding   = new Padding(28, 18, 28, 0),
                BackColor = Color.White
            };
            pnlInfo.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawLine(pen, 28, ((Panel)s).Height - 1, ((Panel)s).Width - 28, ((Panel)s).Height - 1);
            };

            var fields = new[]
            {
                ("Order No.",     o.OrderID),
                ("Customer",      o.CustomerName),
                ("Sales Staff",   o.SalesName),
                ("Contact",       o.OrderContactName),
                ("Issued Date",   o.IssuedTime.ToString("yyyy-MM-dd")),
                ("Delivery Date", o.DeliveryDate.ToString("yyyy-MM-dd")),
                ("Grand Total",   $"HK$ {o.GrandTotal:N2}"),
                ("Shipping Addr", o.ShippingAddress),
                ("Billing Addr",  o.BillingAddress),
                ("Status",        o.OrderStatus),
            };

            var tblInfo = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 5,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  50f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  50f));
            for (int r = 0; r < 5; r++)
                tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));

            for (int i = 0; i < fields.Length; i++)
            {
                int col = (i % 2) * 2;
                int row = i / 2;
                tblInfo.Controls.Add(new Label { Text = fields[i].Item1, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(98, 112, 135), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(0, 0, 8, 0) }, col, row);
                tblInfo.Controls.Add(new Label { Text = fields[i].Item2 ?? "—", Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(15, 31, 53), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true }, col + 1, row);
            }
            pnlInfo.Controls.Add(tblInfo);

            // ── Section label
            var pnlLineLabel = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.FromArgb(246, 249, 255), Padding = new Padding(28, 0, 0, 0) };
            pnlLineLabel.Controls.Add(new Label { Text = "ORDER ITEMS", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(98, 112, 135), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft });
            pnlLineLabel.Paint += PaintBottomBorderStatic;

            // ── Items grid
            var dgv = new DataGridView
            {
                ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(221, 227, 236), Font = new Font("Segoe UI", 12f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate = { Height = 44 }, Dock = DockStyle.Fill,
                ColumnHeadersHeight = 40, EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(246, 249, 255), ForeColor = Color.FromArgb(98, 112, 135), Font = new Font("Segoe UI", 10f, FontStyle.Bold), Padding = new Padding(12, 0, 0, 0) },
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.White, ForeColor = Color.FromArgb(15, 31, 53), SelectionBackColor = Color.FromArgb(219, 234, 254), SelectionForeColor = Color.FromArgb(15, 31, 53), Padding = new Padding(12, 6, 12, 6) }
            };
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cItem",  HeaderText = "ITEM ID",    FillWeight = 18 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cName",  HeaderText = "ITEM NAME",  FillWeight = 42 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cQty",   HeaderText = "QTY",        FillWeight = 10 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cPrice", HeaderText = "UNIT PRICE", FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cTotal", HeaderText = "LINE TOTAL", FillWeight = 15 });
            foreach (var l in detail.Lines)
                dgv.Rows.Add(l.ItemID, l.ItemName, l.Quantity, $"HK$ {l.Price:N2}", $"HK$ {l.LineTotal:N2}");

            // ── Grand total row
            var pnlTotalRow = new Panel { Dock = DockStyle.Bottom, Height = 50, BackColor = Color.FromArgb(246, 249, 255), Padding = new Padding(0, 0, 28, 0) };
            pnlTotalRow.Controls.Add(new Label { Text = $"Grand Total:   HK$ {o.GrandTotal:N2}", Font = new Font("Segoe UI", 13f, FontStyle.Bold), ForeColor = Color.FromArgb(15, 31, 53), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight });

            // ── Footer with Close button
            // Height 80 px; Padding top+bottom 10 px each → button gets 60 px usable height
            var pnlFooter = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 80,
                BackColor = Color.White,
                Padding   = new Padding(0, 10, 28, 10)   // top=10, bottom=10 → 60 px for button
            };
            pnlFooter.Paint += PaintTopBorderStatic;
            var btnClose = new Button
            {
                Text      = "Close",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock      = DockStyle.Right,
                Width     = 140,
                Cursor    = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnClose.Click += (s, ev) => dlg.Close();
            pnlFooter.Controls.Add(btnClose);

            // ── Assemble
            dlg.Controls.Add(dgv);           // Fill
            dlg.Controls.Add(pnlTotalRow);   // Bottom
            dlg.Controls.Add(pnlLineLabel);  // Top (last Top = renders below pnlInfo)
            dlg.Controls.Add(pnlInfo);       // Top
            dlg.Controls.Add(pnlHeader);     // Top (first)
            dlg.Controls.Add(pnlFooter);     // Bottom (first bottom)
            dlg.ShowDialog(this);
        }

        private static void PaintBottomBorderStatic(object s, PaintEventArgs e)
        { var p = (Panel)s; using var pen = new Pen(Color.FromArgb(221, 227, 236), 1); e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1); }

        private static void PaintTopBorderStatic(object s, PaintEventArgs e)
        { var p = (Panel)s; using var pen = new Pen(Color.FromArgb(221, 227, 236), 1); e.Graphics.DrawLine(pen, 0, 0, p.Width, 0); }

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

        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }
    }
}
