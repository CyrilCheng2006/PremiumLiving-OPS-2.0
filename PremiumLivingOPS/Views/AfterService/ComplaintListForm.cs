using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.DAL;
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

        private const int D_RowH   = 80;
        private const int D_LabelW = 260;
        private const int D_BtnW   = 200;
        private const int D_BtnH   = 56;

        private const int DLG_LabelW = 340;
        private const int DLG_RowH   = 80;
        private const int DLG_BtnW   = 210;
        private const int DLG_BtnH   = 60;

        public ComplaintListForm()
        {
            InitializeComponent();
            this.Load += ComplaintListForm_Load;
        }

        private void ComplaintListForm_Load(object sender, EventArgs e) => RefreshGrid();

        // ════════════════════════════════════════════════════════════════
        //  Refresh
        // ════════════════════════════════════════════════════════════════
        private void RefreshGrid()
        {
            string statusSel    = cboStatus.SelectedItem?.ToString();
            string statusFilter = (statusSel == "All" || string.IsNullOrEmpty(statusSel)) ? null : statusSel;
            string keyword      = txtKeyword.Text.Trim();

            var vm = _ctrl.GetComplaintListVM(statusFilter, string.IsNullOrEmpty(keyword) ? null : keyword);

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("After-Service  \u203a  Complaint List");

            _currentComplaints = vm.Complaints;

            dgvComplaints.Rows.Clear();
            foreach (var c in _currentComplaints)
                dgvComplaints.Rows.Add(
                    c.ComplaintID,
                    c.OrderID ?? "\u2014",
                    c.StaffName,
                    c.ComplaintDescription ?? "\u2014",
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

        // ════════════════════════════════════════════════════════════════
        //  KPI Pills
        // ════════════════════════════════════════════════════════════════
        private void RefreshKpi()
        {
            pnlKpi.Controls.Clear();

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

            const int PillW   = 290;
            const int PillH   = 60;
            const int Gap     = 8;
            const int NumColW = 80;

            foreach (var (label, count, fg, bg, filterVal) in pills)
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
                    Text      = count,
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

                string localFilter = filterVal;
                EventHandler click = (s, e) =>
                {
                    int idx = cboStatus.FindStringExact(localFilter);
                    if (idx >= 0) cboStatus.SelectedIndex = idx;
                    RefreshGrid();
                };
                pill.Click += click;
                tlp.Click  += click;
                foreach (Control ch in tlp.Controls) ch.Click += click;

                pill.Controls.Add(tlp);
                flow.Controls.Add(pill);
            }

            pnlKpi.Controls.Add(flow);
        }

        // ════════════════════════════════════════════════════════════════
        //  Action state
        // ════════════════════════════════════════════════════════════════
        private void UpdateActionButtons()
        {
            bool sel = dgvComplaints.SelectedRows.Count > 0;
            btnUpdateStatus.Enabled = sel;
            btnViewDetail.Enabled   = sel;
        }

        private void dgvComplaints_SelectionChanged(object sender, EventArgs e) => UpdateActionButtons();

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

        private string ShowOrderPicker(Form owner)
        {
            List<string> orders = new List<string>();
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                using var cmd = new MySql.Data.MySqlClient.MySqlCommand(
                    "SELECT OrderID FROM `Order` ORDER BY OrderID DESC", conn);
                using var rdr = cmd.ExecuteReader();
                while (rdr.Read()) orders.Add(rdr.GetString(0));
            }
            catch { }

            string selected = null;

            using var dlg = new Form
            {
                Text            = "Select Order ID",
                Size            = new Size(700, 560),
                MinimumSize     = new Size(500, 400),
                StartPosition   = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false,
                BackColor       = Color.FromArgb(240, 244, 249),
                Font            = new Font("Segoe UI", 12f)
            };

            var hdr = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(19, 35, 61) };
            hdr.Controls.Add(new Label
            {
                Text = "\uD83D\uDD0D  Select Order ID",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(20, 0, 0, 0)
            });

            var pnlSearch = new Panel
            {
                Dock = DockStyle.Top, Height = 56,
                BackColor = Color.White, Padding = new Padding(16, 10, 16, 10)
            };
            PaintBottomBorderStatic(pnlSearch);
            var txtSearch = new TextBox
            {
                Dock            = DockStyle.Fill,
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                PlaceholderText = "Type to search Order ID..."
            };
            pnlSearch.Controls.Add(txtSearch);

            var lst = new ListBox
            {
                Dock          = DockStyle.Fill,
                Font          = new Font("Segoe UI", 12f),
                BorderStyle   = BorderStyle.None,
                ItemHeight    = 36,
                BackColor     = Color.White,
                SelectionMode = SelectionMode.One
            };
            void Populate(string kw)
            {
                lst.BeginUpdate();
                lst.Items.Clear();
                foreach (var o in orders)
                    if (string.IsNullOrEmpty(kw) || o.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                        lst.Items.Add(o);
                lst.EndUpdate();
            }
            Populate(string.Empty);
            txtSearch.TextChanged += (_, __) => Populate(txtSearch.Text.Trim());

            var foot = new Panel
            {
                Dock = DockStyle.Bottom, Height = 72,
                BackColor = Color.White, Padding = new Padding(0, 12, 20, 12)
            };
            foot.Paint += (s2, pe) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                pe.Graphics.DrawLine(pen, 0, 0, ((Panel)s2).Width, 0);
            };

            var btnSelect = new Button
            {
                Text = "\u2714  Select",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                BackColor = Color.FromArgb(47, 111, 237), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Width = 160, Height = 48, Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0)
            };
            btnSelect.FlatAppearance.BorderSize = 0;

            var btnClear = new Button
            {
                Text = "Clear (Optional)",
                Font = new Font("Segoe UI", 12f),
                BackColor = Color.White, ForeColor = Color.FromArgb(15, 31, 53),
                FlatStyle = FlatStyle.Flat, Width = 180, Height = 48, Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0)
            };
            btnClear.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            btnClear.FlatAppearance.BorderSize  = 1;

            var btnCancel = new Button
            {
                Text = "Cancel",
                Font = new Font("Segoe UI", 12f),
                BackColor = Color.White, ForeColor = Color.FromArgb(15, 31, 53),
                FlatStyle = FlatStyle.Flat, Width = 120, Height = 48, Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            btnCancel.FlatAppearance.BorderSize  = 1;

            btnSelect.Click += (_, __) =>
            {
                if (lst.SelectedItem != null)
                { selected = lst.SelectedItem.ToString(); dlg.DialogResult = DialogResult.OK; }
                else MessageBox.Show("Please select an Order ID.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };
            btnClear.Click  += (_, __) => { selected = string.Empty; dlg.DialogResult = DialogResult.OK; };
            btnCancel.Click += (_, __) => dlg.Close();
            lst.DoubleClick += (_, __) =>
            {
                if (lst.SelectedItem != null)
                { selected = lst.SelectedItem.ToString(); dlg.DialogResult = DialogResult.OK; }
            };

            var footFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight, BackColor = Color.Transparent
            };
            footFlow.Controls.Add(btnSelect);
            footFlow.Controls.Add(btnClear);
            footFlow.Controls.Add(btnCancel);
            foot.Controls.Add(footFlow);

            var body = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            body.Controls.Add(lst);

            dlg.Controls.Add(body);
            dlg.Controls.Add(foot);
            dlg.Controls.Add(pnlSearch);
            dlg.Controls.Add(hdr);

            dlg.ShowDialog(owner);
            return selected;
        }

        private StaffItem ShowStaffPicker(Form owner)
        {
            List<StaffItem> staffList = new List<StaffItem>();
            try
            {
                foreach (var s in _ctrl.GetStaffList())
                    staffList.Add(new StaffItem { StaffID = s.StaffID, StaffName = s.StaffName });
            }
            catch { }

            staffList.Sort((a, b) => string.Compare(a.StaffID, b.StaffID, StringComparison.Ordinal));

            StaffItem selected = null;

            using var dlg = new Form
            {
                Text            = "Select Staff Member",
                Size            = new Size(700, 560),
                MinimumSize     = new Size(500, 400),
                StartPosition   = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false,
                BackColor       = Color.FromArgb(240, 244, 249),
                Font            = new Font("Segoe UI", 12f)
            };

            var hdr = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(19, 35, 61) };
            hdr.Controls.Add(new Label
            {
                Text = "\uD83D\uDD0D  Select Handled By (Staff)",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(20, 0, 0, 0)
            });

            var pnlSearch = new Panel
            {
                Dock = DockStyle.Top, Height = 56,
                BackColor = Color.White, Padding = new Padding(16, 10, 16, 10)
            };
            PaintBottomBorderStatic(pnlSearch);
            var txtSearch = new TextBox
            {
                Dock            = DockStyle.Fill,
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                PlaceholderText = "Type staff name or ID..."
            };
            pnlSearch.Controls.Add(txtSearch);

            var grid = new DataGridView
            {
                Dock                  = DockStyle.Fill,
                ReadOnly              = true,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect           = false,
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                BorderStyle           = BorderStyle.None,
                BackgroundColor       = Color.White,
                RowHeadersVisible     = false,
                Font                  = new Font("Segoe UI", 12f),
                RowTemplate           = { Height = 40 }
            };
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colID",   HeaderText = "Staff ID",   FillWeight = 40 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Staff Name", FillWeight = 60 });
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 255);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(47, 111, 237);
            grid.EnableHeadersVisualStyles = false;

            void Populate(string kw)
            {
                grid.Rows.Clear();
                foreach (var s in staffList)
                    if (string.IsNullOrEmpty(kw) ||
                        s.StaffName.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        s.StaffID.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                        grid.Rows.Add(s.StaffID, s.StaffName);
            }
            Populate(string.Empty);
            txtSearch.TextChanged += (_, __) => Populate(txtSearch.Text.Trim());

            var foot = new Panel
            {
                Dock = DockStyle.Bottom, Height = 72,
                BackColor = Color.White, Padding = new Padding(0, 12, 20, 12)
            };
            foot.Paint += (s2, pe) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                pe.Graphics.DrawLine(pen, 0, 0, ((Panel)s2).Width, 0);
            };

            var btnSelect = new Button
            {
                Text = "\u2714  Select",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                BackColor = Color.FromArgb(47, 111, 237), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Width = 160, Height = 48, Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0)
            };
            btnSelect.FlatAppearance.BorderSize = 0;

            var btnCancel = new Button
            {
                Text = "Cancel",
                Font = new Font("Segoe UI", 12f),
                BackColor = Color.White, ForeColor = Color.FromArgb(15, 31, 53),
                FlatStyle = FlatStyle.Flat, Width = 120, Height = 48, Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            btnCancel.FlatAppearance.BorderSize  = 1;

            StaffItem SelectedStaff()
            {
                if (grid.SelectedRows.Count == 0) return null;
                var row = grid.SelectedRows[0];
                return staffList.Find(s => s.StaffID == row.Cells["colID"].Value?.ToString());
            }

            btnSelect.Click += (_, __) =>
            {
                selected = SelectedStaff();
                if (selected != null) { dlg.DialogResult = DialogResult.OK; }
                else MessageBox.Show("Please select a staff member.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };
            btnCancel.Click  += (_, __) => dlg.Close();
            grid.CellDoubleClick += (_, __) =>
            {
                selected = SelectedStaff();
                if (selected != null) dlg.DialogResult = DialogResult.OK;
            };

            var footFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight, BackColor = Color.Transparent
            };
            footFlow.Controls.Add(btnSelect);
            footFlow.Controls.Add(btnCancel);
            foot.Controls.Add(footFlow);

            var body = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            body.Controls.Add(grid);

            dlg.Controls.Add(body);
            dlg.Controls.Add(foot);
            dlg.Controls.Add(pnlSearch);
            dlg.Controls.Add(hdr);

            dlg.ShowDialog(owner);
            return selected;
        }

        private Panel MakePickerRow(
            string labelText,
            out Label valueDisplay,
            Action onBrowse,
            bool lastRow = false)
        {
            var row = new Panel { Height = DLG_RowH, BackColor = Color.White };
            if (!lastRow)
                row.Paint += (s, pe) =>
                {
                    using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                    pe.Graphics.DrawLine(pen, 0, ((Panel)s).Height - 1, ((Panel)s).Width, ((Panel)s).Height - 1);
                };

            var outer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.White, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DLG_LabelW));
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var lbl = new Label
            {
                Text = labelText,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 85, 110),
                BackColor = Color.FromArgb(248, 250, 252),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false, Padding = new Padding(24, 0, 8, 0)
            };

            var inner = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.White, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(20, 14, 24, 14)
            };
            inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            inner.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            inner.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            valueDisplay = new Label
            {
                Text      = "(none selected)",
                Font      = new Font("Segoe UI", 12f, FontStyle.Italic),
                ForeColor = Color.FromArgb(150, 160, 175),
                BackColor = Color.FromArgb(248, 250, 252),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false,
                Padding   = new Padding(8, 0, 4, 0)
            };

            var btnBrowse = new Button
            {
                Text      = "\uD83D\uDD0D  Browse",
                Font      = new Font("Segoe UI", 11f),
                BackColor = Color.FromArgb(47, 111, 237),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock      = DockStyle.Fill,
                Cursor    = Cursors.Hand,
                Margin    = new Padding(4, 0, 0, 0)
            };
            btnBrowse.FlatAppearance.BorderSize = 0;
            btnBrowse.FlatAppearance.MouseOverBackColor = Color.FromArgb(29, 78, 216);
            btnBrowse.Click += (_, __) => onBrowse();

            inner.Controls.Add(valueDisplay, 0, 0);
            inner.Controls.Add(btnBrowse,    1, 0);

            outer.Controls.Add(lbl,   0, 0);
            outer.Controls.Add(inner, 1, 0);
            row.Controls.Add(outer);
            return row;
        }

        private void btnAddNew_Click(object sender, EventArgs e)
        {
            string autoId = GeneratePreviewComplaintId();

            using var dlg = new Form
            {
                Text            = "Create New Complaint",
                Size            = new Size(1400, 800),
                MinimumSize     = new Size(1100, 800),
                StartPosition   = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false, MinimizeBox = false,
                BackColor       = Color.FromArgb(240, 244, 249),
                Font            = new Font("Segoe UI", 13f)
            };

            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            pnlHeader.Controls.Add(new Label
            {
                Text = "\u2795  Create New Complaint",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold), ForeColor = Color.White,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false, Padding = new Padding(32, 0, 0, 0)
            });

            var pnlSectionTitle = new Panel
            {
                Dock = DockStyle.Top, Height = 44,
                BackColor = Color.FromArgb(241, 245, 255), Padding = new Padding(32, 0, 16, 0)
            };
            PaintBottomBorderStatic(pnlSectionTitle);
            pnlSectionTitle.Controls.Add(new Label
            {
                Text = "\uD83D\uDCCB  Complaint Information",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(47, 111, 237),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            });

            Panel MakeRow(string lText, Control input, bool last = false)
            {
                var row = new Panel { Height = DLG_RowH, BackColor = Color.White };
                if (!last)
                    row.Paint += (s2, pe) =>
                    {
                        using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                        pe.Graphics.DrawLine(pen, 0, ((Panel)s2).Height - 1, ((Panel)s2).Width, ((Panel)s2).Height - 1);
                    };
                var tlp = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                    BackColor = Color.White, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DLG_LabelW));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                var lbl = new Label
                {
                    Text = lText, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(70, 85, 110), BackColor = Color.FromArgb(248, 250, 252),
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                    AutoSize = false, Padding = new Padding(24, 0, 8, 0)
                };
                var wrap = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(20, 14, 24, 14) };
                input.Dock = DockStyle.Fill;
                wrap.Controls.Add(input);
                tlp.Controls.Add(lbl, 0, 0);
                tlp.Controls.Add(wrap, 1, 0);
                row.Controls.Add(tlp);
                return row;
            }

            string selectedOrderId = null;
            StaffItem selectedStaff = null;

            var txtComplaintId = new TextBox
            {
                Text = autoId, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle, ReadOnly = true,
                BackColor = Color.FromArgb(240, 244, 249), ForeColor = Color.FromArgb(47, 111, 237)
            };
            var rowId = MakeRow("Complaint ID  (auto)", txtComplaintId);

            Label lblOrderVal = null;
            var rowOrder = MakePickerRow("Order No. (optional)", out lblOrderVal, () =>
            {
                string result = ShowOrderPicker(dlg);
                if (result == null) return;
                selectedOrderId = string.IsNullOrEmpty(result) ? null : result;
                if (selectedOrderId != null)
                {
                    lblOrderVal.Text      = selectedOrderId;
                    lblOrderVal.Font      = new Font("Segoe UI", 12f, FontStyle.Bold);
                    lblOrderVal.ForeColor = Color.FromArgb(15, 31, 53);
                    lblOrderVal.BackColor = Color.White;
                }
                else
                {
                    lblOrderVal.Text      = "(none selected)";
                    lblOrderVal.Font      = new Font("Segoe UI", 12f, FontStyle.Italic);
                    lblOrderVal.ForeColor = Color.FromArgb(150, 160, 175);
                    lblOrderVal.BackColor = Color.FromArgb(248, 250, 252);
                }
            });

            Label lblStaffVal = null;
            var rowStaff = MakePickerRow("Handled By *", out lblStaffVal, () =>
            {
                StaffItem result = ShowStaffPicker(dlg);
                if (result == null) return;
                selectedStaff         = result;
                lblStaffVal.Text      = $"{result.StaffName}  [{result.StaffID}]";
                lblStaffVal.Font      = new Font("Segoe UI", 12f, FontStyle.Bold);
                lblStaffVal.ForeColor = Color.FromArgb(15, 31, 53);
                lblStaffVal.BackColor = Color.White;
            });

            var cboStatusNew = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 12f)
            };
            cboStatusNew.Items.AddRange(new object[] { "Pending", "Processing", "Escalated", "Completed" });
            cboStatusNew.SelectedIndex = 0;
            var rowStatus = MakeRow("Status *", cboStatusNew);

            var txtDesc = new TextBox
            {
                Font = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Describe the complaint in detail"
            };
            var rowDesc = MakeRow("Description *", txtDesc, last: true);

            var allRows = new Panel[] { rowId, rowOrder, rowStaff, rowStatus, rowDesc };
            int cardHeight = allRows.Length * DLG_RowH;
            var cardOuter = new Panel
            {
                Dock = DockStyle.Top, Height = cardHeight + 32,
                BackColor = Color.Transparent, Padding = new Padding(20, 16, 20, 16)
            };
            var cardInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(0) };
            cardInner.Paint += (s2, pe) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                pe.Graphics.DrawRectangle(pen, 0, 0, ((Panel)s2).Width - 1, ((Panel)s2).Height - 1);
            };
            int y2 = 0;
            foreach (var r in allRows)
            {
                r.Location = new Point(0, y2);
                r.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                r.Width    = 1200;
                cardInner.Controls.Add(r);
                y2 += DLG_RowH;
            }
            cardInner.Resize += (s2, _) =>
            { var p = (Panel)s2; foreach (Control r in p.Controls) r.Width = p.Width; };
            cardOuter.Controls.Add(cardInner);

            var pnlFoot = new Panel
            {
                Dock = DockStyle.Bottom, Height = 96,
                BackColor = Color.White, Padding = new Padding(0, 18, 28, 18)
            };
            pnlFoot.Paint += (s2, pe) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                pe.Graphics.DrawLine(pen, 0, 0, ((Panel)s2).Width, 0);
            };

            var btnCreate = new Button
            {
                Text = "\u2714  Create Complaint",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(5, 150, 105),
                FlatStyle = FlatStyle.Flat, Width = DLG_BtnW, Height = DLG_BtnH,
                Cursor = Cursors.Hand, Margin = new Padding(0, 0, 10, 0)
            };
            btnCreate.FlatAppearance.BorderSize = 0;
            btnCreate.FlatAppearance.MouseOverBackColor = Color.FromArgb(4, 120, 87);

            var btnCancelCreate = new Button
            {
                Text = "Cancel",
                Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat, Width = DLG_BtnW, Height = DLG_BtnH, Cursor = Cursors.Hand
            };
            btnCancelCreate.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            btnCancelCreate.FlatAppearance.BorderSize  = 1;
            btnCancelCreate.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);

            bool confirmed = false;
            btnCreate.Click += (s2, ev) =>
            {
                if (selectedStaff == null)
                {
                    MessageBox.Show("Please select a staff member for Handled By.",
                        "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return;
                }
                if (string.IsNullOrWhiteSpace(txtDesc.Text))
                {
                    MessageBox.Show("Description is required.",
                        "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtDesc.Focus(); return;
                }
                confirmed = true;
                dlg.Close();
            };
            btnCancelCreate.Click += (s2, ev) => dlg.Close();

            var footFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight, BackColor = Color.Transparent
            };
            footFlow.Controls.Add(btnCreate);
            footFlow.Controls.Add(btnCancelCreate);
            pnlFoot.Controls.Add(footFlow);

            var pnlFill = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };
            dlg.Controls.Add(pnlFill);
            dlg.Controls.Add(cardOuter);
            dlg.Controls.Add(pnlSectionTitle);
            dlg.Controls.Add(pnlHeader);
            dlg.Controls.Add(pnlFoot);
            dlg.ShowDialog(this);

            if (!confirmed) return;

            try
            {
                var entity = new ComplaintEntity
                {
                    ComplaintID          = txtComplaintId.Text.Trim(),
                    OrderID              = selectedOrderId,
                    StaffID              = selectedStaff.StaffID,
                    ComplaintStatus      = cboStatusNew.SelectedItem?.ToString() ?? "Pending",
                    ComplaintDescription = txtDesc.Text.Trim()
                };
                bool ok = _ctrl.CreateComplaint(entity);
                if (ok)
                {
                    MessageBox.Show("Complaint created successfully.",
                        "Created", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    RefreshGrid();
                }
                else
                    MessageBox.Show("Failed to create complaint. Please try again.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create complaint:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GeneratePreviewComplaintId()
        {
            string prefix = "CMP-" + DateTime.Today.ToString("yyyyMMdd") + "-";
            var existing  = new List<string>();
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                using var cmd = new MySql.Data.MySqlClient.MySqlCommand(
                    "SELECT ComplaintID FROM Complaint WHERE ComplaintID LIKE @p", conn);
                cmd.Parameters.AddWithValue("@p", prefix + "%");
                using var rdr = cmd.ExecuteReader();
                while (rdr.Read()) existing.Add(rdr.GetString(0));
            }
            catch { return prefix + "XXXX"; }

            int next = 1;
            foreach (var id in existing)
            {
                if (id.Length >= prefix.Length + 4 &&
                    int.TryParse(id.Substring(prefix.Length, 4), out int seq) && seq >= next)
                    next = seq + 1;
            }
            return $"{prefix}{next:D4}";
        }

        private void btnUpdateStatus_Click(object sender, EventArgs e)
        {
            if (dgvComplaints.SelectedRows.Count == 0) return;
            string id  = dgvComplaints.SelectedRows[0].Cells["colComplaintID"].Value?.ToString();
            var    ent = _currentComplaints.Find(x => x.ComplaintID == id);
            if (ent == null) return;

            Label ReadLabel(string text) => new Label
            {
                Text = text ?? "\u2014", Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.White
            };

            Panel FieldRow(string labelText, Control input, bool lastRow = false)
            {
                var row = new Panel { Height = D_RowH, BackColor = Color.White };
                if (!lastRow)
                    row.Paint += (s2, pe) =>
                    {
                        using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                        pe.Graphics.DrawLine(pen, 0, ((Panel)s2).Height - 1, ((Panel)s2).Width, ((Panel)s2).Height - 1);
                    };
                var tlp = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                    BackColor = Color.White, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D_LabelW));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                var lbl = new Label
                {
                    Text = labelText, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(70, 85, 110), BackColor = Color.FromArgb(248, 250, 252),
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                    AutoSize = false, Padding = new Padding(20, 0, 8, 0)
                };
                var wrap = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(20, 12, 20, 12) };
                input.Dock = DockStyle.Fill;
                wrap.Controls.Add(input);
                tlp.Controls.Add(lbl,  0, 0);
                tlp.Controls.Add(wrap, 1, 0);
                row.Controls.Add(tlp);
                return row;
            }

            var cboNew = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 12f), FlatStyle = FlatStyle.Flat,
                BackColor = Color.White, ForeColor = Color.FromArgb(15, 31, 53)
            };
            cboNew.Items.AddRange(new object[] { "Pending", "Processing", "Escalated", "Completed" });
            cboNew.SelectedItem = ent.ComplaintStatus;

            var rows = new Panel[]
            {
                FieldRow("Complaint ID",  ReadLabel(ent.ComplaintID)),
                FieldRow("Order No.",     ReadLabel(ent.OrderID ?? "\u2014")),
                FieldRow("Current Status",ReadLabel(ent.ComplaintStatus)),
                FieldRow("New Status",    cboNew, lastRow: true)
            };
            var (cardOuter, cardInner) = CardPanel.Create(
                outerHeight: rows.Length * D_RowH + 22,
                outerPadding: new Padding(20, 14, 20, 8));
            cardInner.Padding = new Padding(0);
            cardInner.Controls.Add(BuildStack(rows));

            using var dlg = new Form
            {
                Text            = $"Update Complaint Status  \u2014  {ent.ComplaintID}",
                Size            = new Size(1800, 700),
                MinimumSize     = new Size(1800, 700),
                StartPosition   = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false, MinimizeBox = false,
                BackColor       = Color.FromArgb(240, 244, 249),
                Font            = new Font("Segoe UI", 12f)
            };

            Color pillBg = Color.FromArgb(229, 231, 235);
            Color pillFg = Color.FromArgb(55, 65, 81);
            if (StatusColors.TryGetValue(ent.ComplaintStatus ?? "", out var hsc))
            { pillBg = hsc.bg; pillFg = hsc.fg; }

            var statusFont = new Font("Segoe UI", 13f, FontStyle.Bold);
            int textW      = TextRenderer.MeasureText(ent.ComplaintStatus ?? "\u2014", statusFont).Width;
            int statusColW = textW + 80;

            var statusLbl = new Label
            {
                Text = ent.ComplaintStatus ?? "\u2014",
                Font = statusFont, ForeColor = pillFg, BackColor = pillBg,
                Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleCenter
            };
            statusLbl.Paint += (s2, pe) =>
            {
                var lb = (Label)s2;
                using var pen = new System.Drawing.Pen(Color.FromArgb(120, pillFg.R, pillFg.G, pillFg.B), 1);
                pe.Graphics.DrawRectangle(pen, 0, 0, lb.Width - 1, lb.Height - 1);
            };

            var headerTlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            headerTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            headerTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, statusColW));
            headerTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            headerTlp.Controls.Add(new Label
            {
                Text = $"Update Status  \u2014  {ent.ComplaintID}",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold), ForeColor = Color.White,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent, Padding = new Padding(40, 0, 0, 0)
            }, 0, 0);
            headerTlp.Controls.Add(statusLbl, 1, 0);

            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 88, BackColor = Color.FromArgb(19, 35, 61) };
            pnlHeader.Controls.Add(headerTlp);

            var pnlFoot = new Panel
            {
                Dock = DockStyle.Bottom, Height = 96,
                BackColor = Color.White, Padding = new Padding(0, 18, 40, 18)
            };
            pnlFoot.Paint += (s2, pe) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                pe.Graphics.DrawLine(pen, 0, 0, ((Panel)s2).Width, 0);
            };

            var btnConfirm = new Button
            {
                Text = "Confirm", Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                BackColor = Color.FromArgb(19, 35, 61), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Width = D_BtnW, Height = D_BtnH,
                Cursor = Cursors.Hand, Margin = new Padding(0, 0, 12, 0)
            };
            btnConfirm.FlatAppearance.BorderSize = 0;

            var btnCancelUpd = new Button
            {
                Text = "Cancel", Font = new Font("Segoe UI", 13f),
                BackColor = Color.White, ForeColor = Color.FromArgb(15, 31, 53),
                FlatStyle = FlatStyle.Flat, Width = D_BtnW, Height = D_BtnH, Cursor = Cursors.Hand
            };
            btnCancelUpd.FlatAppearance.BorderColor = Color.FromArgb(200, 207, 220);
            btnCancelUpd.FlatAppearance.BorderSize  = 1;

            btnConfirm.Click += (s2, ev) =>
            {
                if (cboNew.SelectedItem == null) return;
                bool ok = _ctrl.UpdateComplaintStatus(id, cboNew.SelectedItem.ToString());
                if (ok) { dlg.DialogResult = DialogResult.OK; dlg.Close(); }
                else MessageBox.Show("Update failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            btnCancelUpd.Click += (s2, ev) => dlg.Close();

            var footFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight, BackColor = Color.Transparent
            };
            footFlow.Controls.Add(btnConfirm);
            footFlow.Controls.Add(btnCancelUpd);
            pnlFoot.Controls.Add(footFlow);

            var scroll = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249), AutoScroll = true };
            scroll.Controls.Add(cardOuter);
            dlg.Controls.Add(scroll);
            dlg.Controls.Add(pnlFoot);
            dlg.Controls.Add(pnlHeader);

            if (dlg.ShowDialog(this) == DialogResult.OK) RefreshGrid();
        }

        private void btnViewDetail_Click(object sender, EventArgs e) => ShowDetailDialog();

        private void ShowDetailDialog()
        {
            if (dgvComplaints.SelectedRows.Count == 0) return;
            string id = dgvComplaints.SelectedRows[0].Cells["colComplaintID"].Value?.ToString();
            var c = _currentComplaints.Find(x => x.ComplaintID == id);
            if (c == null) return;

            Label ReadLabel(string text) => new Label
            {
                Text = text ?? "\u2014", Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.White
            };

            Panel FieldRow(string labelText, Control input, bool lastRow = false)
            {
                var row = new Panel { Height = D_RowH, BackColor = Color.White };
                if (!lastRow)
                    row.Paint += (s, pe) =>
                    {
                        using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                        pe.Graphics.DrawLine(pen, 0, ((Panel)s).Height - 1, ((Panel)s).Width, ((Panel)s).Height - 1);
                    };
                var tlp = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                    BackColor = Color.White, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D_LabelW));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                var lbl = new Label
                {
                    Text = labelText, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(70, 85, 110), BackColor = Color.FromArgb(248, 250, 252),
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                    AutoSize = false, Padding = new Padding(20, 0, 8, 0)
                };
                var wrap = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(20, 12, 20, 12) };
                input.Dock = DockStyle.Fill;
                wrap.Controls.Add(input);
                tlp.Controls.Add(lbl,  0, 0);
                tlp.Controls.Add(wrap, 1, 0);
                row.Controls.Add(tlp);
                return row;
            }

            var c1Rows = new Panel[]
            {
                FieldRow("Complaint ID", ReadLabel(c.ComplaintID)),
                FieldRow("Order No.",    ReadLabel(c.OrderID ?? "\u2014")),
                FieldRow("Status",       ReadLabel(c.ComplaintStatus), lastRow: true)
            };
            var (c1Outer, c1Inner) = CardPanel.Create(
                outerHeight: c1Rows.Length * D_RowH + 22,
                outerPadding: new Padding(20, 14, 20, 8));
            c1Inner.Padding = new Padding(0);
            c1Inner.Controls.Add(BuildStack(c1Rows));

            var c2Rows = new Panel[]
            {
                FieldRow("Handled By",  ReadLabel(c.StaffName)),
                FieldRow("Description", ReadLabel(c.ComplaintDescription ?? "\u2014"), lastRow: true)
            };
            var (c2Outer, c2Inner) = CardPanel.Create(
                outerHeight: c2Rows.Length * D_RowH + 30,
                outerPadding: new Padding(20, 8, 20, 16));
            c2Inner.Padding = new Padding(0);
            c2Inner.Controls.Add(BuildStack(c2Rows));

            using var dlg = new Form
            {
                Text            = $"Complaint Detail  \u2014  {c.ComplaintID}",
                Size            = new Size(1900, 700),
                MinimumSize     = new Size(1100, 700),
                StartPosition   = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false, MinimizeBox = false,
                BackColor       = Color.FromArgb(240, 244, 249),
                Font            = new Font("Segoe UI", 12f)
            };

            Color pillBg = Color.FromArgb(229, 231, 235);
            Color pillFg = Color.FromArgb(55, 65, 81);
            if (StatusColors.TryGetValue(c.ComplaintStatus ?? "", out var hsc))
            { pillBg = hsc.bg; pillFg = hsc.fg; }

            var statusFont = new Font("Segoe UI", 13f, FontStyle.Bold);
            int textW      = TextRenderer.MeasureText(c.ComplaintStatus ?? "\u2014", statusFont).Width;
            int statusColW = textW + 80;

            var statusLbl = new Label
            {
                Text = c.ComplaintStatus ?? "\u2014",
                Font = statusFont, ForeColor = pillFg, BackColor = pillBg,
                Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleCenter
            };
            statusLbl.Paint += (s, pe) =>
            {
                var lb = (Label)s;
                using var pen = new System.Drawing.Pen(Color.FromArgb(120, pillFg.R, pillFg.G, pillFg.B), 1);
                pe.Graphics.DrawRectangle(pen, 0, 0, lb.Width - 1, lb.Height - 1);
            };

            var headerTlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            headerTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            headerTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, statusColW));
            headerTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            headerTlp.Controls.Add(new Label
            {
                Text = $"Complaint  \u2014  {c.ComplaintID}",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold), ForeColor = Color.White,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent, Padding = new Padding(40, 0, 0, 0)
            }, 0, 0);
            headerTlp.Controls.Add(statusLbl, 1, 0);

            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 88, BackColor = Color.FromArgb(19, 35, 61) };
            pnlHeader.Controls.Add(headerTlp);

            var pnlFoot = new Panel
            {
                Dock = DockStyle.Bottom, Height = 96,
                BackColor = Color.White, Padding = new Padding(0, 18, 40, 18)
            };
            pnlFoot.Paint += (s, pe) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                pe.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0);
            };
            var btnClose = new Button
            {
                Text = "Close", Font = new Font("Segoe UI", 13f),
                BackColor = Color.White, ForeColor = Color.FromArgb(15, 31, 53),
                FlatStyle = FlatStyle.Flat, Width = D_BtnW, Height = D_BtnH, Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(200, 207, 220);
            btnClose.FlatAppearance.BorderSize  = 1;
            btnClose.Click += (s2, ev) => dlg.Close();
            var footFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight, BackColor = Color.Transparent
            };
            footFlow.Controls.Add(btnClose);
            pnlFoot.Controls.Add(footFlow);

            var scroll = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249), AutoScroll = true };
            scroll.Controls.Add(c2Outer);
            scroll.Controls.Add(c1Outer);
            dlg.Controls.Add(scroll);
            dlg.Controls.Add(pnlFoot);
            dlg.Controls.Add(pnlHeader);
            dlg.ShowDialog(this);
        }

        private Panel BuildStack(Panel[] rows)
        {
            var content = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            var stack   = new Panel { Height = rows.Length * D_RowH, BackColor = Color.White };
            int y = 0;
            foreach (var r in rows)
            {
                r.Location = new Point(0, y);
                r.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                stack.Controls.Add(r);
                y += D_RowH;
            }
            content.Controls.Add(stack);
            content.Resize += (s, _) =>
            {
                var p = (Panel)s;
                stack.Width = p.Width; stack.Left = 0; stack.Top = 0;
                foreach (Panel r in stack.Controls) r.Width = p.Width;
            };
            return content;
        }

        private static void PaintBottomBorderStatic(Panel p)
        {
            p.Paint += (s, e) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawLine(pen, 0, ((Panel)s).Height - 1, ((Panel)s).Width, ((Panel)s).Height - 1);
            };
        }

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

        private class StaffItem
        {
            public string StaffID   { get; set; }
            public string StaffName { get; set; }
            public string Display   => StaffName;
            public override string ToString() => StaffName;
        }
    }
}
