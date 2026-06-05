using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.OrderProcessing
{
    /// <summary>
    /// Modify Order — Tab 4 of Order Processing Management.
    ///
    /// MVC contract (View layer):
    ///   • Calls OrderProcessingController for all data and business operations.
    ///   • Uses AppShell (TopNavBar + UserBar) for navigation chrome.
    ///   • Contains NO business logic and NO direct DB calls.
    /// </summary>
    public partial class ModifyOrderForm : Form
    {
        // ── Static entry point: ViewOrderForm passes OrderID here before navigating ──
        public static string PendingOrderId { get; set; } = null;

        private readonly OrderProcessingController _ctrl = new OrderProcessingController();

        private OrderEntity           _currentOrder;
        private List<OrderLineEntity> _lines     = new List<OrderLineEntity>();
        private List<ProductLookup>   _products  = new List<ProductLookup>();
        private List<AddressLookup>   _allAddresses = new List<AddressLookup>();

        public ModifyOrderForm()
        {
            InitializeComponent();
            this.Load += ModifyOrderForm_Load;
        }

        // ── Load ────────────────────────────────────────────────────────────────────────────
        private void ModifyOrderForm_Load(object sender, EventArgs e)
        {
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;

            var vm = _ctrl.GetModifyOrderVM();

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Order Processing  ›  Modify Order");

            _products     = vm.Products;
            _allAddresses = vm.Addresses ?? new List<AddressLookup>();

            cboProduct.Items.Clear();
            cboProduct.Items.Add(new ComboItem("-- Select Product --", ""));
            foreach (var p in _products)
                cboProduct.Items.Add(new ComboItem(p.DisplayText, p.ItemID));
            cboProduct.SelectedIndex = 0;

            cboCustomer.Items.Clear();
            cboCustomer.Items.Add(new ComboItem("-- Select Customer --", ""));
            foreach (var c in vm.Customers ?? new List<CustomerEntity>())
                cboCustomer.Items.Add(new ComboItem(
                    $"{c.CustomerID}  –  {c.CustomerName}", c.CustomerID));

            cboAddressId.Items.Clear();
            cboAddressId.Items.Add(new ComboItem("-- Select Address --", ""));
            cboAddressId.SelectedIndex = 0;

            cboQuotation.Items.Clear();
            cboQuotation.Items.Add(new ComboItem("-- None --", ""));
            cboQuotation.SelectedIndex = 0;

            ReloadOrderCombo();

            if (!string.IsNullOrEmpty(PendingOrderId))
            {
                SelectAndLoadOrder(PendingOrderId);
                PendingOrderId = null;
            }
        }

        // ── Load Order button ───────────────────────────────────────────────────────────
        private void btnLoadOrder_Click(object sender, EventArgs e)
        {
            var sel = cboSearchOrder.SelectedItem as ComboItem;
            if (sel == null || string.IsNullOrEmpty(sel.Value))
            {
                MessageBox.Show("Please select an order to load.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            SelectAndLoadOrder(sel.Value);
        }

        private void SelectAndLoadOrder(string orderId)
        {
            var vm = _ctrl.GetModifyOrderVM(orderId);
            _currentOrder = vm.SelectedOrder;
            if (_currentOrder == null)
            {
                MessageBox.Show($"Order '{orderId}' not found.",
                    "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _lines = vm.Lines ?? new List<OrderLineEntity>();

            for (int i = 0; i < cboSearchOrder.Items.Count; i++)
            {
                if (cboSearchOrder.Items[i] is ComboItem ci && ci.Value == orderId)
                { cboSearchOrder.SelectedIndex = i; break; }
            }

            PopulateHeader(_currentOrder);
            RefreshLineGrid();

            bool isCancelled = _currentOrder.OrderStatus == "Cancelled";
            btnSaveChanges.Enabled = !isCancelled;
            btnAddLine.Enabled     = !isCancelled;
            btnRemoveLine.Enabled  = !isCancelled;
            txtQty.Enabled         = !isCancelled;
            cboProduct.Enabled     = !isCancelled;
            btnCancelOrder.Enabled = !isCancelled;
        }

        // ── Populate header fields from loaded order ─────────────────────────────────
        private void PopulateHeader(OrderEntity o)
        {
            lblOrderIdValue.Text = o.OrderID;

            cboQuotation.Items.Clear();
            cboQuotation.Items.Add(new ComboItem(
                string.IsNullOrEmpty(o.QuotationID) ? "-- None --" : o.QuotationID,
                o.QuotationID ?? ""));
            cboQuotation.SelectedIndex = 0;

            SelectComboByValue(cboCustomer, o.CustomerID);
            LoadAddressCombos(o.CustomerID);
            SelectComboByValue(cboAddressId, o.AddressID);

            txtShippingAddr.Text = o.ShippingAddress;
            txtBillingAddr.Text  = o.BillingAddress;

            bool same = !string.IsNullOrEmpty(o.ShippingAddress)
                     && o.ShippingAddress == o.BillingAddress;
            chkSameAddress.Checked  = same;
            txtBillingAddr.Enabled  = !same;
            txtBillingAddr.BackColor = same
                ? Color.FromArgb(235, 240, 250)
                : System.Drawing.SystemColors.Window;

            txtContactName.Text = o.OrderContactName;
            dtpDelivery.Value   = o.DeliveryDate > DateTime.MinValue
                                      ? o.DeliveryDate : DateTime.Today;

            int statusIdx = cboStatus.FindStringExact(o.OrderStatus);
            cboStatus.SelectedIndex = statusIdx >= 0 ? statusIdx : 0;

            if (string.IsNullOrEmpty(o.DiscountType) || o.DiscountType == "None")
            {
                cboDiscountType.SelectedIndex = 0;
                txtDiscountValue.Text         = "0";
                txtDiscountValue.Enabled      = false;
                lblDiscountUnit.Text          = "";
            }
            else
            {
                int di = cboDiscountType.FindStringExact(o.DiscountType);
                cboDiscountType.SelectedIndex = di >= 0 ? di : 0;
                txtDiscountValue.Text         = o.DiscountValue.ToString("F2");
                txtDiscountValue.Enabled      = true;
                lblDiscountUnit.Text          = o.DiscountType == "Rate (%)" ? "%" : "HK$";
            }
        }

        // ── Address / Customer combo helpers ───────────────────────────────────────
        private void LoadAddressCombos(string customerId)
        {
            cboAddressId.Items.Clear();
            cboAddressId.Items.Add(new ComboItem("-- Select Address --", ""));
            var filtered = _ctrl.GetAddressesByCustomer(customerId, _allAddresses);
            foreach (var a in filtered)
                cboAddressId.Items.Add(new ComboItem(a.DisplayText, a.AddressId));
            cboAddressId.SelectedIndex = 0;
        }

        private void cboCustomer_SelectedIndexChanged(object sender, EventArgs e)
        {
            var sel = cboCustomer.SelectedItem as ComboItem;
            LoadAddressCombos(sel?.Value ?? "");
        }

        private void cboAddressId_SelectedIndexChanged(object sender, EventArgs e)
        {
            var sel = cboAddressId.SelectedItem as ComboItem;
            if (sel == null || string.IsNullOrEmpty(sel.Value)) return;
            var addr = _allAddresses.Find(a => a.AddressId == sel.Value);
            if (addr == null) return;
            txtShippingAddr.Text = addr.FullAddress;
            if (chkSameAddress.Checked)
                txtBillingAddr.Text = addr.FullAddress;
        }

        private void txtShippingAddr_TextChanged(object sender, EventArgs e)
        {
            if (chkSameAddress.Checked)
                txtBillingAddr.Text = txtShippingAddr.Text;
        }

        private void chkSameAddress_CheckedChanged(object sender, EventArgs e)
        {
            bool same = chkSameAddress.Checked;
            txtBillingAddr.Enabled   = !same;
            txtBillingAddr.BackColor = same
                ? Color.FromArgb(235, 240, 250)
                : System.Drawing.SystemColors.Window;
            if (same)
                txtBillingAddr.Text = txtShippingAddr.Text;
        }

        // ── Combo helper ────────────────────────────────────────────────────────────────
        private static void SelectComboByValue(ComboBox cbo, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            for (int i = 0; i < cbo.Items.Count; i++)
            {
                if (cbo.Items[i] is ComboItem ci && ci.Value == value)
                { cbo.SelectedIndex = i; return; }
            }
        }

        // ── Line-item helpers ─────────────────────────────────────────────────────────────
        private void RefreshLineGrid()
        {
            dgvLines.Rows.Clear();
            foreach (var l in _lines)
                dgvLines.Rows.Add(l.ItemID, l.ItemName, l.Quantity,
                                  $"HK$ {l.Price:N2}", $"HK$ {l.LineTotal:N2}");
            UpdateSummary();
        }

        /// <summary>
        /// Unified summary recalculation.
        /// Subtotal  = sum of all line totals.
        /// GrandTotal = Subtotal − discount (clamped to ≥ 0).
        ///
        /// After updating both value labels, PerformLayout() is called on
        /// pnlFooterContent so the Layout handler immediately recalculates
        /// Grand Total Title's X-position based on the new lblSubtotalValue.Width.
        /// </summary>
        private void UpdateSummary()
        {
            double subtotal = 0;
            foreach (var l in _lines) subtotal += l.LineTotal;

            lblSubtotalValue.Text = $"HK$ {subtotal:N2}";

            string dtype    = cboDiscountType.SelectedItem?.ToString() ?? "None";
            double discount = 0;
            if (dtype == "Amount")
                double.TryParse(txtDiscountValue.Text, out discount);
            else if (dtype == "Rate (%)")
            {
                if (double.TryParse(txtDiscountValue.Text, out double rate))
                    discount = subtotal * rate / 100.0;
            }

            if (discount < 0)        discount = 0;
            if (discount > subtotal) discount = subtotal;

            lblGrandTotalValue.Text = $"HK$ {subtotal - discount:N2}";

            // Re-layout footer so Grand Total Title X-position reflects
            // the updated lblSubtotalValue.Width immediately.
            pnlFooterContent.PerformLayout();
        }

        private void btnAddLine_Click(object sender, EventArgs e)
        {
            var selProduct = cboProduct.SelectedItem as ComboItem;
            if (selProduct == null || string.IsNullOrEmpty(selProduct.Value))
            {
                MessageBox.Show("Please select a product.",
                    "No Product", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!int.TryParse(txtQty.Text, out int qty) || qty <= 0)
            {
                MessageBox.Show("Please enter a valid quantity.",
                    "Invalid Qty", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var product = _products.Find(p => p.ItemID == selProduct.Value);
            if (product == null) return;

            var existing = _lines.Find(l => l.ItemID == product.ItemID);
            if (existing != null)
                existing.Quantity += qty;
            else
                _lines.Add(new OrderLineEntity
                {
                    OrderID  = _currentOrder?.OrderID ?? "",
                    ItemID   = product.ItemID,
                    ItemName = product.ItemName,
                    Quantity = qty,
                    Price    = product.SalesPrice
                });

            cboProduct.SelectedIndex = 0;
            txtQty.Text = "1";
            RefreshLineGrid();
        }

        private void btnRemoveLine_Click(object sender, EventArgs e)
        {
            if (dgvLines.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a line to remove.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string itemId = dgvLines.SelectedRows[0]
                .Cells["colLineItemID"].Value?.ToString();
            _lines.RemoveAll(l => l.ItemID == itemId);
            RefreshLineGrid();
        }

        private void cboDiscountType_SelectedIndexChanged(object sender, EventArgs e)
        {
            string dtype = cboDiscountType.SelectedItem?.ToString() ?? "None";
            bool   has   = dtype != "None";
            txtDiscountValue.Enabled = has;
            if (!has) txtDiscountValue.Text = "0";
            lblDiscountUnit.Text = dtype == "Rate (%)" ? "%"
                                 : dtype == "Amount"  ? "HK$"
                                 : "";
            UpdateSummary();
        }

        private void txtDiscountValue_TextChanged(object sender, EventArgs e)
            => UpdateSummary();

        // ── Save Changes ─────────────────────────────────────────────────────────────────
        private void btnSaveChanges_Click(object sender, EventArgs e)
        {
            if (_currentOrder == null)
            {
                MessageBox.Show("No order is loaded.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selCust = cboCustomer.SelectedItem  as ComboItem;
            var selAddr = cboAddressId.SelectedItem as ComboItem;

            string dtype = cboDiscountType.SelectedItem?.ToString() ?? "None";
            double.TryParse(txtDiscountValue.Text, out double discountValue);

            double sub = 0;
            foreach (var l in _lines) sub += l.LineTotal;

            double discountAmount = 0;
            if (dtype == "Amount")        discountAmount = discountValue;
            else if (dtype == "Rate (%)") discountAmount = sub * discountValue / 100.0;
            if (discountAmount < 0)   discountAmount = 0;
            if (discountAmount > sub) discountAmount = sub;

            var header = new OrderEntity
            {
                OrderID          = _currentOrder.OrderID,
                CustomerID       = selCust?.Value ?? _currentOrder.CustomerID,
                AddressID        = selAddr?.Value ?? _currentOrder.AddressID,
                QuotationID      = _currentOrder.QuotationID,
                SalesID          = _currentOrder.SalesID,
                IssuedTime       = _currentOrder.IssuedTime,
                OrderStatus      = cboStatus.SelectedItem?.ToString() ?? _currentOrder.OrderStatus,
                DeliveryDate     = dtpDelivery.Value,
                ShippingAddress  = txtShippingAddr.Text.Trim(),
                BillingAddress   = txtBillingAddr.Text.Trim(),
                OrderContactName = txtContactName.Text.Trim(),
                DiscountType     = dtype == "None" ? null : dtype,
                DiscountValue    = discountValue,
                DiscountAmount   = discountAmount,
                SubTotal         = sub,
                GrandTotal       = sub - discountAmount
            };

            bool ok = _ctrl.SaveOrderChanges(header, new List<OrderLineEntity>(_lines));
            if (ok)
            {
                MessageBox.Show("Order updated successfully.", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                _currentOrder = header;
                ReloadOrderCombo();
            }
            else
            {
                MessageBox.Show("Failed to save changes. Please try again.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Cancel Order ────────────────────────────────────────────────────────────────
        private void btnCancelOrder_Click(object sender, EventArgs e)
        {
            if (_currentOrder == null)
            {
                MessageBox.Show("No order is loaded.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Are you sure you want to CANCEL order '{_currentOrder.OrderID}'?\n"
              + "This action cannot be undone.",
                "Confirm Cancellation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            bool ok = _ctrl.CancelOrder(_currentOrder.OrderID);
            if (ok)
            {
                MessageBox.Show("Order has been cancelled.", "Cancelled",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                _currentOrder.OrderStatus = "Cancelled";
                btnSaveChanges.Enabled = false;
                btnCancelOrder.Enabled = false;
                btnAddLine.Enabled     = false;
                btnRemoveLine.Enabled  = false;
                txtQty.Enabled         = false;
                cboProduct.Enabled     = false;

                int idx = cboStatus.FindStringExact("Cancelled");
                if (idx >= 0) cboStatus.SelectedIndex = idx;

                ReloadOrderCombo();
            }
            else
            {
                MessageBox.Show("Failed to cancel order. Please try again.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────────────────
        private void ReloadOrderCombo()
        {
            string currentId = _currentOrder?.OrderID;
            var listVm = _ctrl.GetViewOrderVM();

            cboSearchOrder.Items.Clear();
            cboSearchOrder.Items.Add(new ComboItem("-- Select Order --", ""));
            foreach (var o in listVm.Orders)
                cboSearchOrder.Items.Add(new ComboItem(
                    $"{o.OrderID}  –  {o.CustomerName}  [{o.OrderStatus}]",
                    o.OrderID));

            if (!string.IsNullOrEmpty(currentId))
            {
                for (int i = 1; i < cboSearchOrder.Items.Count; i++)
                {
                    if (cboSearchOrder.Items[i] is ComboItem ci && ci.Value == currentId)
                    { cboSearchOrder.SelectedIndex = i; break; }
                }
            }
            else
                cboSearchOrder.SelectedIndex = 0;
        }

        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }

        private class ComboItem
        {
            public string Text  { get; }
            public string Value { get; }
            public ComboItem(string text, string value) { Text = text; Value = value; }
            public override string ToString() => Text;
        }
    }
}
