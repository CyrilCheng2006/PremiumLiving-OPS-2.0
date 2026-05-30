using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.OrderProcessing
{
    /// <summary>
    /// View Order — Tab 1 of Order Processing Management.
    /// Displays all orders in a DataGridView with status filtering.
    /// Selecting a row shows its line items in a detail panel.
    ///
    /// MVC contract (View layer):
    ///   • Instantiates OrderProcessingController to request ViewOrderViewModel.
    ///   • Uses AppShell for TopNavBar + UserBar chrome.
    ///   • Contains NO business logic and NO direct DB calls.
    /// </summary>
    public partial class ViewOrderForm : Form
    {
        private readonly OrderProcessingController _ctrl = new OrderProcessingController();
        private List<OrderLineEntity> _currentLines = new List<OrderLineEntity>();

        public ViewOrderForm()
        {
            InitializeComponent();
            this.Load += ViewOrderForm_Load;
        }

        // ── Load ───────────────────────────────────────────────────────────────
        private void ViewOrderForm_Load(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void RefreshData(string statusFilter = null)
        {
            var vm = _ctrl.GetViewOrderVM(statusFilter);

            // UserBarInfo has: DisplayName, Department  (no Role property)
            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Order Processing  ›  View Order");

            // Bind order grid
            dgvOrders.Rows.Clear();
            foreach (var o in vm.Orders)
            {
                dgvOrders.Rows.Add(
                    o.OrderID,
                    o.CustomerName,
                    o.IssuedTime.ToString("yyyy-MM-dd"),
                    o.DeliveryDate.ToString("yyyy-MM-dd"),
                    $"HK$ {o.GrandTotal:N2}",
                    o.OrderStatus
                );
            }

            // Clear detail panel
            dgvLines.Rows.Clear();
            lblDetailTitle.Text = "Select an order to view details";
        }

        // ── Event handlers ──────────────────────────────────────────────────────
        private void cboStatusFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            string sel = cboStatusFilter.SelectedItem?.ToString();
            RefreshData(sel == "All" ? null : sel);
        }

        private void dgvOrders_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvOrders.SelectedRows.Count == 0) return;

            string orderId = dgvOrders.SelectedRows[0].Cells["colOrderID"].Value?.ToString();
            if (string.IsNullOrEmpty(orderId)) return;

            _currentLines = _ctrl.GetOrderLines(orderId);
            lblDetailTitle.Text = $"Line Items — {orderId}";
            dgvLines.Rows.Clear();
            foreach (var l in _currentLines)
                dgvLines.Rows.Add(l.ItemID, l.ItemName, l.Quantity,
                                  $"HK$ {l.Price:N2}", $"HK$ {l.LineTotal:N2}");
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            string sel = cboStatusFilter.SelectedItem?.ToString();
            RefreshData(sel == "All" ? null : sel);
        }

        // ── TopNavBar navigation ────────────────────────────────────────────────
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }
    }
}
