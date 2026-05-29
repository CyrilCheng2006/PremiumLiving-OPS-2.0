using PremiumLivingOPS.Views.Dashboard;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.Dashboard
{
    public partial class DashboardForm : Form
    {
        // ── Brand colours ────────────────────────────────────────────────
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
        }

        // ── Shared fonts (scaled to 20f base) ──────────────────────────
        private static readonly Font FontBody      = new Font("Segoe UI", 20f,  FontStyle.Regular);
        private static readonly Font FontBodyBold  = new Font("Segoe UI", 20f,  FontStyle.Bold);
        private static readonly Font FontSmall     = new Font("Segoe UI", 17f,  FontStyle.Regular);
        private static readonly Font FontSmallBold = new Font("Segoe UI", 17f,  FontStyle.Bold);
        private static readonly Font FontTitle     = new Font("Segoe UI", 32f,  FontStyle.Bold);
        private static readonly Font FontKpiVal    = new Font("Segoe UI", 38f,  FontStyle.Bold);
        private static readonly Font FontKpiLabel  = new Font("Segoe UI", 13f,  FontStyle.Bold);

        private Panel _activeNavItem;

        private static Region MakeCircleRegion(int width, int height)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddEllipse(0, 0, width, height);
            return new Region(path);
        }

        public DashboardForm()
        {
            InitializeComponent();
            PopulateDashboard();
        }

        // ====================================================================
        // DATA POPULATION
        // ====================================================================
        private void PopulateDashboard()
        {
            if (SessionManager.IsLoggedIn)
            {
                lblTopNavUser.Text = SessionManager.CurrentUser.StaffName;
                lblAvatar.Text     = GetInitials(SessionManager.CurrentUser.StaffName);
            }
            else
            {
                lblTopNavUser.Text = "Guest";
                lblAvatar.Text     = "?";
            }

            lblPageSub.Text = "Premium Living Furniture Co.  \u00B7  Overview as of " +
                              DateTime.Now.ToString("d MMMM yyyy");

            SetKpiCard(kpiOrders,     "TOTAL ORDERS (Mar)",   "6",       Palette.Primary,  "2 Pending \u00B7 1 Processing \u00B7 1 Shipped");
            SetKpiCard(kpiDelivered,  "DELIVERED THIS MONTH", "2",       Palette.Success,  "ORD-0045 \u00B7 ORD-0044");
            SetKpiCard(kpiQuotations, "PENDING QUOTATIONS",   "2",       Palette.Warning,  "QT-2026-0033 \u00B7 QT-2026-0034");
            SetKpiCard(kpiLowStock,   "LOW STOCK ALERTS",     "9",       Palette.Danger,   "Immediate procurement action needed");
            SetKpiCard(kpiRevenue,    "REVENUE THIS MONTH",   "HK$221K", Palette.Info,     "Based on delivered orders");
            SetKpiCard(kpiAR,         "OUTSTANDING AR",       "HK$130K", Palette.Warning,  "3 invoices unpaid / overdue");
            SetKpiCard(kpiSuppliers,  "ACTIVE SUPPLIERS",     "3",       Palette.Primary,  "1 On Hold \u00B7 1 Inactive");
            SetKpiCard(kpiCustomers,  "TOTAL CUSTOMERS",      "5",       Palette.Primary,  "1 VIP \u00B7 2 Corporate");

            AddOrderRow("ORD-2026-0048", "Chan Siu Ming",    "HK$21,300",  "Processing", Palette.TagBlueBg,   Palette.TagBlueFg);
            AddOrderRow("ORD-2026-0047", "Lee Wai Kwan",     "HK$29,400",  "Shipped",    Palette.TagGreenBg,  Palette.TagGreenFg);
            AddOrderRow("ORD-2026-0046", "ABC Furniture Ltd", "HK$120,700", "Pending",    Palette.TagYellowBg, Palette.TagYellowFg);
            AddOrderRow("ORD-2026-0045", "Wong Ka Fai",      "HK$26,500",  "Delivered",  Palette.TagGreenBg,  Palette.TagGreenFg);
            AddOrderRow("ORD-2026-0044", "Sunrise Interiors", "HK$57,600", "Delivered",  Palette.TagGreenBg,  Palette.TagGreenFg);

            AddQuotRow("QT-2026-0034", "Chan Siu Ming", "HK$38,400", "29 Mar 2026");
            AddQuotRow("QT-2026-0033", "Cheung Wai Ho", "HK$30,800", "24 Mar 2026");

            AddShipRow("DLV-2026-0033", "Chan Siu Ming", "15 Mar 2026", "Scheduled",  Palette.TagYellowBg, Palette.TagYellowFg);
            AddShipRow("DLV-2026-0031", "Lee Wai Kwan",  "12 Mar 2026", "In Transit", Palette.TagBlueBg,   Palette.TagBlueFg);
            AddShipRow("DLV-2026-0029", "Wong Ka Fai",   "10 Mar 2026", "Delivered",  Palette.TagGreenBg,  Palette.TagGreenFg);

            AddSupplierRow("Green Wood Co.", "INV-S-0041", "HK$14,000", "Pending", Palette.TagYellowBg, Palette.TagYellowFg);
            AddSupplierRow("MetalPro HK",    "INV-S-0039", "HK$2,400",  "Overdue", Palette.TagRedBg,    Palette.TagRedFg);
            AddSupplierRow("FabricPlus Ltd", "INV-S-0035", "HK$9,800",  "Paid",    Palette.TagGreenBg,  Palette.TagGreenFg);

            AddActivity(Palette.Primary, "ORD-2026-0048",   " created for Chan Siu Ming \u2013 HK$21,300", "15 Mar 15:42");
            AddActivity(Palette.Success, "QT-2026-0034",    " saved as Pending quotation",             "15 Mar 14:20");
            AddActivity(Palette.Warning, "Low stock alert", " triggered for Solid Oak Panel (8 left)", "15 Mar 11:05");
            AddActivity(Palette.Success, "ORD-2026-0044",   " marked Delivered \u2013 Sunrise Interiors",  "08 Mar 09:30");
            AddActivity(Palette.Danger,  "CMP-2026-0006",   " filed \u2013 Missing assembly kit",           "10 Mar 11:44");
            AddActivity(Palette.Warning, "MetalPro HK",     " invoice INV-S-0039 now Overdue",         "15 Mar 00:00");
            AddActivity(Palette.Primary, "DLV-2026-0031",   " status updated to In Transit",           "12 Mar 08:15");
        }

        // ====================================================================
        // HELPERS
        // ====================================================================
        private void SetKpiCard(Panel card, string label, string value, Color accent, string sub)
        {
            foreach (Control ctrl in card.Controls)
            {
                if (ctrl.Tag?.ToString() == "kpi-label") ctrl.Text = label;
                if (ctrl.Tag?.ToString() == "kpi-value") { ctrl.Text = value; ctrl.ForeColor = accent; }
                if (ctrl.Tag?.ToString() == "kpi-sub")   ctrl.Text = sub;
            }
        }

        private void AddOrderRow(string orderId, string customer, string total, string status, Color tagBg, Color tagFg)
        {
            int idx = dgvOrders.Rows.Add(orderId, customer, total, status);
            dgvOrders.Rows[idx].Tag = new[] { tagBg, tagFg };
        }

        private void AddQuotRow(string quotId, string customer, string amount, string validUntil)
            => dgvQuotations.Rows.Add(quotId, customer, amount, validUntil);

        private void AddShipRow(string shipId, string customer, string date, string status, Color tagBg, Color tagFg)
        {
            int idx = dgvShipments.Rows.Add(shipId, customer, date, status);
            dgvShipments.Rows[idx].Tag = new[] { tagBg, tagFg };
        }

        private void AddSupplierRow(string supplier, string invoice, string amount, string status, Color tagBg, Color tagFg)
        {
            int idx = dgvSuppliers.Rows.Add(supplier, invoice, amount, status);
            dgvSuppliers.Rows[idx].Tag = new[] { tagBg, tagFg };
        }

        private void AddActivity(Color dotColor, string boldText, string normalText, string time)
        {
            Panel row = new Panel
            {
                Dock = DockStyle.Top, Height = 62,
                Padding = new Padding(0, 10, 0, 10),
                BackColor = Color.Transparent
            };

            Panel dot = new Panel
            {
                Width = 16, Height = 16,
                BackColor = dotColor,
                Location = new Point(0, 23)
            };
            dot.Region = MakeCircleRegion(16, 16);

            Label lblBold = new Label
            {
                Text = boldText, Font = FontBodyBold,
                ForeColor = Palette.TextMain,
                AutoSize = true, Location = new Point(26, 18)
            };
            Label lblNorm = new Label
            {
                Text = normalText, Font = FontBody,
                ForeColor = Palette.TextMain,
                AutoSize = true,
                Location = new Point(26 + TextRenderer.MeasureText(boldText, FontBodyBold).Width, 18)
            };
            Label lblTime = new Label
            {
                Text = time, Font = FontSmall,
                ForeColor = Palette.TextMuted,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                TextAlign = ContentAlignment.TopRight
            };
            lblTime.Location = new Point(pnlActivity.Width - lblTime.PreferredWidth - 8, 20);

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

        private string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            string[] parts = name.Split(' ');
            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
                : name.Substring(0, Math.Min(2, name.Length)).ToUpper();
        }

        // ====================================================================
        // EVENT HANDLERS
        // ====================================================================
        private void SetActiveNav(Panel navPanel)
        {
            if (_activeNavItem != null)
            {
                _activeNavItem.BackColor = Color.Transparent;
                foreach (Control c in _activeNavItem.Controls)
                {
                    if (c is Label l) l.ForeColor = Palette.SidebarText;
                    if (c is Panel && c.Dock == DockStyle.Left && c.Width == 4)
                        c.BackColor = Color.Transparent;
                }
            }
            _activeNavItem           = navPanel;
            _activeNavItem.BackColor = Palette.SidebarHover;
            foreach (Control c in _activeNavItem.Controls)
            {
                if (c is Label l) l.ForeColor = Color.White;
                if (c is Panel && c.Dock == DockStyle.Left && c.Width == 4)
                    c.BackColor = Palette.Primary;
            }
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
                    e.CellBounds.X + 8,
                    e.CellBounds.Y + (e.CellBounds.Height - sz.Height - 8) / 2,
                    sz.Width + 24, sz.Height + 8);

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
                        badge.X + 12, badge.Y + 4);
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
