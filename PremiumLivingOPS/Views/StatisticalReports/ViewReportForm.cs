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
            this.Load += ViewReportForm_Load;
        }

        // ───────────────────────────────────────────────────
        //  Load
        // ───────────────────────────────────────────────────
        private void ViewReportForm_Load(object sender, EventArgs e)
        {
            RefreshShell();
            SwitchToReport(0);
        }

        // ───────────────────────────────────────────────────
        //  AppShell
        // ───────────────────────────────────────────────────
        private void RefreshShell()
        {
            var vm = _ctrl.GetSalesReportVM();
            if (vm == null) return;
            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Statistical Reports  \u203a  View Report");
        }

        // ───────────────────────────────────────────────────
        //  Tab switcher
        // ───────────────────────────────────────────────────
        internal void SwitchToReport(int tabIndex)
        {
            if (_activeTab == tabIndex) return;
            _activeTab = tabIndex;

            pnlContent.SuspendLayout();
            pnlContent.Controls.Clear();

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

            switch (tabIndex)
            {
                case 0: BuildSalesRevenueReport(); break;
                case 1: BuildInventoryReport();    break;
                case 2: BuildProductionReport();   break;
                case 3: BuildLogisticsReport();    break;
                case 4: BuildAfterServiceReport(); break;
            }

            pnlContent.ResumeLayout(true);
        }

        // ───────────────────────────────────────────────────
        //  1. Sales & Revenue
        // ───────────────────────────────────────────────────
        private void BuildSalesRevenueReport()
        {
            var vm = _ctrl.GetSalesReportVM();
            if (vm == null) return;

            var kpiPills = new[]
            {
                ("Total Orders",   vm.SalesKpi?.TotalOrders.ToString()     ?? "0",  Color.FromArgb(55,48,163),  Color.FromArgb(238,242,255)),
                ("Revenue",        $"${vm.SalesKpi?.TotalRevenue:F0}",              Color.FromArgb(6,95,70),    Color.FromArgb(209,250,229)),
                ("Avg Order",      $"${vm.SalesKpi?.AverageOrderValue:F0}",         Color.FromArgb(15,118,110), Color.FromArgb(204,251,241)),
                ("Delivered",      vm.SalesKpi?.DeliveredOrders.ToString()  ?? "0", Color.FromArgb(6,95,70),    Color.FromArgb(209,250,229)),
                ("Pending",        vm.SalesKpi?.PendingOrders.ToString()    ?? "0", Color.FromArgb(146,64,14),  Color.FromArgb(254,243,199)),
                ("Cancelled",      vm.SalesKpi?.CancelledOrders.ToString()  ?? "0", Color.FromArgb(71,85,105),  Color.FromArgb(241,245,249)),
            };

            var dgv = MakeGrid();
            foreach (var (hdr, name, w) in new[]
            {
                ("Order ID",    "colSOrderID",  120),
                ("Customer",    "colSCustomer", 200),
                ("Status",      "colSStatus",   130),
                ("Issued Date", "colSDate",     130),
                ("Grand Total", "colSTotal",    120),
                ("Lines",       "colSLines",     70),
            })
                dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = hdr, Name = name, Width = w });
            dgv.CellFormatting += (s, e) => ApplyStatusBadge(s, e, "colSStatus");
            if (vm.SalesRows != null)
                foreach (var r in vm.SalesRows)
                    dgv.Rows.Add(r.OrderID, r.CustomerName, r.OrderStatus,
                                 r.IssuedTime.ToString("yyyy-MM-dd"),
                                 $"${r.GrandTotal:F2}", r.LineCount);

            var dgvTop = MakeGrid();
            foreach (var (hdr, name, w) in new[]
            {
                ("Item ID",  "colTItemID", 110),
                ("Name",     "colTName",   200),
                ("Category", "colTCat",    130),
                ("Qty Sold", "colTQty",     90),
                ("Revenue",  "colTRev",    120),
            })
                dgvTop.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = hdr, Name = name, Width = w });
            if (vm.TopProducts != null)
                foreach (var p in vm.TopProducts)
                    dgvTop.Rows.Add(p.ItemID, p.ItemName, p.Category,
                                    p.TotalQty, $"${p.TotalRevenue:F2}");

            BuildTwoGridLayout("Sales & Revenue Report", kpiPills,
                               "Orders", dgv, "Top Products", dgvTop);
        }

        // ───────────────────────────────────────────────────
        //  2. Inventory
        // ───────────────────────────────────────────────────
        private void BuildInventoryReport()
        {
            var vm = _ctrl.GetInventoryReportVM();
            if (vm == null) return;

            var kpiPills = new[]
            {
                ("Total SKUs",    vm.InventoryKpi?.TotalSKUs.ToString()         ?? "0", Color.FromArgb(55,48,163),  Color.FromArgb(238,242,255)),
                ("Below Reorder", vm.InventoryKpi?.BelowReorderCount.ToString() ?? "0", Color.FromArgb(164,15,76),  Color.FromArgb(255,228,240)),
                ("Products",      vm.InventoryKpi?.ProductCount.ToString()       ?? "0", Color.FromArgb(6,95,70),    Color.FromArgb(209,250,229)),
                ("Raw Materials", vm.InventoryKpi?.RawMaterialCount.ToString()   ?? "0", Color.FromArgb(15,118,110), Color.FromArgb(204,251,241)),
            };

            var dgv = MakeGrid();
            foreach (var (hdr, name, w) in new[]
            {
                ("WH Item ID",    "colIWHID",    110),
                ("Item ID",       "colIItemID",  100),
                ("Name",          "colIName",    200),
                ("Category",      "colICat",     120),
                ("Material Type", "colIMat",     130),
                ("Warehouse",     "colIWH",      120),
                ("Location",      "colILoc",     130),
                ("Stock",         "colIStock",    80),
                ("Reorder Lvl",   "colIReorder",  90),
                ("Alert",         "colIAlert",    80),
            })
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
                        dgv.Rows[idx].DefaultCellStyle.ForeColor = Color.FromArgb(146, 64, 14);
                        dgv.Rows[idx].DefaultCellStyle.BackColor = Color.FromArgb(254, 243, 199);
                    }
                }

            BuildSingleGridLayout("Inventory Status Report", kpiPills, dgv);
        }

        // ───────────────────────────────────────────────────
        //  3. Procurement
        // ───────────────────────────────────────────────────
        private void BuildProductionReport()
        {
            var vm = _ctrl.GetProcurementReportVM();
            if (vm == null) return;

            var kpiPills = new[]
            {
                ("Total POs",   vm.ProcKpi?.TotalPOs.ToString()       ?? "0",  Color.FromArgb(55,48,163),  Color.FromArgb(238,242,255)),
                ("Total Spend", $"${vm.ProcKpi?.TotalSpend:F0}",               Color.FromArgb(146,64,14),  Color.FromArgb(254,243,199)),
                ("Completed",   vm.ProcKpi?.CompletedPOs.ToString()    ?? "0", Color.FromArgb(6,95,70),    Color.FromArgb(209,250,229)),
                ("Pending",     vm.ProcKpi?.PendingPOs.ToString()      ?? "0", Color.FromArgb(29,78,216),  Color.FromArgb(219,234,254)),
                ("Suppliers",   vm.ProcKpi?.UniqueSuppliers.ToString() ?? "0", Color.FromArgb(15,118,110), Color.FromArgb(204,251,241)),
            };

            var dgv = MakeGrid();
            foreach (var (hdr, name, w) in new[]
            {
                ("PO ID",      "colPPOID",  110),
                ("Supplier",   "colPSupp",  180),
                ("Status",     "colPSt",    130),
                ("Order Date", "colPDate",  120),
                ("Total",      "colPTotal", 110),
                ("Materials",  "colPMat",   220),
                ("Request ID", "colPReq",   110),
            })
                dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = hdr, Name = name, Width = w });
            dgv.CellFormatting += (s, e) => ApplyStatusBadge(s, e, "colPSt");
            if (vm.ProcRows != null)
                foreach (var r in vm.ProcRows)
                    dgv.Rows.Add(r.PurchaseID, r.SupplierName, r.PurchaseStatus,
                                 r.OrderDate.ToString("yyyy-MM-dd"),
                                 $"${r.POTotalAmount:F2}",
                                 r.MaterialNames, r.RequestID);

            BuildSingleGridLayout("Procurement Summary Report", kpiPills, dgv);
        }

        // ───────────────────────────────────────────────────
        //  4. Logistics
        // ───────────────────────────────────────────────────
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

            var dgv = MakeGrid();
            foreach (var (hdr, name, w) in new[]
            {
                ("Shipment ID", "colLShipID", 120),
                ("Order ID",    "colLOrdID",  110),
                ("Customer",    "colLCust",   180),
                ("Status",      "colLSt",     120),
                ("Type",        "colLType",   110),
                ("Method",      "colLMethod", 120),
                ("Ship Date",   "colLDate",   120),
                ("Amount",      "colLAmt",    110),
                ("Note",        "colLNote",    70),
                ("Reply Slip",  "colLSlip",    80),
            })
                dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = hdr, Name = name, Width = w });
            dgv.CellFormatting += (s, e) => ApplyStatusBadge(s, e, "colLSt");
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

        // ───────────────────────────────────────────────────
        //  5. After-Service
        // ───────────────────────────────────────────────────
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

            var dgvC = MakeGrid();
            foreach (var (hdr, name, w) in new[]
            {
                ("Complaint ID", "colCID",   120),
                ("Order ID",     "colCOrd",  110),
                ("Customer",     "colCCust", 180),
                ("Description",  "colCDesc", 280),
                ("Status",       "colCSt",   120),
            })
                dgvC.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = hdr, Name = name, Width = w });
            dgvC.CellFormatting += (s, e) => ApplyStatusBadge(s, e, "colCSt");
            if (vm.Complaints != null)
                foreach (var c in vm.Complaints)
                    dgvC.Rows.Add(c.ComplaintID, c.OrderID, c.CustomerName,
                                  c.ComplaintDescription, c.ComplaintStatus);

            var dgvR = MakeGrid();
            foreach (var (hdr, name, w) in new[]
            {
                ("Return ID",   "colRID",     110),
                ("Order ID",    "colROrd",    110),
                ("Customer",    "colRCust",   180),
                ("Reason",      "colRReason", 220),
                ("Refund",      "colRRef",    100),
                ("Status",      "colRSt",     120),
                ("Return Date", "colRDate",   120),
            })
                dgvR.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = hdr, Name = name, Width = w });
            dgvR.CellFormatting += (s, e) => ApplyStatusBadge(s, e, "colRSt");
            if (vm.Returns != null)
                foreach (var r in vm.Returns)
                    dgvR.Rows.Add(r.ReturnID, r.OrderID, r.CustomerName,
                                  r.Reason, $"${r.RefundAmount:F2}", r.ReturnStatus,
                                  r.ReturnDate == default ? "" : r.ReturnDate.ToString("yyyy-MM-dd"));

            BuildTwoGridLayout("After-Service Summary Report", kpiPills,
                               "Complaints", dgvC, "Returns", dgvR);
        }

        // ───────────────────────────────────────────────────
        //  Layout helpers
        // ───────────────────────────────────────────────────
        private void BuildSingleGridLayout(
            string title,
            (string label, string value, Color fg, Color bg)[] pills,
            DataGridView dgv)
        {
            var root = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };

            var kpiCard = MakeKpiCard(pills);
            kpiCard.Dock = DockStyle.Top;

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
            tbl.Controls.Add(dgv, 0, 1);
            gridInner.Controls.Add(tbl);

            root.Controls.Add(gridOuter);
            root.Controls.Add(kpiCard);
            pnlContent.Controls.Add(root);
        }

        /// <summary>
        /// Two-grid layout with SplitContainer.
        /// SplitterDistance is deferred to the first Layout event so that
        /// the control has a real Height before we calculate 60 %.
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
                Dock          = DockStyle.Fill,
                Orientation   = Orientation.Horizontal,
                BackColor     = Color.FromArgb(240, 244, 249),
                Panel1MinSize = 120,
                Panel2MinSize = 80,
                SplitterWidth = 6
            };

            splitter.Panel1.Controls.Add(MakeGridCard(label1, dgv1, title));
            splitter.Panel2.Controls.Add(MakeGridCard(label2, dgv2, ""));

            // Defer SplitterDistance: Height is 0 before first layout pass.
            // LayoutEventHandler matches SplitContainer.Layout event signature.
            LayoutEventHandler setDist = null;
            setDist = (s, e) =>
            {
                var sc = (SplitContainer)s;
                sc.Layout -= setDist;                         // one-shot: unsubscribe immediately
                int available = sc.Height - sc.SplitterWidth;
                int desired   = (int)(available * 0.6);
                int lo        = sc.Panel1MinSize;
                int hi        = available - sc.Panel2MinSize;
                if (hi > lo)                                  // guard: only set when range is valid
                    sc.SplitterDistance = Math.Max(lo, Math.Min(hi, desired));
            };
            splitter.Layout += setDist;

            root.Controls.Add(splitter);
            root.Controls.Add(kpiCard);
            pnlContent.Controls.Add(root);
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
                Text = string.IsNullOrEmpty(mainTitle) ? subTitle : $"{mainTitle}  \u203a  {subTitle}",
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

            var pnlKpi = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(12, 10, 12, 10)
            };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            foreach (var (label, value, fg, bg) in pills)
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
                    Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                    BackColor = Color.Transparent,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding = new Padding(10, 0, 8, 0)
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70f));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                tlp.Controls.Add(new Label
                {
                    Text      = value,
                    Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                    ForeColor = fg,
                    BackColor = Color.Transparent,
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize  = false
                }, 0, 0);
                tlp.Controls.Add(new Label
                {
                    Text      = label,
                    Font      = new Font("Segoe UI", 11f),
                    ForeColor = fg,
                    BackColor = Color.Transparent,
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoSize  = false
                }, 1, 0);

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
                Dock                        = DockStyle.Fill,
                BackgroundColor             = Color.White,
                BorderStyle                 = BorderStyle.None,
                RowHeadersVisible           = false,
                AllowUserToAddRows          = false,
                AllowUserToDeleteRows       = false,
                ReadOnly                    = true,
                SelectionMode               = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect                 = false,
                AutoSizeColumnsMode         = DataGridViewAutoSizeColumnsMode.None,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight         = 40,
                RowTemplate                 = { Height = 34 },
                Font                        = new Font("Segoe UI", 11f),
                GridColor                   = Color.FromArgb(221, 227, 236),
                EnableHeadersVisualStyles   = false
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
                ForeColor          = Color.FromArgb(15, 31, 53),
                BackColor          = Color.White,
                SelectionBackColor = Color.FromArgb(219, 234, 254),
                SelectionForeColor = Color.FromArgb(29, 78, 216),
                Padding            = new Padding(8, 0, 0, 0)
            };
            dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor          = Color.FromArgb(248, 250, 252),
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

        // ───────────────────────────────────────────────────
        //  Status Badge
        // ───────────────────────────────────────────────────
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
            e.CellStyle.Font               = new Font("Segoe UI", 11f, FontStyle.Bold);
            e.CellStyle.Alignment          = DataGridViewContentAlignment.MiddleCenter;
            e.FormattingApplied            = true;
        }

        // ───────────────────────────────────────────────────
        //  AppShell event handler
        // ───────────────────────────────────────────────────
        private void OnTopNavMenuItemClicked(string menu, string sub)
            => FormNavigator.NavigateTo(this, menu, sub);
    }
}
