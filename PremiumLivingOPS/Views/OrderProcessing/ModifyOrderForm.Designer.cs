using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.OrderProcessing
{
    partial class ModifyOrderForm
    {
        private System.ComponentModel.IContainer components = null;

        private AppShell _shell;
        private ComboBox cboSearchOrder;
        private Button   btnLoadOrder;
        private Label    lblOrderIdValue;
        private ComboBox cboAddressId;
        private ComboBox cboCustomer;
        private ComboBox cboStatus;
        private ComboBox cboQuotation;
        private TextBox        txtContactName;
        private DateTimePicker dtpDelivery;
        private TextBox        txtShippingAddr;
        private TextBox        txtBillingAddr;
        private CheckBox       chkSameAddress;
        private ComboBox       cboDiscountType;
        private TextBox        txtDiscountValue;
        private Label          lblDiscountUnit;
        private ComboBox     cboProduct;
        private TextBox      txtQty;
        private Button       btnAddLine;
        private Button       btnRemoveLine;
        private DataGridView dgvLines;
        private Label  lblGrandTotal;
        private Label  lblSubtotal;
        private Button btnSaveChanges;
        private Button btnCancelOrder;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text          = "Premium Living OPS — Modify Order";
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
            // SEARCH BAR
            // ==================================================================
            var (pnlSearchOuter, pnlSearchInner) = CardPanel.Create(outerHeight: 90);
            Label lblSearchLbl = new Label
            {
                Text = "Select Order to Modify:", Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135), Dock = DockStyle.Left,
                AutoSize = false, Width = 340, TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 12, 0)
            };
            cboSearchOrder = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f), Dock = DockStyle.Fill };
            btnLoadOrder = MakePrimaryBtn("Load Order", Point.Empty, 210, 60);
            btnLoadOrder.Dock   = DockStyle.Right;
            btnLoadOrder.Click += btnLoadOrder_Click;
            var pnlSearchRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(16, 10, 16, 10) };
            pnlSearchRow.Controls.Add(cboSearchOrder);
            pnlSearchRow.Controls.Add(btnLoadOrder);
            pnlSearchRow.Controls.Add(lblSearchLbl);
            pnlSearchInner.Controls.Add(pnlSearchRow);

            // ==================================================================
            // CARD 1 — ORDER DETAILS
            // ==================================================================
            lblOrderIdValue = new Label
            {
                Text = "", Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Palette.Primary, BackColor = Color.FromArgb(219, 234, 254),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0)
            };
            var pnlOrderIdChip = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 8, 12, 8) };
            pnlOrderIdChip.Controls.Add(lblOrderIdValue);

            cboQuotation = MakeCombo();
            cboAddressId = MakeCombo();
            cboAddressId.SelectedIndexChanged += cboAddressId_SelectedIndexChanged;
            cboCustomer  = MakeCombo();
            cboCustomer.SelectedIndexChanged  += cboCustomer_SelectedIndexChanged;
            cboStatus = MakeCombo();
            cboStatus.Items.AddRange(new object[] { "Pending", "Processing", "Delivered", "Cancelled" });

            txtShippingAddr = MakeTextBox();
            txtShippingAddr.TextChanged += txtShippingAddr_TextChanged;
            txtBillingAddr = MakeTextBox();
            txtBillingAddr.Enabled   = false;
            txtBillingAddr.BackColor = Color.FromArgb(235, 240, 250);

            chkSameAddress = new CheckBox
            {
                Text = "Same as Shipping", Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135), Checked = false,
                AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            };
            chkSameAddress.CheckedChanged += chkSameAddress_CheckedChanged;
            var pnlChk = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(12, 0, 0, 0) };
            chkSameAddress.Dock = DockStyle.Fill;
            pnlChk.Controls.Add(chkSameAddress);

            dtpDelivery = new DateTimePicker { Font = new Font("Segoe UI", 12f), Format = DateTimePickerFormat.Short, Dock = DockStyle.Fill };
            txtContactName = MakeTextBox();

            cboDiscountType = MakeCombo();
            cboDiscountType.Items.AddRange(new object[] { "None", "Amount", "Rate (%)" });
            cboDiscountType.SelectedIndex = 0;
            cboDiscountType.SelectedIndexChanged += cboDiscountType_SelectedIndexChanged;

            txtDiscountValue = MakeTextBox();
            txtDiscountValue.Text    = "0";
            txtDiscountValue.Enabled = false;
            txtDiscountValue.TextChanged += txtDiscountValue_TextChanged;

            lblDiscountUnit = new Label
            {
                Text = "", Font = new Font("Segoe UI", 11f), ForeColor = Color.FromArgb(98, 112, 135),
                Dock = DockStyle.Right, AutoSize = false, Width = 48, TextAlign = ContentAlignment.MiddleLeft
            };
            var pnlDiscountInput = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 8, 12, 8) };
            txtDiscountValue.Dock = DockStyle.Fill;
            pnlDiscountInput.Controls.Add(txtDiscountValue);
            pnlDiscountInput.Controls.Add(lblDiscountUnit);

            var tblInfo = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 14,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(18, 8, 18, 8)
            };
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            for (int i = 0; i < 7; i++) { tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f)); tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f)); }

            tblInfo.Controls.Add(FieldLabel("Order ID",         false), 0, 0);
            tblInfo.Controls.Add(FieldLabel("Linked Quotation", false), 1, 0);
            tblInfo.Controls.Add(pnlOrderIdChip,                        0, 1);
            tblInfo.Controls.Add(Pad(cboQuotation),                     1, 1);
            tblInfo.Controls.Add(FieldLabel("Address ID", true),  0, 2);
            tblInfo.Controls.Add(FieldLabel("Customer",   true),  1, 2);
            tblInfo.Controls.Add(Pad(cboAddressId),                0, 3);
            tblInfo.Controls.Add(Pad(cboCustomer),                 1, 3);
            var lblShip = FieldLabel("Shipping Address", true); tblInfo.Controls.Add(lblShip, 0, 4); tblInfo.SetColumnSpan(lblShip, 2);
            var pnlShip = Pad(txtShippingAddr);                 tblInfo.Controls.Add(pnlShip, 0, 5); tblInfo.SetColumnSpan(pnlShip, 2);
            tblInfo.Controls.Add(FieldLabel("Billing Address", true), 0, 6);
            tblInfo.Controls.Add(pnlChk,                              1, 6);
            var pnlBill = Pad(txtBillingAddr); tblInfo.Controls.Add(pnlBill, 0, 7); tblInfo.SetColumnSpan(pnlBill, 2);
            tblInfo.Controls.Add(FieldLabel("Delivery Date",      true), 0,  8);
            tblInfo.Controls.Add(FieldLabel("Order Contact Name", true), 1,  8);
            tblInfo.Controls.Add(Pad(dtpDelivery),                        0,  9);
            tblInfo.Controls.Add(Pad(txtContactName),                     1,  9);
            tblInfo.Controls.Add(FieldLabel("Order Status", true),       0, 10);
            tblInfo.Controls.Add(Pad(cboStatus),                         0, 11);
            tblInfo.Controls.Add(FieldLabel("Discount Type",  false),    0, 12);
            tblInfo.Controls.Add(FieldLabel("Discount Value", false),    1, 12);
            tblInfo.Controls.Add(Pad(cboDiscountType),                    0, 13);
            tblInfo.Controls.Add(pnlDiscountInput,                        1, 13);

            var pnlInfoTitle   = CardTitlePanel("Order Details");
            var pnlInfoContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlInfoContent.Controls.Add(tblInfo);
            pnlInfoContent.Controls.Add(pnlInfoTitle);
            var (pnlInfoOuter, pnlInfoInner) = CardPanel.Create(outerHeight: 900);
            pnlInfoInner.Controls.Add(pnlInfoContent);

            // ==================================================================
            // FOOTER  — Grand Total | Subtotal | [Save Changes] [Cancel Order]
            // ==================================================================

            const int BtnW   = 210;
            const int BtnH   = 60;
            const int BtnGap = 8;
            const int BtnPad = 12;

            // Grand Total — LEFT, leftmost (added AFTER Subtotal so it is outermost-left)
            lblGrandTotal = new Label
            {
                Text      = "Grand Total:  HK$ 0.00",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Palette.TextMain,
                Dock      = DockStyle.Left,
                AutoSize  = false,
                Width     = 320,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(8, 0, 16, 0)
            };

            // Subtotal — LEFT, sits right of Grand Total (added FIRST)
            lblSubtotal = new Label
            {
                Text      = "Subtotal:  HK$ 0.00",
                Font      = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Left,
                AutoSize  = false,
                Width     = 280,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(0, 0, 16, 0)
            };

            btnSaveChanges = MakeGreenBtn("\u2713  Save Changes", Point.Empty, BtnW, BtnH);
            btnCancelOrder = MakeRedBtn(  "\u2715  Cancel Order", Point.Empty, BtnW, BtnH);
            btnSaveChanges.Click += btnSaveChanges_Click;
            btnCancelOrder.Click += btnCancelOrder_Click;

            var pnlActionBtns = new Panel
            {
                Dock      = DockStyle.Right,
                Width     = BtnPad + BtnW + BtnGap + BtnW + BtnPad,
                BackColor = Color.Transparent
            };
            void CentreFooterBtns()
            {
                int top = (pnlActionBtns.Height - BtnH) / 2;
                if (top < 0) top = 0;
                btnSaveChanges.Location = new Point(BtnPad, top);
                btnCancelOrder.Location = new Point(BtnPad + BtnW + BtnGap, top);
            }
            pnlActionBtns.Controls.Add(btnSaveChanges);
            pnlActionBtns.Controls.Add(btnCancelOrder);
            pnlActionBtns.Resize += (s, e) => CentreFooterBtns();

            // DockStyle rule: LAST added = outermost.
            // Left labels in REVERSE visual order: Subtotal first (inner), GrandTotal second (outer-left).
            // Right panel last (claims right edge).
            var pnlFooterContent = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(16, 0, 0, 0)
            };
            pnlFooterContent.Controls.Add(lblSubtotal);    // added 1st → inner-left (right of Grand Total)
            pnlFooterContent.Controls.Add(lblGrandTotal);  // added 2nd → outer-left (leftmost)
            pnlFooterContent.Controls.Add(pnlActionBtns);  // added last → Right outermost

            var footerInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            footerInner.Paint += (s, e) =>
            {
                var p = (Panel)s;
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            };
            footerInner.Controls.Add(pnlFooterContent);

            var footerOuter = new Panel { Dock = DockStyle.Bottom, Height = 108, BackColor = Palette.BgPage, Padding = new Padding(20, 14, 20, 14) };
            footerOuter.Controls.Add(footerInner);

            // ==================================================================
            // CARD 2 — ORDER ITEMS
            // ==================================================================
            cboProduct = MakeCombo();
            txtQty = MakeTextBox();
            txtQty.Text = "1";

            const int ItemBtnW = 210;
            const int ItemBtnH = 60;

            btnAddLine    = MakePrimaryBtn("+ Add Item", Point.Empty, ItemBtnW, ItemBtnH);
            btnRemoveLine = MakeOutlineBtn("\u2212 Remove", Point.Empty, ItemBtnW, ItemBtnH);
            btnAddLine.Anchor    = AnchorStyles.None;
            btnRemoveLine.Anchor = AnchorStyles.None;
            btnAddLine.Click    += btnAddLine_Click;
            btnRemoveLine.Click += btnRemoveLine_Click;

            var tblToolbar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,  70f));
            tblToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,  90f));
            tblToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 218f));
            tblToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210f));
            tblToolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            cboProduct.Dock = DockStyle.Fill;
            var lblQty = new Label { Text = "Qty:", Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.FromArgb(98, 112, 135), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 0, 6, 0) };
            txtQty.Dock = DockStyle.Fill;

            var pnlAddBtn = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var pnlRemBtn = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            void CentreItemBtn(Panel pnl, Button btn)
            {
                int top  = (pnl.Height - ItemBtnH) / 2; if (top  < 0) top  = 0;
                int left = (pnl.Width  - ItemBtnW) / 2; if (left < 0) left = 0;
                btn.Location = new Point(left, top);
            }
            pnlAddBtn.Controls.Add(btnAddLine);
            pnlRemBtn.Controls.Add(btnRemoveLine);
            pnlAddBtn.Resize += (s, e) => CentreItemBtn(pnlAddBtn, btnAddLine);
            pnlRemBtn.Resize += (s, e) => CentreItemBtn(pnlRemBtn, btnRemoveLine);

            tblToolbar.Controls.Add(cboProduct,  0, 0);
            tblToolbar.Controls.Add(lblQty,      1, 0);
            tblToolbar.Controls.Add(txtQty,      2, 0);
            tblToolbar.Controls.Add(pnlAddBtn,   3, 0);
            tblToolbar.Controls.Add(pnlRemBtn,   4, 0);

            var pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 76, BackColor = Color.Transparent, Padding = new Padding(18, 8, 18, 8) };
            pnlToolbar.Controls.Add(tblToolbar);

            dgvLines = new DataGridView
            {
                ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                GridColor = Palette.BorderColor, Font = new Font("Segoe UI", 13f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate = { Height = 48 }, Dock = DockStyle.Fill,
                ColumnHeadersHeight = 46, EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255), ForeColor = Color.FromArgb(98, 112, 135),
                    Font = new Font("Segoe UI", 11f, FontStyle.Bold), Padding = new Padding(12, 0, 0, 0),
                    Alignment = DataGridViewContentAlignment.MiddleLeft
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.White, ForeColor = Palette.TextMain,
                    SelectionBackColor = Color.FromArgb(219, 234, 254), SelectionForeColor = Palette.TextMain,
                    Padding = new Padding(12, 6, 12, 6)
                }
            };
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLineItemID", HeaderText = "ITEM ID",    FillWeight = 14 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLineName",   HeaderText = "ITEM NAME",  FillWeight = 44 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLineQty",    HeaderText = "QTY",        FillWeight = 10 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLinePrice",  HeaderText = "UNIT PRICE", FillWeight = 16 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLineTotal",  HeaderText = "LINE TOTAL", FillWeight = 16 });

            var pnlItemsTitle   = CardTitlePanel("Order Items");
            var pnlDivider      = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Palette.BorderColor };
            var pnlItemsContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlItemsContent.Controls.Add(dgvLines);
            pnlItemsContent.Controls.Add(pnlDivider);
            pnlItemsContent.Controls.Add(pnlToolbar);
            pnlItemsContent.Controls.Add(pnlItemsTitle);

            var (pnlItemsOuter, pnlItemsInner) = CardPanel.CreateFill();
            pnlItemsInner.Controls.Add(pnlItemsContent);

            // ==================================================================
            // Assemble
            // ==================================================================
            pnlMain.Controls.Add(pnlItemsOuter);
            pnlMain.Controls.Add(footerOuter);
            pnlMain.Controls.Add(pnlInfoOuter);
            pnlMain.Controls.Add(pnlSearchOuter);
            pnlMain.Controls.Add(_shell);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        private static TextBox MakeTextBox() => new TextBox { Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle, Dock = DockStyle.Fill };
        private static ComboBox MakeCombo() => new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f), Dock = DockStyle.Fill };
        private static Panel Pad(Control ctrl)
        {
            ctrl.Dock = DockStyle.Fill;
            var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 8, 12, 8) };
            p.Controls.Add(ctrl);
            return p;
        }
        private static Label FieldLabel(string text, bool required) => new Label
        {
            Text = required ? text + " *" : text, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(98, 112, 135), Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft, Padding = new Padding(18, 0, 0, 2)
        };
        private static Panel CardTitlePanel(string title)
        {
            var pnl = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = Color.Transparent };
            var lbl = new Label { Text = title, Font = new Font("Segoe UI", 13f, FontStyle.Bold), ForeColor = Palette.TextMain, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(18, 0, 0, 0) };
            var div = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Palette.BorderColor };
            pnl.Controls.Add(lbl); pnl.Controls.Add(div);
            return pnl;
        }
        private static Button MakeGreenBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(34, 139, 34), FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0; b.FlatAppearance.MouseOverBackColor = Color.FromArgb(22, 111, 22); b.FlatAppearance.MouseDownBackColor = Color.FromArgb(14, 85, 14); return b;
        }
        private static Button MakeRedBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(192, 57, 43), FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0; b.FlatAppearance.MouseOverBackColor = Color.FromArgb(160, 40, 30); b.FlatAppearance.MouseDownBackColor = Color.FromArgb(125, 28, 20); return b;
        }
        private static Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(47, 111, 237), FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0; b.FlatAppearance.MouseOverBackColor = Color.FromArgb(26, 77, 192); b.FlatAppearance.MouseDownBackColor = Color.FromArgb(21, 60, 155); return b;
        }
        private static Button MakeOutlineBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White, FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236); b.FlatAppearance.BorderSize = 1; b.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249); return b;
        }
    }
}
