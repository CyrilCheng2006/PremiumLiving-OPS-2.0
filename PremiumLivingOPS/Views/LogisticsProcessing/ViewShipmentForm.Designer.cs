using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    partial class ViewShipmentForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── Shared shell (TopNavBar 44 px + UserBar 72 px = 116 px total) ──
        private AppShell _shell;

        // ── Filter bar controls ───────────────────────────────────────────
        private TextBox        txtSearchShipmentNo;
        private TextBox        txtSearchCustomer;
        private ComboBox       cboStatus;
        private DateTimePicker dtpDateFrom;
        private CheckBox       chkDateFrom;
        private Button         btnSearch;
        private Button         btnRefresh;

        // ── KPI bar + action buttons ──────────────────────────────────────
        private Panel  pnlKpi;
        private Button btnViewDetail;
        private Button btnModify;
        private Button btnGenDeliveryNote;
        private Button btnGenReplySlip;
        private Button btnScheduleShipment;   // 290×60, purple

        // ── Main grid ─────────────────────────────────────────────────────
        private DataGridView dgvShipments;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // ── Form settings ─────────────────────────────────────────────
            this.Text          = "Premium Living OPS — View Shipment";
            this.Size          = new Size(1440, 900);
            this.MinimumSize   = new Size(1200, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = Color.FromArgb(240, 244, 249);
            this.WindowState   = FormWindowState.Maximized;
            this.Font          = new Font("Segoe UI", 13f);

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  Root panel
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            var pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };

            // ── AppShell (RULE 2: construct inside SuspendLayout scope) ───
            _shell = new AppShell();
            _shell.SetPopupContainer(pnlMain);

            // RULE 4: subscribe events ONCE here in Designer.cs only
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  Search card
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            txtSearchShipmentNo = new TextBox
            {
                Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill, PlaceholderText = "SHP-XXXX"
            };
            txtSearchShipmentNo.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            txtSearchCustomer = new TextBox
            {
                Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill, PlaceholderText = "Name or Order ID"
            };
            txtSearchCustomer.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            cboStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 12f), Dock = DockStyle.Fill
            };
            cboStatus.Items.AddRange(new object[] { "All", "Pending", "In Transit", "Completed" });
            cboStatus.SelectedIndex = 0;

            chkDateFrom = new CheckBox { Text = "", Width = 24, Checked = false, Cursor = Cursors.Hand };
            dtpDateFrom = new DateTimePicker
            {
                Format  = DateTimePickerFormat.Short,
                Value   = DateTime.Today.AddMonths(-1),
                Font    = new Font("Segoe UI", 12f),
                Enabled = false,
                Dock    = DockStyle.Fill
            };
            chkDateFrom.CheckedChanged += (s, e) => { dtpDateFrom.Enabled = chkDateFrom.Checked; };

            TableLayoutPanel MakeCell(string caption, Control ctrl, bool rightPad = true)
            {
                var tlp = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1,
                    BackColor = Color.Transparent,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding = rightPad ? new Padding(0, 0, 12, 0) : Padding.Empty
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent,  70f));
                var lbl = new Label
                {
                    Text = caption, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(98, 112, 135),
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft,
                    Padding = new Padding(0, 0, 0, 2)
                };
                ctrl.Dock = DockStyle.Fill;
                tlp.Controls.Add(lbl,  0, 0);
                tlp.Controls.Add(ctrl, 0, 1);
                return tlp;
            }

            var cellDate = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 2,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            cellDate.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 33f));
            cellDate.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            cellDate.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            cellDate.RowStyles.Add(new RowStyle(SizeType.Percent,  70f));
            var lblDate = new Label
            {
                Text = "Date From", Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft,
                Padding = new Padding(0, 0, 0, 2)
            };
            chkDateFrom.Dock = DockStyle.Fill;
            dtpDateFrom.Dock = DockStyle.Fill;
            cellDate.SetColumnSpan(lblDate, 2);
            cellDate.Controls.Add(lblDate,     0, 0);
            cellDate.Controls.Add(chkDateFrom, 0, 1);
            cellDate.Controls.Add(dtpDateFrom, 1, 1);

            var tblFields = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            for (int c = 0; c < 4; c++)
                tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblFields.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblFields.Controls.Add(MakeCell("Shipment No.",     txtSearchShipmentNo), 0, 0);
            tblFields.Controls.Add(MakeCell("Customer / Order", txtSearchCustomer),   1, 0);
            tblFields.Controls.Add(MakeCell("Status",           cboStatus),           2, 0);
            tblFields.Controls.Add(cellDate,                                          3, 0);

            var pnlBtns = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnSearch  = MakePrimaryBtn("\U0001F50D  Search", new Point(0,   0), 210, 60);
            btnRefresh = MakeOutlineBtn("\u21BA  Reset",      new Point(218, 0), 210, 60);
            btnSearch.Click  += (s, e) => RefreshGrid();
            btnRefresh.Click += (s, e) => ResetFilters();
            pnlBtns.Controls.Add(btnSearch);
            pnlBtns.Controls.Add(btnRefresh);

            var tblCard = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(18, 14, 18, 14)
            };
            tblCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblCard.RowStyles.Add(new RowStyle(SizeType.Absolute,  60f));
            tblCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 125f));
            tblCard.RowStyles.Add(new RowStyle(SizeType.Absolute,  65f));

            var pnlTitle = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlTitle.Controls.Add(new Label
            {
                Text = "Search Shipments", Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            });
            pnlTitle.Controls.Add(new Panel
            {
                Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(221, 227, 236)
            });
            tblCard.Controls.Add(pnlTitle,  0, 0);
            tblCard.Controls.Add(tblFields, 0, 1);
            tblCard.Controls.Add(pnlBtns,   0, 2);

            var pnlCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlCard.Paint += PaintCardBorder;
            pnlCard.Controls.Add(tblCard);

            var pnlSearchOuter = new Panel
            {
                Dock = DockStyle.Top, Height = 300,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding = new Padding(20, 14, 20, 8)
            };
            pnlSearchOuter.Controls.Add(pnlCard);

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  KPI bar + action buttons
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            pnlKpi = new Panel
            {
                Dock = DockStyle.Left, Width = 700,
                BackColor = Color.Transparent
            };

            btnViewDetail = MakePrimaryBtn("🔍  View Details",    new Point(0,   0), 200, 60);
            btnModify     = MakeOutlineBtn("✏️  Modify",           new Point(208, 0), 170, 60);
            btnGenDeliveryNote = MakeOutlineBtn("📄  Delivery Note",  new Point(386, 0), 200, 60);
            btnGenReplySlip    = MakeOutlineBtn("📋  Reply Slip",     new Point(594, 0), 170, 60);
            btnScheduleShipment = new Button
            {
                Text      = "🚚  Schedule Shipment",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(108, 60, 193),
                FlatStyle = FlatStyle.Flat,
                Location  = new Point(772, 0),
                Width     = 290,
                Height    = 60,
                Cursor    = Cursors.Hand
            };
            btnScheduleShipment.FlatAppearance.BorderSize         = 0;
            btnScheduleShipment.FlatAppearance.MouseOverBackColor = Color.FromArgb(88, 44, 163);

            btnViewDetail.Click       += (s, e) => ShowViewDetail();
            btnModify.Click           += (s, e) => ShowModifyShipment();
            btnGenDeliveryNote.Click  += (s, e) => ShowGenDeliveryNote();
            btnGenReplySlip.Click     += (s, e) => ShowGenReplySlip();
            btnScheduleShipment.Click += (s, e) => ShowScheduleShipment();

            var pnlActionBtns = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            pnlActionBtns.Controls.Add(btnViewDetail);
            pnlActionBtns.Controls.Add(btnModify);
            pnlActionBtns.Controls.Add(btnGenDeliveryNote);
            pnlActionBtns.Controls.Add(btnGenReplySlip);
            pnlActionBtns.Controls.Add(btnScheduleShipment);

            var pnlKpiRow = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            pnlKpiRow.Controls.Add(pnlKpi);
            pnlKpiRow.Controls.Add(pnlActionBtns);

            var pnlKpiInner = new Panel
            {
                Dock = DockStyle.Fill, BackColor = Color.White,
                Padding = new Padding(18, 10, 18, 10)
            };
            pnlKpiInner.Paint += PaintCardBorder;
            pnlKpiInner.Controls.Add(pnlKpiRow);

            var pnlKpiOuter = new Panel
            {
                Dock = DockStyle.Top, Height = 100,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding = new Padding(20, 0, 20, 8)
            };
            pnlKpiOuter.Controls.Add(pnlKpiInner);

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  Main grid card
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            dgvShipments = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible   = false,
                AllowUserToAddRows  = false,
                ReadOnly            = true,
                SelectionMode       = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor     = Color.White,
                BorderStyle         = BorderStyle.None,
                Font                = new Font("Segoe UI", 11f),
                ColumnHeadersHeight = 44,
                RowTemplate         = { Height = 48 }
            };
            dgvShipments.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 11f, FontStyle.Bold);
            dgvShipments.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 244, 249);
            dgvShipments.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(60, 80, 110);
            dgvShipments.DefaultCellStyle.SelectionBackColor     = Color.FromArgb(210, 228, 255);
            dgvShipments.DefaultCellStyle.SelectionForeColor     = Color.FromArgb(15, 31, 53);
            dgvShipments.EnableHeadersVisualStyles               = false;
            dgvShipments.Columns.Add(new DataGridViewTextBoxColumn { Name = "ShipmentID",   HeaderText = "Shipment ID",   FillWeight = 14 });
            dgvShipments.Columns.Add(new DataGridViewTextBoxColumn { Name = "OrderID",       HeaderText = "Order ID",      FillWeight = 12 });
            dgvShipments.Columns.Add(new DataGridViewTextBoxColumn { Name = "CustomerName",  HeaderText = "Customer",      FillWeight = 22 });
            dgvShipments.Columns.Add(new DataGridViewTextBoxColumn { Name = "ShipDate",      HeaderText = "Ship Date",     FillWeight = 15 });
            dgvShipments.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status",        HeaderText = "Status",        FillWeight = 15 });
            dgvShipments.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalAmount",   HeaderText = "Total Amount",  FillWeight = 15 });
            dgvShipments.SelectionChanged += (s, e) => UpdateActionButtons();
            dgvShipments.CellDoubleClick  += (s, e) => ShowViewDetail();

            var pnlGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 12, 20, 0),
                BackColor = Color.FromArgb(240, 244, 249)
            };
            var pnlGridInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlGridInner.Paint += PaintCardBorder;
            pnlGridInner.Controls.Add(dgvShipments);
            pnlGridCard.Controls.Add(pnlGridInner);

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  Assemble (RULE 5: Fill first, Top second)
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            pnlMain.Controls.Add(pnlGridCard);
            pnlMain.Controls.Add(pnlKpiOuter);
            pnlMain.Controls.Add(pnlSearchOuter);

            // FIX: _shell must be added directly to the Form (not inside pnlMain)
            // so that its mega-popup panel — which TopNavBar adds to FindForm().Controls —
            // is a sibling of pnlMain and can be brought to front via BringToFront().
            // When _shell was inside pnlMain, the popup was added to the Form but
            // pnlMain (DockStyle.Fill, added after _shell) sat on top in z-order,
            // swallowing all click events on the dropdown rows.
            this.Controls.Add(pnlMain);   // Fill panel added first → lower z-order
            this.Controls.Add(_shell);    // AppShell added second → higher z-order,
                                          // popup BringToFront() now works correctly

            this.ResumeLayout(false);
            this.PerformLayout();

            // RULE 3: re-enforce AppShell height after PerformLayout
            _shell.Height      = AppShell.TotalHeight;
            _shell.MinimumSize = new System.Drawing.Size(0, AppShell.TotalHeight);
        }

        // ── Button factories ──────────────────────────────────────────────
        private Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(15, 99, 177),
                FlatStyle = FlatStyle.Flat,
                Location  = loc,
                Width     = w,
                Height    = h,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize         = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(10, 80, 150);
            return b;
        }

        private Button MakeOutlineBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize         = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            return b;
        }

        // NOTE: PaintCardBorder / PaintTopBorderStatic / PaintBottomBorderStatic
        //       are defined in ViewShipmentForm.cs — do NOT redefine here.
    }
}
