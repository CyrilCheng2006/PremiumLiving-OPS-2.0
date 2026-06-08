using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.SystemControl
{
    partial class StaffListForm
    {
        private System.ComponentModel.IContainer components = null;

        private AppShell     _shell;
        private Panel        pnlKpi;
        private TextBox      txtSearch;
        private Button       btnSearch;
        private Button       btnRefresh;
        private Button       btnAddStaff;
        private Button       btnModifyDetail;
        private DataGridView dgvStaff;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text          = "Premium Living OPS \u2014 Staff List";
            this.Size          = new Size(1440, 900);
            this.MinimumSize   = new Size(1200, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = Color.FromArgb(240, 244, 249);
            this.WindowState   = FormWindowState.Maximized;
            this.Font          = new Font("Segoe UI", 13f);

            // ── Root panel
            var pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };

            // ── AppShell
            _shell = new AppShell();
            _shell.Dock        = DockStyle.Top;
            _shell.Height      = AppShell.TotalHeight;
            _shell.MinimumSize = new Size(0, AppShell.TotalHeight);
            _shell.SetPopupContainer(pnlMain);
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;

            // ════════════════════════════════════════════════════════════════
            //  CARD 1 — Search / filter bar
            // ════════════════════════════════════════════════════════════════
            txtSearch = new TextBox
            {
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                Dock            = DockStyle.Fill,
                PlaceholderText = "Search by Staff ID, Name, Role, Department or Email\u2026"
            };
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            btnSearch  = MakePrimaryBtn("\uD83D\uDD0D  Search", Point.Empty, 210, 60);
            btnRefresh = MakeOutlineBtn("\u21BA  Reset",        Point.Empty, 160, 60);
            btnSearch.Click  += (s, e) => RefreshGrid();
            btnRefresh.Click += (s, e) => ResetFilters();

            var pnlTitle = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlTitle.Controls.Add(new Label
            {
                Text      = "Staff Directory",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            });
            pnlTitle.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(221, 227, 236) });

            var pnlBtns = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnSearch.Location  = new Point(0,   0);
            btnRefresh.Location = new Point(218, 0);
            pnlBtns.Controls.Add(btnSearch);
            pnlBtns.Controls.Add(btnRefresh);

            var tblSearchCard = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                RowCount        = 3,
                ColumnCount     = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(18, 14, 18, 14)
            };
            tblSearchCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblSearchCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));
            tblSearchCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 80f));
            tblSearchCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 65f));
            tblSearchCard.Controls.Add(pnlTitle,  0, 0);
            tblSearchCard.Controls.Add(txtSearch, 0, 1);
            tblSearchCard.Controls.Add(pnlBtns,   0, 2);

            var pnlSearchWhite = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlSearchWhite.Paint += PaintCardBorder;
            pnlSearchWhite.Controls.Add(tblSearchCard);

            var pnlSearchOuter = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 270,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding   = new Padding(20, 14, 20, 8)
            };
            pnlSearchOuter.Controls.Add(pnlSearchWhite);

            // ════════════════════════════════════════════════════════════════
            //  CARD 2 — KPI strip
            //
            //  Layout: TableLayoutPanel 3-col
            //    col 0  Percent 100%  →  pnlKpi  (FlowLayoutPanel pills)
            //    col 1  Absolute 310  →  Add Staff button  (290×60, green)
            //    col 2  Absolute 310  →  Modify Detail button (290×60, blue)
            // ════════════════════════════════════════════════════════════════
            pnlKpi = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(12, 10, 12, 10)
            };

            // Add Staff button (green)
            btnAddStaff = MakeGreenBtn("\u2795  Add Staff", Point.Empty, 290, 60);
            btnAddStaff.Click += btnAddStaff_Click;

            var pnlAddCell = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlAddCell.Controls.Add(btnAddStaff);
            pnlAddCell.Layout += (s, e) =>
            {
                var p = (Panel)s;
                btnAddStaff.Left = (p.Width  - btnAddStaff.Width)  / 2;
                btnAddStaff.Top  = (p.Height - btnAddStaff.Height) / 2;
            };

            // Modify Detail button (blue)
            btnModifyDetail = MakePrimaryBtn("\u270F\uFE0F  Modify Detail", Point.Empty, 290, 60);
            btnModifyDetail.Enabled = false;
            btnModifyDetail.Click  += btnModifyDetail_Click;

            var pnlModifyCell = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlModifyCell.Controls.Add(btnModifyDetail);
            pnlModifyCell.Layout += (s, e) =>
            {
                var p = (Panel)s;
                btnModifyDetail.Left = (p.Width  - btnModifyDetail.Width)  / 2;
                btnModifyDetail.Top  = (p.Height - btnModifyDetail.Height) / 2;
            };

            var tblKpi = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 3,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(0)
            };
            tblKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f)); // pills
            tblKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 310f)); // Add Staff
            tblKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 310f)); // Modify Detail
            tblKpi.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblKpi.Controls.Add(pnlKpi,        0, 0);
            tblKpi.Controls.Add(pnlAddCell,    1, 0);
            tblKpi.Controls.Add(pnlModifyCell, 2, 0);

            var pnlKpiWhite = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlKpiWhite.Paint += PaintCardBorder;
            pnlKpiWhite.Controls.Add(tblKpi);

            var pnlKpiOuter = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 90,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding   = new Padding(20, 8, 20, 8)
            };
            pnlKpiOuter.Controls.Add(pnlKpiWhite);

            // ════════════════════════════════════════════════════════════════
            //  CARD 3 — DataGridView
            // ════════════════════════════════════════════════════════════════
            dgvStaff = new DataGridView
            {
                ReadOnly               = true,
                AllowUserToAddRows     = false,
                AllowUserToDeleteRows  = false,
                RowHeadersVisible      = false,
                SelectionMode          = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect            = false,
                BackgroundColor        = Color.White,
                BorderStyle            = BorderStyle.None,
                GridColor              = Color.FromArgb(221, 227, 236),
                Font                   = new Font("Segoe UI", 13f),
                AutoSizeColumnsMode    = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle        = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate            = { Height = 48 },
                Dock                   = DockStyle.Fill,
                ColumnHeadersHeight    = 46,
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
            dgvStaff.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStaffID",    HeaderText = "STAFF ID",   FillWeight = 15 });
            dgvStaff.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStaffName",  HeaderText = "NAME",       FillWeight = 25 });
            dgvStaff.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRole",       HeaderText = "ROLE",       FillWeight = 18 });
            dgvStaff.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDepartment", HeaderText = "DEPARTMENT", FillWeight = 18 });
            dgvStaff.Columns.Add(new DataGridViewTextBoxColumn { Name = "colEmail",      HeaderText = "EMAIL",      FillWeight = 24 });
            dgvStaff.SelectionChanged += dgvStaff_SelectionChanged;
            dgvStaff.CellDoubleClick  += dgvStaff_CellDoubleClick;

            var pnlGridInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlGridInner.Paint += PaintCardBorder;
            pnlGridInner.Controls.Add(dgvStaff);

            var pnlGridOuter = new Panel
            {
                Dock      = DockStyle.Fill,
                Padding   = new Padding(20, 12, 20, 0),
                BackColor = Color.FromArgb(240, 244, 249)
            };
            pnlGridOuter.Controls.Add(pnlGridInner);

            // ── Assemble pnlMain
            pnlMain.Controls.Add(pnlGridOuter);   // Fill
            pnlMain.Controls.Add(pnlKpiOuter);    // Top
            pnlMain.Controls.Add(pnlSearchOuter); // Top
            pnlMain.Controls.Add(_shell);         // Top

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
            this.PerformLayout();
            _shell.Height      = AppShell.TotalHeight;
            _shell.MinimumSize = new Size(0, AppShell.TotalHeight);
        }

        // ── Button factories
        private static Button MakePrimaryBtn(string text, Point loc, int w, int h)
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
                Location  = loc, Width = w, Height = h,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize         = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(21, 128, 61);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(20,  83, 45);
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
                Location  = loc, Width = w, Height = h,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize         = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            return b;
        }

        // ── Border painter
        private static void PaintCardBorder(object s, PaintEventArgs e)
        {
            var p = (Panel)s;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        }
    }
}
