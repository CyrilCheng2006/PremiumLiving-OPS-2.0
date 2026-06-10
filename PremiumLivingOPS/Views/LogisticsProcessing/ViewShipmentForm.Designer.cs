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
            //  Root panel (identical to ViewOrderForm)
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            var pnlMain = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 244, 249)
            };

            // ── AppShell (TopNavBar 44 px + UserBar 72 px) ─────────────────
            _shell = new AppShell();
            _shell.SetPopupContainer(pnlMain);   // must be called before adding to controls

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  Search card (CardPanel.Create, outerHeight 300)
            //  Mirrors ViewOrderForm.pnlSearchOuter exactly.
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

            // ── Input controls ─────────────────────────────────────────────
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

            // ── MakeCell helper (identical to ViewOrderForm) ───────────────
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

            // ── Date-From cell (identical to ViewOrderForm) ────────────────
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

            // ── 4-column fields TLP ────────────────────────────────────────
            var tblFields = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblFields.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblFields.Controls.Add(MakeCell("Shipment No.",      txtSearchShipmentNo), 0, 0);
            tblFields.Controls.Add(MakeCell("Customer / Order",  txtSearchCustomer),   1, 0);
            tblFields.Controls.Add(MakeCell("Status",            cboStatus),           2, 0);
            tblFields.Controls.Add(cellDate,                                           3, 0);

            // ── Search / Reset buttons (identical to ViewOrderForm) ────────
            var pnlBtns = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnSearch  = MakePrimaryBtn("\U0001F50D  Search", new Point(0,   0), 210, 60);
            btnRefresh = MakeOutlineBtn("\u21BA  Reset",      new Point(218, 0), 210, 60);
            btnSearch.Click  += (s, e) => RefreshGrid();
            btnRefresh.Click += (s, e) => ResetFilters();
            pnlBtns.Controls.Add(btnSearch);
            pnlBtns.Controls.Add(btnRefresh);

            // ── Search card TLP (identical to ViewOrderForm) ───────────────
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
            var lblTitle = new Label
            {
                Text = "Search Shipments", Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            };
            var divider = new Panel
            {
                Dock = DockStyle.Bottom, Height = 1,
                BackColor = Color.FromArgb(221, 227, 236)
            };
            pnlTitle.Controls.Add(lblTitle);
            pnlTitle.Controls.Add(divider);
            tblCard.Controls.Add(pnlTitle,  0, 0);
            tblCard.Controls.Add(tblFields, 0, 1);
            tblCard.Controls.Add(pnlBtns,   0, 2);

            // ── Wrap in CardPanel (outer=grey page, inner=white card) ──────
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
            //  Left  : pnlKpi  (DockStyle.Fill  — pill FlowLayout)
            //  Right : 4 action buttons side-by-side
            //    [View Details] [Modify] [Generate Delivery Note] [Generate Reply Slip]
            //  Width = pad + btn*4 + gap*3 + pad
            //        = 12 + 290*4 + 8*3 + 12 = 1208
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            pnlKpi = new Panel
            {
                Dock = DockStyle.Fill, BackColor = Color.Transparent,
                Padding = new Padding(12, 10, 12, 10)
            };

            const int BtnW   = 290;
            const int BtnH   = 60;
            const int BtnGap = 8;
            const int BtnPad = 12;

            btnViewDetail      = MakePrimaryBtn("\U0001F50D  View Details",             Point.Empty, BtnW, BtnH);
            btnModify          = MakeWarningBtn("\u270F  Modify",                       Point.Empty, BtnW, BtnH);
            btnGenDeliveryNote = MakeSuccessBtn("\U0001F4C4  Delivery Note",   Point.Empty, BtnW, BtnH);
            btnGenReplySlip    = MakeSuccessBtn("\U0001F9FE  Reply Slip",      Point.Empty, BtnW, BtnH);

            btnViewDetail.Enabled      = false;
            btnModify.Enabled          = false;
            btnGenDeliveryNote.Enabled = false;
            btnGenReplySlip.Enabled    = false;

            btnViewDetail.Click      += btnViewDetail_Click;
            btnModify.Click          += btnModify_Click;
            btnGenDeliveryNote.Click += btnGenDeliveryNote_Click;
            btnGenReplySlip.Click    += btnGenReplySlip_Click;

            // Width = 12 + 290 + 8 + 290 + 8 + 290 + 8 + 290 + 12 = 1208
            var pnlActionBtns = new Panel
            {
                Dock      = DockStyle.Right,
                Width     = BtnPad + BtnW + BtnGap + BtnW + BtnGap + BtnW + BtnGap + BtnW + BtnPad,
                BackColor = Color.Transparent
            };

            void CentreActionBtns()
            {
                int top = (pnlActionBtns.Height - BtnH) / 2;
                if (top < 0) top = 0;
                btnViewDetail.Location      = new Point(BtnPad, top);
                btnModify.Location          = new Point(BtnPad + BtnW + BtnGap, top);
                btnGenDeliveryNote.Location = new Point(BtnPad + BtnW + BtnGap + BtnW + BtnGap, top);
                btnGenReplySlip.Location    = new Point(BtnPad + BtnW + BtnGap + BtnW + BtnGap + BtnW + BtnGap, top);
            }
            pnlActionBtns.Controls.Add(btnViewDetail);
            pnlActionBtns.Controls.Add(btnModify);
            pnlActionBtns.Controls.Add(btnGenDeliveryNote);
            pnlActionBtns.Controls.Add(btnGenReplySlip);
            pnlActionBtns.Resize += (s, e) => CentreActionBtns();

            var pnlKpiRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlKpiRow.Controls.Add(pnlKpi);         // DockStyle.Fill  — pills (must be added first)
            pnlKpiRow.Controls.Add(pnlActionBtns);  // DockStyle.Right — buttons (added after Fill)

            var pnlKpiInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlKpiInner.Paint += PaintCardBorder;
            pnlKpiInner.Controls.Add(pnlKpiRow);

            var pnlKpiOuter = new Panel
            {
                Dock = DockStyle.Top, Height = 90,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding = new Padding(20, 8, 20, 8)
            };
            pnlKpiOuter.Controls.Add(pnlKpiInner);

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            //  Shipment Grid card (CardPanel.CreateFill pattern)
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            dgvShipments = new DataGridView
            {
                ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(221, 227, 236),
                Font = new Font("Segoe UI", 13f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate = { Height = 48 },
                Dock = DockStyle.Fill,
                ColumnHeadersHeight = 46,
                EnableHeadersVisualStyles = false,
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
            dgvShipments.Columns.Add(new DataGridViewTextBoxColumn { Name = "colShipmentID",  HeaderText = "SHIPMENT NO.",   FillWeight = 14 });
            dgvShipments.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrderID",     HeaderText = "ORDER NO.",      FillWeight = 13 });
            dgvShipments.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomer",    HeaderText = "CUSTOMER",       FillWeight = 20 });
            dgvShipments.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTracking",    HeaderText = "TRACKING NO.",   FillWeight = 12 });
            dgvShipments.Columns.Add(new DataGridViewTextBoxColumn { Name = "colShipDate",    HeaderText = "SHIP DATE",      FillWeight = 11 });
            dgvShipments.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",      HeaderText = "STATUS",         FillWeight = 10 });
            dgvShipments.Columns.Add(new DataGridViewTextBoxColumn { Name = "colType",        HeaderText = "TYPE",           FillWeight =  9 });
            dgvShipments.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMethod",      HeaderText = "METHOD",         FillWeight = 10 });
            dgvShipments.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAmount",      HeaderText = "TOTAL AMOUNT",   FillWeight = 12 });
            dgvShipments.SelectionChanged += dgvShipments_SelectionChanged;
            dgvShipments.CellFormatting   += dgvShipments_CellFormatting;
            dgvShipments.CellDoubleClick  += dgvShipments_CellDoubleClick;

            var pnlGridCard  = new Panel
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
            //  Assemble — order: Fill → Top (reverse) → AppShell on top
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            pnlMain.Controls.Add(pnlGridCard);     // DockStyle.Fill  — grid
            pnlMain.Controls.Add(pnlKpiOuter);     // DockStyle.Top   — KPI bar + action buttons
            pnlMain.Controls.Add(pnlSearchOuter);  // DockStyle.Top   — Search card
            pnlMain.Controls.Add(_shell);          // DockStyle.Top   — nav chrome (44+72 = 116 px)

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        // ── Button factories ──────────────────────────────────────────────
        private Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(47, 111, 237),
                FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(26, 77, 192);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(21, 60, 155);
            return b;
        }

        private Button MakeWarningBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(217, 119, 6),
                FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(180, 95, 0);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(146, 64, 14);
            return b;
        }

        private Button MakeSuccessBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(5, 150, 105),
                FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(4, 120, 87);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(3, 100, 70);
            return b;
        }

        private Button MakeOutlineBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text = text, Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.White, FlatStyle = FlatStyle.Flat,
                Location = loc, Width = w, Height = h, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize  = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            return b;
        }

        // ── Border painters (identical to ViewOrderForm) ──────────────────
        private static void PaintCardBorder(object s, PaintEventArgs e)
        {
            var p = (Panel)s;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        }
    }
}
