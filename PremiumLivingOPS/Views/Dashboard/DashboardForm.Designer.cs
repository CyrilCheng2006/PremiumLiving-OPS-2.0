using System.Drawing;
using System.Drawing.Drawing2D;
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
        private Panel navDashboard;

        // ====== Top Nav ======================================================
        private Panel  pnlTopNav;
        private Label  lblBreadcrumb;
        private Label  lblTopNavUser;
        private Panel  pnlAvatar;
        private Label  lblAvatar;
        private Button btnLogout;

        // ====== Main Content =================================================
        private Panel pnlContent;
        private Label lblPageTitle;
        private Label lblPageSub;
        private Panel pnlAlert;
        private Label lblAlert;

        private Panel pnlKpi1;
        private Panel kpiOrders, kpiDelivered, kpiQuotations, kpiLowStock;
        private Panel pnlKpi2;
        private Panel kpiRevenue, kpiAR, kpiSuppliers, kpiCustomers;

        private TableLayoutPanel tlpRow1, tlpRow2, tlpRow3;
        private DataGridView dgvOrders, dgvQuotations, dgvShipments, dgvSuppliers;
        private Panel pnlActivity;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // FORM — base font bumped to 11pt so every inherited control scales up
            this.Text          = "Premium Living OPS 2.0 \u2014 Dashboard";
            this.Size          = new Size(1440, 900);
            this.MinimumSize   = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = Palette.BgPage;
            this.WindowState   = FormWindowState.Maximized;
            this.Font          = new Font("Segoe UI", 11f);  // was 9f — ALL inherited controls scale

            // ================================================================
            // SIDEBAR  (width 270 to accommodate larger text)
            // ================================================================
            pnlSidebar = new Panel { Dock = DockStyle.Left, Width = 270, BackColor = Palette.SidebarBg };

            Panel pnlLogo = new Panel
            {
                Dock = DockStyle.Top, Height = 72, BackColor = Palette.SidebarBg,
                Padding = new Padding(18, 16, 18, 8)
            };
            lblSidebarTitle = new Label
            {
                Text = "\uD83E\uDE91 PLF System",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White, AutoSize = false, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopLeft
            };
            lblSidebarSub = new Label
            {
                Text = "Premium Living Furniture Co.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(122, 154, 189), AutoSize = false,
                Dock = DockStyle.Bottom, Height = 20, TextAlign = ContentAlignment.BottomLeft
            };
            pnlLogo.Controls.Add(lblSidebarTitle);
            pnlLogo.Controls.Add(lblSidebarSub);

            Panel pnlLogoDivider = new Panel
            { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(30, 53, 88) };

            FlowLayoutPanel navFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
                AutoScroll = true, WrapContents = false, Width = 270,
                BackColor = Palette.SidebarBg
            };

            navDashboard = MakeNavItem("\uD83C\uDFE0", "Dashboard", true);
            navFlow.Controls.Add(navDashboard);

            navFlow.Controls.Add(MakeNavGroup("\uD83D\uDCCB  1. ORDER PROCESSING MGT"));
            navFlow.Controls.Add(MakeNavSubItem("View & Search Order"));
            navFlow.Controls.Add(MakeNavSubItem("Quotation"));
            navFlow.Controls.Add(MakeNavSubItem("Create Order"));
            navFlow.Controls.Add(MakeNavSubItem("Modify Order"));

            navFlow.Controls.Add(MakeNavGroup("\uD83C\uDFED  2. PRODUCTION PROCESSING MGT"));
            navFlow.Controls.Add(MakeNavSubItem("Search Raw Material Request"));
            navFlow.Controls.Add(MakeNavSubItem("Create Raw Material Request"));

            navFlow.Controls.Add(MakeNavGroup("\uD83D\uDE9A  3. LOGISTICS PROCESSING MGT"));
            navFlow.Controls.Add(MakeNavSubItem("View Shipment"));
            navFlow.Controls.Add(MakeNavSubItem("Handling Goods Received"));

            navFlow.Controls.Add(MakeNavGroup("\uD83D\uDCE6  4. INVENTORY CONTROL MGT"));
            navFlow.Controls.Add(MakeNavSubItem("View Product / Raw Material"));

            navFlow.Controls.Add(MakeNavGroup("\uD83E\uDEB5  5. RAW MATERIAL MGT"));
            navFlow.Controls.Add(MakeNavSubItem("Create Procurement"));
            navFlow.Controls.Add(MakeNavSubItem("Search & List Procurement"));

            navFlow.Controls.Add(MakeNavGroup("\uD83D\uDEE0\uFE0F  6. AFTER-SERVICE MGT"));
            navFlow.Controls.Add(MakeNavSubItem("Create Invoice"));
            navFlow.Controls.Add(MakeNavSubItem("Complaint List"));
            navFlow.Controls.Add(MakeNavSubItem("Return Order List"));
            navFlow.Controls.Add(MakeNavSubItem("Account Receivable"));
            navFlow.Controls.Add(MakeNavSubItem("Account Payable"));

            navFlow.Controls.Add(MakeNavGroup("\uD83D\uDDC2\uFE0F  7. MASTER DATA MAINTENANCE"));
            navFlow.Controls.Add(MakeNavSubItem("Supplier List"));
            navFlow.Controls.Add(MakeNavSubItem("Customer List"));

            navFlow.Controls.Add(MakeNavGroup("\uD83D\uDD10  8. SYSTEM SECURITY & CONTROL"));
            navFlow.Controls.Add(MakeNavSubItem("Staff List"));
            navFlow.Controls.Add(MakeNavSubItem("Log List"));

            navFlow.Controls.Add(MakeNavGroup("\uD83D\uDCCA  9. STATISTICAL REPORTS"));
            navFlow.Controls.Add(MakeNavSubItem("View Report"));

            _activeNavItem = navDashboard;

            pnlSidebar.Controls.Add(navFlow);
            pnlSidebar.Controls.Add(pnlLogoDivider);
            pnlSidebar.Controls.Add(pnlLogo);

            // ================================================================
            // MAIN AREA
            // ================================================================
            Panel pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage };

            // Top Nav (taller for larger font)
            pnlTopNav = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.White };
            Panel topNavBorder = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Palette.BorderColor };

            lblBreadcrumb = new Label
            {
                Text = "Dashboard", Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Palette.TextMain, AutoSize = true, Location = new Point(22, 20)
            };
            lblTopNavUser = new Label
            {
                Text = "...", Font = new Font("Segoe UI", 11f), ForeColor = Palette.TextMuted,
                AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            pnlAvatar = new Panel
            {
                Width = 38, Height = 38, BackColor = Palette.Primary,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            pnlAvatar.Region = MakeCircleRegion(38, 38);

            lblAvatar = new Label
            {
                Text = "?", Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White, TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            pnlAvatar.Controls.Add(lblAvatar);

            btnLogout = new Button
            {
                Text = "Log Out", Font = new Font("Segoe UI", 10f),
                ForeColor = Palette.Danger, BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat, Size = new Size(80, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Right, Cursor = Cursors.Hand
            };
            btnLogout.FlatAppearance.BorderColor = Palette.Danger;
            btnLogout.Click += btnLogout_Click;

            pnlTopNav.Resize += (s, e) =>
            {
                btnLogout.Location     = new Point(pnlTopNav.Width - 96,  16);
                pnlAvatar.Location     = new Point(pnlTopNav.Width - 186, 13);
                lblTopNavUser.Location = new Point(pnlTopNav.Width - 260, 22);
            };

            pnlTopNav.Controls.Add(lblBreadcrumb);
            pnlTopNav.Controls.Add(lblTopNavUser);
            pnlTopNav.Controls.Add(pnlAvatar);
            pnlTopNav.Controls.Add(btnLogout);
            pnlTopNav.Controls.Add(topNavBorder);

            // Scrollable content
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill, AutoScroll = true,
                Padding = new Padding(26, 20, 26, 26), BackColor = Palette.BgPage
            };

            lblPageTitle = new Label
            {
                Text = "Dashboard", Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = Palette.TextMain, AutoSize = true
            };
            lblPageSub = new Label
            {
                Text = "...", Font = new Font("Segoe UI", 11f),
                ForeColor = Palette.TextMuted, AutoSize = true
            };

            // Alert banner
            pnlAlert = new Panel
            {
                Height = 46, BackColor = Color.FromArgb(254, 243, 199),
                Padding = new Padding(12, 0, 12, 0)
            };
            Panel alertBorder = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = Palette.Warning };
            lblAlert = new Label
            {
                Text = "\u26A0\uFE0F  9 items are currently below minimum stock threshold.",
                Font = new Font("Segoe UI", 11f), ForeColor = Color.FromArgb(120, 53, 15),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };
            pnlAlert.Controls.Add(lblAlert);
            pnlAlert.Controls.Add(alertBorder);

            // KPI Row 1
            pnlKpi1 = new Panel { Height = 110, BackColor = Color.Transparent };
            TableLayoutPanel tlpKpi1 = new TableLayoutPanel
            { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, BackColor = Color.Transparent };
            for (int i = 0; i < 4; i++) tlpKpi1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tlpKpi1.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            kpiOrders     = MakeKpiCard(Palette.Primary);
            kpiDelivered  = MakeKpiCard(Palette.Success);
            kpiQuotations = MakeKpiCard(Palette.Warning);
            kpiLowStock   = MakeKpiCard(Palette.Danger);
            tlpKpi1.Controls.Add(kpiOrders, 0, 0); tlpKpi1.Controls.Add(kpiDelivered,  1, 0);
            tlpKpi1.Controls.Add(kpiQuotations, 2, 0); tlpKpi1.Controls.Add(kpiLowStock, 3, 0);
            pnlKpi1.Controls.Add(tlpKpi1);

            // KPI Row 2
            pnlKpi2 = new Panel { Height = 110, BackColor = Color.Transparent };
            TableLayoutPanel tlpKpi2 = new TableLayoutPanel
            { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, BackColor = Color.Transparent };
            for (int i = 0; i < 4; i++) tlpKpi2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tlpKpi2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            kpiRevenue   = MakeKpiCard(Palette.Info);
            kpiAR        = MakeKpiCard(Palette.Warning);
            kpiSuppliers = MakeKpiCard(Palette.Primary);
            kpiCustomers = MakeKpiCard(Palette.Primary);
            tlpKpi2.Controls.Add(kpiRevenue, 0, 0); tlpKpi2.Controls.Add(kpiAR,        1, 0);
            tlpKpi2.Controls.Add(kpiSuppliers, 2, 0); tlpKpi2.Controls.Add(kpiCustomers, 3, 0);
            pnlKpi2.Controls.Add(tlpKpi2);

            // Section Row 1
            tlpRow1 = MakeSectionRow();
            Panel secOrders   = MakeSectionCard("Recent Orders");
            Panel secLowStock = MakeSectionCard("\u26A0\uFE0F Low Stock Alerts");
            dgvOrders = MakeDgv(new[] { "Order No.", "Customer", "Total", "Status" });
            dgvOrders.CellPainting += dgvOrders_CellPainting;
            secOrders.Controls.Add(dgvOrders);
            DataGridView dgvLowStock = MakeDgv(new[] { "Item", "On Hand", "Min", "Status" });
            AddLowStockRows(dgvLowStock);
            secLowStock.Controls.Add(dgvLowStock);
            tlpRow1.Controls.Add(secOrders, 0, 0);
            tlpRow1.Controls.Add(secLowStock, 1, 0);

            // Section Row 2
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

            // Section Row 3
            tlpRow3 = MakeSectionRow();
            Panel secSup = MakeSectionCard("Supplier Payment Status");
            Panel secAct = MakeSectionCard("Recent Activity");
            dgvSuppliers = MakeDgv(new[] { "Supplier", "Invoice", "Amount", "Status" });
            dgvSuppliers.CellPainting += dgvSuppliers_CellPainting;
            pnlActivity = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Transparent };
            secSup.Controls.Add(dgvSuppliers);
            secAct.Controls.Add(pnlActivity);
            tlpRow3.Controls.Add(secSup, 0, 0);
            tlpRow3.Controls.Add(secAct, 1, 0);

            // Assemble flow
            FlowLayoutPanel flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, FlowDirection = FlowDirection.TopDown,
                WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent
            };

            System.Action<int> addSpacer = (h) => flow.Controls.Add(
                new Panel { Height = h, Width = 10, BackColor = Color.Transparent });

            flow.Controls.Add(lblPageTitle); addSpacer(4);
            flow.Controls.Add(lblPageSub);   addSpacer(14);
            flow.Controls.Add(pnlAlert);     addSpacer(20);
            flow.Controls.Add(pnlKpi1);      addSpacer(14);
            flow.Controls.Add(pnlKpi2);      addSpacer(18);
            flow.Controls.Add(tlpRow1);      addSpacer(16);
            flow.Controls.Add(tlpRow2);      addSpacer(16);
            flow.Controls.Add(tlpRow3);

            pnlContent.Controls.Add(flow);

            pnlContent.Resize += (s, e) =>
            {
                int w = pnlContent.ClientSize.Width - pnlContent.Padding.Horizontal;
                flow.Width     = w; pnlAlert.Width = w;
                pnlKpi1.Width  = w; pnlKpi2.Width  = w;
                tlpRow1.Width  = w; tlpRow2.Width  = w; tlpRow3.Width  = w;
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
                Width = 270, Height = 42,
                BackColor = active ? Palette.SidebarHover : Color.Transparent,
                Cursor = Cursors.Hand
            };
            Panel accent = new Panel
            { Dock = DockStyle.Left, Width = 3, BackColor = active ? Palette.Primary : Color.Transparent };
            Label lblIcon = new Label
            {
                Text = icon, Font = new Font("Segoe UI", 13f),
                ForeColor = active ? Color.White : Palette.SidebarText,
                AutoSize = false, Width = 32, Height = 42,
                TextAlign = ContentAlignment.MiddleCenter, Location = new Point(10, 0)
            };
            Label lblText = new Label
            {
                Text = label, Font = new Font("Segoe UI", 11f),
                ForeColor = active ? Color.White : Palette.SidebarText,
                AutoSize = false, Width = 210, Height = 42,
                TextAlign = ContentAlignment.MiddleLeft, Location = new Point(46, 0)
            };
            p.Controls.Add(accent);
            p.Controls.Add(lblIcon);
            p.Controls.Add(lblText);

            p.MouseEnter  += (s, e) => { if (p != _activeNavItem) p.BackColor = Palette.SidebarHover; };
            p.MouseLeave  += (s, e) => { if (p != _activeNavItem) p.BackColor = Color.Transparent; };
            p.Click       += (s, e) => SetActiveNav(p);
            lblIcon.Click += (s, e) => SetActiveNav(p);
            lblText.Click += (s, e) => SetActiveNav(p);
            return p;
        }

        private Label MakeNavGroup(string title) => new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(74, 96, 128),
            AutoSize = false, Width = 270, Height = 30,
            TextAlign = ContentAlignment.BottomLeft,
            Padding = new Padding(14, 0, 0, 2),
            Margin = new Padding(0, 8, 0, 0)
        };

        private Panel MakeNavSubItem(string label)
        {
            Panel p = new Panel
            {
                Width = 270, Height = 36,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };

            Panel indentLine = new Panel
            {
                Width = 2, Height = 18,
                BackColor = Color.FromArgb(50, 80, 120),
                Location = new Point(22, 9)
            };

            Label lblText = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = Palette.SidebarText,
                AutoSize = false, Width = 224, Height = 36,
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(36, 0)
            };

            p.Controls.Add(indentLine);
            p.Controls.Add(lblText);

            p.MouseEnter       += (s, e) => { p.BackColor = Palette.SidebarHover; lblText.ForeColor = Color.White; };
            p.MouseLeave       += (s, e) => { p.BackColor = Color.Transparent;    lblText.ForeColor = Palette.SidebarText; };
            lblText.MouseEnter += (s, e) => { p.BackColor = Palette.SidebarHover; lblText.ForeColor = Color.White; };
            lblText.MouseLeave += (s, e) => { p.BackColor = Color.Transparent;    lblText.ForeColor = Palette.SidebarText; };

            System.EventHandler showComingSoon = (s, e) =>
                MessageBox.Show(
                    $"\u231B  {label}\n\nThis feature is currently under development.\nPlease check back in a later version.",
                    "Coming Soon",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

            p.Click       += showComingSoon;
            lblText.Click += showComingSoon;
            return p;
        }

        private Panel MakeKpiCard(Color accent)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 0),
                BackColor = Palette.BgCard, Padding = new Padding(20, 16, 20, 12)
            };
            card.Paint += (s, e) =>
            {
                e.Graphics.FillRectangle(new SolidBrush(accent), 0, 0, ((Panel)s).Width, 5);
                e.Graphics.DrawRectangle(new System.Drawing.Pen(Palette.BorderColor, 1),
                    0, 0, ((Panel)s).Width - 1, ((Panel)s).Height - 1);
            };
            Label lblLabel = new Label
            {
                Tag = "kpi-label", Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Palette.TextMuted, AutoSize = false, Width = 200, Height = 20,
                Location = new Point(20, 16), TextAlign = ContentAlignment.TopLeft
            };
            Label lblValue = new Label
            {
                Tag = "kpi-value", Font = new Font("Segoe UI", 24f, FontStyle.Bold),
                ForeColor = accent, AutoSize = false, Width = 200, Height = 38,
                Location = new Point(18, 36), TextAlign = ContentAlignment.TopLeft
            };
            Label lblSub = new Label
            {
                Tag = "kpi-sub", Font = new Font("Segoe UI", 9f),
                ForeColor = Palette.TextMuted, AutoSize = false, Width = 200, Height = 20,
                Location = new Point(20, 78), TextAlign = ContentAlignment.TopLeft
            };
            card.Controls.Add(lblLabel);
            card.Controls.Add(lblValue);
            card.Controls.Add(lblSub);
            return card;
        }

        private TableLayoutPanel MakeSectionRow()
        {
            var tlp = new TableLayoutPanel
            {
                Height = 280, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, Margin = new Padding(0)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            return tlp;
        }

        private Panel MakeSectionCard(string title)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill, BackColor = Palette.BgCard,
                Margin = new Padding(0, 0, 10, 0)
            };
            card.Paint += (s, e) =>
                e.Graphics.DrawRectangle(new System.Drawing.Pen(Palette.BorderColor, 1),
                    0, 0, ((Panel)s).Width - 1, ((Panel)s).Height - 1);

            Panel header = new Panel
            { Dock = DockStyle.Top, Height = 48, BackColor = Palette.BgCard, Padding = new Padding(18, 0, 18, 0) };
            Panel headerDiv = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Palette.BorderColor };
            Label lblTitle = new Label
            {
                Text = title, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Palette.TextMain, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            };
            header.Controls.Add(lblTitle);
            header.Controls.Add(headerDiv);
            card.Controls.Add(header);
            return card;
        }

        private DataGridView MakeDgv(string[] columns)
        {
            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true,
                AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Palette.BgCard, BorderStyle = BorderStyle.None,
                GridColor = Palette.BorderColor, Font = new Font("Segoe UI", 10.5f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate = { Height = 32 }
            };
            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(246, 249, 255), ForeColor = Palette.TextMuted,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Padding = new Padding(6)
            };
            dgv.ColumnHeadersHeight = 36;
            dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Palette.BgCard, ForeColor = Palette.TextMain,
                SelectionBackColor = Color.FromArgb(240, 246, 255), SelectionForeColor = Palette.TextMain,
                Padding = new Padding(8, 5, 8, 5)
            };
            dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            { BackColor = Color.FromArgb(248, 250, 253) };
            foreach (string col in columns)
                dgv.Columns.Add(new DataGridViewTextBoxColumn
                { HeaderText = col, Name = col.Replace(" ", "_"), SortMode = DataGridViewColumnSortMode.NotSortable });
            return dgv;
        }

        private void AddLowStockRows(DataGridView dgv)
        {
            dgv.Rows.Add("Solid Oak Panel (IID-R-0001)",          "8",  "20", "Critical");
            dgv.Rows.Add("High-density Foam (IID-R-0002)",        "3",  "15", "Critical");
            dgv.Rows.Add("5-Door Wardrobe (IID-P-0005)",          "5",  "8",  "Low");
            dgv.Rows.Add("Steel Bolt Set (IID-R-0003)",           "12", "50", "Critical");
            dgv.Rows.Add("Queen Size Oak Bed Frame (IID-P-0002)", "8",  "10", "Low");

            foreach (DataGridViewRow row in dgv.Rows)
            {
                string status = row.Cells[3].Value?.ToString();
                row.Tag = status == "Critical"
                    ? new[] { Palette.TagRedBg,    Palette.TagRedFg }
                    : new[] { Palette.TagYellowBg, Palette.TagYellowFg };
            }
            dgv.CellPainting += (s, e) => PaintStatusCell(s, e, 3);
        }
    }
}
