using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.AfterService
{
    partial class AccountReceivableForm
    {
        private System.ComponentModel.IContainer components = null;

        private AppShell     _shell;
        private TextBox      txtKeyword;
        private ComboBox     cboStatus;
        private Button       btnSearch;
        private Button       btnReset;
        private Panel        pnlKpi;
        private DataGridView dgvAR;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text          = "Premium Living OPS — Account Receivable";
            this.Size          = new Size(1440, 900);
            this.MinimumSize   = new Size(1200, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = Palette.BgPage;
            this.WindowState   = FormWindowState.Maximized;
            this.Font          = new Font("Segoe UI", 13f);

            var pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage };
            _shell = new AppShell();
            _shell.SetPopupContainer(pnlMain);
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;

            // ════════════════════════════════════════════════════════════════
            // CARD 1 — Search Bar  (outerHeight 150)
            // ════════════════════════════════════════════════════════════════
            var (searchOuter, searchInner) = CardPanel.Create(outerHeight: 150);

            txtKeyword = new TextBox
            {
                Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Invoice ID / Order No. / Customer"
            };
            txtKeyword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            cboStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            cboStatus.Items.AddRange(new object[] { "All", "Partial", "Full", "Overdue" });
            cboStatus.SelectedIndex = 0;

            btnSearch = MakePrimaryBtn("🔍  Search", Point.Empty, 190, 52);
            btnReset  = MakeOutlineBtn("↺  Reset",  Point.Empty, 190, 52);
            btnSearch.Click += (s, e) => RefreshGrid();
            btnReset.Click  += (s, e) => ResetSearch();

            var tblSearch = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(18, 10, 18, 10)
            };
            tblSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 205f));
            tblSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 205f));
            tblSearch.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
            tblSearch.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            tblSearch.Controls.Add(MakeFieldLabel("Search"),         0, 0);
            tblSearch.Controls.Add(MakeFieldLabel("Payment Status"), 1, 0);
            txtKeyword.Dock = DockStyle.Fill;
            cboStatus.Dock  = DockStyle.Fill;
            tblSearch.Controls.Add(txtKeyword, 0, 1);
            tblSearch.Controls.Add(cboStatus,  1, 1);

            var pnlBtns = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnSearch.Location = new Point(0, 0);
            btnReset.Location  = new Point(198, 0);
            pnlBtns.Controls.Add(btnSearch);
            pnlBtns.Controls.Add(btnReset);
            tblSearch.SetColumnSpan(pnlBtns, 2);
            tblSearch.Controls.Add(pnlBtns, 2, 1);
            searchInner.Controls.Add(tblSearch);

            // ════════════════════════════════════════════════════════════════
            // CARD 2 — KPI Summary + Invoice List button  (outerHeight 90)
            //   Left:  pnlKpi (KPI pills, Percent 100%)
            //   Right: btnInvoiceList (210 × 60, Absolute 226 incl. padding)
            // ════════════════════════════════════════════════════════════════
            var (kpiOuter, kpiInner) = CardPanel.Create(outerHeight: 90);

            var tblKpi = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.White, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(0)
            };
            tblKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 226f));
            tblKpi.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            pnlKpi = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12, 10, 0, 10) };

            // Invoice List button  210×60  (right of KPI bar)
            var btnInvoiceList = new Button
            {
                Text = "📋  Invoice List",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Palette.Primary,
                FlatStyle = FlatStyle.Flat, Dock = DockStyle.Fill, Cursor = Cursors.Hand
            };
            btnInvoiceList.FlatAppearance.BorderSize = 0;
            btnInvoiceList.FlatAppearance.MouseOverBackColor = Palette.PrimaryDark;
            btnInvoiceList.Click += (s, e) =>
            {
                using var dlg = new InvoiceListDialog();
                dlg.ShowDialog(this);
                RefreshGrid();
            };

            var pnlBtnRight = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(0, 10, 16, 10) };
            btnInvoiceList.Dock = DockStyle.Fill;
            pnlBtnRight.Controls.Add(btnInvoiceList);

            tblKpi.Controls.Add(pnlKpi,      0, 0);
            tblKpi.Controls.Add(pnlBtnRight, 1, 0);
            kpiInner.Controls.Add(tblKpi);

            // ════════════════════════════════════════════════════════════════
            // CARD 3 — AR Grid  (Fill)
            // ════════════════════════════════════════════════════════════════
            var (gridOuter, gridInner) = CardPanel.CreateFill();

            dgvAR = new DataGridView
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
            dgvAR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colInvoiceID", HeaderText = "INVOICE ID",    FillWeight = 16 });
            dgvAR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrderID",   HeaderText = "ORDER NO.",     FillWeight = 15 });
            dgvAR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomer",  HeaderText = "CUSTOMER",      FillWeight = 18 });
            dgvAR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal",     HeaderText = "TOTAL (HK$)",   FillWeight = 12 });
            dgvAR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPaid",      HeaderText = "PAID (HK$)",    FillWeight = 12 });
            dgvAR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colBalance",   HeaderText = "BALANCE (HK$)", FillWeight = 12 });
            dgvAR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",    HeaderText = "STATUS",        FillWeight = 10 });
            dgvAR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDueDate",   HeaderText = "DUE DATE",      FillWeight = 12 });
            dgvAR.CellFormatting += dgvAR_CellFormatting;

            gridInner.Controls.Add(dgvAR);

            // ── Assemble
            pnlMain.Controls.Add(gridOuter);   // Fill
            pnlMain.Controls.Add(kpiOuter);    // Top
            pnlMain.Controls.Add(searchOuter); // Top
            pnlMain.Controls.Add(_shell);      // Top — topmost

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        private static Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.White, BackColor = Palette.Primary, FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0; b.FlatAppearance.MouseOverBackColor = Palette.PrimaryDark; return b;
        }
        private static Button MakeOutlineBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f), ForeColor = Palette.TextMain, BackColor = Color.White, FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderColor = Palette.BorderColor; b.FlatAppearance.BorderSize = 1; b.FlatAppearance.MouseOverBackColor = Palette.BgPage; return b;
        }
        private static Label MakeFieldLabel(string text) => new Label { Text = text, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Palette.TextMuted, Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft, Padding = new Padding(0, 0, 0, 2) };
    }
}
