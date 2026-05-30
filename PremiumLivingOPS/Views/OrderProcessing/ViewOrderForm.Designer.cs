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
            this.Font          = new Font("Segoe UI", 11f);

            // ── AppShell (TopNavBar + UserBar) ──────────────────────
            _shell = new AppShell();
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;

            // ── Outer content panel (fills below AppShell) ───────────
            Panel pnlContent = new Panel
            {
                Dock      = DockStyle.Fill,
                Padding   = new Padding(28, 20, 28, 24),
                BackColor = Palette.BgPage
            };

            // ── Page title ───────────────────────────────────────
            Label lblTitle = new Label
            {
                Text      = "View Orders",
                Font      = new Font("Segoe UI", 22f, FontStyle.Bold),
                ForeColor = Palette.TextMain,
                Dock      = DockStyle.Top,
                Height    = 42
            };

            // ── Filter toolbar ─────────────────────────────────
            Panel pnlToolbar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 52,
                BackColor = Palette.BgCard,
                Padding   = new Padding(12, 9, 12, 9)
            };
            pnlToolbar.Paint += (s, e) =>
                e.Graphics.DrawRectangle(
                    new System.Drawing.Pen(Palette.BorderColor, 1),
                    0, 0, ((Panel)s).Width - 1, ((Panel)s).Height - 1);

            lblFilterLabel = new Label
            {
                Text      = "Status:",
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Palette.TextMuted,
                AutoSize  = true,
                Location  = new Point(12, 14)
            };
            cboStatusFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width         = 160,
                Location      = new Point(72, 12),
                Font          = new Font("Segoe UI", 11f)
            };
            cboStatusFilter.Items.AddRange(new object[]
                { "All", "Pending", "Processing", "Delivered", "Cancelled" });
            cboStatusFilter.SelectedIndex = 0;
            cboStatusFilter.SelectedIndexChanged += cboStatusFilter_SelectedIndexChanged;

            btnRefresh = new Button
            {
                Text      = "↻ Refresh",
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Palette.Primary,
                FlatStyle = FlatStyle.Flat,
                Width     = 100,
                Height    = 32,
                Location  = new Point(246, 10)
            };
            btnRefresh.FlatAppearance.BorderColor = Palette.Primary;
            btnRefresh.FlatAppearance.BorderSize  = 1;
            btnRefresh.Click += btnRefresh_Click;

            pnlToolbar.Controls.Add(lblFilterLabel);
            pnlToolbar.Controls.Add(cboStatusFilter);
            pnlToolbar.Controls.Add(btnRefresh);

            // Spacer between toolbar and grids
            Panel pnlSpacer = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 10,
                BackColor = Palette.BgPage
            };

            // ── Detail panel (BOTTOM, fixed height) ────────────────
            Panel pnlDetail = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 260,
                BackColor = Palette.BgCard
            };
            pnlDetail.Paint += (s, e) =>
                e.Graphics.DrawRectangle(
                    new System.Drawing.Pen(Palette.BorderColor, 1),
                    0, 0, ((Panel)s).Width - 1, ((Panel)s).Height - 1);

            lblDetailTitle = new Label
            {
                Text      = "Select an order to view details",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Palette.TextMain,
                Dock      = DockStyle.Top,
                Height    = 40,
                Padding   = new Padding(12, 10, 0, 0)
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

            // ── Orders grid (FILL — takes all remaining space) ───────
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

            // ── Add controls in REVERSE DockStyle.Top order ───────────
            // DockStyle.Bottom and DockStyle.Fill must be added before Top items
            // so the layout engine reserves space correctly.
            pnlContent.Controls.Add(dgvOrders);   // Fill — added first
            pnlContent.Controls.Add(pnlDetail);   // Bottom
            pnlContent.Controls.Add(pnlSpacer);   // Top (added last among Top items → topmost)
            pnlContent.Controls.Add(pnlToolbar);  // Top
            pnlContent.Controls.Add(lblTitle);    // Top (topmost visually → added last)

            this.Controls.Add(pnlContent);  // Fill
            this.Controls.Add(_shell);      // Top (AppShell docks to top of Form)

            this.ResumeLayout(false);
        }

        // ── DGV factory ─────────────────────────────────────────────
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
                Font                  = new Font("Segoe UI", 11f),
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate           = { Height = 36 },
                MultiSelect           = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = System.Drawing.Color.FromArgb(246, 249, 255),
                    ForeColor = Palette.TextMuted,
                    Font      = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                    Padding   = new Padding(6)
                },
                ColumnHeadersHeight = 40,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor          = Palette.BgCard,
                    ForeColor          = Palette.TextMain,
                    SelectionBackColor = System.Drawing.Color.FromArgb(240, 246, 255),
                    SelectionForeColor = Palette.TextMain,
                    Padding            = new Padding(8, 5, 8, 5)
                }
            };
        }
    }
}
