using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.OrderProcessing
{
    partial class ViewOrderForm
    {
        private System.ComponentModel.IContainer components = null;

        private AppShell     _shell;
        private DataGridView dgvOrders;
        private DataGridView dgvLines;
        private Label        lblDetailTitle;
        private ComboBox     cboStatusFilter;
        private Button       btnRefresh;
        private Label        lblFilterLabel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text          = "Premium Living OPS — View Order";
            this.Size          = new Size(1440, 900);
            this.MinimumSize   = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = Palette.BgPage;
            this.WindowState   = FormWindowState.Maximized;
            this.Font          = new Font("Segoe UI", 14f); // 11 × 1.3 ≈ 14

            // ── AppShell ───────────────────────────────────────
            _shell = new AppShell();
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;

            // ── Filter toolbar (Top, height 52→68) ───────────────
            Panel pnlToolbar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 68,           // 52 × 1.3
                BackColor = Palette.BgCard,
                Padding   = new Padding(20, 13, 20, 13)  // 16/10 × 1.3
            };
            pnlToolbar.Paint += (s, e) =>
                e.Graphics.DrawRectangle(
                    new System.Drawing.Pen(Palette.BorderColor, 1),
                    0, 0, ((Panel)s).Width - 1, ((Panel)s).Height - 1);

            lblFilterLabel = new Label
            {
                Text      = "Status:",
                Font      = new Font("Segoe UI", 14f),   // 11 × 1.3
                ForeColor = Palette.TextMuted,
                AutoSize  = true,
                Location  = new Point(20, 20)            // 16/15 × 1.3
            };
            cboStatusFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width         = 208,                     // 160 × 1.3
                Location      = new Point(100, 17),      // 76/13 × 1.3
                Font          = new Font("Segoe UI", 14f)
            };
            cboStatusFilter.Items.AddRange(new object[]
                { "All", "Pending", "Processing", "Delivered", "Cancelled" });
            cboStatusFilter.SelectedIndex = 0;
            cboStatusFilter.SelectedIndexChanged += cboStatusFilter_SelectedIndexChanged;

            btnRefresh = new Button
            {
                Text      = "↻ Refresh",
                Font      = new Font("Segoe UI", 14f),
                ForeColor = Palette.Primary,
                FlatStyle = FlatStyle.Flat,
                Width     = 130,                         // 100 × 1.3
                Height    = 39,                          // 30 × 1.3
                Location  = new Point(328, 14)           // 252/11 × 1.3
            };
            btnRefresh.FlatAppearance.BorderColor = Palette.Primary;
            btnRefresh.FlatAppearance.BorderSize  = 1;
            btnRefresh.Click += btnRefresh_Click;

            pnlToolbar.Controls.Add(lblFilterLabel);
            pnlToolbar.Controls.Add(cboStatusFilter);
            pnlToolbar.Controls.Add(btnRefresh);

            // ── Detail panel — line items (Bottom, height 260→338) ───
            Panel pnlDetail = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 338,                         // 260 × 1.3
                BackColor = Palette.BgCard
            };
            pnlDetail.Paint += (s, e) =>
                e.Graphics.DrawRectangle(
                    new System.Drawing.Pen(Palette.BorderColor, 1),
                    0, 0, ((Panel)s).Width - 1, ((Panel)s).Height - 1);

            lblDetailTitle = new Label
            {
                Text      = "Select an order to view details",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Palette.TextMain,
                Dock      = DockStyle.Top,
                Height    = 49,                          // 38 × 1.3
                Padding   = new Padding(16, 13, 0, 0)    // 12/10 × 1.3
            };

            dgvLines = MakeDgv();
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colItemID",    HeaderText = "Item ID",    FillWeight = 14 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colItemName",  HeaderText = "Item Name",  FillWeight = 36 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colQty",       HeaderText = "Qty",        FillWeight = 10 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colPrice",     HeaderText = "Unit Price", FillWeight = 20 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colLineTotal", HeaderText = "Line Total", FillWeight = 20 });
            dgvLines.Dock = DockStyle.Fill;

            pnlDetail.Controls.Add(dgvLines);
            pnlDetail.Controls.Add(lblDetailTitle);

            // ── Orders grid (Fill) ──────────────────────────────
            dgvOrders = MakeDgv();
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colOrderID",  HeaderText = "Order ID",      FillWeight = 14 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colCustomer", HeaderText = "Customer",      FillWeight = 26 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colIssued",   HeaderText = "Issued Date",   FillWeight = 14 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colDelivery", HeaderText = "Delivery Date", FillWeight = 14 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colTotal",    HeaderText = "Grand Total",   FillWeight = 16 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colStatus",   HeaderText = "Status",        FillWeight = 16 });
            dgvOrders.SelectionChanged += dgvOrders_SelectionChanged;
            dgvOrders.Dock = DockStyle.Fill;

            // ── Main content panel ──────────────────────────────
            Panel pnlMain = new Panel
            {
                Dock    = DockStyle.Fill,
                Padding = new Padding(26, 16, 26, 16)    // 20/12 × 1.3
            };
            // DockStyle order: Fill first, Bottom second, Top last
            pnlMain.Controls.Add(dgvOrders);
            pnlMain.Controls.Add(pnlDetail);
            pnlMain.Controls.Add(pnlToolbar);

            this.Controls.Add(pnlMain);
            this.Controls.Add(_shell);

            this.ResumeLayout(false);
        }

        // ── DGV factory ──────────────────────────────────────
        private DataGridView MakeDgv()
        {
            return new DataGridView
            {
                ReadOnly              = true,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible     = false,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor       = Palette.BgCard,
                BorderStyle           = BorderStyle.None,
                GridColor             = Palette.BorderColor,
                Font                  = new Font("Segoe UI", 14f),   // 11 × 1.3
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate           = { Height = 47 },             // 36 × 1.3
                MultiSelect           = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = System.Drawing.Color.FromArgb(246, 249, 255),
                    ForeColor = Palette.TextMuted,
                    Font      = new Font("Segoe UI", 13.5f, FontStyle.Bold), // 10.5 × 1.3
                    Padding   = new Padding(8)                       // 6 × 1.3
                },
                ColumnHeadersHeight = 52,                            // 40 × 1.3
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor          = Palette.BgCard,
                    ForeColor          = Palette.TextMain,
                    SelectionBackColor = System.Drawing.Color.FromArgb(240, 246, 255),
                    SelectionForeColor = Palette.TextMain,
                    Padding            = new Padding(10, 7, 10, 7)   // 8/5 × 1.3
                }
            };
        }
    }
}
