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
    public partial class ViewReportForm : Form
    {
        private int _activeTab = -1;

        private readonly StatisticalReportsController _ctrl =
            new StatisticalReportsController();

        public ViewReportForm()
        {
            InitializeComponent();
            this.Load += ViewReportForm_Load;
        }

        // ────────────────────────────────────────────────────────────────────
        //  Load
        // ────────────────────────────────────────────────────────────────────
        private void ViewReportForm_Load(object sender, EventArgs e)
        {
            RefreshShell();
            SwitchToReport(0);
        }

        // ────────────────────────────────────────────────────────────────────
        //  AppShell  — UserBar / menus / breadcrumb
        // ────────────────────────────────────────────────────────────────────
        private void RefreshShell()
        {
            var vm = _ctrl.GetSalesReportVM();
            if (vm == null) return;
            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Statistical Reports  \u203a  View Report");
        }

        // ────────────────────────────────────────────────────────────────────
        //  Tab switcher
        // ────────────────────────────────────────────────────────────────────
        internal void SwitchToReport(int tabIndex)
        {
            if (_activeTab == tabIndex) return;
            _activeTab = tabIndex;

            var tabBtns = new Button[]
            {
                btnTabSalesRevenue, btnTabInventory, btnTabProduction,
                btnTabLogistics,    btnTabAfterService
            };

            for (int i = 0; i < tabBtns.Length; i++)
            {
                bool active = i == tabIndex;
                tabBtns[i].ForeColor = active ? Color.FromArgb(47, 111, 237) : Color.FromArgb(98, 112, 135);
                tabBtns[i].Font      = new Font("Segoe UI", 12f, active ? FontStyle.Bold : FontStyle.Regular);
                tabBtns[i].Padding   = active ? new Padding(0) : new Padding(0, 0, 0, 3);
                tabBtns[i].Invalidate();
            }

            pnlContent.Controls.Clear();

            switch (tabIndex)
            {
                case 0: BuildSalesRevenueReport(); break;
                case 1: BuildInventoryReport();    break;
                case 2: BuildProductionReport();   break;
                case 3: BuildLogisticsReport();    break;
                case 4: BuildAfterServiceReport(); break;
            }
        }

        // ────────────────────────────────────────────────────────────────────
        //  1. Sales & Revenue
        // ────────────────────────────────────────────────────────────────────
        private void BuildSalesRevenueReport()
        {
            var vm = _ctrl.GetSalesReportVM();
            if (vm == null) return;

            // KPI pills
            var kpiPills = new[]
            {
                ("Total Orders",   vm.SalesKpi?.TotalOrders.ToString()     ?? "0",  Color.FromArgb(55,48,163),  Color.FromArgb(238,242,255)),
                ("Revenue",        $"${vm.SalesKpi?.TotalRevenue:F0}",              Color.FromArgb(6,95,70),    Color.FromArgb(209,250,229)),
                ("Avg Order",      $"${vm.SalesKpi?.AverageOrderValue:F0}",         Color.FromArgb(15,118,110), Color.FromArgb(204,251,241)),
                ("Delivered",      vm.SalesKpi?.DeliveredOrders.ToString()  ?? "0", Color.FromArgb(6,95,70),    Color.FromArgb(209,250,229)),
                ("Pending",        vm.SalesKpi?.PendingOrders.ToString()    ?? "0", Color.FromArgb(146,64,14),  Color.FromArgb(254,243,199)),
                ("Cancelled",      vm.SalesKpi?.CancelledOrders.ToString()  ?? "0", Color.FromArgb(71,85,105),  Color.FromArgb(241,245,249)),
            };

            // Grid columns
            var cols = new[]
            {
                ("Order ID",       "colSOrderID",   120),
                ("Customer",       "colSCustomer",  200),
                ("Status",         "colSStatus",    130),
                ("Issued Date",    "colSDate",      130),
                ("Grand Total",    "colSTotal",     120),
                ("Lines",          "colSLines",      70),
            };

            var dgv = MakeGrid();
            foreach (var (hdr, name, w) in cols)
                dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = hdr, Name = name, Width = w });

            if (vm.SalesRows != null)
                foreach (var r in vm.SalesRows)
                    dgv.Rows.Add(r.OrderID, r.CustomerName, r.OrderStatus,
                                 r.IssuedTime.ToString("yyyy-MM-dd"),
                                 $"${r.GrandTotal:F2}", r.LineCount);

            // Top Products sub-grid
            var colsTop = new[]
            {
                ("Item ID",   "colTItemID",  110),
                ("Name",      "colTName",    200),
                ("Category",  "colTCat",     130),
                ("Qty Sold",  "colTQty",      90),
                ("Revenue",   "colTRev",     120),
            };
            var dgvTop = MakeGrid();
            foreach (var (hdr, name, w) in colsTop)
                dgvTop.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = hdr, Name = name, Width = w });
            if (vm.TopProducts != null)
                foreach (var p in vm.TopProducts)
                    dgvTop.Rows.Add(p.ItemID, p.ItemName, p.Category,
                                    p.TotalQty, $"${p.TotalRevenue:F2}");

            BuildTwoGridLayout("Sales & Revenue Report",
                               kpiPills,
                               "Orders",          dgv,
                               "Top Products",    dgvTop);
        }

        // ────────────────────────────────────────────────────────────────────
        //  2. Inventory
        // ────────────────────────────────────────────────────────────────────
        private void BuildInventoryReport()
        {
            var vm = _ctrl.GetInventoryReportVM();
            if (vm == null) return;

            var kpiPills = new[]
            {
                ("Total SKUs",      vm.InventoryKpi?.TotalSKUs.ToString()         ?? "0", Color.FromArgb(55,48,163),  Color.FromArgb(238,242,255)),
                ("Below Reorder",   vm.InventoryKpi?.BelowReorderCount.ToString() ?? "0", Color.FromArgb(164,15,76),  Color.FromArgb(255,228,240)),
                ("Products",        vm.InventoryKpi?.ProductCount.ToString()       ?? "0", Color.FromArgb(6,95,70),    Color.FromArgb(209,250,229)),
                ("Raw Materials",   vm.InventoryKpi?.RawMaterialCount.ToString()   ?? "0", Color.FromArgb(15,118,110), Color.FromArgb(204,251,241)),
            };

            var cols = new[]
            {
                ("WH Item ID",   "colIWHID",   110),
                ("Item ID",      "colIItemID", 100),
                ("Name",         "colIName",   200),
                ("Category",     "colICat",    120),
                ("Material Type","colIMat",    130),
                ("Warehouse",    "colIWH",     120),
                ("Location",     "colILoc",    130),
                ("Stock",        "colIStock",   80),
                ("Reorder Lvl",  "colIReorder", 90),
                ("Alert",        "colIAlert",   80),
            };

            var dgv = MakeGrid();
            foreach (var (hdr, name, w) in cols)
                dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = hdr, Name = name, Width = w });

            if (vm.InventoryRows != null)
                foreach (var r in vm.InventoryRows)
                {
                    int idx = dgv.Rows.Add(
                        r.WarehouseItemID, r.ItemID, r.ItemName,
                        r.ItemCategory, r.MaterialType,
                        r.WarehouseID, r.WarehouseLocation,
                        r.CurrentStock, r.ReorderLevel,
                        r.BelowReorder ? "\u26a0 Low" : "");
                    if (r.BelowReorder)
                    {
                        dgv.Rows[idx].DefaultCellStyle.ForeColor  = Color.FromArgb(146,64,14);
                        dgv.Rows[idx].DefaultCellStyle.BackColor  = Color.FromArgb(254,243,199);
                    }
                }

            BuildSingleGridLayout("Inventory Status Report", kpiPills, dgv);
        }

        // ────────────────────────────────────────────────────────────────────
        //  3. Production (Procurement Summary)
        // ────────────────────────────────────────────────────────────────────
        private void BuildProductionReport()
        {
            var vm = _ctrl.GetProcurementReportVM();
            if (vm == null) return;

            var kpiPills = new[]
            {
                ("Total POs",       vm.ProcKpi?.TotalPOs.ToString()          ?? "0",  Color.FromArgb(55,48,163),  Color.FromArgb(238,242,255)),
                ("Total Spend",     $"${vm.ProcKpi?.TotalSpend:F0}",                  Color.FromArgb(146,64,14),  Color.FromArgb(254,243,199)),
                ("Completed",       vm.ProcKpi?.CompletedPOs.ToString()       ?? "0", Color.FromArgb(6,95,70),    Color.FromArgb(209,250,229)),
                ("Pending",         vm.ProcKpi?.PendingPOs.ToString()         ?? "0", Color.FromArgb(29,78,216),  Color.FromArgb(219,234,254)),
                ("Suppliers",       vm.ProcKpi?.UniqueSuppliers.ToString()    ?? "0", Color.FromArgb(15,118,110), Color.FromArgb(204,251,241)),
            };

            var cols = new[]
            {
                ("PO ID",       "colPPOID",   110),
                ("Supplier",    "colPSupp",   180),
                ("Status",      "colPSt",     130),
                ("Order Date",  "colPDate",   120),
                ("Total",       "colPTotal",  110),
                ("Materials",   "colPMat",    220),
                ("Request ID",  "colPReq",    110),
            };

            var dgv = MakeGrid();
            foreach (var (hdr, name, w) in cols)
                dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = hdr, Name = name, Width = w });

            if (vm.ProcRows != null)
                foreach (var r in vm.ProcRows)
                    dgv.Rows.Add(r.PurchaseID, r.SupplierName, r.PurchaseStatus,
                                 r.OrderDate.ToString("yyyy-MM-dd"),
                                 $"${r.POTotalAmount:F2}",
                                 r.MaterialNames, r.RequestID);

            BuildSingleGridLayout("Procurement Summary Report", kpiPills, dgv);
        }

        // ────────────────────────────────────────────────────────────────────
        //  4. Logistics
        // ────────────────────────────────────────────────────────────────────
        private void BuildLogisticsReport()
        {
            var vm = _ctrl.GetLogisticsReportVM();
            if (vm == null) return;

            var kpiPills = new[]
            {
                ("Total Shipments", vm.LogKpi?.TotalShipments.ToString() ?? "0", Color.FromArgb(55,48,163),  Color.FromArgb(238,242,255)),
                ("Completed",       vm.LogKpi?.Completed.ToString()      ?? "0", Color.FromArgb(6,95,70),    Color.FromArgb(209,250,229)),
                ("In Transit",      vm.LogKpi?.InTransit.ToString()      ?? "0", Color.FromArgb(29,78,216),  Color.FromArgb(219,234,254)),
                ("Pending",         vm.LogKpi?.Pending.ToString()        ?? "0", Color.FromArgb(146,64,14),  Color.FromArgb(254,243,199)),
                ("With Reply Slip", vm.LogKpi?.WithReplySlip.ToString()  ?? "0", Color.FromArgb(15,118,110), Color.FromArgb(204,251,241)),
            };

            var cols = new[]
            {
                ("Shipment ID",  "colLShipID",   120),
                ("Order ID",     "colLOrdID",    110),
                ("Customer",     "colLCust",     180),
                ("Status",       "colLSt",       120),
                ("Type",         "colLType",     110),
                ("Method",       "colLMethod",   120),
                ("Ship Date",    "colLDate",     120),
                ("Amount",       "colLAmt",      110),
                ("Note",         "colLNote",      70),
                ("Reply Slip",   "colLSlip",      80),
            };

            var dgv = MakeGrid();
            foreach (var (hdr, name, w) in cols)
                dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = hdr, Name = name, Width = w });

            if (vm.LogRows != null)
                foreach (var r in vm.LogRows)
                    dgv.Rows.Add(
                        r.ShipmentID, r.OrderID, r.CustomerName,
                        r.ShipmentStatus, r.ShipmentType, r.DeliveryMethod,
                        r.ShipDate == default ? "" : r.ShipDate.ToString("yyyy-MM-dd"),
                        $"${r.TotalAmount:F2}",
                        r.HasDeliveryNote ? "\u2713" : "",
                        r.HasReplySlip    ? "\u2713" : "");

            BuildSingleGridLayout("Logistics Overview Report", kpiPills, dgv);
        }

        // ────────────────────────────────────────────────────────────────────
        //  5. After-Service
        // ────────────────────────────────────────────────────────────────────
        private void BuildAfterServiceReport()
        {
            var vm = _ctrl.GetAfterServiceReportVM();
            if (vm == null) return;

            var kpiPills = new[]
            {
                ("Total Complaints", vm.AfterKpi?.TotalComplaints.ToString() ?? "0", Color.FromArgb(164,15,76),  Color.FromArgb(255,228,240)),
                ("Open Complaints",  vm.AfterKpi?.OpenComplaints.ToString()  ?? "0", Color.FromArgb(146,64,14),  Color.FromArgb(254,243,199)),
                ("Total Returns",    vm.AfterKpi?.TotalReturns.ToString()    ?? "0", Color.FromArgb(29,78,216),  Color.FromArgb(219,234,254)),
                ("Total Refunded",   $"${vm.AfterKpi?.TotalRefunded:F0}",            Color.FromArgb(71,85,105),  Color.FromArgb(241,245,249)),
            };

            // Complaints grid
            var colsC = new[]
            {
                ("Complaint ID",  "colCID",   120),
                ("Order ID",      "colCOrd",  110),
                ("Customer",      "colCCust", 180),
                ("Description",   "colCDesc", 280),
                ("Status",        "colCSt",   120),
            };
            var dgvC = MakeGrid();
            foreach (var (hdr, name, w) in colsC)
                dgvC.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = hdr, Name = name, Width = w });
            if (vm.Complaints != null)
                foreach (var c in vm.Complaints)
                    dgvC.Rows.Add(c.ComplaintID, c.OrderID, c.CustomerName,
                                  c.ComplaintDescription, c.ComplaintStatus);

            // Returns grid
            var colsR = new[]
            {
                ("Return ID",   "colRID",    110),
                ("Order ID",    "colROrd",   110),
                ("Customer",    "colRCust",  180),
                ("Reason",      "colRReason",220),
                ("Refund",      "colRRef",   100),
                ("Status",      "colRSt",    120),
                ("Return Date", "colRDate",  120),
            };
            var dgvR = MakeGrid();
            foreach (var (hdr, name, w) in colsR)
                dgvR.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = hdr, Name = name, Width = w });
            if (vm.Returns != null)
                foreach (var r in vm.Returns)
                    dgvR.Rows.Add(r.ReturnID, r.OrderID, r.CustomerName,
                                  r.Reason, $"${r.RefundAmount:F2}", r.ReturnStatus,
                                  r.ReturnDate == default ? "" : r.ReturnDate.ToString("yyyy-MM-dd"));

            BuildTwoGridLayout("After-Service Summary Report",
                               kpiPills,
                               "Complaints",  dgvC,
                               "Returns",     dgvR);
        }

        // ────────────────────────────────────────────────────────────────────
        //  Layout helpers
        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Single-grid layout:
        ///   [KPI pills bar  — top, 90px]
        ///   [Grid card      — fill]
        /// </summary>
        private void BuildSingleGridLayout(
            string title,
            (string label, string value, Color fg, Color bg)[] pills,
            DataGridView dgv)
        {
            var root = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };

            // KPI card (top)
            var kpiCard = MakeKpiCard(pills);
            kpiCard.Dock = DockStyle.Top;

            // Grid card (fill)
            var (gridOuter, gridInner) = CardPanel.CreateFill();
            gridInner.Padding = new Padding(0);

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(0)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var lblTitle = new Label
            {
                Text = title, Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0)
            };
            dgv.Dock = DockStyle.Fill;
            tbl.Controls.Add(lblTitle, 0, 0);
            tbl.Controls.Add(dgv,      0, 1);
            gridInner.Controls.Add(tbl);

            root.Controls.Add(gridOuter);
            root.Controls.Add(kpiCard);
            pnlContent.Controls.Add(root);
        }

        /// <summary>
        /// Two-grid layout (Sales + After-Service):
        ///   [KPI pills bar      — top, 90px]
        ///   [Primary grid card  — 60% fill]
        ///   [Secondary grid card— 40% fill]
        /// </summary>
        private void BuildTwoGridLayout(
            string title,
            (string label, string value, Color fg, Color bg)[] pills,
            string label1, DataGridView dgv1,
            string label2, DataGridView dgv2)
        {
            var root = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };

            var kpiCard = MakeKpiCard(pills);
            kpiCard.Dock = DockStyle.Top;

            var splitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                BackColor = Color.FromArgb(240, 244, 249),
                Panel1MinSize = 120,
                Panel2MinSize = 80,
                SplitterWidth = 6
            };
            splitter.SplitterMoved += (s, e) => { };

            splitter.Panel1.Controls.Add(MakeGridCard(label1, dgv1, title));
            splitter.Panel2.Controls.Add(MakeGridCard(label2, dgv2, ""));

            root.Controls.Add(splitter);
            root.Controls.Add(kpiCard);
            pnlContent.Controls.Add(root);

            // Set splitter position after layout
            splitter.SplitterDistance = Math.Max(120, splitter.Height * 6 / 10);
        }

        private Panel MakeGridCard(string subTitle, DataGridView dgv, string mainTitle)
        {
            var (outer, inner) = CardPanel.CreateFill();
            inner.Padding = new Padding(0);

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(0)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var lbl = new Label
            {
                Text = string.IsNullOrEmpty(mainTitle) ? subTitle : $"{mainTitle}  ›  {subTitle}",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0)
            };
            dgv.Dock = DockStyle.Fill;
            tbl.Controls.Add(lbl, 0, 0);
            tbl.Controls.Add(dgv, 0, 1);
            inner.Controls.Add(tbl);
            return outer;
        }

        private Panel MakeKpiCard((string label, string value, Color fg, Color bg)[] pills)
        {
            const int PillW = 220, PillH = 60, Gap = 8;

            var pnlKpi = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(12, 10, 12, 10) };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            foreach (var (label, value, fg, bg) in pills)
            {
                var pill = new Panel { BackColor = bg, Size = new Size(PillW, PillH), Margin = new Padding(0, 0, Gap, 0), Cursor = Cursors.Default };
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
                    BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding = new Padding(10, 0, 8, 0)
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70f));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                tlp.Controls.Add(new Label { Text = value, Font = new Font("Segoe UI", 13f, FontStyle.Bold), ForeColor = fg, BackColor = Color.Transparent, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, AutoSize = false }, 0, 0);
                tlp.Controls.Add(new Label { Text = label, Font = new Font("Segoe UI", 11f),                  ForeColor = fg, BackColor = Color.Transparent, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,   AutoSize = false }, 1, 0);

                pill.Controls.Add(tlp);
                flow.Controls.Add(pill);
            }

            pnlKpi.Controls.Add(flow);

            var (outer, inner) = CardPanel.Create(outerHeight: 90, outerPadding: new Padding(20, 8, 20, 8));
            inner.Controls.Add(pnlKpi);
            return outer;
        }

        private static DataGridView MakeGrid()
        {
            var dgv = new DataGridView
            {
                Dock                    = DockStyle.Fill,
                BackgroundColor         = Color.White,
                BorderStyle             = BorderStyle.None,
                RowHeadersVisible       = false,
                AllowUserToAddRows      = false,
                AllowUserToDeleteRows   = false,
                ReadOnly                = true,
                SelectionMode           = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect             = false,
                AutoSizeColumnsMode     = DataGridViewAutoSizeColumnsMode.None,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight     = 40,
                RowTemplate             = { Height = 34 },
                Font                    = new Font("Segoe UI", 11f),
                GridColor               = Color.FromArgb(221, 227, 236),
                EnableHeadersVisualStyles = false
            };
            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = Color.FromArgb(98, 112, 135),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding   = new Padding(8, 0, 0, 0)
            };
            dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.White,
                SelectionBackColor = Color.FromArgb(219, 234, 254),
                SelectionForeColor = Color.FromArgb(29, 78, 216),
                Padding = new Padding(8, 0, 0, 0)
            };
            dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(248, 250, 252),
                SelectionBackColor = Color.FromArgb(219, 234, 254),
                SelectionForeColor = Color.FromArgb(29, 78, 216)
            };
            return dgv;
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X,         bounds.Y,          d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y,          d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d,   0, 90);
            path.AddArc(bounds.X,         bounds.Bottom - d, d, d,  90, 90);
            path.CloseFigure();
            return path;
        }

        // ────────────────────────────────────────────────────────────────────
        //  AppShell event handlers  (subscribed ONCE in Designer.cs — RULE 4)
        // ────────────────────────────────────────────────────────────────────
        private void OnTopNavMenuItemClicked(string menu, string subItem)
            => FormNavigator.NavigateTo(this, menu, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to logout?",
                    "Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                FormNavigator.NavigateTo(this, "Logout");
        }
    }
}
