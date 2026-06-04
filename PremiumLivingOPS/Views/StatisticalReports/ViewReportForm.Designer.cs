using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.StatisticalReports
{
    partial class ViewReportForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── AppShell (mandatory chrome)
        private AppShell _shell;

        // ── Sidebar report selector buttons
        internal Button btnSales;
        internal Button btnInventory;
        internal Button btnProcurement;
        internal Button btnLogistics;
        internal Button btnAfterService;
        internal Button btnFinance;

        // ── Right-side content panel (rebuilt on each report switch)
        internal Panel pnlContent;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // ── Form ──────────────────────────────────────────────────────
            this.Text          = "Premium Living OPS — Statistical Reports";
            this.Size          = new Size(1440, 900);
            this.MinimumSize   = new Size(1280, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = Palette.BgPage;
            this.WindowState   = FormWindowState.Maximized;
            this.Font          = new Font("Segoe UI", 13f);

            // ── Root panel ────────────────────────────────────────────────
            var pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage };

            // ── AppShell (RULE 2) ──────────────────────────────────────────
            _shell = new AppShell();
            _shell.SetPopupContainer(pnlMain);
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += BtnLogout_Click;

            // ════════════════════════════════════════════════════════════
            //  BODY — sidebar (Left) + content (Fill)
            // ════════════════════════════════════════════════════════════
            var pnlBody = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage, Padding = new Padding(16, 12, 16, 16) };

            // ── SIDEBAR ──────────────────────────────────────────────────
            var pnlSidebar = new Panel
            {
                Dock      = DockStyle.Left,
                Width     = 230,
                BackColor = Palette.SidebarBg,
                Padding   = new Padding(0, 12, 0, 12)
            };

            // Section label
            var lblSection = new Label
            {
                Text      = "REPORTS",
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(130, 155, 185),
                BackColor = Color.Transparent,
                Dock      = DockStyle.Top,
                Height    = 36,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(20, 0, 0, 0)
            };

            // Sidebar button factory
            Button MakeSidebarBtn(string icon, string label)
            {
                var b = new Button
                {
                    Text      = $"  {icon}  {label}",
                    Font      = new Font("Segoe UI", 11f),
                    ForeColor = Palette.SidebarText,
                    BackColor = Color.Transparent,
                    FlatStyle = FlatStyle.Flat,
                    Dock      = DockStyle.Top,
                    Height    = 54,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Cursor    = Cursors.Hand,
                    Padding   = new Padding(8, 0, 0, 0)
                };
                b.FlatAppearance.BorderSize         = 0;
                b.FlatAppearance.MouseOverBackColor = Palette.SidebarHover;
                b.FlatAppearance.MouseDownBackColor = Color.FromArgb(Palette.Primary.R, Palette.Primary.G, Palette.Primary.B, 200);
                return b;
            }

            // Build buttons in reverse dock order (last added = topmost in layout)
            btnFinance      = MakeSidebarBtn("💰", "Finance Overview");
            btnAfterService = MakeSidebarBtn("🔧", "After-Service");
            btnLogistics    = MakeSidebarBtn("🚚", "Logistics");
            btnProcurement  = MakeSidebarBtn("📦", "Procurement");
            btnInventory    = MakeSidebarBtn("🗄", "Inventory Status");
            btnSales        = MakeSidebarBtn("📊", "Sales Performance");

            // Divider
            var divider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(40, 65, 100), Margin = new Padding(0, 4, 0, 4) };

            // Add controls to sidebar (Fill-first / Top-last rule)
            var pnlSidebarFill = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };  // spacer
            pnlSidebar.Controls.Add(pnlSidebarFill);   // Fill
            pnlSidebar.Controls.Add(btnFinance);        // Top
            pnlSidebar.Controls.Add(btnAfterService);   // Top
            pnlSidebar.Controls.Add(btnLogistics);      // Top
            pnlSidebar.Controls.Add(btnProcurement);    // Top
            pnlSidebar.Controls.Add(btnInventory);      // Top
            pnlSidebar.Controls.Add(btnSales);          // Top
            pnlSidebar.Controls.Add(divider);           // Top
            pnlSidebar.Controls.Add(lblSection);        // Top

            // ── CONTENT PANEL ─────────────────────────────────────────────
            pnlContent = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Palette.BgPage,
                Padding   = new Padding(16, 0, 0, 0)
            };

            // Assemble body (Fill first, then Left)
            pnlBody.Controls.Add(pnlContent);  // Fill
            pnlBody.Controls.Add(pnlSidebar);  // Left

            // ════════════════════════════════════════════════════════════
            //  Assemble pnlMain (RULE 5 — Fill before Top)
            // ════════════════════════════════════════════════════════════
            pnlMain.Controls.Add(pnlBody);    // Fill
            pnlMain.Controls.Add(_shell);     // Top — AppShell last = topmost

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
            this.PerformLayout();

            // RULE 3 — lock AppShell height
            _shell.Height      = AppShell.TotalHeight;
            _shell.MinimumSize = new System.Drawing.Size(0, AppShell.TotalHeight);
        }
    }
}
