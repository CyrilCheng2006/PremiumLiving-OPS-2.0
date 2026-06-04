using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    public partial class HandlingGoodsReceivedForm : Form
    {
        // ── Dependencies ───────────────────────────────────────────────
        private readonly LogisticsProcessingController _ctrl;

        // ── ViewModel snapshot ────────────────────────────────────────
        private HandlingGoodsReceivedVM _vm;

        // ── KPI status → visual theme mapping ──────────────────────────
        private static readonly Dictionary<string, (Color bg, Color fg)> StatusTheme
            = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Sent"]               = (ColorFromHex("#FEF3C7"), ColorFromHex("#92400E")),
            ["Partially Received"] = (ColorFromHex("#DBEAFE"), ColorFromHex("#1D4ED8")),
            ["Received"]           = (ColorFromHex("#E0F2FE"), ColorFromHex("#0360AA")),
            ["Completed"]          = (ColorFromHex("#D1FAE5"), ColorFromHex("#065F46")),
            ["Cancelled"]          = (ColorFromHex("#F3F4F6"), ColorFromHex("#6B7280"))
        };

        // ── Constructor ────────────────────────────────────────────────
        public HandlingGoodsReceivedForm()
        {
            InitializeComponent();
            _ctrl = new LogisticsProcessingController();
            this.Load += HandlingGoodsReceivedForm_Load;
        }

        // ── Colour helper ────────────────────────────────────────────
        private static Color ColorFromHex(string hex)
        {
            hex = hex.TrimStart('#');
            return Color.FromArgb(
                Convert.ToInt32(hex.Substring(0, 2), 16),
                Convert.ToInt32(hex.Substring(2, 2), 16),
                Convert.ToInt32(hex.Substring(4, 2), 16));
        }

        // ── Form Load ────────────────────────────────────────────────
        private void HandlingGoodsReceivedForm_Load(object sender, EventArgs e)
        {
            var vm = _ctrl.GetHandlingGoodsReceivedVM(null, null, null);
            if (vm?.UserBar != null)
                _shell.SetUserInfo(vm.UserBar.DisplayName, vm.UserBar.Department);

            if (vm?.AllowedMenus != null)
                _shell.SetMenuItems(vm.AllowedMenus);

            RefreshGrids();
        }

        // ── AppShell navigation ───────────────────────────────────────
        private void OnTopNavMenuItemClicked(object sender, EventArgs e)
        {
            if (_shell.LastClickedModule != null)
                FormNavigator.Navigate(this, _shell.LastClickedModule, _shell.LastClickedItem);
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            FormNavigator.Logout(this);
        }

        // ── Data refresh helpers ──────────────────────────────────────
        private void RefreshGrids()
        {
            string keyword    = txtSearchKeyword.Text.Trim();
            string status     = cboStatus.SelectedIndex == 0 ? null : cboStatus.SelectedItem?.ToString();
            DateTime? dateFrom = chkDateFrom.Checked ? dtpDateFrom.Value.Date : (DateTime?)null;

            _vm = _ctrl.GetHandlingGoodsReceivedVM(status, keyword, dateFrom);

            if (_vm == null) return;

            PopulateReceiptsGrid(_vm.Receipts);
            PopulatePOGrid(_vm.PurchaseOrders);
            RefreshKpi(_vm.PurchaseOrders);

            btnViewPODetail.Enabled     = false;
            btnViewReceiptLines.Enabled = false;
        }

        private void ResetFilters()
        {
            txtSearchKeyword.Clear();
            cboStatus.SelectedIndex = 0;
            chkDateFrom.Checked     = false;
            dtpDateFrom.Value       = DateTime.Today.AddMonths(-1);
            RefreshGrids();
        }

        // ── Grid population ─────────────────────────────────────────────
        private void PopulateReceiptsGrid(List<GoodsReceivedEntity> rows)
        {
            dgvReceipts.Rows.Clear();
            if (rows == null) return;

            foreach (var r in rows)
            {
                int idx = dgvReceipts.Rows.Add(
                    r.ReceiptID,
                    r.PurchaseID,
                    r.SupplierName,
                    r.RawMaterialItemID,
                    r.ItemName,
                    r.QtyReceived,
                    r.OutstandingQty,
                    r.ReceiptDate?.ToString("yyyy-MM-dd") ?? "",
                    r.WarehouseLocation,
                    r.PurchaseStatus,
                    r.UnitPrice.HasValue ? $"${r.UnitPrice:F2}" : ""
                );
                dgvReceipts.Rows[idx].Tag = r;
            }
        }

        private void PopulatePOGrid(List<PurchaseOrderEntity> rows)
        {
            dgvPO.Rows.Clear();
            if (rows == null) return;

            foreach (var po in rows)
            {
                int idx = dgvPO.Rows.Add(
                    po.PurchaseID,
                    po.SupplierName,
                    po.OrderDate?.ToString("yyyy-MM-dd") ?? "",
                    $"${po.POTotalAmount:F2}",
                    po.PurchaseStatus
                );
                dgvPO.Rows[idx].Tag = po;
            }
        }

        // ── KPI pill bar ───────────────────────────────────────────────
        private void RefreshKpi(List<PurchaseOrderEntity> pos)
        {
            pnlKpi.Controls.Clear();

            if (pos == null || pos.Count == 0) return;

            var counts = pos
                .GroupBy(p => p.PurchaseStatus ?? "Unknown", StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            int total = pos.Count;

            var pills = new[]
            {
                ("All POs",            (string)null,        total,
                    Color.FromArgb(239, 246, 255), Color.FromArgb(29, 78, 216)),
                ("Sent",               "Sent",              counts.GetValueOrDefault("Sent"),
                    ColorFromHex("#FEF3C7"), ColorFromHex("#92400E")),
                ("Partially Received", "Partially Received",counts.GetValueOrDefault("Partially Received"),
                    ColorFromHex("#DBEAFE"), ColorFromHex("#1D4ED8")),
                ("Received",           "Received",          counts.GetValueOrDefault("Received"),
                    ColorFromHex("#E0F2FE"), ColorFromHex("#0360AA")),
                ("Completed",          "Completed",         counts.GetValueOrDefault("Completed"),
                    ColorFromHex("#D1FAE5"), ColorFromHex("#065F46")),
                ("Cancelled",          "Cancelled",         counts.GetValueOrDefault("Cancelled"),
                    ColorFromHex("#F3F4F6"), ColorFromHex("#6B7280"))
            };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, AutoScroll = false,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false, BackColor = Color.Transparent,
                Padding = new Padding(0)
            };

            foreach (var (label, filter, count, bg, fg) in pills)
            {
                string capturedFilter = filter;

                var pill = new Panel
                {
                    Width = 148, Height = 52, Margin = new Padding(0, 0, 10, 0),
                    BackColor = bg, Cursor = Cursors.Hand
                };

                pill.Paint += (s, pe) =>
                {
                    pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path = RoundedRect(new Rectangle(0, 0, pill.Width - 1, pill.Height - 1), 10);
                    using var fill = new SolidBrush(bg);
                    pe.Graphics.FillPath(fill, path);
                    using var border = new Pen(Color.FromArgb(60, fg), 1);
                    pe.Graphics.DrawPath(border, path);
                };

                var tbl = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1,
                    BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding = new Padding(10, 6, 10, 6)
                };
                tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 55f));
                tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 45f));

                var lblCount = new Label
                {
                    Text      = count.ToString(),
                    Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                    ForeColor = fg, BackColor = Color.Transparent,
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft
                };
                var lblName = new Label
                {
                    Text      = label,
                    Font      = new Font("Segoe UI", 9f),
                    ForeColor = fg, BackColor = Color.Transparent,
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopLeft
                };
                tbl.Controls.Add(lblCount, 0, 0);
                tbl.Controls.Add(lblName,  0, 1);

                foreach (Control c in new Control[] { tbl, lblCount, lblName })
                    c.Click += (s, ev) => FilterByKpiStatus(capturedFilter);

                pill.Controls.Add(tbl);
                pill.Click += (s, ev) => FilterByKpiStatus(capturedFilter);

                flow.Controls.Add(pill);
            }

            pnlKpi.Controls.Add(flow);
        }

        private void FilterByKpiStatus(string status)
        {
            cboStatus.SelectedIndex = status == null
                ? 0
                : cboStatus.FindStringExact(status);
            RefreshGrids();
        }

        // ── Rounded rectangle helper ───────────────────────────────────
        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // ── DataGridView events — Receipts ──────────────────────────────
        private void dgvReceipts_SelectionChanged(object sender, EventArgs e)
        {
            bool hasRow = dgvReceipts.SelectedRows.Count > 0;
            btnViewReceiptLines.Enabled = hasRow;

            if (hasRow)
            {
                var entity = dgvReceipts.SelectedRows[0].Tag as GoodsReceivedEntity;
                btnViewPODetail.Enabled = entity?.PurchaseID != null;

                if (entity?.PurchaseID != null)
                    HighlightPORow(entity.PurchaseID);
            }
            else
            {
                btnViewPODetail.Enabled = false;
            }
        }

        private void dgvReceipts_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var col = dgvReceipts.Columns[e.ColumnIndex];
            if (col.Name != "colPOStatus_R") return;

            string status = e.Value?.ToString() ?? "";
            if (StatusTheme.TryGetValue(status, out var theme))
            {
                e.CellStyle.BackColor = theme.bg;
                e.CellStyle.ForeColor = theme.fg;
                e.CellStyle.Font      = new Font("Segoe UI", 11f, FontStyle.Bold);
            }
        }

        private void dgvReceipts_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            ShowReceiptLinesDetail(dgvReceipts.Rows[e.RowIndex].Tag as GoodsReceivedEntity);
        }

        // ── DataGridView events — PO grid ───────────────────────────────
        private void dgvPO_SelectionChanged(object sender, EventArgs e)
        {
            bool hasRow = dgvPO.SelectedRows.Count > 0;
            btnViewPODetail.Enabled = hasRow;
        }

        private void dgvPO_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var col = dgvPO.Columns[e.ColumnIndex];
            if (col.Name != "colPOStatus") return;

            string status = e.Value?.ToString() ?? "";
            if (StatusTheme.TryGetValue(status, out var theme))
            {
                e.CellStyle.BackColor = theme.bg;
                e.CellStyle.ForeColor = theme.fg;
                e.CellStyle.Font      = new Font("Segoe UI", 11f, FontStyle.Bold);
            }
        }

        private void dgvPO_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            ShowPODetail(dgvPO.Rows[e.RowIndex].Tag as PurchaseOrderEntity);
        }

        // ── Cross-highlight helper ───────────────────────────────────────
        private void HighlightPORow(string purchaseId)
        {
            foreach (DataGridViewRow row in dgvPO.Rows)
            {
                if (row.Tag is PurchaseOrderEntity po &&
                    string.Equals(po.PurchaseID, purchaseId, StringComparison.OrdinalIgnoreCase))
                {
                    dgvPO.ClearSelection();
                    row.Selected = true;
                    dgvPO.FirstDisplayedScrollingRowIndex = row.Index;
                    break;
                }
            }
        }

        // ── Action buttons ─────────────────────────────────────────────
        private void btnViewPODetail_Click(object sender, EventArgs e)
        {
            PurchaseOrderEntity po = null;

            if (dgvPO.SelectedRows.Count > 0)
                po = dgvPO.SelectedRows[0].Tag as PurchaseOrderEntity;
            else if (dgvReceipts.SelectedRows.Count > 0)
            {
                var receipt = dgvReceipts.SelectedRows[0].Tag as GoodsReceivedEntity;
                if (receipt?.PurchaseID != null)
                    po = _vm?.PurchaseOrders?.FirstOrDefault(p =>
                        string.Equals(p.PurchaseID, receipt.PurchaseID, StringComparison.OrdinalIgnoreCase));
            }

            if (po == null) return;
            ShowPODetail(po);
        }

        private void btnViewReceiptLines_Click(object sender, EventArgs e)
        {
            if (dgvReceipts.SelectedRows.Count == 0) return;
            ShowReceiptLinesDetail(dgvReceipts.SelectedRows[0].Tag as GoodsReceivedEntity);
        }

        // ── PO Detail popup ──────────────────────────────────────────────
        private void ShowPODetail(PurchaseOrderEntity po)
        {
            if (po == null) return;

            using var dlg = new Form
            {
                Text          = $"Purchase Order — {po.PurchaseID}",
                Size          = new Size(620, 420),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox   = false, MinimizeBox = false,
                BackColor     = Color.FromArgb(240, 244, 249),
                Font          = new Font("Segoe UI", 12f)
            };

            var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            card.Paint += PaintCardBorder;

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Color.Transparent,
                Padding = new Padding(24, 20, 24, 20),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));

            var fields = new[]
            {
                ("PO ID",          po.PurchaseID             ?? "—"),
                ("Supplier ID",    po.SupplierID             ?? "—"),
                ("Supplier Name",  po.SupplierName           ?? "—"),
                ("Order Date",     po.OrderDate?.ToString("yyyy-MM-dd") ?? "—"),
                ("PO Total",       $"${po.POTotalAmount:F2}"),
                ("Status",         po.PurchaseStatus         ?? "—"),
                ("Request ID",     po.RequestID              ?? "—")
            };

            tbl.RowCount = fields.Length + 1;
            var lblHdr = new Label
            {
                Text = "Purchase Order Detail",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 0, 10)
            };
            tbl.SetColumnSpan(lblHdr, 2);
            tbl.Controls.Add(lblHdr, 0, 0);
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));

            int row = 1;
            foreach (var (caption, value) in fields)
            {
                tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));
                tbl.Controls.Add(new Label
                {
                    Text = caption, Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(98, 112, 135),
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
                }, 0, row);
                tbl.Controls.Add(new Label
                {
                    Text = value, Font = new Font("Segoe UI", 11f),
                    ForeColor = Color.FromArgb(15, 31, 53),
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
                }, 1, row);
                row++;
            }

            card.Controls.Add(tbl);
            var outer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Color.FromArgb(240, 244, 249) };
            outer.Controls.Add(card);
            dlg.Controls.Add(outer);
            dlg.ShowDialog(this);
        }

        // ── Receipt Lines popup ──────────────────────────────────────────
        private void ShowReceiptLinesDetail(GoodsReceivedEntity receipt)
        {
            if (receipt == null) return;

            using var dlg = new Form
            {
                Text          = $"Receipt Lines — {receipt.ReceiptID}",
                Size          = new Size(700, 520),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox   = false, MinimizeBox = false,
                BackColor     = Color.FromArgb(240, 244, 249),
                Font          = new Font("Segoe UI", 12f)
            };

            var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            card.Paint += PaintCardBorder;

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Color.Transparent,
                Padding = new Padding(24, 20, 24, 20),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));

            var fields = new[]
            {
                ("Receipt ID",     receipt.ReceiptID                         ?? "—"),
                ("PO ID",          receipt.PurchaseID                        ?? "—"),
                ("PO Line ID",     receipt.POLineID                          ?? "—"),
                ("Supplier",       receipt.SupplierName                      ?? "—"),
                ("Material ID",    receipt.RawMaterialItemID                 ?? "—"),
                ("Item Name",      receipt.ItemName                          ?? "—"),
                ("Qty Received",   receipt.QtyReceived.ToString()),
                ("Outstanding",    receipt.OutstandingQty.ToString()),
                ("Receipt Date",   receipt.ReceiptDate?.ToString("yyyy-MM-dd") ?? "—"),
                ("Unit Price",     receipt.UnitPrice.HasValue ? $"${receipt.UnitPrice:F2}" : "—"),
                ("Warehouse",      receipt.WarehouseLocation                 ?? "—"),
                ("PO Status",      receipt.PurchaseStatus                    ?? "—")
            };

            tbl.RowCount = fields.Length + 1;
            var lblHdr = new Label
            {
                Text = "Receipt Line Detail",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 0, 10)
            };
            tbl.SetColumnSpan(lblHdr, 2);
            tbl.Controls.Add(lblHdr, 0, 0);
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));

            int row = 1;
            foreach (var (caption, value) in fields)
            {
                tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
                tbl.Controls.Add(new Label
                {
                    Text = caption, Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(98, 112, 135),
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
                }, 0, row);
                tbl.Controls.Add(new Label
                {
                    Text = value, Font = new Font("Segoe UI", 11f),
                    ForeColor = Color.FromArgb(15, 31, 53),
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
                }, 1, row);
                row++;
            }

            card.Controls.Add(tbl);
            var outer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Color.FromArgb(240, 244, 249) };
            outer.Controls.Add(card);
            dlg.Controls.Add(outer);
            dlg.ShowDialog(this);
        }

        // ── CardBorder painter (used by both popups and card panels) ────────
        private static void PaintCardBorder(object s, PaintEventArgs e)
        {
            var p = (Panel)s;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        }
    }
}
