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
        private Button       btnRecord;  // KPI Bar — Record Payment (enabled on row select)

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
            // CARD 1 — Search  (Top, fixed 300px)
            // ════════════════════════════════════════════════════════════════
            var (searchOuter, searchInner) = CardPanel.Create(outerHeight: 300);

            txtKeyword = new TextBox
            {
                Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Invoice ID / Order No. / Customer"
            };
            txtKeyword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            cboStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            cboStatus.Items.AddRange(new object[] { "All", "Partial", "Full", "Overdue" });
            cboStatus.SelectedIndex = 0;

            TableLayoutPanel MakeCell(string caption, Control ctrl, bool rightPad = true)
            {
                var tlp = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1,
                    BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding = rightPad ? new Padding(0, 0, 12, 0) : Padding.Empty
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent,  70f));
                var lbl = new Label
                {
                    Text = caption, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    ForeColor = Palette.TextMuted, Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.BottomLeft, Padding = new Padding(0, 0, 0, 2)
                };
                ctrl.Dock = DockStyle.Fill;
                tlp.Controls.Add(lbl,  0, 0);
                tlp.Controls.Add(ctrl, 0, 1);
                return tlp;
            }

            var tblFields = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblFields.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblFields.Controls.Add(MakeCell("Keyword",        txtKeyword), 0, 0);
            tblFields.Controls.Add(MakeCell("Payment Status", cboStatus),  1, 0);

            var pnlBtns = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnSearch = MakePrimaryBtn("🔍  Search", new Point(0,   0), 210, 60);
            btnReset  = MakeOutlineBtn("↺  Reset",  new Point(218, 0), 210, 60);
            btnSearch.Click += (s, e) => RefreshGrid();
            btnReset.Click  += (s, e) => ResetSearch();
            pnlBtns.Controls.Add(btnSearch);
            pnlBtns.Controls.Add(btnReset);

            var tblCard = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(18, 14, 18, 14)
            };
            tblCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblCard.RowStyles.Add(new RowStyle(SizeType.Absolute,  60f));
            tblCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 125f));
            tblCard.RowStyles.Add(new RowStyle(SizeType.Absolute,  65f));

            var pnlTitle = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlTitle.Controls.Add(new Label
            {
                Text = "Search Invoices",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Palette.TextMain, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            });
            pnlTitle.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Palette.BorderColor });

            tblCard.Controls.Add(pnlTitle,  0, 0);
            tblCard.Controls.Add(tblFields, 0, 1);
            tblCard.Controls.Add(pnlBtns,   0, 2);
            searchInner.Controls.Add(tblCard);

            // ════════════════════════════════════════════════════════════════
            // CARD 2 — KPI Bar  (Top, fixed 90px)
            //
            // Left  → pnlKpi        (Fill)   KPI pills
            // Right → pnlActionBtns (Right)  [💳 Record Payment]  only
            //
            // Invoice List button removed — functionality integrated into page.
            // ════════════════════════════════════════════════════════════════
            var (kpiOuter, kpiInner) = CardPanel.Create(outerHeight: 90);

            pnlKpi = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(12, 10, 12, 10)
            };

            const int BtnW   = 290;
            const int BtnH   =  60;
            const int BtnPad =  12;

            btnRecord = MakeTealBtn("💳  Record Payment", Point.Empty, BtnW, BtnH);
            btnRecord.Enabled = false;
            btnRecord.Click  += (s, e) => OpenRecordPayment();

            var pnlActionBtns = new Panel
            {
                Dock      = DockStyle.Right,
                Width     = BtnPad + BtnW + BtnPad,
                BackColor = Color.Transparent
            };

            void CentreActionBtns()
            {
                int top = (pnlActionBtns.Height - BtnH) / 2;
                if (top < 0) top = 0;
                btnRecord.Location = new Point(BtnPad, top);
            }
            pnlActionBtns.Controls.Add(btnRecord);
            pnlActionBtns.Resize += (s, e) => CentreActionBtns();

            var pnlKpiRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlKpiRow.Controls.Add(pnlKpi);        // Fill — pills
            pnlKpiRow.Controls.Add(pnlActionBtns); // Right — button
            kpiInner.Controls.Add(pnlKpiRow);

            // ════════════════════════════════════════════════════════════════
            // CARD 3 — Invoice Grid  (Fill)
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
            dgvAR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrderID",   HeaderText = "ORDER NO.",     FillWeight = 14 });
            dgvAR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomer",  HeaderText = "CUSTOMER",      FillWeight = 18 });
            dgvAR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal",     HeaderText = "TOTAL (HK$)",   FillWeight = 12 });
            dgvAR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPaid",      HeaderText = "PAID (HK$)",    FillWeight = 12 });
            dgvAR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colBalance",   HeaderText = "BALANCE (HK$)", FillWeight = 12 });
            dgvAR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",    HeaderText = "STATUS",        FillWeight = 10 });
            dgvAR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDueDate",   HeaderText = "DUE DATE",      FillWeight = 10 });
            dgvAR.SelectionChanged += dgvAR_SelectionChanged;
            dgvAR.CellFormatting   += dgvAR_CellFormatting;
            dgvAR.CellDoubleClick  += (s, e) => { if (e.RowIndex >= 0) OpenRecordPayment(); };

            gridInner.Controls.Add(dgvAR);

            // — Assemble
            pnlMain.Controls.Add(gridOuter);   // Fill
            pnlMain.Controls.Add(kpiOuter);    // Top
            pnlMain.Controls.Add(searchOuter); // Top
            pnlMain.Controls.Add(_shell);      // Top — topmost

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        // — Button factories
        private static Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.White, BackColor = Palette.Primary, FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0; b.FlatAppearance.MouseOverBackColor = Palette.PrimaryDark; b.FlatAppearance.MouseDownBackColor = Color.FromArgb(21, 60, 155); return b;
        }
        private static Button MakeTealBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(1, 105, 111), FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0; b.FlatAppearance.MouseOverBackColor = Color.FromArgb(12, 78, 84); b.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 60, 64); return b;
        }
        private static Button MakeOutlineBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f), ForeColor = Palette.TextMain, BackColor = Color.White, FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderColor = Palette.BorderColor; b.FlatAppearance.BorderSize = 1; b.FlatAppearance.MouseOverBackColor = Palette.BgPage; return b;
        }
    }
}
