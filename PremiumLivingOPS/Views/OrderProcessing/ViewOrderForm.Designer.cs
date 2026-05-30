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

        // Shell
        private AppShell _shell;

        // ── Search card controls (matching order-list.html Search Orders section)
        private TextBox          txtSearchOrderNo;   // Order No.
        private TextBox          txtSearchCustomer;  // Customer
        private ComboBox         cboStatus;          // Status
        private DateTimePicker   dtpDateFrom;        // Date From
        private CheckBox         chkDateFrom;        // enable/disable date filter
        private Button           btnSearch;
        private Button           btnRefresh;

        // ── KPI Summary bar
        private Panel pnlKpi;

        // ── Main grid
        private DataGridView dgvOrders;

        // ── Action bar
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
            this.BackColor     = Color.FromArgb(240, 244, 249);
            this.WindowState   = FormWindowState.Maximized;
            this.Font          = new Font("Segoe UI", 13f);

            // ── Root panel ────────────────────────────────────────────────────────
            Panel pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };

            // AppShell
            _shell = new AppShell();
            _shell.SetPopupContainer(pnlMain);

            // ═══════════════════════════════════════════════════════════════════════
            // SEARCH CARD  —  replicates the "Search Orders" section-card in
            //                  PremiumLiving-OPS-HTML / order-list.html
            //
            // Layout (inside the white card):
            //   Row 1 title   : "Search Orders"  (bold label + bottom divider)
            //   Row 2 inputs  : [Order No.] [Customer] [Status ▾] [Date From ☐]
            //   Row 3 buttons : [Search (primary)]  [Refresh (outline)]
            // ═══════════════════════════════════════════════════════════════════════
            Panel pnlSearchOuter = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 136,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding   = new Padding(20, 14, 20, 0)
            };

            // White card inside outer padding panel
            Panel pnlSearchCard = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.White,
                Padding   = new Padding(18, 14, 18, 14)
            };
            pnlSearchCard.Paint += PaintCardBorder;

            // ── Section title + divider ─────────────────────────────────────────
            var lblSearchTitle = new Label
            {
                Text      = "Search Orders",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                AutoSize  = true,
                Location  = new Point(18, 14)
            };

            Panel pnlDivider = new Panel
            {
                BackColor = Color.FromArgb(221, 227, 236),
                Height    = 1,
                Left      = 18,
                Top       = 40,
                Anchor    = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            // Width set after card is added (Resize event)
            pnlSearchCard.Resize += (s, e) => pnlDivider.Width = pnlSearchCard.Width - 36;

            // ── Field helper: label + control stacked ──────────────────────────
            // Each field group: label on top, control below, fixed width
            // Positioned manually to match 4-column grid from HTML cols-4

            int fieldTop    = 52;   // y of the label inside card
            int ctrlTop     = 72;   // y of the input inside card
            int fieldH      = 34;   // height of inputs / combos
            int col1X       = 18;
            // column widths scale at runtime via Anchor; we set initial widths
            // but all four fields anchor Left+Right evenly via a sub-panel grid

            // We use a FlowLayoutPanel row so the four fields share space equally
            // and reflow when the form is resized.
            var pnlFields = new TableLayoutPanel
            {
                Left        = 18,
                Top         = 50,
                Height      = 72,
                Anchor      = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                ColumnCount = 4,
                RowCount    = 1,
                BackColor   = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            pnlFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlFields.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // Width anchored to card after add
            pnlSearchCard.Resize += (s, e) => pnlFields.Width = pnlSearchCard.Width - 36;

            // Helper: creates a label+control stacked sub-panel for one column
            Panel MakeFieldPanel(string labelText, Control ctrl)
            {
                var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 0, 12, 0) };
                var lbl = new Label
                {
                    Text      = labelText,
                    Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(98, 112, 135),
                    Dock      = DockStyle.Top,
                    Height    = 22
                };
                ctrl.Dock   = DockStyle.Fill;
                ctrl.Height = fieldH;
                p.Controls.Add(ctrl);
                p.Controls.Add(lbl);
                return p;
            }

            // ── Field 1 : Order No. ─────────────────────────────────────────────
            txtSearchOrderNo = new TextBox
            {
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                PlaceholderText = "ORD-XXXX"
            };
            txtSearchOrderNo.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            // ── Field 2 : Customer ──────────────────────────────────────────────
            txtSearchCustomer = new TextBox
            {
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                PlaceholderText = "Name or ID"
            };
            txtSearchCustomer.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            // ── Field 3 : Status ────────────────────────────────────────────────
            cboStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 12f)
            };
            cboStatus.Items.AddRange(new object[] { "All", "Pending", "Processing", "Shipped", "Delivered", "Cancelled" });
            cboStatus.SelectedIndex = 0;
            cboStatus.SelectedIndexChanged += (s, e) => RefreshGrid();

            // ── Field 4 : Date From (with enable checkbox) ──────────────────────
            // A small panel that holds a checkbox label + DateTimePicker side by side
            var pnlDateField = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 0, 12, 0) };
            var lblDateFrom = new Label
            {
                Text      = "Date From",
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Top,
                Height    = 22
            };
            // Row for checkbox + picker
            var pnlDateRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            chkDateFrom = new CheckBox
            {
                Text      = "",
                Width     = 24,
                Dock      = DockStyle.Left,
                Checked   = false,
                Cursor    = Cursors.Hand
            };
            dtpDateFrom = new DateTimePicker
            {
                Format    = DateTimePickerFormat.Short,
                Value     = DateTime.Today.AddMonths(-1),
                Font      = new Font("Segoe UI", 12f),
                Dock      = DockStyle.Fill,
                Enabled   = false   // disabled until checkbox ticked
            };
            chkDateFrom.CheckedChanged += (s, e) =>
            {
                dtpDateFrom.Enabled = chkDateFrom.Checked;
                RefreshGrid();
            };
            dtpDateFrom.ValueChanged += (s, e) => { if (chkDateFrom.Checked) RefreshGrid(); };

            pnlDateRow.Controls.Add(dtpDateFrom);
            pnlDateRow.Controls.Add(chkDateFrom);
            pnlDateField.Controls.Add(pnlDateRow);
            pnlDateField.Controls.Add(lblDateFrom);

            // Add four field panels into the TableLayoutPanel columns
            pnlFields.Controls.Add(MakeFieldPanel("Order No.",   txtSearchOrderNo),  0, 0);
            pnlFields.Controls.Add(MakeFieldPanel("Customer",    txtSearchCustomer), 1, 0);
            pnlFields.Controls.Add(MakeFieldPanel("Status",      cboStatus),         2, 0);
            pnlFields.Controls.Add(pnlDateField,                                     3, 0);

            // ── Button row ──────────────────────────────────────────────────────
            var pnlBtnRow = new Panel
            {
                Left      = 18,
                Top       = 128,   // below fields
                Height    = 36,
                Anchor    = AnchorStyles.Left | AnchorStyles.Top,
                BackColor = Color.Transparent
            };
            // (Top is calculated: pnlFields.Top + pnlFields.Height + 6)
            pnlSearchCard.Resize += (s, e) => { pnlBtnRow.Top = pnlFields.Top + pnlFields.Height + 6; };

            btnSearch  = MakePrimaryBtn("Search",      new Point(0,  0), 110, 36);
            btnRefresh = MakeOutlineBtn("↻  Refresh",  new Point(118, 0), 120, 36);
            btnSearch.Click  += (s, e) => RefreshGrid();
            btnRefresh.Click += (s, e) => RefreshGrid();

            pnlBtnRow.Controls.Add(btnSearch);
            pnlBtnRow.Controls.Add(btnRefresh);

            // Assemble search card
            pnlSearchCard.Controls.Add(pnlBtnRow);
            pnlSearchCard.Controls.Add(pnlFields);
            pnlSearchCard.Controls.Add(pnlDivider);
            pnlSearchCard.Controls.Add(lblSearchTitle);
            pnlSearchOuter.Controls.Add(pnlSearchCard);

            // ── KPI bar ───────────────────────────────────────────────────────────
            pnlKpi = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 70,
                BackColor = Color.White,
                Padding   = new Padding(20, 12, 20, 0)
            };
            pnlKpi.Paint += PaintBottomBorder;

            // ── Action bar (bottom) ───────────────────────────────────────────────
            pnlActions = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 64,
                BackColor = Color.White,
                Padding   = new Padding(20, 12, 20, 12)
            };
            pnlActions.Paint += PaintTopBorder;

            btnViewDetail  = MakePrimaryBtn("\uD83D\uDD0D  View Details",  new Point(20,  12), 180, 40);
            btnModifyOrder = MakeWarningBtn("✏️  Modify Order",  new Point(210, 12), 180, 40);
            btnViewDetail.Enabled  = false;
            btnModifyOrder.Enabled = false;
            btnViewDetail.Click  += btnViewDetail_Click;
            btnModifyOrder.Click += btnModifyOrder_Click;

            pnlActions.Controls.Add(btnViewDetail);
            pnlActions.Controls.Add(btnModifyOrder);

            // ── Orders DataGridView ───────────────────────────────────────────────
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
            dgvOrders.EnableHeadersVisualStyles = false;
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrderID",  HeaderText = "ORDER NO.",     FillWeight = 14 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomer", HeaderText = "CUSTOMER",      FillWeight = 22 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSales",    HeaderText = "SALES STAFF",   FillWeight = 16 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIssued",   HeaderText = "ISSUED DATE",   FillWeight = 13 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDelivery", HeaderText = "DELIVERY DATE", FillWeight = 13 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal",    HeaderText = "GRAND TOTAL",   FillWeight = 13 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",   HeaderText = "STATUS",        FillWeight = 10 });
            dgvOrders.SelectionChanged   += dgvOrders_SelectionChanged;
            dgvOrders.CellFormatting     += dgvOrders_CellFormatting;
            dgvOrders.CellDoubleClick    += dgvOrders_CellDoubleClick;

            // Grid wrapper card
            Panel pnlGridCard = new Panel
            {
                Dock      = DockStyle.Fill,
                Padding   = new Padding(20, 12, 20, 0),
                BackColor = Color.FromArgb(240, 244, 249)
            };
            Panel pnlCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(0) };
            pnlCard.Paint += PaintCardBorder;
            pnlCard.Controls.Add(dgvOrders);
            pnlGridCard.Controls.Add(pnlCard);

            // ── Assemble root ─────────────────────────────────────────────────────
            pnlMain.Controls.Add(pnlGridCard);     // Fill
            pnlMain.Controls.Add(pnlActions);      // Bottom
            pnlMain.Controls.Add(pnlKpi);          // Top
            pnlMain.Controls.Add(pnlSearchOuter);  // Top  (replaces old pnlToolbar)
            pnlMain.Controls.Add(_shell);           // Top  (AppShell — topmost)

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        // ── Button factories ─────────────────────────────────────────────────
        private Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(47, 111, 237),
                FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(26, 77, 192);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(21, 60, 155);
            return b;
        }
        private Button MakeWarningBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(245, 158, 11),
                FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(217, 119, 6);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(180, 90, 0);
            return b;
        }
        private Button MakeOutlineBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize  = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            return b;
        }

        // ── Border painters ────────────────────────────────────────────────
        private static void PaintBottomBorder(object s, PaintEventArgs e)
        { var p = (Panel)s; using var pen = new Pen(Color.FromArgb(221, 227, 236), 1); e.Graphics.DrawLine(pen, 0, p.Height-1, p.Width, p.Height-1); }
        private static void PaintTopBorder(object s, PaintEventArgs e)
        { var p = (Panel)s; using var pen = new Pen(Color.FromArgb(221, 227, 236), 1); e.Graphics.DrawLine(pen, 0, 0, p.Width, 0); }
        private static void PaintCardBorder(object s, PaintEventArgs e)
        { var p = (Panel)s; using var pen = new Pen(Color.FromArgb(221, 227, 236), 1); e.Graphics.DrawRectangle(pen, 0, 0, p.Width-1, p.Height-1); }
    }
}
