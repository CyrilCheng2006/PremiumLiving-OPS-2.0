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

        // ── Search card controls
        private TextBox          txtSearchOrderNo;
        private TextBox          txtSearchCustomer;
        private ComboBox         cboStatus;
        private DateTimePicker   dtpDateFrom;
        private CheckBox         chkDateFrom;
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

            // ── Root panel
            Panel pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };

            // AppShell
            _shell = new AppShell();
            _shell.SetPopupContainer(pnlMain);

            // ═══════════════════════════════════════════════════════════════════
            // SEARCH CARD
            //
            // Uses a TableLayoutPanel with 3 rows so nothing overlaps:
            //   Row 0 (auto) : Title label + divider
            //   Row 1 (auto) : 4-column field grid
            //   Row 2 (auto) : Search + Refresh buttons
            //
            // pnlSearchOuter height is set explicitly after all rows are measured.
            // ═══════════════════════════════════════════════════════════════════

            // ── Fields ──────────────────────────────────────────────────────────

            txtSearchOrderNo = new TextBox
            {
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                PlaceholderText = "ORD-XXXX",
                Height          = 32
            };
            txtSearchOrderNo.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            txtSearchCustomer = new TextBox
            {
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                PlaceholderText = "Name or ID",
                Height          = 32
            };
            txtSearchCustomer.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            cboStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 12f)
            };
            cboStatus.Items.AddRange(new object[] { "All", "Pending", "Processing", "Shipped", "Delivered", "Cancelled" });
            cboStatus.SelectedIndex = 0;
            cboStatus.SelectedIndexChanged += (s, e) => RefreshGrid();

            chkDateFrom = new CheckBox
            {
                Text    = "",
                Width   = 24,
                Checked = false,
                Cursor  = Cursors.Hand,
                Dock    = DockStyle.Left
            };
            dtpDateFrom = new DateTimePicker
            {
                Format  = DateTimePickerFormat.Short,
                Value   = DateTime.Today.AddMonths(-1),
                Font    = new Font("Segoe UI", 12f),
                Enabled = false,
                Dock    = DockStyle.Fill
            };
            chkDateFrom.CheckedChanged += (s, e) => { dtpDateFrom.Enabled = chkDateFrom.Checked; RefreshGrid(); };
            dtpDateFrom.ValueChanged   += (s, e) => { if (chkDateFrom.Checked) RefreshGrid(); };

            // ── Helper: label + control stacked in a DockStyle panel ─────────
            Panel MakeFieldPanel(string labelText, Control ctrl)
            {
                var outer = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 0, 12, 0) };
                var lbl   = new Label
                {
                    Text      = labelText,
                    Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(98, 112, 135),
                    Dock      = DockStyle.Top,
                    Height    = 22
                };
                ctrl.Dock = DockStyle.Fill;
                // Controls added bottom-first so DockStyle.Top label sits above Fill control
                outer.Controls.Add(ctrl);
                outer.Controls.Add(lbl);
                return outer;
            }

            // Date From cell: label on top, then [checkbox][picker] row
            var pnlDateOuter = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 0, 0, 0) };
            var lblDateFrom  = new Label
            {
                Text      = "Date From",
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Top,
                Height    = 22
            };
            var pnlDateRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlDateRow.Controls.Add(dtpDateFrom);
            pnlDateRow.Controls.Add(chkDateFrom);
            pnlDateOuter.Controls.Add(pnlDateRow);
            pnlDateOuter.Controls.Add(lblDateFrom);

            // ── 4-column field row ───────────────────────────────────────────
            var tblFields = new TableLayoutPanel
            {
                Dock        = DockStyle.Top,
                Height      = 62,
                ColumnCount = 4,
                RowCount    = 1,
                BackColor   = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblFields.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblFields.Controls.Add(MakeFieldPanel("Order No.",  txtSearchOrderNo),  0, 0);
            tblFields.Controls.Add(MakeFieldPanel("Customer",   txtSearchCustomer), 1, 0);
            tblFields.Controls.Add(MakeFieldPanel("Status",     cboStatus),         2, 0);
            tblFields.Controls.Add(pnlDateOuter,                                    3, 0);

            // ── Button row ───────────────────────────────────────────────────
            var pnlBtnRow = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 44,
                BackColor = Color.Transparent,
                Padding   = new Padding(0, 6, 0, 0)
            };
            btnSearch  = MakePrimaryBtn("Search",     new Point(0,   6), 110, 34);
            btnRefresh = MakeOutlineBtn("↻  Refresh", new Point(118, 6), 120, 34);
            btnSearch.Click  += (s, e) => RefreshGrid();
            btnRefresh.Click += (s, e) => RefreshGrid();
            pnlBtnRow.Controls.Add(btnSearch);
            pnlBtnRow.Controls.Add(btnRefresh);

            // ── Title + divider row ──────────────────────────────────────────
            var pnlTitleRow = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 38,
                BackColor = Color.Transparent
            };
            var lblSearchTitle = new Label
            {
                Text      = "Search Orders",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                AutoSize  = false,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var pnlTitleDivider = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 1,
                BackColor = Color.FromArgb(221, 227, 236)
            };
            pnlTitleRow.Controls.Add(lblSearchTitle);
            pnlTitleRow.Controls.Add(pnlTitleDivider);

            // ── White card: stack rows bottom-first for DockStyle.Top order ──
            //   Added order  =>  render order (top to bottom)
            //   [3] pnlBtnRow  →  bottom row
            //   [2] tblFields  →  middle row
            //   [1] pnlTitleRow→  top row
            //   (DockStyle.Top stacks in reverse-add order)
            Panel pnlSearchCard = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.White,
                Padding   = new Padding(18, 0, 18, 10)
            };
            pnlSearchCard.Paint += PaintCardBorder;
            // Add in reverse order so DockStyle.Top renders title→fields→buttons
            pnlSearchCard.Controls.Add(pnlBtnRow);
            pnlSearchCard.Controls.Add(tblFields);
            pnlSearchCard.Controls.Add(pnlTitleRow);

            // Outer padding panel
            // Height = top-padding(14) + titleRow(38) + fieldsRow(62) + btnRow(44) + bottom-padding(10) = 168
            Panel pnlSearchOuter = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 168,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding   = new Padding(20, 14, 20, 0)
            };
            pnlSearchOuter.Controls.Add(pnlSearchCard);

            // ── KPI bar ──────────────────────────────────────────────────────
            pnlKpi = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 70,
                BackColor = Color.White,
                Padding   = new Padding(20, 12, 20, 0)
            };
            pnlKpi.Paint += PaintBottomBorder;

            // ── Action bar (bottom) ──────────────────────────────────────────
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

            // ── Orders DataGridView ──────────────────────────────────────────
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
            dgvOrders.SelectionChanged += dgvOrders_SelectionChanged;
            dgvOrders.CellFormatting   += dgvOrders_CellFormatting;
            dgvOrders.CellDoubleClick  += dgvOrders_CellDoubleClick;

            Panel pnlGridCard = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 12, 20, 0), BackColor = Color.FromArgb(240, 244, 249) };
            Panel pnlCard     = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(0) };
            pnlCard.Paint += PaintCardBorder;
            pnlCard.Controls.Add(dgvOrders);
            pnlGridCard.Controls.Add(pnlCard);

            // ── Assemble root (Fill first, then Bottom, then Top in order) ───
            pnlMain.Controls.Add(pnlGridCard);    // Fill
            pnlMain.Controls.Add(pnlActions);     // Bottom
            pnlMain.Controls.Add(pnlKpi);         // Top (added 2nd → below AppShell)
            pnlMain.Controls.Add(pnlSearchOuter); // Top (added 1st → below AppShell)
            pnlMain.Controls.Add(_shell);          // Top (AppShell — topmost)

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

        // ── Border painters ──────────────────────────────────────────────────
        private static void PaintBottomBorder(object s, PaintEventArgs e)
        { var p = (Panel)s; using var pen = new Pen(Color.FromArgb(221, 227, 236), 1); e.Graphics.DrawLine(pen, 0, p.Height-1, p.Width, p.Height-1); }
        private static void PaintTopBorder(object s, PaintEventArgs e)
        { var p = (Panel)s; using var pen = new Pen(Color.FromArgb(221, 227, 236), 1); e.Graphics.DrawLine(pen, 0, 0, p.Width, 0); }
        private static void PaintCardBorder(object s, PaintEventArgs e)
        { var p = (Panel)s; using var pen = new Pen(Color.FromArgb(221, 227, 236), 1); e.Graphics.DrawRectangle(pen, 0, 0, p.Width-1, p.Height-1); }
    }
}
