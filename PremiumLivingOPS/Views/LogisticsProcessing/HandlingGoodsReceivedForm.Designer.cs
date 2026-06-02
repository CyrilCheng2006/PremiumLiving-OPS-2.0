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
            lblReceiptCount  = new Label();

            // receipts grid card (layer 3)
            pnlReceiptsCard  = CardPanel.CreateInner();
            lblReceiptsTitle = new Label();
            dgvReceipts      = new DataGridView();

            // purchase orders card (layer 3)
            pnlPOCard  = CardPanel.CreateInner();
            lblPOTitle = new Label();
            dgvPO      = new DataGridView();

            SuspendLayout();

            // ── Form ──────────────────────────────────────────────────────────
            Text          = "Logistics Processing – Handling Goods Received";
            MinimumSize   = new System.Drawing.Size(1280, 800);
            WindowState   = FormWindowState.Maximized;
            BackColor     = System.Drawing.Color.FromArgb(243, 240, 236);
            Font          = new System.Drawing.Font("Segoe UI", 9.5f);
            AutoScaleMode = AutoScaleMode.Font;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);

            // ── UserInfoLabel ────────────────────────────────────────────────
            userInfoLabel.Dock      = DockStyle.Top;
            userInfoLabel.Height    = 48;
            userInfoLabel.BackColor = System.Drawing.Color.FromArgb(249, 248, 245);
            userInfoLabel.Padding   = new Padding(16, 10, 0, 0);

            // ── TopNavBar ─────────────────────────────────────────────────────
            topNavBar.Dock   = DockStyle.Top;
            topNavBar.Height = 44;
            topNavBar.MenuItemClicked += TopNavBar_MenuItemClicked;

            // ── Outer ────────────────────────────────────────────────────────────
            pnlOuter.Dock    = DockStyle.Fill;
            pnlOuter.Padding = new Padding(16);

            // ── Middle ──────────────────────────────────────────────────────────
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
            cmbStatusFilter.Size     = new System.Drawing.Size(170, 26);
            cmbStatusFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbStatusFilter.Items.AddRange(new object[] { "(All)", "Sent", "Partially Received", "Received", "Completed", "Cancelled" });
            cmbStatusFilter.SelectedIndex = 0;

            SetLabel(lblFromDate, "From:", new System.Drawing.Point(526, 18));
            dtpFrom.Location = new System.Drawing.Point(570, 14);
            dtpFrom.Size     = new System.Drawing.Size(140, 26);
            dtpFrom.Format   = DateTimePickerFormat.Short;
            dtpFrom.ShowCheckBox = true; dtpFrom.Checked = false;

            SetButton(btnSearch, "Search", new System.Drawing.Point(726, 13), new System.Drawing.Size(80, 28));
            btnSearch.Click += btnSearch_Click;
            SetButton(btnReset,  "Reset",  new System.Drawing.Point(814, 13), new System.Drawing.Size(70, 28), secondary: true);
            btnReset.Click += btnReset_Click;

            lblReceiptCount.AutoSize  = true;
            lblReceiptCount.Location  = new System.Drawing.Point(900, 18);
            lblReceiptCount.ForeColor = System.Drawing.Color.FromArgb(122, 121, 116);

            pnlFilterCard.Controls.AddRange(new Control[]
            { lblSearch, txtSearch, lblStatusFilter, cmbStatusFilter,
              lblFromDate, dtpFrom, btnSearch, btnReset, lblReceiptCount });

            // ── Receipts grid card ──────────────────────────────────────────
            pnlReceiptsCard.Dock    = DockStyle.Top;
            pnlReceiptsCard.Height  = 350;
            pnlReceiptsCard.Padding = new Padding(10);
            pnlReceiptsCard.Margin  = new Padding(0, 8, 0, 0);

            lblReceiptsTitle.Text      = "Goods Received Records";
            lblReceiptsTitle.Font      = new System.Drawing.Font("Segoe UI", 10f, FontStyle.Bold);
            lblReceiptsTitle.AutoSize  = true;
            lblReceiptsTitle.Location  = new System.Drawing.Point(10, 8);
            lblReceiptsTitle.ForeColor = System.Drawing.Color.FromArgb(40, 37, 29);

            dgvReceipts.Location = new System.Drawing.Point(10, 30);
            dgvReceipts.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dgvReceipts.Size     = new System.Drawing.Size(1100, 300);
            StyleGrid(dgvReceipts);

            AddCol(dgvReceipts, "colRID",        "Receipt ID",      90);
            AddCol(dgvReceipts, "colRPO",        "PO ID",            90);
            AddCol(dgvReceipts, "colRSupplier",  "Supplier",        130);
            AddCol(dgvReceipts, "colRMatID",     "Material ID",     100);
            AddCol(dgvReceipts, "colRMatName",   "Material Name",   150);
            AddCol(dgvReceipts, "colRQtyRcv",    "Qty Received",     90);
            AddCol(dgvReceipts, "colROutQty",    "Outstanding",      90);
            AddCol(dgvReceipts, "colRDate",      "Receipt Date",    100);
            AddCol(dgvReceipts, "colRWarehouse", "Warehouse",       140);
            AddCol(dgvReceipts, "colRStatus",    "PO Status",       100);
            AddCol(dgvReceipts, "colRUnitPrice", "Unit Price",       80);

            pnlReceiptsCard.Controls.AddRange(new Control[] { lblReceiptsTitle, dgvReceipts });

            // ── Purchase Orders card ───────────────────────────────────────
            pnlPOCard.Dock    = DockStyle.Top;
            pnlPOCard.Height  = 260;
            pnlPOCard.Padding = new Padding(10);
            pnlPOCard.Margin  = new Padding(0, 8, 0, 0);

            lblPOTitle.Text      = "Purchase Orders";
            lblPOTitle.Font      = new System.Drawing.Font("Segoe UI", 10f, FontStyle.Bold);
            lblPOTitle.AutoSize  = true;
            lblPOTitle.Location  = new System.Drawing.Point(10, 8);
            lblPOTitle.ForeColor = System.Drawing.Color.FromArgb(40, 37, 29);

            dgvPO.Location = new System.Drawing.Point(10, 30);
            dgvPO.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dgvPO.Size     = new System.Drawing.Size(1100, 210);
            StyleGrid(dgvPO);

            AddCol(dgvPO, "colPOID",       "PO ID",        100);
            AddCol(dgvPO, "colPOSupplier", "Supplier",     160);
            AddCol(dgvPO, "colPODate",     "Order Date",   100);
            AddCol(dgvPO, "colPOTotal",    "Total Amount", 110);
            AddCol(dgvPO, "colPOStatus",   "Status",       110);

            pnlPOCard.Controls.AddRange(new Control[] { lblPOTitle, dgvPO });

            // ── Assembly ──────────────────────────────────────────────────────
            pnlMiddle.Controls.Add(pnlPOCard);
            pnlMiddle.Controls.Add(pnlReceiptsCard);
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

        private static void SetButton(Button btn, string text, System.Drawing.Point loc,
            System.Drawing.Size size, bool secondary = false)
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

        // ── Control declarations ─────────────────────────────────────────
        private TopNavBar     topNavBar;
        private UserInfoLabel userInfoLabel;

        private Panel pnlOuter;
        private Panel pnlMiddle;
        private Panel pnlFilterCard;
        private Panel pnlReceiptsCard;
        private Panel pnlPOCard;

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
