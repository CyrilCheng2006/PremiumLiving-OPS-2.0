namespace PremiumLivingOPS.Views.StatisticalReports
{
    partial class ViewReportForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // ── Form ──────────────────────────────────────────────────────────
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize          = new System.Drawing.Size(1280, 800);
            this.MinimumSize         = new System.Drawing.Size(900, 600);
            this.Name                = "ViewReportForm";
            this.Text                = "Statistical Reports – View Report";
            this.WindowState         = System.Windows.Forms.FormWindowState.Maximized;

            // ── AppShell ──────────────────────────────────────────────────────
            this._shell = new PremiumLivingOPS.Views.Shared.AppShell();
            this._shell.Height      = 112;
            this._shell.MinimumSize = new System.Drawing.Size(0, 112);
            this._shell.Dock        = System.Windows.Forms.DockStyle.Top;
            this._shell.MenuItemClicked += this.OnTopNavMenuItemClicked;
            this._shell.LogoutClicked   += this.btnLogout_Click;

            // ── pnlMain (fill) ────────────────────────────────────────────────
            this.pnlMain = new System.Windows.Forms.Panel();
            this.pnlMain.Dock      = System.Windows.Forms.DockStyle.Fill;
            this.pnlMain.BackColor = System.Drawing.Color.FromArgb(241, 244, 249);

            // ── pnlContent (fill within pnlMain) ─────────────────────────────
            this.pnlContent = new System.Windows.Forms.Panel();
            this.pnlContent.Dock      = System.Windows.Forms.DockStyle.Fill;
            this.pnlContent.BackColor = System.Drawing.Color.FromArgb(241, 244, 249);

            // ── pnlFilterOuter (top within pnlMain) ──────────────────────────
            this.pnlFilterOuter = new System.Windows.Forms.Panel();
            this.pnlFilterOuter.Dock      = System.Windows.Forms.DockStyle.Top;
            this.pnlFilterOuter.Height    = 270;
            this.pnlFilterOuter.BackColor = System.Drawing.Color.FromArgb(241, 244, 249);
            this.pnlFilterOuter.Padding   = new System.Windows.Forms.Padding(20, 12, 20, 0);

            // ── pnlTabOuter (top within pnlMain) ─────────────────────────────
            this.pnlTabOuter = new System.Windows.Forms.Panel();
            this.pnlTabOuter.Dock      = System.Windows.Forms.DockStyle.Top;
            this.pnlTabOuter.Height    = 52;
            this.pnlTabOuter.BackColor = System.Drawing.Color.White;
            this.pnlTabOuter.Padding   = new System.Windows.Forms.Padding(20, 0, 20, 0);
            this.pnlTabOuter.Paint    += this.PaintTabUnderline;

            // ── Tab buttons ───────────────────────────────────────────────────
            string[] tabLabels = { "Sales", "Inventory", "Procurement", "Logistics", "After-Service", "Finance" };
            int tabW = 150;
            this.btnTab0 = new System.Windows.Forms.Button();
            this.btnTab1 = new System.Windows.Forms.Button();
            this.btnTab2 = new System.Windows.Forms.Button();
            this.btnTab3 = new System.Windows.Forms.Button();
            this.btnTab4 = new System.Windows.Forms.Button();
            this.btnTab5 = new System.Windows.Forms.Button();
            System.Windows.Forms.Button[] tabs = { btnTab0, btnTab1, btnTab2, btnTab3, btnTab4, btnTab5 };
            for (int i = 0; i < tabs.Length; i++)
            {
                tabs[i].Text      = tabLabels[i];
                tabs[i].Width     = tabW;
                tabs[i].Height    = 52;
                tabs[i].Location  = new System.Drawing.Point(i * tabW, 0);
                tabs[i].FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                tabs[i].FlatAppearance.BorderSize = 0;
                tabs[i].Font      = new System.Drawing.Font("Segoe UI", 12f);
                tabs[i].BackColor = System.Drawing.Color.White;
                tabs[i].ForeColor = System.Drawing.Color.FromArgb(98, 112, 135);
                tabs[i].Cursor    = System.Windows.Forms.Cursors.Hand;
                int idx = i;
                tabs[i].Click += (s, e) => SwitchToReport(idx);
                this.pnlTabOuter.Controls.Add(tabs[i]);
            }

            // ── Report content host (DockStyle.Fill) ──────────────────────────
            // pnlContent is Fill — it absorbs all remaining space after Top panels.

            // ── Assemble ──────────────────────────────────────────────────────
            // Controls are added to pnlMain in reverse dock order:
            // Fill first, then Top panels last-to-first so _shell ends up topmost.
            this.pnlMain.Controls.Add(this.pnlContent);      // Fill
            this.pnlMain.Controls.Add(this.pnlFilterOuter);  // Top
            this.pnlMain.Controls.Add(this.pnlTabOuter);     // Top

            this.Controls.Add(this.pnlMain);   // Fill
            this.Controls.Add(this._shell);    // Top (last = topmost)

            this._shell.Height      = 112;
            this._shell.MinimumSize = new System.Drawing.Size(0, 112);

            this.ResumeLayout(false);
            this.PerformLayout();

            this._shell.Height      = 112;
            this._shell.MinimumSize = new System.Drawing.Size(0, 112);
        }

        private PremiumLivingOPS.Views.Shared.AppShell _shell;
        private System.Windows.Forms.Panel pnlMain;
        private System.Windows.Forms.Panel pnlContent;
        private System.Windows.Forms.Panel pnlFilterOuter;
        private System.Windows.Forms.Panel pnlTabOuter;
        private System.Windows.Forms.Button btnTab0;
        private System.Windows.Forms.Button btnTab1;
        private System.Windows.Forms.Button btnTab2;
        private System.Windows.Forms.Button btnTab3;
        private System.Windows.Forms.Button btnTab4;
        private System.Windows.Forms.Button btnTab5;
    }
}
