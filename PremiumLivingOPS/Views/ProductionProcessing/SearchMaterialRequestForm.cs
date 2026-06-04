using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.ProductionProcessing
{
    /// <summary>
    /// View — Search Raw Material Request.
    ///
    /// MVC role : View only.  All data access goes through ProductionProcessingController.
    /// AppShell  : mandatory chrome (TopNavBar + UserBar).
    /// CardPanel : all content wrapped in 3-layer nested cards.
    ///
    /// Schema coverage:
    ///   MaterialRequest  — primary list
    ///   RawMaterial/Item — joined for material name + type
    ///   WarehouseItem    — joined for stock info
    ///   Warehouse        — joined for location
    ///   PurchaseOrder    — checked to flag whether a PO is linked
    /// </summary>
    public partial class SearchMaterialRequestForm : Form
    {
        private readonly ProductionProcessingController      _ctrl     = new ProductionProcessingController();
        private List<MaterialRequestEntity>                  _current  = new List<MaterialRequestEntity>();

        // ── Urgency colour map ───────────────────────────────────────────
        private static readonly Dictionary<string, (Color bg, Color fg)> UrgencyColors =
            new Dictionary<string, (Color, Color)>
            {
                { "Critical", (Color.FromArgb(254, 226, 226), Color.FromArgb(153,  27,  27)) },
                { "High",     (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "Medium",   (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) }
            };

        // ── Trigger colour map ───────────────────────────────────────────
        private static readonly Dictionary<string, (Color bg, Color fg)> TriggerColors =
            new Dictionary<string, (Color, Color)>
            {
                { "Reorder",      (Color.FromArgb(219, 234, 254), Color.FromArgb( 30,  64, 175)) },
                { "OrderDemand",  (Color.FromArgb(243, 232, 255), Color.FromArgb( 88,  28, 135)) }
            };

        private const int D_RowH   = 60;
        private const int D_LabelW = 240;

        public SearchMaterialRequestForm()
        {
            InitializeComponent();
            this.Load += SearchMaterialRequestForm_Load;
        }

        // ════════════════════════════════════════════════════════════════
        //  Load
        // ════════════════════════════════════════════════════════════════

        private void SearchMaterialRequestForm_Load(object sender, EventArgs e)
        {
            dgvRequests.SelectionChanged += (s, _) => UpdateActionButtons();
            dgvRequests.CellDoubleClick  += (s, ce) => { if (ce.RowIndex >= 0) OpenDetailDialog(); };
            dgvRequests.CellFormatting   += DgvRequests_CellFormatting;

            btnViewDetail.Click  += (s, _) => OpenDetailDialog();
            btnCreateNew.Click   += BtnCreateNew_Click;
            btnSearch.Click      += (s, _) => RefreshGrid();
            btnReset.Click       += (s, _) => ResetFilters();
            txtKeyword.KeyDown   += (s, ke) => { if (ke.KeyCode == Keys.Enter) RefreshGrid(); };

            RefreshGrid();
        }

        // ════════════════════════════════════════════════════════════════
        //  Data helpers
        // ════════════════════════════════════════════════════════════════

        internal void RefreshGrid()
        {
            string keyword     = txtKeyword.Text.Trim();
            string urgency     = cboUrgency.SelectedItem?.ToString();
            string triggerType = cboTrigger.SelectedItem?.ToString();

            var vm = _ctrl.GetSearchMaterialRequestVM(
                string.IsNullOrEmpty(keyword) ? null : keyword,
                urgency     == "All" ? null : urgency,
                triggerType == "All" ? null : triggerType);

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Production Processing  \u203a  Search Raw Material Request");

            _current = vm.Requests;

            dgvRequests.Rows.Clear();
            foreach (var r in _current)
            {
                dgvRequests.Rows.Add(
                    r.RequestID,
                    $"{r.RawMaterialItemID}  —  {r.RawMaterialName}",
                    r.MaterialType,
                    r.RequestedQty,
                    r.UrgencyLevel,
                    r.TriggerType,
                    r.OrderID ?? "—",
                    r.WarehouseLocation,
                    r.CurrentStock,
                    r.IsLinkedToPO ? "Yes" : "No");
            }

            RefreshKpi();
            UpdateActionButtons();
        }

        internal void ResetFilters()
        {
            txtKeyword.Text          = string.Empty;
            cboUrgency.SelectedIndex = 0;
            cboTrigger.SelectedIndex = 0;
            RefreshGrid();
        }

        // ════════════════════════════════════════════════════════════════
        //  KPI Pills
        // ════════════════════════════════════════════════════════════════

        private void RefreshKpi()
        {
            pnlKpi.Controls.Clear();

            var all = _ctrl.GetSearchMaterialRequestVM().Requests;

            int total    = all.Count;
            int critical = all.FindAll(r => r.UrgencyLevel == "Critical").Count;
            int high     = all.FindAll(r => r.UrgencyLevel == "High").Count;
            int linked   = all.FindAll(r => r.IsLinkedToPO).Count;

            var pills = new[]
            {
                ("Total Requests", total.ToString(),    Color.FromArgb( 47, 111, 237), Color.FromArgb(219, 234, 254)),
                ("Critical",       critical.ToString(), Color.FromArgb(153,  27,  27), Color.FromArgb(254, 226, 226)),
                ("High",           high.ToString(),     Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199)),
                ("Linked to PO",   linked.ToString(),   Color.FromArgb(  6,  95,  70), Color.FromArgb(209, 250, 229)),
            };

            const int PillW   = 280;
            const int PillH   = 60;
            const int Gap     = 10;
            const int LeftPad = 12;

            var flow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent,
                Padding       = new Padding(0),
                AutoSize      = true,
                AutoSizeMode  = AutoSizeMode.GrowAndShrink
            };

            foreach (var (label, count, fg, bg) in pills)
            {
                var pill = new Panel
                {
                    BackColor = bg,
                    Size      = new Size(PillW, PillH),
                    Margin    = new Padding(0, 0, Gap, 0)
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
                    Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                    BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding = new Padding(10, 0, 8, 0)
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                tlp.Controls.Add(new Label
                {
                    Text = count, Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                    ForeColor = fg, BackColor = Color.Transparent,
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, AutoSize = false
                }, 0, 0);
                tlp.Controls.Add(new Label
                {
                    Text = label, Font = new Font("Segoe UI", 11f),
                    ForeColor = fg, BackColor = Color.Transparent,
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
                }, 1, 0);

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
            btnViewDetail.Enabled = dgvRequests.SelectedRows.Count > 0;
        }

        private void DgvRequests_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.Value == null) return;
            string val     = e.Value.ToString();
            string colName = dgvRequests.Columns[e.ColumnIndex].Name;

            if (colName == "colUrgency" && UrgencyColors.TryGetValue(val, out var uc))
            {
                e.CellStyle.ForeColor = uc.fg; e.CellStyle.BackColor = uc.bg;
                e.CellStyle.SelectionForeColor = uc.fg; e.CellStyle.SelectionBackColor = uc.bg;
                e.CellStyle.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                e.FormattingApplied = true;
            }
            else if (colName == "colTrigger" && TriggerColors.TryGetValue(val, out var tc))
            {
                e.CellStyle.ForeColor = tc.fg; e.CellStyle.BackColor = tc.bg;
                e.CellStyle.SelectionForeColor = tc.fg; e.CellStyle.SelectionBackColor = tc.bg;
                e.CellStyle.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                e.FormattingApplied = true;
            }
            else if (colName == "colLinkedPO")
            {
                if (val == "Yes")
                {
                    e.CellStyle.ForeColor = Color.FromArgb(6, 95, 70);
                    e.CellStyle.BackColor = Color.FromArgb(209, 250, 229);
                    e.CellStyle.SelectionForeColor = Color.FromArgb(6, 95, 70);
                    e.CellStyle.SelectionBackColor = Color.FromArgb(209, 250, 229);
                }
                else
                {
                    e.CellStyle.ForeColor = Color.FromArgb(98, 112, 135);
                    e.CellStyle.BackColor = Color.FromArgb(248, 250, 252);
                    e.CellStyle.SelectionForeColor = Color.FromArgb(98, 112, 135);
                    e.CellStyle.SelectionBackColor = Color.FromArgb(248, 250, 252);
                }
                e.CellStyle.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                e.FormattingApplied = true;
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  Action handlers
        // ════════════════════════════════════════════════════════════════

        private void BtnCreateNew_Click(object sender, EventArgs e)
            => FormNavigator.NavigateTo(this, "Production Processing", "Create Raw Material Request");

        // ════════════════════════════════════════════════════════════════
        //  Detail Dialog
        // ════════════════════════════════════════════════════════════════

        private void OpenDetailDialog()
        {
            if (dgvRequests.SelectedRows.Count == 0) return;

            string requestId = dgvRequests.SelectedRows[0]
                .Cells["colRequestID"].Value?.ToString();

            // Find the entity from local cache
            var req = _current.Find(r => r.RequestID == requestId);
            if (req == null) return;

            // ── Local helpers ────────────────────────────────────────────
            Label ReadLabel(string text) => new Label
            {
                Text = text ?? "\u2014", Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.White
            };

            Panel FieldRow(string labelText, Control input, bool lastRow = false)
            {
                var row = new Panel { Height = D_RowH, BackColor = Color.White };
                if (!lastRow)
                    row.Paint += (s, pe) =>
                    {
                        using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                        pe.Graphics.DrawLine(pen, 0, ((Panel)s).Height - 1, ((Panel)s).Width, ((Panel)s).Height - 1);
                    };

                var tlp = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                    BackColor = Color.White, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D_LabelW));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                var lbl = new Label
                {
                    Text = labelText, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(70, 85, 110), BackColor = Color.FromArgb(248, 250, 252),
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                    AutoSize = false, Padding = new Padding(20, 0, 8, 0)
                };
                var wrap = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(20, 10, 20, 10) };
                input.Dock = DockStyle.Fill;
                wrap.Controls.Add(input);
                tlp.Controls.Add(lbl,  0, 0);
                tlp.Controls.Add(wrap, 1, 0);
                row.Controls.Add(tlp);
                return row;
            }

            // ── Badge label helper ───────────────────────────────────────
            Label BadgeLabel(string text, Dictionary<string, (Color bg, Color fg)> colorMap)
            {
                Color bg = Color.FromArgb(229, 231, 235);
                Color fg = Color.FromArgb(55, 65, 81);
                if (colorMap != null && colorMap.TryGetValue(text ?? "", out var c)) { bg = c.bg; fg = c.fg; }
                return new Label
                {
                    Text = text ?? "\u2014", Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                    ForeColor = fg, BackColor = bg,
                    Dock = DockStyle.Fill, AutoSize = false,
                    TextAlign = ContentAlignment.MiddleCenter
                };
            }

            // ── CARD 1 – Request Header ──────────────────────────────────
            var c1Rows = new Panel[]
            {
                FieldRow("Request ID",    ReadLabel(req.RequestID)),
                FieldRow("Urgency Level", BadgeLabel(req.UrgencyLevel, UrgencyColors)),
                FieldRow("Trigger Type",  BadgeLabel(req.TriggerType,  TriggerColors)),
                FieldRow("Linked Order",  ReadLabel(req.OrderID ?? "None (Reorder)"), lastRow: true)
            };
            var (c1Outer, c1Inner) = CardPanel.Create(
                outerHeight: c1Rows.Length * D_RowH + 22,
                outerPadding: new Padding(20, 14, 20, 8));
            c1Inner.Padding = new Padding(0);
            c1Inner.Controls.Add(BuildStack(c1Rows));

            // ── CARD 2 – Material & Stock Info ───────────────────────────
            var c2Rows = new Panel[]
            {
                FieldRow("Raw Material ID",   ReadLabel(req.RawMaterialItemID)),
                FieldRow("Material Name",     ReadLabel(req.RawMaterialName)),
                FieldRow("Material Type",     ReadLabel(req.MaterialType)),
                FieldRow("Requested Qty",     ReadLabel(req.RequestedQty.ToString())),
                FieldRow("Warehouse",         ReadLabel(req.WarehouseLocation)),
                FieldRow("Warehouse Item ID", ReadLabel(req.WarehouseItemID)),
                FieldRow("Current Stock",     ReadLabel(req.CurrentStock.ToString())),
                FieldRow("Reorder Level",     ReadLabel(req.ReorderLevel.ToString()), lastRow: true)
            };
            var (c2Outer, c2Inner) = CardPanel.Create(
                outerHeight: c2Rows.Length * D_RowH + 30,
                outerPadding: new Padding(20, 8, 20, 16));
            c2Inner.Padding = new Padding(0);
            c2Inner.Controls.Add(BuildStack(c2Rows));

            // ── Dialog shell ─────────────────────────────────────────────
            (Color pillBg, Color pillFg) = UrgencyColors.TryGetValue(req.UrgencyLevel ?? "", out var huc)
                ? huc : (Color.FromArgb(229, 231, 235), Color.FromArgb(55, 65, 81));

            using var dlg = new Form
            {
                Text            = $"Material Request  \u2014  {req.RequestID}",
                Size            = new Size(1100, 800),
                MinimumSize     = new Size(900, 640),
                StartPosition   = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false, MinimizeBox = false,
                BackColor       = Color.FromArgb(240, 244, 249),
                Font            = new Font("Segoe UI", 12f)
            };

            var statusLbl = new Label
            {
                Text = $"{req.UrgencyLevel} Urgency",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = pillFg, BackColor = pillBg,
                Dock = DockStyle.Fill, AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var headerTlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            headerTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            headerTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
            headerTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            headerTlp.Controls.Add(new Label
            {
                Text = $"Material Request  \u2014  {req.RequestID}",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent, Padding = new Padding(40, 0, 0, 0)
            }, 0, 0);
            headerTlp.Controls.Add(statusLbl, 1, 0);

            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top, Height = 88,
                BackColor = Color.FromArgb(19, 35, 61)
            };
            pnlHeader.Controls.Add(headerTlp);

            var pnlFoot = new Panel
            {
                Dock = DockStyle.Bottom, Height = 96,
                BackColor = Color.White, Padding = new Padding(0, 18, 40, 18)
            };
            pnlFoot.Paint += (s, pe) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                pe.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0);
            };
            var btnClose = new Button
            {
                Text = "Close", Font = new Font("Segoe UI", 13f),
                BackColor = Color.White, ForeColor = Color.FromArgb(15, 31, 53),
                FlatStyle = FlatStyle.Flat, Width = 200, Height = 56, Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(200, 207, 220);
            btnClose.FlatAppearance.BorderSize  = 1;
            btnClose.Click += (s, ev) => dlg.Close();
            var footFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight, BackColor = Color.Transparent
            };
            footFlow.Controls.Add(btnClose);
            pnlFoot.Controls.Add(footFlow);

            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 244, 249),
                AutoScroll = true
            };
            scroll.Controls.Add(c2Outer);
            scroll.Controls.Add(c1Outer);

            dlg.Controls.Add(scroll);
            dlg.Controls.Add(pnlFoot);
            dlg.Controls.Add(pnlHeader);
            dlg.ShowDialog(this);
        }

        // ── Stack builder helper ──────────────────────────────────────────
        private Panel BuildStack(Panel[] rows)
        {
            var content = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            var stack   = new Panel { Height = rows.Length * D_RowH, BackColor = Color.White };
            int y = 0;
            foreach (var r in rows)
            {
                r.Location = new Point(0, y);
                r.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                stack.Controls.Add(r);
                y += D_RowH;
            }
            content.Controls.Add(stack);
            content.Resize += (s, _) =>
            {
                var p = (Panel)s;
                stack.Width = p.Width; stack.Left = 0; stack.Top = 0;
                foreach (Panel r in stack.Controls) r.Width = p.Width;
            };
            return content;
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

        private static GraphicsPath RoundedRect(System.Drawing.Rectangle r, int radius)
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
