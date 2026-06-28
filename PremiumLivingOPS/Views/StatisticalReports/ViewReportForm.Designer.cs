using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;
using PremiumLivingOPS.Controllers;          // SessionManager

namespace PremiumLivingOPS.Views.StatisticalReports
{
    partial class ViewReportForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── AppShell ───────────────────────────────────────────────────────────────
        private AppShell _shell;

        // ── Top Tab Bar buttons (commit 8d7b970 baseline) ─────────────────────
        private Button btnTabSalesRevenue;
        private Button btnTabInventory;
        private Button btnTabProduction;
        private Button btnTabLogistics;
        private Button btnTabAfterService;

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
            _shell.MenuItemClicked += (menu, sub) => OnTopNavMenuItemClicked(menu, sub);
            _shell.LogoutClicked   += (s, e) => { SessionManager.Clear(); Application.Restart(); };

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  Top Tab Bar (DockStyle.Top, Height 48)
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            Button MakeTabBtn(string text)
            {
                var b = new Button
                {
                    Text      = text,
                    Font      = new Font("Segoe UI", 12f),
                    ForeColor = Color.FromArgb(98, 112, 135),
                    BackColor = Color.Transparent,
                    FlatStyle = FlatStyle.Flat,
                    Dock      = DockStyle.Left,
                    Width     = 170,
                    Cursor    = Cursors.Hand,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Padding   = new Padding(0, 0, 0, 3)
                };
                b.FlatAppearance.BorderSize         = 0;
                b.FlatAppearance.MouseOverBackColor = Color.FromArgb(235, 241, 255);
                b.FlatAppearance.MouseDownBackColor = Color.FromArgb(219, 234, 254);
                return b;
            }

            btnTabSalesRevenue = MakeTabBtn("Sales & Revenue");
            btnTabInventory    = MakeTabBtn("Inventory");
            btnTabProduction   = MakeTabBtn("Procurement");
            btnTabLogistics    = MakeTabBtn("Logistics");
            btnTabAfterService = MakeTabBtn("After-Service");

            btnTabSalesRevenue.Click += (s, e) => SwitchToReport(0);
            btnTabInventory.Click    += (s, e) => SwitchToReport(1);
            btnTabProduction.Click   += (s, e) => SwitchToReport(2);
            btnTabLogistics.Click    += (s, e) => SwitchToReport(3);
            btnTabAfterService.Click += (s, e) => SwitchToReport(4);

            var pnlTabBar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 48,
                BackColor = Color.White
            };
            // Add right-to-left so DockStyle.Left stacks left-to-right
            pnlTabBar.Controls.Add(btnTabAfterService);
            pnlTabBar.Controls.Add(btnTabLogistics);
            pnlTabBar.Controls.Add(btnTabProduction);
            pnlTabBar.Controls.Add(btnTabInventory);
            pnlTabBar.Controls.Add(btnTabSalesRevenue);

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  Content panel (DockStyle.Fill — rebuilt by each BuildXxxReport)
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            pnlContent = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding   = new Padding(0)
            };

            // Placeholder — replaced on first SwitchToReport() call
            pnlContent.Controls.Add(MakePlaceholderLabel("Select a report from the tab bar above"));

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  Assemble — RULE 5: Fill first, Top (tabBar) next, _shell LAST
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            pnlMain.Controls.Add(pnlContent);   // DockStyle.Fill  — first
            pnlMain.Controls.Add(pnlTabBar);    // DockStyle.Top
            pnlMain.Controls.Add(_shell);        // DockStyle.Top   — LAST = topmost

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        // ── Placeholder label (used in Designer only) ─────────────────────────
        private static Label MakePlaceholderLabel(string text) => new Label
        {
            Text      = text,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = Color.FromArgb(98, 112, 135)
        };
    }
}
