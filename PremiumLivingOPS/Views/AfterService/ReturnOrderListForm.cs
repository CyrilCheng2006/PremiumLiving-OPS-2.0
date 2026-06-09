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
    public partial class ReturnOrderListForm : Form
    {
        private readonly AfterServiceController _ctrl = new AfterServiceController();
        private List<ReturnOrderEntity> _currentReturns = new List<ReturnOrderEntity>();

        private static readonly Dictionary<string, (Color bg, Color fg)> StatusColors =
            new Dictionary<string, (Color, Color)>
            {
                { "Pending",    (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "Approved",   (Color.FromArgb(219, 234, 254), Color.FromArgb( 29,  78, 216)) },
                { "Processing", (Color.FromArgb(237, 233, 254), Color.FromArgb( 91,  33, 182)) },
                { "Rejected",   (Color.FromArgb(254, 226, 226), Color.FromArgb(185,  28,  28)) },
                { "Completed",  (Color.FromArgb(220, 252, 231), Color.FromArgb( 22, 101,  52)) },
            };

        public ReturnOrderListForm()
        {
            InitializeComponent();
            this.Load += ReturnOrderListForm_Load;
        }

        private void ReturnOrderListForm_Load(object sender, EventArgs e) => RefreshGrid();

        private void RefreshGrid()
        {
            string statusSel = cboStatus.SelectedItem?.ToString();
            string statusFilter = (statusSel == "All" || string.IsNullOrEmpty(statusSel)) ? null : statusSel;
            string keyword = txtKeyword.Text.Trim();

            var vm = _ctrl.GetReturnOrderListVM(statusFilter, string.IsNullOrEmpty(keyword) ? null : keyword);

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("After-Service  \u203a  Return Order List");

            _currentReturns = vm.ReturnOrders;

            dgvReturns.Rows.Clear();
            foreach (var r in _currentReturns)
                dgvReturns.Rows.Add(
                    r.ReturnID,
                    r.OrderID,
                    r.CustomerName,
                    r.ReturnDate.ToString("yyyy-MM-dd"),
                    r.Reason ?? "\u2014",
                    $"HK$ {r.RefundAmount:N2}",
                    r.ReturnStatus);

            RefreshKpi();
            UpdateActionButtons();
        }

        private void ResetSearch()
        {
            txtKeyword.Text         = string.Empty;
            cboStatus.SelectedIndex = 0;
            RefreshGrid();
        }

        private void RefreshKpi()
        {
            pnlKpi.Controls.Clear();
            pnlKpi.BackColor = Color.Transparent;

            var all = _ctrl.GetReturnOrderListVM().ReturnOrders;

            int total      = all.Count;
            int pending    = all.FindAll(r => r.ReturnStatus == "Pending").Count;
            int approved   = all.FindAll(r => r.ReturnStatus == "Approved").Count;
            int processing = all.FindAll(r => r.ReturnStatus == "Processing").Count;
            int rejected   = all.FindAll(r => r.ReturnStatus == "Rejected").Count;
            int completed  = all.FindAll(r => r.ReturnStatus == "Completed").Count;

            // fg, bg — all explicit Color.FromArgb, no Palette references
            var pills = new[]
            {
                ("Total",      total.ToString(),      Color.FromArgb( 19,  35,  61), Color.FromArgb(219, 234, 254), "All"),
                ("Pending",    pending.ToString(),    Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199), "Pending"),
                ("Approved",   approved.ToString(),   Color.FromArgb( 29,  78, 216), Color.FromArgb(219, 234, 254), "Approved"),
                ("Processing", processing.ToString(), Color.FromArgb( 91,  33, 182), Color.FromArgb(237, 233, 254), "Processing"),
                ("Rejected",   rejected.ToString(),   Color.FromArgb(185,  28,  28), Color.FromArgb(254, 226, 226), "Rejected"),
                ("Completed",  completed.ToString(),  Color.FromArgb( 22, 101,  52), Color.FromArgb(220, 252, 231), "Completed"),
            };

            var flow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                AutoScroll    = false,
                BackColor     = Color.Transparent,
            };

            const int PillW  = 175;
            const int PillH  = 60;
            const int Gap    = 8;
            const int NumColW = 64;

            foreach (var (label, count, fg, bg, filterVal) in pills)
            {
                Color pillBg = bg;
                var pill = new Panel
                {
                    BackColor = Color.Transparent,   // let Paint handle the rounded bg
                    Size      = new Size(PillW, PillH),
                    Margin    = new Padding(0, 0, Gap, 0),
                    Cursor    = Cursors.Hand,
                };

                pill.Paint += (s, e) =>
                {
                    var p = (Panel)s;
                    // 1. clear to parent bg so no square corners bleed through
                    Color parentBg = p.Parent?.BackColor ?? Color.Transparent;
                    e.Graphics.Clear(parentBg);
                    // 2. fill rounded rect
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path  = RoundedRect(p.ClientRectangle, 8);
                    using var brush = new SolidBrush(pillBg);
                    e.Graphics.FillPath(brush, path);
                };

                var tlp = new TableLayoutPanel
                {
                    Dock            = DockStyle.Fill,
                    ColumnCount     = 2,
                    RowCount        = 1,
                    BackColor       = Color.Transparent,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding         = new Padding(8, 0, 6, 0),
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, NumColW));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                tlp.Controls.Add(new Label
                {
                    Text      = count,
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
                    Font      = new Font("Segoe UI", 11f),
                    ForeColor = fg,
                    BackColor = Color.Transparent,
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoSize  = false,
                }, 1, 0);

                string localFilter = filterVal;
                EventHandler click = (s, e) =>
                {
                    int idx = cboStatus.FindStringExact(localFilter);
                    if (idx >= 0) cboStatus.SelectedIndex = idx;
                    RefreshGrid();
                };
                pill.Click += click;
                tlp.Click  += click;
                foreach (Control c in tlp.Controls) c.Click += click;

                pill.Controls.Add(tlp);
                flow.Controls.Add(pill);
            }

            pnlKpi.Controls.Add(flow);
        }

        private void UpdateActionButtons()
        {
            bool sel = dgvReturns.SelectedRows.Count > 0;
            btnUpdateStatus.Enabled = sel;
            btnViewDetail.Enabled   = sel;
        }

        private void dgvReturns_SelectionChanged(object sender, EventArgs e) => UpdateActionButtons();

        private void dgvReturns_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvReturns.Columns[e.ColumnIndex].Name != "colStatus" || e.Value == null) return;
            if (!StatusColors.TryGetValue(e.Value.ToString(), out var c)) return;
            e.CellStyle.BackColor          = c.bg;
            e.CellStyle.ForeColor          = c.fg;
            e.CellStyle.SelectionBackColor = c.bg;
            e.CellStyle.SelectionForeColor = c.fg;
            e.CellStyle.Font      = new Font("Segoe UI", 11f, FontStyle.Bold);
            e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            e.FormattingApplied   = true;
        }

        private void btnUpdateStatus_Click(object sender, EventArgs e)
        {
            if (dgvReturns.SelectedRows.Count == 0) return;
            string id         = dgvReturns.SelectedRows[0].Cells["colReturnID"].Value?.ToString();
            string currentSts = dgvReturns.SelectedRows[0].Cells["colStatus"].Value?.ToString();

            using var dlg = new Form
            {
                Text            = "Update Return Order Status",
                Size            = new Size(460, 260),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 12f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false,
            };
            var lbl = new Label
            {
                Text    = $"Return ID:  {id}\nCurrent:  {currentSts}\n\nNew Status:",
                Dock    = DockStyle.Top,
                Height  = 120,
                Padding = new Padding(20, 16, 20, 8),
            };
            var cbo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 12f),
                Left = 20, Top = 140, Width = 400,
            };
            cbo.Items.AddRange(new object[] { "Pending", "Approved", "Processing", "Rejected", "Completed" });
            cbo.SelectedItem = currentSts;

            var btnOk = new Button
            {
                Text      = "Confirm",
                Left      = 20, Top = 185, Width = 190, Height = 40,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                BackColor = Color.FromArgb(19, 35, 61),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
            };
            var btnCnl = new Button
            {
                Text      = "Cancel",
                Left      = 220, Top = 185, Width = 190, Height = 40,
                Font      = new Font("Segoe UI", 12f),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
            };
            btnOk.FlatAppearance.BorderSize    = 0;
            btnCnl.FlatAppearance.BorderColor  = Color.FromArgb(209, 213, 219);

            btnOk.Click += (s2, e2) =>
            {
                if (cbo.SelectedItem == null) return;
                bool ok = _ctrl.UpdateReturnOrderStatus(id, cbo.SelectedItem.ToString());
                if (ok) { dlg.DialogResult = DialogResult.OK; dlg.Close(); }
                else MessageBox.Show("Update failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            btnCnl.Click += (s2, e2) => dlg.Close();

            dlg.Controls.Add(lbl);
            dlg.Controls.Add(cbo);
            dlg.Controls.Add(btnOk);
            dlg.Controls.Add(btnCnl);
            if (dlg.ShowDialog(this) == DialogResult.OK) RefreshGrid();
        }

        private void btnViewDetail_Click(object sender, EventArgs e) => ShowDetailDialog();

        private void ShowDetailDialog()
        {
            if (dgvReturns.SelectedRows.Count == 0) return;
            string id = dgvReturns.SelectedRows[0].Cells["colReturnID"].Value?.ToString();
            var r = _currentReturns.Find(x => x.ReturnID == id);
            if (r == null) return;

            StatusColors.TryGetValue(r.ReturnStatus ?? "", out var sc);
            using var dlg = new Form
            {
                Text            = $"Return Order Detail \u2014 {r.ReturnID}",
                Size            = new Size(680, 400),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 12f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false,
            };

            var pnlHdr = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Color.FromArgb(19, 35, 61) };
            var tblHdr = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2, RowCount = 1,
                BackColor   = Color.Transparent,
                Padding     = new Padding(20, 0, 20, 0),
            };
            tblHdr.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblHdr.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140f));
            tblHdr.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblHdr.Controls.Add(new Label { Text = $"Return Order  \u2014  {r.ReturnID}", Font = new Font("Segoe UI", 15f, FontStyle.Bold), ForeColor = Color.White, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            tblHdr.Controls.Add(new Label { Text = r.ReturnStatus, Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = sc.fg != default ? sc.fg : Color.White, BackColor = sc.bg != default ? sc.bg : Color.Gray, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter }, 1, 0);
            pnlHdr.Controls.Add(tblHdr);

            var pnlBody = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 16, 24, 16) };
            var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5, BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            for (int i = 0; i < 5; i++) tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));
            var fields = new[]
            {
                ("Return ID",     r.ReturnID),
                ("Order No.",     r.OrderID),
                ("Customer",      r.CustomerName),
                ("Return Date",   r.ReturnDate.ToString("yyyy-MM-dd")),
                ("Refund Amount", $"HK$ {r.RefundAmount:N2}"),
            };
            for (int i = 0; i < fields.Length; i++)
            {
                tbl.Controls.Add(new Label { Text = fields[i].Item1, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(107, 114, 128), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, i);
                tbl.Controls.Add(new Label { Text = fields[i].Item2, Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(17, 24, 39), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 1, i);
            }
            pnlBody.Controls.Add(tbl);

            var pnlReason = new Panel { Dock = DockStyle.Bottom, Height = 80, BackColor = Color.FromArgb(246, 249, 255), Padding = new Padding(24, 8, 24, 8) };
            pnlReason.Paint += (s, e2) => { using var pen = new Pen(Color.FromArgb(209, 213, 219), 1); e2.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0); };
            pnlReason.Controls.Add(new Label { Text = $"Reason:  {r.Reason ?? "\u2014"}", Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(17, 24, 39), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft });

            var pnlFtr    = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.White, Padding = new Padding(0, 8, 20, 8) };
            var btnClose  = new Button { Text = "Close", Dock = DockStyle.Right, Width = 120, Height = 40, Font = new Font("Segoe UI", 12f), BackColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);
            btnClose.Click += (s2, e2) => dlg.Close();
            pnlFtr.Controls.Add(btnClose);

            dlg.Controls.Add(pnlBody);
            dlg.Controls.Add(pnlReason);
            dlg.Controls.Add(pnlHdr);
            dlg.Controls.Add(pnlFtr);
            dlg.ShowDialog(this);
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
