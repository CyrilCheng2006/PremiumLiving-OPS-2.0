using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.OrderProcessing
{
    /// <summary>
    /// Quotation — Tab 2 of Order Processing Management.
    /// Lists all quotations from the database and allows status updates.
    ///
    /// MVC contract (View layer):
    ///   • Calls OrderProcessingController to obtain QuotationViewModel.
    ///   • Uses AppShell (TopNavBar + UserBar) for navigation chrome.
    ///   • Contains NO business logic and NO direct DB calls.
    /// </summary>
    public partial class QuotationForm : Form
    {
        private readonly OrderProcessingController _ctrl = new OrderProcessingController();

        public QuotationForm()
        {
            InitializeComponent();
            this.Load += QuotationForm_Load;
        }

        // ── Load ───────────────────────────────────────────────────────────
        private void QuotationForm_Load(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void RefreshData(string statusFilter = null)
        {
            var vm = _ctrl.GetQuotationVM();

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Role);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Order Processing  ›  Quotation");

            dgvQuotations.Rows.Clear();
            foreach (var q in vm.Quotations)
            {
                // Apply optional status filter
                if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All"
                    && q.QuotationStatus != statusFilter)
                    continue;

                dgvQuotations.Rows.Add(
                    q.QuotationID,
                    q.CustomerName,
                    q.ExpiryDate.ToString("yyyy-MM-dd"),
                    $"HK$ {q.TotalAmount:N2}",
                    $"HK$ {q.DepositRequired:N2}",
                    q.LeadTimeEstimated,
                    q.QuotationStatus
                );
            }
        }

        // ── Event handlers ─────────────────────────────────────────────────
        private void cboStatusFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            string sel = cboStatusFilter.SelectedItem?.ToString();
            RefreshData(sel);
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            string sel = cboStatusFilter.SelectedItem?.ToString();
            RefreshData(sel);
        }

        /// <summary>
        /// Updates the selected quotation's status to the value chosen in cboNewStatus.
        /// Delegates business validation to the controller.
        /// </summary>
        private void btnUpdateStatus_Click(object sender, EventArgs e)
        {
            if (dgvQuotations.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a quotation first.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string quotationId = dgvQuotations.SelectedRows[0]
                .Cells["colQuotationID"].Value?.ToString();
            string newStatus = cboNewStatus.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(newStatus))
            {
                MessageBox.Show("Please select a new status.",
                    "No Status", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool ok = _ctrl.UpdateQuotationStatus(quotationId, newStatus);
            if (ok)
            {
                MessageBox.Show($"Quotation {quotationId} updated to '{newStatus}'.",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshData(cboStatusFilter.SelectedItem?.ToString());
            }
            else
            {
                MessageBox.Show("Failed to update quotation status. Please try again.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dgvQuotations_SelectionChanged(object sender, EventArgs e)
        {
            bool hasSelection = dgvQuotations.SelectedRows.Count > 0;
            btnUpdateStatus.Enabled = hasSelection;
            cboNewStatus.Enabled    = hasSelection;

            if (hasSelection)
            {
                string currentStatus = dgvQuotations.SelectedRows[0]
                    .Cells["colStatus"].Value?.ToString();
                // Pre-select current status in the combo
                int idx = cboNewStatus.FindStringExact(currentStatus);
                if (idx >= 0) cboNewStatus.SelectedIndex = idx;
            }
        }

        // ── TopNavBar navigation ──────────────────────────────────────────────
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }
    }
}
