using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.MasterData
{
    partial class SupplierListForm
    {
        private System.ComponentModel.IContainer components = null;

        private AppShell     _shell;
        private TextBox      txtSearch;
        private Button       btnSearch;
        private Button       btnRefresh;
        private DataGridView dgvSuppliers;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();                                        // RULE 1

            this.Text          = "Premium Living OPS — Supplier List";
            this.Size          = new Size(1440, 900);
            this.MinimumSize   = new Size(1200, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = Color.FromArgb(240, 244, 249);
            this.WindowState   = FormWindowState.Maximized;
            this.Font          = new Font("Segoe UI", 13f);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.AutoScaleDimensions = new SizeF(7F, 15F);

            // ── Root panel ──────────────────────────────────────────────────
            var pnlMain = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 244, 249)
            };

            // ── AppShell (RULE 2) ────────────────────────────────────────────
            _shell = new AppShell();                                     // RULE 2
            _shell.Dock        = DockStyle.Top;
            _shell.Height      = AppShell.TotalHeight;
            _shell.MinimumSize = new Size(0, AppShell.TotalHeight);
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;           // RULE 4
            _shell.LogoutClicked   += btnLogout_Click;                   // RULE 4
            _shell.SetPopupContainer(pnlMain);

            // ════════════════════════════════════════════════════════════════
            //  CARD 1 — Search bar (CardPanel.Create, DockStyle.Top, h=110)
            // ════════════════════════════════════════════════════════════════
            txtSearch = new TextBox
            {
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                PlaceholderText = "Search by Supplier ID or Name…",
                Dock            = DockStyle.Fill
            };
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            btnSearch = MakePrimaryBtn("Search", 0, 210, 44);
            btnSearch.Click += (s, e) => RefreshGrid();

            btnRefresh = MakeOutlineBtn("Reset", 0, 130, 44);
            btnRefresh.Click += (s, e) => ResetFilters();

            // Search-field + buttons in a horizontal TLP
            var tblSearch = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 3,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(18, 0, 18, 0)
            };
            tblSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f));
            tblSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 148f));
            tblSearch.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var pnlBtnSearch  = MakeCentredBtnPanel(btnSearch,  220);
            var pnlBtnRefresh = MakeCentredBtnPanel(btnRefresh, 148);

            tblSearch.Controls.Add(txtSearch,    0, 0);
            tblSearch.Controls.Add(pnlBtnSearch,  1, 0);
            tblSearch.Controls.Add(pnlBtnRefresh, 2, 0);

            // Card title row
            var pnlCardTitle = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 48,
                BackColor = Color.Transparent,
                Padding   = new Padding(18, 0, 18, 0)
            };
            var lblCardTitle = new Label
            {
                Text      = "Supplier Directory",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var divider = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 1,
                BackColor = Color.FromArgb(221, 227, 236)
            };
            pnlCardTitle.Controls.Add(lblCardTitle);
            pnlCardTitle.Controls.Add(divider);

            // Assemble the inner card content
            var pnlSearchContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlSearchContent.Controls.Add(tblSearch);
            pnlSearchContent.Controls.Add(pnlCardTitle);

            // Three-layer card wrap (outer=gray, inner=white card)
            var (searchOuter, searchInner) = CardPanel.Create(outerHeight: 116);
            searchInner.Controls.Add(pnlSearchContent);

            // ════════════════════════════════════════════════════════════════
            //  CARD 2 — Summary KPI strip (CardPanel.Create, h=72)
            // ════════════════════════════════════════════════════════════════
            pnlKpi = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(14, 0, 14, 0)
            };

            var (kpiOuter, kpiInner) = CardPanel.Create(
                outerHeight:  72,
                outerPadding: new Padding(20, 8, 20, 8));
            kpiInner.Controls.Add(pnlKpi);

            // ════════════════════════════════════════════════════════════════
            //  CARD 3 — DataGridView (CardPanel.CreateFill)
            // ════════════════════════════════════════════════════════════════
            dgvSuppliers = BuildGrid();
            dgvSuppliers.SelectionChanged += dgvSuppliers_SelectionChanged;
            dgvSuppliers.CellDoubleClick  += dgvSuppliers_CellDoubleClick;

            var (gridOuter, gridInner) = CardPanel.CreateFill();
            gridInner.Controls.Add(dgvSuppliers);

            // ── Assemble pnlMain (RULE 5 — Fill first, Top afterwards) ──────
            pnlMain.Controls.Add(gridOuter);    // Fill  — grid card
            pnlMain.Controls.Add(kpiOuter);     // Top   — KPI strip
            pnlMain.Controls.Add(searchOuter);  // Top   — Search card
            pnlMain.Controls.Add(_shell);       // Top   — AppShell chrome (RULE 5)

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
            this.PerformLayout();

            // RULE 3 — post-layout re-enforcement
            _shell.Height      = AppShell.TotalHeight;
            _shell.MinimumSize = new Size(0, AppShell.TotalHeight);
        }

        // ── DataGridView factory ─────────────────────────────────────────────
        private DataGridView BuildGrid()
        {
            var dgv = new DataGridView
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
                Dock                   = DockStyle.Fill,
                ColumnHeadersHeight    = 46,
                EnableHeadersVisualStyles = false,
                RowTemplate            = { Height = 48 },
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
                    BackColor            = Color.White,
                    ForeColor            = Color.FromArgb(15, 31, 53),
                    SelectionBackColor   = Color.FromArgb(219, 234, 254),
                    SelectionForeColor   = Color.FromArgb(15, 31, 53),
                    Padding              = new Padding(12, 6, 12, 6)
                }
            };

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSupplierID",      HeaderText = "SUPPLIER ID",   FillWeight = 18 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSupplierName",     HeaderText = "SUPPLIER NAME", FillWeight = 28 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPhoneNumber",      HeaderText = "PHONE",         FillWeight = 18 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSupplierAddress",  HeaderText = "ADDRESS",       FillWeight = 36 });

            return dgv;
        }

        // ── Button & panel helpers ───────────────────────────────────────────
        private static Button MakePrimaryBtn(string text, int x, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(47, 111, 237),
                FlatStyle = FlatStyle.Flat,
                Location  = new Point(x, 0),
                Width     = w, Height = h,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize           = 0;
            b.FlatAppearance.MouseOverBackColor   = Color.FromArgb(26, 77, 192);
            b.FlatAppearance.MouseDownBackColor   = Color.FromArgb(21, 60, 155);
            return b;
        }

        private static Button MakeOutlineBtn(string text, int x, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location  = new Point(x, 0),
                Width     = w, Height = h,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderColor          = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize           = 1;
            b.FlatAppearance.MouseOverBackColor   = Color.FromArgb(240, 244, 249);
            return b;
        }

        /// <summary>Wraps a fixed-size button in a panel so it can sit inside a TLP cell.</summary>
        private static Panel MakeCentredBtnPanel(Button btn, int panelWidth)
        {
            var p = new Panel
            {
                Width     = panelWidth,
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            btn.Dock = DockStyle.Fill;
            p.Controls.Add(btn);
            return p;
        }
    }
}
