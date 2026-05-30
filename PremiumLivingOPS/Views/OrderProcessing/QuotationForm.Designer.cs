using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.OrderProcessing
{
    partial class QuotationForm
    {
        private System.ComponentModel.IContainer components = null;

        private AppShell     _shell;
        private DataGridView dgvQuotations;
        private ComboBox     cboStatusFilter;
        private ComboBox     cboNewStatus;
        private Button       btnRefresh;
        private Button       btnUpdateStatus;
        private Label        lblFilterLabel;
        private Label        lblNewStatusLabel;

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
            this.MinimumSize   = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = Palette.BgPage;
            this.WindowState   = FormWindowState.Maximized;
            this.Font          = new Font("Segoe UI", 11f);

            Panel pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage };

            // AppShell — events are bound in QuotationForm.cs
            _shell = new AppShell();
            _shell.SetPopupContainer(pnlMain);

            Panel pnlContent = new Panel
            {
                Dock = DockStyle.Fill, Padding = new Padding(28, 20, 28, 24), BackColor = Palette.BgPage
            };

            Label lblTitle = new Label
            {
                Text = "Quotation Management", Font = new Font("Segoe UI", 22f, FontStyle.Bold),
                ForeColor = Palette.TextMain, AutoSize = true, Location = new Point(0, 0)
            };

            // Toolbar
            Panel pnlToolbar = new Panel { Height = 56, BackColor = Palette.BgCard, Padding = new Padding(12, 10, 12, 10) };
            pnlToolbar.Paint += (s, e) => e.Graphics.DrawRectangle(new System.Drawing.Pen(Palette.BorderColor, 1), 0, 0, ((Panel)s).Width-1, ((Panel)s).Height-1);

            lblFilterLabel = new Label { Text = "Filter:", Font = new Font("Segoe UI", 11f), ForeColor = Palette.TextMuted, AutoSize = true, Location = new Point(12, 16) };
            cboStatusFilter = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150, Location = new Point(62, 12), Font = new Font("Segoe UI", 11f) };
            cboStatusFilter.Items.AddRange(new object[] { "All", "Pending", "Converted", "Rejected" });
            cboStatusFilter.SelectedIndex = 0;
            cboStatusFilter.SelectedIndexChanged += cboStatusFilter_SelectedIndexChanged;

            btnRefresh = new Button { Text = "↻ Refresh", Font = new Font("Segoe UI", 11f), ForeColor = Palette.Primary, FlatStyle = FlatStyle.Flat, Width = 100, Height = 34, Location = new Point(226, 11) };
            btnRefresh.FlatAppearance.BorderColor = Palette.Primary; btnRefresh.FlatAppearance.BorderSize = 1;
            btnRefresh.Click += btnRefresh_Click;

            Panel divider = new Panel { Width = 1, Height = 34, Location = new Point(340, 11), BackColor = Palette.BorderColor };

            lblNewStatusLabel = new Label { Text = "Change Status:", Font = new Font("Segoe UI", 11f), ForeColor = Palette.TextMuted, AutoSize = true, Location = new Point(356, 16) };
            cboNewStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140, Location = new Point(472, 12), Font = new Font("Segoe UI", 11f), Enabled = false };
            cboNewStatus.Items.AddRange(new object[] { "Pending", "Converted", "Rejected" });
            cboNewStatus.SelectedIndex = 0;

            btnUpdateStatus = new Button { Text = "Update", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = Color.White, BackColor = Palette.Primary, FlatStyle = FlatStyle.Flat, Width = 100, Height = 34, Location = new Point(626, 11), Enabled = false };
            btnUpdateStatus.FlatAppearance.BorderSize = 0;
            btnUpdateStatus.Click += btnUpdateStatus_Click;

            pnlToolbar.Controls.Add(lblFilterLabel);
            pnlToolbar.Controls.Add(cboStatusFilter);
            pnlToolbar.Controls.Add(btnRefresh);
            pnlToolbar.Controls.Add(divider);
            pnlToolbar.Controls.Add(lblNewStatusLabel);
            pnlToolbar.Controls.Add(cboNewStatus);
            pnlToolbar.Controls.Add(btnUpdateStatus);

            // DataGridView
            dgvQuotations = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true,
                AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Palette.BgCard, BorderStyle = BorderStyle.None,
                GridColor = Palette.BorderColor, Font = new Font("Segoe UI", 11f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate = { Height = 38 }, MultiSelect = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255), ForeColor = Palette.TextMuted,
                    Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), Padding = new Padding(6)
                },
                ColumnHeadersHeight = 42,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Palette.BgCard, ForeColor = Palette.TextMain,
                    SelectionBackColor = Color.FromArgb(240, 246, 255), SelectionForeColor = Palette.TextMain,
                    Padding = new Padding(8, 5, 8, 5)
                }
            };
            dgvQuotations.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQuotationID", HeaderText = "Quotation ID",  FillWeight = 14, SortMode = DataGridViewColumnSortMode.NotSortable });
            dgvQuotations.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomer",    HeaderText = "Customer",       FillWeight = 22, SortMode = DataGridViewColumnSortMode.NotSortable });
            dgvQuotations.Columns.Add(new DataGridViewTextBoxColumn { Name = "colExpiry",      HeaderText = "Expiry Date",    FillWeight = 13, SortMode = DataGridViewColumnSortMode.NotSortable });
            dgvQuotations.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal",       HeaderText = "Total Amount",   FillWeight = 14, SortMode = DataGridViewColumnSortMode.NotSortable });
            dgvQuotations.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDeposit",     HeaderText = "Deposit Req.",   FillWeight = 13, SortMode = DataGridViewColumnSortMode.NotSortable });
            dgvQuotations.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLeadTime",    HeaderText = "Lead Time",      FillWeight = 12, SortMode = DataGridViewColumnSortMode.NotSortable });
            dgvQuotations.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",      HeaderText = "Status",         FillWeight = 12, SortMode = DataGridViewColumnSortMode.NotSortable });
            dgvQuotations.SelectionChanged += dgvQuotations_SelectionChanged;

            // Header strip
            FlowLayoutPanel flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = lblTitle.PreferredHeight + 12 + 56 + 10,
                FlowDirection = FlowDirection.TopDown, WrapContents = false,
                AutoSize = false, BackColor = Palette.BgPage, Padding = new Padding(0)
            };
            flow.Controls.Add(lblTitle);
            flow.Controls.Add(new Panel { Height = 12, Width = 10, BackColor = Palette.BgPage });
            flow.Controls.Add(pnlToolbar);
            flow.Controls.Add(new Panel { Height = 10, Width = 10, BackColor = Palette.BgPage });
            flow.Resize += (s, e) => pnlToolbar.Width = flow.Width;

            pnlContent.Controls.Add(dgvQuotations);
            pnlContent.Controls.Add(flow);

            pnlMain.Controls.Add(pnlContent);
            pnlMain.Controls.Add(_shell);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }
    }
}
