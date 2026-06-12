using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.OrderProcessing
{
    public partial class QuotationForm : Form
    {
        private readonly OrderProcessingController _ctrl   = new OrderProcessingController();
        private QuotationViewModel                 _vm;
        private AppShell                           _shell;

        public QuotationForm()
        {
            InitializeComponent();
            Load += QuotationForm_Load;
        }

        // ── Load ──────────────────────────────────────────────────────────────

        private void QuotationForm_Load(object sender, EventArgs e)
        {
            _vm = _ctrl.GetQuotationListVM();

            // Wire AppShell (Line 31 fix: ApplyViewModel exists on AppShell)
            _shell = _appShell;           // _appShell is the designer-placed AppShell control
            _shell.ApplyViewModel(_vm.UserBar);

            LoadGrid();
        }

        // ── Grid ───────────────────────────────────────────────────────────────

        private void LoadGrid()
        {
            dgvQuotations.Rows.Clear();
            if (_vm?.Quotations == null) return;

            foreach (var q in _vm.Quotations)
            {
                int idx = dgvQuotations.Rows.Add(
                    q.QuotationID,
                    q.CustomerName,
                    q.IssuedDate.ToString("yyyy-MM-dd"),
                    q.ExpiryDate.ToString("yyyy-MM-dd"),
                    string.Format("HK$ {0:N2}", q.TotalAmount),
                    q.QuotationStatus,
                    q.SalesStaffName);

                // colour-code status
                var row = dgvQuotations.Rows[idx];
                switch (q.QuotationStatus?.ToLower())
                {
                    case "pending":
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(180, 120, 0);
                        break;
                    case "converted":
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(5, 130, 80);
                        break;
                    case "rejected":
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(160, 30, 30);
                        break;
                }
            }
        }

        // ── Search / filter ────────────────────────────────────────────────────

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            string kw = txtSearch.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(kw)) { LoadGrid(); return; }

            dgvQuotations.Rows.Clear();
            if (_vm?.Quotations == null) return;

            var filtered = _vm.Quotations.Where(q =>
                (q.QuotationID    ?? "").ToLower().Contains(kw) ||
                (q.CustomerName   ?? "").ToLower().Contains(kw) ||
                (q.QuotationStatus?? "").ToLower().Contains(kw) ||
                (q.SalesStaffName ?? "").ToLower().Contains(kw));

            foreach (var q in filtered)
                dgvQuotations.Rows.Add(
                    q.QuotationID,
                    q.CustomerName,
                    q.IssuedDate.ToString("yyyy-MM-dd"),
                    q.ExpiryDate.ToString("yyyy-MM-dd"),
                    string.Format("HK$ {0:N2}", q.TotalAmount),
                    q.QuotationStatus,
                    q.SalesStaffName);
        }

        private void cboStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            string kw = txtSearch.Text.Trim().ToLower();
            string status = cboStatus.SelectedItem?.ToString();

            dgvQuotations.Rows.Clear();
            if (_vm?.Quotations == null) return;

            var filtered = _vm.Quotations.AsEnumerable();
            if (!string.IsNullOrEmpty(kw))
                filtered = filtered.Where(q =>
                    (q.QuotationID ?? "").ToLower().Contains(kw) ||
                    (q.CustomerName?? "").ToLower().Contains(kw));
            if (!string.IsNullOrEmpty(status) && status != "All")
                filtered = filtered.Where(q => q.QuotationStatus == status);

            foreach (var q in filtered)
                dgvQuotations.Rows.Add(
                    q.QuotationID,
                    q.CustomerName,
                    q.IssuedDate.ToString("yyyy-MM-dd"),
                    q.ExpiryDate.ToString("yyyy-MM-dd"),
                    string.Format("HK$ {0:N2}", q.TotalAmount),
                    q.QuotationStatus,
                    q.SalesStaffName);
        }

        // ── Button handlers ────────────────────────────────────────────────────

        /// <summary>
        /// "Create New Quotation" button — opens the CreateNewQuotationForm dialog.
        /// </summary>
        private void btnCreateNew_Click(object sender, EventArgs e)
        {
            using (var dlg = new CreateNewQuotationForm())
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    // Refresh the list after a new quotation is saved
                    _vm = _ctrl.GetQuotationListVM();
                    LoadGrid();
                }
            }
        }

        /// <summary>"View Detail" button — opens read-only QuotationDetailForm.</summary>
        private void btnViewDetail_Click(object sender, EventArgs e)
        {
            if (dgvQuotations.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a quotation to view.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string qid = dgvQuotations.SelectedRows[0].Cells["colQuotationID"].Value?.ToString();
            if (string.IsNullOrEmpty(qid)) return;

            var entity = _ctrl.GetQuotationDetail(qid);
            if (entity == null)
            {
                MessageBox.Show("Could not load quotation detail.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var dlg = new QuotationDetailForm(entity))
                dlg.ShowDialog(this);
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            _vm = _ctrl.GetQuotationListVM();
            LoadGrid();
        }

        private void dgvQuotations_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) btnViewDetail_Click(sender, EventArgs.Empty);
        }
    }
}
