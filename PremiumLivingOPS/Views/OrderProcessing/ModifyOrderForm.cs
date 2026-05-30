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
    /// Provides two functions on the SAME tab:
    ///   1. Edit Order   — modify header fields and order lines of an existing order.
    ///   2. Cancel Order — set order status to "Cancelled" (guarded by business rules
    ///                      enforced in the controller).
    ///
    /// MVC contract (View layer):
    ///   • Calls OrderProcessingController for all data and business operations.
    ///   • Uses AppShell (TopNavBar + UserBar) for navigation chrome.
    ///   • Contains NO business logic and NO direct DB calls.
    ///   • All totals displayed are re-calculated server-side inside the controller.
    /// </summary>
    public partial class ModifyOrderForm : Form
    {
        private readonly OrderProcessingController _ctrl = new OrderProcessingController();

        // Currently loaded order
        private OrderEntity           _currentOrder;
        private List<OrderLineEntity> _lines = new List<OrderLineEntity>();
        private List<ProductLookup>   _products = new List<ProductLookup>();

        public ModifyOrderForm()
        {
            InitializeComponent();
            this.Load += ModifyOrderForm_Load;
        }

        // ── Load ───────────────────────────────────────────────────────────
        private void ModifyOrderForm_Load(object sender, EventArgs e)
        {
            var vm = _ctrl.GetModifyOrderVM();

            // UserBarInfo has: DisplayName, Department  (no Role property)
            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Order Processing  ›  Modify Order");

            // Populate product catalogue (for line-item editing)
            _products = vm.Products;
            cboAddProduct.Items.Clear();
            cboAddProduct.Items.Add(new ComboItem("-- Select Product --", ""));
            foreach (var p in _products)
                cboAddProduct.Items.Add(new ComboItem(p.DisplayText, p.ItemID));
            cboAddProduct.SelectedIndex = 0;

            // Populate search combo
            cboSearchOrder.Items.Clear();
            cboSearchOrder.Items.Add(new ComboItem("-- Select Order --", ""));
            foreach (var o in vm.Orders)
                cboSearchOrder.Items.Add(
                    new ComboItem($"{o.OrderID}  –  {o.CustomerName}  [{o.OrderStatus}]",
                                  o.OrderID));
            cboSearchOrder.SelectedIndex = 0;

            // Status combo (for Edit)
            cboStatus.Items.Clear();
            cboStatus.Items.AddRange(new object[]
                { "Pending", "Confirmed", "In Progress", "Delivered", "Completed", "Cancelled" });

            // Discount type combo
            cboDiscountType.Items.Clear();
            cboDiscountType.Items.AddRange(new object[] { "None", "Amount", "Rate (%)" });
            cboDiscountType.SelectedIndex = 0;

            SetEditPanelEnabled(false);
        }

        // ── Search / Load Order ───────────────────────────────────────────────
        private void btnLoadOrder_Click(object sender, EventArgs e)
        {
            var sel = cboSearchOrder.SelectedItem as ComboItem;
            if (sel == null || string.IsNullOrEmpty(sel.Value))
            {
                MessageBox.Show("Please select an order to load.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Fetch fresh VM to get the order lines
            var vm = _ctrl.GetModifyOrderVM();
            _currentOrder = vm.Orders.Find(o => o.OrderID == sel.Value);
            if (_currentOrder == null) return;

            // Fetch line items for this order
            _lines = _ctrl.GetOrderLines(_currentOrder.OrderID);

            PopulateHeader(_currentOrder);
            RefreshLineGrid();
            SetEditPanelEnabled(true);

            // Cannot edit a Cancelled order — disable save but keep Cancel button visible
            bool isCancelled = _currentOrder.OrderStatus == "Cancelled";
            btnSaveChanges.Enabled = !isCancelled;
            btnAddLine.Enabled     = !isCancelled;
            btnRemoveLine.Enabled  = !isCancelled;
            txtAddQty.Enabled      = !isCancelled;
            cboAddProduct.Enabled  = !isCancelled;

            // Already cancelled — Cancel button is irrelevant
            btnCancelOrder.Enabled = !isCancelled;
        }

        private void PopulateHeader(OrderEntity o)
        {
            txtOrderID.Text       = o.OrderID;
            txtCustomer.Text      = o.CustomerName;
            txtContactName.Text   = o.OrderContactName;
            txtShippingAddr.Text  = o.ShippingAddress;
            txtBillingAddr.Text   = o.BillingAddress;
            dtpDelivery.Value     = o.DeliveryDate > DateTime.MinValue
                                        ? o.DeliveryDate : DateTime.Today;

            int idx = cboStatus.FindStringExact(o.OrderStatus);
            cboStatus.SelectedIndex = idx >= 0 ? idx : 0;

            // Discount
            if (string.IsNullOrEmpty(o.DiscountType) || o.DiscountType == "None")
            {
                cboDiscountType.SelectedIndex = 0;
                txtDiscountValue.Text  = "0";
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

        // ── Line-item helpers ──────────────────────────────────────────────────
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

        // ── Save Changes (Edit Order) ────────────────────────────────────────────
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
            if (dtype == "Amount")     discountAmount = discountValue;
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
                DiscountAmount   = discountAmount
            };

            var (ok, message) = _ctrl.SubmitModifyOrder(header, new List<OrderLineEntity>(_lines));
            if (ok)
            {
                MessageBox.Show(message, "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                // Refresh the search list to reflect new status
                ReloadOrderCombo();
            }
            else
            {
                MessageBox.Show(message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Cancel Order ───────────────────────────────────────────────────────────
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

            var (ok, message) = _ctrl.CancelOrder(_currentOrder.OrderID);
            if (ok)
            {
                MessageBox.Show(message, "Cancelled",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Update local state to reflect cancellation
                _currentOrder.OrderStatus = "Cancelled";
                btnSaveChanges.Enabled  = false;
                btnCancelOrder.Enabled  = false;
                btnAddLine.Enabled      = false;
                btnRemoveLine.Enabled   = false;
                txtAddQty.Enabled       = false;
                cboAddProduct.Enabled   = false;

                int idx = cboStatus.FindStringExact("Cancelled");
                if (idx >= 0) cboStatus.SelectedIndex = idx;

                ReloadOrderCombo();
            }
            else
            {
                MessageBox.Show(message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────────
        private void SetEditPanelEnabled(bool enabled)
        {
            pnlEditCard.Visible    = enabled;
            pnlLinesCard.Visible   = enabled;
            pnlActionsBar.Visible  = enabled;
        }

        private void ReloadOrderCombo()
        {
            var vm = _ctrl.GetModifyOrderVM();
            string currentId = _currentOrder?.OrderID;

            cboSearchOrder.Items.Clear();
            cboSearchOrder.Items.Add(new ComboItem("-- Select Order --", ""));
            foreach (var o in vm.Orders)
                cboSearchOrder.Items.Add(
                    new ComboItem(
                        $"{o.OrderID}  –  {o.CustomerName}  [{o.OrderStatus}]",
                        o.OrderID));

            // Re-select the same order if still present
            if (!string.IsNullOrEmpty(currentId))
            {
                for (int i = 1; i < cboSearchOrder.Items.Count; i++)
                {
                    if (((ComboItem)cboSearchOrder.Items[i]).Value == currentId)
                    { cboSearchOrder.SelectedIndex = i; break; }
                }
            }
        }

        // ── TopNavBar navigation ───────────────────────────────────────────────
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }

        // ── ComboItem helper ───────────────────────────────────────────────────
        private class ComboItem
        {
            public string Text  { get; }
            public string Value { get; }
            public ComboItem(string text, string value) { Text = text; Value = value; }
            public override string ToString() => Text;
        }
    }
}
