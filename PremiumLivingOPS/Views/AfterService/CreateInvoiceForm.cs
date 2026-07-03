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
        private List<OrderEntity> _currentOrders = new List<OrderEntity>();
        private OrderEntity       _selectedOrder;

        private static readonly Dictionary<string, (Color bg, Color fg)> OrderStatusColors =
            new Dictionary<string, (Color, Color)>
            {
                { "Pending",             (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "Processing",          (Color.FromArgb(219, 234, 254), Color.FromArgb( 29,  78, 216)) },
                { "Delivered",           (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
                { "Partially Delivered", (Color.FromArgb(237, 233, 254), Color.FromArgb( 91,  33, 182)) },
                { "Cancelled",           (Color.FromArgb(254, 226, 226), Color.FromArgb(185,  28,  28)) },
                { "Completed",           (Color.FromArgb(220, 252, 231), Color.FromArgb( 22, 101,  52)) },
            };

        public CreateInvoiceForm()
        {
            InitializeComponent();
            this.Load += CreateInvoiceForm_Load;
        }

        private void CreateInvoiceForm_Load(object sender, EventArgs e)
        {
            RefreshGrid();
        }

        // ── Refresh grid from DB ────────────────────────────────────────────────────────────────────────
        private void RefreshGrid()
        {
            string orderNo   = txtSearchOrderNo.Text.Trim();
            string customer  = txtSearchCustomer.Text.Trim();

            var vm = _ctrl.GetCreateInvoiceVM();

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("After-Service  ›  Create Invoice");

            _currentOrders = vm.Orders;

            // Apply local keyword filter (order no / customer)
            // Exclude orders whose OrderID starts with "STG-QT" (quotation orders)
            var displayed = _currentOrders.FindAll(o =>
            {
                if (o.OrderID.StartsWith("STG-QT", StringComparison.OrdinalIgnoreCase)) return false;
                bool matchOrder    = string.IsNullOrEmpty(orderNo)   || o.OrderID.Contains(orderNo, StringComparison.OrdinalIgnoreCase);
                bool matchCustomer = string.IsNullOrEmpty(customer)  || o.CustomerName.Contains(customer, StringComparison.OrdinalIgnoreCase);
                return matchOrder && matchCustomer;
            });

            dgvOrders.Rows.Clear();
            foreach (var o in displayed)
                dgvOrders.Rows.Add(
                    o.OrderID,
                    o.CustomerName,
                    o.OrderContactName,
                    o.IssuedTime.ToString("yyyy-MM-dd"),
                    o.DeliveryDate?.ToString("yyyy-MM-dd") ?? "—",
                    $"HK$ {o.GrandTotal:N2}",
                    o.OrderStatus);

            ClearForm();
        }

        private void ResetSearch()
        {
            txtSearchOrderNo.Text  = string.Empty;
            txtSearchCustomer.Text = string.Empty;
            RefreshGrid();
        }

        // ── CellFormatting — colour OrderStatus badge ──────────────────────────────────────────
        private void dgvOrders_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvOrders.Columns[e.ColumnIndex].Name != "colStatus" || e.Value == null) return;
            if (!OrderStatusColors.TryGetValue(e.Value.ToString(), out var c)) return;
            e.CellStyle.BackColor          = c.bg;
            e.CellStyle.ForeColor          = c.fg;
            e.CellStyle.SelectionBackColor = c.bg;
            e.CellStyle.SelectionForeColor = c.fg;
            e.CellStyle.Font               = new Font("Segoe UI", 11f, FontStyle.Bold);
            e.CellStyle.Alignment          = DataGridViewContentAlignment.MiddleCenter;
            e.FormattingApplied            = true;
        }

        // ── Grid selection → fill form fields ─────────────────────────────────────────────────
        private void dgvOrders_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvOrders.SelectedRows.Count == 0) { ClearForm(); return; }
            string orderId = dgvOrders.SelectedRows[0].Cells["colOrderID"].Value?.ToString();
            _selectedOrder = _currentOrders.Find(o => o.OrderID == orderId);
            FillFormFromGrid();
        }

        private void FillFormFromGrid()
        {
            if (_selectedOrder == null) return;

            lblSelectedOrderID.Text = _selectedOrder.OrderID;
            lblCustomer.Text        = _selectedOrder.CustomerName;
            lblGrandTotal.Text      = $"HK$ {_selectedOrder.GrandTotal:N2}";

            // Pre-fill amounts
            nudDeposit.Maximum = (decimal)_selectedOrder.GrandTotal;
            nudPaid.Maximum    = (decimal)_selectedOrder.GrandTotal;
            nudDeposit.Value   = 0;
            nudPaid.Value      = (decimal)_selectedOrder.GrandTotal;
            dtpDueDate.Value   = DateTime.Today.AddDays(30);

            RecalcBalance();
            btnCreateInvoice.Enabled = true;
        }

        private void ClearForm()
        {
            _selectedOrder             = null;
            lblSelectedOrderID.Text    = "—";
            lblCustomer.Text           = "—";
            lblGrandTotal.Text         = "—";
            nudDeposit.Value           = 0;
            nudPaid.Value              = 0;
            lblRemaining.Text          = "HK$ 0.00";
            lblRemaining.ForeColor     = Palette.TextMuted;
            cboPaymentStatus.SelectedIndex = 0;
            btnCreateInvoice.Enabled   = false;
        }

        // ── Auto-calculate remaining balance ────────────────────────────────────────────────────
        private void RecalcBalance()
        {
            if (_selectedOrder == null) return;

            double paid      = (double)nudPaid.Value;
            double total     = _selectedOrder.GrandTotal;
            double remaining = Math.Max(0, total - paid);

            lblRemaining.Text      = $"HK$ {remaining:N2}";
            lblRemaining.ForeColor = remaining > 0 ? Palette.Danger : Palette.Success;

            // Auto-suggest payment status
            cboPaymentStatus.SelectedItem = remaining <= 0 ? "Full" : "Partial";
        }

        // ── Create Invoice button ────────────────────────────────────────────────────────────────────────
        private void btnCreateInvoice_Click(object sender, EventArgs e)
        {
            if (_selectedOrder == null)
            {
                MessageBox.Show("Please select an order from the grid first.",
                    "No Order Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            double paid      = (double)nudPaid.Value;
            double total     = _selectedOrder.GrandTotal;
            double remaining = Math.Max(0, total - paid);

            var inv = new InvoiceEntity
            {
                OrderID       = _selectedOrder.OrderID,
                InvoiceDate   = DateTime.Today,
                DepositAmount = (double)nudDeposit.Value,
                PaidAmount    = paid,
                RemainingBalance = remaining,
                TotalAmount   = total,
                PaymentStatus = cboPaymentStatus.SelectedItem?.ToString() ?? "Partial",
                DueDate       = dtpDueDate.Value.Date
            };

            bool ok = _ctrl.SaveInvoice(inv);
            if (ok)
            {
                MessageBox.Show(
                    $"Invoice created successfully.\nInvoice ID: {inv.InvoiceID}",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshGrid();
            }
            else
            {
                MessageBox.Show("Failed to create invoice. Please try again.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Navigation / logout ────────────────────────────────────────────────────────────────────────
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }
    }
}
