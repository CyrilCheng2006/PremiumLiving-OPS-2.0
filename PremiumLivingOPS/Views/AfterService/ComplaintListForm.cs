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
                { "Completed",  (Color.FromArgb(220, 252, 231), Color.FromArgb( 22, 101,  52)) }
            };

        public ComplaintListForm()
        {
            InitializeComponent();
            this.Load += ComplaintListForm_Load;
        }

        // ── Load ──────────────────────────────────────────────────────────
        private void ComplaintListForm_Load(object sender, EventArgs e)
        {
            RefreshGrid();
        }

        // ── Refresh grid ──────────────────────────────────────────────────
        private void RefreshGrid()
        {
            string keyword = txtKeyword.Text.Trim();
            string status  = cboStatus.SelectedItem?.ToString();
            if (status == "All" || string.IsNullOrEmpty(status)) status = null;

            var vm = _ctrl.GetComplaintListVM(status, keyword);

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("After-Service  ›  Complaints");

            _currentComplaints = vm.Complaints;

            dgvComplaints.Rows.Clear();
            foreach (var c in _currentComplaints)
                dgvComplaints.Rows.Add(
                    c.ComplaintID,
                    c.OrderID,
                    c.StaffName,
                    c.ComplaintDescription,
                    c.ComplaintStatus);

            RefreshKpi(vm.Complaints);
            UpdateButtons();
        }

        // ── KPI pills ─────────────────────────────────────────────────────
        private void RefreshKpi(List<ComplaintEntity> all)
        {
            pnlKpi.Controls.Clear();

            var allFull = _ctrl.GetComplaintListVM().Complaints;

            int total      = allFull.Count;
            int pending    = allFull.FindAll(c => c.ComplaintStatus == "Pending").Count;
            int processing = allFull.FindAll(c => c.ComplaintStatus == "Processing").Count;
            int escalated  = allFull.FindAll(c => c.ComplaintStatus == "Escalated").Count;
            int completed  = allFull.FindAll(c => c.ComplaintStatus == "Completed").Count;

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
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent,
                Padding = new Padding(0), AutoScroll = false
            };

            const int PillW = 260, PillH = 60, Gap = 8;
            foreach (var (label, count, fg, bg, filterVal) in pills)
            {
                var pill = new Panel
                {
                    BackColor = bg, Size = new Size(PillW, PillH),
                    Margin = new Padding(0, 0, Gap, 0), Cursor = Cursors.Hand
                };

                var tlp = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                    BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding = new Padding(10, 0, 8, 0)
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70f));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                tlp.Controls.Add(new Label { Text = count, Font = new Font("Segoe UI", 14f, FontStyle.Bold), ForeColor = fg, BackColor = Color.Transparent, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, AutoSize = false }, 0, 0);
                tlp.Controls.Add(new Label { Text = label, Font = new Font("Segoe UI", 11f), ForeColor = fg, BackColor = Color.Transparent, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false }, 1, 0);

                string localFilter = filterVal;
                EventHandler click = (s, e) =>
                {
                    int idx = cboStatus.FindStringExact(localFilter);
                    if (idx >= 0) cboStatus.SelectedIndex = idx;
                    RefreshGrid();
                };
                pill.Click += click; tlp.Click += click;
                foreach (Control c in tlp.Controls) c.Click += click;
                pill.Controls.Add(tlp);
                flow.Controls.Add(pill);
            }
            pnlKpi.Controls.Add(flow);
        }

        // ── Button state ──────────────────────────────────────────────────
        private void UpdateButtons()
        {
            bool sel = dgvComplaints.SelectedRows.Count > 0;
            btnUpdateStatus.Enabled = sel;
            btnViewDetail.Enabled   = sel;
        }

        private void dgvComplaints_SelectionChanged(object sender, EventArgs e) => UpdateButtons();

        // ── CellFormatting: status badge ─────────────────────────────────
        private void dgvComplaints_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvComplaints.Columns[e.ColumnIndex].Name != "colStatus" || e.Value == null) return;
            string val = e.Value.ToString();
            e.FormattingApplied = true;
            if (StatusColors.TryGetValue(val, out var colors))
            {
                e.CellStyle.ForeColor = colors.fg; e.CellStyle.BackColor = colors.bg;
                e.CellStyle.SelectionForeColor = colors.fg; e.CellStyle.SelectionBackColor = colors.bg;
                e.CellStyle.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }

        // ── Get selected entity ──────────────────────────────────────────
        private ComplaintEntity GetSelectedComplaint()
        {
            if (dgvComplaints.SelectedRows.Count == 0) return null;
            int idx = dgvComplaints.SelectedRows[0].Index;
            return (idx >= 0 && idx < _currentComplaints.Count) ? _currentComplaints[idx] : null;
        }

        // ── Update Status dialog ─────────────────────────────────────────
        private void btnUpdateStatus_Click(object sender, EventArgs e)
        {
            var complaint = GetSelectedComplaint();
            if (complaint == null) return;

            using var dlg = new Form
            {
                Text = $"Update Status — {complaint.ComplaintID}",
                Size = new Size(420, 220), StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.White, FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false, Font = new Font("Segoe UI", 12f)
            };

            var lbl = new Label { Text = "New Status:", Dock = DockStyle.Top, Height = 32, TextAlign = ContentAlignment.BottomLeft, Padding = new Padding(16, 0, 0, 2), Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(98, 112, 135) };
            var cbo = new ComboBox { Dock = DockStyle.Top, Height = 44, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f), Margin = new Padding(16, 0, 16, 0) };
            cbo.Items.AddRange(new object[] { "Pending", "Processing", "Escalated", "Completed" });
            int ci = cbo.FindStringExact(complaint.ComplaintStatus);
            cbo.SelectedIndex = ci >= 0 ? ci : 0;

            var pnlFoot = new Panel { Dock = DockStyle.Bottom, Height = 64, BackColor = Color.FromArgb(246, 249, 255), Padding = new Padding(16, 10, 16, 10) };
            var btnOk     = new Button { Text = "Update", Dock = DockStyle.Right, Width = 130, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(47, 111, 237), ForeColor = Color.White, Font = new Font("Segoe UI", 11f, FontStyle.Bold), Cursor = Cursors.Hand };
            var btnCancel = new Button { Text = "Cancel", Dock = DockStyle.Right, Width = 110, FlatStyle = FlatStyle.Flat, BackColor = Color.White, ForeColor = Color.FromArgb(15, 31, 53), Font = new Font("Segoe UI", 11f), Cursor = Cursors.Hand, Margin = new Padding(0, 0, 8, 0) };
            btnOk.FlatAppearance.BorderSize = 0;
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            btnCancel.Click += (s, ev) => dlg.Close();
            btnOk.Click += (s, ev) =>
            {
                string newStatus = cbo.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(newStatus)) return;
                bool ok = _ctrl.UpdateComplaintStatus(complaint.ComplaintID, newStatus);
                if (ok)
                {
                    MessageBox.Show($"Status updated to {newStatus}.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.Close();
                    RefreshGrid();
                }
                else
                    MessageBox.Show("Failed to update status.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            pnlFoot.Controls.Add(btnOk);
            pnlFoot.Controls.Add(btnCancel);

            var pnlPad = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 8, 16, 0) };
            pnlPad.Controls.Add(cbo);
            dlg.Controls.Add(pnlPad);
            dlg.Controls.Add(lbl);
            dlg.Controls.Add(pnlFoot);
            dlg.ShowDialog(this);
        }

        // ── View Detail dialog ────────────────────────────────────────────
        private void btnViewDetail_Click(object sender, EventArgs e)
        {
            var complaint = GetSelectedComplaint();
            if (complaint == null) return;

            using var dlg = new Form
            {
                Text = $"Complaint Detail — {complaint.ComplaintID}",
                Size = new Size(640, 380), StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.White, FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false, Font = new Font("Segoe UI", 12f)
            };

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5,
                BackColor = Color.White, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(28, 20, 28, 20)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            for (int i = 0; i < 5; i++) tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));

            var fields = new[]
            {
                ("Complaint ID",  complaint.ComplaintID),
                ("Order ID",      complaint.OrderID),
                ("Staff",         complaint.StaffName),
                ("Status",        complaint.ComplaintStatus),
                ("Description",   complaint.ComplaintDescription)
            };
            for (int i = 0; i < fields.Length; i++)
            {
                tbl.Controls.Add(new Label { Text = fields[i].Item1, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(98, 112, 135), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, i);
                tbl.Controls.Add(new Label { Text = fields[i].Item2, Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(15, 31, 53), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true }, 1, i);
            }

            var pnlFoot = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.FromArgb(246, 249, 255), Padding = new Padding(16, 10, 16, 10) };
            var btnClose = new Button { Text = "Close", Dock = DockStyle.Right, Width = 120, FlatStyle = FlatStyle.Flat, BackColor = Color.White, ForeColor = Color.FromArgb(15, 31, 53), Cursor = Cursors.Hand };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            btnClose.Click += (s, ev) => dlg.Close();
            pnlFoot.Controls.Add(btnClose);

            dlg.Controls.Add(tbl);
            dlg.Controls.Add(pnlFoot);
            dlg.ShowDialog(this);
        }

        // ── Navigation / Logout ───────────────────────────────────────────
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }
    }
}
