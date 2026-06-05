using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    partial class HandlingGoodsReceivedForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── AppShell (TopNavBar 44 px + UserBar 72 px = 116 px) ──────────
        // Events wired in Form_Load (Form.cs), NOT here — mirrors ViewShipmentForm.
        private AppShell _shell;

        // ── Search-bar controls ──────────────────────────────────────────
        private TextBox        txtKeyword;
        private ComboBox       cboStatus;
        private DateTimePicker dtpDateFrom;
        private CheckBox       chkDateFrom;
        private Button         btnSearch;
        private Button         btnReset;

        // ── KPI pills panel (populated at runtime by RenderKpi) ───────────
        private Panel pnlKpi;

        // ── Action buttons ───────────────────────────────────────────────
        private Button btnViewPODetail;
        private Button btnViewReceiptLines;
        private Button btnUploadReceipt;
        private Button btnRecordInvoice;

        // ── Data grids ───────────────────────────────────────────────────
        private DataGridView dgvReceipts;    // Tab 1 — Goods Received lines
        private DataGridView dgvPO;          // Tab 2 — Purchase Orders
        private DataGridView dgvInvoices;    // Tab 3 — Purchase Invoices

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            // ────────────────────────────────────────────────────────────
            // RULE 1  SuspendLayout() MUST be the absolute first statement.
            // ────────────────────────────────────────────────────────────
            this.SuspendLayout();

            // ────────────────────────────────────────────────────────────
            // RULE 2  Construct AppShell INSIDE SuspendLayout scope.
            //         Set Dock / Height / MinimumSize immediately.
            //  *** NO event wiring here — mirrors ViewShipmentForm.Designer ***
            //      MenuItemClicked and LogoutClicked are wired in Form_Load.
            // ────────────────────────────────────────────────────────────
            _shell             = new AppShell();
            _shell.Dock        = DockStyle.Top;
            _shell.Height      = AppShell.TotalHeight;               // 116 px
            _shell.MinimumSize = new Size(0, AppShell.TotalHeight);

            // ── Root panel ───────────────────────────────────────────────
            var pnlMain = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 244, 249)
            };

            // SetPopupContainer BEFORE _shell is added to any Controls collection.
            _shell.SetPopupContainer(pnlMain);

            // ============================================================
            //  SEARCH CARD  (DockStyle.Top, height = 270)
            // ============================================================

            txtKeyword = new TextBox
            {
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                Dock            = DockStyle.Fill,
                PlaceholderText = "Receipt ID / PO ID / Supplier"
            };
            txtKeyword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrids(); };

            cboStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 12f),
                Dock          = DockStyle.Fill
            };
            cboStatus.Items.AddRange(new object[]
                { "All", "Sent", "Partially Received", "Received", "Completed", "Cancelled" });
            cboStatus.SelectedIndex = 0;

            chkDateFrom = new CheckBox { Text = "", Width = 24, Checked = false, Cursor = Cursors.Hand };
            dtpDateFrom = new DateTimePicker
            {
                Format  = DateTimePickerFormat.Short,
                Value   = DateTime.Today.AddMonths(-1),
                Font    = new Font("Segoe UI", 12f),
                Enabled = false,
                Dock    = DockStyle.Fill
            };
            chkDateFrom.CheckedChanged += (s, e) => { dtpDateFrom.Enabled = chkDateFrom.Checked; };

            // ── MakeCell helper (mirrors ViewShipmentForm.Designer) ───────
            TableLayoutPanel MakeCell(string caption, Control ctrl, bool rightPad = true)
            {
                var t = new TableLayoutPanel
                {
                    Dock            = DockStyle.Fill,
                    RowCount        = 2,
                    ColumnCount     = 1,
                    BackColor       = Color.Transparent,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding         = rightPad ? new Padding(0, 0, 12, 0) : Padding.Empty
                };
                t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                t.RowStyles.Add(new RowStyle(SizeType.Absolute,  40f));
                t.RowStyles.Add(new RowStyle(SizeType.Percent,   70f));
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
                t.Controls.Add(lbl,  0, 0);
                t.Controls.Add(ctrl, 0, 1);
                return t;
            }

            // ── Date-From cell ────────────────────────────────────────────
            var cellDate = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                RowCount        = 2,
                ColumnCount     = 2,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            cellDate.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 33f));
            cellDate.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            cellDate.RowStyles.Add(new RowStyle(SizeType.Absolute,  40f));
            cellDate.RowStyles.Add(new RowStyle(SizeType.Percent,   70f));
            var lblDateCap = new Label
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
            cellDate.SetColumnSpan(lblDateCap, 2);
            cellDate.Controls.Add(lblDateCap,  0, 0);
            cellDate.Controls.Add(chkDateFrom, 0, 1);
            cellDate.Controls.Add(dtpDateFrom, 1, 1);

            // ── 3-column fields row ───────────────────────────────────────
            var tblFields = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 3,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            tblFields.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblFields.Controls.Add(MakeCell("Keyword (Receipt / PO / Supplier)", txtKeyword), 0, 0);
            tblFields.Controls.Add(MakeCell("PO Status",                         cboStatus),  1, 0);
            tblFields.Controls.Add(cellDate,                                                   2, 0);

            // ── Search / Reset buttons ────────────────────────────────────
            var pnlSearchBtns = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnSearch = MakePrimaryBtn ("\U0001f50d  Search", new Point(0,   0), 210, 60);
            btnReset  = MakeOutlineBtn ("↺  Reset",         new Point(218, 0), 210, 60);
            btnSearch.Click += (s, e) => RefreshGrids();
            btnReset.Click  += (s, e) => ResetFilters();
            pnlSearchBtns.Controls.Add(btnSearch);
            pnlSearchBtns.Controls.Add(btnReset);

            // ── Search card TLP ───────────────────────────────────────────
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
            tblSearchCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 120f));
            tblSearchCard.RowStyles.Add(new RowStyle(SizeType.Absolute,  65f));

            var pnlCardTitle = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlCardTitle.Controls.Add(new Label
            {
                Text      = "Search Goods Received",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            });
            pnlCardTitle.Controls.Add(new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 1,
                BackColor = Color.FromArgb(221, 227, 236)
            });

            tblSearchCard.Controls.Add(pnlCardTitle,  0, 0);
            tblSearchCard.Controls.Add(tblFields,     0, 1);
            tblSearchCard.Controls.Add(pnlSearchBtns, 0, 2);

            var pnlSearchWhite = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlSearchWhite.Paint += PaintCardBorder;
            pnlSearchWhite.Controls.Add(tblSearchCard);

            var pnlSearchOuter = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 270,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding   = new Padding(20, 14, 20, 8)
            };
            pnlSearchOuter.Controls.Add(pnlSearchWhite);

            // ============================================================
            //  KPI BAR  (DockStyle.Top, height = 90)
            //  Left: KPI pills (Fill)    Right: 4 action buttons (Right)
            // ============================================================
            pnlKpi = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(12, 10, 12, 10)
            };

            const int BW = 210; const int BH = 60; const int BG = 8; const int BP = 12;

            btnViewPODetail     = MakePrimaryBtn   ("\U0001f50d  View PO",       Point.Empty, BW, BH);
            btnViewReceiptLines = MakeSecondaryBtn ("\U0001f4cb  Receipt Lines", Point.Empty, BW, BH);
            btnUploadReceipt    = MakeWarningBtn   ("\U0001f4ce  Upload",         Point.Empty, BW, BH);
            btnRecordInvoice    = MakeSuccessBtn   ("\U0001f4c4  Record Invoice", Point.Empty, BW, BH);

            btnViewPODetail.Enabled     = false;
            btnViewReceiptLines.Enabled = false;
            btnUploadReceipt.Enabled    = false;
            btnRecordInvoice.Enabled    = false;

            btnViewPODetail.Click     += btnViewPODetail_Click;
            btnViewReceiptLines.Click += btnViewReceiptLines_Click;
            btnUploadReceipt.Click    += btnUploadReceipt_Click;
            btnRecordInvoice.Click    += btnRecordInvoice_Click;

            var pnlActions = new Panel
            {
                Dock      = DockStyle.Right,
                Width     = BP + (BW + BG) * 4 + BP,
                BackColor = Color.Transparent
            };
            void CentreActions()
            {
                int top = (pnlActions.Height - BH) / 2;
                if (top < 0) top = 0;
                btnViewPODetail.Location     = new Point(BP,                  top);
                btnViewReceiptLines.Location = new Point(BP + (BW + BG),     top);
                btnUploadReceipt.Location    = new Point(BP + (BW + BG) * 2, top);
                btnRecordInvoice.Location    = new Point(BP + (BW + BG) * 3, top);
            }
            pnlActions.Controls.Add(btnViewPODetail);
            pnlActions.Controls.Add(btnViewReceiptLines);
            pnlActions.Controls.Add(btnUploadReceipt);
            pnlActions.Controls.Add(btnRecordInvoice);
            pnlActions.Resize += (s, e) => CentreActions();

            var pnlKpiRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlKpiRow.Controls.Add(pnlKpi);     // DockStyle.Fill  — pills
            pnlKpiRow.Controls.Add(pnlActions); // DockStyle.Right — buttons

            var pnlKpiWhite = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlKpiWhite.Paint += PaintCardBorder;
            pnlKpiWhite.Controls.Add(pnlKpiRow);

            var pnlKpiOuter = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 90,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding   = new Padding(20, 8, 20, 8)
            };
            pnlKpiOuter.Controls.Add(pnlKpiWhite);

            // ============================================================
            //  GRID AREA  (DockStyle.Fill) — TabControl with 3 tabs
            // ============================================================

            DataGridView BuildGrid() => new DataGridView
            {
                ReadOnly               = true,
                AllowUserToAddRows     = false,
                AllowUserToDeleteRows  = false,
                RowHeadersVisible      = false,
                SelectionMode          = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect            = false,
                BackgroundColor        = Color.White,
                BorderStyle            = BorderStyle.None,
                GridColor              = Color.FromArgb(221, 227, 236),
                Font                   = new Font("Segoe UI", 13f),
                AutoSizeColumnsMode    = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle        = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate            = { Height = 48 },
                Dock                   = DockStyle.Fill,
                ColumnHeadersHeight    = 46,
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255),
                    ForeColor = Color.FromArgb(98,  112, 135),
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

            // Tab 1 — Receipt Lines
            dgvReceipts = BuildGrid();
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRID",  HeaderText = "RECEIPT ID",   FillWeight = 10 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPOIDr",HeaderText = "PO ID",        FillWeight =  9 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSup",  HeaderText = "SUPPLIER",     FillWeight = 14 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMat",  HeaderText = "MATERIAL ID",  FillWeight =  9 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItem", HeaderText = "ITEM NAME",    FillWeight = 14 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQtyR", HeaderText = "QTY RECEIVED", FillWeight =  8 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOut",  HeaderText = "OUTSTANDING",  FillWeight =  8 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate", HeaderText = "RECEIPT DATE", FillWeight = 10 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colWH",   HeaderText = "WAREHOUSE",    FillWeight = 10 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPOSt", HeaderText = "PO STATUS",    FillWeight = 10 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUnit", HeaderText = "UNIT PRICE",   FillWeight =  8 });
            dgvReceipts.SelectionChanged += dgvReceipts_SelectionChanged;
            dgvReceipts.CellFormatting   += dgvReceipts_CellFormatting;
            dgvReceipts.CellDoubleClick  += dgvReceipts_CellDoubleClick;

            // Tab 2 — Purchase Orders
            dgvPO = BuildGrid();
            dgvPO.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPID",  HeaderText = "PO ID",        FillWeight = 12 });
            dgvPO.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPSup", HeaderText = "SUPPLIER",     FillWeight = 22 });
            dgvPO.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPDate",HeaderText = "ORDER DATE",   FillWeight = 15 });
            dgvPO.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPAmt", HeaderText = "TOTAL AMOUNT", FillWeight = 18 });
            dgvPO.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPSt",  HeaderText = "STATUS",       FillWeight = 13 });
            dgvPO.SelectionChanged += dgvPO_SelectionChanged;
            dgvPO.CellFormatting   += dgvPO_CellFormatting;
            dgvPO.CellDoubleClick  += dgvPO_CellDoubleClick;

            // Tab 3 — Purchase Invoices
            dgvInvoices = BuildGrid();
            dgvInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "colInvID", HeaderText = "INVOICE ID",     FillWeight = 16 });
            dgvInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "colInvPO", HeaderText = "PO ID",          FillWeight = 14 });
            dgvInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "colInvSup",HeaderText = "SUPPLIER",       FillWeight = 22 });
            dgvInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "colInvAmt",HeaderText = "TOTAL AMOUNT",   FillWeight = 16 });
            dgvInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "colInvPay",HeaderText = "PAYMENT STATUS", FillWeight = 14 });
            dgvInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "colInvExp",HeaderText = "EXPECTED DATE",  FillWeight = 14 });
            dgvInvoices.CellFormatting += dgvInvoices_CellFormatting;

            // Section header label
            Label SectionLabel(string text) => new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Top,
                Height    = 36,
                Padding   = new Padding(10, 7, 0, 0),
                BackColor = Color.FromArgb(246, 249, 255)
            };

            Panel WrapCard(string title, DataGridView grid)
            {
                var inner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
                inner.Paint += PaintCardBorder;
                inner.Controls.Add(grid);
                inner.Controls.Add(SectionLabel(title));
                var outer = new Panel
                {
                    Dock      = DockStyle.Fill,
                    Padding   = new Padding(12, 6, 12, 6),
                    BackColor = Color.FromArgb(240, 244, 249)
                };
                outer.Controls.Add(inner);
                return outer;
            }

            var tabs = new TabControl
            {
                Dock       = DockStyle.Fill,
                Font       = new Font("Segoe UI", 11f),
                Appearance = TabAppearance.Normal,
                Padding    = new Point(14, 6)
            };
            var tabReceipts = new TabPage("  Receipt Lines  ")
                { BackColor = Color.FromArgb(240, 244, 249), Padding = Padding.Empty };
            var tabPO = new TabPage("  Purchase Orders  ")
                { BackColor = Color.FromArgb(240, 244, 249), Padding = Padding.Empty };
            var tabInvoices = new TabPage("  Purchase Invoices  ")
                { BackColor = Color.FromArgb(240, 244, 249), Padding = Padding.Empty };

            tabReceipts.Controls.Add(WrapCard("Goods Received — Receipt Lines", dgvReceipts));
            tabPO.Controls.Add(WrapCard("Purchase Orders",                      dgvPO));
            tabInvoices.Controls.Add(WrapCard("Purchase Invoices",              dgvInvoices));

            tabs.TabPages.Add(tabReceipts);
            tabs.TabPages.Add(tabPO);
            tabs.TabPages.Add(tabInvoices);

            var pnlGridOuter = new Panel
            {
                Dock      = DockStyle.Fill,
                Padding   = new Padding(20, 8, 20, 12),
                BackColor = Color.FromArgb(240, 244, 249)
            };
            pnlGridOuter.Controls.Add(tabs);

            // ============================================================
            //  RULE 5  pnlMain.Controls.Add ORDER — mirrors ViewShipmentForm
            //    1. DockStyle.Fill FIRST
            //    2. DockStyle.Top in reverse visual order
            //    3. _shell LAST → sits at absolute top of window
            // ============================================================
            pnlMain.Controls.Add(pnlGridOuter);   // Fill  — grid/tab area
            pnlMain.Controls.Add(pnlKpiOuter);    // Top   — KPI bar
            pnlMain.Controls.Add(pnlSearchOuter); // Top   — Search card
            pnlMain.Controls.Add(_shell);          // Top   — AppShell LAST

            // ── Form-level properties — mirror ViewShipmentForm exactly ──
            this.Text                = "Premium Living OPS — Handling Goods Received";
            this.Size                = new Size(1440, 900);
            this.MinimumSize         = new Size(1280, 800);
            this.StartPosition       = FormStartPosition.CenterScreen;
            this.BackColor           = Color.FromArgb(240, 244, 249);
            this.WindowState         = FormWindowState.Maximized;
            this.Font                = new Font("Segoe UI", 13f);
            this.AutoScaleMode       = AutoScaleMode.Font;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);

            this.Controls.Add(pnlMain);

            // ────────────────────────────────────────────────────────────
            // RULE 3  Re-enforce _shell height AFTER ResumeLayout + PerformLayout.
            //         AutoScaleMode = Font can silently shrink _shell during
            //         the first PerformLayout pass. These two lines are the
            //         final safety net that guarantees UserBar stays visible.
            // ────────────────────────────────────────────────────────────
            this.ResumeLayout(false);
            this.PerformLayout();
            _shell.Height      = AppShell.TotalHeight;               // 116 px — RULE 3
            _shell.MinimumSize = new Size(0, AppShell.TotalHeight);  // RULE 3
        }

        // ── Button factory helpers ────────────────────────────────────────
        private Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(47, 111, 237),
                FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(26,  77, 192);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(21,  60, 155);
            return b;
        }

        private Button MakeSecondaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(100, 116, 139),
                FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(71,  85, 105);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(51,  65,  85);
            return b;
        }

        private Button MakeWarningBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(245, 158, 11),
                FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(217, 119,  6);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(180,  90,  0);
            return b;
        }

        private Button MakeSuccessBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(5, 150, 105),
                FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(4, 120, 87);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(3, 100, 70);
            return b;
        }

        private Button MakeOutlineBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text = text, Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.White, FlatStyle = FlatStyle.Flat,
                Location = loc, Width = w, Height = h, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize  = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            return b;
        }
    }
}
