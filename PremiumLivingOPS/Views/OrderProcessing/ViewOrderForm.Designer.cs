using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.OrderProcessing
{
    partial class ViewOrderForm
    {
        private System.ComponentModel.IContainer components = null;

        private AppShell         _shell;
        private TextBox          txtSearchOrderNo;
        private TextBox          txtSearchCustomer;
        private ComboBox         cboStatus;
        private DateTimePicker   dtpDateFrom;
        private CheckBox         chkDateFrom;
        private Button           btnSearch;
        private Button           btnRefresh;
        private Panel            pnlKpi;
        private DataGridView     dgvOrders;
        private Panel            pnlActions;
        private Button           btnViewDetail;
        private Button           btnModifyOrder;

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
            this.BackColor     = Color.FromArgb(240, 244, 249);
            this.WindowState   = FormWindowState.Maximized;
            this.Font          = new Font("Segoe UI", 13f);

            // ── Root
            var pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };
            _shell = new AppShell();
            _shell.SetPopupContainer(pnlMain);

            // ═════════════════════════════════════════════════════════════
            // SEARCH CARD
            // One TableLayoutPanel owns all 3 rows with fixed pixel heights:
            //   Row 0 = 36px  : "Search Orders" title + bottom divider
            //   Row 1 = 62px  : 4-column field strip
            //   Row 2 = 46px  : Search + Refresh buttons
            // Total content = 144px.  Card padding top+bottom = 14+10 = 24px.
            // pnlSearchOuter height = 14(outer top) + 144 + 10(card bottom pad) = 168px.
            // ═════════════════════════════════════════════════════════════

            // ── Input controls
            txtSearchOrderNo = new TextBox
            {
                Font = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "ORD-XXXX"
            };
            txtSearchOrderNo.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            txtSearchCustomer = new TextBox
            {
                Font = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Name or ID"
            };
            txtSearchCustomer.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            cboStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            cboStatus.Items.AddRange(new object[] { "All", "Pending", "Processing", "Shipped", "Delivered", "Cancelled" });
            cboStatus.SelectedIndex = 0;
            cboStatus.SelectedIndexChanged += (s, e) => RefreshGrid();

            chkDateFrom = new CheckBox { Text = "", Width = 24, Checked = false, Cursor = Cursors.Hand };
            dtpDateFrom = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value  = DateTime.Today.AddMonths(-1),
                Font   = new Font("Segoe UI", 12f),
                Enabled = false
            };
            chkDateFrom.CheckedChanged += (s, e) => { dtpDateFrom.Enabled = chkDateFrom.Checked; RefreshGrid(); };
            dtpDateFrom.ValueChanged   += (s, e) => { if (chkDateFrom.Checked) RefreshGrid(); };

            // ── Helper: builds a label-on-top + control-below cell panel
            // The cell panel uses absolute positions so heights are guaranteed.
            Panel MakeCell(string caption, Control ctrl, bool rightPad = true)
            {
                var cell = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
                if (rightPad) cell.Padding = new Padding(0, 0, 12, 0);

                var lbl = new Label
                {
                    Text      = caption,
                    Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(98, 112, 135),
                    Location  = new Point(0, 0),
                    Height    = 20,
                    Anchor    = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right
                };
                ctrl.Location = new Point(0, 22);
                ctrl.Height   = 30;
                ctrl.Anchor   = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;

                cell.Controls.Add(lbl);
                cell.Controls.Add(ctrl);

                // Keep label and control widths in sync with cell width
                cell.Resize += (s, e) =>
                {
                    int w = cell.ClientSize.Width - (rightPad ? 12 : 0);
                    lbl.Width  = w;
                    ctrl.Width = w;
                };
                return cell;
            }

            // Date-From cell: label + [checkbox | datepicker] row
            var cellDate = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var lblDate  = new Label
            {
                Text      = "Date From",
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Location  = new Point(0, 0),
                Height    = 20,
                Anchor    = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right
            };
            chkDateFrom.Location = new Point(0, 24);
            chkDateFrom.Height   = 26;
            dtpDateFrom.Location = new Point(28, 24);
            dtpDateFrom.Height   = 26;
            dtpDateFrom.Anchor   = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            cellDate.Controls.Add(lblDate);
            cellDate.Controls.Add(chkDateFrom);
            cellDate.Controls.Add(dtpDateFrom);
            cellDate.Resize += (s, e) =>
            {
                lblDate.Width     = cellDate.ClientSize.Width;
                dtpDateFrom.Width = cellDate.ClientSize.Width - 30;
            };

            // ── Row 1: 4-column fields  (62 px)
            var tblFields = new TableLayoutPanel
            {
                Location        = new Point(0, 0),   // positioned by outer TLP
                Dock            = DockStyle.Fill,
                ColumnCount     = 4,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblFields.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblFields.Controls.Add(MakeCell("Order No.", txtSearchOrderNo), 0, 0);
            tblFields.Controls.Add(MakeCell("Customer",  txtSearchCustomer), 1, 0);
            tblFields.Controls.Add(MakeCell("Status",    cboStatus),         2, 0);
            tblFields.Controls.Add(cellDate,                                 3, 0);

            // ── Row 2: buttons  (46 px)
            var pnlBtns = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnSearch  = MakePrimaryBtn("Search",      new Point(0,   6), 110, 32);
            btnRefresh = MakeOutlineBtn("↻  Refresh",  new Point(118, 6), 120, 32);
            btnSearch.Click  += (s, e) => RefreshGrid();
            btnRefresh.Click += (s, e) => RefreshGrid();
            pnlBtns.Controls.Add(btnSearch);
            pnlBtns.Controls.Add(btnRefresh);

            // ── Master TableLayoutPanel for the card body (3 rows, fixed heights)
            var tblCard = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                RowCount    = 3,
                ColumnCount = 1,
                BackColor   = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding     = new Padding(18, 10, 18, 8)
            };
            tblCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));  // title
            tblCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 62f));  // fields
            tblCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));  // buttons

            // Row 0: title panel
            var pnlTitle = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var lblTitle = new Label
            {
                Text      = "Search Orders",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var divider = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(221, 227, 236) };
            pnlTitle.Controls.Add(lblTitle);
            pnlTitle.Controls.Add(divider);

            tblCard.Controls.Add(pnlTitle,   0, 0);
            tblCard.Controls.Add(tblFields,  0, 1);
            tblCard.Controls.Add(pnlBtns,    0, 2);

            // White card shell
            var pnlCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlCard.Paint += PaintCardBorder;
            pnlCard.Controls.Add(tblCard);

            // Outer grey wrapper (top-docked, fixed height)
            // 36 + 62 + 46 = 144 content + 10+8 TLP padding + 14 outer top margin = 176
            var pnlSearchOuter = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 176,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding   = new Padding(20, 14, 20, 8)
            };
            pnlSearchOuter.Controls.Add(pnlCard);

            // ── KPI bar
            pnlKpi = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Color.White, Padding = new Padding(20, 12, 20, 0) };
            pnlKpi.Paint += PaintBottomBorder;

            // ── Action bar
            pnlActions = new Panel { Dock = DockStyle.Bottom, Height = 64, BackColor = Color.White, Padding = new Padding(20, 12, 20, 12) };
            pnlActions.Paint += PaintTopBorder;
            btnViewDetail  = MakePrimaryBtn("\uD83D\uDD0D  View Details", new Point(20,  12), 180, 40);
            btnModifyOrder = MakeWarningBtn("✏️  Modify Order",  new Point(210, 12), 180, 40);
            btnViewDetail.Enabled = btnModifyOrder.Enabled = false;
            btnViewDetail.Click  += btnViewDetail_Click;
            btnModifyOrder.Click += btnModifyOrder_Click;
            pnlActions.Controls.Add(btnViewDetail);
            pnlActions.Controls.Add(btnModifyOrder);

            // ── Grid
            dgvOrders = new DataGridView
            {
                ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(221, 227, 236), Font = new Font("Segoe UI", 13f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate = { Height = 48 }, Dock = DockStyle.Fill,
                ColumnHeadersHeight = 46, EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255), ForeColor = Color.FromArgb(98, 112, 135),
                    Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                    Padding = new Padding(12, 0, 0, 0), Alignment = DataGridViewContentAlignment.MiddleLeft
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.White, ForeColor = Color.FromArgb(15, 31, 53),
                    SelectionBackColor = Color.FromArgb(219, 234, 254), SelectionForeColor = Color.FromArgb(15, 31, 53),
                    Padding = new Padding(12, 6, 12, 6)
                }
            };
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrderID",  HeaderText = "ORDER NO.",     FillWeight = 14 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomer", HeaderText = "CUSTOMER",      FillWeight = 22 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSales",    HeaderText = "SALES STAFF",   FillWeight = 16 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIssued",   HeaderText = "ISSUED DATE",   FillWeight = 13 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDelivery", HeaderText = "DELIVERY DATE", FillWeight = 13 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal",    HeaderText = "GRAND TOTAL",   FillWeight = 13 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",   HeaderText = "STATUS",        FillWeight = 10 });
            dgvOrders.SelectionChanged += dgvOrders_SelectionChanged;
            dgvOrders.CellFormatting   += dgvOrders_CellFormatting;
            dgvOrders.CellDoubleClick  += dgvOrders_CellDoubleClick;

            var pnlGridCard = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 12, 20, 0), BackColor = Color.FromArgb(240, 244, 249) };
            var pnlGridInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlGridInner.Paint += PaintCardBorder;
            pnlGridInner.Controls.Add(dgvOrders);
            pnlGridCard.Controls.Add(pnlGridInner);

            // ── Assemble — Fill first, Bottom, then Top controls in desired top-to-bottom order
            pnlMain.Controls.Add(pnlGridCard);    // Fill
            pnlMain.Controls.Add(pnlActions);     // Bottom
            pnlMain.Controls.Add(pnlKpi);         // Top #2 (renders below search)
            pnlMain.Controls.Add(pnlSearchOuter); // Top #1 (renders below AppShell)
            pnlMain.Controls.Add(_shell);          // Top #0 (AppShell — topmost)

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        // ── Button factories
        private Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.White,
                BackColor = Color.FromArgb(47, 111, 237), FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(26, 77, 192);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(21, 60, 155);
            return b;
        }
        private Button MakeWarningBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.White,
                BackColor = Color.FromArgb(245, 158, 11), FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(217, 119, 6);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(180, 90, 0);
            return b;
        }
        private Button MakeOutlineBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.White, FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize  = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            return b;
        }

        // ── Border painters
        private static void PaintBottomBorder(object s, PaintEventArgs e)
        { var p = (Panel)s; using var pen = new Pen(Color.FromArgb(221, 227, 236), 1); e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1); }
        private static void PaintTopBorder(object s, PaintEventArgs e)
        { var p = (Panel)s; using var pen = new Pen(Color.FromArgb(221, 227, 236), 1); e.Graphics.DrawLine(pen, 0, 0, p.Width, 0); }
        private static void PaintCardBorder(object s, PaintEventArgs e)
        { var p = (Panel)s; using var pen = new Pen(Color.FromArgb(221, 227, 236), 1); e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1); }
    }
}
