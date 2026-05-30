namespace PremiumLivingOPS.Views.Auth
{
    partial class LoginForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── Controls ─────────────────────────────────────────────────
        private System.Windows.Forms.Label       lblTitle;
        private System.Windows.Forms.Label       lblStaffId;
        private System.Windows.Forms.Label       lblPassword;
        private System.Windows.Forms.TextBox     txtStaffId;
        private System.Windows.Forms.TextBox     txtPassword;
        private System.Windows.Forms.Button      btnLogin;
        private System.Windows.Forms.PictureBox  picLogo;
        private System.Windows.Forms.Panel       pnlMain;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            this.lblTitle   = new System.Windows.Forms.Label();
            this.lblStaffId = new System.Windows.Forms.Label();
            this.lblPassword= new System.Windows.Forms.Label();
            this.txtStaffId = new System.Windows.Forms.TextBox();
            this.txtPassword= new System.Windows.Forms.TextBox();
            this.btnLogin   = new System.Windows.Forms.Button();
            this.pnlMain    = new System.Windows.Forms.Panel();

            this.pnlMain.SuspendLayout();
            this.SuspendLayout();

            // ── pnlMain ──────────────────────────────────────────────
            this.pnlMain.BackColor = System.Drawing.Color.White;
            this.pnlMain.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlMain.Location = new System.Drawing.Point(80, 40);
            this.pnlMain.Name     = "pnlMain";
            this.pnlMain.Size     = new System.Drawing.Size(360, 340);
            this.pnlMain.Controls.AddRange(new System.Windows.Forms.Control[] {
                this.lblTitle, this.lblStaffId, this.lblPassword,
                this.txtStaffId, this.txtPassword, this.btnLogin
            });

            // ── lblTitle ─────────────────────────────────────────────
            this.lblTitle.Font      = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(31, 73, 125);
            this.lblTitle.Location  = new System.Drawing.Point(20, 30);
            this.lblTitle.Name      = "lblTitle";
            this.lblTitle.Size      = new System.Drawing.Size(320, 36);
            this.lblTitle.Text      = "Premium Living OPS";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // ── lblStaffId ───────────────────────────────────────────
            this.lblStaffId.Font     = new System.Drawing.Font("Segoe UI", 12F);
            this.lblStaffId.Location = new System.Drawing.Point(40, 100);
            this.lblStaffId.Name     = "lblStaffId";
            this.lblStaffId.Size     = new System.Drawing.Size(80, 22);
            this.lblStaffId.Text     = "Staff ID:";

            // ── txtStaffId ───────────────────────────────────────────
            this.txtStaffId.Font     = new System.Drawing.Font("Segoe UI", 11F);
            this.txtStaffId.Location = new System.Drawing.Point(130, 98);
            this.txtStaffId.Name     = "txtStaffId";
            this.txtStaffId.Size     = new System.Drawing.Size(180, 26);
            this.txtStaffId.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtStaffId_KeyDown);

            // ── lblPassword ──────────────────────────────────────────
            this.lblPassword.Font     = new System.Drawing.Font("Segoe UI", 12F);
            this.lblPassword.Location = new System.Drawing.Point(40, 150);
            this.lblPassword.Name     = "lblPassword";
            this.lblPassword.Size     = new System.Drawing.Size(80, 22);
            this.lblPassword.Text     = "Password:";

            // ── txtPassword ──────────────────────────────────────────
            this.txtPassword.Font         = new System.Drawing.Font("Segoe UI", 11F);
            this.txtPassword.Location     = new System.Drawing.Point(130, 148);
            this.txtPassword.Name         = "txtPassword";
            this.txtPassword.PasswordChar = '*';
            this.txtPassword.Size         = new System.Drawing.Size(180, 26);
            this.txtPassword.KeyDown     += new System.Windows.Forms.KeyEventHandler(this.txtPassword_KeyDown);

            // ── btnLogin ─────────────────────────────────────────────
            this.btnLogin.BackColor = System.Drawing.Color.FromArgb(31, 73, 125);
            this.btnLogin.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLogin.Font      = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnLogin.ForeColor = System.Drawing.Color.White;
            this.btnLogin.Location  = new System.Drawing.Point(110, 220);
            this.btnLogin.Name      = "btnLogin";
            this.btnLogin.Size      = new System.Drawing.Size(140, 38);
            this.btnLogin.Text      = "Login";
            this.btnLogin.UseVisualStyleBackColor = false;
            this.btnLogin.Click    += new System.EventHandler(this.btnLogin_Click);

            // ── LoginForm ────────────────────────────────────────────
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor     = System.Drawing.Color.FromArgb(240, 244, 248);
            this.ClientSize    = new System.Drawing.Size(520, 420);
            this.Controls.Add(this.pnlMain);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox    = false;
            this.Name           = "LoginForm";
            this.StartPosition  = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text           = "Login — Premium Living OPS";

            this.pnlMain.ResumeLayout(false);
            this.pnlMain.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}
