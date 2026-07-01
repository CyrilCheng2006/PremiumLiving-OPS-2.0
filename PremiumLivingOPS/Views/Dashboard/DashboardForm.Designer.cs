using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.Dashboard
{
    partial class DashboardForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── Shell (TopNavBar + UserBar) ──────────────────────────────────
        private AppShell _shell;

        private Panel pnlContent;
        private Label lblPageTitle;
        private Label lblPageSub;
        private Panel pnlAlert;
        private Label lblAlert;

        // KPI card panels (8 cards across 2 rows)
        private Panel pnlKpi1;
        private Panel kpiOrders, kpiDelivered, kpiQuotations, kpiLowStock;
        private Panel pnlKpi2;
        private Panel kpiRevenue, kpiAR, kpiSuppliers, kpiCustomers;

        // Data section rows
        private TableLayoutPanel tlpRow1, tlpRow2, tlpRow3;
        private DataGridView     dgvOrders, dgvQuotations, dgvShipments, dgvSuppliers;
        private DataGridView     _dgvLowStock;

        private Panel pnlActivity;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text          = "Premium Living OPS — Dashboard";
            this.Size          = new Size(1440, 900);
            this.MinimumSize   = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = Palette.BgPage;
            this.WindowState   = FormWindowState.Maximized;
            this.Font          = new Font("Segoe UI", 16f);

            Panel pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage };

            // ── AppShell (TopNavBar + UserBar) ──────────────────────────
            // Events wired in BindViewModel() — NOT here to avoid double-firing.
            _shell = new AppShell();
            _shell.SetPopupContainer(pnlMain);

            // ── Content area ────────────────────────────────────────────
            pnlContent = new Panel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                Padding    = new Padding(26, 20, 26, 26),
                BackColor  = Palette.BgPage
            };

            // ── Page header labels ───────────────────────────────────────
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

            // ── Low-stock alert banner ────────────────────────────────────
            pnlAlert = new Panel
            {
                Height    = 51,
                BackColor = System.Drawing.Color.FromArgb(254, 243, 199),
                Padding   = new Padding(13, 0, 13, 0)
            };
            Panel alertBorder = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = Palette.Warning };
            lblAlert = new Label
            {
                Text      = "",
                Font      = new Font("Segoe UI", 13.6f),
                ForeColor = System.Drawing.Color.FromArgb(120, 53, 15),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(8, 0, 0, 0)
            };
            pnlAlert.Controls.Add(lblAlert);
            pnlAlert.Controls.Add(alertBorder);

            // ── KPI rows — wrapped in CardPanel three-layer structure ─────────
            //
            // Layer 1: Outer Panel  (PageBg #F0F4F9, padding 20/14/20/8)
            // Layer 2: Inner Panel  (White, 1-px #DDE3EC border)   ← visible card
            // Layer 3: Content TLP  (4 equal KPI metric cards)
            //
            // KPI row height: card(94) + outer padding(14+8) = 116 → use 122 for breathing room.

            var (kpiOuter1, kpiInner1) = CardPanel.Create(outerHeight: 122);
            TableLayoutPanel tlpKpi1 = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1,
                BackColor = System.Drawing.Color.Transparent, Padding = new Padding(8)
            };
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
            kpiInner1.Controls.Add(tlpKpi1);
            pnlKpi1 = kpiOuter1;

            var (kpiOuter2, kpiInner2) = CardPanel.Create(outerHeight: 122);
            TableLayoutPanel tlpKpi2 = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1,
                BackColor = System.Drawing.Color.Transparent, Padding = new Padding(8)
            };
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
            kpiInner2.Controls.Add(tlpKpi2);
            pnlKpi2 = kpiOuter2;

            // ── Section rows — wrapped in CardPanel three-layer structure ──────
            //
            // Row height calculation (row height 53 px):
            //   Header bar : 48
            //   Column header : 42
            //   5 data rows : 5 × 53 = 265
            //   Buffer : 8
            //   Inner total : 363
            //   CardPanel outer padding (top 12 + bottom 0) : 12
            //   outerHeight → 375

            // Row 1: Recent Orders | Low Stock Alerts
            var (secOuter1, secInner1) = CardPanel.Create(outerHeight: 375);
            tlpRow1 = MakeSectionTlp();
            Panel secOrders   = MakeSectionCard("Recent Orders");
            Panel secLowStock = MakeSectionCard("\u26a0\ufe0f  Low Stock Alerts");
            dgvOrders    = MakeDgv(new[] { "Order No.", "Customer", "Total", "Status" });
            dgvOrders.CellPainting += dgvOrders_CellPainting;
            secOrders.Controls.Add(dgvOrders);
            _dgvLowStock = MakeDgv(new[] { "Item", "On Hand", "Min", "Status" });
            _dgvLowStock.CellPainting += (s, e) => PaintStatusCell(s, e, 3);
            secLowStock.Controls.Add(_dgvLowStock);
            tlpRow1.Controls.Add(secOrders,   0, 0);
            tlpRow1.Controls.Add(secLowStock, 1, 0);
            secInner1.Controls.Add(tlpRow1);

            // Row 2: Pending Quotations | Active Shipments
            var (secOuter2, secInner2) = CardPanel.Create(outerHeight: 375);
            tlpRow2 = MakeSectionTlp();
            Panel secQuot = MakeSectionCard("Pending Quotations");
            Panel secShip = MakeSectionCard("Active Shipments");
            dgvQuotations = MakeDgv(new[] { "Quotation No.", "Customer", "Amount", "Valid Until" });
            dgvShipments  = MakeDgv(new[] { "Shipment ID", "Customer", "Ship Date", "Status" });
            dgvShipments.CellPainting += dgvShipments_CellPainting;
            secQuot.Controls.Add(dgvQuotations);
            secShip.Controls.Add(dgvShipments);
            tlpRow2.Controls.Add(secQuot, 0, 0);
            tlpRow2.Controls.Add(secShip, 1, 0);
            secInner2.Controls.Add(tlpRow2);

            // Row 3: Supplier Payment Status | Recent Activity
            var (secOuter3, secInner3) = CardPanel.Create(outerHeight: 375);
            tlpRow3 = MakeSectionTlp();
            Panel secSup = MakeSectionCard("Supplier Payment Status");
            Panel secAct = MakeSectionCard("Recent Activity");
            dgvSuppliers = MakeDgv(new[] { "Supplier", "Invoice", "Amount", "Status" });
            dgvSuppliers.CellPainting += dgvSuppliers_CellPainting;
            pnlActivity = new Panel
            {
                Dock = DockStyle.Fill, AutoScroll = true,
                BackColor = System.Drawing.Color.Transparent
            };
            secSup.Controls.Add(dgvSuppliers);
            secAct.Controls.Add(pnlActivity);
            tlpRow3.Controls.Add(secSup, 0, 0);
            tlpRow3.Controls.Add(secAct, 1, 0);
            secInner3.Controls.Add(tlpRow3);

            // ── Flow layout ────────────────────────────────────────────────────
            FlowLayoutPanel flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, FlowDirection = FlowDirection.TopDown,
                WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = System.Drawing.Color.Transparent
            };
            System.Action<int> addSpacer = h =>
                flow.Controls.Add(new Panel
                {
                    Height = h, Width = 10,
                    BackColor = System.Drawing.Color.Transparent
                });

            flow.Controls.Add(lblPageTitle);  addSpacer(5);
            flow.Controls.Add(lblPageSub);    addSpacer(14);
            flow.Controls.Add(pnlAlert);      addSpacer(6);
            flow.Controls.Add(pnlKpi1);       addSpacer(0);
            flow.Controls.Add(pnlKpi2);       addSpacer(0);
            flow.Controls.Add(secOuter1);     addSpacer(0);
            flow.Controls.Add(secOuter2);     addSpacer(0);
            flow.Controls.Add(secOuter3);

            // Stretch all flow children to content width on resize
            pnlContent.Resize += (s, e) =>
            {
                int w = pnlContent.ClientSize.Width - pnlContent.Padding.Horizontal;
                flow.Width      = w;
                pnlAlert.Width  = w;
                pnlKpi1.Width   = w;   pnlKpi2.Width   = w;
                secOuter1.Width = w;   secOuter2.Width = w;   secOuter3.Width = w;
            };

            pnlContent.Controls.Add(flow);
            pnlMain.Controls.Add(pnlContent);
            pnlMain.Controls.Add(_shell);   // shell docks Top over content

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        // ── UI factory helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Creates one KPI metric card: accent top stripe + Label + Value + Sub-text.
        /// Lives inside the white CardPanel inner panel as the innermost content layer.
        /// </summary>
        private Panel MakeKpiCard(System.Drawing.Color accent)
        {
            Panel card = new Panel
            {
                Dock      = DockStyle.Fill,
                Margin    = new Padding(6, 6, 6, 6),
                BackColor = Palette.BgCard,
                Padding   = new Padding(18, 20, 18, 10)
            };
            card.Paint += (s, e) =>
            {
                var p = (Panel)s;
                e.Graphics.FillRectangle(new System.Drawing.SolidBrush(accent), 0, 0, p.Width, 5);
                e.Graphics.DrawRectangle(
                    new System.Drawing.Pen(Palette.BorderColor, 1), 0, 0, p.Width - 1, p.Height - 1);
            };
            Label ll = new Label
            {
                Tag = "kpi-label", Font = new Font("Segoe UI", 10.4f, FontStyle.Bold),
                ForeColor = Palette.TextMuted, AutoSize = false,
                Width = 220, Height = 22, Location = new Point(18, 18),
                TextAlign = ContentAlignment.TopLeft
            };
            Label lv = new Label
            {
                Tag = "kpi-value", Font = new Font("Segoe UI", 28f, FontStyle.Bold),
                ForeColor = accent, AutoSize = false,
                Width = 220, Height = 42, Location = new Point(16, 42),
                TextAlign = ContentAlignment.TopLeft
            };
            Label ls = new Label
            {
                Tag = "kpi-sub", Font = new Font("Segoe UI", 10.4f),
                ForeColor = Palette.TextMuted, AutoSize = false,
                Width = 220, Height = 22, Location = new Point(18, 87),
                TextAlign = ContentAlignment.TopLeft
            };
            card.Controls.Add(ll);
            card.Controls.Add(lv);
            card.Controls.Add(ls);
            return card;
        }

        /// <summary>
        /// 50/50 two-column TableLayoutPanel — fills the white CardPanel inner panel
        /// and hosts the two MakeSectionCard() sub-panels side by side.
        /// </summary>
        private TableLayoutPanel MakeSectionTlp()
        {
            var t = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2, RowCount = 1,
                BackColor   = System.Drawing.Color.Transparent,
                Margin      = new Padding(0)
            };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            return t;
        }

        /// <summary>
        /// Titled section sub-card: white panel with header bar + 1-px divider.
        /// DataGridView or pnlActivity is added as DockStyle.Fill content.
        /// </summary>
        private Panel MakeSectionCard(string title)
        {
            Panel c = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Palette.BgCard,
                Margin    = new Padding(0, 0, 8, 0)
            };
            c.Paint += (s, e) =>
                e.Graphics.DrawRectangle(
                    new System.Drawing.Pen(Palette.BorderColor, 1),
                    0, 0, ((Panel)s).Width - 1, ((Panel)s).Height - 1);
            Panel hdr = new Panel
            {
                Dock = DockStyle.Top, Height = 48,
                BackColor = Palette.BgCard, Padding = new Padding(16, 0, 16, 0)
            };
            Panel hdiv = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Palette.BorderColor };
            Label lt = new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI", 13.6f, FontStyle.Bold),
                ForeColor = Palette.TextMain,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            hdr.Controls.Add(lt);
            hdr.Controls.Add(hdiv);
            c.Controls.Add(hdr);
            return c;
        }

        /// <summary>
        /// Styled read-only DataGridView with project-standard colours.
        /// Row height is fixed at 53 px to give data breathing room.
        /// </summary>
        private DataGridView MakeDgv(string[] columns)
        {
            var d = new DataGridView
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
                Font                  = new Font("Segoe UI", 12.8f),
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate           = { Height = 53 }   // ← 53 px per data row
            };
            d.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = System.Drawing.Color.FromArgb(246, 249, 255),
                ForeColor = Palette.TextMuted,
                Font      = new Font("Segoe UI", 11.2f, FontStyle.Bold),
                Padding   = new Padding(6)
            };
            d.ColumnHeadersHeight = 42;
            d.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor           = Palette.BgCard,
                ForeColor           = Palette.TextMain,
                SelectionBackColor  = System.Drawing.Color.FromArgb(240, 246, 255),
                SelectionForeColor  = Palette.TextMain,
                Padding             = new Padding(8, 10, 8, 10)  // extra vertical padding matches 53 px row
            };
            d.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = System.Drawing.Color.FromArgb(248, 250, 253)
            };
            foreach (string col in columns)
                d.Columns.Add(new DataGridViewTextBoxColumn
                {
                    HeaderText = col,
                    Name       = col.Replace(" ", "_"),
                    SortMode   = DataGridViewColumnSortMode.NotSortable
                });
            return d;
        }
    }
}
