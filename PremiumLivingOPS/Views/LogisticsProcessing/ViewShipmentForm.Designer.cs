using PremiumLivingOPS.Views.Shared;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    partial class ViewShipmentForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            // ---- Controls declared ----
            topNavBar           = new TopNavBar();
            userBar             = new UserBar();
            lblUserName         = new Label();
            lblDepartment       = new Label();

            // Outer card (layer 1) – page background panel
            pnlOuter = CardPanel.CreateOuter();

            // Middle card (layer 2) – main content
            pnlMiddle = CardPanel.CreateMiddle();

            // ---- Filter bar card (layer 3) ----
            pnlFilterCard = CardPanel.CreateInner();
            lblSearch        = new Label();
            txtSearch        = new TextBox();
            lblStatusFilter  = new Label();
            cmbStatusFilter  = new ComboBox();
            lblFromDate      = new Label();
            dtpFrom          = new DateTimePicker();
            btnSearch        = new Button();
            btnReset         = new Button();
            lblRecordCount   = new Label();

            // ---- Shipments grid card (layer 3) ----
            pnlGridCard = CardPanel.CreateInner();
            lblGridTitle    = new Label();
            dgvShipments    = new DataGridView();

            // ---- Detail card (layer 3) ----
            pnlDetail = CardPanel.CreateInner();
            lblDetailTitle        = new Label();

            // Header info labels
            lblDetailShipmentID   = new Label();
            lblDetailOrderID      = new Label();
            lblDetailCustomer     = new Label();
            lblDetailTracking     = new Label();
            lblDetailStatus       = new Label();
            lblDetailType         = new Label();
            lblDetailMethod       = new Label();
            lblDetailShipDate     = new Label();
            lblDetailAmount       = new Label();
            lblDetailAddress      = new Label();

            // Delivery Note sub-card
            pnlDeliveryNote = CardPanel.CreateInner();
            lblDNTitle  = new Label();
            lblDNID     = new Label();
            lblDNShipTo = new Label();
            lblDNDate   = new Label();
            lblDNOutQty = new Label();

            // Lines grid
            lblLinesTitle = new Label();
            dgvLines      = new DataGridView();

            // ==================================================================
            SuspendLayout();

            // ── Form ──────────────────────────────────────────────────────────
            Text            = "Logistics Processing – View Shipment";
            MinimumSize     = new Size(1280, 800);
            WindowState     = FormWindowState.Maximized;
            BackColor       = Color.FromArgb(243, 240, 236);  // --color-bg
            Font            = new Font("Segoe UI", 9.5f);
            AutoScaleMode   = AutoScaleMode.Font;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);

            // ── UserBar ───────────────────────────────────────────────────────
            userBar.Dock    = DockStyle.Top;
            userBar.Height  = 48;
            userBar.BackColor = Color.FromArgb(1, 105, 111);  // primary teal
            lblUserName.AutoSize = true;
            lblDepartment.AutoSize = true;
            lblUserName.ForeColor  = Color.White;
            lblDepartment.ForeColor = Color.FromArgb(180, 220, 218);
            userBar.Controls.AddRange(new Control[] { lblUserName, lblDepartment });
            lblUserName.Location   = new Point(16, 12);
            lblDepartment.Location = new Point(160, 12);

            // ── TopNavBar ─────────────────────────────────────────────────────
            topNavBar.Dock   = DockStyle.Top;
            topNavBar.Height = 48;
            topNavBar.MenuItemClicked += topNavBar_MenuItemClicked;

            // ── Outer panel ───────────────────────────────────────────────────
            pnlOuter.Dock    = DockStyle.Fill;
            pnlOuter.Padding = new Padding(16);

            // ── Middle card ───────────────────────────────────────────────────
            pnlMiddle.Dock    = DockStyle.Fill;
            pnlMiddle.Padding = new Padding(12);
            pnlMiddle.AutoScroll = true;

            // ── Filter card ───────────────────────────────────────────────────
            pnlFilterCard.Dock    = DockStyle.Top;
            pnlFilterCard.Height  = 60;
            pnlFilterCard.Padding = new Padding(10, 8, 10, 8);

            ConfigureLabel(lblSearch, "Search:", new Point(8, 18));
            txtSearch.Location = new Point(70, 14); txtSearch.Size = new Size(200, 26);

            ConfigureLabel(lblStatusFilter, "Status:", new Point(286, 18));
            cmbStatusFilter.Location = new Point(340, 14); cmbStatusFilter.Size = new Size(140, 26);
            cmbStatusFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbStatusFilter.Items.AddRange(new object[] { "(All)", "Pending", "In Transit", "Completed" });
            cmbStatusFilter.SelectedIndex = 0;

            ConfigureLabel(lblFromDate, "From:", new Point(496, 18));
            dtpFrom.Location = new Point(540, 14); dtpFrom.Size = new Size(140, 26);
            dtpFrom.Format   = DateTimePickerFormat.Short;
            dtpFrom.ShowCheckBox = true; dtpFrom.Checked = false;

            ConfigureButton(btnSearch, "Search", new Point(696, 13), new Size(80, 28));
            btnSearch.Click += btnSearch_Click;
            ConfigureButton(btnReset, "Reset",  new Point(784, 13), new Size(70, 28), secondary: true);
            btnReset.Click += btnReset_Click;

            lblRecordCount.AutoSize  = true;
            lblRecordCount.Location  = new Point(870, 18);
            lblRecordCount.ForeColor = Color.FromArgb(122, 121, 116);

            pnlFilterCard.Controls.AddRange(new Control[]
            { lblSearch, txtSearch, lblStatusFilter, cmbStatusFilter,
              lblFromDate, dtpFrom, btnSearch, btnReset, lblRecordCount });

            // ── Grid card ─────────────────────────────────────────────────────
            pnlGridCard.Dock    = DockStyle.Top;
            pnlGridCard.Height  = 320;
            pnlGridCard.Padding = new Padding(10);
            pnlGridCard.Margin  = new Padding(0, 8, 0, 0);

            ConfigureLabel(lblGridTitle, "Shipments", new Point(10, 8),
                font: new Font("Segoe UI", 10f, FontStyle.Bold));

            dgvShipments.Location = new Point(10, 30);
            dgvShipments.Size     = new Size(pnlGridCard.Width - 20, 270);
            dgvShipments.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            StyleGrid(dgvShipments);
            dgvShipments.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvShipments.SelectionChanged += dgvShipments_SelectionChanged;

            // Columns
            AddColumn(dgvShipments, "colShipmentID",  "Shipment ID",  100);
            AddColumn(dgvShipments, "colOrderID",     "Order ID",      90);
            AddColumn(dgvShipments, "colCustomer",    "Customer",     140);
            AddColumn(dgvShipments, "colTracking",    "Tracking No.",  120);
            AddColumn(dgvShipments, "colShipDate",    "Ship Date",     100);
            AddColumn(dgvShipments, "colStatus",      "Status",         90);
            AddColumn(dgvShipments, "colType",        "Type",           80);
            AddColumn(dgvShipments, "colMethod",      "Method",         90);
            AddColumn(dgvShipments, "colAmount",      "Amount",         90);

            pnlGridCard.Controls.AddRange(new Control[] { lblGridTitle, dgvShipments });

            // ── Detail card ───────────────────────────────────────────────────
            pnlDetail.Dock    = DockStyle.Top;
            pnlDetail.Padding = new Padding(12);
            pnlDetail.Margin  = new Padding(0, 8, 0, 0);
            pnlDetail.Visible = false;
            pnlDetail.AutoSize = true;

            ConfigureLabel(lblDetailTitle, "Shipment Detail", new Point(0, 0),
                font: new Font("Segoe UI", 10f, FontStyle.Bold));

            // Two-column info grid layout using TableLayoutPanel
            var tbl = new TableLayoutPanel();
            tbl.Location    = new Point(0, 26);
            tbl.AutoSize    = true;
            tbl.ColumnCount = 4;
            tbl.RowCount    = 5;
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  25));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  25));

            AddInfoRow(tbl, 0, "Shipment ID:", ref lblDetailShipmentID, "Order ID:",    ref lblDetailOrderID);
            AddInfoRow(tbl, 1, "Customer:",    ref lblDetailCustomer,   "Tracking No:", ref lblDetailTracking);
            AddInfoRow(tbl, 2, "Status:",      ref lblDetailStatus,     "Type:",        ref lblDetailType);
            AddInfoRow(tbl, 3, "Method:",      ref lblDetailMethod,     "Ship Date:",   ref lblDetailShipDate);
            AddInfoRow(tbl, 4, "Total Amount:",ref lblDetailAmount,     "Address:",     ref lblDetailAddress);

            // Delivery Note sub-card
            pnlDeliveryNote.Location = new Point(0, tbl.Bottom + 10);
            pnlDeliveryNote.AutoSize = true;
            pnlDeliveryNote.Padding  = new Padding(8);

            ConfigureLabel(lblDNTitle, "Delivery Note", new Point(0, 0),
                font: new Font("Segoe UI", 9.5f, FontStyle.Bold));

            var tblDN = new TableLayoutPanel();
            tblDN.Location    = new Point(0, 22);
            tblDN.AutoSize    = true;
            tblDN.ColumnCount = 4;
            tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  25));
            tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  25));
            AddInfoRow(tblDN, 0, "Delivery ID:", ref lblDNID, "Ship To:", ref lblDNShipTo);
            AddInfoRow(tblDN, 1, "Delivery Date:", ref lblDNDate, "Outstanding Qty:", ref lblDNOutQty);

            pnlDeliveryNote.Controls.AddRange(new Control[] { lblDNTitle, tblDN });

            // Lines grid
            lblLinesTitle.Text     = "Shipment Lines";
            lblLinesTitle.Font     = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblLinesTitle.AutoSize = true;
            lblLinesTitle.Location = new Point(0, pnlDeliveryNote.Bottom + 10);

            dgvLines.Location = new Point(0, lblLinesTitle.Bottom + 4);
            dgvLines.Size     = new Size(900, 160);
            StyleGrid(dgvLines);
            AddColumn(dgvLines, "colLID",  "Line ID",       100);
            AddColumn(dgvLines, "colLItem","Item ID",        90);
            AddColumn(dgvLines, "colLName","Item Name",     200);
            AddColumn(dgvLines, "colLQty", "Qty Shipped",    90);
            AddColumn(dgvLines, "colLOut", "Qty Outstanding", 110);

            pnlDetail.Controls.AddRange(new Control[]
            { lblDetailTitle, tbl, pnlDeliveryNote, lblLinesTitle, dgvLines });

            // ── Assembly ──────────────────────────────────────────────────────
            pnlMiddle.Controls.Add(pnlDetail);
            pnlMiddle.Controls.Add(pnlGridCard);
            pnlMiddle.Controls.Add(pnlFilterCard);

            pnlOuter.Controls.Add(pnlMiddle);

            Controls.Add(pnlOuter);
            Controls.Add(topNavBar);
            Controls.Add(userBar);

            ResumeLayout(false);
            PerformLayout();
        }
        #endregion

        // ── UI helpers ───────────────────────────────────────────────────
        private static void ConfigureLabel(Label lbl, string text, Point loc, Font font = null)
        {
            lbl.Text      = text;
            lbl.Location  = loc;
            lbl.AutoSize  = true;
            lbl.ForeColor = Color.FromArgb(40, 37, 29);
            if (font != null) lbl.Font = font;
        }

        private static void ConfigureButton(Button btn, string text, Point loc, Size size, bool secondary = false)
        {
            btn.Text      = text;
            btn.Location  = loc;
            btn.Size      = size;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            if (secondary)
            {
                btn.BackColor = Color.White;
                btn.ForeColor = Color.FromArgb(1, 105, 111);
                btn.FlatAppearance.BorderColor = Color.FromArgb(1, 105, 111);
            }
            else
            {
                btn.BackColor = Color.FromArgb(1, 105, 111);
                btn.ForeColor = Color.White;
                btn.FlatAppearance.BorderColor = Color.FromArgb(1, 105, 111);
            }
        }

        private static void StyleGrid(DataGridView dgv)
        {
            dgv.ReadOnly              = true;
            dgv.AllowUserToAddRows    = false;
            dgv.RowHeadersVisible     = false;
            dgv.AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.BackgroundColor       = Color.White;
            dgv.BorderStyle           = BorderStyle.None;
            dgv.GridColor             = Color.FromArgb(212, 209, 202);
            dgv.ColumnHeadersDefaultCellStyle.BackColor  = Color.FromArgb(249, 248, 245);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor  = Color.FromArgb(40, 37, 29);
            dgv.ColumnHeadersDefaultCellStyle.Font       = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.DefaultCellStyle.SelectionBackColor      = Color.FromArgb(206, 220, 216);
            dgv.DefaultCellStyle.SelectionForeColor      = Color.FromArgb(40, 37, 29);
            dgv.EnableHeadersVisualStyles = false;
        }

        private static void AddColumn(DataGridView dgv, string name, string header, int width)
        {
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name            = name,
                HeaderText      = header,
                FillWeight      = width,
                SortMode        = DataGridViewColumnSortMode.Automatic
            });
        }

        private static void AddInfoRow(
            TableLayoutPanel tbl, int row,
            string lbl1, ref Label val1,
            string lbl2, ref Label val2)
        {
            var l1 = new Label { Text = lbl1, AutoSize = true, ForeColor = Color.FromArgb(122, 121, 116), Font = new Font("Segoe UI", 9f) };
            val1 = new Label { Text = "--", AutoSize = true, ForeColor = Color.FromArgb(40, 37, 29), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            var l2 = new Label { Text = lbl2, AutoSize = true, ForeColor = Color.FromArgb(122, 121, 116), Font = new Font("Segoe UI", 9f) };
            val2 = new Label { Text = "--", AutoSize = true, ForeColor = Color.FromArgb(40, 37, 29), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            tbl.Controls.Add(l1,  0, row);
            tbl.Controls.Add(val1,1, row);
            tbl.Controls.Add(l2,  2, row);
            tbl.Controls.Add(val2,3, row);
        }

        // ── Control declarations ───────────────────────────────────────────
        private TopNavBar topNavBar;
        private UserBar   userBar;
        private Label     lblUserName;
        private Label     lblDepartment;

        private Panel pnlOuter;
        private Panel pnlMiddle;
        private Panel pnlFilterCard;
        private Panel pnlGridCard;
        private Panel pnlDetail;
        private Panel pnlDeliveryNote;

        private Label        lblSearch;
        private TextBox      txtSearch;
        private Label        lblStatusFilter;
        private ComboBox     cmbStatusFilter;
        private Label        lblFromDate;
        private DateTimePicker dtpFrom;
        private Button       btnSearch;
        private Button       btnReset;
        private Label        lblRecordCount;

        private Label        lblGridTitle;
        private DataGridView dgvShipments;

        private Label lblDetailTitle;
        private Label lblDetailShipmentID;
        private Label lblDetailOrderID;
        private Label lblDetailCustomer;
        private Label lblDetailTracking;
        private Label lblDetailStatus;
        private Label lblDetailType;
        private Label lblDetailMethod;
        private Label lblDetailShipDate;
        private Label lblDetailAmount;
        private Label lblDetailAddress;

        private Label lblDNTitle;
        private Label lblDNID;
        private Label lblDNShipTo;
        private Label lblDNDate;
        private Label lblDNOutQty;

        private Label        lblLinesTitle;
        private DataGridView dgvLines;
    }
}
