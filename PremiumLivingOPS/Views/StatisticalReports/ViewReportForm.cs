using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
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
    /// Tab Bar   : 6 report tabs in a CardPanel 3-layer card at the top of pnlContent.
    ///             Style baseline: HandlingGoodsReceivedForm (Logistics Processing).
    ///             Active tab → blue underline (#2F6FED, 3px) + Bold font.
    ///             Inactive   → grey text (#627087), regular font.
    ///             Sidebar buttons removed entirely (no Designer stub needed).
    /// KPI pills : 290 × 60, rounded, Cursor.Hand, click → filter.
    /// Filter bar: FlowLayout (search/date/combobox + Apply/Reset + divider + Chart/Table/Export).
    /// DataGridView: header #F6F9FF / cell row 44px / selection #DBEAFe.
    /// CardPanel : every block wrapped in 3-layer nested cards.
    ///
    /// pnlContent card stack (top → bottom, Controls.Add order reversed):
    ///   tabOuter  (DockStyle.Top, H=69)  ← Tab Bar card
    ///   aOuter    (DockStyle.Top, H=86)  ← KPI Pills card
    ///   bOuter    (DockStyle.Top, H=70)  ← Filter Bar card
    ///   cOuter    (DockStyle.Fill)        ← Primary data card
    ///   dOuter    (DockStyle.Bottom)      ← Secondary data card (Sales / AfterService)
    /// </summary>
    public partial class ViewReportForm : Form
    {
        private readonly StatisticalReportsController _ctrl = new StatisticalReportsController();
        private ReportType _activeReport = ReportType.SalesPerformance;
        private bool _tabPaintWired = false;

        private bool _salesChart        = false;
        private bool _inventoryChart    = false;
        private bool _procurementChart  = false;
        private bool _logisticsChart    = false;
        private bool _afterServiceChart = false;
        private bool _financeChart      = false;

        // Tab button references — kept so SwitchReport can repaint without rebuild
        private Button[] _tabButtons = new Button[0];

        // Status badge colour palette
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

        // Active tab underline colour — matches HandlingGoodsReceivedForm exactly
        private static readonly Color TabActiveColor   = Color.FromArgb(47, 111, 237);
        private static readonly Color TabInactiveColor = Color.FromArgb(98, 112, 135);

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
            var initVm = _ctrl.GetSalesReportVM();
            _shell.SetUser(initVm.UserBar.DisplayName, initVm.UserBar.Department);
            _shell.SetVisibleMenus(initVm.AllowedMenus);
            _shell.SetBreadcrumb("Statistical Reports  ›  View Report");

            SwitchReport(ReportType.SalesPerformance);
        }

        // ════════════════════════════════════════════════════════════════
        //  REPORT SWITCHER
        // ════════════════════════════════════════════════════════════════

        private void SwitchReport(ReportType rt)
        {
            _activeReport = rt;
            RenderReport(rt);
        }

        // ════════════════════════════════════════════════════════════════
        //  RENDER REPORT
        // ════════════════════════════════════════════════════════════════

        private void RenderReport(ReportType rt)
        {
            pnlContent.SuspendLayout();
            pnlContent.Controls.Clear();
            _tabPaintWired = false;

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
            var dtpFrom  = MakeDatePicker(DateTime.Today.AddMonths(-3));
            var dtpTo    = MakeDatePicker(DateTime.Today);
            var chkDate  = new CheckBox { Text = "From:", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = Color.FromArgb(98, 112, 135), BackColor = Color.Transparent, AutoSize = true };
            var btnApply = MakePrimaryBtn("Apply",  110, 40);
            var btnReset = MakeOutlineBtn("Reset",   90, 40);
            var btnChart  = MakeToggleBtn("📊  Chart",  130, 40, _salesChart);
            var btnTable  = MakeToggleBtn("📋  Table",  120, 40, !_salesChart);
            var btnExport = MakeExportBtn(150, 40);

            var pnlKpi = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(4, 0, 4, 0) };

            var dgv = MakeGrid();
            dgv.Columns.Add("OrderID", "Order ID");
            dgv.Columns.Add("Cust",    "Customer");
            dgv.Columns.Add("Emp",     "Salesperson");
            dgv.Columns.Add("Date",    "Order Date");
            dgv.Columns.Add("Total",   "Total Amount");
            dgv.Columns.Add("Status",  "Payment Status");

            var vm = _ctrl.GetSalesReportVM();
            ApplyShell(vm, "Statistical Reports  ›  View Report");

            int pending=0, approved=0, rejected=0;
            decimal revenue=0;
            foreach (var o in vm.Orders)
            {
                dgv.Rows.Add(o.OrderID, o.CustomerName, o.EmployeeName, o.OrderDate.ToString("yyyy-MM-dd"), o.TotalAmount.ToString("N2"), o.PaymentStatus);
                revenue += o.TotalAmount;
                if (o.PaymentStatus == "Pending")  pending++;
                if (o.PaymentStatus == "Approved") approved++;
                if (o.PaymentStatus == "Rejected") rejected++;
            }
            dgv.CellFormatting += (s, e) => ApplyStatusBadge(s, e, "Status");

            BuildKpiPills(
                pnlKpi,
                new[]
                {
                    ("Total Revenue",   revenue.ToString("N0"), Color.FromArgb(  6,  95, 70), Color.FromArgb(209,250,229), (string)null),
                    ("Pending Orders",  pending.ToString(),      Color.FromArgb(146, 64, 14), Color.FromArgb(254,243,199), "Pending"),
                    ("Approved Orders", approved.ToString(),     Color.FromArgb(  6, 95, 70), Color.FromArgb(209,250,229), "Approved"),
                    ("Rejected Orders", rejected.ToString(),     Color.FromArgb(185, 28, 28), Color.FromArgb(254,226,226), "Rejected")
                },
                filter =>
                {
                    dgv.ClearSelection();
                    foreach (DataGridViewRow r in dgv.Rows)
                        if ((r.Cells[5].Value?.ToString() ?? "") == filter) { r.Selected = true; dgv.FirstDisplayedScrollingRowIndex = r.Index; break; }
                });

            var chartMain = CreateBarChartPanel("Monthly Revenue", new[] { 42000f, 51500f, 48800f, 56200f, 60100f, 63800f }, new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun" }, Color.FromArgb(47,111,237));
            var chartTop  = CreateBarChartPanel("Top 5 Customers", new[] { 15600f, 14950f, 13980f, 12840f, 11760f }, new[] { "Lee", "Wong", "Chan", "Ng", "Lau" }, Color.FromArgb(16,185,129));
            chartMain.Visible = chartTop.Visible = _salesChart;
            dgv.Visible = !_salesChart;

            // FIX: declare dgvTop BEFORE the lambdas that capture it
            var dgvTop = MakeGrid();
            btnChart.Click  += (s, e) => { _salesChart = true;  FlipToggle(btnChart, btnTable, true);  ToggleChartTable(_salesChart, dgv, chartMain, dgvTop, chartTop); };
            btnTable.Click  += (s, e) => { _salesChart = false; FlipToggle(btnChart, btnTable, false); ToggleChartTable(_salesChart, dgv, chartMain, dgvTop, chartTop); };
            btnApply.Click  += (s, e) => MessageBox.Show("Filters applied.");
            btnReset.Click  += (s, e) => { dtpFrom.Value = DateTime.Today.AddMonths(-3); dtpTo.Value = DateTime.Today; };
            btnExport.Click += (s, e) => ExportGridCsv(dgv, "sales_report.csv");

            var cOuter = BuildDataCard(
                "Sales Orders",
                BuildFilterRow(chkDate, dtpFrom, MakeLabel("To:"), dtpTo, btnApply, btnReset, null, btnChart, btnTable, btnExport),
                dgv,
                chartMain);

            dgvTop.Columns.Add("Cust",  "Customer");
            dgvTop.Columns.Add("Spend", "Total Spend");
            dgvTop.Rows.Add("Lee",  "15,600");
            dgvTop.Rows.Add("Wong", "14,950");
            dgvTop.Rows.Add("Chan", "13,980");
            dgvTop.Rows.Add("Ng",   "12,840");
            dgvTop.Rows.Add("Lau",  "11,760");
            dgvTop.Visible = !_salesChart;

            var dOuter = BuildSecondaryCard("Top Customers", dgvTop, chartTop, 260);
            ComposeReportFrame(pnlKpi, cOuter, dOuter, null);
        }

        // ════════════════════════════════════════════════════════════════
        //  2. INVENTORY STATUS
        // ════════════════════════════════════════════════════════════════

        private void RenderInventory()
        {
            var cboCat    = MakeCbo(new[] { "All", "Furniture", "Lighting", "Decor" }, 150);
            var chkReorder = new CheckBox { Text = "Reorder Only", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = Color.FromArgb(98,112,135), AutoSize = true, BackColor = Color.Transparent };
            var btnApply  = MakePrimaryBtn("Apply", 110, 40);
            var btnReset  = MakeOutlineBtn("Reset", 90, 40);
            var btnChart  = MakeToggleBtn("📊  Chart", 130, 40, _inventoryChart);
            var btnTable   = MakeToggleBtn("📋  Table", 120, 40, !_inventoryChart);
            var btnExport = MakeExportBtn(150, 40);
            var pnlKpi = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(4,0,4,0) };

            var dgv = MakeGrid();
            dgv.Columns.Add("SKU",      "SKU");
            dgv.Columns.Add("Product",  "Product");
            dgv.Columns.Add("Category", "Category");
            dgv.Columns.Add("Stock",    "Stock Qty");
            dgv.Columns.Add("Reorder",  "Reorder Level");
            dgv.Columns.Add("Status",   "Status");

            var vm = _ctrl.GetInventoryReportVM();
            ApplyShell(vm, "Statistical Reports  ›  View Report");

            int inStock=0, lowStock=0, outStock=0;
            foreach (var i in vm.Inventory)
            {
                string status = i.CurrentStock <= 0 ? "Cancelled" : (i.CurrentStock <= i.ReorderLevel ? "Pending" : "Completed");
                dgv.Rows.Add(i.InventoryID, i.ProductName, i.CategoryName, i.CurrentStock, i.ReorderLevel, status == "Completed" ? "In Stock" : status == "Pending" ? "Low Stock" : "Out of Stock");
                if (i.CurrentStock <= 0) outStock++;
                else if (i.CurrentStock <= i.ReorderLevel) lowStock++;
                else inStock++;
            }
            dgv.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex < 0 || e.RowIndex < 0 || e.Value == null) return;
                if (dgv.Columns[e.ColumnIndex].Name != "Status") return;
                string val = e.Value.ToString();
                Color bg, fg;
                if (val == "In Stock")      { bg = Color.FromArgb(209,250,229); fg = Color.FromArgb(6,95,70); }
                else if (val == "Low Stock") { bg = Color.FromArgb(254,243,199); fg = Color.FromArgb(146,64,14); }
                else                         { bg = Color.FromArgb(254,226,226); fg = Color.FromArgb(185,28,28); }
                e.CellStyle.BackColor = e.CellStyle.SelectionBackColor = bg;
                e.CellStyle.ForeColor = e.CellStyle.SelectionForeColor = fg;
                e.CellStyle.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                e.FormattingApplied = true;
            };

            BuildKpiPills(
                pnlKpi,
                new[]
                {
                    ("In Stock",      inStock.ToString(),  Color.FromArgb(  6,95,70),  Color.FromArgb(209,250,229), "In Stock"),
                    ("Low Stock",     lowStock.ToString(), Color.FromArgb(146,64,14),  Color.FromArgb(254,243,199), "Low Stock"),
                    ("Out of Stock",  outStock.ToString(), Color.FromArgb(185,28,28),  Color.FromArgb(254,226,226), "Out of Stock")
                },
                filter =>
                {
                    dgv.ClearSelection();
                    foreach (DataGridViewRow r in dgv.Rows)
                        if ((r.Cells[5].Value?.ToString() ?? "") == filter) { r.Selected = true; dgv.FirstDisplayedScrollingRowIndex = r.Index; break; }
                });

            var chartStock    = CreateBarChartPanel("Current Stock by Category", new[] { 190f, 145f, 86f }, new[] { "Furniture", "Lighting", "Decor" }, Color.FromArgb(47,111,237));
            var chartCategory = CreateBarChartPanel("Reorder Risk", new[] { 4f, 2f, 1f }, new[] { "Furniture", "Lighting", "Decor" }, Color.FromArgb(245,158,11));
            chartStock.Visible = chartCategory.Visible = _inventoryChart;
            dgv.Visible = !_inventoryChart;

            btnChart.Click  += (s, e) => { _inventoryChart = true;  FlipToggle(btnChart, btnTable, true);  ToggleChartTable(_inventoryChart, dgv, chartStock, (DataGridView)null, chartCategory); };
            btnTable.Click  += (s, e) => { _inventoryChart = false; FlipToggle(btnChart, btnTable, false); ToggleChartTable(_inventoryChart, dgv, chartStock, (DataGridView)null, chartCategory); };
            btnApply.Click  += (s, e) => MessageBox.Show("Filters applied.");
            btnReset.Click  += (s, e) => { cboCat.SelectedIndex = 0; chkReorder.Checked = false; };
            btnExport.Click += (s, e) => ExportGridCsv(dgv, "inventory_report.csv");

            var cOuter = BuildDataCard(
                "Inventory Status",
                BuildFilterRow(MakeLabel("Category:"), cboCat, chkReorder, null, btnApply, btnReset, null, btnChart, btnTable, btnExport),
                dgv,
                chartStock);

            var dOuter = BuildSecondaryCard("Inventory Risk Breakdown", null, chartCategory, 260);
            ComposeReportFrame(pnlKpi, cOuter, dOuter, null);
        }

        // ════════════════════════════════════════════════════════════════
        //  3. PROCUREMENT SUMMARY
        // ════════════════════════════════════════════════════════════════

        private void RenderProcurement()
        {
            var cboStatus  = MakeCbo(new[] { "All", "Sent", "Partially Received", "Received", "Completed", "Cancelled" }, 180);
            var btnApply   = MakePrimaryBtn("Apply", 110, 40);
            var btnReset   = MakeOutlineBtn("Reset",  90, 40);
            var btnChart   = MakeToggleBtn("📊  Chart", 130, 40, _procurementChart);
            var btnTable  = MakeToggleBtn("📋  Table", 120, 40, !_procurementChart);
            var btnExport  = MakeExportBtn(150, 40);
            var pnlKpi = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(4,0,4,0) };

            var dgv = MakeGrid();
            dgv.Columns.Add("POID",    "PO ID");
            dgv.Columns.Add("Supp",    "Supplier");
            dgv.Columns.Add("Date",    "Order Date");
            dgv.Columns.Add("Amount",  "Total Amount");
            dgv.Columns.Add("Status",  "Status");

            var vm = _ctrl.GetProcurementReportVM();
            ApplyShell(vm, "Statistical Reports  ›  View Report");

            int sent=0, partial=0, received=0;
            foreach (var p in vm.PurchaseOrders)
            {
                dgv.Rows.Add(p.PurchaseID, p.SupplierName, p.OrderDate.ToString("yyyy-MM-dd"), p.TotalAmount.ToString("N2"), p.Status);
                if (p.Status == "Sent") sent++;
                else if (p.Status == "Partially Received") partial++;
                else if (p.Status == "Received" || p.Status == "Completed") received++;
            }
            dgv.CellFormatting += (s, e) => ApplyStatusBadge(s, e, "Status");

            BuildKpiPills(
                pnlKpi,
                new[]
                {
                    ("Sent",               sent.ToString(),     Color.FromArgb(29,78,216), Color.FromArgb(219,234,254), "Sent"),
                    ("Partially Received", partial.ToString(),  Color.FromArgb(91,33,182), Color.FromArgb(237,233,254), "Partially Received"),
                    ("Received / Completed", received.ToString(), Color.FromArgb(6,95,70),  Color.FromArgb(209,250,229), "Received")
                },
                filter =>
                {
                    dgv.ClearSelection();
                    foreach (DataGridViewRow r in dgv.Rows)
                    {
                        var v = r.Cells[4].Value?.ToString() ?? "";
                        if ((filter == "Received" && (v == "Received" || v == "Completed")) || v == filter)
                        { r.Selected = true; dgv.FirstDisplayedScrollingRowIndex = r.Index; break; }
                    }
                });

            var chartSupplier = CreateBarChartPanel("Top Suppliers by Value", new[] { 92500f, 81200f, 69800f, 61100f }, new[] { "Ikea", "Philips", "Muji", "Sony" }, Color.FromArgb(47,111,237));
            // FIX: explicit float cast for int variables
            var chartStatus   = CreateBarChartPanel("PO Status Breakdown", new[] { (float)sent, (float)partial, (float)received }, new[] { "Sent", "Partial", "Received" }, Color.FromArgb(16,185,129));
            chartSupplier.Visible = chartStatus.Visible = _procurementChart;
            dgv.Visible = !_procurementChart;

            btnChart.Click  += (s, e) => { _procurementChart = true;  FlipToggle(btnChart, btnTable, true);  ToggleChartTable(_procurementChart, dgv, chartSupplier, (DataGridView)null, chartStatus); };
            btnTable.Click  += (s, e) => { _procurementChart = false; FlipToggle(btnChart, btnTable, false); ToggleChartTable(_procurementChart, dgv, chartSupplier, (DataGridView)null, chartStatus); };
            btnApply.Click  += (s, e) => MessageBox.Show("Filters applied.");
            btnReset.Click  += (s, e) => cboStatus.SelectedIndex = 0;
            btnExport.Click += (s, e) => ExportGridCsv(dgv, "procurement_report.csv");

            var cOuter = BuildDataCard(
                "Purchase Orders",
                BuildFilterRow(MakeLabel("Status:"), cboStatus, null, null, btnApply, btnReset, null, btnChart, btnTable, btnExport),
                dgv,
                chartSupplier);

            var dOuter = BuildSecondaryCard("Procurement Status Overview", null, chartStatus, 260);
            ComposeReportFrame(pnlKpi, cOuter, dOuter, null);
        }

        // ════════════════════════════════════════════════════════════════
        //  4. LOGISTICS OVERVIEW
        // ════════════════════════════════════════════════════════════════

        private void RenderLogistics()
        {
            var cboStatus = MakeCbo(new[] { "All", "Pending", "Processing", "In Transit", "Delivered", "Cancelled" }, 170);
            var btnApply  = MakePrimaryBtn("Apply", 110, 40);
            var btnReset  = MakeOutlineBtn("Reset",  90, 40);
            var btnChart  = MakeToggleBtn("📊  Chart", 130, 40, _logisticsChart);
            var btnTable  = MakeToggleBtn("📋  Table", 120, 40, !_logisticsChart);
            var btnExport = MakeExportBtn(150, 40);
            var pnlKpi = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(4,0,4,0) };

            var dgv = MakeGrid();
            dgv.Columns.Add("DN",      "Delivery Note ID");
            dgv.Columns.Add("Customer", "Customer");
            dgv.Columns.Add("Date",    "Delivery Date");
            dgv.Columns.Add("Status",  "Status");

            var vm = _ctrl.GetLogisticsReportVM();
            ApplyShell(vm, "Statistical Reports  ›  View Report");

            int processing=0, transit=0, delivered=0;
            foreach (var d in vm.DeliveryNotes)
            {
                dgv.Rows.Add(d.DeliveryNoteID, d.CustomerName, d.DeliveryDate.ToString("yyyy-MM-dd"), d.Status);
                if (d.Status == "Processing") processing++;
                else if (d.Status == "In Transit") transit++;
                else if (d.Status == "Delivered") delivered++;
            }
            dgv.CellFormatting += (s, e) => ApplyStatusBadge(s, e, "Status");

            BuildKpiPills(
                pnlKpi,
                new[]
                {
                    ("Processing", processing.ToString(), Color.FromArgb(29,78,216),  Color.FromArgb(219,234,254), "Processing"),
                    ("In Transit", transit.ToString(),    Color.FromArgb(91,33,182),  Color.FromArgb(237,233,254), "In Transit"),
                    ("Delivered",  delivered.ToString(),  Color.FromArgb(6,95,70),    Color.FromArgb(209,250,229), "Delivered")
                },
                filter =>
                {
                    dgv.ClearSelection();
                    foreach (DataGridViewRow r in dgv.Rows)
                        if ((r.Cells[3].Value?.ToString() ?? "") == filter) { r.Selected = true; dgv.FirstDisplayedScrollingRowIndex = r.Index; break; }
                });

            // FIX: explicit float cast for int variables
            var chartStatus = CreateBarChartPanel("Delivery Status", new[] { (float)processing, (float)transit, (float)delivered }, new[] { "Processing", "Transit", "Delivered" }, Color.FromArgb(47,111,237));
            chartStatus.Visible = _logisticsChart;
            dgv.Visible = !_logisticsChart;

            btnChart.Click  += (s, e) => { _logisticsChart = true;  FlipToggle(btnChart, btnTable, true);  ToggleChartTable(_logisticsChart, dgv, chartStatus, (DataGridView)null, null); };
            btnTable.Click  += (s, e) => { _logisticsChart = false; FlipToggle(btnChart, btnTable, false); ToggleChartTable(_logisticsChart, dgv, chartStatus, (DataGridView)null, null); };
            btnApply.Click  += (s, e) => MessageBox.Show("Filters applied.");
            btnReset.Click  += (s, e) => cboStatus.SelectedIndex = 0;
            btnExport.Click += (s, e) => ExportGridCsv(dgv, "logistics_report.csv");

            var cOuter = BuildDataCard(
                "Delivery Notes",
                BuildFilterRow(MakeLabel("Status:"), cboStatus, null, null, btnApply, btnReset, null, btnChart, btnTable, btnExport),
                dgv,
                chartStatus);

            ComposeReportFrame(pnlKpi, cOuter, null, null);
        }

        // ════════════════════════════════════════════════════════════════
        //  5. AFTER-SERVICE SUMMARY
        // ════════════════════════════════════════════════════════════════

        private void RenderAfterService()
        {
            var cboCmp   = MakeCbo(new[] { "All", "Pending", "Escalated", "Completed" }, 150);
            var cboRtn   = MakeCbo(new[] { "All", "Pending", "Approved", "Rejected" }, 150);
            var btnApply = MakePrimaryBtn("Apply", 110, 40);
            var btnReset = MakeOutlineBtn("Reset",  90, 40);
            var btnChart = MakeToggleBtn("📊  Chart", 130, 40, _afterServiceChart);
            var btnTable  = MakeToggleBtn("📋  Table", 120, 40, !_afterServiceChart);
            var btnExport= MakeExportBtn(150, 40);
            var pnlKpi = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(4,0,4,0) };

            var dgvCmp = MakeGrid();
            dgvCmp.Columns.Add("CID",    "Complaint ID");
            dgvCmp.Columns.Add("Customer","Customer");
            dgvCmp.Columns.Add("Date",   "Complaint Date");
            dgvCmp.Columns.Add("Status", "Status");

            var dgvRtn = MakeGrid();
            dgvRtn.Columns.Add("RID",    "Return ID");
            dgvRtn.Columns.Add("Customer","Customer");
            dgvRtn.Columns.Add("Date",   "Return Date");
            dgvRtn.Columns.Add("Status", "Status");

            var vm = _ctrl.GetAfterServiceReportVM();
            ApplyShell(vm, "Statistical Reports  ›  View Report");

            int cmpPending=0, cmpEsc=0, rtnApproved=0, rtnRejected=0;
            foreach (var c in vm.Complaints)
            {
                dgvCmp.Rows.Add(c.ComplaintID, c.CustomerName, c.ComplaintDate.ToString("yyyy-MM-dd"), c.Status);
                if (c.Status == "Pending") cmpPending++; else if (c.Status == "Escalated") cmpEsc++;
            }
            foreach (var r in vm.ReturnRequests)
            {
                dgvRtn.Rows.Add(r.ReturnRequestID, r.CustomerName, r.RequestDate.ToString("yyyy-MM-dd"), r.Status);
                if (r.Status == "Approved") rtnApproved++; else if (r.Status == "Rejected") rtnRejected++;
            }
            dgvCmp.CellFormatting += (s, e) => ApplyStatusBadge(s, e, "Status");
            dgvRtn.CellFormatting += (s, e) => ApplyStatusBadge(s, e, "Status");

            BuildKpiPills(
                pnlKpi,
                new[]
                {
                    ("Pending Complaints", cmpPending.ToString(), Color.FromArgb(146,64,14), Color.FromArgb(254,243,199), "CMP:Pending"),
                    ("Escalated Cases",    cmpEsc.ToString(),     Color.FromArgb(185,28,28), Color.FromArgb(254,226,226), "CMP:Escalated"),
                    ("Approved Returns",   rtnApproved.ToString(),Color.FromArgb(6,95,70),   Color.FromArgb(209,250,229), "RTN:Approved"),
                    ("Rejected Returns",   rtnRejected.ToString(),Color.FromArgb(185,28,28), Color.FromArgb(254,226,226), "RTN:Rejected")
                },
                filter =>
                {
                    var parts = filter.Split(':');
                    if (parts.Length != 2) return;
                    var type = parts[0];
                    var status = parts[1];
                    var grid = type == "CMP" ? dgvCmp : dgvRtn;
                    grid.ClearSelection();
                    foreach (DataGridViewRow r in grid.Rows)
                        if ((r.Cells[3].Value?.ToString() ?? "") == status) { r.Selected = true; grid.FirstDisplayedScrollingRowIndex = r.Index; break; }
                });

            // FIX: explicit float cast for int variables
            var chartCmp = CreateBarChartPanel("Complaint Status", new[] { (float)cmpPending, (float)cmpEsc, (float)Math.Max(0, vm.Complaints.Count - cmpPending - cmpEsc) }, new[] { "Pending", "Escalated", "Completed" }, Color.FromArgb(47,111,237));
            var chartRtn = CreateBarChartPanel("Return Request Status", new[] { (float)rtnApproved, (float)rtnRejected, (float)Math.Max(0, vm.ReturnRequests.Count - rtnApproved - rtnRejected) }, new[] { "Approved", "Rejected", "Pending" }, Color.FromArgb(16,185,129));
            chartCmp.Visible = chartRtn.Visible = _afterServiceChart;
            dgvCmp.Visible = dgvRtn.Visible = !_afterServiceChart;

            btnChart.Click  += (s, e) => { _afterServiceChart = true;  FlipToggle(btnChart, btnTable, true);  ToggleChartTable(_afterServiceChart, dgvCmp, chartCmp, dgvRtn, chartRtn); };
            btnTable.Click  += (s, e) => { _afterServiceChart = false; FlipToggle(btnChart, btnTable, false); ToggleChartTable(_afterServiceChart, dgvCmp, chartCmp, dgvRtn, chartRtn); };
            btnApply.Click  += (s, e) => MessageBox.Show("Filters applied.");
            btnReset.Click  += (s, e) => { cboCmp.SelectedIndex = 0; cboRtn.SelectedIndex = 0; };
            btnExport.Click += (s, e) => ExportGridCsv(dgvCmp, "complaints_report.csv");

            var cOuter = BuildDataCard(
                "Complaints",
                BuildFilterRow(MakeLabel("Complaint:"), cboCmp, MakeLabel("Return:"), cboRtn, btnApply, btnReset, null, btnChart, btnTable, btnExport),
                dgvCmp,
                chartCmp);

            var dOuter = BuildSecondaryCard("Return Requests", dgvRtn, chartRtn, 260);
            ComposeReportFrame(pnlKpi, cOuter, dOuter, null);
        }

        // ════════════════════════════════════════════════════════════════
        //  6. FINANCE OVERVIEW
        // ════════════════════════════════════════════════════════════════

        private void RenderFinance()
        {
            var dtpFrom  = MakeDatePicker(DateTime.Today.AddMonths(-3));
            var dtpTo    = MakeDatePicker(DateTime.Today);
            var chkDate  = new CheckBox { Text = "From:", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = Color.FromArgb(98,112,135), BackColor = Color.Transparent, AutoSize = true };
            var btnApply = MakePrimaryBtn("Apply", 110, 40);
            var btnReset = MakeOutlineBtn("Reset",  90, 40);
            var btnChart = MakeToggleBtn("📊  Chart", 130, 40, _financeChart);
            var btnTable  = MakeToggleBtn("📋  Table", 120, 40, !_financeChart);
            var btnExport= MakeExportBtn(150, 40);
            var pnlKpi = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(4,0,4,0) };

            var dgv = MakeGrid();
            dgv.Columns.Add("PID",   "Payment ID");
            dgv.Columns.Add("Cust",  "Customer");
            dgv.Columns.Add("Date",  "Payment Date");
            dgv.Columns.Add("Method","Method");
            dgv.Columns.Add("Amount","Amount");
            dgv.Columns.Add("Type",  "Type");

            var vm = _ctrl.GetFinanceReportVM();
            ApplyShell(vm, "Statistical Reports  ›  View Report");

            decimal revenue=0, expense=0, refund=0;
            foreach (var p in vm.Payments)
            {
                string type = p.PaymentAmount >= 0 ? "Revenue" : "Refund";
                dgv.Rows.Add(p.PaymentID, p.CustomerName, p.PaymentDate.ToString("yyyy-MM-dd"), p.PaymentMethod, Math.Abs(p.PaymentAmount).ToString("N2"), type);
                if (p.PaymentAmount >= 0) revenue += p.PaymentAmount; else refund += Math.Abs(p.PaymentAmount);
            }
            expense = Math.Round(revenue * 0.38m, 2); // visual placeholder for dashboard ratio
            dgv.CellFormatting += (s, e) => ApplyStatusBadge(s, e, "Type");

            BuildKpiPills(
                pnlKpi,
                new[]
                {
                    ("Revenue", revenue.ToString("N0"), Color.FromArgb(6,95,70),   Color.FromArgb(209,250,229), "Revenue"),
                    ("Expense", expense.ToString("N0"), Color.FromArgb(185,28,28), Color.FromArgb(254,226,226), null),
                    ("Refund",  refund.ToString("N0"),  Color.FromArgb(146,64,14), Color.FromArgb(254,243,199), "Refund")
                },
                filter =>
                {
                    dgv.ClearSelection();
                    foreach (DataGridViewRow r in dgv.Rows)
                        if ((r.Cells[5].Value?.ToString() ?? "") == filter) { r.Selected = true; dgv.FirstDisplayedScrollingRowIndex = r.Index; break; }
                });

            var chartAmounts   = CreateBarChartPanel("Monthly Net Cash Flow", new[] { 18500f, 22400f, 20100f, 23700f, 25100f, 26600f }, new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun" }, Color.FromArgb(47,111,237));
            var chartBreakdown = CreateBarChartPanel("Revenue / Expense / Refund", new[] { (float)revenue, (float)expense, (float)refund }, new[] { "Revenue", "Expense", "Refund" }, Color.FromArgb(16,185,129));
            chartAmounts.Visible = chartBreakdown.Visible = _financeChart;
            dgv.Visible = !_financeChart;

            btnChart.Click  += (s, e) => { _financeChart = true;  FlipToggle(btnChart, btnTable, true);  ToggleChartTable(_financeChart, dgv, chartAmounts, (DataGridView)null, chartBreakdown); };
            btnTable.Click  += (s, e) => { _financeChart = false; FlipToggle(btnChart, btnTable, false); ToggleChartTable(_financeChart, dgv, chartAmounts, (DataGridView)null, chartBreakdown); };
            btnApply.Click  += (s, e) => MessageBox.Show("Filters applied.");
            btnReset.Click  += (s, e) => { dtpFrom.Value = DateTime.Today.AddMonths(-3); dtpTo.Value = DateTime.Today; };
            btnExport.Click += (s, e) => ExportGridCsv(dgv, "finance_report.csv");

            var cOuter = BuildDataCard(
                "Payments",
                BuildFilterRow(chkDate, dtpFrom, MakeLabel("To:"), dtpTo, btnApply, btnReset, null, btnChart, btnTable, btnExport),
                dgv,
                chartAmounts);

            var dOuter = BuildSecondaryCard("Finance Breakdown", null, chartBreakdown, 260);
            ComposeReportFrame(pnlKpi, cOuter, dOuter, null);
        }

        // ════════════════════════════════════════════════════════════════
        //  COMMON COMPOSER
        // ════════════════════════════════════════════════════════════════

        private void ComposeReportFrame(Panel pnlKpi, Panel cOuter, Panel dOuter, Panel extraBottom)
        {
            if (extraBottom != null) pnlContent.Controls.Add(extraBottom);
            if (dOuter != null)      pnlContent.Controls.Add(dOuter);
            pnlContent.Controls.Add(cOuter);

            var aOuter = WrapCard(DockStyle.Top, 86, 0, 0, 0, 8);
            var aInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            aInner.Paint += PaintCardBorder;
            pnlKpi.Dock = DockStyle.Fill;
            aInner.Controls.Add(pnlKpi);
            aOuter.Controls.Add(aInner);
            pnlContent.Controls.Add(aOuter);

            // Tab Bar card — always topmost in pnlContent
            BuildTabBar(_activeReport);
        }

        // ════════════════════════════════════════════════════════════════
        //  TAB BAR  — baseline: HandlingGoodsReceivedForm
        //
        //  Style rules (mirror HGR exactly):
        //  • Active   : ForeColor = #2F6FED, Font Bold 12pt, 3px blue underline painted
        //  • Inactive : ForeColor = #627087, Font Regular 12pt, no underline
        //  • BackColor: White card / white button surface like HGR
        //  • CardPanel: pnlTabOuter (gray, DockStyle.Top, H=69)
        //                └─ pnlTabCard (white, PaintCardBorder)
        //                     └─ TableLayoutPanel (equal-width tabs)
        //
        //  The Statistical Reports tab bar therefore uses the same
        //  rendering structure and active-state pattern as the
        //  HandlingGoodsReceived tab switcher.
        // ════════════════════════════════════════════════════════════════

        private void BuildTabBar(ReportType activeRt)
        {
            var tabDefs = new[]
            {
                (ReportType.SalesPerformance,    "📊  Sales Performance"),
                (ReportType.InventoryStatus,     "📦  Inventory Status"),
                (ReportType.ProcurementSummary,  "🛒  Procurement"),
                (ReportType.LogisticsOverview,   "🚚  Logistics"),
                (ReportType.AfterServiceSummary, "🔧  After-Service"),
                (ReportType.FinanceOverview,     "💰  Finance")
            };

            int tabCount = tabDefs.Length;
            var buttons = new Button[tabCount];

            Button MakeTabBtn(string text, bool active)
            {
                var b = new Button
                {
                    Text      = text,
                    Font      = new Font("Segoe UI", 12f, active ? FontStyle.Bold : FontStyle.Regular),
                    ForeColor = active ? TabActiveColor : TabInactiveColor,
                    BackColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Dock      = DockStyle.Fill,
                    Cursor    = Cursors.Hand,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Padding   = new Padding(0, 0, 0, 3)
                };
                b.FlatAppearance.BorderSize         = 0;
                b.FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 248, 255);
                b.FlatAppearance.MouseDownBackColor = Color.FromArgb(235, 241, 255);
                return b;
            }

            for (int i = 0; i < tabCount; i++)
            {
                var (rt, label) = tabDefs[i];
                bool active = rt == activeRt;

                var btn = MakeTabBtn(label, active);
                buttons[i] = btn;
                btn.Paint += PaintTabUnderline;

                var localRt = rt;
                btn.Click += (s, e) =>
                {
                    for (int j = 0; j < buttons.Length; j++)
                    {
                        bool isActive = tabDefs[j].Item1 == localRt;
                        buttons[j].ForeColor = isActive ? TabActiveColor : TabInactiveColor;
                        buttons[j].Font      = new Font("Segoe UI", 12f, isActive ? FontStyle.Bold : FontStyle.Regular);
                        buttons[j].Invalidate();
                    }
                    SwitchReport(localRt);
                };
            }

            _tabButtons = buttons;

            var tblTabs = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                RowCount        = 1,
                ColumnCount     = tabCount,
                BackColor       = Color.White,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(8, 0, 8, 0)
            };
            for (int i = 0; i < tabCount; i++)
                tblTabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / tabCount));
            tblTabs.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            for (int i = 0; i < tabCount; i++)
                tblTabs.Controls.Add(buttons[i], i, 0);

            var pnlTabCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlTabCard.Paint += PaintCardBorder;
            pnlTabCard.Controls.Add(tblTabs);

            var pnlTabOuter = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 69,
                BackColor = Palette.BgPage,
                Padding   = new Padding(20, 4, 20, 0)
            };
            pnlTabOuter.Controls.Add(pnlTabCard);

            pnlContent.Controls.Add(pnlTabOuter);
        }

        // Mirrors HandlingGoodsReceivedForm.PaintTabUnderline exactly
        private static void PaintTabUnderline(object sender, PaintEventArgs e)
        {
            var btn = (Button)sender;
            if (btn.ForeColor != TabActiveColor) return;
            using var pen = new Pen(TabActiveColor, 3f);
            e.Graphics.DrawLine(pen, 0, btn.Height - 2, btn.Width, btn.Height - 2);
        }

        // ════════════════════════════════════════════════════════════════
        //  CARD HELPERS
        // ════════════════════════════════════════════════════════════════

        private static Panel WrapCard(DockStyle dock, int height, int padL, int padT, int padR, int padB)
        {
            var p = new Panel { Dock = dock, BackColor = Palette.BgPage, Padding = new Padding(padL, padT, padR, padB) };
            if (height > 0) p.Height = height;
            return p;
        }

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
                Text      = title ?? string.Empty,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(19, 35, 61),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            });
            hdr.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(221, 227, 236) });
            tbl.Controls.Add(hdr, 0, 0);
            return tbl;
        }

        // ════════════════════════════════════════════════════════════════
        //  KPI PILLS
        // ════════════════════════════════════════════════════════════════

        private static void BuildKpiPills(
            Panel pnlKpi,
            (string label, string count, Color fg, Color bg, string filterValue)[] pills,
            Action<string> onPillClick)
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
                bool isClickable = filterValue != null && onPillClick != null;

                var pill = new Panel
                {
                    BackColor = bg,
                    Size      = new Size(PillW, PillH),
                    Margin    = new Padding(0, 0, Gap, 0),
                    Cursor    = isClickable ? Cursors.Hand : Cursors.Default
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
                    Padding         = new Padding(10, 0, 8, 0),
                    Cursor          = isClickable ? Cursors.Hand : Cursors.Default
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, NumColW));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                var lblCount = new Label
                {
                    Text      = count,
                    Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                    ForeColor = fg, BackColor = Color.Transparent,
                    Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, AutoSize = false,
                    Cursor    = isClickable ? Cursors.Hand : Cursors.Default
                };
                var lblLabel = new Label
                {
                    Text      = label,
                    Font      = new Font("Segoe UI", 12f),
                    ForeColor = fg, BackColor = Color.Transparent,
                    Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false,
                    Cursor    = isClickable ? Cursors.Hand : Cursors.Default
                };

                tlp.Controls.Add(lblCount, 0, 0);
                tlp.Controls.Add(lblLabel, 1, 0);

                if (isClickable)
                {
                    string localFilterValue = filterValue;
                    EventHandler clickHandler = (s, e) => onPillClick(localFilterValue);
                    pill.Click     += clickHandler;
                    tlp.Click      += clickHandler;
                    lblCount.Click += clickHandler;
                    lblLabel.Click += clickHandler;
                }

                pill.Controls.Add(tlp);
                flow.Controls.Add(pill);
            }
            pnlKpi.Controls.Add(flow);
        }

        // ════════════════════════════════════════════════════════════════
        //  FILTER BAR
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

            var div = new Panel { Width = 1, BackColor = Color.FromArgb(221, 227, 236), Margin = new Padding(0, 8, 16, 8) };
            flow.Controls.Add(div);

            Add(btnChart); Add(btnTable, 12); Add(btnExport);

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
            e.CellStyle.Font      = new Font("Segoe UI", 11f, FontStyle.Bold);
            e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            e.FormattingApplied   = true;
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
            b.FlatAppearance.MouseOverBackColor = active ? Palette.PrimaryDark : Color.FromArgb(240, 244, 249);
            return b;
        }

        private static Button MakeExportBtn(int w, int h)
        {
            var b = new Button
            {
                Text      = "⬇  Export CSV",
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(6, 95, 70),
                BackColor = Color.FromArgb(209, 250, 229),
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(w, h),
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(6, 95, 70);
            b.FlatAppearance.BorderSize  = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 252, 231);
            return b;
        }

        private static Label MakeLabel(string text)
        {
            return new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                BackColor = Color.Transparent,
                AutoSize  = true
            };
        }

        private static DateTimePicker MakeDatePicker(DateTime value)
        {
            return new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value  = value,
                Font   = new Font("Segoe UI", 11f),
                Width  = 130,
                Height = 34
            };
        }

        private static ComboBox MakeCbo(string[] items, int width)
        {
            var c = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 11f),
                Width         = width,
                Height        = 34
            };
            c.Items.AddRange(items);
            c.SelectedIndex = 0;
            return c;
        }

        // ════════════════════════════════════════════════════════════════
        //  SIMPLE CHART PANEL
        // ════════════════════════════════════════════════════════════════

        private static Panel CreateBarChartPanel(string title, IEnumerable<float> values, IEnumerable<string> labels, Color barColor)
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(18, 14, 18, 18) };
            panel.Controls.Add(new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(19, 35, 61),
                TextAlign = ContentAlignment.MiddleLeft
            });

            var plot = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            plot.Paint += (s, e) =>
            {
                var vals = new List<float>(values);
                var labs = new List<string>(labels);
                if (vals.Count == 0) return;

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = plot.ClientRectangle;
                rect = new Rectangle(rect.X + 10, rect.Y + 10, rect.Width - 20, rect.Height - 36);
                if (rect.Width <= 0 || rect.Height <= 0) return;

                float max = 1f;
                foreach (var v in vals) if (v > max) max = v;

                using var axisPen = new Pen(Color.FromArgb(221, 227, 236), 1f);
                using var barBrush = new SolidBrush(barColor);
                using var textBrush = new SolidBrush(Color.FromArgb(98, 112, 135));
                using var valBrush = new SolidBrush(Color.FromArgb(19, 35, 61));
                using var labelFont = new Font("Segoe UI", 9f);
                using var valueFont = new Font("Segoe UI", 9f, FontStyle.Bold);

                e.Graphics.DrawLine(axisPen, rect.Left, rect.Bottom, rect.Right, rect.Bottom);

                int n = vals.Count;
                float slot = n == 0 ? rect.Width : rect.Width / (float)n;
                float barW = Math.Max(18f, Math.Min(48f, slot * 0.52f));

                for (int i = 0; i < n; i++)
                {
                    float h = max <= 0 ? 0 : (vals[i] / max) * (rect.Height - 34);
                    float x = rect.Left + i * slot + (slot - barW) / 2f;
                    float y = rect.Bottom - h;
                    e.Graphics.FillRectangle(barBrush, x, y, barW, h);

                    var sf = new StringFormat { Alignment = StringAlignment.Center };
                    e.Graphics.DrawString(vals[i].ToString("N0"), valueFont, valBrush, x + barW / 2f, Math.Max(rect.Top, y - 18), sf);
                    e.Graphics.DrawString(i < labs.Count ? labs[i] : string.Empty, labelFont, textBrush, x + barW / 2f, rect.Bottom + 6, sf);
                }
            };

            panel.Controls.Add(plot);
            return panel;
        }

        private static void ExportGridCsv(DataGridView grid, string fileName)
        {
            if (grid == null || grid.Columns.Count == 0) return;

            using var sfd = new SaveFileDialog
            {
                FileName = fileName,
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*"
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;

            var lines = new List<string>();
            var headers = new List<string>();
            foreach (DataGridViewColumn c in grid.Columns) headers.Add(EscapeCsv(c.HeaderText));
            lines.Add(string.Join(",", headers));

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                var cells = new List<string>();
                foreach (DataGridViewCell cell in row.Cells)
                    cells.Add(EscapeCsv(cell.Value?.ToString() ?? string.Empty));
                lines.Add(string.Join(",", cells));
            }

            System.IO.File.WriteAllLines(sfd.FileName, lines);
            MessageBox.Show("CSV exported successfully.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static string EscapeCsv(string value)
        {
            if (value.Contains("\"") || value.Contains(",") || value.Contains("\n") || value.Contains("\r"))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        private static Panel BuildDataCard(string title, Panel filterRow, Control tableView, Control chartView)
        {
            var cOuter = WrapCard(DockStyle.Fill, 0, 0, 0, 0, 8);
            var cInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            cInner.Paint += PaintCardBorder;

            var cTbl = MakeCardTable(title);
            if (filterRow != null)
            {
                var filterHost = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.Transparent };
                filterHost.Controls.Add(filterRow);
                cInner.Controls.Add(filterHost);
            }

            var contentHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            if (chartView != null) { chartView.Dock = DockStyle.Fill; contentHost.Controls.Add(chartView); }
            if (tableView != null) { tableView.Dock = DockStyle.Fill; contentHost.Controls.Add(tableView); }

            cTbl.Controls.Add(contentHost, 0, 1);
            cInner.Controls.Add(cTbl);
            cOuter.Controls.Add(cInner);
            return cOuter;
        }

        private static Panel BuildSecondaryCard(string title, Control tableView, Control chartView, int height)
        {
            var dOuter = WrapCard(DockStyle.Bottom, height, 0, 0, 0, 0);
            var dInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            dInner.Paint += PaintCardBorder;

            var dTbl = MakeCardTable(title);
            var contentHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            if (chartView != null) { chartView.Dock = DockStyle.Fill; contentHost.Controls.Add(chartView); }
            if (tableView != null) { tableView.Dock = DockStyle.Fill; contentHost.Controls.Add(tableView); }
            dTbl.Controls.Add(contentHost, 0, 1);

            dInner.Controls.Add(dTbl);
            dOuter.Controls.Add(dInner);
            return dOuter;
        }

        // ════════════════════════════════════════════════════════════════
        //  APPSHELL HELPER
        // ════════════════════════════════════════════════════════════════

        private void ApplyShell(ViewReportViewModel vm, string breadcrumb)
        {
            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb(breadcrumb);
        }

        // ════════════════════════════════════════════════════════════════
        //  CARD BORDER PAINT
        // ════════════════════════════════════════════════════════════════

        private static void PaintCardBorder(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            var p = (Panel)sender;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        }

        // ════════════════════════════════════════════════════════════════
        //  ROUNDED RECTANGLE HELPER
        // ════════════════════════════════════════════════════════════════

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X,                    bounds.Y,                     d, d, 180, 90);
            path.AddArc(bounds.X + bounds.Width - d, bounds.Y,                     d, d, 270, 90);
            path.AddArc(bounds.X + bounds.Width - d, bounds.Y + bounds.Height - d, d, d,   0, 90);
            path.AddArc(bounds.X,                    bounds.Y + bounds.Height - d, d, d,  90, 90);
            path.CloseFigure();
            return path;
        }

        // ════════════════════════════════════════════════════════════════
        //  NAVIGATION & LOGOUT  (AppShell Rule 4)
        // ════════════════════════════════════════════════════════════════

        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }
    }
}
