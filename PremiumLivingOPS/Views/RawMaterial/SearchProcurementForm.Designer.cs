using PremiumLivingOPS.Views.Shared;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.RawMaterial
{
    partial class SearchProcurementForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            SuspendLayout();   // RULE 1

            // ── Form properties ────────────────────────────────────────────
            Name          = "SearchProcurementForm";
            Text          = "Premium Living OPS — Raw Material";
            Size          = new Size(1440, 900);
            MinimumSize   = new Size(1280, 800);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor     = Color.FromArgb(240, 244, 249);
            WindowState   = FormWindowState.Maximized;
            Font          = new Font("Segoe UI", 13f);
            AutoScaleMode = AutoScaleMode.Font;
            AutoScaleDimensions = new SizeF(7F, 15F);

            // ── Root panel ─────────────────────────────────────────────────
            pnlRoot = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 244, 249)
            };

            // ── AppShell (RULE 2) ──────────────────────────────────────────
            _shell = new AppShell();
            _shell.Dock        = DockStyle.Top;
            _shell.Height      = AppShell.TotalHeight;
            _shell.MinimumSize = new Size(0, AppShell.TotalHeight);
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;  // RULE 4
            _shell.LogoutClicked   += BtnLogout_Click;          // RULE 4
            _shell.SetPopupContainer(pnlRoot);

            // ── Scroll panel ──────────────────────────────────────────────
            pnlScroll = new Panel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                BackColor  = Color.FromArgb(240, 244, 249)
            };

            // ════════════════════════════════════════════════════════════════
            // CARD 1 — Search Filters
            //   Row 0 : Title bar
            //   Row 1 : Keyword | Status | Date From | Date To
            //   Row 2 : Search / Reset buttons
            // ════════════════════════════════════════════════════════════════
            var (searchOuter, searchInner) = CardPanel.Create(outerHeight: 280,
                outerPadding: new Padding(20, 14, 20, 0));

            var tblSearch = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(20, 12, 20, 12)
            };
            tblSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblSearch.RowStyles.Add(new RowStyle(SizeType.Absolute,  46f));
            tblSearch.RowStyles.Add(new RowStyle(SizeType.Absolute, 114f));
            tblSearch.RowStyles.Add(new RowStyle(SizeType.Absolute,  72f));

            // Title
            var pnlSearchTitle = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlSearchTitle.Controls.Add(new Label
            {
                Text      = "Search Procurement",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            });
            pnlSearchTitle.Controls.Add(new Panel
            {
                Dock = DockStyle.Bottom, Height = 1,
                BackColor = Color.FromArgb(221, 227, 236)
            });

            // Filter fields
            txtKeyword = new TextBox
            {
                Font = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Purchase ID, Supplier or Material…"
            };
            txtKeyword.KeyDown += (s, ke) =>
            {
                if (ke.KeyCode == Keys.Enter) RefreshGrid();
            };

            cboStatus = new ComboBox
            {
                Font = new Font("Segoe UI", 12f),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboStatus.Items.AddRange(new object[]
                { "All", "Sent", "Cancelled", "Partially Received", "Received", "Completed" });
            cboStatus.SelectedIndex = 0;

            dtpDateFrom = new DateTimePicker
            {
                Font   = new Font("Segoe UI", 12f),
                Format = DateTimePickerFormat.Short,
                Value  = DateTime.Today.AddMonths(-3)
            };
            dtpDateTo = new DateTimePicker
            {
                Font   = new Font("Segoe UI", 12f),
                Format = DateTimePickerFormat.Short,
                Value  = DateTime.Today
            };

            chkUseDateRange = new CheckBox
            {
                Text      = "Filter by Date",
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(70, 85, 110),
                AutoSize  = true,
                Checked   = false
            };
            chkUseDateRange.CheckedChanged += (s, e) =>
            {
                dtpDateFrom.Enabled = chkUseDateRange.Checked;
                dtpDateTo.Enabled   = chkUseDateRange.Checked;
            };
            dtpDateFrom.Enabled = false;
            dtpDateTo.Enabled   = false;

            var tblFields = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblFields.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            tblFields.Controls.Add(MakeCell("Keyword",          txtKeyword,    true),  0, 0);
            tblFields.Controls.Add(MakeCell("Status",           cboStatus,     true),  1, 0);
            tblFields.Controls.Add(MakeCellWithExtra("",        chkUseDateRange, false), 2, 0);
            tblFields.Controls.Add(MakeCell("Date From",        dtpDateFrom,   true),  3, 0);
            tblFields.Controls.Add(MakeCell("Date To",          dtpDateTo,     false), 4, 0);

            // Buttons row
            var pnlSearchBtns = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnSearch = MakePrimaryBtn("\uD83D\uDD0D  Search", Point.Empty, 210, 52);
            btnReset  = MakeOutlineBtn("\u21BA  Reset",        new Point(218, 0), 210, 52);
            btnSearch.Click += (s, e) => RefreshGrid();
            btnReset.Click  += (s, e) => ResetFilters();
            pnlSearchBtns.Controls.Add(btnSearch);
            pnlSearchBtns.Controls.Add(btnReset);

            tblSearch.Controls.Add(pnlSearchTitle, 0, 0);
            tblSearch.Controls.Add(tblFields,      0, 1);
            tblSearch.Controls.Add(pnlSearchBtns,  0, 2);
            searchInner.Controls.Add(tblSearch);

            // ════════════════════════════════════════════════════════════════
            // CARD 2 — KPI + Action Bar
            // ════════════════════════════════════════════════════════════════
            var (actionOuter, actionInner) = CardPanel.Create(outerHeight: 96,
                outerPadding: new Padding(20, 12, 20, 0));

            var tblAction = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = Padding.Empty
            };
            tblAction.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblAction.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 650f));
            tblAction.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            pnlKpi = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            var pnlActionBtns = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnViewDetail      = MakePrimaryBtn("\uD83D\uDD0D View Detail", Point.Empty, 250, 60);
            btnCreateNew       = MakeGreenBtn  ("\uFF0B Create New",        Point.Empty, 250, 60);

            btnViewDetail.Enabled = false;

            pnlActionBtns.Layout += (s, ev) =>
            {
                var p    = (Panel)s;
                var btns = new Button[] { btnViewDetail, btnCreateNew };
                int xCursor = 4;
                foreach (var b in btns)
                {
                    b.Left = xCursor;
                    b.Top  = (p.Height - b.Height) / 2;
                    xCursor += b.Width + 8;
                }
            };
            pnlActionBtns.Controls.Add(btnViewDetail);
            pnlActionBtns.Controls.Add(btnCreateNew);

            tblAction.Controls.Add(pnlKpi,        0, 0);
            tblAction.Controls.Add(pnlActionBtns, 1, 0);
            actionInner.Controls.Add(tblAction);

            // ════════════════════════════════════════════════════════════════
            // CARD 3 — Results Grid
            //   Columns: Purchase ID | Supplier | Raw Material | Requested Qty |
            //            Order Date | PO Total | Status | Urgency
            // ════════════════════════════════════════════════════════════════
            var (gridOuter, gridInner) = CardPanel.CreateFill(
                outerPadding: new Padding(20, 12, 20, 20));

            dgvOrders = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true,
                AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(221, 227, 236),
                Font = new Font("Segoe UI", 12f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 46,
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
            dgvOrders.RowTemplate.Height = 48;

            // Columns — aligned with DB schema joins
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colPurchaseID",   HeaderText = "PURCHASE ID",   FillWeight = 16 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colSupplier",     HeaderText = "SUPPLIER",      FillWeight = 20 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colMaterial",     HeaderText = "RAW MATERIAL",  FillWeight = 20 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colReqQty",       HeaderText = "REQ QTY",       FillWeight = 8  });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colOrderDate",    HeaderText = "ORDER DATE",    FillWeight = 12 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colPOTotal",      HeaderText = "PO TOTAL",      FillWeight = 12 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colStatus",       HeaderText = "STATUS",        FillWeight = 14 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colUrgency",      HeaderText = "URGENCY",       FillWeight = 10 });

            gridInner.Controls.Add(dgvOrders);

            // ── Assemble scroll content ────────────────────────────────────
            pnlScroll.Controls.Add(gridOuter);
            pnlScroll.Controls.Add(actionOuter);
            pnlScroll.Controls.Add(searchOuter);

            // RULE 5: Fill first, Top second
            pnlRoot.Controls.Add(pnlScroll);
            pnlRoot.Controls.Add(_shell);

            Controls.Add(pnlRoot);
            ResumeLayout(false);
            PerformLayout();

            // RULE 3
            _shell.Height      = AppShell.TotalHeight;
            _shell.MinimumSize = new Size(0, AppShell.TotalHeight);
        }

        // ── Button factories ──────────────────────────────────────────────
        private static Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text = text, Font = new Font("Segoe UI", 11f),
                ForeColor = Color.White, BackColor = Color.FromArgb(47, 111, 237),
                FlatStyle = FlatStyle.Flat, Location = loc, Size = new Size(w, h), Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private static Button MakeGreenBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text = text, Font = new Font("Segoe UI", 11f),
                ForeColor = Color.White, BackColor = Color.FromArgb(22, 163, 74),
                FlatStyle = FlatStyle.Flat, Location = loc, Size = new Size(w, h), Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private static Button MakeOutlineBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text = text, Font = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(98, 112, 135), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat, Location = loc, Size = new Size(w, h), Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize  = 1;
            return b;
        }

        private static TableLayoutPanel MakeCell(string caption, Control ctrl, bool rightPad)
        {
            var tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = rightPad ? new Padding(0, 0, 12, 0) : Padding.Empty
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tlp.Controls.Add(new Label
            {
                Text      = caption,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft,
                Padding   = new Padding(0, 0, 0, 2)
            }, 0, 0);
            ctrl.Dock = DockStyle.Fill;
            tlp.Controls.Add(ctrl, 0, 1);
            return tlp;
        }

        /// <summary>A cell whose label row is replaced by an inline checkbox.</summary>
        private static TableLayoutPanel MakeCellWithExtra(string caption, Control extra, bool rightPad)
        {
            var tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = rightPad ? new Padding(0, 0, 12, 0) : Padding.Empty
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            // Empty top row for caption alignment
            tlp.Controls.Add(new Label { Dock = DockStyle.Fill, BackColor = Color.Transparent }, 0, 0);
            extra.Dock = DockStyle.Fill;
            tlp.Controls.Add(extra, 0, 1);
            return tlp;
        }

        // ── Field declarations ─────────────────────────────────────────────
        private Panel           pnlRoot;
        private AppShell        _shell;
        private Panel           pnlScroll;
        internal Panel          pnlKpi;
        internal TextBox        txtKeyword;
        internal ComboBox       cboStatus;
        internal DateTimePicker dtpDateFrom;
        internal DateTimePicker dtpDateTo;
        internal CheckBox       chkUseDateRange;
        private  Button         btnSearch;
        private  Button         btnReset;
        internal Button         btnViewDetail;
        internal Button         btnCreateNew;
        internal DataGridView   dgvOrders;
    }
}
