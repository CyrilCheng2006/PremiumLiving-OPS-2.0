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
    public partial class AccountPayableForm : Form
    {
        private readonly AfterServiceController _ctrl = new AfterServiceController();
        private List<AccountPayableEntity> _currentItems = new List<AccountPayableEntity>();

        private static readonly Dictionary<string, (Color bg, Color fg)> StatusColors =
            new Dictionary<string, (Color, Color)>
            {
                { "Partial",  (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "Full",     (Color.FromArgb(220, 252, 231), Color.FromArgb( 22, 101,  52)) },
                { "Overdue",  (Color.FromArgb(254, 226, 226), Color.FromArgb(185,  28,  28)) },
            };

        private static readonly Color OverdueBg = Color.FromArgb(255, 242, 242);

        public AccountPayableForm()
        {
            InitializeComponent();
            this.Load += AccountPayableForm_Load;
        }

        private void AccountPayableForm_Load(object sender, EventArgs e) => RefreshGrid();

        private void RefreshGrid()
        {
            string statusSel    = cboStatus.SelectedItem?.ToString();
            string statusFilter = (statusSel == "All" || string.IsNullOrEmpty(statusSel)) ? null : statusSel;

            var vm = _ctrl.GetAccountPayableVM(statusFilter);

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("After-Service  ›  Account Payable");

            _currentItems = vm.Items;

            dgvAP.Rows.Clear();
            foreach (var item in _currentItems)
                dgvAP.Rows.Add(
                    item.PurInvoiceID,
                    item.PurchaseID,
                    item.SupplierName,
                    $"HK$ {item.TotalAmount:N2}",
                    item.IsOverdue ? "Overdue" : item.PaymentStatus,
                    item.ExpectedDate.ToString("yyyy-MM-dd"));

            RefreshKpi();
        }

        // ── KPI Pills ────────────────────────────────────────────────────────
        private void RefreshKpi()
        {
            pnlKpi.Controls.Clear();

            var all = _ctrl.GetAccountPayableVM().Items;

            int    totalCount   = all.Count;
            double outstanding  = 0;
            int    overdueCount = 0;
            int    partialCount = 0;
            int    fullCount    = 0;

            foreach (var i in all)
            {
                if (i.PaymentStatus != "Full") outstanding += i.TotalAmount;
                if (i.IsOverdue)                 overdueCount++;
                if (i.PaymentStatus == "Partial") partialCount++;
                if (i.PaymentStatus == "Full")    fullCount++;
            }

            var pills = new[]
            {
                ("Total PO Invoices", totalCount.ToString(),   Color.FromArgb( 19,  35,  61), Color.FromArgb(219, 234, 254)),
                ("Outstanding (HK$)", $"{outstanding:N0}",     Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199)),
                ("Partial",           partialCount.ToString(), Color.FromArgb( 29,  78, 216), Color.FromArgb(219, 234, 254)),
                ("Fully Paid",        fullCount.ToString(),    Color.FromArgb( 22, 101,  52), Color.FromArgb(220, 252, 231)),
                ("Overdue",           overdueCount.ToString(), Color.FromArgb(185,  28,  28), Color.FromArgb(254, 226, 226)),
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
                    ForeColor = fg,
                    BackColor = Color.Transparent,
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize  = false,
                }, 0, 0);

                tlp.Controls.Add(new Label
                {
                    Text      = label,
                    Font      = new Font("Segoe UI", 12f),
                    ForeColor = fg,
                    BackColor = Color.Transparent,
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoSize  = false,
                }, 1, 0);

                pill.Controls.Add(tlp);
                flow.Controls.Add(pill);
            }

            pnlKpi.Controls.Add(flow);
        }

        // ── CellFormatting — status badge + overdue row highlight ────────────
        private void dgvAP_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _currentItems.Count) return;
            var item = _currentItems[e.RowIndex];

            if (item.IsOverdue)
            {
                e.CellStyle.BackColor          = OverdueBg;
                e.CellStyle.SelectionBackColor = Color.FromArgb(255, 220, 220);
            }

            if (dgvAP.Columns[e.ColumnIndex].Name == "colStatus" && e.Value != null)
            {
                string val = e.Value.ToString();
                if (StatusColors.TryGetValue(val, out var c))
                {
                    e.CellStyle.BackColor          = c.bg;
                    e.CellStyle.ForeColor          = c.fg;
                    e.CellStyle.SelectionBackColor = c.bg;
                    e.CellStyle.SelectionForeColor = c.fg;
                    e.CellStyle.Font      = new Font("Segoe UI", 11f, FontStyle.Bold);
                    e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                e.FormattingApplied = true;
            }
        }

        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        { SessionManager.Clear(); Application.Restart(); }

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
