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
    public partial class SearchMaterialRequestForm : Form
    {
        private readonly ProductionProcessingController  _ctrl    = new ProductionProcessingController();
        private List<MaterialRequestBatchEntity>         _current = new List<MaterialRequestBatchEntity>();

        private static readonly Dictionary<string, (Color bg, Color fg)> UrgencyColors =
            new Dictionary<string, (Color, Color)>
            {
                { "Critical", (Color.FromArgb(254, 226, 226), Color.FromArgb(153,  27,  27)) },
                { "High",     (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "Medium",   (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) }
            };

        private static readonly Dictionary<string, (Color bg, Color fg)> TriggerColors =
            new Dictionary<string, (Color, Color)>
            {
                { "Reorder",     (Color.FromArgb(219, 234, 254), Color.FromArgb( 30,  64, 175)) },
                { "OrderDemand", (Color.FromArgb(243, 232, 255), Color.FromArgb( 88,  28, 135)) }
            };

        public SearchMaterialRequestForm()
        {
            InitializeComponent();
            this.Load += SearchMaterialRequestForm_Load;
        }

        private void SearchMaterialRequestForm_Load(object sender, EventArgs e)
        {
            dgvRequests.SelectionChanged += (s, _) => UpdateActionButtons();
            dgvRequests.CellDoubleClick  += (s, ce) => { if (ce.RowIndex >= 0) OpenDetailDialog(); };
            dgvRequests.CellFormatting   += DgvRequests_CellFormatting;
            RefreshGrid();
        }

        // \u2550\u2550 AppShell handlers (wired once in Designer.cs) \u2550\u2550
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void BtnLogout_Click(object sender, EventArgs e)
        { SessionManager.Clear(); Application.Restart(); }

        // \u2550\u2550 Grid refresh \u2550\u2550
        internal void RefreshGrid()
        {
            string keyword     = txtKeyword.Text.Trim();
            string urgency     = cboUrgency.SelectedItem?.ToString();
            string triggerType = cboTrigger.SelectedItem?.ToString();

            var vm = _ctrl.GetSearchMaterialRequestVM(
                string.IsNullOrEmpty(keyword)     ? null : keyword,
                urgency     == "All" || string.IsNullOrEmpty(urgency)     ? null : urgency,
                triggerType == "All" || string.IsNullOrEmpty(triggerType) ? null : triggerType);

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Production Processing  \u203a  Search Raw Material Request");

            _current = vm.Batches;

            dgvRequests.Rows.Clear();
            foreach (var b in _current)
            {
                dgvRequests.Rows.Add(
                    b.BatchPrefix,
                    b.TotalLines,
                    b.TotalRequestedQty,
                    b.UrgencyLevel,
                    b.TriggerType,
                    b.OrderID ?? "\u2014",
                    b.WarehouseLocation,
                    b.CurrentStock,
                    b.IsLinkedToPO ? "Yes" : "No");
            }

            RefreshKpi(vm);
            UpdateActionButtons();
        }

        internal void ResetFilters()
        {
            txtKeyword.Text          = string.Empty;
            cboUrgency.SelectedIndex = 0;
            cboTrigger.SelectedIndex = 0;
            RefreshGrid();
        }

        // \u2550\u2550 KPI Pills \u2550\u2550
        private void RefreshKpi(PremiumLivingOPS.Models.ViewModels.SearchMaterialRequestViewModel vm)
        {
            pnlKpi.Controls.Clear();

            var all = vm.Requests;
            int total    = vm.Batches.Count;
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

            const int PillW = 260;
            const int PillH =  58;
            const int Gap   =  10;

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
                var pill = new Panel { BackColor = bg, Size = new Size(PillW, PillH), Margin = new Padding(0, 0, Gap, 0) };
                pill.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path  = RoundedRect(((Panel)s).ClientRectangle, 8);
                    using var brush = new SolidBrush(((Panel)s).BackColor);
                    e.Graphics.FillPath(brush, path);
                };
                var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None, Padding = new Padding(10, 0, 8, 0) };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64f));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                var lblCount = new Label { Text = count, Font = new Font("Segoe UI", 14f, FontStyle.Bold), ForeColor = fg, BackColor = Color.Transparent, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, AutoSize = false };
                var lblText  = new Label { Text = label, Font = new Font("Segoe UI", 11f),                ForeColor = fg, BackColor = Color.Transparent, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,   AutoSize = false };
                tlp.Controls.Add(lblCount, 0, 0);
                tlp.Controls.Add(lblText,  1, 0);
                pill.Controls.Add(tlp);
                flow.Controls.Add(pill);
            }

            var wrapper = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            wrapper.Controls.Add(flow);
            wrapper.Layout += (s, e) =>
            {
                var w = (Panel)s;
                flow.Left = 0;
                flow.Top  = Math.Max(0, (w.Height - PillH) / 2);
            };
            pnlKpi.Controls.Add(wrapper);
        }

        private void UpdateActionButtons()
        {
            btnViewDetail.Enabled = dgvRequests.SelectedRows.Count > 0;
        }

        // \u2550\u2550 Cell formatting \u2550\u2550
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
                e.CellStyle.ForeColor = val == "Yes" ? Color.FromArgb(6, 95, 70) : Color.FromArgb(107, 114, 128);
                if (val == "Yes") e.CellStyle.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
                e.FormattingApplied = true;
            }
            else if (colName == "colStock" && int.TryParse(val, out int sv) && sv == 0)
            {
                e.CellStyle.ForeColor = Color.FromArgb(153, 27, 27);
                e.CellStyle.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
                e.FormattingApplied = true;
            }
        }

        private void BtnCreateNew_Click(object sender, EventArgs e)
            => FormNavigator.NavigateTo(this, "Production Processing", "Create Raw Material Request");

        // \u2550\u2550 Detail Dialog \u2550\u2550
        private void OpenDetailDialog()
        {
            if (dgvRequests.SelectedRows.Count == 0) return;
            string batchPrefix = dgvRequests.SelectedRows[0].Cells["colRequestID"].Value?.ToString();
            if (string.IsNullOrEmpty(batchPrefix)) return;

            var detail = _ctrl.GetMaterialRequestBatchDetail(batchPrefix);
            if (detail == null)
            {
                MessageBox.Show("Material Request not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            ShowBatchDetailDialog(detail);
        }

        private void ShowBatchDetailDialog(MaterialRequestBatchDetailEntity d)
        {
            using var dlg = new Form
            {
                Text            = $"Material Request Detail \u2014 {d.BatchPrefix}",
                Size            = new Size(2100, 1100),
                MinimumSize     = new Size(1400,  820),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 13f),
                FormBorderStyle = FormBorderStyle.Sizable,
                MaximizeBox     = true,
                MinimizeBox     = false
            };

            // ── Top: Header ───────────────────────────────────────────────
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            var tblHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(24, 0, 24, 0)
            };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblHeader.Controls.Add(new Label
            {
                Text = $"Material Request Details  \u2014  {d.BatchPrefix}",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold), ForeColor = Color.White,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);
            UrgencyColors.TryGetValue(d.UrgencyLevel ?? "", out var uc);
            tblHeader.Controls.Add(new Label
            {
                Text = d.UrgencyLevel ?? "\u2014",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = uc.fg != default ? uc.fg : Color.White,
                BackColor = uc.bg != default ? uc.bg : Color.FromArgb(80, 80, 80),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false, Padding = new Padding(8, 4, 8, 4)
            }, 1, 0);
            pnlHeader.Controls.Add(tblHeader);

            // ── Top: Meta row ─────────────────────────────────────────────
            var pnlMeta = new Panel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(28, 0, 28, 0), BackColor = Color.White };
            pnlMeta.Paint += DlgPaintBottomBorder;
            var tblMeta = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 6, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblMeta.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12f)); tblMeta.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16f));
            tblMeta.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12f)); tblMeta.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16f));
            tblMeta.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12f)); tblMeta.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32f));
            tblMeta.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblMeta.Controls.Add(DlgMakeLabelKey("Trigger Type"), 0, 0); tblMeta.Controls.Add(DlgMakeLabelVal(d.TriggerType ?? "\u2014"), 1, 0);
            tblMeta.Controls.Add(DlgMakeLabelKey("Linked Order"), 2, 0); tblMeta.Controls.Add(DlgMakeLabelVal(string.IsNullOrEmpty(d.OrderID) ? "\u2014 (Reorder)" : d.OrderID), 3, 0);
            tblMeta.Controls.Add(DlgMakeLabelKey("Total Lines"),  4, 0); tblMeta.Controls.Add(DlgMakeLabelVal(d.TotalLines.ToString()), 5, 0);
            pnlMeta.Controls.Add(tblMeta);

            // ── Top: Section label ────────────────────────────────────────
            var pnlLinesLabel = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = Color.FromArgb(246, 249, 255), Padding = new Padding(28, 0, 0, 0) };
            pnlLinesLabel.Controls.Add(new Label
            {
                Text = "REQUESTED RAW MATERIAL LINES",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(98, 112, 135),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            });
            pnlLinesLabel.Paint += DlgPaintBottomBorder;

            // ── Fill: Lines DataGridView ──────────────────────────────────
            var dgvLines = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true,
                AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false,
                RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 11f), ColumnHeadersHeight = 36, RowTemplate = { Height = 40 },
                EnableHeadersVisualStyles = false,
                GridColor = Color.FromArgb(221, 227, 236),
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
            };
            dgvLines.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 10f, FontStyle.Bold);
            dgvLines.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(246, 249, 255);
            dgvLines.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(98, 112, 135);
            dgvLines.ColumnHeadersDefaultCellStyle.Padding   = new Padding(12, 0, 0, 0);
            dgvLines.DefaultCellStyle.Padding                = new Padding(12, 6, 12, 6);
            dgvLines.DefaultCellStyle.SelectionBackColor     = Color.FromArgb(219, 234, 254);
            dgvLines.DefaultCellStyle.SelectionForeColor     = Color.FromArgb(15, 31, 53);
            dgvLines.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);

            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLineReqID",      HeaderText = "Line Request ID",    FillWeight = 18 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLineMaterialID", HeaderText = "Material ID",        FillWeight = 13 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLineName",        HeaderText = "Material Name",      FillWeight = 22 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLineType",        HeaderText = "Type",               FillWeight = 10 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLineQty",         HeaderText = "Requested Qty",      FillWeight = 10 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLineWHI",         HeaderText = "WH Item ID",         FillWeight = 12 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLineStock",       HeaderText = "Current Stock",      FillWeight = 10 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLineReorder",     HeaderText = "Reorder Level",      FillWeight = 10 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLineLocation",    HeaderText = "Warehouse Location", FillWeight = 25 });

            foreach (var ln in d.Lines)
            {
                int ri = dgvLines.Rows.Add(
                    ln.RequestID, ln.RawMaterialItemID, ln.RawMaterialName,
                    ln.MaterialType, ln.RequestedQty, ln.WarehouseItemID,
                    ln.CurrentStock, ln.ReorderLevel, ln.WarehouseLocation);
                if (ln.CurrentStock <= ln.ReorderLevel)
                    dgvLines.Rows[ri].DefaultCellStyle.BackColor = Color.FromArgb(255, 243, 205);
            }

            // ── Bottom: Footer (Close button) — add LAST so it sits lowest ──
            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 68, BackColor = Color.White, Padding = new Padding(28, 8, 28, 8) };
            pnlFooter.Paint += DlgPaintTopBorder;
            var btnClose = new Button
            {
                Text = "Close", Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                BackColor = Color.White, ForeColor = Color.FromArgb(15, 31, 53),
                FlatStyle = FlatStyle.Flat, Width = 148, Height = 48,
                Dock = DockStyle.Right, Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            btnClose.FlatAppearance.BorderSize  = 1;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnClose.Click += (s, ev) => dlg.Close();
            pnlFooter.Controls.Add(btnClose);

            // ── Bottom: PO Detail row ─────────────────────────────────────
            Panel pnlPoDetail;
            if (!string.IsNullOrEmpty(d.PurchaseID))
            {
                pnlPoDetail = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = Color.White, Padding = new Padding(28, 4, 28, 4) };
                var tblPo = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 6, RowCount = 1, BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None };
                for (int ci = 0; ci < 6; ci++) tblPo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, ci % 2 == 0 ? 13f : 20f));
                tblPo.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                tblPo.Controls.Add(DlgMakeLabelKey("Purchase Order ID"), 0, 0); tblPo.Controls.Add(DlgMakeLabelVal(d.PurchaseID), 1, 0);
                tblPo.Controls.Add(DlgMakeLabelKey("PO Status"),          2, 0); tblPo.Controls.Add(DlgMakeLabelVal(d.PurchaseStatus ?? "\u2014"), 3, 0);
                tblPo.Controls.Add(DlgMakeLabelKey("PO Total Amount"),    4, 0); tblPo.Controls.Add(DlgMakeLabelVal(d.POTotalAmount.HasValue ? $"HK$ {d.POTotalAmount.Value:N2}" : "\u2014"), 5, 0);
                pnlPoDetail.Controls.Add(tblPo);
            }
            else
            {
                pnlPoDetail = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = Color.White, Padding = new Padding(28, 0, 28, 0) };
                pnlPoDetail.Controls.Add(new Label
                {
                    Text = "No Purchase Order has been raised for this request yet.",
                    Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(156, 163, 175),
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
                });
            }

            // ── Bottom: PO section label ──────────────────────────────────
            var pnlPoLabel = new Panel { Dock = DockStyle.Bottom, Height = 38, BackColor = Color.FromArgb(246, 249, 255), Padding = new Padding(28, 0, 0, 0) };
            pnlPoLabel.Controls.Add(new Label
            {
                Text = "LINKED PURCHASE ORDER",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(98, 112, 135),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            });
            pnlPoLabel.Paint += DlgPaintTopBorder;

            // ── Assemble ──────────────────────────────────────────────────
            // DockStyle.Top  — added first = highest visual position
            // DockStyle.Fill — takes remaining space
            // DockStyle.Bottom — added LAST = lowest visual position
            //
            // Top stack (first added = topmost):
            dlg.Controls.Add(pnlHeader);      // Top 1 — dark header
            dlg.Controls.Add(pnlMeta);         // Top 2 — meta row
            dlg.Controls.Add(pnlLinesLabel);   // Top 3 — section label
            // Fill:
            dlg.Controls.Add(dgvLines);        // Fill  — grid
            // Bottom stack (last added = lowest):
            dlg.Controls.Add(pnlPoLabel);      // Bottom 1 (added first) — sits just above PO detail
            dlg.Controls.Add(pnlPoDetail);     // Bottom 2
            dlg.Controls.Add(pnlFooter);       // Bottom 3 (added last)  — lowest / true bottom

            dlg.ShowDialog(this);
        }

        // ── Helpers ──────────────────────────────────────────────────────
        private static Label DlgMakeLabelKey(string text) => new Label
        {
            Text = text, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(98, 112, 135), Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(0, 0, 8, 0)
        };
        private static Label DlgMakeLabelVal(string text) => new Label
        {
            Text = text, Font = new Font("Segoe UI", 12f),
            ForeColor = Color.FromArgb(15, 31, 53), Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true
        };
        private static void DlgPaintBottomBorder(object s, PaintEventArgs e)
        { var p = (Panel)s; using var pen = new Pen(Color.FromArgb(221, 227, 236), 1); e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1); }
        private static void DlgPaintTopBorder(object s, PaintEventArgs e)
        { using var pen = new Pen(Color.FromArgb(221, 227, 236), 1); e.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0); }

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
