using PremiumLivingOPS.Views.Shared;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    partial class HandlingGoodsReceivedForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── Field declarations ────────────────────────────────────────────────
        private AppShell _shell;
        private Panel    pnlPage;
        private Panel    pnlScroll;
        private Panel    pnlKpi;

        private Label          lblSearchHint;
        private TextBox        txtSearch;
        private Label          lblStatusFilter;
        private ComboBox       cmbStatusFilter;
        private Label          lblFromDate;
        private DateTimePicker dtpFrom;
        private Button         btnSearch;
        private Button         btnReset;
        private Label          lblReceiptCount;

        private Label        lblReceiptsTitle;
        private DataGridView dgvReceipts;

        private Label        lblPOTitle;
        private DataGridView dgvPO;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            // ── Form settings ─────────────────────────────────────────────────
            this.Text                = "Logistics Processing – Handling Goods Received";
            this.Size                = new Size(1440, 900);
            this.MinimumSize         = new Size(1280, 800);
            this.StartPosition       = FormStartPosition.CenterScreen;
            this.BackColor           = Color.FromArgb(240, 244, 249);
            this.WindowState         = FormWindowState.Maximized;
            this.Font                = new Font("Segoe UI", 9.5f);
            this.AutoScaleMode       = AutoScaleMode.Font;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);

            // ── Root panel ───────────────────────────────────────────────────
            var pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };

            // ── AppShell ─────────────────────────────────────────────────────
            // AppShell composes TopNavBar (44 px) + UserBar (72 px) = 116 px.
            // Both children self-lock their heights via their own OnLayout.
            // SetPopupContainer must be called before adding to pnlMain so
            // dropdown menus render inside pnlMain rather than the Form root.
            _shell = new AppShell();
            _shell.SetPopupContainer(pnlMain);
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;

            // ── Page area ───────────────────────────────────────────────────
            pnlPage   = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };
            pnlScroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(240, 244, 249) };

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            // Card 1 — KPI Bar
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            var (kpiOuter, kpiInner) = CardPanel.Create(outerHeight: 102);
            kpiOuter.Dock = DockStyle.Top;
            pnlKpi = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.White,
                Padding   = new Padding(12, 10, 12, 10)
            };
            kpiInner.Controls.Add(pnlKpi);

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            // Card 2 — Filter / Search Bar
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            var (filterOuter, filterInner) = CardPanel.Create(
                outerHeight: 76, outerPadding: new Padding(20, 8, 20, 8));
            filterOuter.Dock = DockStyle.Top;

            lblSearchHint   = new Label  { Text = "Search:",    AutoSize = true, TextAlign = System.Drawing.ContentAlignment.MiddleLeft };
            txtSearch       = new TextBox { Size = new Size(220, 28) };
            lblStatusFilter = new Label  { Text = "PO Status:", AutoSize = true, TextAlign = System.Drawing.ContentAlignment.MiddleLeft };
            cmbStatusFilter = new ComboBox { Size = new Size(180, 28), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatusFilter.Items.AddRange(new object[]
            {
                "All", "Sent", "Partially Received", "Received", "Completed", "Cancelled"
            });
            cmbStatusFilter.SelectedIndex = 0;

            lblFromDate = new Label { Text = "From:", AutoSize = true, TextAlign = System.Drawing.ContentAlignment.MiddleLeft };
            dtpFrom     = new DateTimePicker { Size = new Size(150, 28), Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false };

            btnSearch = MakeButton("Search", new Size(88, 30), primary: true);
            btnSearch.Click += btnSearch_Click;
            btnReset  = MakeButton("Reset",  new Size(78, 30), primary: false);
            btnReset.Click  += btnReset_Click;

            lblReceiptCount = new Label
            {
                AutoSize  = true,
                ForeColor = Color.FromArgb(98, 112, 135),
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
                btnSearch, Spacer(6), btnReset, Spacer(18), lblReceiptCount
            });
            filterInner.Controls.Add(filterFlow);

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            // Card 3 — Goods Received Records Grid
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            var (rcvOuter, rcvInner) = CardPanel.Create(
                outerHeight: 400, outerPadding: new Padding(20, 8, 20, 8));
            rcvOuter.Dock = DockStyle.Top;

            lblReceiptsTitle = new Label
            {
                Text      = "GOODS RECEIVED RECORDS",
                AutoSize  = true,
                Dock      = DockStyle.Top,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Padding   = new Padding(14, 10, 0, 6),
                ForeColor = Color.FromArgb(98, 112, 135)
            };
            dgvReceipts = MakeGrid();
            dgvReceipts.CellFormatting += dgvReceipts_CellFormatting;
            AddCol(dgvReceipts, "colRID",        "RECEIPT ID",      90);
            AddCol(dgvReceipts, "colRPO",        "PO ID",            90);
            AddCol(dgvReceipts, "colRSupplier",  "SUPPLIER",        140);
            AddCol(dgvReceipts, "colRMatID",     "MATERIAL ID",     100);
            AddCol(dgvReceipts, "colRMatName",   "MATERIAL NAME",   150);
            AddCol(dgvReceipts, "colRQtyRcv",    "QTY RECEIVED",     90);
            AddCol(dgvReceipts, "colROutQty",    "OUTSTANDING",      90);
            AddCol(dgvReceipts, "colRDate",      "RECEIPT DATE",    100);
            AddCol(dgvReceipts, "colRWarehouse", "WAREHOUSE",       140);
            AddCol(dgvReceipts, "colRStatus",    "PO STATUS",       110);
            AddCol(dgvReceipts, "colRUnitPrice", "UNIT PRICE",       90);
            rcvInner.Controls.Add(dgvReceipts);
            rcvInner.Controls.Add(lblReceiptsTitle);

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            // Card 4 — Purchase Orders Grid
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            var (poOuter, poInner) = CardPanel.Create(
                outerHeight: 300, outerPadding: new Padding(20, 8, 20, 14));
            poOuter.Dock = DockStyle.Top;

            lblPOTitle = new Label
            {
                Text      = "PURCHASE ORDERS",
                AutoSize  = true,
                Dock      = DockStyle.Top,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Padding   = new Padding(14, 10, 0, 6),
                ForeColor = Color.FromArgb(98, 112, 135)
            };
            dgvPO = MakeGrid();
            dgvPO.CellFormatting += dgvPO_CellFormatting;
            AddCol(dgvPO, "colPOID",       "PO ID",         100);
            AddCol(dgvPO, "colPOSupplier", "SUPPLIER",       180);
            AddCol(dgvPO, "colPODate",     "ORDER DATE",     110);
            AddCol(dgvPO, "colPOTotal",    "TOTAL AMOUNT",   120);
            AddCol(dgvPO, "colPOStatus",   "STATUS",         120);
            poInner.Controls.Add(dgvPO);
            poInner.Controls.Add(lblPOTitle);

            // ── Assemble scroll panel (reverse Dock.Top order) ─────────────────
            pnlScroll.Controls.Add(poOuter);
            pnlScroll.Controls.Add(rcvOuter);
            pnlScroll.Controls.Add(filterOuter);
            pnlScroll.Controls.Add(kpiOuter);
            pnlPage.Controls.Add(pnlScroll);

            // ── Wire pnlMain ────────────────────────────────────────────────────
            // DockStyle.Top controls stack in add-order; _shell added last sits
            // at the very top (same pattern as ViewOrderForm).
            pnlMain.Controls.Add(pnlPage);   // DockStyle.Fill — content
            pnlMain.Controls.Add(_shell);    // DockStyle.Top  — chrome (wins)

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private static Button MakeButton(string text, Size size, bool primary)
        {
            var btn = new Button
            {
                Text      = text,
                Size      = size,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9.5f),
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 1;
            if (primary)
            {
                btn.BackColor                         = Color.FromArgb(47, 111, 237);
                btn.ForeColor                         = Color.White;
                btn.FlatAppearance.BorderColor        = Color.FromArgb(47, 111, 237);
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(29,  78, 216);
            }
            else
            {
                btn.BackColor                         = Color.White;
                btn.ForeColor                         = Color.FromArgb(47, 111, 237);
                btn.FlatAppearance.BorderColor        = Color.FromArgb(47, 111, 237);
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(219, 234, 254);
            }
            return btn;
        }

        private static Panel Spacer(int w) =>
            new Panel { Width = w, Height = 1, BackColor = Color.Transparent };

        private static DataGridView MakeGrid()
        {
            var dgv = new DataGridView
            {
                Dock                      = DockStyle.Fill,
                ReadOnly                  = true,
                AllowUserToAddRows        = false,
                RowHeadersVisible         = false,
                AutoSizeColumnsMode       = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor           = Color.White,
                BorderStyle               = BorderStyle.None,
                GridColor                 = Color.FromArgb(221, 227, 236),
                EnableHeadersVisualStyles = false,
                CellBorderStyle           = DataGridViewCellBorderStyle.SingleHorizontal,
                SelectionMode             = DataGridViewSelectionMode.FullRowSelect,
                RowTemplate               = { Height = 42 }
            };
            dgv.ColumnHeadersHeight = 38;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(246, 249, 255);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(98,  112, 135);
            dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Padding   = new Padding(10, 0, 0, 0);
            dgv.DefaultCellStyle.BackColor              = Color.White;
            dgv.DefaultCellStyle.ForeColor              = Color.FromArgb(15, 31, 53);
            dgv.DefaultCellStyle.SelectionBackColor     = Color.FromArgb(219, 234, 254);
            dgv.DefaultCellStyle.SelectionForeColor     = Color.FromArgb(15, 31, 53);
            dgv.DefaultCellStyle.Padding                = new Padding(10, 4, 10, 4);
            dgv.DefaultCellStyle.Font                   = new Font("Segoe UI", 10f);
            return dgv;
        }

        private static void AddCol(DataGridView dgv, string name, string header, int weight)
            => dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name       = name,
                HeaderText = header,
                FillWeight = weight,
                SortMode   = DataGridViewColumnSortMode.Automatic
            });
    }
}
