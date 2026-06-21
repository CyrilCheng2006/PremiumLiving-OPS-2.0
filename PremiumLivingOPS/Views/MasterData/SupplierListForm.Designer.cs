using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.MasterData
{
    partial class SupplierListForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── Controls declared to match ViewOrderForm field-declaration style
        private AppShell     _shell;
        private TextBox      txtSearchID;        // Supplier ID keyword
        private TextBox      txtSearchName;      // Supplier Name keyword
        private TextBox      txtSearchPhone;     // Phone keyword
        private TextBox      txtSearchAddress;   // Address keyword
        private Button       btnSearch;
        private Button       btnRefresh;
        private Panel        pnlKpi;
        private DataGridView dgvSuppliers;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text          = "Premium Living OPS \u2014 Supplier List";
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

            // ────────────────────────────────────────────────────────────────
            // CARD 1 — Search (same structure as ViewOrderForm)
            // ────────────────────────────────────────────────────────────────

            // Four search TextBoxes — mirrors ViewOrderForm’s 4-column field row
            txtSearchID = new TextBox
            {
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                Dock            = DockStyle.Fill,
                PlaceholderText = "SUP-XXXX"
            };
            txtSearchID.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            txtSearchName = new TextBox
            {
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                Dock            = DockStyle.Fill,
                PlaceholderText = "Supplier name"
            };
            txtSearchName.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            txtSearchPhone = new TextBox
            {
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                Dock            = DockStyle.Fill,
                PlaceholderText = "Phone number"
            };
            txtSearchPhone.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            txtSearchAddress = new TextBox
            {
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                Dock            = DockStyle.Fill,
                PlaceholderText = "Address keyword"
            };
            txtSearchAddress.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            // ── MakeCell helper — identical to ViewOrderForm
            TableLayoutPanel MakeCell(string caption, Control ctrl, bool rightPad = true)
            {
                var tlp = new TableLayoutPanel
                {
                    Dock            = DockStyle.Fill,
                    RowCount        = 2,
                    ColumnCount     = 1,
                    BackColor       = Color.Transparent,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding         = rightPad ? new Padding(0, 0, 12, 0) : Padding.Empty
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

            // ── 4-column fields TLP — identical layout token to ViewOrderForm
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
            tblFields.Controls.Add(MakeCell("Supplier ID",   txtSearchID),      0, 0);
            tblFields.Controls.Add(MakeCell("Supplier Name", txtSearchName),    1, 0);
            tblFields.Controls.Add(MakeCell("Phone",         txtSearchPhone),   2, 0);
            tblFields.Controls.Add(MakeCell("Address",       txtSearchAddress, rightPad: false), 3, 0);

            // ── Search / Reset buttons panel — same dimensions as ViewOrderForm
            var pnlBtns = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnSearch  = MakePrimaryBtn("\U0001F50D  Search", new Point(0,   0), 210, 60);
            btnRefresh = MakeOutlineBtn("\u21BA  Reset",      new Point(218, 0), 210, 60);
            btnSearch.Click  += (s, e) => RefreshGrid();
            btnRefresh.Click += (s, e) => ResetFilters();
            pnlBtns.Controls.Add(btnSearch);
            pnlBtns.Controls.Add(btnRefresh);

            // ── Search card TLP: 3 rows (title / fields / buttons) — same as ViewOrderForm
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
            tblCard.RowStyles.Add(new RowStyle(SizeType.Absolute,  60f));  // title row
            tblCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 125f));  // 4-col fields row
            tblCard.RowStyles.Add(new RowStyle(SizeType.Absolute,  65f));  // buttons row

            var pnlTitle = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var lblTitle = new Label
            {
                Text      = "Search Suppliers",
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

            // Height = 300, same as ViewOrderForm
            var pnlSearchOuter = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 300,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding   = new Padding(20, 14, 20, 8)
            };
            pnlSearchOuter.Controls.Add(pnlCard);

            // ════════════════════════════════════════════════════════════════
            // CARD 2 — KPI Bar (Left: pnlKpi Fill, Right: pnlActionBtns)
            //   Exactly mirrors ViewOrderForm’s KPI bar split pattern.
            // ════════════════════════════════════════════════════════════════
            pnlKpi = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(12, 10, 12, 10)
            };

            // Action-button dimensions — same constants as ViewOrderForm
            const int BtnW   = 290;
            const int BtnH   = 60;
            const int BtnGap = 8;
            const int BtnPad = 12;

            // Declare as fields so RefreshKpi() can toggle Enabled
            _btnAddNew  = MakePrimaryBtn("+ Add New",    Point.Empty, BtnW, BtnH);
            _btnModify  = MakeWarningBtn("\u270F\uFE0F  Modify", Point.Empty, BtnW, BtnH);
            _btnModify.BackColor = Color.FromArgb(234, 179, 8);
            _btnModify.ForeColor = Color.FromArgb(92, 60, 0);
            _btnModify.FlatAppearance.MouseOverBackColor = Color.FromArgb(202, 152, 0);
            _btnModify.FlatAppearance.MouseDownBackColor = Color.FromArgb(172, 124, 0);
            _btnAddNew.BackColor = Color.FromArgb(22, 163, 74);
            _btnAddNew.FlatAppearance.MouseOverBackColor = Color.FromArgb(21, 128, 61);
            _btnAddNew.FlatAppearance.MouseDownBackColor = Color.FromArgb(18, 100, 50);

            _btnModify.Enabled = false;
            _btnAddNew.Click  += (s, e) => ShowAddDialog();
            _btnModify.Click  += (s, e) =>
            {
                int idx = dgvSuppliers.CurrentRow?.Index ?? -1;
                if (idx >= 0 && idx < _currentSuppliers.Count) ShowModifyDialog(idx);
            };

            // Panel wide enough for two side-by-side buttons (same formula as ViewOrderForm)
            var pnlActionBtns = new Panel
            {
                Dock      = DockStyle.Right,
                Width     = BtnPad + BtnW + BtnGap + BtnW + BtnPad,   // 12+290+8+290+12 = 612
                BackColor = Color.Transparent
            };

            // Vertically centre at runtime — same pattern as ViewOrderForm
            void CentreActionBtns()
            {
                int top = (pnlActionBtns.Height - BtnH) / 2;
                if (top < 0) top = 0;
                _btnAddNew.Location = new Point(BtnPad, top);
                _btnModify.Location = new Point(BtnPad + BtnW + BtnGap, top);
            }
            pnlActionBtns.Controls.Add(_btnAddNew);
            pnlActionBtns.Controls.Add(_btnModify);
            pnlActionBtns.Resize += (s, e) => CentreActionBtns();

            // Container: pills fill left, action buttons docked right
            var pnlKpiRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlKpiRow.Controls.Add(pnlKpi);         // DockStyle.Fill  — pills added at runtime
            pnlKpiRow.Controls.Add(pnlActionBtns);  // DockStyle.Right — must be added AFTER Fill

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

            // ════════════════════════════════════════════════════════════════
            // CARD 3 — DataGridView (unchanged columns, same DGV style as ViewOrderForm)
            // ════════════════════════════════════════════════════════════════
            dgvSuppliers = new DataGridView
            {
                ReadOnly               = true,
                AllowUserToAddRows     = false,
                AllowUserToDeleteRows  = false,
                RowHeadersVisible      = false,
                SelectionMode          = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect            = false,
                BackgroundColor        = Color.White,
                BorderStyle            = BorderStyle.None,
                GridColor              = Color.FromArgb(221, 227, 236),
                Font                   = new Font("Segoe UI", 13f),
                AutoSizeColumnsMode    = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle        = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate            = { Height = 48 },
                Dock                   = DockStyle.Fill,
                ColumnHeadersHeight    = 46,
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
            dgvSuppliers.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSupplierID",     HeaderText = "SUPPLIER ID",   FillWeight = 18 });
            dgvSuppliers.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSupplierName",    HeaderText = "SUPPLIER NAME", FillWeight = 28 });
            dgvSuppliers.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPhoneNumber",     HeaderText = "PHONE",         FillWeight = 18 });
            dgvSuppliers.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSupplierAddress", HeaderText = "ADDRESS",       FillWeight = 36 });
            dgvSuppliers.SelectionChanged += dgvSuppliers_SelectionChanged;
            dgvSuppliers.CellDoubleClick  += dgvSuppliers_CellDoubleClick;

            var pnlGridInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlGridInner.Paint += PaintCardBorder;
            pnlGridInner.Controls.Add(dgvSuppliers);

            var pnlGridOuter = new Panel
            {
                Dock      = DockStyle.Fill,
                Padding   = new Padding(20, 12, 20, 0),
                BackColor = Color.FromArgb(240, 244, 249)
            };
            pnlGridOuter.Controls.Add(pnlGridInner);

            // ── Assemble (same stacking order as ViewOrderForm)
            pnlMain.Controls.Add(pnlGridOuter);    // Fill  — grid card
            pnlMain.Controls.Add(pnlKpiOuter);     // Top   — KPI bar
            pnlMain.Controls.Add(pnlSearchOuter);  // Top   — Search card
            pnlMain.Controls.Add(_shell);          // Top   — AppShell nav chrome

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        // ── Button factories — identical to ViewOrderForm
        private static Button MakePrimaryBtn(string text, Point loc, int w, int h)
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
        private static Button MakeWarningBtn(string text, Point loc, int w, int h)
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
        private static Button MakeOutlineBtn(string text, Point loc, int w, int h)
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

        // ── Border painter — identical to ViewOrderForm
        private static void PaintCardBorder(object s, PaintEventArgs e)
        {
            var p = (Panel)s;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        }
    }
}
