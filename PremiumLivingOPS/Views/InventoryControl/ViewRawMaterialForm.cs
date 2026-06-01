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
    public partial class ViewRawMaterialForm : Form
    {
        private readonly InventoryControlController _ctrl =
            new InventoryControlController();

        private List<RawMaterialEntity> _currentMaterials = new List<RawMaterialEntity>();

        // Status badge colours (keyed on StockStatus computed value)
        private static readonly Dictionary<string, (Color bg, Color fg)> StatusColors =
            new Dictionary<string, (Color, Color)>
            {
                { "In Stock",     (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
                { "Low Stock",    (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "Out of Stock", (Color.FromArgb(254, 226, 226), Color.FromArgb(153,  27,  27)) }
            };

        public ViewRawMaterialForm()
        {
            InitializeComponent();
            this.Load += ViewRawMaterialForm_Load;
        }

        private void ViewRawMaterialForm_Load(object sender, EventArgs e)
        {
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += BtnLogout_Click;

            dgvMaterials.SelectionChanged += (s, _) => UpdateActionButtons();
            dgvMaterials.CellDoubleClick  += (s, ce) => { if (ce.RowIndex >= 0) OpenDetailDialog(); };
            dgvMaterials.CellFormatting   += DgvMaterials_CellFormatting;

            btnViewDetail.Click  += (s, _) => OpenDetailDialog();
            btnAddItem.Click     += BtnAddItem_Click;
            btnModifyItem.Click  += BtnModifyItem_Click;
            btnInwardGoods.Click += BtnInwardGoods_Click;
            btnWhTransfer.Click  += BtnWhTransfer_Click;

            LoadMaterialTypeFilter();
            RefreshGrid();
        }

        // ════════════════════════════════════════════════════════════════
        //  Action handlers
        // ════════════════════════════════════════════════════════════════

        private void BtnAddItem_Click(object sender, EventArgs e)
        {
            using var frm = new AddItemForm(AddItemForm.ItemMode.RawMaterial);
            if (frm.ShowDialog(this) == DialogResult.OK)
                RefreshGrid();
        }

        private void BtnModifyItem_Click(object sender, EventArgs e)
        {
            string itemId = GetSelectedItemId("colMaterialID");
            if (itemId == null) return;
            using var frm = new ModifyItemForm(ModifyItemForm.ItemMode.RawMaterial, itemId);
            if (frm.ShowDialog(this) == DialogResult.OK)
                RefreshGrid();
        }

        private void BtnInwardGoods_Click(object sender, EventArgs e)
        {
            string itemId = GetSelectedItemId("colMaterialID");
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
            if (dgvMaterials.SelectedRows.Count == 0) return null;
            return dgvMaterials.SelectedRows[0].Cells[columnName].Value?.ToString();
        }

        private void LoadMaterialTypeFilter()
        {
            cboMaterialType.Items.Clear();
            cboMaterialType.Items.Add("All");
            foreach (var t in new[] { "Wood", "Metal", "Fabric", "Foam", "Glass", "Paint" })
                cboMaterialType.Items.Add(t);
            cboMaterialType.SelectedIndex = 0;
        }

        internal void RefreshGrid()
        {
            string keyword      = txtSearch.Text.Trim();
            string materialType = cboMaterialType.SelectedItem?.ToString();
            string status       = cboStatus.SelectedItem?.ToString();

            var vm = _ctrl.GetViewRawMaterialVM(
                string.IsNullOrEmpty(keyword)                                     ? null : keyword,
                materialType == "All" || string.IsNullOrEmpty(materialType)       ? null : materialType);

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Inventory Control  \u203a  View Raw Material");

            _currentMaterials = vm.Materials;

            if (!string.IsNullOrEmpty(status) && status != "All")
                _currentMaterials = _currentMaterials.FindAll(m => m.StockStatus == status);

            dgvMaterials.Rows.Clear();
            foreach (var m in _currentMaterials)
                dgvMaterials.Rows.Add(
                    m.MaterialID,
                    m.MaterialName,
                    m.Category,
                    $"HK$ {m.UnitCost:N2}",
                    m.StockQty,
                    m.ReorderLevel,
                    m.StockStatus);

            RefreshKpi();
            UpdateActionButtons();
        }

        internal void ResetFilters()
        {
            txtSearch.Text = string.Empty;
            if (cboMaterialType.Items.Count > 0) cboMaterialType.SelectedIndex = 0;
            cboStatus.SelectedIndex = 0;
            RefreshGrid();
        }

        // ────────────────────────────────────────────────────────────────
        //  KPI pills
        // ────────────────────────────────────────────────────────────────
        private void RefreshKpi()
        {
            pnlKpi.Controls.Clear();

            var allMaterials = _ctrl.GetViewRawMaterialVM().Materials;

            int total    = allMaterials.Count;
            int inStock  = allMaterials.FindAll(m => m.StockStatus == "In Stock").Count;
            int lowStock = allMaterials.FindAll(m => m.StockStatus == "Low Stock").Count;
            int outStock = allMaterials.FindAll(m => m.StockStatus == "Out of Stock").Count;

            var pills = new[]
            {
                ("Total",        total.ToString(),    Color.FromArgb( 47, 111, 237), Color.FromArgb(219, 234, 254), "All"),
                ("In Stock",     inStock.ToString(),  Color.FromArgb(  6,  95,  70), Color.FromArgb(209, 250, 229), "In Stock"),
                ("Low Stock",    lowStock.ToString(), Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199), "Low Stock"),
                ("Out of Stock", outStock.ToString(), Color.FromArgb(153,  27,  27), Color.FromArgb(254, 226, 226), "Out of Stock"),
            };

            const int PillW   = 280;
            const int PillH   = 60;
            const int Gap     = 8;
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
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
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
            bool hasSelection = dgvMaterials.SelectedRows.Count > 0;
            btnViewDetail.Enabled  = hasSelection;
            btnModifyItem.Enabled  = hasSelection;
            btnInwardGoods.Enabled = hasSelection;
        }

        // ────────────────────────────────────────────────────────────────
        //  Cell formatting  — Status badge
        // ────────────────────────────────────────────────────────────────
        private void DgvMaterials_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvMaterials.Columns[e.ColumnIndex].Name != "colStatus" || e.Value == null) return;
            string val = e.Value.ToString();
            e.FormattingApplied = true;
            if (StatusColors.TryGetValue(val, out var colors))
            {
                e.CellStyle.ForeColor          = colors.fg;
                e.CellStyle.BackColor          = colors.bg;
                e.CellStyle.SelectionForeColor = colors.fg;
                e.CellStyle.SelectionBackColor = colors.bg;
                e.CellStyle.Font               = new Font("Segoe UI", 11f, FontStyle.Bold);
                e.CellStyle.Alignment          = DataGridViewContentAlignment.MiddleCenter;
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  Detail Dialog
        //  Uses FlowLayoutPanel for vertical stacking — avoids the blank-
        //  content bug that occurs when absolute Location coords are used
        //  inside a DockStyle.Fill panel (coords are ignored by Fill layout).
        //
        //  DB fields displayed:
        //    Item          : ItemID, ItemName, ItemDescription
        //    RawMaterial   : MaterialType (Category), purchasePrice (UnitCost)
        //    WarehouseItem : WarehouseItemQuantity (sum), ReorderLevel
        //    Computed      : StockStatus
        // ════════════════════════════════════════════════════════════════
        private void OpenDetailDialog()
        {
            if (dgvMaterials.SelectedRows.Count == 0) return;

            string materialId = dgvMaterials.SelectedRows[0]
                .Cells["colMaterialID"].Value?.ToString();

            var vm = _ctrl.GetModifyRawMaterialVM(materialId);
            if (vm?.Material == null) return;

            var m  = vm.Material;
            var wh = vm.WarehouseBreakdown ?? new List<WarehouseItemEntity>();

            // ── Dialog shell ─────────────────────────────────────────────
            using var dlg = new Form
            {
                Text            = $"View Raw Material  \u2014  {m.MaterialID}",
                Size            = new Size(900, 780),
                MinimumSize     = new Size(700, 600),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.FromArgb(240, 244, 249),
                Font            = new Font("Segoe UI", 11f),
                FormBorderStyle = FormBorderStyle.Sizable,
                MaximizeBox     = true,
                MinimizeBox     = false
            };

            // ── Header ───────────────────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 80,
                BackColor = Color.FromArgb(19, 35, 61)
            };
            pnlHeader.Controls.Add(new Label
            {
                Text      = $"View Raw Material  \u2014  {m.MaterialID}",
                Font      = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(36, 0, 0, 0)
            });

            // ── Footer ───────────────────────────────────────────────────
            var pnlFoot = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 72,
                BackColor = Color.White
            };
            pnlFoot.Paint += (s, pe) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                pe.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0);
            };
            var btnClose = new Button
            {
                Text      = "Close",
                Font      = new Font("Segoe UI", 11f),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 31, 53),
                FlatStyle = FlatStyle.Flat,
                Width     = 160,
                Height    = 44,
                Anchor    = AnchorStyles.Right | AnchorStyles.Top
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(200, 207, 220);
            btnClose.FlatAppearance.BorderSize  = 1;
            btnClose.Click += (s, ev) => dlg.Close();
            btnClose.Location = new Point(pnlFoot.Width - 180, 14);
            pnlFoot.Resize   += (s, ev) => btnClose.Left = pnlFoot.Width - 180;
            pnlFoot.Controls.Add(btnClose);

            // ── Scrollable body ──────────────────────────────────────────
            var scroll = new Panel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                BackColor  = Color.FromArgb(240, 244, 249),
                Padding    = new Padding(36, 24, 36, 16)
            };

            // ── White card inside scroll ─────────────────────────────────
            var card = new Panel
            {
                BackColor   = Color.White,
                AutoSize    = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor      = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Padding     = new Padding(40, 32, 40, 32)
            };
            card.Paint += (s, pe) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                pe.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            // FlowLayoutPanel stacks rows top-to-bottom automatically
            var flow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                AutoSize      = true,
                AutoSizeMode  = AutoSizeMode.GrowAndShrink,
                Dock          = DockStyle.Top,
                BackColor     = Color.Transparent
            };

            // ── Helpers ──────────────────────────────────────────────────
            // Section header
            Control MakeSectionHeader(string title)
            {
                var p = new Panel
                {
                    Height    = 48,
                    BackColor = Color.Transparent,
                    Margin    = new Padding(0, 16, 0, 4)
                };
                p.Controls.Add(new Label
                {
                    Text      = title,
                    Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(47, 111, 237),
                    Dock      = DockStyle.Top,
                    Height    = 30,
                    TextAlign = ContentAlignment.BottomLeft
                });
                p.Controls.Add(new Panel
                {
                    Dock      = DockStyle.Bottom,
                    Height    = 1,
                    BackColor = Color.FromArgb(221, 227, 236)
                });
                return p;
            }

            // Field row: label + plain text value
            Control MakeFieldRow(string label, string value)
            {
                var row = new Panel
                {
                    Height    = 56,
                    BackColor = Color.Transparent,
                    Margin    = new Padding(0, 0, 0, 2)
                };
                var tlp = new TableLayoutPanel
                {
                    Dock            = DockStyle.Fill,
                    ColumnCount     = 2,
                    RowCount        = 1,
                    BackColor       = Color.Transparent,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                tlp.Controls.Add(new Label
                {
                    Text      = label,
                    Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(90, 105, 130),
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                }, 0, 0);

                tlp.Controls.Add(new Label
                {
                    Text      = value ?? "\u2014",
                    Font      = new Font("Segoe UI", 11f),
                    ForeColor = Color.FromArgb(15, 31, 53),
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                }, 1, 0);

                row.Controls.Add(tlp);

                // thin divider at bottom
                row.Controls.Add(new Panel
                {
                    Dock      = DockStyle.Bottom,
                    Height    = 1,
                    BackColor = Color.FromArgb(240, 243, 248)
                });
                return row;
            }

            // Field row: label + coloured Status pill
            Control MakeStatusRow(string label, string value)
            {
                var row = new Panel
                {
                    Height    = 56,
                    BackColor = Color.Transparent,
                    Margin    = new Padding(0, 0, 0, 2)
                };
                var tlp = new TableLayoutPanel
                {
                    Dock            = DockStyle.Fill,
                    ColumnCount     = 2,
                    RowCount        = 1,
                    BackColor       = Color.Transparent,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                tlp.Controls.Add(new Label
                {
                    Text      = label,
                    Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(90, 105, 130),
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                }, 0, 0);

                Color pillBg = Color.FromArgb(229, 231, 235);
                Color pillFg = Color.FromArgb(55, 65, 81);
                if (StatusColors.TryGetValue(value ?? "", out var sc))
                {
                    pillBg = sc.bg;
                    pillFg = sc.fg;
                }

                var pillPanel = new Panel
                {
                    Dock      = DockStyle.Fill,
                    BackColor = Color.Transparent,
                    Padding   = new Padding(0, 12, 0, 12)
                };
                var pill = new Label
                {
                    Text        = value ?? "\u2014",
                    Font        = new Font("Segoe UI", 10f, FontStyle.Bold),
                    ForeColor   = pillFg,
                    BackColor   = pillBg,
                    AutoSize    = true,
                    Padding     = new Padding(12, 3, 12, 3),
                    TextAlign   = ContentAlignment.MiddleCenter,
                    BorderStyle = BorderStyle.FixedSingle
                };
                pillPanel.Controls.Add(pill);
                pill.Location = new Point(0, 0);

                tlp.Controls.Add(pillPanel, 1, 0);
                row.Controls.Add(tlp);
                row.Controls.Add(new Panel
                {
                    Dock      = DockStyle.Bottom,
                    Height    = 1,
                    BackColor = Color.FromArgb(240, 243, 248)
                });
                return row;
            }

            // ── Set width of a flow item when dialog resizes ─────────────
            void SetFlowItemWidth(Control c)
            {
                int w = flow.ClientSize.Width - flow.Padding.Horizontal;
                if (w > 0) c.Width = w;
            }

            // ── Add all rows ─────────────────────────────────────────────

            // Section A — Item
            var secA = MakeSectionHeader("Item Information");
            flow.Controls.Add(secA);

            var rowItemId   = MakeFieldRow("Item ID",           m.MaterialID ?? "\u2014");
            var rowItemName = MakeFieldRow("Item Name",         m.MaterialName ?? "\u2014");
            var rowItemDesc = MakeFieldRow("Item Description",  m.ItemDescription ?? "\u2014");
            flow.Controls.Add(rowItemId);
            flow.Controls.Add(rowItemName);
            flow.Controls.Add(rowItemDesc);

            // Section B — RawMaterial
            var secB = MakeSectionHeader("Raw Material Details");
            flow.Controls.Add(secB);

            var rowMatType = MakeFieldRow("Material Type",   m.Category ?? "\u2014");
            var rowPrice   = MakeFieldRow("Purchase Price",  $"HK$ {m.UnitCost:N2}");
            flow.Controls.Add(rowMatType);
            flow.Controls.Add(rowPrice);

            // Section C — WarehouseItem (aggregated)
            var secC = MakeSectionHeader("Warehouse Stock");
            flow.Controls.Add(secC);

            var rowQty     = MakeFieldRow("Total Stock Qty", m.StockQty.ToString());
            var rowReorder = MakeFieldRow("Reorder Level",   m.ReorderLevel.ToString());
            flow.Controls.Add(rowQty);
            flow.Controls.Add(rowReorder);

            // Section D — Computed
            var secD = MakeSectionHeader("Status");
            flow.Controls.Add(secD);

            var rowStatus = MakeStatusRow("Stock Status", m.StockStatus);
            flow.Controls.Add(rowStatus);

            // Section E — Warehouse Breakdown (if data exists)
            if (wh.Count > 0)
            {
                var secE = MakeSectionHeader("Warehouse Breakdown");
                flow.Controls.Add(secE);

                var dgv = new DataGridView
                {
                    Height                = 28 + wh.Count * 30,
                    BackgroundColor       = Color.White,
                    BorderStyle           = BorderStyle.None,
                    RowHeadersVisible     = false,
                    AllowUserToAddRows    = false,
                    AllowUserToDeleteRows = false,
                    ReadOnly              = true,
                    AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                    SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                    Font                  = new Font("Segoe UI", 10f),
                    Margin                = new Padding(0, 4, 0, 8),
                    ColumnHeadersHeight   = 28
                };
                dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 244, 249);
                dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 10f, FontStyle.Bold);
                dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(70, 85, 110);
                dgv.ColumnHeadersBorderStyle                 = DataGridViewHeaderBorderStyle.None;
                dgv.DefaultCellStyle.BackColor               = Color.White;
                dgv.DefaultCellStyle.SelectionBackColor      = Color.FromArgb(219, 234, 254);
                dgv.DefaultCellStyle.SelectionForeColor      = Color.FromArgb(15, 31, 53);
                dgv.EnableHeadersVisualStyles                = false;

                dgv.Columns.Add("whId",   "Warehouse ID");
                dgv.Columns.Add("whLoc",  "Location");
                dgv.Columns.Add("qty",    "Qty");
                dgv.Columns.Add("reorder","Reorder Level");

                foreach (var row in wh)
                    dgv.Rows.Add(row.WarehouseID, row.WarehouseName, row.Quantity, row.ReorderLevel);

                flow.Controls.Add(dgv);
            }

            // ── Wire resizing so rows fill the card width ────────────────
            flow.Layout += (s, le) =>
            {
                int w = flow.ClientSize.Width - flow.Padding.Horizontal;
                if (w <= 0) return;
                foreach (Control c in flow.Controls)
                    c.Width = w;
            };

            card.Controls.Add(flow);
            scroll.Controls.Add(card);

            // Keep card width in sync with scroll panel
            scroll.Resize += (s, ev) =>
            {
                int w = scroll.ClientSize.Width - scroll.Padding.Horizontal;
                if (w > 0) card.Width = w;
            };
            dlg.Load += (s, ev) =>
            {
                int w = scroll.ClientSize.Width - scroll.Padding.Horizontal;
                if (w > 0) card.Width = w;
            };

            dlg.Controls.Add(scroll);
            dlg.Controls.Add(pnlFoot);
            dlg.Controls.Add(pnlHeader);
            dlg.ShowDialog(this);
        }

        // ════════════════════════════════════════════════════════════════
        //  Navigation / session
        // ════════════════════════════════════════════════════════════════

        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }

        // ════════════════════════════════════════════════════════════════
        //  Utility
        // ════════════════════════════════════════════════════════════════

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
