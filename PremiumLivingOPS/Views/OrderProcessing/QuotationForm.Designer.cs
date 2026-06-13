using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.OrderProcessing
{
    partial class QuotationForm
    {
        private System.ComponentModel.IContainer components = null;

        private AppShell        _shell;
        private TextBox         txtSearchKeyword;
        private ComboBox        cboStatus;
        private Button          btnSearch;
        private Button          btnReset;
        private Panel           pnlKpi;
        private DataGridView    dgvQuotations;
        private Button          btnViewDetail;
        private Button          btnAddFrom;
        private Button          btnUpdateStatus;
        private ComboBox        cboNewStatus;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text          = "Premium Living OPS — Quotation";
            this.Size          = new Size(1440, 900);
            this.MinimumSize   = new Size(1200, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = Palette.BgPage;
            this.WindowState   = FormWindowState.Maximized;
            this.Font          = new Font("Segoe UI", 13f);

            // ── Root
            var pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage };
            _shell = new AppShell();
            _shell.SetPopupContainer(pnlMain);

            // ──────────────────────────────────────────────────────────────────
            // SEARCH BAR CARD  (三層: pnlSearchOuter > pnlSearchInner > tblCard)
            // ──────────────────────────────────────────────────────────────────

            txtSearchKeyword = new TextBox
            {
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                Dock            = DockStyle.Fill,
                PlaceholderText = "Quotation ID or Customer"
            };
            txtSearchKeyword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            cboStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 12f),
                Dock          = DockStyle.Fill
            };
            cboStatus.Items.AddRange(new object[] { "All", "Pending", "Converted", "Rejected" });
            cboStatus.SelectedIndex = 0;

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

            var tblFields = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 2,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblFields.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblFields.Controls.Add(MakeCell("Search", txtSearchKeyword), 0, 0);
            tblFields.Controls.Add(MakeCell("Status", cboStatus, false), 1, 0);

            var pnlBtns = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnSearch = MakePrimaryBtn("\uD83D\uDD0D  Search", new Point(0,   0), 210, 60);
            btnReset  = MakeOutlineBtn("\u21BA  Reset",  new Point(218, 0), 210, 60);
            btnSearch.Click += (s, e) => RefreshGrid();
            btnReset.Click  += (s, e) => ResetFilters();
            pnlBtns.Controls.Add(btnSearch);
            pnlBtns.Controls.Add(btnReset);

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

            var pnlTitleRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var lblTitle    = new Label
            {
                Text      = "Search Quotations",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Palette.TextMain,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var divider = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Palette.BorderColor };
            pnlTitleRow.Controls.Add(lblTitle);
            pnlTitleRow.Controls.Add(divider);
            tblCard.Controls.Add(pnlTitleRow, 0, 0);
            tblCard.Controls.Add(tblFields,   0, 1);
            tblCard.Controls.Add(pnlBtns,     0, 2);

            var (pnlSearchOuter, pnlSearchInner) = CardPanel.Create(outerHeight: 300);
            pnlSearchInner.Controls.Add(tblCard);

            // ──────────────────────────────────────────────────────────────────
            // KPI BAR CARD  (三層: pnlKpiOuter > pnlKpiInner > pnlKpiRow)
            // ──────────────────────────────────────────────────────────────────

            pnlKpi = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(12, 10, 12, 10)
            };

            const int ItemW      = 210;
            const int ItemH      = 60;
            const int ItemGap    = 8;
            const int ModifyW    = 210;   // "Modify" button width
            const int ActionPad  = 12;
            // ActionAreaW = pad + View(210) + gap + Modify(210) + gap + Combo(210) + gap + Update(210) + pad
            const int ActionAreaW = ActionPad + ItemW + ItemGap + ModifyW + ItemGap + ItemW + ItemGap + ItemW + ActionPad; // 890

            btnViewDetail = MakePrimaryBtn("\uD83D\uDD0D  View Detail", Point.Empty, ItemW, ItemH);
            btnViewDetail.Enabled = false;
            btnViewDetail.Click  += btnViewDetail_Click;

            btnAddFrom = MakePrimaryBtn("\u270E  Modify", Point.Empty, ModifyW, ItemH);
            btnAddFrom.BackColor = Color.FromArgb(5, 150, 105);   // green
            btnAddFrom.FlatAppearance.MouseOverBackColor = Color.FromArgb(4, 120, 87);
            btnAddFrom.FlatAppearance.MouseDownBackColor = Color.FromArgb(3, 90, 65);
            btnAddFrom.Enabled = false;
            btnAddFrom.Click  += btnAddFrom_Click;

            cboNewStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 12f),
                Width         = ItemW,
                Height        = ItemH,
                Enabled       = false
            };
            cboNewStatus.Items.AddRange(new object[] { "Pending", "Converted", "Rejected" });
            cboNewStatus.SelectedIndex = 0;

            btnUpdateStatus = MakePrimaryBtn("\u2713  Update Status", Point.Empty, ItemW, ItemH);
            btnUpdateStatus.BackColor = Color.FromArgb(245, 158, 11);
            btnUpdateStatus.FlatAppearance.MouseOverBackColor = Color.FromArgb(217, 119, 6);
            btnUpdateStatus.FlatAppearance.MouseDownBackColor = Color.FromArgb(180,  90,  0);
            btnUpdateStatus.Enabled = false;
            btnUpdateStatus.Click  += btnUpdateStatus_Click;

            var pnlActionArea = new Panel
            {
                Dock      = DockStyle.Right,
                Width     = ActionAreaW,
                BackColor = Color.Transparent
            };

            void CentreActions()
            {
                int top = (pnlActionArea.Height - ItemH) / 2;
                if (top < 0) top = 0;

                // View Detail
                btnViewDetail.Location = new Point(ActionPad, top);
                btnViewDetail.Size     = new Size(ItemW, ItemH);

                // Modify (right of View)
                btnAddFrom.Location = new Point(ActionPad + ItemW + ItemGap, top);
                btnAddFrom.Size     = new Size(ModifyW, ItemH);

                // Status ComboBox
                int comboLeft = ActionPad + ItemW + ItemGap + ModifyW + ItemGap;
                cboNewStatus.Location = new Point(comboLeft, top + (ItemH - cboNewStatus.Height) / 2);
                cboNewStatus.Width    = ItemW;

                // Update Status
                btnUpdateStatus.Location = new Point(comboLeft + ItemW + ItemGap, top);
                btnUpdateStatus.Size     = new Size(ItemW, ItemH);
            }
            pnlActionArea.Controls.Add(btnViewDetail);
            pnlActionArea.Controls.Add(btnAddFrom);
            pnlActionArea.Controls.Add(cboNewStatus);
            pnlActionArea.Controls.Add(btnUpdateStatus);
            pnlActionArea.Resize += (s, e) => CentreActions();

            var pnlKpiRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlKpiRow.Controls.Add(pnlKpi);        // Fill — pills
            pnlKpiRow.Controls.Add(pnlActionArea); // Right — must be added AFTER Fill

            var (pnlKpiOuter, pnlKpiInner) = CardPanel.Create(
                outerHeight: 90,
                outerPadding: new System.Windows.Forms.Padding(20, 8, 20, 8));
            pnlKpiInner.Controls.Add(pnlKpiRow);

            // ──────────────────────────────────────────────────────────────────
            // GRID CARD  (三層: pnlGridOuter > pnlGridInner > dgvQuotations)
            // ──────────────────────────────────────────────────────────────────

            dgvQuotations = new DataGridView
            {
                ReadOnly              = true,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible     = false,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect           = false,
                BackgroundColor       = Color.White,
                BorderStyle           = BorderStyle.None,
                GridColor             = Palette.BorderColor,
                Font                  = new Font("Segoe UI", 13f),
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate           = { Height = 48 },
                Dock                  = DockStyle.Fill,
                ColumnHeadersHeight   = 46,
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
                    ForeColor          = Palette.TextMain,
                    SelectionBackColor = Color.FromArgb(219, 234, 254),
                    SelectionForeColor = Palette.TextMain,
                    Padding            = new Padding(12, 6, 12, 6)
                }
            };
            dgvQuotations.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQuotationID", HeaderText = "QUOTATION ID", FillWeight = 12 });
            dgvQuotations.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomer",    HeaderText = "CUSTOMER",      FillWeight = 25 });
            dgvQuotations.Columns.Add(new DataGridViewTextBoxColumn { Name = "colExpiry",      HeaderText = "EXPIRY DATE",   FillWeight = 10 });
            dgvQuotations.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal",       HeaderText = "TOTAL AMOUNT",  FillWeight = 13 });
            dgvQuotations.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDeposit",     HeaderText = "DEPOSIT REQ.",  FillWeight = 13 });
            dgvQuotations.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLeadTime",    HeaderText = "LEAD TIME",     FillWeight = 15 });
            dgvQuotations.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",      HeaderText = "STATUS",        FillWeight = 12 });
            dgvQuotations.SelectionChanged += dgvQuotations_SelectionChanged;
            dgvQuotations.CellFormatting   += dgvQuotations_CellFormatting;
            dgvQuotations.CellDoubleClick  += dgvQuotations_CellDoubleClick;

            var (pnlGridOuter, pnlGridInner) = CardPanel.CreateFill();
            pnlGridInner.Controls.Add(dgvQuotations);

            // ── Assemble
            pnlMain.Controls.Add(pnlGridOuter);   // Fill  — grid
            pnlMain.Controls.Add(pnlKpiOuter);    // Top   — KPI bar + action controls
            pnlMain.Controls.Add(pnlSearchOuter); // Top   — Search card
            pnlMain.Controls.Add(_shell);         // Top   — nav chrome

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        // ── Button factories
        private Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Palette.Primary,
                FlatStyle = FlatStyle.Flat,
                Location  = loc, Width = w, Height = h,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(26, 77, 192);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(21, 60, 155);
            return b;
        }
        private Button MakeOutlineBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Palette.TextMain,
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location  = loc, Width = w, Height = h,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderColor = Palette.BorderColor;
            b.FlatAppearance.BorderSize  = 1;
            b.FlatAppearance.MouseOverBackColor = Palette.BgPage;
            return b;
        }
    }
}
