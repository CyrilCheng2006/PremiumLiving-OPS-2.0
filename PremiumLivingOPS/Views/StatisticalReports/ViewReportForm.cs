using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.StatisticalReports
{
    /// <summary>
    /// View — Statistical Reports  ›  View Report
    ///
    /// MVC role  : View only.  All DB access goes through StatisticalReportsController.
    /// AppShell  : mandatory chrome (TopNavBar + UserBar) — RULES 1-5.
    /// CardPanel : every content block in 3-layer nested cards.
    ///
    /// Layout:
    ///   AppShell (Top)
    ///   [Report Selector sidebar | Report content panel (Fill)]
    ///
    ///   Content panel per report:
    ///     CARD A — KPI pills            (DockStyle.Top)
    ///     CARD B — Filter bar           (DockStyle.Top)
    ///     CARD D — secondary grid/chart (DockStyle.Bottom, only for After-Service / Sales)
    ///     CARD C — Main data grid/chart (DockStyle.Fill)
    ///
    ///   Each report tracks its own _showChart state so toggling between
    ///   Table and Chart view is instant without re-querying the DB.
    /// </summary>
    public partial class ViewReportForm : Form
    {
        private readonly StatisticalReportsController _ctrl = new StatisticalReportsController();
        private ReportType _activeReport = ReportType.SalesPerformance;

        // ── Per-report chart-mode toggle flags ───────────────────────────
        private bool _salesChart        = false;
        private bool _inventoryChart    = false;
        private bool _procurementChart  = false;
        private bool _logisticsChart    = false;
        private bool _afterServiceChart = false;
        private bool _financeChart      = false;

        public ViewReportForm()
        {
            InitializeComponent();
            this.Load += ViewReportForm_Load;
        }

        // ════════════════════════════════════════════════════════════════
        //  LOAD
        // ════════════════════════════════════════════════════════════════

        private void ViewReportForm_Load(object sender, EventArgs e)
        {
            btnSales.Click        += (s, _) => SwitchReport(ReportType.SalesPerformance);
            btnInventory.Click    += (s, _) => SwitchReport(ReportType.InventoryStatus);
            btnProcurement.Click  += (s, _) => SwitchReport(ReportType.ProcurementSummary);
            btnLogistics.Click    += (s, _) => SwitchReport(ReportType.LogisticsOverview);
            btnAfterService.Click += (s, _) => SwitchReport(ReportType.AfterServiceSummary);
            btnFinance.Click      += (s, _) => SwitchReport(ReportType.FinanceOverview);

            SwitchReport(ReportType.SalesPerformance);
        }

        // ════════════════════════════════════════════════════════════════
        //  REPORT SWITCHER
        // ════════════════════════════════════════════════════════════════

        private void SwitchReport(ReportType rt)
        {
            _activeReport = rt;
            HighlightSidebarButton(rt);
            RenderReport(rt);
        }

        private void HighlightSidebarButton(ReportType rt)
        {
            var map = new Dictionary<ReportType, Button>
            {
                { ReportType.SalesPerformance,    btnSales        },
                { ReportType.InventoryStatus,     btnInventory    },
                { ReportType.ProcurementSummary,  btnProcurement  },
                { ReportType.LogisticsOverview,   btnLogistics    },
                { ReportType.AfterServiceSummary, btnAfterService },
                { ReportType.FinanceOverview,     btnFinance      }
            };
            foreach (var kv in map)
            {
                bool active = kv.Key == rt;
                kv.Value.BackColor = active ? Palette.Primary : Color.Transparent;
                kv.Value.ForeColor = active ? Color.White     : Palette.SidebarText;
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  RENDER REPORT
        // ════════════════════════════════════════════════════════════════

        private void RenderReport(ReportType rt)
        {
            pnlContent.SuspendLayout();
            pnlContent.Controls.Clear();

            switch (rt)
            {
                case ReportType.SalesPerformance:    RenderSales();        break;
                case ReportType.InventoryStatus:     RenderInventory();    break;
                case ReportType.ProcurementSummary:  RenderProcurement();  break;
                case ReportType.LogisticsOverview:   RenderLogistics();    break;
                case ReportType.AfterServiceSummary: RenderAfterService(); break;
                case ReportType.FinanceOverview:     RenderFinance();      break;
            }

            pnlContent.ResumeLayout(true);
        }

        // ════════════════════════════════════════════════════════════════
        //  1. SALES PERFORMANCE
        // ════════════════════════════════════════════════════════════════

        private void RenderSales()
        {
            var dtpFrom  = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 160, Value = DateTime.Today.AddMonths(-3) };
            var dtpTo    = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 160, Value = DateTime.Today };
            var btnApply = MakePrimaryBtn("Apply", 110, 36);
            var btnReset = MakeOutlineBtn("Reset",  90, 36);
            var pnlKpi   = BuildKpiPanel();

            // ── Chart-toggle + Export buttons ────────────────────────────
            var btnChart  = MakeToggleBtn("📊  Chart",  120, 36, _salesChart);
            var btnTable  = MakeToggleBtn("📋  Table",  110, 36, !_salesChart);
            var btnExport = MakeExportBtn(140, 36);

            // ── Main grid: Orders ────────────────────────────────────────
            var dgv = MakeGrid();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrderID",  HeaderText = "ORDER ID",     FillWeight = 18 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomer", HeaderText = "CUSTOMER",     FillWeight = 22 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",   HeaderText = "STATUS",       FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",     HeaderText = "ORDER DATE",   FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal",    HeaderText = "GRAND TOTAL",  FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLines",    HeaderText = "ITEMS",        FillWeight =  8 });
            dgv.CellFormatting += (s, e) => FormatStatusBadge(s, e, "colStatus");

            // ── Secondary grid: Top Products ─────────────────────────────
            var dgvTop = MakeGrid();
            dgvTop.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItemID",  HeaderText = "ITEM ID",    FillWeight = 15 });
            dgvTop.Columns.Add(new DataGridViewTextBoxColumn { Name = "colProduct", HeaderText = "PRODUCT",    FillWeight = 32 });
            dgvTop.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCat",     HeaderText = "CATEGORY",   FillWeight = 14 });
            dgvTop.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQty",     HeaderText = "TOTAL QTY",  FillWeight = 14 });
            dgvTop.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRev",     HeaderText = "REVENUE",    FillWeight = 20 });

            // ── Chart panels (built lazily on first toggle) ───────────────
            Panel chartMain = null;
            Panel chartTop  = null;

            // ── Data load action ─────────────────────────────────────────
            Action<DateTime?, DateTime?> load = (from, to) =>
            {
                var vm = _ctrl.GetSalesReportVM(from, to);
                ApplyShell(vm, "Sales Performance");
                var k = vm.SalesKpi;
                RefreshKpiPanel(pnlKpi, new[]
                {
                    ("Total Orders",    k.TotalOrders.ToString(),           Palette.Primary,     Palette.TagBlueBg),
                    ("Revenue (HK$)",   $"{k.TotalRevenue:N0}",             Palette.TagGreenFg,  Palette.TagGreenBg),
                    ("Avg Order (HK$)", $"{k.AverageOrderValue:N0}",        Palette.TagBlueFg,   Palette.TagBlueBg),
                    ("Delivered",       k.DeliveredOrders.ToString(),       Palette.TagGreenFg,  Palette.TagGreenBg),
                    ("Processing",      k.ProcessingOrders.ToString(),      Palette.TagYellowFg, Palette.TagYellowBg),
                    ("Pending",         k.PendingOrders.ToString(),         Palette.TagYellowFg, Palette.TagYellowBg),
                });
                dgv.Rows.Clear();
                foreach (var r in vm.SalesRows)
                    dgv.Rows.Add(r.OrderID, r.CustomerName, r.OrderStatus,
                                 r.IssuedTime.ToString("yyyy-MM-dd"), $"HK$ {r.GrandTotal:N2}", r.LineCount);
                dgvTop.Rows.Clear();
                foreach (var p in vm.TopProducts)
                    dgvTop.Rows.Add(p.ItemID, p.ItemName, p.Category, p.TotalQty, $"HK$ {p.TotalRevenue:N2}");

                // ── Rebuild chart data ────────────────────────────────────
                var statusTotals = new Dictionary<string, double>();
                foreach (var r in vm.SalesRows)
                {
                    if (!statusTotals.ContainsKey(r.OrderStatus)) statusTotals[r.OrderStatus] = 0;
                    statusTotals[r.OrderStatus] += (double)r.GrandTotal;
                }
                var barData  = new List<(string, double)>();
                foreach (var kv in statusTotals) barData.Add((kv.Key, kv.Value));

                var topData  = new List<(string, double)>();
                foreach (var p in vm.TopProducts) topData.Add((p.ItemName.Length > 18 ? p.ItemName.Substring(0, 16) + "…" : p.ItemName, (double)p.TotalRevenue));

                chartMain = ChartRenderer.CreateBarChart(barData,  "Revenue by Order Status", "HK$", "N0", Palette.Primary);
                chartTop  = ChartRenderer.CreateHorizontalBarChart(topData, "Top Products by Revenue", "N0", Palette.Primary);

                RefreshChartView(_salesChart, dgv, chartMain, dgvTop, chartTop);
            };

            btnApply.Click += (s, e) => load(dtpFrom.Value, dtpTo.Value);
            btnReset.Click += (s, e) => { dtpFrom.Value = DateTime.Today.AddMonths(-3); dtpTo.Value = DateTime.Today; load(null, null); };

            btnChart.Click += (s, e) =>
            {
                _salesChart = true;
                btnChart.BackColor = Palette.Primary;     btnChart.ForeColor = Color.White;
                btnTable.BackColor = Palette.BgCard;      btnTable.ForeColor = Palette.TextMuted;
                RefreshChartView(_salesChart, dgv, chartMain, dgvTop, chartTop);
            };
            btnTable.Click += (s, e) =>
            {
                _salesChart = false;
                btnTable.BackColor = Palette.Primary;     btnTable.ForeColor = Color.White;
                btnChart.BackColor = Palette.BgCard;      btnChart.ForeColor = Palette.TextMuted;
                RefreshChartView(_salesChart, dgv, chartMain, dgvTop, chartTop);
            };
            btnExport.Click += (s, e) => CsvExporter.Export(dgv, "SalesPerformance");

            load(null, null);

            BuildContentLayout(pnlKpi,
                BuildFilterBar(new Control[] { MakeLabel("From:"), dtpFrom, MakeSpacer(8), MakeLabel("To:"), dtpTo, MakeSpacer(12), btnApply, MakeSpacer(6), btnReset, MakeSpacer(16), MakeDivider(), MakeSpacer(16), btnChart, MakeSpacer(4), btnTable, MakeSpacer(12), btnExport }),
                "Orders",                  dgv,    chartMain, _salesChart,
                "Top Products by Revenue", dgvTop, chartTop,  _salesChart, 220);
        }

        // ════════════════════════════════════════════════════════════════
        //  2. INVENTORY STATUS
        // ════════════════════════════════════════════════════════════════

        private void RenderInventory()
        {
            var cboCat     = MakeCbo(new[] { "All", "Product", "Raw Material" });
            var chkReorder = new CheckBox { Text = "Below Reorder Only", Font = new Font("Segoe UI", 11f), ForeColor = Palette.TextMain, BackColor = Color.Transparent, AutoSize = true };
            var btnApply   = MakePrimaryBtn("Apply", 110, 36);
            var btnReset   = MakeOutlineBtn("Reset",  90, 36);
            var pnlKpi     = BuildKpiPanel();

            var btnChart  = MakeToggleBtn("📊  Chart",  120, 36, _inventoryChart);
            var btnTable  = MakeToggleBtn("📋  Table",  110, 36, !_inventoryChart);
            var btnExport = MakeExportBtn(140, 36);

            var dgv = MakeGrid();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colWHIID",   HeaderText = "WHI ID",         FillWeight = 13 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItem",    HeaderText = "ITEM",           FillWeight = 24 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCat",     HeaderText = "CATEGORY",       FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMat",     HeaderText = "MATERIAL TYPE",  FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colWH",      HeaderText = "WAREHOUSE",      FillWeight = 22 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStock",   HeaderText = "CURRENT STOCK",  FillWeight = 10 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colReorder", HeaderText = "REORDER LVL",    FillWeight = 10 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAlert",   HeaderText = "ALERT",          FillWeight =  9 });
            dgv.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex < 0 || e.RowIndex < 0 || e.Value == null) return;
                string name = ((DataGridView)s).Columns[e.ColumnIndex].Name;
                if (name != "colAlert") return;
                bool low = e.Value.ToString() == "Low Stock";
                e.CellStyle.ForeColor          = low ? Palette.TagRedFg    : Palette.TagGreenFg;
                e.CellStyle.BackColor          = low ? Palette.TagRedBg    : Palette.TagGreenBg;
                e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
                e.CellStyle.SelectionBackColor = e.CellStyle.BackColor;
                e.CellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                e.FormattingApplied = true;
            };

            Panel chartStock    = null;
            Panel chartCategory = null;

            Action load = () =>
            {
                var vm = _ctrl.GetInventoryReportVM(cboCat.SelectedItem?.ToString(), chkReorder.Checked);
                ApplyShell(vm, "Inventory Status");
                var k = vm.InventoryKpi;
                RefreshKpiPanel(pnlKpi, new[]
                {
                    ("Total SKUs",    k.TotalSKUs.ToString(),         Palette.Primary,    Palette.TagBlueBg),
                    ("Products",      k.ProductCount.ToString(),      Palette.TagBlueFg,  Palette.TagBlueBg),
                    ("Raw Materials", k.RawMaterialCount.ToString(),  Palette.TagGreenFg, Palette.TagGreenBg),
                    ("Below Reorder", k.BelowReorderCount.ToString(), Palette.TagRedFg,   Palette.TagRedBg),
                });
                dgv.Rows.Clear();
                foreach (var r in vm.InventoryRows)
                    dgv.Rows.Add(r.WarehouseItemID, $"{r.ItemID}  —  {r.ItemName}",
                                 r.ItemCategory, string.IsNullOrEmpty(r.MaterialType) ? "—" : r.MaterialType,
                                 r.WarehouseLocation, r.CurrentStock, r.ReorderLevel,
                                 r.BelowReorder ? "Low Stock" : "OK");

                // ── Chart data ────────────────────────────────────────────
                var stockData = new List<(string, double)>();
                foreach (var r in vm.InventoryRows)
                    stockData.Add(($"{r.ItemID}", (double)r.CurrentStock));
                // Top 10 by stock for readability
                if (stockData.Count > 10) stockData = stockData.GetRange(0, 10);

                var catTotals = new Dictionary<string, double>();
                foreach (var r in vm.InventoryRows)
                {
                    string cat = string.IsNullOrEmpty(r.ItemCategory) ? "Other" : r.ItemCategory;
                    if (!catTotals.ContainsKey(cat)) catTotals[cat] = 0;
                    catTotals[cat] += r.CurrentStock;
                }
                var donutData = new List<(string, double)>();
                foreach (var kv in catTotals) donutData.Add((kv.Key, kv.Value));

                chartStock    = ChartRenderer.CreateHorizontalBarChart(stockData, "Stock Levels (Top 10)", "N0", Palette.Primary);
                chartCategory = ChartRenderer.CreateDonutChart(donutData, "Stock by Category");

                RefreshChartView(_inventoryChart, dgv, chartStock, null, chartCategory);
            };

            btnApply.Click += (s, e) => load();
            btnReset.Click += (s, e) => { cboCat.SelectedIndex = 0; chkReorder.Checked = false; load(); };
            btnChart.Click += (s, e) =>
            {
                _inventoryChart = true;
                btnChart.BackColor = Palette.Primary; btnChart.ForeColor = Color.White;
                btnTable.BackColor = Palette.BgCard;  btnTable.ForeColor = Palette.TextMuted;
                RefreshChartView(_inventoryChart, dgv, chartStock, null, chartCategory);
            };
            btnTable.Click += (s, e) =>
            {
                _inventoryChart = false;
                btnTable.BackColor = Palette.Primary; btnTable.ForeColor = Color.White;
                btnChart.BackColor = Palette.BgCard;  btnChart.ForeColor = Palette.TextMuted;
                RefreshChartView(_inventoryChart, dgv, chartStock, null, chartCategory);
            };
            btnExport.Click += (s, e) => CsvExporter.Export(dgv, "InventoryStatus");
            load();

            BuildContentLayout(pnlKpi,
                BuildFilterBar(new Control[] { MakeLabel("Category:"), cboCat, MakeSpacer(12), chkReorder, MakeSpacer(12), btnApply, MakeSpacer(6), btnReset, MakeSpacer(16), MakeDivider(), MakeSpacer(16), btnChart, MakeSpacer(4), btnTable, MakeSpacer(12), btnExport }),
                "Inventory Detail", dgv, chartStock, _inventoryChart,
                null, null, null, false, 0);
        }

        // ════════════════════════════════════════════════════════════════
        //  3. PROCUREMENT SUMMARY
        // ════════════════════════════════════════════════════════════════

        private void RenderProcurement()
        {
            var cboStatus = MakeCbo(new[] { "All", "Sent", "Partially Received", "Received", "Completed", "Cancelled" });
            var btnApply  = MakePrimaryBtn("Apply", 110, 36);
            var btnReset  = MakeOutlineBtn("Reset",  90, 36);
            var pnlKpi    = BuildKpiPanel();

            var btnChart  = MakeToggleBtn("📊  Chart",  120, 36, _procurementChart);
            var btnTable  = MakeToggleBtn("📋  Table",  110, 36, !_procurementChart);
            var btnExport = MakeExportBtn(140, 36);

            var dgv = MakeGrid();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPOID",     HeaderText = "PO ID",        FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSupplier", HeaderText = "SUPPLIER",     FillWeight = 22 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",   HeaderText = "STATUS",       FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",     HeaderText = "ORDER DATE",   FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAmount",   HeaderText = "PO AMOUNT",    FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMat",      HeaderText = "MATERIALS",    FillWeight = 24 });
            dgv.CellFormatting += (s, e) => FormatStatusBadge(s, e, "colStatus");

            Panel chartSupplier = null;
            Panel chartStatus   = null;

            Action load = () =>
            {
                var vm = _ctrl.GetProcurementReportVM(cboStatus.SelectedItem?.ToString());
                ApplyShell(vm, "Procurement Summary");
                var k = vm.ProcKpi;
                RefreshKpiPanel(pnlKpi, new[]
                {
                    ("Total POs",      k.TotalPOs.ToString(),         Palette.Primary,     Palette.TagBlueBg),
                    ("Spend (HK$)",    $"{k.TotalSpend:N0}",          Palette.TagRedFg,    Palette.TagRedBg),
                    ("Completed",      k.CompletedPOs.ToString(),     Palette.TagGreenFg,  Palette.TagGreenBg),
                    ("Pending",        k.PendingPOs.ToString(),       Palette.TagYellowFg, Palette.TagYellowBg),
                    ("Suppliers",      k.UniqueSuppliers.ToString(),  Palette.TagBlueFg,   Palette.TagBlueBg),
                });
                dgv.Rows.Clear();
                foreach (var r in vm.ProcRows)
                    dgv.Rows.Add(r.PurchaseID, r.SupplierName, r.PurchaseStatus,
                                 r.OrderDate.ToString("yyyy-MM-dd"), $"HK$ {r.POTotalAmount:N2}", r.MaterialNames);

                // ── Chart data ────────────────────────────────────────────
                var supplierSpend = new Dictionary<string, double>();
                foreach (var r in vm.ProcRows)
                {
                    if (!supplierSpend.ContainsKey(r.SupplierName)) supplierSpend[r.SupplierName] = 0;
                    supplierSpend[r.SupplierName] += (double)r.POTotalAmount;
                }
                var supplierData = new List<(string, double)>();
                foreach (var kv in supplierSpend) supplierData.Add((kv.Key, kv.Value));

                var statusTotals = new Dictionary<string, double>();
                foreach (var r in vm.ProcRows)
                {
                    if (!statusTotals.ContainsKey(r.PurchaseStatus)) statusTotals[r.PurchaseStatus] = 0;
                    statusTotals[r.PurchaseStatus]++;
                }
                var statusData = new List<(string, double)>();
                foreach (var kv in statusTotals) statusData.Add((kv.Key, kv.Value));

                chartSupplier = ChartRenderer.CreateBarChart(supplierData, "Spend by Supplier (HK$)", "HK$", "N0", Palette.Primary);
                chartStatus   = ChartRenderer.CreateDonutChart(statusData, "PO Status Breakdown");

                RefreshChartView(_procurementChart, dgv, chartSupplier, null, chartStatus);
            };

            btnApply.Click += (s, e) => load();
            btnReset.Click += (s, e) => { cboStatus.SelectedIndex = 0; load(); };
            btnChart.Click += (s, e) =>
            {
                _procurementChart = true;
                btnChart.BackColor = Palette.Primary; btnChart.ForeColor = Color.White;
                btnTable.BackColor = Palette.BgCard;  btnTable.ForeColor = Palette.TextMuted;
                RefreshChartView(_procurementChart, dgv, chartSupplier, null, chartStatus);
            };
            btnTable.Click += (s, e) =>
            {
                _procurementChart = false;
                btnTable.BackColor = Palette.Primary; btnTable.ForeColor = Color.White;
                btnChart.BackColor = Palette.BgCard;  btnChart.ForeColor = Palette.TextMuted;
                RefreshChartView(_procurementChart, dgv, chartSupplier, null, chartStatus);
            };
            btnExport.Click += (s, e) => CsvExporter.Export(dgv, "ProcurementSummary");
            load();

            BuildContentLayout(pnlKpi,
                BuildFilterBar(new Control[] { MakeLabel("Status:"), cboStatus, MakeSpacer(12), btnApply, MakeSpacer(6), btnReset, MakeSpacer(16), MakeDivider(), MakeSpacer(16), btnChart, MakeSpacer(4), btnTable, MakeSpacer(12), btnExport }),
                "Purchase Orders", dgv, chartSupplier, _procurementChart,
                null, null, null, false, 0);
        }

        // ════════════════════════════════════════════════════════════════
        //  4. LOGISTICS OVERVIEW
        // ════════════════════════════════════════════════════════════════

        private void RenderLogistics()
        {
            var cboStatus = MakeCbo(new[] { "All", "Pending", "In Transit", "Completed" });
            var btnApply  = MakePrimaryBtn("Apply", 110, 36);
            var btnReset  = MakeOutlineBtn("Reset",  90, 36);
            var pnlKpi    = BuildKpiPanel();

            var btnChart  = MakeToggleBtn("📊  Chart",  120, 36, _logisticsChart);
            var btnTable  = MakeToggleBtn("📋  Table",  110, 36, !_logisticsChart);
            var btnExport = MakeExportBtn(140, 36);

            var dgv = MakeGrid();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colShipID",   HeaderText = "SHIPMENT ID",   FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrder",    HeaderText = "ORDER ID",      FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCust",     HeaderText = "CUSTOMER",      FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",   HeaderText = "STATUS",        FillWeight = 13 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colType",     HeaderText = "TYPE",          FillWeight = 10 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMethod",   HeaderText = "METHOD",        FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",     HeaderText = "SHIP DATE",     FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDN",       HeaderText = "D.NOTE",        FillWeight =  9 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRS",       HeaderText = "REPLY SLIP",    FillWeight =  8 });
            dgv.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex < 0 || e.RowIndex < 0 || e.Value == null) return;
                var gv  = (DataGridView)s;
                string col = gv.Columns[e.ColumnIndex].Name;
                if (col == "colStatus") { FormatStatusBadge(s, e, "colStatus"); return; }
                if (col == "colDN" || col == "colRS")
                {
                    bool yes = e.Value.ToString() == "Yes";
                    e.CellStyle.ForeColor = yes ? Palette.TagGreenFg : Palette.TagRedFg;
                    e.CellStyle.BackColor = yes ? Palette.TagGreenBg : Palette.TagRedBg;
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
                var k = vm.LogKpi;
                RefreshKpiPanel(pnlKpi, new[]
                {
                    ("Total",        k.TotalShipments.ToString(), Palette.Primary,     Palette.TagBlueBg),
                    ("Completed",    k.Completed.ToString(),      Palette.TagGreenFg,  Palette.TagGreenBg),
                    ("In Transit",   k.InTransit.ToString(),      Palette.TagYellowFg, Palette.TagYellowBg),
                    ("Pending",      k.Pending.ToString(),        Palette.TagRedFg,    Palette.TagRedBg),
                    ("Reply Slips",  k.WithReplySlip.ToString(),  Palette.TagBlueFg,   Palette.TagBlueBg),
                });
                dgv.Rows.Clear();
                foreach (var r in vm.LogRows)
                    dgv.Rows.Add(r.ShipmentID, r.OrderID, r.CustomerName, r.ShipmentStatus,
                                 r.ShipmentType, r.DeliveryMethod, r.ShipDate.ToString("yyyy-MM-dd"),
                                 r.HasDeliveryNote ? "Yes" : "No", r.HasReplySlip ? "Yes" : "No");

                // ── Chart data ────────────────────────────────────────────
                var statusCounts = new Dictionary<string, double>
                {
                    { "Completed",  k.Completed  },
                    { "In Transit", k.InTransit  },
                    { "Pending",    k.Pending    },
                };
                var donutData = new List<(string, double)>();
                foreach (var kv in statusCounts) if (kv.Value > 0) donutData.Add((kv.Key, kv.Value));

                chartStatus = ChartRenderer.CreateDonutChart(donutData, "Shipment Status");
                RefreshChartView(_logisticsChart, dgv, chartStatus, null, null);
            };

            btnApply.Click += (s, e) => load();
            btnReset.Click += (s, e) => { cboStatus.SelectedIndex = 0; load(); };
            btnChart.Click += (s, e) =>
            {
                _logisticsChart = true;
                btnChart.BackColor = Palette.Primary; btnChart.ForeColor = Color.White;
                btnTable.BackColor = Palette.BgCard;  btnTable.ForeColor = Palette.TextMuted;
                RefreshChartView(_logisticsChart, dgv, chartStatus, null, null);
            };
            btnTable.Click += (s, e) =>
            {
                _logisticsChart = false;
                btnTable.BackColor = Palette.Primary; btnTable.ForeColor = Color.White;
                btnChart.BackColor = Palette.BgCard;  btnChart.ForeColor = Palette.TextMuted;
                RefreshChartView(_logisticsChart, dgv, chartStatus, null, null);
            };
            btnExport.Click += (s, e) => CsvExporter.Export(dgv, "LogisticsOverview");
            load();

            BuildContentLayout(pnlKpi,
                BuildFilterBar(new Control[] { MakeLabel("Status:"), cboStatus, MakeSpacer(12), btnApply, MakeSpacer(6), btnReset, MakeSpacer(16), MakeDivider(), MakeSpacer(16), btnChart, MakeSpacer(4), btnTable, MakeSpacer(12), btnExport }),
                "Shipments", dgv, chartStatus, _logisticsChart,
                null, null, null, false, 0);
        }

        // ════════════════════════════════════════════════════════════════
        //  5. AFTER-SERVICE SUMMARY
        // ════════════════════════════════════════════════════════════════

        private void RenderAfterService()
        {
            var cboCmp   = MakeCbo(new[] { "All", "Pending", "Processing", "Escalated", "Completed" });
            var cboRtn   = MakeCbo(new[] { "All", "Pending", "Approved", "Processing", "Rejected", "Completed" });
            var btnApply = MakePrimaryBtn("Apply", 110, 36);
            var btnReset = MakeOutlineBtn("Reset",  90, 36);
            var pnlKpi   = BuildKpiPanel();

            var btnChart  = MakeToggleBtn("📊  Chart",  120, 36, _afterServiceChart);
            var btnTable  = MakeToggleBtn("📋  Table",  110, 36, !_afterServiceChart);
            var btnExport = MakeExportBtn(140, 36);

            var dgvCmp = MakeGrid();
            dgvCmp.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCmpID",  HeaderText = "COMPLAINT ID",  FillWeight = 22 });
            dgvCmp.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrder",  HeaderText = "ORDER ID",      FillWeight = 18 });
            dgvCmp.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCust",   HeaderText = "CUSTOMER",      FillWeight = 20 });
            dgvCmp.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDesc",   HeaderText = "DESCRIPTION",   FillWeight = 28 });
            dgvCmp.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "STATUS",        FillWeight = 14 });
            dgvCmp.CellFormatting += (s, e) => FormatStatusBadge(s, e, "colStatus");

            var dgvRtn = MakeGrid();
            dgvRtn.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRtnID",  HeaderText = "RETURN ID",     FillWeight = 20 });
            dgvRtn.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrder",  HeaderText = "ORDER ID",      FillWeight = 16 });
            dgvRtn.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCust",   HeaderText = "CUSTOMER",      FillWeight = 18 });
            dgvRtn.Columns.Add(new DataGridViewTextBoxColumn { Name = "colReason", HeaderText = "REASON",        FillWeight = 22 });
            dgvRtn.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRefund", HeaderText = "REFUND",        FillWeight = 12 });
            dgvRtn.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "STATUS",        FillWeight = 14 });
            dgvRtn.CellFormatting += (s, e) => FormatStatusBadge(s, e, "colStatus");

            Panel chartCmp = null;
            Panel chartRtn = null;

            Action load = () =>
            {
                var vm = _ctrl.GetAfterServiceReportVM(cboCmp.SelectedItem?.ToString(), cboRtn.SelectedItem?.ToString());
                ApplyShell(vm, "After-Service Summary");
                var k = vm.AfterKpi;
                RefreshKpiPanel(pnlKpi, new[]
                {
                    ("Complaints",    k.TotalComplaints.ToString(), Palette.TagRedFg,    Palette.TagRedBg),
                    ("Open",          k.OpenComplaints.ToString(),  Palette.TagOrangeFg, Palette.TagOrangeBg),
                    ("Returns",       k.TotalReturns.ToString(),    Palette.TagYellowFg, Palette.TagYellowBg),
                    ("Refunded (HK$)",$"{k.TotalRefunded:N0}",      Palette.TagBlueFg,   Palette.TagBlueBg),
                });
                dgvCmp.Rows.Clear();
                foreach (var r in vm.Complaints)
                    dgvCmp.Rows.Add(r.ComplaintID, r.OrderID, r.CustomerName, r.ComplaintDescription, r.ComplaintStatus);
                dgvRtn.Rows.Clear();
                foreach (var r in vm.Returns)
                    dgvRtn.Rows.Add(r.ReturnID, r.OrderID, r.CustomerName, r.Reason, $"HK$ {r.RefundAmount:N2}", r.ReturnStatus);

                // ── Chart data ────────────────────────────────────────────
                var cmpCounts = new Dictionary<string, double>();
                foreach (var r in vm.Complaints)
                {
                    if (!cmpCounts.ContainsKey(r.ComplaintStatus)) cmpCounts[r.ComplaintStatus] = 0;
                    cmpCounts[r.ComplaintStatus]++;
                }
                var cmpData = new List<(string, double)>();
                foreach (var kv in cmpCounts) cmpData.Add((kv.Key, kv.Value));

                var rtnCounts = new Dictionary<string, double>();
                foreach (var r in vm.Returns)
                {
                    if (!rtnCounts.ContainsKey(r.ReturnStatus)) rtnCounts[r.ReturnStatus] = 0;
                    rtnCounts[r.ReturnStatus]++;
                }
                var rtnData = new List<(string, double)>();
                foreach (var kv in rtnCounts) rtnData.Add((kv.Key, kv.Value));

                chartCmp = ChartRenderer.CreateDonutChart(cmpData, "Complaint Status");
                chartRtn = ChartRenderer.CreateDonutChart(rtnData, "Return Status");

                RefreshChartView(_afterServiceChart, dgvCmp, chartCmp, dgvRtn, chartRtn);
            };

            btnApply.Click += (s, e) => load();
            btnReset.Click += (s, e) => { cboCmp.SelectedIndex = 0; cboRtn.SelectedIndex = 0; load(); };
            btnChart.Click += (s, e) =>
            {
                _afterServiceChart = true;
                btnChart.BackColor = Palette.Primary; btnChart.ForeColor = Color.White;
                btnTable.BackColor = Palette.BgCard;  btnTable.ForeColor = Palette.TextMuted;
                RefreshChartView(_afterServiceChart, dgvCmp, chartCmp, dgvRtn, chartRtn);
            };
            btnTable.Click += (s, e) =>
            {
                _afterServiceChart = false;
                btnTable.BackColor = Palette.Primary; btnTable.ForeColor = Color.White;
                btnChart.BackColor = Palette.BgCard;  btnChart.ForeColor = Palette.TextMuted;
                RefreshChartView(_afterServiceChart, dgvCmp, chartCmp, dgvRtn, chartRtn);
            };
            btnExport.Click += (s, e) =>
            {
                CsvExporter.Export(dgvCmp, "Complaints");
                CsvExporter.Export(dgvRtn, "Returns");
            };
            load();

            BuildContentLayout(pnlKpi,
                BuildFilterBar(new Control[]
                {
                    MakeLabel("Complaint:"), cboCmp, MakeSpacer(10),
                    MakeLabel("Return:"),    cboRtn, MakeSpacer(10),
                    btnApply, MakeSpacer(6), btnReset,
                    MakeSpacer(16), MakeDivider(), MakeSpacer(16),
                    btnChart, MakeSpacer(4), btnTable, MakeSpacer(12), btnExport
                }),
                "Complaints",    dgvCmp, chartCmp, _afterServiceChart,
                "Return Orders", dgvRtn, chartRtn, _afterServiceChart, 210);
        }

        // ════════════════════════════════════════════════════════════════
        //  6. FINANCE OVERVIEW
        // ════════════════════════════════════════════════════════════════

        private void RenderFinance()
        {
            var dtpFrom  = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 160, Value = DateTime.Today.AddMonths(-3) };
            var dtpTo    = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 160, Value = DateTime.Today };
            var btnApply = MakePrimaryBtn("Apply", 110, 36);
            var btnReset = MakeOutlineBtn("Reset",  90, 36);
            var pnlKpi   = BuildKpiPanel();

            var btnChart  = MakeToggleBtn("📊  Chart",  120, 36, _financeChart);
            var btnTable  = MakeToggleBtn("📋  Table",  110, 36, !_financeChart);
            var btnExport = MakeExportBtn(140, 36);

            var dgv = MakeGrid();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTxnID",   HeaderText = "TRANSACTION ID",  FillWeight = 22 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colType",    HeaderText = "TYPE",            FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAmount",  HeaderText = "AMOUNT (HK$)",    FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",    HeaderText = "DATE",            FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDoc",     HeaderText = "LINKED DOCUMENT", FillWeight = 22 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDocType", HeaderText = "DOCUMENT TYPE",   FillWeight = 18 });
            dgv.CellFormatting += (s, e) => FormatStatusBadge(s, e, "colType");

            Panel chartAmounts  = null;
            Panel chartBreakdown = null;

            Action<DateTime?, DateTime?> load = (from, to) =>
            {
                var vm = _ctrl.GetFinanceReportVM(from, to);
                ApplyShell(vm, "Finance Overview");
                var k = vm.FinanceKpi;
                RefreshKpiPanel(pnlKpi, new[]
                {
                    ("Sales Rev (HK$)",  $"{k.TotalSalesRevenue:N0}",      Palette.TagGreenFg,  Palette.TagGreenBg),
                    ("Procurement (HK$)",$"{k.TotalProcurementSpend:N0}",  Palette.TagRedFg,    Palette.TagRedBg),
                    ("Refunds (HK$)",    $"{k.TotalRefunds:N0}",           Palette.TagOrangeFg, Palette.TagOrangeBg),
                    ("AR Due (HK$)",     $"{k.AROutstanding:N0}",          Palette.TagYellowFg, Palette.TagYellowBg),
                    ("AP Due (HK$)",     $"{k.APOutstanding:N0}",          Palette.TagBlueFg,   Palette.TagBlueBg),
                });
                dgv.Rows.Clear();
                foreach (var r in vm.FinanceRows)
                    dgv.Rows.Add(r.TransactionID, r.TransactionType, $"{r.Amount:N2}",
                                 r.TransactionDate.ToString("yyyy-MM-dd"), r.LinkedDocument, r.DocumentType);

                // ── Chart data ────────────────────────────────────────────
                var typeTotals = new Dictionary<string, double>();
                foreach (var r in vm.FinanceRows)
                {
                    if (!typeTotals.ContainsKey(r.TransactionType)) typeTotals[r.TransactionType] = 0;
                    typeTotals[r.TransactionType] += (double)r.Amount;
                }
                var barData = new List<(string, double)>();
                foreach (var kv in typeTotals) barData.Add((kv.Key, kv.Value));

                var breakdownData = new List<(string, double)>
                {
                    ("Sales Revenue",       (double)k.TotalSalesRevenue),
                    ("Procurement Spend",   (double)k.TotalProcurementSpend),
                    ("Refunds",             (double)k.TotalRefunds),
                    ("AR Outstanding",      (double)k.AROutstanding),
                    ("AP Outstanding",      (double)k.APOutstanding),
                };
                breakdownData.RemoveAll(x => x.Item2 <= 0);

                chartAmounts   = ChartRenderer.CreateBarChart(barData, "Transaction Amounts by Type (HK$)", "HK$", "N0", Palette.Primary);
                chartBreakdown = ChartRenderer.CreateDonutChart(breakdownData, "Revenue Breakdown");

                RefreshChartView(_financeChart, dgv, chartAmounts, null, chartBreakdown);
            };

            btnApply.Click += (s, e) => load(dtpFrom.Value, dtpTo.Value);
            btnReset.Click += (s, e) => { dtpFrom.Value = DateTime.Today.AddMonths(-3); dtpTo.Value = DateTime.Today; load(null, null); };
            btnChart.Click += (s, e) =>
            {
                _financeChart = true;
                btnChart.BackColor = Palette.Primary; btnChart.ForeColor = Color.White;
                btnTable.BackColor = Palette.BgCard;  btnTable.ForeColor = Palette.TextMuted;
                RefreshChartView(_financeChart, dgv, chartAmounts, null, chartBreakdown);
            };
            btnTable.Click += (s, e) =>
            {
                _financeChart = false;
                btnTable.BackColor = Palette.Primary; btnTable.ForeColor = Color.White;
                btnChart.BackColor = Palette.BgCard;  btnChart.ForeColor = Palette.TextMuted;
                RefreshChartView(_financeChart, dgv, chartAmounts, null, chartBreakdown);
            };
            btnExport.Click += (s, e) => CsvExporter.Export(dgv, "FinanceOverview");
            load(null, null);

            BuildContentLayout(pnlKpi,
                BuildFilterBar(new Control[] { MakeLabel("From:"), dtpFrom, MakeSpacer(8), MakeLabel("To:"), dtpTo, MakeSpacer(12), btnApply, MakeSpacer(6), btnReset, MakeSpacer(16), MakeDivider(), MakeSpacer(16), btnChart, MakeSpacer(4), btnTable, MakeSpacer(12), btnExport }),
                "Transactions", dgv, chartAmounts, _financeChart,
                null, null, null, false, 0);
        }

        // ════════════════════════════════════════════════════════════════
        //  CHART / TABLE SWITCHER HELPER
        //  Swaps visibility between a DataGridView and its chart Panel
        //  in-place (no layout rebuild needed).
        // ════════════════════════════════════════════════════════════════

        private static void RefreshChartView(
            bool showChart,
            DataGridView dgv1, Panel chart1,
            DataGridView dgv2, Panel chart2)
        {
            if (dgv1 != null)   dgv1.Visible   = !showChart;
            if (chart1 != null) chart1.Visible =  showChart;
            if (dgv2 != null)   dgv2.Visible   = !showChart;
            if (chart2 != null) chart2.Visible =  showChart;
        }

        // ════════════════════════════════════════════════════════════════
        //  LAYOUT BUILDER  (extended to support chart panels)
        // ════════════════════════════════════════════════════════════════

        private void BuildContentLayout(
            Panel pnlKpi, Panel filterBar,
            string title1, DataGridView grid1, Panel chartPanel1, bool showChart1,
            string title2, DataGridView grid2, Panel chartPanel2, bool showChart2,
            int grid2Height)
        {
            bool hasSecondary = !string.IsNullOrEmpty(title2) && grid2Height > 0;

            // ── CARD D — secondary grid/chart (DockStyle.Bottom) ─────────
            if (hasSecondary)
            {
                var outer = new Panel { Dock = DockStyle.Bottom, Height = grid2Height + 62, BackColor = Palette.BgPage, Padding = new Padding(0, 0, 0, 10) };
                var inner = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgCard };
                inner.Paint += PaintCardBorder;
                var tbl = MakeCardTbl(title2);

                // Grid
                if (grid2 != null)
                {
                    grid2.Dock    = DockStyle.Fill;
                    grid2.Visible = !showChart2;
                    tbl.Controls.Add(grid2, 0, 1);
                }

                // Chart
                if (chartPanel2 != null)
                {
                    chartPanel2.Dock    = DockStyle.Fill;
                    chartPanel2.Visible = showChart2;
                    tbl.Controls.Add(chartPanel2, 0, 1);
                }

                inner.Controls.Add(tbl);
                outer.Controls.Add(inner);
                pnlContent.Controls.Add(outer);
            }

            // ── CARD C — main grid/chart (DockStyle.Fill) ────────────────
            var cOuter = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage, Padding = new Padding(0, 0, 0, hasSecondary ? 0 : 10) };
            var cInner = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgCard };
            cInner.Paint += PaintCardBorder;
            var cTbl = MakeCardTbl(title1);

            if (grid1 != null)
            {
                grid1.Dock    = DockStyle.Fill;
                grid1.Visible = !showChart1;
                cTbl.Controls.Add(grid1, 0, 1);
            }

            if (chartPanel1 != null)
            {
                chartPanel1.Dock    = DockStyle.Fill;
                chartPanel1.Visible = showChart1;
                cTbl.Controls.Add(chartPanel1, 0, 1);
            }

            cInner.Controls.Add(cTbl);
            cOuter.Controls.Add(cInner);
            pnlContent.Controls.Add(cOuter);

            // ── CARD B — filter bar (DockStyle.Top) ──────────────────────
            var bOuter = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Palette.BgPage, Padding = new Padding(0, 0, 0, 8) };
            var bInner = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgCard };
            bInner.Paint += PaintCardBorder;
            filterBar.Dock = DockStyle.Fill;
            bInner.Controls.Add(filterBar);
            bOuter.Controls.Add(bInner);
            pnlContent.Controls.Add(bOuter);

            // ── CARD A — KPI pills (DockStyle.Top — added last = topmost) ─
            var aOuter = new Panel { Dock = DockStyle.Top, Height = 86, BackColor = Palette.BgPage, Padding = new Padding(0, 0, 0, 8) };
            var aInner = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgCard };
            aInner.Paint += PaintCardBorder;
            pnlKpi.Dock = DockStyle.Fill;
            aInner.Controls.Add(pnlKpi);
            aOuter.Controls.Add(aInner);
            pnlContent.Controls.Add(aOuter);
        }

        private static TableLayoutPanel MakeCardTbl(string title)
        {
            var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None, Padding = new Padding(14, 8, 14, 8) };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            var hdr = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            hdr.Controls.Add(new Label { Text = title ?? string.Empty, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Palette.Primary, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft });
            hdr.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Palette.BorderColor });
            tbl.Controls.Add(hdr, 0, 0);
            return tbl;
        }

        // ════════════════════════════════════════════════════════════════
        //  SHARED HELPERS
        // ════════════════════════════════════════════════════════════════

        private void ApplyShell(ViewReportViewModel vm, string subTitle)
        {
            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb($"Statistical Reports  \u203a  {subTitle}");
        }

        private static Panel BuildKpiPanel()
            => new Panel { BackColor = Color.Transparent, Dock = DockStyle.Fill, Padding = new Padding(12, 10, 12, 10) };

        private static void RefreshKpiPanel(Panel pnlKpi, (string label, string value, Color fg, Color bg)[] pills)
        {
            pnlKpi.Controls.Clear();
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Color.Transparent };
            const int W = 206; const int H = 58; const int G = 8;
            foreach (var (label, value, fg, bg) in pills)
            {
                var pill = new Panel { BackColor = bg, Size = new Size(W, H), Margin = new Padding(0, 0, G, 0) };
                pill.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path  = RoundedRect(((Panel)s).ClientRectangle, 8);
                    using var brush = new SolidBrush(((Panel)s).BackColor);
                    e.Graphics.FillPath(brush, path);
                };
                var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None, Padding = new Padding(8, 0, 6, 0) };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72f));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                tlp.Controls.Add(new Label { Text = value, Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = fg, BackColor = Color.Transparent, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, AutoSize = false }, 0, 0);
                tlp.Controls.Add(new Label { Text = label, Font = new Font("Segoe UI", 10f),               ForeColor = fg, BackColor = Color.Transparent, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,  AutoSize = false }, 1, 0);
                pill.Controls.Add(tlp);
                flow.Controls.Add(pill);
            }
            pnlKpi.Controls.Add(flow);
        }

        private static Panel BuildFilterBar(Control[] controls)
        {
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Color.Transparent, Padding = new Padding(14, 0, 14, 0) };
            foreach (var c in controls) { c.Margin = new Padding(0, 0, 4, 0); if (!(c is Label || c is CheckBox)) c.Height = 36; flow.Controls.Add(c); }
            var w = new Panel { BackColor = Color.Transparent, Dock = DockStyle.Fill };
            w.Controls.Add(flow);
            w.Layout += (s, e) => { var p = (Panel)s; flow.Top = (p.Height - flow.PreferredSize.Height) / 2; };
            return w;
        }

        private static DataGridView MakeGrid()
        {
            var g = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true,
                AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                BackgroundColor = Palette.BgCard, BorderStyle = BorderStyle.None, GridColor = Palette.BorderColor,
                Font = new Font("Segoe UI", 11f), AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 40,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255), ForeColor = Palette.TextMuted,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold), Padding = new Padding(10, 0, 0, 0),
                    Alignment = DataGridViewContentAlignment.MiddleLeft
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Palette.BgCard, ForeColor = Palette.TextMain,
                    SelectionBackColor = Palette.TagBlueBg, SelectionForeColor = Palette.TextMain,
                    Padding = new Padding(10, 4, 10, 4)
                }
            };
            g.RowTemplate.Height = 42;
            return g;
        }

        private static void FormatStatusBadge(object sender, DataGridViewCellFormattingEventArgs e, string colName)
        {
            if (e.ColumnIndex < 0 || e.RowIndex < 0 || e.Value == null) return;
            if (((DataGridView)sender).Columns[e.ColumnIndex].Name != colName) return;
            var (bg, fg) = Palette.TagColours(e.Value.ToString());
            e.CellStyle.BackColor = bg; e.CellStyle.ForeColor = fg;
            e.CellStyle.SelectionBackColor = bg; e.CellStyle.SelectionForeColor = fg;
            e.CellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            e.FormattingApplied = true;
        }

        // ── Button factories ─────────────────────────────────────────────
        private static Label   MakeLabel(string t)   => new Label { Text = t, Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = Palette.TextMuted, BackColor = Color.Transparent, AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
        private static Panel   MakeSpacer(int w)     => new Panel { Width = w, BackColor = Color.Transparent };
        private static Panel   MakeDivider()         => new Panel { Width = 1, BackColor = Palette.BorderColor };
        private static ComboBox MakeCbo(string[] items) { var c = new ComboBox { Font = new Font("Segoe UI", 11f), DropDownStyle = ComboBoxStyle.DropDownList, Width = 185 }; c.Items.AddRange(items); c.SelectedIndex = 0; return c; }

        private static Button MakePrimaryBtn(string t, int w, int h)
        {
            var b = new Button { Text = t, Font = new Font("Segoe UI", 11f), ForeColor = Color.White, BackColor = Palette.Primary, FlatStyle = FlatStyle.Flat, Size = new Size(w, h), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0; b.FlatAppearance.MouseOverBackColor = Palette.PrimaryDark; return b;
        }
        private static Button MakeOutlineBtn(string t, int w, int h)
        {
            var b = new Button { Text = t, Font = new Font("Segoe UI", 11f), ForeColor = Palette.TextMuted, BackColor = Palette.BgCard, FlatStyle = FlatStyle.Flat, Size = new Size(w, h), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderColor = Palette.BorderColor; b.FlatAppearance.BorderSize = 1; b.FlatAppearance.MouseOverBackColor = Palette.BgPage; return b;
        }

        /// <summary>Toggle button — active state = solid blue, inactive = outline.</summary>
        private static Button MakeToggleBtn(string t, int w, int h, bool active)
        {
            var b = new Button { Text = t, Font = new Font("Segoe UI", 11f), Size = new Size(w, h), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            if (active) { b.BackColor = Palette.Primary; b.ForeColor = Color.White; b.FlatAppearance.BorderSize = 0; }
            else        { b.BackColor = Palette.BgCard;  b.ForeColor = Palette.TextMuted; b.FlatAppearance.BorderColor = Palette.BorderColor; b.FlatAppearance.BorderSize = 1; }
            b.FlatAppearance.MouseOverBackColor = active ? Palette.PrimaryDark : Palette.BgPage;
            return b;
        }

        /// <summary>Export CSV button — always outline style with download icon.</summary>
        private static Button MakeExportBtn(int w, int h)
        {
            var b = new Button { Text = "⬇  Export CSV", Font = new Font("Segoe UI", 11f), ForeColor = Palette.TagGreenFg, BackColor = Palette.TagGreenBg, FlatStyle = FlatStyle.Flat, Size = new Size(w, h), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderColor = Palette.TagGreenFg; b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 252, 231);
            return b;
        }

        private void OnTopNavMenuItemClicked(string menuLabel, string subItem) => FormNavigator.NavigateTo(this, menuLabel, subItem);
        private void BtnLogout_Click(object sender, EventArgs e) { SessionManager.Clear(); Application.Restart(); }

        private static void PaintCardBorder(object s, PaintEventArgs e)
        {
            var p = (Panel)s;
            using var pen = new Pen(Palette.BorderColor, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        }
        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath(); int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90); path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure(); return path;
        }
    }
}
