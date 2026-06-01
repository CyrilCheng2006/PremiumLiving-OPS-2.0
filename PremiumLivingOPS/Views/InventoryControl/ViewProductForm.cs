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

            // ── Action bar button wiring ──────────────────────────────────────────────────
            btnViewDetail.Click    += (s, _) => OpenDetailDialog();
            btnAddItem.Click       += BtnAddItem_Click;
            btnModifyItem.Click    += BtnModifyItem_Click;
            btnInwardGoods.Click   += BtnInwardGoods_Click;
            btnWhTransfer.Click    += BtnWhTransfer_Click;

            LoadCategories();
            RefreshGrid();
        }

        // ════════════════════════════════════════════════════════════════
        //  Action handlers
        // ════════════════════════════════════════════════════════════════

        private void BtnAddItem_Click(object sender, EventArgs e)
        {
            using var frm = new AddItemForm(AddItemForm.ItemMode.Product);
            if (frm.ShowDialog(this) == DialogResult.OK)
                RefreshGrid();
        }

        private void BtnModifyItem_Click(object sender, EventArgs e)
        {
            string itemId = GetSelectedItemId("colItemID");
            if (itemId == null) return;
            using var frm = new ModifyItemForm(ModifyItemForm.ItemMode.Product, itemId);
            if (frm.ShowDialog(this) == DialogResult.OK)
                RefreshGrid();
        }

        private void BtnInwardGoods_Click(object sender, EventArgs e)
        {
            string itemId = GetSelectedItemId("colItemID");
            using var frm = new InwardGoodsForm(itemId);
            if (frm.ShowDialog(this) == DialogResult.OK)
                RefreshGrid();
        }

        private void BtnWhTransfer_Click(object sender, EventArgs e)
        {
            using var frm = new WarehouseTransferForm();
            if (frm.ShowDialog(this) == DialogResult.OK)
                RefreshGrid();
        }

        // ════════════════════════════════════════════════════════════════
        //  Helpers
        // ════════════════════════════════════════════════════════════════

        private string GetSelectedItemId(string columnName)
        {
            if (dgvProducts.SelectedRows.Count == 0) return null;
            return dgvProducts.SelectedRows[0].Cells[columnName].Value?.ToString();
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
            _shell.SetBreadcrumb("Inventory Control  \u203a  View Product");

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

            const int PillW   = 310;
            const int PillH   = 60;
            const int Gap     = 10;
            const int NumColW = 80;
            const int LeftPad = 12;

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
        {
            bool hasSelection = dgvProducts.SelectedRows.Count > 0;
            btnViewDetail.Enabled  = hasSelection;
            btnModifyItem.Enabled  = hasSelection;
            btnInwardGoods.Enabled = hasSelection;
        }

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
            string itemId = row.Cells["colItemID"].Value?.ToString();

            // Fetch full record from controller to get ItemDescription
            var vm = _ctrl.GetModifyProductVM(itemId);
            if (vm?.Product == null) return;
            var p = vm.Product;

            string itemName = p.ItemName;
            string itemDesc = p.ItemDescription ?? "—";
            string category = p.Category;
            string price    = $"HK$ {p.SalesPrice:N2}";
            string stockQty = p.StockQty.ToString();
            string status   = p.StockStatus;

            using var dlg = new Form
            {
                Text            = $"Product Detail — {itemId}",
                Size            = new Size(1200, 800),
                MinimumSize     = new Size(900, 650),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.FromArgb(240, 244, 249),
                Font            = new Font("Segoe UI", 12f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false
            };

            // ── Header ──────────────────────────────────────────────────────────────
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Color.FromArgb(19, 35, 61) };
            pnlHeader.Controls.Add(new Label
            {
                Text      = $"Product Detail  —  {itemId}",
                Font      = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(32, 0, 0, 0)
            });

            // ── Footer ──────────────────────────────────────────────────────────────
            var pnlFoot = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 72,
                Padding   = new Padding(0, 12, 32, 12),
                BackColor = Color.FromArgb(248, 250, 253)
            };
            pnlFoot.Paint += (s, e) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0);
            };

            var btnClose = new Button
            {
                Text      = "Close",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock      = DockStyle.Right,
                Width     = 160,
                Cursor    = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(200, 207, 220);
            btnClose.FlatAppearance.BorderSize  = 1;
            btnClose.Click += (s, ev) => dlg.Close();
            pnlFoot.Controls.Add(btnClose);

            // ── Scrollable body ──────────────────────────────────────────────────────
            var pnlScroll = new Panel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                BackColor  = Color.FromArgb(240, 244, 249),
                Padding    = new Padding(36, 24, 36, 16)
            };

            // ── Card wrapping fields ──────────────────────────────────────────────
            var (outerCard, innerCard) = CardPanel.Create(outerHeight: 100,
                outerPadding: new Padding(0));
            innerCard.Padding = new Padding(32, 24, 32, 24);

            const int RowH     = 66;
            const int RowGap   = 2;
            const int NumRows  = 7;   // ItemID, ItemName, Description, Category, SalesPrice, StockQty, Status
            const int LabelCol = 260;

            var tbl = new TableLayoutPanel
            {
                Dock            = DockStyle.Top,
                Height          = RowH * NumRows + RowGap * (NumRows - 1),
                ColumnCount     = 2,
                RowCount        = NumRows,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LabelCol));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            for (int i = 0; i < NumRows; i++)
                tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, RowH));

            // Schema fields: Item(ItemID, ItemName, ItemDescription) + Product(SalesPrice, Category) + computed(StockQty, StockStatus)
            var fields = new[]
            {
                ("Item ID",      itemId),
                ("Item Name",    itemName),
                ("Description",  itemDesc),
                ("Category",     category),
                ("Sales Price",  price),
                ("Stock Qty",    stockQty),
                ("Status",       status)
            };

            for (int i = 0; i < fields.Length; i++)
            {
                if (i > 0)
                {
                    var prev = tbl.GetControlFromPosition(0, i - 1);
                    if (prev != null)
                    {
                        var div = new Panel { Height = 1, Dock = DockStyle.Bottom, BackColor = Color.FromArgb(221, 227, 236) };
                        prev.Controls.Add(div);
                    }
                }

                tbl.Controls.Add(new Label
                {
                    Text      = fields[i].Item1,
                    Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(98, 112, 135),
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding   = new Padding(0, 0, 16, 0)
                }, 0, i);

                tbl.Controls.Add(new Label
                {
                    Text      = fields[i].Item2 ?? "—",
                    Font      = new Font("Segoe UI", 12f),
                    ForeColor = Color.FromArgb(15, 31, 53),
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                }, 1, i);
            }

            // Set card height to fit content
            int cardH = tbl.Height + innerCard.Padding.Vertical + 16;
            outerCard.Height = cardH;
            innerCard.Height = cardH - 16;

            innerCard.Controls.Add(tbl);
            pnlScroll.Controls.Add(outerCard);

            dlg.Load += (s, e) =>
            {
                outerCard.Width  = pnlScroll.ClientSize.Width - pnlScroll.Padding.Horizontal;
                outerCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            };
            pnlScroll.Resize += (s, e) =>
            {
                outerCard.Width = pnlScroll.ClientSize.Width - pnlScroll.Padding.Horizontal;
            };

            dlg.Controls.Add(pnlScroll);
            dlg.Controls.Add(pnlFoot);
            dlg.Controls.Add(pnlHeader);
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
