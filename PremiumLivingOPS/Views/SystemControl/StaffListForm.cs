using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.SystemControl
{
    /// <summary>
    /// View — Staff List page (System Control module).
    ///
    /// KPI bar: Pills (left) + "Modify Detail" button (right).
    /// Modify Detail opens a 1000x600 popup with two options:
    ///   · Change Password
    ///   · Change Department
    /// </summary>
    public partial class StaffListForm : Form
    {
        private readonly SystemControlController _ctrl = new SystemControlController();
        private List<Staff> _allStaff     = new List<Staff>();
        private List<Staff> _currentStaff = new List<Staff>();
        private string      _deptFilter   = null;

        // Department colour palette — matches ViewOrderForm StatusColors style
        private static readonly (Color fg, Color bg)[] DeptColors =
        {
            (Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199)),
            (Color.FromArgb( 29,  78, 216), Color.FromArgb(219, 234, 254)),
            (Color.FromArgb(  6,  95,  70), Color.FromArgb(209, 250, 229)),
            (Color.FromArgb( 91,  33, 182), Color.FromArgb(237, 233, 254)),
            (Color.FromArgb(185,  28,  28), Color.FromArgb(254, 226, 226)),
            (Color.FromArgb( 22, 101,  52), Color.FromArgb(220, 252, 231)),
            (Color.FromArgb( 75,  85,  99), Color.FromArgb(241, 245, 249)),
        };

        private Panel pnlKpi;

        public StaffListForm()
        {
            InitializeComponent();
            this.Load += StaffListForm_Load;
        }

        // ── Load
        private void StaffListForm_Load(object sender, EventArgs e) => RefreshGrid();

        // ── Data refresh
        private void RefreshGrid()
        {
            string keyword = txtSearch.Text.Trim();
            var vm = _ctrl.GetStaffListVM(string.IsNullOrEmpty(keyword) ? null : keyword);

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("System Control  \u203a  Staff List");

            _allStaff = _ctrl.GetStaffListVM().Staffs;

            _currentStaff = string.IsNullOrEmpty(_deptFilter)
                ? vm.Staffs
                : vm.Staffs.FindAll(s => s.Department == _deptFilter);

            dgvStaff.Rows.Clear();
            foreach (var s in _currentStaff)
                dgvStaff.Rows.Add(s.StaffId, s.StaffName, s.Role, s.Department, s.Email);

            RefreshKpi();
            UpdateActionButtons();
        }

        private void ResetFilters()
        {
            txtSearch.Text = string.Empty;
            _deptFilter    = null;
            RefreshGrid();
        }

        private void UpdateActionButtons()
        {
            btnModifyDetail.Enabled = dgvStaff.SelectedRows.Count > 0;
        }

        // ── KPI strip — inline pill rendering (aligned with ViewOrderForm standard)
        private void RefreshKpi()
        {
            pnlKpi.Controls.Clear();

            // Always count from the full staff list (no active filter applied)
            var allStaff = _ctrl.GetStaffListVM().Staffs;

            int total = allStaff.Count;

            var deptGroups = allStaff
                .GroupBy(s => string.IsNullOrWhiteSpace(s.Department) ? "(Unknown)" : s.Department)
                .OrderBy(g => g.Key)
                .ToList();

            // Build pill definitions: (label, count, fg, bg, deptFilter)
            var pills = new List<(string label, string count, Color fg, Color bg, string dept)>();

            // Total Staff pill
            pills.Add(("Total Staff", total.ToString(),
                Color.FromArgb(47, 111, 237), Color.FromArgb(219, 234, 254), null));

            // One pill per department
            int colorIdx = 0;
            foreach (var grp in deptGroups)
            {
                var (fg, bg) = DeptColors[colorIdx++ % DeptColors.Length];
                pills.Add((grp.Key, grp.Count().ToString(), fg, bg, grp.Key));
            }

            var flow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent,
                Padding       = new Padding(0),
                AutoScroll    = false
            };

            const int PillW   = 290;
            const int PillH   = 60;
            const int Gap     = 8;
            const int NumColW = 80;

            foreach (var (label, count, fg, bg, dept) in pills)
            {
                // ─ Outer pill panel (rounded card)
                var pill = new Panel
                {
                    BackColor = bg,
                    Size      = new Size(PillW, PillH),
                    Margin    = new Padding(0, 0, Gap, 0),
                    Cursor    = Cursors.Hand
                };
                pill.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path  = RoundedRect(((Panel)s).ClientRectangle, 8);
                    using var brush = new SolidBrush(((Panel)s).BackColor);
                    e.Graphics.FillPath(brush, path);
                };

                // ─ Inner 2-column TableLayoutPanel
                //   Padding is horizontal only (left/right).
                //   RowStyles uses Percent 100 so the single row fills the full height,
                //   ensuring Dock=Fill labels can vertically centre their text.
                var tlp = new TableLayoutPanel
                {
                    Dock            = DockStyle.Fill,
                    ColumnCount     = 2,
                    RowCount        = 1,
                    BackColor       = Color.Transparent,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding         = new Padding(10, 0, 8, 0),  // top/bottom = 0 — keep vertical space intact
                    Margin          = new Padding(0)
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, NumColW));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
                // Explicitly set the single row to fill 100% of TLP height
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                // Col 0 — count number (large bold, vertically + horizontally centred)
                var lblCount = new Label
                {
                    Text      = count,
                    Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                    ForeColor = fg,
                    BackColor = Color.Transparent,
                    Dock      = DockStyle.Fill,   // fill the TLP cell completely
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize  = false,
                    Margin    = new Padding(0)
                };

                // Col 1 — department/category label (vertically centred, ellipsis on overflow)
                var lblName = new Label
                {
                    Text         = label,
                    Font         = new Font("Segoe UI", 12f),
                    ForeColor    = fg,
                    BackColor    = Color.Transparent,
                    Dock         = DockStyle.Fill,   // fill the TLP cell completely
                    TextAlign    = ContentAlignment.MiddleLeft,
                    AutoSize     = false,
                    AutoEllipsis = true,
                    Margin       = new Padding(0)
                };

                tlp.Controls.Add(lblCount, 0, 0);
                tlp.Controls.Add(lblName,  1, 0);

                // ─ Click handler on pill + tlp + every child label (3-layer)
                string localDept = dept;
                EventHandler clickHandler = (s, e) =>
                {
                    _deptFilter = localDept;
                    RefreshGrid();
                };
                pill.Click += clickHandler;
                tlp.Click  += clickHandler;
                foreach (Control c in tlp.Controls) c.Click += clickHandler;

                pill.Controls.Add(tlp);
                flow.Controls.Add(pill);
            }

            pnlKpi.Controls.Add(flow);
        }

        // ── Modify Detail button — opens action-selector popup
        private void btnModifyDetail_Click(object sender, EventArgs e)
        {
            if (dgvStaff.SelectedRows.Count == 0) return;
            int rowIdx = dgvStaff.SelectedRows[0].Index;
            if (rowIdx < 0 || rowIdx >= _currentStaff.Count) return;
            var staff = _currentStaff[rowIdx];

            ShowModifyDialog(staff);
        }

        // ── Modify Detail popup dialog  (1000 × 600)
        private void ShowModifyDialog(Staff staff)
        {
            bool isSelf = string.Equals(
                staff.StaffId,
                SessionManager.CurrentUser?.StaffId,
                StringComparison.OrdinalIgnoreCase);

            using var dlg = new Form
            {
                Text            = $"Modify Detail  \u2014  {staff.StaffId}",
                Size            = new Size(1000, 600),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 12f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false
            };

            var pnlHdr = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Color.FromArgb(19, 35, 61) };
            pnlHdr.Controls.Add(new Label
            {
                Text      = $"Modify Detail  \u2014  {staff.StaffName}",
                Font      = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(32, 0, 0, 0)
            });

            var pnlSub = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.FromArgb(246, 249, 255) };
            pnlSub.Paint += (s, ev) =>
            {
                using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
                ev.Graphics.DrawLine(pen, 0, ((Panel)s).Height - 1, ((Panel)s).Width, ((Panel)s).Height - 1);
            };
            pnlSub.Controls.Add(new Label
            {
                Text      = "Select an action to perform on this staff record:",
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(32, 0, 0, 0)
            });

            const int BtnW   = 500;
            const int BtnH   = 100;
            const int BtnGap = 24;

            var pnlBody = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            var btnChangePwd = new Button
            {
                Text      = "\uD83D\uDD11  Change Password",
                Font      = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(124, 58, 237),
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(BtnW, BtnH),
                Cursor    = Cursors.Hand
            };
            btnChangePwd.FlatAppearance.BorderSize         = 0;
            btnChangePwd.FlatAppearance.MouseOverBackColor = Color.FromArgb(109, 40, 217);
            btnChangePwd.FlatAppearance.MouseDownBackColor = Color.FromArgb( 91, 33, 182);
            btnChangePwd.Click += (s, ev) => { dlg.Close(); ShowChangePasswordDialog(staff); };

            var btnChangeDept = new Button
            {
                Text      = "\uD83C\uDFE2  Change Department",
                Font      = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = isSelf ? Color.FromArgb(160, 160, 160) : Color.White,
                BackColor = isSelf ? Color.FromArgb(230, 230, 230) : Color.FromArgb(234, 88, 12),
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(BtnW, BtnH),
                Enabled   = !isSelf,
                Cursor    = isSelf ? Cursors.No : Cursors.Hand
            };
            btnChangeDept.FlatAppearance.BorderSize         = 0;
            btnChangeDept.FlatAppearance.MouseOverBackColor = isSelf
                ? Color.FromArgb(230, 230, 230)
                : Color.FromArgb(194, 65, 12);
            btnChangeDept.FlatAppearance.MouseDownBackColor = Color.FromArgb(154, 52, 18);
            if (!isSelf)
                btnChangeDept.Click += (s, ev) => { dlg.Close(); ShowChangeDepartmentDialog(staff); };

            pnlBody.Layout += (s, ev) =>
            {
                int totalH = BtnH * 2 + BtnGap;
                int startX = (pnlBody.ClientSize.Width  - BtnW)   / 2;
                int startY = (pnlBody.ClientSize.Height - totalH) / 2;
                btnChangePwd.Location  = new Point(startX, startY);
                btnChangeDept.Location = new Point(startX, startY + BtnH + BtnGap);
            };

            if (isSelf)
            {
                var lblNotice = new Label
                {
                    Text      = "You cannot change your own department.",
                    Font      = new Font("Segoe UI", 10f, FontStyle.Italic),
                    ForeColor = Color.FromArgb(185, 28, 28),
                    AutoSize  = true,
                    BackColor = Color.Transparent
                };
                pnlBody.Controls.Add(lblNotice);
                pnlBody.Layout += (s, ev) =>
                {
                    int startX = (pnlBody.ClientSize.Width - BtnW) / 2;
                    int totalH = BtnH * 2 + BtnGap;
                    int startY = (pnlBody.ClientSize.Height - totalH) / 2;
                    lblNotice.Location = new Point(
                        startX + (BtnW - lblNotice.Width) / 2,
                        startY + totalH + 8);
                };
            }

            pnlBody.Controls.Add(btnChangePwd);
            pnlBody.Controls.Add(btnChangeDept);

            var pnlFtr = new Panel { Dock = DockStyle.Bottom, Height = 80, BackColor = Color.White, Padding = new Padding(0, 10, 28, 10) };
            pnlFtr.Paint += (s, ev) =>
            {
                using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
                ev.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0);
            };
            var btnCancel = new Button
            {
                Text      = "Cancel",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(210, 60),
                Dock      = DockStyle.Right,
                Cursor    = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnCancel.FlatAppearance.BorderSize         = 1;
            btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnCancel.Click += (s, ev) => dlg.Close();
            pnlFtr.Controls.Add(btnCancel);

            dlg.Controls.Add(pnlBody);
            dlg.Controls.Add(pnlSub);
            dlg.Controls.Add(pnlHdr);
            dlg.Controls.Add(pnlFtr);
            dlg.ShowDialog(this);
        }

        // ── Change Password dialog  (1000 × 600)
        private void ShowChangePasswordDialog(Staff staff)
        {
            using var dlg = new Form
            {
                Text            = $"Change Password  \u2014  {staff.StaffId}",
                Size            = new Size(1000, 600),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 12f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false
            };

            var pnlHdr = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.FromArgb(19, 35, 61) };
            pnlHdr.Controls.Add(new Label
            {
                Text      = $"Change Password  \u2014  {staff.StaffName}",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(24, 0, 0, 0)
            });

            var pnlBody = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(28, 24, 28, 8) };

            var tbl = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 1,
                RowCount        = 4,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));

            var txtNewPwd = new TextBox
            {
                Dock                 = DockStyle.Fill,
                Font                 = new Font("Segoe UI", 12f),
                BorderStyle          = BorderStyle.FixedSingle,
                UseSystemPasswordChar = true,
                PlaceholderText      = "Enter new password"
            };
            var txtConfirmPwd = new TextBox
            {
                Dock                 = DockStyle.Fill,
                Font                 = new Font("Segoe UI", 12f),
                BorderStyle          = BorderStyle.FixedSingle,
                UseSystemPasswordChar = true,
                PlaceholderText      = "Confirm new password"
            };

            tbl.Controls.Add(MakeLblKey("New Password"),     0, 0);
            tbl.Controls.Add(txtNewPwd,                      0, 1);
            tbl.Controls.Add(MakeLblKey("Confirm Password"), 0, 2);
            tbl.Controls.Add(txtConfirmPwd,                  0, 3);
            pnlBody.Controls.Add(tbl);

            var pnlFtr = new Panel { Dock = DockStyle.Bottom, Height = 80, BackColor = Color.White, Padding = new Padding(0, 10, 20, 10) };
            pnlFtr.Paint += (s, ev) =>
            {
                using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
                ev.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0);
            };

            var btnCancel = new Button
            {
                Text      = "Cancel",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(210, 60),
                Dock      = DockStyle.Right,
                Cursor    = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnCancel.FlatAppearance.BorderSize         = 1;
            btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnCancel.Click += (s, ev) => dlg.Close();

            var btnSave = new Button
            {
                Text      = "Save",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(22, 163, 74),
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(210, 60),
                Dock      = DockStyle.Right,
                Cursor    = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize         = 0;
            btnSave.FlatAppearance.MouseOverBackColor = Color.FromArgb(21, 128, 61);
            btnSave.FlatAppearance.MouseDownBackColor = Color.FromArgb(20,  83, 45);
            btnSave.Click += (s, ev) =>
            {
                if (string.IsNullOrWhiteSpace(txtNewPwd.Text))
                { MessageBox.Show("Password cannot be empty.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                if (txtNewPwd.Text != txtConfirmPwd.Text)
                { MessageBox.Show("Passwords do not match.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                bool ok = _ctrl.ChangeStaffPassword(staff.StaffId, txtNewPwd.Text);
                if (ok)
                {
                    MessageBox.Show("Password updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.Close();
                }
                else
                    MessageBox.Show("Failed to update password. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            pnlFtr.Controls.Add(btnCancel);
            pnlFtr.Controls.Add(btnSave);
            dlg.Controls.Add(pnlBody);
            dlg.Controls.Add(pnlHdr);
            dlg.Controls.Add(pnlFtr);
            dlg.ShowDialog(this);
        }

        // ── Change Department dialog  (1000 × 600)
        private void ShowChangeDepartmentDialog(Staff staff)
        {
            var departments = _allStaff
                .Select(s => s.Department)
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            using var dlg = new Form
            {
                Text            = $"Change Department  \u2014  {staff.StaffId}",
                Size            = new Size(1000, 600),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 12f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false
            };

            var pnlHdr = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.FromArgb(19, 35, 61) };
            pnlHdr.Controls.Add(new Label
            {
                Text      = $"Change Department  \u2014  {staff.StaffName}",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(24, 0, 0, 0)
            });

            var pnlBody = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(28, 24, 28, 8) };

            var tbl = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 1,
                RowCount        = 2,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));

            var cbo = new ComboBox
            {
                Dock          = DockStyle.Fill,
                Font          = new Font("Segoe UI", 12f),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle     = FlatStyle.Flat
            };
            foreach (var d in departments) cbo.Items.Add(d);
            int cur = cbo.Items.IndexOf(staff.Department);
            if (cur >= 0) cbo.SelectedIndex = cur;
            else if (cbo.Items.Count > 0) cbo.SelectedIndex = 0;

            tbl.Controls.Add(MakeLblKey("Select Department"), 0, 0);
            tbl.Controls.Add(cbo,                             0, 1);
            pnlBody.Controls.Add(tbl);

            var pnlFtr = new Panel { Dock = DockStyle.Bottom, Height = 80, BackColor = Color.White, Padding = new Padding(0, 10, 20, 10) };
            pnlFtr.Paint += (s, ev) =>
            {
                using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
                ev.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0);
            };

            var btnCancel = new Button
            {
                Text      = "Cancel",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(210, 60),
                Dock      = DockStyle.Right,
                Cursor    = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnCancel.FlatAppearance.BorderSize         = 1;
            btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnCancel.Click += (s, ev) => dlg.Close();

            var btnSave = new Button
            {
                Text      = "Save",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(22, 163, 74),
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(210, 60),
                Dock      = DockStyle.Right,
                Cursor    = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize         = 0;
            btnSave.FlatAppearance.MouseOverBackColor = Color.FromArgb(21, 128, 61);
            btnSave.FlatAppearance.MouseDownBackColor = Color.FromArgb(20,  83, 45);
            btnSave.Click += (s, ev) =>
            {
                if (cbo.SelectedItem == null) return;
                string newDept = cbo.SelectedItem.ToString();
                bool ok = _ctrl.ChangeStaffDepartment(staff.StaffId, newDept);
                if (ok)
                {
                    MessageBox.Show("Department updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.Close();
                    RefreshGrid();
                }
                else
                    MessageBox.Show("Failed to update department. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            pnlFtr.Controls.Add(btnCancel);
            pnlFtr.Controls.Add(btnSave);
            dlg.Controls.Add(pnlBody);
            dlg.Controls.Add(pnlHdr);
            dlg.Controls.Add(pnlFtr);
            dlg.ShowDialog(this);
        }

        // ── Grid events
        private void dgvStaff_SelectionChanged(object sender, EventArgs e) => UpdateActionButtons();

        private void dgvStaff_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            ShowDetailDialog(e.RowIndex);
        }

        // ── Detail dialog (double-click)
        private void ShowDetailDialog(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _currentStaff.Count) return;
            var s = _currentStaff[rowIndex];

            using var dlg = new Form
            {
                Text            = $"Staff \u2014 {s.StaffId}",
                Size            = new Size(640, 400),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 12f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false
            };

            var pnlHdr = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(19, 35, 61) };
            pnlHdr.Controls.Add(new Label
            {
                Text      = $"Staff Details  \u2014  {s.StaffId}",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(20, 0, 0, 0)
            });

            var pnlBody = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 16, 24, 8), BackColor = Color.White };
            var tbl = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 2,
                RowCount        = 5,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            for (int r = 0; r < 5; r++) tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));

            var fields = new[]
            {
                ("Staff ID",    s.StaffId),
                ("Name",        s.StaffName),
                ("Role",        s.Role),
                ("Department",  s.Department),
                ("Email",       s.Email)
            };
            for (int i = 0; i < fields.Length; i++)
            {
                tbl.Controls.Add(MakeLblKey(fields[i].Item1), 0, i);
                tbl.Controls.Add(MakeLblVal(fields[i].Item2), 1, i);
            }
            pnlBody.Controls.Add(tbl);

            var pnlFtr = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.White, Padding = new Padding(0, 8, 20, 8) };
            pnlFtr.Paint += (snd, ev) =>
            {
                using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
                ev.Graphics.DrawLine(pen, 0, 0, ((Panel)snd).Width, 0);
            };
            var btnClose = new Button
            {
                Text      = "Close",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock      = DockStyle.Right,
                Width     = 130,
                Cursor    = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnClose.Click += (snd, ev) => dlg.Close();
            pnlFtr.Controls.Add(btnClose);

            dlg.Controls.Add(pnlBody);
            dlg.Controls.Add(pnlHdr);
            dlg.Controls.Add(pnlFtr);
            dlg.ShowDialog(this);
        }

        // ── RoundedRect helper (shared by pill Paint events)
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

        // ── Label helpers
        private static Label MakeLblKey(string text) => new Label
        {
            Text      = text,
            Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(98, 112, 135),
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(0, 0, 8, 0)
        };

        private static Label MakeLblVal(string text) => new Label
        {
            Text         = text ?? "\u2014",
            Font         = new Font("Segoe UI", 12f),
            ForeColor    = Color.FromArgb(15, 31, 53),
            Dock         = DockStyle.Fill,
            TextAlign    = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        // ── Navigation & Logout
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }
    }
}
