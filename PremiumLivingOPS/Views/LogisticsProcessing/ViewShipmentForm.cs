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
    /// Logistics Processing – View Shipment.
    ///
    /// AppShell wiring
    /// ───────────────
    /// Designer.cs creates AppShell, calls SetPopupContainer, and subscribes
    /// MenuItemClicked + LogoutClicked ONCE.  This file must NOT re-subscribe.
    ///
    /// AppShell internally composes:
    ///   TopNavBar (44 px, TopNavBar.cs) + UserBar (72 px, UserBar.cs) = 116 px
    ///
    /// Constructor calls SetBreadcrumb + SetUser from SessionManager immediately
    /// after InitializeComponent so UserBar has real content before the first
    /// WinForms layout pass.  RefreshGrid() (triggered by Shown) overwrites
    /// with authoritative DB values.
    /// </summary>
    public partial class ViewShipmentForm : Form
    {
        private readonly LogisticsProcessingController _ctrl =
            new LogisticsProcessingController();

        private List<ShipmentEntity> _currentShipments = new List<ShipmentEntity>();

        // Status colours (bg, fg) — must match DB schema values exactly
        private static readonly Dictionary<string, (Color bg, Color fg)> StatusColors =
            new Dictionary<string, (Color, Color)>
            {
                { "Pending",    (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "In Transit", (Color.FromArgb(219, 234, 254), Color.FromArgb( 29,  78, 216)) },
                { "Completed",  (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
            };

        // ── Constructor ────────────────────────────────────────────────────────
        public ViewShipmentForm()
        {
            InitializeComponent();

            // Populate UserBar before the first layout pass
            _shell.SetBreadcrumb("Logistics Processing  ›  View Shipment");
            _shell.SetUser(
                SessionManager.CurrentUser?.StaffName   ?? "",
                SessionManager.CurrentUser?.Department  ?? "");

            // Shown fires after the handle is created and all layout passes complete
            Shown += (s, e) => RefreshGrid();
        }

        // ── Grid + KPI refresh ─────────────────────────────────────────────────
        private void RefreshGrid()
        {
            string statusSel    = cmbStatusFilter.SelectedItem?.ToString();
            string statusFilter = (statusSel == "All" || string.IsNullOrEmpty(statusSel)) ? null : statusSel;
            string keyword      = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(keyword)) keyword = null;
            DateTime? dateFrom  = dtpFrom.Checked ? (DateTime?)dtpFrom.Value.Date : null;

            try
            {
                var vm = _ctrl.GetViewShipmentVM(statusFilter, keyword, dateFrom);

                _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
                _shell.SetVisibleMenus(vm.AllowedMenus);
                _shell.SetBreadcrumb("Logistics Processing  ›  View Shipment");

                _currentShipments = vm.Shipments;
                BindShipmentGrid(_currentShipments);
                RefreshKpi(_currentShipments);
                ClearDetail();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading shipments:\n{ex.Message}",
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

        // ── KPI Pill Bar ────────────────────────────────────────────────────────
        private void RefreshKpi(List<ShipmentEntity> shipments)
        {
            pnlKpi.Controls.Clear();

            int total     = shipments.Count;
            int pending   = shipments.FindAll(s => s.ShipmentStatus == "Pending").Count;
            int inTransit = shipments.FindAll(s => s.ShipmentStatus == "In Transit").Count;
            int completed = shipments.FindAll(s => s.ShipmentStatus == "Completed").Count;

            var pills = new[]
            {
                ("Total",      total.ToString(),     Color.FromArgb( 47, 111, 237), Color.FromArgb(219, 234, 254), "All"),
                ("Pending",    pending.ToString(),   Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199), "Pending"),
                ("In Transit", inTransit.ToString(), Color.FromArgb( 29,  78, 216), Color.FromArgb(219, 234, 254), "In Transit"),
                ("Completed",  completed.ToString(), Color.FromArgb(  6,  95,  70), Color.FromArgb(209, 250, 229), "Completed"),
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

        // ── Grid events ────────────────────────────────────────────────────────
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
                MessageBox.Show(
                    $"Error loading shipment detail:\n{ex.Message}",
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
                e.CellStyle.ForeColor          = colors.fg;
                e.CellStyle.BackColor          = colors.bg;
                e.CellStyle.SelectionForeColor = colors.fg;
                e.CellStyle.SelectionBackColor = colors.bg;
                e.CellStyle.Font               = new Font("Segoe UI", 10f, FontStyle.Bold);
                e.CellStyle.Alignment          = DataGridViewContentAlignment.MiddleCenter;
            }
        }

        // ── Detail panel ───────────────────────────────────────────────────────
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
                lblDNID.Text       = d.DeliveryNote.DeliveryID;
                lblDNShipTo.Text   = d.DeliveryNote.ShipToName;
                lblDNDate.Text     = d.DeliveryNote.DeliveryDate.ToString("yyyy-MM-dd");
                lblDNOutQty.Text   = d.DeliveryNote.OutstandingQty?.ToString() ?? "0";
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

        // ── Filter buttons ─────────────────────────────────────────────────────
        private void btnSearch_Click(object sender, EventArgs e) => RefreshGrid();

        private void btnReset_Click(object sender, EventArgs e)
        {
            cmbStatusFilter.SelectedIndex = 0;
            txtSearch.Clear();
            dtpFrom.Checked = false;
            RefreshGrid();
        }

        // ── Nav / Logout — events wired ONCE in Designer.cs; do NOT re-subscribe ─
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }

        // ── Helpers ────────────────────────────────────────────────────────────
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
