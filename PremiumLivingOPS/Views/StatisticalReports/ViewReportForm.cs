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
    ///     CARD A — KPI pills (DockStyle.Top)
    ///     CARD B — Filter bar (DockStyle.Top)
    ///     CARD D — secondary grid (DockStyle.Bottom, only for After-Service / Sales)
    ///     CARD C — Main data grid (DockStyle.Fill)
    /// </summary>
    public partial class ViewReportForm : Form
    {
        private readonly StatisticalReportsController _ctrl = new StatisticalReportsController();
        private ReportType _activeReport = ReportType.SalesPerformance;

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

            var dgv = MakeGrid();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrderID",  HeaderText = "ORDER ID",     FillWeight = 18 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomer", HeaderText = "CUSTOMER",     FillWeight = 22 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",   HeaderText = "STATUS",       FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",     HeaderText = "ORDER DATE",   FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal",    HeaderText = "GRAND TOTAL",  FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLines",    HeaderText = "ITEMS",        FillWeight =  8 });
            dgv.CellFormatting += (s, e) => FormatStatusBadge(s, e, "colStatus");

            var dgvTop = MakeGrid();
            dgvTop.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItemID",  HeaderText = "ITEM ID",    FillWeight = 15 });
            dgvTop.Columns.Add(new DataGridViewTextBoxColumn { Name = "colProduct", HeaderText = "PRODUCT",    FillWeight = 32 });
            dgvTop.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCat",     HeaderText = "CATEGORY",   FillWeight = 14 });
            dgvTop.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQty",     HeaderText = "TOTAL QTY",  FillWeight = 14 });
            dgvTop.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRev",     HeaderText = "REVENUE",    FillWeight = 20 });

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
            };

            btnApply.Click += (s, e) => load(dtpFrom.Value, dtpTo.Value);
            btnReset.Click += (s, e) => { dtpFrom.Value = DateTime.Today.AddMonths(-3); dtpTo.Value = DateTime.Today; load(null, null); };
            load(null, null);

            BuildContentLayout(pnlKpi,
                BuildFilterBar(new Control[] { MakeLabel("From:"), dtpFrom, MakeSpacer(8), MakeLabel("To:"), dtpTo, MakeSpacer(12), btnApply, MakeSpacer(6), btnReset }),
                "Orders",              dgv,
                "Top Products by Revenue", dgvTop, 220);
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

            var dgv = MakeGrid();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colWHIID",     HeaderText = "WHI ID",         FillWeight = 13 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItem",      HeaderText = "ITEM",           FillWeight = 24 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCat",       HeaderText = "CATEGORY",       FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMat",       HeaderText = "MATERIAL TYPE",  FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colWH",        HeaderText = "WAREHOUSE",      FillWeight = 22 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStock",     HeaderText = "CURRENT STOCK",  FillWeight = 10 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colReorder",   HeaderText = "REORDER LVL",    FillWeight = 10 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAlert",     HeaderText = "ALERT",          FillWeight =  9 });
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
            };

            btnApply.Click += (s, e) => load();
            btnReset.Click += (s, e) => { cboCat.SelectedIndex = 0; chkReorder.Checked = false; load(); };
            load();

            BuildContentLayout(pnlKpi,
                BuildFilterBar(new Control[] { MakeLabel("Category:"), cboCat, MakeSpacer(12), chkReorder, MakeSpacer(12), btnApply, MakeSpacer(6), btnReset }),
                "Inventory Detail", dgv, null, null, 0);
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

            var dgv = MakeGrid();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPOID",     HeaderText = "PO ID",        FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSupplier", HeaderText = "SUPPLIER",     FillWeight = 22 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",   HeaderText = "STATUS",       FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",     HeaderText = "ORDER DATE",   FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAmount",   HeaderText = "PO AMOUNT",    FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMat",      HeaderText = "MATERIALS",    FillWeight = 24 });
            dgv.CellFormatting += (s, e) => FormatStatusBadge(s, e, "colStatus");

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
            };

            btnApply.Click += (s, e) => load();
            btnReset.Click += (s, e) => { cboStatus.SelectedIndex = 0; load(); };
            load();

            BuildContentLayout(pnlKpi,
                BuildFilterBar(new Control[] { MakeLabel("Status:"), cboStatus, MakeSpacer(12), btnApply, MakeSpacer(6), btnReset }),
                "Purchase Orders", dgv, null, null, 0);
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
                var g   = (DataGridView)s;
                string col = g.Columns[e.ColumnIndex].Name;
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
            };

            btnApply.Click += (s, e) => load();
            btnReset.Click += (s, e) => { cboStatus.SelectedIndex = 0; load(); };
            load();

            BuildContentLayout(pnlKpi,
                BuildFilterBar(new Control[] { MakeLabel("Status:"), cboStatus, MakeSpacer(12), btnApply, MakeSpacer(6), btnReset }),
                "Shipments", dgv, null, null, 0);
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
            };

            btnApply.Click += (s, e) => load();
            btnReset.Click += (s, e) => { cboCmp.SelectedIndex = 0; cboRtn.SelectedIndex = 0; load(); };
            load();

            BuildContentLayout(pnlKpi,
                BuildFilterBar(new Control[]
                {
                    MakeLabel("Complaint:"), cboCmp, MakeSpacer(10),
                    MakeLabel("Return:"),    cboRtn, MakeSpacer(10),
                    btnApply, MakeSpacer(6), btnReset
                }),
                "Complaints", dgvCmp,
                "Return Orders", dgvRtn, 210);
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

            var dgv = MakeGrid();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTxnID",   HeaderText = "TRANSACTION ID",  FillWeight = 22 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colType",    HeaderText = "TYPE",            FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAmount",  HeaderText = "AMOUNT (HK$)",    FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",    HeaderText = "DATE",            FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDoc",     HeaderText = "LINKED DOCUMENT", FillWeight = 22 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDocType", HeaderText = "DOCUMENT TYPE",   FillWeight = 18 });
            dgv.CellFormatting += (s, e) => FormatStatusBadge(s, e, "colType");

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
            };

            btnApply.Click += (s, e) => load(dtpFrom.Value, dtpTo.Value);
            btnReset.Click += (s, e) => { dtpFrom.Value = DateTime.Today.AddMonths(-3); dtpTo.Value = DateTime.Today; load(null, null); };
            load(null, null);

            BuildContentLayout(pnlKpi,
                BuildFilterBar(new Control[] { MakeLabel("From:"), dtpFrom, MakeSpacer(8), MakeLabel("To:"), dtpTo, MakeSpacer(12), btnApply, MakeSpacer(6), btnReset }),
                "Transactions", dgv, null, null, 0);
        }

        // ════════════════════════════════════════════════════════════════
        //  LAYOUT BUILDER
        // ════════════════════════════════════════════════════════════════

        private void BuildContentLayout(
            Panel pnlKpi, Panel filterBar,
            string title1, DataGridView grid1,
            string title2, DataGridView grid2, int grid2Height)
        {
            bool hasSecondary = !string.IsNullOrEmpty(title2) && grid2 != null && grid2Height > 0;

            // ── CARD D secondary grid (DockStyle.Bottom — added first) ──
            if (hasSecondary)
            {
                var outer = new Panel { Dock = DockStyle.Bottom, Height = grid2Height + 62, BackColor = Palette.BgPage, Padding = new Padding(0, 0, 0, 10) };
                var inner = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgCard };
                inner.Paint += PaintCardBorder;
                var tbl = MakeCardTbl(title2);
                grid2.Dock = DockStyle.Fill;
                tbl.Controls.Add(grid2, 0, 1);
                inner.Controls.Add(tbl);
                outer.Controls.Add(inner);
                pnlContent.Controls.Add(outer);
            }

            // ── CARD C main grid (DockStyle.Fill) ──
            var cOuter = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage, Padding = new Padding(0, 0, 0, hasSecondary ? 0 : 10) };
            var cInner = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgCard };
            cInner.Paint += PaintCardBorder;
            var cTbl = MakeCardTbl(title1);
            grid1.Dock = DockStyle.Fill;
            cTbl.Controls.Add(grid1, 0, 1);
            cInner.Controls.Add(cTbl);
            cOuter.Controls.Add(cInner);
            pnlContent.Controls.Add(cOuter);

            // ── CARD B filter bar (DockStyle.Top) ──
            var bOuter = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Palette.BgPage, Padding = new Padding(0, 0, 0, 8) };
            var bInner = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgCard };
            bInner.Paint += PaintCardBorder;
            filterBar.Dock = DockStyle.Fill;
            bInner.Controls.Add(filterBar);
            bOuter.Controls.Add(bInner);
            pnlContent.Controls.Add(bOuter);

            // ── CARD A KPI pills (DockStyle.Top — added last so it renders on top) ──
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
            hdr.Controls.Add(new Label { Text = title, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Palette.Primary, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft });
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

        private static Label  MakeLabel(string t) => new Label { Text = t, Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = Palette.TextMuted, BackColor = Color.Transparent, AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
        private static Panel  MakeSpacer(int w)    => new Panel { Width = w, BackColor = Color.Transparent };
        private static ComboBox MakeCbo(string[] items) { var c = new ComboBox { Font = new Font("Segoe UI", 11f), DropDownStyle = ComboBoxStyle.DropDownList, Width = 185 }; c.Items.AddRange(items); c.SelectedIndex = 0; return c; }
        private static Button MakePrimaryBtn(string t, int w, int h) { var b = new Button { Text = t, Font = new Font("Segoe UI", 11f), ForeColor = Color.White, BackColor = Palette.Primary, FlatStyle = FlatStyle.Flat, Size = new Size(w, h), Cursor = Cursors.Hand }; b.FlatAppearance.BorderSize = 0; b.FlatAppearance.MouseOverBackColor = Palette.PrimaryDark; return b; }
        private static Button MakeOutlineBtn(string t, int w, int h) { var b = new Button { Text = t, Font = new Font("Segoe UI", 11f), ForeColor = Palette.TextMuted, BackColor = Palette.BgCard, FlatStyle = FlatStyle.Flat, Size = new Size(w, h), Cursor = Cursors.Hand }; b.FlatAppearance.BorderColor = Palette.BorderColor; b.FlatAppearance.BorderSize = 1; b.FlatAppearance.MouseOverBackColor = Palette.BgPage; return b; }

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
