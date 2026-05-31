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

        private string                         _orderId;
        private readonly List<OrderLineEntity> _lines    = new List<OrderLineEntity>();
        private List<ProductLookup>            _products = new List<ProductLookup>();

        // Full address list loaded from VM; filtered per customer in PopulateAddresses()
        private List<AddressLookup> _allAddresses = new List<AddressLookup>();

        public CreateOrderForm()
        {
            InitializeComponent();
            this.Load += CreateOrderForm_Load;
        }

        // ── Load ──────────────────────────────────────────────────────────────────────────
        private void CreateOrderForm_Load(object sender, EventArgs e)
        {
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;

            var vm = _ctrl.GetCreateOrderVM();

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Order Processing  ›  Create Order");

            _orderId             = vm.NextOrderId;
            lblOrderIdValue.Text = _orderId;

            // Cache full address list for filtering
            _allAddresses = vm.Addresses ?? new List<AddressLookup>();

            // Customer
            cboCustomer.Items.Clear();
            cboCustomer.Items.Add(new ComboItem("-- Select Customer --", ""));
            foreach (var c in vm.Customers)
                cboCustomer.Items.Add(new ComboItem(
                    $"{c.CustomerID}  —  {c.CustomerName}", c.CustomerID));
            cboCustomer.SelectedIndex = 0;

            // AddressID — initially empty until a customer is chosen
            cboAddressId.Items.Clear();
            cboAddressId.Items.Add(new ComboItem("-- Select Address --", ""));
            cboAddressId.SelectedIndex = 0;

            // Linked Quotation
            cboQuotation.Items.Clear();
            cboQuotation.Items.Add(new ComboItem("-- None --", ""));
            foreach (var q in vm.PendingQuotations)
                cboQuotation.Items.Add(new ComboItem(
                    $"{q.QuotationID}  –  {q.CustomerName}  (HK$ {q.TotalAmount:N0})",
                    q.QuotationID));
            cboQuotation.SelectedIndex = 0;

            // Product
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

        // ── Customer selection → populate Address ComboBox ─────────────────────────────
        private void cboCustomer_SelectedIndexChanged(object sender, EventArgs e)
        {
            var sel = cboCustomer.SelectedItem as ComboItem;
            PopulateAddresses(sel?.Value ?? "");
        }

        /// <summary>
        /// Fills cboAddressId with addresses that belong to the given customerId.
        /// Delegates filtering to Controller (no business logic in View).
        /// </summary>
        private void PopulateAddresses(string customerId)
        {
            cboAddressId.Items.Clear();
            cboAddressId.Items.Add(new ComboItem("-- Select Address --", ""));

            var filtered = _ctrl.GetAddressesByCustomer(customerId, _allAddresses);
            foreach (var a in filtered)
                cboAddressId.Items.Add(new ComboItem(a.DisplayText, a.AddressId));

            cboAddressId.SelectedIndex = 0;
            txtShippingAddr.Text = string.Empty;
        }

        // ── AddressID selection → auto-fill Shipping Address ───────────────────────────
        private void cboAddressId_SelectedIndexChanged(object sender, EventArgs e)
        {
            var sel = cboAddressId.SelectedItem as ComboItem;
            if (sel == null || string.IsNullOrEmpty(sel.Value))
            {
                txtShippingAddr.Text = string.Empty;
                return;
            }

            // Find full address text from cached list
            var addr = _allAddresses.Find(a => a.AddressId == sel.Value);
            if (addr != null)
            {
                txtShippingAddr.Text = addr.FullAddress;
                // If 'Same as Shipping' is checked, billing syncs automatically
                if (chkSameAddress.Checked)
                    txtBillingAddr.Text = addr.FullAddress;
            }
        }

        // ── Same-address checkbox ───────────────────────────────────────────────────
        private void chkSameAddress_CheckedChanged(object sender, EventArgs e)
        {
            if (chkSameAddress.Checked)
            {
                txtBillingAddr.Text      = txtShippingAddr.Text;
                txtBillingAddr.Enabled   = false;
                txtBillingAddr.BackColor = System.Drawing.Color.FromArgb(235, 240, 250);
            }
            else
            {
                txtBillingAddr.Enabled   = true;
                txtBillingAddr.BackColor = System.Drawing.Color.FromArgb(245, 248, 255);
            }
        }

        // Keep Billing in sync while checkbox is checked
        private void txtShippingAddr_TextChanged(object sender, EventArgs e)
        {
            if (chkSameAddress.Checked)
                txtBillingAddr.Text = txtShippingAddr.Text;
        }

        // ── Line-item helpers ─────────────────────────────────────────────────────

        /// <summary>
        /// Refreshes the DataGridView rows from _lines, then calls UpdateSummary().
        /// Single source of truth for all row rendering and totals display.
        /// </summary>
        private void RefreshLineGrid()
        {
            dgvLines.Rows.Clear();
            foreach (var l in _lines)
            {
                dgvLines.Rows.Add(l.ItemID, l.ItemName, l.Quantity,
                                  $"HK$ {l.Price:N2}", $"HK$ {l.LineTotal:N2}");
            }
            UpdateSummary();
        }

        /// <summary>
        /// Unified summary recalculation — reads _lines directly so that
        /// Subtotal and Grand Total are always in sync regardless of trigger.
        /// Subtotal  = sum of all line totals (Qty × Price).
        /// GrandTotal = Subtotal − discount (clamped to ≥ 0).
        /// </summary>
        private void UpdateSummary()
        {
            // 1. Subtotal — sum of all line totals
            double subtotal = 0;
            foreach (var l in _lines)
                subtotal += l.LineTotal;

            lblSubtotal.Text = $"Subtotal:  HK$ {subtotal:N2}";

            // 2. Discount
            string dtype    = cboDiscountType.SelectedItem?.ToString() ?? "None";
            double discount = 0;
            if (dtype == "Amount")
            {
                double.TryParse(txtDiscountValue.Text, out discount);
            }
            else if (dtype == "Rate (%)")
            {
                if (double.TryParse(txtDiscountValue.Text, out double rate))
                    discount = subtotal * rate / 100.0;
            }

            // Clamp so Grand Total never goes negative
            if (discount < 0)        discount = 0;
            if (discount > subtotal) discount = subtotal;

            // 3. Grand Total
            lblGrandTotal.Text = $"Grand Total:  HK$ {subtotal - discount:N2}";
        }

        // ── Add / Remove line ──────────────────────────────────────────────────────
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

        // ── Discount ────────────────────────────────────────────────────────────────────
        private void cboDiscountType_SelectedIndexChanged(object sender, EventArgs e)
        {
            string dtype     = cboDiscountType.SelectedItem?.ToString() ?? "None";
            bool hasDiscount = dtype != "None";
            txtDiscountValue.Enabled = hasDiscount;
            if (!hasDiscount)
            {
                txtDiscountValue.Text = "0";
                lblDiscountUnit.Text  = "";
            }
            else
            {
                lblDiscountUnit.Text = dtype == "Rate (%)" ? "%" : "HK$";
            }
            UpdateSummary();
        }

        private void txtDiscountValue_TextChanged(object sender, EventArgs e)
        {
            UpdateSummary();
        }

        // ── Submit ────────────────────────────────────────────────────────────────────────
        private void btnSubmit_Click(object sender, EventArgs e)
        {
            var selCustomer = cboCustomer.SelectedItem as ComboItem;
            if (selCustomer == null || string.IsNullOrEmpty(selCustomer.Value))
            { ShowWarning("Please select a customer."); return; }

            if (string.IsNullOrWhiteSpace(txtShippingAddr.Text))
            { ShowWarning("Shipping address is required."); return; }

            if (string.IsNullOrWhiteSpace(txtBillingAddr.Text))
            { ShowWarning("Billing address is required."); return; }

            if (string.IsNullOrWhiteSpace(txtContactName.Text))
            { ShowWarning("Order contact name is required."); return; }

            if (_lines.Count == 0)
            { ShowWarning("Please add at least one order item before submitting."); return; }

            var selQuotation = cboQuotation.SelectedItem as ComboItem;
            var selAddress   = cboAddressId.SelectedItem  as ComboItem;
            string dtype = cboDiscountType.SelectedItem?.ToString() ?? "None";
            double.TryParse(txtDiscountValue.Text, out double discountValue);

            // 1. Subtotal — sum of all line totals
            double sub = 0;
            foreach (var l in _lines) sub += l.LineTotal;

            // 2. Discount amount
            double discountAmount = 0;
            if (dtype == "Amount")        discountAmount = discountValue;
            else if (dtype == "Rate (%)") discountAmount = sub * discountValue / 100.0;

            // Clamp so Grand Total never goes negative
            if (discountAmount < 0)    discountAmount = 0;
            if (discountAmount > sub)  discountAmount = sub;

            var header = new OrderEntity
            {
                OrderID          = _orderId,
                CustomerID       = selCustomer.Value,
                AddressID        = selAddress?.Value ?? "",
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

        // ── Clear ────────────────────────────────────────────────────────────────────────────
        private void btnClear_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Clear all entered data?", "Confirm Clear",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                ClearForm();
        }

        private void ClearForm()
        {
            _orderId             = _ctrl.GenerateOrderId();
            lblOrderIdValue.Text = _orderId;

            cboCustomer.SelectedIndex     = 0;
            // Clearing customer will trigger SelectedIndexChanged → PopulateAddresses("")
            cboQuotation.SelectedIndex    = 0;
            cboProduct.SelectedIndex      = 0;
            cboDiscountType.SelectedIndex = 0;
            txtShippingAddr.Text  = string.Empty;
            txtBillingAddr.Text   = string.Empty;
            txtContactName.Text   = string.Empty;
            txtDiscountValue.Text = "0";
            lblDiscountUnit.Text  = "";
            txtQty.Text           = "1";
            chkSameAddress.Checked = false;
            dtpDelivery.Value     = DateTime.Today.AddDays(14);
            _lines.Clear();
            RefreshLineGrid();
        }

        private static void ShowWarning(string msg)
            => MessageBox.Show(msg, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        // ── Nav / Logout ───────────────────────────────────────────────────────────────────
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }

        // ── ComboItem helper ─────────────────────────────────────────────────────────────────
        private class ComboItem
        {
            public string Text  { get; }
            public string Value { get; }
            public ComboItem(string text, string value) { Text = text; Value = value; }
            public override string ToString() => Text;
        }
    }
}
