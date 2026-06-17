using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    partial class HandlingGoodsReceivedForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── AppShell (contains TopNavBar + UserBar) ───────────────────────────
        // Declared here so RefreshGrids() in .cs can call _shell.SetUser() etc.
        private AppShell _shell;

        // ── Filter bar controls ───────────────────────────────────────────────
        private TextBox        txtKeyword;
        private ComboBox       cboStatus;
        private DateTimePicker dtpDateFrom;
        private CheckBox       chkDateFrom;
        private Button         btnSearch;
        private Button         btnRefresh;

        // ── KPI bar ──────────────────────────────────────────────────────────
        private Panel pnlKpi;

        // ── Grid tab switcher (below KPI bar) ────────────────────────────────
        private Button btnTabReceipts;
        private Button btnTabPO;
        private Button btnTabInvoices;

        // ── Action buttons ───────────────────────────────────────────────────
        private Button btnViewPODetail;
        private Button btnViewReceiptLines;
        private Button btnUploadReceipt;
        private Button btnRecordInvoice;

        // ── Three grids ──────────────────────────────────────────────────────
        private DataGridView dgvReceipts;
        private DataGridView dgvPO;
        private DataGridView dgvInvoices;

        // ── Grid container (shared Fill area) ────────────────────────────────
        private Panel pnlGridHost;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            // ════════════════════════════════════════════════════════════════
            // RULE 1: SuspendLayout MUST be the very first statement.
            //         Every control is created while layout is suspended.
            //         Violating this causes AutoScaleMode = Font to
            //         re-calculate control sizes on each Controls.Add() call.
            // ════════════════════════════════════════════════════════════════
            this.SuspendLayout();

            // ── Form settings ────────────────────────────────────────────────
            this.Text             = "Premium Living OPS — Handling Goods Received";
            this.Size             = new Size(1440, 900);
            this.MinimumSize      = new Size(1280, 720);
            this.StartPosition    = FormStartPosition.CenterScreen;
            this.BackColor        = Color.FromArgb(240, 244, 249);
            this.WindowState      = FormWindowState.Maximized;
            this.Font             = new Font("Segoe UI", 13f);
            this.AutoScaleMode    = AutoScaleMode.Font;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);

            // ════════════════════════════════════════════════════════════════
            // RULE 2: AppShell MUST be constructed INSIDE the SuspendLayout
            //         scope (i.e. before ResumeLayout / PerformLayout).
            //         This prevents AutoScaleMode = Font from resizing _shell
            //         during PerformLayout.
            //
            //  AppShell internally composes:
            //    ┌─ AppShell (DockStyle.Top, Height = AppShell.TotalHeight) ──┐
            //    │  TopNavBar  (44 px) — menu items, breadcrumb               │
            //    │  UserBar    (72 px) — UserInfoLabel, logout button          │
            //    └───────────────────────────────────────────────────────────  ┘
            // ════════════════════════════════════════════════════════════════
            _shell             = new AppShell();
            _shell.Dock        = DockStyle.Top;
            _shell.Height      = AppShell.TotalHeight;
            _shell.MinimumSize = new System.Drawing.Size(0, AppShell.TotalHeight);

            // ════════════════════════════════════════════════════════════════
            // RULE 4: Subscribe AppShell events ONCE, HERE in Designer.cs.
            //         The .cs Load / constructor must NOT re-subscribe these.
            //         Duplicate subscriptions cause every click to fire twice.
            // ════════════════════════════════════════════════════════════════
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;

            // ── Root panel ───────────────────────────────────────────────────
            // pnlMain is the popup container for AppShell dropdowns.
            var pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };
            _shell.SetPopupContainer(pnlMain);

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  Search / Filter card  (DockStyle.Top, Height 300)
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            txtKeyword = new TextBox
            {
                Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill, PlaceholderText = "PO ID / Supplier / Material"
            };
            txtKeyword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrids(); };

            cboStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f), Dock = DockStyle.Fill };
            cboStatus.Items.AddRange(new object[] { "All", "Sent", "Partially Received", "Received", "Completed", "Cancelled" });
            cboStatus.SelectedIndex = 0;

            chkDateFrom = new CheckBox { Text = "", Width = 24, Checked = false, Cursor = Cursors.Hand };
            dtpDateFrom = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-1),
                Font = new Font("Segoe UI", 12f), Enabled = false, Dock = DockStyle.Fill
            };
            chkDateFrom.CheckedChanged += (s, e) => { dtpDateFrom.Enabled = chkDateFrom.Checked; };

            TableLayoutPanel MakeCell(string caption, Control ctrl, bool rightPad = true)
            {
                var tlp = new TableLayoutPanel
                {
                    Dock        = DockStyle.Fill,
                    RowCount    = 2,
                    ColumnCount = 1,
                    BackColor   = Color.Transparent,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding     = rightPad ? new Padding(0, 0, 12, 0) : Padding.Empty
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent,  70f));
                var lbl = new Label
                {
                    Text      = caption,
                    Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(98, 112, 135),
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.BottomLeft,
                    Padding   = new Padding(0, 0, 0, 2)
                };
                ctrl.Dock = DockStyle.Fill;
                tlp.Controls.Add(lbl,  0, 0);
                tlp.Controls.Add(ctrl, 0, 1);
                return tlp;
            }

            var cellDate = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                RowCount    = 2,
                ColumnCount = 2,
                BackColor   = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            cellDate.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 33f));
            cellDate.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            cellDate.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            cellDate.RowStyles.Add(new RowStyle(SizeType.Percent,  70f));
            var lblDate = new Label
            {
                Text      = "Date From",
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft,
                Padding   = new Padding(0, 0, 0, 2)
            };
            chkDateFrom.Dock = DockStyle.Fill;
            dtpDateFrom.Dock = DockStyle.Fill;
            cellDate.SetColumnSpan(lblDate, 2);
            cellDate.Controls.Add(lblDate,     0, 0);
            cellDate.Controls.Add(chkDateFrom, 0, 1);
            cellDate.Controls.Add(dtpDateFrom, 1, 1);

            var tblFields = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 3,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));
            tblFields.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblFields.Controls.Add(MakeCell("Keyword (PO / Supplier / Material)", txtKeyword), 0, 0);
            tblFields.Controls.Add(MakeCell("PO Status",                          cboStatus),  1, 0);
            tblFields.Controls.Add(cellDate,                                                    2, 0);

            var pnlBtns = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnSearch  = MakePrimaryBtn("\U0001F50D  Search", new Point(0,   0), 210, 60);
            btnRefresh = MakeOutlineBtn("\u21ba  Reset",     new Point(218, 0), 210, 60);
            btnSearch.Click  += (s, e) => RefreshGrids();
            btnRefresh.Click += (s, e) => ResetFilters();
            pnlBtns.Controls.Add(btnSearch);
            pnlBtns.Controls.Add(btnRefresh);

            var tblCard = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                RowCount        = 3,
                ColumnCount     = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(18, 14, 18, 14)
            };
            tblCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblCard.RowStyles.Add(new RowStyle(SizeType.Absolute,  60f));
            tblCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 125f));
            tblCard.RowStyles.Add(new RowStyle(SizeType.Absolute,  65f));

            var pnlTitleBar = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var lblTitle = new Label
            {
                Text      = "Search",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var divider = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(221, 227, 236) };
            pnlTitleBar.Controls.Add(lblTitle);
            pnlTitleBar.Controls.Add(divider);
            tblCard.Controls.Add(pnlTitleBar, 0, 0);
            tblCard.Controls.Add(tblFields,   0, 1);
            tblCard.Controls.Add(pnlBtns,     0, 2);

            var pnlCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlCard.Paint += PaintCardBorder;
            pnlCard.Controls.Add(tblCard);

            var pnlSearchOuter = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 300,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding   = new Padding(20, 14, 20, 8)
            };
            pnlSearchOuter.Controls.Add(pnlCard);

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  KPI bar + 4 action buttons  (DockStyle.Top, Height 90)
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            pnlKpi = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(12, 10, 12, 10) };

            const int BtnW = 230, BtnH = 60, BtnGap = 8, BtnPad = 12;
            btnViewPODetail     = MakePrimaryBtn("\U0001F50D  PO Detail",     Point.Empty, BtnW, BtnH);
            btnViewReceiptLines = MakePrimaryBtn("\U0001F4CB  Receipt Lines",  Point.Empty, BtnW, BtnH);
            btnUploadReceipt    = MakeSuccessBtn("\U0001F4E4  Upload Receipt", Point.Empty, BtnW, BtnH);
            btnRecordInvoice    = MakeWarningBtn("\U0001F4C4  Record Invoice", Point.Empty, BtnW, BtnH);
            btnViewPODetail.Enabled = btnViewReceiptLines.Enabled =
                btnUploadReceipt.Enabled = btnRecordInvoice.Enabled = false;
            btnViewPODetail.Click     += btnViewPODetail_Click;
            btnViewReceiptLines.Click += btnViewReceiptLines_Click;
            btnUploadReceipt.Click    += btnUploadReceipt_Click;
            btnRecordInvoice.Click    += btnRecordInvoice_Click;

            int actionPanelWidth = BtnPad + (BtnW + BtnGap) * 4 + BtnPad - BtnGap;
            var pnlActionBtns = new Panel { Dock = DockStyle.Right, Width = actionPanelWidth, BackColor = Color.Transparent };
            void CentreActionBtns()
            {
                int top = Math.Max(0, (pnlActionBtns.Height - BtnH) / 2);
                btnViewPODetail.Location     = new Point(BtnPad, top);
                btnViewReceiptLines.Location = new Point(BtnPad +  BtnW + BtnGap,      top);
                btnUploadReceipt.Location    = new Point(BtnPad + (BtnW + BtnGap) * 2, top);
                btnRecordInvoice.Location    = new Point(BtnPad + (BtnW + BtnGap) * 3, top);
            }
            pnlActionBtns.Controls.AddRange(new Control[] { btnViewPODetail, btnViewReceiptLines, btnUploadReceipt, btnRecordInvoice });
            pnlActionBtns.Resize += (s, e) => CentreActionBtns();

            var pnlKpiRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlKpiRow.Controls.Add(pnlKpi);        // Fill — pills
            pnlKpiRow.Controls.Add(pnlActionBtns); // Right — buttons

            var pnlKpiInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlKpiInner.Paint += PaintCardBorder;
            pnlKpiInner.Controls.Add(pnlKpiRow);

            var pnlKpiOuter = new Panel
            {
                Dock = DockStyle.Top, Height = 90,
                BackColor = Color.FromArgb(240, 244, 249), Padding = new Padding(20, 8, 20, 8)
            };
            pnlKpiOuter.Controls.Add(pnlKpiInner);

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  GRID TAB SWITCHER BAR  (DockStyle.Top, Height 69)
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            Color tabInactiveFg = Color.FromArgb(98, 112, 135);

            Button MakeTabBtn(string text)
            {
                var b = new Button
                {
                    Text      = text,
                    Font      = new Font("Segoe UI", 12f),
                    ForeColor = tabInactiveFg,
                    BackColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Dock      = DockStyle.Fill,
                    Cursor    = Cursors.Hand,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Padding   = new Padding(0, 0, 0, 3)
                };
                b.FlatAppearance.BorderSize         = 0;
                b.FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 248, 255);
                b.FlatAppearance.MouseDownBackColor = Color.FromArgb(235, 241, 255);
                return b;
            }

            btnTabReceipts = MakeTabBtn("\U0001F4E6  Goods Received Receipts");
            btnTabPO       = MakeTabBtn("\U0001F4CB  Purchase Orders");
            btnTabInvoices = MakeTabBtn("\U0001F4C4  Purchase Invoices");

            btnTabReceipts.Click += (s, e) => SwitchToGrid(0);
            btnTabPO.Click       += (s, e) => SwitchToGrid(1);
            btnTabInvoices.Click += (s, e) => SwitchToGrid(2);

            var tblTabs = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                RowCount    = 1,
                ColumnCount = 3,
                BackColor   = Color.White,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding     = new Padding(8, 0, 8, 0)
            };
            tblTabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            tblTabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            tblTabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            tblTabs.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblTabs.Controls.Add(btnTabReceipts, 0, 0);
            tblTabs.Controls.Add(btnTabPO,       1, 0);
            tblTabs.Controls.Add(btnTabInvoices, 2, 0);

            var pnlTabCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlTabCard.Paint += PaintCardBorder;
            pnlTabCard.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawLine(pen, 0, pnlTabCard.Height - 1, pnlTabCard.Width, pnlTabCard.Height - 1);
            };
            pnlTabCard.Controls.Add(tblTabs);

            var pnlTabOuter = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 69,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding   = new Padding(20, 4, 20, 0)
            };
            pnlTabOuter.Controls.Add(pnlTabCard);

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  SINGLE-GRID HOST PANEL  (DockStyle.Fill)
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            DataGridView MakeDgv() => new DataGridView
            {
                ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect   = false,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                GridColor       = Color.FromArgb(221, 227, 236),
                Font            = new Font("Segoe UI", 12f),
                AutoSizeColumnsMode     = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle         = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate             = { Height = 44 },
                Dock                    = DockStyle.Fill,
                ColumnHeadersHeight     = 40,
                EnableHeadersVisualStyles = false,
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

            Panel MakeSectionLabel(string text)
            {
                var p = new Panel
                {
                    Dock = DockStyle.Top, Height = 38,
                    BackColor = Color.FromArgb(246, 249, 255), Padding = new Padding(16, 0, 0, 0)
                };
                p.Paint += (o, ev) =>
                {
                    using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
                    ev.Graphics.DrawLine(pen, 0, ((Panel)o).Height - 1, ((Panel)o).Width, ((Panel)o).Height - 1);
                };
                p.Controls.Add(new Label
                {
                    Text = text, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(98, 112, 135), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
                });
                return p;
            }

            Panel WrapGridCard(DataGridView dgv, Panel sectionLabel)
            {
                var inner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
                inner.Paint += PaintCardBorder;
                inner.Controls.Add(dgv);
                inner.Controls.Add(sectionLabel);

                var outer = new Panel
                {
                    Dock      = DockStyle.Fill,
                    BackColor = Color.FromArgb(240, 244, 249),
                    Padding   = new Padding(20, 6, 20, 10),
                    Visible   = false
                };
                outer.Controls.Add(inner);
                return outer;
            }

            dgvReceipts = MakeDgv();
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRcptID",  HeaderText = "RECEIPT ID",   FillWeight = 13 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPOID",    HeaderText = "PO ID",        FillWeight = 13 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSupplier",HeaderText = "SUPPLIER",     FillWeight = 18 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMatID",   HeaderText = "MATERIAL ID",  FillWeight = 12 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItem",    HeaderText = "ITEM NAME",    FillWeight = 18 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQtyRcvd", HeaderText = "QTY RECEIVED", FillWeight =  9 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQtyOut",  HeaderText = "OUTSTANDING",  FillWeight =  9 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRcptDate",HeaderText = "RECEIPT DATE", FillWeight = 11 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colWH",      HeaderText = "WAREHOUSE",    FillWeight = 10 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPOSt",    HeaderText = "PO STATUS",    FillWeight = 11 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUP",      HeaderText = "UNIT PRICE",   FillWeight =  9 });
            dgvReceipts.SelectionChanged += dgvReceipts_SelectionChanged;
            dgvReceipts.CellFormatting   += dgvReceipts_CellFormatting;
            dgvReceipts.CellDoubleClick  += dgvReceipts_CellDoubleClick;

            dgvPO = MakeDgv();
            dgvPO.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPID",   HeaderText = "PO ID",      FillWeight = 18 });
            dgvPO.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPSup",  HeaderText = "SUPPLIER",   FillWeight = 28 });
            dgvPO.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPDate", HeaderText = "ORDER DATE", FillWeight = 18 });
            dgvPO.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPAmt",  HeaderText = "PO AMOUNT",  FillWeight = 18 });
            dgvPO.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPSt",   HeaderText = "STATUS",     FillWeight = 18 });
            dgvPO.SelectionChanged += dgvPO_SelectionChanged;
            dgvPO.CellFormatting   += dgvPO_CellFormatting;
            dgvPO.CellDoubleClick  += dgvPO_CellDoubleClick;

            dgvInvoices = MakeDgv();
            dgvInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "colInvID",   HeaderText = "INVOICE ID",     FillWeight = 18 });
            dgvInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "colInvPO",   HeaderText = "PO ID",          FillWeight = 15 });
            dgvInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "colInvSup",  HeaderText = "SUPPLIER",       FillWeight = 25 });
            dgvInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "colInvAmt",  HeaderText = "TOTAL AMOUNT",   FillWeight = 15 });
            dgvInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "colInvPay",  HeaderText = "PAYMENT STATUS", FillWeight = 15 });
            dgvInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "colInvDate", HeaderText = "EXPECTED DATE",  FillWeight = 12 });
            dgvInvoices.CellFormatting += dgvInvoices_CellFormatting;

            var pnlReceiptsCard = WrapGridCard(dgvReceipts, MakeSectionLabel("GOODS RECEIVED RECEIPTS"));
            var pnlPOCard       = WrapGridCard(dgvPO,       MakeSectionLabel("PURCHASE ORDERS"));
            var pnlInvoicesCard = WrapGridCard(dgvInvoices, MakeSectionLabel("PURCHASE INVOICES"));

            btnTabReceipts.Tag = pnlReceiptsCard;
            btnTabPO.Tag       = pnlPOCard;
            btnTabInvoices.Tag = pnlInvoicesCard;

            pnlGridHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };
            pnlGridHost.Controls.Add(pnlReceiptsCard);
            pnlGridHost.Controls.Add(pnlPOCard);
            pnlGridHost.Controls.Add(pnlInvoicesCard);

            // ════════════════════════════════════════════════════════════════
            // RULE 5 — Final assembly: Fill first, then Top (reverse display
            //          order), AppShell added LAST so it sits at the very top.
            //
            //  Windows Forms DockStyle stacking rule:
            //    Fill controls must be added FIRST (they claim remaining space).
            //    Top controls are added AFTER Fill; the LAST Top added sits
            //    visually at the top of the window.
            //
            //  Resulting visual stack (top → bottom):
            //    _shell          (TopNavBar 44 px + UserBar 72 px = 116 px)
            //    pnlSearchOuter  (Search card, 300 px)
            //    pnlKpiOuter     (KPI pills + action buttons, 90 px)
            //    pnlTabOuter     (Grid tab switcher, 69 px)
            //    pnlGridHost     (Fill — the three data grids)
            // ════════════════════════════════════════════════════════════════
            pnlMain.Controls.Add(pnlGridHost);    // DockStyle.Fill  — added first
            pnlMain.Controls.Add(pnlTabOuter);    // DockStyle.Top
            pnlMain.Controls.Add(pnlKpiOuter);    // DockStyle.Top
            pnlMain.Controls.Add(pnlSearchOuter); // DockStyle.Top
            pnlMain.Controls.Add(_shell);         // DockStyle.Top   — added LAST = topmost

            this.Controls.Add(pnlMain);

            // ════════════════════════════════════════════════════════════════
            // RULE 3: After ResumeLayout(false) + PerformLayout(), re-enforce
            //         _shell.Height and MinimumSize as a mandatory safety net
            //         against high-DPI / AutoScaleMode = Font side-effects.
            //         This mirrors the exact pattern used in ViewShipmentForm.
            // ════════════════════════════════════════════════════════════════
            this.ResumeLayout(false);
            this.PerformLayout();
            _shell.Height      = AppShell.TotalHeight;
            _shell.MinimumSize = new System.Drawing.Size(0, AppShell.TotalHeight);
        }

        // ── Button factories ─────────────────────────────────────────────────
        private Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(47, 111, 237), FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0; b.FlatAppearance.MouseOverBackColor = Color.FromArgb(26, 77, 192); b.FlatAppearance.MouseDownBackColor = Color.FromArgb(21, 60, 155); return b;
        }
        private Button MakeSuccessBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(5, 150, 105), FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0; b.FlatAppearance.MouseOverBackColor = Color.FromArgb(4, 120, 87); b.FlatAppearance.MouseDownBackColor = Color.FromArgb(3, 100, 70); return b;
        }
        private Button MakeWarningBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(180, 83, 9), FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0; b.FlatAppearance.MouseOverBackColor = Color.FromArgb(146, 64, 14); b.FlatAppearance.MouseDownBackColor = Color.FromArgb(120, 53, 15); return b;
        }
        private Button MakeOutlineBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White, FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236); b.FlatAppearance.BorderSize = 1; b.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249); return b;
        }

        // NOTE: PaintCardBorder defined in HandlingGoodsReceivedForm.cs — do NOT redefine here.
    }
}
