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
    /// KPI bar: Pills (left) | Add Staff (green) | Modify Detail (blue).
    /// </summary>
    public partial class StaffListForm : Form
    {
        private readonly SystemControlController _ctrl = new SystemControlController();
        private List<Staff> _allStaff     = new List<Staff>();
        private List<Staff> _currentStaff = new List<Staff>();
        private string      _deptFilter   = null;

        // Department colour palette — mirrors ViewOrderForm StatusColors
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

        // ── KPI strip — structure 1:1 identical to ViewOrderForm.RefreshKpi()
        private void RefreshKpi()
        {
            pnlKpi.Controls.Clear();

            var allStaff = _ctrl.GetStaffListVM().Staffs;
            int total    = allStaff.Count;

            var deptGroups = allStaff
                .GroupBy(s => string.IsNullOrWhiteSpace(s.Department) ? "(Unknown)" : s.Department)
                .OrderBy(g => g.Key)
                .ToList();

            // Build pill list: (label, count, fg, bg, dept)
            var pillDefs = new List<(string label, string count, Color fg, Color bg, string dept)>();
            pillDefs.Add(("Total Staff", total.ToString(), Color.FromArgb(47, 111, 237), Color.FromArgb(219, 234, 254), null));
            int ci = 0;
            foreach (var grp in deptGroups)
            {
                var (fg, bg) = DeptColors[ci++ % DeptColors.Length];
                pillDefs.Add((grp.Key, grp.Count().ToString(), fg, bg, grp.Key));
            }

            // FlowLayoutPanel — identical to ViewOrderForm
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent,
                Padding = new Padding(0), AutoScroll = false
            };

            const int PillW   = 290;
            const int PillH   = 60;
            const int Gap     = 8;
            const int NumColW = 80;

            foreach (var (label, count, fg, bg, dept) in pillDefs)
            {
                // Outer pill panel — identical to ViewOrderForm
                var pill = new Panel { BackColor = bg, Size = new Size(PillW, PillH), Margin = new Padding(0, 0, Gap, 0), Cursor = Cursors.Hand };
                pill.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path  = RoundedRect(((Panel)s).ClientRectangle, 8);
                    using var brush = new SolidBrush(((Panel)s).BackColor);
                    e.Graphics.FillPath(brush, path);
                };

                // Inner TableLayoutPanel — identical to ViewOrderForm
                var tlp = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                    BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding = new Padding(10, 0, 8, 0)
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, NumColW));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                // Labels — identical to ViewOrderForm
                tlp.Controls.Add(new Label { Text = count, Font = new Font("Segoe UI", 14f, FontStyle.Bold), ForeColor = fg, BackColor = Color.Transparent, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, AutoSize = false }, 0, 0);
                tlp.Controls.Add(new Label { Text = label, Font = new Font("Segoe UI", 12f),                ForeColor = fg, BackColor = Color.Transparent, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,   AutoSize = false, AutoEllipsis = true }, 1, 0);

                // Click handler on pill + tlp + every child label — identical to ViewOrderForm
                string localDept = dept;
                EventHandler clickHandler = (s, e) =>
                {
                    _deptFilter = localDept;
                    RefreshGrid();
                };
                pill.Click += clickHandler; tlp.Click += clickHandler;
                foreach (Control c in tlp.Controls) c.Click += clickHandler;

                pill.Controls.Add(tlp);
                flow.Controls.Add(pill);
            }

            pnlKpi.Controls.Add(flow);
        }

        // ── Add Staff button
        private void btnAddStaff_Click(object sender, EventArgs e) => ShowAddStaffDialog();

        // ── Add Staff dialog  (1000 × 680)
        private void ShowAddStaffDialog()
        {
            // Auto-generate next Staff ID from DB
            string nextId;
            try   { nextId = _ctrl.GetNextStaffId(); }
            catch { nextId = "S-???"; }

            using var dlg = new Form
            {
                Text            = "Add New Staff",
                Size            = new Size(1000, 680),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 12f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false
            };

            // Header
            var pnlHdr = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.FromArgb(19, 35, 61) };
            pnlHdr.Controls.Add(new Label
            {
                Text      = "Add New Staff",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(24, 0, 0, 0)
            });

            // Body
            var pnlBody = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(28, 20, 28, 8) };

            // Row heights: label = 28, input = 52 (original). 1.5x spacing means
            // we add a 26px gap row between each label+input pair.
            // Layout (7 rows):
            //   row 0 — label  28px   Staff ID
            //   row 1 — input  52px
            //   row 2 — gap    26px
            //   row 3 — label  28px   Full Name / Role / Department (2-col)
            //   row 4 — input  52px
            //   row 5 — gap    26px
            //   row 6 — label  28px   Email
            //   row 7 — input  52px

            var tbl = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 2,
                RowCount        = 8,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            // Row heights
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));  // 0 label
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));  // 1 input
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));  // 2 gap
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));  // 3 label
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));  // 4 input
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));  // 5 gap
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));  // 6 label
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));  // 7 input

            // ── Row 0/1: Staff ID (read-only, auto) | Full Name
            var txtStaffId = new TextBox
            {
                Dock          = DockStyle.Fill,
                Font          = new Font("Segoe UI", 12f),
                BorderStyle   = BorderStyle.FixedSingle,
                Text          = nextId,
                ReadOnly      = true,
                BackColor     = Color.FromArgb(240, 244, 249),   // grey-tint — signals read-only
                ForeColor     = Color.FromArgb(98, 112, 135)
            };
            var txtName = MakeTextInput("Full name");

            tbl.Controls.Add(MakeLblKey("Staff ID (auto-generated)"), 0, 0);
            tbl.Controls.Add(txtStaffId,                              0, 1);
            tbl.Controls.Add(MakeLblKey("Full Name *"),               1, 0);
            tbl.Controls.Add(txtName,                                 1, 1);

            // Row 2: gap (empty)

            // ── Row 3/4: Role | Department
            var cboRole = new ComboBox
            {
                Dock          = DockStyle.Fill,
                Font          = new Font("Segoe UI", 12f),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle     = FlatStyle.Flat
            };
            foreach (var r in new[] { "Admin", "Sales", "Warehouse", "Manager", "Accountant" })
                cboRole.Items.Add(r);
            if (cboRole.Items.Count > 0) cboRole.SelectedIndex = 0;

            var cboDept = new ComboBox
            {
                Dock          = DockStyle.Fill,
                Font          = new Font("Segoe UI", 12f),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle     = FlatStyle.Flat
            };
            var depts = _allStaff.Select(s => s.Department)
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Distinct().OrderBy(d => d).ToList();
            foreach (var d in depts) cboDept.Items.Add(d);
            if (cboDept.Items.Count > 0) cboDept.SelectedIndex = 0;

            tbl.Controls.Add(MakeLblKey("Role *"),       0, 3);
            tbl.Controls.Add(cboRole,                    0, 4);
            tbl.Controls.Add(MakeLblKey("Department *"), 1, 3);
            tbl.Controls.Add(cboDept,                    1, 4);

            // Row 5: gap (empty)

            // ── Row 6/7: Email (full-width)
            // Email field: user types the local part; @plf.com is shown as a
            // suffix label docked to the right of a Panel that holds both.
            var lblEmailKey = MakeLblKey("Email * (local part only — @plf.com will be appended)");
            tbl.Controls.Add(lblEmailKey, 0, 6);
            tbl.SetColumnSpan(lblEmailKey, 2);

            // Email input panel: [TextBox ............] [@plf.com]
            var pnlEmail = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            var lblSuffix = new Label
            {
                Text      = "@plf.com",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(98, 112, 135),
                BackColor = Color.FromArgb(240, 244, 249),
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock      = DockStyle.Right,
                Width     = 110,
                AutoSize  = false
            };

            var txtEmailLocal = new TextBox
            {
                Dock        = DockStyle.Fill,
                Font        = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "e.g. john.doe"
            };

            pnlEmail.Controls.Add(txtEmailLocal);
            pnlEmail.Controls.Add(lblSuffix);

            tbl.Controls.Add(pnlEmail, 0, 7);
            tbl.SetColumnSpan(pnlEmail, 2);

            pnlBody.Controls.Add(tbl);

            // Footer
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
                string id        = txtStaffId.Text.Trim();          // always valid (auto-gen)
                string name      = txtName.Text.Trim();
                string localPart = txtEmailLocal.Text.Trim();
                string email     = string.IsNullOrEmpty(localPart) ? "" : localPart + "@plf.com";
                string role      = cboRole.SelectedItem?.ToString();
                string dept      = cboDept.SelectedItem?.ToString();

                if (string.IsNullOrEmpty(name))
                { MessageBox.Show("Full Name is required.",  "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                if (string.IsNullOrEmpty(localPart))
                { MessageBox.Show("Email local part is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                if (string.IsNullOrEmpty(role))
                { MessageBox.Show("Role is required.",       "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                if (string.IsNullOrEmpty(dept))
                { MessageBox.Show("Department is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                bool ok = _ctrl.AddStaff(new Staff
                {
                    StaffId    = id,
                    StaffName  = name,
                    Email      = email,
                    Role       = role,
                    Department = dept
                });

                if (ok)
                {
                    MessageBox.Show($"Staff {id} added successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.Close();
                    RefreshGrid();
                }
                else
                    MessageBox.Show("Failed to add staff. The Staff ID may already exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            pnlFtr.Controls.Add(btnCancel);
            pnlFtr.Controls.Add(btnSave);
            dlg.Controls.Add(pnlBody);
            dlg.Controls.Add(pnlHdr);
            dlg.Controls.Add(pnlFtr);
            dlg.ShowDialog(this);
        }

        // ── Modify Detail button
        private void btnModifyDetail_Click(object sender, EventArgs e)
        {
            if (dgvStaff.SelectedRows.Count == 0) return;
            int rowIdx = dgvStaff.SelectedRows[0].Index;
            if (rowIdx < 0 || rowIdx >= _currentStaff.Count) return;
            ShowModifyDialog(_currentStaff[rowIdx]);
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
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(32, 0, 0, 0)
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
                ForeColor = Color.FromArgb(98, 112, 135), Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(32, 0, 0, 0)
            });

            const int BtnW   = 500;
            const int BtnH   = 100;
            const int BtnGap = 24;

            var pnlBody = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            var btnChangePwd = new Button
            {
                Text = "\uD83D\uDD11  Change Password", Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(124, 58, 237),
                FlatStyle = FlatStyle.Flat, Size = new Size(BtnW, BtnH), Cursor = Cursors.Hand
            };
            btnChangePwd.FlatAppearance.BorderSize         = 0;
            btnChangePwd.FlatAppearance.MouseOverBackColor = Color.FromArgb(109, 40, 217);
            btnChangePwd.FlatAppearance.MouseDownBackColor = Color.FromArgb( 91, 33, 182);
            btnChangePwd.Click += (s, ev) => { dlg.Close(); ShowChangePasswordDialog(staff); };

            var btnChangeDept = new Button
            {
                Text = "\uD83C\uDFE2  Change Department", Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = isSelf ? Color.FromArgb(160, 160, 160) : Color.White,
                BackColor = isSelf ? Color.FromArgb(230, 230, 230) : Color.FromArgb(234, 88, 12),
                FlatStyle = FlatStyle.Flat, Size = new Size(BtnW, BtnH),
                Enabled = !isSelf, Cursor = isSelf ? Cursors.No : Cursors.Hand
            };
            btnChangeDept.FlatAppearance.BorderSize         = 0;
            btnChangeDept.FlatAppearance.MouseOverBackColor = isSelf ? Color.FromArgb(230, 230, 230) : Color.FromArgb(194, 65, 12);
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
                    Text = "You cannot change your own department.",
                    Font = new Font("Segoe UI", 10f, FontStyle.Italic),
                    ForeColor = Color.FromArgb(185, 28, 28), AutoSize = true, BackColor = Color.Transparent
                };
                pnlBody.Controls.Add(lblNotice);
                pnlBody.Layout += (s, ev) =>
                {
                    int startX = (pnlBody.ClientSize.Width - BtnW) / 2;
                    int totalH = BtnH * 2 + BtnGap;
                    int startY = (pnlBody.ClientSize.Height - totalH) / 2;
                    lblNotice.Location = new Point(startX + (BtnW - lblNotice.Width) / 2, startY + totalH + 8);
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
                Text = "Cancel", Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat, Size = new Size(210, 60), Dock = DockStyle.Right, Cursor = Cursors.Hand
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
                Text = $"Change Password  \u2014  {staff.StaffId}", Size = new Size(1000, 600),
                StartPosition = FormStartPosition.CenterParent, BackColor = Color.White,
                Font = new Font("Segoe UI", 12f), FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false
            };
            var pnlHdr = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.FromArgb(19, 35, 61) };
            pnlHdr.Controls.Add(new Label { Text = $"Change Password  \u2014  {staff.StaffName}", Font = new Font("Segoe UI", 14f, FontStyle.Bold), ForeColor = Color.White, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(24, 0, 0, 0) });

            var pnlBody = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(28, 24, 28, 8) };
            var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));

            var txtNewPwd     = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle, UseSystemPasswordChar = true, PlaceholderText = "Enter new password" };
            var txtConfirmPwd = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle, UseSystemPasswordChar = true, PlaceholderText = "Confirm new password" };
            tbl.Controls.Add(MakeLblKey("New Password"),     0, 0);
            tbl.Controls.Add(txtNewPwd,                      0, 1);
            tbl.Controls.Add(MakeLblKey("Confirm Password"), 0, 2);
            tbl.Controls.Add(txtConfirmPwd,                  0, 3);
            pnlBody.Controls.Add(tbl);

            var pnlFtr = new Panel { Dock = DockStyle.Bottom, Height = 80, BackColor = Color.White, Padding = new Padding(0, 10, 20, 10) };
            pnlFtr.Paint += (s, ev) => { using var pen = new Pen(Color.FromArgb(221, 227, 236), 1); ev.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0); };
            var btnCancel = new Button { Text = "Cancel", Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White, FlatStyle = FlatStyle.Flat, Size = new Size(210, 60), Dock = DockStyle.Right, Cursor = Cursors.Hand };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236); btnCancel.FlatAppearance.BorderSize = 1; btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnCancel.Click += (s, ev) => dlg.Close();
            var btnSave = new Button { Text = "Save", Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(22, 163, 74), FlatStyle = FlatStyle.Flat, Size = new Size(210, 60), Dock = DockStyle.Right, Cursor = Cursors.Hand };
            btnSave.FlatAppearance.BorderSize = 0; btnSave.FlatAppearance.MouseOverBackColor = Color.FromArgb(21, 128, 61); btnSave.FlatAppearance.MouseDownBackColor = Color.FromArgb(20, 83, 45);
            btnSave.Click += (s, ev) =>
            {
                if (string.IsNullOrWhiteSpace(txtNewPwd.Text)) { MessageBox.Show("Password cannot be empty.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                if (txtNewPwd.Text != txtConfirmPwd.Text)       { MessageBox.Show("Passwords do not match.",   "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                bool ok = _ctrl.ChangeStaffPassword(staff.StaffId, txtNewPwd.Text);
                if (ok) { MessageBox.Show("Password updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information); dlg.Close(); }
                else      MessageBox.Show("Failed to update password. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            pnlFtr.Controls.Add(btnCancel); pnlFtr.Controls.Add(btnSave);
            dlg.Controls.Add(pnlBody); dlg.Controls.Add(pnlHdr); dlg.Controls.Add(pnlFtr);
            dlg.ShowDialog(this);
        }

        // ── Change Department dialog  (1000 × 600)
        private void ShowChangeDepartmentDialog(Staff staff)
        {
            var departments = _allStaff.Select(s => s.Department).Where(d => !string.IsNullOrWhiteSpace(d)).Distinct().OrderBy(d => d).ToList();
            using var dlg = new Form { Text = $"Change Department  \u2014  {staff.StaffId}", Size = new Size(1000, 600), StartPosition = FormStartPosition.CenterParent, BackColor = Color.White, Font = new Font("Segoe UI", 12f), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            var pnlHdr = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.FromArgb(19, 35, 61) };
            pnlHdr.Controls.Add(new Label { Text = $"Change Department  \u2014  {staff.StaffName}", Font = new Font("Segoe UI", 14f, FontStyle.Bold), ForeColor = Color.White, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(24, 0, 0, 0) });

            var pnlBody = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(28, 24, 28, 8) };
            var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
            var cbo = new ComboBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12f), DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
            foreach (var d in departments) cbo.Items.Add(d);
            int cur = cbo.Items.IndexOf(staff.Department);
            if (cur >= 0) cbo.SelectedIndex = cur; else if (cbo.Items.Count > 0) cbo.SelectedIndex = 0;
            tbl.Controls.Add(MakeLblKey("Select Department"), 0, 0);
            tbl.Controls.Add(cbo, 0, 1);
            pnlBody.Controls.Add(tbl);

            var pnlFtr = new Panel { Dock = DockStyle.Bottom, Height = 80, BackColor = Color.White, Padding = new Padding(0, 10, 20, 10) };
            pnlFtr.Paint += (s, ev) => { using var pen = new Pen(Color.FromArgb(221, 227, 236), 1); ev.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0); };
            var btnCancel = new Button { Text = "Cancel", Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White, FlatStyle = FlatStyle.Flat, Size = new Size(210, 60), Dock = DockStyle.Right, Cursor = Cursors.Hand };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236); btnCancel.FlatAppearance.BorderSize = 1; btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnCancel.Click += (s, ev) => dlg.Close();
            var btnSave = new Button { Text = "Save", Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(22, 163, 74), FlatStyle = FlatStyle.Flat, Size = new Size(210, 60), Dock = DockStyle.Right, Cursor = Cursors.Hand };
            btnSave.FlatAppearance.BorderSize = 0; btnSave.FlatAppearance.MouseOverBackColor = Color.FromArgb(21, 128, 61); btnSave.FlatAppearance.MouseDownBackColor = Color.FromArgb(20, 83, 45);
            btnSave.Click += (s, ev) =>
            {
                if (cbo.SelectedItem == null) return;
                bool ok = _ctrl.ChangeStaffDepartment(staff.StaffId, cbo.SelectedItem.ToString());
                if (ok) { MessageBox.Show("Department updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information); dlg.Close(); RefreshGrid(); }
                else      MessageBox.Show("Failed to update department. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            pnlFtr.Controls.Add(btnCancel); pnlFtr.Controls.Add(btnSave);
            dlg.Controls.Add(pnlBody); dlg.Controls.Add(pnlHdr); dlg.Controls.Add(pnlFtr);
            dlg.ShowDialog(this);
        }

        // ── Grid events
        private void dgvStaff_SelectionChanged(object sender, EventArgs e) => UpdateActionButtons();
        private void dgvStaff_CellDoubleClick(object sender, DataGridViewCellEventArgs e) { if (e.RowIndex < 0) return; ShowDetailDialog(e.RowIndex); }

        // ── Detail dialog (double-click)
        private void ShowDetailDialog(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _currentStaff.Count) return;
            var s = _currentStaff[rowIndex];
            using var dlg = new Form { Text = $"Staff \u2014 {s.StaffId}", Size = new Size(640, 400), StartPosition = FormStartPosition.CenterParent, BackColor = Color.White, Font = new Font("Segoe UI", 12f), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            var pnlHdr = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(19, 35, 61) };
            pnlHdr.Controls.Add(new Label { Text = $"Staff Details  \u2014  {s.StaffId}", Font = new Font("Segoe UI", 14f, FontStyle.Bold), ForeColor = Color.White, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(20, 0, 0, 0) });
            var pnlBody = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 16, 24, 8), BackColor = Color.White };
            var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5, BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            for (int r = 0; r < 5; r++) tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));
            var fields = new[] { ("Staff ID", s.StaffId), ("Name", s.StaffName), ("Role", s.Role), ("Department", s.Department), ("Email", s.Email) };
            for (int i = 0; i < fields.Length; i++) { tbl.Controls.Add(MakeLblKey(fields[i].Item1), 0, i); tbl.Controls.Add(MakeLblVal(fields[i].Item2), 1, i); }
            pnlBody.Controls.Add(tbl);
            var pnlFtr = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.White, Padding = new Padding(0, 8, 20, 8) };
            pnlFtr.Paint += (snd, ev) => { using var pen = new Pen(Color.FromArgb(221, 227, 236), 1); ev.Graphics.DrawLine(pen, 0, 0, ((Panel)snd).Width, 0); };
            var btnClose = new Button { Text = "Close", Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White, FlatStyle = FlatStyle.Flat, Dock = DockStyle.Right, Width = 130, Cursor = Cursors.Hand };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236); btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnClose.Click += (snd, ev) => dlg.Close();
            pnlFtr.Controls.Add(btnClose);
            dlg.Controls.Add(pnlBody); dlg.Controls.Add(pnlHdr); dlg.Controls.Add(pnlFtr);
            dlg.ShowDialog(this);
        }

        // ── RoundedRect helper
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
        private static Label MakeLblKey(string text) => new Label { Text = text, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(98, 112, 135), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(0, 0, 8, 0) };
        private static Label MakeLblVal(string text) => new Label { Text = text ?? "\u2014", Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(15, 31, 53), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
        private static TextBox MakeTextInput(string placeholder) => new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle, PlaceholderText = placeholder };

        // ── Navigation & Logout
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem) => FormNavigator.NavigateTo(this, menuLabel, subItem);
        private void btnLogout_Click(object sender, EventArgs e) { SessionManager.Clear(); Application.Restart(); }
    }
}
