using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.SystemControl
{
    partial class LogListForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── Fields declared here (owned by Designer) ──────────────────────────
        private AppShell     _shell;
        private Panel        pnlKpi;      // KPI strip container (filled by LogListForm.cs RefreshKpi)
        private TextBox      txtSearch;
        private Button       btnSearch;
        private Button       btnRefresh;
        private DataGridView dgvLogs;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text          = "Premium Living OPS \u2014 Log List";
            this.Size          = new Size(1440, 900);
            this.MinimumSize   = new Size(1200, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = Color.FromArgb(240, 244, 249);
            this.WindowState   = FormWindowState.Maximized;
            this.Font          = new Font("Segoe UI", 13f);

            // ── Root panel ───────────────────────────────────────────────────
            var pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };

            // ── AppShell ─────────────────────────────────────────────────────
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
                PlaceholderText = "Search by Staff ID, Log Type or Target Table\u2026"
            };
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            btnSearch  = MakePrimaryBtn("\uD83D\uDD0D  Search", Point.Empty, 210, 60);
            btnRefresh = MakeOutlineBtn("\u21BA  Reset",        Point.Empty, 160, 60);
            btnSearch.Click  += (s, e) => RefreshGrid();
            btnRefresh.Click += (s, e) => ResetFilters();

            var pnlTitle = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlTitle.Controls.Add(new Label
            {
                Text      = "System Activity Log",
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
            // ════════════════════════════════════════════════════════════════
            pnlKpi = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(12, 10, 12, 10)
            };

            var pnlKpiWhite = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlKpiWhite.Paint += PaintCardBorder;
            pnlKpiWhite.Controls.Add(pnlKpi);

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
            dgvLogs = new DataGridView
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
            dgvLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLogID",       HeaderText = "LOG ID",       FillWeight = 22 });
            dgvLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStaffID",     HeaderText = "STAFF ID",     FillWeight = 14 });
            dgvLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLogType",     HeaderText = "LOG TYPE",     FillWeight = 12 });
            dgvLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTargetTable", HeaderText = "TARGET TABLE", FillWeight = 16 });
            dgvLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTimestamp",   HeaderText = "TIMESTAMP",    FillWeight = 22 });
            dgvLogs.SelectionChanged += dgvLogs_SelectionChanged;
            dgvLogs.CellDoubleClick  += dgvLogs_CellDoubleClick;

            var pnlGridInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlGridInner.Paint += PaintCardBorder;
            pnlGridInner.Controls.Add(dgvLogs);

            var pnlGridOuter = new Panel
            {
                Dock      = DockStyle.Fill,
                Padding   = new Padding(20, 12, 20, 0),
                BackColor = Color.FromArgb(240, 244, 249)
            };
            pnlGridOuter.Controls.Add(pnlGridInner);

            // ── Assemble pnlMain (Fill first, then Top in reverse, AppShell last)
            pnlMain.Controls.Add(pnlGridOuter);
            pnlMain.Controls.Add(pnlKpiOuter);
            pnlMain.Controls.Add(pnlSearchOuter);
            pnlMain.Controls.Add(_shell);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);

            // Build KPI pills after all controls are wired
            this.Load += (s, e) => RefreshKpi();
        }
    }
}
