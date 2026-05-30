using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.OrderProcessing
{
    partial class ModifyOrderForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── AppShell ──────────────────────────────────────────────────────────
        private AppShell _shell;

        // ── Search bar ─────────────────────────────────────────────────────────
        private ComboBox cboSearchOrder;
        private Button   btnLoadOrder;

        // ── Edit card fields ───────────────────────────────────────────────────
        private Panel        pnlEditCard;
        private TextBox      txtOrderID;
        private TextBox      txtCustomer;
        private ComboBox     cboStatus;
        private DateTimePicker dtpDelivery;
        private TextBox      txtContactName;
        private TextBox      txtShippingAddr;
        private TextBox      txtBillingAddr;
        private ComboBox     cboDiscountType;
        private TextBox      txtDiscountValue;

        // ── Lines card ──────────────────────────────────────────────────────────
        private Panel        pnlLinesCard;
        private ComboBox     cboAddProduct;
        private TextBox      txtAddQty;
        private Button       btnAddLine;
        private Button       btnRemoveLine;
        private DataGridView dgvLines;
        private Label        lblSubtotal;
        private Label        lblGrandTotal;

        // ── Action buttons bar ─────────────────────────────────────────────────
        private Panel  pnlActionsBar;
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
            this.Font          = new Font("Segoe UI", 11f);

            // ── Root panel ────────────────────────────────────────────────────
            Panel pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage };

            // ── AppShell ──────────────────────────────────────────────────────
            _shell = new AppShell();
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;
            _shell.SetPopupContainer(pnlMain);

            // ── Scrollable content ────────────────────────────────────────────
            Panel pnlContent = new Panel
            {
                Dock       = DockStyle.Fill,
                Padding    = new Padding(28, 20, 28, 24),
                BackColor  = Palette.BgPage,
                AutoScroll = true
            };

            // ── Page title ──────────────────────────────────────────────────
            Label lblTitle = new Label
            {
                Text      = "Modify Order",
                Font      = new Font("Segoe UI", 22f, FontStyle.Bold),
                ForeColor = Palette.TextMain,
                AutoSize  = true,
                Location  = new Point(0, 0)
            };

            // ── Search bar ────────────────────────────────────────────────
            Panel pnlSearch = new Panel
            {
                Location  = new Point(0, lblTitle.PreferredHeight + 16),
                Height    = 56,
                BackColor = Palette.BgCard,
                Padding   = new Padding(12, 10, 12, 10)
            };
            pnlSearch.Paint += (s, e) =>
                e.Graphics.DrawRectangle(
                    new System.Drawing.Pen(Palette.BorderColor, 1),
                    0, 0, ((Panel)s).Width - 1, ((Panel)s).Height - 1);

            Label lblSearch = new Label
            {
                Text      = "Select Order:",
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Palette.TextMuted,
                AutoSize  = true,
                Location  = new Point(12, 16)
            };
            cboSearchOrder = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width         = 520,
                Location      = new Point(120, 12),
                Font          = new Font("Segoe UI", 11f)
            };
            btnLoadOrder = new Button
            {
                Text      = "Load Order",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Palette.Primary,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(130, 34),
                Location  = new Point(654, 11)
            };
            btnLoadOrder.FlatAppearance.BorderSize = 0;
            btnLoadOrder.Click += btnLoadOrder_Click;

            pnlSearch.Controls.Add(lblSearch);
            pnlSearch.Controls.Add(cboSearchOrder);
            pnlSearch.Controls.Add(btnLoadOrder);

            int editTop = pnlSearch.Bottom + 16;

            // ─────────────────────────────────────────────────────────────────
            //  EDIT CARD
            // ─────────────────────────────────────────────────────────────────
            pnlEditCard = MakeCard();
            pnlEditCard.Location = new Point(0, editTop);
            pnlEditCard.Visible  = false;

            Label lblEditTitle = MakeSectionLabel("Order Details");
            lblEditTitle.Location = new Point(16, 14);

            // Row 1
            Label lblOID  = MakeFieldLabel("Order ID",      new Point(16,  52));
            txtOrderID    = MakeTextBox(new Point(16,  72), 200);
            txtOrderID.ReadOnly = true;
            txtOrderID.BackColor = Color.FromArgb(248, 248, 248);

            Label lblCust = MakeFieldLabel("Customer",      new Point(236, 52));
            txtCustomer   = MakeTextBox(new Point(236, 72), 280);
            txtCustomer.ReadOnly = true;
            txtCustomer.BackColor = Color.FromArgb(248, 248, 248);

            Label lblStat = MakeFieldLabel("Status",        new Point(536, 52));
            cboStatus     = MakeCombo(new Point(536,  72), 180);

            // Row 2
            Label lblDel  = MakeFieldLabel("Delivery Date", new Point(16,  118));
            dtpDelivery   = new DateTimePicker
            {
                Location = new Point(16, 138),
                Width    = 200,
                Format   = DateTimePickerFormat.Short
            };

            Label lblCon  = MakeFieldLabel("Contact Name",  new Point(236, 118));
            txtContactName = MakeTextBox(new Point(236, 138), 260);

            // Row 3
            Label lblShip = MakeFieldLabel("Shipping Address", new Point(16,  182));
            txtShippingAddr = MakeTextBox(new Point(16,  202), 420);

            Label lblBill = MakeFieldLabel("Billing Address",  new Point(456, 182));
            txtBillingAddr  = MakeTextBox(new Point(456, 202), 420);

            // Row 4
            Label lblDType = MakeFieldLabel("Discount Type",  new Point(16,  248));
            cboDiscountType = MakeCombo(new Point(16,  268), 160);
            cboDiscountType.SelectedIndexChanged += cboDiscountType_SelectedIndexChanged;

            Label lblDVal  = MakeFieldLabel("Discount Value", new Point(196, 248));
            txtDiscountValue = MakeTextBox(new Point(196, 268), 130);
            txtDiscountValue.TextChanged += txtDiscountValue_TextChanged;

            pnlEditCard.Controls.AddRange(new System.Windows.Forms.Control[]
            {
                lblEditTitle,
                lblOID,  txtOrderID,
                lblCust, txtCustomer,
                lblStat, cboStatus,
                lblDel,  dtpDelivery,
                lblCon,  txtContactName,
                lblShip, txtShippingAddr,
                lblBill, txtBillingAddr,
                lblDType, cboDiscountType,
                lblDVal,  txtDiscountValue
            });
            pnlEditCard.Height = 316;

            // ─────────────────────────────────────────────────────────────────
            //  LINES CARD
            // ─────────────────────────────────────────────────────────────────
            pnlLinesCard = MakeCard();
            pnlLinesCard.Location = new Point(0, pnlEditCard.Bottom + 16);
            pnlLinesCard.Visible  = false;

            Label lblLinesTitle = MakeSectionLabel("Order Lines");
            lblLinesTitle.Location = new Point(16, 14);

            Label lblProd = MakeFieldLabel("Add Product",  new Point(16,  52));
            cboAddProduct = MakeCombo(new Point(16,  72), 340);

            Label lblQtyLbl = MakeFieldLabel("Qty",        new Point(370, 52));
            txtAddQty       = MakeTextBox(new Point(370, 72), 80);
            txtAddQty.Text  = "1";

            btnAddLine = new Button
            {
                Text      = "+ Add",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Palette.Primary,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(90, 34),
                Location  = new Point(464, 71)
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
                Location  = new Point(566, 71)
            };
            btnRemoveLine.FlatAppearance.BorderColor = Palette.Danger;
            btnRemoveLine.FlatAppearance.BorderSize  = 1;
            btnRemoveLine.Click += btnRemoveLine_Click;

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
                { Name = "colModLineItemID",   HeaderText = "Item ID",    FillWeight = 14,
                  SortMode = DataGridViewColumnSortMode.NotSortable });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colModLineItemName", HeaderText = "Item Name",  FillWeight = 38,
                  SortMode = DataGridViewColumnSortMode.NotSortable });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colModLineQty",      HeaderText = "Qty",        FillWeight = 10,
                  SortMode = DataGridViewColumnSortMode.NotSortable });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colModLinePrice",    HeaderText = "Unit Price", FillWeight = 19,
                  SortMode = DataGridViewColumnSortMode.NotSortable });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colModLineTotal",    HeaderText = "Line Total", FillWeight = 19,
                  SortMode = DataGridViewColumnSortMode.NotSortable });

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

            pnlLinesCard.Controls.AddRange(new System.Windows.Forms.Control[]
            {
                lblLinesTitle,
                lblProd,    cboAddProduct,
                lblQtyLbl,  txtAddQty,
                btnAddLine, btnRemoveLine,
                dgvLines,
                lblSubtotal, lblGrandTotal
            });
            pnlLinesCard.Height = 416;

            // ─────────────────────────────────────────────────────────────────
            //  ACTIONS BAR
            // ─────────────────────────────────────────────────────────────────
            pnlActionsBar = new Panel
            {
                Location  = new Point(0, pnlLinesCard.Bottom + 16),
                Height    = 50,
                BackColor = Palette.BgPage,
                Visible   = false
            };

            btnSaveChanges = new Button
            {
                Text      = "Save Changes",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Palette.Success,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(160, 44),
                Location  = new Point(0, 0)
            };
            btnSaveChanges.FlatAppearance.BorderSize = 0;
            btnSaveChanges.Click += btnSaveChanges_Click;

            btnCancelOrder = new Button
            {
                Text      = "Cancel Order",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Palette.Danger,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(160, 44),
                Location  = new Point(172, 0)
            };
            btnCancelOrder.FlatAppearance.BorderSize = 0;
            btnCancelOrder.Click += btnCancelOrder_Click;

            pnlActionsBar.Controls.Add(btnSaveChanges);
            pnlActionsBar.Controls.Add(btnCancelOrder);

            // ── Resize handler ────────────────────────────────────────────────
            pnlContent.Resize += (s, e) =>
            {
                int w = pnlContent.ClientSize.Width - pnlContent.Padding.Horizontal;
                pnlSearch.Width    = w;
                pnlEditCard.Width  = w;
                pnlLinesCard.Width = w;
                pnlActionsBar.Width = w;
                dgvLines.Width     = w - 32;
            };

            pnlContent.Controls.Add(lblTitle);
            pnlContent.Controls.Add(pnlSearch);
            pnlContent.Controls.Add(pnlEditCard);
            pnlContent.Controls.Add(pnlLinesCard);
            pnlContent.Controls.Add(pnlActionsBar);

            pnlMain.Controls.Add(pnlContent);
            pnlMain.Controls.Add(_shell);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        // ── UI factory helpers ─────────────────────────────────────────────────
        private static Panel MakeCard()
        {
            var p = new Panel { BackColor = Palette.BgCard, Padding = new Padding(16) };
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
                Location    = loc,
                Width       = width,
                Font        = new Font("Segoe UI", 11f),
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
