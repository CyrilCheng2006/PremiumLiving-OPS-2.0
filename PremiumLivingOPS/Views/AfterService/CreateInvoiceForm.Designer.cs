using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.AfterService
{
    partial class CreateInvoiceForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── AppShell ──────────────────────────────────────────────────────
        private AppShell _shell;

        // ── Search card controls ──────────────────────────────────────────
        private TextBox  txtSearchOrder;
        private TextBox  txtSearchCustomer;
        private ComboBox cboStatusFilter;
        private Button   btnSearch;
        private Button   btnReset;

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
            this.SuspendLayout();                                           // RULE 1

            // ── Form properties ───────────────────────────────────────────
            this.Text               = "Premium Living OPS — After-Service  ›  Create Invoice";
            this.Size               = new Size(1440, 900);
            this.MinimumSize        = new Size(1280, 800);
            this.StartPosition      = FormStartPosition.CenterScreen;
            this.BackColor          = Color.FromArgb(240, 244, 249);
            this.WindowState        = FormWindowState.Maximized;
            this.Font               = new Font("Segoe UI", 13f);
            this.AutoScaleMode      = AutoScaleMode.Font;                   // RULE 2 (form)
            this.AutoScaleDimensions = new SizeF(7F, 15F);                 // RULE 2 (form)

            // ── AppShell — RULE 2 ─────────────────────────────────────────
            _shell             = new AppShell();
            _shell.Dock        = DockStyle.Top;                             // RULE 2
            _shell.Height      = AppShell.TotalHeight;                     // RULE 2
            _shell.MinimumSize = new Size(0, AppShell.TotalHeight);        // RULE 2
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;              // RULE 4
            _shell.LogoutClicked   += OnLogoutClicked;                     // RULE 4

            // ── Root panel ────────────────────────────────────────────────
            var pnlMain = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 244, 249)
            };
            _shell.SetPopupContainer(pnlMain);

            // ═════════════════════════════════════════════════════════════
            //  SEARCH CARD  (DockStyle.Top, height 300)
            // ═════════════════════════════════════════════════════════════

            txtSearchOrder = new TextBox
            {
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                Dock            = DockStyle.Fill,
                PlaceholderText = "ORD-XXXX"
            };
            txtSearchOrder.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            txtSearchCustomer = new TextBox
            {
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                Dock            = DockStyle.Fill,
                PlaceholderText = "Customer name"
            };
            txtSearchCustomer.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            // Status values from schema ENUM only — Pending/Processing/Partially Delivered/Delivered/Completed/Cancelled
            cboStatusFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 12f),
                Dock          = DockStyle.Fill
            };
            cboStatusFilter.Items.AddRange(new object[]
            {
                "All",
                "Pending",
                "Processing",
                "Partially Delivered",
                "Delivered",
                "Completed",
                "Cancelled"
            });
            cboStatusFilter.SelectedIndex = 0;

            btnSearch = MakePrimaryBtn("\U0001F50D  Search", new Point(0,   0), 210, 60);
            btnReset  = MakeOutlineBtn("\u21BA  Reset",     new Point(218, 0), 210, 60);
            btnSearch.Click += (s, e) => RefreshGrid();
            btnReset.Click  += (s, e) =>
            {
                txtSearchOrder.Clear();
                txtSearchCustomer.Clear();
                cboStatusFilter.SelectedIndex = 0;
                RefreshGrid();
            };

            // Field-cell helper
            TableLayoutPanel MakeCell(string caption, Control ctrl)
            {
                var tlp = new TableLayoutPanel
                {
                    Dock            = DockStyle.Fill,
                    RowCount        = 2,
                    ColumnCount     = 1,
                    BackColor       = Color.Transparent,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding         = new Padding(0, 0, 12, 0)
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute,  40f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent,  70f));
                tlp.Controls.Add(new Label
                {
                    Text      = caption,
                    Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(98, 112, 135),
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.BottomLeft,
                    Padding   = new Padding(0, 0, 0, 2)
                }, 0, 0);
                ctrl.Dock = DockStyle.Fill;
                tlp.Controls.Add(ctrl, 0, 1);
                return tlp;
            }

            var tblFields = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 3,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            tblFields.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblFields.Controls.Add(MakeCell("Order No.", txtSearchOrder),    0, 0);
            tblFields.Controls.Add(MakeCell("Customer",  txtSearchCustomer), 1, 0);
            tblFields.Controls.Add(MakeCell("Status",    cboStatusFilter),   2, 0);

            var pnlBtns = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlBtns.Controls.Add(btnSearch);
            pnlBtns.Controls.Add(btnReset);

            var tblSearchCard = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                RowCount        = 3,
                ColumnCount     = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(18, 14, 18, 14)
            };
            tblSearchCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblSearchCard.RowStyles.Add(new RowStyle(SizeType.Absolute,  60f));
            tblSearchCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 125f));
            tblSearchCard.RowStyles.Add(new RowStyle(SizeType.Absolute,  65f));

            var pnlSearchTitle = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlSearchTitle.Controls.Add(new Label
            {
                Text      = "Search Orders (Without Invoice)",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            });
            pnlSearchTitle.Controls.Add(new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 1,
                BackColor = Color.FromArgb(221, 227, 236)
            });

            tblSearchCard.Controls.Add(pnlSearchTitle, 0, 0);
            tblSearchCard.Controls.Add(tblFields,      0, 1);
            tblSearchCard.Controls.Add(pnlBtns,        0, 2);

            var pnlSearchCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlSearchCard.Paint += PaintCardBorder;
            pnlSearchCard.Controls.Add(tblSearchCard);

            var pnlSearchOuter = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 300,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding   = new Padding(20, 14, 20, 8)
            };
            pnlSearchOuter.Controls.Add(pnlSearchCard);

            // ═════════════════════════════════════════════════════════════
            //  FORM CARD  (DockStyle.Top, height 340)
            // ═════════════════════════════════════════════════════════════

            lblSelectedOrder = MakeValueLbl("\u2014");
            lblCustomer      = MakeValueLbl("\u2014");
            lblGrandTotal    = MakeValueLbl("\u2014");

            nudDepositAmount = new NumericUpDown
            {
                Minimum = 0, Maximum = 9999999, DecimalPlaces = 2,
                Font = new Font("Segoe UI", 12f),
                Dock = DockStyle.Fill, ThousandsSeparator = true
            };
            nudPaidAmount = new NumericUpDown
            {
                Minimum = 0, Maximum = 9999999, DecimalPlaces = 2,
                Font = new Font("Segoe UI", 12f),
                Dock = DockStyle.Fill, ThousandsSeparator = true
            };
            dtpDueDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value  = DateTime.Today.AddMonths(1),
                Font   = new Font("Segoe UI", 12f),
                Dock   = DockStyle.Fill
            };
            cboPaymentStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 12f),
                Dock          = DockStyle.Fill
            };
            cboPaymentStatus.Items.AddRange(new object[] { "Partial", "Full" });
            cboPaymentStatus.SelectedIndex = 0;

            lblRemainingBalance = new Label
            {
                Text      = "HK$ 0.00",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(47, 111, 237),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false
            };

            nudDepositAmount.ValueChanged += (s, e) => RecalcBalance();
            nudPaidAmount.ValueChanged    += (s, e) => RecalcBalance();

            btnCreateInvoice        = MakePrimaryBtn("Create Invoice", Point.Empty, 220, 52);
            btnCreateInvoice.Click += btnCreateInvoice_Click;

            var tblForm = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 6,
                RowCount        = 5,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(18, 12, 18, 12)
            };
            for (int i = 0; i < 6; i++)
                tblForm.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 6));
            tblForm.RowStyles.Add(new RowStyle(SizeType.Absolute,  36f));
            tblForm.RowStyles.Add(new RowStyle(SizeType.Absolute,  60f));
            tblForm.RowStyles.Add(new RowStyle(SizeType.Absolute,  60f));
            tblForm.RowStyles.Add(new RowStyle(SizeType.Absolute,  60f));
            tblForm.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var lblFormHdr = new Label
            {
                Text      = "Selected Order  /  Invoice Details",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            tblForm.SetColumnSpan(lblFormHdr, 6);
            tblForm.Controls.Add(lblFormHdr, 0, 0);

            tblForm.Controls.Add(MakeFormCell("Order No.",   lblSelectedOrder), 0, 1);
            tblForm.Controls.Add(MakeFormCell("Customer",    lblCustomer),      2, 1);
            tblForm.Controls.Add(MakeFormCell("Grand Total", lblGrandTotal),    4, 1);

            tblForm.Controls.Add(MakeFormCell("Deposit Amount", nudDepositAmount), 0, 2);
            tblForm.Controls.Add(MakeFormCell("Paid Amount",    nudPaidAmount),    2, 2);
            tblForm.Controls.Add(MakeFormCell("Due Date",       dtpDueDate),       4, 2);

            tblForm.Controls.Add(MakeFormCell("Payment Status",    cboPaymentStatus),    0, 3);
            tblForm.Controls.Add(MakeFormCell("Remaining Balance", lblRemainingBalance), 2, 3);

            var pnlBtnRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnCreateInvoice.Location = new Point(0, 4);
            pnlBtnRow.Controls.Add(btnCreateInvoice);
            tblForm.SetColumnSpan(pnlBtnRow, 6);
            tblForm.Controls.Add(pnlBtnRow, 0, 4);

            var pnlFormCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlFormCard.Paint += PaintCardBorder;
            pnlFormCard.Controls.Add(tblForm);

            var pnlFormOuter = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 340,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding   = new Padding(20, 8, 20, 8)
            };
            pnlFormOuter.Controls.Add(pnlFormCard);

            // ═════════════════════════════════════════════════════════════
            //  GRID CARD  (DockStyle.Fill — remaining space)
            // ═════════════════════════════════════════════════════════════

            dgvOrders = new DataGridView
            {
                ReadOnly              = true,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible     = false,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect           = false,
                BackgroundColor       = Color.White,
                BorderStyle           = BorderStyle.None,
                GridColor             = Color.FromArgb(221, 227, 236),
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
                    ForeColor          = Color.FromArgb(15, 31, 53),
                    SelectionBackColor = Color.FromArgb(219, 234, 254),
                    SelectionForeColor = Color.FromArgb(15, 31, 53),
                    Padding            = new Padding(12, 6, 12, 6)
                }
            };
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrderID",  HeaderText = "ORDER NO.",    FillWeight = 18 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomer", HeaderText = "CUSTOMER",     FillWeight = 30 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal",    HeaderText = "GRAND TOTAL",  FillWeight = 18 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",   HeaderText = "ORDER STATUS", FillWeight = 18 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIssued",   HeaderText = "ISSUED DATE",  FillWeight = 16 });
            dgvOrders.SelectionChanged += dgvOrders_SelectionChanged;
            dgvOrders.CellFormatting   += dgvOrders_CellFormatting;

            var pnlGridInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlGridInner.Paint += PaintCardBorder;
            pnlGridInner.Controls.Add(dgvOrders);

            var pnlGridCard = new Panel
            {
                Dock      = DockStyle.Fill,
                Padding   = new Padding(20, 8, 20, 0),
                BackColor = Color.FromArgb(240, 244, 249)
            };
            pnlGridCard.Controls.Add(pnlGridInner);

            // ── Assemble: RULE 5 — Fill first, Top controls in reverse, _shell last ──
            pnlMain.Controls.Add(pnlGridCard);    // Fill  — grid
            pnlMain.Controls.Add(pnlFormOuter);   // Top   — invoice form card
            pnlMain.Controls.Add(pnlSearchOuter); // Top   — search card
            pnlMain.Controls.Add(_shell);          // Top   — AppShell chrome (topmost)

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
            this.PerformLayout();                                            // RULE 3
            _shell.Height      = AppShell.TotalHeight;                     // RULE 3
            _shell.MinimumSize = new Size(0, AppShell.TotalHeight);        // RULE 3
        }

        // ── Control factory helpers ───────────────────────────────────────

        private static Label MakeValueLbl(string text) => new Label
        {
            Text         = text,
            Font         = new Font("Segoe UI", 12f),
            ForeColor    = Color.FromArgb(15, 31, 53),
            Dock         = DockStyle.Fill,
            TextAlign    = ContentAlignment.MiddleLeft,
            AutoSize     = false,
            AutoEllipsis = true
        };

        private static TableLayoutPanel MakeFormCell(string caption, Control ctrl)
        {
            var tlp = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                RowCount        = 2,
                ColumnCount     = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(0, 0, 12, 0)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tlp.Controls.Add(new Label
            {
                Text      = caption,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft,
                Padding   = new Padding(0, 0, 0, 2)
            }, 0, 0);
            ctrl.Dock = DockStyle.Fill;
            tlp.Controls.Add(ctrl, 0, 1);
            return tlp;
        }

        private Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(47, 111, 237),
                FlatStyle = FlatStyle.Flat,
                Location  = loc,
                Width     = w,
                Height    = h,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize           = 0;
            b.FlatAppearance.MouseOverBackColor    = Color.FromArgb(26,  77, 192);
            b.FlatAppearance.MouseDownBackColor    = Color.FromArgb(21,  60, 155);
            return b;
        }

        private Button MakeOutlineBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location  = loc,
                Width     = w,
                Height    = h,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize         = 1;
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
