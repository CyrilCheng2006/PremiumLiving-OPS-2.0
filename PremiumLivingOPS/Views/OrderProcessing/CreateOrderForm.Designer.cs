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
        private Label    lblOrderIdValue;

        // ── Order Information — editable fields
        private ComboBox     cboCustomer;
        private TextBox      txtContactName;
        private DateTimePicker dtpDelivery;
        private ComboBox     cboQuotation;
        private TextBox      txtShippingAddr;
        private TextBox      txtBillingAddr;
        private CheckBox     chkSameAddress;
        private ComboBox     cboDiscountType;
        private TextBox      txtDiscountValue;

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
            // CARD 1 — ORDER INFORMATION  (CardPanel.Create, fixed height)
            // Three layers: pnlInfoOuter > pnlInfoInner > title + tblInfo
            // ==================================================================

            // ── Read-only Order ID label (auto-generated, shown in a styled chip)
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
                Padding   = new Padding(0, 10, 12, 10)
            };
            lblOrderIdValue.Dock = DockStyle.Fill;
            pnlOrderIdChip.Controls.Add(lblOrderIdValue);

            // ── All other input controls
            cboCustomer    = MakeCombo();
            txtContactName = MakeTextBox();
            dtpDelivery    = new DateTimePicker
            {
                Font   = new Font("Segoe UI", 12f),
                Format = DateTimePickerFormat.Short,
                Dock   = DockStyle.Fill
            };
            cboQuotation = MakeCombo();

            txtShippingAddr          = MakeTextBox();
            txtBillingAddr           = MakeTextBox();
            txtBillingAddr.BackColor = Color.FromArgb(245, 248, 255);

            chkSameAddress = new CheckBox
            {
                Text      = "Same as Shipping Address",
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            chkSameAddress.CheckedChanged += chkSameAddress_CheckedChanged;

            cboDiscountType = MakeCombo();
            cboDiscountType.Items.AddRange(new object[] { "None", "Amount", "Rate (%)" });
            cboDiscountType.SelectedIndex        = 0;
            cboDiscountType.SelectedIndexChanged += cboDiscountType_SelectedIndexChanged;

            txtDiscountValue         = MakeTextBox();
            txtDiscountValue.Text    = "0";
            txtDiscountValue.Enabled = false;
            txtDiscountValue.TextChanged += txtDiscountValue_TextChanged;

            // ── 4-column TableLayoutPanel (label / value / label / value) × 5 rows
            var tblInfo = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 4,
                RowCount        = 5,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(18, 10, 18, 10)
            };
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180f));  // L label
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  50f));   // L value
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f));  // R label
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  50f));   // R value
            for (int r = 0; r < 5; r++)
                tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 64f));

            // Row 0 — Order ID (read-only chip) | Customer
            tblInfo.Controls.Add(FieldLabel("Order ID"),           0, 0);
            tblInfo.Controls.Add(pnlOrderIdChip,                   1, 0);
            tblInfo.Controls.Add(FieldLabel("Customer *"),         2, 0);
            tblInfo.Controls.Add(Pad(cboCustomer),                 3, 0);

            // Row 1 — Shipping Address | Contact Name
            tblInfo.Controls.Add(FieldLabel("Shipping Address *"), 0, 1);
            tblInfo.Controls.Add(Pad(txtShippingAddr),             1, 1);
            tblInfo.Controls.Add(FieldLabel("Contact Name"),       2, 1);
            tblInfo.Controls.Add(Pad(txtContactName),              3, 1);

            // Row 2 — Billing Address | Same-address checkbox
            tblInfo.Controls.Add(FieldLabel("Billing Address *"),  0, 2);
            tblInfo.Controls.Add(Pad(txtBillingAddr),              1, 2);
            tblInfo.Controls.Add(FieldLabel(""),                   2, 2);  // spacer label
            tblInfo.Controls.Add(Pad(chkSameAddress),              3, 2);

            // Row 3 — Delivery Date | Linked Quotation
            tblInfo.Controls.Add(FieldLabel("Delivery Date *"),    0, 3);
            tblInfo.Controls.Add(Pad(dtpDelivery),                 1, 3);
            tblInfo.Controls.Add(FieldLabel("Linked Quotation"),   2, 3);
            tblInfo.Controls.Add(Pad(cboQuotation),                3, 3);

            // Row 4 — Discount Type | Discount Value
            tblInfo.Controls.Add(FieldLabel("Discount Type"),      0, 4);
            tblInfo.Controls.Add(Pad(cboDiscountType),             1, 4);
            tblInfo.Controls.Add(FieldLabel("Discount Value"),     2, 4);
            tblInfo.Controls.Add(Pad(txtDiscountValue),            3, 4);

            // Card title bar
            var pnlInfoTitle = CardTitlePanel("Order Information");

            // Inner content: title (Top) + fields (Fill)
            var pnlInfoContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlInfoContent.Controls.Add(tblInfo);       // Fill
            pnlInfoContent.Controls.Add(pnlInfoTitle);  // Top

            var (pnlInfoOuter, pnlInfoInner) = CardPanel.Create(outerHeight: 420);
            pnlInfoInner.Controls.Add(pnlInfoContent);

            // ==================================================================
            // CARD 2 — ORDER ITEMS  (CardPanel.CreateFill)
            // ==================================================================

            cboProduct = MakeCombo();
            txtQty     = MakeTextBox();
            txtQty.Text = "1";

            btnAddLine    = MakePrimaryBtn("+ Add Item",  Point.Empty, 180, 52);
            btnRemoveLine = MakeOutlineBtn("− Remove",    Point.Empty, 150, 52);
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
            tblToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));  // product combo
            tblToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,  70f));  // "Qty:"
            tblToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,  90f));  // qty box
            tblToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 196f));  // Add btn
            tblToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 158f));  // Remove btn
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

            // DataGridView
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

            // Footer bar
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

            var pnlItemsTitle  = CardTitlePanel("Order Items");
            var pnlDivider     = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Palette.BorderColor };

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
            pnlMain.Controls.Add(pnlItemsOuter); // Fill
            pnlMain.Controls.Add(pnlInfoOuter);  // Top
            pnlMain.Controls.Add(_shell);        // Top

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        // ── Factory helpers ─────────────────────────────────────────────

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
            var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 8, 12, 8) };
            p.Controls.Add(ctrl);
            return p;
        }

        private static Label FieldLabel(string text) => new Label
        {
            Text      = text,
            Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = Color.FromArgb(98, 112, 135),
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(18, 0, 0, 0)
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
