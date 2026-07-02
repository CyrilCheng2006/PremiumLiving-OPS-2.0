using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.RawMaterial
{
    /// <summary>
    /// View — Search Procurement.
    ///
    /// Main grid shows ONE row per base PO-ID (PO-YYYYMMDD-NNNN).
    /// Row.Tag stores BasePurchaseID so OpenDetailDialog() never relies on cell index.
    /// "View Detail" opens a dialog showing all -NN sub-orders and their line items.
    /// </summary>
    public partial class SearchProcurementForm : Form
    {
        private readonly ProcurementController  _ctrl          = new ProcurementController();
        private List<ProcurementOrderGroup>     _currentGroups = new List<ProcurementOrderGroup>();

        private static readonly Font _fontBadge = new Font("Segoe UI", 11f, FontStyle.Bold);

        // ── Status colour map ────────────────────────────────────────────
        private static readonly Dictionary<string, (Color bg, Color fg)> StatusColors =
            new Dictionary<string, (Color, Color)>
            {
                { "Sent",               (Color.FromArgb(219, 234, 254), Color.FromArgb( 30,  64, 175)) },
                { "Cancelled",          (Color.FromArgb(254, 226, 226), Color.FromArgb(153,  27,  27)) },
                { "Partially Received", (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "Received",           (Color.FromArgb(243, 232, 255), Color.FromArgb( 88,  28, 135)) },
                { "Completed",          (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
                { "Mixed",              (Color.FromArgb(229, 231, 235), Color.FromArgb( 55,  65,  81)) }
            };

        // ── Urgency colour map ───────────────────────────────────────────
        private static readonly Dictionary<string, (Color bg, Color fg)> UrgencyColors =
            new Dictionary<string, (Color, Color)>
            {
                { "Critical", (Color.FromArgb(254, 226, 226), Color.FromArgb(153,  27,  27)) },
                { "High",     (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "Medium",   (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) }
            };

        public SearchProcurementForm()
        {
            InitializeComponent();
            this.Load += SearchProcurementForm_Load;
        }

        // ════════════════════════════════════════════════════════════════
        //  Load
        // ════════════════════════════════════════════════════════════════
        private void SearchProcurementForm_Load(object sender, EventArgs e)
        {
            dgvOrders.SelectionChanged += (s, _) => UpdateActionButtons();
            dgvOrders.CellDoubleClick  += (s, ce) => { if (ce.RowIndex >= 0) OpenDetailDialog(); };
            dgvOrders.CellFormatting   += DgvOrders_CellFormatting;

            btnViewDetail.Click += (s, _) => OpenDetailDialog();
            btnCreateNew.Click  += BtnCreateNew_Click;

            RefreshGrid();
        }

        // ════════════════════════════════════════════════════════════════
        //  Data helpers
        // ════════════════════════════════════════════════════════════════
        internal void RefreshGrid()
        {
            string    keyword  = txtKeyword.Text.Trim();
            string    status   = cboStatus.SelectedItem?.ToString();
            DateTime? dateFrom = chkUseDateRange.Checked ? (DateTime?)dtpDateFrom.Value.Date : null;
            DateTime? dateTo   = chkUseDateRange.Checked ? (DateTime?)dtpDateTo.Value.Date   : null;

            var vm = _ctrl.GetSearchProcurementVM(
                string.IsNullOrEmpty(keyword) ? null : keyword,
                status == "All" ? null : status,
                dateFrom, dateTo);

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Raw Material  \u203a  Search Procurement");

            _currentGroups = vm.Groups ?? new List<ProcurementOrderGroup>();

            dgvOrders.Rows.Clear();
            foreach (var g in _currentGroups)
            {
                int ri = dgvOrders.Rows.Add(
                    g.BasePurchaseID,
                    g.SupplierName,
                    $"{g.ItemCount} item(s)",
                    g.OrderDateStr,
                    $"HK$ {g.TotalAmount:N2}",
                    g.PurchaseStatus,
                    g.UrgencyLevel);

                // Store BasePurchaseID in Tag for safe retrieval in OpenDetailDialog
                dgvOrders.Rows[ri].Tag = g.BasePurchaseID;
            }

            RefreshKpi();
            UpdateActionButtons();
        }

        internal void ResetFilters()
        {
            txtKeyword.Text = string.Empty;
            cboStatus.SelectedIndex = 0;
            chkUseDateRange.Checked = false;
            dtpDateFrom.Value = DateTime.Today.AddMonths(-3);
            dtpDateTo.Value   = DateTime.Today;
            RefreshGrid();
        }

        // ════════════════════════════════════════════════════════════════
        //  KPI Pills
        // ════════════════════════════════════════════════════════════════
        private void RefreshKpi()
        {
            pnlKpi.Controls.Clear();
            var groups = _currentGroups ?? new List<ProcurementOrderGroup>();

            int total     = groups.Count;
            int sent      = groups.FindAll(g => g.PurchaseStatus == "Sent").Count;
            int partial   = groups.FindAll(g => g.PurchaseStatus == "Partially Received").Count;
            int received  = groups.FindAll(g => g.PurchaseStatus == "Received").Count;
            int completed = groups.FindAll(g => g.PurchaseStatus == "Completed").Count;

            var pills = new[]
            {
                ("Total Orders", total.ToString(),     Color.FromArgb( 47, 111, 237), Color.FromArgb(219, 234, 254)),
                ("Sent",         sent.ToString(),      Color.FromArgb( 30,  64, 175), Color.FromArgb(219, 234, 254)),
                ("Partially",    partial.ToString(),   Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199)),
                ("Received",     received.ToString(),  Color.FromArgb( 88,  28, 135), Color.FromArgb(243, 232, 255)),
                ("Completed",    completed.ToString(), Color.FromArgb(  6,  95,  70), Color.FromArgb(209, 250, 229)),
            };

            const int PillW   = 260;
            const int PillH   =  60;
            const int Gap     =  10;
            const int LeftPad =  12;
            const int NumColW =  70;

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
                    Margin    = new Padding(0, 0, Gap, 0),
                    Cursor    = Cursors.Default
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
                    ColumnCount     = 2, RowCount = 1,
                    BackColor       = Color.Transparent,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding         = new Padding(10, 0, 8, 0)
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, NumColW));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
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
                flow.Top  = Math.Max(0, (w.Height - PillH) / 2);
            };
            pnlKpi.Controls.Add(wrapper);
        }

        private void UpdateActionButtons()
            => btnViewDetail.Enabled = dgvOrders.SelectedRows.Count > 0;

        private void DgvOrders_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Value == null) return;
            string val     = e.Value.ToString();
            string colName = dgvOrders.Columns[e.ColumnIndex].Name;

            void Apply(Color bg, Color fg, bool bold = false)
            {
                e.CellStyle.BackColor          = bg;
                e.CellStyle.ForeColor          = fg;
                e.CellStyle.SelectionBackColor = bg;
                e.CellStyle.SelectionForeColor = fg;
                if (bold) e.CellStyle.Font     = _fontBadge;
                e.CellStyle.Alignment          = DataGridViewContentAlignment.MiddleCenter;
                e.FormattingApplied            = true;
            }

            switch (colName)
            {
                case "colStatus":
                    if (StatusColors.TryGetValue(val, out var sc)) Apply(sc.bg, sc.fg, bold: true);
                    break;
                case "colUrgency":
                    if (UrgencyColors.TryGetValue(val, out var uc)) Apply(uc.bg, uc.fg, bold: true);
                    break;
                case "colItems":
                    Apply(Color.FromArgb(219, 234, 254), Color.FromArgb(47, 111, 237), bold: true);
                    break;
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  Navigation
        // ════════════════════════════════════════════════════════════════
        private void BtnCreateNew_Click(object sender, EventArgs e)
            => FormNavigator.NavigateTo(this, "Raw Material", "Create Procurement");

        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }

        // ════════════════════════════════════════════════════════════════
        //  Detail Dialog
        // ════════════════════════════════════════════════════════════════

        private void OpenDetailDialog()
        {
            if (dgvOrders.SelectedRows.Count == 0) return;

            // Read from Row.Tag (set in RefreshGrid) — safe regardless of sort order
            string basePurchaseId = dgvOrders.SelectedRows[0].Tag?.ToString();
            if (string.IsNullOrEmpty(basePurchaseId)) return;

            var vm = _ctrl.GetProcurementDetailVM(basePurchaseId);
            if (vm == null || vm.Orders == null || vm.Orders.Count == 0)
            {
                MessageBox.Show(
                    $"No records found for Purchase Order: {basePurchaseId}.\n"
                    + "Please verify the database records.",
                    "Not Found",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ShowProcurementDetailDialog(basePurchaseId, vm);
        }

        private void ShowProcurementDetailDialog(string basePurchaseId, ProcurementDetailViewModel vm)
        {
            var orders = vm.Orders;
            var lines  = vm.Lines ?? new List<PurchaseOrderLineEntity>();
            var first  = orders[0];

            double grandTotal = 0;
            foreach (var o in orders) grandTotal += o.POTotalAmount;

            string statusDisplay = orders.Count == 1
                ? first.PurchaseStatus
                : (new HashSet<string>(orders.ConvertAll(o => o.PurchaseStatus)).Count == 1
                    ? first.PurchaseStatus : "Mixed");

            using var dlg = new Form
            {
                Text            = $"Purchase Order Detail — {basePurchaseId}",
                Size            = new Size(1400, 900),
                MinimumSize     = new Size(1100, 700),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 13f),
                FormBorderStyle = FormBorderStyle.Sizable,
                MaximizeBox     = true, MinimizeBox = false
            };

            // ── HEADER ─────────────────────────────────────────────────────
            StatusColors.TryGetValue(statusDisplay ?? string.Empty, out var hsc);
            Color hBg = hsc.bg != default ? hsc.bg : Color.FromArgb(229, 231, 235);
            Color hFg = hsc.fg != default ? hsc.fg : Color.FromArgb(55, 65, 81);

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
                Text = $"Purchase Order Details  —  {basePurchaseId}",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);
            tblHeader.Controls.Add(new Label
            {
                Text = statusDisplay ?? "—",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = hFg, BackColor = hBg,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false, Padding = new Padding(8, 4, 8, 4)
            }, 1, 0);
            pnlHeader.Controls.Add(tblHeader);

            // ── META ROW (Supplier / Date / Items / Grand Total) ─────────
            var pnlMeta = new Panel
            {
                Dock = DockStyle.Top, Height = 64,
                BackColor = Color.White, Padding = new Padding(28, 0, 28, 0)
            };
            pnlMeta.Paint += DlgPaintBottomBorder;
            var tblMeta = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 8, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            for (int i = 0; i < 8; i++)
                tblMeta.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5f));
            tblMeta.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblMeta.Controls.Add(DlgKey("Supplier"),    0, 0);
            tblMeta.Controls.Add(DlgVal($"{first.SupplierID}  —  {first.SupplierName}"), 1, 0);
            tblMeta.Controls.Add(DlgKey("Order Date"),  2, 0);
            tblMeta.Controls.Add(DlgVal(first.OrderDateStr), 3, 0);
            tblMeta.Controls.Add(DlgKey("Sub-Orders"),  4, 0);
            tblMeta.Controls.Add(DlgVal($"{orders.Count} item(s)"), 5, 0);
            tblMeta.Controls.Add(DlgKey("Grand Total"), 6, 0);
            tblMeta.Controls.Add(DlgVal($"HK$ {grandTotal:N2}"), 7, 0);
            pnlMeta.Controls.Add(tblMeta);

            // ── URGENCY / TRIGGER META ROW ───────────────────────────
            var pnlMeta2 = new Panel
            {
                Dock = DockStyle.Top, Height = 56,
                BackColor = Color.White, Padding = new Padding(28, 0, 28, 0)
            };
            pnlMeta2.Paint += DlgPaintBottomBorder;
            var tblMeta2 = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblMeta2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblMeta2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblMeta2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblMeta2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblMeta2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblMeta2.Controls.Add(DlgKey("Urgency Level"), 0, 0);
            tblMeta2.Controls.Add(DlgVal(first.UrgencyLevel ?? "—"), 1, 0);
            tblMeta2.Controls.Add(DlgKey("Trigger Type"),  2, 0);
            tblMeta2.Controls.Add(DlgVal(first.TriggerType  ?? "—"), 3, 0);
            pnlMeta2.Controls.Add(tblMeta2);

            // ── LINES SECTION LABEL ─────────────────────────────────
            var pnlLinesLabel = new Panel
            {
                Dock = DockStyle.Top, Height = 38,
                BackColor = Color.FromArgb(246, 249, 255), Padding = new Padding(28, 0, 0, 0)
            };
            pnlLinesLabel.Controls.Add(new Label
            {
                Text = $"ORDER LINES  ({lines.Count} item{(lines.Count == 1 ? "" : "s")})",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            });
            pnlLinesLabel.Paint += DlgPaintBottomBorder;

            // ── FOOTER ────────────────────────────────────────────────────
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom, Height = 68,
                BackColor = Color.White, Padding = new Padding(28, 10, 28, 10)
            };
            pnlFooter.Paint += DlgPaintTopBorder;
            var btnClose = new Button
            {
                Text = "Close", Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                BackColor = Color.White, ForeColor = Color.FromArgb(15, 31, 53),
                FlatStyle = FlatStyle.Flat, Width = 148, Height = 48,
                Dock = DockStyle.Right, Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnClose.FlatAppearance.BorderSize         = 1;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnClose.Click += (s, ev) => dlg.Close();
            pnlFooter.Controls.Add(btnClose);

            // ── ORDER LINES DataGridView (Fill) ──────────────────────
            var dgvLines = new DataGridView
            {
                Dock                  = DockStyle.Fill,
                ReadOnly              = true,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible     = false,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor       = Color.White,
                BorderStyle           = BorderStyle.None,
                GridColor             = Color.FromArgb(221, 227, 236),
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal,
                Font                  = new Font("Segoe UI", 11f),
                ColumnHeadersHeight   = 36,
                RowTemplate           = { Height = 44 },
                EnableHeadersVisualStyles = false
            };
            dgvLines.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 10f, FontStyle.Bold);
            dgvLines.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(246, 249, 255);
            dgvLines.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(98, 112, 135);
            dgvLines.ColumnHeadersDefaultCellStyle.Padding   = new Padding(12, 0, 0, 0);
            dgvLines.DefaultCellStyle.Padding                = new Padding(12, 6, 12, 6);
            dgvLines.DefaultCellStyle.SelectionBackColor     = Color.FromArgb(219, 234, 254);
            dgvLines.DefaultCellStyle.SelectionForeColor     = Color.FromArgb(15, 31, 53);
            dgvLines.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);

            // Sub-order sequence column ("-NN" shown as LINE #)
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "cLineNo", HeaderText = "LINE #", FillWeight = 7,
                DefaultCellStyle = {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(30, 64, 175)
                }
            });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "cPOID",    HeaderText = "PURCHASE ID",   FillWeight = 16 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "cPOLine",  HeaderText = "PO LINE ID",    FillWeight = 14 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "cMat",     HeaderText = "RAW MATERIAL",  FillWeight = 22 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "cType",    HeaderText = "TYPE",          FillWeight = 10 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "cWH",      HeaderText = "WAREHOUSE",     FillWeight = 16 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "cQty",     HeaderText = "ORDER QTY",     FillWeight =  9 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "cPrice",   HeaderText = "UNIT PRICE",    FillWeight = 12 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "cTotal",   HeaderText = "LINE TOTAL",    FillWeight = 12 });

            int lineSeq = 0;
            foreach (var ln in lines)
            {
                lineSeq++;
                // Extract -NN suffix from PurchaseID (last 3 chars, e.g. "-01")
                string lineNo = ln.PurchaseID?.Length >= 3
                    ? ln.PurchaseID.Substring(ln.PurchaseID.Length - 3)
                    : lineSeq.ToString("D2");

                dgvLines.Rows.Add(
                    lineNo,
                    ln.PurchaseID,
                    ln.POLineID,
                    ln.MaterialName,
                    ln.MaterialType,
                    ln.WarehouseLocation,
                    ln.OrderQty,
                    $"HK$ {ln.UnitPrice:N2}",
                    $"HK$ {ln.LineTotal:N2}");
            }

            // ── Assemble dialog (Bottom → Top → Fill order) ──────────────
            // Add Bottom panels first, then Top panels, then Fill last
            dlg.Controls.Add(dgvLines);      // Fill  — must be added before Top panels
            dlg.Controls.Add(pnlLinesLabel); // Top
            dlg.Controls.Add(pnlMeta2);      // Top
            dlg.Controls.Add(pnlMeta);       // Top
            dlg.Controls.Add(pnlHeader);     // Top
            dlg.Controls.Add(pnlFooter);     // Bottom

            dlg.ShowDialog(this);
        }

        // ── Dialog helper labels ──────────────────────────────────────
        private static Label DlgKey(string text) => new Label
        {
            Text      = text,
            Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(98, 112, 135),
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(0, 0, 8, 0)
        };

        private static Label DlgVal(string text) => new Label
        {
            Text         = text,
            Font         = new Font("Segoe UI", 12f),
            ForeColor    = Color.FromArgb(15, 31, 53),
            Dock         = DockStyle.Fill,
            TextAlign    = ContentAlignment.MiddleLeft,
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

        // ── RoundedRect helper ───────────────────────────────────────
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
