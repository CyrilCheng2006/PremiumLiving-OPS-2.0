using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.OrderProcessing
{
    /// <summary>
    /// Dialog for creating a brand-new Quotation.
    /// Returns DialogResult.OK when the Quotation is saved successfully.
    /// </summary>
    public partial class CreateNewQuotationForm : Form
    {
        private readonly OrderProcessingController _ctrl = new OrderProcessingController();
        private CreateQuotationViewModel           _vm;
        private readonly List<QuotationItemEntity> _lines = new List<QuotationItemEntity>();

        public CreateNewQuotationForm()
        {
            InitializeComponent();
            Load += CreateNewQuotationForm_Load;
        }

        // ── Lifecycle ────────────────────────────────────────────────────────────

        private void CreateNewQuotationForm_Load(object sender, EventArgs e)
        {
            _vm = _ctrl.GetCreateQuotationVM();

            // Header fields
            txtQuotationId.Text   = _vm.NextQuotationId;
            txtSalesStaff.Text    = _vm.SalesStaffName;
            dtpIssuedDate.Value   = DateTime.Today;
            dtpExpiryDate.Value   = DateTime.Today.AddDays(30);
            txtDeposit.Text       = "0";
            txtLeadTime.Text      = "";
            txtTerms.Text         = "";
            txtNotes.Text         = "";

            // Customer combo
            cboCustomer.DisplayMember = "CustomerName";
            cboCustomer.ValueMember   = "CustomerID";
            cboCustomer.DataSource    = _vm.Customers;
            cboCustomer.SelectedIndex = -1;

            // Status combo
            cboStatus.Items.AddRange(new object[] { "Pending", "Converted", "Rejected" });
            cboStatus.SelectedIndex = 0;

            // Product combo in item section
            cboProduct.DisplayMember = "DisplayText";
            cboProduct.ValueMember   = "ItemID";
            cboProduct.DataSource    = _vm.Products;
            cboProduct.SelectedIndex = -1;

            RefreshLineGrid();
        }

        // ── Line Item Grid ───────────────────────────────────────────────────────

        private void RefreshLineGrid()
        {
            dgvLines.Rows.Clear();
            double total = 0;
            foreach (var li in _lines)
            {
                double sub = li.Quantity * li.UnitPrice * (1 - li.DiscountPercent / 100.0);
                total += sub;
                dgvLines.Rows.Add(
                    li.ItemID,
                    li.ProductName,
                    li.Quantity,
                    li.Unit,
                    li.UnitPrice.ToString("N2"),
                    li.DiscountPercent.ToString("N1"),
                    sub.ToString("N2"),
                    li.ItemNote);
            }
            lblTotal.Text = string.Format("Total:  HK$ {0:N2}", total);
        }

        // ── Button Handlers ──────────────────────────────────────────────────────

        private void btnAddLine_Click(object sender, EventArgs e)
        {
            if (cboProduct.SelectedItem == null)
            {
                MessageBox.Show("Please select a product.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!int.TryParse(txtQty.Text.Trim(), out int qty) || qty <= 0)
            {
                MessageBox.Show("Quantity must be a positive integer.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!double.TryParse(txtUnitPrice.Text.Trim(), out double price) || price < 0)
            {
                MessageBox.Show("Unit price must be a non-negative number.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            double disc = 0;
            if (!string.IsNullOrWhiteSpace(txtDiscount.Text))
                double.TryParse(txtDiscount.Text.Trim(), out disc);

            var prod = (ProductLookup)cboProduct.SelectedItem;
            _lines.Add(new QuotationItemEntity
            {
                ItemID          = prod.ItemID,
                ProductName     = prod.ItemName,
                Quantity        = qty,
                Unit            = txtUnit.Text.Trim(),
                UnitPrice       = price,
                DiscountPercent = disc,
                ItemNote        = txtItemNote.Text.Trim()
            });

            // Clear line entry
            cboProduct.SelectedIndex = -1;
            txtQty.Clear();
            txtUnitPrice.Clear();
            txtDiscount.Clear();
            txtUnit.Clear();
            txtItemNote.Clear();

            RefreshLineGrid();
        }

        private void btnRemoveLine_Click(object sender, EventArgs e)
        {
            if (dgvLines.SelectedRows.Count == 0) return;
            _lines.RemoveAt(dgvLines.SelectedRows[0].Index);
            RefreshLineGrid();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            // Validate header
            if (cboCustomer.SelectedItem == null)
            {
                MessageBox.Show("Please select a customer.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_lines.Count == 0)
            {
                MessageBox.Show("Please add at least one line item.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            double deposit  = 0;
            double.TryParse(txtDeposit.Text.Trim(), out deposit);

            double total = 0;
            foreach (var li in _lines)
                total += li.Quantity * li.UnitPrice * (1 - li.DiscountPercent / 100.0);

            var q = new QuotationEntity
            {
                QuotationID       = txtQuotationId.Text.Trim(),
                CustomerID        = ((CustomerEntity)cboCustomer.SelectedItem).CustomerID,
                CustomerName      = ((CustomerEntity)cboCustomer.SelectedItem).CustomerName,
                IssuedDate        = dtpIssuedDate.Value.Date,
                ExpiryDate        = dtpExpiryDate.Value.Date,
                TotalAmount       = total,
                DepositRequired   = deposit,
                LeadTimeEstimated = txtLeadTime.Text.Trim(),
                TermsandCondition = txtTerms.Text.Trim(),
                QuotationStatus   = cboStatus.SelectedItem?.ToString() ?? "Pending",
                SalesStaffName    = _vm.SalesStaffName,
                Notes             = txtNotes.Text.Trim()
            };

            bool ok = _ctrl.SaveNewQuotation(q, _lines, _vm.SalesStaffId);
            if (ok)
            {
                MessageBox.Show(
                    string.Format("Quotation {0} created successfully.", q.QuotationID),
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show("Failed to save quotation. Please try again.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        // ── Product combo auto-fill unit price ───────────────────────────────────

        private void cboProduct_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboProduct.SelectedItem is ProductLookup p)
                txtUnitPrice.Text = p.SalesPrice.ToString("N2");
        }
    }
}
