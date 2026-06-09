using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.AfterService
{
    partial class ComplaintListForm
    {
        private System.ComponentModel.IContainer components = null;

        private AppShell      _shell;
        private TextBox       txtKeyword;
        private ComboBox      cboStatus;
        private Button        btnSearch;
        private Button        btnReset;
        private Panel         pnlKpi;
        private DataGridView  dgvComplaints;
        private Button        btnUpdateStatus;
        private Button        btnViewDetail;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text          = "Premium Living OPS — Complaint List";
            this.Size          = new Size(1440, 900);
            this.MinimumSize   = new Size(1200, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = Palette.BgPage;
            this.WindowState   = FormWindowState.Maximized;
            this.Font          = new Font("Segoe UI", 13f);

            var pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage };
            _shell = new AppShell();
            _shell.SetPopupContainer(pnlMain);
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;

            // ══════════════════════════════════════════════════════════════════
            // CARD 1 — Search Bar  (outerHeight 150)
            // ══════════════════════════════════════════════════════════════════
            var (searchOuter, searchInner) = CardPanel.Create(outerHeight: 150);

            txtKeyword = new TextBox
            {
                Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Complaint ID / Order No. / Staff name / Description"
            };
            txtKeyword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            cboStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f)
            };
            cboStatus.Items.AddRange(new object[] { "All", "Pending", "Processing", "Escalated", "Completed" });
            cboStatus.SelectedIndex = 0;

            btnSearch = MakePrimaryBtn("🔍  Search", Point.Empty, 190, 52);
            btnReset  = MakeOutlineBtn("↺  Reset",  Point.Empty, 190, 52);
            btnSearch.Click += (s, e) => RefreshGrid();
            btnReset.Click  += (s, e) => ResetSearch();

            var tblSearch = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(18, 10, 18, 10)
            };
            tblSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 205f));
            tblSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 205f));
            tblSearch.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
            tblSearch.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            tblSearch.Controls.Add(MakeFieldLabel("Search"), 0, 0);
            tblSearch.Controls.Add(MakeFieldLabel("Status"), 1, 0);
            txtKeyword.Dock = DockStyle.Fill;
            cboStatus.Dock  = DockStyle.Fill;
            tblSearch.Controls.Add(txtKeyword, 0, 1);
            tblSearch.Controls.Add(cboStatus,  1, 1);

            var pnlBtns = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnSearch.Location = new Point(0, 0);
            btnReset.Location  = new Point(198, 0);
            pnlBtns.Controls.Add(btnSearch);
            pnlBtns.Controls.Add(btnReset);
            tblSearch.SetColumnSpan(pnlBtns, 2);
            tblSearch.Controls.Add(pnlBtns, 2, 1);
            searchInner.Controls.Add(tblSearch);

            // ══════════════════════════════════════════════════════════════════
            // CARD 2 — KPI Bar + Action Buttons  (outerHeight 90)
            // ══════════════════════════════════════════════════════════════════
            var (kpiOuter, kpiInner) = CardPanel.Create(outerHeight: 90);

            // pnlKpi: fills left side, RefreshKpi() populates FlowLayoutPanel of pills
            pnlKpi = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(12, 10, 12, 10)
            };

            const int BtnW   = 240;
            const int BtnH   = 60;
            const int BtnGap = 8;
            const int BtnPad = 12;

            btnUpdateStatus = MakeWarningBtn("✏️  Update Status", Point.Empty, BtnW, BtnH);
            btnViewDetail   = MakePrimaryBtn("🔍  View Detail",   Point.Empty, BtnW, BtnH);
            btnUpdateStatus.Enabled = false;
            btnViewDetail.Enabled   = false;
            btnUpdateStatus.Click  += btnUpdateStatus_Click;
            btnViewDetail.Click    += btnViewDetail_Click;

            // Panel wide enough for two buttons side-by-side + outer padding
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
                btnUpdateStatus.Location = new Point(BtnPad, top);
                btnViewDetail.Location   = new Point(BtnPad + BtnW + BtnGap, top);
            }
            pnlActionBtns.Controls.Add(btnUpdateStatus);
            pnlActionBtns.Controls.Add(btnViewDetail);
            pnlActionBtns.Resize += (s, e) => CentreActionBtns();

            // Container: pills fill left, action buttons docked right
            var pnlKpiRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlKpiRow.Controls.Add(pnlKpi);        // DockStyle.Fill  — pills
            pnlKpiRow.Controls.Add(pnlActionBtns); // DockStyle.Right — buttons (must add AFTER Fill)
            kpiInner.Controls.Add(pnlKpiRow);

            // ══════════════════════════════════════════════════════════════════
            // CARD 3 — Complaints Grid  (Fill)
            // ══════════════════════════════════════════════════════════════════
            var (gridOuter, gridInner) = CardPanel.CreateFill();

            dgvComplaints = new DataGridView
            {
                ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                GridColor = Palette.BorderColor, Font = new Font("Segoe UI", 13f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate = { Height = 48 }, Dock = DockStyle.Fill,
                ColumnHeadersHeight = 46, EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255), ForeColor = Palette.TextMuted,
                    Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                    Padding = new Padding(12, 0, 0, 0), Alignment = DataGridViewContentAlignment.MiddleLeft
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.White, ForeColor = Palette.TextMain,
                    SelectionBackColor = Color.FromArgb(219, 234, 254), SelectionForeColor = Palette.TextMain,
                    Padding = new Padding(12, 6, 12, 6)
                }
            };
            dgvComplaints.Columns.Add(new DataGridViewTextBoxColumn { Name = "colComplaintID",  HeaderText = "COMPLAINT ID", FillWeight = 16 });
            dgvComplaints.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrderID",      HeaderText = "ORDER NO.",    FillWeight = 16 });
            dgvComplaints.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStaff",        HeaderText = "HANDLED BY",   FillWeight = 16 });
            dgvComplaints.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDescription",  HeaderText = "DESCRIPTION",  FillWeight = 36 });
            dgvComplaints.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",       HeaderText = "STATUS",       FillWeight = 16 });
            dgvComplaints.SelectionChanged += dgvComplaints_SelectionChanged;
            dgvComplaints.CellFormatting   += dgvComplaints_CellFormatting;
            dgvComplaints.CellDoubleClick  += (s, e) => { if (e.RowIndex >= 0) ShowDetailDialog(); };

            gridInner.Controls.Add(dgvComplaints);

            // ── Assemble
            pnlMain.Controls.Add(gridOuter);   // Fill
            pnlMain.Controls.Add(kpiOuter);    // Top
            pnlMain.Controls.Add(searchOuter); // Top
            pnlMain.Controls.Add(_shell);      // Top — topmost

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        // ── Button factories ──────────────────────────────────────────────────
        private static Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Palette.Primary, FlatStyle = FlatStyle.Flat,
                Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Palette.PrimaryDark;
            return b;
        }
        private static Button MakeWarningBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Palette.Warning, FlatStyle = FlatStyle.Flat,
                Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(217, 119, 6);
            return b;
        }
        private static Button MakeOutlineBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f),
                ForeColor = Palette.TextMain, BackColor = Color.White, FlatStyle = FlatStyle.Flat,
                Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderColor = Palette.BorderColor;
            b.FlatAppearance.BorderSize  = 1;
            b.FlatAppearance.MouseOverBackColor = Palette.BgPage;
            return b;
        }
        private static Label MakeFieldLabel(string text) => new Label
        {
            Text = text, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Palette.TextMuted, Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft, Padding = new Padding(0, 0, 0, 2)
        };
    }
}
