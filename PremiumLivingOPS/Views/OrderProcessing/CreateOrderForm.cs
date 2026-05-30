using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.OrderProcessing
{
    /// <summary>
    /// Create Order — Tab 3 of Order Processing Management.
    /// Allows staff to create a new sales order with line items.
    ///
    /// MVC contract (View layer):
    ///   • Calls OrderProcessingController for drop-down data and order submission.
    ///   • Uses AppShell (TopNavBar + UserBar) for navigation chrome.
    ///   • Contains NO business logic and NO direct DB calls.
    /// </summary>
    public partial class CreateOrderForm : Form
    {
        private readonly OrderProcessingController _ctrl = new OrderProcessingController();

        private readonly List<OrderLineEntity> _lines    = new List<OrderLineEntity>();
        private List<ProductLookup>            _products = new List<ProductLookup>();

        public CreateOrderForm()
        {
            InitializeComponent();
            this.Load += CreateOrderForm_Load;
        }

        // ── Load ─────────────────────────────────────────────────────────────────
        private void CreateOrderForm_Load(object sender, EventArgs e)
        {
            // Wire AppShell events — must be done once, before first data load
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;

            var vm = _ctrl.GetCreateOrderVM();

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Order Processing  ›  Create Order");

            // Customer combo
            cboCustomer.Items.Clear();
            cboCustomer.Items.Add(new ComboItem("-- Select Customer --", ""));
            foreach (var c in vm.Customers)
                cboCustomer.Items.Add(new ComboItem(c.CustomerName, c.CustomerID));
            cboCustomer.SelectedIndex = 0;

            // Quotation combo — PendingQuotations pre-filtered in Controller
            cboQuotation.Items.Clear();
            cboQuotation.Items.Add(new ComboItem("-- None --", ""));
            foreach (var q in vm.PendingQuotations)
                cboQuotation.Items.Add(new ComboItem(
                    $"{q.QuotationID}  –  {q.CustomerName}  (HK$ {q.TotalAmount:N0})",
                    q.QuotationID));
            cboQuotation.SelectedIndex = 0;

            // Product combo — DisplayText property on ProductLookup
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

        // ── Line-item helpers ────────────────────────────────────────────────────
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

        // ── Add / Remove line ────────────────────────────────────────────────────
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
            string itemId = dgvLines.SelectedRows[0].Cells["colLineItemID"].Value?.ToString();
            _lines.RemoveAll(l => l.ItemID == itemId);
            RefreshLineGrid();
        }

        // ── Discount ─────────────────────────────────────────────────────────────
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

        // ── Submit ─────────────────────────────────────────────────────────────
        private void btnSubmit_Click(object sender, EventArgs e)
        {
            var selCustomer  = cboCustomer.SelectedItem  as ComboItem;
            var selQuotation = cboQuotation.SelectedItem as ComboItem;
            string dtype = cboDiscountType.SelectedItem?.ToString() ?? "None";

            double discountValue = 0;
            double.TryParse(txtDiscountValue.Text, out discountValue);

            double sub = 0;
            foreach (var l in _lines) sub += l.LineTotal;
            double discountAmount = 0;
            if (dtype == "Amount")        discountAmount = discountValue;
            else if (dtype == "Rate (%)") discountAmount = sub * discountValue / 100.0;

            var header = new OrderEntity
            {
                OrderID          = txtOrderID.Text.Trim(),
                CustomerID       = selCustomer?.Value  ?? "",
                QuotationID      = selQuotation?.Value ?? "",
                DeliveryDate     = dtpDelivery.Value,
                ShippingAddress  = txtShippingAddr.Text.Trim(),
                BillingAddress   = txtBillingAddr.Text.Trim(),
                DiscountType     = dtype == "None" ? null : dtype,
                DiscountValue    = discountValue,
                DiscountAmount   = discountAmount,
                GrandTotal       = sub - discountAmount,
                OrderContactName = txtContactName.Text.Trim(),
                OrderStatus      = "Pending",
                IssuedTime       = DateTime.Now,
                SalesID          = SessionManager.CurrentUser?.StaffId ?? ""
            };

            bool ok = _ctrl.SaveNewOrder(header, new List<OrderLineEntity>(_lines));
            if (ok)
            {
                MessageBox.Show("Order created successfully.", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearForm();
            }
            else
            {
                MessageBox.Show("Failed to create order. Please check the details and try again.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Clear all entered data?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                ClearForm();
        }

        private void ClearForm()
        {
            txtOrderID.Text       = "";
            txtShippingAddr.Text  = "";
            txtBillingAddr.Text   = "";
            txtContactName.Text   = "";
            txtDiscountValue.Text = "0";
            cboCustomer.SelectedIndex     = 0;
            cboQuotation.SelectedIndex    = 0;
            cboProduct.SelectedIndex      = 0;
            cboDiscountType.SelectedIndex = 0;
            txtQty.Text       = "1";
            dtpDelivery.Value = DateTime.Today.AddDays(14);
            _lines.Clear();
            RefreshLineGrid();
        }

        // ── Nav / Logout ─────────────────────────────────────────────────────────
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }

        // ── ComboItem helper ─────────────────────────────────────────────────────
        private class ComboItem
        {
            public string Text  { get; }
            public string Value { get; }
            public ComboItem(string text, string value) { Text = text; Value = value; }
            public override string ToString() => Text;
        }
    }
}
