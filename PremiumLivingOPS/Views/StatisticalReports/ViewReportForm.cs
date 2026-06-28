using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.StatisticalReports
{
    /// <summary>
    /// View — Statistical Reports › View Report
    ///
    /// Rendering baseline: HandlingGoodsReceivedForm (Logistics Processing)
    ///   - Tab Bar:    pnlTabOuter  Height=69,  Padding=(20,4,20,0)
    ///   - Filter Bar: pnlFilterOuter Height=200, Padding=(20,14,20,8)
    ///     3-row tblCard: row0=50(title), row1=60(fields single-row), row2=65(buttons)
    ///
    /// Changes (2026-06-29):
    ///   - KPI bar removed entirely
    ///   - Date filter collapsed to single horizontal row:
    ///       [chk] [dtpFrom] ["to" label] [dtpTo]
    ///   - All buttons resized to 210×60
    ///
    /// Tab index map:
    ///   0 = Sales Performance      3 = Logistics Overview
    ///   1 = Inventory Status       4 = After-Service Summary
    ///   2 = Procurement Summary    5 = Finance Overview
    /// </summary>
    public partial class ViewReportForm : Form
    {
        private readonly StatisticalReportsController _ctrl = new StatisticalReportsController();
        private int    _activeTab         = -1;
        private bool   _salesChart        = false;
        private bool   _inventoryChart    = false;
        private bool   _procurementChart  = false;
        private bool   _logisticsChart    = false;
        private bool   _afterServiceChart = false;
        private bool   _financeChart      = false;
        private Button[] _tabButtons;

        private static readonly Dictionary<string, (Color bg, Color fg)> StatusColors =
            new Dictionary<string, (Color, Color)>
            {
                { "Pending",             (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "Processing",          (Color.FromArgb(219, 234, 254), Color.FromArgb( 29,  78, 216)) },
                { "Delivered",           (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
                { "Partially Delivered", (Color.FromArgb(237, 233, 254), Color.FromArgb( 91,  33, 182)) },
                { "Cancelled",           (Color.FromArgb(254, 226, 226), Color.FromArgb(185,  28,  28)) },
                { "Completed",           (Color.FromArgb(220, 252, 231), Color.FromArgb( 22, 101,  52)) },
                { "In Transit",          (Color.FromArgb(219, 234, 254), Color.FromArgb( 29,  78, 216)) },
                { "Sent",                (Color.FromArgb(219, 234, 254), Color.FromArgb( 29,  78, 216)) },
                { "Partially Received",  (Color.FromArgb(237, 233, 254), Color.FromArgb( 91,  33, 182)) },
                { "Received",            (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
                { "Escalated",           (Color.FromArgb(254, 226, 226), Color.FromArgb(185,  28,  28)) },
                { "Approved",            (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
                { "Rejected",            (Color.FromArgb(254, 226, 226), Color.FromArgb(185,  28,  28)) },
                { "Revenue",             (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
                { "Expense",             (Color.FromArgb(254, 226, 226), Color.FromArgb(185,  28,  28)) },
                { "Refund",              (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
            };

        public ViewReportForm()
        {
            InitializeComponent();
            _tabButtons = new Button[] { btnTab0, btnTab1, btnTab2, btnTab3, btnTab4, btnTab5 };
            this.Load += (s, e) => SwitchToReport(0);
        }

        // ════════════════════════════════════════════════════════════════
        //  REPORT SWITCHER
        // ════════════════════════════════════════════════════════════════

        private void SwitchToReport(int tabIndex)
        {
            if (_activeTab == tabIndex && pnlContent.Controls.Count > 0) return;
            _activeTab = tabIndex;

            pnlContent.SuspendLayout();
            pnlContent.Controls.Clear();
            pnlFilterOuter.Controls.Clear();

            HighlightTab(tabIndex);
            pnlTabOuter.Invalidate();

            switch (tabIndex)
            {
                case 0: RenderSales();        break;
                case 1: RenderInventory();    break;
                case 2: RenderProcurement();  break;
                case 3: RenderLogistics();    break;
                case 4: RenderAfterService(); break;
                case 5: RenderFinance();      break;
            }

            pnlContent.ResumeLayout(true);
        }

        // ════════════════════════════════════════════════════════════════
        //  TAB HIGHLIGHT + UNDERLINE
        // ════════════════════════════════════════════════════════════════

        private void HighlightTab(int activeIndex)
        {
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                bool active = i == activeIndex;
                _tabButtons[i].ForeColor = active ? Palette.Primary : Color.FromArgb(98, 112, 135);
                _tabButtons[i].Font      = active
                    ? new Font("Segoe UI", 12f, FontStyle.Bold)
                    : new Font("Segoe UI", 12f, FontStyle.Regular);
                _tabButtons[i].BackColor = Color.White;
            }
        }

        private void PaintTabUnderline(object sender, PaintEventArgs e)
        {
            if (_activeTab < 0 || _activeTab >= _tabButtons.Length) return;
            var btn = _tabButtons[_activeTab];
            int padL = pnlTabOuter.Padding.Left;
            int x    = padL + btn.Bounds.X + 24;
            int w    = Math.Max(0, btn.Bounds.Width - 48);
            int y    = pnlTabOuter.Height - 4;
            using var brush = new SolidBrush(Palette.Primary);
            e.Graphics.FillRectangle(brush, x, y, w, 4);
        }

        // ════════════════════════════════════════════════════════════════
        //  FILTER BAR BUILDER
        //  3-row tblCard inside white CardPanel inside pnlFilterOuter:
        //    Row 0 — title + divider  (50 px, Absolute)
        //    Row 1 — filter fields   ( 60 px, Absolute)  ← single input row
        //    Row 2 — action buttons  ( 65 px, Absolute)
        //  tblCard Padding=(18,10,18,10)
        // ════════════════════════════════════════════════════════════════

        private void SetFilterBar(string titleText, Panel fieldRow, Panel btnRow)
        {
            var tbl = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                RowCount        = 3,
                ColumnCount     = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(18, 10, 18, 10)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute,  50f));  // title
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute,  60f));  // fields (single row)
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute,  65f));  // buttons

            // Row 0 — title + bottom divider
            var pnlTitle = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlTitle.Controls.Add(new Label
            {
                Text      = titleText,
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            });
            pnlTitle.Controls.Add(new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 1,
                BackColor = Color.FromArgb(221, 227, 236)
            });

            fieldRow.Dock = DockStyle.Fill;
            btnRow.Dock   = DockStyle.Fill;

            tbl.Controls.Add(pnlTitle, 0, 0);
            tbl.Controls.Add(fieldRow, 0, 1);
            tbl.Controls.Add(btnRow,   0, 2);

            var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            card.Paint += PaintCardBorder;
            card.Controls.Add(tbl);

            pnlFilterOuter.Controls.Add(card);
        }

        // ────────────────────────────────────────────────────────────────
        //  BuildFieldsRow  — single horizontal row of labelled controls.
        //  Each column: optional caption label (22 px) + control (fill).
        //  For date range filters use BuildDateRangeRow instead.
        // ────────────────────────────────────────────────────────────────
        private static Panel BuildFieldsRow(params (string caption, Control ctrl)[] cols)
        {
            int n = Math.Max(1, cols.Length);
            var tbl = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = n,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(0, 4, 0, 4)
            };
            for (int i = 0; i < n; i++)
                tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / n));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            for (int i = 0; i < cols.Length; i++)
            {
                var (caption, ctrl) = cols[i];
                bool last = i == cols.Length - 1;

                // Wrap each col in a mini 2-row TLP: caption (22px) + control (fill)
                var cell = new TableLayoutPanel
                {
                    Dock            = DockStyle.Fill,
                    RowCount        = 2,
                    ColumnCount     = 1,
                    BackColor       = Color.Transparent,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding         = last ? Padding.Empty : new Padding(0, 0, 12, 0)
                };
                cell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                cell.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));
                cell.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                if (!string.IsNullOrEmpty(caption))
                    cell.Controls.Add(new Label
                    {
                        Text      = caption,
                        Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                        ForeColor = Color.FromArgb(98, 112, 135),
                        Dock      = DockStyle.Fill,
                        TextAlign = ContentAlignment.BottomLeft
                    }, 0, 0);

                ctrl.Dock = DockStyle.Fill;
                cell.Controls.Add(ctrl, 0, 1);
                tbl.Controls.Add(cell, i, 0);
            }
            return tbl;
        }

        // ────────────────────────────────────────────────────────────────
        //  BuildDateRangeRow — single horizontal row for date range input:
        //    [CheckBox] [From DatePicker] ["to" label] [To DatePicker]
        //    plus any additional columns appended after.
        //  chkEnable: the checkbox that enables/disables dtpFrom.
        //  dtpFrom / dtpTo: the two date pickers.
        //  extraCols: optional additional (caption, ctrl) pairs.
        // ────────────────────────────────────────────────────────────────
        private static Panel BuildDateRangeRow(
            CheckBox chkEnable,
            DateTimePicker dtpFrom,
            DateTimePicker dtpTo,
            params (string caption, Control ctrl)[] extraCols)
        {
            // "to" connector label
            var lblTo = new Label
            {
                Text      = "to",
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(98, 112, 135),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock      = DockStyle.Fill
            };

            // Total columns: chk(fixed) + dtpFrom + lblTo(fixed) + dtpTo + extras
            int extraCount = extraCols == null ? 0 : extraCols.Length;
            int totalCols  = 4 + extraCount;

            var tbl = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = totalCols,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(0, 2, 0, 2)
            };
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            // Checkbox col: fixed 34px
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34f));
            // From DatePicker: percent
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, extraCount == 0 ? 40f : 30f));
            // "to" label: fixed 36px
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36f));
            // To DatePicker: percent
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, extraCount == 0 ? 40f : 30f));
            // Extra cols share remaining percent
            for (int i = 0; i < extraCount; i++)
                tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));

            chkEnable.Dock = DockStyle.Fill;
            dtpFrom.Dock   = DockStyle.Fill;
            dtpTo.Dock     = DockStyle.Fill;

            tbl.Controls.Add(chkEnable, 0, 0);
            tbl.Controls.Add(dtpFrom,   1, 0);
            tbl.Controls.Add(lblTo,     2, 0);
            tbl.Controls.Add(dtpTo,     3, 0);

            if (extraCols != null)
            {
                for (int i = 0; i < extraCols.Length; i++)
                {
                    var (cap, ctrl) = extraCols[i];
                    // Wrap extra in a mini 2-row cell with caption
                    var cell = new TableLayoutPanel
                    {
                        Dock            = DockStyle.Fill,
                        RowCount        = 2,
                        ColumnCount     = 1,
                        BackColor       = Color.Transparent,
                        CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                        Padding         = new Padding(8, 0, 0, 0)
                    };
                    cell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                    cell.RowStyles.Add(new RowStyle(SizeType.Absolute, 20f));
                    cell.RowStyles.Add(new RowStyle(SizeType.Percent,  100f));
                    if (!string.IsNullOrEmpty(cap))
                        cell.Controls.Add(new Label
                        {
                            Text = cap, Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                            ForeColor = Color.FromArgb(98, 112, 135),
                            Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft
                        }, 0, 0);
                    ctrl.Dock = DockStyle.Fill;
                    cell.Controls.Add(ctrl, 0, 1);
                    tbl.Controls.Add(cell, 4 + i, 0);
                }
            }
            return tbl;
        }

        // ────────────────────────────────────────────────────────────────
        //  BuildButtonsRow — all buttons 210×60
        // ────────────────────────────────────────────────────────────────
        private static Panel BuildButtonsRow(
            Button btnApply, Button btnReset,
            Button btnChart, Button btnTable, Button btnExport)
        {
            const int BtnW = 210;
            const int BtnH =  60;
            const int Gap  =   8;

            btnApply.Size  = new Size(BtnW, BtnH);
            btnReset.Size  = new Size(BtnW, BtnH);
            btnChart.Size  = new Size(BtnW, BtnH);
            btnTable.Size  = new Size(BtnW, BtnH);
            btnExport.Size = new Size(BtnW, BtnH);

            btnApply.Location  = new Point(0, 0);
            btnReset.Location  = new Point(BtnW + Gap, 0);

            var div = new Panel
            {
                Size      = new Size(1, BtnH),
                Location  = new Point((BtnW + Gap) * 2 + 8, 0),
                BackColor = Color.FromArgb(221, 227, 236)
            };

            int xRight = div.Left + div.Width + 8;
            btnChart.Location  = new Point(xRight, 0);
            btnTable.Location  = new Point(xRight + BtnW + Gap, 0);
            btnExport.Location = new Point(xRight + (BtnW + Gap) * 2, 0);

            var pnl = new Panel { BackColor = Color.Transparent };
            pnl.Controls.AddRange(new Control[] { btnApply, btnReset, div, btnChart, btnTable, btnExport });
            return pnl;
        }

        // ════════════════════════════════════════════════════════════════
        //  GRID CARD BUILDER
        // ════════════════════════════════════════════════════════════════

        private void AddGridCard(
            DockStyle dock, int height,
            string sectionLabel,
            DataGridView dgv, Panel chart, bool showChart,
            bool isLast)
        {
            var hdrPanel = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 38,
                BackColor = Color.FromArgb(246, 249, 255),
                Padding   = new Padding(16, 0, 0, 0)
            };
            hdrPanel.Paint += (o, ev) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                ev.Graphics.DrawLine(pen, 0, ((Panel)o).Height - 1, ((Panel)o).Width, ((Panel)o).Height - 1);
            };
            hdrPanel.Controls.Add(new Label
            {
                Text      = sectionLabel,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            });

            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            inner.Paint += PaintCardBorder;
            if (dgv   != null) { dgv.Dock   = DockStyle.Fill; dgv.Visible   = !showChart; inner.Controls.Add(dgv); }
            if (chart != null) { chart.Dock = DockStyle.Fill; chart.Visible =  showChart; inner.Controls.Add(chart); }
            inner.Controls.Add(hdrPanel);

            int padB = isLast ? 10 : 0;
            var outer = new Panel
            {
                Dock      = dock,
                BackColor = Palette.BgPage,
                Padding   = new Padding(20, 6, 20, padB)
            };
            if (dock == DockStyle.Bottom && height > 0) outer.Height = height;
            outer.Controls.Add(inner);
            pnlContent.Controls.Add(outer);
        }

        // ════════════════════════════════════════════════════════════════
        //  REPORT RENDERS
        // ════════════════════════════════════════════════════════════════

        private void RenderSales()
        {
            var dtpFrom  = MakeDatePicker(DateTime.Today.AddMonths(-3));
            var dtpTo    = MakeDatePicker(DateTime.Today);
            var chkDate  = new CheckBox { Text = "Date Range", Font = new Font("Segoe UI", 11f), ForeColor = Color.FromArgb(98, 112, 135), BackColor = Color.Transparent, AutoSize = false, Dock = DockStyle.Fill };
            var btnApply = MakePrimaryBtn("Apply");
            var btnReset = MakeOutlineBtn("Reset");
            var btnChart = MakeToggleBtn("\U0001F4CA  Chart", _salesChart);
            var btnTable = MakeToggleBtn("\U0001F4CB  Table", !_salesChart);
            var btnExport = MakeExportBtn();

            dtpFrom.Enabled = false;
            chkDate.CheckedChanged += (s, e) => dtpFrom.Enabled = chkDate.Checked;

            SetFilterBar("Filter: Sales Performance",
                BuildDateRangeRow(chkDate, dtpFrom, dtpTo),
                BuildButtonsRow(btnApply, btnReset, btnChart, btnTable, btnExport));

            var dgv = MakeGrid();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrderID",  HeaderText = "ORDER ID",    FillWeight = 18 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomer", HeaderText = "CUSTOMER",    FillWeight = 22 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",   HeaderText = "STATUS",      FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",     HeaderText = "ORDER DATE",  FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal",    HeaderText = "GRAND TOTAL", FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLines",    HeaderText = "ITEMS",       FillWeight =  8 });
            dgv.CellFormatting += (s, e) => ApplyStatusBadge(s, e, "colStatus");

            var dgvTop = MakeGrid();
            dgvTop.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItemID",  HeaderText = "ITEM ID",   FillWeight = 15 });
            dgvTop.Columns.Add(new DataGridViewTextBoxColumn { Name = "colProduct", HeaderText = "PRODUCT",   FillWeight = 32 });
            dgvTop.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCat",     HeaderText = "CATEGORY",  FillWeight = 14 });
            dgvTop.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQty",     HeaderText = "TOTAL QTY", FillWeight = 14 });
            dgvTop.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRev",     HeaderText = "REVENUE",   FillWeight = 20 });

            Panel chartMain = null, chartTop = null;

            Action<DateTime?, DateTime?> load = (from, to) =>
            {
                var vm = _ctrl.GetSalesReportVM(from, to);
                ApplyShell(vm, "Sales Performance");

                dgv.Rows.Clear();
                foreach (var r in vm.SalesRows)
                    dgv.Rows.Add(r.OrderID, r.CustomerName, r.OrderStatus, r.IssuedTime.ToString("yyyy-MM-dd"), $"HK$ {r.GrandTotal:N2}", r.LineCount);

                dgvTop.Rows.Clear();
                foreach (var p in vm.TopProducts)
                    dgvTop.Rows.Add(p.ItemID, p.ItemName, p.Category, p.TotalQty, $"HK$ {p.TotalRevenue:N2}");

                var statusTotals = new Dictionary<string, double>();
                foreach (var r in vm.SalesRows) { if (!statusTotals.ContainsKey(r.OrderStatus)) statusTotals[r.OrderStatus] = 0; statusTotals[r.OrderStatus] += (double)r.GrandTotal; }
                var barData = new List<(string, double)>(); foreach (var kv in statusTotals) barData.Add((kv.Key, kv.Value));

                var topData = new List<(string, double)>();
                foreach (var p in vm.TopProducts) topData.Add((p.ItemName.Length > 18 ? p.ItemName.Substring(0, 16) + "\u2026" : p.ItemName, (double)p.TotalRevenue));

                chartMain = ChartRenderer.CreateBarChart(barData, "Revenue by Order Status", "HK$", "N0", Palette.Primary);
                chartTop  = ChartRenderer.CreateHorizontalBarChart(topData, "Top Products by Revenue", "N0", Palette.Primary);
                ToggleChartTable(_salesChart, dgv, chartMain, dgvTop, chartTop);
            };

            btnApply.Click  += (s, e) => load(chkDate.Checked ? (DateTime?)dtpFrom.Value : null, dtpTo.Value);
            btnReset.Click  += (s, e) => { chkDate.Checked = false; dtpFrom.Value = DateTime.Today.AddMonths(-3); dtpTo.Value = DateTime.Today; load(null, null); };
            btnChart.Click  += (s, e) => { _salesChart = true;  FlipToggle(btnChart, btnTable, true);  ToggleChartTable(_salesChart, dgv, chartMain, dgvTop, chartTop); };
            btnTable.Click  += (s, e) => { _salesChart = false; FlipToggle(btnChart, btnTable, false); ToggleChartTable(_salesChart, dgv, chartMain, dgvTop, chartTop); };
            btnExport.Click += (s, e) => CsvExporter.Export(dgv, "SalesPerformance");
            load(null, null);

            AddGridCard(DockStyle.Bottom, 292, "TOP PRODUCTS BY REVENUE", dgvTop, chartTop, _salesChart, true);
            AddGridCard(DockStyle.Fill,     0, "ORDERS",                  dgv,  chartMain, _salesChart, false);
        }

        private void RenderInventory()
        {
            var cboCat     = MakeCbo(new[] { "All", "Product", "Raw Material" });
            var chkReorder = new CheckBox { Text = "Below Reorder Only", Font = new Font("Segoe UI", 11f), ForeColor = Color.FromArgb(98, 112, 135), BackColor = Color.Transparent, AutoSize = true };
            var btnApply   = MakePrimaryBtn("Apply");
            var btnReset   = MakeOutlineBtn("Reset");
            var btnChart   = MakeToggleBtn("\U0001F4CA  Chart", _inventoryChart);
            var btnTable   = MakeToggleBtn("\U0001F4CB  Table", !_inventoryChart);
            var btnExport  = MakeExportBtn();

            SetFilterBar("Filter: Inventory Status",
                BuildFieldsRow(("Category", cboCat), ("Reorder Alert", chkReorder)),
                BuildButtonsRow(btnApply, btnReset, btnChart, btnTable, btnExport));

            var dgv = MakeGrid();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colWHIID",   HeaderText = "WHI ID",        FillWeight = 13 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItem",    HeaderText = "ITEM",          FillWeight = 24 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCat",     HeaderText = "CATEGORY",      FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMat",     HeaderText = "MATERIAL TYPE", FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colWH",      HeaderText = "WAREHOUSE",     FillWeight = 22 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStock",   HeaderText = "CURRENT STOCK", FillWeight = 10 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colReorder", HeaderText = "REORDER LVL",   FillWeight = 10 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAlert",   HeaderText = "ALERT",         FillWeight =  9 });
            dgv.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex < 0 || e.RowIndex < 0 || e.Value == null) return;
                if (((DataGridView)s).Columns[e.ColumnIndex].Name != "colAlert") return;
                bool low = e.Value.ToString() == "Low Stock";
                e.CellStyle.ForeColor = low ? Color.FromArgb(185, 28, 28) : Color.FromArgb(6, 95, 70);
                e.CellStyle.BackColor = low ? Color.FromArgb(254, 226, 226) : Color.FromArgb(209, 250, 229);
                e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
                e.CellStyle.SelectionBackColor = e.CellStyle.BackColor;
                e.CellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                e.FormattingApplied = true;
            };

            Panel chartStock = null, chartCat = null;

            Action load = () =>
            {
                var vm = _ctrl.GetInventoryReportVM(cboCat.SelectedItem?.ToString(), chkReorder.Checked);
                ApplyShell(vm, "Inventory Status");
                dgv.Rows.Clear();
                foreach (var r in vm.InventoryRows)
                    dgv.Rows.Add(r.WarehouseItemID, $"{r.ItemID}  —  {r.ItemName}", r.ItemCategory,
                                 string.IsNullOrEmpty(r.MaterialType) ? "—" : r.MaterialType,
                                 r.WarehouseLocation, r.CurrentStock, r.ReorderLevel,
                                 r.BelowReorder ? "Low Stock" : "OK");

                var stockData = new List<(string, double)>();
                foreach (var r in vm.InventoryRows) stockData.Add(($"{r.ItemID}", (double)r.CurrentStock));
                if (stockData.Count > 10) stockData = stockData.GetRange(0, 10);

                var catTotals = new Dictionary<string, double>();
                foreach (var r in vm.InventoryRows) { if (!catTotals.ContainsKey(r.ItemCategory)) catTotals[r.ItemCategory] = 0; catTotals[r.ItemCategory] += r.CurrentStock; }
                var donutData = new List<(string, double)>(); foreach (var kv in catTotals) donutData.Add((kv.Key, kv.Value));

                chartStock = ChartRenderer.CreateHorizontalBarChart(stockData, "Stock Levels (Top 10)", "N0", Palette.Primary);
                chartCat   = ChartRenderer.CreateDonutChart(donutData, "Stock by Category");
                ToggleChartTable(_inventoryChart, dgv, chartStock, null, chartCat);
            };

            btnApply.Click  += (s, e) => load();
            btnReset.Click  += (s, e) => { cboCat.SelectedIndex = 0; chkReorder.Checked = false; load(); };
            btnChart.Click  += (s, e) => { _inventoryChart = true;  FlipToggle(btnChart, btnTable, true);  ToggleChartTable(_inventoryChart, dgv, chartStock, null, chartCat); };
            btnTable.Click  += (s, e) => { _inventoryChart = false; FlipToggle(btnChart, btnTable, false); ToggleChartTable(_inventoryChart, dgv, chartStock, null, chartCat); };
            btnExport.Click += (s, e) => CsvExporter.Export(dgv, "InventoryStatus");
            load();

            AddGridCard(DockStyle.Fill, 0, "INVENTORY DETAIL", dgv, chartStock, _inventoryChart, true);
        }

        private void RenderProcurement()
        {
            var cboStatus = MakeCbo(new[] { "All", "Sent", "Partially Received", "Received", "Completed", "Cancelled" });
            var btnApply  = MakePrimaryBtn("Apply");
            var btnReset  = MakeOutlineBtn("Reset");
            var btnChart  = MakeToggleBtn("\U0001F4CA  Chart", _procurementChart);
            var btnTable  = MakeToggleBtn("\U0001F4CB  Table", !_procurementChart);
            var btnExport = MakeExportBtn();

            SetFilterBar("Filter: Procurement Summary",
                BuildFieldsRow(("Status", cboStatus)),
                BuildButtonsRow(btnApply, btnReset, btnChart, btnTable, btnExport));

            var dgv = MakeGrid();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPOID",     HeaderText = "PO ID",      FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSupplier", HeaderText = "SUPPLIER",   FillWeight = 22 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",   HeaderText = "STATUS",     FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",     HeaderText = "ORDER DATE", FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAmount",   HeaderText = "PO AMOUNT",  FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMat",      HeaderText = "MATERIALS",  FillWeight = 24 });
            dgv.CellFormatting += (s, e) => ApplyStatusBadge(s, e, "colStatus");

            Panel chartSupplier = null, chartStatus = null;

            Action load = () =>
            {
                var vm = _ctrl.GetProcurementReportVM(cboStatus.SelectedItem?.ToString());
                ApplyShell(vm, "Procurement Summary");
                dgv.Rows.Clear();
                foreach (var r in vm.ProcRows)
                    dgv.Rows.Add(r.PurchaseID, r.SupplierName, r.PurchaseStatus, r.OrderDate.ToString("yyyy-MM-dd"), $"HK$ {r.POTotalAmount:N2}", r.MaterialNames);

                var supplierSpend = new Dictionary<string, double>();
                foreach (var r in vm.ProcRows) { if (!supplierSpend.ContainsKey(r.SupplierName)) supplierSpend[r.SupplierName] = 0; supplierSpend[r.SupplierName] += (double)r.POTotalAmount; }
                var supplierData = new List<(string, double)>(); foreach (var kv in supplierSpend) supplierData.Add((kv.Key, kv.Value));

                var statusCounts = new Dictionary<string, double>();
                foreach (var r in vm.ProcRows) { if (!statusCounts.ContainsKey(r.PurchaseStatus)) statusCounts[r.PurchaseStatus] = 0; statusCounts[r.PurchaseStatus]++; }
                var statusData = new List<(string, double)>(); foreach (var kv in statusCounts) statusData.Add((kv.Key, kv.Value));

                chartSupplier = ChartRenderer.CreateBarChart(supplierData, "Spend by Supplier (HK$)", "HK$", "N0", Palette.Primary);
                chartStatus   = ChartRenderer.CreateDonutChart(statusData, "PO Status Breakdown");
                ToggleChartTable(_procurementChart, dgv, chartSupplier, null, chartStatus);
            };

            btnApply.Click  += (s, e) => load();
            btnReset.Click  += (s, e) => { cboStatus.SelectedIndex = 0; load(); };
            btnChart.Click  += (s, e) => { _procurementChart = true;  FlipToggle(btnChart, btnTable, true);  ToggleChartTable(_procurementChart, dgv, chartSupplier, null, chartStatus); };
            btnTable.Click  += (s, e) => { _procurementChart = false; FlipToggle(btnChart, btnTable, false); ToggleChartTable(_procurementChart, dgv, chartSupplier, null, chartStatus); };
            btnExport.Click += (s, e) => CsvExporter.Export(dgv, "ProcurementSummary");
            load();

            AddGridCard(DockStyle.Fill, 0, "PURCHASE ORDERS", dgv, chartSupplier, _procurementChart, true);
        }

        private void RenderLogistics()
        {
            var cboStatus = MakeCbo(new[] { "All", "Pending", "In Transit", "Completed" });
            var btnApply  = MakePrimaryBtn("Apply");
            var btnReset  = MakeOutlineBtn("Reset");
            var btnChart  = MakeToggleBtn("\U0001F4CA  Chart", _logisticsChart);
            var btnTable  = MakeToggleBtn("\U0001F4CB  Table", !_logisticsChart);
            var btnExport = MakeExportBtn();

            SetFilterBar("Filter: Logistics Overview",
                BuildFieldsRow(("Status", cboStatus)),
                BuildButtonsRow(btnApply, btnReset, btnChart, btnTable, btnExport));

            var dgv = MakeGrid();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colShipID", HeaderText = "SHIPMENT ID", FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrder",  HeaderText = "ORDER ID",    FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCust",   HeaderText = "CUSTOMER",    FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "STATUS",      FillWeight = 13 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colType",   HeaderText = "TYPE",        FillWeight = 10 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMethod", HeaderText = "METHOD",      FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",   HeaderText = "SHIP DATE",   FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDN",     HeaderText = "D.NOTE",      FillWeight =  9 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRS",     HeaderText = "REPLY SLIP",  FillWeight =  8 });
            dgv.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex < 0 || e.RowIndex < 0 || e.Value == null) return;
                var gv = (DataGridView)s;
                string col = gv.Columns[e.ColumnIndex].Name;
                if (col == "colStatus") { ApplyStatusBadge(s, e, "colStatus"); return; }
                if (col == "colDN" || col == "colRS")
                {
                    bool yes = e.Value.ToString() == "Yes";
                    e.CellStyle.ForeColor = yes ? Color.FromArgb(6, 95, 70) : Color.FromArgb(185, 28, 28);
                    e.CellStyle.BackColor = yes ? Color.FromArgb(209, 250, 229) : Color.FromArgb(254, 226, 226);
                    e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
                    e.CellStyle.SelectionBackColor = e.CellStyle.BackColor;
                    e.CellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
                    e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    e.FormattingApplied = true;
                }
            };

            Panel chartStatus = null;

            Action load = () =>
            {
                var vm = _ctrl.GetLogisticsReportVM(cboStatus.SelectedItem?.ToString());
                ApplyShell(vm, "Logistics Overview");
                dgv.Rows.Clear();
                foreach (var r in vm.LogRows)
                    dgv.Rows.Add(r.ShipmentID, r.OrderID, r.CustomerName, r.ShipmentStatus, r.ShipmentType,
                                 r.DeliveryMethod, r.ShipDate.ToString("yyyy-MM-dd"),
                                 r.HasDeliveryNote ? "Yes" : "No", r.HasReplySlip ? "Yes" : "No");

                var k = vm.LogKpi;
                var donutData = new List<(string, double)>
                {
                    ("Completed",  (double)k.Completed),
                    ("In Transit", (double)k.InTransit),
                    ("Pending",    (double)k.Pending),
                };
                donutData.RemoveAll(x => x.Item2 <= 0);
                chartStatus = ChartRenderer.CreateDonutChart(donutData, "Shipment Status");
                ToggleChartTable(_logisticsChart, dgv, chartStatus, null, null);
            };

            btnApply.Click  += (s, e) => load();
            btnReset.Click  += (s, e) => { cboStatus.SelectedIndex = 0; load(); };
            btnChart.Click  += (s, e) => { _logisticsChart = true;  FlipToggle(btnChart, btnTable, true);  ToggleChartTable(_logisticsChart, dgv, chartStatus, null, null); };
            btnTable.Click  += (s, e) => { _logisticsChart = false; FlipToggle(btnChart, btnTable, false); ToggleChartTable(_logisticsChart, dgv, chartStatus, null, null); };
            btnExport.Click += (s, e) => CsvExporter.Export(dgv, "LogisticsOverview");
            load();

            AddGridCard(DockStyle.Fill, 0, "SHIPMENTS", dgv, chartStatus, _logisticsChart, true);
        }

        private void RenderAfterService()
        {
            var cboCmp    = MakeCbo(new[] { "All", "Pending", "Processing", "Escalated", "Completed" });
            var cboRtn    = MakeCbo(new[] { "All", "Pending", "Approved", "Processing", "Rejected", "Completed" });
            var btnApply  = MakePrimaryBtn("Apply");
            var btnReset  = MakeOutlineBtn("Reset");
            var btnChart  = MakeToggleBtn("\U0001F4CA  Chart", _afterServiceChart);
            var btnTable  = MakeToggleBtn("\U0001F4CB  Table", !_afterServiceChart);
            var btnExport = MakeExportBtn();

            SetFilterBar("Filter: After-Service Summary",
                BuildFieldsRow(("Complaint Status", cboCmp), ("Return Status", cboRtn)),
                BuildButtonsRow(btnApply, btnReset, btnChart, btnTable, btnExport));

            var dgvCmp = MakeGrid();
            dgvCmp.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCmpID",  HeaderText = "COMPLAINT ID", FillWeight = 22 });
            dgvCmp.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrder",  HeaderText = "ORDER ID",     FillWeight = 18 });
            dgvCmp.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCust",   HeaderText = "CUSTOMER",     FillWeight = 20 });
            dgvCmp.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDesc",   HeaderText = "DESCRIPTION",  FillWeight = 28 });
            dgvCmp.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "STATUS",       FillWeight = 14 });
            dgvCmp.CellFormatting += (s, e) => ApplyStatusBadge(s, e, "colStatus");

            var dgvRtn = MakeGrid();
            dgvRtn.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRtnID",  HeaderText = "RETURN ID",  FillWeight = 20 });
            dgvRtn.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrder",  HeaderText = "ORDER ID",   FillWeight = 16 });
            dgvRtn.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCust",   HeaderText = "CUSTOMER",   FillWeight = 18 });
            dgvRtn.Columns.Add(new DataGridViewTextBoxColumn { Name = "colReason", HeaderText = "REASON",     FillWeight = 22 });
            dgvRtn.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRefund", HeaderText = "REFUND",     FillWeight = 12 });
            dgvRtn.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "STATUS",     FillWeight = 14 });
            dgvRtn.CellFormatting += (s, e) => ApplyStatusBadge(s, e, "colStatus");

            Panel chartCmp = null, chartRtn = null;

            Action load = () =>
            {
                var vm = _ctrl.GetAfterServiceReportVM(cboCmp.SelectedItem?.ToString(), cboRtn.SelectedItem?.ToString());
                ApplyShell(vm, "After-Service Summary");
                dgvCmp.Rows.Clear();
                foreach (var r in vm.Complaints) dgvCmp.Rows.Add(r.ComplaintID, r.OrderID, r.CustomerName, r.ComplaintDescription, r.ComplaintStatus);
                dgvRtn.Rows.Clear();
                foreach (var r in vm.Returns) dgvRtn.Rows.Add(r.ReturnID, r.OrderID, r.CustomerName, r.Reason, $"HK$ {r.RefundAmount:N2}", r.ReturnStatus);

                var cmpCounts = new Dictionary<string, double>(); foreach (var r in vm.Complaints) { if (!cmpCounts.ContainsKey(r.ComplaintStatus)) cmpCounts[r.ComplaintStatus] = 0; cmpCounts[r.ComplaintStatus]++; }
                var cmpData = new List<(string, double)>(); foreach (var kv in cmpCounts) cmpData.Add((kv.Key, kv.Value));

                var rtnCounts = new Dictionary<string, double>(); foreach (var r in vm.Returns) { if (!rtnCounts.ContainsKey(r.ReturnStatus)) rtnCounts[r.ReturnStatus] = 0; rtnCounts[r.ReturnStatus]++; }
                var rtnData = new List<(string, double)>(); foreach (var kv in rtnCounts) rtnData.Add((kv.Key, kv.Value));

                chartCmp = ChartRenderer.CreateDonutChart(cmpData, "Complaint Status");
                chartRtn = ChartRenderer.CreateDonutChart(rtnData, "Return Status");
                ToggleChartTable(_afterServiceChart, dgvCmp, chartCmp, dgvRtn, chartRtn);
            };

            btnApply.Click  += (s, e) => load();
            btnReset.Click  += (s, e) => { cboCmp.SelectedIndex = 0; cboRtn.SelectedIndex = 0; load(); };
            btnChart.Click  += (s, e) => { _afterServiceChart = true;  FlipToggle(btnChart, btnTable, true);  ToggleChartTable(_afterServiceChart, dgvCmp, chartCmp, dgvRtn, chartRtn); };
            btnTable.Click  += (s, e) => { _afterServiceChart = false; FlipToggle(btnChart, btnTable, false); ToggleChartTable(_afterServiceChart, dgvCmp, chartCmp, dgvRtn, chartRtn); };
            btnExport.Click += (s, e) => { CsvExporter.Export(dgvCmp, "Complaints"); CsvExporter.Export(dgvRtn, "Returns"); };
            load();

            AddGridCard(DockStyle.Bottom, 292, "RETURN ORDERS", dgvRtn, chartRtn, _afterServiceChart, true);
            AddGridCard(DockStyle.Fill,     0, "COMPLAINTS",    dgvCmp, chartCmp, _afterServiceChart, false);
        }

        private void RenderFinance()
        {
            var dtpFrom   = MakeDatePicker(DateTime.Today.AddMonths(-3));
            var dtpTo     = MakeDatePicker(DateTime.Today);
            var chkDate   = new CheckBox { Text = "Date Range", Font = new Font("Segoe UI", 11f), ForeColor = Color.FromArgb(98, 112, 135), BackColor = Color.Transparent, AutoSize = false, Dock = DockStyle.Fill };
            var btnApply  = MakePrimaryBtn("Apply");
            var btnReset  = MakeOutlineBtn("Reset");
            var btnChart  = MakeToggleBtn("\U0001F4CA  Chart", _financeChart);
            var btnTable  = MakeToggleBtn("\U0001F4CB  Table", !_financeChart);
            var btnExport = MakeExportBtn();

            dtpFrom.Enabled = false;
            chkDate.CheckedChanged += (s, e) => dtpFrom.Enabled = chkDate.Checked;

            SetFilterBar("Filter: Finance Overview",
                BuildDateRangeRow(chkDate, dtpFrom, dtpTo),
                BuildButtonsRow(btnApply, btnReset, btnChart, btnTable, btnExport));

            var dgv = MakeGrid();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTxnID",   HeaderText = "TRANSACTION ID",  FillWeight = 22 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colType",    HeaderText = "TYPE",            FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAmount",  HeaderText = "AMOUNT (HK$)",    FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",    HeaderText = "DATE",            FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDoc",     HeaderText = "LINKED DOCUMENT", FillWeight = 22 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDocType", HeaderText = "DOCUMENT TYPE",   FillWeight = 18 });
            dgv.CellFormatting += (s, e) => ApplyStatusBadge(s, e, "colType");

            Panel chartAmounts = null, chartBreakdown = null;

            Action<DateTime?, DateTime?> load = (from, to) =>
            {
                var vm = _ctrl.GetFinanceReportVM(from, to);
                ApplyShell(vm, "Finance Overview");
                dgv.Rows.Clear();
                foreach (var r in vm.FinanceRows)
                    dgv.Rows.Add(r.TransactionID, r.TransactionType, $"{r.Amount:N2}",
                                 r.TransactionDate.ToString("yyyy-MM-dd"), r.LinkedDocument, r.DocumentType);

                var k = vm.FinanceKpi;
                var typeTotals = new Dictionary<string, double>();
                foreach (var r in vm.FinanceRows) { if (!typeTotals.ContainsKey(r.TransactionType)) typeTotals[r.TransactionType] = 0; typeTotals[r.TransactionType] += (double)r.Amount; }
                var barData = new List<(string, double)>(); foreach (var kv in typeTotals) barData.Add((kv.Key, kv.Value));

                var breakdownData = new List<(string, double)>
                {
                    ("Sales Revenue",     (double)k.TotalSalesRevenue),
                    ("Procurement Spend", (double)k.TotalProcurementSpend),
                    ("Refunds",           (double)k.TotalRefunds),
                    ("AR Outstanding",    (double)k.AROutstanding),
                    ("AP Outstanding",    (double)k.APOutstanding),
                };
                breakdownData.RemoveAll(x => x.Item2 <= 0);

                chartAmounts   = ChartRenderer.CreateBarChart(barData, "Transaction Amounts by Type (HK$)", "HK$", "N0", Palette.Primary);
                chartBreakdown = ChartRenderer.CreateDonutChart(breakdownData, "Revenue Breakdown");
                ToggleChartTable(_financeChart, dgv, chartAmounts, null, chartBreakdown);
            };

            chkDate.CheckedChanged += (s, e) => dtpFrom.Enabled = chkDate.Checked;
            btnApply.Click  += (s, e) => load(chkDate.Checked ? (DateTime?)dtpFrom.Value : null, dtpTo.Value);
            btnReset.Click  += (s, e) => { chkDate.Checked = false; dtpFrom.Value = DateTime.Today.AddMonths(-3); dtpTo.Value = DateTime.Today; load(null, null); };
            btnChart.Click  += (s, e) => { _financeChart = true;  FlipToggle(btnChart, btnTable, true);  ToggleChartTable(_financeChart, dgv, chartAmounts, null, chartBreakdown); };
            btnTable.Click  += (s, e) => { _financeChart = false; FlipToggle(btnChart, btnTable, false); ToggleChartTable(_financeChart, dgv, chartAmounts, null, chartBreakdown); };
            btnExport.Click += (s, e) => CsvExporter.Export(dgv, "FinanceOverview");
            load(null, null);

            AddGridCard(DockStyle.Fill, 0, "TRANSACTIONS", dgv, chartAmounts, _financeChart, true);
        }

        // ════════════════════════════════════════════════════════════════
        //  DATAGRIDVIEW FACTORY
        // ════════════════════════════════════════════════════════════════

        private static DataGridView MakeGrid()
        {
            var g = new DataGridView
            {
                ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                GridColor       = Color.FromArgb(221, 227, 236),
                Font            = new Font("Segoe UI", 12f),
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersHeight   = 40,
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255), ForeColor = Color.FromArgb(98, 112, 135),
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    Padding = new Padding(12, 0, 0, 0), Alignment = DataGridViewContentAlignment.MiddleLeft
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.White, ForeColor = Color.FromArgb(15, 31, 53),
                    SelectionBackColor = Color.FromArgb(219, 234, 254), SelectionForeColor = Color.FromArgb(15, 31, 53),
                    Padding = new Padding(12, 6, 12, 6)
                }
            };
            g.RowTemplate.Height = 44;
            return g;
        }

        // ════════════════════════════════════════════════════════════════
        //  STATUS BADGE
        // ════════════════════════════════════════════════════════════════

        private void ApplyStatusBadge(object sender, DataGridViewCellFormattingEventArgs e, string colName)
        {
            if (e.ColumnIndex < 0 || e.RowIndex < 0 || e.Value == null) return;
            if (((DataGridView)sender).Columns[e.ColumnIndex].Name != colName) return;
            string val = e.Value.ToString();
            if (!StatusColors.TryGetValue(val, out var sc))
                sc = (Color.FromArgb(240, 244, 249), Color.FromArgb(98, 112, 135));
            e.CellStyle.BackColor = sc.bg; e.CellStyle.ForeColor = sc.fg;
            e.CellStyle.SelectionBackColor = sc.bg; e.CellStyle.SelectionForeColor = sc.fg;
            e.CellStyle.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            e.FormattingApplied = true;
        }

        // ════════════════════════════════════════════════════════════════
        //  CHART / TABLE TOGGLE
        // ════════════════════════════════════════════════════════════════

        private static void ToggleChartTable(bool showChart, DataGridView dgv1, Panel chart1, DataGridView dgv2, Panel chart2)
        {
            if (dgv1   != null) dgv1.Visible   = !showChart;
            if (chart1 != null) chart1.Visible =  showChart;
            if (dgv2   != null) dgv2.Visible   = !showChart;
            if (chart2 != null) chart2.Visible =  showChart;
        }

        private static void FlipToggle(Button btnChart, Button btnTable, bool chartActive)
        {
            btnChart.BackColor = chartActive ? Palette.Primary : Color.White;
            btnChart.ForeColor = chartActive ? Color.White : Color.FromArgb(98, 112, 135);
            btnChart.FlatAppearance.BorderSize = chartActive ? 0 : 1;
            btnTable.BackColor = chartActive ? Color.White : Palette.Primary;
            btnTable.ForeColor = chartActive ? Color.FromArgb(98, 112, 135) : Color.White;
            btnTable.FlatAppearance.BorderSize = chartActive ? 1 : 0;
        }

        // ════════════════════════════════════════════════════════════════
        //  BUTTON / CONTROL FACTORIES  (all buttons 210×60)
        // ════════════════════════════════════════════════════════════════

        private static Button MakePrimaryBtn(string text)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f), ForeColor = Color.White, BackColor = Color.FromArgb(19, 35, 61), FlatStyle = FlatStyle.Flat, Size = new Size(210, 60), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(29, 52, 92);
            return b;
        }

        private static Button MakeOutlineBtn(string text)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White, FlatStyle = FlatStyle.Flat, Size = new Size(210, 60), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize  = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            return b;
        }

        private static Button MakeToggleBtn(string text, bool active)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 11f), Size = new Size(210, 60), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            b.BackColor = active ? Palette.Primary : Color.White;
            b.ForeColor = active ? Color.White : Color.FromArgb(98, 112, 135);
            b.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize  = active ? 0 : 1;
            return b;
        }

        private static Button MakeExportBtn()
        {
            var b = new Button { Text = "\u2B07 Export CSV", Font = new Font("Segoe UI", 11f), ForeColor = Color.White, BackColor = Color.FromArgb(5, 150, 105), FlatStyle = FlatStyle.Flat, Size = new Size(210, 60), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(4, 120, 87);
            return b;
        }

        private static DateTimePicker MakeDatePicker(DateTime value)
            => new DateTimePicker { Format = DateTimePickerFormat.Short, Value = value, Font = new Font("Segoe UI", 11f), Width = 130, CalendarForeColor = Color.FromArgb(15, 31, 53), CalendarTitleBackColor = Color.FromArgb(19, 35, 61), CalendarTitleForeColor = Color.White };

        private static ComboBox MakeCbo(string[] items)
        {
            var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 11f), BackColor = Color.White, ForeColor = Color.FromArgb(15, 31, 53) };
            c.Items.AddRange(items); c.SelectedIndex = 0;
            return c;
        }

        // ════════════════════════════════════════════════════════════════
        //  APPSHELL UPDATER
        // ════════════════════════════════════════════════════════════════

        private void ApplyShell(dynamic vm, string reportTitle)
        {
            if (vm == null) return;
            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb($"Statistical Reports  ›  {reportTitle}");
        }

        // ════════════════════════════════════════════════════════════════
        //  ROUNDED RECT HELPER
        // ════════════════════════════════════════════════════════════════

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X,          bounds.Y,          d, d, 180, 90);
            path.AddArc(bounds.Right - d,  bounds.Y,          d, d, 270, 90);
            path.AddArc(bounds.Right - d,  bounds.Bottom - d, d, d,   0, 90);
            path.AddArc(bounds.X,          bounds.Bottom - d, d, d,  90, 90);
            path.CloseFigure();
            return path;
        }

        // ════════════════════════════════════════════════════════════════
        //  NAVIGATION / SESSION
        // ════════════════════════════════════════════════════════════════

        private void OnTopNavMenuItemClicked(string menu, string sub)
            => FormNavigator.NavigateTo(this, menu, sub);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to logout?",
                "Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                FormNavigator.NavigateTo(this, "Logout");
        }
    }
}
