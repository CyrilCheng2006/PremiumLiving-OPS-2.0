using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.ProductionProcessing
{
    partial class SearchMaterialRequestForm
    {
        private System.ComponentModel.IContainer components = null;

        private AppShell                          _shell;
        private System.Windows.Forms.Panel        pnlKpi;
        private System.Windows.Forms.TextBox      txtKeyword;
        private System.Windows.Forms.ComboBox     cboUrgency;
        private System.Windows.Forms.ComboBox     cboTrigger;
        private System.Windows.Forms.Button       btnSearch;
        private System.Windows.Forms.Button       btnReset;
        private System.Windows.Forms.Button       btnViewDetail;
        private System.Windows.Forms.Button       btnCreateNew;
        private System.Windows.Forms.DataGridView dgvRequests;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text          = "Production Processing — Search Raw Material Request";
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
            _shell.SetPopupContainer(pnlMain);
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += BtnLogout_Click;

            // ────────────────────────────────────────────────────────
            // FILTER CARD
            // ────────────────────────────────────────────────────────
            txtKeyword = new TextBox
            {
                Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                PlaceholderText = "Request ID / Material Name / Item ID"
            };
            txtKeyword.KeyDown += (s, ke) => { if (ke.KeyCode == Keys.Enter) RefreshGrid(); };

            cboUrgency = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 12f), Dock = DockStyle.Fill
            };
            cboUrgency.Items.AddRange(new object[] { "All", "Critical", "High", "Medium" });
            cboUrgency.SelectedIndex = 0;

            cboTrigger = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 12f), Dock = DockStyle.Fill
            };
            cboTrigger.Items.AddRange(new object[] { "All", "Reorder", "OrderDemand" });
            cboTrigger.SelectedIndex = 0;

            TableLayoutPanel MakeCell(string caption, Control ctrl)
            {
                var tlp = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1,
                    BackColor = Color.Transparent,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding = new Padding(0, 0, 12, 0)
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 70f));
                var lbl = new Label
                {
                    Text = caption, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(98, 112, 135), Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.BottomLeft, Padding = new Padding(0, 0, 0, 2)
                };
                ctrl.Dock = DockStyle.Fill;
                tlp.Controls.Add(lbl,  0, 0);
                tlp.Controls.Add(ctrl, 0, 1);
                return tlp;
            }

            var tblFields = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            tblFields.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblFields.Controls.Add(MakeCell("Keyword",      txtKeyword), 0, 0);
            tblFields.Controls.Add(MakeCell("Urgency",      cboUrgency), 1, 0);
            tblFields.Controls.Add(MakeCell("Trigger Type", cboTrigger), 2, 0);

            var pnlBtns = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnSearch = MakePrimaryBtn("🔍  Search", new Point(0,   0), 200, 56);
            btnReset  = MakeOutlineBtn("↺  Reset",  new Point(208, 0), 160, 56);
            btnSearch.Click += (s, e) => RefreshGrid();
            btnReset.Click  += (s, e) => ResetFilters();
            pnlBtns.Controls.Add(btnSearch);
            pnlBtns.Controls.Add(btnReset);

            var tblFilterCard = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(18, 12, 18, 12)
            };
            tblFilterCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblFilterCard.RowStyles.Add(new RowStyle(SizeType.Absolute,  52f));
            tblFilterCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 110f));
            tblFilterCard.RowStyles.Add(new RowStyle(SizeType.Absolute,  64f));

            var pnlTitle = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var lblTitle = new Label
            {
                Text = "Search Raw Material Requests",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            };
            var divider = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(221, 227, 236) };
            pnlTitle.Controls.Add(lblTitle);
            pnlTitle.Controls.Add(divider);
            tblFilterCard.Controls.Add(pnlTitle,  0, 0);
            tblFilterCard.Controls.Add(tblFields, 0, 1);
            tblFilterCard.Controls.Add(pnlBtns,   0, 2);

            var pnlFilterCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlFilterCard.Paint += PaintCardBorder;
            pnlFilterCard.Controls.Add(tblFilterCard);

            var pnlFilterOuter = new Panel
            {
                Dock = DockStyle.Top, Height = 260,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding = new Padding(20, 14, 20, 8)
            };
            pnlFilterOuter.Controls.Add(pnlFilterCard);

            // ────────────────────────────────────────────────────────
            // KPI BAR
            // ────────────────────────────────────────────────────────
            pnlKpi = new Panel
            {
                Dock = DockStyle.Fill, BackColor = Color.Transparent,
                Padding = new Padding(12, 10, 12, 10)
            };

            const int BtnW = 280, BtnH = 58, BtnGap = 8, BtnPad = 12;

            btnViewDetail = MakePrimaryBtn("🔍  View Detail", Point.Empty, BtnW, BtnH);
            btnCreateNew  = MakeOutlineBtn("+ Create New",   Point.Empty, BtnW, BtnH);
            btnViewDetail.Enabled = false;
            btnViewDetail.Click  += (s, e) => OpenDetailDialog();
            btnCreateNew.Click   += BtnCreateNew_Click;

            var pnlActionBtns = new Panel
            {
                Dock = DockStyle.Right,
                Width = BtnPad + BtnW + BtnGap + BtnW + BtnPad,
                BackColor = Color.Transparent
            };
            void CentreActionBtns()
            {
                int top = (pnlActionBtns.Height - BtnH) / 2;
                if (top < 0) top = 0;
                btnViewDetail.Location = new Point(BtnPad, top);
                btnCreateNew.Location  = new Point(BtnPad + BtnW + BtnGap, top);
            }
            pnlActionBtns.Controls.Add(btnViewDetail);
            pnlActionBtns.Controls.Add(btnCreateNew);
            pnlActionBtns.Resize += (s, e) => CentreActionBtns();

            var pnlKpiRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlKpiRow.Controls.Add(pnlKpi);
            pnlKpiRow.Controls.Add(pnlActionBtns);

            var pnlKpiInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlKpiInner.Paint += PaintCardBorder;
            pnlKpiInner.Controls.Add(pnlKpiRow);

            var pnlKpiOuter = new Panel
            {
                Dock = DockStyle.Top, Height = 88,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding = new Padding(20, 8, 20, 8)
            };
            pnlKpiOuter.Controls.Add(pnlKpiInner);

            // ────────────────────────────────────────────────────────
            // MAIN GRID
            // Columns are BATCH-LEVEL only — no -NN per-item columns.
            // Per-item detail is shown exclusively in the View Detail dialog.
            // ────────────────────────────────────────────────────────
            dgvRequests = new DataGridView
            {
                ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(221, 227, 236),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                Font = new Font("Segoe UI", 13f), RowTemplate = { Height = 44 },
                Dock = DockStyle.Fill, ColumnHeadersHeight = 44,
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
            dgvRequests.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);

            // !! Columns represent ONE batch (BatchPrefix), NOT individual -NN lines !!
            dgvRequests.Columns.AddRange(new DataGridViewColumn[]
            {
                // col name          header text           weight  meaning
                new DataGridViewTextBoxColumn { Name = "colRequestID", HeaderText = "REQUEST ID",      FillWeight = 22 },  // MRQ-YYMMDD-NNN
                new DataGridViewTextBoxColumn { Name = "colLines",     HeaderText = "ITEMS",           FillWeight =  8 },  // count of -NN lines
                new DataGridViewTextBoxColumn { Name = "colTotalQty",  HeaderText = "TOTAL REQ. QTY",  FillWeight = 12 },  // SUM qty
                new DataGridViewTextBoxColumn { Name = "colUrgency",   HeaderText = "URGENCY",         FillWeight = 12 },
                new DataGridViewTextBoxColumn { Name = "colTrigger",   HeaderText = "TRIGGER",         FillWeight = 13 },
                new DataGridViewTextBoxColumn { Name = "colOrderID",   HeaderText = "LINKED ORDER",    FillWeight = 17 },
                new DataGridViewTextBoxColumn { Name = "colLinkedPO",  HeaderText = "LINKED TO PO",    FillWeight = 11 },
                new DataGridViewTextBoxColumn { Name = "colStockNote", HeaderText = "STOCK STATUS",    FillWeight = 15 },  // aggregated note
            });

            var pnlGridInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlGridInner.Paint += PaintCardBorder;
            pnlGridInner.Controls.Add(dgvRequests);

            var pnlGridOuter = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding = new Padding(20, 8, 20, 14)
            };
            pnlGridOuter.Controls.Add(pnlGridInner);

            // ── Assemble pnlMain (RULE 5: Fill first, then Top in reverse, _shell last)
            pnlMain.Controls.Add(pnlGridOuter);   // Fill
            pnlMain.Controls.Add(pnlKpiOuter);    // Top
            pnlMain.Controls.Add(pnlFilterOuter); // Top
            pnlMain.Controls.Add(_shell);          // Top (topmost — RULE 5)

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        private Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(47, 111, 237),
                FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(26, 77, 192);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(21, 60, 155);
            return b;
        }

        private Button MakeOutlineBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text = text, Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize  = 1;
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
