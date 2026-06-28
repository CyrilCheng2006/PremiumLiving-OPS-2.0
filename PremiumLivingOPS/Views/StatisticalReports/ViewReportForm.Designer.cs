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

        // Filter bar
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
            // Setting them causes WinForms font-scaling to recalculate
            // _shell.Height after ResumeLayout and collapse the UserBar.

            // ── pnlMain (Fill) ───────────────────────────────────────────────
            var pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage };

            // ── AppShell  ────────────────────────────────────────────────────
            // DO NOT set Dock / Height / MinimumSize externally here.
            // AppShell.OnLayout + ScaleControl self-lock height at 116 px.
            _shell = new AppShell();
            _shell.SetPopupContainer(pnlMain);
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;  // RULE 4 — once
            _shell.LogoutClicked   += btnLogout_Click;          // RULE 4 — once

            // ── Tab bar outer (DockStyle.Top, 69 px) ────────────────────────
            // Mirrors HGR: Height=69, outer Padding=(20,4,20,0)
            // tblTabs inner Padding=(8,0,8,0) matching HGR tab switcher
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
                // HGR baseline: tab switcher uses (8,0,8,0) inner padding so
                // buttons don't press against the white card left/right edges.
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
                // HGR baseline: (20,4,20,0) — 0 bottom lets the active tab
                // underline sit flush against the card bottom edge.
                Padding   = new Padding(20, 4, 20, 0)
            };
            pnlTabOuter.Paint   += PaintTabUnderline;
            pnlTabOuter.Controls.Add(pnlTabCard);

            // ── KPI bar outer (DockStyle.Top, 90 px) ─────────────────────────
            // HGR baseline: Height=90, Padding=(20,8,20,8)
            pnlKpi = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(12, 10, 12, 10)
            };

            var pnlKpiInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlKpiInner.Paint += PaintCardBorder;
            pnlKpiInner.Controls.Add(pnlKpi);

            pnlKpiOuter = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 90,
                BackColor = Palette.BgPage,
                // HGR baseline: (20,8,20,8) — equal top/bottom grey margin
                Padding   = new Padding(20, 8, 20, 8)
            };
            pnlKpiOuter.Controls.Add(pnlKpiInner);

            // ── Filter bar outer (DockStyle.Top, 72 px) ──────────────────────
            // HGR baseline reference: pnlSearchOuter uses top=14 for its large
            // multi-row form card.  VRF uses a compact single-row filter strip;
            // top=12 gives proportional breathing room while keeping height lean.
            //
            //   Outer H = 72 px
            //   Padding  = (20, 12, 20, 8)   → grey left/right = 20 px (= HGR)
            //   Inner usable height = 72 − 12 − 8 = 52 px
            //   Filter buttons h = 40 px → centred with 6 px clearance each side
            pnlFilterOuter = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 72,
                BackColor = Palette.BgPage,
                Padding   = new Padding(20, 12, 20, 8)
            };
            // Content is populated dynamically by each Render*() method via SetFilterBar()

            // ── Report content host (DockStyle.Fill) ─────────────────────────
            pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage };

            // ── Assemble ──────────────────────────────────────────────────────
            // HGR canonical order: Fill first, Top sections bottom→top, _shell LAST.
            //   pnlGridHost    (Fill)
            //   pnlTabOuter    (Top)
            //   pnlKpiOuter    (Top)
            //   pnlSearchOuter (Top)
            //   _shell          (Top — topmost chrome)
            pnlMain.Controls.Add(pnlContent);     // Fill — first
            pnlMain.Controls.Add(pnlTabOuter);    // Top
            pnlMain.Controls.Add(pnlKpiOuter);    // Top
            pnlMain.Controls.Add(pnlFilterOuter); // Top
            pnlMain.Controls.Add(_shell);          // Top — LAST = topmost chrome

            this.Controls.Add(pnlMain);

            // NOTE: ResumeLayout(false) only — NO PerformLayout().
            // PerformLayout triggers AutoScaleMode font-scaling which can
            // recalculate and collapse AppShell.Height (UserBar disappears).
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
