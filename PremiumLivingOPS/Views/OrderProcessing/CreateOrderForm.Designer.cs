using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.OrderProcessing
{
    partial class CreateOrderForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── AppShell ──────────────────────────────────────────────────────────
        private AppShell _shell;

        // ── Header fields ─────────────────────────────────────────────────────
        private TextBox  txtOrderID;
        private ComboBox cboCustomer;
        private ComboBox cboQuotation;
        private DateTimePicker dtpDelivery;
        private TextBox  txtShippingAddr;
        private TextBox  txtBillingAddr;
        private TextBox  txtContactName;
        private ComboBox cboDiscountType;
        private TextBox  txtDiscountValue;

        // ── Line-item entry ───────────────────────────────────────────────────
        private ComboBox cboProduct;
        private TextBox  txtQty;
        private Button   btnAddLine;
        private Button   btnRemoveLine;

        // ── Line-item grid ────────────────────────────────────────────────────
        private DataGridView dgvLines;

        // ── Totals & actions ──────────────────────────────────────────────────
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
            this.Font          = new Font("Segoe UI", 11f);

            // ── Root panel ────────────────────────────────────────────────────
            Panel pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage };

            // ── AppShell ──────────────────────────────────────────────────────
            _shell = new AppShell();
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;

            // ── Scrollable content ────────────────────────────────────────────
            Panel pnlContent = new Panel
            {
                Dock        = DockStyle.Fill,
                Padding     = new Padding(28, 20, 28, 24),
                BackColor   = Palette.BgPage,
                AutoScroll  = true
            };

            // Page title
            Label lblTitle = new Label
            {
                Text      = "Create New Order",
                Font      = new Font("Segoe UI", 22f, FontStyle.Bold),
                ForeColor = Palette.TextMain,
                AutoSize  = true,
                Location  = new Point(0, 0)
            };

            // ─────────────────────────────────────────────────────────────────
            //  ORDER HEADER CARD
            // ─────────────────────────────────────────────────────────────────
            Panel cardHeader = MakeCard();
            cardHeader.Location = new Point(0, lblTitle.PreferredHeight + 16);

            Label lblHeaderTitle = MakeSectionLabel("Order Header");
            lblHeaderTitle.Location = new Point(16, 14);

            // Row 1 — Order ID  |  Customer
            Label lblOrderID  = MakeFieldLabel("Order ID *",      new Point(16, 52));
            txtOrderID        = MakeTextBox(new Point(16, 74), 220);

            Label lblCust     = MakeFieldLabel("Customer *",       new Point(256, 52));
            cboCustomer       = MakeCombo(new Point(256, 72), 280);

            Label lblQuot     = MakeFieldLabel("Linked Quotation", new Point(556, 52));
            cboQuotation      = MakeCombo(new Point(556, 72), 320);

            // Row 2 — Delivery Date  |  Contact Name
            Label lblDelivery = MakeFieldLabel("Delivery Date *",  new Point(16, 118));
            dtpDelivery       = new DateTimePicker
            {
                Location = new Point(16, 138),
                Width    = 200,
                Format   = DateTimePickerFormat.Short
            };

            Label lblContact  = MakeFieldLabel("Order Contact",    new Point(236, 118));
            txtContactName    = MakeTextBox(new Point(236, 138), 240);

            // Row 3 — Shipping Address  |  Billing Address
            Label lblShip     = MakeFieldLabel("Shipping Address", new Point(16, 182));
            txtShippingAddr   = MakeTextBox(new Point(16, 202), 420);

            Label lblBill     = MakeFieldLabel("Billing Address",  new Point(456, 182));
            txtBillingAddr    = MakeTextBox(new Point(456, 202), 420);

            // Row 4 — Discount
            Label lblDType    = MakeFieldLabel("Discount Type",    new Point(16, 248));
            cboDiscountType   = MakeCombo(new Point(16, 268), 160);
            cboDiscountType.Items.AddRange(new object[] { "None", "Amount", "Rate (%)" });
            cboDiscountType.SelectedIndex = 0;
            cboDiscountType.SelectedIndexChanged += cboDiscountType_SelectedIndexChanged;

            Label lblDVal     = MakeFieldLabel("Discount Value",   new Point(196, 248));
            txtDiscountValue  = MakeTextBox(new Point(196, 268), 130);
            txtDiscountValue.Text    = "0";
            txtDiscountValue.Enabled = false;
            txtDiscountValue.TextChanged += txtDiscountValue_TextChanged;

            cardHeader.Controls.AddRange(new System.Windows.Forms.Control[]
            {
                lblHeaderTitle,
                lblOrderID,  txtOrderID,
                lblCust,     cboCustomer,
                lblQuot,     cboQuotation,
                lblDelivery, dtpDelivery,
                lblContact,  txtContactName,
                lblShip,     txtShippingAddr,
                lblBill,     txtBillingAddr,
                lblDType,    cboDiscountType,
                lblDVal,     txtDiscountValue
            });
            cardHeader.Height = 316;

            // ─────────────────────────────────────────────────────────────────
            //  LINE ITEMS CARD
            // ─────────────────────────────────────────────────────────────────
            Panel cardLines = MakeCard();
            cardLines.Location = new Point(0, cardHeader.Bottom + 16);

            Label lblLinesTitle = MakeSectionLabel("Order Lines");
            lblLinesTitle.Location = new Point(16, 14);

            // Entry row
            Label lblProd   = MakeFieldLabel("Product",   new Point(16,  52));
            cboProduct      = MakeCombo(new Point(16,  72), 340);

            Label lblQtyLbl = MakeFieldLabel("Qty",       new Point(370, 52));
            txtQty          = MakeTextBox(new Point(370, 72), 80);
            txtQty.Text     = "1";

            btnAddLine = new Button
            {
                Text      = "+ Add Line",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Palette.Primary,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(120, 34),
                Location  = new Point(466, 71)
            };
            btnAddLine.FlatAppearance.BorderSize = 0;
            btnAddLine.Click += btnAddLine_Click;

            btnRemoveLine = new Button
            {
                Text      = "− Remove",
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Palette.Danger,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(110, 34),
                Location  = new Point(598, 71)
            };
            btnRemoveLine.FlatAppearance.BorderColor = Palette.Danger;
            btnRemoveLine.FlatAppearance.BorderSize  = 1;
            btnRemoveLine.Click += btnRemoveLine_Click;

            // Lines DataGridView
            dgvLines = new DataGridView
            {
                Location              = new Point(16, 116),
                ReadOnly              = true,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible     = false,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor       = Palette.BgCard,
                BorderStyle           = BorderStyle.FixedSingle,
                GridColor             = Palette.BorderColor,
                Font                  = new Font("Segoe UI", 11f),
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate           = { Height = 36 },
                MultiSelect           = false,
                Height                = 220,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255),
                    ForeColor = Palette.TextMuted,
                    Font      = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                    Padding   = new Padding(6)
                },
                ColumnHeadersHeight = 40,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor          = Palette.BgCard,
                    ForeColor          = Palette.TextMain,
                    SelectionBackColor = Color.FromArgb(240, 246, 255),
                    SelectionForeColor = Palette.TextMain,
                    Padding            = new Padding(8, 5, 8, 5)
                }
            };
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colLineItemID",   HeaderText = "Item ID",    FillWeight = 14,
                  SortMode = DataGridViewColumnSortMode.NotSortable });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colLineItemName", HeaderText = "Item Name",  FillWeight = 38,
                  SortMode = DataGridViewColumnSortMode.NotSortable });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colLineQty",      HeaderText = "Qty",        FillWeight = 10,
                  SortMode = DataGridViewColumnSortMode.NotSortable });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colLinePrice",    HeaderText = "Unit Price", FillWeight = 19,
                  SortMode = DataGridViewColumnSortMode.NotSortable });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colLineTotal",    HeaderText = "Line Total", FillWeight = 19,
                  SortMode = DataGridViewColumnSortMode.NotSortable });

            // Totals row
            lblSubtotal = new Label
            {
                Text      = "Subtotal:  HK$ 0.00",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Palette.TextMuted,
                AutoSize  = true,
                Location  = new Point(16, 348)
            };
            lblGrandTotal = new Label
            {
                Text      = "Grand Total:  HK$ 0.00",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Palette.Primary,
                AutoSize  = true,
                Location  = new Point(16, 374)
            };

            cardLines.Controls.AddRange(new System.Windows.Forms.Control[]
            {
                lblLinesTitle,
                lblProd,   cboProduct,
                lblQtyLbl, txtQty,
                btnAddLine, btnRemoveLine,
                dgvLines,
                lblSubtotal, lblGrandTotal
            });
            cardLines.Height = 416;

            // ─────────────────────────────────────────────────────────────────
            //  ACTION BUTTONS
            // ─────────────────────────────────────────────────────────────────
            Panel pnlActions = new Panel
            {
                Location  = new Point(0, cardLines.Bottom + 16),
                Height    = 50,
                BackColor = Palette.BgPage
            };

            btnSubmit = new Button
            {
                Text      = "Submit Order",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Palette.Success,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(160, 44),
                Location  = new Point(0, 0)
            };
            btnSubmit.FlatAppearance.BorderSize = 0;
            btnSubmit.Click += btnSubmit_Click;

            btnClear = new Button
            {
                Text      = "Clear Form",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Palette.TextMuted,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(130, 44),
                Location  = new Point(172, 0)
            };
            btnClear.FlatAppearance.BorderColor = Palette.BorderColor;
            btnClear.FlatAppearance.BorderSize  = 1;
            btnClear.Click += btnClear_Click;

            pnlActions.Controls.Add(btnSubmit);
            pnlActions.Controls.Add(btnClear);

            // ── Wire Resize so cards fill available width ─────────────────────
            pnlContent.Resize += (s, e) =>
            {
                int w = pnlContent.ClientSize.Width - pnlContent.Padding.Horizontal;
                cardHeader.Width  = w;
                cardLines.Width   = w;
                pnlActions.Width  = w;
                dgvLines.Width    = w - 32;
            };

            pnlContent.Controls.Add(lblTitle);
            pnlContent.Controls.Add(cardHeader);
            pnlContent.Controls.Add(cardLines);
            pnlContent.Controls.Add(pnlActions);

            pnlMain.Controls.Add(pnlContent);
            pnlMain.Controls.Add(_shell);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        // ── UI factory helpers ─────────────────────────────────────────────────
        private static Panel MakeCard()
        {
            var p = new Panel
            {
                BackColor = Palette.BgCard,
                Padding   = new Padding(16)
            };
            p.Paint += (s, e) =>
                e.Graphics.DrawRectangle(
                    new System.Drawing.Pen(Palette.BorderColor, 1),
                    0, 0, ((Panel)s).Width - 1, ((Panel)s).Height - 1);
            return p;
        }

        private static Label MakeSectionLabel(string text)
            => new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Palette.TextMain,
                AutoSize  = true
            };

        private static Label MakeFieldLabel(string text, System.Drawing.Point loc)
            => new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI", 10f),
                ForeColor = Palette.TextMuted,
                AutoSize  = true,
                Location  = loc
            };

        private static TextBox MakeTextBox(System.Drawing.Point loc, int width)
            => new TextBox
            {
                Location  = loc,
                Width     = width,
                Font      = new Font("Segoe UI", 11f),
                BorderStyle = BorderStyle.FixedSingle
            };

        private static ComboBox MakeCombo(System.Drawing.Point loc, int width)
            => new ComboBox
            {
                Location      = loc,
                Width         = width,
                Font          = new Font("Segoe UI", 11f),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
    }
}
