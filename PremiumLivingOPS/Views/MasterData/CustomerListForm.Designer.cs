using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.MasterData
{
    partial class CustomerListForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── AppShell + shared controls ───────────────────────────────────────
        private AppShell     _shell;
        private DataGridView dgvCustomers;

        // ── Search controls (mirrors SupplierListForm field names) ───────────
        private TextBox txtSearchID;    // Customer ID keyword
        private TextBox txtSearchName;  // Customer Name keyword
        private TextBox txtSearchEmail; // Email keyword
        private TextBox txtSearchPhone; // Phone keyword
        private Button  btnSearch;
        private Button  btnRefresh;

        // ── KPI host panel (populated by RefreshKpi) ─────────────────────────
        private Panel pnlKpi;

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

            // ── Root panel
            var pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };

            // ── AppShell
            _shell = new AppShell();
            _shell.SetPopupContainer(pnlMain);
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;

            // ════════════════════════════════════════════════════════════════
            //  CARD 1 ── Search bar  (mirrors SupplierListForm spec)
            //  4 keyword TextBoxes arranged in a 2-column TLP, same row height
            //  and section-title bar as SupplierListForm.Designer.cs
            // ════════════════════════════════════════════════════════════════

            // Section title bar (44px, #F1F5FF, blue label, 1px bottom border)
            var pnlSearchTitle = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 44,
                BackColor = Color.FromArgb(241, 245, 255),
                Padding   = new Padding(20, 0, 16, 0)
            };
            pnlSearchTitle.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawLine(pen, 0, ((Panel)s).Height - 1, ((Panel)s).Width, ((Panel)s).Height - 1);
            };
            pnlSearchTitle.Controls.Add(new Label
            {
                Text      = "\uD83D\uDD0D  Customer Search",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(47, 111, 237),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false
            });

            // Input fields
            txtSearchID = MakeSearchInput("Customer ID");
            txtSearchName  = MakeSearchInput("Customer Name");
            txtSearchEmail = MakeSearchInput("Email Address");
            txtSearchPhone = MakeSearchInput("Phone Number");

            // Enter key on any field triggers search
            foreach (var tb in new[] { txtSearchID, txtSearchName, txtSearchEmail, txtSearchPhone })
                tb.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            // 2-column field grid
            var tblFields = new TableLayoutPanel
            {
                Dock        = DockStyle.Top,
                Height      = 120,
                ColumnCount = 4,
                RowCount    = 2,
                BackColor   = Color.White,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding     = new Padding(16, 10, 16, 4)
            };
            // 4 equal columns
            for (int i = 0; i < 4; i++)
                tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f)); // labels
            tblFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f)); // inputs

            var fieldDefs = new[]
            {
                ("Customer ID",   txtSearchID),
                ("Customer Name", txtSearchName),
                ("Email Address", txtSearchEmail),
                ("Phone Number",  txtSearchPhone),
            };
            for (int i = 0; i < fieldDefs.Length; i++)
            {
                tblFields.Controls.Add(new Label
                {
                    Text      = fieldDefs[i].Item1,
                    Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(70, 85, 110),
                    BackColor = Color.White,
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.BottomLeft,
                    AutoSize  = false,
                    Padding   = new Padding(4, 0, 8, 2)
                }, i, 0);
                tblFields.Controls.Add(fieldDefs[i].Item2, i, 1);
                tblFields.SetColumn(fieldDefs[i].Item2, i);
            }

            // Button row (Search primary blue, Reset outline — exact SupplierListForm sizes)
            btnSearch  = MakePrimaryBtn("\uD83D\uDD0D  Search", Point.Empty, 210, 60);
            btnRefresh = MakeOutlineBtn("\u21BA  Reset",       Point.Empty, 160, 60);
            btnSearch.Click  += (s, e) => RefreshGrid();
            btnRefresh.Click += (s, e) => ResetFilters();

            var pnlBtnRow = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 72,
                BackColor = Color.White,
                Padding   = new Padding(16, 8, 16, 8)
            };
            btnSearch.Location  = new Point(0,   0);
            btnRefresh.Location = new Point(218, 0);
            pnlBtnRow.Controls.Add(btnSearch);
            pnlBtnRow.Controls.Add(btnRefresh);

            // Compose Card 1 (title + fields + buttons, DockStyle.Top)
            var pnlSearchInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlSearchInner.Paint += PaintCardBorder;
            pnlSearchInner.Controls.Add(pnlBtnRow);    // last added = bottom (DockStyle.Top stacks)
            pnlSearchInner.Controls.Add(tblFields);
            pnlSearchInner.Controls.Add(pnlSearchTitle);

            var pnlSearchOuter = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 260,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding   = new Padding(20, 14, 20, 8)
            };
            pnlSearchOuter.Controls.Add(pnlSearchInner);

            // ════════════════════════════════════════════════════════════════
            //  CARD 2 ── KPI Bar  (mirrors SupplierListForm spec)
            //  Rounded pills 340×60, SmoothingMode.AntiAlias RoundedRect,
            //  NumColW=90, Font size 14pt bold / 12pt, Gap=8
            //  Action buttons (Add New green, Modify amber) docked Right,
            //  each 210×60, no border, FlowDirection LeftToRight
            // ════════════════════════════════════════════════════════════════
            pnlKpi = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(12, 10, 12, 10)
            };

            var pnlKpiWhite = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlKpiWhite.Paint += PaintCardBorder;
            pnlKpiWhite.Controls.Add(pnlKpi);

            var pnlKpiOuter = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 90,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding   = new Padding(20, 8, 20, 8)
            };
            pnlKpiOuter.Controls.Add(pnlKpiWhite);

            // ════════════════════════════════════════════════════════════════
            //  CARD 3 ── DataGridView
            // ════════════════════════════════════════════════════════════════
            dgvCustomers = new DataGridView
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
            dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomerID",   HeaderText = "CUSTOMER ID",   FillWeight = 16 });
            dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomerName", HeaderText = "CUSTOMER NAME", FillWeight = 28 });
            dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn { Name = "colEmail",        HeaderText = "EMAIL",         FillWeight = 30 });
            dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPhone",        HeaderText = "PHONE",         FillWeight = 20 });
            dgvCustomers.SelectionChanged += dgvCustomers_SelectionChanged;
            dgvCustomers.CellDoubleClick  += dgvCustomers_CellDoubleClick;

            var pnlGridInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlGridInner.Paint += PaintCardBorder;
            pnlGridInner.Controls.Add(dgvCustomers);

            var pnlGridOuter = new Panel
            {
                Dock      = DockStyle.Fill,
                Padding   = new Padding(20, 12, 20, 0),
                BackColor = Color.FromArgb(240, 244, 249)
            };
            pnlGridOuter.Controls.Add(pnlGridInner);

            // ── Assemble pnlMain (Fill first, Top in reverse order, AppShell last)
            pnlMain.Controls.Add(pnlGridOuter);   // Fill
            pnlMain.Controls.Add(pnlKpiOuter);    // Top
            pnlMain.Controls.Add(pnlSearchOuter); // Top
            pnlMain.Controls.Add(_shell);          // Top (AppShell)

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        // ── Search TextBox factory (mirrors SupplierListForm) ────────────────
        private static TextBox MakeSearchInput(string placeholder)
        {
            var tb = new TextBox
            {
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                Dock            = DockStyle.Fill,
                BackColor       = Color.White,
                ForeColor       = Color.FromArgb(15, 31, 53),
                PlaceholderText = placeholder,
                Margin          = new Padding(4, 0, 8, 0)
            };
            return tb;
        }

        // ── Button factories (identical to SupplierListForm) ─────────────────
        private static Button MakePrimaryBtn(string text, Point loc, int w, int h)
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
            b.FlatAppearance.BorderSize         = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(26, 77, 192);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(21, 60, 155);
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
                Location  = loc, Width = w, Height = h,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize         = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            return b;
        }

        // ── Card border painter ──────────────────────────────────────────────
        private static void PaintCardBorder(object s, PaintEventArgs e)
        {
            var p = (Panel)s;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        }
    }
}
