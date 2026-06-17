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
        private readonly ProductionProcessingController      _ctrl    = new ProductionProcessingController();
        private List<MaterialRequestEntity>                  _current = new List<MaterialRequestEntity>();

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

            btnViewDetail.Click += (s, _) => OpenDetailDialog();
            btnCreateNew.Click  += BtnCreateNew_Click;
            btnSearch.Click     += (s, _) => RefreshGrid();
            btnReset.Click      += (s, _) => ResetFilters();
            txtKeyword.KeyDown  += (s, ke) => { if (ke.KeyCode == Keys.Enter) RefreshGrid(); };

            RefreshGrid();
        }

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
                    $"{r.RawMaterialItemID}  \u2014  {r.RawMaterialName}",
                    r.MaterialType,
                    r.RequestedQty,
                    r.UrgencyLevel,
                    r.TriggerType,
                    r.OrderID ?? "\u2014",
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

            const int PillW   = 290;
            const int PillH   =  60;
            const int Gap     =  10;
            const int LeftPad =  12;
            const int NumColW =  70;   // fixed width for count column

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
                // Outer pill panel with rounded-rect paint
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

                // Inner TLP: 2 columns (count | label), 1 row, fills entire pill.
                // Both cells use Dock=Fill so they stretch to the TLP height.
                // TextAlign = MiddleCenter / MiddleLeft ensures vertical centring
                // within each cell without needing an extra wrapper.
                var tlp = new TableLayoutPanel
                {
                    Dock            = DockStyle.Fill,
                    ColumnCount     = 2,
                    RowCount        = 1,
                    BackColor       = Color.Transparent,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding         = new Padding(10, 0, 8, 0)
                };
                // Col 0: fixed width for the number; Col 1: remaining space for text
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, NumColW));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
                // Single row that fills the full pill height
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                var lblCount = new Label
                {
                    Text      = count,
                    Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                    ForeColor = fg,
                    BackColor = Color.Transparent,
                    Dock      = DockStyle.Fill,          // stretches to full cell height
                    TextAlign = ContentAlignment.MiddleCenter,  // vertically centred
                    AutoSize  = false
                };
                var lblText = new Label
                {
                    Text      = label,
                    Font      = new Font("Segoe UI", 11f),
                    ForeColor = fg,
                    BackColor = Color.Transparent,
                    Dock      = DockStyle.Fill,          // stretches to full cell height
                    TextAlign = ContentAlignment.MiddleLeft,    // vertically centred
                    AutoSize  = false
                };

                tlp.Controls.Add(lblCount, 0, 0);
                tlp.Controls.Add(lblText,  1, 0);
                pill.Controls.Add(tlp);
                flow.Controls.Add(pill);
            }

            // Wrapper centres the flow row vertically inside pnlKpi
            var wrapper = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            wrapper.Controls.Add(flow);
            wrapper.Layout += (s, e) =>
            {
                var w = (Panel)s;
                flow.Left = LeftPad;
                flow.Top  = Math.Max(0, (w.Height - PillH) / 2);
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
                    e.CellStyle.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
                }
                else
                {
                    e.CellStyle.ForeColor = Color.FromArgb(107, 114, 128);
                }
                e.FormattingApplied = true;
            }
            else if (colName == "colStock")
            {
                if (int.TryParse(val, out int stockVal) && stockVal == 0)
                {
                    e.CellStyle.ForeColor = Color.FromArgb(153, 27, 27);
                    e.CellStyle.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
                    e.FormattingApplied = true;
                }
            }
        }

        private void BtnCreateNew_Click(object sender, EventArgs e)
            => FormNavigator.NavigateTo(this, "Production Processing", "Create Raw Material Request");

        // ════════════════════════════════════════════════════════════════
        //  Detail Dialog
        // ════════════════════════════════════════════════════════════════

        private void OpenDetailDialog()
        {
            if (dgvRequests.SelectedRows.Count == 0) return;
            string requestId = dgvRequests.SelectedRows[0].Cells["colRequestID"].Value?.ToString();
            if (string.IsNullOrEmpty(requestId)) return;
            var detail = _ctrl.GetMaterialRequestDetail(requestId);
            if (detail == null)
            {
                MessageBox.Show("Material Request not found.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            ShowDetailDialog(detail);
        }

        private void ShowDetailDialog(MaterialRequestDetailEntity d)
        {
            using var dlg = new Form
            {
                Text            = $"Material Request Detail \u2014 {d.RequestID}",
                Size            = new Size(2000, 1200),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 13f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false
            };

            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            var tblHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(24, 0, 24, 0)
            };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblHeader.Controls.Add(new Label
            {
                Text = $"Material Request Details  \u2014  {d.RequestID}",
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

            var pnlInfo = new Panel { Dock = DockStyle.Top, Height = 560, Padding = new Padding(28, 18, 28, 8), BackColor = Color.White };
            pnlInfo.Paint += DlgPaintBottomBorder;
            var tblInfo = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 7,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            for (int i = 0; i < 5; i++) tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 63f / 5f));
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 8f));
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 29f));

            var leftFields  = new (string key, string val)[] { ("Request ID", d.RequestID), ("Material ID", d.RawMaterialItemID), ("Material Name", d.RawMaterialName), ("Material Type", d.MaterialType), ("Requested Qty", d.RequestedQty.ToString()) };
            var rightFields = new (string key, string val)[] { ("Trigger Type", d.TriggerType), ("Urgency Level", d.UrgencyLevel), ("Linked Order", string.IsNullOrEmpty(d.OrderID) ? "\u2014 (Reorder)" : d.OrderID), ("Warehouse Item ID", d.WarehouseItemID), ("Warehouse ID", d.WarehouseID) };
            for (int i = 0; i < leftFields.Length;  i++) { tblInfo.Controls.Add(DlgMakeLabelKey(leftFields[i].key),  0, i); tblInfo.Controls.Add(DlgMakeLabelVal(leftFields[i].val  ?? "\u2014"), 1, i); }
            for (int i = 0; i < rightFields.Length; i++) { tblInfo.Controls.Add(DlgMakeLabelKey(rightFields[i].key), 2, i); tblInfo.Controls.Add(DlgMakeLabelVal(rightFields[i].val ?? "\u2014"), 3, i); }

            var lblWHKey = new Label { Text = "Warehouse Location", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(98, 112, 135), Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft, AutoSize = false };
            tblInfo.Controls.Add(lblWHKey, 0, 5); tblInfo.SetColumnSpan(lblWHKey, 4);
            var lblWHVal = new Label { Text = d.WarehouseLocation ?? "\u2014", Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(15, 31, 53), Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopLeft, AutoEllipsis = false, AutoSize = false, UseMnemonic = false };
            tblInfo.Controls.Add(lblWHVal, 0, 6); tblInfo.SetColumnSpan(lblWHVal, 4);
            pnlInfo.Controls.Add(tblInfo);

            bool isBelowReorder = d.CurrentStock <= d.ReorderLevel;
            var pnlStock = new Panel { Dock = DockStyle.Top, Height = 64, Padding = new Padding(28, 0, 28, 0), BackColor = isBelowReorder ? Color.FromArgb(255, 243, 205) : Color.FromArgb(240, 253, 244) };
            pnlStock.Paint += DlgPaintBottomBorder;
            var tblStock = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 6, RowCount = 1, BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None };
            tblStock.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13f)); tblStock.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10f));
            tblStock.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13f)); tblStock.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10f));
            tblStock.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17f)); tblStock.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37f));
            tblStock.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblStock.Controls.Add(DlgMakeLabelKey("Current Stock"), 0, 0); tblStock.Controls.Add(DlgMakeLabelVal(d.CurrentStock.ToString()), 1, 0);
            tblStock.Controls.Add(DlgMakeLabelKey("Reorder Level"), 2, 0); tblStock.Controls.Add(DlgMakeLabelVal(d.ReorderLevel.ToString()), 3, 0);
            tblStock.Controls.Add(DlgMakeLabelKey("Stock Status"),  4, 0); tblStock.Controls.Add(DlgMakeLabelVal(isBelowReorder ? "\u26A0  Below Reorder Level" : "\u2714  Sufficient Stock"), 5, 0);
            pnlStock.Controls.Add(tblStock);

            var pnlPoLabel = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.FromArgb(246, 249, 255), Padding = new Padding(28, 0, 0, 0) };
            pnlPoLabel.Controls.Add(new Label { Text = "LINKED PURCHASE ORDER", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(98, 112, 135), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft });
            pnlPoLabel.Paint += DlgPaintBottomBorder;

            Panel pnlPoDetail;
            if (!string.IsNullOrEmpty(d.PurchaseID))
            {
                pnlPoDetail = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(28, 12, 28, 12) };
                var tblPo = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 6, RowCount = 1, BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None };
                tblPo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13f)); tblPo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
                tblPo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13f)); tblPo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
                tblPo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13f)); tblPo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 21f));
                tblPo.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                tblPo.Controls.Add(DlgMakeLabelKey("Purchase Order ID"), 0, 0); tblPo.Controls.Add(DlgMakeLabelVal(d.PurchaseID), 1, 0);
                tblPo.Controls.Add(DlgMakeLabelKey("PO Status"), 2, 0);         tblPo.Controls.Add(DlgMakeLabelVal(d.PurchaseStatus ?? "\u2014"), 3, 0);
                tblPo.Controls.Add(DlgMakeLabelKey("PO Total Amount"), 4, 0);   tblPo.Controls.Add(DlgMakeLabelVal(d.POTotalAmount.HasValue ? $"HK$ {d.POTotalAmount.Value:N2}" : "\u2014"), 5, 0);
                pnlPoDetail.Controls.Add(tblPo);
            }
            else
            {
                pnlPoDetail = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(28, 0, 28, 0) };
                pnlPoDetail.Controls.Add(new Label { Text = "No Purchase Order has been raised for this request yet.", Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(156, 163, 175), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft });
            }

            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 86, BackColor = Color.White, Padding = new Padding(28, 14, 28, 14) };
            pnlFooter.Paint += DlgPaintTopBorder;
            var btnClose = new Button { Text = "Close", Font = new Font("Segoe UI", 12f, FontStyle.Bold), BackColor = Color.White, ForeColor = Color.FromArgb(15, 31, 53), FlatStyle = FlatStyle.Flat, Width = 200, Height = 56, Dock = DockStyle.Right, Cursor = Cursors.Hand };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236); btnClose.FlatAppearance.BorderSize = 1; btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnClose.Click += (s, ev) => dlg.Close();
            pnlFooter.Controls.Add(btnClose);

            dlg.Controls.Add(pnlPoDetail);
            dlg.Controls.Add(pnlPoLabel);
            dlg.Controls.Add(pnlStock);
            dlg.Controls.Add(pnlInfo);
            dlg.Controls.Add(pnlHeader);
            dlg.Controls.Add(pnlFooter);
            dlg.ShowDialog(this);
        }

        private static Label DlgMakeLabelKey(string text) => new Label { Text = text, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(98, 112, 135), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(0, 0, 8, 0), AutoEllipsis = false };
        private static Label DlgMakeLabelVal(string text) => new Label { Text = text, Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(15, 31, 53), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };

        private static void DlgPaintBottomBorder(object s, PaintEventArgs e) { var p = (Panel)s; using var pen = new Pen(Color.FromArgb(221, 227, 236), 1); e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1); }
        private static void DlgPaintTopBorder(object s, PaintEventArgs e)    { using var pen = new Pen(Color.FromArgb(221, 227, 236), 1); e.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0); }

        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void BtnLogout_Click(object sender, EventArgs e) { SessionManager.Clear(); Application.Restart(); }

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
