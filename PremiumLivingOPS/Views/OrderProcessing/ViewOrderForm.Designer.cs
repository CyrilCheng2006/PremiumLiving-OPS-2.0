using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.OrderProcessing
{
    partial class ViewOrderForm
    {
        private System.ComponentModel.IContainer components = null;

        private AppShell    _shell;
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

            Panel pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage };

            // ── AppShell ────────────────────────────────────────────────
            _shell = new AppShell();
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;
            _shell.SetPopupContainer(pnlMain);

            // ── Content panel ──────────────────────────────────────────
            Panel pnlContent = new Panel
            {
                Dock    = DockStyle.Fill,
                Padding = new Padding(24, 20, 24, 24),
                BackColor = Palette.BgPage
            };

            // ── Page title ────────────────────────────────────────────
            Label lblTitle = new Label
            {
                Text      = "View Orders",
                Font      = new Font("Segoe UI", 22f, FontStyle.Bold),
                ForeColor = Palette.TextMain,
                AutoSize  = true,
                Location  = new Point(0, 0)
            };

            // ── Filter toolbar ────────────────────────────────────────
            Panel pnlToolbar = new Panel
            {
                Height    = 48,
                BackColor = Palette.BgCard,
                Padding   = new Padding(12, 8, 12, 8)
            };
            pnlToolbar.Paint += (s, e) =>
                e.Graphics.DrawRectangle(new System.Drawing.Pen(Palette.BorderColor, 1), 0, 0,
                    ((Panel)s).Width - 1, ((Panel)s).Height - 1);

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
                Location      = new Point(70, 10),
                Font          = new Font("Segoe UI", 11f)
            };
            cboStatusFilter.Items.AddRange(new object[] { "All", "Pending", "Processing", "Delivered", "Cancelled" });
            cboStatusFilter.SelectedIndex = 0;
            cboStatusFilter.SelectedIndexChanged += cboStatusFilter_SelectedIndexChanged;

            btnRefresh = new Button
            {
                Text      = "\u21BB Refresh",
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Palette.Primary,
                FlatStyle = FlatStyle.Flat,
                Width     = 100,
                Height    = 32,
                Location  = new Point(244, 8)
            };
            btnRefresh.FlatAppearance.BorderColor = Palette.Primary;
            btnRefresh.FlatAppearance.BorderSize  = 1;
            btnRefresh.Click += btnRefresh_Click;

            pnlToolbar.Controls.Add(lblFilterLabel);
            pnlToolbar.Controls.Add(cboStatusFilter);
            pnlToolbar.Controls.Add(btnRefresh);

            // ── Orders DataGridView ─────────────────────────────────
            dgvOrders = MakeDgv();
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrderID",   HeaderText = "Order ID",       FillWeight = 15, SortMode = DataGridViewColumnSortMode.NotSortable });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomer",  HeaderText = "Customer",       FillWeight = 25, SortMode = DataGridViewColumnSortMode.NotSortable });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIssued",    HeaderText = "Issued Date",    FillWeight = 15, SortMode = DataGridViewColumnSortMode.NotSortable });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDelivery",  HeaderText = "Delivery Date",  FillWeight = 15, SortMode = DataGridViewColumnSortMode.NotSortable });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal",     HeaderText = "Grand Total",    FillWeight = 15, SortMode = DataGridViewColumnSortMode.NotSortable });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",    HeaderText = "Status",         FillWeight = 15, SortMode = DataGridViewColumnSortMode.NotSortable });
            dgvOrders.SelectionChanged += dgvOrders_SelectionChanged;

            // ── Line items panel ─────────────────────────────────────
            Panel pnlDetail = new Panel { Height = 260, BackColor = Palette.BgCard };
            pnlDetail.Paint += (s, e) =>
                e.Graphics.DrawRectangle(new System.Drawing.Pen(Palette.BorderColor, 1), 0, 0,
                    ((Panel)s).Width - 1, ((Panel)s).Height - 1);

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
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItemID",   HeaderText = "Item ID",    FillWeight = 15, SortMode = DataGridViewColumnSortMode.NotSortable });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItemName", HeaderText = "Item Name",  FillWeight = 35, SortMode = DataGridViewColumnSortMode.NotSortable });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQty",      HeaderText = "Qty",        FillWeight = 10, SortMode = DataGridViewColumnSortMode.NotSortable });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPrice",    HeaderText = "Unit Price", FillWeight = 20, SortMode = DataGridViewColumnSortMode.NotSortable });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLineTotal",HeaderText = "Line Total", FillWeight = 20, SortMode = DataGridViewColumnSortMode.NotSortable });
            dgvLines.Dock = DockStyle.Fill;

            pnlDetail.Controls.Add(dgvLines);
            pnlDetail.Controls.Add(lblDetailTitle);

            // ── Layout using SplitContainer ───────────────────────────
            SplitContainer split = new SplitContainer
            {
                Dock           = DockStyle.Fill,
                Orientation    = Orientation.Horizontal,
                SplitterDistance = 400,
                Panel1MinSize  = 200,
                Panel2MinSize  = 180,
                BackColor      = Palette.BgPage
            };
            split.Panel1.Controls.Add(dgvOrders);
            split.Panel2.Controls.Add(pnlDetail);

            FlowLayoutPanel flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
                WrapContents = false, AutoSize = false, BackColor = Palette.BgPage
            };
            flow.Controls.Add(lblTitle);
            flow.Controls.Add(new Panel { Height = 12, Width = 10, BackColor = Palette.BgPage });
            flow.Controls.Add(pnlToolbar);
            flow.Controls.Add(new Panel { Height = 8,  Width = 10, BackColor = Palette.BgPage });

            pnlContent.Controls.Add(split);
            pnlContent.Controls.Add(flow);

            pnlContent.Resize += (s, e) =>
            {
                int w = pnlContent.ClientSize.Width - pnlContent.Padding.Horizontal;
                flow.Width     = w;
                pnlToolbar.Width = w;
                split.Width    = w;
                split.Height   = pnlContent.ClientSize.Height
                                 - pnlContent.Padding.Vertical
                                 - lblTitle.Height - 12 - 48 - 8;
                split.Top      = lblTitle.Height + 12 + 48 + 8 + pnlContent.Padding.Top;
            };

            pnlMain.Controls.Add(pnlContent);
            pnlMain.Controls.Add(_shell);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        // ── DGV factory ───────────────────────────────────────────────────
        private DataGridView MakeDgv()
        {
            return new DataGridView
            {
                ReadOnly             = true,
                AllowUserToAddRows   = false,
                AllowUserToDeleteRows= false,
                RowHeadersVisible    = false,
                SelectionMode        = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor      = Palette.BgCard,
                BorderStyle          = BorderStyle.None,
                GridColor            = Palette.BorderColor,
                Font                 = new Font("Segoe UI", 11f),
                AutoSizeColumnsMode  = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle      = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate          = { Height = 36 },
                MultiSelect          = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255),
                    ForeColor = Palette.TextMuted,
                    Font      = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                    Padding   = new Padding(6)
                },
                ColumnHeadersHeight = 40,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor          = Palette.BgCard,
                    ForeColor          = Palette.TextMain,
                    SelectionBackColor = Color.FromArgb(240, 246, 255),
                    SelectionForeColor = Palette.TextMain,
                    Padding            = new Padding(8, 5, 8, 5)
                }
            };
        }
    }
}
