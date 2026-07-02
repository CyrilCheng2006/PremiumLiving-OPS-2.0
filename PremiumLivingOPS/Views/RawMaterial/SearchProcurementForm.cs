using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
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
        //  Grid  —  one row per BasePurchaseID
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
                int childCount = g.ChildPurchaseIDs?.Count ?? 0;
                string lineLabel = childCount > 1
                    ? $"{g.ItemCount} line(s) / {childCount} POs"
                    : $"{g.ItemCount} line(s)";

                int ri = dgvOrders.Rows.Add(
                    g.BasePurchaseID,           // col 0: Purchase ID (base)
                    g.SupplierName,             // col 1: Supplier
                    lineLabel,                  // col 2: Items
                    g.OrderDateStr,             // col 3: Order Date
                    $"HK$ {g.TotalAmount:N2}",  // col 4: Total Amount
                    g.PurchaseStatus,           // col 5: Status
                    g.UrgencyLevel);            // col 6: Urgency

                // Tag stores BasePurchaseID for the detail dialog
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
        //  Detail Dialog  (opens grouped view for BasePurchaseID)
        // ════════════════════════════════════════════════════════════════
        private void OpenDetailDialog()
        {
            if (dgvOrders.SelectedRows.Count == 0) return;

            string baseId = dgvOrders.SelectedRows[0].Tag?.ToString();
            if (string.IsNullOrEmpty(baseId)) return;

            GroupedProcurementDetailViewModel vm = null;
            try
            {
                vm = _ctrl.GetGroupedProcurementDetailVM(baseId);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to load details for {baseId}.\n\n{ex.Message}",
                    "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (vm == null || vm.Children.Count == 0)
            {
                MessageBox.Show(
                    $"No Purchase Order found for base ID: {baseId}",
                    "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ShowGroupedDetailDialog(vm);
        }

        // ── Grouped detail dialog ────────────────────────────────────────
        private void ShowGroupedDetailDialog(GroupedProcurementDetailViewModel vm)
        {
            StatusColors.TryGetValue(vm.PurchaseStatus ?? string.Empty, out var hsc);
            Color hBg = hsc.bg != default ? hsc.bg : Color.FromArgb(229, 231, 235);
            Color hFg = hsc.fg != default ? hsc.fg : Color.FromArgb(55, 65, 81);

            int totalLines = vm.Children.Sum(c => c.Lines.Count);

            using var dlg = new Form
            {
                Text            = $"Purchase Order Group — {vm.BasePurchaseID}",
                Size            = new Size(2300, 1200),
                MinimumSize     = new Size(1400, 860),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 13f),
                FormBorderStyle = FormBorderStyle.Sizable,
                MaximizeBox     = true, MinimizeBox = false
            };

            // ── HEADER ───────────────────────────────────────────────────
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            var tblHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(24, 0, 24, 0)
            };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblHeader.Controls.Add(new Label
            {
                Text      = $"Purchase Order Group  —  {vm.BasePurchaseID}  ({vm.Children.Count} sub-order{(vm.Children.Count == 1 ? "" : "s")})",
                Font      = new Font("Segoe UI", 17f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);
            tblHeader.Controls.Add(new Label
            {
                Text      = vm.PurchaseStatus ?? "—",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = hFg, BackColor = hBg,
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                AutoSize  = false, Padding = new Padding(8, 4, 8, 4)
            }, 1, 0);
            pnlHeader.Controls.Add(tblHeader);

            // ── SUMMARY META ROW ─────────────────────────────────────────
            var pnlMeta = BuildMetaRow(0,
                ("Supplier",     vm.SupplierDisplay ?? "—"),
                ("Order Date",   vm.OrderDateStr    ?? "—"),
                ("PO Total",     $"HK$ {vm.TotalAmount:N2}"),
                ("Total Lines",  $"{totalLines} line{(totalLines == 1 ? "" : "s")}"));

            // ── FOOTER ───────────────────────────────────────────────────
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom, Height = 68,
                BackColor = Color.White, Padding = new Padding(28, 10, 28, 10)
            };
            pnlFooter.Paint += DlgPaintTopBorder;
            var btnClose = new Button
            {
                Text      = "Close",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                BackColor = Color.White, ForeColor = Color.FromArgb(15, 31, 53),
                FlatStyle = FlatStyle.Flat, Width = 148, Height = 48,
                Dock      = DockStyle.Right, Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnClose.FlatAppearance.BorderSize         = 1;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnClose.Click += (s, ev) => dlg.Close();
            pnlFooter.Controls.Add(btnClose);

            // ── SCROLLABLE CONTENT AREA (one section per child PO) ───────
            var scroll = new Panel
            {
                Dock        = DockStyle.Fill,
                AutoScroll  = true,
                BackColor   = Color.FromArgb(246, 249, 255),
                Padding     = new Padding(20, 16, 20, 16)
            };

            // Build child sections bottom-up (Controls.Add prepends top)
            var childControls = new List<Control>();

            foreach (var child in vm.Children)
            {
                var section = BuildChildSection(child);
                childControls.Add(section);
            }

            // Add in reverse so first child appears at top
            for (int i = childControls.Count - 1; i >= 0; i--)
                scroll.Controls.Add(childControls[i]);

            // Assemble dialog (Bottom first, then Top panels, then Fill)
            dlg.Controls.Add(scroll);
            dlg.Controls.Add(pnlMeta);
            dlg.Controls.Add(pnlHeader);
            dlg.Controls.Add(pnlFooter);

            dlg.ShowDialog(this);
        }

        /// <summary>Builds one card section for a single child PO inside the detail dialog.</summary>
        private Panel BuildChildSection(ProcurementChildGroup child)
        {
            StatusColors.TryGetValue(child.PurchaseStatus ?? string.Empty, out var sc);
            Color sBg = sc.bg != default ? sc.bg : Color.FromArgb(229, 231, 235);
            Color sFg = sc.fg != default ? sc.fg : Color.FromArgb(55, 65, 81);

            UrgencyColors.TryGetValue(child.UrgencyLevel ?? string.Empty, out var uc);

            // ── Card wrapper ────────────────────────────────────────────
            var card = new Panel
            {
                Dock      = DockStyle.Top,
                BackColor = Color.White,
                Padding   = new Padding(0),
                Margin    = new Padding(0, 0, 0, 14)
            };

            // Sub-header bar for this child PO
            var subHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 44,
                BackColor = Color.FromArgb(236, 242, 255),
                Padding   = new Padding(20, 0, 16, 0)
            };
            var subTlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            subTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            subTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200f));
            subTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160f));
            subTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // Child PO ID label
            subTlp.Controls.Add(new Label
            {
                Text      = child.PurchaseID,
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(19, 35, 61),
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false
            }, 0, 0);

            // Urgency badge
            if (!string.IsNullOrEmpty(child.UrgencyLevel) && uc.bg != default)
                subTlp.Controls.Add(new Label
                {
                    Text      = child.UrgencyLevel,
                    Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                    ForeColor = uc.fg, BackColor = uc.bg,
                    Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize  = false
                }, 1, 0);
            else
                subTlp.Controls.Add(new Label(), 1, 0);

            // Status badge
            subTlp.Controls.Add(new Label
            {
                Text      = child.PurchaseStatus ?? "—",
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = sFg, BackColor = sBg,
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                AutoSize  = false
            }, 2, 0);

            subHeader.Controls.Add(subTlp);

            // Request ID + SubTotal meta row
            var metaRow = BuildMetaRow(0,
                ("Request ID", string.IsNullOrEmpty(child.RequestID) ? "—" : child.RequestID),
                ("Sub-Total",  $"HK$ {child.SubTotal:N2}"),
                ("Trigger",    string.IsNullOrEmpty(child.TriggerType) ? "—" : child.TriggerType));

            // ── Lines DGV ───────────────────────────────────────────────
            Control linesContent;
            if (child.Lines.Count > 0)
            {
                var dgv = BuildLinesDgv(child.Lines);
                linesContent = dgv;
            }
            else
            {
                var emptyLbl = new Label
                {
                    Text      = "No order line items for this sub-order.",
                    Font      = new Font("Segoe UI", 12f),
                    ForeColor = Color.FromArgb(156, 163, 175),
                    Height    = 52, Dock = DockStyle.Top,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                linesContent = emptyLbl;
            }

            // ── Lines label ─────────────────────────────────────────────
            var linesLabel = new Panel
            {
                Dock = DockStyle.Top, Height = 34,
                BackColor = Color.FromArgb(246, 249, 255),
                Padding   = new Padding(20, 0, 0, 0)
            };
            linesLabel.Controls.Add(new Label
            {
                Text      = $"ORDER LINES  ({child.Lines.Count} item{(child.Lines.Count == 1 ? "" : "s")})",
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            });
            linesLabel.Paint += DlgPaintBottomBorder;

            // Assemble card (inner DockStyle.Top panels stack top-down when added bottom-first)
            card.Controls.Add(linesContent);
            card.Controls.Add(linesLabel);
            card.Controls.Add(metaRow);
            card.Controls.Add(subHeader);

            // Compute card height for DockStyle.Top in AutoScroll parent
            int dgvH = child.Lines.Count > 0
                ? 36 + (child.Lines.Count * 44) + 4   // header + rows + border
                : 52;
            card.Height = 44 + metaRow.Height + 34 + dgvH + 12;

            return card;
        }

        /// <summary>Creates a pre-styled DataGridView for PurchaseOrderLines.</summary>
        private static DataGridView BuildLinesDgv(List<PurchaseOrderLineEntity> lines)
        {
            var dgv = new DataGridView
            {
                Dock                  = DockStyle.Top,
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
            dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 10f, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(246, 249, 255);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(98, 112, 135);
            dgv.ColumnHeadersDefaultCellStyle.Padding   = new Padding(12, 0, 0, 0);
            dgv.DefaultCellStyle.Padding                = new Padding(12, 6, 12, 6);
            dgv.DefaultCellStyle.SelectionBackColor     = Color.FromArgb(219, 234, 254);
            dgv.DefaultCellStyle.SelectionForeColor     = Color.FromArgb(15, 31, 53);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cNo",    HeaderText = "#",            FillWeight =  5 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cLine",  HeaderText = "PO LINE ID",   FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cMat",   HeaderText = "RAW MATERIAL", FillWeight = 22 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cType",  HeaderText = "TYPE",         FillWeight = 10 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cWH",    HeaderText = "WAREHOUSE",    FillWeight = 18 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cQty",   HeaderText = "ORDER QTY",   FillWeight =  9 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cPrice", HeaderText = "UNIT PRICE",  FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cTotal", HeaderText = "LINE TOTAL",  FillWeight = 12 });

            foreach (DataGridViewColumn col in dgv.Columns)
                if (col.Name == "cNo" || col.Name == "cQty" || col.Name == "cPrice" || col.Name == "cTotal")
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            int seq = 0;
            foreach (var ln in lines)
            {
                seq++;
                dgv.Rows.Add(
                    seq.ToString(),
                    ln.POLineID,
                    ln.MaterialName,
                    string.IsNullOrEmpty(ln.MaterialType) ? "—" : ln.MaterialType,
                    string.IsNullOrEmpty(ln.WarehouseLocation) ? ln.WarehouseID : ln.WarehouseLocation,
                    ln.OrderQty,
                    $"HK$ {ln.UnitPrice:N2}",
                    $"HK$ {ln.LineTotal:N2}");
            }

            dgv.Height = 36 + (lines.Count * 44) + 4;
            return dgv;
        }

        // ── Helper: build a 2-column (Key | Value) meta row panel ─────
        private Panel BuildMetaRow(int height, params (string key, string val)[] pairs)
        {
            const int ROW_H     = 56;
            const int KEY_W_PCT = 18;
            const int SIDE_PAD  = 28;

            int chunks = (int)Math.Ceiling(pairs.Length / 2.0);
            int totalH = chunks * ROW_H;

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
                int  idxA    = ci * 2;
                int  idxB    = idxA + 1;
                bool hasPairB = idxB < pairs.Length;

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
