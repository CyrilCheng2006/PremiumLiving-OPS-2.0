using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.StatisticalReports
{
    partial class ViewReportForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── AppShell ────────────────────────────────────────────────────────────────
        private AppShell _shell;

        // ── Sidebar report buttons ────────────────────────────────────────────
        private Button btnSales;
        private Button btnInventory;
        private Button btnProcurement;
        private Button btnLogistics;
        private Button btnAfterService;
        private Button btnFinance;

        // ── Content panel (rebuilt on each report switch) ────────────────────
        internal Panel pnlContent;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // ── Form ───────────────────────────────────────────────────────────────
            this.Text          = "Premium Living OPS — Statistical Reports";
            this.Size          = new Size(1440, 900);
            this.MinimumSize   = new Size(1280, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = Color.FromArgb(240, 244, 249);
            this.WindowState   = FormWindowState.Maximized;
            this.Font          = new Font("Segoe UI", 13f);
            // NOTE: No AutoScaleMode/AutoScaleDimensions — mirrors HGR pattern.

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  Root panel
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            var pnlMain = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 244, 249)
            };

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  AppShell — RULE 2/4
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            _shell = new AppShell();
            _shell.SetPopupContainer(pnlMain);
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += OnLogoutClicked;

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  Sidebar (DockStyle.Left, Width 200)
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            Button MakeSideBtn(string text)
            {
                var b = new Button
                {
                    Text      = text,
                    Font      = new Font("Segoe UI", 11f),
                    ForeColor = Palette.SidebarText,
                    BackColor = Color.Transparent,
                    FlatStyle = FlatStyle.Flat,
                    Dock      = DockStyle.Top,
                    Height    = 44,
                    Cursor    = Cursors.Hand,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding   = new Padding(16, 0, 0, 0)
                };
                b.FlatAppearance.BorderSize         = 0;
                b.FlatAppearance.MouseOverBackColor = Color.FromArgb(235, 241, 255);
                b.FlatAppearance.MouseDownBackColor = Color.FromArgb(219, 234, 254);
                return b;
            }

            btnSales        = MakeSideBtn("\U0001F4B0  Sales Performance");
            btnInventory    = MakeSideBtn("\U0001F4E6  Inventory Status");
            btnProcurement  = MakeSideBtn("\U0001F4CB  Procurement");
            btnLogistics    = MakeSideBtn("\U0001F69A  Logistics");
            btnAfterService = MakeSideBtn("\U0001F527  After-Service");
            btnFinance      = MakeSideBtn("\U0001F4B3  Finance");

            btnSales.Click        += (s, e) => BtnSales_Click(s, e);
            btnInventory.Click    += (s, e) => BtnInventory_Click(s, e);
            btnProcurement.Click  += (s, e) => BtnProcurement_Click(s, e);
            btnLogistics.Click    += (s, e) => BtnLogistics_Click(s, e);
            btnAfterService.Click += (s, e) => BtnAfterService_Click(s, e);
            btnFinance.Click      += (s, e) => BtnFinance_Click(s, e);

            // Sidebar outer card (CardPanel 3-layer)
            var (pnlSideOuter, pnlSideCard) = CardPanel.Create(
                outerHeight  : 0,
                outerPadding : new Padding(12, 8, 0, 8));
            pnlSideOuter.Dock  = DockStyle.Left;
            pnlSideOuter.Width = 210;
            pnlSideCard.Dock   = DockStyle.Fill;

            // Add buttons bottom-up so DockStyle.Top stacks correctly
            pnlSideCard.Controls.Add(btnFinance);
            pnlSideCard.Controls.Add(btnAfterService);
            pnlSideCard.Controls.Add(btnLogistics);
            pnlSideCard.Controls.Add(btnProcurement);
            pnlSideCard.Controls.Add(btnInventory);
            pnlSideCard.Controls.Add(btnSales);

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  Content panel (DockStyle.Fill — rebuilt by each RenderXxx)
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            pnlContent = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding   = new Padding(0)
            };

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  Assemble — RULE 5: Fill first, Left next, _shell LAST (topmost)
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            pnlMain.Controls.Add(pnlContent);    // DockStyle.Fill  — first
            pnlMain.Controls.Add(pnlSideOuter);  // DockStyle.Left
            pnlMain.Controls.Add(_shell);         // DockStyle.Top   — LAST = topmost

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }
    }
}
