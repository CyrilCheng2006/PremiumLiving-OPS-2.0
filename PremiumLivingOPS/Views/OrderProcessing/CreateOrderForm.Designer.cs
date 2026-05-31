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

        // ── Order Information — read-only / auto fields
        private Label         lblOrderIdValue;

        // ── Order Information — editable fields
        private ComboBox      cboAddressId;
        private ComboBox      cboCustomer;
        private ComboBox      cboQuotation;
        private TextBox       txtContactName;
        private DateTimePicker dtpDelivery;
        private TextBox       txtShippingAddr;
        private TextBox       txtBillingAddr;
        private CheckBox      chkSameAddress;
        private ComboBox      cboDiscountType;
        private TextBox       txtDiscountValue;
        private Label         lblDiscountUnit;

        // ── Order Items controls
        private ComboBox     cboProduct;
        private TextBox      txtQty;
        private Button       btnAddLine;
        private Button       btnRemoveLine;
        private DataGridView dgvLines;

        // ── Totals / actions
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
            //
            // 3-col × 12-row TableLayoutPanel.
            //
            // r6 layout (Billing Address label row):
            //   col-0 : "Billing Address *" label   (colspan 1, same as other field labels)
            //   col-1 : chkSameAddress panel         (tight left-padding gap of 12px)
            //   col-2 : empty spacer
            //
            // chkSameAddress font = 10.5f Bold to match FieldLabel, so the label
            // and checkbox sit on the same visual baseline.
            // ==================================================================

            // ── Order ID chip (read-only)
            lblOrderIdValue = new Label
            {
                Text      = "",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Palette.Primary,
                BackColor = System.Drawing.Color.FromArgb(219, 234, 254),
                Dock      = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Padding   = new Padding(10, 0, 0, 0)
            };
            var pnlOrderIdChip = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = System.Drawing.Color.Transparent,
                Padding   = new Padding(0, 8, 12, 8)
            };
            lblOrderIdValue.Dock = DockStyle.Fill;
            pnlOrderIdChip.Controls.Add(lblOrderIdValue);

            // ── Linked Quotation
            cboQuotation = MakeCombo();

            // ── Address ID ComboBox
            cboAddressId = MakeCombo();
            cboAddressId.SelectedIndexChanged += cboAddressId_SelectedIndexChanged;

            // ── Customer
            cboCustomer = MakeCombo();
            cboCustomer.SelectedIndexChanged += cboCustomer_SelectedIndexChanged;

            // ── Shipping Address
            txtShippingAddr = MakeTextBox();
            txtShippingAddr.TextChanged += txtShippingAddr_TextChanged;

            // ── Billing Address
            txtBillingAddr           = MakeTextBox();
            txtBillingAddr.Enabled   = false;
            txtBillingAddr.BackColor = System.Drawing.Color.FromArgb(235, 240, 250);

            // ── "Same as Shipping" checkbox
            //   • Font 10.5f Bold  — matches FieldLabel so they align on the same row baseline.
            //   • TextAlign MiddleLeft + vertical centering via panel padding.
            chkSameAddress = new CheckBox
            {
                Text      = "Same as Shipping",
                Font      = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(98, 112, 135),
                Checked   = true,
                AutoSize  = false,
                Dock      = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.BottomLeft
            };
            chkSameAddress.CheckedChanged += chkSameAddress_CheckedChanged;

            // Panel that wraps chkSameAddress.
            // Left padding = 12px  → small gap between the Billing Address label cell and the checkbox.
            // Bottom padding = 2px → matches FieldLabel.Padding to keep baselines aligned.
            var pnlChk = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = System.Drawing.Color.Transparent,
                Padding   = new Padding(12, 0, 0, 2)   // 12px left gap; 0 top so it uses natural row height
            };
            chkSameAddress.Dock = DockStyle.Fill;
            pnlChk.Controls.Add(chkSameAddress);

            // ── Other fields
            dtpDelivery = new DateTimePicker
            {
                Font   = new Font("Segoe UI", 12f),
                Format = DateTimePickerFormat.Short,
                Dock   = DockStyle.Fill
            };

            txtContactName  = MakeTextBox();

            cboDiscountType = MakeCombo();
            cboDiscountType.Items.AddRange(new object[] { "None", "Amount", "Rate" });
            cboDiscountType.SelectedIndex        = 0;
            cboDiscountType.SelectedIndexChanged += cboDiscountType_SelectedIndexChanged;

            txtDiscountValue         = MakeTextBox();
            txtDiscountValue.Text    = "0";
            txtDiscountValue.Enabled = false;
            txtDiscountValue.TextChanged += txtDiscountValue_TextChanged;

            lblDiscountUnit = new Label
            {
                Text      = "",
                Font      = new Font("Segoe UI", 11f),
                ForeColor = System.Drawing.Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Right,
                AutoSize  = false,
                Width     = 48,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };
            var pnlDiscountInput = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = System.Drawing.Color.Transparent,
                Padding   = new Padding(0, 8, 12, 8)
            };
            txtDiscountValue.Dock = DockStyle.Fill;
            pnlDiscountInput.Controls.Add(txtDiscountValue);
            pnlDiscountInput.Controls.Add(lblDiscountUnit);

            // ==================================================================
            //  TableLayoutPanel — 3 columns × 12 rows
            // ==================================================================
            var tblInfo = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 3,
                RowCount        = 12,
                BackColor       = System.Drawing.Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(18, 8, 18, 8)
            };
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));

            tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));  // r0  label
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f));  // r1  input
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));  // r2  label
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f));  // r3  input
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));  // r4  label (Shipping)
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f));  // r5  input (Shipping)
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));  // r6  label (Billing + chk)
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f));  // r7  input (Billing)
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));  // r8  label
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f));  // r9  input
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));  // r10 label
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f));  // r11 input

            // r0 labels
            tblInfo.Controls.Add(FieldLabel("Order ID",         false), 0, 0);
            tblInfo.Controls.Add(FieldLabel("Linked Quotation", false), 1, 0);
            tblInfo.Controls.Add(FieldLabel("",                 false), 2, 0);
            // r1 inputs
            tblInfo.Controls.Add(pnlOrderIdChip,                        0, 1);
            tblInfo.Controls.Add(Pad(cboQuotation),                     1, 1);
            tblInfo.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = System.Drawing.Color.Transparent }, 2, 1);

            // r2 labels
            tblInfo.Controls.Add(FieldLabel("Address ID", true),  0, 2);
            tblInfo.Controls.Add(FieldLabel("Customer",   true),  1, 2);
            tblInfo.Controls.Add(FieldLabel("",           false), 2, 2);
            // r3 inputs
            tblInfo.Controls.Add(Pad(cboAddressId), 0, 3);
            tblInfo.Controls.Add(Pad(cboCustomer),  1, 3);
            tblInfo.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = System.Drawing.Color.Transparent }, 2, 3);

            // r4  Shipping Address label (spans 3 cols)
            var lblShipping = FieldLabel("Shipping Address", true);
            tblInfo.Controls.Add(lblShipping, 0, 4);
            tblInfo.SetColumnSpan(lblShipping, 3);
            // r5  Shipping Address input (spans 3 cols)
            var pnlShipping = Pad(txtShippingAddr);
            tblInfo.Controls.Add(pnlShipping, 0, 5);
            tblInfo.SetColumnSpan(pnlShipping, 3);

            // r6  Billing Address label  — col-0 only (NO colspan)
            //     chkSameAddress panel   — col-1  (tight left gap = 12px)
            //     spacer                 — col-2
            tblInfo.Controls.Add(FieldLabel("Billing Address", true), 0, 6);  // colspan = 1
            tblInfo.Controls.Add(pnlChk, 1, 6);
            tblInfo.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = System.Drawing.Color.Transparent }, 2, 6);
            // r7  Billing Address input (spans 3 cols)
            var pnlBilling = Pad(txtBillingAddr);
            tblInfo.Controls.Add(pnlBilling, 0, 7);
            tblInfo.SetColumnSpan(pnlBilling, 3);

            // r8 labels
            tblInfo.Controls.Add(FieldLabel("Delivery Date",      true),  0, 8);
            tblInfo.Controls.Add(FieldLabel("Order Contact Name", true),  1, 8);
            tblInfo.Controls.Add(FieldLabel("Discount Type",      false), 2, 8);
            // r9 inputs
            tblInfo.Controls.Add(Pad(dtpDelivery),     0, 9);
            tblInfo.Controls.Add(Pad(txtContactName),  1, 9);
            tblInfo.Controls.Add(Pad(cboDiscountType), 2, 9);

            // r10 labels
            tblInfo.Controls.Add(FieldLabel("",               false), 0, 10);
            tblInfo.Controls.Add(FieldLabel("",               false), 1, 10);
            tblInfo.Controls.Add(FieldLabel("Discount Value", false), 2, 10);
            // r11 inputs
            tblInfo.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = System.Drawing.Color.Transparent }, 0, 11);
            tblInfo.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = System.Drawing.Color.Transparent }, 1, 11);
            tblInfo.Controls.Add(pnlDiscountInput, 2, 11);

            var pnlInfoTitle   = CardTitlePanel("Order Information");
            var pnlInfoContent = new Panel { Dock = DockStyle.Fill, BackColor = System.Drawing.Color.Transparent };
            pnlInfoContent.Controls.Add(tblInfo);
            pnlInfoContent.Controls.Add(pnlInfoTitle);

            var (pnlInfoOuter, pnlInfoInner) = CardPanel.Create(outerHeight: 784);
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
                BackColor       = System.Drawing.Color.Transparent,
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
                ForeColor = System.Drawing.Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleRight,
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
                BackColor = System.Drawing.Color.Transparent,
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
                BackgroundColor           = System.Drawing.Color.White,
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
                    BackColor = System.Drawing.Color.FromArgb(246, 249, 255),
                    ForeColor = System.Drawing.Color.FromArgb(98, 112, 135),
                    Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                    Padding   = new Padding(12, 0, 0, 0),
                    Alignment = DataGridViewContentAlignment.MiddleLeft
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor          = System.Drawing.Color.White,
                    ForeColor          = Palette.TextMain,
                    SelectionBackColor = System.Drawing.Color.FromArgb(219, 234, 254),
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
                BackColor = System.Drawing.Color.FromArgb(246, 249, 255),
                Padding   = new Padding(18, 0, 18, 0)
            };
            pnlFooter.Paint += (s, e) =>
            {
                using var pen = new System.Drawing.Pen(Palette.BorderColor, 1);
                e.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0);
            };

            lblSubtotal = new Label
            {
                Text      = "Subtotal:  HK$ 0.00",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = System.Drawing.Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Left,
                AutoSize  = false,
                Width     = 340,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };
            lblGrandTotal = new Label
            {
                Text      = "Grand Total:  HK$ 0.00",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Palette.TextMain,
                Dock      = DockStyle.Left,
                AutoSize  = false,
                Width     = 380,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };

            btnSubmit = MakePrimaryBtn("✓  Submit Order", Point.Empty, 220, 52);
            btnClear  = MakeOutlineBtn("↺  Clear",        Point.Empty, 150, 52);
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
            var pnlItemsContent = new Panel { Dock = DockStyle.Fill, BackColor = System.Drawing.Color.Transparent };
            pnlItemsContent.Controls.Add(dgvLines);
            pnlItemsContent.Controls.Add(pnlDivider);
            pnlItemsContent.Controls.Add(pnlToolbar);
            pnlItemsContent.Controls.Add(pnlItemsTitle);
            pnlItemsContent.Controls.Add(pnlFooter);

            var (pnlItemsOuter, pnlItemsInner) = CardPanel.CreateFill();
            pnlItemsInner.Controls.Add(pnlItemsContent);

            // ==================================================================
            // Assemble page
            // ==================================================================
            pnlMain.Controls.Add(pnlItemsOuter);
            pnlMain.Controls.Add(pnlInfoOuter);
            pnlMain.Controls.Add(_shell);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        // ── Factory helpers

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

        private static Panel Pad(Control ctrl)
        {
            ctrl.Dock = DockStyle.Fill;
            var p = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = System.Drawing.Color.Transparent,
                Padding   = new Padding(0, 8, 12, 8)
            };
            p.Controls.Add(ctrl);
            return p;
        }

        private static Label FieldLabel(string text, bool required) => new Label
        {
            Text      = required ? text + " *" : text,
            Font      = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = System.Drawing.Color.FromArgb(98, 112, 135),
            Dock      = DockStyle.Fill,
            TextAlign = System.Drawing.ContentAlignment.BottomLeft,
            Padding   = new Padding(18, 0, 0, 2)
        };

        private static Panel CardTitlePanel(string title)
        {
            var pnl = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = System.Drawing.Color.Transparent };
            var lbl = new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Palette.TextMain,
                Dock      = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
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
                ForeColor = System.Drawing.Color.White,
                BackColor = Palette.Primary,
                FlatStyle = FlatStyle.Flat,
                Location  = loc, Width = w, Height = h,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize         = 0;
            b.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(26, 77, 192);
            b.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(21, 60, 155);
            return b;
        }

        private static Button MakeOutlineBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Palette.TextMain,
                BackColor = System.Drawing.Color.White,
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
