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
        private Panel             pnlTabOuter;
        private TableLayoutPanel  tblTabs;
        private Button btnTab0, btnTab1, btnTab2, btnTab3, btnTab4, btnTab5;

        // KPI strip
        private Panel pnlKpiOuter;
        private Panel pnlKpi;

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
            this.SuspendLayout();

            // ── Form ────────────────────────────────────────────────────────
            this.Text          = "Statistical Reports · View Report";
            this.Size          = new Size(1440, 900);
            this.MinimumSize   = new Size(1200, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = Palette.BgPage;
            this.WindowState   = FormWindowState.Maximized;
            this.Font          = new Font("Segoe UI", 13f);
            // NOTE: AutoScaleMode / AutoScaleDimensions intentionally omitted.

            // ── pnlMain (Fill) ───────────────────────────────────────────────
            var pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage };

            // ── AppShell  ────────────────────────────────────────────────────
            _shell = new AppShell();
            _shell.SetPopupContainer(pnlMain);
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;

            // ── Tab bar outer (DockStyle.Top, 69 px) ────────────────────────
            // Baseline: HandlingGoodsReceivedForm pnlTabOuter
            //   Height=69, Padding=(20,4,20,0)
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
                Padding     = new Padding(8, 0, 8, 0)
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
                Padding   = new Padding(20, 4, 20, 0)   // ← HGR baseline exact
            };
            pnlTabOuter.Paint += PaintTabUnderline;
            pnlTabOuter.Controls.Add(pnlTabCard);

            // ── KPI bar outer (DockStyle.Top, 90 px) ─────────────────────────
            // Baseline: HandlingGoodsReceivedForm pnlKpiOuter
            //   Height=90, Padding=(20,8,20,8)
            pnlKpi = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            var pnlKpiInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlKpiInner.Paint += PaintCardBorder;
            pnlKpiInner.Controls.Add(pnlKpi);

            pnlKpiOuter = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 90,                          // ← HGR baseline (was 96)
                BackColor = Palette.BgPage,
                Padding   = new Padding(20, 8, 20, 8)   // ← HGR baseline (was (20,12,20,0))
            };
            pnlKpiOuter.Controls.Add(pnlKpiInner);

            // ── Filter bar outer (DockStyle.Top, 300 px) ─────────────────────
            // Baseline: HandlingGoodsReceivedForm pnlSearchOuter
            //   Height=300, Padding=(20,14,20,8)
            pnlFilterOuter = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 300,                         // ← HGR baseline (was 260)
                BackColor = Palette.BgPage,
                Padding   = new Padding(20, 14, 20, 8)  // ← HGR baseline (was (20,12,20,0))
            };
            // Content is populated dynamically by each Render*() method via SetFilterBar()

            // ── Report content host (DockStyle.Fill) ─────────────────────────
            pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage };

            // ── Assemble ──────────────────────────────────────────────────────
            pnlMain.Controls.Add(pnlContent);     // Fill — first
            pnlMain.Controls.Add(pnlTabOuter);    // Top
            pnlMain.Controls.Add(pnlKpiOuter);    // Top
            pnlMain.Controls.Add(pnlFilterOuter); // Top
            pnlMain.Controls.Add(_shell);          // Top — LAST = topmost chrome

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        private static void PaintCardBorder(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            var p = (Panel)sender;
            using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        }
    }
}
