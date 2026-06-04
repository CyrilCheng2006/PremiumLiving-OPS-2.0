using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;   // SearchProcurementViewModel, ProcurementDetailViewModel
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
    /// MVC role : View only. All data access goes through ProcurementController.
    /// AppShell  : mandatory chrome (TopNavBar + UserBar).
    /// CardPanel : all content wrapped in 3-layer nested cards.
    ///
    /// Schema coverage:
    ///   PurchaseOrder     — primary list
    ///   Supplier          — joined for display
    ///   MaterialRequest   — joined for display
    ///   RawMaterial / Item — joined for material name
    ///   PurchaseOrderLine — shown in detail dialog
    ///   Warehouse         — shown in detail dialog
    /// </summary>
    public partial class SearchProcurementForm : Form
    {
        private readonly ProcurementController  _ctrl          = new ProcurementController();
        private List<ProcurementOrderEntity>    _currentOrders = new List<ProcurementOrderEntity>();

        // ── Status colour map ────────────────────────────────────────────
        private static readonly Dictionary<string, (Color bg, Color fg)> StatusColors =
            new Dictionary<string, (Color, Color)>
            {
                { "Sent",               (Color.FromArgb(219, 234, 254), Color.FromArgb( 30,  64, 175)) },
                { "Cancelled",          (Color.FromArgb(254, 226, 226), Color.FromArgb(153,  27,  27)) },
                { "Partially Received", (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "Received",           (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
                { "Completed",          (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) }
            };

        // ── Urgency colour map ───────────────────────────────────────────
        private static readonly Dictionary<string, (Color bg, Color fg)> UrgencyColors =
            new Dictionary<string, (Color, Color)>
            {
                { "Critical", (Color.FromArgb(254, 226, 226), Color.FromArgb(153,  27,  27)) },
                { "High",     (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "Medium",   (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) }
            };

        private const int D_RowH   = 60;
        private const int D_LabelW = 260;
        private const int D_BtnW   = 200;
        private const int D_BtnH   = 56;

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

            _currentOrders = vm.Orders;

            dgvOrders.Rows.Clear();
            foreach (var o in _currentOrders)
            {
                dgvOrders.Rows.Add(
                    o.PurchaseID,
                    o.SupplierName,
                    o.RawMaterialName,
                    o.RequestedQty,
                    o.OrderDateStr,
                    $"HK$ {o.POTotalAmount:N2}",
                    o.PurchaseStatus,
                    o.UrgencyLevel);
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

            var allOrders = _ctrl.GetSearchProcurementVM().Orders;

            int total      = allOrders.Count;
            int sent       = allOrders.FindAll(o => o.PurchaseStatus == "Sent").Count;
            int inProgress = allOrders.FindAll(o => o.PurchaseStatus == "Partially Received").Count;
            int completed  = allOrders.FindAll(o => o.PurchaseStatus == "Completed" || o.PurchaseStatus == "Received").Count;

            var pills = new[]
            {
                ("Total",              total.ToString(),      Color.FromArgb( 47, 111, 237), Color.FromArgb(219, 234, 254)),
                ("Sent",               sent.ToString(),       Color.FromArgb( 30,  64, 175), Color.FromArgb(219, 234, 254)),
                ("Partial / Received", inProgress.ToString(), Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199)),
                ("Completed",          completed.ToString(),  Color.FromArgb(  6,  95,  70), Color.FromArgb(209, 250, 229)),
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
                    ColumnCount     = 2,
                    RowCount        = 1,
                    BackColor       = Color.Transparent,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding         = new Padding(10, 0, 8, 0)
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
            btnViewDetail.Enabled = dgvOrders.SelectedRows.Count > 0;
        }

        private void DgvOrders_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.Value == null) return;
            string val     = e.Value.ToString();
            string colName = dgvOrders.Columns[e.ColumnIndex].Name;

            if (colName == "colStatus" && StatusColors.TryGetValue(val, out var sc))
            {
                e.CellStyle.ForeColor = sc.fg; e.CellStyle.BackColor = sc.bg;
                e.CellStyle.SelectionForeColor = sc.fg; e.CellStyle.SelectionBackColor = sc.bg;
                e.CellStyle.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                e.FormattingApplied = true;
            }
            else if (colName == "colUrgency" && UrgencyColors.TryGetValue(val, out var uc))
            {
                e.CellStyle.ForeColor = uc.fg; e.CellStyle.BackColor = uc.bg;
                e.CellStyle.SelectionForeColor = uc.fg; e.CellStyle.SelectionBackColor = uc.bg;
                e.CellStyle.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                e.FormattingApplied = true;
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  Action handlers
        // ════════════════════════════════════════════════════════════════

        private void BtnCreateNew_Click(object sender, EventArgs e)
            => FormNavigator.NavigateTo(this, "Raw Material", "Create Procurement");

        // ════════════════════════════════════════════════════════════════
        //  Detail Dialog
        // ════════════════════════════════════════════════════════════════

        private void OpenDetailDialog()
        {
            if (dgvOrders.SelectedRows.Count == 0) return;

            string purchaseId = dgvOrders.SelectedRows[0]
                .Cells["colPurchaseID"].Value?.ToString();

            var vm = _ctrl.GetProcurementDetailVM(purchaseId);
            if (vm?.Order == null) return;

            var o     = vm.Order;
            var lines = vm.Lines ?? new List<PurchaseOrderLineEntity>();

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
                    ForeColor = Color.FromArgb(70, 85, 110),
                    BackColor = Color.FromArgb(248, 250, 252),
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

            // ── CARD 1 – Purchase Order Header ───────────────────────────
            var c1Rows = new Panel[]
            {
                FieldRow("Purchase ID", ReadLabel(o.PurchaseID)),
                FieldRow("Order Date",  ReadLabel(o.OrderDateStr)),
                FieldRow("Status",      ReadLabel(o.PurchaseStatus)),
                FieldRow("PO Total",    ReadLabel($"HK$ {o.POTotalAmount:N2}"), lastRow: true)
            };
            var (c1Outer, c1Inner) = CardPanel.Create(
                outerHeight: c1Rows.Length * D_RowH + 22,
                outerPadding: new Padding(20, 14, 20, 8));
            c1Inner.Padding = new Padding(0);
            c1Inner.Controls.Add(BuildStack(c1Rows));

            // ── CARD 2 – Supplier & Request ──────────────────────────────
            var c2Rows = new Panel[]
            {
                FieldRow("Supplier ID",   ReadLabel(o.SupplierID)),
                FieldRow("Supplier Name", ReadLabel(o.SupplierName)),
                FieldRow("Request ID",    ReadLabel(o.RequestID)),
                FieldRow("Raw Material",  ReadLabel($"{o.RawMaterialItemID}  \u2014  {o.RawMaterialName}")),
                FieldRow("Requested Qty", ReadLabel(o.RequestedQty.ToString())),
                FieldRow("Trigger Type",  ReadLabel(o.TriggerType)),
                FieldRow("Urgency Level", ReadLabel(o.UrgencyLevel), lastRow: true)
            };
            var (c2Outer, c2Inner) = CardPanel.Create(
                outerHeight: c2Rows.Length * D_RowH + 30,
                outerPadding: new Padding(20, 8, 20, 8));
            c2Inner.Padding = new Padding(0);
            c2Inner.Controls.Add(BuildStack(c2Rows));

            // ── CARD 3 – Order Lines Grid ────────────────────────────────
            Panel c3Outer = null;
            if (lines.Count > 0)
            {
                var lineDgv = new DataGridView
                {
                    Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false, RowHeadersVisible = false,
                    BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                    GridColor = Color.FromArgb(221, 227, 236),
                    Font = new Font("Segoe UI", 12f),
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                    EnableHeadersVisualStyles = false, ColumnHeadersHeight = 42,
                    ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                    {
                        BackColor = Color.FromArgb(248, 250, 252),
                        ForeColor = Color.FromArgb(70, 85, 110),
                        Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                        Padding   = new Padding(10, 0, 0, 0)
                    },
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        BackColor          = Color.White,
                        ForeColor          = Color.FromArgb(15, 31, 53),
                        SelectionBackColor = Color.FromArgb(219, 234, 254),
                        SelectionForeColor = Color.FromArgb(15, 31, 53),
                        Padding            = new Padding(10, 4, 10, 4)
                    }
                };
                lineDgv.RowTemplate.Height = 44;
                lineDgv.Columns.Add("cPOLine", "PO LINE ID");
                lineDgv.Columns.Add("cMat",    "RAW MATERIAL");
                lineDgv.Columns.Add("cType",   "TYPE");
                lineDgv.Columns.Add("cWH",     "WAREHOUSE");
                lineDgv.Columns.Add("cQty",    "ORDER QTY");
                lineDgv.Columns.Add("cPrice",  "UNIT PRICE");
                lineDgv.Columns.Add("cTotal",  "LINE TOTAL");

                foreach (var ln in lines)
                    lineDgv.Rows.Add(
                        ln.POLineID, ln.MaterialName, ln.MaterialType,
                        ln.WarehouseLocation, ln.OrderQty,
                        $"HK$ {ln.UnitPrice:N2}", $"HK$ {ln.LineTotal:N2}");

                const int LineSecH = 46, LineHdrH = 42, LineRowH = 44;
                int c3H = LineSecH + LineHdrH + lines.Count * LineRowH + 16 + 22;

                var lineHeader = new Panel
                {
                    Dock = DockStyle.Top, Height = LineSecH,
                    BackColor = Color.White, Padding = new Padding(20, 0, 20, 0)
                };
                lineHeader.Controls.Add(new Label
                {
                    Text = "Order Lines", Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(47, 111, 237),
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
                });
                lineHeader.Paint += (s, pe) =>
                {
                    using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                    pe.Graphics.DrawLine(pen, 20, ((Panel)s).Height - 1, ((Panel)s).Width - 20, ((Panel)s).Height - 1);
                };

                var (c3o, c3i) = CardPanel.Create(
                    outerHeight: c3H, outerPadding: new Padding(20, 8, 20, 16));
                c3i.Padding = new Padding(0);
                c3i.Controls.Add(lineDgv);
                c3i.Controls.Add(lineHeader);
                c3Outer = c3o;
            }

            // ── Dialog shell ─────────────────────────────────────────────
            using var dlg = new Form
            {
                Text            = $"View Purchase Order  \u2014  {o.PurchaseID}",
                Size            = new Size(1400, 900),
                MinimumSize     = new Size(1100, 700),
                StartPosition   = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false, MinimizeBox = false,
                BackColor       = Color.FromArgb(240, 244, 249),
                Font            = new Font("Segoe UI", 12f)
            };

            Color pillBg = Color.FromArgb(229, 231, 235);
            Color pillFg = Color.FromArgb(55, 65, 81);
            if (StatusColors.TryGetValue(o.PurchaseStatus ?? "", out var hsc))
            { pillBg = hsc.bg; pillFg = hsc.fg; }

            var statusFont = new Font("Segoe UI", 13f, FontStyle.Bold);
            int textW      = TextRenderer.MeasureText(o.PurchaseStatus ?? "\u2014", statusFont).Width;
            int statusColW = textW + 80;

            var statusLbl = new Label
            {
                Text = o.PurchaseStatus ?? "\u2014",
                Font = statusFont, ForeColor = pillFg, BackColor = pillBg,
                Dock = DockStyle.Fill, AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter
            };
            statusLbl.Paint += (s, pe) =>
            {
                var lb = (Label)s;
                using var pen = new System.Drawing.Pen(Color.FromArgb(120, pillFg.R, pillFg.G, pillFg.B), 1);
                pe.Graphics.DrawRectangle(pen, 0, 0, lb.Width - 1, lb.Height - 1);
            };

            var headerTlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            headerTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            headerTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, statusColW));
            headerTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            headerTlp.Controls.Add(new Label
            {
                Text = $"Purchase Order  \u2014  {o.PurchaseID}",
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
                FlatStyle = FlatStyle.Flat, Width = D_BtnW, Height = D_BtnH, Cursor = Cursors.Hand
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
            if (c3Outer != null) scroll.Controls.Add(c3Outer);
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
