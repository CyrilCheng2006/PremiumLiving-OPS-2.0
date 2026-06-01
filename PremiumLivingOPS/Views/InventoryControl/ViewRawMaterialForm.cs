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

        /// <summary>
        /// Populate the Material Type combo-box from the DB ENUM values.
        /// </summary>
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

            // Pass materialType as the category filter (maps to RawMaterial.MaterialType)
            var vm = _ctrl.GetViewRawMaterialVM(
                string.IsNullOrEmpty(keyword)                      ? null : keyword,
                materialType == "All" || string.IsNullOrEmpty(materialType) ? null : materialType);

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Inventory Control  \u203a  View Raw Material");

            _currentMaterials = vm.Materials;

            // Apply status filter client-side
            if (!string.IsNullOrEmpty(status) && status != "All")
                _currentMaterials = _currentMaterials.FindAll(m => m.StockStatus == status);

            dgvMaterials.Rows.Clear();
            foreach (var m in _currentMaterials)
                dgvMaterials.Rows.Add(
                    m.MaterialID,
                    m.MaterialName,
                    m.Category,          // maps to RawMaterial.MaterialType
                    $"HK$ {m.UnitCost:N2}",  // maps to RawMaterial.purchasePrice
                    m.StockQty,          // sum of WarehouseItem.WarehouseItemQuantity
                    m.ReorderLevel,      // WarehouseItem.ReorderLevel
                    m.StockStatus);      // computed

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
        //  Detail Dialog  — rebuilt to match AddItemForm style
        //  Fields mirror DB tables:
        //    Item           : ItemID, ItemName, ItemDescription
        //    RawMaterial    : MaterialType, purchasePrice
        //    WarehouseItem  : WarehouseID (+ Location), WarehouseItemQuantity,
        //                     ReorderLevel
        //    (computed)     : StockStatus
        // ════════════════════════════════════════════════════════════════
        private void OpenDetailDialog()
        {
            if (dgvMaterials.SelectedRows.Count == 0) return;

            string materialId = dgvMaterials.SelectedRows[0]
                .Cells["colMaterialID"].Value?.ToString();

            // Fetch full record so we get every schema field
            var vm = _ctrl.GetModifyRawMaterialVM(materialId);
            if (vm?.Material == null) return;
            var m = vm.Material;

            // ── Derive display values ────────────────────────────────────
            // Item table
            string itemId       = m.MaterialID;
            string itemName     = m.MaterialName ?? "\u2014";
            string itemDesc     = m.ItemDescription ?? "\u2014";
            // RawMaterial table
            string materialType = m.Category ?? "\u2014";     // MaterialType ENUM
            string purchPrice   = $"HK$ {m.UnitCost:N2}";   // purchasePrice
            // WarehouseItem table (aggregated across warehouses)
            string stockQty     = m.StockQty.ToString();     // SUM(WarehouseItemQuantity)
            string reorderLvl   = m.ReorderLevel.ToString(); // ReorderLevel
            // computed
            string status       = m.StockStatus ?? "\u2014";

            // ── Dialog shell — mirrors AddItemForm sizing/style ──────────
            using var dlg = new Form
            {
                Text            = $"View Raw Material  \u2014  {itemId}",
                Size            = new Size(1600, 1100),
                MinimumSize     = new Size(1100, 800),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.FromArgb(240, 244, 249),
                Font            = new Font("Segoe UI", 12f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false
            };

            // ── Header (dark navy, identical to AddItemForm) ─────────────
            var pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 90,
                BackColor = Color.FromArgb(19, 35, 61)
            };
            pnlHeader.Controls.Add(new Label
            {
                Text      = $"View Raw Material  \u2014  {itemId}",
                Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(48, 0, 0, 0)
            });

            // ── Footer ──────────────────────────────────────────────────
            var pnlFoot = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 100,
                BackColor = Color.White,
                Padding   = new Padding(0, 20, 48, 20)
            };
            pnlFoot.Paint += (s, e) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0);
            };
            var btnClose = new Button
            {
                Text      = "Close",
                Font      = new Font("Segoe UI", 13f),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 31, 53),
                FlatStyle = FlatStyle.Flat,
                Width     = 210,
                Height    = 60,
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

            // ── Scrollable body ─────────────────────────────────────────
            var pnlScroll = new Panel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                BackColor  = Color.FromArgb(240, 244, 249),
                Padding    = new Padding(56, 40, 56, 24)
            };

            // ── Card (white rounded panel, same as AddItemForm) ──────────
            var (outerCard, innerCard) = CardPanel.Create(
                outerHeight: 100, outerPadding: new Padding(0));
            innerCard.Padding = new Padding(56, 40, 56, 40);

            // ── Field rows — each label + read-only value ────────────────
            //  Section A: Item table fields
            //  Section B: RawMaterial table fields
            //  Section C: WarehouseItem table fields (aggregated)
            //  Section D: Computed / derived

            const int RowH    = 84;
            const int RowGap  = 20;
            const int LabelW  = 340;

            var fieldDefs = new[]
            {
                // ── Item ─────────────────────────────────────────────────
                ("Item ID",            itemId,       "Item.ItemID"),
                ("Item Name",          itemName,     "Item.ItemName"),
                ("Item Description",   itemDesc,     "Item.ItemDescription"),
                // ── RawMaterial ──────────────────────────────────────────
                ("Material Type",      materialType, "RawMaterial.MaterialType"),
                ("Purchase Price",     purchPrice,   "RawMaterial.purchasePrice"),
                // ── WarehouseItem (aggregated) ───────────────────────────
                ("Total Stock Qty",    stockQty,     "WarehouseItem.WarehouseItemQuantity (sum)"),
                ("Reorder Level",      reorderLvl,   "WarehouseItem.ReorderLevel"),
                // ── Computed ────────────────────────────────────────────
                ("Stock Status",       status,       "Computed")
            };

            int yPos = 0;
            var rows = new List<Panel>();

            // Section header helper
            Panel SectionHeader(string title)
            {
                var p = new Panel
                {
                    Height    = 40,
                    BackColor = Color.Transparent,
                    Location  = new Point(0, yPos),
                    Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                p.Controls.Add(new Label
                {
                    Text      = title,
                    Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(47, 111, 237),
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.BottomLeft,
                    Padding   = new Padding(0, 0, 0, 4)
                });
                p.Controls.Add(new Panel
                {
                    Dock      = DockStyle.Bottom,
                    Height    = 1,
                    BackColor = Color.FromArgb(221, 227, 236)
                });
                return p;
            }

            // Field row helper — label on left, value on right
            Panel FieldRow(string label, string value, bool isStatus = false)
            {
                var row = new Panel
                {
                    Height    = RowH,
                    BackColor = Color.Transparent,
                    Location  = new Point(0, yPos),
                    Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
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
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LabelW));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                tlp.Controls.Add(new Label
                {
                    Text      = label,
                    Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(70, 85, 110),
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoSize  = false
                }, 0, 0);

                Control valueCtrl;
                if (isStatus && StatusColors.TryGetValue(value, out var sc))
                {
                    // Render Status as a coloured pill label
                    var pill = new Label
                    {
                        Text      = value,
                        Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                        ForeColor = sc.fg,
                        BackColor = sc.bg,
                        AutoSize  = true,
                        Padding   = new Padding(14, 4, 14, 4),
                        TextAlign = ContentAlignment.MiddleCenter,
                        BorderStyle = BorderStyle.FixedSingle
                    };
                    var pillWrapper = new Panel
                    {
                        Dock      = DockStyle.Fill,
                        BackColor = Color.Transparent,
                        Padding   = new Padding(0, 22, 0, 22)
                    };
                    pillWrapper.Controls.Add(pill);
                    pill.Location = new Point(0, 0);
                    valueCtrl = pillWrapper;
                }
                else
                {
                    var lbl = new Label
                    {
                        Text      = value ?? "\u2014",
                        Font      = new Font("Segoe UI", 13f),
                        ForeColor = Color.FromArgb(15, 31, 53),
                        Dock      = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleLeft,
                        AutoSize  = false
                    };
                    valueCtrl = lbl;
                }

                var inputWrapper = new Panel
                {
                    Dock      = DockStyle.Fill,
                    BackColor = Color.Transparent,
                    Padding   = new Padding(0, 14, 0, 14)
                };
                valueCtrl.Dock = DockStyle.Fill;
                inputWrapper.Controls.Add(valueCtrl);

                tlp.Controls.Add(inputWrapper, 1, 0);
                row.Controls.Add(tlp);

                // Divider at bottom
                row.Controls.Add(new Panel
                {
                    Dock      = DockStyle.Bottom,
                    Height    = 1,
                    BackColor = Color.FromArgb(235, 238, 245)
                });
                return row;
            }

            // ── Build sections ──────────────────────────────────────────
            // Section A: Item
            var secA = SectionHeader("Item Information");
            innerCard.Controls.Add(secA);
            yPos += 40 + 10;

            foreach (var (lbl, val, _) in new[]
            {
                (fieldDefs[0].Item1, fieldDefs[0].Item2, false),
                (fieldDefs[1].Item1, fieldDefs[1].Item2, false),
                (fieldDefs[2].Item1, fieldDefs[2].Item2, false)
            })
            {
                var r = FieldRow(lbl, val, false);
                r.Location = new Point(0, yPos);
                r.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                innerCard.Controls.Add(r);
                yPos += RowH + RowGap;
            }

            // Section B: RawMaterial
            var secB = SectionHeader("Raw Material Details");
            secB.Location = new Point(0, yPos);
            innerCard.Controls.Add(secB);
            yPos += 40 + 10;

            foreach (var (lbl, val, _) in new[]
            {
                (fieldDefs[3].Item1, fieldDefs[3].Item2, false),
                (fieldDefs[4].Item1, fieldDefs[4].Item2, false)
            })
            {
                var r = FieldRow(lbl, val, false);
                r.Location = new Point(0, yPos);
                r.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                innerCard.Controls.Add(r);
                yPos += RowH + RowGap;
            }

            // Section C: WarehouseItem
            var secC = SectionHeader("Warehouse Stock");
            secC.Location = new Point(0, yPos);
            innerCard.Controls.Add(secC);
            yPos += 40 + 10;

            foreach (var (lbl, val, _) in new[]
            {
                (fieldDefs[5].Item1, fieldDefs[5].Item2, false),
                (fieldDefs[6].Item1, fieldDefs[6].Item2, false)
            })
            {
                var r = FieldRow(lbl, val, false);
                r.Location = new Point(0, yPos);
                r.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                innerCard.Controls.Add(r);
                yPos += RowH + RowGap;
            }

            // Section D: Computed
            var secD = SectionHeader("Computed / Status");
            secD.Location = new Point(0, yPos);
            innerCard.Controls.Add(secD);
            yPos += 40 + 10;

            var statusRow = FieldRow(fieldDefs[7].Item1, fieldDefs[7].Item2, isStatus: true);
            statusRow.Location = new Point(0, yPos);
            statusRow.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            innerCard.Controls.Add(statusRow);
            yPos += RowH + RowGap;

            // ── Size card to content ─────────────────────────────────────
            int cardContentH = yPos + innerCard.Padding.Vertical;
            outerCard.Height  = cardContentH + 16;
            innerCard.Height  = cardContentH;

            pnlScroll.Controls.Add(outerCard);

            // ── Resize helpers ───────────────────────────────────────────
            dlg.Load += (s, e) =>
            {
                outerCard.Width  = pnlScroll.ClientSize.Width - pnlScroll.Padding.Horizontal;
                outerCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

                // Also anchor section headers and field rows
                foreach (Control c in innerCard.Controls)
                    c.Width = innerCard.ClientSize.Width - innerCard.Padding.Horizontal;
            };
            pnlScroll.Resize += (s, e) =>
            {
                outerCard.Width = pnlScroll.ClientSize.Width - pnlScroll.Padding.Horizontal;
                foreach (Control c in innerCard.Controls)
                    c.Width = innerCard.ClientSize.Width - innerCard.Padding.Horizontal;
            };

            dlg.Controls.Add(pnlScroll);
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
