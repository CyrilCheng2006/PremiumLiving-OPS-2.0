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
    /// View — Statistical Reports  ›  View Report
    ///
    /// Rendering baseline: ViewOrderForm (Order Processing › View Order)
    /// ─ KPI pills:  290 × 60, rounded, clickable, identical to ViewOrderForm.RefreshKpi()
    /// ─ Filter bar: single-row FlowLayout (search + DatePicker + Apply/Reset + divider + Chart/Table/Export)
    /// ─ DataGridView: identical header / cell / row style to ViewOrderForm.dgvOrders
    ///
    /// MVC role  : View only — all DB access via StatisticalReportsController.
    /// AppShell  : mandatory chrome (Rule 1-5).
    /// CardPanel : every content block in 3-layer nested cards.
    ///
    /// Tab navigation: 6 tabs mapped by index (0-5).
    ///   0 = Sales Performance
    ///   1 = Inventory Status
    ///   2 = Procurement Summary
    ///   3 = Logistics Overview
    ///   4 = After-Service Summary
    ///   5 = Finance Overview
    /// </summary>
    public partial class ViewReportForm : Form
    {
        private readonly StatisticalReportsController _ctrl = new StatisticalReportsController();

        // Active tab index (-1 = none loaded yet)
        private int _activeTab = -1;

        // Per-report chart toggle state
        private bool _salesChart        = false;
        private bool _inventoryChart    = false;
        private bool _procurementChart  = false;
        private bool _logisticsChart    = false;
        private bool _afterServiceChart = false;
        private bool _financeChart      = false;

        // Tab button array — matches Designer btnTab0..btnTab5
        private Button[] _tabButtons;

        // ── ViewOrderForm-style status colours (for badge cells) ──────────
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

            // Build tab array from Designer fields (index == tab number)
            _tabButtons = new Button[] { btnTab0, btnTab1, btnTab2, btnTab3, btnTab4, btnTab5 };

            this.Load += (s, e) => SwitchToReport(0);
        }

        // ════════════════════════════════════════════════════════════════
        //  REPORT SWITCHER  (index-based)
        // ════════════════════════════════════════════════════════════════

        private void SwitchToReport(int tabIndex)
        {
            if (_activeTab == tabIndex && pnlContent.Controls.Count > 0) return;
            _activeTab = tabIndex;

            pnlContent.SuspendLayout();
            pnlContent.Controls.Clear();
            HighlightTab(tabIndex);

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
        //  TAB HIGHLIGHT  (index loop, no dictionary)
        // ════════════════════════════════════════════════════════════════

        private void HighlightTab(int activeIndex)
        {
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                bool active = i == activeIndex;
                _tabButtons[i].ForeColor  = active ? Palette.Primary              : Color.FromArgb(98, 112, 135);
                _tabButtons[i].Font       = active ? new Font("Segoe UI", 12f, FontStyle.Bold)
                                                   : new Font("Segoe UI", 12f);
                _tabButtons[i].BackColor  = active ? Color.FromArgb(235, 241, 255) : Color.Transparent;
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  REPORT RENDERS
        // ════════════════════════════════════════════════════════════════

        private void RenderSales()
        {
            // ── Filter controls ──────────────────────────────────────────
            var dtpFrom  = MakeDatePicker(DateTime.Today.AddMonths(-3));
            var dtpTo    = MakeDatePicker(DateTime.Today);
            var chkDate  = new CheckBox { Text = "From:", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = Color.FromArgb(98, 112, 135), BackColor = Color.Transparent, AutoSize = true };
            var btnApply = MakePrimaryBtn("Apply",  110, 40);
            var btnReset = MakeOutlineBtn("Reset",   90, 40);

            // ── Chart / Table / Export ───────────────────────────────────
            var btnChart  = MakeToggleBtn("\U0001F4CA  Chart",  130, 40, _salesChart);
            var btnTable  = MakeToggleBtn("\U0001F4CB  Table",  120, 40, !_salesChart);
            var btnExport = MakeExportBtn(150, 40);

            // ── KPI strip ────────────────────────────────────────────────
            var pnlKpi = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(4, 0, 4, 0) };

            // ── Main grid ────────────────────────────────────────────────
            var dgv = MakeGrid();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrderID",  HeaderText = "ORDER ID",    FillWeight = 18 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomer", HeaderText = "CUSTOMER",    FillWeight = 22 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",   HeaderText = "STATUS",      FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",     HeaderText = "ORDER DATE",  FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal",    HeaderText = "GRAND TOTAL", FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLines",    HeaderText = "ITEMS",       FillWeight =  8 });
            dgv.CellFormatting += (s, e) => ApplyStatusBadge(s, e, "colStatus");

            // ── Secondary grid: Top Products ─────────────────────────────
            var dgvTop = MakeGrid();
            dgvTop.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItemID",  HeaderText = "ITEM ID",   FillWeight = 15 });
            dgvTop.Columns.Add(new DataGridViewTextBoxColumn { Name = "colProduct", HeaderText = "PRODUCT",   FillWeight = 32 });
            dgvTop.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCat",     HeaderText = "CATEGORY",  FillWeight = 14 });
            dgvTop.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQty",     HeaderText = "TOTAL QTY", FillWeight = 14 });
            dgvTop.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRev",     HeaderText = "REVENUE",   FillWeight = 20 });

            Panel chartMain = null;
            Panel chartTop  = null;

            Action<DateTime?, DateTime?> load = (from, to) =>
            {
                var vm = _ctrl.GetSalesReportVM(from, to);
                ApplyShell(vm, "Sales Performance");
                var k = vm.SalesKpi;

                BuildKpiPills(pnlKpi, new[]
                {
                    ("Total Orders",    k.TotalOrders.ToString(),      Color.FromArgb( 47, 111, 237), Color.FromArgb(219, 234, 254), (string)null),
                    ("Revenue (HK$)",   $"{k.TotalRevenue:N0}",        Color.FromArgb(  6,  95,  70), Color.FromArgb(209, 250, 229), (string)null),
                    ("Avg Order",       $"HK$ {k.AverageOrderValue:N0}",Color.FromArgb( 29,  78, 216), Color.FromArgb(219, 234, 254), (string)null),
                    ("Delivered",       k.DeliveredOrders.ToString(),   Color.FromArgb(  6,  95,  70), Color.FromArgb(209, 250, 229), (string)null),
                    ("Processing",      k.ProcessingOrders.ToString(),  Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199), (string)null),
                    ("Pending",         k.PendingOrders.ToString(),     Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199), (string)null),
                });

                dgv.Rows.Clear();
                foreach (var r in vm.SalesRows)
                    dgv.Rows.Add(r.OrderID, r.CustomerName, r.OrderStatus,
                                 r.IssuedTime.ToString("yyyy-MM-dd"), $"HK$ {r.GrandTotal:N2}", r.LineCount);

                dgvTop.Rows.Clear();
                foreach (var p in vm.TopProducts)
                    dgvTop.Rows.Add(p.ItemID, p.ItemName, p.Category, p.TotalQty, $"HK$ {p.TotalRevenue:N2}");

                var statusTotals = new Dictionary<string, double>();
                foreach (var r in vm.SalesRows)
                {
                    if (!statusTotals.ContainsKey(r.OrderStatus)) statusTotals[r.OrderStatus] = 0;
                    statusTotals[r.OrderStatus] += (double)r.GrandTotal;
                }
                var barData = new List<(string, double)>();
                foreach (var kv in statusTotals) barData.Add((kv.Key, kv.Value));

                var topData = new List<(string, double)>();
                foreach (var p in vm.TopProducts)
                    topData.Add((p.ItemName.Length > 18 ? p.ItemName.Substring(0, 16) + "\u2026" : p.ItemName, (double)p.TotalRevenue));

                chartMain = ChartRenderer.CreateBarChart(barData, "Revenue by Order Status", "HK$", "N0", Palette.Primary);
                chartTop  = ChartRenderer.CreateHorizontalBarChart(topData, "Top Products by Revenue", "N0", Palette.Primary);

                ToggleChartTable(_salesChart, dgv, chartMain, dgvTop, chartTop);
            };

            chkDate.CheckedChanged += (s, e) => dtpFrom.Enabled = chkDate.Checked;
            dtpFrom.Enabled = false;
            btnApply.Click  += (s, e) => load(chkDate.Checked ? (DateTime?)dtpFrom.Value : null, dtpTo.Value);
            btnReset.Click  += (s, e) => { chkDate.Checked = false; dtpFrom.Value = DateTime.Today.AddMonths(-3); dtpTo.Value = DateTime.Today; load(null, null); };
            btnChart.Click  += (s, e) => { _salesChart = true;  FlipToggle(btnChart, btnTable, true);  ToggleChartTable(_salesChart, dgv, chartMain, dgvTop, chartTop); };
            btnTable.Click  += (s, e) => { _salesChart = false; FlipToggle(btnChart, btnTable, false); ToggleChartTable(_salesChart, dgv, chartMain, dgvTop, chartTop); };
            btnExport.Click += (s, e) => CsvExporter.Export(dgv, "SalesPerformance");

            load(null, null);

            BuildLayout(
                pnlKpi,
                BuildFilterRow(chkDate, dtpFrom, MakeLabel("To:"), dtpTo, btnApply, btnReset, null, btnChart, btnTable, btnExport),
                "Orders",                  dgv,    chartMain, _salesChart,
                "Top Products by Revenue", dgvTop, chartTop,  _salesChart, 230);
        }

        // ════════════════════════════════════════════════════════════════
        //  INVENTORY STATUS
        // ════════════════════════════════════════════════════════════════

        private void RenderInventory()
        {
            var cboCat     = MakeCbo(new[] { "All", "Product", "Raw Material" }, 185);
            var chkReorder = new CheckBox { Text = "Below Reorder Only", Font = new Font("Segoe UI", 11f), ForeColor = Color.FromArgb(98, 112, 135), BackColor = Color.Transparent, AutoSize = true };
            var btnApply   = MakePrimaryBtn("Apply", 110, 40);
            var btnReset   = MakeOutlineBtn("Reset",  90, 40);
            var btnChart   = MakeToggleBtn("\U0001F4CA  Chart", 130, 40, _inventoryChart);
            var btnTable   = MakeToggleBtn("\U0001F4CB  Table", 120, 40, !_inventoryChart);
            var btnExport  = MakeExportBtn(150, 40);

            var pnlKpi = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(4, 0, 4, 0) };

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

            Panel chartStock = null; Panel chartCategory = null;

            Action load = () =>
            {
                var vm = _ctrl.GetInventoryReportVM(cboCat.SelectedItem?.ToString(), chkReorder.Checked);
                ApplyShell(vm, "Inventory Status");
                var k = vm.InventoryKpi;

                BuildKpiPills(pnlKpi, new[]
                {
                    ("Total SKUs",    k.TotalSKUs.ToString(),         Color.FromArgb( 47, 111, 237), Color.FromArgb(219, 234, 254), (string)null),
                    ("Products",      k.ProductCount.ToString(),      Color.FromArgb( 29,  78, 216), Color.FromArgb(219, 234, 254), (string)null),
                    ("Raw Materials", k.RawMaterialCount.ToString(),  Color.FromArgb(  6,  95,  70), Color.FromArgb(209, 250, 229), (string)null),
                    ("Below Reorder", k.BelowReorderCount.ToString(), Color.FromArgb(185,  28,  28), Color.FromArgb(254, 226, 226), (string)null),
                });

                dgv.Rows.Clear();
                foreach (var r in vm.InventoryRows)
                    dgv.Rows.Add(r.WarehouseItemID, $"{r.ItemID}  —  {r.ItemName}",
                                 r.ItemCategory, string.IsNullOrEmpty(r.MaterialType) ? "—" : r.MaterialType,
                                 r.WarehouseLocation, r.CurrentStock, r.ReorderLevel,
                                 r.BelowReorder ? "Low Stock" : "OK");

                var stockData = new List<(string, double)>();
                foreach (var r in vm.InventoryRows) stockData.Add(($"{r.ItemID}", (double)r.CurrentStock));
                if (stockData.Count > 10) stockData = stockData.GetRange(0, 10);

                var catTotals = new Dictionary<string, double>();
                foreach (var r in vm.InventoryRows)
                {
                    if (!catTotals.ContainsKey(r.ItemCategory)) catTotals[r.ItemCategory] = 0;
                    catTotals[r.ItemCategory] += r.CurrentStock;
                }
                var donutData = new List<(string, double)>();
                foreach (var kv in catTotals) donutData.Add((kv.Key, kv.Value));

                chartStock    = ChartRenderer.CreateHorizontalBarChart(stockData, "Stock Levels (Top 10)", "N0", Palette.Primary);
                chartCategory = ChartRenderer.CreateDonutChart(donutData, "Stock by Category");

                ToggleChartTable(_inventoryChart, dgv, chartStock, null, chartCategory);
            };

            btnApply.Click += (s, e) => load();
            btnReset.Click += (s, e) => { cboCat.SelectedIndex = 0; chkReorder.Checked = false; load(); };
            btnChart.Click += (s, e) => { _inventoryChart = true;  FlipToggle(btnChart, btnTable, true);  ToggleChartTable(_inventoryChart, dgv, chartStock, null, chartCategory); };
            btnTable.Click += (s, e) => { _inventoryChart = false; FlipToggle(btnChart, btnTable, false); ToggleChartTable(_inventoryChart, dgv, chartStock, null, chartCategory); };
            btnExport.Click += (s, e) => CsvExporter.Export(dgv, "InventoryStatus");
            load();

            BuildLayout(
                pnlKpi,
                BuildFilterRow(MakeLabel("Category:"), cboCat, chkReorder, null, btnApply, btnReset, null, btnChart, btnTable, btnExport),
                "Inventory Detail", dgv, chartStock, _inventoryChart,
                null, null, null, false, 0);
        }

        // ════════════════════════════════════════════════════════════════
        //  PROCUREMENT SUMMARY
        // ════════════════════════════════════════════════════════════════

        private void RenderProcurement()
        {
            var cboStatus = MakeCbo(new[] { "All", "Sent", "Partially Received", "Received", "Completed", "Cancelled" }, 200);
            var btnApply  = MakePrimaryBtn("Apply", 110, 40);
            var btnReset  = MakeOutlineBtn("Reset",  90, 40);
            var btnChart  = MakeToggleBtn("\U0001F4CA  Chart", 130, 40, _procurementChart);
            var btnTable  = MakeToggleBtn("\U0001F4CB  Table", 120, 40, !_procurementChart);
            var btnExport = MakeExportBtn(150, 40);

            var pnlKpi = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(4, 0, 4, 0) };

            var dgv = MakeGrid();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPOID",     HeaderText = "PO ID",      FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSupplier", HeaderText = "SUPPLIER",   FillWeight = 22 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",   HeaderText = "STATUS",     FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",     HeaderText = "ORDER DATE", FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAmount",   HeaderText = "PO AMOUNT",  FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMat",      HeaderText = "MATERIALS",  FillWeight = 24 });
            dgv.CellFormatting += (s, e) => ApplyStatusBadge(s, e, "colStatus");

            Panel chartSupplier = null; Panel chartStatus = null;

            Action load = () =>
            {
                var vm = _ctrl.GetProcurementReportVM(cboStatus.SelectedItem?.ToString());
                ApplyShell(vm, "Procurement Summary");
                var k = vm.ProcKpi;

                BuildKpiPills(pnlKpi, new[]
                {
                    ("Total POs",   k.TotalPOs.ToString(),        Color.FromArgb( 47, 111, 237), Color.FromArgb(219, 234, 254), (string)null),
                    ("Spend (HK$)", $"{k.TotalSpend:N0}",         Color.FromArgb(185,  28,  28), Color.FromArgb(254, 226, 226), (string)null),
                    ("Completed",   k.CompletedPOs.ToString(),    Color.FromArgb(  6,  95,  70), Color.FromArgb(209, 250, 229), (string)null),
                    ("Pending",     k.PendingPOs.ToString(),      Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199), (string)null),
                    ("Suppliers",   k.UniqueSuppliers.ToString(), Color.FromArgb( 29,  78, 216), Color.FromArgb(219, 234, 254), (string)null),
                });

                dgv.Rows.Clear();
                foreach (var r in vm.ProcRows)
                    dgv.Rows.Add(r.PurchaseID, r.SupplierName, r.PurchaseStatus,
                                 r.OrderDate.ToString("yyyy-MM-dd"), $"HK$ {r.POTotalAmount:N2}", r.MaterialNames);

                var supplierSpend = new Dictionary<string, double>();
                foreach (var r in vm.ProcRows)
                {
                    if (!supplierSpend.ContainsKey(r.SupplierName)) supplierSpend[r.SupplierName] = 0;
                    supplierSpend[r.SupplierName] += (double)r.POTotalAmount;
                }
                var supplierData = new List<(string, double)>();
                foreach (var kv in supplierSpend) supplierData.Add((kv.Key, kv.Value));

                var statusCounts = new Dictionary<string, double>();
                foreach (var r in vm.ProcRows)
                {
                    if (!statusCounts.ContainsKey(r.PurchaseStatus)) statusCounts[r.PurchaseStatus] = 0;
                    statusCounts[r.PurchaseStatus]++;
                }
                var statusData = new List<(string, double)>();
                foreach (var kv in statusCounts) statusData.Add((kv.Key, kv.Value));

                chartSupplier = ChartRenderer.CreateBarChart(supplierData, "Spend by Supplier (HK$)", "HK$", "N0", Palette.Primary);
                chartStatus   = ChartRenderer.CreateDonutChart(statusData, "PO Status Breakdown");

                ToggleChartTable(_procurementChart, dgv, chartSupplier, null, chartStatus);
            };

            btnApply.Click += (s, e) => load();
            btnReset.Click += (s, e) => { cboStatus.SelectedIndex = 0; load(); };
            btnChart.Click += (s, e) => { _procurementChart = true;  FlipToggle(btnChart, btnTable, true);  ToggleChartTable(_procurementChart, dgv, chartSupplier, null, chartStatus); };
            btnTable.Click += (s, e) => { _procurementChart = false; FlipToggle(btnChart, btnTable, false); ToggleChartTable(_procurementChart, dgv, chartSupplier, null, chartStatus); };
            btnExport.Click += (s, e) => CsvExporter.Export(dgv, "ProcurementSummary");
            load();

            BuildLayout(
                pnlKpi,
                BuildFilterRow(MakeLabel("Status:"), cboStatus, null, null, btnApply, btnReset, null, btnChart, btnTable, btnExport),
                "Purchase Orders", dgv, chartSupplier, _procurementChart,
                null, null, null, false, 0);
        }

        // ════════════════════════════════════════════════════════════════
        //  LOGISTICS OVERVIEW
        // ════════════════════════════════════════════════════════════════

        private void RenderLogistics()
        {
            var cboStatus = MakeCbo(new[] { "All", "Pending", "In Transit", "Completed" }, 185);
            var btnApply  = MakePrimaryBtn("Apply", 110, 40);
            var btnReset  = MakeOutlineBtn("Reset",  90, 40);
            var btnChart  = MakeToggleBtn("\U0001F4CA  Chart", 130, 40, _logisticsChart);
            var btnTable  = MakeToggleBtn("\U0001F4CB  Table", 120, 40, !_logisticsChart);
            var btnExport = MakeExportBtn(150, 40);

            var pnlKpi = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(4, 0, 4, 0) };

            var dgv = MakeGrid();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colShipID",  HeaderText = "SHIPMENT ID", FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrder",   HeaderText = "ORDER ID",    FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCust",    HeaderText = "CUSTOMER",    FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",  HeaderText = "STATUS",      FillWeight = 13 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colType",    HeaderText = "TYPE",        FillWeight = 10 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMethod",  HeaderText = "METHOD",      FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",    HeaderText = "SHIP DATE",   FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDN",      HeaderText = "D.NOTE",      FillWeight =  9 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRS",      HeaderText = "REPLY SLIP",  FillWeight =  8 });
            dgv.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex < 0 || e.RowIndex < 0 || e.Value == null) return;
                var gv  = (DataGridView)s;
                string col = gv.Columns[e.ColumnIndex].Name;
                if (col == "colStatus") { ApplyStatusBadge(s, e, "colStatus"); return; }
                if (col == "colDN" || col == "colRS")
                {
                    bool yes = e.Value.ToString() == "Yes";
                    e.CellStyle.ForeColor          = yes ? Color.FromArgb(  6,  95,  70) : Color.FromArgb(185, 28, 28);
                    e.CellStyle.BackColor          = yes ? Color.FromArgb(209, 250, 229) : Color.FromArgb(254, 226, 226);
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

                BuildKpiPills(pnlKpi, new[]
                {
                    ("Total",       k.TotalShipments.ToString(), Color.FromArgb( 47, 111, 237), Color.FromArgb(219, 234, 254), (string)null),
                    ("Completed",   k.Completed.ToString(),      Color.FromArgb(  6,  95,  70), Color.FromArgb(209, 250, 229), (string)null),
                    ("In Transit",  k.InTransit.ToString(),      Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199), (string)null),
                    ("Pending",     k.Pending.ToString(),        Color.FromArgb(185,  28,  28), Color.FromArgb(254, 226, 226), (string)null),
                    ("Reply Slips", k.WithReplySlip.ToString(),  Color.FromArgb( 29,  78, 216), Color.FromArgb(219, 234, 254), (string)null),
                });

                dgv.Rows.Clear();
                foreach (var r in vm.LogRows)
                    dgv.Rows.Add(r.ShipmentID, r.OrderID, r.CustomerName, r.ShipmentStatus,
                                 r.ShipmentType, r.DeliveryMethod, r.ShipDate.ToString("yyyy-MM-dd"),
                                 r.HasDeliveryNote ? "Yes" : "No", r.HasReplySlip ? "Yes" : "No");

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

            btnApply.Click += (s, e) => load();
            btnReset.Click += (s, e) => { cboStatus.SelectedIndex = 0; load(); };
            btnChart.Click += (s, e) => { _logisticsChart = true;  FlipToggle(btnChart, btnTable, true);  ToggleChartTable(_logisticsChart, dgv, chartStatus, null, null); };
            btnTable.Click += (s, e) => { _logisticsChart = false; FlipToggle(btnChart, btnTable, false); ToggleChartTable(_logisticsChart, dgv, chartStatus, null, null); };
            btnExport.Click += (s, e) => CsvExporter.Export(dgv, "LogisticsOverview");
            load();

            BuildLayout(
                pnlKpi,
                BuildFilterRow(MakeLabel("Status:"), cboStatus, null, null, btnApply, btnReset, null, btnChart, btnTable, btnExport),
                "Shipments", dgv, chartStatus, _logisticsChart,
                null, null, null, false, 0);
        }

        // ════════════════════════════════════════════════════════════════
        //  AFTER-SERVICE SUMMARY
        // ════════════════════════════════════════════════════════════════

        private void RenderAfterService()
        {
            var cboCmp   = MakeCbo(new[] { "All", "Pending", "Processing", "Escalated", "Completed" }, 185);
            var cboRtn   = MakeCbo(new[] { "All", "Pending", "Approved", "Processing", "Rejected", "Completed" }, 185);
            var btnApply = MakePrimaryBtn("Apply", 110, 40);
            var btnReset = MakeOutlineBtn("Reset",  90, 40);
            var btnChart = MakeToggleBtn("\U0001F4CA  Chart", 130, 40, _afterServiceChart);
            var btnTable = MakeToggleBtn("\U0001F4CB  Table", 120, 40, !_afterServiceChart);
            var btnExport = MakeExportBtn(150, 40);

            var pnlKpi = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(4, 0, 4, 0) };

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

            Panel chartCmp = null; Panel chartRtn = null;

            Action load = () =>
            {
                var vm = _ctrl.GetAfterServiceReportVM(cboCmp.SelectedItem?.ToString(), cboRtn.SelectedItem?.ToString());
                ApplyShell(vm, "After-Service Summary");
                var k = vm.AfterKpi;

                BuildKpiPills(pnlKpi, new[]
                {
                    ("Complaints",     k.TotalComplaints.ToString(), Color.FromArgb(185,  28,  28), Color.FromArgb(254, 226, 226), (string)null),
                    ("Open",           k.OpenComplaints.ToString(),  Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199), (string)null),
                    ("Returns",        k.TotalReturns.ToString(),    Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199), (string)null),
                    ("Refunded (HK$)", $"{k.TotalRefunded:N0}",      Color.FromArgb( 29,  78, 216), Color.FromArgb(219, 234, 254), (string)null),
                });

                dgvCmp.Rows.Clear();
                foreach (var r in vm.Complaints)
                    dgvCmp.Rows.Add(r.ComplaintID, r.OrderID, r.CustomerName, r.ComplaintDescription, r.ComplaintStatus);

                dgvRtn.Rows.Clear();
                foreach (var r in vm.Returns)
                    dgvRtn.Rows.Add(r.ReturnID, r.OrderID, r.CustomerName, r.Reason, $"HK$ {r.RefundAmount:N2}", r.ReturnStatus);

                var cmpCounts = new Dictionary<string, double>();
                foreach (var r in vm.Complaints) { if (!cmpCounts.ContainsKey(r.ComplaintStatus)) cmpCounts[r.ComplaintStatus] = 0; cmpCounts[r.ComplaintStatus]++; }
                var cmpData = new List<(string, double)>(); foreach (var kv in cmpCounts) cmpData.Add((kv.Key, kv.Value));

                var rtnCounts = new Dictionary<string, double>();
                foreach (var r in vm.Returns) { if (!rtnCounts.ContainsKey(r.ReturnStatus)) rtnCounts[r.ReturnStatus] = 0; rtnCounts[r.ReturnStatus]++; }
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

            BuildLayout(
                pnlKpi,
                BuildFilterRow(MakeLabel("Complaint:"), cboCmp, MakeLabel("Return:"), cboRtn, btnApply, btnReset, null, btnChart, btnTable, btnExport),
                "Complaints",    dgvCmp, chartCmp, _afterServiceChart,
                "Return Orders", dgvRtn, chartRtn, _afterServiceChart, 230);
        }

        // ════════════════════════════════════════════════════════════════
        //  FINANCE OVERVIEW
        // ════════════════════════════════════════════════════════════════

        private void RenderFinance()
        {
            var dtpFrom   = MakeDatePicker(DateTime.Today.AddMonths(-3));
            var dtpTo     = MakeDatePicker(DateTime.Today);
            var chkDate   = new CheckBox { Text = "From:", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = Color.FromArgb(98, 112, 135), BackColor = Color.Transparent, AutoSize = true };
            var btnApply  = MakePrimaryBtn("Apply",  110, 40);
            var btnReset  = MakeOutlineBtn("Reset",   90, 40);
            var btnChart  = MakeToggleBtn("\U0001F4CA  Chart", 130, 40, _financeChart);
            var btnTable  = MakeToggleBtn("\U0001F4CB  Table", 120, 40, !_financeChart);
            var btnExport = MakeExportBtn(150, 40);

            var pnlKpi = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(4, 0, 4, 0) };

            var dgv = MakeGrid();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTxnID",   HeaderText = "TRANSACTION ID",  FillWeight = 22 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colType",    HeaderText = "TYPE",            FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAmount",  HeaderText = "AMOUNT (HK$)",    FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",    HeaderText = "DATE",            FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDoc",     HeaderText = "LINKED DOCUMENT", FillWeight = 22 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDocType", HeaderText = "DOCUMENT TYPE",   FillWeight = 18 });
            dgv.CellFormatting += (s, e) => ApplyStatusBadge(s, e, "colType");

            Panel chartAmounts = null; Panel chartBreakdown = null;

            Action<DateTime?, DateTime?> load = (from, to) =>
            {
                var vm = _ctrl.GetFinanceReportVM(from, to);
                ApplyShell(vm, "Finance Overview");
                var k = vm.FinanceKpi;

                BuildKpiPills(pnlKpi, new[]
                {
                    ("Sales Rev (HK$)",   $"{k.TotalSalesRevenue:N0}",    Color.FromArgb(  6,  95,  70), Color.FromArgb(209, 250, 229), (string)null),
                    ("Procurement (HK$)", $"{k.TotalProcurementSpend:N0}",Color.FromArgb(185,  28,  28), Color.FromArgb(254, 226, 226), (string)null),
                    ("Refunds (HK$)",     $"{k.TotalRefunds:N0}",         Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199), (string)null),
                    ("AR Due (HK$)",      $"{k.AROutstanding:N0}",        Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199), (string)null),
                    ("AP Due (HK$)",      $"{k.APOutstanding:N0}",        Color.FromArgb( 29,  78, 216), Color.FromArgb(219, 234, 254), (string)null),
                });

                dgv.Rows.Clear();
                foreach (var r in vm.FinanceRows)
                    dgv.Rows.Add(r.TransactionID, r.TransactionType, $"{r.Amount:N2}",
                                 r.TransactionDate.ToString("yyyy-MM-dd"), r.LinkedDocument, r.DocumentType);

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
            dtpFrom.Enabled = false;
            btnApply.Click  += (s, e) => load(chkDate.Checked ? (DateTime?)dtpFrom.Value : null, dtpTo.Value);
            btnReset.Click  += (s, e) => { chkDate.Checked = false; dtpFrom.Value = DateTime.Today.AddMonths(-3); dtpTo.Value = DateTime.Today; load(null, null); };
            btnChart.Click  += (s, e) => { _financeChart = true;  FlipToggle(btnChart, btnTable, true);  ToggleChartTable(_financeChart, dgv, chartAmounts, null, chartBreakdown); };
            btnTable.Click  += (s, e) => { _financeChart = false; FlipToggle(btnChart, btnTable, false); ToggleChartTable(_financeChart, dgv, chartAmounts, null, chartBreakdown); };
            btnExport.Click += (s, e) => CsvExporter.Export(dgv, "FinanceOverview");
            load(null, null);

            BuildLayout(
                pnlKpi,
                BuildFilterRow(chkDate, dtpFrom, MakeLabel("To:"), dtpTo, btnApply, btnReset, null, btnChart, btnTable, btnExport),
                "Transactions", dgv, chartAmounts, _financeChart,
                null, null, null, false, 0);
        }

        // ════════════════════════════════════════════════════════════════
        //  LAYOUT BUILDER
        //  CARD A (KPI) → CARD B (Filter) → CARD C (Main grid) → CARD D (Secondary)
        // ════════════════════════════════════════════════════════════════

        private void BuildLayout(
            Panel pnlKpi, Panel filterBar,
            string title1, DataGridView grid1, Panel chart1, bool showChart1,
            string title2, DataGridView grid2, Panel chart2, bool showChart2,
            int grid2Height)
        {
            bool hasSecondary = !string.IsNullOrEmpty(title2) && grid2Height > 0;

            // ── CARD D — secondary (DockStyle.Bottom) ────────────────────
            if (hasSecondary)
            {
                var dOuter = WrapCard(DockStyle.Bottom, grid2Height + 62, 0, 0, 0, 10);
                var dInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
                dInner.Paint += PaintCardBorder;
                var dTbl = MakeCardTable(title2);
                if (grid2  != null) { grid2.Dock  = DockStyle.Fill; grid2.Visible  = !showChart2; dTbl.Controls.Add(grid2,  0, 1); }
                if (chart2 != null) { chart2.Dock = DockStyle.Fill; chart2.Visible = showChart2;  dTbl.Controls.Add(chart2, 0, 1); }
                dInner.Controls.Add(dTbl);
                dOuter.Controls.Add(dInner);
                pnlContent.Controls.Add(dOuter);
            }

            // ── CARD C — main grid (DockStyle.Fill) ──────────────────────
            var cOuter = WrapCard(DockStyle.Fill, 0, 0, 0, 0, hasSecondary ? 0 : 10);
            var cInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            cInner.Paint += PaintCardBorder;
            var cTbl = MakeCardTable(title1);
            if (grid1  != null) { grid1.Dock  = DockStyle.Fill; grid1.Visible  = !showChart1; cTbl.Controls.Add(grid1,  0, 1); }
            if (chart1 != null) { chart1.Dock = DockStyle.Fill; chart1.Visible = showChart1;  cTbl.Controls.Add(chart1, 0, 1); }
            cInner.Controls.Add(cTbl);
            cOuter.Controls.Add(cInner);
            pnlContent.Controls.Add(cOuter);

            // ── CARD B — filter bar (DockStyle.Top, Height 70) ───────────
            var bOuter = WrapCard(DockStyle.Top, 70, 0, 0, 0, 8);
            var bInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            bInner.Paint += PaintCardBorder;
            filterBar.Dock = DockStyle.Fill;
            bInner.Controls.Add(filterBar);
            bOuter.Controls.Add(bInner);
            pnlContent.Controls.Add(bOuter);

            // ── CARD A — KPI strip (DockStyle.Top, Height 86) ────────────
            // Added last → renders topmost
            var aOuter = WrapCard(DockStyle.Top, 86, 0, 0, 0, 8);
            var aInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            aInner.Paint += PaintCardBorder;
            pnlKpi.Dock = DockStyle.Fill;
            aInner.Controls.Add(pnlKpi);
            aOuter.Controls.Add(aInner);
            pnlContent.Controls.Add(aOuter);
        }

        // Outer wrapper panel (grey page background, acts as card margin)
        private static Panel WrapCard(DockStyle dock, int height, int padL, int padT, int padR, int padB)
        {
            var p = new Panel { Dock = dock, BackColor = Palette.BgPage, Padding = new Padding(padL, padT, padR, padB) };
            if (height > 0) p.Height = height;
            return p;
        }

        // Card title + horizontal rule + content row
        private static TableLayoutPanel MakeCardTable(string title)
        {
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(14, 8, 14, 8)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var hdr = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            hdr.Controls.Add(new Label
            {
                Text = title ?? string.Empty,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(19, 35, 61),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            });
            hdr.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(221, 227, 236) });
            tbl.Controls.Add(hdr, 0, 0);
            return tbl;
        }

        // ════════════════════════════════════════════════════════════════
        //  KPI PILLS  — 290 × 60, rounded-8 corners, clickable filter
        // ════════════════════════════════════════════════════════════════

        private static void BuildKpiPills(
            Panel pnlKpi,
            (string label, string count, Color fg, Color bg, string filterValue)[] pills)
        {
            pnlKpi.Controls.Clear();

            const int PillW   = 290;
            const int PillH   = 60;
            const int Gap     = 8;
            const int NumColW = 80;

            var flow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent,
                Padding       = new Padding(0),
                AutoScroll    = false
            };

            foreach (var (label, count, fg, bg, filterValue) in pills)
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
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, NumColW));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                var lblCount = new Label
                {
                    Text      = count,
                    Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                    ForeColor = fg, BackColor = Color.Transparent,
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, AutoSize = false
                };
                var lblLabel = new Label
                {
                    Text      = label,
                    Font      = new Font("Segoe UI", 12f),
                    ForeColor = fg, BackColor = Color.Transparent,
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
                };

                tlp.Controls.Add(lblCount, 0, 0);
                tlp.Controls.Add(lblLabel, 1, 0);
                pill.Controls.Add(tlp);
                flow.Controls.Add(pill);
            }
            pnlKpi.Controls.Add(flow);
        }

        // ════════════════════════════════════════════════════════════════
        //  FILTER BAR  — single-row FlowLayout
        //  filter1..4 → Apply / Reset → divider → Chart / Table / Export
        // ════════════════════════════════════════════════════════════════

        private static Panel BuildFilterRow(
            Control filter1, Control filter2,
            Control filter3, Control filter4,
            Button btnApply, Button btnReset,
            object _divider,
            Button btnChart, Button btnTable, Button btnExport)
        {
            var flow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent,
                Padding       = new Padding(16, 0, 16, 0)
            };

            void Add(Control c, int rightGap = 8)
            {
                if (c == null) return;
                c.Margin = new Padding(0, 0, rightGap, 0);
                if (c is DateTimePicker || c is ComboBox) c.Height = 34;
                flow.Controls.Add(c);
            }

            Add(filter1); Add(filter2, 12); Add(filter3); Add(filter4, 12);
            Add(btnApply); Add(btnReset, 16);

            // Vertical divider
            var div = new Panel { Width = 1, BackColor = Color.FromArgb(221, 227, 236), Margin = new Padding(0, 8, 16, 8) };
            flow.Controls.Add(div);

            Add(btnChart); Add(btnTable, 12); Add(btnExport);

            // Vertical centering wrapper
            var wrapper = new Panel { BackColor = Color.Transparent, Dock = DockStyle.Fill };
            wrapper.Controls.Add(flow);
            wrapper.Layout += (s, e) =>
            {
                var p = (Panel)s;
                flow.Top = Math.Max(0, (p.Height - flow.PreferredSize.Height) / 2);
            };
            return wrapper;
        }

        // ════════════════════════════════════════════════════════════════
        //  DATAGRIDVIEW
        // ════════════════════════════════════════════════════════════════

        private static DataGridView MakeGrid()
        {
            var g = new DataGridView
            {
                ReadOnly              = true,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible     = false,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect           = false,
                BackgroundColor       = Color.White,
                BorderStyle           = BorderStyle.None,
                GridColor             = Color.FromArgb(221, 227, 236),
                Font                  = new Font("Segoe UI", 12f),
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersHeight   = 40,
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255),
                    ForeColor = Color.FromArgb(98, 112, 135),
                    Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                    Padding   = new Padding(12, 0, 0, 0),
                    Alignment = DataGridViewContentAlignment.MiddleLeft
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor          = Color.White,
                    ForeColor          = Color.FromArgb(15, 31, 53),
                    SelectionBackColor = Color.FromArgb(219, 234, 254),
                    SelectionForeColor = Color.FromArgb(15, 31, 53),
                    Padding            = new Padding(12, 6, 12, 6)
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
            e.CellStyle.BackColor          = sc.bg;
            e.CellStyle.ForeColor          = sc.fg;
            e.CellStyle.SelectionBackColor = sc.bg;
            e.CellStyle.SelectionForeColor = sc.fg;
            e.CellStyle.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            e.FormattingApplied = true;
        }

        // ════════════════════════════════════════════════════════════════
        //  CHART / TABLE SWITCHER
        // ════════════════════════════════════════════════════════════════

        private static void ToggleChartTable(
            bool showChart,
            DataGridView dgv1, Panel chart1,
            DataGridView dgv2, Panel chart2)
        {
            if (dgv1   != null) dgv1.Visible   = !showChart;
            if (chart1 != null) chart1.Visible =  showChart;
            if (dgv2   != null) dgv2.Visible   = !showChart;
            if (chart2 != null) chart2.Visible =  showChart;
        }

        private static void FlipToggle(Button btnChart, Button btnTable, bool chartActive)
        {
            btnChart.BackColor = chartActive ? Palette.Primary : Color.White;
            btnChart.ForeColor = chartActive ? Color.White     : Color.FromArgb(98, 112, 135);
            btnChart.FlatAppearance.BorderSize = chartActive ? 0 : 1;
            btnTable.BackColor = chartActive ? Color.White     : Palette.Primary;
            btnTable.ForeColor = chartActive ? Color.FromArgb(98, 112, 135) : Color.White;
            btnTable.FlatAppearance.BorderSize = chartActive ? 1 : 0;
        }

        // ════════════════════════════════════════════════════════════════
        //  BUTTON / CONTROL FACTORIES
        // ════════════════════════════════════════════════════════════════

        private static Button MakePrimaryBtn(string text, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(19, 35, 61),
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(w, h),
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(29, 52, 92);
            return b;
        }

        private static Button MakeOutlineBtn(string text, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(w, h),
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize  = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            return b;
        }

        private static Button MakeToggleBtn(string text, int w, int h, bool active)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 11f),
                Size      = new Size(w, h),
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            if (active)
            {
                b.BackColor = Palette.Primary;
                b.ForeColor = Color.White;
                b.FlatAppearance.BorderSize = 0;
            }
            else
            {
                b.BackColor = Color.White;
                b.ForeColor = Color.FromArgb(98, 112, 135);
                b.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
                b.FlatAppearance.BorderSize  = 1;
            }
            return b;
        }

        private static Button MakeExportBtn(int w, int h)
        {
            var b = new Button
            {
                Text      = "\u2B07 Export CSV",
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(5, 150, 105),
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(w, h),
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(4, 120, 87);
            return b;
        }

        private static DateTimePicker MakeDatePicker(DateTime value)
            => new DateTimePicker
            {
                Format    = DateTimePickerFormat.Short,
                Value     = value,
                Font      = new Font("Segoe UI", 11f),
                Width     = 130,
                CalendarForeColor  = Color.FromArgb(15, 31, 53),
                CalendarTitleBackColor = Color.FromArgb(19, 35, 61),
                CalendarTitleForeColor = Color.White,
            };

        private static ComboBox MakeCbo(string[] items, int width)
        {
            var c = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 11f),
                Width         = width,
                BackColor     = Color.White,
                ForeColor     = Color.FromArgb(15, 31, 53)
            };
            c.Items.AddRange(items);
            c.SelectedIndex = 0;
            return c;
        }

        private static Label MakeLabel(string text)
            => new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                BackColor = Color.Transparent,
                AutoSize  = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

        // ════════════════════════════════════════════════════════════════
        //  CARD BORDER PAINTER
        // ════════════════════════════════════════════════════════════════

        private static void PaintCardBorder(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            var p = (Panel)sender;
            using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        }

        // ════════════════════════════════════════════════════════════════
        //  APPSHELL UPDATER
        // ════════════════════════════════════════════════════════════════

        private void ApplyShell(dynamic vm, string reportTitle)
        {
            if (vm == null) return;
            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb($"Statistical Reports  \u203a  {reportTitle}");
        }

        // ════════════════════════════════════════════════════════════════
        //  ROUNDED RECTANGLE HELPER
        // ════════════════════════════════════════════════════════════════

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d    = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X,             bounds.Y,              d, d, 180, 90);
            path.AddArc(bounds.Right - d,      bounds.Y,              d, d, 270, 90);
            path.AddArc(bounds.Right - d,      bounds.Bottom - d,     d, d,   0, 90);
            path.AddArc(bounds.X,             bounds.Bottom - d,     d, d,  90, 90);
            path.CloseFigure();
            return path;
        }

        // ════════════════════════════════════════════════════════════════
        //  TOP NAV EVENT HANDLER
        // ════════════════════════════════════════════════════════════════

        private void OnTopNavMenuItemClicked(string menu, string sub)
            => FormNavigator.NavigateTo(this, menu, sub);
    }
}
