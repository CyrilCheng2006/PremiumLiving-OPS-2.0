using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
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
    /// AppShell wiring contract
    /// ─────────────────────────────────────────────────────────────────
    ///   TopNavBar : 44 px  (enforced by AppShell.NavBarHeight / TopNavBar.OnLayout)
    ///   UserBar   : 72 px  (enforced by AppShell.UserBarHeight / AppShell.OnLayout)
    ///   Total     : 116 px (AppShell.TotalHeight)
    ///
    /// Designer.cs (InitializeComponent) handles ALL construction-time wiring:
    ///   RULE 1  SuspendLayout() first.
    ///   RULE 2  _shell = new AppShell() inside SuspendLayout.
    ///           _shell.Dock = DockStyle.Top; _shell.Height = AppShell.TotalHeight;
    ///   RULE 3  After ResumeLayout/PerformLayout, _shell.Height set again.
    ///   RULE 4  _shell.MenuItemClicked and LogoutClicked subscribed ONCE in
    ///           Designer.cs.  This file must NOT re-subscribe them.
    ///   RULE 5  pnlMain.Controls: Fill-content first, _shell last (Top wins).
    ///
    /// UserBar render guarantee
    /// ────────────────────────
    ///   The constructor calls SetBreadcrumb + SetUser from SessionManager
    ///   immediately after InitializeComponent() so the UserBar has real
    ///   content before WinForms performs its first layout pass.
    ///   RefreshGrid() then overwrites with the authoritative DB values.
    ///
    /// Entity types used (all in PremiumLivingOPS.Models.Entities):
    ///   GoodsReceivedEntity  — receipt rows  (vm.Receipts)
    ///   PurchaseOrderEntity  — PO rows       (vm.PurchaseOrders)
    /// Page VM (PremiumLivingOPS.Models.ViewModels):
    ///   HandlingGoodsReceivedVM
    /// </summary>
    public partial class HandlingGoodsReceivedForm : Form
    {
        private readonly LogisticsProcessingController _ctrl =
            new LogisticsProcessingController();

        // ── PO status colour map (bg, fg) ──────────────────────────────────────────────
        private static readonly Dictionary<string, (Color bg, Color fg)> POStatusColors =
            new Dictionary<string, (Color, Color)>
            {
                { "Sent",               (Color.FromArgb(219, 234, 254), Color.FromArgb( 29,  78, 216)) },
                { "Partially Received", (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "Received",           (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
                { "Completed",          (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
                { "Cancelled",          (Color.FromArgb(254, 226, 226), Color.FromArgb(185,  28,  28)) },
            };

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Constructor
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        public HandlingGoodsReceivedForm()
        {
            // InitializeComponent() runs Designer.cs:
            //   • SuspendLayout / ResumeLayout / PerformLayout (RULE 1)
            //   • _shell constructed, Dock=Top, Height=116 (RULE 2 + 3)
            //   • MenuItemClicked + LogoutClicked subscribed (RULE 4)
            //   • pnlMain: pnlPage(Fill) then _shell(Top) (RULE 5)
            InitializeComponent();

            // Guarantee UserBar is populated BEFORE the first layout pass
            // that WinForms triggers when the Form is shown.  Without this,
            // the TableLayoutPanel inside AppShell sizes the UserInfo column
            // to 0 px (empty content) and the UserBar appears invisible.
            _shell.SetBreadcrumb("Logistics Processing  ›  Handling Goods Received");
            _shell.SetUser(
                SessionManager.CurrentUser?.StaffName ?? "",
                SessionManager.CurrentUser?.Department ?? "");

            // Wire Shown event to trigger the first DB load.
            // Using Shown (not Load) ensures the Form handle is fully created
            // and all layout passes have completed before any UI update.
            Shown += (s, e) => RefreshGrid();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Grid refresh
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void RefreshGrid()
        {
            string statusSel = cmbStatusFilter.SelectedItem?.ToString();
            string statusFilter = (statusSel == "All" || string.IsNullOrEmpty(statusSel))
                                  ? null : statusSel;
            string keyword = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(keyword)) keyword = null;
            DateTime? dateFrom = dtpFrom.Checked ? (DateTime?)dtpFrom.Value.Date : null;

            try
            {
                HandlingGoodsReceivedVM vm =
                    _ctrl.GetHandlingGoodsReceivedVM(statusFilter, keyword, dateFrom);

                // Overwrite with authoritative DB values
                _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
                _shell.SetVisibleMenus(vm.AllowedMenus);
                _shell.SetBreadcrumb("Logistics Processing  ›  Handling Goods Received");

                BindReceiptsGrid(vm.Receipts);
                BindPOGrid(vm.PurchaseOrders);
                RefreshKpi(vm);

                lblReceiptCount.Text = $"{vm.Receipts.Count} record(s)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading goods received data:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Grid binding ───────────────────────────────────────────────────────────────────
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
        }

        private void BindPOGrid(List<PurchaseOrderEntity> data)
        {
            dgvPO.Rows.Clear();
            foreach (var p in data)
                dgvPO.Rows.Add(
                    p.PurchaseID,
                    p.SupplierName,
                    p.OrderDate.ToString("yyyy-MM-dd"),
                    $"HK$ {p.POTotalAmount:N2}",
                    p.PurchaseStatus);
        }

        // ── KPI Pill Bar ───────────────────────────────────────────────────────────────────
        private void RefreshKpi(HandlingGoodsReceivedVM vm)
        {
            pnlKpi.Controls.Clear();

            var pos = vm.PurchaseOrders;
            int total     = pos.Count;
            int sent      = pos.FindAll(p => p.PurchaseStatus == "Sent").Count;
            int partial   = pos.FindAll(p => p.PurchaseStatus == "Partially Received").Count;
            int received  = pos.FindAll(p => p.PurchaseStatus == "Received").Count;
            int completed = pos.FindAll(p => p.PurchaseStatus == "Completed").Count;
            int cancelled = pos.FindAll(p => p.PurchaseStatus == "Cancelled").Count;

            var pills = new[]
            {
                ("Total POs",          total.ToString(),     Color.FromArgb( 47, 111, 237), Color.FromArgb(219, 234, 254), "All"),
                ("Sent",               sent.ToString(),      Color.FromArgb( 29,  78, 216), Color.FromArgb(219, 234, 254), "Sent"),
                ("Partially Received", partial.ToString(),   Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199), "Partially Received"),
                ("Received",           received.ToString(),  Color.FromArgb(  6,  95,  70), Color.FromArgb(209, 250, 229), "Received"),
                ("Completed",          completed.ToString(), Color.FromArgb(  6,  95,  70), Color.FromArgb(209, 250, 229), "Completed"),
                ("Cancelled",          cancelled.ToString(), Color.FromArgb(185,  28,  28), Color.FromArgb(254, 226, 226), "Cancelled"),
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

            const int PillW   = 200;
            const int PillH   = 64;
            const int Gap     = 8;
            const int NumColW = 60;

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
                    Padding         = new Padding(8, 0, 6, 0)
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, NumColW));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                tlp.Controls.Add(new Label
                {
                    Text      = count,
                    Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                    ForeColor = fg,
                    BackColor = Color.Transparent,
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize  = false
                }, 0, 0);
                tlp.Controls.Add(new Label
                {
                    Text      = label,
                    Font      = new Font("Segoe UI", 10f),
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
                    RefreshGrid();
                };
                pill.Click += clickHandler;
                tlp.Click  += clickHandler;
                foreach (Control c in tlp.Controls) c.Click += clickHandler;

                pill.Controls.Add(tlp);
                flow.Controls.Add(pill);
            }

            pnlKpi.Controls.Add(flow);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  CellFormatting — colour PO status cells
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void dgvReceipts_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvReceipts.Columns[e.ColumnIndex].Name != "colRStatus" || e.Value == null)
                return;
            ApplyStatusColor(e, POStatusColors);
        }

        private void dgvPO_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvPO.Columns[e.ColumnIndex].Name != "colPOStatus" || e.Value == null)
                return;
            ApplyStatusColor(e, POStatusColors);
        }

        private static void ApplyStatusColor(
            DataGridViewCellFormattingEventArgs e,
            Dictionary<string, (Color bg, Color fg)> map)
        {
            if (!map.TryGetValue(e.Value.ToString(), out var colors)) return;
            e.FormattingApplied                = true;
            e.CellStyle.ForeColor              = colors.fg;
            e.CellStyle.BackColor              = colors.bg;
            e.CellStyle.SelectionForeColor     = colors.fg;
            e.CellStyle.SelectionBackColor     = colors.bg;
            e.CellStyle.Font                   = new Font("Segoe UI", 10f, FontStyle.Bold);
            e.CellStyle.Alignment              = DataGridViewContentAlignment.MiddleCenter;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Filter buttons
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void btnSearch_Click(object sender, EventArgs e) => RefreshGrid();

        private void btnReset_Click(object sender, EventArgs e)
        {
            cmbStatusFilter.SelectedIndex = 0;
            txtSearch.Clear();
            dtpFrom.Checked = false;
            RefreshGrid();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Nav / Logout — wired ONCE in Designer.cs InitializeComponent (RULE 4)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }

        // ── Rounded rectangle helper ────────────────────────────────────────────────────────────
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
