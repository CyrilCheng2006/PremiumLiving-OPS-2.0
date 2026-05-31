using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.OrderProcessing
{
    /// <summary>
    /// Create Order — View layer.
    ///
    /// MVC contract:
    ///   • Controller produces CreateOrderViewModel (incl. auto-generated NextOrderId).
    ///   • View displays the ID read-only; SalesID and IssuedTime are stamped at Submit time.
    ///   • NO business logic or DB calls in this class.
    ///   • CardPanel three-layer card structure throughout.
    /// </summary>
    public partial class CreateOrderForm : Form
    {
        private readonly OrderProcessingController _ctrl = new OrderProcessingController();

        private string                    _orderId;               // auto-generated, set on Load
        private readonly List<OrderLineEntity> _lines    = new List<OrderLineEntity>();
        private List<ProductLookup>            _products = new List<ProductLookup>();

        public CreateOrderForm()
        {
            InitializeComponent();
            this.Load += CreateOrderForm_Load;
        }

        // ── Load ────────────────────────────────────────────────────────────
        private void CreateOrderForm_Load(object sender, EventArgs e)
        {
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;

            var vm = _ctrl.GetCreateOrderVM();  // Controller generates NextOrderId

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Order Processing  ›  Create Order");

            // ─ Auto-generated Order ID (read-only display)
            _orderId              = vm.NextOrderId;
            lblOrderIdValue.Text  = _orderId;

            // ─ Customer dropdown  (ID — Name)
            cboCustomer.Items.Clear();
            cboCustomer.Items.Add(new ComboItem("-- Select Customer --", ""));
            foreach (var c in vm.Customers)
                cboCustomer.Items.Add(new ComboItem(
                    $"{c.CustomerID}  —  {c.CustomerName}", c.CustomerID));
            cboCustomer.SelectedIndex = 0;

            // ─ Linked Quotation dropdown (Pending only)
            cboQuotation.Items.Clear();
            cboQuotation.Items.Add(new ComboItem("-- None --", ""));
            foreach (var q in vm.PendingQuotations)
                cboQuotation.Items.Add(new ComboItem(
                    $"{q.QuotationID}  –  {q.CustomerName}  (HK$ {q.TotalAmount:N0})",
                    q.QuotationID));
            cboQuotation.SelectedIndex = 0;

            // ─ Product dropdown
            _products = vm.Products;
            cboProduct.Items.Clear();
            cboProduct.Items.Add(new ComboItem("-- Select Product --", ""));
            foreach (var p in _products)
                cboProduct.Items.Add(new ComboItem(p.DisplayText, p.ItemID));
            cboProduct.SelectedIndex = 0;

            dtpDelivery.Value             = DateTime.Today.AddDays(14);
            cboDiscountType.SelectedIndex = 0;
            RefreshLineGrid();
        }

        // ── Same-address checkbox ──────────────────────────────────────────
        private void chkSameAddress_CheckedChanged(object sender, EventArgs e)
        {
            if (chkSameAddress.Checked)
            {
                txtBillingAddr.Text    = txtShippingAddr.Text;
                txtBillingAddr.Enabled = false;
                txtBillingAddr.BackColor = System.Drawing.Color.FromArgb(235, 240, 250);
            }
            else
            {
                txtBillingAddr.Enabled   = true;
                txtBillingAddr.BackColor = System.Drawing.Color.FromArgb(245, 248, 255);
            }
        }

        // Keep Billing Address in sync while checkbox is active
        // (wire this to txtShippingAddr.TextChanged in constructor if needed)
        private void SyncBillingIfSame()
        {
            if (chkSameAddress.Checked)
                txtBillingAddr.Text = txtShippingAddr.Text;
        }

        // ── Line-item helpers ─────────────────────────────────────────────
        private void RefreshLineGrid()
        {
            dgvLines.Rows.Clear();
            double subtotal = 0;
            foreach (var l in _lines)
            {
                dgvLines.Rows.Add(l.ItemID, l.ItemName, l.Quantity,
                                  $"HK$ {l.Price:N2}", $"HK$ {l.LineTotal:N2}");
                subtotal += l.LineTotal;
            }
            lblSubtotal.Text = $"Subtotal:  HK$ {subtotal:N2}";
            RecalcGrandTotal(subtotal);
        }

        private void RecalcGrandTotal(double subtotal)
        {
            double discount = 0;
            string dtype = cboDiscountType.SelectedItem?.ToString() ?? "None";
            if (dtype == "Amount")
                double.TryParse(txtDiscountValue.Text, out discount);
            else if (dtype == "Rate (%)")
            {
                if (double.TryParse(txtDiscountValue.Text, out double rate))
                    discount = subtotal * rate / 100.0;
            }
            lblGrandTotal.Text = $"Grand Total:  HK$ {subtotal - discount:N2}";
        }

        // ── Add / Remove line ──────────────────────────────────────────
        private void btnAddLine_Click(object sender, EventArgs e)
        {
            var selProduct = cboProduct.SelectedItem as ComboItem;
            if (selProduct == null || string.IsNullOrEmpty(selProduct.Value))
            { ShowWarning("Please select a product."); return; }

            if (!int.TryParse(txtQty.Text, out int qty) || qty <= 0)
            { ShowWarning("Please enter a valid quantity (minimum 1)."); return; }

            var product = _products.Find(p => p.ItemID == selProduct.Value);
            if (product == null) return;

            var existing = _lines.Find(l => l.ItemID == product.ItemID);
            if (existing != null)
                existing.Quantity += qty;
            else
                _lines.Add(new OrderLineEntity
                {
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
            { ShowWarning("Please select a line item to remove."); return; }

            string itemId = dgvLines.SelectedRows[0].Cells["colLineItemID"].Value?.ToString();
            _lines.RemoveAll(l => l.ItemID == itemId);
            RefreshLineGrid();
        }

        // ── Discount ───────────────────────────────────────────────────
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

        // ── Submit ─────────────────────────────────────────────────────
        private void btnSubmit_Click(object sender, EventArgs e)
        {
            // ─ Validate
            var selCustomer = cboCustomer.SelectedItem as ComboItem;
            if (selCustomer == null || string.IsNullOrEmpty(selCustomer.Value))
            { ShowWarning("Please select a customer."); return; }

            if (string.IsNullOrWhiteSpace(txtShippingAddr.Text))
            { ShowWarning("Shipping address is required."); return; }

            if (string.IsNullOrWhiteSpace(txtBillingAddr.Text))
            { ShowWarning("Billing address is required."); return; }

            if (_lines.Count == 0)
            { ShowWarning("Please add at least one order item before submitting."); return; }

            // Sync billing address one last time in case user typed in Shipping after checking the box
            SyncBillingIfSame();

            // ─ Build entity — SalesID and IssuedTime stamped here at submission time
            var selQuotation = cboQuotation.SelectedItem as ComboItem;
            string dtype = cboDiscountType.SelectedItem?.ToString() ?? "None";
            double.TryParse(txtDiscountValue.Text, out double discountValue);

            double sub = 0;
            foreach (var l in _lines) sub += l.LineTotal;

            double discountAmount = 0;
            if (dtype == "Amount")        discountAmount = discountValue;
            else if (dtype == "Rate (%)") discountAmount = sub * discountValue / 100.0;

            var header = new OrderEntity
            {
                OrderID          = _orderId,                              // auto-generated
                CustomerID       = selCustomer.Value,
                QuotationID      = selQuotation?.Value ?? "",
                DeliveryDate     = dtpDelivery.Value,
                ShippingAddress  = txtShippingAddr.Text.Trim(),
                BillingAddress   = txtBillingAddr.Text.Trim(),
                OrderContactName = txtContactName.Text.Trim(),
                DiscountType     = dtype == "None" ? null : dtype,
                DiscountValue    = discountValue,
                DiscountAmount   = discountAmount,
                SubTotal         = sub,
                GrandTotal       = sub - discountAmount,
                OrderStatus      = "Pending",
                // ─ Stamped automatically at submit ─────────────────
                SalesID          = SessionManager.CurrentUser?.StaffId ?? "",
                IssuedTime       = DateTime.Now
            };

            bool ok = _ctrl.SaveNewOrder(header, new List<OrderLineEntity>(_lines));
            if (ok)
            {
                MessageBox.Show(
                    $"Order {header.OrderID} has been created successfully.",
                    "Order Created", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearForm();
            }
            else
            {
                MessageBox.Show(
                    "Failed to save order. Please verify the details and try again.",
                    "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Clear all entered data?", "Confirm Clear",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                ClearForm();
        }

        private void ClearForm()
        {
            // Re-generate a fresh Order ID for the next entry
            _orderId             = _ctrl.GenerateOrderId();
            lblOrderIdValue.Text = _orderId;

            cboCustomer.SelectedIndex     = 0;
            cboQuotation.SelectedIndex    = 0;
            cboProduct.SelectedIndex      = 0;
            cboDiscountType.SelectedIndex = 0;
            txtShippingAddr.Text  = string.Empty;
            txtBillingAddr.Text   = string.Empty;
            txtContactName.Text   = string.Empty;
            txtDiscountValue.Text = "0";
            txtQty.Text           = "1";
            chkSameAddress.Checked = false;
            dtpDelivery.Value     = DateTime.Today.AddDays(14);
            _lines.Clear();
            RefreshLineGrid();
        }

        private static void ShowWarning(string msg)
            => MessageBox.Show(msg, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        // ── Nav / Logout ───────────────────────────────────────────────
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }

        // ── ComboItem helper ─────────────────────────────────────────
        private class ComboItem
        {
            public string Text  { get; }
            public string Value { get; }
            public ComboItem(string text, string value) { Text = text; Value = value; }
            public override string ToString() => Text;
        }
    }
}
