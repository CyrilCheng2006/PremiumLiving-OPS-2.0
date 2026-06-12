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
        private readonly OrderProcessingController _ctrl = new OrderProcessingController();
        private QuotationViewModel _vm;
        // NOTE: _shell is declared in QuotationForm.Designer.cs (internal AppShell _shell)
        // DO NOT re-declare it here — that causes CS0102 duplicate definition.

        public QuotationForm()
        {
            InitializeComponent();
            Load += QuotationForm_Load;
        }

        // ── Load ──────────────────────────────────────────────────────────────────────────────

        private void QuotationForm_Load(object sender, EventArgs e)
        {
            // ── Wire up AppShell navigation (MUST be here, not in Designer.cs)
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;

            _vm = _ctrl.GetQuotationListVM();

            if (_vm?.UserBar != null)
                _shell.SetUser(_vm.UserBar.DisplayName, _vm.UserBar.Department);

            if (_vm?.AllowedMenus != null)
                _shell.SetVisibleMenus(_vm.AllowedMenus);

            _shell.SetBreadcrumb("Order Processing  ›  Quotation");

            LoadGrid();
            UpdateKpiBar();
        }

        // ── Navigation ───────────────────────────────────────────────────────────────────

        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }

        // ── Grid ──────────────────────────────────────────────────────────────────────────────

        private void LoadGrid()
        {
            dgvQuotations.Rows.Clear();
            if (_vm?.Quotations == null) return;

            foreach (var q in _vm.Quotations)
            {
                dgvQuotations.Rows.Add(
                    q.QuotationID,
                    q.CustomerName,
                    q.ExpiryDate.ToString("yyyy-MM-dd"),
                    string.Format("HK$ {0:N2}", q.TotalAmount),
                    string.Format("HK$ {0:N2}", q.DepositRequired),
                    q.LeadTimeEstimated,
                    q.QuotationStatus);
            }
        }

        private void RefreshGrid()
        {
            string kw     = txtSearchKeyword.Text.Trim().ToLower();
            string status = cboStatus.SelectedItem?.ToString();

            _vm = _ctrl.GetQuotationListVM();

            var filtered = _vm.Quotations.AsEnumerable();
            if (!string.IsNullOrEmpty(kw))
                filtered = filtered.Where(q =>
                    (q.QuotationID   ?? "").ToLower().Contains(kw) ||
                    (q.CustomerName  ?? "").ToLower().Contains(kw));
            if (!string.IsNullOrEmpty(status) && status != "All")
                filtered = filtered.Where(q => q.QuotationStatus == status);

            dgvQuotations.Rows.Clear();
            foreach (var q in filtered)
                dgvQuotations.Rows.Add(
                    q.QuotationID,
                    q.CustomerName,
                    q.ExpiryDate.ToString("yyyy-MM-dd"),
                    string.Format("HK$ {0:N2}", q.TotalAmount),
                    string.Format("HK$ {0:N2}", q.DepositRequired),
                    q.LeadTimeEstimated,
                    q.QuotationStatus);

            UpdateKpiBar();
        }

        private void ResetFilters()
        {
            txtSearchKeyword.Clear();
            cboStatus.SelectedIndex = 0;
            _vm = _ctrl.GetQuotationListVM();
            LoadGrid();
            UpdateKpiBar();
        }

        // ── KPI bar ───────────────────────────────────────────────────────────────────────

        private void UpdateKpiBar()
        {
            pnlKpi.Controls.Clear();
            if (_vm?.Quotations == null) return;

            int total     = _vm.Quotations.Count;
            int pending   = _vm.Quotations.Count(q => q.QuotationStatus == "Pending");
            int converted = _vm.Quotations.Count(q => q.QuotationStatus == "Converted");
            int rejected  = _vm.Quotations.Count(q => q.QuotationStatus == "Rejected");

            int x = 0;
            foreach (var kv in new[]
            {
                ("Total",     total.ToString(),     Palette.Primary),
                ("Pending",   pending.ToString(),   Color.FromArgb(180, 120, 0)),
                ("Converted", converted.ToString(), Color.FromArgb(5,  130, 80)),
                ("Rejected",  rejected.ToString(),  Color.FromArgb(160, 30, 30))
            })
            {
                var chip = new Panel
                {
                    Location  = new Point(x, 8),
                    Size      = new Size(160, 44),
                    BackColor = Color.White
                };
                chip.Controls.Add(new Label
                {
                    Text      = kv.Item2 + "  " + kv.Item1,
                    Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                    ForeColor = kv.Item3,
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                });
                pnlKpi.Controls.Add(chip);
                x += 168;
            }
        }

        // ── Selection changed ────────────────────────────────────────────────────────────

        private void dgvQuotations_SelectionChanged(object sender, EventArgs e)
        {
            bool sel = dgvQuotations.SelectedRows.Count > 0;
            btnViewDetail.Enabled   = sel;
            btnUpdateStatus.Enabled = sel;
            cboNewStatus.Enabled    = sel;
        }

        // ── Cell formatting (colour-code status) ──────────────────────────────

        private void dgvQuotations_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvQuotations.Columns[e.ColumnIndex].Name != "colStatus") return;
            switch (e.Value?.ToString()?.ToLower())
            {
                case "pending":   e.CellStyle.ForeColor = Color.FromArgb(180, 120,  0); break;
                case "converted": e.CellStyle.ForeColor = Color.FromArgb(  5, 130, 80); break;
                case "rejected":  e.CellStyle.ForeColor = Color.FromArgb(160,  30, 30); break;
            }
        }

        // ── Button handlers ──────────────────────────────────────────────────────────────────

        /// <summary>Create New Quotation — opens CreateNewQuotationForm dialog.</summary>
        private void btnCreateNew_Click(object sender, EventArgs e)
        {
            using (var dlg = new CreateNewQuotationForm())
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _vm = _ctrl.GetQuotationListVM();
                    LoadGrid();
                    UpdateKpiBar();
                }
            }
        }

        /// <summary>View Detail — opens read-only QuotationDetailForm.</summary>
        private void btnViewDetail_Click(object sender, EventArgs e)
        {
            if (dgvQuotations.SelectedRows.Count == 0) return;

            string qid = dgvQuotations.SelectedRows[0]
                .Cells["colQuotationID"].Value?.ToString();
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

        /// <summary>Update Status — persists the selected status combo value.</summary>
        private void btnUpdateStatus_Click(object sender, EventArgs e)
        {
            if (dgvQuotations.SelectedRows.Count == 0) return;

            string qid       = dgvQuotations.SelectedRows[0]
                .Cells["colQuotationID"].Value?.ToString();
            string newStatus = cboNewStatus.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(qid) || string.IsNullOrEmpty(newStatus)) return;

            bool ok = _ctrl.UpdateQuotationStatus(qid, newStatus);
            if (ok)
            {
                MessageBox.Show(
                    string.Format("Quotation {0} updated to '{1}'.", qid, newStatus),
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _vm = _ctrl.GetQuotationListVM();
                LoadGrid();
                UpdateKpiBar();
            }
            else
            {
                MessageBox.Show("Failed to update status. Please try again.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dgvQuotations_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) btnViewDetail_Click(sender, EventArgs.Empty);
        }
    }
}
