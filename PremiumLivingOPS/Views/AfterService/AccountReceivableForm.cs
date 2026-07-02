using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.AfterService
{
    public partial class AccountReceivableForm : Form
    {
        private readonly AfterServiceController _ctrl = new AfterServiceController();
        private List<InvoiceDetailEntity> _invoices = new List<InvoiceDetailEntity>();

        private static readonly Dictionary<string, (Color bg, Color fg)> StatusColors =
            new Dictionary<string, (Color, Color)>
            {
                { "Partial",  (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "Full",     (Color.FromArgb(220, 252, 231), Color.FromArgb( 22, 101,  52)) },
                { "Overdue",  (Color.FromArgb(254, 226, 226), Color.FromArgb(185,  28,  28)) },
            };

        private static readonly Color OverdueBg = Color.FromArgb(255, 242, 242);

        public AccountReceivableForm()
        {
            InitializeComponent();
            this.Load += AccountReceivableForm_Load;
        }

        private void AccountReceivableForm_Load(object sender, EventArgs e) => RefreshGrid();

        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to log out?",
                                "Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                SessionManager.Clear();
                Application.Restart();
            }
        }

        // ── Grid refresh
        private void RefreshGrid()
        {
            string statusSel = cboStatus.SelectedItem?.ToString();
            bool filterOverdue = string.Equals(statusSel, "Overdue", StringComparison.OrdinalIgnoreCase);
            string statusFilter = (string.IsNullOrEmpty(statusSel) || statusSel == "All" || filterOverdue)
                ? null
                : statusSel;

            string keyword = txtKeyword.Text.Trim();

            var arVm = _ctrl.GetAccountReceivableVM(statusFilter, string.IsNullOrEmpty(keyword) ? null : keyword);
            _shell.SetUser(arVm.UserBar.DisplayName, arVm.UserBar.Department);
            _shell.SetVisibleMenus(arVm.AllowedMenus);
            _shell.SetBreadcrumb("After-Service  \u203a  Account Receivable");

            _invoices = _ctrl.GetInvoiceListVM(string.IsNullOrEmpty(keyword) ? null : keyword).Invoices;

            if (filterOverdue)
                _invoices = _invoices.FindAll(i => i.IsOverdue);
            else if (!string.IsNullOrEmpty(statusFilter))
                _invoices = _invoices.FindAll(i =>
                    string.Equals(i.PaymentStatus, statusFilter, StringComparison.OrdinalIgnoreCase));

            dgvAR.Rows.Clear();
            foreach (var inv in _invoices)
                dgvAR.Rows.Add(
                    inv.InvoiceID,
                    inv.OrderID,
                    inv.InvoiceDate.ToString("yyyy-MM-dd"),
                    inv.CustomerName,
                    $"HK$ {inv.TotalAmount:N2}",
                    $"HK$ {inv.PaidAmount:N2}",
                    $"HK$ {inv.RemainingBalance:N2}",
                    inv.IsOverdue ? "Overdue" : inv.PaymentStatus,
                    inv.DueDate.ToString("yyyy-MM-dd"));

            RefreshKpi();
        }

        private void ResetSearch()
        {
            txtKeyword.Text         = string.Empty;
            cboStatus.SelectedIndex = 0;
            RefreshGrid();
        }

        private void dgvAR_SelectionChanged(object sender, EventArgs e)
        {
            bool hasRow = dgvAR.SelectedRows.Count > 0 && dgvAR.SelectedRows[0].Index >= 0;
            btnRecord.Enabled = hasRow;
        }

        // ── CellFormatting — colour Status badge + highlight overdue rows
        private void dgvAR_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _invoices.Count) return;
            var inv = _invoices[e.RowIndex];

            if (inv.IsOverdue)
            {
                e.CellStyle.BackColor          = OverdueBg;
                e.CellStyle.SelectionBackColor = Color.FromArgb(255, 220, 220);
            }

            if (dgvAR.Columns[e.ColumnIndex].Name == "colStatus" && e.Value != null)
            {
                string val = e.Value.ToString();
                if (StatusColors.TryGetValue(val, out var c))
                {
                    e.CellStyle.BackColor          = c.bg;
                    e.CellStyle.ForeColor          = c.fg;
                    e.CellStyle.SelectionBackColor = c.bg;
                    e.CellStyle.SelectionForeColor = c.fg;
                    e.CellStyle.Font               = new Font("Segoe UI", 11f, FontStyle.Bold);
                    e.CellStyle.Alignment          = DataGridViewContentAlignment.MiddleCenter;
                }
                e.FormattingApplied = true;
            }
        }

        // ── Record Payment dialog
        private void OpenRecordPayment()
        {
            if (dgvAR.SelectedRows.Count == 0) return;
            string invoiceId = dgvAR.SelectedRows[0].Cells["colInvoiceID"].Value?.ToString();
            var inv = _invoices.Find(i => i.InvoiceID == invoiceId);
            if (inv == null) return;

            using var dlg = new RecordPaymentDialog(_ctrl, inv);
            if (dlg.ShowDialog(this) == DialogResult.OK)
                RefreshGrid();
        }

        // ── KPI Pills
        private void RefreshKpi()
        {
            pnlKpi.Controls.Clear();

            var all = _ctrl.GetAccountReceivableVM().Items;

            int totalCount   = all.Count;
            int overdueCount = 0;
            int partialCount = 0;
            int fullCount    = 0;

            foreach (var i in all)
            {
                if (i.IsOverdue)                 overdueCount++;
                if (i.PaymentStatus == "Partial") partialCount++;
                if (i.PaymentStatus == "Full")    fullCount++;
            }

            var pills = new[]
            {
                ("Total Invoices", totalCount.ToString(),   Color.FromArgb( 19,  35,  61), Color.FromArgb(219, 234, 254)),
                ("Partial",        partialCount.ToString(), Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199)),
                ("Full",           fullCount.ToString(),    Color.FromArgb( 22, 101,  52), Color.FromArgb(220, 252, 231)),
                ("Overdue",        overdueCount.ToString(), Color.FromArgb(185,  28,  28), Color.FromArgb(254, 226, 226)),
            };

            var flow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent,
                Padding       = new Padding(0),
                AutoScroll    = false,
            };

            const int PillW   = 340;
            const int PillH   = 60;
            const int Gap     = 8;
            const int NumColW = 90;

            foreach (var (label, value, fg, bg) in pills)
            {
                var pill = new Panel
                {
                    BackColor = bg,
                    Size      = new Size(PillW, PillH),
                    Margin    = new Padding(0, 0, Gap, 0),
                    Cursor    = Cursors.Hand,
                };
                pill.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path  = RoundedRect(((Panel)s).ClientRectangle, 8);
                    using var brush = new SolidBrush(((Panel)s).BackColor);
                    e.Graphics.FillPath(brush, path);
                };

                var tlp = new TableLayoutPanel
                {
                    Dock            = DockStyle.Fill,
                    ColumnCount     = 2,
                    RowCount        = 1,
                    BackColor       = Color.Transparent,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding         = new Padding(10, 0, 8, 0),
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, NumColW));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                tlp.Controls.Add(new Label
                {
                    Text      = value,
                    Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                    ForeColor = fg, BackColor = Color.Transparent,
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, AutoSize = false,
                }, 0, 0);
                tlp.Controls.Add(new Label
                {
                    Text      = label,
                    Font      = new Font("Segoe UI", 12f),
                    ForeColor = fg, BackColor = Color.Transparent,
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false,
                }, 1, 0);

                pill.Controls.Add(tlp);
                flow.Controls.Add(pill);
            }

            pnlKpi.Controls.Add(flow);
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X,         r.Y,          d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y,          d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d,   0, 90);
            path.AddArc(r.X,         r.Bottom - d, d, d,  90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
