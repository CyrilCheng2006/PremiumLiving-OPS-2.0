using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.StatisticalReports
{
    partial class ViewReportForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── AppShell (TopNavBar 44 px + UserBar 72 px = 116 px total) ─────────
        private AppShell _shell;

        // ── Tab switcher buttons ───────────────────────────────────────────────
        private Button btnTabSalesRevenue;
        private Button btnTabInventory;
        private Button btnTabProduction;
        private Button btnTabLogistics;
        private Button btnTabAfterService;

        // ── Right-side content panel (rebuilt on each report switch) ──────────
        internal Panel pnlContent;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // ── Form ──────────────────────────────────────────────────────────
            this.Text          = "Premium Living OPS — Statistical Reports";
            this.Size          = new Size(1440, 900);
            this.MinimumSize   = new Size(1280, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = Color.FromArgb(240, 244, 249);
            this.WindowState   = FormWindowState.Maximized;
            this.Font          = new Font("Segoe UI", 13f);
            // NOTE: No AutoScaleMode / AutoScaleDimensions — mirrors HGR pattern
            //       to prevent WinForms font-scaling collapsing the UserBar.

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  Root panel
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            var pnlMain = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 244, 249)
            };

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  AppShell  — RULE 2: construct inside SuspendLayout scope.
            //  SetPopupContainer() wires the dropdown overlay.
            //  Event subscriptions here ONCE (RULE 4) — never in .cs Load.
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            _shell = new AppShell();
            _shell.SetPopupContainer(pnlMain);
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;  // RULE 4
            _shell.LogoutClicked   += btnLogout_Click;           // RULE 4

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  Tab switcher bar  (DockStyle.Top, Height 69)
            //  — identical construction to HGR's pnlTabOuter / pnlTabCard
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            Button MakeTabBtn(string text)
            {
                var b = new Button
                {
                    Text      = text,
                    Font      = new Font("Segoe UI", 12f),
                    ForeColor = Color.FromArgb(98, 112, 135),
                    BackColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Dock      = DockStyle.Fill,
                    Cursor    = Cursors.Hand,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Padding   = new Padding(0, 0, 0, 3)
                };
                b.FlatAppearance.BorderSize         = 0;
                b.FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 248, 255);
                b.FlatAppearance.MouseDownBackColor = Color.FromArgb(235, 241, 255);
                return b;
            }

            btnTabSalesRevenue = MakeTabBtn("\U0001F4B0  Sales & Revenue");
            btnTabInventory    = MakeTabBtn("\U0001F4E6  Inventory");
            btnTabProduction   = MakeTabBtn("\U0001F3ED  Production");
            btnTabLogistics    = MakeTabBtn("\U0001F69A  Logistics");
            btnTabAfterService = MakeTabBtn("\U0001F527  After-Service");

            btnTabSalesRevenue.Click += (s, e) => SwitchToReport(0);
            btnTabInventory.Click    += (s, e) => SwitchToReport(1);
            btnTabProduction.Click   += (s, e) => SwitchToReport(2);
            btnTabLogistics.Click    += (s, e) => SwitchToReport(3);
            btnTabAfterService.Click += (s, e) => SwitchToReport(4);

            var tblTabs = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                RowCount    = 1,
                ColumnCount = 5,
                BackColor   = Color.White,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding     = new Padding(8, 0, 8, 0)
            };
            tblTabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            tblTabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            tblTabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            tblTabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            tblTabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            tblTabs.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblTabs.Controls.Add(btnTabSalesRevenue, 0, 0);
            tblTabs.Controls.Add(btnTabInventory,    1, 0);
            tblTabs.Controls.Add(btnTabProduction,   2, 0);
            tblTabs.Controls.Add(btnTabLogistics,    3, 0);
            tblTabs.Controls.Add(btnTabAfterService, 4, 0);

            // CardPanel wrapping (Tab Bar card)
            var (pnlTabOuter, pnlTabCard) = CardPanel.Create(
                outerHeight  : 69,
                outerPadding : new Padding(20, 4, 20, 0));
            pnlTabCard.Controls.Add(tblTabs);

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  Content panel  (DockStyle.Fill — rebuilt by controller)
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            pnlContent = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding   = new Padding(0)
            };

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  Assemble — RULE 5: Fill first, Top reverse-order, _shell LAST
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            pnlMain.Controls.Add(pnlContent);   // DockStyle.Fill  — added first
            pnlMain.Controls.Add(pnlTabOuter);  // DockStyle.Top
            pnlMain.Controls.Add(_shell);        // DockStyle.Top   — LAST = topmost

            this.Controls.Add(pnlMain);

            // NOTE: ResumeLayout(false) only — NO PerformLayout() call.
            //       Mirrors HGR; PerformLayout() triggers AutoScaleMode
            //       font-scaling which can collapse AppShell.Height.
            this.ResumeLayout(false);
        }

        // ── Card border painter (shared utility) ───────────────────────────────
        private static void PaintCardBorder(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            var p = (Panel)sender;
            using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        }
    }
}
