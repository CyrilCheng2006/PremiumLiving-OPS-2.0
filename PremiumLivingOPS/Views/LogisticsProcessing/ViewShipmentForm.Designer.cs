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
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            // ─ Shared chrome ──────────────────────────────────────────────────────
            topNavBar     = new TopNavBar();
            userInfoLabel = new UserInfoLabel();

            // ─ Layer 1: page background panel ──────────────────────────────────
            pnlPage = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 244, 249),  // PageBg matches CardPanel
                Padding   = new Padding(0)
            };

            // ─ Layer 2: scroll container ───────────────────────────────────────
            pnlScroll = new Panel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                BackColor  = Color.FromArgb(240, 244, 249)
            };

            // ─ Card 1: Filter / Search  (CardPanel.Create — fixed height) ────────
            var (filterOuter, filterInner) = CardPanel.Create(outerHeight: 72);
            filterOuter.Dock = DockStyle.Top;

            lblSearch       = new Label  { Text = "Search:",  AutoSize = true };
            txtSearch       = new TextBox { Size = new Size(200, 26) };
            lblStatusFilter = new Label  { Text = "Status:",  AutoSize = true };
            cmbStatusFilter = new ComboBox
            {
                Size = new Size(140, 26), DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbStatusFilter.Items.AddRange(new object[] { "(All)", "Pending", "In Transit", "Completed" });
            cmbStatusFilter.SelectedIndex = 0;
            lblFromDate = new Label { Text = "From:", AutoSize = true };
            dtpFrom = new DateTimePicker
            {
                Size = new Size(140, 26), Format = DateTimePickerFormat.Short,
                ShowCheckBox = true, Checked = false
            };
            btnSearch = MakeButton("Search", new Size(80, 28), primary: true);
            btnSearch.Click += btnSearch_Click;
            btnReset  = MakeButton("Reset",  new Size(70, 28), primary: false);
            btnReset.Click  += btnReset_Click;
            lblRecordCount = new Label { AutoSize = true, ForeColor = Color.FromArgb(122, 121, 116) };

            var filterFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, AutoSize = false, Padding = new Padding(8, 10, 8, 0)
            };
            filterFlow.Controls.AddRange(new Control[]
            {
                lblSearch, txtSearch, Spacer(10), lblStatusFilter, cmbStatusFilter,
                Spacer(10), lblFromDate, dtpFrom, Spacer(10),
                btnSearch, Spacer(4), btnReset, Spacer(12), lblRecordCount
            });
            filterInner.Controls.Add(filterFlow);

            // ─ Card 2: Shipments grid  (CardPanel.CreateFill) ──────────────────
            var (gridOuter, gridInner) = CardPanel.Create(outerHeight: 340);
            gridOuter.Dock = DockStyle.Top;

            lblGridTitle = new Label
            {
                Text = "Shipments", AutoSize = true, Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Padding = new Padding(8, 6, 0, 4),
                ForeColor = Color.FromArgb(40, 37, 29)
            };
            dgvShipments = MakeGrid();
            dgvShipments.SelectionMode   = DataGridViewSelectionMode.FullRowSelect;
            dgvShipments.SelectionChanged += dgvShipments_SelectionChanged;
            AddCol(dgvShipments, "colShipmentID", "Shipment ID",  100);
            AddCol(dgvShipments, "colOrderID",    "Order ID",       90);
            AddCol(dgvShipments, "colCustomer",   "Customer",      140);
            AddCol(dgvShipments, "colTracking",   "Tracking No.",  120);
            AddCol(dgvShipments, "colShipDate",   "Ship Date",     100);
            AddCol(dgvShipments, "colStatus",     "Status",         90);
            AddCol(dgvShipments, "colType",       "Type",           80);
            AddCol(dgvShipments, "colMethod",     "Method",         90);
            AddCol(dgvShipments, "colAmount",     "Amount",         90);
            gridInner.Controls.Add(dgvShipments);
            gridInner.Controls.Add(lblGridTitle);

            // ─ Card 3: Shipment detail  (CardPanel.Create — auto-height via AutoSize) ─
            var (detailOuter, detailInner) = CardPanel.Create(outerHeight: 380);
            pnlDetailOuter = detailOuter;
            pnlDetailOuter.Dock    = DockStyle.Top;
            pnlDetailOuter.Visible = false;

            lblDetailTitle = new Label
            {
                Text = "Shipment Detail", AutoSize = true, Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Padding = new Padding(8, 6, 0, 4),
                ForeColor = Color.FromArgb(40, 37, 29)
            };

            // Info table
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, ColumnCount = 4, RowCount = 5,
                Padding = new Padding(8, 0, 8, 8)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

            lblDetailShipmentID = new Label(); lblDetailOrderID   = new Label();
            lblDetailCustomer   = new Label(); lblDetailTracking  = new Label();
            lblDetailStatus     = new Label(); lblDetailType      = new Label();
            lblDetailMethod     = new Label(); lblDetailShipDate  = new Label();
            lblDetailAmount     = new Label(); lblDetailAddress   = new Label();

            AddInfoRow(tbl, 0, "Shipment ID:",  lblDetailShipmentID, "Order ID:",    lblDetailOrderID);
            AddInfoRow(tbl, 1, "Customer:",     lblDetailCustomer,   "Tracking No:", lblDetailTracking);
            AddInfoRow(tbl, 2, "Status:",       lblDetailStatus,     "Type:",        lblDetailType);
            AddInfoRow(tbl, 3, "Method:",       lblDetailMethod,     "Ship Date:",   lblDetailShipDate);
            AddInfoRow(tbl, 4, "Total Amount:", lblDetailAmount,     "Address:",     lblDetailAddress);

            // Delivery Note sub-card (Layer 3 inside detail card)
            var (dnOuter, dnInner) = CardPanel.Create(outerHeight: 100);
            pnlDNOuter = dnOuter;
            pnlDNOuter.Dock = DockStyle.Top;

            lblDNTitle = new Label
            {
                Text = "Delivery Note", AutoSize = true, Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Padding = new Padding(8, 4, 0, 2),
                ForeColor = Color.FromArgb(40, 37, 29)
            };
            var tblDN = new TableLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, ColumnCount = 4,
                Padding = new Padding(8, 0, 8, 4)
            };
            tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            lblDNID = new Label(); lblDNShipTo = new Label();
            lblDNDate = new Label(); lblDNOutQty = new Label();
            AddInfoRow(tblDN, 0, "Delivery ID:",   lblDNID,     "Ship To:",         lblDNShipTo);
            AddInfoRow(tblDN, 1, "Delivery Date:", lblDNDate,   "Outstanding Qty:", lblDNOutQty);
            dnInner.Controls.Add(tblDN);
            dnInner.Controls.Add(lblDNTitle);

            // Shipment lines grid
            lblLinesTitle = new Label
            {
                Text = "Shipment Lines", AutoSize = true, Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Padding = new Padding(8, 6, 0, 2),
                ForeColor = Color.FromArgb(40, 37, 29)
            };
            dgvLines = MakeGrid();
            dgvLines.Height = 160;
            dgvLines.Dock   = DockStyle.Top;
            AddCol(dgvLines, "colLID",  "Line ID",        100);
            AddCol(dgvLines, "colLItem","Item ID",          90);
            AddCol(dgvLines, "colLName","Item Name",       200);
            AddCol(dgvLines, "colLQty", "Qty Shipped",      90);
            AddCol(dgvLines, "colLOut", "Qty Outstanding", 110);

            // Stack controls inside detailInner (Dock.Top = bottom-to-top in winforms)
            detailInner.Controls.Add(dgvLines);
            detailInner.Controls.Add(lblLinesTitle);
            detailInner.Controls.Add(pnlDNOuter);
            detailInner.Controls.Add(tbl);
            detailInner.Controls.Add(lblDetailTitle);

            // ─ Assemble scroll area (reverse Dock.Top order) ─────────────────────
            // Add in reverse visual order because Dock.Top stacks from the top
            pnlScroll.Controls.Add(detailOuter);  // bottom
            pnlScroll.Controls.Add(gridOuter);
            pnlScroll.Controls.Add(filterOuter);  // top
            pnlPage.Controls.Add(pnlScroll);

            // ─ Form ────────────────────────────────────────────────────────────
            SuspendLayout();
            Text          = "Logistics Processing – View Shipment";
            MinimumSize   = new Size(1280, 800);
            WindowState   = FormWindowState.Maximized;
            BackColor     = Color.FromArgb(240, 244, 249);
            Font          = new Font("Segoe UI", 9.5f);
            AutoScaleMode = AutoScaleMode.Font;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);

            userInfoLabel.Dock      = DockStyle.Top;
            userInfoLabel.Height    = 48;
            userInfoLabel.BackColor = Color.FromArgb(249, 248, 245);

            topNavBar.Dock   = DockStyle.Top;
            topNavBar.Height = 44;
            topNavBar.MenuItemClicked += TopNavBar_MenuItemClicked;

            Controls.Add(pnlPage);
            Controls.Add(topNavBar);
            Controls.Add(userInfoLabel);
            ResumeLayout(false);
            PerformLayout();
        }
        #endregion

        // ── Helpers ─────────────────────────────────────────────────────────
        private static Button MakeButton(string text, Size size, bool primary)
        {
            var btn = new Button { Text = text, Size = size, FlatStyle = FlatStyle.Flat };
            btn.FlatAppearance.BorderSize = 1;
            if (primary)
            {
                btn.BackColor = Color.FromArgb(1, 105, 111);
                btn.ForeColor = Color.White;
                btn.FlatAppearance.BorderColor = Color.FromArgb(1, 105, 111);
            }
            else
            {
                btn.BackColor = Color.White;
                btn.ForeColor = Color.FromArgb(1, 105, 111);
                btn.FlatAppearance.BorderColor = Color.FromArgb(1, 105, 111);
            }
            return btn;
        }

        private static Panel Spacer(int w) =>
            new Panel { Width = w, Height = 1, BackColor = Color.Transparent };

        private static DataGridView MakeGrid()
        {
            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(221, 227, 236),
                EnableHeadersVisualStyles = false
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(249, 248, 245);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(40, 37, 29);
            dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.DefaultCellStyle.SelectionBackColor     = Color.FromArgb(206, 220, 216);
            dgv.DefaultCellStyle.SelectionForeColor     = Color.FromArgb(40, 37, 29);
            return dgv;
        }

        private static void AddCol(DataGridView dgv, string name, string header, int weight)
            => dgv.Columns.Add(new DataGridViewTextBoxColumn
               { Name = name, HeaderText = header, FillWeight = weight,
                 SortMode = DataGridViewColumnSortMode.Automatic });

        private static void AddInfoRow(
            TableLayoutPanel tbl, int row,
            string lbl1, Label val1, string lbl2, Label val2)
        {
            val1.Text = "--"; val1.AutoSize = true;
            val1.ForeColor = Color.FromArgb(40, 37, 29);
            val1.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            val2.Text = "--"; val2.AutoSize = true;
            val2.ForeColor = Color.FromArgb(40, 37, 29);
            val2.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            tbl.Controls.Add(MakeLabel(lbl1), 0, row); tbl.Controls.Add(val1, 1, row);
            tbl.Controls.Add(MakeLabel(lbl2), 2, row); tbl.Controls.Add(val2, 3, row);
        }

        private static Label MakeLabel(string text) => new Label
        {
            Text = text, AutoSize = true,
            ForeColor = Color.FromArgb(122, 121, 116),
            Font = new Font("Segoe UI", 9f)
        };

        // ── Field declarations ────────────────────────────────────────────
        private TopNavBar     topNavBar;
        private UserInfoLabel userInfoLabel;
        private Panel         pnlPage;
        private Panel         pnlScroll;
        private Panel         pnlDetailOuter;
        private Panel         pnlDNOuter;

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
