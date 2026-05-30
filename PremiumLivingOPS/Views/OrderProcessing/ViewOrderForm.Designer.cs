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

            // ── Filter toolbar (Top) ─────────────────────────────
            Panel pnlToolbar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 52,
                BackColor = Palette.BgCard,
                Padding   = new Padding(16, 10, 16, 10)
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
                Location  = new Point(16, 15)
            };
            cboStatusFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width         = 160,
                Location      = new Point(76, 13),
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
                Height    = 30,
                Location  = new Point(252, 11)
            };
            btnRefresh.FlatAppearance.BorderColor = Palette.Primary;
            btnRefresh.FlatAppearance.BorderSize  = 1;
            btnRefresh.Click += btnRefresh_Click;

            pnlToolbar.Controls.Add(lblFilterLabel);
            pnlToolbar.Controls.Add(cboStatusFilter);
            pnlToolbar.Controls.Add(btnRefresh);

            // ── Detail panel — line items (Bottom, fixed height) ──────
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
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Palette.TextMain,
                Dock      = DockStyle.Top,
                Height    = 38,
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

            // Controls added bottom-up: Fill first, then Top items
            pnlDetail.Controls.Add(dgvLines);
            pnlDetail.Controls.Add(lblDetailTitle);

            // ── Orders grid (Fill — takes all remaining space) ─────────
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

            // ── Main content panel ─────────────────────────────────
            // DockStyle layout rules:
            //   - Add Fill/Bottom controls FIRST (they are claimed last by layout)
            //   - Add Top controls AFTER (they push down from the top)
            // Result: toolbar at top, detail panel at bottom, orders grid fills middle.
            Panel pnlMain = new Panel
            {
                Dock    = DockStyle.Fill,
                Padding = new Padding(20, 12, 20, 12)
            };
            pnlMain.Controls.Add(dgvOrders);   // Fill  (added first)
            pnlMain.Controls.Add(pnlDetail);   // Bottom
            pnlMain.Controls.Add(pnlToolbar);  // Top   (added last → appears at top)

            // ── Compose form ─────────────────────────────────────
            this.Controls.Add(pnlMain);  // Fill
            this.Controls.Add(_shell);   // Top  (AppShell docks above pnlMain)

            this.ResumeLayout(false);
        }

        // ── DGV factory ─────────────────────────────────────────
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
