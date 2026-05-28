using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.Dashboard
{
    partial class DashboardForm
    {
        private System.ComponentModel.IContainer components = null;

        // ====== Sidebar ======================================================
        private Panel pnlSidebar;
        private Label lblSidebarTitle;
        private Label lblSidebarSub;

        // nav items
        private Panel navDashboard, navOrders, navQuotation, navComplaints,
                      navInventory, navLogistics, navStaff, navLogs,
                      navAccRecv, navAccPay, navSupplier, navCustomer;

        // ====== Top Nav ======================================================
        private Panel pnlTopNav;
        private Label lblBreadcrumb;
        private Label lblTopNavUser;
        private Panel pnlAvatar;
        private Label lblAvatar;
        private Button btnLogout;

        // ====== Main Content (scroll panel) ==================================
        private Panel pnlContent;

        // page title
        private Label lblPageTitle;
        private Label lblPageSub;

        // alert banner
        private Panel pnlAlert;
        private Label lblAlert;

        // KPI cards (row 1)
        private Panel pnlKpi1;
        private Panel kpiOrders, kpiDelivered, kpiQuotations, kpiLowStock;

        // KPI cards (row 2)
        private Panel pnlKpi2;
        private Panel kpiRevenue, kpiAR, kpiSuppliers, kpiCustomers;

        // Section panels
        private TableLayoutPanel tlpRow1, tlpRow2, tlpRow3;

        // DataGridViews
        private DataGridView dgvOrders, dgvQuotations, dgvShipments, dgvSuppliers;

        // Activity feed
        private Panel pnlActivity;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // ================================================================
            // FORM
            // ================================================================
            this.Text            = "Premium Living OPS 2.0 — Dashboard";
            this.Size            = new Size(1380, 820);
            this.MinimumSize     = new Size(1024, 600);
            this.StartPosition   = FormStartPosition.CenterScreen;
            this.BackColor       = Palette.BgPage;
            this.WindowState     = FormWindowState.Maximized;
            this.Font            = new Font("Segoe UI", 9f);

            // ================================================================
            // SIDEBAR  (width 240, dark navy)
            // ================================================================
            pnlSidebar = new Panel
            {
                Dock      = DockStyle.Left,
                Width     = 240,
                BackColor = Palette.SidebarBg
            };

            // Logo block
            Panel pnlLogo = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 62,
                BackColor = Palette.SidebarBg,
                Padding   = new Padding(18, 14, 18, 8)
            };
            lblSidebarTitle = new Label
            {
                Text      = "🪑 PLF System",
                Font      = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize  = false,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.TopLeft
            };
            lblSidebarSub = new Label
            {
                Text      = "Premium Living Furniture Co.",
                Font      = new Font("Segoe UI", 7.5f),
                ForeColor = Color.FromArgb(122, 154, 189),
                AutoSize  = false,
                Dock      = DockStyle.Bottom,
                Height    = 16,
                TextAlign = ContentAlignment.BottomLeft
            };
            pnlLogo.Controls.Add(lblSidebarTitle);
            pnlLogo.Controls.Add(lblSidebarSub);

            Panel pnlLogoDivider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(30, 53, 88) };

            // Build nav
            FlowLayoutPanel navFlow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                AutoScroll    = true,
                WrapContents  = false,
                Width         = 240,
                BackColor     = Palette.SidebarBg
            };

            navDashboard  = MakeNavItem("🏠", "Dashboard",           true);
            navOrders     = MakeNavItem("📊", "Order Tracking",      false);
            navQuotation  = MakeNavItem("📋", "Quotation",           false);
            navComplaints = MakeNavItem("🔍", "Complaint Tracker",   false);
            navInventory  = MakeNavItem("📦", "Inventory",           false);
            navLogistics  = MakeNavItem("🚚", "Logistics",           false);
            navSupplier   = MakeNavItem("🏢", "Supplier List",       false);
            navCustomer   = MakeNavItem("👥", "Customer List",       false);
            navStaff      = MakeNavItem("👤", "User Accounts",       false);
            navLogs       = MakeNavItem("🔐", "Audit Logs",          false);
            navAccRecv    = MakeNavItem("💰", "Accounts Receivable", false);
            navAccPay     = MakeNavItem("💳", "Accounts Payable",    false);

            // Section headers
            navFlow.Controls.Add(MakeNavSection("MAIN"));
            navFlow.Controls.Add(navDashboard);
            navFlow.Controls.Add(MakeNavSection("SALES"));
            navFlow.Controls.Add(navOrders);
            navFlow.Controls.Add(navQuotation);
            navFlow.Controls.Add(navComplaints);
            navFlow.Controls.Add(MakeNavSection("STOCK"));
            navFlow.Controls.Add(navInventory);
            navFlow.Controls.Add(MakeNavSection("LOGISTICS"));
            navFlow.Controls.Add(navLogistics);
            navFlow.Controls.Add(MakeNavSection("MASTER DATA"));
            navFlow.Controls.Add(navSupplier);
            navFlow.Controls.Add(navCustomer);
            navFlow.Controls.Add(MakeNavSection("SYSTEM"));
            navFlow.Controls.Add(navStaff);
            navFlow.Controls.Add(navLogs);
            navFlow.Controls.Add(MakeNavSection("ACCOUNTING"));
            navFlow.Controls.Add(navAccRecv);
            navFlow.Controls.Add(navAccPay);

            _activeNavItem = navDashboard;

            pnlSidebar.Controls.Add(navFlow);
            pnlSidebar.Controls.Add(pnlLogoDivider);
            pnlSidebar.Controls.Add(pnlLogo);

            // ================================================================
            // MAIN AREA  (right of sidebar)
            // ================================================================
            Panel pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage };

            // ---- Top Nav bar ------------------------------------------------
            pnlTopNav = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 56,
                BackColor = Color.White
            };
            Panel topNavBorder = new Panel
            {
                Dock = DockStyle.Bottom, Height = 1, BackColor = Palette.BorderColor
            };

            lblBreadcrumb = new Label
            {
                Text      = "Dashboard",
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Palette.TextMain,
                AutoSize  = true,
                Location  = new Point(22, 18)
            };

            lblTopNavUser = new Label
            {
                Text      = "...",
                Font      = new Font("Segoe UI", 9f),
                ForeColor = Palette.TextMuted,
                AutoSize  = true,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };

            pnlAvatar = new Panel
            {
                Width     = 32,
                Height    = 32,
                BackColor = Palette.Primary,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };
            pnlAvatar.Region = System.Drawing.Region.FromHrgn(
                CreateEllipseRgn(0, 0, 32, 32));

            lblAvatar = new Label
            {
                Text      = "?",
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock      = DockStyle.Fill
            };
            pnlAvatar.Controls.Add(lblAvatar);

            btnLogout = new Button
            {
                Text      = "Log Out",
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = Palette.Danger,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(68, 28),
                Anchor    = AnchorStyles.Top | AnchorStyles.Right,
                Cursor    = Cursors.Hand
            };
            btnLogout.FlatAppearance.BorderColor = Palette.Danger;
            btnLogout.Click += btnLogout_Click;

            // Lay out top-nav controls on Resize
            pnlTopNav.Resize += (s, e) =>
            {
                btnLogout.Location     = new Point(pnlTopNav.Width - 84,  14);
                pnlAvatar.Location     = new Point(pnlTopNav.Width - 162, 12);
                lblTopNavUser.Location = new Point(pnlTopNav.Width - 230, 20);
            };

            pnlTopNav.Controls.Add(lblBreadcrumb);
            pnlTopNav.Controls.Add(lblTopNavUser);
            pnlTopNav.Controls.Add(pnlAvatar);
            pnlTopNav.Controls.Add(btnLogout);
            pnlTopNav.Controls.Add(topNavBorder);

            // ---- Scrollable content -----------------------------------------
            pnlContent = new Panel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                Padding    = new Padding(22, 18, 22, 22),
                BackColor  = Palette.BgPage
            };

            // Page title
            lblPageTitle = new Label
            {
                Text     = "Dashboard",
                Font     = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor= Palette.TextMain,
                AutoSize = true
            };
            lblPageSub = new Label
            {
                Text     = "...",
                Font     = new Font("Segoe UI", 9f),
                ForeColor= Palette.TextMuted,
                AutoSize = true
            };

            // Alert banner
            pnlAlert = new Panel
            {
                Height    = 38,
                BackColor = Color.FromArgb(254, 243, 199),
                Padding   = new Padding(12, 0, 12, 0)
            };
            Panel alertBorder = new Panel
            { Dock = DockStyle.Left, Width = 4, BackColor = Palette.Warning };
            lblAlert = new Label
            {
                Text      = "⚠️  9 items are currently below minimum stock threshold.",
                Font      = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(120, 53, 15),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(8, 0, 0, 0)
            };
            pnlAlert.Controls.Add(lblAlert);
            pnlAlert.Controls.Add(alertBorder);

            // ---- KPI Row 1 --------------------------------------------------
            pnlKpi1 = new Panel { Height = 96, BackColor = Color.Transparent };
            TableLayoutPanel tlpKpi1 = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1,
                BackColor = Color.Transparent
            };
            for (int i = 0; i < 4; i++)
                tlpKpi1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tlpKpi1.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            kpiOrders     = MakeKpiCard(Palette.Primary);
            kpiDelivered  = MakeKpiCard(Palette.Success);
            kpiQuotations = MakeKpiCard(Palette.Warning);
            kpiLowStock   = MakeKpiCard(Palette.Danger);

            tlpKpi1.Controls.Add(kpiOrders,     0, 0);
            tlpKpi1.Controls.Add(kpiDelivered,  1, 0);
            tlpKpi1.Controls.Add(kpiQuotations, 2, 0);
            tlpKpi1.Controls.Add(kpiLowStock,   3, 0);
            pnlKpi1.Controls.Add(tlpKpi1);

            // ---- KPI Row 2 --------------------------------------------------
            pnlKpi2 = new Panel { Height = 96, BackColor = Color.Transparent };
            TableLayoutPanel tlpKpi2 = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1,
                BackColor = Color.Transparent
            };
            for (int i = 0; i < 4; i++)
                tlpKpi2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tlpKpi2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            kpiRevenue   = MakeKpiCard(Palette.Info);
            kpiAR        = MakeKpiCard(Palette.Warning);
            kpiSuppliers = MakeKpiCard(Palette.Primary);
            kpiCustomers = MakeKpiCard(Palette.Primary);

            tlpKpi2.Controls.Add(kpiRevenue,   0, 0);
            tlpKpi2.Controls.Add(kpiAR,        1, 0);
            tlpKpi2.Controls.Add(kpiSuppliers, 2, 0);
            tlpKpi2.Controls.Add(kpiCustomers, 3, 0);
            pnlKpi2.Controls.Add(tlpKpi2);

            // ---- Section row 1: Orders + Low Stock -------------------------
            tlpRow1 = MakeSectionRow();
            Panel secOrders   = MakeSectionCard("Recent Orders");
            Panel secLowStock = MakeSectionCard("⚠️ Low Stock Alerts");

            dgvOrders    = MakeDgv(new[] { "Order No.", "Customer", "Total", "Status" });
            dgvOrders.CellPainting += dgvOrders_CellPainting;
            secOrders.Controls.Add(dgvOrders);

            // Low-stock DGV (4 cols: Item, On Hand, Min, Status)
            dgvShipments = null; // placeholder; real low-stock grid reuses slot
            DataGridView dgvLowStock = MakeDgv(new[] { "Item", "On Hand", "Min", "Status" });
            AddLowStockRows(dgvLowStock);
            secLowStock.Controls.Add(dgvLowStock);

            tlpRow1.Controls.Add(secOrders,   0, 0);
            tlpRow1.Controls.Add(secLowStock, 1, 0);

            // ---- Section row 2: Quotations + Shipments ---------------------
            tlpRow2 = MakeSectionRow();
            Panel secQuot = MakeSectionCard("Pending Quotations");
            Panel secShip = MakeSectionCard("Active Shipments");

            dgvQuotations = MakeDgv(new[] { "Quotation No.", "Customer", "Amount", "Valid Until" });
            dgvShipments  = MakeDgv(new[] { "Shipment ID", "Customer", "Sched. Date", "Status" });
            dgvShipments.CellPainting += dgvShipments_CellPainting;

            secQuot.Controls.Add(dgvQuotations);
            secShip.Controls.Add(dgvShipments);

            tlpRow2.Controls.Add(secQuot, 0, 0);
            tlpRow2.Controls.Add(secShip, 1, 0);

            // ---- Section row 3: Supplier Payments + Activity ---------------
            tlpRow3 = MakeSectionRow();
            Panel secSup = MakeSectionCard("Supplier Payment Status");
            Panel secAct = MakeSectionCard("Recent Activity");

            dgvSuppliers = MakeDgv(new[] { "Supplier", "Invoice", "Amount", "Status" });
            dgvSuppliers.CellPainting += dgvSuppliers_CellPainting;

            pnlActivity = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor  = Color.Transparent
            };

            secSup.Controls.Add(dgvSuppliers);
            secAct.Controls.Add(pnlActivity);

            tlpRow3.Controls.Add(secSup, 0, 0);
            tlpRow3.Controls.Add(secAct, 1, 0);

            // ---- Assemble content panel (stack from bottom to top via Dock.Top)
            // We use a FlowLayoutPanel for vertical stacking
            FlowLayoutPanel flow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                AutoSize      = true,
                AutoSizeMode  = AutoSizeMode.GrowAndShrink,
                BackColor     = Color.Transparent
            };

            // Spacing helper
            System.Action<int> addSpacer = (h) => flow.Controls.Add(
                new Panel { Height = h, Width = 10, BackColor = Color.Transparent });

            flow.Controls.Add(lblPageTitle);
            addSpacer(4);
            flow.Controls.Add(lblPageSub);
            addSpacer(12);
            flow.Controls.Add(pnlAlert);
            addSpacer(18);
            flow.Controls.Add(pnlKpi1);
            addSpacer(12);
            flow.Controls.Add(pnlKpi2);
            addSpacer(16);
            flow.Controls.Add(tlpRow1);
            addSpacer(14);
            flow.Controls.Add(tlpRow2);
            addSpacer(14);
            flow.Controls.Add(tlpRow3);

            pnlContent.Controls.Add(flow);

            // ---- Wire up resize to adjust fluid widths ----------------------
            pnlContent.Resize += (s, e) =>
            {
                int w = pnlContent.ClientSize.Width - pnlContent.Padding.Horizontal;
                flow.Width    = w;
                pnlAlert.Width = w;
                pnlKpi1.Width  = w;
                pnlKpi2.Width  = w;
                tlpRow1.Width  = w;
                tlpRow2.Width  = w;
                tlpRow3.Width  = w;
            };

            pnlMain.Controls.Add(pnlContent);
            pnlMain.Controls.Add(pnlTopNav);

            this.Controls.Add(pnlMain);
            this.Controls.Add(pnlSidebar);

            this.ResumeLayout(false);
        }

        // ====================================================================
        // FACTORY HELPERS
        // ====================================================================

        private Panel MakeNavItem(string icon, string label, bool active)
        {
            Panel p = new Panel
            {
                Width     = 240,
                Height    = 36,
                BackColor = active ? Palette.SidebarHover : Color.Transparent,
                Cursor    = Cursors.Hand,
                Padding   = new Padding(0)
            };

            // Left accent bar
            Panel accent = new Panel
            {
                Dock      = DockStyle.Left,
                Width     = 3,
                BackColor = active ? Palette.Primary : Color.Transparent
            };

            Label lblIcon = new Label
            {
                Text      = icon,
                Font      = new Font("Segoe UI", 11f),
                ForeColor = active ? Color.White : Palette.SidebarText,
                AutoSize  = false,
                Width     = 28,
                Height    = 36,
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblIcon.Location = new Point(10, 0);

            Label lblText = new Label
            {
                Text      = label,
                Font      = new Font("Segoe UI", 9.5f),
                ForeColor = active ? Color.White : Palette.SidebarText,
                AutoSize  = false,
                Width     = 185,
                Height    = 36,
                TextAlign = ContentAlignment.MiddleLeft
            };
            lblText.Location = new Point(42, 0);

            p.Controls.Add(accent);
            p.Controls.Add(lblIcon);
            p.Controls.Add(lblText);

            // Hover
            p.MouseEnter += (s, e) => { if (p != _activeNavItem) p.BackColor = Palette.SidebarHover; };
            p.MouseLeave += (s, e) => { if (p != _activeNavItem) p.BackColor = Color.Transparent; };
            p.Click += (s, e) => SetActiveNav(p);
            lblIcon.Click += (s, e) => { p.PerformClick(); };
            lblText.Click += (s, e) => { p.PerformClick(); };

            return p;
        }

        private Label MakeNavSection(string title)
        {
            return new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI", 7.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(74, 96, 128),
                AutoSize  = false,
                Width     = 240,
                Height    = 24,
                TextAlign = ContentAlignment.BottomLeft,
                Padding   = new Padding(14, 0, 0, 0)
            };
        }

        /// <summary>Creates a KPI card panel with accent border-top.</summary>
        private Panel MakeKpiCard(Color accent)
        {
            Panel card = new Panel
            {
                Dock      = DockStyle.Fill,
                Margin    = new Padding(0, 0, 8, 0),
                BackColor = Palette.BgCard,
                Padding   = new Padding(18, 14, 18, 10)
            };
            card.Paint += (s, e) =>
            {
                e.Graphics.FillRectangle(new SolidBrush(accent),
                    0, 0, ((Panel)s).Width, 4);
                var pen = new System.Drawing.Pen(Palette.BorderColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, ((Panel)s).Width - 1, ((Panel)s).Height - 1);
            };

            Label lblLabel = new Label
            {
                Tag       = "kpi-label",
                Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Palette.TextMuted,
                AutoSize  = false,
                Width     = 180,
                Height    = 18,
                Location  = new Point(18, 14),
                TextAlign = ContentAlignment.TopLeft
            };

            Label lblValue = new Label
            {
                Tag       = "kpi-value",
                Font      = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = accent,
                AutoSize  = false,
                Width     = 180,
                Height    = 32,
                Location  = new Point(16, 32),
                TextAlign = ContentAlignment.TopLeft
            };

            Label lblSub = new Label
            {
                Tag       = "kpi-sub",
                Font      = new Font("Segoe UI", 7.5f),
                ForeColor = Palette.TextMuted,
                AutoSize  = false,
                Width     = 180,
                Height    = 18,
                Location  = new Point(18, 66),
                TextAlign = ContentAlignment.TopLeft
            };

            card.Controls.Add(lblLabel);
            card.Controls.Add(lblValue);
            card.Controls.Add(lblSub);
            return card;
        }

        /// <summary>Creates a 2-column equal-width section row.</summary>
        private TableLayoutPanel MakeSectionRow()
        {
            var tlp = new TableLayoutPanel
            {
                Height       = 240,
                ColumnCount  = 2,
                RowCount     = 1,
                BackColor    = Color.Transparent,
                Margin       = new Padding(0)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            return tlp;
        }

        /// <summary>Creates a white section card with a title header.</summary>
        private Panel MakeSectionCard(string title)
        {
            Panel card = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Palette.BgCard,
                Margin    = new Padding(0, 0, 8, 0),
                Padding   = new Padding(0)
            };
            card.Paint += (s, e) =>
            {
                var pen = new System.Drawing.Pen(Palette.BorderColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, ((Panel)s).Width - 1, ((Panel)s).Height - 1);
            };

            // Header bar
            Panel header = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 42,
                BackColor = Palette.BgCard,
                Padding   = new Padding(16, 0, 16, 0)
            };
            Panel headerDiv = new Panel
            { Dock = DockStyle.Bottom, Height = 1, BackColor = Palette.BorderColor };
            Label lblTitle = new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Palette.TextMain,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            header.Controls.Add(lblTitle);
            header.Controls.Add(headerDiv);

            card.Controls.Add(header);
            return card;
        }

        /// <summary>Creates a styled DataGridView with given column headers.</summary>
        private DataGridView MakeDgv(string[] columns)
        {
            var dgv = new DataGridView
            {
                Dock                  = DockStyle.Fill,
                ReadOnly              = true,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible     = false,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor       = Palette.BgCard,
                BorderStyle           = BorderStyle.None,
                GridColor             = Palette.BorderColor,
                Font                  = new Font("Segoe UI", 8.5f),
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal
            };

            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(246, 249, 255),
                ForeColor = Palette.TextMuted,
                Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                Padding   = new Padding(4, 4, 4, 4)
            };
            dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor        = Palette.BgCard,
                ForeColor        = Palette.TextMain,
                SelectionBackColor = Color.FromArgb(240, 246, 255),
                SelectionForeColor = Palette.TextMain,
                Padding           = new Padding(6, 4, 6, 4)
            };
            dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(248, 250, 253)
            };

            foreach (string col in columns)
                dgv.Columns.Add(new DataGridViewTextBoxColumn
                {
                    HeaderText = col,
                    Name       = col.Replace(" ", "_"),
                    SortMode   = DataGridViewColumnSortMode.NotSortable
                });

            return dgv;
        }

        private void AddLowStockRows(DataGridView dgv)
        {
            dgv.Rows.Add("Solid Oak Panel (IID-R-0001)",         "8",  "20", "Critical");
            dgv.Rows.Add("High-density Foam (IID-R-0002)",       "3",  "15", "Critical");
            dgv.Rows.Add("5-Door Wardrobe (IID-P-0005)",         "5",  "8",  "Low");
            dgv.Rows.Add("Steel Bolt Set (IID-R-0003)",          "12", "50", "Critical");
            dgv.Rows.Add("Queen Size Oak Bed Frame (IID-P-0002)","8",  "10", "Low");

            foreach (DataGridViewRow row in dgv.Rows)
            {
                string status = row.Cells[3].Value?.ToString();
                if (status == "Critical")
                    row.Tag = new[] { Palette.TagRedBg, Palette.TagRedFg };
                else
                    row.Tag = new[] { Palette.TagYellowBg, Palette.TagYellowFg };
            }

            dgv.CellPainting += (s, e) => PaintStatusCell(s, e, 3);
        }
    }
}
