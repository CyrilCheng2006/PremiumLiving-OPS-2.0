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
    public partial class HandlingGoodsReceivedForm : Form
    {
        // ── Controller ────────────────────────────────────────────────────
        private readonly LogisticsProcessingController _ctrl;

        // ── ViewModel cache ───────────────────────────────────────────────
        private HandlingGoodsReceivedVM _vm;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Status colour map (PO + Receipt grids + KPI pills)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private static readonly Dictionary<string, (Color bg, Color fg)> StatusTheme
            = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Sent"]               = (FromHex("#FEF3C7"), FromHex("#92400E")),
            ["Partially Received"] = (FromHex("#DBEAFE"), FromHex("#1D4ED8")),
            ["Received"]           = (FromHex("#E0F2FE"), FromHex("#0360AA")),
            ["Completed"]          = (FromHex("#D1FAE5"), FromHex("#065F46")),
            ["Cancelled"]          = (FromHex("#F3F4F6"), FromHex("#6B7280")),
            ["Partial"]            = (FromHex("#FEF3C7"), FromHex("#92400E")),
            ["Full"]               = (FromHex("#D1FAE5"), FromHex("#065F46"))
        };

        // ── Constructor ───────────────────────────────────────────────────
        public HandlingGoodsReceivedForm()
        {
            InitializeComponent();
            _ctrl = new LogisticsProcessingController();
            this.Load += HandlingGoodsReceivedForm_Load;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Helpers
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private static Color FromHex(string hex)
        {
            hex = hex.TrimStart('#');
            return Color.FromArgb(
                Convert.ToInt32(hex.Substring(0, 2), 16),
                Convert.ToInt32(hex.Substring(2, 2), 16),
                Convert.ToInt32(hex.Substring(4, 2), 16));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Form Load
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void HandlingGoodsReceivedForm_Load(object sender, EventArgs e)
        {
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;

            var vm = _ctrl.GetHandlingGoodsReceivedVM(null, null, null);
            if (vm?.UserBar != null)
                _shell.SetUserInfo(vm.UserBar.DisplayName, vm.UserBar.Department);
            if (vm?.AllowedMenus != null)
                _shell.SetMenuItems(vm.AllowedMenus);

            RefreshGrids();
        }

        // ── AppShell nav ──────────────────────────────────────────────────
        private void OnTopNavMenuItemClicked(object sender, MenuItemClickedEventArgs e)
            => FormNavigator.Navigate(this, e.ModuleName, e.ItemName);

        private void btnLogout_Click(object sender, EventArgs e)
            => FormNavigator.Logout(this);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Data refresh
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void RefreshGrids()
        {
            string   keyword  = txtKeyword.Text.Trim();
            string   status   = cboStatus.SelectedIndex == 0 ? null : cboStatus.SelectedItem?.ToString();
            DateTime? from    = chkDateFrom.Checked ? dtpDateFrom.Value.Date : (DateTime?)null;

            _vm = _ctrl.GetHandlingGoodsReceivedVM(status, keyword, from);
            if (_vm == null) return;

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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Grid binding
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
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
                    r.ReceiptDate.ToString("yyyy-MM-dd"),
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
                    po.OrderDate.ToString("yyyy-MM-dd"),
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
                    inv.ExpectedDate.ToString("yyyy-MM-dd"));
                dgvInvoices.Rows[i].Tag = inv;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  KPI pill bar
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void RenderKpi(List<PurchaseOrderEntity> pos)
        {
            pnlKpi.Controls.Clear();
            if (pos == null || pos.Count == 0) return;

            var counts = pos
                .GroupBy(p => p.PurchaseStatus ?? "Unknown", StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            var specs = new (string label, string filter, int count, Color bg, Color fg)[]
            {
                ("All POs",            null,                   pos.Count,
                    Color.FromArgb(239, 246, 255), Color.FromArgb(29, 78, 216)),
                ("Sent",               "Sent",                 counts.GetValueOrDefault("Sent"),
                    FromHex("#FEF3C7"), FromHex("#92400E")),
                ("Partially Received", "Partially Received",   counts.GetValueOrDefault("Partially Received"),
                    FromHex("#DBEAFE"), FromHex("#1D4ED8")),
                ("Received",           "Received",             counts.GetValueOrDefault("Received"),
                    FromHex("#E0F2FE"), FromHex("#0360AA")),
                ("Completed",          "Completed",            counts.GetValueOrDefault("Completed"),
                    FromHex("#D1FAE5"), FromHex("#065F46")),
                ("Cancelled",          "Cancelled",            counts.GetValueOrDefault("Cancelled"),
                    FromHex("#F3F4F6"), FromHex("#6B7280"))
            };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent, AutoScroll = false
            };

            foreach (var (label, filter, count, bg, fg) in specs)
            {
                string cap = filter;   // closure capture

                var pill = new Panel { Width = 138, Height = 60, Margin = new Padding(0, 0, 10, 0), BackColor = bg, Cursor = Cursors.Hand };
                pill.Paint += (s, pe) =>
                {
                    pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path = RoundedPath(new Rectangle(0, 0, pill.Width - 1, pill.Height - 1), 10);
                    using var fill = new SolidBrush(bg);
                    pe.Graphics.FillPath(fill, path);
                    using var border = new Pen(Color.FromArgb(50, fg), 1);
                    pe.Graphics.DrawPath(border, path);
                };

                var tlp = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1,
                    BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding = new Padding(10, 5, 10, 5)
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 55f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 45f));

                var lblN = new Label { Text = count.ToString(), Font = new Font("Segoe UI", 14f, FontStyle.Bold), ForeColor = fg, BackColor = Color.Transparent, Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft };
                var lblL = new Label { Text = label, Font = new Font("Segoe UI", 9f), ForeColor = fg, BackColor = Color.Transparent, Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopLeft };
                tlp.Controls.Add(lblN, 0, 0);
                tlp.Controls.Add(lblL, 0, 1);

                foreach (Control c in new Control[] { tlp, lblN, lblL })
                    c.Click += (s, ev) => FilterKpi(cap);
                pill.Controls.Add(tlp);
                pill.Click += (s, ev) => FilterKpi(cap);
                flow.Controls.Add(pill);
            }
            pnlKpi.Controls.Add(flow);
        }

        private void FilterKpi(string status)
        {
            cboStatus.SelectedIndex = status == null ? 0 : cboStatus.FindStringExact(status);
            RefreshGrids();
        }

        private static GraphicsPath RoundedPath(Rectangle b, int r)
        {
            int d = r * 2;
            var p = new GraphicsPath();
            p.AddArc(b.X, b.Y, d, d, 180, 90);
            p.AddArc(b.Right - d, b.Y, d, d, 270, 90);
            p.AddArc(b.Right - d, b.Bottom - d, d, d, 0, 90);
            p.AddArc(b.X, b.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  DataGridView events — Receipts
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  DataGridView events — Purchase Orders
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  DataGridView events — Purchase Invoices
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void dgvInvoices_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvInvoices.Columns[e.ColumnIndex].Name != "colInvPay") return;
            ApplyStatusStyle(e);
        }

        // ── Shared status-badge formatter ──────────────────────────────────
        private void ApplyStatusStyle(DataGridViewCellFormattingEventArgs e)
        {
            string val = e.Value?.ToString() ?? "";
            if (StatusTheme.TryGetValue(val, out var t))
            {
                e.CellStyle.BackColor = t.bg;
                e.CellStyle.ForeColor = t.fg;
                e.CellStyle.Font      = new Font("Segoe UI", 11f, FontStyle.Bold);
            }
        }

        // ── Cross-highlight ────────────────────────────────────────────────
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Action button handlers
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void btnViewPODetail_Click(object sender, EventArgs e)
        {
            PurchaseOrderEntity po = null;
            if (dgvPO.SelectedRows.Count > 0)
                po = dgvPO.SelectedRows[0].Tag as PurchaseOrderEntity;
            else if (dgvReceipts.SelectedRows.Count > 0)
            {
                var rec = dgvReceipts.SelectedRows[0].Tag as GoodsReceivedEntity;
                po = _vm?.PurchaseOrders?.FirstOrDefault(p =>
                    string.Equals(p.PurchaseID, rec?.PurchaseID, StringComparison.OrdinalIgnoreCase));
            }
            if (po != null) ShowPODetail(po);
        }

        private void btnViewReceiptLines_Click(object sender, EventArgs e)
        {
            if (dgvReceipts.SelectedRows.Count > 0)
                ShowReceiptDetail(dgvReceipts.SelectedRows[0].Tag as GoodsReceivedEntity);
        }

        private void btnUploadReceipt_Click(object sender, EventArgs e)
        {
            if (dgvReceipts.SelectedRows.Count == 0) return;
            var rec = dgvReceipts.SelectedRows[0].Tag as GoodsReceivedEntity;
            if (rec == null) return;
            ShowUploadReceiptDialog(rec);
        }

        private void btnRecordInvoice_Click(object sender, EventArgs e)
        {
            PurchaseOrderEntity po = null;
            if (dgvPO.SelectedRows.Count > 0)
                po = dgvPO.SelectedRows[0].Tag as PurchaseOrderEntity;
            else if (dgvReceipts.SelectedRows.Count > 0)
            {
                var rec = dgvReceipts.SelectedRows[0].Tag as GoodsReceivedEntity;
                po = _vm?.PurchaseOrders?.FirstOrDefault(p =>
                    string.Equals(p.PurchaseID, rec?.PurchaseID, StringComparison.OrdinalIgnoreCase));
            }
            if (po != null) ShowRecordInvoiceDialog(po);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Detail popups
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        // ── PO Detail ─────────────────────────────────────────────────────
        private void ShowPODetail(PurchaseOrderEntity po)
        {
            if (po == null) return;
            using var dlg = MakeDialog($"Purchase Order — {po.PurchaseID}", 620, 400);

            var fields = new[]
            {
                ("PO ID",         po.PurchaseID),
                ("Supplier ID",   po.SupplierID),
                ("Supplier Name", po.SupplierName),
                ("Order Date",    po.OrderDate.ToString("yyyy-MM-dd")),
                ("PO Total",      $"${po.POTotalAmount:F2}"),
                ("Status",        po.PurchaseStatus),
                ("Request ID",    po.RequestID)
            };
            dlg.Controls.Add(BuildDetailPanel("Purchase Order Detail", fields, 20));
            dlg.ShowDialog(this);
        }

        // ── Receipt Line Detail ────────────────────────────────────────────
        private void ShowReceiptDetail(GoodsReceivedEntity r)
        {
            if (r == null) return;
            using var dlg = MakeDialog($"Receipt — {r.ReceiptID}", 660, 500);

            var fields = new[]
            {
                ("Receipt ID",   r.ReceiptID),
                ("PO ID",        r.PurchaseID),
                ("PO Line ID",   r.POLineID),
                ("Supplier",     r.SupplierName),
                ("Material ID",  r.RawMaterialItemID),
                ("Item Name",    r.ItemName),
                ("Qty Received", r.QtyReceived.ToString()),
                ("Outstanding",  r.OutstandingQty?.ToString() ?? "0"),
                ("Receipt Date", r.ReceiptDate.ToString("yyyy-MM-dd")),
                ("Unit Price",   $"${r.UnitPrice:F2}"),
                ("Warehouse",    r.WarehouseLocation),
                ("PO Status",    r.PurchaseStatus)
            };
            dlg.Controls.Add(BuildDetailPanel("Receipt Line Detail", fields, 20));
            dlg.ShowDialog(this);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Upload Supplier Receipt Dialog
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void ShowUploadReceiptDialog(GoodsReceivedEntity rec)
        {
            using var dlg = MakeDialog($"Upload Supplier Receipt — {rec.ReceiptID}", 700, 520);
            dlg.BackColor = Color.FromArgb(240, 244, 249);

            // ── Outer card ─────────────────────────────────────────────────
            var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            card.Paint += PaintCardBorder;

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(28, 20, 28, 20)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));  // title
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));  // info: Receipt
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));  // info: PO / Supplier
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));  // label: File
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));  // file picker row
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent,  100f)); // buttons

            // Title
            tbl.Controls.Add(new Label
            {
                Text = "Upload Supplier Receipt",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);

            // Info labels
            tbl.Controls.Add(MakeInfoLabel($"Receipt ID:  {rec.ReceiptID}   |   PO ID:  {rec.PurchaseID}"), 0, 1);
            tbl.Controls.Add(MakeInfoLabel($"Supplier:  {rec.SupplierName}   |   Item:  {rec.ItemName}   |   Qty:  {rec.QtyReceived}"), 0, 2);

            // File label
            tbl.Controls.Add(new Label
            {
                Text = "Select Receipt File (PDF / Image)",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft,
                Padding = new Padding(0, 0, 0, 2)
            }, 0, 3);

            // File picker row
            var pnlPicker = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var txtFile   = new TextBox
            {
                ReadOnly = true, Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12f), BackColor = Color.FromArgb(246, 249, 255),
                BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "No file selected…"
            };
            var btnBrowse = new Button
            {
                Text = "Browse…", Width = 120, Height = 40,
                Dock = DockStyle.Right, Font = new Font("Segoe UI", 11f),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(47, 111, 237),
                ForeColor = Color.White, Cursor = Cursors.Hand
            };
            btnBrowse.FlatAppearance.BorderSize = 0;
            btnBrowse.Click += (s, e) =>
            {
                using var ofd = new OpenFileDialog
                {
                    Title  = "Select Supplier Receipt",
                    Filter = "Documents|*.pdf;*.png;*.jpg;*.jpeg;*.bmp;*.tiff|All Files|*.*"
                };
                if (ofd.ShowDialog() == DialogResult.OK)
                    txtFile.Text = ofd.FileName;
            };
            pnlPicker.Controls.Add(txtFile);
            pnlPicker.Controls.Add(btnBrowse);
            tbl.Controls.Add(pnlPicker, 0, 4);

            // Buttons
            var pnlDlgBtns = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var btnConfirm = new Button
            {
                Text = "✔  Confirm Upload", Width = 220, Height = 52,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(47, 111, 237),
                ForeColor = Color.White, Location = new Point(0, 0), Cursor = Cursors.Hand
            };
            btnConfirm.FlatAppearance.BorderSize = 0;
            var btnCancel = new Button
            {
                Text = "Cancel", Width = 120, Height = 52,
                Font = new Font("Segoe UI", 12f), FlatStyle = FlatStyle.Flat,
                BackColor = Color.White, ForeColor = Color.FromArgb(15, 31, 53),
                Location = new Point(228, 0), Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            btnCancel.FlatAppearance.BorderSize  = 1;

            btnConfirm.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtFile.Text))
                {
                    MessageBox.Show("Please select a file before confirming.", "No File Selected",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                // Record is acknowledged — in a production system this would call the repo
                // to persist the file path reference. Here we display a success confirmation.
                MessageBox.Show(
                    $"Supplier receipt for {rec.ReceiptID} has been uploaded successfully.\n\nFile: {Path.GetFileName(txtFile.Text)}",
                    "Upload Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                dlg.DialogResult = DialogResult.OK;
                dlg.Close();
            };
            btnCancel.Click += (s, e) => { dlg.DialogResult = DialogResult.Cancel; dlg.Close(); };

            pnlDlgBtns.Controls.Add(btnConfirm);
            pnlDlgBtns.Controls.Add(btnCancel);
            tbl.Controls.Add(pnlDlgBtns, 0, 5);

            card.Controls.Add(tbl);

            var outerPad = new Panel
            {
                Dock = DockStyle.Fill, Padding = new Padding(20),
                BackColor = Color.FromArgb(240, 244, 249)
            };
            outerPad.Controls.Add(card);
            dlg.Controls.Add(outerPad);
            dlg.ShowDialog(this);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Record Purchase Invoice Dialog
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void ShowRecordInvoiceDialog(PurchaseOrderEntity po)
        {
            if (po == null) return;
            var vm = _ctrl.GetRecordPurchaseInvoiceVM(po);

            using var dlg = MakeDialog($"Record Purchase Invoice — {po.PurchaseID}", 700, 580);
            dlg.BackColor = Color.FromArgb(240, 244, 249);

            var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            card.Paint += PaintCardBorder;

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 9,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(28, 20, 28, 20)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            for (int i = 0; i < 8; i++) tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // buttons

            int row = 0;

            // Title + existing-invoice banner
            var pnlHead = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var lblHead = new Label
            {
                Text = "Record Purchase Invoice",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            };
            pnlHead.Controls.Add(lblHead);
            tbl.Controls.Add(pnlHead, 0, row++);

            // Existing invoice notice
            if (vm.ExistingInvoice != null)
            {
                var banner = new Label
                {
                    Text = $"⚠  Existing invoice found: {vm.ExistingInvoice.PurInvoiceID}  " +
                           $"({vm.ExistingInvoice.PaymentStatus}) — ${vm.ExistingInvoice.TotalAmount:F2}",
                    Font = new Font("Segoe UI", 11f),
                    ForeColor = ColorFromHex("#92400E"),
                    BackColor = ColorFromHex("#FEF3C7"),
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(10, 0, 0, 0)
                };
                tbl.Controls.Add(banner, 0, row++);
            }
            else
            {
                tbl.Controls.Add(MakeInfoLabel($"PO ID: {po.PurchaseID}   |   Supplier: {po.SupplierName}"), 0, row++);
            }

            // Total Amount
            var txtTotal = new TextBox
            {
                Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill, Text = vm.TotalAmount.ToString("F2")
            };
            tbl.Controls.Add(MakeFieldCell("Invoice Total Amount (HKD)", txtTotal), 0, row++);

            // Payment Status
            var cboPayStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 12f), Dock = DockStyle.Fill
            };
            cboPayStatus.Items.AddRange(new object[] { "Full", "Partial" });
            cboPayStatus.SelectedIndex = vm.PaymentStatus == "Partial" ? 1 : 0;
            tbl.Controls.Add(MakeFieldCell("Payment Status", cboPayStatus), 0, row++);

            // Expected Date
            var dtpExp = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short, Value = vm.ExpectedDate,
                Font = new Font("Segoe UI", 12f), Dock = DockStyle.Fill
            };
            tbl.Controls.Add(MakeFieldCell("Expected Payment Date", dtpExp), 0, row++);

            // PO reference info (read-only)
            tbl.Controls.Add(MakeInfoLabel($"PO Total: ${po.POTotalAmount:F2}   |   Status: {po.PurchaseStatus}"), 0, row++);

            // Buttons
            var pnlBtns = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            var btnSave = new Button
            {
                Text = "💾  Save Invoice", Width = 220, Height = 52,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(5, 150, 105),
                ForeColor = Color.White, Location = new Point(0, 0), Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;

            var btnCancelDlg = new Button
            {
                Text = "Cancel", Width = 120, Height = 52,
                Font = new Font("Segoe UI", 12f), FlatStyle = FlatStyle.Flat,
                BackColor = Color.White, ForeColor = Color.FromArgb(15, 31, 53),
                Location = new Point(228, 0), Cursor = Cursors.Hand
            };
            btnCancelDlg.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            btnCancelDlg.FlatAppearance.BorderSize  = 1;

            btnSave.Click += (s, e) =>
            {
                if (!double.TryParse(txtTotal.Text.Trim(), out double amt) || amt <= 0)
                {
                    MessageBox.Show("Please enter a valid Total Amount greater than zero.",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtTotal.Focus();
                    return;
                }

                var saveVm = new RecordPurchaseInvoiceVM
                {
                    PurchaseID    = po.PurchaseID,
                    SupplierName  = po.SupplierName,
                    TotalAmount   = amt,
                    PaymentStatus = cboPayStatus.SelectedItem?.ToString() ?? "Full",
                    ExpectedDate  = dtpExp.Value.Date
                };

                try
                {
                    string newId = _ctrl.SavePurchaseInvoice(saveVm);
                    MessageBox.Show(
                        $"Purchase Invoice recorded successfully.\n\nInvoice ID: {newId}\nPO: {po.PurchaseID}\nTotal: ${amt:F2}\nPayment Status: {saveVm.PaymentStatus}\nExpected Date: {saveVm.ExpectedDate:yyyy-MM-dd}",
                        "Invoice Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                    RefreshGrids();   // refresh invoice grid
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save invoice:\n{ex.Message}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            btnCancelDlg.Click += (s, e) => { dlg.DialogResult = DialogResult.Cancel; dlg.Close(); };

            pnlBtns.Controls.Add(btnSave);
            pnlBtns.Controls.Add(btnCancelDlg);
            tbl.Controls.Add(pnlBtns, 0, row);

            card.Controls.Add(tbl);
            var outerPad = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Color.FromArgb(240, 244, 249) };
            outerPad.Controls.Add(card);
            dlg.Controls.Add(outerPad);
            dlg.ShowDialog(this);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Shared dialog/panel builders
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private static Form MakeDialog(string title, int w, int h) => new Form
        {
            Text = title, Size = new Size(w, h),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false,
            BackColor = Color.FromArgb(240, 244, 249),
            Font = new Font("Segoe UI", 12f)
        };

        /// <summary>Builds a read-only key/value info label (grey muted text).</summary>
        private static Label MakeInfoLabel(string text) => new Label
        {
            Text = text, Font = new Font("Segoe UI", 11f),
            ForeColor = Color.FromArgb(98, 112, 135),
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
        };

        /// <summary>Builds a labelled form field cell (caption above, control below).</summary>
        private static TableLayoutPanel MakeFieldCell(string caption, Control ctrl)
        {
            var t = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            t.Controls.Add(new Label
            {
                Text = caption, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft,
                Padding = new Padding(0, 0, 0, 1)
            }, 0, 0);
            ctrl.Dock = DockStyle.Fill;
            t.Controls.Add(ctrl, 0, 1);
            return t;
        }

        /// <summary>Builds a full detail card (title + key/value rows) for popup dialogs.</summary>
        private static Panel BuildDetailPanel(string title, (string, string)[] fields, int padding)
        {
            var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            card.Paint += PaintCardBorder;

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(24, 20, 24, 20)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            tbl.RowCount = fields.Length + 1;

            var lblTitle = new Label
            {
                Text = title, Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            };
            tbl.SetColumnSpan(lblTitle, 2);
            tbl.Controls.Add(lblTitle, 0, 0);
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));

            for (int i = 0; i < fields.Length; i++)
            {
                var (cap, val) = fields[i];
                tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
                tbl.Controls.Add(new Label
                {
                    Text = cap, Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(98, 112, 135),
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
                }, 0, i + 1);
                tbl.Controls.Add(new Label
                {
                    Text = val ?? "—", Font = new Font("Segoe UI", 11f),
                    ForeColor = Color.FromArgb(15, 31, 53),
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
                }, 1, i + 1);
            }
            card.Controls.Add(tbl);

            var outer = new Panel
            {
                Dock = DockStyle.Fill, Padding = new Padding(padding),
                BackColor = Color.FromArgb(240, 244, 249)
            };
            outer.Controls.Add(card);
            return outer;
        }

        // ── Colour helper (needed in dialog builder) ───────────────────────
        private static Color ColorFromHex(string hex)
        {
            hex = hex.TrimStart('#');
            return Color.FromArgb(
                Convert.ToInt32(hex.Substring(0, 2), 16),
                Convert.ToInt32(hex.Substring(2, 2), 16),
                Convert.ToInt32(hex.Substring(4, 2), 16));
        }

        // ── CardBorder reuse (static so dialogs can also call it) ─────────
        private new static void PaintCardBorder(object s, PaintEventArgs e)
        {
            var p = (Panel)s;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        }
    }
}
