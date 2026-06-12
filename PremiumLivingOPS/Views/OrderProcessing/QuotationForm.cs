using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.OrderProcessing
{
    /// <summary>
    /// Quotation list page (View layer).
    /// All business logic is delegated to OrderProcessingController.
    /// </summary>
    public partial class QuotationForm : Form
    {
        private readonly OrderProcessingController _ctrl = new OrderProcessingController();
        private QuotationViewModel _vm;

        public QuotationForm()
        {
            InitializeComponent();
            Load += QuotationForm_Load;
        }

        // ── Lifecycle

        private void QuotationForm_Load(object sender, EventArgs e)
        {
            _vm = _ctrl.GetQuotationVM();
            // _shell is declared internal in Designer so it is accessible here
            _shell.ApplyViewModel(_vm.UserBar, _vm.AllowedMenus, this);
            RefreshGrid();
        }

        // ── Grid helpers

        internal void RefreshGrid()
        {
            string status  = cboStatus.SelectedItem?.ToString() == "All"
                             ? null : cboStatus.SelectedItem?.ToString();
            string keyword = txtSearchKeyword.Text.Trim();
            _vm = _ctrl.GetQuotationVM(status, keyword);

            dgvQuotations.Rows.Clear();
            foreach (var q in _vm.Quotations)
            {
                // LeadTimeEstimated is a string field (e.g. "14 days") — display directly
                string leadTime = string.IsNullOrEmpty(q.LeadTimeEstimated)
                                  ? "" : q.LeadTimeEstimated;

                dgvQuotations.Rows.Add(
                    q.QuotationID,
                    q.CustomerName,
                    q.ExpiryDate.ToString("yyyy-MM-dd"),
                    q.TotalAmount.ToString("C"),
                    q.DepositRequired.ToString("C"),
                    leadTime,
                    q.QuotationStatus);
            }

            BuildKpiPills(_vm.Quotations);
            ClearSelection();
        }

        private void ResetFilters()
        {
            txtSearchKeyword.Clear();
            cboStatus.SelectedIndex = 0;
            RefreshGrid();
        }

        private void ClearSelection()
        {
            dgvQuotations.ClearSelection();
            btnViewDetail.Enabled   = false;
            btnUpdateStatus.Enabled = false;
            cboNewStatus.Enabled    = false;
        }

        private void BuildKpiPills(List<QuotationEntity> list)
        {
            pnlKpi.Controls.Clear();

            // Avoid tuple-array syntax to ensure compatibility with all target frameworks
            BuildPill(list, "All",       q => true,                             Color.FromArgb( 59, 130, 246), 0);
            BuildPill(list, "Pending",   q => q.QuotationStatus == "Pending",   Color.FromArgb(245, 158,  11), 140);
            BuildPill(list, "Converted", q => q.QuotationStatus == "Converted", Color.FromArgb( 16, 185, 129), 280);
            BuildPill(list, "Rejected",  q => q.QuotationStatus == "Rejected",  Color.FromArgb(239,  68,  68), 420);
        }

        private void BuildPill(List<QuotationEntity> list,
                                string label,
                                Func<QuotationEntity, bool> predicate,
                                Color color,
                                int xOffset)
        {
            int cnt  = 0;
            foreach (var q in list) if (predicate(q)) cnt++;

            var pill = new Panel
            {
                Width     = 130,
                Height    = 48,
                Location  = new Point(xOffset, 0),
                BackColor = Color.Transparent
            };
            pill.Controls.Add(new Label
            {
                Text      = cnt.ToString(),
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = color,
                AutoSize  = false,
                Location  = new Point(0,  0),
                Width     = 130,
                Height    = 26,
                TextAlign = ContentAlignment.MiddleLeft
            });
            pill.Controls.Add(new Label
            {
                Text      = label,
                Font      = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(98, 112, 135),
                AutoSize  = false,
                Location  = new Point(0, 24),
                Width     = 130,
                Height    = 22,
                TextAlign = ContentAlignment.MiddleLeft
            });
            pnlKpi.Controls.Add(pill);
        }

        // ── Grid events

        private void dgvQuotations_SelectionChanged(object sender, EventArgs e)
        {
            bool hasRow = dgvQuotations.SelectedRows.Count > 0;
            btnViewDetail.Enabled   = hasRow;
            btnUpdateStatus.Enabled = hasRow;
            cboNewStatus.Enabled    = hasRow;
        }

        private void dgvQuotations_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex != dgvQuotations.Columns["colStatus"]?.Index) return;
            var status = e.Value?.ToString();
            Color fc;
            switch (status)
            {
                case "Pending":   fc = Color.FromArgb(180, 100,  0); break;
                case "Converted": fc = Color.FromArgb( 16, 120, 80); break;
                case "Rejected":  fc = Color.FromArgb(185,  28, 28); break;
                default:          fc = Color.FromArgb( 75,  85, 99); break;
            }
            e.CellStyle.ForeColor   = fc;
            e.CellStyle.Font        = new Font("Segoe UI", 11f, FontStyle.Bold);
            e.FormattingApplied     = true;
        }

        private void dgvQuotations_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            OpenViewDetail();
        }

        // ── Button handlers

        private void btnViewDetail_Click(object sender, EventArgs e)
        {
            OpenViewDetail();
        }

        /// <summary>
        /// Opens CreateNewQuotationForm as a dialog to create a brand-new Quotation.
        /// Refreshes the list automatically on successful save (DialogResult.OK).
        /// </summary>
        private void btnCreateNew_Click(object sender, EventArgs e)
        {
            // Use classic using-block for .NET Framework 4.x compatibility
            using (var form = new CreateNewQuotationForm())
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                    RefreshGrid();
            }
        }

        private void btnUpdateStatus_Click(object sender, EventArgs e)
        {
            if (dgvQuotations.SelectedRows.Count == 0) return;
            string qId       = dgvQuotations.SelectedRows[0].Cells["colQuotationID"].Value?.ToString();
            string newStatus = cboNewStatus.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(qId) || string.IsNullOrEmpty(newStatus)) return;

            if (_ctrl.UpdateQuotationStatus(qId, newStatus))
            {
                MessageBox.Show(
                    string.Format("Quotation {0} updated to '{1}'.", qId, newStatus),
                    "Status Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshGrid();
            }
            else
            {
                MessageBox.Show("Failed to update status. Please try again.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Detail helper

        private void OpenViewDetail()
        {
            if (dgvQuotations.SelectedRows.Count == 0) return;
            string qId = dgvQuotations.SelectedRows[0].Cells["colQuotationID"].Value?.ToString();
            if (string.IsNullOrEmpty(qId)) return;

            var entity = _ctrl.GetQuotationDetail(qId);
            if (entity == null)
            {
                MessageBox.Show("Quotation not found.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (var detail = new QuotationDetailForm(entity))
            {
                detail.ShowDialog(this);
            }
        }
    }
}
