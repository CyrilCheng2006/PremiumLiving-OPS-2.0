// ViewOrderForm.Designer.cs — Search Orders panel redesigned to match order-list.html
namespace PremiumLivingOPS.Views.OrderProcessing
{
    partial class ViewOrderForm
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
            this.pnlShell        = new System.Windows.Forms.Panel();
            this.pnlContent      = new System.Windows.Forms.Panel();
            this.pnlToolbar      = new System.Windows.Forms.Panel();
            this.pnlSearchFields = new System.Windows.Forms.TableLayoutPanel();

            // ── Search field labels & controls ──
            this.lblOrderNo      = new System.Windows.Forms.Label();
            this.txtOrderNo      = new System.Windows.Forms.TextBox();
            this.lblCustomer     = new System.Windows.Forms.Label();
            this.txtCustomer     = new System.Windows.Forms.TextBox();
            this.lblStatus       = new System.Windows.Forms.Label();
            this.cboStatus       = new System.Windows.Forms.ComboBox();
            this.lblDateFrom     = new System.Windows.Forms.Label();
            this.dtpDateFrom     = new System.Windows.Forms.DateTimePicker();
            this.chkDateFrom     = new System.Windows.Forms.CheckBox();
            this.lblDateTo       = new System.Windows.Forms.Label();
            this.dtpDateTo       = new System.Windows.Forms.DateTimePicker();
            this.chkDateTo       = new System.Windows.Forms.CheckBox();

            // ── Action buttons ──
            this.pnlButtons      = new System.Windows.Forms.FlowLayoutPanel();
            this.btnSearch       = new System.Windows.Forms.Button();
            this.btnClear        = new System.Windows.Forms.Button();
            this.btnCreateOrder  = new System.Windows.Forms.Button();

            // ── KPI summary bar ──
            this.pnlKpi          = new System.Windows.Forms.FlowLayoutPanel();

            // ── Grid ──
            this.dgvOrders       = new System.Windows.Forms.DataGridView();

            // ── Result count label ──
            this.lblResultCount  = new System.Windows.Forms.Label();

            this.pnlShell.SuspendLayout();
            this.pnlContent.SuspendLayout();
            this.pnlToolbar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvOrders)).BeginInit();
            this.SuspendLayout();

            // ════════════════════════════════════════════════════════════════
            //  SHELL (AppShell placeholder — sized at runtime)
            // ════════════════════════════════════════════════════════════════
            this.pnlShell.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlShell.Name = "pnlShell";

            // ════════════════════════════════════════════════════════════════
            //  CONTENT  (sits to the right of / below the shell nav)
            // ════════════════════════════════════════════════════════════════
            this.pnlContent.Dock      = System.Windows.Forms.DockStyle.Fill;
            this.pnlContent.BackColor = System.Drawing.Color.FromArgb(240, 244, 249);
            this.pnlContent.Padding   = new System.Windows.Forms.Padding(24, 20, 24, 20);
            this.pnlContent.Name      = "pnlContent";

            // ════════════════════════════════════════════════════════════════
            //  TOOLBAR  (white card, rounded look via border)
            // ════════════════════════════════════════════════════════════════
            this.pnlToolbar.Dock      = System.Windows.Forms.DockStyle.Top;
            this.pnlToolbar.Height    = 180;
            this.pnlToolbar.BackColor = System.Drawing.Color.White;
            this.pnlToolbar.Padding   = new System.Windows.Forms.Padding(20, 14, 20, 10);
            this.pnlToolbar.Name      = "pnlToolbar";

            // ── Search fields (TableLayoutPanel 6-col) ─────────────────────
            this.pnlSearchFields.Dock        = System.Windows.Forms.DockStyle.Top;
            this.pnlSearchFields.Height      = 106;
            this.pnlSearchFields.ColumnCount  = 6;
            this.pnlSearchFields.RowCount     = 2;
            this.pnlSearchFields.BackColor    = System.Drawing.Color.Transparent;
            this.pnlSearchFields.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 18F));
            this.pnlSearchFields.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 18F));
            this.pnlSearchFields.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14F));
            this.pnlSearchFields.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 18F));
            this.pnlSearchFields.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14F));
            this.pnlSearchFields.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 18F));
            this.pnlSearchFields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.pnlSearchFields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.pnlSearchFields.Name         = "pnlSearchFields";
            this.pnlSearchFields.Padding       = new System.Windows.Forms.Padding(0);

            // Labels (row 0)
            StyleLabel(this.lblOrderNo,  "Order No.");
            StyleLabel(this.lblCustomer, "Customer");
            StyleLabel(this.lblStatus,   "Status");
            StyleLabel(this.lblDateFrom, "Date From");
            StyleLabel(this.lblDateTo,   "Date To");

            this.pnlSearchFields.Controls.Add(this.lblOrderNo,  0, 0);
            this.pnlSearchFields.Controls.Add(this.lblCustomer, 1, 0);
            this.pnlSearchFields.Controls.Add(this.lblStatus,   2, 0);
            this.pnlSearchFields.Controls.Add(this.lblDateFrom, 3, 0);
            this.pnlSearchFields.Controls.Add(this.lblDateTo,   5, 0);

            // Inputs (row 1)
            StyleTextBox(this.txtOrderNo);
            this.txtOrderNo.Name = "txtOrderNo";
            this.pnlSearchFields.Controls.Add(this.txtOrderNo, 0, 1);

            StyleTextBox(this.txtCustomer);
            this.txtCustomer.Name = "txtCustomer";
            this.pnlSearchFields.Controls.Add(this.txtCustomer, 1, 1);

            // Status combo
            this.cboStatus.Dock          = System.Windows.Forms.DockStyle.Fill;
            this.cboStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboStatus.FlatStyle     = System.Windows.Forms.FlatStyle.Flat;
            this.cboStatus.Font          = new System.Drawing.Font("Segoe UI", 9.5F);
            this.cboStatus.BackColor     = System.Drawing.Color.FromArgb(245, 247, 250);
            this.cboStatus.Margin        = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.cboStatus.Name          = "cboStatus";
            this.cboStatus.Items.AddRange(new object[] { "All", "Pending", "Processing", "Delivered", "Cancelled" });
            this.cboStatus.SelectedIndex = 0;
            this.pnlSearchFields.Controls.Add(this.cboStatus, 2, 1);

            // DateFrom — inline checkbox + DateTimePicker in a sub-panel
            var pnlDF = BuildDatePanel(this.chkDateFrom, this.dtpDateFrom, "dtpDateFrom");
            this.pnlSearchFields.Controls.Add(pnlDF, 3, 1);

            // DateTo label — spans col 4 (empty) + col 5
            this.pnlSearchFields.Controls.Add(this.lblDateTo,   5, 0);
            var pnlDT = BuildDatePanel(this.chkDateTo,   this.dtpDateTo,   "dtpDateTo");
            this.pnlSearchFields.Controls.Add(pnlDT, 5, 1);

            // ── Button row ────────────────────────────────────────────────
            this.pnlButtons.Dock          = System.Windows.Forms.DockStyle.Top;
            this.pnlButtons.Height        = 46;
            this.pnlButtons.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.pnlButtons.BackColor     = System.Drawing.Color.Transparent;
            this.pnlButtons.Padding       = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.pnlButtons.WrapContents  = false;
            this.pnlButtons.Name          = "pnlButtons";

            StyleBtn(this.btnSearch,      "🔍  Search",       System.Drawing.Color.FromArgb(37, 99, 235),   System.Drawing.Color.White);
            StyleBtn(this.btnClear,       "✕  Clear",         System.Drawing.Color.White,                    System.Drawing.Color.FromArgb(75, 85, 99));
            StyleBtn(this.btnCreateOrder, "＋  Create Order", System.Drawing.Color.FromArgb(5, 150, 105),   System.Drawing.Color.White);
            this.btnClear.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(209, 213, 219);
            this.btnClear.FlatAppearance.BorderSize  = 1;

            this.pnlButtons.Controls.Add(this.btnSearch);
            this.pnlButtons.Controls.Add(this.btnClear);
            this.pnlButtons.Controls.Add(this.btnCreateOrder);

            this.pnlToolbar.Controls.Add(this.pnlButtons);
            this.pnlToolbar.Controls.Add(this.pnlSearchFields);

            // ════════════════════════════════════════════════════════════════
            //  KPI bar
            // ════════════════════════════════════════════════════════════════
            this.pnlKpi.Dock          = System.Windows.Forms.DockStyle.Top;
            this.pnlKpi.Height        = 52;
            this.pnlKpi.BackColor     = System.Drawing.Color.Transparent;
            this.pnlKpi.Padding       = new System.Windows.Forms.Padding(0, 8, 0, 4);
            this.pnlKpi.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.pnlKpi.WrapContents  = false;
            this.pnlKpi.Name          = "pnlKpi";

            // ════════════════════════════════════════════════════════════════
            //  RESULT COUNT
            // ════════════════════════════════════════════════════════════════
            this.lblResultCount.Dock      = System.Windows.Forms.DockStyle.Top;
            this.lblResultCount.Height    = 22;
            this.lblResultCount.Font      = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.lblResultCount.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
            this.lblResultCount.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblResultCount.Padding   = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.lblResultCount.Name      = "lblResultCount";
            this.lblResultCount.Text      = "";

            // ════════════════════════════════════════════════════════════════
            //  DATAGRIDVIEW
            // ════════════════════════════════════════════════════════════════
            this.dgvOrders.Dock                        = System.Windows.Forms.DockStyle.Fill;
            this.dgvOrders.BackgroundColor             = System.Drawing.Color.White;
            this.dgvOrders.BorderStyle                 = System.Windows.Forms.BorderStyle.None;
            this.dgvOrders.RowHeadersVisible           = false;
            this.dgvOrders.AllowUserToAddRows          = false;
            this.dgvOrders.AllowUserToDeleteRows       = false;
            this.dgvOrders.AllowUserToResizeRows       = false;
            this.dgvOrders.ReadOnly                    = true;
            this.dgvOrders.SelectionMode               = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvOrders.MultiSelect                 = false;
            this.dgvOrders.AutoSizeColumnsMode         = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvOrders.ColumnHeadersDefaultCellStyle.BackColor  = System.Drawing.Color.FromArgb(246, 249, 255);
            this.dgvOrders.ColumnHeadersDefaultCellStyle.ForeColor  = System.Drawing.Color.FromArgb(107, 114, 128);
            this.dgvOrders.ColumnHeadersDefaultCellStyle.Font       = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.dgvOrders.ColumnHeadersDefaultCellStyle.Padding    = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.dgvOrders.ColumnHeadersHeight          = 38;
            this.dgvOrders.ColumnHeadersHeightSizeMode  = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvOrders.DefaultCellStyle.Font        = new System.Drawing.Font("Segoe UI", 9.5F);
            this.dgvOrders.DefaultCellStyle.ForeColor   = System.Drawing.Color.FromArgb(31, 41, 55);
            this.dgvOrders.DefaultCellStyle.BackColor   = System.Drawing.Color.White;
            this.dgvOrders.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(219, 234, 254);
            this.dgvOrders.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.FromArgb(30, 64, 175);
            this.dgvOrders.DefaultCellStyle.Padding     = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.dgvOrders.RowTemplate.Height            = 48;
            this.dgvOrders.GridColor                     = System.Drawing.Color.FromArgb(229, 231, 235);
            this.dgvOrders.CellBorderStyle               = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgvOrders.EnableHeadersVisualStyles     = false;
            this.dgvOrders.Name                          = "dgvOrders";

            // Columns
            this.dgvOrders.Columns.Add(new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "colOrderID",      HeaderText = "ORDER NO.",    FillWeight = 13 });
            this.dgvOrders.Columns.Add(new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "colCustomer",     HeaderText = "CUSTOMER",     FillWeight = 18 });
            this.dgvOrders.Columns.Add(new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "colSales",        HeaderText = "SALES REP",    FillWeight = 14 });
            this.dgvOrders.Columns.Add(new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "colIssuedTime",   HeaderText = "ORDER DATE",   FillWeight = 13 });
            this.dgvOrders.Columns.Add(new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "colDelivery",     HeaderText = "DELIVERY DATE",FillWeight = 13 });
            this.dgvOrders.Columns.Add(new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "colGrandTotal",   HeaderText = "GRAND TOTAL",  FillWeight = 13 });
            this.dgvOrders.Columns.Add(new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "colStatus",       HeaderText = "STATUS",       FillWeight = 11 });
            var colAction = new System.Windows.Forms.DataGridViewButtonColumn
            {
                Name       = "colAction",
                HeaderText = "",
                Text       = "View Details",
                UseColumnTextForButtonValue = true,
                FillWeight  = 10,
                FlatStyle   = System.Windows.Forms.FlatStyle.Flat
            };
            this.dgvOrders.Columns.Add(colAction);

            // ════════════════════════════════════════════════════════════════
            //  CONTENT layout (DockStyle stacking — bottom to top order)
            // ════════════════════════════════════════════════════════════════
            this.pnlContent.Controls.Add(this.dgvOrders);
            this.pnlContent.Controls.Add(this.lblResultCount);
            this.pnlContent.Controls.Add(this.pnlKpi);
            this.pnlContent.Controls.Add(this.pnlToolbar);

            // ════════════════════════════════════════════════════════════════
            //  FORM
            // ════════════════════════════════════════════════════════════════
            this.Controls.Add(this.pnlContent);
            this.Text          = "Order Processing — View Orders";
            this.WindowState   = System.Windows.Forms.FormWindowState.Maximized;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Font          = new System.Drawing.Font("Segoe UI", 9.5F);
            this.Name          = "ViewOrderForm";

            this.pnlShell.ResumeLayout(false);
            this.pnlContent.ResumeLayout(false);
            this.pnlToolbar.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvOrders)).EndInit();
            this.ResumeLayout(false);
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private static void StyleLabel(System.Windows.Forms.Label lbl, string text)
        {
            lbl.Text      = text;
            lbl.Dock      = System.Windows.Forms.DockStyle.Fill;
            lbl.Font      = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            lbl.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
            lbl.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            lbl.Margin    = new System.Windows.Forms.Padding(0, 0, 8, 2);
        }

        private static void StyleTextBox(System.Windows.Forms.TextBox txt)
        {
            txt.Dock        = System.Windows.Forms.DockStyle.Fill;
            txt.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            txt.BackColor   = System.Drawing.Color.FromArgb(245, 247, 250);
            txt.Font        = new System.Drawing.Font("Segoe UI", 9.5F);
            txt.Margin      = new System.Windows.Forms.Padding(0, 0, 8, 0);
        }

        private static void StyleBtn(
            System.Windows.Forms.Button btn, string text,
            System.Drawing.Color bg, System.Drawing.Color fg)
        {
            btn.Text      = text;
            btn.BackColor = bg;
            btn.ForeColor = fg;
            btn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btn.FlatAppearance.BorderSize  = 0;
            btn.Font      = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            btn.Height    = 36;
            btn.Width     = 130;
            btn.Margin    = new System.Windows.Forms.Padding(0, 0, 8, 0);
            btn.Cursor    = System.Windows.Forms.Cursors.Hand;
        }

        /// <summary>Builds a small panel with a CheckBox enable-toggle + DateTimePicker side by side.</summary>
        private static System.Windows.Forms.Panel BuildDatePanel(
            System.Windows.Forms.CheckBox chk,
            System.Windows.Forms.DateTimePicker dtp,
            string dtpName)
        {
            chk.Text      = "";
            chk.Width     = 20;
            chk.Dock      = System.Windows.Forms.DockStyle.Left;
            chk.Checked   = false;
            chk.Cursor    = System.Windows.Forms.Cursors.Hand;

            dtp.Name      = dtpName;
            dtp.Format    = System.Windows.Forms.DateTimePickerFormat.Short;
            dtp.Font      = new System.Drawing.Font("Segoe UI", 9.5F);
            dtp.Dock      = System.Windows.Forms.DockStyle.Fill;
            dtp.Enabled   = false;   // enabled only when chk is ticked
            dtp.Value     = System.DateTime.Today;

            var pnl       = new System.Windows.Forms.Panel();
            pnl.Dock      = System.Windows.Forms.DockStyle.Fill;
            pnl.Margin    = new System.Windows.Forms.Padding(0, 0, 8, 0);
            pnl.Controls.Add(dtp);
            pnl.Controls.Add(chk);
            return pnl;
        }

        // ── Control declarations ──────────────────────────────────────────────
        private System.Windows.Forms.Panel              pnlShell;
        private System.Windows.Forms.Panel              pnlContent;
        private System.Windows.Forms.Panel              pnlToolbar;
        private System.Windows.Forms.TableLayoutPanel   pnlSearchFields;
        private System.Windows.Forms.FlowLayoutPanel    pnlButtons;
        private System.Windows.Forms.FlowLayoutPanel    pnlKpi;
        private System.Windows.Forms.Label              lblOrderNo;
        private System.Windows.Forms.Label              lblCustomer;
        private System.Windows.Forms.Label              lblStatus;
        private System.Windows.Forms.Label              lblDateFrom;
        private System.Windows.Forms.Label              lblDateTo;
        private System.Windows.Forms.TextBox            txtOrderNo;
        private System.Windows.Forms.TextBox            txtCustomer;
        private System.Windows.Forms.ComboBox           cboStatus;
        private System.Windows.Forms.DateTimePicker     dtpDateFrom;
        private System.Windows.Forms.DateTimePicker     dtpDateTo;
        private System.Windows.Forms.CheckBox           chkDateFrom;
        private System.Windows.Forms.CheckBox           chkDateTo;
        private System.Windows.Forms.Button             btnSearch;
        private System.Windows.Forms.Button             btnClear;
        private System.Windows.Forms.Button             btnCreateOrder;
        private System.Windows.Forms.DataGridView       dgvOrders;
        private System.Windows.Forms.Label              lblResultCount;
        #endregion
    }
}
