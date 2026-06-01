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

        private static readonly Dictionary<string, (Color bg, Color fg)> StatusColors =
            new Dictionary<string, (Color, Color)>
            {
                { "In Stock",     (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
                { "Low Stock",    (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "Out of Stock", (Color.FromArgb(254, 226, 226), Color.FromArgb(153,  27,  27)) }
            };

        // ── Detail dialog sizing — mirrors AddItemForm constants ──────────
        private const int D_RowH     = 84;
        private const int D_RowGap   = 20;
        private const int D_LabelW   = 300;
        private const int D_CardPadH = 56;
        private const int D_CardPadV = 40;
        private const int D_BtnW     = 210;
        private const int D_BtnH     = 60;

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
        //  Detail Dialog — mirrors AddItemForm visual language exactly:
        //    same dark header, same scroll+card layout, same FieldRow()
        //    two-column TLP structure, same footer with Close button.
        //
        //  Read-only display fields (all DB schema columns):
        //    Item         : Item ID, Item Name, Description
        //    RawMaterial  : Material Type (ENUM), Purchase Price
        //    WarehouseItem: Total Stock Qty, Reorder Level
        //    Computed     : Stock Status (colour pill)
        //    Breakdown    : per-warehouse DGV if WarehouseBreakdown data exists
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

            // ── helper: read-only label control ───────────────────────────────
            Label ReadLabel(string text, bool isBold = false) => new Label
            {
                Text      = text ?? "\u2014",
                Font      = new Font("Segoe UI", 13f, isBold ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };

            // ── helper: status pill label ───────────────────────────────────
            Control StatusPill(string status)
            {
                Color pillBg = Color.FromArgb(229, 231, 235);
                Color pillFg = Color.FromArgb(55,  65,  81);
                if (StatusColors.TryGetValue(status ?? "", out var sc))
                { pillBg = sc.bg; pillFg = sc.fg; }

                var wrapper = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent,
                                          Padding = new Padding(0, 18, 0, 18) };
                var lbl = new Label
                {
                    Text        = status ?? "\u2014",
                    Font        = new Font("Segoe UI", 11f, FontStyle.Bold),
                    ForeColor   = pillFg,
                    BackColor   = pillBg,
                    AutoSize    = true,
                    Padding     = new Padding(14, 4, 14, 4),
                    TextAlign   = ContentAlignment.MiddleCenter,
                    BorderStyle = BorderStyle.FixedSingle
                };
                wrapper.Controls.Add(lbl);
                lbl.Location = new Point(0, 0);
                return wrapper;
            }

            // ── helper: FieldRow — identical signature to AddItemForm.FieldRow() ──
            Panel FieldRow(string labelText, Control input)
            {
                var row = new Panel
                {
                    Height    = D_RowH,
                    BackColor = Color.Transparent
                };
                var tlp = new TableLayoutPanel
                {
                    Dock            = DockStyle.Fill,
                    ColumnCount     = 2,
                    RowCount        = 1,
                    BackColor       = Color.Transparent,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding         = new Padding(0)
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D_LabelW));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                var lbl = new Label
                {
                    Text      = labelText,
                    Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(70, 85, 110),
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoSize  = false
                };

                var inputWrapper = new Panel
                {
                    Dock      = DockStyle.Fill,
                    BackColor = Color.Transparent,
                    Padding   = new Padding(0, 14, 0, 14)
                };
                input.Dock = DockStyle.Fill;
                inputWrapper.Controls.Add(input);

                tlp.Controls.Add(lbl,          0, 0);
                tlp.Controls.Add(inputWrapper, 1, 0);
                row.Controls.Add(tlp);
                return row;
            }

            // ── helper: thin section divider with title ───────────────────────
            Panel SectionHeader(string title)
            {
                var p = new Panel { Height = 36, BackColor = Color.Transparent,
                                    Margin = new Padding(0, 12, 0, 4) };
                p.Controls.Add(new Label
                {
                    Text      = title,
                    Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(47, 111, 237),
                    Dock      = DockStyle.Top,
                    Height    = 26
                });
                p.Controls.Add(new Panel
                {
                    Dock      = DockStyle.Bottom,
                    Height    = 1,
                    BackColor = Color.FromArgb(221, 227, 236)
                });
                return p;
            }

            // ── Collect all rows ─────────────────────────────────────────────
            var rows = new List<Control>();

            // —— Section A: Item —————————————————————————————————————
            rows.Add(SectionHeader("Item Information"));
            rows.Add(FieldRow("Item ID",           ReadLabel(m.MaterialID)));
            rows.Add(FieldRow("Item Name",         ReadLabel(m.MaterialName)));
            rows.Add(FieldRow("Description",       ReadLabel(m.ItemDescription)));

            // —— Section B: RawMaterial ————————————————————————————
            rows.Add(SectionHeader("Raw Material Details"));
            rows.Add(FieldRow("Material Type",     ReadLabel(m.Category)));
            rows.Add(FieldRow("Purchase Price (HK$)", ReadLabel($"HK$ {m.UnitCost:N2}")));

            // —— Section C: WarehouseItem ———————————————————————————
            rows.Add(SectionHeader("Warehouse Stock"));
            rows.Add(FieldRow("Total Stock Qty",   ReadLabel(m.StockQty.ToString())));
            rows.Add(FieldRow("Reorder Level",     ReadLabel(m.ReorderLevel.ToString())));

            // —— Section D: Stock Status ————————————————————————————
            rows.Add(SectionHeader("Status"));
            rows.Add(FieldRow("Stock Status",      StatusPill(m.StockStatus)));

            // —— Section E: Warehouse Breakdown DGV ————————————————————
            DataGridView dgvWh = null;
            if (wh.Count > 0)
            {
                rows.Add(SectionHeader("Warehouse Breakdown"));

                dgvWh = new DataGridView
                {
                    BackgroundColor       = Color.White,
                    BorderStyle           = BorderStyle.FixedSingle,
                    RowHeadersVisible     = false,
                    AllowUserToAddRows    = false,
                    AllowUserToDeleteRows = false,
                    ReadOnly              = true,
                    AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                    SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                    Font                  = new Font("Segoe UI", 11f),
                    ColumnHeadersHeight   = 32
                };
                dgvWh.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 244, 249);
                dgvWh.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 11f, FontStyle.Bold);
                dgvWh.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(70, 85, 110);
                dgvWh.ColumnHeadersBorderStyle                 = DataGridViewHeaderBorderStyle.None;
                dgvWh.DefaultCellStyle.BackColor               = Color.White;
                dgvWh.DefaultCellStyle.SelectionBackColor      = Color.FromArgb(219, 234, 254);
                dgvWh.DefaultCellStyle.SelectionForeColor      = Color.FromArgb(15, 31, 53);
                dgvWh.EnableHeadersVisualStyles                = false;

                dgvWh.Columns.Add("whId",    "Warehouse ID");
                dgvWh.Columns.Add("whLoc",   "Location");
                dgvWh.Columns.Add("qty",     "Stock Qty");
                dgvWh.Columns.Add("reorder", "Reorder Level");

                foreach (var row in wh)
                    dgvWh.Rows.Add(
                        row.WarehouseID,
                        row.WarehouseName,
                        row.Quantity,
                        row.ReorderLevel);

                // Height: header + rows
                dgvWh.Height = 32 + wh.Count * 30 + 4;

                // Wrap DGV in a FieldRow-height panel so it sits in the card
                var dgvHolder = new Panel
                {
                    Height    = dgvWh.Height + 16,
                    BackColor = Color.Transparent
                };
                dgvWh.Dock = DockStyle.Fill;
                dgvHolder.Controls.Add(dgvWh);
                rows.Add(dgvHolder);
            }

            // ── Calculate card height the same way AddItemForm does ───────────
            int y = 0;
            foreach (var r in rows)
                y += r.Height + (r is Panel rp && rp.Height == D_RowH ? D_RowGap : 4);
            int cardContentH = y + D_CardPadV * 2;

            // ── Build innerCard + outerCard (same as AddItemForm) ────────────
            var (outerCard, innerCard) = CardPanel.Create(
                outerHeight : cardContentH + 16,
                outerPadding: new Padding(0));
            innerCard.Padding = new Padding(D_CardPadH, D_CardPadV, D_CardPadH, D_CardPadV);

            int yPos = 0;
            foreach (var r in rows)
            {
                r.Location = new Point(0, yPos);
                r.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                innerCard.Controls.Add(r);
                yPos += r.Height + (r.Height == D_RowH ? D_RowGap : 4);
            }
            innerCard.Height = cardContentH;

            // ── Scroll panel ────────────────────────────────────────────────
            var scroll = new Panel
            {
                Dock       = DockStyle.Fill,
                BackColor  = Color.FromArgb(240, 244, 249),
                AutoScroll = true,
                Padding    = new Padding(56, 40, 56, 24)
            };
            scroll.Controls.Add(outerCard);

            // ── Dialog shell ─────────────────────────────────────────────
            using var dlg = new Form
            {
                Text            = $"View Raw Material  \u2014  {m.MaterialID}",
                Size            = new Size(1600, 1200),
                MinimumSize     = new Size(1200, 900),
                StartPosition   = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false,
                BackColor       = Color.FromArgb(240, 244, 249),
                Font            = new Font("Segoe UI", 12f)
            };

            // header
            var pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 90,
                BackColor = Color.FromArgb(19, 35, 61)
            };
            pnlHeader.Controls.Add(new Label
            {
                Text      = $"View Raw Material  \u2014  {m.MaterialID}",
                Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(48, 0, 0, 0)
            });

            // footer
            var pnlFoot = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 100,
                BackColor = Color.White,
                Padding   = new Padding(0, 20, 48, 20)
            };
            pnlFoot.Paint += (s, pe) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                pe.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0);
            };

            var btnClose = new Button
            {
                Text      = "Close",
                Font      = new Font("Segoe UI", 13f),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 31, 53),
                FlatStyle = FlatStyle.Flat,
                Width     = D_BtnW,
                Height    = D_BtnH,
                Margin    = new Padding(12, 0, 0, 0),
                Cursor    = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(200, 207, 220);
            btnClose.FlatAppearance.BorderSize  = 1;
            btnClose.Click += (s, ev) => dlg.Close();

            var footFlow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Right,
                AutoSize      = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent
            };
            footFlow.Controls.Add(btnClose);
            pnlFoot.Controls.Add(footFlow);

            dlg.Controls.Add(scroll);
            dlg.Controls.Add(pnlFoot);
            dlg.Controls.Add(pnlHeader);

            // ── Resize outerCard width to match scroll, same as AddItemForm ──
            void ResizeCard()
            {
                int w = scroll.ClientSize.Width - scroll.Padding.Horizontal;
                if (w > 100) outerCard.Width = w;
            }
            dlg.Load          += (s, ev) => ResizeCard();
            scroll.Resize     += (s, ev) => ResizeCard();

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
