using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.Dashboard
{
    partial class DashboardForm
    {
        private System.ComponentModel.IContainer components = null;

        private TopNavBar pnlTopNav;

        private Label  lblBreadcrumb;
        private Label  lblTopNavUser;
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

            this.Text          = "Premium Living OPS 2.0 — Dashboard";
            this.Size          = new Size(1440, 900);
            this.MinimumSize   = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = Palette.BgPage;
            this.WindowState   = FormWindowState.Maximized;
            this.Font          = new Font("Segoe UI", 16f);

            Panel pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage };

            pnlTopNav = new TopNavBar();
            pnlTopNav.MenuItemClicked += OnTopNavMenuItemClicked;

            // ============================================================
            // User Bar — 72 px tall
            //
            // Elements (left):  lblBreadcrumb
            // Elements (right): lblTopNavUser | gap | btnLogout
            //
            // Vertical centre formula:  Y = (UBH − ctrl.Height) / 2
            // Applied inside layoutUserBar() which fires on Resize + Load.
            // btnLogout uses AutoSize so its border always fits the text.
            // ============================================================
            const int UBH      = 72;
            const int RightPad = 16;
            const int ItemGap  = 12;

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
                Location  = new Point(22, 0)   // Y set properly in layoutUserBar
            };

            lblTopNavUser = new Label
            {
                Text      = "...",
                Font      = new Font("Segoe UI", 14.4f),
                ForeColor = Palette.TextMuted,
                AutoSize  = true
            };

            // AutoSize = true + Padding ensures the border always wraps the text
            btnLogout = new Button
            {
                Text      = "Log Out",
                Font      = new Font("Segoe UI", 12.8f),
                ForeColor = Palette.Danger,
                BackColor = System.Drawing.Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                AutoSize  = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding   = new Padding(14, 4, 14, 4),  // horizontal room so border is clear
                Cursor    = Cursors.Hand
            };
            btnLogout.FlatAppearance.BorderColor = Palette.Danger;
            btnLogout.FlatAppearance.BorderSize  = 1;
            btnLogout.Click += btnLogout_Click;

            // ---- layout closure ----------------------------------------
            // Fires on Resize (window size changes) AND Form.Load
            // (labels have been measured by WinForms before Load fires).
            System.Action layoutUserBar = () =>
            {
                int bw = pnlUserBar.ClientSize.Width;

                // Right side: [Log Out] | gap | [User Name]
                int logoutX  = bw - RightPad - btnLogout.Width;
                int userLblX = logoutX - ItemGap - lblTopNavUser.Width;

                // Vertically centre each control
                btnLogout.Location     = new Point(logoutX,  (UBH - btnLogout.Height)     / 2);
                lblTopNavUser.Location = new Point(userLblX, (UBH - lblTopNavUser.Height) / 2);
                lblBreadcrumb.Location = new Point(22,        (UBH - lblBreadcrumb.Height) / 2);
            };

            pnlUserBar.Resize += (s, e) => layoutUserBar();
            this.Load         += (s, e) => layoutUserBar();
            // ------------------------------------------------------------

            pnlUserBar.Controls.Add(lblBreadcrumb);
            pnlUserBar.Controls.Add(lblTopNavUser);
            pnlUserBar.Controls.Add(btnLogout);
            pnlUserBar.Controls.Add(userBarBorder);

            pnlTopNav.SetPopupContainer(pnlMain);

            // ── Content area ──
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
            Panel alertBorder = new Panel { Dock=DockStyle.Left, Width=4, BackColor=Palette.Warning };
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

            pnlKpi1 = new Panel { Height=128, BackColor=System.Drawing.Color.Transparent };
            TableLayoutPanel tlpKpi1 = new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=4, RowCount=1, BackColor=System.Drawing.Color.Transparent };
            for (int i=0;i<4;i++) tlpKpi1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,25f));
            tlpKpi1.RowStyles.Add(new RowStyle(SizeType.Percent,100f));
            kpiOrders=MakeKpiCard(Palette.Primary); kpiDelivered=MakeKpiCard(Palette.Success);
            kpiQuotations=MakeKpiCard(Palette.Warning); kpiLowStock=MakeKpiCard(Palette.Danger);
            tlpKpi1.Controls.Add(kpiOrders,0,0); tlpKpi1.Controls.Add(kpiDelivered,1,0);
            tlpKpi1.Controls.Add(kpiQuotations,2,0); tlpKpi1.Controls.Add(kpiLowStock,3,0);
            pnlKpi1.Controls.Add(tlpKpi1);

            pnlKpi2 = new Panel { Height=128, BackColor=System.Drawing.Color.Transparent };
            TableLayoutPanel tlpKpi2 = new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=4, RowCount=1, BackColor=System.Drawing.Color.Transparent };
            for (int i=0;i<4;i++) tlpKpi2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,25f));
            tlpKpi2.RowStyles.Add(new RowStyle(SizeType.Percent,100f));
            kpiRevenue=MakeKpiCard(Palette.Info); kpiAR=MakeKpiCard(Palette.Warning);
            kpiSuppliers=MakeKpiCard(Palette.Primary); kpiCustomers=MakeKpiCard(Palette.Primary);
            tlpKpi2.Controls.Add(kpiRevenue,0,0); tlpKpi2.Controls.Add(kpiAR,1,0);
            tlpKpi2.Controls.Add(kpiSuppliers,2,0); tlpKpi2.Controls.Add(kpiCustomers,3,0);
            pnlKpi2.Controls.Add(tlpKpi2);

            tlpRow1=MakeSectionRow();
            Panel secOrders=MakeSectionCard("Recent Orders"); Panel secLowStock=MakeSectionCard("\u26A0\uFE0F Low Stock Alerts");
            dgvOrders=MakeDgv(new[]{"Order No.","Customer","Total","Status"}); dgvOrders.CellPainting+=dgvOrders_CellPainting;
            secOrders.Controls.Add(dgvOrders);
            DataGridView dgvLowStock=MakeDgv(new[]{"Item","On Hand","Min","Status"}); AddLowStockRows(dgvLowStock); secLowStock.Controls.Add(dgvLowStock);
            tlpRow1.Controls.Add(secOrders,0,0); tlpRow1.Controls.Add(secLowStock,1,0);

            tlpRow2=MakeSectionRow();
            Panel secQuot=MakeSectionCard("Pending Quotations"); Panel secShip=MakeSectionCard("Active Shipments");
            dgvQuotations=MakeDgv(new[]{"Quotation No.","Customer","Amount","Valid Until"});
            dgvShipments=MakeDgv(new[]{"Shipment ID","Customer","Sched. Date","Status"}); dgvShipments.CellPainting+=dgvShipments_CellPainting;
            secQuot.Controls.Add(dgvQuotations); secShip.Controls.Add(dgvShipments);
            tlpRow2.Controls.Add(secQuot,0,0); tlpRow2.Controls.Add(secShip,1,0);

            tlpRow3=MakeSectionRow();
            Panel secSup=MakeSectionCard("Supplier Payment Status"); Panel secAct=MakeSectionCard("Recent Activity");
            dgvSuppliers=MakeDgv(new[]{"Supplier","Invoice","Amount","Status"}); dgvSuppliers.CellPainting+=dgvSuppliers_CellPainting;
            pnlActivity=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=System.Drawing.Color.Transparent};
            secSup.Controls.Add(dgvSuppliers); secAct.Controls.Add(pnlActivity);
            tlpRow3.Controls.Add(secSup,0,0); tlpRow3.Controls.Add(secAct,1,0);

            FlowLayoutPanel flow = new FlowLayoutPanel
            {
                Dock=DockStyle.Top, FlowDirection=FlowDirection.TopDown,
                WrapContents=false, AutoSize=true, AutoSizeMode=AutoSizeMode.GrowAndShrink,
                BackColor=System.Drawing.Color.Transparent
            };
            System.Action<int> addSpacer = h =>
                flow.Controls.Add(new Panel{Height=h,Width=10,BackColor=System.Drawing.Color.Transparent});

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
                flow.Width=w; pnlAlert.Width=w;
                pnlKpi1.Width=w; pnlKpi2.Width=w;
                tlpRow1.Width=w; tlpRow2.Width=w; tlpRow3.Width=w;
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

        private Panel MakeKpiCard(System.Drawing.Color accent)
        {
            Panel card = new Panel { Dock=DockStyle.Fill, Margin=new Padding(0,0,11,0), BackColor=Palette.BgCard, Padding=new Padding(21,18,21,13) };
            card.Paint += (s, e) =>
            {
                e.Graphics.FillRectangle(new System.Drawing.SolidBrush(accent), 0, 0, ((Panel)s).Width, 6);
                e.Graphics.DrawRectangle(new System.Drawing.Pen(Palette.BorderColor,1), 0, 0, ((Panel)s).Width-1, ((Panel)s).Height-1);
            };
            Label ll=new Label{Tag="kpi-label",Font=new Font("Segoe UI",10.4f,FontStyle.Bold),ForeColor=Palette.TextMuted, AutoSize=false,Width=208,Height=22,Location=new Point(21,18),TextAlign=ContentAlignment.TopLeft};
            Label lv=new Label{Tag="kpi-value",Font=new Font("Segoe UI",30.4f,FontStyle.Bold),ForeColor=accent,            AutoSize=false,Width=208,Height=45,Location=new Point(19,42),TextAlign=ContentAlignment.TopLeft};
            Label ls=new Label{Tag="kpi-sub",  Font=new Font("Segoe UI",10.4f),              ForeColor=Palette.TextMuted, AutoSize=false,Width=208,Height=22,Location=new Point(21,90),TextAlign=ContentAlignment.TopLeft};
            card.Controls.Add(ll); card.Controls.Add(lv); card.Controls.Add(ls);
            return card;
        }

        private TableLayoutPanel MakeSectionRow()
        {
            var t=new TableLayoutPanel{Height=304,ColumnCount=2,RowCount=1,BackColor=System.Drawing.Color.Transparent,Margin=new Padding(0)};
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50f));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50f));
            t.RowStyles.Add(new RowStyle(SizeType.Percent,100f));
            return t;
        }

        private Panel MakeSectionCard(string title)
        {
            Panel c=new Panel{Dock=DockStyle.Fill,BackColor=Palette.BgCard,Margin=new Padding(0,0,11,0)};
            c.Paint+=(s,e)=>e.Graphics.DrawRectangle(new System.Drawing.Pen(Palette.BorderColor,1),0,0,((Panel)s).Width-1,((Panel)s).Height-1);
            Panel hdr=new Panel{Dock=DockStyle.Top,Height=51,BackColor=Palette.BgCard,Padding=new Padding(18,0,18,0)};
            Panel hdiv=new Panel{Dock=DockStyle.Bottom,Height=1,BackColor=Palette.BorderColor};
            Label lt=new Label{Text=title,Font=new Font("Segoe UI",14.4f,FontStyle.Bold),ForeColor=Palette.TextMain,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft};
            hdr.Controls.Add(lt); hdr.Controls.Add(hdiv); c.Controls.Add(hdr);
            return c;
        }

        private DataGridView MakeDgv(string[] columns)
        {
            var d=new DataGridView
            {
                Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,
                RowHeadersVisible=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor=Palette.BgCard,BorderStyle=BorderStyle.None,GridColor=Palette.BorderColor,
                Font=new Font("Segoe UI",12.8f),AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle=DataGridViewCellBorderStyle.SingleHorizontal,RowTemplate={Height=38}
            };
            d.ColumnHeadersDefaultCellStyle=new DataGridViewCellStyle{BackColor=System.Drawing.Color.FromArgb(246,249,255),ForeColor=Palette.TextMuted,Font=new Font("Segoe UI",11.2f,FontStyle.Bold),Padding=new Padding(6)};
            d.ColumnHeadersHeight=42;
            d.DefaultCellStyle=new DataGridViewCellStyle{BackColor=Palette.BgCard,ForeColor=Palette.TextMain,SelectionBackColor=System.Drawing.Color.FromArgb(240,246,255),SelectionForeColor=Palette.TextMain,Padding=new Padding(8,6,8,6)};
            d.AlternatingRowsDefaultCellStyle=new DataGridViewCellStyle{BackColor=System.Drawing.Color.FromArgb(248,250,253)};
            foreach(string col in columns)
                d.Columns.Add(new DataGridViewTextBoxColumn{HeaderText=col,Name=col.Replace(" ","_"),SortMode=DataGridViewColumnSortMode.NotSortable});
            return d;
        }

        private void AddLowStockRows(DataGridView dgv)
        {
            dgv.Rows.Add("Solid Oak Panel (IID-R-0001)","8","20","Critical");
            dgv.Rows.Add("High-density Foam (IID-R-0002)","3","15","Critical");
            dgv.Rows.Add("5-Door Wardrobe (IID-P-0005)","5","8","Low");
            dgv.Rows.Add("Steel Bolt Set (IID-R-0003)","12","50","Critical");
            dgv.Rows.Add("Queen Size Oak Bed Frame (IID-P-0002)","8","10","Low");
            foreach(DataGridViewRow row in dgv.Rows)
            {
                string st=row.Cells[3].Value?.ToString();
                row.Tag=st=="Critical"
                    ?new System.Drawing.Color[]{Palette.TagRedBg,Palette.TagRedFg}
                    :new System.Drawing.Color[]{Palette.TagYellowBg,Palette.TagYellowFg};
            }
            dgv.CellPainting+=(s,e)=>PaintStatusCell(s,e,3);
        }
    }
}
