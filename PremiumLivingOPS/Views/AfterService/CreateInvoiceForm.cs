using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.DAL;
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

        // ── Load ────────────────────────────────────────────────────────
        private void CreateInvoiceForm_Load(object sender, EventArgs e)
        {
            RefreshGrid();
        }

        // ── Refresh grid ────────────────────────────────────────────────
        private void RefreshGrid()
        {
            string orderKw    = txtSearchOrder.Text.Trim();
            string customerKw = txtSearchCustomer.Text.Trim();
            string statusKw   = cboStatusFilter.SelectedIndex > 0
                                    ? cboStatusFilter.SelectedItem.ToString()
                                    : string.Empty;

            var vm = _ctrl.GetCreateInvoiceVM();

            // ── Bind AppShell via its public API ──────────────────────────
            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("After-Service  ›  Create Invoice");

            // ── Local filters ─────────────────────────────────────────────
            _currentOrders = vm.Orders ?? new List<OrderEntity>();

            if (!string.IsNullOrEmpty(orderKw))
                _currentOrders = _currentOrders.FindAll(o =>
                    (o.OrderID ?? "").IndexOf(orderKw, StringComparison.OrdinalIgnoreCase) >= 0);

            if (!string.IsNullOrEmpty(customerKw))
                _currentOrders = _currentOrders.FindAll(o =>
                    (o.CustomerName ?? "").IndexOf(customerKw, StringComparison.OrdinalIgnoreCase) >= 0);

            if (!string.IsNullOrEmpty(statusKw))
                _currentOrders = _currentOrders.FindAll(o =>
                    string.Equals(o.OrderStatus, statusKw, StringComparison.OrdinalIgnoreCase));

            dgvOrders.Rows.Clear();
            foreach (var o in _currentOrders)
                dgvOrders.Rows.Add(
                    o.OrderID,
                    o.CustomerName,
                    $"HK$ {o.GrandTotal:N2}",
                    o.OrderStatus,
                    o.IssuedTime.ToString("yyyy-MM-dd"));
        }

        // ── Grid cell formatting — status chip colours ────────────────────
        private void dgvOrders_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvOrders.Columns[e.ColumnIndex].Name != "colStatus") return;

            string status = e.Value as string ?? "";
            switch (status)
            {
                case "Pending":
                    e.CellStyle.ForeColor = Color.FromArgb(180, 120,   0);
                    e.CellStyle.BackColor = Color.FromArgb(255, 248, 220);
                    break;
                case "Processing":
                    e.CellStyle.ForeColor = Color.FromArgb( 47, 111, 237);
                    e.CellStyle.BackColor = Color.FromArgb(219, 234, 254);
                    break;
                case "Partially Delivered":
                    e.CellStyle.ForeColor = Color.FromArgb(100,  60, 180);
                    e.CellStyle.BackColor = Color.FromArgb(237, 230, 255);
                    break;
                case "Delivered":
                    e.CellStyle.ForeColor = Color.FromArgb( 22, 130,  80);
                    e.CellStyle.BackColor = Color.FromArgb(209, 250, 229);
                    break;
                case "Completed":
                    e.CellStyle.ForeColor = Color.FromArgb( 15,  90,  60);
                    e.CellStyle.BackColor = Color.FromArgb(167, 243, 208);
                    break;
                case "Cancelled":
                    e.CellStyle.ForeColor = Color.FromArgb(180,  30,  30);
                    e.CellStyle.BackColor = Color.FromArgb(254, 226, 226);
                    break;
                default:
                    e.CellStyle.ForeColor = Color.FromArgb( 98, 112, 135);
                    e.CellStyle.BackColor = Color.FromArgb(240, 244, 249);
                    break;
            }
        }

        // ── Grid selection → populate form fields ────────────────────────────
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

        // ── Auto-calculate remaining balance ────────────────────────────
        private void RecalcBalance()
        {
            double total   = _selectedOrder != null ? _selectedOrder.GrandTotal : 0;
            double paid    = (double)nudPaidAmount.Value;
            double balance = Math.Max(0, total - paid);

            lblRemainingBalance.Text = $"HK$ {balance:N2}";

            if (total > 0 && balance <= 0)
                cboPaymentStatus.SelectedIndex = 1; // Full
            else
                cboPaymentStatus.SelectedIndex = 0; // Partial
        }

        // ── Create Invoice ──────────────────────────────────────────────
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
                MessageBox.Show("Paid Amount cannot exceed the Grand Total.",
                    "Invalid Amount", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string payStatus = cboPaymentStatus.SelectedItem?.ToString() ?? "Partial";

            // Build InvoiceEntity; InvoiceID left empty — SaveInvoice() auto-generates it
            var inv = new InvoiceEntity
            {
                OrderID          = _selectedOrder.OrderID,
                InvoiceDate      = DateTime.Today,
                PaidAmount       = paid,
                DepositAmount    = deposit,
                TotalAmount      = total,
                RemainingBalance = balance,
                PaymentStatus    = payStatus,
                DueDate          = dtpDueDate.Value
            };

            bool ok = _ctrl.SaveInvoice(inv);

            if (ok)
            {
                MessageBox.Show(
                    $"Invoice created successfully for Order {_selectedOrder.OrderID}.",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Reset selection and form fields
                _selectedOrder = null;
                lblSelectedOrder.Text    = "—";
                lblCustomer.Text         = "—";
                lblGrandTotal.Text       = "—";
                nudPaidAmount.Value      = 0;
                nudDepositAmount.Value   = 0;
                lblRemainingBalance.Text = "HK$ 0.00";
                cboPaymentStatus.SelectedIndex = 0;

                RefreshGrid();
            }
            else
            {
                MessageBox.Show("Failed to create invoice. Please try again.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── AppShell event handlers (wired ONCE in Designer.cs — RULE 4) ─────────

        /// <summary>
        /// Handles top-nav menu clicks forwarded by AppShell.
        /// Navigation is delegated to the host MainForm via event propagation.
        /// </summary>
        private void OnTopNavMenuItemClicked(string menu, string sub)
        {
            // Intentionally empty: routing is handled by the host MainForm
            // which subscribes to this form's AppShell events at the app level.
        }

        /// <summary>
        /// Handles the logout button click forwarded by AppShell.LogoutClicked.
        /// Wired in Designer.cs (RULE 4). Do NOT subscribe again in Load/constructor.
        /// </summary>
        private void OnLogoutClicked(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to log out?",
                    "Log Out", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                == DialogResult.Yes)
            {
                Application.Restart();
            }
        }
    }
}
