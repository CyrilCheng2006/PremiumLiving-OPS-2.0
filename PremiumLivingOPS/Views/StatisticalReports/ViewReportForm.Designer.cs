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

            // ── Root panel ──────────────────────────────────────────────
            var pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage };

            // ── AppShell (RULE 2) ──────────────────────────────────────────
            _shell = new AppShell();
            _shell.SetPopupContainer(pnlMain);
            _shell.MenuItemClicked += (menu, sub) => OnTopNavMenuItemClicked(menu, sub);
            _shell.LogoutClicked   += (s, e)      => BtnLogout_Click(s, e);

            // ════════════════════════════════════════════════════════════
            //  BODY — content panel fills all remaining space
            //  (Sidebar buttons removed; report selection is handled
            //   exclusively by the Tab Bar card inside pnlContent)
            // ════════════════════════════════════════════════════════════
            var pnlBody = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Palette.BgPage,
                Padding   = new Padding(16, 12, 16, 16)
            };

            // ── CONTENT PANEL ──────────────────────────────────────────────
            pnlContent = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Palette.BgPage,
                Padding   = new Padding(0)
            };

            pnlBody.Controls.Add(pnlContent);

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
