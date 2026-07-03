using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    partial class ViewShipmentForm
    {
        private System.ComponentModel.IContainer components = null;

        private AppShell        _shell;
        private TextBox         txtSearchShipmentNo;
        private TextBox         txtSearchCustomer;
        private ComboBox        cboStatus;
        private DateTimePicker  dtpDateFrom;
        private CheckBox        chkDateFrom;
        private Button          btnSearch;
        private Button          btnRefresh;
        private Panel           pnlKpi;
        private DataGridView    dgvShipments;
        private Button          btnViewDetail;
        private Button          btnModify;
        private Button          btnGenDeliveryNote;
        private Button          btnGenReplySlip;
        private Button          btnScheduleShipment;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text          = "Premium Living OPS \u2014 View Shipment";
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
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;

            // ────────────────────────────────────────────────────────────
            //  Search card
            // ────────────────────────────────────────────────────────────
            txtSearchShipmentNo = new TextBox
            {
                Font = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                PlaceholderText = "SHP-XXXX"
            };
            txtSearchShipmentNo.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            txtSearchCustomer = new TextBox
            {
                Font = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                PlaceholderText = "Name or Order ID"
            };
            txtSearchCustomer.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            cboStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 12f),
                Dock = DockStyle.Fill
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
                    Dock        = DockStyle.Fill,
                    RowCount    = 2,
                    ColumnCount = 1,
                    BackColor   = Color.Transparent,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding     = rightPad ? new Padding(0, 0, 12, 0) : Padding.Empty
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent,  70f));
                var lbl = new Label
                {
                    Text      = caption,
                    Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(98, 112, 135),
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.BottomLeft,
                    Padding   = new Padding(0, 0, 0, 2)
                };
                ctrl.Dock = DockStyle.Fill;
                tlp.Controls.Add(lbl,  0, 0);
                tlp.Controls.Add(ctrl, 0, 1);
                return tlp;
            }

            var cellDate = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                RowCount    = 2,
                ColumnCount = 2,
                BackColor   = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            cellDate.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 33f));
            cellDate.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            cellDate.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            cellDate.RowStyles.Add(new RowStyle(SizeType.Percent,  70f));
            var lblDate = new Label
            {
                Text      = "Date From",
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft,
                Padding   = new Padding(0, 0, 0, 2)
            };
            chkDateFrom.Dock = DockStyle.Fill;
            dtpDateFrom.Dock = DockStyle.Fill;
            cellDate.SetColumnSpan(lblDate, 2);
            cellDate.Controls.Add(lblDate,     0, 0);
            cellDate.Controls.Add(chkDateFrom, 0, 1);
            cellDate.Controls.Add(dtpDateFrom, 1, 1);

            var tblFields = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 4,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
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
                Dock            = DockStyle.Fill,
                RowCount        = 3,
                ColumnCount     = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(18, 14, 18, 14)
            };
            tblCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblCard.RowStyles.Add(new RowStyle(SizeType.Absolute,  60f));
            tblCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 125f));
            tblCard.RowStyles.Add(new RowStyle(SizeType.Absolute,  65f));

            var pnlTitle = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlTitle.Controls.Add(new Label
            {
                Text      = "Search Shipments",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
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
                Dock      = DockStyle.Top,
                Height    = 300,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding   = new Padding(20, 14, 20, 8)
            };
            pnlSearchOuter.Controls.Add(pnlCard);

            // ────────────────────────────────────────────────────────────
            //  KPI bar
            //  Left : pnlKpi (FlowLayout of pills)     ─ DockStyle.Fill
            //  Right: 5 action buttons side-by-side     ─ DockStyle.Right
            // ────────────────────────────────────────────────────────────
            pnlKpi = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(12, 10, 12, 10)
            };

            // Button dimensions
            const int BtnW   = 200;
            const int BtnH   = 60;
            const int BtnGap = 8;
            const int BtnPad = 12;
            // Total width: BtnPad + 5 buttons + 4 gaps + BtnPad
            int actionPanelW = BtnPad + (BtnW * 5) + (BtnGap * 4) + BtnPad;

            btnViewDetail       = MakePrimaryBtn("\U0001F50D  View Details",  Point.Empty, BtnW, BtnH);
            btnModify           = MakeWarningBtn("\u270F\uFE0F  Modify",        Point.Empty, BtnW, BtnH);
            btnGenDeliveryNote  = MakeGreenBtn  ("\U0001F4C4  Delivery Note",  Point.Empty, BtnW, BtnH);
            btnGenReplySlip     = MakeGreenBtn  ("\U0001F9FE  Reply Slip",     Point.Empty, BtnW, BtnH);
            btnScheduleShipment = MakePurpleBtn ("\U0001F69A  Schedule Ship.", Point.Empty, BtnW, BtnH);

            btnViewDetail.Enabled       = false;
            btnModify.Enabled           = false;
            btnGenDeliveryNote.Enabled  = false;
            btnGenReplySlip.Enabled     = false;
            btnScheduleShipment.Enabled = false;

            btnViewDetail.Click       += (s, e) => btnViewDetail_Click(s, e);
            btnModify.Click           += (s, e) => btnModify_Click(s, e);
            btnGenDeliveryNote.Click  += (s, e) => btnGenDeliveryNote_Click(s, e);
            btnGenReplySlip.Click     += (s, e) => btnGenReplySlip_Click(s, e);
            btnScheduleShipment.Click += (s, e) => btnScheduleShipment_Click(s, e);

            var pnlActionBtns = new Panel
            {
                Dock      = DockStyle.Right,
                Width     = actionPanelW,
                BackColor = Color.Transparent
            };

            void CentreActionBtns()
            {
                int top = (pnlActionBtns.Height - BtnH) / 2;
                if (top < 0) top = 0;
                btnViewDetail.Location       = new Point(BtnPad,                              top);
                btnModify.Location           = new Point(BtnPad + (BtnW + BtnGap),           top);
                btnGenDeliveryNote.Location  = new Point(BtnPad + (BtnW + BtnGap) * 2,       top);
                btnGenReplySlip.Location     = new Point(BtnPad + (BtnW + BtnGap) * 3,       top);
                btnScheduleShipment.Location = new Point(BtnPad + (BtnW + BtnGap) * 4,       top);
            }
            pnlActionBtns.Controls.Add(btnViewDetail);
            pnlActionBtns.Controls.Add(btnModify);
            pnlActionBtns.Controls.Add(btnGenDeliveryNote);
            pnlActionBtns.Controls.Add(btnGenReplySlip);
            pnlActionBtns.Controls.Add(btnScheduleShipment);
            pnlActionBtns.Resize += (s, e) => CentreActionBtns();

            // Pills fill left, action buttons docked right
            var pnlKpiRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlKpiRow.Controls.Add(pnlKpi);        // Fill  — KPI pills
            pnlKpiRow.Controls.Add(pnlActionBtns); // Right — action buttons (add AFTER Fill)

            var pnlKpiInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlKpiInner.Paint += PaintCardBorder;
            pnlKpiInner.Controls.Add(pnlKpiRow);

            var pnlKpiOuter = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 90,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding   = new Padding(20, 8, 20, 8)
            };
            pnlKpiOuter.Controls.Add(pnlKpiInner);

            // ────────────────────────────────────────────────────────────
            //  Main grid
            // ────────────────────────────────────────────────────────────
            dgvShipments = new DataGridView
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

            // Column Names use "col" prefix to match Cells["colXxx"] access in ViewShipmentForm.cs
            dgvShipments.Columns.Add(new DataGridViewTextBoxColumn { Name = "colShipmentID",  HeaderText = "SHIPMENT ID",   FillWeight = 14 });
            dgvShipments.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrderID",      HeaderText = "ORDER ID",      FillWeight = 12 });
            dgvShipments.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomerName", HeaderText = "CUSTOMER",      FillWeight = 22 });
            dgvShipments.Columns.Add(new DataGridViewTextBoxColumn { Name = "colShipDate",     HeaderText = "SHIP DATE",     FillWeight = 15 });
            dgvShipments.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",       HeaderText = "STATUS",        FillWeight = 15 });
            dgvShipments.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotalAmount",  HeaderText = "TOTAL AMOUNT",  FillWeight = 15 });

            dgvShipments.SelectionChanged += dgvShipments_SelectionChanged;
            dgvShipments.CellDoubleClick  += dgvShipments_CellDoubleClick;
            dgvShipments.CellFormatting   += dgvShipments_CellFormatting;

            var pnlGridCard  = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 12, 20, 0), BackColor = Color.FromArgb(240, 244, 249) };
            var pnlGridInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlGridInner.Paint += PaintCardBorder;
            pnlGridInner.Controls.Add(dgvShipments);
            pnlGridCard.Controls.Add(pnlGridInner);

            // ────────────────────────────────────────────────────────────
            //  Assemble (RULE 5: Fill first, Top second)
            // ────────────────────────────────────────────────────────────
            pnlMain.Controls.Add(pnlGridCard);    // Fill  — grid
            pnlMain.Controls.Add(pnlKpiOuter);    // Top   — KPI bar + action buttons
            pnlMain.Controls.Add(pnlSearchOuter); // Top   — search card

            this.Controls.Add(pnlMain);
            this.Controls.Add(_shell);

            this.ResumeLayout(false);
            this.PerformLayout();

            _shell.Height      = AppShell.TotalHeight;
            _shell.MinimumSize = new System.Drawing.Size(0, AppShell.TotalHeight);
        }

        // ── Button factories ────────────────────────────────────────────
        private Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(47, 111, 237),
                FlatStyle = FlatStyle.Flat,
                Location  = loc, Width = w, Height = h, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize         = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(26, 77, 192);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(21, 60, 155);
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
                Location  = loc, Width = w, Height = h, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize         = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(217, 119, 6);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(180, 90, 0);
            return b;
        }

        private Button MakeGreenBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(22, 163, 74),
                FlatStyle = FlatStyle.Flat,
                Location  = loc, Width = w, Height = h, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize         = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(16, 131, 58);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(10, 100, 40);
            return b;
        }

        private Button MakePurpleBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(108, 60, 193),
                FlatStyle = FlatStyle.Flat,
                Location  = loc, Width = w, Height = h, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize         = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(88, 44, 163);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(68, 28, 140);
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
                Location  = loc, Width = w, Height = h, Cursor = Cursors.Hand
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
