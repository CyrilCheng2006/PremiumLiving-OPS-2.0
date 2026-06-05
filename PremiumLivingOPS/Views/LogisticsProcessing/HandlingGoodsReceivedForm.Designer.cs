using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    partial class HandlingGoodsReceivedForm
    {
        private System.ComponentModel.IContainer components = null;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  RULE 1 — AppShell declared as field (TopNavBar 44 px + UserBar 72 px = 116 px)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private AppShell _shell;

        // ── Filter bar controls ───────────────────────────────────────────────
        private TextBox        txtKeyword;
        private ComboBox       cboStatus;
        private DateTimePicker dtpDateFrom;
        private CheckBox       chkDateFrom;
        private Button         btnSearch;
        private Button         btnRefresh;

        // ── KPI bar ──────────────────────────────────────────────────────
        private Panel pnlKpi;

        // ── Action buttons ─────────────────────────────────────────────────
        private Button btnViewPODetail;
        private Button btnViewReceiptLines;
        private Button btnUploadReceipt;
        private Button btnRecordInvoice;

        // ── Three grids ────────────────────────────────────────────────────
        private DataGridView dgvReceipts;
        private DataGridView dgvPO;
        private DataGridView dgvInvoices;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // ── Form settings ────────────────────────────────────────────
            this.Text          = "Premium Living OPS — Handling Goods Received";
            this.Size          = new Size(1440, 900);
            this.MinimumSize   = new Size(1280, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = Color.FromArgb(240, 244, 249);
            this.WindowState   = FormWindowState.Maximized;
            this.Font          = new Font("Segoe UI", 13f);

            // ── Root panel ─────────────────────────────────────────────
            var pnlMain = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 244, 249)
            };

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  RULE 3 — AppShell: new AppShell() then SetPopupContainer(pnlMain)
            //            BEFORE adding to Controls.
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            _shell = new AppShell();
            _shell.SetPopupContainer(pnlMain);

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  Search / Filter card  (DockStyle.Top, Height 270)
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            txtKeyword = new TextBox
            {
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                Dock            = DockStyle.Fill,
                PlaceholderText = "PO ID / Supplier / Material"
            };
            txtKeyword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrids(); };

            cboStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 12f),
                Dock          = DockStyle.Fill
            };
            cboStatus.Items.AddRange(new object[]
            {
                "All", "Sent", "Partially Received", "Received", "Completed", "Cancelled"
            });
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

            // ── MakeCell helper ──────────────────────────────────────────
            TableLayoutPanel MakeCell(string caption, Control ctrl, bool rightPad = true)
            {
                var tlp = new TableLayoutPanel
                {
                    Dock            = DockStyle.Fill,
                    RowCount        = 2,
                    ColumnCount     = 1,
                    BackColor       = Color.Transparent,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding         = rightPad ? new Padding(0, 0, 12, 0) : Padding.Empty
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute,  40f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent,   70f));
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
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            tblFields.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblFields.Controls.Add(MakeCell("Keyword (PO / Supplier / Material)", txtKeyword), 0, 0);
            tblFields.Controls.Add(MakeCell("PO Status",  cboStatus),                          1, 0);
            tblFields.Controls.Add(cellDate,                                                    2, 0);

            var pnlBtns = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnSearch  = MakePrimaryBtn("\U0001F50D  Search", new Point(0,   0), 210, 60);
            btnRefresh = MakeOutlineBtn("↺  Reset",         new Point(218, 0), 210, 60);
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
            tblCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 120f));
            tblCard.RowStyles.Add(new RowStyle(SizeType.Absolute,  65f));

            var pnlTitleBar = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var lblTitle = new Label
            {
                Text      = "Search — Goods Received / Purchase Orders",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var divider = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 1,
                BackColor = Color.FromArgb(221, 227, 236)
            };
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
                Height    = 270,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding   = new Padding(20, 14, 20, 8)
            };
            pnlSearchOuter.Controls.Add(pnlCard);

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  KPI bar + 4 action buttons  (DockStyle.Top, Height 90)
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            pnlKpi = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(12, 10, 12, 10)
            };

            const int BtnW   = 230;
            const int BtnH   = 60;
            const int BtnGap = 8;
            const int BtnPad = 12;

            btnViewPODetail     = MakePrimaryBtn("\U0001F50D  PO Detail",     Point.Empty, BtnW, BtnH);
            btnViewReceiptLines = MakePrimaryBtn("\U0001F4CB  Receipt Lines",  Point.Empty, BtnW, BtnH);
            btnUploadReceipt    = MakeSuccessBtn("\U0001F4E4  Upload Receipt", Point.Empty, BtnW, BtnH);
            btnRecordInvoice    = MakeWarningBtn("\U0001F4C4  Record Invoice", Point.Empty, BtnW, BtnH);

            btnViewPODetail.Enabled     = false;
            btnViewReceiptLines.Enabled = false;
            btnUploadReceipt.Enabled    = false;
            btnRecordInvoice.Enabled    = false;

            btnViewPODetail.Click     += btnViewPODetail_Click;
            btnViewReceiptLines.Click += btnViewReceiptLines_Click;
            btnUploadReceipt.Click    += btnUploadReceipt_Click;
            btnRecordInvoice.Click    += btnRecordInvoice_Click;

            int actionPanelWidth = BtnPad + BtnW + BtnGap + BtnW + BtnGap + BtnW + BtnGap + BtnW + BtnPad;
            var pnlActionBtns = new Panel
            {
                Dock      = DockStyle.Right,
                Width     = actionPanelWidth,
                BackColor = Color.Transparent
            };

            void CentreActionBtns()
            {
                int top = (pnlActionBtns.Height - BtnH) / 2;
                if (top < 0) top = 0;
                btnViewPODetail.Location     = new Point(BtnPad, top);
                btnViewReceiptLines.Location = new Point(BtnPad + BtnW + BtnGap, top);
                btnUploadReceipt.Location    = new Point(BtnPad + (BtnW + BtnGap) * 2, top);
                btnRecordInvoice.Location    = new Point(BtnPad + (BtnW + BtnGap) * 3, top);
            }
            pnlActionBtns.Controls.Add(btnViewPODetail);
            pnlActionBtns.Controls.Add(btnViewReceiptLines);
            pnlActionBtns.Controls.Add(btnUploadReceipt);
            pnlActionBtns.Controls.Add(btnRecordInvoice);
            pnlActionBtns.Resize += (s, e) => CentreActionBtns();

            var pnlKpiRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlKpiRow.Controls.Add(pnlKpi);        // DockStyle.Fill  (added first)
            pnlKpiRow.Controls.Add(pnlActionBtns); // DockStyle.Right (added after Fill)

            var pnlKpiInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlKpiInner.Paint += PaintCardBorder;
            pnlKpiInner.Controls.Add(pnlKpiRow);

            var pnlKpiOuter = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 90,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding   = new Padding(20, 8, 20, 8)
            };
            pnlKpiOuter.Controls.Add(pnlKpiInner);

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  Three-grid section (DockStyle.Fill)
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

            DataGridView MakeDgv()
            {
                return new DataGridView
                {
                    ReadOnly                  = true,
                    AllowUserToAddRows        = false,
                    AllowUserToDeleteRows     = false,
                    RowHeadersVisible         = false,
                    SelectionMode             = DataGridViewSelectionMode.FullRowSelect,
                    MultiSelect               = false,
                    BackgroundColor           = Color.White,
                    BorderStyle               = BorderStyle.None,
                    GridColor                 = Color.FromArgb(221, 227, 236),
                    Font                      = new Font("Segoe UI", 12f),
                    AutoSizeColumnsMode       = DataGridViewAutoSizeColumnsMode.Fill,
                    CellBorderStyle           = DataGridViewCellBorderStyle.SingleHorizontal,
                    RowTemplate               = { Height = 44 },
                    Dock                      = DockStyle.Fill,
                    ColumnHeadersHeight       = 40,
                    EnableHeadersVisualStyles = false,
                    ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                    {
                        BackColor = Color.FromArgb(246, 249, 255),
                        ForeColor = Color.FromArgb(98, 112, 135),
                        Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
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
            }

            Panel MakeSectionLabel(string text)
            {
                var p = new Panel
                {
                    Dock      = DockStyle.Top,
                    Height    = 38,
                    BackColor = Color.FromArgb(246, 249, 255),
                    Padding   = new Padding(16, 0, 0, 0)
                };
                p.Paint += (o, ev) =>
                {
                    using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
                    ev.Graphics.DrawLine(pen, 0, ((Panel)o).Height - 1, ((Panel)o).Width, ((Panel)o).Height - 1);
                };
                p.Controls.Add(new Label
                {
                    Text      = text,
                    Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(98, 112, 135),
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                });
                return p;
            }

            Panel WrapInCard(Control content, Panel sectionLabel, DockStyle outerDock, int outerHeight = 0)
            {
                var inner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
                inner.Paint += PaintCardBorder;
                inner.Controls.Add(content);      // Fill — grid
                inner.Controls.Add(sectionLabel); // Top  — section header

                var outer = new Panel
                {
                    Dock      = outerDock,
                    BackColor = Color.FromArgb(240, 244, 249),
                    Padding   = new Padding(20, 6, 20, 6)
                };
                if (outerDock == DockStyle.Top) outer.Height = outerHeight;
                outer.Controls.Add(inner);
                return outer;
            }

            // ---- Grid 1: Goods Received Receipts ----------------------------
            dgvReceipts = MakeDgv();
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRcptID",  HeaderText = "RECEIPT ID",     FillWeight = 13 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPOID",    HeaderText = "PO ID",          FillWeight = 13 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSupplier",HeaderText = "SUPPLIER",       FillWeight = 18 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMatID",   HeaderText = "MATERIAL ID",    FillWeight = 12 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItem",    HeaderText = "ITEM NAME",      FillWeight = 18 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQtyRcvd", HeaderText = "QTY RECEIVED",   FillWeight =  9 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQtyOut",  HeaderText = "OUTSTANDING",    FillWeight =  9 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRcptDate",HeaderText = "RECEIPT DATE",   FillWeight = 11 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colWH",      HeaderText = "WAREHOUSE",      FillWeight = 10 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPOSt",    HeaderText = "PO STATUS",      FillWeight = 11 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUP",      HeaderText = "UNIT PRICE",     FillWeight =  9 });
            dgvReceipts.SelectionChanged += dgvReceipts_SelectionChanged;
            dgvReceipts.CellFormatting   += dgvReceipts_CellFormatting;
            dgvReceipts.CellDoubleClick  += dgvReceipts_CellDoubleClick;

            var pnlReceiptsOuter = WrapInCard(dgvReceipts, MakeSectionLabel("GOODS RECEIVED RECEIPTS"), DockStyle.Top, 300);

            // ---- Grid 2: Purchase Orders ------------------------------------
            dgvPO = MakeDgv();
            dgvPO.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPID",   HeaderText = "PO ID",      FillWeight = 18 });
            dgvPO.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPSup",  HeaderText = "SUPPLIER",   FillWeight = 28 });
            dgvPO.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPDate", HeaderText = "ORDER DATE", FillWeight = 18 });
            dgvPO.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPAmt",  HeaderText = "PO AMOUNT",  FillWeight = 18 });
            dgvPO.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPSt",   HeaderText = "STATUS",     FillWeight = 18 });
            dgvPO.SelectionChanged += dgvPO_SelectionChanged;
            dgvPO.CellFormatting   += dgvPO_CellFormatting;
            dgvPO.CellDoubleClick  += dgvPO_CellDoubleClick;

            var pnlPOOuter = WrapInCard(dgvPO, MakeSectionLabel("PURCHASE ORDERS"), DockStyle.Top, 240);

            // ---- Grid 3: Purchase Invoices (DockStyle.Fill) -----------------
            dgvInvoices = MakeDgv();
            dgvInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "colInvID",   HeaderText = "INVOICE ID",    FillWeight = 18 });
            dgvInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "colInvPO",   HeaderText = "PO ID",         FillWeight = 15 });
            dgvInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "colInvSup",  HeaderText = "SUPPLIER",      FillWeight = 25 });
            dgvInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "colInvAmt",  HeaderText = "TOTAL AMOUNT",  FillWeight = 15 });
            dgvInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "colInvPay",  HeaderText = "PAYMENT STATUS",FillWeight = 15 });
            dgvInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "colInvDate", HeaderText = "EXPECTED DATE", FillWeight = 12 });
            dgvInvoices.CellFormatting += dgvInvoices_CellFormatting;

            var pnlInvoicesOuter = WrapInCard(dgvInvoices, MakeSectionLabel("PURCHASE INVOICES"), DockStyle.Fill);

            // ── Grids container ─────────────────────────────────────────────
            var pnlGridsContainer = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 244, 249)
            };
            pnlGridsContainer.Controls.Add(pnlInvoicesOuter); // Fill — added first
            pnlGridsContainer.Controls.Add(pnlPOOuter);        // Top
            pnlGridsContainer.Controls.Add(pnlReceiptsOuter);  // Top

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  RULE 4 — Final assembly into pnlMain
            //  Fill panels first, then Top panels in reverse visual order.
            //  _shell (AppShell) added LAST as the topmost DockStyle.Top.
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            pnlMain.Controls.Add(pnlGridsContainer); // DockStyle.Fill
            pnlMain.Controls.Add(pnlKpiOuter);       // DockStyle.Top
            pnlMain.Controls.Add(pnlSearchOuter);    // DockStyle.Top
            pnlMain.Controls.Add(_shell);            // DockStyle.Top — AppShell (NavBar + UserBar)

            this.Controls.Add(pnlMain);

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  RULE 5 — ResumeLayout last; correct _shell height.
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            this.ResumeLayout(false);
            _shell.Height = AppShell.NavBarHeight + AppShell.UserBarHeight;
        }

        // ── Button factories (identical to ViewShipmentForm) ───────────────
        private Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(47, 111, 237),
                FlatStyle = FlatStyle.Flat,
                Location  = loc, Width = w, Height = h,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize         = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(26,  77, 192);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(21,  60, 155);
            return b;
        }

        private Button MakeSuccessBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(5, 150, 105),
                FlatStyle = FlatStyle.Flat,
                Location  = loc, Width = w, Height = h,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize         = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(4, 120, 87);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(3, 100, 70);
            return b;
        }

        private Button MakeWarningBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(180, 83, 9),
                FlatStyle = FlatStyle.Flat,
                Location  = loc, Width = w, Height = h,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize         = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(146, 64, 14);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(120, 53, 15);
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
                Location  = loc, Width = w, Height = h,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize         = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            return b;
        }

        // NOTE: PaintCardBorder is defined in HandlingGoodsReceivedForm.cs
        //       and shared across both partial class files via C# partial class.
        //       It must NOT be redefined here to avoid CS0111.
    }
}
