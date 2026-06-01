using PremiumLivingOPS.Views.Shared;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.InventoryControl
{
    partial class ViewRawMaterialForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Name          = "ViewRawMaterialForm";
            Text          = "Premium Living OPS — Inventory Control";
            Size          = new Size(1440, 900);
            MinimumSize   = new Size(1200, 720);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor     = Color.FromArgb(240, 244, 249);
            WindowState   = FormWindowState.Maximized;
            Font          = new Font("Segoe UI", 13f);

            pnlRoot = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };
            _shell  = new AppShell();
            _shell.SetPopupContainer(pnlRoot);

            pnlScroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(240, 244, 249) };

            // ── Search card ─────────────────────────────────────────────────────
            var (searchOuter, searchInner) = CardPanel.Create(outerHeight: 260,
                outerPadding: new Padding(20, 12, 20, 0));

            var tblSearchCard = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(18, 14, 18, 14)
            };
            tblSearchCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblSearchCard.RowStyles.Add(new RowStyle(SizeType.Absolute,  52f));
            tblSearchCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 110f));
            tblSearchCard.RowStyles.Add(new RowStyle(SizeType.Absolute,  68f));

            var pnlSearchTitle = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlSearchTitle.Controls.Add(new Label { Text = "Search Raw Materials", Font = new Font("Segoe UI", 13f, FontStyle.Bold), ForeColor = Color.FromArgb(15, 31, 53), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft });
            pnlSearchTitle.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(221, 227, 236) });

            TableLayoutPanel MakeCell(string caption, Control ctrl, bool rightPad = true)
            {
                var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None, Padding = rightPad ? new Padding(0, 0, 12, 0) : Padding.Empty };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                tlp.Controls.Add(new Label { Text = caption, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(98, 112, 135), Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft, Padding = new Padding(0,0,0,2) }, 0, 0);
                ctrl.Dock = DockStyle.Fill;
                tlp.Controls.Add(ctrl, 0, 1);
                return tlp;
            }

            txtSearch   = new TextBox { Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "Search material ID, name or category…" };
            txtSearch.KeyDown += (s, ke) => { if (ke.KeyCode == Keys.Enter) RefreshGrid(); };
            cboCategory = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            cboStatus   = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            cboStatus.Items.AddRange(new object[] { "All", "In Stock", "Low Stock", "Out of Stock" });
            cboStatus.SelectedIndex = 0;

            var tblFields = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None };
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            tblFields.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblFields.Controls.Add(MakeCell("Keyword",  txtSearch),        0, 0);
            tblFields.Controls.Add(MakeCell("Category", cboCategory),      1, 0);
            tblFields.Controls.Add(MakeCell("Status",   cboStatus, false), 2, 0);

            var pnlSearchBtns = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnSearch = MakePrimaryBtn("🔍  Search", new Point(0, 4),   210, 52);
            btnReset  = MakeOutlineBtn("↺  Reset",  new Point(218, 4), 210, 52);
            btnSearch.Click += (s, e) => RefreshGrid();
            btnReset.Click  += (s, e) => ResetFilters();
            pnlSearchBtns.Controls.Add(btnSearch);
            pnlSearchBtns.Controls.Add(btnReset);

            tblSearchCard.Controls.Add(pnlSearchTitle, 0, 0);
            tblSearchCard.Controls.Add(tblFields,      0, 1);
            tblSearchCard.Controls.Add(pnlSearchBtns,  0, 2);
            searchInner.Controls.Add(tblSearchCard);

            // ── Action bar card ─────────────────────────────────────────────────
            var (actionOuter, actionInner) = CardPanel.Create(outerHeight: 96,
                outerPadding: new Padding(20, 12, 20, 0));

            var tblAction = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None, Padding = Padding.Empty
            };
            tblAction.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblAction.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 1400f)); // 5×270 + gaps
            tblAction.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            pnlKpi = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            var pnlActionBtns = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnAddItem     = MakeGreenBtn  ("＋ Add New",       Point.Empty, 270, 60);
            btnViewDetail  = MakePrimaryBtn("🔍 View Detail",  Point.Empty, 270, 60);
            btnModifyItem  = MakeYellowBtn("✏  Modify Item",   Point.Empty, 270, 60);
            btnInwardGoods = MakePrimaryBtn("📥 Inward Goods",  Point.Empty, 270, 60);
            btnWhTransfer  = MakeOutlineBtn("🔄 WH Transfer",   Point.Empty, 270, 60);

            btnViewDetail.Enabled  = false;
            btnModifyItem.Enabled  = false;
            btnInwardGoods.Enabled = false;

            pnlActionBtns.Layout += (s, ev) =>
            {
                var p      = (Panel)s;
                var btns   = new Button[] { btnAddItem, btnViewDetail, btnModifyItem, btnInwardGoods, btnWhTransfer };
                int total  = 0; foreach (var b in btns) total += b.Width;
                int gaps   = (p.Width - total - 8) / (btns.Length - 1);
                int xCursor = 4;
                foreach (var b in btns)
                {
                    b.Left = xCursor;
                    b.Top  = (p.Height - b.Height) / 2;
                    xCursor += b.Width + gaps;
                }
            };
            pnlActionBtns.Controls.AddRange(new Control[] { btnAddItem, btnViewDetail, btnModifyItem, btnInwardGoods, btnWhTransfer });

            tblAction.Controls.Add(pnlKpi,        0, 0);
            tblAction.Controls.Add(pnlActionBtns, 1, 0);
            actionInner.Controls.Add(tblAction);

            // ── Table card ───────────────────────────────────────────────────────
            var (tableOuter, tableInner) = CardPanel.CreateFill(
                outerPadding: new Padding(20, 12, 20, 20));

            dgvMaterials = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                AllowUserToDeleteRows = false, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(221, 227, 236), Font = new Font("Segoe UI", 13f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                EnableHeadersVisualStyles = false, ColumnHeadersHeight = 46,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255), ForeColor = Color.FromArgb(98, 112, 135),
                    Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                    Padding = new Padding(12, 0, 0, 0), Alignment = DataGridViewContentAlignment.MiddleLeft
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.White, ForeColor = Color.FromArgb(15, 31, 53),
                    SelectionBackColor = Color.FromArgb(219, 234, 254),
                    SelectionForeColor = Color.FromArgb(15, 31, 53),
                    Padding = new Padding(12, 6, 12, 6)
                }
            };
            dgvMaterials.RowTemplate.Height = 48;

            dgvMaterials.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMaterialID",   HeaderText = "MATERIAL ID",   FillWeight = 14 });
            dgvMaterials.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMaterialName", HeaderText = "MATERIAL NAME", FillWeight = 30 });
            dgvMaterials.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCategory",     HeaderText = "CATEGORY",      FillWeight = 16 });
            dgvMaterials.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUnit",         HeaderText = "UNIT",          FillWeight =  8 });
            dgvMaterials.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUnitCost",     HeaderText = "UNIT COST",     FillWeight = 12 });
            dgvMaterials.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStockQty",     HeaderText = "STOCK QTY",     FillWeight = 10 });
            dgvMaterials.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",       HeaderText = "STATUS",        FillWeight = 10 });

            tableInner.Controls.Add(dgvMaterials);

            pnlScroll.Controls.Add(tableOuter);
            pnlScroll.Controls.Add(actionOuter);
            pnlScroll.Controls.Add(searchOuter);

            pnlRoot.Controls.Add(pnlScroll);
            pnlRoot.Controls.Add(_shell);
            Controls.Add(pnlRoot);
            ResumeLayout(false);
        }

        // ── Button factories ────────────────────────────────────────────────────
        private static Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 11f), ForeColor = Color.White, BackColor = Color.FromArgb(47, 111, 237), FlatStyle = FlatStyle.Flat, Location = loc, Size = new Size(w, h), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private static Button MakeGreenBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 11f), ForeColor = Color.White, BackColor = Color.FromArgb(22, 163, 74), FlatStyle = FlatStyle.Flat, Location = loc, Size = new Size(w, h), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private static Button MakeYellowBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.FromArgb(255, 255, 66),
                FlatStyle = FlatStyle.Flat,
                Location  = loc,
                Size      = new Size(w, h),
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private static Button MakeOutlineBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 11f), ForeColor = Color.FromArgb(98, 112, 135), BackColor = Color.White, FlatStyle = FlatStyle.Flat, Location = loc, Size = new Size(w, h), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236); b.FlatAppearance.BorderSize = 1;
            return b;
        }

        // ── Field declarations ────────────────────────────────────────────────
        private Panel        pnlRoot;
        private AppShell     _shell;
        private Panel        pnlScroll;
        internal Panel       pnlKpi;
        private TextBox      txtSearch;
        private ComboBox     cboCategory;
        private ComboBox     cboStatus;
        private Button       btnSearch;
        private Button       btnReset;
        private DataGridView dgvMaterials;
        private Button       btnViewDetail;
        private Button       btnAddItem;
        private Button       btnModifyItem;
        private Button       btnInwardGoods;
        private Button       btnWhTransfer;
    }
}
