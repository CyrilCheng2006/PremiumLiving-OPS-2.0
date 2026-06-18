using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    partial class HandlingGoodsReceivedForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── AppShell (TopNavBar 44 px + UserBar 72 px = 116 px total) ─────────
        private AppShell _shell;

        // ── Filter bar controls ──────────────────────────────────────────────────
        private TextBox        txtKeyword;
        private ComboBox       cboStatus;
        private DateTimePicker dtpDateFrom;
        private CheckBox       chkDateFrom;
        private Button         btnSearch;
        private Button         btnRefresh;

        // ── KPI bar + action buttons ──────────────────────────────────────────
        private Panel  pnlKpi;
        private Button btnViewPODetail;
        private Button btnViewReceiptLines;
        private Button btnUploadReceipt;
        private Button btnRecordInvoice;

        // ── Grid tab switcher ───────────────────────────────────────────────────
        private Button btnTabReceipts;
        private Button btnTabPO;
        private Button btnTabInvoices;

        // ── Three grids ───────────────────────────────────────────────────────
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
            // ── Identical opening to ViewShipmentForm ──────────────────────
            this.SuspendLayout();

            this.Text          = "Premium Living OPS — Handling Goods Received";
            this.Size          = new Size(1440, 900);
            this.MinimumSize   = new Size(1200, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = Color.FromArgb(240, 244, 249);
            this.WindowState   = FormWindowState.Maximized;
            this.Font          = new Font("Segoe UI", 13f);
            // NOTE: No AutoScaleMode / AutoScaleDimensions here.
            //       ViewShipmentForm omits them; omitting them prevents
            //       WinForms font-scaling from recalculating _shell.Height
            //       after ResumeLayout and collapsing the UserBar.

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  Root panel  (matches ViewShipmentForm exactly)
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            var pnlMain = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 244, 249)
            };

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  AppShell  — constructed and wired via SetPopupContainer only.
            //  Do NOT set _shell.Dock / .Height / .MinimumSize manually here.
            //  AppShell overrides OnLayout and ScaleControl to lock its own
            //  height at 116 px; any external height assignment before
            //  ResumeLayout fights those overrides and can collapse UserBar.
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            _shell = new AppShell();
            _shell.SetPopupContainer(pnlMain);

            // Event subscriptions — ONCE here, NEVER in .cs Load (RULE 4)
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  Search card  (DockStyle.Top, Height 300)
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
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
                    ForeColor = Color.FromArgb(98, 112, 135),
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft,
                    Padding = new Padding(0, 0, 0, 2)
                };
                ctrl.Dock = DockStyle.Fill;
                tlp.Controls.Add(lbl,  0, 0);
                tlp.Controls.Add(ctrl, 0, 1);
                return tlp;
            }

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

            var tblFields = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
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
            btnRefresh = MakeOutlineBtn("\u21BA  Reset",      new Point(218, 0), 210, 60);
            btnSearch.Click  += (s, e) => RefreshGrids();
            btnRefresh.Click += (s, e) => ResetFilters();
            pnlBtns.Controls.Add(btnSearch);
            pnlBtns.Controls.Add(btnRefresh);

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
            var lblTitle = new Label
            {
                Text = "Search Goods Received", Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            };
            var divider = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(221, 227, 236) };
            pnlTitle.Controls.Add(lblTitle);
            pnlTitle.Controls.Add(divider);
            tblCard.Controls.Add(pnlTitle,  0, 0);
            tblCard.Controls.Add(tblFields, 0, 1);
            tblCard.Controls.Add(pnlBtns,   0, 2);

            var pnlCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlCard.Paint += PaintCardBorder;
            pnlCard.Controls.Add(tblCard);

            var pnlSearchOuter = new Panel
            {
                Dock = DockStyle.Top, Height = 300,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding = new Padding(20, 14, 20, 8)
            };
            pnlSearchOuter.Controls.Add(pnlCard);

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  KPI bar + 4 action buttons  (DockStyle.Top, Height 90)
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            pnlKpi = new Panel
            {
                Dock = DockStyle.Fill, BackColor = Color.Transparent,
                Padding = new Padding(12, 10, 12, 10)
            };

            const int BtnW   = 230;
            const int BtnH   = 60;
            const int BtnGap = 8;
            const int BtnPad = 12;

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

            int actionPanelWidth = BtnPad + (BtnW + BtnGap) * 4 - BtnGap + BtnPad;
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
            pnlKpiRow.Controls.Add(pnlKpi);
            pnlKpiRow.Controls.Add(pnlActionBtns);

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

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  Grid tab switcher bar  (DockStyle.Top, Height 69)
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            Button MakeTabBtn(string text)
            {
                var b = new Button
                {
                    Text      = text,
                    Font      = new Font("Segoe UI", 12f),
                    ForeColor = Color.FromArgb(98, 112, 135),
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
                Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 3,
                BackColor = Color.White, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(8, 0, 8, 0)
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
            pnlTabCard.Controls.Add(tblTabs);

            var pnlTabOuter = new Panel
            {
                Dock = DockStyle.Top, Height = 69,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding = new Padding(20, 4, 20, 0)
            };
            pnlTabOuter.Controls.Add(pnlTabCard);

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  Grid host panel  (DockStyle.Fill — three grids, one visible)
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            DataGridView MakeDgv() => new DataGridView
            {
                ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect   = false,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                GridColor       = Color.FromArgb(221, 227, 236),
                Font            = new Font("Segoe UI", 13f),
                AutoSizeColumnsMode     = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle         = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate             = { Height = 48 },
                Dock                    = DockStyle.Fill,
                ColumnHeadersHeight     = 46,
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

            Panel WrapGridCard(DataGridView dgv, string sectionLabel, out Panel outerPanel)
            {
                var hdrPanel = new Panel
                {
                    Dock = DockStyle.Top, Height = 38,
                    BackColor = Color.FromArgb(246, 249, 255), Padding = new Padding(16, 0, 0, 0)
                };
                hdrPanel.Paint += (o, ev) =>
                {
                    using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                    ev.Graphics.DrawLine(pen, 0, ((Panel)o).Height - 1, ((Panel)o).Width, ((Panel)o).Height - 1);
                };
                hdrPanel.Controls.Add(new Label
                {
                    Text = sectionLabel, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(98, 112, 135), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
                });

                var inner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
                inner.Paint += PaintCardBorder;
                inner.Controls.Add(dgv);
                inner.Controls.Add(hdrPanel);

                outerPanel = new Panel
                {
                    Dock      = DockStyle.Fill,
                    BackColor = Color.FromArgb(240, 244, 249),
                    Padding   = new Padding(20, 6, 20, 10),
                    Visible   = false
                };
                outerPanel.Controls.Add(inner);
                return outerPanel;
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

            Panel pnlReceiptsCard, pnlPOCard, pnlInvoicesCard;
            WrapGridCard(dgvReceipts, "GOODS RECEIVED RECEIPTS", out pnlReceiptsCard);
            WrapGridCard(dgvPO,       "PURCHASE ORDERS",         out pnlPOCard);
            WrapGridCard(dgvInvoices, "PURCHASE INVOICES",       out pnlInvoicesCard);

            btnTabReceipts.Tag = pnlReceiptsCard;
            btnTabPO.Tag       = pnlPOCard;
            btnTabInvoices.Tag = pnlInvoicesCard;

            var pnlGridHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };
            pnlGridHost.Controls.Add(pnlReceiptsCard);
            pnlGridHost.Controls.Add(pnlPOCard);
            pnlGridHost.Controls.Add(pnlInvoicesCard);

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  Assemble — Fill first, Top sections reverse-order, _shell LAST
            //
            //  Mirrors ViewShipmentForm exactly:
            //    pnlMain.Controls.Add(pnlGridCard);    // Fill
            //    pnlMain.Controls.Add(pnlKpiOuter);    // Top
            //    pnlMain.Controls.Add(pnlSearchOuter); // Top
            //    pnlMain.Controls.Add(_shell);          // Top — topmost
            //
            //  Extra Top layers for tab switcher inserted between KPI and grid:
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            pnlMain.Controls.Add(pnlGridHost);    // DockStyle.Fill  — added first
            pnlMain.Controls.Add(pnlTabOuter);    // DockStyle.Top
            pnlMain.Controls.Add(pnlKpiOuter);    // DockStyle.Top
            pnlMain.Controls.Add(pnlSearchOuter); // DockStyle.Top
            pnlMain.Controls.Add(_shell);          // DockStyle.Top   — LAST = topmost

            this.Controls.Add(pnlMain);

            // NOTE: ResumeLayout(false) only — NO PerformLayout() call.
            //       ViewShipmentForm ends here; we mirror that exactly.
            //       PerformLayout() triggers AutoScaleMode font-scaling which
            //       recalculates and can collapse AppShell.Height.
            this.ResumeLayout(false);
        }

        // ── Button factories (identical to ViewShipmentForm) ───────────────────
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

        private Button MakeWarningBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(217, 119, 6),
                FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(180, 95, 4);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(146, 75, 2);
            return b;
        }

        private Button MakeSuccessBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(22, 163, 74),
                FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(16, 131, 58);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(10, 100, 40);
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
            b.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize         = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            return b;
        }

        private static void PaintCardBorder(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            var p = (Panel)sender;
            using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        }
    }
}
