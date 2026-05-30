using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
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
        private List<OrderLineEntity> _lines    = new List<OrderLineEntity>();
        private List<ProductLookup>   _products = new List<ProductLookup>();

        public ModifyOrderForm()
        {
            InitializeComponent();
            this.Load += ModifyOrderForm_Load;
        }

        // ── Load ───────────────────────────────────────────────────────────────────
        private void ModifyOrderForm_Load(object sender, EventArgs e)
        {
            // Wire AppShell events — must be done once, before first data load
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;

            var vm = _ctrl.GetModifyOrderVM();

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Order Processing  ›  Modify Order");

            // Populate product catalogue for line-item editing
            _products = vm.Products;
            cboAddProduct.Items.Clear();
            cboAddProduct.Items.Add(new ComboItem("-- Select Product --", ""));
            foreach (var p in _products)
                cboAddProduct.Items.Add(new ComboItem(
                    $"{p.ItemID}  –  {p.ItemName}  (HK$ {p.SalesPrice:N2})",
                    p.ItemID));
            cboAddProduct.SelectedIndex = 0;

            // Populate search combo
            ReloadOrderCombo();

            // Status combo
            cboStatus.Items.Clear();
            cboStatus.Items.AddRange(new object[]
                { "Pending", "Processing", "Delivered", "Cancelled" });

            // Discount type combo
            cboDiscountType.Items.Clear();
            cboDiscountType.Items.AddRange(new object[] { "None", "Amount", "Rate (%)" });
            cboDiscountType.SelectedIndex = 0;

            SetEditPanelEnabled(false);

            // If ViewOrderForm passed a PendingOrderId, auto-load that order
            if (!string.IsNullOrEmpty(PendingOrderId))
            {
                SelectAndLoadOrder(PendingOrderId);
                PendingOrderId = null;   // consume after use
            }
        }

        // ── Search / Load Order ────────────────────────────────────────────────────
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

        /// <summary>Loads a specific order by ID into the edit panel.</summary>
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

            // Sync combo selection
            for (int i = 0; i < cboSearchOrder.Items.Count; i++)
            {
                if (cboSearchOrder.Items[i] is ComboItem ci && ci.Value == orderId)
                { cboSearchOrder.SelectedIndex = i; break; }
            }

            PopulateHeader(_currentOrder);
            RefreshLineGrid();
            SetEditPanelEnabled(true);

            bool isCancelled = _currentOrder.OrderStatus == "Cancelled";
            btnSaveChanges.Enabled = !isCancelled;
            btnAddLine.Enabled     = !isCancelled;
            btnRemoveLine.Enabled  = !isCancelled;
            txtAddQty.Enabled      = !isCancelled;
            cboAddProduct.Enabled  = !isCancelled;
            btnCancelOrder.Enabled = !isCancelled;
        }

        private void PopulateHeader(OrderEntity o)
        {
            txtOrderID.Text      = o.OrderID;
            txtCustomer.Text     = o.CustomerName;
            txtContactName.Text  = o.OrderContactName;
            txtShippingAddr.Text = o.ShippingAddress;
            txtBillingAddr.Text  = o.BillingAddress;
            dtpDelivery.Value    = o.DeliveryDate > DateTime.MinValue
                                       ? o.DeliveryDate : DateTime.Today;

            int idx = cboStatus.FindStringExact(o.OrderStatus);
            cboStatus.SelectedIndex = idx >= 0 ? idx : 0;

            if (string.IsNullOrEmpty(o.DiscountType) || o.DiscountType == "None")
            {
                cboDiscountType.SelectedIndex = 0;
                txtDiscountValue.Text    = "0";
                txtDiscountValue.Enabled = false;
            }
            else
            {
                int di = cboDiscountType.FindStringExact(o.DiscountType);
                cboDiscountType.SelectedIndex = di >= 0 ? di : 0;
                txtDiscountValue.Text    = o.DiscountValue.ToString("F2");
                txtDiscountValue.Enabled = true;
            }
        }

        // ── Line-item helpers ──────────────────────────────────────────────────────
        private void RefreshLineGrid()
        {
            dgvLines.Rows.Clear();
            double sub = 0;
            foreach (var l in _lines)
            {
                dgvLines.Rows.Add(l.ItemID, l.ItemName, l.Quantity,
                                  $"HK$ {l.Price:N2}", $"HK$ {l.LineTotal:N2}");
                sub += l.LineTotal;
            }
            lblSubtotal.Text = $"Subtotal:  HK$ {sub:N2}";
            RecalcGrandTotal(sub);
        }

        private void RecalcGrandTotal(double sub)
        {
            double discount = 0;
            string dtype = cboDiscountType.SelectedItem?.ToString() ?? "None";
            if (dtype == "Amount")
                double.TryParse(txtDiscountValue.Text, out discount);
            else if (dtype == "Rate (%)")
            {
                if (double.TryParse(txtDiscountValue.Text, out double rate))
                    discount = sub * rate / 100.0;
            }
            lblGrandTotal.Text = $"Grand Total:  HK$ {sub - discount:N2}";
        }

        private void btnAddLine_Click(object sender, EventArgs e)
        {
            var selProduct = cboAddProduct.SelectedItem as ComboItem;
            if (selProduct == null || string.IsNullOrEmpty(selProduct.Value))
            {
                MessageBox.Show("Please select a product.",
                    "No Product", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!int.TryParse(txtAddQty.Text, out int qty) || qty <= 0)
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

            cboAddProduct.SelectedIndex = 0;
            txtAddQty.Text = "1";
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
                .Cells["colModLineItemID"].Value?.ToString();
            _lines.RemoveAll(l => l.ItemID == itemId);
            RefreshLineGrid();
        }

        private void cboDiscountType_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool hasDiscount = cboDiscountType.SelectedItem?.ToString() != "None";
            txtDiscountValue.Enabled = hasDiscount;
            if (!hasDiscount) txtDiscountValue.Text = "0";
            double sub = 0;
            foreach (var l in _lines) sub += l.LineTotal;
            RecalcGrandTotal(sub);
        }

        private void txtDiscountValue_TextChanged(object sender, EventArgs e)
        {
            double sub = 0;
            foreach (var l in _lines) sub += l.LineTotal;
            RecalcGrandTotal(sub);
        }

        // ── Save Changes ──────────────────────────────────────────────────────────
        private void btnSaveChanges_Click(object sender, EventArgs e)
        {
            if (_currentOrder == null)
            {
                MessageBox.Show("No order is loaded.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string dtype = cboDiscountType.SelectedItem?.ToString() ?? "None";
            double.TryParse(txtDiscountValue.Text, out double discountValue);

            double sub = 0;
            foreach (var l in _lines) sub += l.LineTotal;
            double discountAmount = 0;
            if (dtype == "Amount")       discountAmount = discountValue;
            else if (dtype == "Rate (%)") discountAmount = sub * discountValue / 100.0;

            var header = new OrderEntity
            {
                OrderID          = _currentOrder.OrderID,
                CustomerID       = _currentOrder.CustomerID,
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
                GrandTotal       = sub - discountAmount
            };

            bool ok = _ctrl.SaveOrderChanges(header, new List<OrderLineEntity>(_lines));
            if (ok)
            {
                MessageBox.Show("Order updated successfully.", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                ReloadOrderCombo();
            }
            else
            {
                MessageBox.Show("Failed to save changes. Please try again.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Cancel Order ──────────────────────────────────────────────────────────
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
                txtAddQty.Enabled      = false;
                cboAddProduct.Enabled  = false;

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

        // ── Helpers ───────────────────────────────────────────────────────────────
        private void SetEditPanelEnabled(bool enabled)
        {
            pnlEditCard.Visible   = enabled;
            pnlLinesCard.Visible  = enabled;
            pnlActionsBar.Visible = enabled;
        }

        /// <summary>
        /// Rebuilds cboSearchOrder from the View Order list.
        /// ModifyOrderViewModel has no Orders list;
        /// we source the dropdown from GetViewOrderVM() instead.
        /// </summary>
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

            // Re-select the currently loaded order if still present
            if (!string.IsNullOrEmpty(currentId))
            {
                for (int i = 1; i < cboSearchOrder.Items.Count; i++)
                {
                    if (cboSearchOrder.Items[i] is ComboItem ci && ci.Value == currentId)
                    { cboSearchOrder.SelectedIndex = i; break; }
                }
            }
            else
            {
                cboSearchOrder.SelectedIndex = 0;
            }
        }

        // ── TopNavBar navigation ──────────────────────────────────────────────────
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }

        // ── ComboItem helper ──────────────────────────────────────────────────────
        private class ComboItem
        {
            public string Text  { get; }
            public string Value { get; }
            public ComboItem(string text, string value) { Text = text; Value = value; }
            public override string ToString() => Text;
        }
    }
}
