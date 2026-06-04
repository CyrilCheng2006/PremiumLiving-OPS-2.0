using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.AfterService
{
    partial class AccountPayableForm
    {
        private System.ComponentModel.IContainer components = null;

        private AppShell     _shell;
        private ComboBox     cboStatus;
        private Button       btnSearch;
        private Button       btnReset;
        private Panel        pnlKpi;
        private DataGridView dgvAP;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text          = "Premium Living OPS — Account Payable";
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
            // CARD 1 — Filter Bar  (outerHeight 120)
            // ══════════════════════════════════════════════════════════════════
            var (filterOuter, filterInner) = CardPanel.Create(outerHeight: 120);

            cboStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            cboStatus.Items.AddRange(new object[] { "All", "Partial", "Full", "Overdue" });
            cboStatus.SelectedIndex = 0;

            btnSearch = MakePrimaryBtn("🔍  Filter", Point.Empty, 190, 52);
            btnReset  = MakeOutlineBtn("↺  Reset",  Point.Empty, 190, 52);
            btnSearch.Click += (s, e) => RefreshGrid();
            btnReset.Click  += (s, e) => { cboStatus.SelectedIndex = 0; RefreshGrid(); };

            var tblFilter = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(18, 8, 18, 8)
            };
            tblFilter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblFilter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblFilter.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 205f));
            tblFilter.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 205f));
            tblFilter.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            tblFilter.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            tblFilter.Controls.Add(MakeFieldLabel("Payment Status"), 0, 0);
            cboStatus.Dock = DockStyle.Fill;
            tblFilter.Controls.Add(cboStatus, 0, 1);

            var pnlBtns = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnSearch.Location = new Point(0, 0);
            btnReset.Location  = new Point(198, 0);
            pnlBtns.Controls.Add(btnSearch);
            pnlBtns.Controls.Add(btnReset);
            tblFilter.SetColumnSpan(pnlBtns, 3);
            tblFilter.Controls.Add(pnlBtns, 1, 1);
            filterInner.Controls.Add(tblFilter);

            // ══════════════════════════════════════════════════════════════════
            // CARD 2 — KPI Summary  (outerHeight 90)
            // ══════════════════════════════════════════════════════════════════
            var (kpiOuter, kpiInner) = CardPanel.Create(outerHeight: 90);
            pnlKpi = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12, 10, 12, 10) };
            kpiInner.Controls.Add(pnlKpi);

            // ══════════════════════════════════════════════════════════════════
            // CARD 3 — AP Grid  (Fill)
            // ══════════════════════════════════════════════════════════════════
            var (gridOuter, gridInner) = CardPanel.CreateFill();

            dgvAP = new DataGridView
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
            dgvAP.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPurInvID",  HeaderText = "PUR. INVOICE ID",   FillWeight = 18 });
            dgvAP.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPurchaseID",HeaderText = "PURCHASE ORDER",    FillWeight = 16 });
            dgvAP.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSupplier",  HeaderText = "SUPPLIER",          FillWeight = 22 });
            dgvAP.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal",     HeaderText = "TOTAL (HK$)",       FillWeight = 14 });
            dgvAP.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",    HeaderText = "STATUS",            FillWeight = 12 });
            dgvAP.Columns.Add(new DataGridViewTextBoxColumn { Name = "colExpected",  HeaderText = "EXPECTED DATE",     FillWeight = 14 });
            dgvAP.CellFormatting += dgvAP_CellFormatting;

            gridInner.Controls.Add(dgvAP);

            // ── Assemble
            pnlMain.Controls.Add(gridOuter);   // Fill
            pnlMain.Controls.Add(kpiOuter);    // Top
            pnlMain.Controls.Add(filterOuter); // Top
            pnlMain.Controls.Add(_shell);      // Top — topmost

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        private static Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.White, BackColor = Palette.Primary, FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0; b.FlatAppearance.MouseOverBackColor = Palette.PrimaryDark; return b;
        }
        private static Button MakeOutlineBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f), ForeColor = Palette.TextMain, BackColor = Color.White, FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderColor = Palette.BorderColor; b.FlatAppearance.BorderSize = 1; b.FlatAppearance.MouseOverBackColor = Palette.BgPage; return b;
        }
        private static Label MakeFieldLabel(string text) => new Label { Text = text, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Palette.TextMuted, Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft, Padding = new Padding(0, 0, 0, 2) };
    }
}
