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

            btnFinance      = MakeSidebarBtn("\U0001F4B0", "Finance Overview");
            btnAfterService = MakeSidebarBtn("\U0001F527", "After-Service");
            btnLogistics    = MakeSidebarBtn("\U0001F69A", "Logistics");
            btnProcurement  = MakeSidebarBtn("\U0001F4E6", "Procurement");
            btnInventory    = MakeSidebarBtn("\U0001F5C4", "Inventory Status");
            btnSales        = MakeSidebarBtn("\U0001F4CA", "Sales Performance");

            var divider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(40, 65, 100) };

            var pnlSidebarFill = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlSidebar.Controls.Add(pnlSidebarFill);
            pnlSidebar.Controls.Add(btnFinance);
            pnlSidebar.Controls.Add(btnAfterService);
            pnlSidebar.Controls.Add(btnLogistics);
            pnlSidebar.Controls.Add(btnProcurement);
            pnlSidebar.Controls.Add(btnInventory);
            pnlSidebar.Controls.Add(btnSales);
            pnlSidebar.Controls.Add(divider);
            pnlSidebar.Controls.Add(lblSection);

            // ── CONTENT PANEL ─────────────────────────────────────────────
            pnlContent = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Palette.BgPage,
                Padding   = new Padding(16, 0, 0, 0)
            };

            pnlBody.Controls.Add(pnlContent);
            pnlBody.Controls.Add(pnlSidebar);

            pnlMain.Controls.Add(pnlBody);
            pnlMain.Controls.Add(_shell);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
            this.PerformLayout();

            _shell.Height      = AppShell.TotalHeight;
            _shell.MinimumSize = new System.Drawing.Size(0, AppShell.TotalHeight);
        }
    }
}
