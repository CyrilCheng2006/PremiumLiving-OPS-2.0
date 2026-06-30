using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.StatisticalReports
{
    partial class ViewReportForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── AppShell (TopNavBar 44 px + UserBar 72 px = 116 px total) ──
        private AppShell _shell;

        // ── Tab bar controls ───────────────────────────────────────────
        private Panel            pnlTabOuter;
        private TableLayoutPanel tblTabs;
        private Button btnTab0, btnTab1, btnTab2, btnTab3, btnTab4, btnTab5;

        // ── Filter bar outer wrapper ───────────────────────────────────
        private Panel pnlFilterOuter;

        // ── Report content host ────────────────────────────────────────
        private Panel pnlContent;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();                                   // RULE 1 — must be first

            // ── Form properties ───────────────────────────────────────
            this.Text                = "Statistical Reports \u00b7 View Report";
            this.Size                = new Size(1440, 900);
            this.MinimumSize         = new Size(1200, 720);
            this.StartPosition       = FormStartPosition.CenterScreen;
            this.BackColor           = Palette.BgPage;
            this.WindowState         = FormWindowState.Maximized;
            this.Font                = new Font("Segoe UI", 13f);
            this.AutoScaleMode       = AutoScaleMode.Font;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);

            // ── Root panel ────────────────────────────────────────────
            var pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage };

            // ── AppShell — identical pattern to StaffListForm ─────────
            _shell             = new AppShell();
            _shell.Dock        = DockStyle.Top;
            _shell.Height      = AppShell.TotalHeight;
            _shell.MinimumSize = new Size(0, AppShell.TotalHeight);
            _shell.SetPopupContainer(pnlMain);
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;      // RULE 4 — subscribe here only
            _shell.LogoutClicked   += btnLogout_Click;              // RULE 4 — subscribe here only

            // ── Tab buttons ───────────────────────────────────────────
            Button MakeTabBtn(string text, int idx)
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
                b.Click += (s, e) => SwitchToReport(idx);
                return b;
            }

            btnTab0 = MakeTabBtn("Sales Performance",     0);
            btnTab1 = MakeTabBtn("Inventory Status",      1);
            btnTab2 = MakeTabBtn("Procurement Summary",   2);
            btnTab3 = MakeTabBtn("Logistics Overview",    3);
            btnTab4 = MakeTabBtn("After-Service Summary", 4);
            btnTab5 = MakeTabBtn("Finance Overview",      5);

            tblTabs = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                BackColor   = Color.White,
                ColumnCount = 6,
                RowCount    = 1,
                Margin      = new Padding(0),
                Padding     = new Padding(0)
            };
            for (int i = 0; i < 6; i++)
                tblTabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 6f));
            tblTabs.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblTabs.Controls.Add(btnTab0, 0, 0);
            tblTabs.Controls.Add(btnTab1, 1, 0);
            tblTabs.Controls.Add(btnTab2, 2, 0);
            tblTabs.Controls.Add(btnTab3, 3, 0);
            tblTabs.Controls.Add(btnTab4, 4, 0);
            tblTabs.Controls.Add(btnTab5, 5, 0);

            var pnlTabCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlTabCard.Paint += PaintCardBorder;
            pnlTabCard.Controls.Add(tblTabs);

            pnlTabOuter = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 69,
                BackColor = Palette.BgPage,
                Padding   = new Padding(20, 4, 20, 0)
            };
            pnlTabOuter.Paint += PaintTabUnderline;
            pnlTabOuter.Controls.Add(pnlTabCard);

            // ── Filter bar outer ──────────────────────────────────────
            pnlFilterOuter = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 300,
                BackColor = Palette.BgPage,
                Padding   = new Padding(20, 14, 20, 8)
            };

            // ── Report content host ───────────────────────────────────
            pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage };

            // ── pnlPage: all page content below _shell ─────────────────
            // Add order inside pnlPage: Fill first, then Top panels (bottom-to-top)
            var pnlPage = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage };
            pnlPage.Controls.Add(pnlContent);      // Fill — always underneath
            pnlPage.Controls.Add(pnlFilterOuter);  // Top  — stacks above Fill
            pnlPage.Controls.Add(pnlTabOuter);     // Top  — stacks above pnlFilterOuter

            // ── pnlMain: Fill = pnlPage, Top = _shell (AppShell RULE 5) ──
            pnlMain.Controls.Add(pnlPage);   // Fill — page content
            pnlMain.Controls.Add(_shell);    // Top  — AppShell chrome (UserBar always visible)

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
            this.PerformLayout();

            // ── RULE 3 — re-enforce _shell height after layout ─────────
            _shell.Height      = AppShell.TotalHeight;
            _shell.MinimumSize = new Size(0, AppShell.TotalHeight);
        }

        // PaintCardBorder is defined in ViewReportForm.cs (partial class) — do NOT redeclare here.
    }
}
