using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.OrderProcessing
{
    public partial class QuotationForm : Form
    {
        private readonly OrderProcessingController _ctrl = new OrderProcessingController();
        private List<QuotationEntity> _currentQuotations = new List<QuotationEntity>();

        private Button _btnCreate;

        private static readonly Dictionary<string, (Color bg, Color fg)> StatusColors =
            new Dictionary<string, (Color, Color)>
            {
                { "Pending",   (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "Converted", (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
                { "Rejected",  (Color.FromArgb(254, 226, 226), Color.FromArgb(153,  27,  27)) },
            };

        public QuotationForm()
        {
            InitializeComponent();
            this.Load += QuotationForm_Load;
        }

        private void QuotationForm_Load(object sender, EventArgs e)
        {
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;
            InjectCreateButton();
            RefreshGrid();
        }

        private void InjectCreateButton()
        {
            var pnlActionArea = btnViewDetail.Parent as Panel;
            if (pnlActionArea == null) return;

            const int ItemW = 210;
            const int ItemH =  60;
            const int Gap   =   8;
            const int Pad   =  12;

            pnlActionArea.Width += ItemW + Gap;

            _btnCreate = new Button
            {
                Text      = "+ New",
                Name      = "btnCreateQuotation",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(6, 95, 70),
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
                Size      = new Size(ItemW, ItemH)
            };
            _btnCreate.FlatAppearance.BorderSize         = 0;
            _btnCreate.FlatAppearance.MouseOverBackColor = Color.FromArgb(4, 78, 57);
            _btnCreate.FlatAppearance.MouseDownBackColor = Color.FromArgb(3, 60, 43);
            _btnCreate.Click += BtnCreateQuotation_Click;
            pnlActionArea.Controls.Add(_btnCreate);

            void CentreAll()
            {
                int top = (pnlActionArea.Height - ItemH) / 2;
                if (top < 0) top = 0;
                int x = Pad;

                _btnCreate.Location  = new Point(x, top); _btnCreate.Size  = new Size(ItemW, ItemH); x += ItemW + Gap;
                btnViewDetail.Location = new Point(x, top); btnViewDetail.Size = new Size(ItemW, ItemH); x += ItemW + Gap;
                btnAddFrom.Location  = new Point(x, top); btnAddFrom.Size  = new Size(ItemW, ItemH); x += ItemW + Gap;
                cboNewStatus.Location = new Point(x, top + (ItemH - cboNewStatus.Height) / 2); cboNewStatus.Width = ItemW; x += ItemW + Gap;
                btnUpdateStatus.Location = new Point(x, top); btnUpdateStatus.Size = new Size(ItemW, ItemH);
            }

            pnlActionArea.Resize += (s, e) => CentreAll();
            CentreAll();
        }

        // ── Core refresh
        private void RefreshGrid()
        {
            string keyword      = txtSearchKeyword.Text.Trim();
            string statusSelect = cboStatus.SelectedItem?.ToString();
            string statusFilter = (statusSelect == "All" || string.IsNullOrEmpty(statusSelect)) ? null : statusSelect;

            var vm = _ctrl.GetQuotationListVM(statusFilter, keyword);

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Order Processing  ›  Quotation");

            _currentQuotations = vm.Quotations;

            dgvQuotations.Rows.Clear();
            foreach (var q in _currentQuotations)
                dgvQuotations.Rows.Add(
                    q.QuotationID, q.CustomerName,
                    q.ExpiryDate.ToString("yyyy-MM-dd"),
                    $"HK$ {q.TotalAmount:N2}",
                    q.DepositRequired.HasValue ? $"HK$ {q.DepositRequired.Value:N2}" : "—",
                    q.LeadTimeEstimated,
                    q.QuotationStatus);

            RefreshKpi();
            UpdateActionButtons();
        }

        private void ResetFilters()
        {
            txtSearchKeyword.Text   = string.Empty;
            cboStatus.SelectedIndex = 0;
            RefreshGrid();
        }

        // ── KPI bar
        private void RefreshKpi()
        {
            pnlKpi.Controls.Clear();

            var allQuotations = _ctrl.GetQuotationListVM().Quotations;
            int total     = allQuotations.Count;
            int pending   = allQuotations.FindAll(q => q.QuotationStatus == "Pending").Count;
            int converted = allQuotations.FindAll(q => q.QuotationStatus == "Converted").Count;
            int rejected  = allQuotations.FindAll(q => q.QuotationStatus == "Rejected").Count;

            var pills = new[]
            {
                ("Total",     total.ToString(),     Color.FromArgb( 47, 111, 237), Color.FromArgb(219, 234, 254), "All"),
                ("Pending",   pending.ToString(),   Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199), "Pending"),
                ("Converted", converted.ToString(), Color.FromArgb(  6,  95,  70), Color.FromArgb(209, 250, 229), "Converted"),
                ("Rejected",  rejected.ToString(),  Color.FromArgb(153,  27,  27), Color.FromArgb(254, 226, 226), "Rejected"),
            };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent,
                Padding = new Padding(0), AutoScroll = false
            };

            const int PillW = 290, PillH = 60, Gap = 8, NumColW = 80;

            foreach (var (label, count, fg, bg, filterItem) in pills)
            {
                var pill = new Panel { BackColor = bg, Size = new Size(PillW, PillH), Margin = new Padding(0, 0, Gap, 0), Cursor = Cursors.Hand };
                pill.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path  = RoundedRect(((Panel)s).ClientRectangle, 8);
                    using var brush = new SolidBrush(((Panel)s).BackColor);
                    e.Graphics.FillPath(brush, path);
                };

                var tlp = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                    BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding = new Padding(10, 0, 8, 0)
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, NumColW));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                tlp.Controls.Add(new Label { Text = count, Font = new Font("Segoe UI", 14f, FontStyle.Bold), ForeColor = fg, BackColor = Color.Transparent, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, AutoSize = false }, 0, 0);
                tlp.Controls.Add(new Label { Text = label, Font = new Font("Segoe UI", 12f), ForeColor = fg, BackColor = Color.Transparent, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false }, 1, 0);

                string localFilter = filterItem;
                EventHandler click = (s, e) => { int idx = cboStatus.FindStringExact(localFilter); if (idx >= 0) cboStatus.SelectedIndex = idx; RefreshGrid(); };
                pill.Click += click; tlp.Click += click;
                foreach (Control c in tlp.Controls) c.Click += click;
                pill.Controls.Add(tlp);
                flow.Controls.Add(pill);
            }
            pnlKpi.Controls.Add(flow);
        }

        private void UpdateActionButtons()
        {
            bool sel = dgvQuotations.SelectedRows.Count > 0;
            btnViewDetail.Enabled   = sel;
            btnAddFrom.Enabled      = sel;
            btnUpdateStatus.Enabled = sel;
            cboNewStatus.Enabled    = sel;
        }

        // ── Event handlers
        private void dgvQuotations_SelectionChanged(object sender, EventArgs e)
        {
            UpdateActionButtons();
            if (dgvQuotations.SelectedRows.Count > 0)
            {
                string current = dgvQuotations.SelectedRows[0].Cells["colStatus"].Value?.ToString();
                int idx = cboNewStatus.FindStringExact(current);
                if (idx >= 0) cboNewStatus.SelectedIndex = idx;
            }
        }

        private void dgvQuotations_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvQuotations.Columns[e.ColumnIndex].Name != "colStatus" || e.Value == null) return;
            string dbValue = e.Value.ToString();
            e.FormattingApplied = true;
            if (StatusColors.TryGetValue(dbValue, out var colors))
            {
                e.CellStyle.ForeColor          = colors.fg;
                e.CellStyle.BackColor          = colors.bg;
                e.CellStyle.SelectionForeColor = colors.fg;
                e.CellStyle.SelectionBackColor = colors.bg;
                e.CellStyle.Font               = new Font("Segoe UI", 11f, FontStyle.Bold);
                e.CellStyle.Alignment          = DataGridViewContentAlignment.MiddleCenter;
            }
        }

        private void dgvQuotations_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) OpenDetailDialog();
        }

        private void btnViewDetail_Click(object sender, EventArgs e) => OpenDetailDialog();

        // ── CREATE NEW QUOTATION
        private void BtnCreateQuotation_Click(object sender, EventArgs e)
        {
            using var dlg = new CreateQuotationDialog(_ctrl);
            if (dlg.ShowDialog(this) == DialogResult.OK)
                RefreshGrid();
        }

        // ── MODIFY QUOTATION
        private void btnAddFrom_Click(object sender, EventArgs e)
        {
            string qid = SelectedQuotationId();
            if (qid == null) return;

            var q = _ctrl.GetQuotationDetail(qid);
            if (q == null) { MessageBox.Show("Quotation not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }

            bool linkedToOrder = q.QuotationStatus == "Converted" || _ctrl.IsQuotationLinkedToOrder(qid);
            if (linkedToOrder)
            {
                MessageBox.Show($"Quotation {qid} has already been linked to an Order and cannot be modified.",
                    "Modification Not Allowed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var dlg = new ModifyQuotationDialog(q, _ctrl);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                MessageBox.Show($"Quotation {qid} has been updated successfully.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshGrid();
            }
        }

        private string SelectedQuotationId()
        {
            if (dgvQuotations.SelectedRows.Count == 0) return null;
            return dgvQuotations.SelectedRows[0].Cells["colQuotationID"].Value?.ToString();
        }

        private void OpenDetailDialog()
        {
            string qid = SelectedQuotationId();
            if (qid == null) return;
            var q = _ctrl.GetQuotationDetail(qid);
            if (q == null)
            {
                MessageBox.Show("Quotation not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            using var dlg = new QuotationDetailForm(q);
            dlg.ShowDialog(this);
        }

        // ── Status update
        private void btnUpdateStatus_Click(object sender, EventArgs e)
        {
            if (dgvQuotations.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a quotation first.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string quotationId = dgvQuotations.SelectedRows[0].Cells["colQuotationID"].Value?.ToString();
            string newStatus   = cboNewStatus.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(newStatus))
            {
                MessageBox.Show("Please select a new status.", "No Status", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            bool ok = _ctrl.UpdateQuotationStatus(quotationId, newStatus);
            if (ok)
            {
                MessageBox.Show($"Quotation {quotationId} updated to '{newStatus}'.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshGrid();
            }
            else
                MessageBox.Show("Failed to update quotation status. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // ── Helpers
        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }
    }
}
