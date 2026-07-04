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
        private Button        btnAddNew;
        private Button        btnDeleteComplaint;   // ✔ Delete button

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

            // ════════════════════════════════════════════════════════════════
            // CARD 1 — Search  (Top, fixed 300px)
            // ════════════════════════════════════════════════════════════════
            var (searchOuter, searchInner) = CardPanel.Create(outerHeight: 300);

            txtKeyword = new TextBox
            {
                Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Complaint ID / Order No. / Staff name"
            };
            txtKeyword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            cboStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f)
            };
            // Include Cancelled in the filter dropdown
            cboStatus.Items.AddRange(new object[] { "All", "Pending", "Processing", "Escalated", "Completed", "Cancelled" });
            cboStatus.SelectedIndex = 0;

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
                    ForeColor = Palette.TextMuted,
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.BottomLeft,
                    Padding   = new Padding(0, 0, 0, 2)
                };
                ctrl.Dock = DockStyle.Fill;
                tlp.Controls.Add(lbl,  0, 0);
                tlp.Controls.Add(ctrl, 0, 1);
                return tlp;
            }

            var tblFields = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 4,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblFields.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblFields.Controls.Add(MakeCell("Keyword", txtKeyword),  0, 0);
            tblFields.Controls.Add(MakeCell("Status",  cboStatus),   1, 0);

            var pnlBtns = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnSearch = MakePrimaryBtn("\uD83D\uDD0D  Search", new Point(0,   0), 210, 60);
            btnReset  = MakeOutlineBtn("\u21BA  Reset",       new Point(218, 0), 210, 60);
            btnSearch.Click += (s, e) => RefreshGrid();
            btnReset.Click  += (s, e) => ResetSearch();
            pnlBtns.Controls.Add(btnSearch);
            pnlBtns.Controls.Add(btnReset);

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
            tblCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 125f));
            tblCard.RowStyles.Add(new RowStyle(SizeType.Absolute,  65f));

            var pnlTitle = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var lblTitle = new Label
            {
                Text      = "Search Complaints",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Palette.TextMain,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var divider = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Palette.BorderColor };
            pnlTitle.Controls.Add(lblTitle);
            pnlTitle.Controls.Add(divider);

            tblCard.Controls.Add(pnlTitle,  0, 0);
            tblCard.Controls.Add(tblFields, 0, 1);
            tblCard.Controls.Add(pnlBtns,   0, 2);
            searchInner.Controls.Add(tblCard);

            // ════════════════════════════════════════════════════════════════
            // CARD 2 — KPI Bar + Action Buttons  (Top, fixed 90px)
            //
            // Button order (left → right):
            //   btnAddNew | btnUpdateStatus | btnViewDetail | btnDeleteComplaint
            // ════════════════════════════════════════════════════════════════
            var (kpiOuter, kpiInner) = CardPanel.Create(outerHeight: 90);

            pnlKpi = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(12, 10, 12, 10)
            };

            const int BtnW   = 260;
            const int BtnH   = 60;
            const int BtnGap = 8;
            const int BtnPad = 12;

            // Green: Add New
            btnAddNew = MakeSuccessBtn("\u2795  Add New", Point.Empty, BtnW, BtnH);
            btnAddNew.Click += btnAddNew_Click;

            // Amber: Update Status
            btnUpdateStatus = MakeWarningBtn("\u270F\uFE0F  Update Status", Point.Empty, BtnW, BtnH);
            btnUpdateStatus.Enabled = false;
            btnUpdateStatus.Click  += btnUpdateStatus_Click;

            // Blue: View Detail
            btnViewDetail = MakePrimaryBtn("\uD83D\uDD0D  View Detail", Point.Empty, BtnW, BtnH);
            btnViewDetail.Enabled = false;
            btnViewDetail.Click  += btnViewDetail_Click;

            // Red: Delete Complaint
            btnDeleteComplaint = MakeDangerBtn("\uD83D\uDDD1\uFE0F  Delete", Point.Empty, BtnW, BtnH);
            btnDeleteComplaint.Enabled = false;
            btnDeleteComplaint.Click  += btnDeleteComplaint_Click;

            // Panel holds all 4 buttons, docked to the right of kpiInner
            var pnlActionBtns = new Panel
            {
                Dock      = DockStyle.Right,
                Width     = BtnPad + (BtnW + BtnGap) * 4 - BtnGap + BtnPad,
                BackColor = Color.Transparent
            };

            void CentreActionBtns()
            {
                int top = (pnlActionBtns.Height - BtnH) / 2;
                if (top < 0) top = 0;
                btnAddNew.Location          = new Point(BtnPad,                                    top);
                btnUpdateStatus.Location    = new Point(BtnPad + (BtnW + BtnGap),                 top);
                btnViewDetail.Location      = new Point(BtnPad + (BtnW + BtnGap) * 2,             top);
                btnDeleteComplaint.Location = new Point(BtnPad + (BtnW + BtnGap) * 3,             top);
            }
            pnlActionBtns.Controls.Add(btnAddNew);
            pnlActionBtns.Controls.Add(btnUpdateStatus);
            pnlActionBtns.Controls.Add(btnViewDetail);
            pnlActionBtns.Controls.Add(btnDeleteComplaint);
            pnlActionBtns.Resize += (s, e) => CentreActionBtns();

            var pnlKpiRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlKpiRow.Controls.Add(pnlKpi);        // Fill  — pills
            pnlKpiRow.Controls.Add(pnlActionBtns); // Right — buttons
            kpiInner.Controls.Add(pnlKpiRow);

            // ════════════════════════════════════════════════════════════════
            // CARD 3 — Complaints Grid  (Fill)
            // ════════════════════════════════════════════════════════════════
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
            dgvComplaints.Columns.Add(new DataGridViewTextBoxColumn { Name = "colComplaintID", HeaderText = "COMPLAINT ID", FillWeight = 16 });
            dgvComplaints.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrderID",     HeaderText = "ORDER NO.",    FillWeight = 16 });
            dgvComplaints.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStaff",       HeaderText = "HANDLED BY",   FillWeight = 16 });
            dgvComplaints.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDescription", HeaderText = "DESCRIPTION",  FillWeight = 36 });
            dgvComplaints.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",      HeaderText = "STATUS",       FillWeight = 16 });
            dgvComplaints.SelectionChanged += dgvComplaints_SelectionChanged;
            dgvComplaints.CellFormatting   += dgvComplaints_CellFormatting;
            dgvComplaints.CellDoubleClick  += (s, e) => { if (e.RowIndex >= 0) ShowDetailDialog(); };

            gridInner.Controls.Add(dgvComplaints);

            // —— Assemble
            pnlMain.Controls.Add(gridOuter);    // Fill
            pnlMain.Controls.Add(kpiOuter);     // Top
            pnlMain.Controls.Add(searchOuter);  // Top
            pnlMain.Controls.Add(_shell);       // Top — topmost

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        // ── Button factories ────────────────────────────────────────────────────────────────────
        private static Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Palette.Primary, FlatStyle = FlatStyle.Flat,
                Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Palette.PrimaryDark;
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(21, 60, 155);
            return b;
        }
        private static Button MakeWarningBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Palette.Warning, FlatStyle = FlatStyle.Flat,
                Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(217, 119, 6);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(180, 90, 0);
            return b;
        }
        private static Button MakeSuccessBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(5, 150, 105), FlatStyle = FlatStyle.Flat,
                Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(4, 120, 87);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(3, 90, 68);
            return b;
        }
        // Red danger button for destructive actions
        private static Button MakeDangerBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(220, 38, 38), FlatStyle = FlatStyle.Flat,
                Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(185, 28, 28);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(153, 18, 18);
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
