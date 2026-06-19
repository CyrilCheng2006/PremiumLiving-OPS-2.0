using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.AfterService
{
    partial class CreateInvoiceForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── Field declarations ────────────────────────────────────────────────────────
        private AppShell        _shell;
        private TextBox         txtSearchOrderNo;
        private TextBox         txtSearchCustomer;
        private Button          btnSearch;
        private Button          btnReset;
        private DataGridView    dgvOrders;
        private Label           lblSelectedOrderID;
        private Label           lblCustomer;
        private Label           lblGrandTotal;
        private NumericUpDown   nudDeposit;
        private NumericUpDown   nudPaid;
        private Label           lblRemaining;
        private DateTimePicker  dtpDueDate;
        private ComboBox        cboPaymentStatus;
        private Button          btnCreateInvoice;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text          = "Premium Living OPS — Create Invoice";
            this.Size          = new Size(1440, 900);
            this.MinimumSize   = new Size(1200, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = Palette.BgPage;
            this.WindowState   = FormWindowState.Maximized;
            this.Font          = new Font("Segoe UI", 13f);

            // ── Root panel
            var pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage };
            _shell = new AppShell();
            _shell.SetPopupContainer(pnlMain);
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;

            // ════════════════════════════════════════════════════════════════
            // CARD 1 — Search  (Top, fixed 210px)
            // ════════════════════════════════════════════════════════════════
            var (searchOuter, searchInner) = CardPanel.Create(outerHeight: 210);

            var pnlSearchTitle = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.Transparent };
            var lblSearchTitle = new Label
            {
                Text = "Search",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Palette.TextMain, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(18, 0, 0, 0)
            };
            var divSearch = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Palette.BorderColor };
            pnlSearchTitle.Controls.Add(lblSearchTitle);
            pnlSearchTitle.Controls.Add(divSearch);

            txtSearchOrderNo = new TextBox
            {
                Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Order No. (e.g. ORD-20260301-0033)"
            };
            txtSearchOrderNo.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            txtSearchCustomer = new TextBox
            {
                Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Customer name"
            };
            txtSearchCustomer.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            btnSearch = MakePrimaryBtn("🔍  Search", Point.Empty, 190, 52);
            btnReset  = MakeOutlineBtn("↺  Reset",  Point.Empty, 190, 52);
            btnSearch.Click += (s, e) => RefreshGrid();
            btnReset.Click  += (s, e) => ResetSearch();

            var tblSearch = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(18, 12, 18, 12)
            };
            tblSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210f));
            tblSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210f));
            tblSearch.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
            tblSearch.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            tblSearch.Controls.Add(MakeFieldLabel("Order No."), 0, 0);
            tblSearch.Controls.Add(MakeFieldLabel("Customer"),  1, 0);
            txtSearchOrderNo.Dock  = DockStyle.Fill;
            txtSearchCustomer.Dock = DockStyle.Fill;
            tblSearch.Controls.Add(txtSearchOrderNo,  0, 1);
            tblSearch.Controls.Add(txtSearchCustomer, 1, 1);

            var pnlBtn1 = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnSearch.Location = new Point(0, 0);   btnSearch.Width = 190;
            btnReset.Location  = new Point(198, 0); btnReset.Width  = 190;
            pnlBtn1.Controls.Add(btnSearch);
            pnlBtn1.Controls.Add(btnReset);
            tblSearch.SetColumnSpan(pnlBtn1, 2);
            tblSearch.Controls.Add(pnlBtn1, 2, 1);

            searchInner.Controls.Add(tblSearch);
            searchInner.Controls.Add(pnlSearchTitle);

            // ════════════════════════════════════════════════════════════════
            // CARD 3 — Invoice Detail  (Bottom, fixed 450px — anchored to bottom edge)
            // ════════════════════════════════════════════════════════════════
            var (formOuter, formInner) = CardPanel.Create(outerHeight: 450);
            formOuter.Dock = DockStyle.Bottom;

            var pnlFormTitle = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.Transparent };
            var lblFormTitle = new Label
            {
                Text = "Invoice Details",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Palette.TextMain, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(18, 0, 0, 0)
            };
            var divForm = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Palette.BorderColor };
            pnlFormTitle.Controls.Add(lblFormTitle);
            pnlFormTitle.Controls.Add(divForm);

            var pnlOrderSummary = new Panel
            {
                Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(246, 249, 255),
                Padding = new Padding(18, 0, 18, 0)
            };
            pnlOrderSummary.Paint += PaintBottomBorderStatic;
            var tblSummary = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 6, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            for (int i = 0; i < 6; i++)
                tblSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 6));
            tblSummary.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            lblSelectedOrderID = MakeDetailLabel("—");
            lblCustomer        = MakeDetailLabel("—");
            lblGrandTotal      = MakeDetailLabel("—");

            tblSummary.Controls.Add(MakeLabelKey("Order No."),   0, 0);
            tblSummary.Controls.Add(lblSelectedOrderID,          1, 0);
            tblSummary.Controls.Add(MakeLabelKey("Customer"),    2, 0);
            tblSummary.Controls.Add(lblCustomer,                 3, 0);
            tblSummary.Controls.Add(MakeLabelKey("Grand Total"), 4, 0);
            tblSummary.Controls.Add(lblGrandTotal,               5, 0);
            pnlOrderSummary.Controls.Add(tblSummary);

            var tblInputs = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(18, 10, 18, 10)
            };
            for (int i = 0; i < 4; i++)
                tblInputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblInputs.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            tblInputs.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            nudDeposit = new NumericUpDown
            {
                Minimum = 0, Maximum = 9999999, DecimalPlaces = 2,
                Font = new Font("Segoe UI", 12f), Dock = DockStyle.Fill
            };
            nudPaid = new NumericUpDown
            {
                Minimum = 0, Maximum = 9999999, DecimalPlaces = 2,
                Font = new Font("Segoe UI", 12f), Dock = DockStyle.Fill
            };
            nudDeposit.ValueChanged += (s, e) => RecalcBalance();
            nudPaid.ValueChanged    += (s, e) => RecalcBalance();

            dtpDueDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value  = DateTime.Today.AddDays(30),
                Font   = new Font("Segoe UI", 12f), Dock = DockStyle.Fill
            };
            cboPaymentStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 12f), Dock = DockStyle.Fill
            };
            cboPaymentStatus.Items.AddRange(new object[] { "Partial", "Full" });
            cboPaymentStatus.SelectedIndex = 0;

            lblRemaining = new Label
            {
                Text = "HK$ 0.00", Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Palette.Danger, TextAlign = ContentAlignment.MiddleLeft
            };

            tblInputs.Controls.Add(MakeFieldLabel("Deposit Amount (HK$)"), 0, 0);
            tblInputs.Controls.Add(MakeFieldLabel("Paid Amount (HK$)"),    1, 0);
            tblInputs.Controls.Add(MakeFieldLabel("Due Date"),             2, 0);
            tblInputs.Controls.Add(MakeFieldLabel("Payment Status"),       3, 0);
            tblInputs.Controls.Add(nudDeposit,       0, 1);
            tblInputs.Controls.Add(nudPaid,          1, 1);
            tblInputs.Controls.Add(dtpDueDate,       2, 1);
            tblInputs.Controls.Add(cboPaymentStatus, 3, 1);

            var pnlActionRow = new Panel
            {
                Dock = DockStyle.Bottom, Height = 80, BackColor = Color.Transparent,
                Padding = new Padding(18, 10, 18, 10)
            };
            pnlActionRow.Paint += PaintTopBorderStatic;

            var tblAction = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblAction.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160f));
            tblAction.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblAction.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f));
            tblAction.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            btnCreateInvoice = MakePrimaryBtn("✔  Create Invoice", Point.Empty, 210, 60);
            btnCreateInvoice.Dock    = DockStyle.Right;
            btnCreateInvoice.Enabled = false;
            btnCreateInvoice.Click  += btnCreateInvoice_Click;

            tblAction.Controls.Add(MakeLabelKey("Remaining Balance"), 0, 0);
            tblAction.Controls.Add(lblRemaining,                       1, 0);
            tblAction.Controls.Add(btnCreateInvoice,                   2, 0);
            pnlActionRow.Controls.Add(tblAction);

            formInner.Controls.Add(tblInputs);
            formInner.Controls.Add(pnlActionRow);
            formInner.Controls.Add(pnlOrderSummary);
            formInner.Controls.Add(pnlFormTitle);

            // ════════════════════════════════════════════════════════════════
            // CARD 2 — Orders Without Invoice  (Fill — expands to all remaining vertical space)
            // ════════════════════════════════════════════════════════════════
            var (gridOuter, gridInner) = CardPanel.CreateFill();

            var pnlGridTitle = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.Transparent };
            var lblGridTitle = new Label
            {
                Text = "Orders Without Invoice",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Palette.TextMain, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(16, 0, 0, 0)
            };
            var divGrid = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Palette.BorderColor };
            pnlGridTitle.Controls.Add(lblGridTitle);
            pnlGridTitle.Controls.Add(divGrid);

            dgvOrders = new DataGridView
            {
                ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                GridColor = Palette.BorderColor, Font = new Font("Segoe UI", 13f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate = { Height = 48 }, Dock = DockStyle.Fill,
                ColumnHeadersHeight = 46, EnableHeadersVisualStyles = false,
                ScrollBars = ScrollBars.Vertical,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255), ForeColor = Palette.TextMuted,
                    Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                    Padding = new Padding(12, 0, 0, 0), Alignment = DataGridViewContentAlignment.MiddleLeft
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.White, ForeColor = Palette.TextMain,
                    SelectionBackColor = Color.FromArgb(219, 234, 254), SelectionForeColor = Palette.TextMain,
                    Padding = new Padding(12, 6, 12, 6)
                }
            };
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrderID",  HeaderText = "ORDER NO.",     FillWeight = 18 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomer", HeaderText = "CUSTOMER",      FillWeight = 22 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colContact",  HeaderText = "CONTACT NAME",  FillWeight = 18 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIssued",   HeaderText = "ISSUED DATE",   FillWeight = 14 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDelivery", HeaderText = "DELIVERY DATE", FillWeight = 14 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal",    HeaderText = "GRAND TOTAL",   FillWeight = 14 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",   HeaderText = "ORDER STATUS",  FillWeight = 12 });
            dgvOrders.SelectionChanged += dgvOrders_SelectionChanged;
            dgvOrders.CellFormatting   += dgvOrders_CellFormatting;
            dgvOrders.CellDoubleClick  += (s, e) => { if (e.RowIndex >= 0) FillFormFromGrid(); };

            gridInner.Controls.Add(dgvOrders);
            gridInner.Controls.Add(pnlGridTitle);

            // ── Assemble ──────────────────────────────────────────────────────────────────
            // Visual order (top → bottom): AppShell | Search | Orders Without Invoice | Invoice Detail
            // Controls.Add: gridOuter(Fill) → formOuter(Bottom,450) → searchOuter(Top,210) → _shell(Top)
            pnlMain.Controls.Add(gridOuter);    // Fill
            pnlMain.Controls.Add(formOuter);    // Bottom (450px) — Invoice Detail at foot
            pnlMain.Controls.Add(searchOuter);  // Top   (210px) — Search below AppShell
            pnlMain.Controls.Add(_shell);       // Top            — AppShell topmost

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        // ── Button / label factories ─────────────────────────────────────────────────────
        private static Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Palette.Primary, FlatStyle = FlatStyle.Flat,
                Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Palette.PrimaryDark;
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(21, 60, 155);
            return b;
        }
        private static Button MakeOutlineBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f),
                ForeColor = Palette.TextMain, BackColor = Color.White, FlatStyle = FlatStyle.Flat,
                Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderColor = Palette.BorderColor;
            b.FlatAppearance.BorderSize  = 1;
            b.FlatAppearance.MouseOverBackColor = Palette.BgPage;
            return b;
        }
        private static Label MakeFieldLabel(string text) => new Label
        {
            Text = text, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Palette.TextMuted, Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft, Padding = new Padding(0, 0, 0, 2)
        };
        private static Label MakeLabelKey(string text) => new Label
        {
            Text = text, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Palette.TextMuted, Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        private static Label MakeDetailLabel(string text) => new Label
        {
            Text = text, Font = new Font("Segoe UI", 12f),
            ForeColor = Palette.TextMain, Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true
        };
        private static void PaintBottomBorderStatic(object s, PaintEventArgs e)
        { var p = (Panel)s; using var pen = new Pen(Palette.BorderColor, 1); e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1); }
        private static void PaintTopBorderStatic(object s, PaintEventArgs e)
        { var p = (Panel)s; using var pen = new Pen(Palette.BorderColor, 1); e.Graphics.DrawLine(pen, 0, 0, p.Width, 0); }
    }
}
