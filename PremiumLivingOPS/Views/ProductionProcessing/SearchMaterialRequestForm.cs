using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.ProductionProcessing
{
    /// <summary>
    /// Search Raw Material Request — Production Processing
    ///
    /// Grid rule:
    ///   • ONE row per BatchPrefix  (MRQ-YYMMDD-NNN)
    ///   • The per-line -NN suffix is NEVER shown in the main grid
    ///   • View Detail dialog shows every -NN line item in the batch
    /// </summary>
    public partial class SearchMaterialRequestForm : Form
    {
        private readonly ProductionProcessingController _ctrl = new ProductionProcessingController();
        private List<MaterialRequestBatchEntity> _current = new List<MaterialRequestBatchEntity>();

        private static readonly Font _fontBadge = new Font("Segoe UI", 11f, FontStyle.Bold);

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
            dgvRequests.CellDoubleClick += (s, ce) => { if (ce.RowIndex >= 0) OpenDetailDialog(); };
            dgvRequests.CellFormatting += DgvRequests_CellFormatting;
            RefreshGrid();
        }

        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void BtnLogout_Click(object sender, EventArgs e)
        { SessionManager.Clear(); Application.Restart(); }

        internal void RefreshGrid()
        {
            string keyword = txtKeyword.Text.Trim();
            string urgency = cboUrgency.SelectedItem?.ToString();
            string triggerType = cboTrigger.SelectedItem?.ToString();

            var vm = _ctrl.GetSearchMaterialRequestVM(
                string.IsNullOrEmpty(keyword) ? null : keyword,
                urgency == "All" || string.IsNullOrEmpty(urgency) ? null : urgency,
                triggerType == "All" || string.IsNullOrEmpty(triggerType) ? null : triggerType);

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Production Processing  ›  Search Raw Material Request");

            _current = vm.Batches;
            dgvRequests.Rows.Clear();

            foreach (var b in _current)
            {
                string stockNote = b.CurrentStock == 0
                    ? "⚠ Out of Stock"
                    : b.CurrentStock <= b.ReorderLevel
                        ? "⚠ Low Stock"
                        : "✔ In Stock";

                string displayPrefix = string.IsNullOrEmpty(b.BatchPrefix)
                    ? string.Empty
                    : (b.BatchPrefix.Length > 14 ? b.BatchPrefix.Substring(0, 14) : b.BatchPrefix);

                dgvRequests.Rows.Add(
                    displayPrefix,
                    b.TotalLines.ToString(),
                    b.TotalRequestedQty.ToString(),
                    b.UrgencyLevel,
                    b.TriggerType,
                    b.OrderID ?? "—",
                    b.IsLinkedToPO ? "Yes" : "No",
                    stockNote);
            }

            RefreshKpi(vm);
            UpdateActionButtons();
        }

        internal void ResetFilters()
        {
            txtKeyword.Text = string.Empty;
            cboUrgency.SelectedIndex = 0;
            cboTrigger.SelectedIndex = 0;
            RefreshGrid();
        }

        private void RefreshKpi(SearchMaterialRequestViewModel vm)
        {
            pnlKpi.Controls.Clear();

            var all = vm.Requests;
            int total = vm.Batches.Count;
            int critical = all.FindAll(r => r.UrgencyLevel == "Critical").Count;
            int high = all.FindAll(r => r.UrgencyLevel == "High").Count;
            int linked = all.FindAll(r => r.IsLinkedToPO).Count;

            var pills = new[]
            {
                ("Total Requests", total.ToString(),    Color.FromArgb( 47, 111, 237), Color.FromArgb(219, 234, 254)),
                ("Critical",       critical.ToString(), Color.FromArgb(153,  27,  27), Color.FromArgb(254, 226, 226)),
                ("High",           high.ToString(),     Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199)),
                ("Linked to PO",   linked.ToString(),   Color.FromArgb(  6,  95,  70), Color.FromArgb(209, 250, 229)),
            };

            const int PillW = 258, PillH = 58, Gap = 10;
            var flow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            foreach (var (label, count, fg, bg) in pills)
            {
                var pill = new Panel
                {
                    BackColor = bg,
                    Size = new Size(PillW, PillH),
                    Margin = new Padding(0, 0, Gap, 0)
                };
                pill.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path = RoundedRect(((Panel)s).ClientRectangle, 8);
                    using var brush = new SolidBrush(((Panel)s).BackColor);
                    e.Graphics.FillPath(brush, path);
                };

                var tlp = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 1,
                    BackColor = Color.Transparent,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding = new Padding(10, 0, 8, 0)
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64f));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                tlp.Controls.Add(new Label
                {
                    Text = count,
                    Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                    ForeColor = fg,
                    BackColor = Color.Transparent,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = false
                }, 0, 0);
                tlp.Controls.Add(new Label
                {
                    Text = label,
                    Font = new Font("Segoe UI", 11f),
                    ForeColor = fg,
                    BackColor = Color.Transparent,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoSize = false
                }, 1, 0);
                pill.Controls.Add(tlp);
                flow.Controls.Add(pill);
            }

            var wrapper = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            wrapper.Controls.Add(flow);
            wrapper.Layout += (s, e) =>
            {
                var w = (Panel)s;
                flow.Left = 0;
                flow.Top = Math.Max(0, (w.Height - PillH) / 2);
            };
            pnlKpi.Controls.Add(wrapper);
        }

        private void UpdateActionButtons()
            => btnViewDetail.Enabled = dgvRequests.SelectedRows.Count > 0;

        private void DgvRequests_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.Value == null) return;

            string val = e.Value.ToString();
            string colName = dgvRequests.Columns[e.ColumnIndex].Name;

            if (colName == "colUrgency" && UrgencyColors.TryGetValue(val, out var uc))
            {
                e.CellStyle.ForeColor = uc.fg;
                e.CellStyle.BackColor = uc.bg;
                e.CellStyle.SelectionForeColor = uc.fg;
                e.CellStyle.SelectionBackColor = uc.bg;
                e.CellStyle.Font = _fontBadge;
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                e.FormattingApplied = true;
            }
            else if (colName == "colTrigger" && TriggerColors.TryGetValue(val, out var tc))
            {
                e.CellStyle.ForeColor = tc.fg;
                e.CellStyle.BackColor = tc.bg;
                e.CellStyle.SelectionForeColor = tc.fg;
                e.CellStyle.SelectionBackColor = tc.bg;
                e.CellStyle.Font = _fontBadge;
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                e.FormattingApplied = true;
            }
            else if (colName == "colLinkedPO")
            {
                e.CellStyle.ForeColor = val == "Yes"
                    ? Color.FromArgb(6, 95, 70)
                    : Color.FromArgb(107, 114, 128);
                if (val == "Yes") e.CellStyle.Font = _fontBadge;
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                e.FormattingApplied = true;
            }
            else if (colName == "colStockNote")
            {
                e.CellStyle.ForeColor = val.Contains("Out of Stock") ? Color.FromArgb(153, 27, 27)
                                      : val.Contains("Low Stock") ? Color.FromArgb(146, 64, 14)
                                      : Color.FromArgb(6, 95, 70);
                e.FormattingApplied = true;
            }
            else if (colName == "colLines")
            {
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                e.CellStyle.ForeColor = Color.FromArgb(30, 64, 175);
                e.CellStyle.Font = _fontBadge;
                e.FormattingApplied = true;
            }
            else if (colName == "colTotalQty")
            {
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                e.CellStyle.ForeColor = Color.FromArgb(15, 31, 53);
                e.FormattingApplied = true;
            }
        }

        private void BtnCreateNew_Click(object sender, EventArgs e)
            => FormNavigator.NavigateTo(this, "Production Processing", "Create Raw Material Request");

        private void OpenDetailDialog()
        {
            if (dgvRequests.SelectedRows.Count == 0) return;

            int rowIndex = dgvRequests.SelectedRows[0].Index;
            if (rowIndex < 0 || rowIndex >= _current.Count) return;

            // IMPORTANT:
            // Use the original BatchPrefix from the backing list, not the grid display text.
            // The grid may render a shortened value for display, but detail lookup must use
            // the exact underlying batch key from the repo result.
            string batchPrefix = _current[rowIndex]?.BatchPrefix;
            if (string.IsNullOrEmpty(batchPrefix)) return;

            var detail = _ctrl.GetMaterialRequestBatchDetail(batchPrefix);
            if (detail == null)
            {
                MessageBox.Show(
                    $"No line items found for Request ID: {batchPrefix}.\n"
                    + "Please verify the DB records contain the correct -NN line suffix.",
                    "Not Found",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            ShowBatchDetailDialog(detail);
        }

        private void ShowBatchDetailDialog(MaterialRequestBatchDetailEntity d)
        {
            using var dlg = new Form
            {
                Text = $"Material Request Detail — {d.BatchPrefix}",
                Size = new Size(2100, 1000),
                MinimumSize = new Size(1400, 780),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 13f),
                FormBorderStyle = FormBorderStyle.Sizable,
                MaximizeBox = true,
                MinimizeBox = false
            };

            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            var tblHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(24, 0, 24, 0)
            };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblHeader.Controls.Add(new Label
            {
                Text = $"Material Request Details  —  {d.BatchPrefix}",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false
            }, 0, 0);
            UrgencyColors.TryGetValue(d.UrgencyLevel ?? string.Empty, out var uc);
            tblHeader.Controls.Add(new Label
            {
                Text = d.UrgencyLevel ?? "—",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = uc.fg != default ? uc.fg : Color.White,
                BackColor = uc.bg != default ? uc.bg : Color.FromArgb(80, 80, 80),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Padding = new Padding(8, 4, 8, 4)
            }, 1, 0);
            pnlHeader.Controls.Add(tblHeader);

            var pnlMeta = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.White, Padding = new Padding(28, 0, 28, 0) };
            pnlMeta.Paint += DlgPaintBottomBorder;
            var tblMeta = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 6,
                RowCount = 1,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblMeta.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12f));
            tblMeta.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18f));
            tblMeta.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12f));
            tblMeta.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18f));
            tblMeta.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12f));
            tblMeta.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28f));
            tblMeta.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblMeta.Controls.Add(DlgKey("Trigger Type"), 0, 0);
            tblMeta.Controls.Add(DlgVal(d.TriggerType ?? "—"), 1, 0);
            tblMeta.Controls.Add(DlgKey("Linked Order"), 2, 0);
            tblMeta.Controls.Add(DlgVal(string.IsNullOrEmpty(d.OrderID) ? "— (Reorder)" : d.OrderID), 3, 0);
            tblMeta.Controls.Add(DlgKey("Total Items"), 4, 0);
            tblMeta.Controls.Add(DlgVal(d.TotalLines.ToString()), 5, 0);
            pnlMeta.Controls.Add(tblMeta);

            var pnlLinesLabel = new Panel
            { Dock = DockStyle.Top, Height = 38, BackColor = Color.FromArgb(246, 249, 255), Padding = new Padding(28, 0, 0, 0) };
            pnlLinesLabel.Controls.Add(new Label
            {
                Text = $"REQUESTED RAW MATERIAL LINES  ({d.TotalLines} item{(d.TotalLines == 1 ? "" : "s")})",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            });
            pnlLinesLabel.Paint += DlgPaintBottomBorder;

            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 68, BackColor = Color.White, Padding = new Padding(28, 10, 28, 10) };
            pnlFooter.Paint += DlgPaintTopBorder;
            var btnClose = new Button
            {
                Text = "Close",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 31, 53),
                FlatStyle = FlatStyle.Flat,
                Width = 148,
                Height = 48,
                Dock = DockStyle.Right,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            btnClose.FlatAppearance.BorderSize = 1;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnClose.Click += (s, ev) => dlg.Close();
            pnlFooter.Controls.Add(btnClose);

            var pnlPoLabel = new Panel
            { Dock = DockStyle.Bottom, Height = 38, BackColor = Color.FromArgb(246, 249, 255), Padding = new Padding(28, 0, 0, 0) };
            pnlPoLabel.Controls.Add(new Label
            {
                Text = "LINKED PURCHASE ORDER",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            });
            pnlPoLabel.Paint += DlgPaintTopBorder;

            Panel pnlPoDetail;
            if (!string.IsNullOrEmpty(d.PurchaseID))
            {
                pnlPoDetail = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = Color.White, Padding = new Padding(28, 4, 28, 4) };
                var tblPo = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 6,
                    RowCount = 1,
                    BackColor = Color.Transparent,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None
                };
                tblPo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14f));
                tblPo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 19f));
                tblPo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10f));
                tblPo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 19f));
                tblPo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14f));
                tblPo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24f));
                tblPo.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                tblPo.Controls.Add(DlgKey("Purchase Order ID"), 0, 0);
                tblPo.Controls.Add(DlgVal(d.PurchaseID), 1, 0);
                tblPo.Controls.Add(DlgKey("PO Status"), 2, 0);
                tblPo.Controls.Add(DlgVal(d.PurchaseStatus ?? "—"), 3, 0);
                tblPo.Controls.Add(DlgKey("PO Total Amount"), 4, 0);
                tblPo.Controls.Add(DlgVal(d.POTotalAmount.HasValue ? $"HK$ {d.POTotalAmount.Value:N2}" : "—"), 5, 0);
                pnlPoDetail.Controls.Add(tblPo);
            }
            else
            {
                pnlPoDetail = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = Color.White, Padding = new Padding(28, 0, 28, 0) };
                pnlPoDetail.Controls.Add(new Label
                {
                    Text = "No Purchase Order has been raised for this request yet.",
                    Font = new Font("Segoe UI", 12f),
                    ForeColor = Color.FromArgb(156, 163, 175),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                });
            }

            var dgvLines = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(221, 227, 236),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                Font = new Font("Segoe UI", 11f),
                ColumnHeadersHeight = 36,
                RowTemplate = { Height = 40 },
                EnableHeadersVisualStyles = false
            };
            dgvLines.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            dgvLines.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(246, 249, 255);
            dgvLines.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(98, 112, 135);
            dgvLines.ColumnHeadersDefaultCellStyle.Padding = new Padding(12, 0, 0, 0);
            dgvLines.DefaultCellStyle.Padding = new Padding(12, 6, 12, 6);
            dgvLines.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            dgvLines.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 31, 53);
            dgvLines.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);

            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "cLineID", HeaderText = "LINE REQUEST ID", FillWeight = 18 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "cMatID", HeaderText = "MATERIAL ID", FillWeight = 13 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "cMatName", HeaderText = "MATERIAL NAME", FillWeight = 22 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "cType", HeaderText = "TYPE", FillWeight = 10 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "cQty", HeaderText = "REQUESTED QTY", FillWeight = 10 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "cWHItem", HeaderText = "WH ITEM ID", FillWeight = 12 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "cStock", HeaderText = "CURRENT STOCK", FillWeight = 10 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "cReorder", HeaderText = "REORDER LEVEL", FillWeight = 10 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "cLocation", HeaderText = "WAREHOUSE LOCATION", FillWeight = 25 });

            foreach (var ln in d.Lines)
            {
                int ri = dgvLines.Rows.Add(
                    ln.RequestID,
                    ln.RawMaterialItemID,
                    ln.RawMaterialName,
                    ln.MaterialType,
                    ln.RequestedQty.ToString(),
                    ln.WarehouseItemID,
                    ln.CurrentStock.ToString(),
                    ln.ReorderLevel.ToString(),
                    ln.WarehouseLocation);

                if (ln.CurrentStock == 0)
                    dgvLines.Rows[ri].DefaultCellStyle.BackColor = Color.FromArgb(254, 226, 226);
                else if (ln.CurrentStock <= ln.ReorderLevel)
                    dgvLines.Rows[ri].DefaultCellStyle.BackColor = Color.FromArgb(255, 243, 205);
            }

            dlg.Controls.Add(pnlLinesLabel);
            dlg.Controls.Add(pnlMeta);
            dlg.Controls.Add(pnlHeader);
            dlg.Controls.Add(pnlPoLabel);
            dlg.Controls.Add(pnlPoDetail);
            dlg.Controls.Add(pnlFooter);
            dlg.Controls.Add(dgvLines);

            dlg.ShowDialog(this);
        }

        private static Label DlgKey(string text) => new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(98, 112, 135),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(0, 0, 8, 0)
        };

        private static Label DlgVal(string text) => new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 12f),
            ForeColor = Color.FromArgb(15, 31, 53),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        private static void DlgPaintBottomBorder(object s, PaintEventArgs e)
        {
            var p = (Panel)s;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
        }

        private static void DlgPaintTopBorder(object s, PaintEventArgs e)
        {
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0);
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
