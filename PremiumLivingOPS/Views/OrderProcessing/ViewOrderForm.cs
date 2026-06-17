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
                { "Delivered",          (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
                { "Partially Delivered",(Color.FromArgb(237, 233, 254), Color.FromArgb( 91,  33, 182)) },
                { "Cancelled",          (Color.FromArgb(254, 226, 226), Color.FromArgb(185,  28,  28)) },
                { "Completed",          (Color.FromArgb(220, 252, 231), Color.FromArgb( 22, 101,  52)) },
            };

        private static string StatusBadgeText(string status)
            => status == "Partially Delivered" ? "Partially" : (status ?? "Unknown");

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
            _shell.SetBreadcrumb("Order Processing  \u203a  View Order");

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
            int partially  = allOrders.FindAll(o => o.OrderStatus == "Partially Delivered").Count;
            int cancelled  = allOrders.FindAll(o => o.OrderStatus == "Cancelled").Count;
            int completed  = allOrders.FindAll(o => o.OrderStatus == "Completed").Count;

            var pills = new[]
            {
                ("Total",     total.ToString(),      Color.FromArgb( 47, 111, 237), Color.FromArgb(219, 234, 254), "All"),
                ("Pending",   pending.ToString(),    Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199), "Pending"),
                ("Processing",processing.ToString(), Color.FromArgb( 29,  78, 216), Color.FromArgb(219, 234, 254), "Processing"),
                ("Delivered", delivered.ToString(),  Color.FromArgb(  6,  95,  70), Color.FromArgb(209, 250, 229), "Delivered"),
                ("Partially", partially.ToString(),  Color.FromArgb( 91,  33, 182), Color.FromArgb(237, 233, 254), "Partially Delivered"),
                ("Cancelled", cancelled.ToString(),  Color.FromArgb(185,  28,  28), Color.FromArgb(254, 226, 226), "Cancelled"),
                ("Completed", completed.ToString(),  Color.FromArgb( 22, 101,  52), Color.FromArgb(220, 252, 231), "Completed"),
            };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent,
                Padding = new Padding(0), AutoScroll = false
            };

            const int PillW   = 290;
            const int PillH   = 60;
            const int Gap     = 8;
            const int NumColW = 80;

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

        private void btnModifyOrder_Click(object sender, EventArgs e)
        {
            string id = SelectedOrderId();
            if (id == null) return;

            ModifyOrderForm.PendingOrderId = id;
            FormNavigator.NavigateTo(this, "Order Processing", "Modify Order");
        }

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

        private void ShowDetailDialog(OrderDetailViewModel detail)
        {
            var o = detail.Order;
            bool hasDiscount = !string.IsNullOrWhiteSpace(o.DiscountType);

            using var dlg = new Form
            {
                Text            = $"Order Detail \u2014 {o.OrderID}",
                Size            = new Size(2500, 1100),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 13f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false
            };

            // ── Header ────────────────────────────────────────────────────────
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            var tblHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(24, 0, 24, 0)
            };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 264f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            tblHeader.Controls.Add(new Label
            {
                Text = $"Order Details  \u2014  {o.OrderID}",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);

            StatusColors.TryGetValue(o.OrderStatus ?? "", out var sc);
            tblHeader.Controls.Add(new Label
            {
                Text      = StatusBadgeText(o.OrderStatus),
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = sc.fg != default ? sc.fg : Color.White,
                BackColor = sc.bg != default ? sc.bg : Color.FromArgb(80, 80, 80),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false, Padding = new Padding(8, 4, 8, 4)
            }, 1, 0);
            pnlHeader.Controls.Add(tblHeader);

            // ── Info panel ────────────────────────────────────────────────────
            var pnlInfo = new Panel
            {
                Dock = DockStyle.Top, Height = 400,
                Padding = new Padding(28, 18, 28, 8), BackColor = Color.White
            };
            pnlInfo.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawLine(pen, 28, ((Panel)s).Height - 1, ((Panel)s).Width - 28, ((Panel)s).Height - 1);
            };

            var tblInfo = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 6,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));

            tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 14f));
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 14f));
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 14f));
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 14f));
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 30f));
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 14f));

            var leftFields = new[]
            {
                ("Order ID",        o.OrderID),
                ("Sales Name",      o.SalesName),
                ("Issued Date",     o.IssuedTime.ToString("yyyy-MM-dd")),
                ("Delivery Date",   o.DeliveryDate.ToString("yyyy-MM-dd")),
                ("Billing Address", o.BillingAddress),
                ("Address ID",      string.IsNullOrWhiteSpace(o.AddressID) ? "\u2014" : o.AddressID),
            };
            for (int i = 0; i < leftFields.Length; i++)
            {
                tblInfo.Controls.Add(MakeLabelKey(leftFields[i].Item1), 0, i);
                tblInfo.Controls.Add(
                    i == 4 ? MakeLabelValMultiLine(leftFields[i].Item2 ?? "\u2014")
                           : MakeLabelVal(leftFields[i].Item2 ?? "\u2014"),
                    1, i);
            }

            var rightFields = new[]
            {
                ("Quotation ID",     string.IsNullOrWhiteSpace(o.QuotationID) ? "\u2014" : o.QuotationID, false),
                ("Customer Name",    o.CustomerName,                                            false),
                ("Contact Name",     o.OrderContactName,                                        false),
                ("Order Status",     o.OrderStatus,                                             false),
                ("Shipping Address", o.ShippingAddress,                                         true ),
                ("Grand Total",      $"HK$ {o.GrandTotal:N2}",                                 false),
            };
            for (int i = 0; i < rightFields.Length; i++)
            {
                tblInfo.Controls.Add(MakeLabelKey(rightFields[i].Item1), 2, i);
                tblInfo.Controls.Add(
                    rightFields[i].Item3 ? MakeLabelValMultiLine(rightFields[i].Item2 ?? "\u2014")
                                        : MakeLabelVal(rightFields[i].Item2 ?? "\u2014"),
                    3, i);
            }
            pnlInfo.Controls.Add(tblInfo);

            // ── Discount bar (optional) ───────────────────────────────────────
            Panel pnlDiscount = null;
            if (hasDiscount)
            {
                pnlDiscount = new Panel
                {
                    Dock = DockStyle.Top, Height = 60,
                    Padding = new Padding(28, 0, 28, 0), BackColor = Color.FromArgb(255, 251, 235)
                };
                pnlDiscount.Paint += PaintBottomBorderStatic;

                var tblDisc = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill, ColumnCount = 6, RowCount = 1,
                    BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
                };
                tblDisc.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12f));
                tblDisc.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 21.3f));
                tblDisc.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12f));
                tblDisc.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 21.3f));
                tblDisc.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12f));
                tblDisc.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 21.4f));
                tblDisc.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                tblDisc.Controls.Add(MakeLabelKey("Discount Type"),                0, 0);
                tblDisc.Controls.Add(MakeLabelVal(o.DiscountType),                 1, 0);
                tblDisc.Controls.Add(MakeLabelKey("Discount Value"),               2, 0);
                tblDisc.Controls.Add(MakeLabelVal(o.DiscountValue.ToString("N2")), 3, 0);
                tblDisc.Controls.Add(MakeLabelKey("Discount Amount"),              4, 0);
                tblDisc.Controls.Add(MakeLabelVal($"HK$ {o.DiscountAmount:N2}"),   5, 0);
                pnlDiscount.Controls.Add(tblDisc);
            }

            // ── "ORDER ITEMS" section label ───────────────────────────────────
            var pnlLineLabel = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.FromArgb(246, 249, 255), Padding = new Padding(28, 0, 0, 0) };
            pnlLineLabel.Controls.Add(new Label { Text = "ORDER ITEMS", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(98, 112, 135), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft });
            pnlLineLabel.Paint += PaintBottomBorderStatic;

            // ── Order items DGV (Fill) ────────────────────────────────────────
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

            // ── Subtotal / Grand Total bar ─────────────────────────────────────
            // Root cause of the original bug:
            // Using DockStyle.Left + DockStyle.Right labels inside a DockStyle.Bottom
            // panel causes one label to be clipped or hidden entirely because WinForms
            // processes Left/Right dock AFTER Fill, leaving no guaranteed space.
            //
            // Fix: Replace with a TableLayoutPanel (3 columns) so Subtotal and
            // Grand Total are placed in absolute-width cells — no dock competition.
            var pnlTotalRow = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 64,
                BackColor = Color.White,
                Padding   = new Padding(28, 0, 28, 0)
            };
            pnlTotalRow.Paint += PaintTopBorderStatic;

            var tblTotals = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 3,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            // Left cell  — Subtotal (fixed width)
            // Middle cell — spacer (fills remaining space)
            // Right cell  — Grand Total (fixed width)
            tblTotals.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 400f));
            tblTotals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblTotals.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 480f));
            tblTotals.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            tblTotals.Controls.Add(new Label
            {
                Text      = $"Subtotal:   HK$ {o.SubTotal:N2}",
                Dock      = DockStyle.Fill,
                AutoSize  = false,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);

            // Spacer cell — intentionally empty
            tblTotals.Controls.Add(new Label { Dock = DockStyle.Fill, AutoSize = false }, 1, 0);

            tblTotals.Controls.Add(new Label
            {
                Text      = $"Grand Total:   HK$ {o.GrandTotal:N2}",
                Dock      = DockStyle.Fill,
                AutoSize  = false,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(47, 111, 237),
                TextAlign = ContentAlignment.MiddleRight
            }, 2, 0);

            pnlTotalRow.Controls.Add(tblTotals);

            // ── Footer ────────────────────────────────────────────────────────
            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 86, BackColor = Color.White, Padding = new Padding(28, 14, 28, 14) };
            pnlFooter.Paint += PaintTopBorderStatic;
            var btnClose = new Button
            {
                Text      = "Close",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(150, 44),
                Anchor    = AnchorStyles.Right | AnchorStyles.Top,
                Location  = new Point(2500 - 28 - 150 - 16, 14),
                Cursor    = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            btnClose.FlatAppearance.BorderSize = 1;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnClose.Click += (s, ev) => dlg.Close();
            pnlFooter.Controls.Add(btnClose);

            // ── Assemble ──────────────────────────────────────────────────────
            // DockStyle.Bottom rule: controls added LATER claim the bottom edge
            // first (innermost). Correct visual order from bottom to top:
            //   pnlFooter    (very bottom)     → add first  among Bottom panels
            //   pnlTotalRow  (above footer)    → add second among Bottom panels
            //   dgv          (Fill — expands)  → add before any Top panels
            //   pnlLineLabel (Top)
            //   pnlDiscount  (Top, optional)
            //   pnlInfo      (Top)
            //   pnlHeader    (Top, topmost)    → add last   among Top panels
            dlg.Controls.Add(dgv);
            dlg.Controls.Add(pnlFooter);
            dlg.Controls.Add(pnlTotalRow);
            dlg.Controls.Add(pnlLineLabel);
            if (hasDiscount)
                dlg.Controls.Add(pnlDiscount);
            dlg.Controls.Add(pnlInfo);
            dlg.Controls.Add(pnlHeader);
            dlg.ShowDialog(this);
        }

        private static Label MakeLabelKey(string text) => new Label
        {
            Text = text, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(98, 112, 135), Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(0, 0, 8, 0),
            AutoEllipsis = false
        };

        private static Label MakeLabelVal(string text) => new Label
        {
            Text = text, Font = new Font("Segoe UI", 12f),
            ForeColor = Color.FromArgb(15, 31, 53), Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true
        };

        private static Label MakeLabelValMultiLine(string text) => new Label
        {
            Text         = text,
            Font         = new Font("Segoe UI", 12f),
            ForeColor    = Color.FromArgb(15, 31, 53),
            Dock         = DockStyle.Fill,
            TextAlign    = ContentAlignment.TopLeft,
            AutoEllipsis = false,
            AutoSize     = false,
            Padding      = new Padding(0, 8, 8, 4)
        };

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
