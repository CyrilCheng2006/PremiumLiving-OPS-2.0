using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.AfterService
{
    partial class ComplaintListForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── AppShell ──────────────────────────────────────────────────────
        private AppShell _shell;

        // ── Search card controls ──────────────────────────────────────────
        private TextBox  txtKeyword;
        private ComboBox cboStatus;
        private Button   btnSearch;
        private Button   btnReset;

        // ── KPI bar ───────────────────────────────────────────────────────
        private Panel pnlKpi;

        // ── Grid card ─────────────────────────────────────────────────────
        private DataGridView dgvComplaints;
        private Button       btnUpdateStatus;
        private Button       btnViewDetail;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // ── Form properties ───────────────────────────────────────────
            // Do NOT set AutoScaleMode or AutoScaleDimensions — breaks UserBar
            this.Text          = "Premium Living OPS — After-Service  ›  Complaints";
            this.Size          = new Size(1440, 900);
            this.MinimumSize   = new Size(1280, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = Color.FromArgb(240, 244, 249);
            this.WindowState   = FormWindowState.Maximized;
            this.Font          = new Font("Segoe UI", 13f);

            // ── Root panel ────────────────────────────────────────────────
            var pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };

            // ── AppShell — black-box pattern (ViewOrderForm baseline) ──────
            // Never set Dock / Height / MinimumSize externally.
            // AppShell self-locks via its own OnLayout + ScaleControl.
            _shell = new AppShell();
            _shell.SetPopupContainer(pnlMain);
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;  // once only
            _shell.LogoutClicked   += btnLogout_Click;          // once only

            var pnlPage = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };

            // ═════════════════════════════════════════════════════════════
            //  SEARCH CARD  (DockStyle.Top, height 160)
            // ═════════════════════════════════════════════════════════════

            txtKeyword = new TextBox
            {
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                Dock            = DockStyle.Fill,
                PlaceholderText = "Complaint ID / Order ID / Staff"
            };
            txtKeyword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            cboStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 12f),
                Dock          = DockStyle.Fill
            };
            cboStatus.Items.AddRange(new object[] { "All", "Pending", "Processing", "Escalated", "Completed" });
            cboStatus.SelectedIndex = 0;

            btnSearch = MakePrimaryBtn("Search", Point.Empty, 180, 50);
            btnReset  = MakeOutlineBtn("Reset",  Point.Empty, 180, 50);
            btnSearch.Click += (s, e) => RefreshGrid();
            btnReset.Click  += (s, e) => { txtKeyword.Clear(); cboStatus.SelectedIndex = 0; RefreshGrid(); };

            var tblSearch = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 4,
                RowCount        = 2,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(18, 12, 18, 12)
            };
            tblSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100f));
            tblSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,   40f));
            tblSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,  80f));
            tblSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,   30f));
            tblSearch.RowStyles.Add(new RowStyle(SizeType.Absolute,  42f));
            tblSearch.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblSearch.Controls.Add(MakeLbl("Keyword"), 0, 0);
            tblSearch.Controls.Add(txtKeyword,         1, 0);
            tblSearch.Controls.Add(MakeLbl("Status"),  2, 0);
            tblSearch.Controls.Add(cboStatus,          3, 0);

            var pnlBtns = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnSearch.Location = new Point(0,   0);
            btnReset.Location  = new Point(192, 0);
            pnlBtns.Controls.AddRange(new Control[] { btnSearch, btnReset });
            tblSearch.SetColumnSpan(pnlBtns, 4);
            tblSearch.Controls.Add(pnlBtns, 0, 1);

            var (searchOuter, searchInner) = CardPanel.Create(outerHeight: 160);
            searchInner.Controls.Add(tblSearch);

            // ═════════════════════════════════════════════════════════════
            //  KPI CARD  (DockStyle.Top, height 90)
            // ═════════════════════════════════════════════════════════════

            pnlKpi = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(12, 10, 12, 10)
            };
            var (kpiOuter, kpiInner) = CardPanel.Create(outerHeight: 90);
            kpiInner.Controls.Add(pnlKpi);

            // ═════════════════════════════════════════════════════════════
            //  GRID CARD  (DockStyle.Fill — remaining space)
            // ═════════════════════════════════════════════════════════════

            dgvComplaints = BuildDataGridView();
            dgvComplaints.Columns.Add(new DataGridViewTextBoxColumn { Name = "colID",     HeaderText = "COMPLAINT ID", FillWeight = 16 });
            dgvComplaints.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrder",  HeaderText = "ORDER ID",     FillWeight = 14 });
            dgvComplaints.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStaff",  HeaderText = "STAFF",        FillWeight = 16 });
            dgvComplaints.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDesc",   HeaderText = "DESCRIPTION",  FillWeight = 36 });
            dgvComplaints.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "STATUS",       FillWeight = 18 });
            dgvComplaints.SelectionChanged += dgvComplaints_SelectionChanged;
            dgvComplaints.CellFormatting   += dgvComplaints_CellFormatting;

            btnUpdateStatus = MakeWarningBtn("Update Status", Point.Empty, 220, 52);
            btnViewDetail   = MakePrimaryBtn("View Detail",   Point.Empty, 200, 52);
            btnUpdateStatus.Enabled = false;
            btnViewDetail.Enabled   = false;
            btnUpdateStatus.Click  += btnUpdateStatus_Click;
            btnViewDetail.Click    += btnViewDetail_Click;

            var pnlGridBtns = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 66,
                BackColor = Color.White,
                Padding   = new Padding(16, 8, 16, 8)
            };
            pnlGridBtns.Paint += PaintTopBorder;
            btnViewDetail.Location   = new Point(16,  8);
            btnUpdateStatus.Location = new Point(228, 8);
            pnlGridBtns.Controls.AddRange(new Control[] { btnViewDetail, btnUpdateStatus });

            var (gridOuter, gridInner) = CardPanel.CreateFill();
            gridInner.Controls.Add(pnlGridBtns);
            gridInner.Controls.Add(dgvComplaints);

            // ── Assemble pnlPage (Fill first, Top panels bottom → top) ────
            pnlPage.Controls.Add(gridOuter);    // Fill  — grid
            pnlPage.Controls.Add(kpiOuter);     // Top   — KPI bar
            pnlPage.Controls.Add(searchOuter);  // Top   — search card

            // ── Assemble pnlMain (_shell added last → topmost) ──────────
            pnlMain.Controls.Add(pnlPage);  // Fill
            pnlMain.Controls.Add(_shell);   // Top — AppShell chrome

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);        // Stop here. No PerformLayout(). No re-lock.
        }

        // ── Factory helpers ───────────────────────────────────────────

        private static Label MakeLbl(string text) => new Label
        {
            Text      = text,
            Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(98, 112, 135),
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize  = false
        };

        private static DataGridView BuildDataGridView() => new DataGridView
        {
            ReadOnly              = true,
            AllowUserToAddRows    = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible     = false,
            SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect           = false,
            BackgroundColor       = Color.White,
            BorderStyle           = BorderStyle.None,
            GridColor             = Color.FromArgb(221, 227, 236),
            Font                  = new Font("Segoe UI", 12f),
            AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
            CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal,
            RowTemplate           = { Height = 46 },
            Dock                  = DockStyle.Fill,
            ColumnHeadersHeight   = 44,
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

        private Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(47, 111, 237),
                FlatStyle = FlatStyle.Flat,
                Location  = loc,
                Width     = w,
                Height    = h,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize           = 0;
            b.FlatAppearance.MouseOverBackColor    = Color.FromArgb(26,  77, 192);
            b.FlatAppearance.MouseDownBackColor    = Color.FromArgb(21,  60, 155);
            return b;
        }

        private Button MakeWarningBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(245, 158, 11),
                FlatStyle = FlatStyle.Flat,
                Location  = loc,
                Width     = w,
                Height    = h,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize           = 0;
            b.FlatAppearance.MouseOverBackColor    = Color.FromArgb(217, 119,   6);
            b.FlatAppearance.MouseDownBackColor    = Color.FromArgb(180,  90,   0);
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
                Location  = loc,
                Width     = w,
                Height    = h,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize         = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            return b;
        }

        private static void PaintTopBorder(object s, PaintEventArgs e)
        {
            var p = (Panel)s;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, 0, p.Width, 0);
        }
    }
}
