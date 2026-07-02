using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.RawMaterial
{
    partial class SearchProcurementForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── AppShell (contains TopNavBar + UserBar)
        private AppShell _shell;

        // ── CARD 1: Search Filters
        internal TextBox        txtKeyword;
        internal ComboBox       cboStatus;
        internal DateTimePicker dtpDateFrom;
        internal DateTimePicker dtpDateTo;
        internal CheckBox       chkUseDateRange;
        private  Button         btnSearch;
        private  Button         btnReset;

        // ── CARD 2: KPI + Action Buttons
        internal Panel  pnlKpi;
        internal Button btnViewDetail;
        internal Button btnCreateNew;

        // ── CARD 3: Results Grid
        internal DataGridView dgvOrders;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // ── Form
            this.Text          = "Premium Living OPS — Raw Material";
            this.Size          = new Size(1440, 900);
            this.MinimumSize   = new Size(1280, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = Color.FromArgb(240, 244, 249);
            this.WindowState   = FormWindowState.Maximized;
            this.Font          = new Font("Segoe UI", 13f);

            // ── Root panel
            var pnlMain = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 244, 249)
            };

            // ── AppShell
            _shell = new AppShell();
            _shell.SetPopupContainer(pnlMain);
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += BtnLogout_Click;

            // ════════════════════════════════════════════════════════════
            // CARD 1 — Search Filters
            // ════════════════════════════════════════════════════════════
            txtKeyword = new TextBox
            {
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                PlaceholderText = "Purchase ID or Supplier…"
            };
            txtKeyword.KeyDown += (s, ke) => { if (ke.KeyCode == Keys.Enter) RefreshGrid(); };

            cboStatus = new ComboBox { Font = new Font("Segoe UI", 12f), DropDownStyle = ComboBoxStyle.DropDownList };
            cboStatus.Items.AddRange(new object[] { "All", "Sent", "Cancelled", "Partially Received", "Received", "Completed" });
            cboStatus.SelectedIndex = 0;

            dtpDateFrom = new DateTimePicker { Font = new Font("Segoe UI", 12f), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-3), Enabled = false };
            dtpDateTo   = new DateTimePicker { Font = new Font("Segoe UI", 12f), Format = DateTimePickerFormat.Short, Value = DateTime.Today, Enabled = false };

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

            var tblFields = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  30f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  20f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  25f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  25f));
            tblFields.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblFields.Controls.Add(MakeCell("Keyword",   txtKeyword,      true),  0, 0);
            tblFields.Controls.Add(MakeCell("Status",    cboStatus,       true),  1, 0);
            tblFields.Controls.Add(MakeCellWithExtra("", chkUseDateRange, false), 2, 0);
            tblFields.Controls.Add(MakeCell("Date From", dtpDateFrom,     true),  3, 0);
            tblFields.Controls.Add(MakeCell("Date To",   dtpDateTo,       false), 4, 0);

            const int SBtnW  = 200;
            const int RBtnW  = 160;
            const int BtnH   =  52;
            const int BtnGap =   8;

            btnSearch = MakePrimaryBtn("🔍  Search", Point.Empty,          SBtnW, BtnH);
            btnReset  = MakeOutlineBtn("↺  Reset",  new Point(SBtnW + BtnGap, 0), RBtnW, BtnH);
            btnSearch.Click += (s, e) => RefreshGrid();
            btnReset.Click  += (s, e) => ResetFilters();

            var pnlBtnHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            void CentreBtns()
            {
                int top = Math.Max(0, (pnlBtnHost.Height - BtnH) / 2);
                btnSearch.Location = new Point(0,              top);
                btnReset.Location  = new Point(SBtnW + BtnGap, top);
            }
            pnlBtnHost.Controls.Add(btnSearch);
            pnlBtnHost.Controls.Add(btnReset);
            pnlBtnHost.Resize += (s, e) => CentreBtns();

            var tblBtnRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblBtnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
            tblBtnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27.5f));
            tblBtnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27.5f));
            tblBtnRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblBtnRow.Controls.Add(pnlBtnHost, 0, 0);

            var tblSearch = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(18, 14, 18, 14)
            };
            tblSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblSearch.RowStyles.Add(new RowStyle(SizeType.Absolute,  44f));   // title
            tblSearch.RowStyles.Add(new RowStyle(SizeType.Absolute, 110f));   // fields
            tblSearch.RowStyles.Add(new RowStyle(SizeType.Absolute,  72f));   // buttons

            tblSearch.Controls.Add(BuildTitlePanel("Search Procurement", isSectionTitle: false), 0, 0);
            tblSearch.Controls.Add(tblFields,  0, 1);
            tblSearch.Controls.Add(tblBtnRow,  0, 2);

            var pnlSearchOuter = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 280,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding   = new Padding(20, 14, 20, 0)
            };
            var pnlSearchCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlSearchCard.Paint += PaintCardBorder;
            pnlSearchCard.Controls.Add(tblSearch);
            pnlSearchOuter.Controls.Add(pnlSearchCard);

            // ════════════════════════════════════════════════════════════
            // CARD 2 — KPI pills (left) + Action Buttons (right)
            // ════════════════════════════════════════════════════════════
            pnlKpi = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(12, 10, 12, 10)
            };

            const int ABtnW   = 290;
            const int ABtnH   =  60;
            const int ABtnGap =   8;
            const int ABtnPad =  12;

            btnViewDetail = MakePrimaryBtn("🔍  View Details", Point.Empty, ABtnW, ABtnH);
            btnCreateNew  = MakeGreenBtn  ("＋  Create New",  Point.Empty, ABtnW, ABtnH);
            btnViewDetail.Enabled = false;

            var pnlActionBtns = new Panel
            {
                Dock      = DockStyle.Right,
                Width     = ABtnPad + ABtnW + ABtnGap + ABtnW + ABtnPad,
                BackColor = Color.Transparent
            };
            void CentreActionBtns()
            {
                int top = (pnlActionBtns.Height - ABtnH) / 2;
                if (top < 0) top = 0;
                btnViewDetail.Location = new Point(ABtnPad, top);
                btnCreateNew.Location  = new Point(ABtnPad + ABtnW + ABtnGap, top);
            }
            pnlActionBtns.Controls.Add(btnViewDetail);
            pnlActionBtns.Controls.Add(btnCreateNew);
            pnlActionBtns.Resize += (s, e) => CentreActionBtns();

            var pnlKpiRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlKpiRow.Controls.Add(pnlKpi);        // Fill — add first
            pnlKpiRow.Controls.Add(pnlActionBtns); // Right — add after

            var pnlActionOuter = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 90,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding   = new Padding(20, 8, 20, 8)
            };
            var pnlActionCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlActionCard.Paint += PaintCardBorder;
            pnlActionCard.Controls.Add(pnlKpiRow);
            pnlActionOuter.Controls.Add(pnlActionCard);

            // ════════════════════════════════════════════════════════════
            // CARD 3 — Results Grid  (7 columns, grouped by base PO-ID)
            // ════════════════════════════════════════════════════════════
            dgvOrders = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true,
                AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                RowHeadersVisible  = false,
                SelectionMode      = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect        = false,
                BackgroundColor    = Color.White, BorderStyle = BorderStyle.None,
                GridColor          = Color.FromArgb(221, 227, 236),
                Font               = new Font("Segoe UI", 12f),
                AutoSizeColumnsMode    = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle        = DataGridViewCellBorderStyle.SingleHorizontal,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight    = 46,
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
                    Padding            = new Padding(12, 10, 12, 10)
                }
            };
            dgvOrders.RowTemplate.Height = 72;

            // 7 columns — must match RefreshGrid() Rows.Add(...) order exactly
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPurchaseID", HeaderText = "PURCHASE ID",  FillWeight = 18 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSupplier",   HeaderText = "SUPPLIER",     FillWeight = 22 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItems",      HeaderText = "ITEMS",        FillWeight =  9 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrderDate",  HeaderText = "ORDER DATE",   FillWeight = 12 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal",      HeaderText = "TOTAL AMOUNT", FillWeight = 14 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",     HeaderText = "STATUS",       FillWeight = 15 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUrgency",    HeaderText = "URGENCY",      FillWeight = 10 });

            var pnlGridCard = new Panel
            {
                Dock      = DockStyle.Fill,
                Padding   = new Padding(20, 12, 20, 20),
                BackColor = Color.FromArgb(240, 244, 249)
            };
            var pnlGridInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlGridInner.Paint += PaintCardBorder;
            pnlGridInner.Controls.Add(dgvOrders);
            pnlGridCard.Controls.Add(pnlGridInner);

            // ════════════════════════════════════════════════════════════
            // Assemble pnlMain
            // ════════════════════════════════════════════════════════════
            pnlMain.Controls.Add(pnlGridCard);    // Fill  — grid
            pnlMain.Controls.Add(pnlActionOuter); // Top   — KPI + buttons
            pnlMain.Controls.Add(pnlSearchOuter); // Top   — search filters
            pnlMain.Controls.Add(_shell);         // Top   — AppShell (last = topmost)

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        // ════════════════════════════════════════════════════════════
        // Helpers
        // ════════════════════════════════════════════════════════════

        private static Panel BuildTitlePanel(string title, bool isSectionTitle)
        {
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnl.Controls.Add(new Label
            {
                Text      = title,
                Font      = isSectionTitle
                                ? new Font("Segoe UI", 15f, FontStyle.Bold)
                                : new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            });
            pnl.Controls.Add(new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 1,
                BackColor = Color.FromArgb(221, 227, 236)
            });
            return pnl;
        }

        private static TableLayoutPanel MakeCell(string caption, Control ctrl, bool rightPad)
        {
            var tlp = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill, RowCount = 2, ColumnCount = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = rightPad ? new Padding(0, 0, 14, 0) : new Padding(0)
            };
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent,  100f));
            tlp.Controls.Add(new Label
            {
                Text      = caption,
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Fill,
                Font      = new Font("Segoe UI", 11f),
                TextAlign = ContentAlignment.BottomLeft
            }, 0, 0);
            ctrl.Dock = DockStyle.Fill;
            tlp.Controls.Add(ctrl, 0, 1);
            return tlp;
        }

        private static TableLayoutPanel MakeCellWithExtra(string caption, Control extra, bool rightPad)
        {
            var tlp = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill, RowCount = 2, ColumnCount = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = rightPad ? new Padding(0, 0, 14, 0) : new Padding(0)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            tlp.Controls.Add(new Label { Dock = DockStyle.Fill, BackColor = Color.Transparent }, 0, 0);
            extra.Dock = DockStyle.Fill;
            tlp.Controls.Add(extra, 0, 1);
            return tlp;
        }

        private static Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(47, 111, 237),
                FlatStyle = FlatStyle.Flat,
                Location  = loc,
                Size      = new Size(w, h),
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize         = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(26, 77, 192);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(21, 60, 155);
            return b;
        }

        private static Button MakeGreenBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(22, 163, 74),
                FlatStyle = FlatStyle.Flat,
                Location  = loc,
                Size      = new Size(w, h),
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize         = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(16, 131, 58);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(10, 100, 40);
            return b;
        }

        private static Button MakeOutlineBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location  = loc,
                Size      = new Size(w, h),
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize         = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            return b;
        }

        private static void PaintCardBorder(object s, PaintEventArgs e)
        {
            var p = (Panel)s;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        }
    }
}
