using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.OrderProcessing
{
    /// <summary>
    /// Modify Order From Quotation — inline-rendered dialog.
    ///
    /// MVC contract
    /// ─────────────────────────────────────────────────────────────────────
    /// • Receives a pre-loaded QuotationEntity (with Items) from QuotationForm.
    /// • Visually mirrors the ShowDetailDialog language from QuotationForm:
    ///     – pnlHeader      Top  80   — dark navy, Quotation ID + status badge
    ///     – pnlQuoteInfo   Top  220  — read-only 4-col: Quotation fields (pre-filled)
    ///     – pnlOrderTitle  Top  44   — blue title bar  "Modify Order Details"
    ///     – pnlOrderFields Top  300  — editable: Contact, Delivery Date,
    ///                                  Address selector, Shipping Addr, Billing Addr
    ///     – pnlLineLabel   Top  40   — "QUOTATION ITEMS" bar
    ///     – dgv            Fill      — Quotation items (read-only preview)
    ///     – pnlTotalRow    Bottom 50 — Total Amount
    ///     – pnlFooter      Bottom 80 — [✔ Save Changes] [Cancel]
    /// • On Confirm: calls _ctrl.SaveNewOrder(), exposes CreatedOrderID.
    /// • Size: 2500 × 1200, StartPosition CenterParent.
    /// </summary>
    public partial class AddFromQuotationForm : Form
    {
        private readonly OrderProcessingController _ctrl = new OrderProcessingController();
        private readonly QuotationEntity           _q;

        // Outputs
        public string CreatedOrderID { get; private set; }

        // Picker backing
        private string _selectedAddressId = "";
        private List<AddressLookup> _allAddresses = new List<AddressLookup>();

        // Controls that need cross-method access
        private TextBox    txtShippingAddr;
        private TextBox    txtBillingAddr;
        private TextBox    txtContactName;
        private DateTimePicker dtpDelivery;
        private ComboBox   cboAddress;
        private CheckBox   chkSameAddress;

        public AddFromQuotationForm(QuotationEntity q)
        {
            _q = q ?? throw new ArgumentNullException(nameof(q));
            InitializeComponent();
            BuildDialog();
        }

        // ──────────────────────────────────────────────────────────────────
        //  Full inline build
        // ──────────────────────────────────────────────────────────────────
        private void BuildDialog()
        {
            // ── Form
            this.Text            = $"Modify Order From Quotation  —  {_q.QuotationID}";
            this.Size            = new Size(2500, 1200);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.BackColor       = Color.White;
            this.Font            = new Font("Segoe UI", 13f);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;

            // ── VM for addresses (no DB call in View — uses controller)
            var vm = _ctrl.GetCreateOrderVM();
            _allAddresses = vm?.Addresses ?? new List<AddressLookup>();
            var filteredAddresses = _ctrl.GetAddressesByCustomer(_q.CustomerID, _allAddresses);

            // ── Header
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            var tblHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(24, 0, 24, 0)
            };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblHeader.Controls.Add(new Label
            {
                Text      = $"Modify Order From Quotation  —  {_q.QuotationID}",
                Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);

            var (scBg, scFg) = GetStatusColor(_q.QuotationStatus);
            tblHeader.Controls.Add(new Label
            {
                Text      = _q.QuotationStatus ?? "Unknown",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = scFg, BackColor = scBg,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false, Padding = new Padding(8, 4, 8, 4)
            }, 1, 0);
            pnlHeader.Controls.Add(tblHeader);

            // ── Quotation info (read-only, 4-col)
            var pnlQuoteInfo = new Panel
            {
                Dock = DockStyle.Top, Height = 200,
                Padding = new Padding(28, 14, 28, 8), BackColor = Color.White
            };
            pnlQuoteInfo.Paint += PaintBottomBorder;

            var tblQ = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 3,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblQ.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblQ.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblQ.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblQ.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            for (int r = 0; r < 3; r++)
                tblQ.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f));

            AddReadRow(tblQ, 0, "Quotation ID:",     _q.QuotationID,                      "Customer:",          _q.CustomerName);
            AddReadRow(tblQ, 1, "Total Amount:",     $"HK$ {_q.TotalAmount:N2}",          "Deposit Required:",  $"HK$ {_q.DepositRequired:N2}");
            AddReadRow(tblQ, 2, "Lead Time:",        _q.LeadTimeEstimated ?? "—",         "Expiry Date:",       _q.ExpiryDate.ToString("yyyy-MM-dd"));
            pnlQuoteInfo.Controls.Add(tblQ);

            // ── Modify Order title bar
            var pnlOrderTitle = new Panel
            {
                Dock = DockStyle.Top, Height = 44,
                BackColor = Color.FromArgb(239, 246, 255),
                Padding = new Padding(28, 0, 16, 0)
            };
            pnlOrderTitle.Paint += PaintBottomBorder;
            pnlOrderTitle.Controls.Add(new Label
            {
                Text      = "\uD83D\uDCCB  Modify Order Details  —  Update the required fields",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(29, 78, 216),
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            });

            // ── Editable Order fields panel
            //  Layout (2-col, each col = label + control):
            //  Row 0: Contact Name           | Delivery Date
            //  Row 1: Address (combobox)     | (same address checkbox)
            //  Row 2: Shipping Address       | Billing Address
            var pnlOrderFields = new Panel
            {
                Dock = DockStyle.Top, Height = 300,
                BackColor = Color.FromArgb(249, 252, 255),
                Padding = new Padding(28, 16, 28, 16)
            };
            pnlOrderFields.Paint += PaintBottomBorder;

            var tblF = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 3,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblF.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12f));  // label
            tblF.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38f));  // control
            tblF.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12f));  // label
            tblF.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38f));  // control
            tblF.RowStyles.Add(new RowStyle(SizeType.Percent, 33f));
            tblF.RowStyles.Add(new RowStyle(SizeType.Percent, 33f));
            tblF.RowStyles.Add(new RowStyle(SizeType.Percent, 34f));

            // Row 0: Contact Name / Delivery Date
            txtContactName = new TextBox
            {
                Font = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                PlaceholderText = "Enter contact name",
                Text = _q.CustomerName   // pre-fill from quotation customer
            };
            dtpDelivery = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Font   = new Font("Segoe UI", 12f),
                Dock   = DockStyle.Fill,
                Value  = DateTime.Today.AddDays(14)
            };
            tblF.Controls.Add(MakeFieldLabel("Contact Name *"), 0, 0);
            tblF.Controls.Add(txtContactName, 1, 0);
            tblF.Controls.Add(MakeFieldLabel("Delivery Date *"), 2, 0);
            tblF.Controls.Add(dtpDelivery, 3, 0);

            // Row 1: Address dropdown / Same-address checkbox
            cboAddress = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 12f),
                Dock          = DockStyle.Fill
            };
            cboAddress.Items.Add(new ComboItem("-- Select Saved Address --", ""));
            foreach (var a in filteredAddresses)
                cboAddress.Items.Add(new ComboItem(a.DisplayText, a.AddressId));
            cboAddress.SelectedIndex = 0;
            cboAddress.SelectedIndexChanged += CboAddress_SelectedIndexChanged;

            chkSameAddress = new CheckBox
            {
                Text      = "Billing address same as shipping",
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Checked   = true
            };
            chkSameAddress.CheckedChanged += ChkSameAddress_CheckedChanged;

            tblF.Controls.Add(MakeFieldLabel("Saved Address"), 0, 1);
            tblF.Controls.Add(cboAddress, 1, 1);
            tblF.Controls.Add(new Label { Dock = DockStyle.Fill, BackColor = Color.Transparent }, 2, 1);
            tblF.Controls.Add(chkSameAddress, 3, 1);

            // Row 2: Shipping Address / Billing Address
            txtShippingAddr = new TextBox
            {
                Font        = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle,
                Dock        = DockStyle.Fill,
                Multiline   = true,
                PlaceholderText = "Shipping address"
            };
            txtShippingAddr.TextChanged += TxtShippingAddr_TextChanged;

            txtBillingAddr = new TextBox
            {
                Font        = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle,
                Dock        = DockStyle.Fill,
                Multiline   = true,
                PlaceholderText = "Billing address",
                BackColor   = Color.FromArgb(235, 240, 250),
                Enabled     = false
            };

            tblF.Controls.Add(MakeFieldLabel("Shipping Addr *"), 0, 2);
            tblF.Controls.Add(txtShippingAddr, 1, 2);
            tblF.Controls.Add(MakeFieldLabel("Billing Addr *"), 2, 2);
            tblF.Controls.Add(txtBillingAddr, 3, 2);

            pnlOrderFields.Controls.Add(tblF);

            // ── QUOTATION ITEMS label bar
            var pnlLineLabel = new Panel
            {
                Dock = DockStyle.Top, Height = 40,
                BackColor = Color.FromArgb(246, 249, 255),
                Padding = new Padding(28, 0, 0, 0)
            };
            pnlLineLabel.Controls.Add(new Label
            {
                Text      = "QUOTATION ITEMS (will be carried into the order)",
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            });
            pnlLineLabel.Paint += PaintBottomBorder;

            // ── Items grid (read-only preview)
            var dgv = new DataGridView
            {
                ReadOnly              = true,
                AllowUserToAddRows    = false,
                RowHeadersVisible     = false,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor       = Color.White,
                BorderStyle           = BorderStyle.None,
                GridColor             = Color.FromArgb(221, 227, 236),
                Font                  = new Font("Segoe UI", 12f),
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate           = { Height = 44 },
                Dock                  = DockStyle.Fill,
                ColumnHeadersHeight   = 40,
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
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cProduct",  HeaderText = "PRODUCT",    FillWeight = 35 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cQty",       HeaderText = "QTY",        FillWeight = 10 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cUnit",      HeaderText = "UNIT",       FillWeight = 10 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cUnitPrice", HeaderText = "UNIT PRICE", FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cDiscount",  HeaderText = "DISCOUNT %", FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cSubtotal",  HeaderText = "SUBTOTAL",   FillWeight = 18 });

            foreach (var item in _q.Items ?? new List<QuotationItemEntity>())
                dgv.Rows.Add(
                    item.ProductName,
                    item.Quantity,
                    item.Unit,
                    $"HK$ {item.UnitPrice:N2}",
                    $"{item.DiscountPercent:N1}%",
                    $"HK$ {item.Subtotal:N2}");

            // ── Total row
            var pnlTotalRow = new Panel
            {
                Dock = DockStyle.Bottom, Height = 50,
                BackColor = Color.FromArgb(246, 249, 255),
                Padding = new Padding(0, 0, 28, 0)
            };
            pnlTotalRow.Controls.Add(new Label
            {
                Text      = $"Total Amount:   HK$ {_q.TotalAmount:N2}",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize  = false
            });

            // ── Footer  [✔ Save Changes]  [Cancel]
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom, Height = 80,
                BackColor = Color.White, Padding = new Padding(0, 10, 28, 10)
            };
            pnlFooter.Paint += PaintTopBorder;

            var btnSave = new Button
            {
                Text      = "\u2714  Save Changes",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(5, 150, 105),
                FlatStyle = FlatStyle.Flat,
                Dock      = DockStyle.Right,
                Width     = 210,
                Cursor    = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.FlatAppearance.MouseOverBackColor = Color.FromArgb(4, 120, 87);

            var btnCancel = new Button
            {
                Text      = "Cancel",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock      = DockStyle.Right,
                Width     = 140,
                Cursor    = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnCancel.FlatAppearance.BorderSize         = 1;
            btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);

            btnSave.Click += BtnSave_Click;
            btnCancel.Click += (o, ev) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            // Add Cancel first (Dock.Right stacks right-to-left)
            pnlFooter.Controls.Add(btnSave);
            pnlFooter.Controls.Add(btnCancel);

            // ── Assemble (Bottom → Fill → Top)
            this.Controls.Add(dgv);
            this.Controls.Add(pnlTotalRow);
            this.Controls.Add(pnlLineLabel);
            this.Controls.Add(pnlOrderFields);
            this.Controls.Add(pnlOrderTitle);
            this.Controls.Add(pnlQuoteInfo);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlFooter);
        }

        // ──────────────────────────────────────────────────────────────────
        //  Save Changes handler
        // ──────────────────────────────────────────────────────────────────
        private void BtnSave_Click(object sender, EventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(txtContactName.Text))
            { ShowWarning("Contact name is required."); return; }
            if (string.IsNullOrWhiteSpace(txtShippingAddr.Text))
            { ShowWarning("Shipping address is required."); return; }
            if (string.IsNullOrWhiteSpace(txtBillingAddr.Text))
            { ShowWarning("Billing address is required."); return; }
            if (_q.Items == null || _q.Items.Count == 0)
            { ShowWarning("The linked quotation has no items."); return; }

            try
            {
                var selAddr = cboAddress.SelectedItem as ComboItem;

                // Build OrderLineEntity list from QuotationItems
                var lines = (_q.Items ?? new List<QuotationItemEntity>())
                    .Select(qi => new OrderLineEntity
                    {
                        ItemID   = qi.ItemID,
                        ItemName = qi.ProductName,
                        Quantity = qi.Quantity,
                        Price    = qi.UnitPrice
                    }).ToList();

                double sub = lines.Sum(l => l.LineTotal);

                var header = new OrderEntity
                {
                    OrderID          = _ctrl.GenerateOrderId(),
                    CustomerID       = _q.CustomerID,
                    QuotationID      = _q.QuotationID,
                    AddressID        = selAddr?.Value ?? "",
                    SalesID          = SessionManager.CurrentUser?.StaffId ?? "",
                    IssuedTime       = DateTime.Now,
                    DeliveryDate     = dtpDelivery.Value,
                    ShippingAddress  = txtShippingAddr.Text.Trim(),
                    BillingAddress   = txtBillingAddr.Text.Trim(),
                    OrderContactName = txtContactName.Text.Trim(),
                    DiscountType     = null,
                    DiscountValue    = 0,
                    DiscountAmount   = 0,
                    SubTotal         = sub,
                    GrandTotal       = sub,
                    OrderStatus      = "Pending"
                };

                bool ok = _ctrl.SaveNewOrder(header, lines);
                if (ok)
                {
                    CreatedOrderID    = header.OrderID;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                    MessageBox.Show("Failed to save changes. Please verify the details.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ──────────────────────────────────────────────────────────────────
        //  Event handlers for editable fields
        // ──────────────────────────────────────────────────────────────────
        private void CboAddress_SelectedIndexChanged(object sender, EventArgs e)
        {
            var sel = cboAddress.SelectedItem as ComboItem;
            if (sel == null || string.IsNullOrEmpty(sel.Value))
            { txtShippingAddr.Text = string.Empty; return; }

            var addr = _allAddresses.Find(a => a.AddressId == sel.Value);
            if (addr != null)
            {
                txtShippingAddr.Text = addr.FullAddress;
                if (chkSameAddress.Checked) txtBillingAddr.Text = addr.FullAddress;
            }
        }

        private void ChkSameAddress_CheckedChanged(object sender, EventArgs e)
        {
            if (chkSameAddress.Checked)
            {
                txtBillingAddr.Text      = txtShippingAddr.Text;
                txtBillingAddr.Enabled   = false;
                txtBillingAddr.BackColor = Color.FromArgb(235, 240, 250);
            }
            else
            {
                txtBillingAddr.Enabled   = true;
                txtBillingAddr.BackColor = Color.FromArgb(245, 248, 255);
            }
        }

        private void TxtShippingAddr_TextChanged(object sender, EventArgs e)
        { if (chkSameAddress.Checked) txtBillingAddr.Text = txtShippingAddr.Text; }

        // ──────────────────────────────────────────────────────────────────
        //  Helpers
        // ──────────────────────────────────────────────────────────────────
        private static (Color bg, Color fg) GetStatusColor(string status)
        {
            return status switch
            {
                "Pending"   => (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)),
                "Converted" => (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)),
                "Rejected"  => (Color.FromArgb(254, 226, 226), Color.FromArgb(153,  27,  27)),
                _           => (Color.FromArgb(80, 80, 80),    Color.White)
            };
        }

        private static void AddReadRow(TableLayoutPanel tbl, int row,
            string keyL, string valL, string keyR, string valR)
        {
            tbl.Controls.Add(MakeLabelKey(keyL),     0, row);
            tbl.Controls.Add(MakeLabelVal(valL ?? "—"), 1, row);
            tbl.Controls.Add(MakeLabelKey(keyR),     2, row);
            tbl.Controls.Add(MakeLabelVal(valR ?? "—"), 3, row);
        }

        private static Label MakeLabelKey(string text) => new Label
        {
            Text      = text,
            Font      = new Font("Segoe UI", 11f),
            ForeColor = Color.FromArgb(98, 112, 135),
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize  = false
        };

        private static Label MakeLabelVal(string text) => new Label
        {
            Text         = text ?? "—",
            Font         = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor    = Color.FromArgb(15, 31, 53),
            Dock         = DockStyle.Fill,
            TextAlign    = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        private static Label MakeFieldLabel(string text) => new Label
        {
            Text      = text,
            Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(98, 112, 135),
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize  = false,
            Padding   = new Padding(0, 0, 8, 0)
        };

        private static void PaintBottomBorder(object sender, PaintEventArgs e)
        {
            var p = (Panel)sender;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
        }

        private static void PaintTopBorder(object sender, PaintEventArgs e)
        {
            var p = (Panel)sender;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, 0, p.Width, 0);
        }

        private static void ShowWarning(string msg)
            => MessageBox.Show(msg, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        // ── Designer stub (no Designer.cs needed — fully inline build)
        private void InitializeComponent() { this.SuspendLayout(); this.ResumeLayout(false); }

        private class ComboItem
        {
            public string Text  { get; }
            public string Value { get; }
            public ComboItem(string text, string value) { Text = text; Value = value; }
            public override string ToString() => Text;
        }
    }
}
