using PremiumLivingOPS.Views.Shared;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    partial class ViewShipmentForm
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        // ══════════════════════════════════════════════════════════════════════
        // AppShell wiring — canonical pattern (mirrors ViewOrderForm exactly)
        // ══════════════════════════════════════════════════════════════════════
        // RULE 1  SuspendLayout() is the FIRST statement in InitializeComponent.
        //         Every control is built while layout is suspended.
        // RULE 2  AppShell is constructed INSIDE SuspendLayout so AutoScaleMode
        //         cannot touch its height during PerformLayout.
        // RULE 3  After ResumeLayout/PerformLayout, _shell.Height is set AGAIN
        //         to AppShell.TotalHeight as a safety net against DPI scaling.
        // RULE 4  Events (MenuItemClicked, LogoutClicked) are subscribed HERE,
        //         ONCE.  The .cs Load handler must NOT re-subscribe them.
        // RULE 5  pnlMain.Controls add order:
        //           Add(pnlPage)  first  → DockStyle.Fill  (content)
        //           Add(_shell)   second → DockStyle.Top   (chrome, wins)
        //
        //   TopNavBar height = AppShell.NavBarHeight  =  44 px  (const in AppShell)
        //   UserBar   height = AppShell.UserBarHeight =  72 px  (const in AppShell)
        //   TotalHeight      = AppShell.TotalHeight   = 116 px
        // ══════════════════════════════════════════════════════════════════════

        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            // RULE 1 — suspend before touching any control
            SuspendLayout();

            // ── Page background ───────────────────────────────────────────────
            pnlPage = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = System.Drawing.Color.FromArgb(240, 244, 249),
                Padding   = new Padding(0)
            };

            // ── Scroll container ──────────────────────────────────────────────
            pnlScroll = new Panel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                BackColor  = System.Drawing.Color.FromArgb(240, 244, 249)
            };

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            // Card 1 — KPI Bar
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            var (kpiOuter, kpiInner) = CardPanel.Create(outerHeight: 102);
            kpiOuter.Dock = DockStyle.Top;
            pnlKpi = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = System.Drawing.Color.White,
                Padding   = new Padding(12, 10, 12, 10)
            };
            kpiInner.Controls.Add(pnlKpi);

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            // Card 2 — Filter / Search Bar
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            var (filterOuter, filterInner) = CardPanel.Create(
                outerHeight: 76, outerPadding: new Padding(20, 8, 20, 8));
            filterOuter.Dock = DockStyle.Top;

            lblSearchHint   = new Label  { Text = "Search:",  AutoSize = true, TextAlign = System.Drawing.ContentAlignment.MiddleLeft };
            txtSearch       = new TextBox { Size = new Size(220, 28) };
            lblStatusFilter = new Label  { Text = "Status:",  AutoSize = true, TextAlign = System.Drawing.ContentAlignment.MiddleLeft };
            cmbStatusFilter = new ComboBox { Size = new Size(160, 28), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatusFilter.Items.AddRange(new object[] { "All", "Pending", "In Transit", "Completed" });
            cmbStatusFilter.SelectedIndex = 0;

            lblFromDate = new Label { Text = "From:", AutoSize = true, TextAlign = System.Drawing.ContentAlignment.MiddleLeft };
            dtpFrom = new DateTimePicker { Size = new Size(150, 28), Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false };

            btnSearch = MakeButton("Search", new Size(88, 30), primary: true);
            btnSearch.Click += btnSearch_Click;
            btnReset  = MakeButton("Reset",  new Size(78, 30), primary: false);
            btnReset.Click  += btnReset_Click;

            lblRecordCount = new Label
            {
                AutoSize  = true,
                ForeColor = System.Drawing.Color.FromArgb(98, 112, 135),
                Font      = new Font("Segoe UI", 9.5f),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };

            var filterFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, AutoSize = false, Padding = new Padding(4, 10, 4, 0)
            };
            filterFlow.Controls.AddRange(new Control[]
            {
                lblSearchHint, Spacer(4), txtSearch, Spacer(14),
                lblStatusFilter, Spacer(4), cmbStatusFilter, Spacer(14),
                lblFromDate, Spacer(4), dtpFrom, Spacer(14),
                btnSearch, Spacer(6), btnReset, Spacer(18), lblRecordCount
            });
            filterInner.Controls.Add(filterFlow);

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            // Card 3 — Shipments Grid
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            var (gridOuter, gridInner) = CardPanel.Create(
                outerHeight: 380, outerPadding: new Padding(20, 8, 20, 8));
            gridOuter.Dock = DockStyle.Top;

            lblGridTitle = new Label
            {
                Text = "SHIPMENTS", AutoSize = true, Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Padding = new Padding(14, 10, 0, 6),
                ForeColor = System.Drawing.Color.FromArgb(98, 112, 135)
            };
            dgvShipments = MakeGrid();
            dgvShipments.SelectionMode    = DataGridViewSelectionMode.FullRowSelect;
            dgvShipments.SelectionChanged += dgvShipments_SelectionChanged;
            dgvShipments.CellFormatting   += dgvShipments_CellFormatting;
            AddCol(dgvShipments, "colShipmentID", "SHIPMENT ID",  90);
            AddCol(dgvShipments, "colOrderID",    "ORDER ID",      80);
            AddCol(dgvShipments, "colCustomer",   "CUSTOMER",     140);
            AddCol(dgvShipments, "colTracking",   "TRACKING NO.", 120);
            AddCol(dgvShipments, "colShipDate",   "SHIP DATE",     95);
            AddCol(dgvShipments, "colStatus",     "STATUS",        90);
            AddCol(dgvShipments, "colType",       "TYPE",          70);
            AddCol(dgvShipments, "colMethod",     "METHOD",        90);
            AddCol(dgvShipments, "colAmount",     "AMOUNT",        90);
            gridInner.Controls.Add(dgvShipments);
            gridInner.Controls.Add(lblGridTitle);

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            // Card 4 — Shipment Detail  (toggled visible)
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            var (detailOuter, detailInner) = CardPanel.Create(
                outerHeight: 460, outerPadding: new Padding(20, 8, 20, 8));
            pnlDetailOuter         = detailOuter;
            pnlDetailOuter.Dock    = DockStyle.Top;
            pnlDetailOuter.Visible = false;

            lblDetailTitle = new Label
            {
                Text = "SHIPMENT DETAIL", AutoSize = true, Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Padding = new Padding(14, 10, 0, 6),
                ForeColor = System.Drawing.Color.FromArgb(98, 112, 135)
            };

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true,
                ColumnCount = 4, RowCount = 5,
                BackColor = System.Drawing.Color.Transparent,
                Padding = new Padding(12, 0, 12, 10),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  50f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  50f));

            lblDetailShipmentID = new Label(); lblDetailOrderID  = new Label();
            lblDetailCustomer   = new Label(); lblDetailTracking = new Label();
            lblDetailStatus     = new Label(); lblDetailType     = new Label();
            lblDetailMethod     = new Label(); lblDetailShipDate = new Label();
            lblDetailAmount     = new Label(); lblDetailAddress  = new Label();

            AddInfoRow(tbl, 0, "Shipment ID:",  lblDetailShipmentID, "Order ID:",     lblDetailOrderID);
            AddInfoRow(tbl, 1, "Customer:",     lblDetailCustomer,   "Tracking No:",  lblDetailTracking);
            AddInfoRow(tbl, 2, "Status:",       lblDetailStatus,     "Type:",         lblDetailType);
            AddInfoRow(tbl, 3, "Method:",       lblDetailMethod,     "Ship Date:",    lblDetailShipDate);
            AddInfoRow(tbl, 4, "Total Amount:", lblDetailAmount,     "Ship Address:", lblDetailAddress);

            // ── Delivery Note sub-card ────────────────────────────────────────
            var (dnOuter, dnInner) = CardPanel.Create(
                outerHeight: 106, outerPadding: new Padding(12, 4, 12, 4));
            pnlDNOuter      = dnOuter;
            pnlDNOuter.Dock = DockStyle.Top;

            lblDNTitle = new Label
            {
                Text = "DELIVERY NOTE", AutoSize = true, Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Padding = new Padding(12, 8, 0, 4),
                ForeColor = System.Drawing.Color.FromArgb(98, 112, 135)
            };
            var tblDN = new TableLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, ColumnCount = 4,
                BackColor = System.Drawing.Color.Transparent,
                Padding = new Padding(12, 0, 12, 8),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  50f));
            tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  50f));
            lblDNID     = new Label();
            lblDNShipTo = new Label();
            lblDNDate   = new Label();
            lblDNOutQty = new Label();
            AddInfoRow(tblDN, 0, "Delivery ID:",   lblDNID,   "Ship To:",         lblDNShipTo);
            AddInfoRow(tblDN, 1, "Delivery Date:", lblDNDate, "Outstanding Qty:", lblDNOutQty);
            dnInner.Controls.Add(tblDN);
            dnInner.Controls.Add(lblDNTitle);

            // ── Shipment Lines sub-grid ───────────────────────────────────────
            lblLinesTitle = new Label
            {
                Text = "SHIPMENT LINES", AutoSize = true, Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Padding = new Padding(14, 8, 0, 4),
                ForeColor = System.Drawing.Color.FromArgb(98, 112, 135)
            };
            dgvLines        = MakeGrid();
            dgvLines.Height = 168;
            dgvLines.Dock   = DockStyle.Top;
            AddCol(dgvLines, "colLID",   "LINE ID",         100);
            AddCol(dgvLines, "colLItem", "ITEM ID",          90);
            AddCol(dgvLines, "colLName", "ITEM NAME",        220);
            AddCol(dgvLines, "colLQty",  "QTY SHIPPED",       90);
            AddCol(dgvLines, "colLOut",  "QTY OUTSTANDING",  110);

            // Dock.Top: last-added appears highest; declare bottom-to-top
            detailInner.Controls.Add(dgvLines);
            detailInner.Controls.Add(lblLinesTitle);
            detailInner.Controls.Add(pnlDNOuter);
            detailInner.Controls.Add(tbl);
            detailInner.Controls.Add(lblDetailTitle);

            // ── Assemble scroll panel (reverse Dock.Top order) ────────────────
            pnlScroll.Controls.Add(detailOuter);
            pnlScroll.Controls.Add(gridOuter);
            pnlScroll.Controls.Add(filterOuter);
            pnlScroll.Controls.Add(kpiOuter);
            pnlPage.Controls.Add(pnlScroll);

            // ── RULE 2 — AppShell built inside SuspendLayout ──────────────────
            // TopNavBar height  = AppShell.NavBarHeight  = 44 px
            // UserBar height    = AppShell.UserBarHeight = 72 px
            // Both are enforced by AppShell.OnLayout and TopNavBar.OnLayout.
            _shell = new AppShell();
            _shell.Dock        = DockStyle.Top;                  // explicit — never rely on constructor default alone
            _shell.Height      = AppShell.TotalHeight;           // 116 px
            _shell.MinimumSize = new System.Drawing.Size(0, AppShell.TotalHeight);

            // RULE 4 — subscribe events ONCE here; .cs Load must NOT repeat
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;

            // ── Root panel ────────────────────────────────────────────────────
            var pnlMain = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = System.Drawing.Color.FromArgb(240, 244, 249)
            };
            _shell.SetPopupContainer(pnlMain);

            // RULE 5 — Fill first, then Top (Top always wins)
            pnlMain.Controls.Add(pnlPage);   // DockStyle.Fill — content
            pnlMain.Controls.Add(_shell);    // DockStyle.Top  — chrome

            // ── Form settings ─────────────────────────────────────────────────
            Text          = "Logistics Processing – View Shipment";
            MinimumSize   = new System.Drawing.Size(1280, 800);
            WindowState   = FormWindowState.Maximized;
            BackColor     = System.Drawing.Color.FromArgb(240, 244, 249);
            Font          = new Font("Segoe UI", 9.5f);
            AutoScaleMode = AutoScaleMode.Font;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);

            Controls.Add(pnlMain);
            ResumeLayout(false);
            PerformLayout();

            // RULE 3 — re-enforce after PerformLayout (DPI safety net)
            _shell.Height      = AppShell.TotalHeight;
            _shell.MinimumSize = new System.Drawing.Size(0, AppShell.TotalHeight);
        }
        #endregion

        // ── Helpers ──────────────────────────────────────────────────────────
        private static Button MakeButton(string text, Size size, bool primary)
        {
            var btn = new Button
            {
                Text = text, Size = size,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9.5f),
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 1;
            if (primary)
            {
                btn.BackColor = System.Drawing.Color.FromArgb(47, 111, 237);
                btn.ForeColor = System.Drawing.Color.White;
                btn.FlatAppearance.BorderColor        = System.Drawing.Color.FromArgb(47, 111, 237);
                btn.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(29, 78, 216);
            }
            else
            {
                btn.BackColor = System.Drawing.Color.White;
                btn.ForeColor = System.Drawing.Color.FromArgb(47, 111, 237);
                btn.FlatAppearance.BorderColor        = System.Drawing.Color.FromArgb(47, 111, 237);
                btn.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(219, 234, 254);
            }
            return btn;
        }

        private static Panel Spacer(int w) =>
            new Panel { Width = w, Height = 1, BackColor = System.Drawing.Color.Transparent };

        internal static DataGridView MakeGrid()
        {
            var dgv = new DataGridView
            {
                Dock                      = DockStyle.Fill,
                ReadOnly                  = true,
                AllowUserToAddRows        = false,
                RowHeadersVisible         = false,
                AutoSizeColumnsMode       = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor           = System.Drawing.Color.White,
                BorderStyle               = BorderStyle.None,
                GridColor                 = System.Drawing.Color.FromArgb(221, 227, 236),
                EnableHeadersVisualStyles = false,
                CellBorderStyle           = DataGridViewCellBorderStyle.SingleHorizontal,
                SelectionMode             = DataGridViewSelectionMode.FullRowSelect,
                RowTemplate               = { Height = 42 }
            };
            dgv.ColumnHeadersHeight = 38;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(246, 249, 255);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(98, 112, 135);
            dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Padding   = new Padding(10, 0, 0, 0);
            dgv.DefaultCellStyle.BackColor              = System.Drawing.Color.White;
            dgv.DefaultCellStyle.ForeColor              = System.Drawing.Color.FromArgb(15, 31, 53);
            dgv.DefaultCellStyle.SelectionBackColor     = System.Drawing.Color.FromArgb(219, 234, 254);
            dgv.DefaultCellStyle.SelectionForeColor     = System.Drawing.Color.FromArgb(15, 31, 53);
            dgv.DefaultCellStyle.Padding                = new Padding(10, 4, 10, 4);
            dgv.DefaultCellStyle.Font                   = new Font("Segoe UI", 10f);
            return dgv;
        }

        private static void AddCol(DataGridView dgv, string name, string header, int weight)
            => dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = name, HeaderText = header,
                FillWeight = weight,
                SortMode   = DataGridViewColumnSortMode.Automatic
            });

        private static void AddInfoRow(
            TableLayoutPanel tbl, int row,
            string lbl1, Label val1, string lbl2, Label val2)
        {
            val1.Text = "—"; val1.AutoSize = true;
            val1.ForeColor = System.Drawing.Color.FromArgb(15, 31, 53);
            val1.Font      = new Font("Segoe UI", 10f, FontStyle.Bold);
            val2.Text = "—"; val2.AutoSize = true;
            val2.ForeColor = System.Drawing.Color.FromArgb(15, 31, 53);
            val2.Font      = new Font("Segoe UI", 10f, FontStyle.Bold);
            tbl.Controls.Add(MakeInfoKey(lbl1), 0, row);
            tbl.Controls.Add(val1,              1, row);
            tbl.Controls.Add(MakeInfoKey(lbl2), 2, row);
            tbl.Controls.Add(val2,              3, row);
        }

        private static Label MakeInfoKey(string text) => new Label
        {
            Text      = text,
            AutoSize  = true,
            ForeColor = System.Drawing.Color.FromArgb(98, 112, 135),
            Font      = new Font("Segoe UI", 9.5f),
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
            Padding   = new Padding(0, 2, 0, 2)
        };

        // ── Field declarations ────────────────────────────────────────────────
        private AppShell _shell;
        private Panel    pnlPage;
        private Panel    pnlScroll;
        private Panel    pnlKpi;
        private Panel    pnlDetailOuter;
        private Panel    pnlDNOuter;

        private Label          lblSearchHint;
        private TextBox        txtSearch;
        private Label          lblStatusFilter;
        private ComboBox       cmbStatusFilter;
        private Label          lblFromDate;
        private DateTimePicker dtpFrom;
        private Button         btnSearch;
        private Button         btnReset;
        private Label          lblRecordCount;

        private Label        lblGridTitle;
        private DataGridView dgvShipments;

        private Label lblDetailTitle;
        private Label lblDetailShipmentID;
        private Label lblDetailOrderID;
        private Label lblDetailCustomer;
        private Label lblDetailTracking;
        private Label lblDetailStatus;
        private Label lblDetailType;
        private Label lblDetailMethod;
        private Label lblDetailShipDate;
        private Label lblDetailAmount;
        private Label lblDetailAddress;

        private Label lblDNTitle;
        private Label lblDNID;
        private Label lblDNShipTo;
        private Label lblDNDate;
        private Label lblDNOutQty;

        private Label        lblLinesTitle;
        private DataGridView dgvLines;
    }
}
