using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.Dashboard
{
    /// <summary>
    /// Dashboard View — responsibility is UI binding only.
    ///
    /// Rules enforced:
    ///   ✔  Reads data exclusively from DashboardViewModel (supplied by DashboardController).
    ///   ✔  No SQL, no hardcoded data, no business logic.
    ///   ✔  Colour mapping from AccentKey / CategoryKey strings → Palette colours.
    /// </summary>
    public partial class DashboardForm : Form
    {
        // ── Palette ───────────────────────────────────────────────────
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

            /// <summary>Maps the string key stored in ViewModel → actual Color.</summary>
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

            /// <summary>Returns (background, foreground) tag badge colours for a status string.</summary>
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

        // ── Fonts ─────────────────────────────────────────────────────
        private static readonly Font FontBody      = new Font("Segoe UI", 16f,   FontStyle.Regular);
        private static readonly Font FontBodyBold  = new Font("Segoe UI", 16f,   FontStyle.Bold);
        private static readonly Font FontSmall     = new Font("Segoe UI", 13.6f, FontStyle.Regular);
        private static readonly Font FontSmallBold = new Font("Segoe UI", 13.6f, FontStyle.Bold);

        // ── Fields ────────────────────────────────────────────────────
        private readonly DashboardController _controller;
        private Panel _activeNavItem;

        // ── Constructor ───────────────────────────────────────────────
        public DashboardForm()
        {
            _controller = new DashboardController();
            InitializeComponent();
            BindViewModel();
        }

        // ── ViewModel binding (View layer — no business logic) ────────

        /// <summary>
        /// Calls the Controller, receives a fully prepared ViewModel,
        /// and binds each piece to the corresponding UI control.
        /// </summary>
        private void BindViewModel()
        {
            // 1. User bar — from session (this is session state, not DB data)
            if (SessionManager.IsLoggedIn)
            {
                lblTopNavUser.UserName   = SessionManager.CurrentUser.StaffName;
                lblTopNavUser.Department = SessionManager.CurrentUser.Department ?? string.Empty;
            }
            else
            {
                lblTopNavUser.UserName   = "Guest";
                lblTopNavUser.Department = string.Empty;
            }

            lblPageSub.Text = "Premium Living Furniture Co.  ·  Overview as of " +
                              DateTime.Now.ToString("d MMMM yyyy");

            // 2. Ask controller for all dashboard data
            DashboardViewModel vm = _controller.LoadDashboard();

            // 3. KPI cards (2 rows × 4 cards)
            Panel[] kpiPanels = { kpiOrders, kpiDelivered, kpiQuotations, kpiLowStock,
                                  kpiRevenue, kpiAR,       kpiSuppliers,  kpiCustomers };
            for (int i = 0; i < kpiPanels.Length && i < vm.Kpis.Count; i++)
                SetKpiCard(kpiPanels[i], vm.Kpis[i]);

            // 4. Recent Orders grid
            foreach (var row in vm.Orders)
            {
                var (bg, fg) = Palette.TagColours(row.Status);
                int idx = dgvOrders.Rows.Add(row.OrderId, row.Customer, row.Total, row.Status);
                dgvOrders.Rows[idx].Tag = new[] { bg, fg };
            }

            // 5. Low-Stock grid
            // (built into Designer's AddLowStockRows; now replaced by ViewModel data)
            BindLowStockGrid(vm.LowStock);

            // 6. Pending Quotations grid
            foreach (var row in vm.Quotations)
                dgvQuotations.Rows.Add(row.QuotationId, row.Customer, row.Amount, row.ValidUntil);

            // 7. Active Shipments grid
            foreach (var row in vm.Shipments)
            {
                var (bg, fg) = Palette.TagColours(row.Status);
                int idx = dgvShipments.Rows.Add(row.ShipmentId, row.Customer, row.SchedDate, row.Status);
                dgvShipments.Rows[idx].Tag = new[] { bg, fg };
            }

            // 8. Supplier Payments grid
            foreach (var row in vm.Suppliers)
            {
                var (bg, fg) = Palette.TagColours(row.Status);
                int idx = dgvSuppliers.Rows.Add(row.Supplier, row.InvoiceId, row.Amount, row.Status);
                dgvSuppliers.Rows[idx].Tag = new[] { bg, fg };
            }

            // 9. Activity feed
            foreach (var row in vm.Activities)
                AddActivity(Palette.FromKey(row.CategoryKey),
                            row.BoldText, row.NormalText, row.TimeLabel);
        }

        // ── Helpers (pure UI — no data/logic) ────────────────────────

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

        private void AddActivity(Color dotColor, string boldText, string normalText, string time)
        {
            Panel row = new Panel
            {
                Dock = DockStyle.Top, Height = 50,
                Padding = new Padding(0, 8, 0, 8),
                BackColor = Color.Transparent
            };
            Panel dot = new Panel { Width = 13, Height = 13, BackColor = dotColor, Location = new Point(0, 18) };
            dot.Region = MakeCircleRegion(13, 13);

            Label lblBold = new Label { Text = boldText, Font = FontBodyBold,
                ForeColor = Palette.TextMain, AutoSize = true, Location = new Point(21, 14) };
            Label lblNorm = new Label { Text = normalText, Font = FontBody,
                ForeColor = Palette.TextMain, AutoSize = true,
                Location = new Point(21 + TextRenderer.MeasureText(boldText, FontBodyBold).Width, 14) };
            Label lblTime = new Label { Text = time, Font = FontSmall,
                ForeColor = Palette.TextMuted, AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                TextAlign = ContentAlignment.TopRight };
            lblTime.Location = new Point(pnlActivity.Width - lblTime.PreferredWidth - 6, 16);

            row.Controls.Add(dot);
            row.Controls.Add(lblBold);
            row.Controls.Add(lblNorm);
            row.Controls.Add(lblTime);

            Panel sep = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Palette.BorderColor };
            pnlActivity.Controls.Add(sep);
            pnlActivity.Controls.Add(row);
            pnlActivity.Controls.SetChildIndex(row, 0);
            pnlActivity.Controls.SetChildIndex(sep, 1);
        }

        private static Region MakeCircleRegion(int w, int h)
        {
            GraphicsPath p = new GraphicsPath();
            p.AddEllipse(0, 0, w, h);
            return new Region(p);
        }

        // ── Nav / logout ──────────────────────────────────────────────

        private void OnTopNavMenuItemClicked(string itemLabel)
        {
            if (itemLabel == "Dashboard") { lblBreadcrumb.Text = "Dashboard"; return; }
            lblBreadcrumb.Text = itemLabel;
            MessageBox.Show(
                $"⌛  {itemLabel}\n\nThis feature is currently under development.",
                "Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        // ── Cell painting (pure rendering — no logic) ─────────────────

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

        private void dgvOrders_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
            => PaintStatusCell(sender, e, 3);
        private void dgvShipments_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
            => PaintStatusCell(sender, e, 3);
        private void dgvSuppliers_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
            => PaintStatusCell(sender, e, 3);
    }
}
