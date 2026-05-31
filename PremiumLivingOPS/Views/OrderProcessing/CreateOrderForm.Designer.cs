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

        // ── Order Information fields
        private TextBox      txtOrderID;
        private ComboBox     cboCustomer;
        private TextBox      txtContactName;
        private DateTimePicker dtpDelivery;
        private ComboBox     cboQuotation;
        private TextBox      txtShippingAddr;
        private TextBox      txtBillingAddr;
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
            // CARD 1: ORDER INFORMATION
            // 三層: pnlInfoOuter > pnlInfoInner > tblInfo
            // ==================================================================

            // ── All input controls for the header section
            txtOrderID        = MakeTextBox();
            cboCustomer       = MakeCombo();
            txtContactName    = MakeTextBox();
            dtpDelivery       = new DateTimePicker { Font = new Font("Segoe UI", 12f), Format = DateTimePickerFormat.Short, Dock = DockStyle.Fill };
            cboQuotation      = MakeCombo();
            txtShippingAddr   = MakeTextBox();
            txtBillingAddr    = MakeTextBox();
            cboDiscountType   = MakeCombo();
            cboDiscountType.Items.AddRange(new object[] { "None", "Amount", "Rate (%)" });
            cboDiscountType.SelectedIndex = 0;
            cboDiscountType.SelectedIndexChanged += cboDiscountType_SelectedIndexChanged;
            txtDiscountValue  = MakeTextBox();
            txtDiscountValue.Text    = "0";
            txtDiscountValue.Enabled = false;
            txtDiscountValue.TextChanged += txtDiscountValue_TextChanged;

            // ── 4-column field grid (label / value / label / value)
            var tblInfo = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 5,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(18, 14, 18, 14)
            };
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160f));  // left label
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  50f));   // left value
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180f));  // right label
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  50f));   // right value
            for (int r = 0; r < 5; r++)
                tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 64f));

            // Row 0
            tblInfo.Controls.Add(FieldLabel("Order ID *"),          0, 0);
            tblInfo.Controls.Add(Pad(txtOrderID),                   1, 0);
            tblInfo.Controls.Add(FieldLabel("Customer *"),          2, 0);
            tblInfo.Controls.Add(Pad(cboCustomer),                  3, 0);
            // Row 1
            tblInfo.Controls.Add(FieldLabel("Contact Name"),        0, 1);
            tblInfo.Controls.Add(Pad(txtContactName),               1, 1);
            tblInfo.Controls.Add(FieldLabel("Delivery Date *"),     2, 1);
            tblInfo.Controls.Add(Pad(dtpDelivery),                  3, 1);
            // Row 2
            tblInfo.Controls.Add(FieldLabel("Linked Quotation"),    0, 2);
            tblInfo.Controls.Add(Pad(cboQuotation),                 1, 2);
            tblInfo.Controls.Add(FieldLabel("Discount Type"),       2, 2);
            tblInfo.Controls.Add(Pad(cboDiscountType),              3, 2);
            // Row 3
            tblInfo.Controls.Add(FieldLabel("Shipping Address *"),  0, 3);
            tblInfo.Controls.Add(Pad(txtShippingAddr),              1, 3);
            tblInfo.Controls.Add(FieldLabel("Discount Value"),      2, 3);
            tblInfo.Controls.Add(Pad(txtDiscountValue),             3, 3);
            // Row 4
            tblInfo.Controls.Add(FieldLabel("Billing Address *"),   0, 4);
            tblInfo.Controls.Add(Pad(txtBillingAddr),               1, 4);
            // (right col row 4 intentionally empty)

            // Card title row (pinned to Top inside inner panel)
            var pnlInfoTitle = CardTitlePanel("Order Information");

            // Inner panel stacks title (top) + fields (fill)
            var pnlInfoContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlInfoContent.Controls.Add(tblInfo);       // Fill
            pnlInfoContent.Controls.Add(pnlInfoTitle);  // Top

            var (pnlInfoOuter, pnlInfoInner) = CardPanel.Create(outerHeight: 420);
            pnlInfoInner.Controls.Add(pnlInfoContent);

            // ==================================================================
            // CARD 2: ORDER ITEMS
            // 三層: pnlItemsOuter > pnlItemsInner > [title + toolbar + grid + totals]
            // ==================================================================

            // ── Product add toolbar
            cboProduct = MakeCombo();
            txtQty     = MakeTextBox();
            txtQty.Text  = "1";
            txtQty.Width = 80;

            btnAddLine    = MakePrimaryBtn("+ Add Item",   new Point(0, 0), 180, 52);
            btnRemoveLine = MakeOutlineBtn("− Remove",      new Point(188, 0), 150, 52);
            btnAddLine.Click    += btnAddLine_Click;
            btnRemoveLine.Click += btnRemoveLine_Click;

            var pnlToolbar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 68,
                BackColor = Color.Transparent,
                Padding   = new Padding(18, 8, 18, 0)
            };
            // Toolbar TLP: [Product combo | Qty label | Qty box | Add | Remove]
            var tblToolbar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f)); // product combo
            tblToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70f)); // "Qty:" label
            tblToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f)); // qty textbox
            tblToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 196f));// Add btn
            tblToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 158f));// Remove btn
            tblToolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            cboProduct.Dock = DockStyle.Fill;
            var lblQty = new Label
            {
                Text = "Qty:", Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 6, 0)
            };
            txtQty.Dock = DockStyle.Fill;

            tblToolbar.Controls.Add(cboProduct,  0, 0);
            tblToolbar.Controls.Add(lblQty,      1, 0);
            tblToolbar.Controls.Add(txtQty,      2, 0);
            tblToolbar.Controls.Add(btnAddLine,  3, 0);
            tblToolbar.Controls.Add(btnRemoveLine, 4, 0);
            pnlToolbar.Controls.Add(tblToolbar);

            // ── Line items DataGridView
            dgvLines = new DataGridView
            {
                ReadOnly              = true,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible     = false,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect           = false,
                BackgroundColor       = Color.White,
                BorderStyle           = BorderStyle.None,
                GridColor             = Palette.BorderColor,
                Font                  = new Font("Segoe UI", 13f),
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate           = { Height = 48 },
                Dock                  = DockStyle.Fill,
                ColumnHeadersHeight   = 46,
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

            // ── Totals / action footer
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

            // ── Items card title
            var pnlItemsTitle = CardTitlePanel("Order Items");

            // Divider between toolbar and grid
            var pnlDivider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Palette.BorderColor };

            // Assemble items card content
            var pnlItemsContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlItemsContent.Controls.Add(dgvLines);      // Fill
            pnlItemsContent.Controls.Add(pnlDivider);   // Top (below toolbar)
            pnlItemsContent.Controls.Add(pnlToolbar);   // Top
            pnlItemsContent.Controls.Add(pnlItemsTitle);// Top
            pnlItemsContent.Controls.Add(pnlFooter);    // Bottom

            var (pnlItemsOuter, pnlItemsInner) = CardPanel.CreateFill();
            pnlItemsInner.Controls.Add(pnlItemsContent);

            // ==================================================================
            // Assemble page (Top stacks in reverse-add order)
            // ==================================================================
            pnlMain.Controls.Add(pnlItemsOuter); // Fill — Order Items
            pnlMain.Controls.Add(pnlInfoOuter);  // Top  — Order Information
            pnlMain.Controls.Add(_shell);        // Top  — nav chrome

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        // ==================================================================
        // Factory helpers (mirrors ViewOrderForm / QuotationForm pattern)
        // ==================================================================

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

        /// <summary>Wraps a control in a Panel with 4px top + bottom padding so it sits centred in its row.</summary>
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

        /// <summary>
        /// Creates a card title row pinned to Top, matching the ViewOrderForm / QuotationForm style.
        /// </summary>
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

        private Button MakePrimaryBtn(string text, Point loc, int w, int h)
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
            b.FlatAppearance.BorderSize            = 0;
            b.FlatAppearance.MouseOverBackColor    = Color.FromArgb(26, 77, 192);
            b.FlatAppearance.MouseDownBackColor    = Color.FromArgb(21, 60, 155);
            return b;
        }

        private Button MakeOutlineBtn(string text, Point loc, int w, int h)
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
            b.FlatAppearance.BorderColor         = Palette.BorderColor;
            b.FlatAppearance.BorderSize          = 1;
            b.FlatAppearance.MouseOverBackColor  = Palette.BgPage;
            return b;
        }
    }
}
