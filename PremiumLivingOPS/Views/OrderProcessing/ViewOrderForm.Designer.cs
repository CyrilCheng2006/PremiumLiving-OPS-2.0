using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.OrderProcessing
{
    partial class ViewOrderForm
    {
        private System.ComponentModel.IContainer components = null;

        // Shell
        private AppShell _shell;

        // Toolbar
        private TextBox  txtSearch;
        private Button   btnSearch;
        private ComboBox cboStatus;
        private Button   btnRefresh;
        private Label    lblSearchLabel;
        private Label    lblStatusLabel;

        // Main grid
        private DataGridView dgvOrders;

        // Action bar
        private Panel  pnlActions;
        private Button btnViewDetail;
        private Button btnModifyOrder;

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
            this.Font          = new Font("Segoe UI", 14f);

            // ── AppShell ──────────────────────────────────────────────────────
            _shell = new AppShell();
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;

            // ── Toolbar (Top) ─────────────────────────────────────────────────
            Panel pnlToolbar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 72,
                BackColor = Palette.BgCard,
                Padding   = new Padding(20, 16, 20, 0)
            };
            pnlToolbar.Paint += (s, e) =>
                e.Graphics.DrawRectangle(
                    new System.Drawing.Pen(Palette.BorderColor, 1),
                    0, 0, ((Panel)s).Width - 1, ((Panel)s).Height - 1);

            lblSearchLabel = new Label
            {
                Text = "Search:", Font = new Font("Segoe UI", 13f),
                ForeColor = Palette.TextMuted, AutoSize = true, Location = new Point(20, 22)
            };
            txtSearch = new TextBox
            {
                Width = 260, Height = 38, Location = new Point(90, 18),
                Font = new Font("Segoe UI", 13f), BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Order ID / Customer / Staff..."
            };
            txtSearch.KeyDown += txtSearch_KeyDown;

            btnSearch = MakeBtn("Search", Palette.Primary, new Point(364, 18), 120, 38);
            btnSearch.Click += btnSearch_Click;

            lblStatusLabel = new Label
            {
                Text = "Status:", Font = new Font("Segoe UI", 13f),
                ForeColor = Palette.TextMuted, AutoSize = true, Location = new Point(504, 22)
            };
            cboStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList, Width = 180,
                Location = new Point(572, 18), Font = new Font("Segoe UI", 13f)
            };
            cboStatus.Items.AddRange(new object[] { "All", "Pending", "Processing", "Delivered", "Cancelled" });
            cboStatus.SelectedIndex = 0;
            cboStatus.SelectedIndexChanged += cboStatus_Changed;

            btnRefresh = MakeBtn("↻  Refresh", Palette.Primary, new Point(768, 18), 130, 38);
            btnRefresh.Click += btnRefresh_Click;

            pnlToolbar.Controls.Add(lblSearchLabel);
            pnlToolbar.Controls.Add(txtSearch);
            pnlToolbar.Controls.Add(btnSearch);
            pnlToolbar.Controls.Add(lblStatusLabel);
            pnlToolbar.Controls.Add(cboStatus);
            pnlToolbar.Controls.Add(btnRefresh);

            // ── Action bar (Bottom) ───────────────────────────────────────────
            pnlActions = new Panel
            {
                Dock = DockStyle.Bottom, Height = 70,
                BackColor = Palette.BgCard, Padding = new Padding(20, 14, 20, 14)
            };
            pnlActions.Paint += (s, e) =>
                e.Graphics.DrawRectangle(
                    new System.Drawing.Pen(Palette.BorderColor, 1),
                    0, 0, ((Panel)s).Width - 1, ((Panel)s).Height - 1);

            btnViewDetail  = MakeBtn("View Details",  Palette.Primary, new Point(20,  14), 180, 42);
            btnModifyOrder = MakeBtn("Modify Order",  Palette.Warning, new Point(214, 14), 180, 42);
            btnViewDetail.Enabled  = false;
            btnModifyOrder.Enabled = false;
            btnViewDetail.Click  += btnViewDetail_Click;
            btnModifyOrder.Click += btnModifyOrder_Click;

            pnlActions.Controls.Add(btnViewDetail);
            pnlActions.Controls.Add(btnModifyOrder);

            // ── Orders DataGridView (Fill) ────────────────────────────────────
            dgvOrders = new DataGridView
            {
                ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Palette.BgCard, BorderStyle = BorderStyle.None,
                GridColor = Palette.BorderColor, Font = new Font("Segoe UI", 14f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate = { Height = 48 }, MultiSelect = false, Dock = DockStyle.Fill,
                ColumnHeadersHeight = 52,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = System.Drawing.Color.FromArgb(240, 245, 255),
                    ForeColor = Palette.TextMuted,
                    Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                    Padding   = new Padding(8)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor          = Palette.BgCard,
                    ForeColor          = Palette.TextMain,
                    SelectionBackColor = System.Drawing.Color.FromArgb(230, 240, 255),
                    SelectionForeColor = Palette.TextMain,
                    Padding            = new Padding(10, 6, 10, 6)
                }
            };

            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrderID",  HeaderText = "Order ID",      FillWeight = 14 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomer", HeaderText = "Customer",      FillWeight = 22 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSales",    HeaderText = "Sales Staff",   FillWeight = 18 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIssued",   HeaderText = "Issued Date",   FillWeight = 14 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDelivery", HeaderText = "Delivery Date", FillWeight = 14 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal",    HeaderText = "Grand Total",   FillWeight = 14 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",   HeaderText = "Status",        FillWeight = 10 });

            dgvOrders.SelectionChanged += dgvOrders_SelectionChanged;
            dgvOrders.CellFormatting   += dgvOrders_CellFormatting;

            // ── Main content panel ────────────────────────────────────────────
            Panel pnlMain = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 12, 20, 12) };
            // DockStyle order: Fill first, Bottom second, Top last
            pnlMain.Controls.Add(dgvOrders);
            pnlMain.Controls.Add(pnlActions);
            pnlMain.Controls.Add(pnlToolbar);

            this.Controls.Add(pnlMain);
            this.Controls.Add(_shell);

            this.ResumeLayout(false);
        }

        private Button MakeBtn(string text, System.Drawing.Color color, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text = text, Font = new Font("Segoe UI", 13f),
                ForeColor = color, FlatStyle = FlatStyle.Flat,
                Location = loc, Width = w, Height = h
            };
            b.FlatAppearance.BorderColor = color;
            b.FlatAppearance.BorderSize  = 1;
            return b;
        }
    }
}
