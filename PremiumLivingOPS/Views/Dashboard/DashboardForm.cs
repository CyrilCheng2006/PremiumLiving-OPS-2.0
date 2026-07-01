using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.Dashboard
{
    /// <summary>
    /// Dashboard View — UI binding only.
    /// Chrome provided by AppShell; data/visibility driven by DashboardViewModel.
    ///
    /// Section visibility contract
    /// ───────────────────────────
    /// BindViewModel() reads vm.Sections (set by DashboardController / NavAccessPolicy)
    /// and calls ApplySectionVisibility() which:
    ///   • Shows/hides individual KPI panels inside their TLP cells.
    ///   • Collapses an entire KPI row (outer CardPanel) when ALL 4 cards are hidden.
    ///   • Shows/hides each Section card column; collapses a row-TLP when both halves
    ///     are hidden (the outer CardPanel itself is also hidden).
    /// The Designer still creates all controls — visibility is purely runtime.
    /// </summary>
    public partial class DashboardForm : Form
    {
        // ── Palette ──────────────────────────────────────────────────────
        internal static class Palette
        {
            public static readonly Color BgPage       = Color.FromArgb(240, 244, 249);
            public static readonly Color BgCard       = Color.White;
            public static readonly Color BorderColor  = Color.FromArgb(221, 227, 236);
            public static readonly Color Primary      = Color.FromArgb(47,  111, 237);
            public static readonly Color PrimaryDark  = Color.FromArgb(26,  77,  192);
            public static readonly Color Danger       = Color.FromArgb(232, 64,  64);
            public static readonly Color Success      = Color.FromArgb(30,  184, 122);
            public static readonly Color Warning      = Color.FromArgb(245, 158, 11);
            public static readonly Color Info         = Color.FromArgb(6,   182, 212);
            public static readonly Color TextMain     = Color.FromArgb(15,  31,  53);
            public static readonly Color TextMuted    = Color.FromArgb(98,  112, 135);
            public static readonly Color SidebarBg    = Color.FromArgb(19,  35,  61);
            public static readonly Color SidebarText  = Color.FromArgb(205, 216, 234);
            public static readonly Color SidebarHover = Color.FromArgb(30,  53,  88);
            public static readonly Color TagBlueBg    = Color.FromArgb(219, 234, 254);
            public static readonly Color TagBlueFg    = Color.FromArgb(29,  78,  216);
            public static readonly Color TagGreenBg   = Color.FromArgb(209, 250, 229);
            public static readonly Color TagGreenFg   = Color.FromArgb(6,   95,  70);
            public static readonly Color TagRedBg     = Color.FromArgb(254, 226, 226);
            public static readonly Color TagRedFg     = Color.FromArgb(153, 27,  27);
            public static readonly Color TagYellowBg  = Color.FromArgb(254, 243, 199);
            public static readonly Color TagYellowFg  = Color.FromArgb(146, 64,  14);
            public static readonly Color TagGrayBg    = Color.FromArgb(241, 245, 249);
            public static readonly Color TagGrayFg    = Color.FromArgb(71,  85,  105);

            public static Color FromKey(string key)
            {
                switch (key)
                {
                    case "Primary": return Primary;
                    case "Success": return Success;
                    case "Warning": return Warning;
                    case "Danger":  return Danger;
                    case "Info":    return Info;
                    default:        return Primary;
                }
            }

            public static (Color bg, Color fg) TagColours(string status)
            {
                switch (status)
                {
                    case "Processing": case "In Transit": case "Shipped":
                        return (TagBlueBg,   TagBlueFg);
                    case "Delivered":  case "Paid":
                        return (TagGreenBg,  TagGreenFg);
                    case "Pending":    case "Scheduled":
                        return (TagYellowBg, TagYellowFg);
                    case "Critical":   case "Overdue":
                        return (TagRedBg,    TagRedFg);
                    case "Low":
                        return (TagYellowBg, TagYellowFg);
                    default:
                        return (TagGrayBg,   TagGrayFg);
                }
            }
        }

        // ── Fonts ─────────────────────────────────────────────────────────
        private static readonly Font FontBodyBold  = new Font("Segoe UI", 12.8f, FontStyle.Bold);
        private static readonly Font FontSmallBold = new Font("Segoe UI", 11.2f, FontStyle.Bold);

        // ── Fields ────────────────────────────────────────────────────────
        private readonly DashboardController _controller;
        private Panel _activeNavItem;

        // ── Constructor ───────────────────────────────────────────────────
        public DashboardForm()
        {
            _controller = new DashboardController();
            InitializeComponent();
            BindViewModel();
        }

        // ── ViewModel binding ──────────────────────────────────────────────
        private void BindViewModel()
        {
            DashboardViewModel vm = _controller.LoadDashboard();

            // 1. Shell chrome
            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Dashboard");
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;

            lblPageSub.Text = "Premium Living Furniture Co.  \u00b7  Overview as of " +
                              DateTime.Now.ToString("d MMMM yyyy");

            // 2. Alert banner (only meaningful when low-stock section is visible)
            int lowCount = vm.LowStock.Count;
            pnlAlert.Visible = vm.Sections.ShowLowStock && lowCount > 0;
            if (pnlAlert.Visible)
                lblAlert.Text = $"\u26a0\ufe0f  {lowCount} item(s) are currently below minimum stock threshold.";

            // 3. Section visibility
            ApplySectionVisibility(vm.Sections);

            // 4. KPI cards — bind in order of Kpis list
            //    Each card slot maps to a KPI by AccentKey+Label (order matches BuildSections)
            Panel[] kpiPanels =
            {
                kpiOrders, kpiDelivered, kpiQuotations, kpiLowStock,
                kpiRevenue, kpiAR,       kpiSuppliers,  kpiCustomers
            };
            // Only visible panels get data; the KPis list in the VM is already filtered
            // We match by position among VISIBLE panels
            int kpiIdx = 0;
            foreach (Panel p in kpiPanels)
            {
                if (!p.Visible) continue;
                if (kpiIdx < vm.Kpis.Count)
                    SetKpiCard(p, vm.Kpis[kpiIdx++]);
            }

            // 5. Recent Orders
            foreach (var row in vm.Orders)
            {
                var (bg, fg) = Palette.TagColours(row.Status);
                int idx = dgvOrders.Rows.Add(row.OrderId, row.Customer, row.Total, row.Status);
                dgvOrders.Rows[idx].Tag = new[] { bg, fg };
            }

            // 6. Low-Stock grid
            BindLowStockGrid(vm.LowStock);

            // 7. Pending Quotations
            foreach (var row in vm.Quotations)
                dgvQuotations.Rows.Add(row.QuotationId, row.Customer, row.Amount, row.ValidUntil);

            // 8. Active Shipments
            foreach (var row in vm.Shipments)
            {
                var (bg, fg) = Palette.TagColours(row.Status);
                int idx = dgvShipments.Rows.Add(row.ShipmentId, row.Customer, row.SchedDate, row.Status);
                dgvShipments.Rows[idx].Tag = new[] { bg, fg };
            }

            // 9. Supplier Payments
            foreach (var row in vm.Suppliers)
            {
                var (bg, fg) = Palette.TagColours(row.Status);
                int idx = dgvSuppliers.Rows.Add(row.Supplier, row.InvoiceId, row.Amount, row.Status);
                dgvSuppliers.Rows[idx].Tag = new[] { bg, fg };
            }

            // 10. Activity feed
            foreach (var row in vm.Activities)
            {
                string actText = string.IsNullOrEmpty(row.NormalText)
                    ? row.BoldText
                    : row.BoldText + "  " + row.NormalText;
                int idx = dgvActivity.Rows.Add("", actText, row.TimeLabel);
                dgvActivity.Rows[idx].Tag = Palette.FromKey(row.CategoryKey);
            }
        }

        // ── Section / KPI visibility ──────────────────────────────────────
        /// <summary>
        /// Applies vm.Sections flags to every UI panel.
        ///
        /// KPI rows: each card is individually shown/hidden inside its TLP cell.
        ///   If ALL 4 cards in a row are hidden the outer CardPanel row is collapsed.
        ///
        /// Section rows (Row1-3 TLPs): each half-column is shown/hidden.
        ///   A row's outer CardPanel is collapsed when both halves are hidden.
        ///   When only one half is visible it expands to full width via ColumnSpan=2.
        /// </summary>
        private void ApplySectionVisibility(DashboardSections s)
        {
            // ── KPI Row 1 ────────────────────────────────────────────────
            kpiOrders.Visible     = s.ShowKpiOrders;
            kpiDelivered.Visible  = s.ShowKpiDelivered;
            kpiQuotations.Visible = s.ShowKpiQuotations;
            kpiLowStock.Visible   = s.ShowKpiLowStock;
            bool kpiRow1Visible = s.ShowKpiOrders || s.ShowKpiDelivered ||
                                  s.ShowKpiQuotations || s.ShowKpiLowStock;
            pnlKpi1.Visible = kpiRow1Visible;

            // ── KPI Row 2 ────────────────────────────────────────────────
            kpiRevenue.Visible   = s.ShowKpiRevenue;
            kpiAR.Visible        = s.ShowKpiAR;
            kpiSuppliers.Visible = s.ShowKpiSuppliers;
            kpiCustomers.Visible = s.ShowKpiCustomers;
            bool kpiRow2Visible = s.ShowKpiRevenue || s.ShowKpiAR ||
                                  s.ShowKpiSuppliers || s.ShowKpiCustomers;
            pnlKpi2.Visible = kpiRow2Visible;

            // ── Section Row 1: Recent Orders (left) + Low Stock (right) ──
            ApplyRowVisibility(
                tlpRow1,
                leftVisible:  s.ShowRecentOrders,
                rightVisible: s.ShowLowStock);

            // ── Section Row 2: Pending Quotations (left) + Shipments (right) ──
            ApplyRowVisibility(
                tlpRow2,
                leftVisible:  s.ShowPendingQuotations,
                rightVisible: s.ShowActiveShipments);

            // ── Section Row 3: Supplier Payments (left) + Activity (right) ──
            ApplyRowVisibility(
                tlpRow3,
                leftVisible:  s.ShowSupplierPayments,
                rightVisible: s.ShowRecentActivity);
        }

        /// <summary>
        /// Shows/hides the left and right columns of a two-column TLP section row.
        /// - Both visible     → normal 50/50 split.
        /// - Only one visible → that column spans both columns (100 %).
        /// - Both hidden      → the TLP's parent outer CardPanel is collapsed.
        /// </summary>
        private static void ApplyRowVisibility(
            TableLayoutPanel tlp, bool leftVisible, bool rightVisible)
        {
            if (tlp == null) return;

            // The outer CardPanel is tlp.Parent.Parent (inner→outer nesting)
            Control outerCard = tlp.Parent?.Parent;

            if (!leftVisible && !rightVisible)
            {
                if (outerCard != null) outerCard.Visible = false;
                return;
            }

            if (outerCard != null) outerCard.Visible = true;

            // Show/hide individual columns
            if (tlp.Controls.Count >= 1) tlp.Controls[0].Visible = leftVisible;
            if (tlp.Controls.Count >= 2) tlp.Controls[1].Visible = rightVisible;

            // Adjust column widths so the visible half fills the row
            tlp.ColumnStyles[0].SizeType = SizeType.Percent;
            tlp.ColumnStyles[1].SizeType = SizeType.Percent;

            if (leftVisible && rightVisible)
            {
                tlp.ColumnStyles[0].Width = 50f;
                tlp.ColumnStyles[1].Width = 50f;
            }
            else if (leftVisible)
            {
                tlp.ColumnStyles[0].Width = 100f;
                tlp.ColumnStyles[1].Width = 0f;
            }
            else
            {
                tlp.ColumnStyles[0].Width = 0f;
                tlp.ColumnStyles[1].Width = 100f;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────
        private void SetKpiCard(Panel card, DashboardKpi kpi)
        {
            Color accent = Palette.FromKey(kpi.AccentKey);
            foreach (Control ctrl in card.Controls)
            {
                if (ctrl.Tag?.ToString() == "kpi-label") ctrl.Text = kpi.Label;
                if (ctrl.Tag?.ToString() == "kpi-value") { ctrl.Text = kpi.Value; ctrl.ForeColor = accent; }
                if (ctrl.Tag?.ToString() == "kpi-sub")   ctrl.Text = kpi.SubText;
            }
        }

        private void BindLowStockGrid(List<LowStockRow> rows)
        {
            if (_dgvLowStock == null) return;
            _dgvLowStock.Rows.Clear();
            foreach (var row in rows)
            {
                var (bg, fg) = Palette.TagColours(row.Status);
                int idx = _dgvLowStock.Rows.Add(
                    row.ItemName,
                    row.OnHand.ToString(),
                    row.MinimumQty.ToString(),
                    row.Status);
                _dgvLowStock.Rows[idx].Tag = new[] { bg, fg };
            }
        }

        // ── Nav / logout ──────────────────────────────────────────────────
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
        {
            if (menuLabel == "Dashboard" && string.IsNullOrEmpty(subItem)) return;
            FormNavigator.NavigateTo(this, menuLabel, subItem);
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to log out?",
                                "Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                SessionManager.Clear();
                Application.Restart();
            }
        }

        private void SetActiveNav(Panel navPanel)
        {
            if (_activeNavItem != null)
            {
                _activeNavItem.BackColor = Color.Transparent;
                foreach (Control c in _activeNavItem.Controls)
                {
                    if (c is Label l) l.ForeColor = Palette.SidebarText;
                    if (c is Panel && c.Dock == DockStyle.Left && c.Width == 4) c.BackColor = Color.Transparent;
                }
            }
            _activeNavItem = navPanel;
            _activeNavItem.BackColor = Palette.SidebarHover;
            foreach (Control c in _activeNavItem.Controls)
            {
                if (c is Label l) l.ForeColor = Color.White;
                if (c is Panel && c.Dock == DockStyle.Left && c.Width == 4) c.BackColor = Palette.Primary;
            }
        }

        // ── Cell painting ─────────────────────────────────────────────────
        private void PaintStatusCell(object sender, DataGridViewCellPaintingEventArgs e, int statusColIndex)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != statusColIndex) return;
            var row = ((DataGridView)sender).Rows[e.RowIndex];
            if (row.Tag is Color[] colours && colours.Length == 2)
            {
                e.Paint(e.CellBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);
                string text = e.Value?.ToString() ?? "";
                SizeF  sz   = e.Graphics.MeasureString(text, FontSmallBold);
                RectangleF badge = new RectangleF(
                    e.CellBounds.X + 6,
                    e.CellBounds.Y + (e.CellBounds.Height - sz.Height - 6) / 2f,
                    sz.Width + 19, sz.Height + 6);
                using (GraphicsPath gp = new GraphicsPath())
                {
                    float r = badge.Height / 2f;
                    gp.AddArc(badge.X,           badge.Y,            r*2, r*2, 180, 90);
                    gp.AddArc(badge.Right - r*2, badge.Y,            r*2, r*2, 270, 90);
                    gp.AddArc(badge.Right - r*2, badge.Bottom - r*2, r*2, r*2,   0, 90);
                    gp.AddArc(badge.X,           badge.Bottom - r*2, r*2, r*2,  90, 90);
                    gp.CloseFigure();
                    e.Graphics.FillPath(new SolidBrush(colours[0]), gp);
                    e.Graphics.DrawString(text, FontSmallBold, new SolidBrush(colours[1]),
                        badge.X + 10, badge.Y + 3);
                }
                e.Handled = true;
            }
        }

        private void dgvActivity_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 0) return;
            e.Paint(e.CellBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);
            if (dgvActivity.Rows[e.RowIndex].Tag is Color dotColor)
            {
                int d = 12;
                int x = e.CellBounds.X + (e.CellBounds.Width  - d) / 2;
                int y = e.CellBounds.Y + (e.CellBounds.Height - d) / 2;
                using (var path = new GraphicsPath())
                {
                    path.AddEllipse(x, y, d, d);
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(new SolidBrush(dotColor), path);
                }
            }
            e.Handled = true;
        }

        private void dgvOrders_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
            => PaintStatusCell(sender, e, 3);
        private void dgvShipments_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
            => PaintStatusCell(sender, e, 3);
        private void dgvSuppliers_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
            => PaintStatusCell(sender, e, 3);
    }
}
