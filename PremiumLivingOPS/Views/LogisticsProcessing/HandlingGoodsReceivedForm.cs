using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    /// <summary>
    /// Logistics Processing – Handling Goods Received page.
    ///
    /// MVC contract
    /// ─────────────────────────────────────────────────────────────────
    /// • All DB access is delegated to LogisticsProcessingController.
    /// • This class contains NO SQL and NO business logic.
    /// • AppShell provides TopNavBar + UserBar (identical pattern to ViewOrderForm).
    /// • CardPanel three-layer nesting wraps every content block.
    /// • KPI pills count PO status breakdowns; clicking filters the Receipts grid.
    /// </summary>
    public partial class HandlingGoodsReceivedForm : Form
    {
        private readonly LogisticsProcessingController _ctrl =
            new LogisticsProcessingController();

        // ── Status colour map (bg, fg) ────────────────────────────────────
        private static readonly Dictionary<string, (Color bg, Color fg)> StatusColors =
            new Dictionary<string, (Color, Color)>
            {
                { "Sent",               (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "Partially Received", (Color.FromArgb(219, 234, 254), Color.FromArgb( 29,  78, 216)) },
                { "Received",           (Color.FromArgb(224, 242, 254), Color.FromArgb(  3,  96, 170)) },
                { "Completed",          (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
                { "Cancelled",          (Color.FromArgb(243, 244, 246), Color.FromArgb(107, 114, 128)) },
            };

        public HandlingGoodsReceivedForm()
        {
            InitializeComponent();
            Load += HandlingGoodsReceivedForm_Load;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Load
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void HandlingGoodsReceivedForm_Load(object sender, EventArgs e)
        {
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;
            RefreshData();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Main refresh
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void RefreshData()
        {
            string statusSel    = cmbStatusFilter.SelectedItem?.ToString();
            string statusFilter = (statusSel == "All" || string.IsNullOrEmpty(statusSel))
                                  ? null : statusSel;
            string keyword = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(keyword)) keyword = null;
            DateTime? dateFrom = dtpFrom.Checked ? (DateTime?)dtpFrom.Value.Date : null;

            try
            {
                var vm = _ctrl.GetHandlingGoodsReceivedVM(statusFilter, keyword, dateFrom);

                _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
                _shell.SetVisibleMenus(vm.AllowedMenus);
                _shell.SetBreadcrumb("Logistics Processing  ›  Handling Goods Received");

                BindReceiptsGrid(vm.Receipts);
                BindPurchaseOrdersGrid(vm.PurchaseOrders);
                RefreshKpi(vm.PurchaseOrders);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading goods received data:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Grid binding
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void BindReceiptsGrid(List<GoodsReceivedEntity> data)
        {
            dgvReceipts.Rows.Clear();
            foreach (var r in data)
                dgvReceipts.Rows.Add(
                    r.ReceiptID,
                    r.PurchaseID,
                    r.SupplierName,
                    r.RawMaterialItemID,
                    r.ItemName,
                    r.QtyReceived,
                    r.OutstandingQty?.ToString() ?? "0",
                    r.ReceiptDate.ToString("yyyy-MM-dd"),
                    r.WarehouseLocation,
                    r.PurchaseStatus,
                    $"HK$ {r.UnitPrice:N2}");

            lblReceiptCount.Text = $"{data.Count} receipt(s)";
        }

        private void BindPurchaseOrdersGrid(List<PurchaseOrderEntity> data)
        {
            dgvPO.Rows.Clear();
            foreach (var po in data)
                dgvPO.Rows.Add(
                    po.PurchaseID,
                    po.SupplierName,
                    po.OrderDate.ToString("yyyy-MM-dd"),
                    $"HK$ {po.POTotalAmount:N2}",
                    po.PurchaseStatus);
        }

        // ── KPI Pill Bar ────────────────────────────────────────────────────
        private void RefreshKpi(List<PurchaseOrderEntity> allPOs)
        {
            pnlKpi.Controls.Clear();

            int total     = allPOs.Count;
            int sent      = allPOs.FindAll(p => p.PurchaseStatus == "Sent").Count;
            int partial   = allPOs.FindAll(p => p.PurchaseStatus == "Partially Received").Count;
            int received  = allPOs.FindAll(p => p.PurchaseStatus == "Received").Count;
            int completed = allPOs.FindAll(p => p.PurchaseStatus == "Completed").Count;

            var pills = new[]
            {
                ("Total POs",  total.ToString(),    Color.FromArgb( 47, 111, 237), Color.FromArgb(219, 234, 254), "All"),
                ("Sent",       sent.ToString(),     Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199), "Sent"),
                ("Partial",    partial.ToString(),  Color.FromArgb( 29,  78, 216), Color.FromArgb(219, 234, 254), "Partially Received"),
                ("Received",   received.ToString(), Color.FromArgb(  3,  96, 170), Color.FromArgb(224, 242, 254), "Received"),
                ("Completed",  completed.ToString(),Color.FromArgb(  6,  95,  70), Color.FromArgb(209, 250, 229), "Completed"),
            };

            var flow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent,
                Padding       = new Padding(0),
                AutoScroll    = false
            };

            const int PillW   = 235;
            const int PillH   = 64;
            const int Gap     = 10;
            const int NumColW = 68;

            foreach (var (label, count, fg, bg, filterVal) in pills)
            {
                string localFilter = filterVal;
                var pill = new Panel
                {
                    BackColor = bg,
                    Size      = new Size(PillW, PillH),
                    Margin    = new Padding(0, 0, Gap, 0),
                    Cursor    = Cursors.Hand
                };
                pill.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path  = RoundedRect(((Panel)s).ClientRectangle, 8);
                    using var brush = new SolidBrush(((Panel)s).BackColor);
                    e.Graphics.FillPath(brush, path);
                };

                var tlp = new TableLayoutPanel
                {
                    Dock            = DockStyle.Fill,
                    ColumnCount     = 2,
                    RowCount        = 1,
                    BackColor       = Color.Transparent,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding         = new Padding(10, 0, 8, 0)
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, NumColW));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                tlp.Controls.Add(new Label
                {
                    Text      = count,
                    Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                    ForeColor = fg,
                    BackColor = Color.Transparent,
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize  = false
                }, 0, 0);
                tlp.Controls.Add(new Label
                {
                    Text      = label,
                    Font      = new Font("Segoe UI", 10.5f),
                    ForeColor = fg,
                    BackColor = Color.Transparent,
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoSize  = false
                }, 1, 0);

                EventHandler clickHandler = (s, e) =>
                {
                    int idx = cmbStatusFilter.FindStringExact(localFilter);
                    if (idx >= 0) cmbStatusFilter.SelectedIndex = idx;
                    RefreshData();
                };
                pill.Click += clickHandler;
                tlp.Click  += clickHandler;
                foreach (Control c in tlp.Controls) c.Click += clickHandler;

                pill.Controls.Add(tlp);
                flow.Controls.Add(pill);
            }

            pnlKpi.Controls.Add(flow);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Cell formatting
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void dgvReceipts_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvReceipts.Columns[e.ColumnIndex].Name != "colRStatus" || e.Value == null)
                return;
            ApplyStatusStyle(e);
        }

        private void dgvPO_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvPO.Columns[e.ColumnIndex].Name != "colPOStatus" || e.Value == null)
                return;
            ApplyStatusStyle(e);
        }

        private void ApplyStatusStyle(DataGridViewCellFormattingEventArgs e)
        {
            string val = e.Value.ToString();
            e.FormattingApplied = true;
            if (StatusColors.TryGetValue(val, out var colors))
            {
                e.CellStyle.ForeColor          = colors.fg;
                e.CellStyle.BackColor          = colors.bg;
                e.CellStyle.SelectionForeColor = colors.fg;
                e.CellStyle.SelectionBackColor = colors.bg;
                e.CellStyle.Font               = new Font("Segoe UI", 10f, FontStyle.Bold);
                e.CellStyle.Alignment          = DataGridViewContentAlignment.MiddleCenter;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Filter buttons
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void btnSearch_Click(object sender, EventArgs e) => RefreshData();

        private void btnReset_Click(object sender, EventArgs e)
        {
            cmbStatusFilter.SelectedIndex = 0;
            txtSearch.Clear();
            dtpFrom.Checked = false;
            RefreshData();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Nav / Logout
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }

        // ── Rounded rectangle helper ───────────────────────────────────────────
        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            int d    = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X,         r.Y,          d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y,          d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d,   0, 90);
            path.AddArc(r.X,         r.Bottom - d, d, d,  90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
