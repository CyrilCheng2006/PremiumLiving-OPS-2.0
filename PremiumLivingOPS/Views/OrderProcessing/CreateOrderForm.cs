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
    ///
    /// Picker fields:
    ///   Customer, Linked Quotation, and Order Item are now opened via PickerDialog
    ///   (searchable popup) rather than a plain ComboBox drop-down.
    /// </summary>
    public partial class CreateOrderForm : Form
    {
        private readonly OrderProcessingController _ctrl = new OrderProcessingController();

        private string                         _orderId;
        private readonly List<OrderLineEntity> _lines    = new List<OrderLineEntity>();
        private List<ProductLookup>            _products = new List<ProductLookup>();
        private List<AddressLookup>            _allAddresses = new List<AddressLookup>();

        // ── Picker backing data ──────────────────────────────────────────────────────────
        private List<CustomerEntity>   _customers   = new List<CustomerEntity>();
        private List<QuotationLookup>  _quotations  = new List<QuotationLookup>();

        // Currently-selected values from pickers
        private string _selectedCustomerId   = "";
        private string _selectedCustomerName = "";
        private string _selectedQuotationId  = "";
        private string _selectedProductId    = "";
        private string _selectedProductName  = "";

        public CreateOrderForm()
        {
            InitializeComponent();
            this.Load += CreateOrderForm_Load;
        }

        // ── Load ──────────────────────────────────────────────────────────────────
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

            _allAddresses = vm.Addresses ?? new List<AddressLookup>();
            _customers    = vm.Customers ?? new List<CustomerEntity>();
            _quotations   = vm.PendingQuotations ?? new List<QuotationLookup>();
            _products     = vm.Products ?? new List<ProductLookup>();

            // Address ComboBox — still a drop-down (filtered per customer)
            cboAddressId.Items.Clear();
            cboAddressId.Items.Add(new ComboItem("-- Select Address --", ""));
            cboAddressId.SelectedIndex = 0;

            // Discount
            cboDiscountType.SelectedIndex = 0;

            dtpDelivery.Value = DateTime.Today.AddDays(14);
            RefreshLineGrid();

            // Reset picker label displays
            ResetPickerLabels();
        }

        // ── Picker button handlers ────────────────────────────────────────────────────

        /// <summary>Opens the Customer picker popup.</summary>
        private void btnPickCustomer_Click(object sender, EventArgs e)
        {
            var rows = new List<PickerRow>();
            foreach (var c in _customers)
                rows.Add(new PickerRow(c.CustomerID, c.CustomerName, c.CustomerID));

            using var dlg = new PickerDialog(
                "Select Customer",
                new[] { "Customer ID", "Customer Name" },
                rows);

            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            _selectedCustomerId   = dlg.SelectedId;
            _selectedCustomerName = dlg.SelectedText;
            lblCustomerPicked.Text = $"{_selectedCustomerId}  —  {_selectedCustomerName}";
            PopulateAddresses(_selectedCustomerId);
        }

        /// <summary>Opens the Linked Quotation picker popup.</summary>
        private void btnPickQuotation_Click(object sender, EventArgs e)
        {
            var rows = new List<PickerRow>();
            // Blank option first
            rows.Add(new PickerRow("", "(None)", "", ""));
            foreach (var q in _quotations)
                rows.Add(new PickerRow(
                    q.QuotationID,
                    q.CustomerName,
                    q.QuotationID,
                    $"HK$ {q.TotalAmount:N0}"));

            using var dlg = new PickerDialog(
                "Link Quotation",
                new[] { "Quotation ID", "Customer", "Ref ID", "Total" },
                rows);

            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            _selectedQuotationId      = dlg.SelectedId;
            lblQuotationPicked.Text   = string.IsNullOrEmpty(dlg.SelectedId)
                ? "(None)"
                : $"{dlg.SelectedId}  –  {dlg.SelectedText}";
        }

        /// <summary>Opens the Product (Select Item) picker popup.</summary>
        private void btnPickProduct_Click(object sender, EventArgs e)
        {
            var rows = new List<PickerRow>();
            foreach (var p in _products)
                rows.Add(new PickerRow(
                    p.ItemID,
                    p.ItemName,
                    p.ItemID,
                    $"HK$ {p.SalesPrice:N2}"));

            using var dlg = new PickerDialog(
                "Select Item",
                new[] { "Item ID", "Item Name", "Ref ID", "Sales Price" },
                rows);

            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            _selectedProductId   = dlg.SelectedId;
            _selectedProductName = dlg.SelectedText;
            lblProductPicked.Text = $"{_selectedProductId}  —  {_selectedProductName}";
        }

        // ── Customer selection → populate Address ComboBox ─────────────────────────────
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

        // ── AddressID selection → auto-fill Shipping Address ──────────────────────────
        private void cboAddressId_SelectedIndexChanged(object sender, EventArgs e)
        {
            var sel = cboAddressId.SelectedItem as ComboItem;
            if (sel == null || string.IsNullOrEmpty(sel.Value))
            {
                txtShippingAddr.Text = string.Empty;
                return;
            }
            var addr = _allAddresses.Find(a => a.AddressId == sel.Value);
            if (addr != null)
            {
                txtShippingAddr.Text = addr.FullAddress;
                if (chkSameAddress.Checked)
                    txtBillingAddr.Text = addr.FullAddress;
            }
        }

        // ── Same-address checkbox ──────────────────────────────────────────────────
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

        private void txtShippingAddr_TextChanged(object sender, EventArgs e)
        {
            if (chkSameAddress.Checked)
                txtBillingAddr.Text = txtShippingAddr.Text;
        }

        // ── Line-item helpers ──────────────────────────────────────────────────────
        private void RefreshLineGrid()
        {
            dgvLines.Rows.Clear();
            foreach (var l in _lines)
                dgvLines.Rows.Add(l.ItemID, l.ItemName, l.Quantity,
                                  $"HK$ {l.Price:N2}", $"HK$ {l.LineTotal:N2}");
            UpdateSummary();
        }

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
            pnlFooterContent.PerformLayout();
        }

        // ── Add / Remove line ────────────────────────────────────────────────────
        private void btnAddLine_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedProductId))
            { ShowWarning("Please select a product via the \"Select Item\" button."); return; }

            if (!int.TryParse(txtQty.Text, out int qty) || qty <= 0)
            { ShowWarning("Please enter a valid quantity (minimum 1)."); return; }

            var product = _products.Find(p => p.ItemID == _selectedProductId);
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

            // Reset product picker
            _selectedProductId   = "";
            _selectedProductName = "";
            lblProductPicked.Text = "(None selected)";
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

        // ── Discount ─────────────────────────────────────────────────────────────────
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
            => UpdateSummary();

        // ── Submit ───────────────────────────────────────────────────────────────────
        private void btnSubmit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedCustomerId))
            { ShowWarning("Please select a customer."); return; }

            if (string.IsNullOrWhiteSpace(txtShippingAddr.Text))
            { ShowWarning("Shipping address is required."); return; }

            if (string.IsNullOrWhiteSpace(txtBillingAddr.Text))
            { ShowWarning("Billing address is required."); return; }

            if (string.IsNullOrWhiteSpace(txtContactName.Text))
            { ShowWarning("Order contact name is required."); return; }

            if (_lines.Count == 0)
            { ShowWarning("Please add at least one order item before submitting."); return; }

            var selAddress = cboAddressId.SelectedItem as ComboItem;
            string dtype   = cboDiscountType.SelectedItem?.ToString() ?? "None";
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
                OrderID          = _orderId,
                CustomerID       = _selectedCustomerId,
                AddressID        = selAddress?.Value ?? "",
                QuotationID      = _selectedQuotationId ?? "",
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

        // ── Clear ────────────────────────────────────────────────────────────────────
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

            _selectedCustomerId   = "";
            _selectedCustomerName = "";
            _selectedQuotationId  = "";
            _selectedProductId    = "";
            _selectedProductName  = "";
            ResetPickerLabels();

            cboAddressId.SelectedIndex    = 0;
            cboDiscountType.SelectedIndex = 0;
            txtShippingAddr.Text   = string.Empty;
            txtBillingAddr.Text    = string.Empty;
            txtContactName.Text    = string.Empty;
            txtDiscountValue.Text  = "0";
            lblDiscountUnit.Text   = "";
            txtQty.Text            = "1";
            chkSameAddress.Checked = false;
            dtpDelivery.Value      = DateTime.Today.AddDays(14);
            _lines.Clear();
            RefreshLineGrid();
        }

        private void ResetPickerLabels()
        {
            lblCustomerPicked.Text  = "(None selected)";
            lblQuotationPicked.Text = "(None)";
            lblProductPicked.Text   = "(None selected)";
        }

        private static void ShowWarning(string msg)
            => MessageBox.Show(msg, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        // ── Nav / Logout ───────────────────────────────────────────────────────────────
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }

        // ── ComboItem helper (Address drop-down only) ───────────────────────────────────
        private class ComboItem
        {
            public string Text  { get; }
            public string Value { get; }
            public ComboItem(string text, string value) { Text = text; Value = value; }
            public override string ToString() => Text;
        }
    }
}
