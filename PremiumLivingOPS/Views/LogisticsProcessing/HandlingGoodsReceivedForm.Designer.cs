using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    partial class HandlingGoodsReceivedForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── Shared shell (TopNavBar 44 px + UserBar 72 px = 116 px total) ──
        private AppShell _shell;

        // ── Filter bar controls ─────────────────────────────────────
        private TextBox        txtSearchKeyword;
        private ComboBox       cboStatus;
        private DateTimePicker dtpDateFrom;
        private CheckBox       chkDateFrom;
        private Button         btnSearch;
        private Button         btnRefresh;

        // ── KPI bar + action buttons ────────────────────────────
        private Panel  pnlKpi;
        private Button btnViewPODetail;
        private Button btnViewReceiptLines;

        // ── Grid 1: Receipts ────────────────────────────────────────
        private DataGridView dgvReceipts;

        // ── Grid 2: Purchase Orders ────────────────────────────────
        private DataGridView dgvPO;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            // RULE 1 — SuspendLayout MUST be the very first statement
            this.SuspendLayout();

            // ── AppShell — RULE 2: construct inside SuspendLayout scope ────
            _shell = new AppShell();
            _shell.Dock        = DockStyle.Top;
            _shell.Height      = AppShell.TotalHeight;
            _shell.MinimumSize = new Size(0, AppShell.TotalHeight);
            // RULE 4 — subscribe ONCE here; .cs Load must NOT re-subscribe
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;

            // ── Form settings ──────────────────────────────────────────
            this.Text            = "Premium Living OPS — Handling Goods Received";
            this.Size            = new Size(1440, 900);
            this.MinimumSize     = new Size(1200, 720);
            this.StartPosition   = FormStartPosition.CenterScreen;
            this.BackColor       = Color.FromArgb(240, 244, 249);
            this.WindowState     = FormWindowState.Maximized;
            this.Font            = new Font("Segoe UI", 13f);
            this.AutoScaleMode   = AutoScaleMode.Font;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);

            // ── Root panel ──────────────────────────────────────────────
            var pnlMain = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 244, 249)
            };

            // SetPopupContainer called after pnlMain is created (RULE 2 ordering)
            _shell.SetPopupContainer(pnlMain);

            // ════════════════════════════════════════════════════════════
            //  Search card  (DockStyle.Top, height 265)
            // ════════════════════════════════════════════════════════════
            txtSearchKeyword = new TextBox
            {
                Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill, PlaceholderText = "Receipt ID / PO ID / Supplier"
            };
            txtSearchKeyword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrids(); };

            cboStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 12f), Dock = DockStyle.Fill
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

            // ── MakeCell helper ─────────────────────────────────────────
            TableLayoutPanel MakeCell(string caption, Control ctrl, bool rightPad = true)
            {
                var tlp = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1,
                    BackColor = Color.Transparent,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding = rightPad ? new Padding(0, 0, 12, 0) : Padding.Empty
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent,  70f));
                var lbl = new Label
                {
                    Text = caption, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(98, 112, 135),
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft,
                    Padding = new Padding(0, 0, 0, 2)
                };
                ctrl.Dock = DockStyle.Fill;
                tlp.Controls.Add(lbl,  0, 0);
                tlp.Controls.Add(ctrl, 0, 1);
                return tlp;
            }

            // ── Date-From cell ───────────────────────────────────────────
            var cellDate = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 2,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            cellDate.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 33f));
            cellDate.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            cellDate.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            cellDate.RowStyles.Add(new RowStyle(SizeType.Percent,  70f));
            var lblDate = new Label
            {
                Text = "Date From", Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft,
                Padding = new Padding(0, 0, 0, 2)
            };
            chkDateFrom.Dock = DockStyle.Fill;
            dtpDateFrom.Dock = DockStyle.Fill;
            cellDate.SetColumnSpan(lblDate, 2);
            cellDate.Controls.Add(lblDate,     0, 0);
            cellDate.Controls.Add(chkDateFrom, 0, 1);
            cellDate.Controls.Add(dtpDateFrom, 1, 1);

            // ── 3-column fields TLP ─────────────────────────────────────
            var tblFields = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            tblFields.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblFields.Controls.Add(MakeCell("Keyword (Receipt / PO / Supplier)", txtSearchKeyword), 0, 0);
            tblFields.Controls.Add(MakeCell("PO Status",  cboStatus),  1, 0);
            tblFields.Controls.Add(cellDate,               2, 0);

            // ── Search / Reset buttons ─────────────────────────────────
            var pnlBtns = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnSearch  = MakePrimaryBtn("🔍  Search", new Point(0,   0), 210, 60);
            btnRefresh = MakeOutlineBtn("↺  Reset",   new Point(218, 0), 210, 60);
            btnSearch.Click  += (s, e) => RefreshGrids();
            btnRefresh.Click += (s, e) => ResetFilters();
            pnlBtns.Controls.Add(btnSearch);
            pnlBtns.Controls.Add(btnRefresh);

            // ── Search card TLP ─────────────────────────────────────────
            var tblCard = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(18, 14, 18, 14)
            };
            tblCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblCard.RowStyles.Add(new RowStyle(SizeType.Absolute,  60f));
            tblCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 120f));
            tblCard.RowStyles.Add(new RowStyle(SizeType.Absolute,  65f));

            var pnlTitle = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var lblTitle = new Label
            {
                Text = "Search Goods Received", Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            };
            var divider = new Panel
            {
                Dock = DockStyle.Bottom, Height = 1,
                BackColor = Color.FromArgb(221, 227, 236)
            };
            pnlTitle.Controls.Add(lblTitle);
            pnlTitle.Controls.Add(divider);
            tblCard.Controls.Add(pnlTitle,  0, 0);
            tblCard.Controls.Add(tblFields, 0, 1);
            tblCard.Controls.Add(pnlBtns,   0, 2);

            var pnlSearchCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlSearchCard.Paint += PaintCardBorder;
            pnlSearchCard.Controls.Add(tblCard);

            var pnlSearchOuter = new Panel
            {
                Dock = DockStyle.Top, Height = 265,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding = new Padding(20, 14, 20, 8)
            };
            pnlSearchOuter.Controls.Add(pnlSearchCard);

            // ════════════════════════════════════════════════════════════
            //  KPI bar + action buttons  (DockStyle.Top, height 90)
            // ════════════════════════════════════════════════════════════
            pnlKpi = new Panel
            {
                Dock = DockStyle.Fill, BackColor = Color.Transparent,
                Padding = new Padding(12, 10, 12, 10)
            };

            const int BtnW   = 270;
            const int BtnH   = 60;
            const int BtnGap = 8;
            const int BtnPad = 12;

            btnViewPODetail      = MakePrimaryBtn("🔍  View PO Detail",    Point.Empty, BtnW, BtnH);
            btnViewReceiptLines  = MakeSuccessBtn("📦  View Receipt Lines", Point.Empty, BtnW, BtnH);
            btnViewPODetail.Enabled     = false;
            btnViewReceiptLines.Enabled = false;
            btnViewPODetail.Click      += btnViewPODetail_Click;
            btnViewReceiptLines.Click  += btnViewReceiptLines_Click;

            var pnlActionBtns = new Panel
            {
                Dock      = DockStyle.Right,
                Width     = BtnPad + BtnW + BtnGap + BtnW + BtnPad,
                BackColor = Color.Transparent
            };

            void CentreActionBtns()
            {
                int top = (pnlActionBtns.Height - BtnH) / 2;
                if (top < 0) top = 0;
                btnViewPODetail.Location     = new Point(BtnPad, top);
                btnViewReceiptLines.Location = new Point(BtnPad + BtnW + BtnGap, top);
            }
            pnlActionBtns.Controls.Add(btnViewPODetail);
            pnlActionBtns.Controls.Add(btnViewReceiptLines);
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
                BackColor = Color.FromArgb(240, 244, 249),
                Padding = new Padding(20, 8, 20, 8)
            };
            pnlKpiOuter.Controls.Add(pnlKpiInner);

            // ════════════════════════════════════════════════════════════
            //  Dual Grid via SplitContainer
            // ════════════════════════════════════════════════════════════
            dgvReceipts = BuildDataGrid();
            dgvReceipts.Name = "dgvReceipts";
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colReceiptID",   HeaderText = "RECEIPT ID",   FillWeight = 10 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPOID_R",      HeaderText = "PO ID",        FillWeight =  9 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSupplier_R",  HeaderText = "SUPPLIER",     FillWeight = 14 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMaterialID",  HeaderText = "MATERIAL ID",  FillWeight =  9 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItemName",    HeaderText = "ITEM NAME",    FillWeight = 14 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQtyReceived", HeaderText = "QTY RECEIVED", FillWeight =  9 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOutstanding", HeaderText = "OUTSTANDING",  FillWeight =  9 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colReceiptDate", HeaderText = "RECEIPT DATE", FillWeight = 10 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colWarehouse",   HeaderText = "WAREHOUSE",    FillWeight = 10 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPOStatus_R",  HeaderText = "PO STATUS",    FillWeight = 10 });
            dgvReceipts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUnitPrice",   HeaderText = "UNIT PRICE",   FillWeight =  8 });
            dgvReceipts.SelectionChanged += dgvReceipts_SelectionChanged;
            dgvReceipts.CellFormatting   += dgvReceipts_CellFormatting;
            dgvReceipts.CellDoubleClick  += dgvReceipts_CellDoubleClick;

            dgvPO = BuildDataGrid();
            dgvPO.Name = "dgvPO";
            dgvPO.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPurchaseID",  HeaderText = "PO ID",        FillWeight = 12 });
            dgvPO.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSupplier_PO", HeaderText = "SUPPLIER",     FillWeight = 22 });
            dgvPO.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrderDate",   HeaderText = "ORDER DATE",   FillWeight = 15 });
            dgvPO.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotalAmount", HeaderText = "TOTAL AMOUNT", FillWeight = 18 });
            dgvPO.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPOStatus",    HeaderText = "STATUS",       FillWeight = 13 });
            dgvPO.SelectionChanged += dgvPO_SelectionChanged;
            dgvPO.CellFormatting   += dgvPO_CellFormatting;
            dgvPO.CellDoubleClick  += dgvPO_CellDoubleClick;

            // ── Grid section header labels ──────────────────────────────────
            Label MakeSectionLabel(string text) => new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Top,
                Height    = 34,
                Padding   = new Padding(8, 6, 0, 0),
                BackColor = Color.FromArgb(246, 249, 255)
            };

            // ── Receipts card ──────────────────────────────────────────────
            var pnlReceiptsInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlReceiptsInner.Paint += PaintCardBorder;
            pnlReceiptsInner.Controls.Add(dgvReceipts);
            pnlReceiptsInner.Controls.Add(MakeSectionLabel("Goods Received — Receipt Lines"));

            var pnlReceiptsOuter = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 10, 20, 4),
                BackColor = Color.FromArgb(240, 244, 249)
            };
            pnlReceiptsOuter.Controls.Add(pnlReceiptsInner);

            // ── PO card ────────────────────────────────────────────────────
            var pnlPOInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlPOInner.Paint += PaintCardBorder;
            pnlPOInner.Controls.Add(dgvPO);
            pnlPOInner.Controls.Add(MakeSectionLabel("Purchase Orders"));

            var pnlPOOuter = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 4, 20, 0),
                BackColor = Color.FromArgb(240, 244, 249)
            };
            pnlPOOuter.Controls.Add(pnlPOInner);

            // ── SplitContainer: top = Receipts, bottom = PO ─────────────────
            var split = new SplitContainer
            {
                Dock        = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 350,
                SplitterWidth    = 6,
                BackColor = Color.FromArgb(240, 244, 249),
                Panel1MinSize = 120,
                Panel2MinSize = 80
            };
            split.Panel1.Controls.Add(pnlReceiptsOuter);
            split.Panel2.Controls.Add(pnlPOOuter);

            var pnlGridCard = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 244, 249)
            };
            pnlGridCard.Controls.Add(split);

            // ════════════════════════════════════════════════════════════
            //  Assemble pnlMain — RULE 5: Fill first, Top last
            // ════════════════════════════════════════════════════════════
            pnlMain.Controls.Add(pnlGridCard);     // Fill  — grids
            pnlMain.Controls.Add(pnlKpiOuter);     // Top   — KPI bar + action buttons
            pnlMain.Controls.Add(pnlSearchOuter);  // Top   — Search card
            pnlMain.Controls.Add(_shell);          // Top   — nav chrome (last = topmost)

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
            this.PerformLayout();

            // RULE 3 — re-enforce shell height after layout pass
            _shell.Height      = AppShell.TotalHeight;
            _shell.MinimumSize = new Size(0, AppShell.TotalHeight);
        }

        // ── DataGrid factory ────────────────────────────────────────────
        private static DataGridView BuildDataGrid() => new DataGridView
        {
            ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
            GridColor = Color.FromArgb(221, 227, 236),
            Font = new Font("Segoe UI", 13f),
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            RowTemplate = { Height = 48 },
            Dock = DockStyle.Fill,
            ColumnHeadersHeight = 46,
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

        // ── Button factories ─────────────────────────────────────────
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
                Text = text, Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.White, FlatStyle = FlatStyle.Flat,
                Location = loc, Width = w, Height = h, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize  = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            return b;
        }
        // NOTE: PaintCardBorder is defined in HandlingGoodsReceivedForm.cs (static)
        //       Do NOT redeclare it here — that would cause CS0111.
    }
}
