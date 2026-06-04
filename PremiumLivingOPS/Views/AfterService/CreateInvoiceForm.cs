using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.AfterService
{
    public partial class CreateInvoiceForm : Form
    {
        private readonly AfterServiceController _ctrl = new AfterServiceController();

        /// <summary>Cached list matching current grid rows.</summary>
        private List<OrderEntity> _currentOrders = new List<OrderEntity>();

        /// <summary>The order currently selected for invoice creation.</summary>
        private OrderEntity _selectedOrder;

        public CreateInvoiceForm()
        {
            InitializeComponent();
            this.Load += CreateInvoiceForm_Load;
        }

        // ── Load ─────────────────────────────────────────────────────────
        private void CreateInvoiceForm_Load(object sender, EventArgs e)
        {
            RefreshGrid();
        }

        // ── Bind shell (called after VM is loaded) ────────────────────────
        private void BindShell(CreateInvoiceViewModel vm)
        {
            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("After-Service  ›  Create Invoice");
        }

        // ── Refresh grid ──────────────────────────────────────────────────
        private void RefreshGrid()
        {
            string orderKw    = txtSearchOrder.Text.Trim();
            string customerKw = txtSearchCustomer.Text.Trim();

            var vm = _ctrl.GetCreateInvoiceVM();
            BindShell(vm);

            // Local filter by keyword (repo returns all without invoice)
            _currentOrders = vm.Orders;
            if (!string.IsNullOrEmpty(orderKw))
                _currentOrders = _currentOrders.FindAll(o =>
                    (o.OrderID      ?? "").IndexOf(orderKw,    StringComparison.OrdinalIgnoreCase) >= 0);
            if (!string.IsNullOrEmpty(customerKw))
                _currentOrders = _currentOrders.FindAll(o =>
                    (o.CustomerName ?? "").IndexOf(customerKw, StringComparison.OrdinalIgnoreCase) >= 0);

            dgvOrders.Rows.Clear();
            foreach (var o in _currentOrders)
                dgvOrders.Rows.Add(
                    o.OrderID,
                    o.CustomerName,
                    $"HK$ {o.GrandTotal:N2}",
                    o.OrderStatus,
                    o.IssuedTime.ToString("yyyy-MM-dd"));
        }

        // ── Grid selection → populate form ───────────────────────────────
        private void dgvOrders_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvOrders.SelectedRows.Count == 0) return;

            int idx = dgvOrders.SelectedRows[0].Index;
            if (idx < 0 || idx >= _currentOrders.Count) return;

            _selectedOrder = _currentOrders[idx];

            lblSelectedOrder.Text = _selectedOrder.OrderID;
            lblCustomer.Text      = _selectedOrder.CustomerName;
            lblGrandTotal.Text    = $"HK$ {_selectedOrder.GrandTotal:N2}";

            nudPaidAmount.Maximum    = (decimal)_selectedOrder.GrandTotal;
            nudDepositAmount.Maximum = (decimal)_selectedOrder.GrandTotal;
            nudPaidAmount.Value      = 0;
            nudDepositAmount.Value   = 0;

            RecalcBalance();
        }

        // ── Auto-calculate remaining balance ─────────────────────────────
        private void RecalcBalance()
        {
            double total   = _selectedOrder != null ? _selectedOrder.GrandTotal : 0;
            double paid    = (double)nudPaidAmount.Value;
            double balance = Math.Max(0, total - paid);

            lblRemainingBalance.Text = $"HK$ {balance:N2}";

            // Suggest PaymentStatus
            if (total > 0 && balance <= 0)
                cboPaymentStatus.SelectedIndex = 1; // Full
            else
                cboPaymentStatus.SelectedIndex = 0; // Partial
        }

        // ── Create Invoice ────────────────────────────────────────────────
        private void btnCreateInvoice_Click(object sender, EventArgs e)
        {
            if (_selectedOrder == null)
            {
                MessageBox.Show("Please select an order from the grid first.",
                    "No Order Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            double paid    = (double)nudPaidAmount.Value;
            double deposit = (double)nudDepositAmount.Value;
            double total   = _selectedOrder.GrandTotal;
            double balance = Math.Max(0, total - paid);

            if (paid < 0 || paid > total)
            {
                MessageBox.Show("Paid Amount must be between 0 and the Grand Total.",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var inv = new InvoiceEntity
            {
                InvoiceID        = string.Empty,          // auto-generated by controller
                OrderID          = _selectedOrder.OrderID,
                CustomerName     = _selectedOrder.CustomerName,
                InvoiceDate      = DateTime.Today,
                DepositAmount    = deposit,
                PaidAmount       = paid,
                RemainingBalance = balance,
                TotalAmount      = total,
                PaymentStatus    = cboPaymentStatus.SelectedItem?.ToString() ?? "Partial",
                DueDate          = dtpDueDate.Value.Date
            };

            bool ok = _ctrl.SaveInvoice(inv);
            if (ok)
            {
                MessageBox.Show($"Invoice created successfully for Order {inv.OrderID}.",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Reset form
                _selectedOrder        = null;
                lblSelectedOrder.Text = "—";
                lblCustomer.Text      = "—";
                lblGrandTotal.Text    = "—";
                nudDepositAmount.Value = 0;
                nudPaidAmount.Value    = 0;
                lblRemainingBalance.Text = "HK$ 0.00";
                cboPaymentStatus.SelectedIndex = 0;
                dtpDueDate.Value = DateTime.Today.AddMonths(1);

                RefreshGrid();
            }
            else
            {
                MessageBox.Show("Failed to create invoice. Please try again.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Navigation / Logout ──────────────────────────────────────────
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }
    }
}
