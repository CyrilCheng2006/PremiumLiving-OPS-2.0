// ViewOrderForm.cs — View layer for Order Processing > View Order tab
// Follows MVC: all data retrieval delegated to OrderProcessingController.
// UI only: bind ViewModel data, handle user events.
using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.OrderProcessing
{
    public partial class ViewOrderForm : Form
    {
        // ── Fields ───────────────────────────────────────────────────────────
        private readonly OrderProcessingController _ctrl = new OrderProcessingController();
        private AppShell _shell;

        // KPI pill buttons keyed by status string
        private readonly Dictionary<string, Button> _kpiButtons = new Dictionary<string, Button>();

        // Tag colours matching order-list.html
        private static readonly Dictionary<string, (Color bg, Color fg)> StatusColors =
            new Dictionary<string, (Color, Color)>(StringComparer.OrdinalIgnoreCase)
            {
                { "Pending",    (Color.FromArgb(254, 243, 199), Color.FromArgb(146, 64,  14))  },
                { "Processing", (Color.FromArgb(219, 234, 254), Color.FromArgb( 30, 64, 175))  },
                { "Delivered",  (Color.FromArgb(209, 250, 229), Color.FromArgb( 22,101,  52))  },
                { "Cancelled",  (Color.FromArgb(254, 226, 226), Color.FromArgb(185, 28,  28))  }
            };

        // ── Constructor ──────────────────────────────────────────────────────
        public ViewOrderForm()
        {
            InitializeComponent();
            WireEvents();
        }

        // ── Initialisation ───────────────────────────────────────────────────
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            AttachShell();
            BuildKpiBar();
            ExecuteSearch();
        }

        private void AttachShell()
        {
            _shell = new AppShell { Dock = DockStyle.Fill };
            pnlShell.Controls.Add(_shell);

            var vm = _ctrl.GetViewOrderVM();
            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Order Processing", "View Orders");

            // Replace pnlContent as child of shell's content area
            _shell.SetContentPanel(pnlContent);
        }

        /// <summary>Builds the five KPI status pills.</summary>
        private void BuildKpiBar()
        {
            pnlKpi.Controls.Clear();
            _kpiButtons.Clear();

            var pills = new[]
            {
                ("All",        Color.FromArgb(229, 231, 235), Color.FromArgb(55, 65, 81)),
                ("Pending",    Color.FromArgb(254, 243, 199), Color.FromArgb(146, 64, 14)),
                ("Processing", Color.FromArgb(219, 234, 254), Color.FromArgb(30, 64, 175)),
                ("Delivered",  Color.FromArgb(209, 250, 229), Color.FromArgb(22, 101, 52)),
                ("Cancelled",  Color.FromArgb(254, 226, 226), Color.FromArgb(185, 28, 28))
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

        // ── Wire events ──────────────────────────────────────────────────────
        private void WireEvents()
        {
            btnSearch.Click      += (s, e) => ExecuteSearch();
            btnClear.Click       += (s, e) => ClearSearch();
            btnCreateOrder.Click += (s, e) => OpenCreateOrder();

            // Enable/disable DateTimePickers based on checkbox
            chkDateFrom.CheckedChanged += (s, e) => dtpDateFrom.Enabled = chkDateFrom.Checked;
            chkDateTo.CheckedChanged   += (s, e) => dtpDateTo.Enabled   = chkDateTo.Checked;

            // Allow pressing Enter in search boxes to trigger search
            txtOrderNo.KeyDown  += SearchBox_KeyDown;
            txtCustomer.KeyDown += SearchBox_KeyDown;

            dgvOrders.CellClick          += dgvOrders_CellClick;
            dgvOrders.CellFormatting     += dgvOrders_CellFormatting;
            dgvOrders.DoubleClick        += (s, e) => OpenDetailFromSelection();
            dgvOrders.CellPainting       += dgvOrders_CellPainting;
        }

        // ── Search helpers ───────────────────────────────────────────────────

        /// <summary>Reads all search controls and triggers a DB query via Controller.</summary>
        private void ExecuteSearch()
        {
            string keyword = BuildKeyword();
            string status  = cboStatus.SelectedItem?.ToString() == "All"
                             ? null : cboStatus.SelectedItem?.ToString();

            DateTime? dateFrom = chkDateFrom.Checked ? (DateTime?)dtpDateFrom.Value.Date : null;
            DateTime? dateTo   = chkDateTo.Checked   ? (DateTime?)dtpDateTo.Value.Date   : null;

            // Validate date range
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

        /// <summary>Combines Order No. + Customer fields into a single keyword string.</summary>
        private string BuildKeyword()
        {
            string kw = "";
            if (!string.IsNullOrWhiteSpace(txtOrderNo.Text))
                kw += txtOrderNo.Text.Trim();
            if (!string.IsNullOrWhiteSpace(txtCustomer.Text))
                kw += (kw.Length > 0 ? " " : "") + txtCustomer.Text.Trim();
            return kw.Length > 0 ? kw : null;
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

        // ── KPI pill click ───────────────────────────────────────────────────
        private void KpiPill_Click(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is string status)
            {
                cboStatus.SelectedItem = status;
                ExecuteSearch();
            }
        }

        /// <summary>Updates each KPI pill's count badge from the current result set.</summary>
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

            // Update result count label
            lblResultCount.Text = $"{orders.Count} order{(orders.Count != 1 ? "s" : "")} found";
        }

        // ── Grid binding ─────────────────────────────────────────────────────
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

        // ── Grid events ──────────────────────────────────────────────────────
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
                var status = e.Value.ToString();
                if (StatusColors.TryGetValue(status, out var c))
                {
                    e.CellStyle.BackColor  = c.bg;
                    e.CellStyle.ForeColor  = c.fg;
                    e.CellStyle.Font       = new Font("Segoe UI", 9F, FontStyle.Bold);
                    e.CellStyle.Alignment  = DataGridViewContentAlignment.MiddleCenter;
                }
            }

            // Alternate row tinting (excluding status column)
            if (col.Name != "colStatus" && e.RowIndex % 2 == 1 &&
                !dgvOrders.Rows[e.RowIndex].Selected)
            {
                e.CellStyle.BackColor = Color.FromArgb(249, 250, 252);
            }
        }

        /// <summary>Custom paint for the "View Details" button column.</summary>
        private void dgvOrders_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || dgvOrders.Columns[e.ColumnIndex].Name != "colAction") return;

            e.Paint(e.ClipBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);

            var btnRect = new Rectangle(
                e.CellBounds.X + 8, e.CellBounds.Y + 8,
                e.CellBounds.Width - 16, e.CellBounds.Height - 16);

            using (var brush = new SolidBrush(Color.FromArgb(37, 99, 235)))
            using (var fnt   = new Font("Segoe UI", 8.5F, FontStyle.Bold))
            using (var pen   = new Pen(Color.FromArgb(37, 99, 235)))
            {
                using (var path = RoundedRect(btnRect, 6))
                    e.Graphics.FillPath(brush, path);

                var sf = new System.Drawing.StringFormat
                {
                    Alignment     = System.Drawing.StringAlignment.Center,
                    LineAlignment = System.Drawing.StringAlignment.Center
                };
                e.Graphics.DrawString("View Details", fnt, Brushes.White, btnRect, sf);
            }
            e.Handled = true;
        }

        private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(r.X, r.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(r.Right - radius * 2, r.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(r.Right - radius * 2, r.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(r.X, r.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }

        // ── Detail Dialog ────────────────────────────────────────────────────
        private void ShowDetailDialog(string orderId)
        {
            if (string.IsNullOrEmpty(orderId)) return;
            var detail = _ctrl.GetOrderDetail(orderId);
            if (detail?.Order == null) return;

            var o = detail.Order;

            using (var dlg = new Form())
            {
                dlg.Text          = "Order Details";
                dlg.Size          = new Size(720, 620);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox   = false;
                dlg.MinimizeBox   = false;
                dlg.BackColor     = Color.White;

                // Header band
                var header = new Panel
                {
                    Dock      = DockStyle.Top,
                    Height    = 56,
                    BackColor = Color.FromArgb(15, 23, 42)
                };
                var (hBg, hFg) = StatusColors.TryGetValue(o.OrderStatus, out var sc)
                                 ? sc : (Color.Gray, Color.White);
                var lblTitle = new Label
                {
                    Text      = $"Order Details  —  {o.OrderID}",
                    ForeColor = Color.White,
                    Font      = new Font("Segoe UI", 12F, FontStyle.Bold),
                    Dock      = DockStyle.Left,
                    AutoSize  = false,
                    Width     = 420,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding   = new Padding(20, 0, 0, 0)
                };
                var lblStatusBadge = new Label
                {
                    Text      = o.OrderStatus,
                    BackColor = hBg,
                    ForeColor = hFg,
                    Font      = new Font("Segoe UI", 9F, FontStyle.Bold),
                    AutoSize  = false,
                    Width     = 90,
                    Height    = 28,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Anchor    = AnchorStyles.Top | AnchorStyles.Right
                };
                lblStatusBadge.Location = new Point(dlg.ClientSize.Width - 120, 14);
                header.Controls.Add(lblTitle);
                header.Controls.Add(lblStatusBadge);

                // Info grid
                var tbl = new TableLayoutPanel
                {
                    Dock        = DockStyle.Top,
                    Height      = 160,
                    ColumnCount  = 4,
                    RowCount     = 3,
                    BackColor    = Color.White,
                    Padding      = new Padding(20, 12, 20, 4)
                };
                for (int i = 0; i < 4; i++)
                    tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

                void AddInfo(int col, int row, string lbl, string val)
                {
                    var lbLbl = new Label { Text = lbl, Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                                           ForeColor = Color.FromArgb(107,114,128), Dock = DockStyle.Fill,
                                           TextAlign = ContentAlignment.BottomLeft };
                    var lbVal = new Label { Text = val, Font = new Font("Segoe UI", 9.5F),
                                           ForeColor = Color.FromArgb(17,24,39), Dock = DockStyle.Fill,
                                           TextAlign = ContentAlignment.TopLeft };
                    tbl.Controls.Add(lbLbl, col, row * 2);
                    tbl.Controls.Add(lbVal, col, row * 2 + 1);
                }

                tbl.RowCount = 6;
                for (int i = 0; i < 6; i++)
                    tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, i % 2 == 0 ? 18F : 26F));

                AddInfo(0, 0, "ORDER NO.",    o.OrderID);
                AddInfo(1, 0, "CUSTOMER",     o.CustomerName);
                AddInfo(2, 0, "SALES REP",    o.SalesName);
                AddInfo(3, 0, "ORDER DATE",   o.IssuedTime.ToString("dd MMM yyyy"));
                AddInfo(0, 1, "DELIVERY DATE",o.DeliveryDate.ToString("dd MMM yyyy"));
                AddInfo(1, 1, "CONTACT",      o.OrderContactName);
                AddInfo(2, 1, "SHIPPING ADDR",o.ShippingAddress);
                AddInfo(3, 1, "BILLING ADDR", o.BillingAddress);

                // Divider
                var div = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(229,231,235), Margin = new Padding(20,0,20,0) };

                // Items section label
                var lblItems = new Label
                {
                    Text      = "ORDER ITEMS",
                    Dock      = DockStyle.Top,
                    Height    = 32,
                    Font      = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(37, 99, 235),
                    Padding   = new Padding(20, 8, 0, 0)
                };

                // Items grid
                var dgv = new DataGridView
                {
                    Dock                = DockStyle.Fill,
                    BackgroundColor     = Color.White,
                    BorderStyle         = BorderStyle.None,
                    RowHeadersVisible   = false,
                    AllowUserToAddRows  = false,
                    ReadOnly            = true,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    SelectionMode       = DataGridViewSelectionMode.FullRowSelect,
                    ColumnHeadersDefaultCellStyle = { BackColor = Color.FromArgb(246,249,255),
                                                      ForeColor = Color.FromArgb(107,114,128),
                                                      Font      = new Font("Segoe UI", 8F, FontStyle.Bold) },
                    EnableHeadersVisualStyles = false
                };
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cItemID",   HeaderText = "ITEM ID",   FillWeight = 20 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cItemName", HeaderText = "ITEM NAME", FillWeight = 40 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cQty",      HeaderText = "QTY",       FillWeight = 15 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cPrice",    HeaderText = "UNIT PRICE",FillWeight = 15 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cTotal",    HeaderText = "TOTAL",     FillWeight = 15 });

                double grandTotal = 0;
                foreach (var line in detail.Lines)
                {
                    double lineTotal = line.Quantity * line.Price;
                    grandTotal += lineTotal;
                    dgv.Rows.Add(line.ItemID, line.ItemName, line.Quantity,
                                 $"HK$ {line.Price:N2}", $"HK$ {lineTotal:N2}");
                }

                // Grand total row
                var pnlGT = new Panel { Dock = DockStyle.Bottom, Height = 36, BackColor = Color.FromArgb(249,250,252) };
                var lblGT = new Label
                {
                    Text      = $"Grand Total:    HK$ {o.GrandTotal:N2}",
                    Font      = new Font("Segoe UI", 10F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(17, 24, 39),
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleRight,
                    Padding   = new Padding(0, 0, 20, 0)
                };
                pnlGT.Controls.Add(lblGT);

                // Close button
                var pnlFoot = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.White,
                                          Padding = new Padding(0, 8, 20, 0) };
                var btnClose = new Button
                {
                    Text      = "Close",
                    Width     = 90, Height = 32,
                    BackColor = Color.FromArgb(243,244,246),
                    ForeColor = Color.FromArgb(55,65,81),
                    FlatStyle = FlatStyle.Flat,
                    Font      = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    Anchor    = AnchorStyles.Top | AnchorStyles.Right,
                    Cursor    = Cursors.Hand
                };
                btnClose.FlatAppearance.BorderColor = Color.FromArgb(209,213,219);
                btnClose.FlatAppearance.BorderSize  = 1;
                btnClose.Location = new Point(dlg.ClientSize.Width - 110, 8);
                btnClose.Click   += (_, __) => dlg.Close();
                pnlFoot.Controls.Add(btnClose);

                // Assemble — note DockStyle.Top stacks bottom-up so reverse order
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

        // ── Navigation helpers ───────────────────────────────────────────────
        private void OpenCreateOrder()
        {
            var frm = new CreateOrderForm();
            frm.ShowDialog(this);
            ExecuteSearch();   // Refresh after creation
        }
    }
}
