using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.AfterService
{
    partial class AccountPayableForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── AppShell ──────────────────────────────────────────────────────
        private AppShell _shell;

        // ── Search card controls ──────────────────────────────────────────
        private ComboBox cboStatus;
        private Button   btnSearch;
        private Button   btnReset;

        // ── KPI card ──────────────────────────────────────────────────────
        private Label lblTotalAP;
        private Label lblOutstanding;
        private Label lblOverdueCount;

        // ── Grid card ─────────────────────────────────────────────────────
        private DataGridView dgvAP;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            SuspendLayout();                                              // RULE 1

            this.Text          = "Premium Living OPS — After-Service  ›  Accounts Payable";
            this.Size          = new Size(1440, 900);
            this.MinimumSize   = new Size(1280, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = Color.FromArgb(240, 244, 249);
            this.WindowState   = FormWindowState.Maximized;
            this.Font          = new Font("Segoe UI", 13f);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.AutoScaleDimensions = new SizeF(7F, 15F);

            var pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };

            // ── AppShell (RULE 2) ──────────────────────────────────────────
            _shell = new AppShell();
            _shell.Dock        = DockStyle.Top;
            _shell.Height      = AppShell.TotalHeight;
            _shell.MinimumSize = new Size(0, AppShell.TotalHeight);
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;           // RULE 4
            _shell.LogoutClicked   += btnLogout_Click;                   // RULE 4
            _shell.SetPopupContainer(pnlMain);

            var pnlPage = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };

            // ── Search card (height 130) ───────────────────────────────────
            cboStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f), Dock = DockStyle.Fill
            };
            cboStatus.Items.AddRange(new object[] { "All", "Partial", "Full", "Overdue" });
            cboStatus.SelectedIndex = 0;
            btnSearch = MakePrimaryBtn("Search", Point.Empty, 160, 46);
            btnReset  = MakeOutlineBtn("Reset",  Point.Empty, 160, 46);
            btnSearch.Click += (s, e) => RefreshGrid();
            btnReset.Click  += (s, e) => { cboStatus.SelectedIndex = 0; RefreshGrid(); };

            var tblSearch = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(18, 10, 18, 10)
            };
            tblSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100f));
            tblSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200f));
            tblSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0f));
            tblSearch.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));
            tblSearch.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblSearch.Controls.Add(MakeLbl("Status"), 0, 0);
            tblSearch.Controls.Add(cboStatus,         1, 0);

            var pnlBtns = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnSearch.Location = new Point(0, 0);
            btnReset.Location  = new Point(172, 0);
            pnlBtns.Controls.AddRange(new Control[] { btnSearch, btnReset });
            tblSearch.SetColumnSpan(pnlBtns, 4);
            tblSearch.Controls.Add(pnlBtns, 0, 1);

            var (searchOuter, searchInner) = CardPanel.Create(outerHeight: 130);
            searchInner.Controls.Add(tblSearch);

            // ── KPI card (height 90) ──────────────────────────────────────
            lblTotalAP     = MakeKpiValueLbl("0",        Color.FromArgb(47, 111, 237));
            lblOutstanding = MakeKpiValueLbl("HK$ 0.00", Color.FromArgb(245, 158, 11));
            lblOverdueCount= MakeKpiValueLbl("0",        Color.FromArgb(232,  64,  64));

            var tblKpi = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(16, 8, 16, 8)
            };
            tblKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            tblKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            tblKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.4f));
            tblKpi.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblKpi.Controls.Add(MakeKpiCell("Total PO Invoices",   lblTotalAP),      0, 0);
            tblKpi.Controls.Add(MakeKpiCell("Outstanding Amount",  lblOutstanding),  1, 0);
            tblKpi.Controls.Add(MakeKpiCell("Overdue Count",       lblOverdueCount), 2, 0);

            var (kpiOuter, kpiInner) = CardPanel.Create(outerHeight: 90);
            kpiInner.Controls.Add(tblKpi);

            // ── Grid card (Fill) ─────────────────────────────────────────
            dgvAP = BuildDataGridView();
            dgvAP.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPurInvoice", HeaderText = "PURINVOICE ID", FillWeight = 16 });
            dgvAP.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPurchase",   HeaderText = "PURCHASE ID",   FillWeight = 14 });
            dgvAP.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSupplier",   HeaderText = "SUPPLIER",      FillWeight = 24 });
            dgvAP.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal",      HeaderText = "TOTAL",         FillWeight = 16 });
            dgvAP.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",     HeaderText = "STATUS",        FillWeight = 14 });
            dgvAP.Columns.Add(new DataGridViewTextBoxColumn { Name = "colExpected",   HeaderText = "EXPECTED DATE", FillWeight = 16 });
            dgvAP.CellFormatting += dgvAP_CellFormatting;

            var (gridOuter, gridInner) = CardPanel.CreateFill();
            gridInner.Controls.Add(dgvAP);

            // ── Add to pnlPage ─────────────────────────────────────────────
            pnlPage.Controls.Add(gridOuter);    // Fill
            pnlPage.Controls.Add(kpiOuter);     // Top
            pnlPage.Controls.Add(searchOuter);  // Top

            // ── Assemble pnlMain (RULE 5) ─────────────────────────────────
            pnlMain.Controls.Add(pnlPage);
            pnlMain.Controls.Add(_shell);

            this.Controls.Add(pnlMain);
            ResumeLayout(false);
            PerformLayout();
            _shell.Height      = AppShell.TotalHeight;                   // RULE 3
            _shell.MinimumSize = new Size(0, AppShell.TotalHeight);      // RULE 3
        }

        // ── Factory helpers ──────────────────────────────────────────────
        private static Label MakeLbl(string text) => new Label
        {
            Text = text, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(98, 112, 135),
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
        };

        private static Label MakeKpiValueLbl(string text, Color fg) => new Label
        {
            Text = text, Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = fg, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
        };

        private static Panel MakeKpiCell(string caption, Label valueLabel)
        {
            var tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            tlp.Controls.Add(new Label { Text = caption, Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(98, 112, 135), Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft, AutoSize = false }, 0, 0);
            valueLabel.Dock = DockStyle.Fill;
            tlp.Controls.Add(valueLabel, 0, 1);
            return tlp;
        }

        private static DataGridView BuildDataGridView() => new DataGridView
        {
            ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false, BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
            GridColor = Color.FromArgb(221, 227, 236), Font = new Font("Segoe UI", 12f),
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            RowTemplate = { Height = 46 }, Dock = DockStyle.Fill,
            ColumnHeadersHeight = 44, EnableHeadersVisualStyles = false,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(246, 249, 255), ForeColor = Color.FromArgb(98, 112, 135),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Padding = new Padding(12, 0, 0, 0), Alignment = DataGridViewContentAlignment.MiddleLeft
            },
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White, ForeColor = Color.FromArgb(15, 31, 53),
                SelectionBackColor = Color.FromArgb(219, 234, 254), SelectionForeColor = Color.FromArgb(15, 31, 53),
                Padding = new Padding(12, 6, 12, 6)
            }
        };

        private Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(47, 111, 237), FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(26, 77, 192);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(21, 60, 155);
            return b;
        }
        private Button MakeOutlineBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White, FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize  = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            return b;
        }
    }
}
