using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
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
    ///
    /// Picker fields:
    ///   Customer, Linked Quotation  → SearchPickerDialog (keyword search popup)
    ///   Order Item + Qty + Add      → AddOrderItemDialog (combined search + qty + confirm)
    ///
    /// Quotation auto-fill:
    ///   When a Quotation is linked, its items are loaded via
    ///   OrderProcessingController.GetQuotationDetail() and pushed into _lines,
    ///   replacing any previously entered items.  The user may still add / remove
    ///   lines afterwards.
    /// </summary>
    public partial class CreateOrderForm : Form
    {
        private readonly OrderProcessingController _ctrl = new OrderProcessingController();

        private string                         _orderId;
        private readonly List<OrderLineEntity> _lines        = new List<OrderLineEntity>();
        private List<ProductLookup>            _products     = new List<ProductLookup>();
        private List<AddressLookup>            _allAddresses = new List<AddressLookup>();
        private List<CustomerEntity>           _customers    = new List<CustomerEntity>();
        private List<QuotationEntity>          _quotations   = new List<QuotationEntity>();

        private string _selectedCustomerId   = "";
        private string _selectedCustomerName = "";
        private string _selectedQuotationId  = "";

        public CreateOrderForm()
        {
            InitializeComponent();
            this.Load += CreateOrderForm_Load;
        }

        private void CreateOrderForm_Load(object sender, EventArgs e)
        {
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;

            var vm = _ctrl.GetCreateOrderVM();

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Order Processing  \u203a  Create Order");

            _orderId             = vm.NextOrderId;
            lblOrderIdValue.Text = _orderId;

            _allAddresses = vm.Addresses        ?? new List<AddressLookup>();
            _customers    = vm.Customers         ?? new List<CustomerEntity>();
            _quotations   = vm.PendingQuotations ?? new List<QuotationEntity>();
            _products     = vm.Products          ?? new List<ProductLookup>();

            cboAddressId.Items.Clear();
            cboAddressId.Items.Add(new ComboItem("-- Select Address --", ""));
            cboAddressId.SelectedIndex = 0;

            cboDiscountType.SelectedIndex = 0;
            dtpDelivery.Value = DateTime.Today.AddDays(14);
            RefreshLineGrid();
            ResetPickerLabels();
        }

        private void btnPickCustomer_Click(object sender, EventArgs e)
        {
            var items = _customers
                .Select(c => new SearchPickerDialog.PickerItem
                {
                    Id      = c.CustomerID,
                    Display = $"{c.CustomerID}  \u2013  {c.CustomerName}"
                }).ToList();

            using var dlg = new SearchPickerDialog("Select Customer", items);
            if (dlg.ShowDialog(this) != DialogResult.OK || dlg.SelectedItem == null) return;

            _selectedCustomerId         = dlg.SelectedItem.Id;
            _selectedCustomerName       = dlg.SelectedItem.Display;
            lblCustomerPicked.Text      = dlg.SelectedItem.Display;
            lblCustomerPicked.ForeColor = System.Drawing.Color.FromArgb(15, 31, 53);
            PopulateAddresses(_selectedCustomerId);
        }

        /// <summary>
        /// Opens the quotation picker.  When a valid Quotation is chosen:
        ///   1. Stores the QuotationID.
        ///   2. Fetches the Quotation detail (including its items) from the
        ///      Controller — which first checks the in-memory cache then falls
        ///      back to DB via GetQuotationDetail().
        ///   3. Converts each QuotationItemEntity → OrderLineEntity and
        ///      replaces the current _lines list.
        ///   4. Refreshes the Order Item grid so the user sees the imported rows.
        ///
        /// Choosing "(None)" clears the linked quotation but does NOT wipe
        /// manually added lines (consistent with existing behaviour).
        /// </summary>
        private void btnPickQuotation_Click(object sender, EventArgs e)
        {
            var items = new List<SearchPickerDialog.PickerItem>
            {
                new SearchPickerDialog.PickerItem { Id = "", Display = "(None)" }
            };
            items.AddRange(_quotations.Select(q => new SearchPickerDialog.PickerItem
            {
                Id      = q.QuotationID,
                Display = $"{q.QuotationID}  \u2013  {q.CustomerName}  (HK$ {q.TotalAmount:N0})  [{q.QuotationStatus}]"
            }));

            using var dlg = new SearchPickerDialog("Link Quotation", items);
            if (dlg.ShowDialog(this) != DialogResult.OK || dlg.SelectedItem == null) return;

            _selectedQuotationId         = dlg.SelectedItem.Id;
            lblQuotationPicked.Text      = string.IsNullOrEmpty(dlg.SelectedItem.Id)
                ? "(None)"
                : dlg.SelectedItem.Display;
            lblQuotationPicked.ForeColor = string.IsNullOrEmpty(dlg.SelectedItem.Id)
                ? System.Drawing.Color.FromArgb(98, 112, 135)
                : System.Drawing.Color.FromArgb(15, 31, 53);

            // ── Auto-populate Order Items from Quotation ───────────────────────
            if (!string.IsNullOrEmpty(_selectedQuotationId))
            {
                var detail = _ctrl.GetQuotationDetail(_selectedQuotationId);

                if (detail?.Items != null && detail.Items.Count > 0)
                {
                    // Ask user whether to replace existing lines (if any)
                    if (_lines.Count > 0)
                    {
                        var confirm = MessageBox.Show(
                            "Replace the current order items with items from the selected Quotation?",
                            "Import Quotation Items",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (confirm != DialogResult.Yes)
                        {
                            RefreshLineGrid();
                            return;
                        }
                    }

                    // Convert QuotationItemEntity → OrderLineEntity
                    _lines.Clear();
                    foreach (var qi in detail.Items)
                    {
                        // Apply per-line discount: effective price = UnitPrice * (1 - disc%)
                        double effectivePrice = qi.UnitPrice * (1.0 - qi.DiscountPercent / 100.0);

                        _lines.Add(new OrderLineEntity
                        {
                            ItemID   = qi.ItemID,
                            ItemName = qi.ProductName,
                            Quantity = qi.Quantity,
                            Price    = effectivePrice
                        });
                    }

                    RefreshLineGrid();

                    MessageBox.Show(
                        $"{detail.Items.Count} item(s) imported from Quotation {_selectedQuotationId}.",
                        "Items Imported",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    // Quotation has no items — inform the user but don't discard existing lines
                    MessageBox.Show(
                        $"Quotation {_selectedQuotationId} has no line items.  You can add items manually.",
                        "No Items Found",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            else
            {
                // (None) selected — keep existing lines unchanged
                RefreshLineGrid();
            }
        }

        private void btnAddItem_Click(object sender, EventArgs e)
        {
            using var dlg = new AddOrderItemDialog(_products);
            if (dlg.ShowDialog(this) != DialogResult.OK || dlg.SelectedProduct == null) return;

            var product = dlg.SelectedProduct;
            int qty     = dlg.SelectedQty;

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

        private void cboAddressId_SelectedIndexChanged(object sender, EventArgs e)
        {
            var sel = cboAddressId.SelectedItem as ComboItem;
            if (sel == null || string.IsNullOrEmpty(sel.Value)) { txtShippingAddr.Text = string.Empty; return; }
            var addr = _allAddresses.Find(a => a.AddressId == sel.Value);
            if (addr != null)
            {
                txtShippingAddr.Text = addr.FullAddress;
                if (chkSameAddress.Checked) txtBillingAddr.Text = addr.FullAddress;
            }
        }

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
        { if (chkSameAddress.Checked) txtBillingAddr.Text = txtShippingAddr.Text; }

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

            string dtype = cboDiscountType.SelectedItem?.ToString() ?? "None";
            double discount = 0;
            if (dtype == "Amount")
                double.TryParse(txtDiscountValue.Text, out discount);
            else if (dtype == "Rate (%)" && double.TryParse(txtDiscountValue.Text, out double rate))
                discount = subtotal * rate / 100.0;
            if (discount < 0)        discount = 0;
            if (discount > subtotal) discount = subtotal;

            lblDiscountAmountValue.Text = $"HK$ {discount:N2}";
            lblGrandTotalValue.Text     = $"HK$ {subtotal - discount:N2}";
            pnlFooterContent.PerformLayout();
        }

        private void cboDiscountType_SelectedIndexChanged(object sender, EventArgs e)
        {
            string dtype = cboDiscountType.SelectedItem?.ToString() ?? "None";
            bool has     = dtype != "None";
            txtDiscountValue.Enabled = has;
            if (!has) { txtDiscountValue.Text = "0"; lblDiscountUnit.Text = ""; }
            else lblDiscountUnit.Text = dtype == "Rate (%)" ? "%" : "HK$";
            UpdateSummary();
        }

        private void txtDiscountValue_TextChanged(object sender, EventArgs e) => UpdateSummary();

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
                MessageBox.Show($"Order {header.OrderID} has been created successfully.",
                    "Order Created", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearForm();
            }
            else
                MessageBox.Show("Failed to save order. Please verify the details and try again.",
                    "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

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
            _selectedCustomerId  = "";
            _selectedCustomerName = "";
            _selectedQuotationId  = "";
            ResetPickerLabels();
            cboAddressId.SelectedIndex    = 0;
            cboDiscountType.SelectedIndex = 0;
            txtShippingAddr.Text   = string.Empty;
            txtBillingAddr.Text    = string.Empty;
            txtContactName.Text    = string.Empty;
            txtDiscountValue.Text  = "0";
            lblDiscountUnit.Text   = "";
            chkSameAddress.Checked = false;
            dtpDelivery.Value      = DateTime.Today.AddDays(14);
            _lines.Clear();
            RefreshLineGrid();
        }

        private void ResetPickerLabels()
        {
            lblCustomerPicked.Text       = "(None selected)";
            lblCustomerPicked.ForeColor  = System.Drawing.Color.FromArgb(98, 112, 135);
            lblQuotationPicked.Text      = "(None)";
            lblQuotationPicked.ForeColor = System.Drawing.Color.FromArgb(98, 112, 135);
        }

        private static void ShowWarning(string msg)
            => MessageBox.Show(msg, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to log out?",
                                "Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                SessionManager.Clear();
                Application.Restart();
            }
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
