using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.OrderProcessing
{
    /// <summary>
    /// Create New Quotation — popup dialog (MVC View layer).
    ///
    /// Schema columns mapped (Database/schema.sql → Quotation table):
    ///   QuotationID         AUTO-GENERATED  (QUO-yyyyMMdd-####)
    ///   CustomerID          ComboBox        (FK → Customer)
    ///   ExpiryDate          DateTimePicker
    ///   TotalAmount         Calculated from item rows
    ///   DepositRequired     NumericUpDown
    ///   LeadTimeEstimated   TextBox
    ///   TermsandCondition   TextBox
    ///   QuotationStatus     Fixed = "Pending" on creation
    ///
    /// There is no QuotationItem table in schema.sql — items are for UI / future
    /// use; only the Quotation header is persisted via Controller.SaveNewQuotation().
    /// </summary>
    public class CreateQuotationDialog : Form
    {
        private readonly OrderProcessingController _ctrl;
        private readonly List<CustomerLookup>      _customers;
        private readonly List<ProductLookup>       _products;
        private readonly string                    _quotationId;
        private readonly string                    _salesStaffName;
        private readonly string                    _salesStaffId;

        // ── Item grid ──────────────────────────────────────────────────────────
        private DataGridView     _dgvItems;
        private Label            _lblGrandTotal;
        private List<QuotationItemEntity> _itemRows = new List<QuotationItemEntity>();

        // ── Header fields ──────────────────────────────────────────────────────
        private ComboBox       _cboCustomer;
        private DateTimePicker _dtpExpiry;
        private NumericUpDown  _nudDeposit;
        private TextBox        _txtLeadTime;
        private TextBox        _txtTnC;

        public CreateQuotationDialog(OrderProcessingController ctrl)
        {
            _ctrl = ctrl;
            var vm = _ctrl.GetCreateQuotationVM();
            _customers      = vm.Customers   ?? new List<CustomerLookup>();
            _products       = vm.Products    ?? new List<ProductLookup>();
            _quotationId    = vm.NextQuotationId;
            _salesStaffName = vm.SalesStaffName;
            _salesStaffId   = vm.SalesStaffId;
            BuildUI();
        }

        // ── UI construction ────────────────────────────────────────────────────
        private void BuildUI()
        {
            Text            = "Create New Quotation";
            Size            = new Size(1800, 960);
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = Color.FromArgb(245, 247, 250);
            Font            = new Font("Segoe UI", 12f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;

            // ── Header bar
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            var tblH = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(24, 0, 24, 0)
            };
            tblH.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblH.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280f));
            tblH.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblH.Controls.Add(new Label
            {
                Text      = $"Create New Quotation  —  {_quotationId}",
                Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);
            tblH.Controls.Add(new Label
            {
                Text         = "Pending",
                Font         = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor    = Color.FromArgb(146, 64, 14),
                BackColor    = Color.FromArgb(254, 243, 199),
                Dock         = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                AutoSize     = false, AutoEllipsis = false,
                Padding      = new Padding(8, 4, 8, 4)
            }, 1, 0);
            pnlHeader.Controls.Add(tblH);

            // ── Info section (Card)
            var pnlInfoCard = BuildCard(240);
            pnlInfoCard.Padding = new Padding(24, 16, 24, 16);
            var tblInfo = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 4,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38f));
            for (int r = 0; r < 4; r++) tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));

            // Row 0: Quotation ID (RO) | Sales Staff (RO)
            tblInfo.Controls.Add(FieldLabel("Quotation ID"), 0, 0);
            tblInfo.Controls.Add(ReadOnlyField(_quotationId), 1, 0);
            tblInfo.Controls.Add(FieldLabel("Sales Staff"), 2, 0);
            tblInfo.Controls.Add(ReadOnlyField(_salesStaffName), 3, 0);

            // Row 1: Customer | Expiry Date
            tblInfo.Controls.Add(FieldLabel("Customer *"), 0, 1);
            _cboCustomer = new ComboBox
            {
                Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 12f), FlatStyle = FlatStyle.Flat
            };
            foreach (var c in _customers) _cboCustomer.Items.Add(c);
            _cboCustomer.DisplayMember = "CustomerName";
            tblInfo.Controls.Add(_cboCustomer, 1, 1);
            tblInfo.Controls.Add(FieldLabel("Expiry Date *"), 2, 1);
            _dtpExpiry = new DateTimePicker
            {
                Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 12f),
                MinDate = DateTime.Today.AddDays(1),
                Value   = DateTime.Today.AddDays(30)
            };
            tblInfo.Controls.Add(_dtpExpiry, 3, 1);

            // Row 2: Deposit Required | Lead Time
            tblInfo.Controls.Add(FieldLabel("Deposit Required"), 0, 2);
            _nudDeposit = new NumericUpDown
            {
                Dock = DockStyle.Fill, Minimum = 0, Maximum = 9999999,
                DecimalPlaces = 2, Font = new Font("Segoe UI", 12f), ThousandsSeparator = true
            };
            tblInfo.Controls.Add(_nudDeposit, 1, 2);
            tblInfo.Controls.Add(FieldLabel("Lead Time"), 2, 2);
            _txtLeadTime = new TextBox
            {
                Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12f),
                PlaceholderText = "e.g. 4–6 weeks"
            };
            tblInfo.Controls.Add(_txtLeadTime, 3, 2);

            // Row 3: Terms & Conditions (span 3 cols)
            tblInfo.Controls.Add(FieldLabel("Terms & Conditions"), 0, 3);
            _txtTnC = new TextBox
            {
                Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12f),
                PlaceholderText = "Optional terms..."
            };
            tblInfo.SetColumnSpan(_txtTnC, 3);
            tblInfo.Controls.Add(_txtTnC, 1, 3);

            pnlInfoCard.Controls.Add(tblInfo);

            // ── Section label: Items
            var pnlItemsLabel = new Panel
            {
                Dock = DockStyle.Top, Height = 44,
                BackColor = Color.FromArgb(246, 249, 255), Padding = new Padding(24, 0, 0, 0)
            };
            pnlItemsLabel.Controls.Add(new Label
            {
                Text = "QUOTATION ITEMS",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            });

            // ── Items toolbar
            var pnlItemsToolbar = new Panel
            {
                Dock = DockStyle.Top, Height = 50,
                BackColor = Color.White, Padding = new Padding(24, 8, 24, 4)
            };
            var btnAddItem = new Button
            {
                Text = "+ Add Item", Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(47, 111, 237),
                FlatStyle = FlatStyle.Flat, Height = 36, Width = 130,
                Cursor = Cursors.Hand, Dock = DockStyle.Left
            };
            btnAddItem.FlatAppearance.BorderSize = 0;
            btnAddItem.Click += BtnAddItem_Click;
            var btnRemoveItem = new Button
            {
                Text = "Remove Selected", Font = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(153, 27, 27), BackColor = Color.FromArgb(254, 226, 226),
                FlatStyle = FlatStyle.Flat, Height = 36, Width = 160,
                Cursor = Cursors.Hand, Dock = DockStyle.Left
            };
            btnRemoveItem.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
            btnRemoveItem.Click += BtnRemoveItem_Click;
            pnlItemsToolbar.Controls.Add(btnRemoveItem);
            pnlItemsToolbar.Controls.Add(btnAddItem);

            // ── Items DataGridView
            _dgvItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible  = false,
                SelectionMode      = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor    = Color.White, BorderStyle = BorderStyle.None,
                GridColor          = Color.FromArgb(221, 227, 236),
                Font               = new Font("Segoe UI", 12f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle    = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate        = { Height = 44 },
                ColumnHeadersHeight = 40,
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255),
                    ForeColor = Color.FromArgb(98, 112, 135),
                    Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                    Padding   = new Padding(12, 0, 0, 0)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor          = Color.White,
                    ForeColor          = Color.FromArgb(15, 31, 53),
                    SelectionBackColor = Color.FromArgb(219, 234, 254),
                    SelectionForeColor = Color.FromArgb(15, 31, 53),
                    Padding            = new Padding(12, 6, 12, 6)
                }
            };

            // Product combobox column
            var colProduct = new DataGridViewComboBoxColumn
            {
                Name = "colProduct", HeaderText = "PRODUCT", FillWeight = 30,
                DisplayMember = "ItemName", ValueMember = "ItemId", FlatStyle = FlatStyle.Flat
            };
            foreach (var p in _products) colProduct.Items.Add(p);
            _dgvItems.Columns.Add(colProduct);
            _dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQty",      HeaderText = "QTY",        FillWeight = 8  });
            _dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUnit",     HeaderText = "UNIT",       FillWeight = 10 });
            _dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPrice",    HeaderText = "UNIT PRICE", FillWeight = 15 });
            _dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDiscount", HeaderText = "DISC %",     FillWeight = 10 });
            _dgvItems.Columns.Add(new DataGridViewReadOnlyTextBoxColumn { Name = "colSubtotal", HeaderText = "SUBTOTAL", FillWeight = 15 });
            _dgvItems.CellValueChanged    += DgvItems_CellValueChanged;
            _dgvItems.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (_dgvItems.IsCurrentCellDirty) _dgvItems.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            // ── Grand Total row
            var pnlTotalRow = new Panel
            {
                Dock = DockStyle.Bottom, Height = 50,
                BackColor = Color.FromArgb(246, 249, 255), Padding = new Padding(24, 0, 24, 0)
            };
            _lblGrandTotal = new Label
            {
                Text      = "Total Amount:   HK$ 0.00",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, AutoSize = false
            };
            pnlTotalRow.Controls.Add(_lblGrandTotal);

            // ── Footer
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom, Height = 80,
                BackColor = Color.White, Padding = new Padding(0, 10, 28, 10)
            };
            var btnSave = new Button
            {
                Text = "Save", Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(47, 111, 237),
                FlatStyle = FlatStyle.Flat, Width = 210, Height = 60,
                Cursor = Cursors.Hand, Dock = DockStyle.Right
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            var btnCancel = new Button
            {
                Text = "Cancel", Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat, Width = 210, Height = 60,
                Cursor = Cursors.Hand, Dock = DockStyle.Right
            };
            btnCancel.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            pnlFooter.Controls.Add(btnSave);
            pnlFooter.Controls.Add(btnCancel);

            // ── Items outer card (fills remaining space)
            var pnlItemsCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(24, 0, 24, 0),
                Padding = new Padding(0)
            };
            pnlItemsCard.Controls.Add(_dgvItems);
            pnlItemsCard.Controls.Add(pnlTotalRow);
            pnlItemsCard.Controls.Add(pnlItemsToolbar);
            pnlItemsCard.Controls.Add(pnlItemsLabel);

            // ── Outer card wrapper with margin
            var pnlOuter = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 10, 20, 4), BackColor = Color.FromArgb(245, 247, 250) };
            pnlOuter.Controls.Add(pnlItemsCard);

            // ── Assemble (reverse DockStyle.Top order)
            Controls.Add(pnlOuter);       // Fill — items
            Controls.Add(pnlInfoCard);    // Top — info
            Controls.Add(pnlHeader);      // Top — header bar
            Controls.Add(pnlFooter);      // Bottom — footer
        }

        // ── Item management ────────────────────────────────────────────────────
        private void BtnAddItem_Click(object sender, EventArgs e)
        {
            _dgvItems.Rows.Add(null, 1, "pc", 0.00m, 0.0m, "HK$ 0.00");
        }

        private void BtnRemoveItem_Click(object sender, EventArgs e)
        {
            if (_dgvItems.SelectedRows.Count == 0) return;
            foreach (DataGridViewRow row in _dgvItems.SelectedRows)
                if (!row.IsNewRow) _dgvItems.Rows.Remove(row);
            RecalcTotal();
        }

        private void DgvItems_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = _dgvItems.Rows[e.RowIndex];

            decimal qty      = TryDecimal(row.Cells["colQty"].Value);
            decimal price    = TryDecimal(row.Cells["colPrice"].Value);
            decimal discPct  = TryDecimal(row.Cells["colDiscount"].Value);
            decimal subtotal = qty * price * (1 - discPct / 100m);
            row.Cells["colSubtotal"].Value = $"HK$ {subtotal:N2}";

            RecalcTotal();
        }

        private void RecalcTotal()
        {
            decimal total = 0;
            foreach (DataGridViewRow row in _dgvItems.Rows)
            {
                if (row.IsNewRow) continue;
                decimal qty     = TryDecimal(row.Cells["colQty"].Value);
                decimal price   = TryDecimal(row.Cells["colPrice"].Value);
                decimal disc    = TryDecimal(row.Cells["colDiscount"].Value);
                total          += qty * price * (1 - disc / 100m);
            }
            _lblGrandTotal.Text = $"Total Amount:   HK$ {total:N2}";
        }

        private static decimal TryDecimal(object v)
        {
            if (v == null) return 0;
            if (decimal.TryParse(v.ToString(), out decimal d)) return d;
            return 0;
        }

        // ── Save ───────────────────────────────────────────────────────────────
        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_cboCustomer.SelectedItem == null)
            {
                MessageBox.Show("Please select a Customer.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var   customer = (CustomerLookup)_cboCustomer.SelectedItem;
            double total   = 0;
            foreach (DataGridViewRow row in _dgvItems.Rows)
            {
                if (row.IsNewRow) continue;
                double qty   = (double)TryDecimal(row.Cells["colQty"].Value);
                double price = (double)TryDecimal(row.Cells["colPrice"].Value);
                double disc  = (double)TryDecimal(row.Cells["colDiscount"].Value);
                total       += qty * price * (1 - disc / 100.0);
            }

            var quotation = new QuotationEntity
            {
                QuotationID       = _quotationId,
                CustomerID        = customer.CustomerID,
                CustomerName      = customer.CustomerName,
                ExpiryDate        = _dtpExpiry.Value.Date,
                TotalAmount       = total,
                DepositRequired   = (double)_nudDeposit.Value,
                LeadTimeEstimated = _txtLeadTime.Text.Trim(),
                TermsandCondition = _txtTnC.Text.Trim(),
                QuotationStatus   = "Pending"
            };

            bool ok = _ctrl.SaveNewQuotation(quotation, null, _salesStaffId);
            if (ok)
            {
                MessageBox.Show(
                    $"Quotation {_quotationId} has been created successfully.",
                    "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show(
                    "Failed to save the quotation. Please try again.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private static Panel BuildCard(int height)
        {
            return new Panel
            {
                Dock      = DockStyle.Top, Height = height,
                BackColor = Color.White,
                Margin    = new Padding(20, 8, 20, 0)
            };
        }

        private static Label FieldLabel(string text) => new Label
        {
            Text      = text, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(98, 112, 135),
            Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(0, 0, 8, 0), AutoEllipsis = false
        };

        private static Control ReadOnlyField(string text)
        {
            return new Label
            {
                Text      = text, Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.FromArgb(245, 247, 250)
            };
        }
    }

    /// <summary>Helper column type: read-only TextBox column for Subtotal.</summary>
    internal class DataGridViewReadOnlyTextBoxColumn : DataGridViewTextBoxColumn
    {
        public DataGridViewReadOnlyTextBoxColumn() { ReadOnly = true; }
    }
}
