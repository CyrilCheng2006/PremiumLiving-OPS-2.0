namespace PremiumLivingOPS.Views.ProductionProcessing
{
    partial class SearchMaterialRequestForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            // ── AppShell (Tap Nav Bar + User Bar) — must not be modified ──
            this._shell = new PremiumLivingOPS.Views.Shared.AppShell();
            this._shell.Dock = System.Windows.Forms.DockStyle.Fill;
            this._shell.TopNavMenuItemClicked += (s, args) => OnTopNavMenuItemClicked(args.MenuLabel, args.SubItem);
            this._shell.LogoutClicked         += BtnLogout_Click;

            // ── Root layout: AppShell fills the form ──
            this.SuspendLayout();
            this.Controls.Add(this._shell);

            // ── Page content panel (injected into AppShell content area) ──
            var pnlPage = new System.Windows.Forms.Panel
            {
                Dock      = System.Windows.Forms.DockStyle.Fill,
                BackColor = System.Drawing.Color.FromArgb(243, 244, 246),
                Padding   = new System.Windows.Forms.Padding(20)
            };
            this._shell.SetContentPanel(pnlPage);

            // ══ Outer CardPanel (Level 1) ══
            var card1 = new System.Windows.Forms.Panel
            {
                Dock        = System.Windows.Forms.DockStyle.Fill,
                BackColor   = System.Drawing.Color.White,
                Padding     = new System.Windows.Forms.Padding(20)
            };
            card1.Paint += (s, e) =>
            {
                using var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, ((System.Windows.Forms.Panel)s).Width - 1, ((System.Windows.Forms.Panel)s).Height - 1);
            };
            pnlPage.Controls.Add(card1);

            // ══ Main layout inside card1: Top (filter bar + KPI) + Fill (grid card) ══
            var pnlTop = new System.Windows.Forms.Panel
            {
                Dock      = System.Windows.Forms.DockStyle.Top,
                Height    = 160,
                BackColor = System.Drawing.Color.Transparent
            };
            card1.Controls.Add(pnlTop);

            // ══ KPI panel inside pnlTop ─ bottom half ══
            this.pnlKpi = new System.Windows.Forms.Panel
            {
                Dock      = System.Windows.Forms.DockStyle.Bottom,
                Height    = 76,
                BackColor = System.Drawing.Color.Transparent
            };
            pnlTop.Controls.Add(this.pnlKpi);

            // ══ Filter bar ─ top half of pnlTop ══
            var pnlFilter = new System.Windows.Forms.Panel
            {
                Dock      = System.Windows.Forms.DockStyle.Fill,
                BackColor = System.Drawing.Color.Transparent
            };
            pnlTop.Controls.Add(pnlFilter);

            // Keyword label + textbox
            var lblKeyword = new System.Windows.Forms.Label
            {
                Text      = "Keyword:",
                Font      = new System.Drawing.Font("Segoe UI", 11f),
                ForeColor = System.Drawing.Color.FromArgb(55, 65, 81),
                AutoSize  = true,
                Location  = new System.Drawing.Point(0, 18)
            };
            this.txtKeyword = new System.Windows.Forms.TextBox
            {
                Font      = new System.Drawing.Font("Segoe UI", 11f),
                Size      = new System.Drawing.Size(260, 32),
                Location  = new System.Drawing.Point(80, 14)
            };

            // Urgency filter
            var lblUrgency = new System.Windows.Forms.Label
            {
                Text      = "Urgency:",
                Font      = new System.Drawing.Font("Segoe UI", 11f),
                ForeColor = System.Drawing.Color.FromArgb(55, 65, 81),
                AutoSize  = true,
                Location  = new System.Drawing.Point(360, 18)
            };
            this.cboUrgency = new System.Windows.Forms.ComboBox
            {
                Font          = new System.Drawing.Font("Segoe UI", 11f),
                DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList,
                Size          = new System.Drawing.Size(160, 32),
                Location      = new System.Drawing.Point(440, 14)
            };
            this.cboUrgency.Items.AddRange(new object[] { "All", "Critical", "High", "Medium" });
            this.cboUrgency.SelectedIndex = 0;

            // Trigger filter
            var lblTrigger = new System.Windows.Forms.Label
            {
                Text      = "Trigger:",
                Font      = new System.Drawing.Font("Segoe UI", 11f),
                ForeColor = System.Drawing.Color.FromArgb(55, 65, 81),
                AutoSize  = true,
                Location  = new System.Drawing.Point(620, 18)
            };
            this.cboTrigger = new System.Windows.Forms.ComboBox
            {
                Font          = new System.Drawing.Font("Segoe UI", 11f),
                DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList,
                Size          = new System.Drawing.Size(160, 32),
                Location      = new System.Drawing.Point(695, 14)
            };
            this.cboTrigger.Items.AddRange(new object[] { "All", "Reorder", "OrderDemand" });
            this.cboTrigger.SelectedIndex = 0;

            // Search button
            this.btnSearch = new System.Windows.Forms.Button
            {
                Text      = "Search",
                Font      = new System.Drawing.Font("Segoe UI", 11f, System.Drawing.FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(47, 111, 237),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = System.Windows.Forms.FlatStyle.Flat,
                Size      = new System.Drawing.Size(110, 36),
                Location  = new System.Drawing.Point(876, 12),
                Cursor    = System.Windows.Forms.Cursors.Hand
            };
            this.btnSearch.FlatAppearance.BorderSize = 0;

            // Reset button
            this.btnReset = new System.Windows.Forms.Button
            {
                Text      = "Reset",
                Font      = new System.Drawing.Font("Segoe UI", 11f),
                BackColor = System.Drawing.Color.White,
                ForeColor = System.Drawing.Color.FromArgb(55, 65, 81),
                FlatStyle = System.Windows.Forms.FlatStyle.Flat,
                Size      = new System.Drawing.Size(90, 36),
                Location  = new System.Drawing.Point(996, 12),
                Cursor    = System.Windows.Forms.Cursors.Hand
            };
            this.btnReset.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(209, 213, 219);
            this.btnReset.FlatAppearance.BorderSize  = 1;

            pnlFilter.Controls.AddRange(new System.Windows.Forms.Control[]
            {
                lblKeyword, this.txtKeyword,
                lblUrgency, this.cboUrgency,
                lblTrigger, this.cboTrigger,
                this.btnSearch, this.btnReset
            });

            // ══ Grid card (Level 2) ══
            var card2 = new System.Windows.Forms.Panel
            {
                Dock      = System.Windows.Forms.DockStyle.Fill,
                BackColor = System.Drawing.Color.White,
                Padding   = new System.Windows.Forms.Padding(0)
            };
            card2.Paint += (s, e) =>
            {
                using var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, ((System.Windows.Forms.Panel)s).Width - 1, ((System.Windows.Forms.Panel)s).Height - 1);
            };
            card1.Controls.Add(card2);

            // Action buttons bar inside card2 (top)
            var pnlActions = new System.Windows.Forms.Panel
            {
                Dock      = System.Windows.Forms.DockStyle.Top,
                Height    = 56,
                BackColor = System.Drawing.Color.White,
                Padding   = new System.Windows.Forms.Padding(12, 10, 12, 0)
            };

            this.btnViewDetail = new System.Windows.Forms.Button
            {
                Text      = "\uD83D\uDD0D  View Detail",
                Font      = new System.Drawing.Font("Segoe UI", 11f, System.Drawing.FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(47, 111, 237),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = System.Windows.Forms.FlatStyle.Flat,
                Size      = new System.Drawing.Size(160, 36),
                Dock      = System.Windows.Forms.DockStyle.Left,
                Enabled   = false,
                Cursor    = System.Windows.Forms.Cursors.Hand
            };
            this.btnViewDetail.FlatAppearance.BorderSize = 0;

            this.btnCreateNew = new System.Windows.Forms.Button
            {
                Text      = "+ Create New",
                Font      = new System.Drawing.Font("Segoe UI", 11f),
                BackColor = System.Drawing.Color.White,
                ForeColor = System.Drawing.Color.FromArgb(15, 31, 53),
                FlatStyle = System.Windows.Forms.FlatStyle.Flat,
                Size      = new System.Drawing.Size(140, 36),
                Dock      = System.Windows.Forms.DockStyle.Left,
                Cursor    = System.Windows.Forms.Cursors.Hand
            };
            this.btnCreateNew.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(209, 213, 219);
            this.btnCreateNew.FlatAppearance.BorderSize  = 1;

            pnlActions.Controls.Add(this.btnCreateNew);   // Left last = rightmost
            pnlActions.Controls.Add(this.btnViewDetail);  // Left first = leftmost

            // ══ DataGridView (Level 3) ══
            this.dgvRequests = new System.Windows.Forms.DataGridView
            {
                Dock                  = System.Windows.Forms.DockStyle.Fill,
                ReadOnly              = true,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible     = false,
                SelectionMode         = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor       = System.Drawing.Color.White,
                BorderStyle           = System.Windows.Forms.BorderStyle.None,
                AutoSizeColumnsMode   = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill,
                Font                  = new System.Drawing.Font("Segoe UI", 11f),
                ColumnHeadersHeight   = 40,
                MultiSelect           = false
            };
            this.dgvRequests.RowTemplate.Height          = 44;
            this.dgvRequests.ColumnHeadersDefaultCellStyle.Font      = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Bold);
            this.dgvRequests.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(246, 249, 255);
            this.dgvRequests.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(98, 112, 135);
            this.dgvRequests.EnableHeadersVisualStyles               = false;
            this.dgvRequests.DefaultCellStyle.SelectionBackColor     = System.Drawing.Color.FromArgb(219, 234, 254);
            this.dgvRequests.DefaultCellStyle.SelectionForeColor     = System.Drawing.Color.FromArgb(15, 31, 53);
            this.dgvRequests.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(249, 250, 251);

            // Columns — one row per BatchPrefix (no per-item columns)
            this.dgvRequests.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[]
            {
                new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "colRequestID",  HeaderText = "Request ID",         FillWeight =  18 },
                new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "colLines",       HeaderText = "Lines",             FillWeight =   6 },
                new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "colTotalQty",    HeaderText = "Total Req. Qty",    FillWeight =  10 },
                new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "colUrgency",     HeaderText = "Urgency",           FillWeight =  10 },
                new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "colTrigger",     HeaderText = "Trigger",           FillWeight =  12 },
                new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "colOrderID",     HeaderText = "Linked Order",      FillWeight =  16 },
                new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "colWarehouse",   HeaderText = "Warehouse",         FillWeight =  20 },
                new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "colStock",       HeaderText = "Current Stock",     FillWeight =  10 },
                new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "colLinkedPO",    HeaderText = "Linked to PO",      FillWeight =  10 },
            });

            card2.Controls.Add(this.dgvRequests);
            card2.Controls.Add(pnlActions);

            // ── Form properties ──
            this.Text            = "Search Raw Material Request";
            this.WindowState     = System.Windows.Forms.FormWindowState.Maximized;
            this.Font            = new System.Drawing.Font("Segoe UI", 11f);
            this.BackColor       = System.Drawing.Color.FromArgb(243, 244, 246);
            this.ResumeLayout(false);
        }

        // Designer fields
        private PremiumLivingOPS.Views.Shared.AppShell _shell;
        private System.Windows.Forms.Panel             pnlKpi;
        private System.Windows.Forms.TextBox           txtKeyword;
        private System.Windows.Forms.ComboBox          cboUrgency;
        private System.Windows.Forms.ComboBox          cboTrigger;
        private System.Windows.Forms.Button            btnSearch;
        private System.Windows.Forms.Button            btnReset;
        private System.Windows.Forms.Button            btnViewDetail;
        private System.Windows.Forms.Button            btnCreateNew;
        private System.Windows.Forms.DataGridView      dgvRequests;
    }
}
