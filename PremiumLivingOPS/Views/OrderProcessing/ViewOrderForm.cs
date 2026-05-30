using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.OrderProcessing
{
    public partial class ViewOrderForm : Form
    {
        private readonly OrderProcessingController _ctrl = new OrderProcessingController();
        private List<OrderLineEntity> _currentLines = new List<OrderLineEntity>();

        public ViewOrderForm()
        {
            InitializeComponent();
            this.Load += ViewOrderForm_Load;
        }

        private void ViewOrderForm_Load(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void RefreshData(string statusFilter = null)
        {
            var vm = _ctrl.GetViewOrderVM(statusFilter);

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Order Processing  ›  View Order");

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

            dgvLines.Rows.Clear();
            lblDetailTitle.Text = "Select an order to view details";
        }

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

        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }
    }
}
