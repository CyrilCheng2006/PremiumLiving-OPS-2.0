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
            topNavBar     = new TopNavBar();
            userInfoLabel = new UserInfoLabel();

            pnlOuter  = CardPanel.CreateOuter();
            pnlMiddle = CardPanel.CreateMiddle();

            // filter card (layer 3)
            pnlFilterCard   = CardPanel.CreateInner();
            lblSearch        = new Label();
            txtSearch        = new TextBox();
            lblStatusFilter  = new Label();
            cmbStatusFilter  = new ComboBox();
            lblFromDate      = new Label();
            dtpFrom          = new DateTimePicker();
            btnSearch        = new Button();
            btnReset         = new Button();
            lblRecordCount   = new Label();

            // grid card (layer 3)
            pnlGridCard  = CardPanel.CreateInner();
            lblGridTitle = new Label();
            dgvShipments = new DataGridView();

            // detail card (layer 3)
            pnlDetail           = CardPanel.CreateInner();
            lblDetailTitle      = new Label();
            lblDetailShipmentID = new Label();
            lblDetailOrderID    = new Label();
            lblDetailCustomer   = new Label();
            lblDetailTracking   = new Label();
            lblDetailStatus     = new Label();
            lblDetailType       = new Label();
            lblDetailMethod     = new Label();
            lblDetailShipDate   = new Label();
            lblDetailAmount     = new Label();
            lblDetailAddress    = new Label();

            pnlDeliveryNote = CardPanel.CreateInner();
            lblDNTitle  = new Label();
            lblDNID     = new Label();
            lblDNShipTo = new Label();
            lblDNDate   = new Label();
            lblDNOutQty = new Label();

            lblLinesTitle = new Label();
            dgvLines      = new DataGridView();

            SuspendLayout();

            // ── Form ──────────────────────────────────────────────────────────
            Text          = "Logistics Processing – View Shipment";
            MinimumSize   = new System.Drawing.Size(1280, 800);
            WindowState   = FormWindowState.Maximized;
            BackColor     = System.Drawing.Color.FromArgb(243, 240, 236);
            Font          = new System.Drawing.Font("Segoe UI", 9.5f);
            AutoScaleMode = AutoScaleMode.Font;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);

            // ── UserInfoLabel ──────────────────────────────────────────────────
            userInfoLabel.Dock     = DockStyle.Top;
            userInfoLabel.Height   = 48;
            userInfoLabel.BackColor = System.Drawing.Color.FromArgb(249, 248, 245);
            userInfoLabel.Padding  = new Padding(16, 10, 0, 0);

            // ── TopNavBar ─────────────────────────────────────────────────────
            topNavBar.Dock   = DockStyle.Top;
            topNavBar.Height = 44;
            topNavBar.MenuItemClicked += TopNavBar_MenuItemClicked;

            // ── Outer panel ───────────────────────────────────────────────────
            pnlOuter.Dock    = DockStyle.Fill;
            pnlOuter.Padding = new Padding(16);

            // ── Middle card ───────────────────────────────────────────────────
            pnlMiddle.Dock       = DockStyle.Fill;
            pnlMiddle.Padding    = new Padding(12);
            pnlMiddle.AutoScroll = true;

            // ── Filter card ───────────────────────────────────────────────────
            pnlFilterCard.Dock    = DockStyle.Top;
            pnlFilterCard.Height  = 60;
            pnlFilterCard.Padding = new Padding(10, 8, 10, 8);

            SetLabel(lblSearch, "Search:", new System.Drawing.Point(8, 18));
            txtSearch.Location = new System.Drawing.Point(70, 14); txtSearch.Size = new System.Drawing.Size(200, 26);

            SetLabel(lblStatusFilter, "Status:", new System.Drawing.Point(286, 18));
            cmbStatusFilter.Location = new System.Drawing.Point(340, 14);
            cmbStatusFilter.Size     = new System.Drawing.Size(140, 26);
            cmbStatusFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbStatusFilter.Items.AddRange(new object[] { "(All)", "Pending", "In Transit", "Completed" });
            cmbStatusFilter.SelectedIndex = 0;

            SetLabel(lblFromDate, "From:", new System.Drawing.Point(496, 18));
            dtpFrom.Location = new System.Drawing.Point(540, 14); dtpFrom.Size = new System.Drawing.Size(140, 26);
            dtpFrom.Format   = DateTimePickerFormat.Short;
            dtpFrom.ShowCheckBox = true; dtpFrom.Checked = false;

            SetButton(btnSearch, "Search", new System.Drawing.Point(696, 13), new System.Drawing.Size(80, 28));
            btnSearch.Click += btnSearch_Click;
            SetButton(btnReset, "Reset", new System.Drawing.Point(784, 13), new System.Drawing.Size(70, 28), secondary: true);
            btnReset.Click += btnReset_Click;

            lblRecordCount.AutoSize  = true;
            lblRecordCount.Location  = new System.Drawing.Point(870, 18);
            lblRecordCount.ForeColor = System.Drawing.Color.FromArgb(122, 121, 116);

            pnlFilterCard.Controls.AddRange(new Control[]
            { lblSearch, txtSearch, lblStatusFilter, cmbStatusFilter,
              lblFromDate, dtpFrom, btnSearch, btnReset, lblRecordCount });

            // ── Grid card ─────────────────────────────────────────────────────
            pnlGridCard.Dock    = DockStyle.Top;
            pnlGridCard.Height  = 320;
            pnlGridCard.Padding = new Padding(10);
            pnlGridCard.Margin  = new Padding(0, 8, 0, 0);

            lblGridTitle.Text      = "Shipments";
            lblGridTitle.Font      = new System.Drawing.Font("Segoe UI", 10f, FontStyle.Bold);
            lblGridTitle.AutoSize  = true;
            lblGridTitle.Location  = new System.Drawing.Point(10, 8);
            lblGridTitle.ForeColor = System.Drawing.Color.FromArgb(40, 37, 29);

            dgvShipments.Location = new System.Drawing.Point(10, 30);
            dgvShipments.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dgvShipments.Size     = new System.Drawing.Size(1100, 270);
            StyleGrid(dgvShipments);
            dgvShipments.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvShipments.SelectionChanged += dgvShipments_SelectionChanged;

            AddCol(dgvShipments, "colShipmentID", "Shipment ID",   100);
            AddCol(dgvShipments, "colOrderID",    "Order ID",        90);
            AddCol(dgvShipments, "colCustomer",   "Customer",       140);
            AddCol(dgvShipments, "colTracking",   "Tracking No.",   120);
            AddCol(dgvShipments, "colShipDate",   "Ship Date",      100);
            AddCol(dgvShipments, "colStatus",     "Status",          90);
            AddCol(dgvShipments, "colType",       "Type",            80);
            AddCol(dgvShipments, "colMethod",     "Method",          90);
            AddCol(dgvShipments, "colAmount",     "Amount",          90);

            pnlGridCard.Controls.AddRange(new Control[] { lblGridTitle, dgvShipments });

            // ── Detail card ───────────────────────────────────────────────────
            pnlDetail.Dock     = DockStyle.Top;
            pnlDetail.Padding  = new Padding(12);
            pnlDetail.Margin   = new Padding(0, 8, 0, 0);
            pnlDetail.Visible  = false;
            pnlDetail.AutoSize = true;

            lblDetailTitle.Text     = "Shipment Detail";
            lblDetailTitle.Font     = new System.Drawing.Font("Segoe UI", 10f, FontStyle.Bold);
            lblDetailTitle.AutoSize = true;
            lblDetailTitle.Location = new System.Drawing.Point(0, 0);
            lblDetailTitle.ForeColor = System.Drawing.Color.FromArgb(40, 37, 29);

            var tbl = new TableLayoutPanel { Location = new System.Drawing.Point(0, 26), AutoSize = true, ColumnCount = 4, RowCount = 5 };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

            AddInfoRow(tbl, 0, "Shipment ID:",  ref lblDetailShipmentID, "Order ID:",    ref lblDetailOrderID);
            AddInfoRow(tbl, 1, "Customer:",     ref lblDetailCustomer,   "Tracking No:", ref lblDetailTracking);
            AddInfoRow(tbl, 2, "Status:",       ref lblDetailStatus,     "Type:",        ref lblDetailType);
            AddInfoRow(tbl, 3, "Method:",       ref lblDetailMethod,     "Ship Date:",   ref lblDetailShipDate);
            AddInfoRow(tbl, 4, "Total Amount:", ref lblDetailAmount,     "Address:",     ref lblDetailAddress);

            // Delivery Note sub-card
            pnlDeliveryNote.AutoSize = true;
            pnlDeliveryNote.Padding  = new Padding(8);

            lblDNTitle.Text     = "Delivery Note";
            lblDNTitle.Font     = new System.Drawing.Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblDNTitle.AutoSize = true;
            lblDNTitle.Location = new System.Drawing.Point(0, 0);
            lblDNTitle.ForeColor = System.Drawing.Color.FromArgb(40, 37, 29);

            var tblDN = new TableLayoutPanel { Location = new System.Drawing.Point(0, 22), AutoSize = true, ColumnCount = 4 };
            tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            AddInfoRow(tblDN, 0, "Delivery ID:",   ref lblDNID,     "Ship To:",        ref lblDNShipTo);
            AddInfoRow(tblDN, 1, "Delivery Date:", ref lblDNDate,   "Outstanding Qty:",ref lblDNOutQty);
            pnlDeliveryNote.Controls.AddRange(new Control[] { lblDNTitle, tblDN });

            lblLinesTitle.Text     = "Shipment Lines";
            lblLinesTitle.Font     = new System.Drawing.Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblLinesTitle.AutoSize = true;
            lblLinesTitle.ForeColor = System.Drawing.Color.FromArgb(40, 37, 29);

            dgvLines.Size = new System.Drawing.Size(900, 160);
            StyleGrid(dgvLines);
            AddCol(dgvLines, "colLID",  "Line ID",        100);
            AddCol(dgvLines, "colLItem","Item ID",          90);
            AddCol(dgvLines, "colLName","Item Name",       200);
            AddCol(dgvLines, "colLQty", "Qty Shipped",      90);
            AddCol(dgvLines, "colLOut", "Qty Outstanding", 110);

            // Use a FlowLayoutPanel to stack detail controls vertically
            var flow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                AutoSize      = true,
                WrapContents  = false
            };
            flow.Controls.AddRange(new Control[] { lblDetailTitle, tbl, pnlDeliveryNote, lblLinesTitle, dgvLines });
            pnlDetail.Controls.Add(flow);

            // ── Assembly ──────────────────────────────────────────────────────
            pnlMiddle.Controls.Add(pnlDetail);
            pnlMiddle.Controls.Add(pnlGridCard);
            pnlMiddle.Controls.Add(pnlFilterCard);
            pnlOuter.Controls.Add(pnlMiddle);
            Controls.Add(pnlOuter);
            Controls.Add(topNavBar);
            Controls.Add(userInfoLabel);

            ResumeLayout(false);
            PerformLayout();
        }
        #endregion

        // ── UI helpers ─────────────────────────────────────────────────
        private static void SetLabel(Label lbl, string text, System.Drawing.Point loc)
        {
            lbl.Text = text; lbl.Location = loc; lbl.AutoSize = true;
            lbl.ForeColor = System.Drawing.Color.FromArgb(40, 37, 29);
        }

        private static void SetButton(Button btn, string text, System.Drawing.Point loc, System.Drawing.Size size, bool secondary = false)
        {
            btn.Text = text; btn.Location = loc; btn.Size = size;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            if (secondary)
            {
                btn.BackColor = System.Drawing.Color.White;
                btn.ForeColor = System.Drawing.Color.FromArgb(1, 105, 111);
                btn.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(1, 105, 111);
            }
            else
            {
                btn.BackColor = System.Drawing.Color.FromArgb(1, 105, 111);
                btn.ForeColor = System.Drawing.Color.White;
                btn.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(1, 105, 111);
            }
        }

        private static void StyleGrid(DataGridView dgv)
        {
            dgv.ReadOnly            = true;
            dgv.AllowUserToAddRows  = false;
            dgv.RowHeadersVisible   = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.BackgroundColor     = System.Drawing.Color.White;
            dgv.BorderStyle         = BorderStyle.None;
            dgv.GridColor           = System.Drawing.Color.FromArgb(212, 209, 202);
            dgv.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(249, 248, 245);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(40, 37, 29);
            dgv.ColumnHeadersDefaultCellStyle.Font      = new System.Drawing.Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.DefaultCellStyle.SelectionBackColor     = System.Drawing.Color.FromArgb(206, 220, 216);
            dgv.DefaultCellStyle.SelectionForeColor     = System.Drawing.Color.FromArgb(40, 37, 29);
            dgv.EnableHeadersVisualStyles = false;
        }

        private static void AddCol(DataGridView dgv, string name, string header, int weight)
        {
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            { Name = name, HeaderText = header, FillWeight = weight,
              SortMode = DataGridViewColumnSortMode.Automatic });
        }

        private static void AddInfoRow(
            TableLayoutPanel tbl, int row,
            string lbl1, ref Label val1,
            string lbl2, ref Label val2)
        {
            var l1 = new Label { Text = lbl1, AutoSize = true, ForeColor = System.Drawing.Color.FromArgb(122, 121, 116), Font = new System.Drawing.Font("Segoe UI", 9f) };
            val1   = new Label { Text = "--",  AutoSize = true, ForeColor = System.Drawing.Color.FromArgb(40, 37, 29),   Font = new System.Drawing.Font("Segoe UI", 9f, FontStyle.Bold) };
            var l2 = new Label { Text = lbl2, AutoSize = true, ForeColor = System.Drawing.Color.FromArgb(122, 121, 116), Font = new System.Drawing.Font("Segoe UI", 9f) };
            val2   = new Label { Text = "--",  AutoSize = true, ForeColor = System.Drawing.Color.FromArgb(40, 37, 29),   Font = new System.Drawing.Font("Segoe UI", 9f, FontStyle.Bold) };
            tbl.Controls.Add(l1, 0, row); tbl.Controls.Add(val1, 1, row);
            tbl.Controls.Add(l2, 2, row); tbl.Controls.Add(val2, 3, row);
        }

        // ── Control declarations ─────────────────────────────────────────
        private TopNavBar     topNavBar;
        private UserInfoLabel userInfoLabel;

        private Panel pnlOuter;
        private Panel pnlMiddle;
        private Panel pnlFilterCard;
        private Panel pnlGridCard;
        private Panel pnlDetail;
        private Panel pnlDeliveryNote;

        private Label          lblSearch;
        private TextBox        txtSearch;
        private Label          lblStatusFilter;
        private ComboBox       cmbStatusFilter;
        private Label          lblFromDate;
        private DateTimePicker dtpFrom;
        private Button         btnSearch;
        private Button         btnReset;
        private Label          lblRecordCount;

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
