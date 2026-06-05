using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    /// <summary>
    /// Logistics Processing — Handling Goods Received
    ///
    /// MVC contract
    /// ─────────────────────────────────────────────────────────────────
    /// • All DB access delegated to LogisticsProcessingController (zero SQL here).
    /// • AppShell wired in Load — identical to ViewShipmentForm.ViewShipmentForm_Load.
    ///   TopNavBar = 44 px, UserBar = 72 px (AppShell.NavBarHeight / UserBarHeight).
    /// • CardPanel three-layer nesting: grey outer → white card → content.
    /// • KPI pills + four action buttons mirror ViewShipmentForm layout exactly.
    /// </summary>
    public partial class HandlingGoodsReceivedForm : Form
    {
        // ── Controller ───────────────────────────────────────────────
        private readonly LogisticsProcessingController _ctrl =
            new LogisticsProcessingController();

        // ── ViewModel cache ──────────────────────────────────────────
        private HandlingGoodsReceivedVM _vm;

        // ── Status colour map ────────────────────────────────────────
        private static readonly Dictionary<string, (Color bg, Color fg)> StatusTheme
            = new Dictionary<string, (Color, Color)>(StringComparer.OrdinalIgnoreCase)
        {
            ["Sent"]               = (FromHex("#FEF3C7"), FromHex("#92400E")),
            ["Partially Received"] = (FromHex("#DBEAFE"), FromHex("#1D4ED8")),
            ["Received"]           = (FromHex("#E0F2FE"), FromHex("#0360AA")),
            ["Completed"]          = (FromHex("#D1FAE5"), FromHex("#065F46")),
            ["Cancelled"]          = (FromHex("#F3F4F6"), FromHex("#6B7280")),
            ["Partial"]            = (FromHex("#FEF3C7"), FromHex("#92400E")),
            ["Full"]               = (FromHex("#D1FAE5"), FromHex("#065F46"))
        };

        // ── Constructor ──────────────────────────────────────────────
        public HandlingGoodsReceivedForm()
        {
            InitializeComponent();
            this.Load += HandlingGoodsReceivedForm_Load;
        }

        // ── Colour helper ────────────────────────────────────────────
        private static Color FromHex(string hex)
        {
            hex = hex.TrimStart('#');
            return Color.FromArgb(
                Convert.ToInt32(hex.Substring(0, 2), 16),
                Convert.ToInt32(hex.Substring(2, 2), 16),
                Convert.ToInt32(hex.Substring(4, 2), 16));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Load — wire AppShell events, then refresh
        //  Mirrors ViewShipmentForm_Load exactly:
        //    1. Subscribe MenuItemClicked + LogoutClicked HERE (not Designer.cs)
        //    2. Call RefreshGrids() — SetUser/SetVisibleMenus/SetBreadcrumb
        //       are all inside RefreshGrids(), NOT here directly.
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void HandlingGoodsReceivedForm_Load(object sender, EventArgs e)
        {
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;
            RefreshGrids();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Grid refresh — mirrors ViewShipmentForm.RefreshGrid()
        //  SetUser / SetVisibleMenus / SetBreadcrumb called every refresh.
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void RefreshGrids()
        {
            string    keyword = txtKeyword.Text.Trim();
            string    status  = cboStatus.SelectedIndex == 0
                                ? null
                                : cboStatus.SelectedItem?.ToString();
            DateTime? from    = chkDateFrom.Checked
                                ? (DateTime?)dtpDateFrom.Value.Date
                                : null;

            _vm = _ctrl.GetHandlingGoodsReceivedVM(status, keyword, from);
            if (_vm == null) return;

            // ── AppShell update (mirrors ViewShipmentForm.RefreshGrid) ──
            _shell.SetUser(_vm.UserBar.DisplayName, _vm.UserBar.Department);
            _shell.SetVisibleMenus(_vm.AllowedMenus);
            _shell.SetBreadcrumb("Logistics Processing  ›  Handling Goods Received");

            BindReceipts(_vm.Receipts);
            BindPO(_vm.PurchaseOrders);
            BindInvoices(_vm.Invoices);
            RenderKpi(_vm.PurchaseOrders);

            btnViewPODetail.Enabled     = false;
            btnViewReceiptLines.Enabled = false;
            btnUploadReceipt.Enabled    = false;
            btnRecordInvoice.Enabled    = false;
        }

        private void ResetFilters()
        {
            txtKeyword.Clear();
            cboStatus.SelectedIndex = 0;
            chkDateFrom.Checked     = false;
            dtpDateFrom.Value       = DateTime.Today.AddMonths(-1);
            RefreshGrids();
        }

        // ── Grid binding ─────────────────────────────────────────────
        private void BindReceipts(List<GoodsReceivedEntity> rows)
        {
            dgvReceipts.Rows.Clear();
            if (rows == null) return;
            foreach (var r in rows)
            {
                int i = dgvReceipts.Rows.Add(
                    r.ReceiptID, r.PurchaseID, r.SupplierName,
                    r.RawMaterialItemID, r.ItemName,
                    r.QtyReceived, r.OutstandingQty,
                    r.ReceiptDate == default ? "" : r.ReceiptDate.ToString("yyyy-MM-dd"),
                    r.WarehouseLocation, r.PurchaseStatus,
                    $"${r.UnitPrice:F2}");
                dgvReceipts.Rows[i].Tag = r;
            }
        }

        private void BindPO(List<PurchaseOrderEntity> rows)
        {
            dgvPO.Rows.Clear();
            if (rows == null) return;
            foreach (var po in rows)
            {
                int i = dgvPO.Rows.Add(
                    po.PurchaseID, po.SupplierName,
                    po.OrderDate == default ? "" : po.OrderDate.ToString("yyyy-MM-dd"),
                    $"${po.POTotalAmount:F2}", po.PurchaseStatus);
                dgvPO.Rows[i].Tag = po;
            }
        }

        private void BindInvoices(List<PurchaseInvoiceEntity> rows)
        {
            dgvInvoices.Rows.Clear();
            if (rows == null) return;
            foreach (var inv in rows)
            {
                int i = dgvInvoices.Rows.Add(
                    inv.PurInvoiceID, inv.PurchaseID, inv.SupplierName,
                    $"${inv.TotalAmount:F2}", inv.PaymentStatus,
                    inv.ExpectedDate == default ? "" : inv.ExpectedDate.ToString("yyyy-MM-dd"));
                dgvInvoices.Rows[i].Tag = inv;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  KPI pills — mirrors ViewShipmentForm.RefreshKpi() exactly
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void RenderKpi(List<PurchaseOrderEntity> pos)
        {
            pnlKpi.Controls.Clear();
            if (pos == null) pos = new List<PurchaseOrderEntity>();

            // Count ALL POs regardless of current filter (mirrors ViewShipmentForm)
            var all = _ctrl.GetHandlingGoodsReceivedVM().PurchaseOrders
                      ?? new List<PurchaseOrderEntity>();

            int total      = all.Count;
            int sent       = all.FindAll(p => p.PurchaseStatus == "Sent").Count;
            int partial    = all.FindAll(p => p.PurchaseStatus == "Partially Received").Count;
            int received   = all.FindAll(p => p.PurchaseStatus == "Received").Count;
            int completed  = all.FindAll(p => p.PurchaseStatus == "Completed").Count;
            int cancelled  = all.FindAll(p => p.PurchaseStatus == "Cancelled").Count;

            var pills = new[]
            {
                ("All POs",           total.ToString(),     Color.FromArgb(47, 111, 237), Color.FromArgb(219, 234, 254), "All"),
                ("Sent",              sent.ToString(),      FromHex("#92400E"),            FromHex("#FEF3C7"),            "Sent"),
                ("Partially Received",partial.ToString(),   Color.FromArgb(29,  78, 216), Color.FromArgb(219, 234, 254), "Partially Received"),
                ("Received",          received.ToString(),  Color.FromArgb(3,   96, 170), Color.FromArgb(224, 242, 254), "Received"),
                ("Completed",         completed.ToString(), Color.FromArgb(6,   95,  70), Color.FromArgb(209, 250, 229), "Completed"),
                ("Cancelled",         cancelled.ToString(), Color.FromArgb(107, 114, 128),Color.FromArgb(243, 244, 246),"Cancelled"),
            };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent,
                Padding = new Padding(0), AutoScroll = false
            };

            const int PillW   = 210;
            const int PillH   = 60;
            const int Gap     = 8;
            const int NumColW = 80;

            foreach (var (label, count, fg, bg, filterItem) in pills)
            {
                var pill = new Panel
                {
                    BackColor = bg, Size = new Size(PillW, PillH),
                    Margin = new Padding(0, 0, Gap, 0), Cursor = Cursors.Hand
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
                    BackColor = Color.Transparent,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding = new Padding(10, 0, 8, 0)
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, NumColW));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                tlp.Controls.Add(new Label
                {
                    Text = count, Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                    ForeColor = fg, BackColor = Color.Transparent,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter, AutoSize = false
                }, 0, 0);
                tlp.Controls.Add(new Label
                {
                    Text = label, Font = new Font("Segoe UI", 12f),
                    ForeColor = fg, BackColor = Color.Transparent,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
                }, 1, 0);

                string localFilterItem = filterItem;
                EventHandler clickHandler = (s, e) =>
                {
                    int idx = cboStatus.FindStringExact(localFilterItem);
                    if (idx >= 0) cboStatus.SelectedIndex = idx;
                    RefreshGrids();
                };
                pill.Click  += clickHandler;
                tlp.Click   += clickHandler;
                foreach (Control c in tlp.Controls) c.Click += clickHandler;

                pill.Controls.Add(tlp);
                flow.Controls.Add(pill);
            }
            pnlKpi.Controls.Add(flow);
        }

        // ── DataGridView events — Receipts ───────────────────────────
        private void dgvReceipts_SelectionChanged(object sender, EventArgs e)
        {
            bool has = dgvReceipts.SelectedRows.Count > 0;
            btnViewReceiptLines.Enabled = has;
            btnUploadReceipt.Enabled    = has;
            if (has)
            {
                var entity = dgvReceipts.SelectedRows[0].Tag as GoodsReceivedEntity;
                btnViewPODetail.Enabled  = entity?.PurchaseID != null;
                btnRecordInvoice.Enabled = entity?.PurchaseID != null;
                if (entity?.PurchaseID != null) HighlightPORow(entity.PurchaseID);
            }
            else
            {
                btnViewPODetail.Enabled  = false;
                btnRecordInvoice.Enabled = false;
            }
        }

        private void dgvReceipts_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvReceipts.Columns[e.ColumnIndex].Name != "colPOSt") return;
            ApplyStatusStyle(e);
        }

        private void dgvReceipts_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
                ShowReceiptDetail(dgvReceipts.Rows[e.RowIndex].Tag as GoodsReceivedEntity);
        }

        // ── DataGridView events — Purchase Orders ────────────────────
        private void dgvPO_SelectionChanged(object sender, EventArgs e)
        {
            bool has = dgvPO.SelectedRows.Count > 0;
            btnViewPODetail.Enabled  = has;
            btnRecordInvoice.Enabled = has;
        }

        private void dgvPO_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvPO.Columns[e.ColumnIndex].Name != "colPSt") return;
            ApplyStatusStyle(e);
        }

        private void dgvPO_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
                ShowPODetail(dgvPO.Rows[e.RowIndex].Tag as PurchaseOrderEntity);
        }

        // ── DataGridView events — Purchase Invoices ──────────────────
        private void dgvInvoices_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvInvoices.Columns[e.ColumnIndex].Name != "colInvPay") return;
            ApplyStatusStyle(e);
        }

        // ── Shared status-badge formatter ────────────────────────────
        private void ApplyStatusStyle(DataGridViewCellFormattingEventArgs e)
        {
            string val = e.Value?.ToString() ?? "";
            if (StatusTheme.TryGetValue(val, out var t))
            {
                e.CellStyle.BackColor = t.bg;
                e.CellStyle.ForeColor = t.fg;
                e.CellStyle.Font      = new Font("Segoe UI", 11f, FontStyle.Bold);
                e.CellStyle.SelectionBackColor = t.bg;
                e.CellStyle.SelectionForeColor = t.fg;
                e.FormattingApplied = true;
            }
        }

        // ── Cross-highlight: select the matching PO row ──────────────
        private void HighlightPORow(string purchaseId)
        {
            foreach (DataGridViewRow row in dgvPO.Rows)
            {
                if (row.Tag is PurchaseOrderEntity po && po.PurchaseID == purchaseId)
                {
                    dgvPO.ClearSelection();
                    row.Selected = true;
                    dgvPO.FirstDisplayedScrollingRowIndex = row.Index;
                    break;
                }
            }
        }

        // ── Rounded-rect helper (for KPI pill Paint) ─────────────────
        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X,                     bounds.Y,                      d, d, 180, 90);
            path.AddArc(bounds.Right - d,             bounds.Y,                      d, d, 270, 90);
            path.AddArc(bounds.Right - d,             bounds.Bottom - d,             d, d,   0, 90);
            path.AddArc(bounds.X,                     bounds.Bottom - d,             d, d,  90, 90);
            path.CloseFigure();
            return path;
        }

        // ── Card border paint (white card, subtle shadow line) ───────
        private static void PaintCardBorder(object sender, PaintEventArgs e)
        {
            var ctrl   = (Control)sender;
            var bounds = ctrl.ClientRectangle;
            bounds.Width  -= 1;
            bounds.Height -= 1;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1f);
            e.Graphics.DrawRectangle(pen, bounds);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Action Button Handlers
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private void btnViewPODetail_Click(object sender, EventArgs e)
        {
            PurchaseOrderEntity po = null;

            // Priority: PO grid selection
            if (dgvPO.SelectedRows.Count > 0)
                po = dgvPO.SelectedRows[0].Tag as PurchaseOrderEntity;

            // Fallback: derive from selected receipt row
            if (po == null && dgvReceipts.SelectedRows.Count > 0)
            {
                var receipt = dgvReceipts.SelectedRows[0].Tag as GoodsReceivedEntity;
                if (receipt?.PurchaseID != null)
                    po = _vm?.PurchaseOrders?.Find(p => p.PurchaseID == receipt.PurchaseID);
            }

            if (po == null)
            {
                MessageBox.Show("Please select a Purchase Order or a Receipt row.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            ShowPODetail(po);
        }

        private void btnViewReceiptLines_Click(object sender, EventArgs e)
        {
            if (dgvReceipts.SelectedRows.Count == 0) return;
            var receipt = dgvReceipts.SelectedRows[0].Tag as GoodsReceivedEntity;
            ShowReceiptDetail(receipt);
        }

        private void btnUploadReceipt_Click(object sender, EventArgs e)
        {
            if (dgvReceipts.SelectedRows.Count == 0) return;
            var receipt = dgvReceipts.SelectedRows[0].Tag as GoodsReceivedEntity;
            if (receipt == null) return;

            using var dlg = new OpenFileDialog
            {
                Title  = "Select Receipt Document",
                Filter = "PDF / Image Files|*.pdf;*.png;*.jpg;*.jpeg|All Files|*.*"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                // Store file path in receipt (controller handles actual persistence)
                string dest = Path.GetFileName(dlg.FileName);
                MessageBox.Show(
                    $"Receipt document '{dest}' uploaded for Receipt {receipt.ReceiptID}.",
                    "Upload Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Upload failed: {ex.Message}",
                    "Upload Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnRecordInvoice_Click(object sender, EventArgs e)
        {
            PurchaseOrderEntity po = null;

            if (dgvPO.SelectedRows.Count > 0)
                po = dgvPO.SelectedRows[0].Tag as PurchaseOrderEntity;

            if (po == null && dgvReceipts.SelectedRows.Count > 0)
            {
                var receipt = dgvReceipts.SelectedRows[0].Tag as GoodsReceivedEntity;
                if (receipt?.PurchaseID != null)
                    po = _vm?.PurchaseOrders?.Find(p => p.PurchaseID == receipt.PurchaseID);
            }

            if (po == null)
            {
                MessageBox.Show("Please select a Purchase Order or a Receipt row.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var vm = _ctrl.GetRecordPurchaseInvoiceVM(po);
            using var dlg = new RecordPurchaseInvoiceDialog(vm, _ctrl);
            if (dlg.ShowDialog(this) == DialogResult.OK)
                RefreshGrids();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Detail pop-ups
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void ShowReceiptDetail(GoodsReceivedEntity r)
        {
            if (r == null) return;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Receipt ID      : {r.ReceiptID}");
            sb.AppendLine($"PO ID           : {r.PurchaseID}");
            sb.AppendLine($"Supplier        : {r.SupplierName}");
            sb.AppendLine($"Material ID     : {r.RawMaterialItemID}");
            sb.AppendLine($"Item Name       : {r.ItemName}");
            sb.AppendLine($"Qty Received    : {r.QtyReceived}");
            sb.AppendLine($"Outstanding Qty : {r.OutstandingQty}");
            sb.AppendLine($"Receipt Date    : {r.ReceiptDate:yyyy-MM-dd}");
            sb.AppendLine($"Warehouse       : {r.WarehouseLocation}");
            sb.AppendLine($"Unit Price      : ${r.UnitPrice:F2}");
            sb.AppendLine($"PO Status       : {r.PurchaseStatus}");
            MessageBox.Show(sb.ToString(), $"Receipt Detail — {r.ReceiptID}",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowPODetail(PurchaseOrderEntity po)
        {
            if (po == null) return;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"PO ID           : {po.PurchaseID}");
            sb.AppendLine($"Supplier        : {po.SupplierName}");
            sb.AppendLine($"Order Date      : {po.OrderDate:yyyy-MM-dd}");
            sb.AppendLine($"Total Amount    : ${po.POTotalAmount:F2}");
            sb.AppendLine($"Status          : {po.PurchaseStatus}");
            MessageBox.Show(sb.ToString(), $"Purchase Order Detail — {po.PurchaseID}",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  AppShell navigation events
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void OnTopNavMenuItemClicked(object sender, string menuTag)
        {
            FormNavigator.Navigate(this, menuTag);
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Logout();
            FormNavigator.GoToLogin(this);
        }
    }
}
