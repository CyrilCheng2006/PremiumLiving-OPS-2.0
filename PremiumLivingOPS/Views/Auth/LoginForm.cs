using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Services;
using PremiumLivingOPS.Views.Dashboard;
using System;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.Auth
{
    /// <summary>
    /// UC-019  Login Account
    /// Staff enters Staff ID and Password to access the system.
    ///
    /// MVC contract:
    ///   This View calls StaffRepo via a thin inline path (no dedicated
    ///   AuthController yet), then delegates session state to
    ///   <see cref="SessionManager"/> in the Controller layer.
    ///   On successful login, a Login audit record is written to the
    ///   MySQL Log table via AuditLogger.WriteAs().
    /// </summary>
    public partial class LoginForm : Form
    {
        private readonly StaffRepo _staffRepo = new StaffRepo();

        public LoginForm()
        {
            InitializeComponent();
        }

        // ── Event Handlers ─────────────────────────────────────────

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string staffId  = txtStaffId.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(staffId) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter your Staff ID and Password.",
                                "Missing Input",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                return;
            }

            Staff staff = _staffRepo.Login(staffId, password);

            if (staff != null)
            {
                // Store in the central session (Controller layer)
                SessionManager.SetUser(staff);

                // ── Write Login audit to MySQL Log table ──────────────
                WriteLoginAudit(staff.StaffID);

                DashboardForm dashboard = new DashboardForm();
                dashboard.FormClosed += (s, args) => this.Close();
                dashboard.Show();
                this.Hide();
            }
            else
            {
                MessageBox.Show("Incorrect Staff ID or Password. Please try again.",
                                "Login Failed",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                txtPassword.Clear();
                txtPassword.Focus();
            }
        }

        private void txtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) btnLogin_Click(sender, e);
        }

        private void txtStaffId_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) txtPassword.Focus();
        }
    }
}
