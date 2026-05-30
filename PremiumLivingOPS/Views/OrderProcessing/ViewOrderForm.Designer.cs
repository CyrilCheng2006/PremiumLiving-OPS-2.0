using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.OrderProcessing
{
    partial class ViewOrderForm
    {
        private System.ComponentModel.IContainer components = null;

        // Shell
        private AppShell _shell;

        // ── Toolbar controls ──────────────────────────────────────────────────
        private TextBox  txtSearch;
        private Button   btnSearch;
        private ComboBox cboStatus;
        private Button   btnRefresh;

        // ── KPI Summary bar ───────────────────────────────────────────────────
        private Panel pnlKpi;

        // ── Main grid ─────────────────────────────────────────────────────────
        private DataGridView dgvOrders;

        // ── Action bar ────────────────────────────────────────────────────────
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
            this.MinimumSize   = new Size(1200, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = Color.FromArgb(240, 244, 249);   // --bg: #f0f4f9
            this.WindowState   = FormWindowState.Maximized;
            this.Font          = new Font("Segoe UI", 13f);

            // ─────────────────────────────────────────────────────────────────
            // AppShell
            // ─────────────────────────────────────────────────────────────────
            _shell = new AppShell();
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;

            // ─────────────────────────────────────────────────────────────────
            // Toolbar panel
            // ─────────────────────────────────────────────────────────────────
            Panel pnlToolbar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 72,
                BackColor = Color.White,
                Padding   = new Padding(20, 0, 20, 0)
            };
            pnlToolbar.Paint += PaintBottomBorder;

            // Page title
            var lblTitle = new Label
            {
                Text      = "Order Tracking",
                Font      = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                AutoSize  = true,
                Location  = new Point(20, 18)
            };

            // Search box
            txtSearch = new TextBox
            {
                Width       = 240, Height = 36, Location = new Point(220, 18),
                Font        = new Font("Segoe UI", 13f),
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Order ID / Customer / Staff..."
            };
            txtSearch.KeyDown += txtSearch_KeyDown;

            // Search button
            btnSearch = MakePrimaryBtn("Search", new Point(468, 18), 100, 36);
            btnSearch.Click += btnSearch_Click;

            // Status combo
            var lblStatus = new Label
            {
                Text      = "Status:",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(98, 112, 135),
                AutoSize  = true,
                Location  = new Point(584, 24)
            };
            cboStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList, Width = 160,
                Location      = new Point(644, 18), Font = new Font("Segoe UI", 13f)
            };
            cboStatus.Items.AddRange(new object[] { "All", "Pending", "Processing", "Shipped", "Delivered", "Cancelled" });
            cboStatus.SelectedIndex      = 0;
            cboStatus.SelectedIndexChanged += cboStatus_Changed;

            // Refresh button
            btnRefresh = MakeOutlineBtn("↻  Refresh", new Point(820, 18), 120, 36);
            btnRefresh.Click += btnRefresh_Click;

            pnlToolbar.Controls.Add(lblTitle);
            pnlToolbar.Controls.Add(txtSearch);
            pnlToolbar.Controls.Add(btnSearch);
            pnlToolbar.Controls.Add(lblStatus);
            pnlToolbar.Controls.Add(cboStatus);
            pnlToolbar.Controls.Add(btnRefresh);

            // ─────────────────────────────────────────────────────────────────
            // KPI Summary bar  (5 pills: Total / Pending / Processing / Delivered / Cancelled)
            // ─────────────────────────────────────────────────────────────────
            pnlKpi = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 70,
                BackColor = Color.White,
                Padding   = new Padding(20, 12, 20, 0)
            };
            pnlKpi.Paint += PaintBottomBorder;
            // KPI labels are created dynamically in ViewOrderForm.cs → RefreshKpi()

            // ─────────────────────────────────────────────────────────────────
            // Action bar (bottom)
            // ─────────────────────────────────────────────────────────────────
            pnlActions = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 64,
                BackColor = Color.White,
                Padding   = new Padding(20, 12, 20, 12)
            };
            pnlActions.Paint += PaintTopBorder;

            btnViewDetail  = MakePrimaryBtn("🔍  View Details",  new Point(20,  12), 180, 40);
            btnModifyOrder = MakeWarningBtn("✏️  Modify Order",  new Point(210, 12), 180, 40);
            btnViewDetail.Enabled  = false;
            btnModifyOrder.Enabled = false;
            btnViewDetail.Click  += btnViewDetail_Click;
            btnModifyOrder.Click += btnModifyOrder_Click;

            pnlActions.Controls.Add(btnViewDetail);
            pnlActions.Controls.Add(btnModifyOrder);

            // ─────────────────────────────────────────────────────────────────
            // Orders DataGridView (Fill)
            // ─────────────────────────────────────────────────────────────────
            dgvOrders = new DataGridView
            {
                ReadOnly              = true,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible     = false,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect           = false,
                BackgroundColor       = Color.White,
                BorderStyle           = BorderStyle.None,
                GridColor             = Color.FromArgb(221, 227, 236),
                Font                  = new Font("Segoe UI", 13f),
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate           = { Height = 48 },
                Dock                  = DockStyle.Fill,
                ColumnHeadersHeight   = 46,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255),
                    ForeColor = Color.FromArgb(98, 112, 135),
                    Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                    Padding   = new Padding(12, 0, 0, 0),
                    Alignment = DataGridViewContentAlignment.MiddleLeft
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor          = Color.White,
                    ForeColor          = Color.FromArgb(15, 31, 53),
                    SelectionBackColor = Color.FromArgb(219, 234, 254),
                    SelectionForeColor = Color.FromArgb(15, 31, 53),
                    Padding            = new Padding(12, 6, 12, 6)
                }
            };
            // Remove the column header bottom line
            dgvOrders.EnableHeadersVisualStyles = false;

            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrderID",  HeaderText = "ORDER NO.",     FillWeight = 14 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomer", HeaderText = "CUSTOMER",      FillWeight = 22 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSales",    HeaderText = "SALES STAFF",   FillWeight = 16 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIssued",   HeaderText = "ISSUED DATE",   FillWeight = 13 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDelivery", HeaderText = "DELIVERY DATE", FillWeight = 13 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal",    HeaderText = "GRAND TOTAL",   FillWeight = 13 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",   HeaderText = "STATUS",        FillWeight = 10 });
            // Hidden action column placeholder (action handled by button bar)

            dgvOrders.SelectionChanged   += dgvOrders_SelectionChanged;
            dgvOrders.CellFormatting     += dgvOrders_CellFormatting;
            dgvOrders.CellDoubleClick    += dgvOrders_CellDoubleClick;

            // ─────────────────────────────────────────────────────────────────
            // Grid wrapper card with padding
            // ─────────────────────────────────────────────────────────────────
            Panel pnlGridCard = new Panel
            {
                Dock      = DockStyle.Fill,
                Padding   = new Padding(20, 12, 20, 0),
                BackColor = Color.FromArgb(240, 244, 249)
            };

            // Inner white card
            Panel pnlCard = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.White,
                Padding   = new Padding(0)
            };
            pnlCard.Paint += PaintCardBorder;
            pnlCard.Controls.Add(dgvOrders);

            pnlGridCard.Controls.Add(pnlCard);

            // ─────────────────────────────────────────────────────────────────
            // Assemble (DockStyle: Fill first, then Bottom, then Top-stacked)
            // ─────────────────────────────────────────────────────────────────
            this.Controls.Add(pnlGridCard);    // Fill
            this.Controls.Add(pnlActions);     // Bottom
            this.Controls.Add(pnlKpi);         // Top (added last = rendered just below toolbar)
            this.Controls.Add(pnlToolbar);     // Top (added last = topmost)
            this.Controls.Add(_shell);         // Top (AppShell sits above everything)

            this.ResumeLayout(false);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Button factory helpers
        // ─────────────────────────────────────────────────────────────────────
        private Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(47, 111, 237),
                FlatStyle = FlatStyle.Flat,
                Location  = loc, Width = w, Height = h,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize  = 0;
            b.FlatAppearance.MouseOverBackColor  = Color.FromArgb(26, 77, 192);
            b.FlatAppearance.MouseDownBackColor  = Color.FromArgb(21, 60, 155);
            return b;
        }

        private Button MakeWarningBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(245, 158, 11),
                FlatStyle = FlatStyle.Flat,
                Location  = loc, Width = w, Height = h,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(217, 119, 6);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(180, 90, 0);
            return b;
        }

        private Button MakeOutlineBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location  = loc, Width = w, Height = h,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize  = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            return b;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Border painters
        // ─────────────────────────────────────────────────────────────────────
        private static void PaintBottomBorder(object s, PaintEventArgs e)
        {
            var p = (Panel)s;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
        }
        private static void PaintTopBorder(object s, PaintEventArgs e)
        {
            var p = (Panel)s;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, 0, p.Width, 0);
        }
        private static void PaintCardBorder(object s, PaintEventArgs e)
        {
            var p = (Panel)s;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        }
    }
}
