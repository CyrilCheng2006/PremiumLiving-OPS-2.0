using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Dashboard;
using System;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.Auth
{
    /// <summary>
    /// UC-019  Login Account
    /// Staff enters Staff ID and Password to access the system.
    /// </summary>
    public partial class LoginForm : Form
    {
        private readonly StaffRepo staffRepo = new StaffRepo();

        // ── Shared session: logged-in staff accessible system-wide ───
        public static Staff CurrentUser { get; private set; }

        // ── Constructor ────────────────────────────────────────────
        public LoginForm()
        {
            InitializeComponent();
        }

        // ── Event Handlers ────────────────────────────────────────

        /// <summary>
        /// Triggered when user clicks the Login button (or presses Enter).
        /// Flow: validate inputs → StaffRepo.Login → open Dashboard or show error.
        /// </summary>
        private void btnLogin_Click(object sender, EventArgs e)
        {
            string staffId  = txtStaffId.Text.Trim();
            string password = txtPassword.Text;

            // ── Input validation ───────────────────────────────────
            if (string.IsNullOrEmpty(staffId) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter your Staff ID and Password.",
                                "Missing Input",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                return;
            }

            // ── Authenticate ───────────────────────────────────────
            Staff staff = staffRepo.Login(staffId, password);

            if (staff != null)
            {
                CurrentUser = staff;

                // ── Open Dashboard and hide Login window ─────────────
                DashboardForm dashboard = new DashboardForm();
                dashboard.FormClosed += (s, args) => this.Close(); // close app when Dashboard exits
                dashboard.Show();
                this.Hide();
            }
            else
            {
                // UC-019 Alternative Flow: wrong credentials → show error
                MessageBox.Show("Incorrect Staff ID or Password. Please try again.",
                                "Login Failed",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);

                txtPassword.Clear();
                txtPassword.Focus();
            }
        }

        /// <summary>Allow Enter key on Password field to trigger login.</summary>
        private void txtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                btnLogin_Click(sender, e);
        }

        /// <summary>Allow Enter key on StaffId field to move focus to Password.</summary>
        private void txtStaffId_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                txtPassword.Focus();
        }
    }
}
