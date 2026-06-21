using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.MasterData
{
    partial class CustomerListForm
    {
        private System.ComponentModel.IContainer components = null;

        private AppShell     _shell;
        private TextBox      txtSearchID;
        private TextBox      txtSearchName;
        private TextBox      txtSearchEmail;
        private TextBox      txtSearchPhone;
        private Button       btnSearch;
        private Button       btnReset;
        private Panel        pnlKpi;
        private DataGridView dgvCustomers;
        private Button       btnAddNew;
        private Button       btnModify;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text          = "Premium Living OPS \u2014 Customer List";
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
            _shell.MenuItemClicked += (ml, si) => OnTopNavMenuItemClicked(ml, si);
            _shell.LogoutClicked   += btnLogout_Click;

            // ── Input controls
            txtSearchID = new TextBox
            {
                Font = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                PlaceholderText = "CUST-XXXX"
            };
            txtSearchID.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            txtSearchName = new TextBox
            {
                Font = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                PlaceholderText = "Customer name"
            };
            txtSearchName.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            txtSearchEmail = new TextBox
            {
                Font = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                PlaceholderText = "Email address"
            };
            txtSearchEmail.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            txtSearchPhone = new TextBox
            {
                Font = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                PlaceholderText = "Phone number"
            };
            txtSearchPhone.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            // ── MakeCell helper  (mirrors ViewOrderForm.MakeCell exactly)
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

            // ── 4-column fields TLP
            var tblFields = new TableLayoutPanel
            {
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
            tblFields.Controls.Add(MakeCell("Customer ID",   txtSearchID,    rightPad: true),  0, 0);
            tblFields.Controls.Add(MakeCell("Customer Name", txtSearchName,  rightPad: true),  1, 0);
            tblFields.Controls.Add(MakeCell("Email",         txtSearchEmail, rightPad: true),  2, 0);
            tblFields.Controls.Add(MakeCell("Phone",         txtSearchPhone, rightPad: false), 3, 0);

            // ── Search / Reset buttons panel
            var pnlBtns = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnSearch = MakePrimaryBtn("\uD83D\uDD0D  Search",  new Point(0,   0), 210, 60);
            btnReset  = MakeOutlineBtn("\u21BA  Reset",         new Point(218, 0), 210, 60);
            btnSearch.Click += (s, e) => RefreshGrid();
            btnReset.Click  += (s, e) => ResetFilters();
            pnlBtns.Controls.Add(btnSearch);
            pnlBtns.Controls.Add(btnReset);

            // ── Search card TLP
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
            var lblTitle = new Label
            {
                Text      = "Search Customers",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var divider = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(221, 227, 236) };
            pnlTitle.Controls.Add(lblTitle);
            pnlTitle.Controls.Add(divider);
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

            // ── KPI bar ───────────────────────────────────────────────────────
            //  Left  : pnlKpi (FlowLayout of pills)  ─ DockStyle.Fill
            //  Right : btnAddNew + btnModify SIDE-BY-SIDE, vertically centred
            pnlKpi = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(12, 10, 12, 10)
            };

            const int BtnW   = 290;
            const int BtnH   = 60;
            const int BtnGap = 8;
            const int BtnPad = 12;

            btnAddNew  = MakePrimaryGreenBtn("\u2795  Add New Customer", Point.Empty, BtnW, BtnH);
            btnModify  = MakeWarningBtn("\u270F\uFE0F  Modify", Point.Empty, BtnW, BtnH);
            btnAddNew.Enabled = true;
            btnModify.Enabled = false;
            btnAddNew.Click += (s, e) => ShowAddDialog();
            btnModify.Click += (s, e) =>
            {
                int idx = dgvCustomers.CurrentRow?.Index ?? -1;
                if (idx >= 0 && idx < _currentCustomers.Count)
                    ShowModifyDialog(idx);
            };

            var pnlActionBtns = new Panel
            {
                Dock      = DockStyle.Right,
                Width     = BtnPad + BtnW + BtnGap + BtnW + BtnPad,
                BackColor = Color.Transparent
            };

            void CentreActionBtns()
            {
                int top = (pnlActionBtns.Height - BtnH) / 2;
                if (top < 0) top = 0;
                btnAddNew.Location = new Point(BtnPad, top);
                btnModify.Location = new Point(BtnPad + BtnW + BtnGap, top);
            }
            pnlActionBtns.Controls.Add(btnAddNew);
            pnlActionBtns.Controls.Add(btnModify);
            pnlActionBtns.Resize += (s, e) => CentreActionBtns();

            var pnlKpiRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlKpiRow.Controls.Add(pnlKpi);        // Fill — pills
            pnlKpiRow.Controls.Add(pnlActionBtns); // Right — buttons (added AFTER Fill)

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

            // ── Grid
            dgvCustomers = new DataGridView
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
            dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomerID",   HeaderText = "CUSTOMER ID",   FillWeight = 20 });
            dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomerName", HeaderText = "CUSTOMER NAME", FillWeight = 35 });
            dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn { Name = "colEmail",        HeaderText = "EMAIL",         FillWeight = 30 });
            dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPhone",        HeaderText = "PHONE",         FillWeight = 15 });
            dgvCustomers.SelectionChanged += (s, e) =>
            {
                if (btnModify != null) btnModify.Enabled = dgvCustomers.CurrentRow != null;
            };
            dgvCustomers.CellDoubleClick += dgvCustomers_CellDoubleClick;

            var pnlGridCard  = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 12, 20, 0), BackColor = Color.FromArgb(240, 244, 249) };
            var pnlGridInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlGridInner.Paint += PaintCardBorder;
            pnlGridInner.Controls.Add(dgvCustomers);
            pnlGridCard.Controls.Add(pnlGridInner);

            // ── Assemble
            pnlMain.Controls.Add(pnlGridCard);    // Fill  — grid
            pnlMain.Controls.Add(pnlKpiOuter);    // Top   — KPI bar + side-by-side action buttons
            pnlMain.Controls.Add(pnlSearchOuter); // Top   — Search card
            pnlMain.Controls.Add(_shell);         // Top   — nav chrome

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        // ── Button factories (identical signature to ViewOrderForm) ───────────
        private Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(47, 111, 237),
                FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(26,  77, 192);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(21,  60, 155);
            return b;
        }

        private Button MakePrimaryGreenBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(5, 150, 105),
                FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(4, 120, 87);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(3,  90, 68);
            return b;
        }

        private Button MakeWarningBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(92, 60, 0), BackColor = Color.FromArgb(234, 179, 8),
                FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(202, 152,  0);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(170, 125,  0);
            return b;
        }

        private Button MakeOutlineBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text = text, Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize  = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            return b;
        }

        // ── Border painter
        private static void PaintCardBorder(object s, PaintEventArgs e)
        {
            var p = (Panel)s;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        }
    }
}
