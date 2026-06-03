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
    /// Logistics Processing – View Shipment page.
    ///
    /// AppShell wiring summary
    /// ─────────────────────────────────────────────────────────────────
    /// Designer.cs InitializeComponent() handles ALL construction-time wiring:
    ///   _shell = new AppShell();
    ///   _shell.Dock        = DockStyle.Top;
    ///   _shell.Height      = AppShell.TotalHeight;          // 44 + 72 = 116 px
    ///   _shell.MinimumSize = new Size(0, AppShell.TotalHeight);
    ///   _shell.SetPopupContainer(pnlMain);
    ///   _shell.MenuItemClicked += OnTopNavMenuItemClicked;  // wired ONCE here
    ///   _shell.LogoutClicked   += btnLogout_Click;          // wired ONCE here
    ///
    /// This file must NOT re-subscribe those events in _Load.
    ///
    /// UserBar render guarantee
    /// ────────────────────────
    /// _Load sets breadcrumb + user from SessionManager FIRST (before any DB
    /// call) so the UserBar always renders even if RefreshGrid() throws.
    /// RefreshGrid() then overwrites with the authoritative DB values.
    ///
    /// KPI pills reuse _currentShipments (already loaded by RefreshGrid) so the
    /// controller is never called twice per refresh cycle.
    /// </summary>
    public partial class ViewShipmentForm : Form
    {
        private readonly LogisticsProcessingController _ctrl =
            new LogisticsProcessingController();

        private List<ShipmentEntity> _currentShipments = new List<ShipmentEntity>();

        // ── Status colour map (bg, fg) ──────────────────────────────────────────────
        private static readonly Dictionary<string, (Color bg, Color fg)> StatusColors =
            new Dictionary<string, (Color, Color)>
            {
                { "Pending",    (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "In Transit", (Color.FromArgb(219, 234, 254), Color.FromArgb( 29,  78, 216)) },
                { "Completed",  (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
            };

        public ViewShipmentForm()
        {
            InitializeComponent();
            // NOTE: AppShell events (MenuItemClicked, LogoutClicked) are already
            // subscribed inside InitializeComponent() in Designer.cs.
            // Do NOT subscribe them again here.
            Load += ViewShipmentForm_Load;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Load — set UserBar immediately from session, then load data
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void ViewShipmentForm_Load(object sender, EventArgs e)
        {
            // Guarantee UserBar renders before any DB call.
            // RefreshGrid() will overwrite these with authoritative DB values.
            _shell.SetBreadcrumb("Logistics Processing  ›  View Shipment");
            _shell.SetUser(
                SessionManager.CurrentUser?.DisplayName ?? SessionManager.CurrentUser?.Username ?? "",
                SessionManager.CurrentUser?.Department  ?? "");

            RefreshGrid();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Grid + KPI refresh
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
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
                // Single controller call — UserBar is set before anything else;
                // KPI pills reuse _currentShipments so no second controller call is needed.
                var vm = _ctrl.GetViewShipmentVM(statusFilter, keyword, dateFrom);

                _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
                _shell.SetVisibleMenus(vm.AllowedMenus);
                _shell.SetBreadcrumb("Logistics Processing  ›  View Shipment");

                _currentShipments = vm.Shipments;

                BindShipmentGrid(_currentShipments);

                // KPI pills count across the currently loaded slice.
                // When no filter is active _currentShipments == all shipments.
                RefreshKpi(_currentShipments);
                ClearDetail();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading shipments:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BindShipmentGrid(List<ShipmentEntity> data)
        {
            dgvShipments.Rows.Clear();
            foreach (var s in data)
                dgvShipments.Rows.Add(
                    s.ShipmentID,
                    s.OrderID,
                    s.CustomerName,
                    s.TrackingNumber,
                    s.ShipDate.ToString("yyyy-MM-dd"),
                    s.ShipmentStatus,
                    s.ShipmentType,
                    s.DeliveryMethod,
                    $"HK$ {s.TotalAmount:N2}");

            lblRecordCount.Text = $"{data.Count} record(s)";
        }

        // ── KPI Pill Bar ───────────────────────────────────────────────────────────────
        // Accepts the already-loaded shipment list — no extra controller call.
        private void RefreshKpi(List<ShipmentEntity> shipments)
        {
            pnlKpi.Controls.Clear();

            int total     = shipments.Count;
            int pending   = shipments.FindAll(s => s.ShipmentStatus == "Pending").Count;
            int inTransit = shipments.FindAll(s => s.ShipmentStatus == "In Transit").Count;
            int completed = shipments.FindAll(s => s.ShipmentStatus == "Completed").Count;

            var pills = new[]
            {
                ("Total",      total.ToString(),      Color.FromArgb( 47, 111, 237), Color.FromArgb(219, 234, 254), "All"),
                ("Pending",    pending.ToString(),    Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199), "Pending"),
                ("In Transit", inTransit.ToString(),  Color.FromArgb( 29,  78, 216), Color.FromArgb(219, 234, 254), "In Transit"),
                ("Completed",  completed.ToString(),  Color.FromArgb(  6,  95,  70), Color.FromArgb(209, 250, 229), "Completed"),
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

            const int PillW   = 260;
            const int PillH   = 64;
            const int Gap     = 10;
            const int NumColW = 70;

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
                    Font      = new Font("Segoe UI", 11f),
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Grid events
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void dgvShipments_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvShipments.SelectedRows.Count == 0) { ClearDetail(); return; }
            string id = dgvShipments.SelectedRows[0].Cells["colShipmentID"].Value?.ToString();
            if (string.IsNullOrEmpty(id)) return;

            try
            {
                ShowDetail(_ctrl.GetShipmentDetail(id));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading shipment detail:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dgvShipments_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvShipments.Columns[e.ColumnIndex].Name != "colStatus" || e.Value == null)
                return;

            string val = e.Value.ToString();
            e.FormattingApplied = true;
            if (StatusColors.TryGetValue(val, out var colors))
            {
                e.CellStyle.ForeColor            = colors.fg;
                e.CellStyle.BackColor            = colors.bg;
                e.CellStyle.SelectionForeColor   = colors.fg;
                e.CellStyle.SelectionBackColor   = colors.bg;
                e.CellStyle.Font                 = new Font("Segoe UI", 10f, FontStyle.Bold);
                e.CellStyle.Alignment            = DataGridViewContentAlignment.MiddleCenter;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Detail panel
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void ShowDetail(ShipmentDetailVM d)
        {
            if (d?.Shipment == null) { ClearDetail(); return; }

            var s = d.Shipment;
            lblDetailShipmentID.Text = s.ShipmentID;
            lblDetailOrderID.Text    = s.OrderID;
            lblDetailCustomer.Text   = s.CustomerName;
            lblDetailTracking.Text   = s.TrackingNumber;
            lblDetailStatus.Text     = s.ShipmentStatus;
            lblDetailType.Text       = s.ShipmentType;
            lblDetailMethod.Text     = s.DeliveryMethod;
            lblDetailShipDate.Text   = s.ShipDate.ToString("yyyy-MM-dd");
            lblDetailAmount.Text     = $"HK$ {s.TotalAmount:N2}";
            lblDetailAddress.Text    = s.ShippingAddress ?? "—";

            if (StatusColors.TryGetValue(s.ShipmentStatus ?? "", out var sc))
            {
                lblDetailStatus.ForeColor = sc.fg;
                lblDetailStatus.BackColor = sc.bg;
            }

            if (d.DeliveryNote != null)
            {
                lblDNID.Text     = d.DeliveryNote.DeliveryID;
                lblDNShipTo.Text = d.DeliveryNote.ShipToName;
                lblDNDate.Text   = d.DeliveryNote.DeliveryDate.ToString("yyyy-MM-dd");
                lblDNOutQty.Text = d.DeliveryNote.OutstandingQty?.ToString() ?? "0";
                pnlDNOuter.Visible = true;
            }
            else
            {
                pnlDNOuter.Visible = false;
            }

            dgvLines.Rows.Clear();
            foreach (var line in d.Lines)
                dgvLines.Rows.Add(
                    line.ShipmentLineID,
                    line.ItemID,
                    line.ItemName,
                    line.QtyShipped,
                    line.QtyOutstanding?.ToString() ?? "0");

            pnlDetailOuter.Visible = true;
        }

        private void ClearDetail() => pnlDetailOuter.Visible = false;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Filter buttons
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void btnSearch_Click(object sender, EventArgs e) => RefreshGrid();

        private void btnReset_Click(object sender, EventArgs e)
        {
            cmbStatusFilter.SelectedIndex = 0;
            txtSearch.Clear();
            dtpFrom.Checked = false;
            RefreshGrid();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Nav / Logout — handlers wired in Designer.cs InitializeComponent()
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
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
