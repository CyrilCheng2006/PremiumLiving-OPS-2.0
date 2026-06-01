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

        // ── Detail dialog layout constants ─────────────────────────────────
        private const int D_RowH    = 64;   // height of each FieldRow
        private const int D_LabelW  = 260;  // fixed width of label column
        private const int D_BtnW    = 210;
        private const int D_BtnH    = 60;

        // Warehouse Breakdown DGV row metrics (must match dgvWh settings below)
        private const int D_WhHdrH  = 44;   // ColumnHeadersHeight
        private const int D_WhRowH  = 44;   // RowTemplate.Height
        private const int D_WhSecH  = 50;   // Section header panel height

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
                string.IsNullOrEmpty(keyword)                               ? null : keyword,
                materialType == "All" || string.IsNullOrEmpty(materialType) ? null : materialType);

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

            const int PillW   = 310;
            const int PillH   = 60;
            const int Gap     = 10;
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
        //  Detail Dialog
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

            // ================================================================
            //  LOCAL HELPERS
            // ================================================================

            Label ReadLabel(string text) => new Label
            {
                Text      = text ?? "\u2014",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.White
            };

            Panel FieldRow(string labelText, Control input, bool lastRow = false)
            {
                var row = new Panel { Height = D_RowH, BackColor = Color.White };
                if (!lastRow)
                {
                    row.Paint += (s, pe) =>
                    {
                        var p = (Panel)s;
                        using var pen = new System.Drawing.Pen(
                            Color.FromArgb(221, 227, 236), 1);
                        pe.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
                    };
                }

                var tlp = new TableLayoutPanel
                {
                    Dock            = DockStyle.Fill,
                    ColumnCount     = 2,
                    RowCount        = 1,
                    BackColor       = Color.White,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding         = new Padding(0)
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D_LabelW));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                var lbl = new Label
                {
                    Text      = labelText,
                    Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(70, 85, 110),
                    BackColor = Color.FromArgb(248, 250, 252),
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoSize  = false,
                    Padding   = new Padding(20, 0, 8, 0)
                };

                var inputWrapper = new Panel
                {
                    Dock      = DockStyle.Fill,
                    BackColor = Color.White,
                    Padding   = new Padding(20, 10, 20, 10)
                };
                input.Dock = DockStyle.Fill;
                inputWrapper.Controls.Add(input);

                tlp.Controls.Add(lbl,          0, 0);
                tlp.Controls.Add(inputWrapper, 1, 0);
                row.Controls.Add(tlp);
                return row;
            }

            Panel StackRows(IList<Panel> rowList, Padding innerPad)
            {
                var content = new Panel
                {
                    Dock      = DockStyle.Fill,
                    BackColor = Color.White,
                    Padding   = innerPad
                };
                var stack = new Panel
                {
                    Location  = new Point(innerPad.Left, innerPad.Top),
                    Height    = rowList.Count * D_RowH,
                    BackColor = Color.White
                };
                int y = 0;
                foreach (var r in rowList)
                {
                    r.Location = new Point(0, y);
                    r.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                    stack.Controls.Add(r);
                    y += D_RowH;
                }
                content.Controls.Add(stack);
                content.Resize += (s, _) =>
                {
                    int w = content.Width - innerPad.Horizontal;
                    if (w < 1) return;
                    stack.Width = w;
                    stack.Left  = innerPad.Left;
                    stack.Top   = innerPad.Top;
                    foreach (Panel r in stack.Controls) r.Width = w;
                };
                return content;
            }

            // ================================================================
            //  CARD 1 — Item Information  (3 rows)
            // ================================================================
            var card1Rows = new List<Panel>
            {
                FieldRow("Item ID",     ReadLabel(m.MaterialID)),
                FieldRow("Item Name",   ReadLabel(m.MaterialName)),
                FieldRow("Description", ReadLabel(m.ItemDescription), lastRow: true)
            };
            var (card1Outer, card1Inner) = CardPanel.Create(
                outerHeight : card1Rows.Count * D_RowH + 22,
                outerPadding: new Padding(20, 14, 20, 8));
            card1Inner.Padding = new Padding(0);
            card1Inner.Controls.Add(StackRows(card1Rows, new Padding(0)));

            // ================================================================
            //  CARD 2 — Material Details + Stock Summary  (4 rows)
            //
            //  Stock Status has been moved to the dialog header right side.
            //  outerHeight = 4 × 64 + 38 = 294
            //  (38 = CardPanel pad 22 + outerPadding.Vertical 16)
            // ================================================================
            var card2Rows = new List<Panel>
            {
                FieldRow("Material Type",        ReadLabel(m.Category)),
                FieldRow("Purchase Price (HK$)", ReadLabel($"HK$ {m.UnitCost:N2}")),
                FieldRow("Total Stock Qty",       ReadLabel(m.StockQty.ToString())),
                FieldRow("Reorder Level",         ReadLabel(m.ReorderLevel.ToString()), lastRow: true)
            };
            var (card2Outer, card2Inner) = CardPanel.Create(
                outerHeight : card2Rows.Count * D_RowH + 38,
                outerPadding: new Padding(20, 8, 20, 8));
            card2Inner.Padding = new Padding(0);
            card2Inner.Controls.Add(StackRows(card2Rows, new Padding(0)));

            // ================================================================
            //  CARD 3 — Warehouse Breakdown (conditional)
            // ================================================================
            Panel card3Outer = null;
            if (wh.Count > 0)
            {
                var whHeader = new Panel
                {
                    Dock      = DockStyle.Top,
                    Height    = D_WhSecH,
                    BackColor = Color.White,
                    Padding   = new Padding(20, 0, 20, 0)
                };
                whHeader.Controls.Add(new Label
                {
                    Text      = "Warehouse Breakdown",
                    Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(47, 111, 237),
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                });
                whHeader.Paint += (s, pe) =>
                {
                    var p = (Panel)s;
                    using var pen = new System.Drawing.Pen(
                        Color.FromArgb(221, 227, 236), 1);
                    pe.Graphics.DrawLine(pen, 20, p.Height - 1, p.Width - 20, p.Height - 1);
                };

                var dgvWh = new DataGridView
                {
                    Dock                  = DockStyle.Fill,
                    BackgroundColor       = Color.White,
                    BorderStyle           = BorderStyle.None,
                    RowHeadersVisible     = false,
                    AllowUserToAddRows    = false,
                    AllowUserToDeleteRows = false,
                    ReadOnly              = true,
                    AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                    SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                    Font                  = new Font("Segoe UI", 12f),
                    ColumnHeadersHeight   = D_WhHdrH,
                    RowTemplate           = { Height = D_WhRowH }
                };
                dgvWh.ColumnHeadersDefaultCellStyle.BackColor  = Color.FromArgb(248, 250, 252);
                dgvWh.ColumnHeadersDefaultCellStyle.Font       = new Font("Segoe UI", 12f, FontStyle.Bold);
                dgvWh.ColumnHeadersDefaultCellStyle.ForeColor  = Color.FromArgb(70, 85, 110);
                dgvWh.ColumnHeadersBorderStyle                  = DataGridViewHeaderBorderStyle.Single;
                dgvWh.DefaultCellStyle.BackColor                = Color.White;
                dgvWh.DefaultCellStyle.SelectionBackColor       = Color.FromArgb(219, 234, 254);
                dgvWh.DefaultCellStyle.SelectionForeColor       = Color.FromArgb(15, 31, 53);
                dgvWh.DefaultCellStyle.Padding                  = new Padding(12, 0, 12, 0);
                dgvWh.EnableHeadersVisualStyles                 = false;
                dgvWh.GridColor                                 = Color.FromArgb(237, 241, 247);

                dgvWh.Columns.Add("whId",    "Warehouse ID");
                dgvWh.Columns.Add("whLoc",   "Location");
                dgvWh.Columns.Add("qty",     "Stock Qty");
                dgvWh.Columns.Add("reorder", "Reorder Level");

                foreach (var row in wh)
                    dgvWh.Rows.Add(row.WarehouseID, row.WarehouseName,
                                   row.Quantity, row.ReorderLevel);

                int card3H = D_WhSecH
                           + D_WhHdrH
                           + wh.Count * D_WhRowH
                           + 16
                           + 22;

                var (c3Outer, c3Inner) = CardPanel.Create(
                    outerHeight : card3H,
                    outerPadding: new Padding(20, 8, 20, 16));
                c3Inner.Padding = new Padding(0);
                c3Inner.Controls.Add(dgvWh);
                c3Inner.Controls.Add(whHeader);
                card3Outer = c3Outer;
            }

            // ================================================================
            //  DIALOG SHELL
            // ================================================================
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

            // ----------------------------------------------------------------
            //  Header bar  —  title (left) + Stock Status pill (right)
            // ----------------------------------------------------------------
            Color pillBg = Color.FromArgb(229, 231, 235);
            Color pillFg = Color.FromArgb(55, 65, 81);
            if (StatusColors.TryGetValue(m.StockStatus ?? "", out var headerSc))
            { pillBg = headerSc.bg; pillFg = headerSc.fg; }

            // Status label (auto-sized so it never clips)
            var statusLbl = new Label
            {
                Text      = m.StockStatus ?? "\u2014",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = pillFg,
                BackColor = pillBg,
                AutoSize  = true,
                Padding   = new Padding(18, 6, 18, 6),
                TextAlign = ContentAlignment.MiddleCenter
            };
            statusLbl.Paint += (s, pe) =>
            {
                var lb = (Label)s;
                using var pen = new System.Drawing.Pen(
                    Color.FromArgb(120, pillFg.R, pillFg.G, pillFg.B), 1);
                pe.Graphics.DrawRectangle(pen, 0, 0, lb.Width - 1, lb.Height - 1);
            };

            // Right cell: FlowLayoutPanel centres the pill vertically
            var pillCell = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                BackColor     = Color.Transparent,
                FlowDirection = FlowDirection.RightToLeft,   // flush to the right edge
                WrapContents  = false,
                AutoSize      = false,
                Padding       = new Padding(0, 0, 48, 0)     // right margin matching header title
            };
            pillCell.Controls.Add(statusLbl);
            pillCell.Layout += (s, _) =>
            {
                var fl = (FlowLayoutPanel)s;
                if (fl.Controls.Count == 0) return;
                fl.Controls[0].Top = Math.Max(0, (fl.Height - fl.Controls[0].Height) / 2);
            };

            var pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 90,
                BackColor = Color.FromArgb(19, 35, 61)
            };

            // Build header as a two-column TLP: [title | pill]
            var headerTlp = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 2,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(0)
            };
            headerTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));  // title takes all spare space
            headerTlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));        // pill cell shrinks to content
            headerTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            headerTlp.Controls.Add(new Label
            {
                Text      = $"View Raw Material  \u2014  {m.MaterialID}",
                Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                Padding   = new Padding(48, 0, 0, 0)
            }, 0, 0);

            headerTlp.Controls.Add(pillCell, 1, 0);
            pnlHeader.Controls.Add(headerTlp);

            // ----------------------------------------------------------------
            //  Footer
            // ----------------------------------------------------------------
            var pnlFoot = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 100,
                BackColor = Color.White,
                Padding   = new Padding(0, 20, 48, 20)
            };
            pnlFoot.Paint += (s, pe) =>
            {
                using var pen = new System.Drawing.Pen(
                    Color.FromArgb(221, 227, 236), 1);
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

            // ----------------------------------------------------------------
            //  Scroll area
            // ----------------------------------------------------------------
            var scroll = new Panel
            {
                Dock       = DockStyle.Fill,
                BackColor  = Color.FromArgb(240, 244, 249),
                AutoScroll = true,
                Padding    = new Padding(0)
            };

            if (card3Outer != null) scroll.Controls.Add(card3Outer);
            scroll.Controls.Add(card2Outer);
            scroll.Controls.Add(card1Outer);

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
