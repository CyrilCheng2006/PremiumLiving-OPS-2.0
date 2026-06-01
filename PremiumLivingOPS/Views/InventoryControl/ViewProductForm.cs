using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.InventoryControl
{
    public partial class ViewProductForm : Form
    {
        private readonly InventoryControlController _ctrl =
            new InventoryControlController();

        private List<ProductEntity> _currentProducts = new List<ProductEntity>();

        private static readonly Dictionary<string, (Color bg, Color fg)> StatusColors =
            new Dictionary<string, (Color, Color)>
            {
                { "In Stock",     (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
                { "Low Stock",    (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "Out of Stock", (Color.FromArgb(254, 226, 226), Color.FromArgb(153,  27,  27)) }
            };

        public ViewProductForm()
        {
            InitializeComponent();
            this.Load += ViewProductForm_Load;
        }

        private void ViewProductForm_Load(object sender, EventArgs e)
        {
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += BtnLogout_Click;

            dgvProducts.SelectionChanged += (s, _) => UpdateActionButtons();
            dgvProducts.CellDoubleClick  += (s, ce) => { if (ce.RowIndex >= 0) OpenDetailDialog(); };
            dgvProducts.CellFormatting   += DgvProducts_CellFormatting;

            btnViewDetail.Click += (s, _) => OpenDetailDialog();

            LoadCategories();
            RefreshGrid();
        }

        private void LoadCategories()
        {
            cboCategory.Items.Clear();
            foreach (var c in _ctrl.GetProductCategories())
                cboCategory.Items.Add(c);
            if (cboCategory.Items.Count > 0)
                cboCategory.SelectedIndex = 0;
        }

        internal void RefreshGrid()
        {
            string keyword  = txtSearch.Text.Trim();
            string category = cboCategory.SelectedItem?.ToString();
            string status   = cboStatus.SelectedItem?.ToString();

            var vm = _ctrl.GetViewProductVM(
                string.IsNullOrEmpty(keyword) ? null : keyword,
                category == "All"             ? null : category);

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Inventory Control  ›  View Product");

            _currentProducts = vm.Products;
            if (!string.IsNullOrEmpty(status) && status != "All")
                _currentProducts = _currentProducts.FindAll(p => p.StockStatus == status);

            dgvProducts.Rows.Clear();
            foreach (var p in _currentProducts)
                dgvProducts.Rows.Add(
                    p.ItemID,
                    p.ItemName,
                    p.Category,
                    $"HK$ {p.SalesPrice:N2}",
                    p.StockQty,
                    p.StockStatus);

            RefreshKpi();
            UpdateActionButtons();
        }

        internal void ResetFilters()
        {
            txtSearch.Text = string.Empty;
            if (cboCategory.Items.Count > 0) cboCategory.SelectedIndex = 0;
            cboStatus.SelectedIndex = 0;
            RefreshGrid();
        }

        private void RefreshKpi()
        {
            pnlKpi.Controls.Clear();

            var allProducts = _ctrl.GetViewProductVM().Products;

            int total    = allProducts.Count;
            int inStock  = allProducts.FindAll(p => p.StockStatus == "In Stock").Count;
            int lowStock = allProducts.FindAll(p => p.StockStatus == "Low Stock").Count;
            int outStock = allProducts.FindAll(p => p.StockStatus == "Out of Stock").Count;

            var pills = new[]
            {
                ("Total",        total.ToString(),    Color.FromArgb( 47, 111, 237), Color.FromArgb(219, 234, 254), "All"),
                ("In Stock",     inStock.ToString(),  Color.FromArgb(  6,  95,  70), Color.FromArgb(209, 250, 229), "In Stock"),
                ("Low Stock",    lowStock.ToString(), Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199), "Low Stock"),
                ("Out of Stock", outStock.ToString(), Color.FromArgb(153,  27,  27), Color.FromArgb(254, 226, 226), "Out of Stock"),
            };

            const int PillW   = 340;
            const int PillH   = 60;
            const int Gap     = 8;
            const int NumColW = 80;
            const int LeftPad = 12;   // 左側留白

            // flow 不用 Dock，用 AutoSize 讓它自身寬度剛好包住所有 pills
            var flow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent,
                Padding       = new Padding(0),
                AutoScroll    = false,
                AutoSize      = true,
                AutoSizeMode  = AutoSizeMode.GrowAndShrink
            };

            foreach (var (label, count, fg, bg, filterStatus) in pills)
            {
                var pill = new Panel
                {
                    BackColor = bg,
                    Size      = new Size(PillW, PillH),
                    Margin    = new Padding(0, 0, Gap, 0),
                    Cursor    = Cursors.Hand
                };
                pill.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path  = RoundedRect(((Panel)s).ClientRectangle, 8);
                    using var brush = new SolidBrush(((Panel)s).BackColor);
                    e.Graphics.FillPath(brush, path);
                };

                var tlp = new TableLayoutPanel
                {
                    Dock            = DockStyle.Fill,
                    ColumnCount     = 2,
                    RowCount        = 1,
                    BackColor       = Color.Transparent,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding         = new Padding(10, 0, 8, 0)
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, NumColW));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                tlp.Controls.Add(new Label
                {
                    Text      = count,
                    Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                    ForeColor = fg,
                    BackColor = Color.Transparent,
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize  = false
                }, 0, 0);

                tlp.Controls.Add(new Label
                {
                    Text      = label,
                    Font      = new Font("Segoe UI", 12f),
                    ForeColor = fg,
                    BackColor = Color.Transparent,
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoSize  = false
                }, 1, 0);

                string localStatus = filterStatus;
                EventHandler clickHandler = (s, e) =>
                {
                    int idx = cboStatus.FindStringExact(localStatus);
                    if (idx >= 0) cboStatus.SelectedIndex = idx;
                    RefreshGrid();
                };
                pill.Click += clickHandler;
                tlp.Click  += clickHandler;
                foreach (Control c in tlp.Controls) c.Click += clickHandler;

                pill.Controls.Add(tlp);
                flow.Controls.Add(pill);
            }

            // wrapper: Dock=Fill, 透過 Layout 事件把 flow 靠左垄直置中
            var wrapper = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            wrapper.Controls.Add(flow);
            wrapper.Layout += (s, e) =>
            {
                var w = (Panel)s;
                flow.Left = LeftPad;
                flow.Top  = (w.Height - PillH) / 2;
            };

            pnlKpi.Controls.Add(wrapper);
        }

        private void UpdateActionButtons()
            => btnViewDetail.Enabled = dgvProducts.SelectedRows.Count > 0;

        private void DgvProducts_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvProducts.Columns[e.ColumnIndex].Name != "colStatus" || e.Value == null) return;
            string val = e.Value.ToString();
            e.FormattingApplied = true;
            if (StatusColors.TryGetValue(val, out var colors))
            {
                e.CellStyle.ForeColor            = colors.fg;
                e.CellStyle.BackColor            = colors.bg;
                e.CellStyle.SelectionForeColor   = colors.fg;
                e.CellStyle.SelectionBackColor   = colors.bg;
                e.CellStyle.Font                 = new Font("Segoe UI", 11f, FontStyle.Bold);
                e.CellStyle.Alignment            = DataGridViewContentAlignment.MiddleCenter;
            }
        }

        private void OpenDetailDialog()
        {
            if (dgvProducts.SelectedRows.Count == 0) return;
            var row     = dgvProducts.SelectedRows[0];
            string itemId   = row.Cells["colItemID"].Value?.ToString();
            string itemName = row.Cells["colItemName"].Value?.ToString();
            string category = row.Cells["colCategory"].Value?.ToString();
            string price    = row.Cells["colPrice"].Value?.ToString();
            string stockQty = row.Cells["colStockQty"].Value?.ToString();
            string status   = row.Cells["colStatus"].Value?.ToString();

            using var dlg = new Form
            {
                Text            = $"Product Detail — {itemId}",
                Size            = new Size(520, 380),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 11f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false
            };

            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.FromArgb(19, 35, 61) };
            pnlHeader.Controls.Add(new Label
            {
                Text      = $"Product Detail  —  {itemId}",
                Font      = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(20, 0, 0, 0)
            });

            var pnlBody = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 16, 24, 16), BackColor = Color.White };
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 6,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            for (int i = 0; i < 6; i++) tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 6f));

            var fields = new[]
            {
                ("Item ID",     itemId),
                ("Item Name",   itemName),
                ("Category",    category),
                ("Sales Price", price),
                ("Stock Qty",   stockQty),
                ("Status",      status)
            };
            for (int i = 0; i < fields.Length; i++)
            {
                tbl.Controls.Add(new Label { Text = fields[i].Item1, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(98, 112, 135), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, i);
                tbl.Controls.Add(new Label { Text = fields[i].Item2 ?? "—", Font = new Font("Segoe UI", 11f), ForeColor = Color.FromArgb(15, 31, 53), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 1, i);
            }
            pnlBody.Controls.Add(tbl);

            var pnlFoot = new Panel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(0, 10, 20, 10) };
            var btnClose = new Button
            {
                Text = "Close", Font = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat, Dock = DockStyle.Right, Width = 120, Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            btnClose.Click += (s, ev) => dlg.Close();
            pnlFoot.Controls.Add(btnClose);

            dlg.Controls.Add(pnlBody);
            dlg.Controls.Add(pnlHeader);
            dlg.Controls.Add(pnlFoot);
            dlg.ShowDialog(this);
        }

        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath(); int d = radius * 2;
            path.AddArc(r.X,         r.Y,          d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y,          d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d,   0, 90);
            path.AddArc(r.X,         r.Bottom - d, d, d,  90, 90);
            path.CloseFigure(); return path;
        }
    }
}
