using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.Shared
{
    partial class AppShell
    {
        private System.ComponentModel.IContainer components = null;

        // User Bar controls
        private Label lblStaffName;
        private Label lblStaffRole;

        // Nav Bar panel
        private Panel pnlNavBar;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Dock      = DockStyle.Fill;
            this.BackColor = Palette.BgPage;

            // ── Root layout: left nav | right content placeholder
            var tblRoot = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                RowCount    = 1,
                BackColor   = System.Drawing.Color.Transparent
            };
            tblRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200f));
            tblRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // ── Left Nav Bar
            pnlNavBar = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Palette.NavBg
            };

            // ── Right panel: User Bar (top) + content area placeholder (rest)
            var pnlRight = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                RowCount    = 2,
                ColumnCount = 1,
                BackColor   = System.Drawing.Color.Transparent
            };
            pnlRight.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f));
            pnlRight.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            pnlRight.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            // User Bar
            var pnlUserBar = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Palette.CardBg,
                Padding   = new Padding(16, 0, 16, 0)
            };

            lblStaffName = new Label
            {
                AutoSize  = true,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Palette.TextMain,
                Anchor    = AnchorStyles.Right | AnchorStyles.Top,
            };
            lblStaffRole = new Label
            {
                AutoSize  = true,
                Font      = new Font("Segoe UI", 10f),
                ForeColor = Palette.TextMuted,
                Anchor    = AnchorStyles.Right | AnchorStyles.Top,
            };

            pnlUserBar.Controls.Add(lblStaffName);
            pnlUserBar.Controls.Add(lblStaffRole);
            pnlUserBar.Resize += (s, e) =>
            {
                lblStaffName.Location = new System.Drawing.Point(pnlUserBar.Width - lblStaffName.Width - 16, 10);
                lblStaffRole.Location = new System.Drawing.Point(pnlUserBar.Width - lblStaffRole.Width - 16, 32);
            };

            pnlRight.Controls.Add(pnlUserBar, 0, 0);

            tblRoot.Controls.Add(pnlNavBar, 0, 0);
            tblRoot.Controls.Add(pnlRight,  1, 0);

            this.Controls.Add(tblRoot);
            this.ResumeLayout(false);
        }
    }
}
