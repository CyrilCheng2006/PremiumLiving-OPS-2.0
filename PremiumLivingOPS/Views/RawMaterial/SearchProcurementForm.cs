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
    public partial class SearchProcurementForm : Form
    {
        private readonly ProcurementController  _ctrl          = new ProcurementController();
        private List<ProcurementOrderGroup>     _currentGroups = new List<ProcurementOrderGroup>();

        private static readonly Font _fontBadge = new Font("Segoe UI", 11f, FontStyle.Bold);

        private static readonly Dictionary<string, (Color bg, Color fg)> StatusColors =
            new Dictionary<string, (Color, Color)>
            {
                { "Sent",               (Color.FromArgb(219, 234, 254), Color.FromArgb( 30,  64, 175)) },
                { "Cancelled",          (Color.FromArgb(254, 226, 226), Color.FromArgb(153,  27,  27)) },
                { "Partially Received", (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "Received",           (Color.FromArgb(243, 232, 255), Color.FromArgb( 88,  28, 135)) },
                { "Completed",          (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) }
            };

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
        //  Grid
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
            _shell.SetBreadcrumb("Raw Material  ›  Search Procurement");

            _currentGroups = vm.Groups ?? new List<ProcurementOrderGroup>();

            dgvOrders.Rows.Clear();
            foreach (var g in _currentGroups)
            {
                int ri = dgvOrders.Rows.Add(
                    g.PurchaseID,
                    g.SupplierName,
                    $"{g.ItemCount} line(s)",
                    g.OrderDateStr,
                    $"HK$ {g.TotalAmount:N2}",
                    g.PurchaseStatus,
                    g.UrgencyLevel);

                dgvOrders.Rows[ri].Tag = g.PurchaseID;
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

            string purchaseId = dgvOrders.SelectedRows[0].Tag?.ToString();
            if (string.IsNullOrEmpty(purchaseId)) return;

            ProcurementDetailViewModel vm = null;
            try
            {
                vm = _ctrl.GetProcurementDetailVM(purchaseId);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to load details for {purchaseId}.\n\n{ex.Message}",
                    "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (vm?.Order == null)
            {
                MessageBox.Show(
                    $"No Purchase Order found for: {purchaseId}",
                    "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ShowProcurementDetailDialog(vm);
        }

        // ── Helper: build a 2-column (Key | Value) meta row panel ─────
        private Panel BuildMetaRow(int height, params (string key, string val)[] pairs)
        {
            // Each row shows exactly 2 key-value pairs side by side.
            // pairs are split into chunks of 2; each chunk becomes one horizontal row.
            // All chunks are stacked vertically inside the returned panel.

            const int ROW_H     = 56;   // height per chunk row
            const int KEY_W_PCT = 18;   // key column % inside each pair half
            const int SIDE_PAD  = 28;

            int chunks   = (int)Math.Ceiling(pairs.Length / 2.0);
            int totalH   = chunks * ROW_H;

            var outer = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = totalH,
                BackColor = Color.White,
                Padding   = new Padding(SIDE_PAD, 0, SIDE_PAD, 0)
            };
            outer.Paint += DlgPaintBottomBorder;

            var stack = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 1,
                RowCount        = chunks,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            for (int r = 0; r < chunks; r++)
                stack.RowStyles.Add(new RowStyle(SizeType.Absolute, ROW_H));
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            for (int ci = 0; ci < chunks; ci++)
            {
                // up to 2 pairs in this chunk
                int idxA = ci * 2;
                int idxB = idxA + 1;
                bool hasPairB = idxB < pairs.Length;

                // inner 4-column TLP: keyA | valA | keyB | valB
                var row = new TableLayoutPanel
                {
                    Dock            = DockStyle.Fill,
                    ColumnCount     = hasPairB ? 4 : 2,
                    RowCount        = 1,
                    BackColor       = Color.Transparent,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None
                };
                row.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                if (hasPairB)
                {
                    row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, KEY_W_PCT));
                    row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f - KEY_W_PCT));
                    row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, KEY_W_PCT));
                    row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f - KEY_W_PCT));
                    row.Controls.Add(DlgKey(pairs[idxA].key), 0, 0);
                    row.Controls.Add(DlgVal(pairs[idxA].val), 1, 0);
                    row.Controls.Add(DlgKey(pairs[idxB].key), 2, 0);
                    row.Controls.Add(DlgVal(pairs[idxB].val), 3, 0);
                }
                else
                {
                    row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, KEY_W_PCT * 2));
                    row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f - KEY_W_PCT * 2));
                    row.Controls.Add(DlgKey(pairs[idxA].key), 0, 0);
                    row.Controls.Add(DlgVal(pairs[idxA].val), 1, 0);
                }

                stack.Controls.Add(row, 0, ci);
            }

            outer.Controls.Add(stack);
            return outer;
        }

        private void ShowProcurementDetailDialog(ProcurementDetailViewModel vm)
        {
            var order = vm.Order;
            var lines = vm.Lines ?? new List<PurchaseOrderLineEntity>();

            StatusColors.TryGetValue(order.PurchaseStatus ?? string.Empty, out var hsc);
            Color hBg = hsc.bg != default ? hsc.bg : Color.FromArgb(229, 231, 235);
            Color hFg = hsc.fg != default ? hsc.fg : Color.FromArgb(55, 65, 81);

            using var dlg = new Form
            {
                Text            = $"Purchase Order Detail — {order.PurchaseID}",
                Size            = new Size(2300, 1100),
                MinimumSize     = new Size(1400, 800),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 13f),
                FormBorderStyle = FormBorderStyle.Sizable,
                MaximizeBox     = true, MinimizeBox = false
            };

            // ── HEADER ──────────────────────────────────────────────────
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
                Text = $"Purchase Order Details  —  {order.PurchaseID}",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);
            tblHeader.Controls.Add(new Label
            {
                Text = order.PurchaseStatus ?? "—",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = hFg, BackColor = hBg,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false, Padding = new Padding(8, 4, 8, 4)
            }, 1, 0);
            pnlHeader.Controls.Add(tblHeader);

            // ── META: 2 pairs per line ───────────────────────────────────
            // Row A: Supplier | Order Date
            // Row B: Request ID | PO Total
            // Row C: Urgency | Trigger
            // Row D: Material (MRQ) — single pair
            string supplierDisplay =
                string.IsNullOrEmpty(order.SupplierID)
                    ? (order.SupplierName ?? "—")
                    : $"{order.SupplierID}  —  {order.SupplierName}";

            string materialDisplay =
                string.IsNullOrEmpty(order.RawMaterialName)
                    ? "—"
                    : $"{order.RawMaterialName}  ({order.RawMaterialItemID})";

            var pnlMeta = BuildMetaRow(0,
                ("Supplier",        supplierDisplay),
                ("Order Date",      order.OrderDateStr ?? "—"),
                ("Request ID",      string.IsNullOrEmpty(order.RequestID) ? "—" : order.RequestID),
                ("PO Total",        $"HK$ {order.POTotalAmount:N2}"),
                ("Urgency",         string.IsNullOrEmpty(order.UrgencyLevel) ? "—" : order.UrgencyLevel),
                ("Trigger",         string.IsNullOrEmpty(order.TriggerType)  ? "—" : order.TriggerType),
                ("Material (MRQ)",  materialDisplay)
            );

            // ── ORDER LINES LABEL ───────────────────────────────────
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

            // ── FOOTER ───────────────────────────────────────────────
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

            // ── ORDER LINES DGV or empty state ───────────────────────
            Control fillContent;
            if (lines.Count > 0)
            {
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

                dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "cNo",    HeaderText = "#",            FillWeight =  5 });
                dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "cLine",  HeaderText = "PO LINE ID",   FillWeight = 16 });
                dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "cMat",   HeaderText = "RAW MATERIAL", FillWeight = 22 });
                dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "cType",  HeaderText = "TYPE",         FillWeight = 10 });
                dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "cWH",    HeaderText = "WAREHOUSE",    FillWeight = 18 });
                dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "cQty",   HeaderText = "ORDER QTY",   FillWeight =  9 });
                dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "cPrice", HeaderText = "UNIT PRICE",  FillWeight = 12 });
                dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "cTotal", HeaderText = "LINE TOTAL",  FillWeight = 12 });

                foreach (DataGridViewColumn col in dgvLines.Columns)
                    if (col.Name == "cNo" || col.Name == "cQty" || col.Name == "cPrice" || col.Name == "cTotal")
                        col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

                int seq = 0;
                foreach (var ln in lines)
                {
                    seq++;
                    dgvLines.Rows.Add(
                        seq.ToString(),
                        ln.POLineID,
                        ln.MaterialName,
                        string.IsNullOrEmpty(ln.MaterialType) ? "—" : ln.MaterialType,
                        string.IsNullOrEmpty(ln.WarehouseLocation) ? ln.WarehouseID : ln.WarehouseLocation,
                        ln.OrderQty,
                        $"HK$ {ln.UnitPrice:N2}",
                        $"HK$ {ln.LineTotal:N2}");
                }
                fillContent = dgvLines;
            }
            else
            {
                var pnlEmpty = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
                pnlEmpty.Controls.Add(new Label
                {
                    Text      = "No order line items found for this Purchase Order.",
                    Font      = new Font("Segoe UI", 13f),
                    ForeColor = Color.FromArgb(156, 163, 175),
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                });
                fillContent = pnlEmpty;
            }

            // Assemble (Bottom first, then Top panels, then Fill)
            dlg.Controls.Add(fillContent);
            dlg.Controls.Add(pnlLinesLabel);
            dlg.Controls.Add(pnlMeta);
            dlg.Controls.Add(pnlHeader);
            dlg.Controls.Add(pnlFooter);

            dlg.ShowDialog(this);
        }

        // ── Dialog helpers ─────────────────────────────────────────
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
