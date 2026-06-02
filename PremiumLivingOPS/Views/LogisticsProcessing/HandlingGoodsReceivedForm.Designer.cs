using PremiumLivingOPS.Views.Shared;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    partial class HandlingGoodsReceivedForm
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
            // ─ Shared chrome ─────────────────────────────────────────────────────
            topNavBar     = new TopNavBar();
            userInfoLabel = new UserInfoLabel();

            // ─ Layer 1: page background ──────────────────────────────────────
            pnlPage = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 244, 249)
            };

            // ─ Layer 2: scroll container ────────────────────────────────────
            pnlScroll = new Panel
            {
                Dock = DockStyle.Fill, AutoScroll = true,
                BackColor = Color.FromArgb(240, 244, 249)
            };

            // ─ Card 1: Filter  ─────────────────────────────────────────────────
            var (filterOuter, filterInner) = CardPanel.Create(outerHeight: 72);
            filterOuter.Dock = DockStyle.Top;

            lblSearch       = new Label  { Text = "Search:",  AutoSize = true };
            txtSearch       = new TextBox { Size = new Size(200, 26) };
            lblStatusFilter = new Label  { Text = "Status:",  AutoSize = true };
            cmbStatusFilter = new ComboBox { Size = new Size(170, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatusFilter.Items.AddRange(new object[]
                { "(All)", "Sent", "Partially Received", "Received", "Completed", "Cancelled" });
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
            lblReceiptCount = new Label { AutoSize = true, ForeColor = Color.FromArgb(122, 121, 116) };

            var filterFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, Padding = new Padding(8, 10, 8, 0)
            };
            filterFlow.Controls.AddRange(new Control[]
            {
                lblSearch, txtSearch, Spacer(10), lblStatusFilter, cmbStatusFilter,
                Spacer(10), lblFromDate, dtpFrom, Spacer(10),
                btnSearch, Spacer(4), btnReset, Spacer(12), lblReceiptCount
            });
            filterInner.Controls.Add(filterFlow);

            // ─ Card 2: Goods Received grid  ──────────────────────────────────
            var (rcvOuter, rcvInner) = CardPanel.Create(outerHeight: 370);
            rcvOuter.Dock = DockStyle.Top;

            lblReceiptsTitle = new Label
            {
                Text = "Goods Received Records", AutoSize = true, Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Padding = new Padding(8, 6, 0, 4), ForeColor = Color.FromArgb(40, 37, 29)
            };
            dgvReceipts = MakeGrid();
            AddCol(dgvReceipts, "colRID",        "Receipt ID",      90);
            AddCol(dgvReceipts, "colRPO",        "PO ID",            90);
            AddCol(dgvReceipts, "colRSupplier",  "Supplier",        130);
            AddCol(dgvReceipts, "colRMatID",     "Material ID",     100);
            AddCol(dgvReceipts, "colRMatName",   "Material Name",   150);
            AddCol(dgvReceipts, "colRQtyRcv",    "Qty Received",     90);
            AddCol(dgvReceipts, "colROutQty",    "Outstanding",      90);
            AddCol(dgvReceipts, "colRDate",      "Receipt Date",    100);
            AddCol(dgvReceipts, "colRWarehouse", "Warehouse",       130);
            AddCol(dgvReceipts, "colRStatus",    "PO Status",       100);
            AddCol(dgvReceipts, "colRUnitPrice", "Unit Price",       80);
            rcvInner.Controls.Add(dgvReceipts);
            rcvInner.Controls.Add(lblReceiptsTitle);

            // ─ Card 3: Purchase Orders grid  ─────────────────────────────────
            var (poOuter, poInner) = CardPanel.Create(outerHeight: 280);
            poOuter.Dock = DockStyle.Top;

            lblPOTitle = new Label
            {
                Text = "Purchase Orders", AutoSize = true, Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Padding = new Padding(8, 6, 0, 4), ForeColor = Color.FromArgb(40, 37, 29)
            };
            dgvPO = MakeGrid();
            AddCol(dgvPO, "colPOID",       "PO ID",        100);
            AddCol(dgvPO, "colPOSupplier", "Supplier",     160);
            AddCol(dgvPO, "colPODate",     "Order Date",   100);
            AddCol(dgvPO, "colPOTotal",    "Total Amount", 110);
            AddCol(dgvPO, "colPOStatus",   "Status",       110);
            poInner.Controls.Add(dgvPO);
            poInner.Controls.Add(lblPOTitle);

            // ─ Assemble (reverse Dock.Top order) ────────────────────────────
            pnlScroll.Controls.Add(poOuter);
            pnlScroll.Controls.Add(rcvOuter);
            pnlScroll.Controls.Add(filterOuter);
            pnlPage.Controls.Add(pnlScroll);

            // ─ Form ─────────────────────────────────────────────────────────
            SuspendLayout();
            Text          = "Logistics Processing – Handling Goods Received";
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
                GridColor = Color.FromArgb(221, 227, 236), EnableHeadersVisualStyles = false
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

        // ── Field declarations ──────────────────────────────────────────
        private TopNavBar     topNavBar;
        private UserInfoLabel userInfoLabel;
        private Panel         pnlPage;
        private Panel         pnlScroll;

        private Label          lblSearch;
        private TextBox        txtSearch;
        private Label          lblStatusFilter;
        private ComboBox       cmbStatusFilter;
        private Label          lblFromDate;
        private DateTimePicker dtpFrom;
        private Button         btnSearch;
        private Button         btnReset;
        private Label          lblReceiptCount;

        private Label        lblReceiptsTitle;
        private DataGridView dgvReceipts;

        private Label        lblPOTitle;
        private DataGridView dgvPO;
    }
}
