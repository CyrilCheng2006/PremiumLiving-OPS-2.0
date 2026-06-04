using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.AfterService
{
    public partial class AccountPayableForm : Form
    {
        private readonly AfterServiceController _ctrl = new AfterServiceController();
        private List<AccountPayableEntity> _currentItems = new List<AccountPayableEntity>();

        private static readonly Dictionary<string, (Color bg, Color fg)> StatusColors =
            new Dictionary<string, (Color, Color)>
            {
                { "Partial", (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "Full",    (Color.FromArgb(220, 252, 231), Color.FromArgb( 22, 101,  52)) }
            };

        public AccountPayableForm()
        {
            InitializeComponent();
            this.Load += AccountPayableForm_Load;
        }

        private void AccountPayableForm_Load(object sender, EventArgs e)
        {
            RefreshGrid();
        }

        // ── Refresh ────────────────────────────────────────────────────────
        private void RefreshGrid()
        {
            string status = cboStatus.SelectedItem?.ToString();
            if (status == "All" || string.IsNullOrEmpty(status)) status = null;

            var vm = _ctrl.GetAccountPayableVM(status);

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("After-Service  ›  Accounts Payable");

            _currentItems = vm.Items;

            dgvAP.Rows.Clear();
            foreach (var item in _currentItems)
                dgvAP.Rows.Add(
                    item.PurInvoiceID,
                    item.PurchaseID,
                    item.SupplierName,
                    $"HK$ {item.TotalAmount:N2}",
                    item.PaymentStatus,
                    item.ExpectedDate.ToString("yyyy-MM-dd"));

            RefreshKpi(vm.Items);
        }

        // ── KPI labels ────────────────────────────────────────────────────
        private void RefreshKpi(List<AccountPayableEntity> items)
        {
            var all = _ctrl.GetAccountPayableVM().Items;

            int total          = all.Count;
            double outstanding = 0;
            int overdueCount   = 0;
            foreach (var i in all)
            {
                if (i.PaymentStatus != "Full") outstanding += i.TotalAmount;
                if (i.IsOverdue) overdueCount++;
            }

            lblTotalAP.Text      = total.ToString();
            lblOutstanding.Text  = $"HK$ {outstanding:N2}";
            lblOverdueCount.Text = overdueCount.ToString();
        }

        // ── CellFormatting: status badge + overdue row highlight ──────────
        private void dgvAP_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _currentItems.Count) return;
            var item = _currentItems[e.RowIndex];

            // Overdue row: light red background
            if (item.IsOverdue)
            {
                e.CellStyle.BackColor          = Color.FromArgb(255, 235, 235);
                e.CellStyle.SelectionBackColor = Color.FromArgb(254, 202, 202);
            }

            // Status column badge
            string colName = dgvAP.Columns[e.ColumnIndex].Name;
            if (colName == "colStatus" && e.Value != null)
            {
                if (item.IsOverdue)
                {
                    e.CellStyle.BackColor = Color.FromArgb(232, 64, 64);
                    e.CellStyle.ForeColor = Color.White;
                    e.CellStyle.SelectionBackColor = Color.FromArgb(185, 28, 28);
                    e.CellStyle.SelectionForeColor = Color.White;
                    e.Value = "Overdue";
                }
                else if (StatusColors.TryGetValue(e.Value.ToString(), out var colors))
                {
                    e.CellStyle.BackColor = colors.bg; e.CellStyle.ForeColor = colors.fg;
                    e.CellStyle.SelectionBackColor = colors.bg; e.CellStyle.SelectionForeColor = colors.fg;
                }
                e.CellStyle.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                e.FormattingApplied = true;
            }
        }

        // ── Navigation / Logout ───────────────────────────────────────────
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }
    }
}
