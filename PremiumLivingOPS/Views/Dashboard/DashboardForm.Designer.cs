using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.Dashboard
{
    partial class DashboardForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── TopNavBar ──
        private TopNavBar pnlTopNav;

        // Controls also referenced by DashboardForm.cs
        private Label  lblBreadcrumb;
        private Label  lblTopNavUser;
        private Panel  pnlAvatar;
        private Label  lblAvatar;
        private Button btnLogout;

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

            // FORM
            this.Text          = "Premium Living OPS 2.0 — Dashboard";
            this.Size          = new Size(1440, 900);
            this.MinimumSize   = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = Palette.BgPage;
            this.WindowState   = FormWindowState.Maximized;
            this.Font          = new Font("Segoe UI", 16f);

            Panel pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage };

            // ── TopNavBar ──
            pnlTopNav = new TopNavBar();
            pnlTopNav.MenuItemClicked += OnTopNavMenuItemClicked;

            // ================================================================
            // User Bar — 56 px tall
            //
            // Element heights:
            //   pnlAvatar  36 px  → Y = (56-36)/2 = 10
            //   btnLogout  34 px  → Y = (56-34)/2 = 11
            //   lblTopNavUser ~26px → Y = (56-26)/2 = 15  (use 16 for optical)
            //   lblBreadcrumb ~29px → Y = (56-29)/2 = 13  (use 14 for optical)
            //
            // RIGHT-SIDE POSITIONS (X measured from right edge of bar):
            //   btnLogout  : right edge at barW-12, so X = barW - 12 - 96 = barW-108
            //   pnlAvatar  : 8px gap left of btn   → X = barW-108 - 8 - 36 = barW-152
            //   lblTopNavUser: 8px gap left of avatar (AutoSize=true, so anchor right)
            //
            // IMPORTANT: layoutUserBar() is called BOTH inside Resize AND
            // immediately after all controls are added so the first paint is
            // already correct (Resize does not fire during construction).
            // ================================================================
            const int UBH = 56;   // User Bar Height

            Panel pnlUserBar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = UBH,
                BackColor = System.Drawing.Color.White
            };
            Panel userBarBorder = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 1,
                BackColor = Palette.BorderColor
            };

            lblBreadcrumb = new Label
            {
                Text      = "Dashboard",
                Font      = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Palette.TextMain,
                AutoSize  = true,
                Location  = new Point(22, 14)
            };

            lblTopNavUser = new Label
            {
                Text      = "...",
                Font      = new Font("Segoe UI", 14.4f),
                ForeColor = Palette.TextMuted,
                AutoSize  = true
                // Location set by layoutUserBar()
            };

            pnlAvatar = new Panel
            {
                Width     = 36,
                Height    = 36,
                BackColor = Palette.Primary
                // Location set by layoutUserBar()
            };
            pnlAvatar.Region = MakeCircleRegion(36, 36);

            lblAvatar = new Label
            {
                Text      = "?",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = System.Drawing.Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock      = DockStyle.Fill
            };
            pnlAvatar.Controls.Add(lblAvatar);

            btnLogout = new Button
            {
                Text      = "Log Out",
                Font      = new Font("Segoe UI", 12.8f),
                ForeColor = Palette.Danger,
                BackColor = System.Drawing.Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(96, 34),
                Cursor    = Cursors.Hand
                // Location set by layoutUserBar()
            };
            btnLogout.FlatAppearance.BorderColor = Palette.Danger;
            btnLogout.Click += btnLogout_Click;

            // Local layout function — called on Resize AND once right away
            System.Action layoutUserBar = () =>
            {
                int bw = pnlUserBar.ClientSize.Width;
                // X positions from right edge
                btnLogout.Location     = new Point(bw - 108,  11);  // Y=(56-34)/2
                pnlAvatar.Location     = new Point(bw - 152,  10);  // Y=(56-36)/2
                lblTopNavUser.Location = new Point(bw - 152 - lblTopNavUser.Width - 8, 16);
            };

            pnlUserBar.Resize += (s, e) => layoutUserBar();

            pnlUserBar.Controls.Add(lblBreadcrumb);
            pnlUserBar.Controls.Add(lblTopNavUser);
            pnlUserBar.Controls.Add(pnlAvatar);
            pnlUserBar.Controls.Add(btnLogout);
            pnlUserBar.Controls.Add(userBarBorder);

            // Fire once now so positions are correct before first paint
            layoutUserBar();

            // no-op kept for source compat
            pnlTopNav.SetPopupContainer(pnlMain);

            // ── Scrollable content area ──
            pnlContent = new Panel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                Padding    = new Padding(26, 20, 26, 26),
                BackColor  = Palette.BgPage
            };

            lblPageTitle = new Label
            {
                Text      = "Dashboard",
                Font      = new Font("Segoe UI", 25.6f, FontStyle.Bold),
                ForeColor = Palette.TextMain,
                AutoSize  = true
            };
            lblPageSub = new Label
            {
                Text      = "...",
                Font      = new Font("Segoe UI", 14.4f),
                ForeColor = Palette.TextMuted,
                AutoSize  = true
            };

            pnlAlert = new Panel
            {
                Height    = 51,
                BackColor = System.Drawing.Color.FromArgb(254, 243, 199),
                Padding   = new Padding(13, 0, 13, 0)
            };
            Panel alertBorder = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = Palette.Warning };
            lblAlert = new Label
            {
                Text      = "\u26A0\uFE0F  9 items are currently below minimum stock threshold.",
                Font      = new Font("Segoe UI", 13.6f),
                ForeColor = System.Drawing.Color.FromArgb(120, 53, 15),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(8, 0, 0, 0)
            };
            pnlAlert.Controls.Add(lblAlert);
            pnlAlert.Controls.Add(alertBorder);

            // KPI Row 1
            pnlKpi1 = new Panel { Height = 128, BackColor = System.Drawing.Color.Transparent };
            TableLayoutPanel tlpKpi1 = new TableLayoutPanel
            { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, BackColor = System.Drawing.Color.Transparent };
            for (int i = 0; i < 4; i++) tlpKpi1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
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

            // KPI Row 2
            pnlKpi2 = new Panel { Height = 128, BackColor = System.Drawing.Color.Transparent };
            TableLayoutPanel tlpKpi2 = new TableLayoutPanel
            { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, BackColor = System.Drawing.Color.Transparent };
            for (int i = 0; i < 4; i++) tlpKpi2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
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

            // Section rows
            tlpRow1 = MakeSectionRow();
            Panel secOrders   = MakeSectionCard("Recent Orders");
            Panel secLowStock = MakeSectionCard("\u26A0\uFE0F Low Stock Alerts");
            dgvOrders = MakeDgv(new[] { "Order No.", "Customer", "Total", "Status" });
            dgvOrders.CellPainting += dgvOrders_CellPainting;
            secOrders.Controls.Add(dgvOrders);
            DataGridView dgvLowStock = MakeDgv(new[] { "Item", "On Hand", "Min", "Status" });
            AddLowStockRows(dgvLowStock);
            secLowStock.Controls.Add(dgvLowStock);
            tlpRow1.Controls.Add(secOrders,   0, 0);
            tlpRow1.Controls.Add(secLowStock, 1, 0);

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

            tlpRow3 = MakeSectionRow();
            Panel secSup = MakeSectionCard("Supplier Payment Status");
            Panel secAct = MakeSectionCard("Recent Activity");
            dgvSuppliers = MakeDgv(new[] { "Supplier", "Invoice", "Amount", "Status" });
            dgvSuppliers.CellPainting += dgvSuppliers_CellPainting;
            pnlActivity = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = System.Drawing.Color.Transparent };
            secSup.Controls.Add(dgvSuppliers);
            secAct.Controls.Add(pnlActivity);
            tlpRow3.Controls.Add(secSup, 0, 0);
            tlpRow3.Controls.Add(secAct, 1, 0);

            FlowLayoutPanel flow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                AutoSize      = true,
                AutoSizeMode  = AutoSizeMode.GrowAndShrink,
                BackColor     = System.Drawing.Color.Transparent
            };
            System.Action<int> addSpacer = h =>
                flow.Controls.Add(new Panel { Height = h, Width = 10, BackColor = System.Drawing.Color.Transparent });

            flow.Controls.Add(lblPageTitle); addSpacer(5);
            flow.Controls.Add(lblPageSub);   addSpacer(14);
            flow.Controls.Add(pnlAlert);     addSpacer(20);
            flow.Controls.Add(pnlKpi1);      addSpacer(14);
            flow.Controls.Add(pnlKpi2);      addSpacer(19);
            flow.Controls.Add(tlpRow1);      addSpacer(16);
            flow.Controls.Add(tlpRow2);      addSpacer(16);
            flow.Controls.Add(tlpRow3);

            pnlContent.Controls.Add(flow);

            pnlContent.Resize += (s, e) =>
            {
                int w = pnlContent.ClientSize.Width - pnlContent.Padding.Horizontal;
                flow.Width    = w; pnlAlert.Width = w;
                pnlKpi1.Width = w; pnlKpi2.Width  = w;
                tlpRow1.Width = w; tlpRow2.Width  = w; tlpRow3.Width = w;
            };

            pnlMain.Controls.Add(pnlContent);
            pnlMain.Controls.Add(pnlUserBar);
            pnlMain.Controls.Add(pnlTopNav);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        private void OnTopNavMenuItemClicked(string itemLabel)
        {
            if (itemLabel == "Dashboard") { lblBreadcrumb.Text = "Dashboard"; return; }
            lblBreadcrumb.Text = itemLabel;
            MessageBox.Show(
                $"\u231B  {itemLabel}\n\nThis feature is currently under development.\nPlease check back in a later version.",
                "Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ====================================================================
        // FACTORY HELPERS
        // ====================================================================

        private Panel MakeKpiCard(System.Drawing.Color accent)
        {
            Panel card = new Panel
            {
                Dock      = DockStyle.Fill,
                Margin    = new Padding(0, 0, 11, 0),
                BackColor = Palette.BgCard,
                Padding   = new Padding(21, 18, 21, 13)
            };
            card.Paint += (s, e) =>
            {
                e.Graphics.FillRectangle(new System.Drawing.SolidBrush(accent), 0, 0, ((Panel)s).Width, 6);
                e.Graphics.DrawRectangle(new System.Drawing.Pen(Palette.BorderColor, 1),
                    0, 0, ((Panel)s).Width - 1, ((Panel)s).Height - 1);
            };
            Label lblLabel = new Label { Tag="kpi-label", Font=new Font("Segoe UI",10.4f,FontStyle.Bold), ForeColor=Palette.TextMuted,  AutoSize=false, Width=208, Height=22, Location=new Point(21,18),  TextAlign=ContentAlignment.TopLeft };
            Label lblValue = new Label { Tag="kpi-value", Font=new Font("Segoe UI",30.4f,FontStyle.Bold), ForeColor=accent,             AutoSize=false, Width=208, Height=45, Location=new Point(19,42),  TextAlign=ContentAlignment.TopLeft };
            Label lblSub   = new Label { Tag="kpi-sub",   Font=new Font("Segoe UI",10.4f),               ForeColor=Palette.TextMuted,  AutoSize=false, Width=208, Height=22, Location=new Point(21,90),  TextAlign=ContentAlignment.TopLeft };
            card.Controls.Add(lblLabel);
            card.Controls.Add(lblValue);
            card.Controls.Add(lblSub);
            return card;
        }

        private TableLayoutPanel MakeSectionRow()
        {
            var tlp = new TableLayoutPanel { Height=304, ColumnCount=2, RowCount=1, BackColor=System.Drawing.Color.Transparent, Margin=new Padding(0) };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            return tlp;
        }

        private Panel MakeSectionCard(string title)
        {
            Panel card = new Panel { Dock=DockStyle.Fill, BackColor=Palette.BgCard, Margin=new Padding(0,0,11,0) };
            card.Paint += (s, e) => e.Graphics.DrawRectangle(new System.Drawing.Pen(Palette.BorderColor,1), 0, 0, ((Panel)s).Width-1, ((Panel)s).Height-1);
            Panel header = new Panel { Dock=DockStyle.Top, Height=51, BackColor=Palette.BgCard, Padding=new Padding(18,0,18,0) };
            Panel headerDiv = new Panel { Dock=DockStyle.Bottom, Height=1, BackColor=Palette.BorderColor };
            Label lblTitle = new Label { Text=title, Font=new Font("Segoe UI",14.4f,FontStyle.Bold), ForeColor=Palette.TextMain, Dock=DockStyle.Fill, TextAlign=ContentAlignment.MiddleLeft };
            header.Controls.Add(lblTitle);
            header.Controls.Add(headerDiv);
            card.Controls.Add(header);
            return card;
        }

        private DataGridView MakeDgv(string[] columns)
        {
            var dgv = new DataGridView
            {
                Dock=DockStyle.Fill, ReadOnly=true, AllowUserToAddRows=false, AllowUserToDeleteRows=false,
                RowHeadersVisible=false, SelectionMode=DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor=Palette.BgCard, BorderStyle=BorderStyle.None, GridColor=Palette.BorderColor,
                Font=new Font("Segoe UI",12.8f), AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle=DataGridViewCellBorderStyle.SingleHorizontal, RowTemplate={ Height=38 }
            };
            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            { BackColor=System.Drawing.Color.FromArgb(246,249,255), ForeColor=Palette.TextMuted, Font=new Font("Segoe UI",11.2f,FontStyle.Bold), Padding=new Padding(6) };
            dgv.ColumnHeadersHeight = 42;
            dgv.DefaultCellStyle = new DataGridViewCellStyle
            { BackColor=Palette.BgCard, ForeColor=Palette.TextMain, SelectionBackColor=System.Drawing.Color.FromArgb(240,246,255), SelectionForeColor=Palette.TextMain, Padding=new Padding(8,6,8,6) };
            dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor=System.Drawing.Color.FromArgb(248,250,253) };
            foreach (string col in columns)
                dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText=col, Name=col.Replace(" ","_"), SortMode=DataGridViewColumnSortMode.NotSortable });
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
                    ? new System.Drawing.Color[] { Palette.TagRedBg,    Palette.TagRedFg    }
                    : new System.Drawing.Color[] { Palette.TagYellowBg, Palette.TagYellowFg };
            }
            dgv.CellPainting += (s, e) => PaintStatusCell(s, e, 3);
        }
    }
}
