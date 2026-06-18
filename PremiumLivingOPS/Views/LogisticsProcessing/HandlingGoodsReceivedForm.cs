using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
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
    /// • AppShell wired in Designer.cs; this file calls SetUser / SetVisibleMenus /
    ///   SetBreadcrumb in RefreshGrids() only — never re-subscribes events.
    /// • CardPanel three-layer nesting: grey outer → white card → content.
    /// • Grid Tab Switcher: three tab buttons switch Receipts / POs / Invoices.
    /// </summary>
    public partial class HandlingGoodsReceivedForm : Form
    {
        private readonly LogisticsProcessingController _ctrl =
            new LogisticsProcessingController();

        private HandlingGoodsReceivedVM _vm;
        private int _activeGridIndex = 0;
        private bool _tabPaintWired  = false;

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

        public HandlingGoodsReceivedForm()
        {
            InitializeComponent();
            // Events subscribed once in Designer.cs — do NOT re-subscribe here.
            this.Load += HandlingGoodsReceivedForm_Load;
        }

        private static Color FromHex(string hex)
        {
            hex = hex.TrimStart('#');
            return Color.FromArgb(
                Convert.ToInt32(hex.Substring(0, 2), 16),
                Convert.ToInt32(hex.Substring(2, 2), 16),
                Convert.ToInt32(hex.Substring(4, 2), 16));
        }

        // ──────────────────────────────────────────────────────────────────
        //  Load
        // ──────────────────────────────────────────────────────────────────
        private void HandlingGoodsReceivedForm_Load(object sender, EventArgs e)
        {
            RefreshGrids();
            SwitchToGrid(0);
        }

        // ──────────────────────────────────────────────────────────────────
        //  Navigation / logout — wired in Designer.cs
        // ──────────────────────────────────────────────────────────────────
        private void OnTopNavMenuItemClicked(string menu, string subItem)
            => FormNavigator.NavigateTo(this, menu, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to logout?",
                "Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                FormNavigator.NavigateTo(this, "Logout");
        }

        // ──────────────────────────────────────────────────────────────────
        //  Tab switcher
        // ──────────────────────────────────────────────────────────────────
        internal void SwitchToGrid(int index)
        {
            _activeGridIndex = index;
            var tabs = new[] { btnTabReceipts, btnTabPO, btnTabInvoices };

            for (int i = 0; i < tabs.Length; i++)
            {
                bool active = (i == index);
                tabs[i].ForeColor = active ? Color.FromArgb(47, 111, 237) : Color.FromArgb(98, 112, 135);
                tabs[i].Font      = active ? new Font("Segoe UI", 12f, FontStyle.Bold) : new Font("Segoe UI", 12f);
                tabs[i].Invalidate();
                if (tabs[i].Tag is Panel card)
                    card.Visible = active;
            }

            if (!_tabPaintWired)
            {
                btnTabReceipts.Paint += PaintTabUnderline;
                btnTabPO.Paint       += PaintTabUnderline;
                btnTabInvoices.Paint += PaintTabUnderline;
                _tabPaintWired = true;
            }

            UpdateActionButtons();
        }

        private static void PaintTabUnderline(object sender, PaintEventArgs e)
        {
            var btn = (Button)sender;
            if (btn.ForeColor != Color.FromArgb(47, 111, 237)) return;
            using var pen = new Pen(Color.FromArgb(47, 111, 237), 3f);
            e.Graphics.DrawLine(pen, 0, btn.Height - 2, btn.Width, btn.Height - 2);
        }

        // ──────────────────────────────────────────────────────────────────
        //  Action button state
        // ──────────────────────────────────────────────────────────────────
        private void UpdateActionButtons()
        {
            switch (_activeGridIndex)
            {
                case 0:
                    bool hasRcpt = dgvReceipts.SelectedRows.Count > 0;
                    btnViewReceiptLines.Enabled = hasRcpt;
                    btnUploadReceipt.Enabled    = true;
                    if (hasRcpt)
                    {
                        var r = dgvReceipts.SelectedRows[0].Tag as GoodsReceivedEntity;
                        btnViewPODetail.Enabled  = r?.PurchaseID != null;
                        btnRecordInvoice.Enabled = r?.PurchaseID != null;
                    }
                    else
                    {
                        btnViewPODetail.Enabled  = false;
                        btnRecordInvoice.Enabled = false;
                    }
                    break;

                case 1:
                    bool hasPO = dgvPO.SelectedRows.Count > 0;
                    btnViewPODetail.Enabled     = hasPO;
                    btnRecordInvoice.Enabled    = hasPO;
                    btnViewReceiptLines.Enabled = false;
                    btnUploadReceipt.Enabled    = false;
                    break;

                default:
                    btnViewPODetail.Enabled     = false;
                    btnViewReceiptLines.Enabled = false;
                    btnUploadReceipt.Enabled    = false;
                    btnRecordInvoice.Enabled    = false;
                    break;
            }
        }

        // ──────────────────────────────────────────────────────────────────
        //  Data refresh
        // ──────────────────────────────────────────────────────────────────
        private void RefreshGrids()
        {
            string keyword = txtKeyword.Text.Trim();
            string status  = cboStatus.SelectedIndex == 0 ? null : cboStatus.SelectedItem?.ToString();
            DateTime? from = chkDateFrom.Checked ? (DateTime?)dtpDateFrom.Value.Date : null;

            _vm = _ctrl.GetHandlingGoodsReceivedVM(status, keyword, from);
            if (_vm == null) return;

            _shell.SetUser(_vm.UserBar.DisplayName, _vm.UserBar.Department);
            _shell.SetVisibleMenus(_vm.AllowedMenus);
            _shell.SetBreadcrumb("Logistics Processing  \u203a  Handling Goods Received");

            BindReceipts(_vm.Receipts);
            BindPO(_vm.PurchaseOrders);
            BindInvoices(_vm.Invoices);
            RenderKpi();
            UpdateActionButtons();
        }

        private void ResetFilters()
        {
            txtKeyword.Clear();
            cboStatus.SelectedIndex = 0;
            chkDateFrom.Checked     = false;
            dtpDateFrom.Value       = DateTime.Today.AddMonths(-1);
            RefreshGrids();
        }

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

        // ──────────────────────────────────────────────────────────────────
        //  KPI pills
        // ──────────────────────────────────────────────────────────────────
        private void RenderKpi()
        {
            pnlKpi.Controls.Clear();

            var all = _ctrl.GetHandlingGoodsReceivedVM().PurchaseOrders
                      ?? new List<PurchaseOrderEntity>();

            int total     = all.Count;
            int sent      = all.FindAll(p => p.PurchaseStatus == "Sent").Count;
            int partial   = all.FindAll(p => p.PurchaseStatus == "Partially Received").Count;
            int received  = all.FindAll(p => p.PurchaseStatus == "Received").Count;
            int completed = all.FindAll(p => p.PurchaseStatus == "Completed").Count;
            int cancelled = all.FindAll(p => p.PurchaseStatus == "Cancelled").Count;

            var pills = new[]
            {
                ("Total POs",      total.ToString(),     Color.FromArgb( 47, 111, 237), Color.FromArgb(219, 234, 254), "All"),
                ("Sent",           sent.ToString(),      Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199), "Sent"),
                ("Partially Rcvd", partial.ToString(),   Color.FromArgb( 29,  78, 216), Color.FromArgb(219, 234, 254), "Partially Received"),
                ("Received",       received.ToString(),  Color.FromArgb(  3,  96, 170), Color.FromArgb(224, 242, 254), "Received"),
                ("Completed",      completed.ToString(), Color.FromArgb(  6,  95,  70), Color.FromArgb(209, 250, 229), "Completed"),
                ("Cancelled",      cancelled.ToString(), Color.FromArgb(107, 114, 128), Color.FromArgb(243, 244, 246), "Cancelled"),
            };

            var flow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent,
                Padding       = Padding.Empty,
                AutoScroll    = false
            };

            const int PillW = 210, PillH = 60, Gap = 8, NumColW = 70;

            foreach (var (label, count, fg, bg, filterItem) in pills)
            {
                var pill = new Panel
                {
                    BackColor = bg, Size = new Size(PillW, PillH),
                    Margin = new Padding(0, 0, Gap, 0), Cursor = Cursors.Hand
                };
                pill.Paint += (s, ev) =>
                {
                    ev.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path  = RoundedRect(((Panel)s).ClientRectangle, 8);
                    using var brush = new SolidBrush(((Panel)s).BackColor);
                    ev.Graphics.FillPath(brush, path);
                };

                var tlp = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                    BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding = new Padding(10, 0, 8, 0)
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, NumColW));
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
                    Text = label, Font = new Font("Segoe UI", 12f),
                    ForeColor = fg, BackColor = Color.Transparent,
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
                }, 1, 0);

                string fi = filterItem;
                EventHandler click = (s, ev) =>
                {
                    int idx = cboStatus.FindStringExact(fi);
                    if (idx >= 0) cboStatus.SelectedIndex = idx;
                    RefreshGrids();
                };
                pill.Click += click;
                tlp.Click  += click;
                foreach (Control c in tlp.Controls) c.Click += click;
                pill.Controls.Add(tlp);
                flow.Controls.Add(pill);
            }

            pnlKpi.Controls.Add(flow);
        }

        // ──────────────────────────────────────────────────────────────────
        //  Grid event handlers
        // ──────────────────────────────────────────────────────────────────
        private void dgvReceipts_SelectionChanged(object sender, EventArgs e)
        {
            if (_activeGridIndex == 0) UpdateActionButtons();
            if (dgvReceipts.SelectedRows.Count > 0)
            {
                var entity = dgvReceipts.SelectedRows[0].Tag as GoodsReceivedEntity;
                if (entity?.PurchaseID != null) HighlightPORow(entity.PurchaseID);
            }
        }

        private void dgvReceipts_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || dgvReceipts.Columns[e.ColumnIndex].Name != "colPOSt") return;
            ApplyStatusStyle(e);
        }

        private void dgvReceipts_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) ShowReceiptDetail(dgvReceipts.Rows[e.RowIndex].Tag as GoodsReceivedEntity);
        }

        private void dgvPO_SelectionChanged(object sender, EventArgs e)
        {
            if (_activeGridIndex == 1) UpdateActionButtons();
        }

        private void dgvPO_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || dgvPO.Columns[e.ColumnIndex].Name != "colPSt") return;
            ApplyStatusStyle(e);
        }

        private void dgvPO_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) ShowPODetail(dgvPO.Rows[e.RowIndex].Tag as PurchaseOrderEntity);
        }

        private void dgvInvoices_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || dgvInvoices.Columns[e.ColumnIndex].Name != "colInvPay") return;
            ApplyStatusStyle(e);
        }

        private void ApplyStatusStyle(DataGridViewCellFormattingEventArgs e)
        {
            string val = e.Value?.ToString() ?? "";
            if (StatusTheme.TryGetValue(val, out var t))
            {
                e.CellStyle.BackColor          = t.bg;
                e.CellStyle.ForeColor          = t.fg;
                e.CellStyle.Font               = new Font("Segoe UI", 11f, FontStyle.Bold);
                e.CellStyle.SelectionBackColor = t.bg;
                e.CellStyle.SelectionForeColor = t.fg;
                e.FormattingApplied            = true;
            }
        }

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

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X,         bounds.Y,          d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y,          d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d,   0, 90);
            path.AddArc(bounds.X,         bounds.Bottom - d, d, d,  90, 90);
            path.CloseFigure();
            return path;
        }

        // ──────────────────────────────────────────────────────────────────
        //  Action button click handlers
        // ──────────────────────────────────────────────────────────────────
        private void btnViewPODetail_Click(object sender, EventArgs e)
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
            ShowPODetail(po);
        }

        private void btnViewReceiptLines_Click(object sender, EventArgs e)
        {
            if (dgvReceipts.SelectedRows.Count == 0) return;
            ShowReceiptDetail(dgvReceipts.SelectedRows[0].Tag as GoodsReceivedEntity);
        }

        private void ShowPODetail(PurchaseOrderEntity po)
        {
            if (po == null) return;
            var vm = new Models.ViewModels.PODetailVM
            {
                PurchaseOrder = po,
                Lines         = new List<Models.Entities.PurchaseOrderLineEntity>()
            };
            using (var dlg = new PODetailDialog(vm))
                dlg.ShowDialog(this);
        }

        private void ShowReceiptDetail(GoodsReceivedEntity receipt)
        {
            if (receipt == null) return;
            var lines = _vm?.Receipts?
                            .Where(r => r.ReceiptID == receipt.ReceiptID)
                            .ToList()
                        ?? new List<GoodsReceivedEntity>();
            using (var dlg = new ReceiptDetailDialog(receipt, lines))
                dlg.ShowDialog(this);
        }

        private void ShowRecordInvoice(PurchaseOrderEntity po)
        {
            if (po == null) return;
            using (var dlg = new RecordInvoiceDialog(po))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    if (dlg.Result != null)
                    {
                        try   { _ctrl.SavePurchaseInvoice(dlg.Result); }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to save invoice:\n{ex.Message}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                    RefreshGrids();
                }
            }
        }

        private void btnUploadReceipt_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog { Title = "Select Receipt CSV File", Filter = "CSV Files (*.csv)|*.csv" })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;

                ReceiptImportResult result;
                try   { result = _ctrl.ImportReceiptsFromCsv(dlg.FileName); }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unexpected error during import:\n{ex.Message}",
                        "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine($"\u2705  {result.SuccessCount} receipt(s) imported successfully.");
                if (result.HasErrors)
                {
                    sb.AppendLine();
                    sb.AppendLine($"\u26a0\ufe0f  {result.Errors.Count} row(s) skipped due to errors:");
                    foreach (var err in result.Errors)
                        sb.AppendLine($"  \u2022 {err}");
                }
                MessageBox.Show(sb.ToString(),
                    result.HasErrors ? "Import Completed with Warnings" : "Import Successful",
                    MessageBoxButtons.OK,
                    result.HasErrors ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
                if (result.SuccessCount > 0) RefreshGrids();
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
                MessageBox.Show("Please select a Purchase Order or a Receipt with a linked PO.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            ShowRecordInvoice(po);
        }
    }
}
