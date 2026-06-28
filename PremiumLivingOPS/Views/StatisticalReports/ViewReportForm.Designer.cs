using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.StatisticalReports
{
    partial class ViewReportForm
    {
        private System.ComponentModel.IContainer components = null;

        private AppShell _shell;
        private Panel pnlMain;
        private Panel pnlTopTabs;
        private Panel pnlTabOuter;
        private TableLayoutPanel tblTabs;
        private Button btnTab0;
        private Button btnTab1;
        private Button btnTab2;
        private Button btnTab3;
        private Button btnTab4;
        private Button btnTab5;
        private Panel pnlContent;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            SuspendLayout();                                              // RULE 1

            // ── Form ─────────────────────────────────────────────────────────────
            this.BackColor          = Palette.BgPage;
            this.Text               = "Statistical Reports \u00b7 View Report";
            this.StartPosition      = FormStartPosition.CenterScreen;
            this.WindowState        = FormWindowState.Maximized;
            this.MinimumSize        = new Size(1440, 900);
            this.Font               = new Font("Segoe UI", 10F);
            this.AutoScaleMode      = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);

            // ── pnlMain (Fill) ────────────────────────────────────────────────────
            pnlMain = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Palette.BgPage
            };

            // ── pnlTopTabs — Tab Bar outer container (grey bg, leaves side margin)
            pnlTopTabs = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 69,
                BackColor = Palette.BgPage,
                Padding   = new Padding(20, 4, 20, 0)
            };

            // ── pnlTabOuter — white card that hosts the tab buttons
            pnlTabOuter = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.White
            };
            pnlTabOuter.Paint += PaintTabUnderline;

            // ── tblTabs — 6-column equal-width TableLayoutPanel
            tblTabs = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                BackColor       = Color.White,
                ColumnCount     = 6,
                RowCount        = 1,
                Margin          = new Padding(0),
                Padding         = new Padding(0)
            };
            for (int i = 0; i < 6; i++)
                tblTabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 6f));
            tblTabs.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            btnTab0 = CreateTabButton("Sales Performance",    0);
            btnTab1 = CreateTabButton("Inventory Status",     1);
            btnTab2 = CreateTabButton("Procurement Summary",  2);
            btnTab3 = CreateTabButton("Logistics Overview",   3);
            btnTab4 = CreateTabButton("After-Service Summary",4);
            btnTab5 = CreateTabButton("Finance Overview",     5);

            tblTabs.Controls.Add(btnTab0, 0, 0);
            tblTabs.Controls.Add(btnTab1, 1, 0);
            tblTabs.Controls.Add(btnTab2, 2, 0);
            tblTabs.Controls.Add(btnTab3, 3, 0);
            tblTabs.Controls.Add(btnTab4, 4, 0);
            tblTabs.Controls.Add(btnTab5, 5, 0);

            pnlTabOuter.Controls.Add(tblTabs);
            pnlTopTabs.Controls.Add(pnlTabOuter);

            // ── pnlContent — report body (Fill)
            pnlContent = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Palette.BgPage
            };

            // ── AppShell — RULE 2: construct inside SuspendLayout scope
            _shell = new AppShell();
            _shell.Dock        = DockStyle.Top;
            _shell.Height      = AppShell.TotalHeight;
            _shell.MinimumSize = new Size(0, AppShell.TotalHeight);
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;            // RULE 4
            _shell.SetPopupContainer(pnlMain);

            // ── Wire hierarchy
            // pnlMain: Fill first, then Top (RULE 5)
            pnlMain.Controls.Add(pnlContent);   // DockStyle.Fill  — goes first
            pnlMain.Controls.Add(pnlTopTabs);   // DockStyle.Top   — stacks above content
            pnlMain.Controls.Add(_shell);        // DockStyle.Top   — wins topmost

            this.Controls.Add(pnlMain);

            ResumeLayout(false);
            PerformLayout();

            // RULE 3: re-enforce after PerformLayout
            _shell.Height      = AppShell.TotalHeight;
            _shell.MinimumSize = new Size(0, AppShell.TotalHeight);
        }

        private Button CreateTabButton(string text, int tabIndex)
        {
            var btn = new Button
            {
                Dock                  = DockStyle.Fill,
                FlatStyle             = FlatStyle.Flat,
                BackColor             = Color.White,
                ForeColor             = Color.FromArgb(98, 112, 135),
                Font                  = new Font("Segoe UI", 12F, FontStyle.Regular),
                Text                  = text,
                Margin                = new Padding(0),
                Padding               = new Padding(0, 0, 0, 4),
                Cursor                = Cursors.Hand,
                TabStop               = false,
                UseVisualStyleBackColor = false
            };
            btn.FlatAppearance.BorderSize            = 0;
            btn.FlatAppearance.MouseDownBackColor    = Color.White;
            btn.FlatAppearance.MouseOverBackColor    = Color.White;
            btn.Click += (s, e) => SwitchToReport(tabIndex);
            return btn;
        }
    }
}
