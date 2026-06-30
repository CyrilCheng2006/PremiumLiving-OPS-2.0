using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.StatisticalReports
{
    partial class ViewReportForm
    {
        private System.ComponentModel.IContainer components = null;

        // AppShell (TopNavBar 44 px + UserBar 72 px = 116 px total)
        private AppShell _shell;

        // Tab bar
        private Panel            pnlTabOuter;
        private TableLayoutPanel tblTabs;
        private Button btnTab0, btnTab1, btnTab2, btnTab3, btnTab4, btnTab5;

        // Filter bar — outer grey wrapper only; card content built per-report
        private Panel pnlFilterOuter;

        // Report content host
        private Panel pnlContent;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();                                            // RULE 1

            // ── Form ─────────────────────────────────────────────────────────────
            this.Text                    = "Statistical Reports · View Report";
            this.Size                    = new Size(1440, 900);
            this.MinimumSize             = new Size(1200, 720);
            this.StartPosition           = FormStartPosition.CenterScreen;
            this.BackColor               = Palette.BgPage;
            this.WindowState             = FormWindowState.Maximized;
            this.Font                    = new Font("Segoe UI", 13f);
            this.AutoScaleMode           = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScaleDimensions     = new System.Drawing.SizeF(7F, 15F);

            // ── Root panel ────────────────────────────────────────────────────
            var pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage };

            // ── AppShell (RULE 2) ───────────────────────────────────────────────
            _shell             = new AppShell();                             // RULE 2
            _shell.Dock        = DockStyle.Top;                              // RULE 2 — explicit
            _shell.Height      = AppShell.TotalHeight;                       // RULE 2
            _shell.MinimumSize = new System.Drawing.Size(0, AppShell.TotalHeight); // RULE 2
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;               // RULE 4
            _shell.LogoutClicked   += btnLogout_Click;                       // RULE 4
            _shell.SetPopupContainer(pnlMain);

            // ── Report content host (DockStyle.Fill) ──────────────────────────────
            pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage };

            // ── Filter bar outer (DockStyle.Top, 300 px) ─────────────────────────
            pnlFilterOuter = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 300,
                BackColor = Palette.BgPage,
                Padding   = new Padding(20, 14, 20, 8)
            };

            // ── Tab bar outer (DockStyle.Top, 69 px) ────────────────────────────
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

            // ── Assemble pnlMain ──────────────────────────────────────────────────
            // Mirrors ViewShipmentForm exactly:
            //   Fill first, then Top panels bottom-to-top, _shell added LAST
            //   so it wins the topmost Top slot and both NavBar + UserBar are visible.
            pnlMain.Controls.Add(pnlContent);     // DockStyle.Fill — added first
            pnlMain.Controls.Add(pnlFilterOuter); // DockStyle.Top  — above content
            pnlMain.Controls.Add(pnlTabOuter);    // DockStyle.Top  — above filter bar
            pnlMain.Controls.Add(_shell);          // DockStyle.Top  — LAST = topmost chrome (RULE 5)

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
            this.PerformLayout();
            // ── Post-layout height re-enforcement (RULE 3) ─────────────────────
            _shell.Height      = AppShell.TotalHeight;
            _shell.MinimumSize = new System.Drawing.Size(0, AppShell.TotalHeight);
        }
        // PaintCardBorder is defined in ViewReportForm.cs (partial class) — do NOT redeclare here.
    }
}
