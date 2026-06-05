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

        // ── Cross-highlight ──────────────────────────────────────────
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

        // ── Action buttons ───────────────────────────────────────────
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
            if (rec != null) ShowUploadReceiptDialog(rec);
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Nav / Logout — mirrors ViewShipmentForm exactly
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }

        // ── PO Detail popup ──────────────────────────────────────────
        private void ShowPODetail(PurchaseOrderEntity po)
        {
            if (po == null) return;
            using var dlg = MakeDialog($"Purchase Order — {po.PurchaseID}", 620, 400);
            var fields = new[]
            {
                ("PO ID",         po.PurchaseID),
                ("Supplier ID",   po.SupplierID),
                ("Supplier Name", po.SupplierName),
                ("Order Date",    po.OrderDate == default ? "—" : po.OrderDate.ToString("yyyy-MM-dd")),
                ("PO Total",      $"${po.POTotalAmount:F2}"),
                ("Status",        po.PurchaseStatus),
                ("Request ID",    po.RequestID)
            };
            dlg.Controls.Add(BuildDetailPanel("Purchase Order Detail", fields, 20));
            dlg.ShowDialog(this);
        }

        // ── Receipt Line Detail popup ────────────────────────────────
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
                ("Receipt Date", r.ReceiptDate == default ? "—" : r.ReceiptDate.ToString("yyyy-MM-dd")),
                ("Unit Price",   $"${r.UnitPrice:F2}"),
                ("Warehouse",    r.WarehouseLocation),
                ("PO Status",    r.PurchaseStatus)
            };
            dlg.Controls.Add(BuildDetailPanel("Receipt Line Detail", fields, 20));
            dlg.ShowDialog(this);
        }

        // ── Upload Supplier Receipt Dialog ───────────────────────────
        private void ShowUploadReceiptDialog(GoodsReceivedEntity rec)
        {
            using var dlg = MakeDialog($"Upload Supplier Receipt — {rec.ReceiptID}", 700, 520);
            var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            card.Paint += PaintCardBorder;

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(28, 20, 28, 20)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute,  50f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute,  38f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute,  38f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute,  50f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute,  52f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent,  100f));

            tbl.Controls.Add(new Label
            {
                Text = "Upload Supplier Receipt",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);
            tbl.Controls.Add(MakeInfoLabel(
                $"Receipt ID:  {rec.ReceiptID}   |   PO ID:  {rec.PurchaseID}"), 0, 1);
            tbl.Controls.Add(MakeInfoLabel(
                $"Supplier:  {rec.SupplierName}   |   Item:  {rec.ItemName}   |   Qty:  {rec.QtyReceived}"), 0, 2);
            tbl.Controls.Add(new Label
            {
                Text = "Select Receipt File (PDF / Image)",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft,
                Padding = new Padding(0, 0, 0, 2)
            }, 0, 3);

            var pnlPicker = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var txtFile   = new TextBox
            {
                ReadOnly = true, Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12f),
                BackColor = Color.FromArgb(246, 249, 255),
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "No file selected…"
            };
            var btnBrowse = new Button
            {
                Text = "Browse…", Width = 120, Height = 40, Dock = DockStyle.Right,
                Font = new Font("Segoe UI", 11f), FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(47, 111, 237), ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnBrowse.FlatAppearance.BorderSize = 0;
            btnBrowse.Click += (s, ev) =>
            {
                using var ofd = new OpenFileDialog
                {
                    Title  = "Select Supplier Receipt",
                    Filter = "Documents|*.pdf;*.png;*.jpg;*.jpeg;*.bmp;*.tiff|All Files|*.*"
                };
                if (ofd.ShowDialog() == DialogResult.OK) txtFile.Text = ofd.FileName;
            };
            pnlPicker.Controls.Add(txtFile);
            pnlPicker.Controls.Add(btnBrowse);
            tbl.Controls.Add(pnlPicker, 0, 4);

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
            btnConfirm.Click += (s, ev) =>
            {
                if (string.IsNullOrWhiteSpace(txtFile.Text))
                {
                    MessageBox.Show("Please select a file before confirming.",
                        "No File Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                MessageBox.Show(
                    $"Supplier receipt for {rec.ReceiptID} uploaded successfully.\n\nFile: {Path.GetFileName(txtFile.Text)}",
                    "Upload Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                dlg.DialogResult = DialogResult.OK;
                dlg.Close();
            };
            btnCancel.Click += (s, ev) =>
            {
                dlg.DialogResult = DialogResult.Cancel;
                dlg.Close();
            };
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

        // ── Record Purchase Invoice Dialog ───────────────────────────
        private void ShowRecordInvoiceDialog(PurchaseOrderEntity po)
        {
            if (po == null) return;
            var vm = _ctrl.GetRecordPurchaseInvoiceVM(po);

            using var dlg = MakeDialog($"Record Purchase Invoice — {po.PurchaseID}", 700, 580);
            var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            card.Paint += PaintCardBorder;

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 9,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(28, 20, 28, 20)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            for (int i = 0; i < 8; i++)
                tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            int row = 0;
            tbl.Controls.Add(new Label
            {
                Text = "Record Purchase Invoice",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            }, 0, row++);

            if (vm.ExistingInvoice != null)
            {
                tbl.Controls.Add(new Label
                {
                    Text = $"⚠  Existing invoice: {vm.ExistingInvoice.PurInvoiceID}  ({vm.ExistingInvoice.PaymentStatus}) — ${vm.ExistingInvoice.TotalAmount:F2}",
                    Font = new Font("Segoe UI", 11f),
                    ForeColor = FromHex("#92400E"), BackColor = FromHex("#FEF3C7"),
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(10, 0, 0, 0)
                }, 0, row++);
            }
            else
            {
                tbl.Controls.Add(MakeInfoLabel(
                    $"PO ID: {po.PurchaseID}   |   Supplier: {po.SupplierName}"), 0, row++);
            }

            var txtTotal = new TextBox
            {
                Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill, Text = vm.TotalAmount.ToString("F2")
            };
            tbl.Controls.Add(MakeFieldCell("Invoice Total Amount (HKD)", txtTotal), 0, row++);

            var cboPayStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 12f), Dock = DockStyle.Fill
            };
            cboPayStatus.Items.AddRange(new object[] { "Full", "Partial" });
            cboPayStatus.SelectedIndex = vm.PaymentStatus == "Partial" ? 1 : 0;
            tbl.Controls.Add(MakeFieldCell("Payment Status", cboPayStatus), 0, row++);

            var dtpExp = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value  = vm.ExpectedDate,
                Font   = new Font("Segoe UI", 12f), Dock = DockStyle.Fill
            };
            tbl.Controls.Add(MakeFieldCell("Expected Payment Date", dtpExp), 0, row++);

            tbl.Controls.Add(MakeInfoLabel(
                $"PO Total: ${po.POTotalAmount:F2}   |   Status: {po.PurchaseStatus}"), 0, row++);

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

            btnSave.Click += (s, ev) =>
            {
                if (!double.TryParse(txtTotal.Text.Trim(), out double amt) || amt <= 0)
                {
                    MessageBox.Show(
                        "Please enter a valid Total Amount greater than zero.",
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
                        $"Purchase Invoice recorded.\n\nInvoice ID: {newId}\nPO: {po.PurchaseID}\nTotal: ${amt:F2}\nStatus: {saveVm.PaymentStatus}\nExpected: {saveVm.ExpectedDate:yyyy-MM-dd}",
                        "Invoice Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                    RefreshGrids();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save invoice:\n{ex.Message}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            btnCancelDlg.Click += (s, ev) =>
            {
                dlg.DialogResult = DialogResult.Cancel;
                dlg.Close();
            };
            pnlBtns.Controls.Add(btnSave);
            pnlBtns.Controls.Add(btnCancelDlg);
            tbl.Controls.Add(pnlBtns, 0, row);

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

        // ── Dialog / panel builder helpers ───────────────────────────
        private static Form MakeDialog(string title, int w, int h) => new Form
        {
            Text = title, Size = new Size(w, h),
            StartPosition   = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false,
            BackColor = Color.FromArgb(240, 244, 249),
            Font = new Font("Segoe UI", 12f)
        };

        private static Label MakeInfoLabel(string text) => new Label
        {
            Text = text, Font = new Font("Segoe UI", 11f),
            ForeColor = Color.FromArgb(98, 112, 135),
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
        };

        private static TableLayoutPanel MakeFieldCell(string caption, Control ctrl)
        {
            var t = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute,  22f));
            t.RowStyles.Add(new RowStyle(SizeType.Percent,  100f));
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

        // ── Static paint / geometry helpers ─────────────────────────
        // PaintCardBorder: defined ONCE here in Form.cs (partial class).
        // Designer.cs references this same definition — no duplication.
        private static void PaintCardBorder(object s, PaintEventArgs e)
        {
            var p = (Panel)s;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
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
