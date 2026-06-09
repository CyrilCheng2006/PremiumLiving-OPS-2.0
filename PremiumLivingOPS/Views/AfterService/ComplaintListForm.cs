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
    public partial class ComplaintListForm : Form
    {
        private readonly AfterServiceController _ctrl = new AfterServiceController();
        private List<ComplaintEntity> _currentComplaints = new List<ComplaintEntity>();

        private static readonly Dictionary<string, (Color bg, Color fg)> StatusColors =
            new Dictionary<string, (Color, Color)>
            {
                { "Pending",    (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "Processing", (Color.FromArgb(219, 234, 254), Color.FromArgb( 29,  78, 216)) },
                { "Escalated",  (Color.FromArgb(254, 226, 226), Color.FromArgb(185,  28,  28)) },
                { "Completed",  (Color.FromArgb(220, 252, 231), Color.FromArgb( 22, 101,  52)) },
            };

        public ComplaintListForm()
        {
            InitializeComponent();
            this.Load += ComplaintListForm_Load;
        }

        private void ComplaintListForm_Load(object sender, EventArgs e) => RefreshGrid();

        // ── Refresh ─────────────────────────────────────────────────────────────
        private void RefreshGrid()
        {
            string statusSel    = cboStatus.SelectedItem?.ToString();
            string statusFilter = (statusSel == "All" || string.IsNullOrEmpty(statusSel)) ? null : statusSel;
            string keyword      = txtKeyword.Text.Trim();

            var vm = _ctrl.GetComplaintListVM(statusFilter, string.IsNullOrEmpty(keyword) ? null : keyword);

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("After-Service  ›  Complaint List");

            _currentComplaints = vm.Complaints;

            dgvComplaints.Rows.Clear();
            foreach (var c in _currentComplaints)
                dgvComplaints.Rows.Add(
                    c.ComplaintID,
                    c.OrderID ?? "—",
                    c.StaffName,
                    c.ComplaintDescription ?? "—",
                    c.ComplaintStatus);

            RefreshKpi();
            UpdateActionButtons();
        }

        private void ResetSearch()
        {
            txtKeyword.Text         = string.Empty;
            cboStatus.SelectedIndex = 0;
            RefreshGrid();
        }

        // ── KPI Pills ────────────────────────────────────────────────────────
        private void RefreshKpi()
        {
            pnlKpi.Controls.Clear();
            pnlKpi.BackColor = Color.Transparent;

            var all = _ctrl.GetComplaintListVM().Complaints;

            int total      = all.Count;
            int pending    = all.FindAll(c => c.ComplaintStatus == "Pending").Count;
            int processing = all.FindAll(c => c.ComplaintStatus == "Processing").Count;
            int escalated  = all.FindAll(c => c.ComplaintStatus == "Escalated").Count;
            int completed  = all.FindAll(c => c.ComplaintStatus == "Completed").Count;

            var pills = new[]
            {
                ("Total",      total.ToString(),      Color.FromArgb( 47, 111, 237), Color.FromArgb(219, 234, 254), "All"),
                ("Pending",    pending.ToString(),    Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199), "Pending"),
                ("Processing", processing.ToString(), Color.FromArgb( 29,  78, 216), Color.FromArgb(219, 234, 254), "Processing"),
                ("Escalated",  escalated.ToString(),  Color.FromArgb(185,  28,  28), Color.FromArgb(254, 226, 226), "Escalated"),
                ("Completed",  completed.ToString(),  Color.FromArgb( 22, 101,  52), Color.FromArgb(220, 252, 231), "Completed"),
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

            const int PillW   = 200;
            const int PillH   = 60;
            const int Gap     = 8;
            const int NumColW = 70;

            foreach (var (label, count, fg, bg, filterVal) in pills)
            {
                Color pillBg = bg;
                var pill = new Panel
                {
                    BackColor = Color.Transparent,   // Paint handles the rounded bg
                    Size      = new Size(PillW, PillH),
                    Margin    = new Padding(0, 0, Gap, 0),
                    Cursor    = Cursors.Hand,
                };

                pill.Paint += (s, e) =>
                {
                    var p = (Panel)s;
                    // 1. clear to parent bg so no square corners bleed through
                    e.Graphics.Clear(p.Parent?.BackColor ?? Color.White);
                    // 2. fill rounded rect with captured pillBg
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
                    Padding         = new Padding(10, 0, 8, 0),
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
                    Font      = new Font("Segoe UI", 12f),
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

        // ── Action buttons enable/disable ────────────────────────────────────
        private void UpdateActionButtons()
        {
            bool sel = dgvComplaints.SelectedRows.Count > 0;
            btnUpdateStatus.Enabled = sel;
            btnViewDetail.Enabled   = sel;
        }

        private void dgvComplaints_SelectionChanged(object sender, EventArgs e) => UpdateActionButtons();

        // ── CellFormatting — status badge ───────────────────────────────────
        private void dgvComplaints_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvComplaints.Columns[e.ColumnIndex].Name != "colStatus" || e.Value == null) return;
            if (!StatusColors.TryGetValue(e.Value.ToString(), out var c)) return;
            e.CellStyle.BackColor          = c.bg;
            e.CellStyle.ForeColor          = c.fg;
            e.CellStyle.SelectionBackColor = c.bg;
            e.CellStyle.SelectionForeColor = c.fg;
            e.CellStyle.Font      = new Font("Segoe UI", 11f, FontStyle.Bold);
            e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            e.FormattingApplied   = true;
        }

        // ── Update Status ──────────────────────────────────────────────────
        private void btnUpdateStatus_Click(object sender, EventArgs e)
        {
            if (dgvComplaints.SelectedRows.Count == 0) return;
            string id         = dgvComplaints.SelectedRows[0].Cells["colComplaintID"].Value?.ToString();
            string currentSts = dgvComplaints.SelectedRows[0].Cells["colStatus"].Value?.ToString();

            using var dlg = new Form
            {
                Text            = "Update Complaint Status",
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
                Text    = $"Complaint:  {id}\nCurrent Status:  {currentSts}\n\nNew Status:",
                Dock    = DockStyle.Top,
                Height  = 120,
                Padding = new Padding(20, 16, 20, 8),
                Font    = new Font("Segoe UI", 12f),
            };
            var cbo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 12f),
                Left = 20, Top = 140, Width = 400,
            };
            cbo.Items.AddRange(new object[] { "Pending", "Processing", "Escalated", "Completed" });
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
            btnOk.FlatAppearance.BorderSize   = 0;
            btnCnl.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);

            btnOk.Click += (s2, e2) =>
            {
                if (cbo.SelectedItem == null) return;
                bool ok = _ctrl.UpdateComplaintStatus(id, cbo.SelectedItem.ToString());
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

        // ── View Detail ──────────────────────────────────────────────────────
        private void btnViewDetail_Click(object sender, EventArgs e) => ShowDetailDialog();

        private void ShowDetailDialog()
        {
            if (dgvComplaints.SelectedRows.Count == 0) return;
            string id = dgvComplaints.SelectedRows[0].Cells["colComplaintID"].Value?.ToString();
            var c = _currentComplaints.Find(x => x.ComplaintID == id);
            if (c == null) return;

            StatusColors.TryGetValue(c.ComplaintStatus ?? "", out var sc);

            using var dlg = new Form
            {
                Text            = $"Complaint Detail — {c.ComplaintID}",
                Size            = new Size(680, 380),
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
            tblHdr.Controls.Add(new Label { Text = $"Complaint  —  {c.ComplaintID}", Font = new Font("Segoe UI", 15f, FontStyle.Bold), ForeColor = Color.White, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            tblHdr.Controls.Add(new Label { Text = c.ComplaintStatus, Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = sc.fg != default ? sc.fg : Color.White, BackColor = sc.bg != default ? sc.bg : Color.Gray, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter }, 1, 0);
            pnlHdr.Controls.Add(tblHdr);

            var pnlBody = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 16, 24, 16), BackColor = Color.White };
            var tbl = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 2,
                RowCount        = 4,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            for (int i = 0; i < 4; i++) tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));

            var fields = new[]
            {
                ("Complaint ID", c.ComplaintID),
                ("Order No.",    c.OrderID ?? "—"),
                ("Handled By",   c.StaffName),
                ("Description",  c.ComplaintDescription ?? "—"),
            };
            for (int i = 0; i < fields.Length; i++)
            {
                tbl.Controls.Add(new Label { Text = fields[i].Item1, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(98, 112, 135), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, i);
                tbl.Controls.Add(new Label { Text = fields[i].Item2, Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(15, 31, 53), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true }, 1, i);
            }
            pnlBody.Controls.Add(tbl);

            var pnlFtr   = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.White, Padding = new Padding(0, 8, 20, 8) };
            var btnClose = new Button
            {
                Text      = "Close",
                Dock      = DockStyle.Right,
                Width     = 120,
                Height    = 40,
                Font      = new Font("Segoe UI", 12f),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);
            btnClose.Click += (s2, e2) => dlg.Close();
            pnlFtr.Controls.Add(btnClose);

            dlg.Controls.Add(pnlBody);
            dlg.Controls.Add(pnlHdr);
            dlg.Controls.Add(pnlFtr);
            dlg.ShowDialog(this);
        }

        // ── Navigation / logout ─────────────────────────────────────────────
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
