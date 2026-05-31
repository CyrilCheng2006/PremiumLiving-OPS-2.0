using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.OrderProcessing
{
    partial class CreateOrderForm
    {
        private System.ComponentModel.IContainer components = null;

        // AppShell
        private AppShell _shell;

        // ── Order Information — read-only / auto fields ──────────────────────
        private Label        lblOrderIdValue;

        // ── Order Information — editable fields ─────────────────────────────
        private ComboBox      cboCustomer;
        private TextBox       txtContactName;
        private DateTimePicker dtpDelivery;
        private ComboBox      cboQuotation;
        private ComboBox      cboShippingAddr;   // ComboBox from saved Address records
        private ComboBox      cboBillingAddr;    // ComboBox from saved Address records
        private CheckBox      chkSameAddress;
        private ComboBox      cboDiscountType;
        private TextBox       txtDiscountValue;
        private Label         lblDiscountUnit;   // dynamic '%' or 'HK$' hint

        // ── Order Items controls ─────────────────────────────────────────────
        private ComboBox     cboProduct;
        private TextBox      txtQty;
        private Button       btnAddLine;
        private Button       btnRemoveLine;
        private DataGridView dgvLines;

        // ── Totals / actions ─────────────────────────────────────────────────
        private Label  lblSubtotal;
        private Label  lblGrandTotal;
        private Button btnSubmit;
        private Button btnClear;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text          = "Premium Living OPS — Create Order";
            this.Size          = new Size(1440, 900);
            this.MinimumSize   = new Size(1200, 760);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = Palette.BgPage;
            this.WindowState   = FormWindowState.Maximized;
            this.Font          = new Font("Segoe UI", 13f);

            var pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage };
            _shell = new AppShell();
            _shell.SetPopupContainer(pnlMain);

            // ==================================================================
            // CARD 1 — ORDER INFORMATION
            // Layout: 2-column × (label row 28px + input row 52px) × 5 field pairs
            //
            //   Left column              Right column
            //   ─────────────────────   ─────────────────────
            //   Order ID (label)        Customer (label)
            //   [auto-generated chip]   [ComboBox            ]
            //   Shipping Address        Billing Address
            //   [ComboBox            ]  [ComboBox  ] ☐ Same…
            //   Delivery Date           Linked Quotation
            //   [DateTimePicker      ]  [ComboBox            ]
            //   Contact Name            Discount Type
            //   [TextBox             ]  [ComboBox            ]
            //   — (spacer)              Discount Value
            //                           [TextBox  ]  [unit   ]
            // ==================================================================

            // ── Order ID chip (read-only) ────────────────────────────────────
            lblOrderIdValue = new Label
            {
                Text      = "",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Palette.Primary,
                BackColor = Color.FromArgb(219, 234, 254),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(10, 0, 0, 0)
            };
            var pnlOrderIdChip = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(0, 6, 12, 6)
            };
            lblOrderIdValue.Dock = DockStyle.Fill;
            pnlOrderIdChip.Controls.Add(lblOrderIdValue);

            // ── Customer ComboBox ────────────────────────────────────────────
            cboCustomer = MakeCombo();
            cboCustomer.SelectedIndexChanged += cboCustomer_SelectedIndexChanged;

            // ── Address ComboBoxes ───────────────────────────────────────────
            cboShippingAddr = MakeCombo();
            cboShippingAddr.SelectedIndexChanged += cboShippingAddr_SelectedIndexChanged;

            cboBillingAddr           = MakeCombo();
            cboBillingAddr.Enabled   = false;   // disabled until chkSameAddress is unchecked

            chkSameAddress = new CheckBox
            {
                Text      = "Same as Shipping",
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(98, 112, 135),
                Checked   = true,
                AutoSize  = false,
                Dock      = DockStyle.Right,
                Width     = 180
            };
            chkSameAddress.CheckedChanged += chkSameAddress_CheckedChanged;

            // Billing row: ComboBox + checkbox side-by-side
            var pnlBilling = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 6, 12, 6) };
            cboBillingAddr.Dock = DockStyle.Fill;
            pnlBilling.Controls.Add(cboBillingAddr);
            pnlBilling.Controls.Add(chkSameAddress);  // Right-docked

            // ── Delivery Date ────────────────────────────────────────────────
            dtpDelivery = new DateTimePicker
            {
                Font   = new Font("Segoe UI", 12f),
                Format = DateTimePickerFormat.Short,
                Dock   = DockStyle.Fill
            };

            // ── Linked Quotation ─────────────────────────────────────────────
            cboQuotation = MakeCombo();

            // ── Contact Name ─────────────────────────────────────────────────
            txtContactName = MakeTextBox();

            // ── Discount Type ─────────────────────────────────────────────────
            cboDiscountType = MakeCombo();
            cboDiscountType.Items.AddRange(new object[] { "None", "Amount", "Rate" });
            cboDiscountType.SelectedIndex        = 0;
            cboDiscountType.SelectedIndexChanged += cboDiscountType_SelectedIndexChanged;

            // ── Discount Value + unit label ───────────────────────────────────
            txtDiscountValue         = MakeTextBox();
            txtDiscountValue.Text    = "0";
            txtDiscountValue.Enabled = false;
            txtDiscountValue.Dock    = DockStyle.Fill;
            txtDiscountValue.TextChanged += txtDiscountValue_TextChanged;

            lblDiscountUnit = new Label
            {
                Text      = "",
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Right,
                AutoSize  = false,
                Width     = 44,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var pnlDiscountInput = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 6, 12, 6) };
            pnlDiscountInput.Controls.Add(txtDiscountValue);
            pnlDiscountInput.Controls.Add(lblDiscountUnit);  // Right-docked

            // ==================================================================
            //  TableLayoutPanel — 2 columns × 10 rows
            //  Odd rows  (0,2,4,6,8) = label rows   (height 28)
            //  Even rows (1,3,5,7,9) = input rows   (height 52)
            //
            //  Col 0 (Left 50%)   Col 1 (Right 50%)
            //  row 0  Order ID *        Customer *
            //  row 1  [chip]            [cboCustomer]
            //  row 2  Shipping Address  Billing Address
            //  row 3  [cboShipping]     [cboBilling + checkbox]
            //  row 4  Delivery Date *   Linked Quotation
            //  row 5  [dtpDelivery]     [cboQuotation]
            //  row 6  Contact Name *    Discount Type
            //  row 7  [txtContact]      [cboDiscountType]
            //  row 8  —                 Discount Value
            //  row 9  —                 [txtDiscountValue + unit]
            // ==================================================================
            var tblInfo = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 2,
                RowCount        = 10,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(18, 8, 18, 8)
            };
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            // Label rows (28 px each)
            foreach (int r in new[] { 0, 2, 4, 6, 8 })
                tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
            // Input rows (52 px each)
            foreach (int r in new[] { 1, 3, 5, 7, 9 })
                tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));

            // Row 0 — labels
            tblInfo.Controls.Add(FieldLabel("Order ID",    required: false), 0, 0);
            tblInfo.Controls.Add(FieldLabel("Customer",    required: true),  1, 0);
            // Row 1 — inputs
            tblInfo.Controls.Add(pnlOrderIdChip,                              0, 1);
            tblInfo.Controls.Add(Pad(cboCustomer),                            1, 1);

            // Row 2 — labels
            tblInfo.Controls.Add(FieldLabel("Shipping Address", required: true),  0, 2);
            tblInfo.Controls.Add(FieldLabel("Billing Address",  required: true),  1, 2);
            // Row 3 — inputs
            tblInfo.Controls.Add(Pad(cboShippingAddr),                             0, 3);
            tblInfo.Controls.Add(pnlBilling,                                       1, 3);

            // Row 4 — labels
            tblInfo.Controls.Add(FieldLabel("Delivery Date",      required: true),  0, 4);
            tblInfo.Controls.Add(FieldLabel("Linked Quotation",   required: false), 1, 4);
            // Row 5 — inputs
            tblInfo.Controls.Add(Pad(dtpDelivery),                                  0, 5);
            tblInfo.Controls.Add(Pad(cboQuotation),                                 1, 5);

            // Row 6 — labels
            tblInfo.Controls.Add(FieldLabel("Order Contact Name", required: true),  0, 6);
            tblInfo.Controls.Add(FieldLabel("Discount Type",      required: false), 1, 6);
            // Row 7 — inputs
            tblInfo.Controls.Add(Pad(txtContactName),                               0, 7);
            tblInfo.Controls.Add(Pad(cboDiscountType),                              1, 7);

            // Row 8 — labels
            tblInfo.Controls.Add(FieldLabel("", required: false),                   0, 8);  // spacer
            tblInfo.Controls.Add(FieldLabel("Discount Value", required: false),     1, 8);
            // Row 9 — inputs
            // col 0 intentionally empty
            tblInfo.Controls.Add(pnlDiscountInput,                                  1, 9);

            // Card title bar
            var pnlInfoTitle = CardTitlePanel("Order Information");

            var pnlInfoContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlInfoContent.Controls.Add(tblInfo);       // Fill
            pnlInfoContent.Controls.Add(pnlInfoTitle);  // Top

            // outerHeight: 10 rows × (28+52)/2 avg = 400 rows + title 54 + outer padding 40 ≈ 520
            var (pnlInfoOuter, pnlInfoInner) = CardPanel.Create(outerHeight: 520);
            pnlInfoInner.Controls.Add(pnlInfoContent);

            // ==================================================================
            // CARD 2 — ORDER ITEMS  (CardPanel.CreateFill)
            // ==================================================================

            cboProduct  = MakeCombo();
            txtQty      = MakeTextBox();
            txtQty.Text = "1";

            btnAddLine    = MakePrimaryBtn("+ Add Item", Point.Empty, 180, 52);
            btnRemoveLine = MakeOutlineBtn("− Remove",   Point.Empty, 150, 52);
            btnAddLine.Dock    = DockStyle.Fill;
            btnRemoveLine.Dock = DockStyle.Fill;
            btnAddLine.Click    += btnAddLine_Click;
            btnRemoveLine.Click += btnRemoveLine_Click;

            var tblToolbar = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 5,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,  70f));
            tblToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,  90f));
            tblToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 196f));
            tblToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 158f));
            tblToolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            cboProduct.Dock = DockStyle.Fill;
            var lblQty = new Label
            {
                Text      = "Qty:",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Padding   = new Padding(0, 0, 6, 0)
            };
            txtQty.Dock = DockStyle.Fill;

            tblToolbar.Controls.Add(cboProduct,    0, 0);
            tblToolbar.Controls.Add(lblQty,        1, 0);
            tblToolbar.Controls.Add(txtQty,        2, 0);
            tblToolbar.Controls.Add(btnAddLine,    3, 0);
            tblToolbar.Controls.Add(btnRemoveLine, 4, 0);

            var pnlToolbar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 68,
                BackColor = Color.Transparent,
                Padding   = new Padding(18, 8, 18, 0)
            };
            pnlToolbar.Controls.Add(tblToolbar);

            dgvLines = new DataGridView
            {
                ReadOnly                  = true,
                AllowUserToAddRows        = false,
                AllowUserToDeleteRows     = false,
                RowHeadersVisible         = false,
                SelectionMode             = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect               = false,
                BackgroundColor           = Color.White,
                BorderStyle               = BorderStyle.None,
                GridColor                 = Palette.BorderColor,
                Font                      = new Font("Segoe UI", 13f),
                AutoSizeColumnsMode       = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle           = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate               = { Height = 48 },
                Dock                      = DockStyle.Fill,
                ColumnHeadersHeight       = 46,
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255),
                    ForeColor = Color.FromArgb(98, 112, 135),
                    Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                    Padding   = new Padding(12, 0, 0, 0),
                    Alignment = DataGridViewContentAlignment.MiddleLeft
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor          = Color.White,
                    ForeColor          = Palette.TextMain,
                    SelectionBackColor = Color.FromArgb(219, 234, 254),
                    SelectionForeColor = Palette.TextMain,
                    Padding            = new Padding(12, 6, 12, 6)
                }
            };
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLineItemID", HeaderText = "ITEM ID",    FillWeight = 14 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLineName",   HeaderText = "ITEM NAME",  FillWeight = 44 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLineQty",    HeaderText = "QTY",        FillWeight = 10 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLinePrice",  HeaderText = "UNIT PRICE", FillWeight = 16 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLineTotal",  HeaderText = "LINE TOTAL", FillWeight = 16 });

            var pnlFooter = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 72,
                BackColor = Color.FromArgb(246, 249, 255),
                Padding   = new Padding(18, 0, 18, 0)
            };
            pnlFooter.Paint += (s, e) =>
            {
                using var pen = new Pen(Palette.BorderColor, 1);
                e.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0);
            };

            lblSubtotal = new Label
            {
                Text      = "Subtotal:  HK$ 0.00",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Left,
                AutoSize  = false,
                Width     = 340,
                TextAlign = ContentAlignment.MiddleLeft
            };
            lblGrandTotal = new Label
            {
                Text      = "Grand Total:  HK$ 0.00",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Palette.TextMain,
                Dock      = DockStyle.Left,
                AutoSize  = false,
                Width     = 380,
                TextAlign = ContentAlignment.MiddleLeft
            };

            btnSubmit = MakePrimaryBtn("\u2713  Submit Order", Point.Empty, 220, 52);
            btnClear  = MakeOutlineBtn("\u21BA  Clear",        Point.Empty, 150, 52);
            btnSubmit.Dock = DockStyle.Right;
            btnClear.Dock  = DockStyle.Right;
            btnSubmit.Click += btnSubmit_Click;
            btnClear.Click  += btnClear_Click;

            pnlFooter.Controls.Add(btnSubmit);
            pnlFooter.Controls.Add(btnClear);
            pnlFooter.Controls.Add(lblGrandTotal);
            pnlFooter.Controls.Add(lblSubtotal);

            var pnlItemsTitle   = CardTitlePanel("Order Items");
            var pnlDivider      = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Palette.BorderColor };

            var pnlItemsContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlItemsContent.Controls.Add(dgvLines);       // Fill
            pnlItemsContent.Controls.Add(pnlDivider);    // Top
            pnlItemsContent.Controls.Add(pnlToolbar);    // Top
            pnlItemsContent.Controls.Add(pnlItemsTitle); // Top
            pnlItemsContent.Controls.Add(pnlFooter);     // Bottom

            var (pnlItemsOuter, pnlItemsInner) = CardPanel.CreateFill();
            pnlItemsInner.Controls.Add(pnlItemsContent);

            // ==================================================================
            // Assemble page
            // ==================================================================
            pnlMain.Controls.Add(pnlItemsOuter);  // Fill (added first = bottom layer)
            pnlMain.Controls.Add(pnlInfoOuter);   // Top
            pnlMain.Controls.Add(_shell);         // Top

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        // ── Factory helpers ──────────────────────────────────────────────────

        private static TextBox MakeTextBox() => new TextBox
        {
            Font        = new Font("Segoe UI", 12f),
            BorderStyle = BorderStyle.FixedSingle,
            Dock        = DockStyle.Fill
        };

        private static ComboBox MakeCombo() => new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font          = new Font("Segoe UI", 12f),
            Dock          = DockStyle.Fill
        };

        /// <summary>
        /// Wraps a control in a transparent Panel with 6px top/bottom padding
        /// and 12px right padding to create breathing room inside each table cell.
        /// </summary>
        private static Panel Pad(Control ctrl)
        {
            ctrl.Dock = DockStyle.Fill;
            var p = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(0, 6, 12, 6)
            };
            p.Controls.Add(ctrl);
            return p;
        }

        /// <summary>
        /// Creates a field label.  When <paramref name="required"/> is true, a red
        /// asterisk is appended so users know the field is mandatory.
        /// Labels are aligned MiddleLeft with 18px left indent matching card padding.
        /// </summary>
        private static Label FieldLabel(string text, bool required = false) => new Label
        {
            Text      = required ? text + " *" : text,
            Font      = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(98, 112, 135),
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft,   // sit at bottom of 28px row → visually flush above input
            Padding   = new Padding(18, 0, 0, 2)
        };

        private static Panel CardTitlePanel(string title)
        {
            var pnl = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = Color.Transparent };
            var lbl = new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Palette.TextMain,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(18, 0, 0, 0)
            };
            var div = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Palette.BorderColor };
            pnl.Controls.Add(lbl);
            pnl.Controls.Add(div);
            return pnl;
        }

        private static Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Palette.Primary,
                FlatStyle = FlatStyle.Flat,
                Location  = loc, Width = w, Height = h,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize           = 0;
            b.FlatAppearance.MouseOverBackColor   = Color.FromArgb(26, 77, 192);
            b.FlatAppearance.MouseDownBackColor   = Color.FromArgb(21, 60, 155);
            return b;
        }

        private static Button MakeOutlineBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Palette.TextMain,
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location  = loc, Width = w, Height = h,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderColor        = Palette.BorderColor;
            b.FlatAppearance.BorderSize         = 1;
            b.FlatAppearance.MouseOverBackColor = Palette.BgPage;
            return b;
        }
    }
}
