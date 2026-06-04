using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.AfterService
{
    partial class CreateInvoiceForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── AppShell ─────────────────────────────────────────────────────
        private AppShell _shell;

        // ── Search card controls ─────────────────────────────────────────
        private TextBox txtSearchOrder;
        private TextBox txtSearchCustomer;
        private Button  btnSearch;
        private Button  btnReset;

        // ── Form card controls ────────────────────────────────────────────
        private Label          lblSelectedOrder;
        private Label          lblCustomer;
        private Label          lblGrandTotal;
        private NumericUpDown  nudDepositAmount;
        private NumericUpDown  nudPaidAmount;
        private DateTimePicker dtpDueDate;
        private ComboBox       cboPaymentStatus;
        private Label          lblRemainingBalance;
        private Button         btnCreateInvoice;

        // ── Grid card ─────────────────────────────────────────────────────
        private DataGridView dgvOrders;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            SuspendLayout();                                              // RULE 1

            this.Text          = "Premium Living OPS — After-Service  ›  Create Invoice";
            this.Size          = new Size(1440, 900);
            this.MinimumSize   = new Size(1280, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = Color.FromArgb(240, 244, 249);
            this.WindowState   = FormWindowState.Maximized;
            this.Font          = new Font("Segoe UI", 13f);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.AutoScaleDimensions = new SizeF(7F, 15F);

            // ── Root panel ───────────────────────────────────────────────
            var pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };

            // ── AppShell (RULE 2) ─────────────────────────────────────────
            _shell = new AppShell();
            _shell.Dock        = DockStyle.Top;
            _shell.Height      = AppShell.TotalHeight;
            _shell.MinimumSize = new Size(0, AppShell.TotalHeight);
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;           // RULE 4
            _shell.LogoutClicked   += btnLogout_Click;                   // RULE 4
            _shell.SetPopupContainer(pnlMain);

            // ══════════════════════════════════════════════════════════════
            //  Scrollable page
            // ══════════════════════════════════════════════════════════════
            var pnlPage = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };

            // ── 1. Search card (CardPanel.Create, height 160) ─────────────
            txtSearchOrder = new TextBox
            {
                Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill, PlaceholderText = "ORD-XXXX"
            };
            txtSearchCustomer = new TextBox
            {
                Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill, PlaceholderText = "Customer name"
            };
            btnSearch = MakePrimaryBtn("Search", Point.Empty, 180, 50);
            btnReset  = MakeOutlineBtn("Reset",  Point.Empty, 180, 50);
            btnSearch.Click += (s, e) => RefreshGrid();
            btnReset.Click  += (s, e) => { txtSearchOrder.Clear(); txtSearchCustomer.Clear(); RefreshGrid(); };
            txtSearchOrder.KeyDown    += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };
            txtSearchCustomer.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            var tblSearch = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 2,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(18, 12, 18, 12)
            };
            tblSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120f));
            tblSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  30f));
            tblSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120f));
            tblSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  30f));
            tblSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  40f));
            tblSearch.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
            tblSearch.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            tblSearch.Controls.Add(MakeLbl("Order No."), 0, 0);
            tblSearch.Controls.Add(txtSearchOrder,       1, 0);
            tblSearch.Controls.Add(MakeLbl("Customer"),  2, 0);
            tblSearch.Controls.Add(txtSearchCustomer,    3, 0);

            var pnlSearchBtns = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnSearch.Location = new Point(0, 0);
            btnReset.Location  = new Point(192, 0);
            pnlSearchBtns.Controls.AddRange(new Control[] { btnSearch, btnReset });
            tblSearch.SetColumnSpan(pnlSearchBtns, 5);
            tblSearch.Controls.Add(pnlSearchBtns, 0, 1);

            var (searchOuter, searchInner) = CardPanel.Create(outerHeight: 160);
            searchInner.Controls.Add(tblSearch);
            pnlPage.Controls.Add(searchOuter);

            // ── 2. Form card (CardPanel.Create, height 380) ───────────────
            lblSelectedOrder = MakeValueLbl("—");
            lblCustomer      = MakeValueLbl("—");
            lblGrandTotal    = MakeValueLbl("—");

            nudDepositAmount = new NumericUpDown
            {
                Minimum = 0, Maximum = 9999999, DecimalPlaces = 2, Font = new Font("Segoe UI", 12f),
                Dock = DockStyle.Fill, ThousandsSeparator = true
            };
            nudPaidAmount = new NumericUpDown
            {
                Minimum = 0, Maximum = 9999999, DecimalPlaces = 2, Font = new Font("Segoe UI", 12f),
                Dock = DockStyle.Fill, ThousandsSeparator = true
            };
            dtpDueDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(1),
                Font = new Font("Segoe UI", 12f), Dock = DockStyle.Fill
            };
            cboPaymentStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f), Dock = DockStyle.Fill
            };
            cboPaymentStatus.Items.AddRange(new object[] { "Partial", "Full" });
            cboPaymentStatus.SelectedIndex = 0;

            lblRemainingBalance = new Label
            {
                Text = "HK$ 0.00", Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(47, 111, 237), Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            };

            nudDepositAmount.ValueChanged += (s, e) => RecalcBalance();
            nudPaidAmount.ValueChanged    += (s, e) => RecalcBalance();

            btnCreateInvoice = MakePrimaryBtn("Create Invoice", Point.Empty, 220, 52);
            btnCreateInvoice.Click += btnCreateInvoice_Click;

            var tblForm = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 6, RowCount = 5,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(18, 12, 18, 12)
            };
            for (int i = 0; i < 6; i++)
                tblForm.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 6));
            tblForm.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
            tblForm.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));
            tblForm.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));
            tblForm.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));
            tblForm.RowStyles.Add(new RowStyle(SizeType.Percent,  100f));

            // Row 0: Section header
            var lblSectionHdr = new Label
            {
                Text = "Selected Order  /  Invoice Details", Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53), TextAlign = ContentAlignment.MiddleLeft
            };
            tblForm.SetColumnSpan(lblSectionHdr, 6);
            tblForm.Controls.Add(lblSectionHdr, 0, 0);

            // Row 1: Order / Customer / Grand Total
            tblForm.Controls.Add(MakeFieldCell("Order No.",   lblSelectedOrder), 0, 1);
            tblForm.Controls.Add(MakeFieldCell("Customer",    lblCustomer),      1, 1);
            tblForm.SetColumnSpan(MakeFieldCell("Grand Total", lblGrandTotal), 1);  // handled inline below
            var grandCell = MakeFieldCell("Grand Total", lblGrandTotal);
            tblForm.Controls.Add(grandCell, 2, 1);

            // Row 2: Deposit / Paid / Due Date
            tblForm.Controls.Add(MakeFieldCell("Deposit Amount", nudDepositAmount), 0, 2);
            tblForm.Controls.Add(MakeFieldCell("Paid Amount",    nudPaidAmount),    1, 2);
            tblForm.Controls.Add(MakeFieldCell("Due Date",       dtpDueDate),       2, 2);

            // Row 3: Payment Status / Remaining Balance
            tblForm.Controls.Add(MakeFieldCell("Payment Status",    cboPaymentStatus),   0, 3);
            tblForm.Controls.Add(MakeFieldCell("Remaining Balance", lblRemainingBalance), 1, 3);

            // Row 4: Create Invoice button
            var pnlBtnRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnCreateInvoice.Location = new Point(0, 4);
            pnlBtnRow.Controls.Add(btnCreateInvoice);
            tblForm.SetColumnSpan(pnlBtnRow, 6);
            tblForm.Controls.Add(pnlBtnRow, 0, 4);

            var (formOuter, formInner) = CardPanel.Create(outerHeight: 380);
            formInner.Controls.Add(tblForm);
            pnlPage.Controls.Add(formOuter);

            // ── 3. Grid card (CardPanel.CreateFill) ───────────────────────
            dgvOrders = BuildDataGridView();
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrderID",   HeaderText = "ORDER NO.",    FillWeight = 18 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomer",  HeaderText = "CUSTOMER",     FillWeight = 30 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colGrandTotal",HeaderText = "GRAND TOTAL",  FillWeight = 18 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",    HeaderText = "ORDER STATUS", FillWeight = 18 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIssued",    HeaderText = "ISSUED DATE",  FillWeight = 16 });
            dgvOrders.SelectionChanged += dgvOrders_SelectionChanged;

            var (gridOuter, gridInner) = CardPanel.CreateFill();
            gridInner.Controls.Add(dgvOrders);
            pnlPage.Controls.Add(gridOuter);

            // ── DockStyle.Top add order: BOTTOM-MOST first ────────────────
            // Because Top controls stack: last-added = highest.
            // pnlPage uses Fill for the grid (already Fill), then two Top cards.
            // We must add cards in REVERSE visual order (bottom → top).
            // gridOuter is Fill, already added; now add formOuter then searchOuter.
            // (They were added above; pnlPage uses layout stack, Fill is added last
            //  so it takes remaining space after two Top panels.)
            // ── Assemble pnlMain (RULE 5) ─────────────────────────────────
            pnlMain.Controls.Add(pnlPage);   // DockStyle.Fill — content area
            pnlMain.Controls.Add(_shell);    // DockStyle.Top  — chrome

            this.Controls.Add(pnlMain);
            ResumeLayout(false);
            PerformLayout();
            _shell.Height      = AppShell.TotalHeight;                   // RULE 3
            _shell.MinimumSize = new Size(0, AppShell.TotalHeight);      // RULE 3
        }

        // ── Control factory helpers ──────────────────────────────────────
        private static Label MakeLbl(string text) => new Label
        {
            Text = text, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(98, 112, 135),
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = false
        };

        private static Label MakeValueLbl(string text) => new Label
        {
            Text = text, Font = new Font("Segoe UI", 12f),
            ForeColor = Color.FromArgb(15, 31, 53),
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = false, AutoEllipsis = true
        };

        private static Panel MakeFieldCell(string caption, Control ctrl)
        {
            var tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(0, 0, 12, 0)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tlp.Controls.Add(new Label
            {
                Text = caption, Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft,
                Padding = new Padding(0, 0, 0, 2)
            }, 0, 0);
            ctrl.Dock = DockStyle.Fill;
            tlp.Controls.Add(ctrl, 0, 1);
            return tlp;
        }

        private static DataGridView BuildDataGridView() => new DataGridView
        {
            ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false, BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
            GridColor = Color.FromArgb(221, 227, 236), Font = new Font("Segoe UI", 12f),
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            RowTemplate = { Height = 46 }, Dock = DockStyle.Fill,
            ColumnHeadersHeight = 44, EnableHeadersVisualStyles = false,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(246, 249, 255), ForeColor = Color.FromArgb(98, 112, 135),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Padding = new Padding(12, 0, 0, 0), Alignment = DataGridViewContentAlignment.MiddleLeft
            },
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White, ForeColor = Color.FromArgb(15, 31, 53),
                SelectionBackColor = Color.FromArgb(219, 234, 254), SelectionForeColor = Color.FromArgb(15, 31, 53),
                Padding = new Padding(12, 6, 12, 6)
            }
        };

        private Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(47, 111, 237),
                FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(26, 77, 192);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(21, 60, 155);
            return b;
        }

        private Button MakeOutlineBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text = text, Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize  = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            return b;
        }

        private static void PaintCardBorder(object s, PaintEventArgs e)
        {
            var p = (Panel)s;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        }
    }
}
