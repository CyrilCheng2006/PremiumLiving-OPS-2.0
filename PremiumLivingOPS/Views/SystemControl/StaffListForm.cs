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
    /// MVC role: pure View. Delegates all data access to SystemControlController.
    ///
    /// KPI bar — fully aligned to ViewOrderForm baseline:
    ///   · PillW = 290, PillH = 60, Gap = 8, NumColW = 80
    ///   · Rounded rect via Paint + AntiAlias (RoundedRect helper)
    ///   · Number: 14pt Bold / Label: 12pt Regular
    ///   · Cursor.Hand; click filters Grid by Department
    ///   · 1 × fixed "Total Staff" pill + 1 dynamic pill per Department
    /// </summary>
    public partial class StaffListForm : Form
    {
        private readonly SystemControlController _ctrl = new SystemControlController();
        private List<Staff> _allStaff     = new List<Staff>();
        private List<Staff> _currentStaff = new List<Staff>();

        // Active department filter — null means "All"
        private string _deptFilter = null;

        // Palette cycling for Department pills (same semantic colours as ViewOrderForm)
        private static readonly (Color fg, Color bg)[] DeptColors =
        {
            (Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199)),  // amber
            (Color.FromArgb( 29,  78, 216), Color.FromArgb(219, 234, 254)),  // blue
            (Color.FromArgb(  6,  95,  70), Color.FromArgb(209, 250, 229)),  // green
            (Color.FromArgb( 91,  33, 182), Color.FromArgb(237, 233, 254)),  // purple
            (Color.FromArgb(185,  28,  28), Color.FromArgb(254, 226, 226)),  // red
            (Color.FromArgb( 22, 101,  52), Color.FromArgb(220, 252, 231)),  // emerald
            (Color.FromArgb( 75,  85,  99), Color.FromArgb(241, 245, 249)),  // slate
        };

        // Shared reference to the KPI inner panel (assigned in Designer)
        private Panel pnlKpi;

        public StaffListForm()
        {
            InitializeComponent();
            this.Load += StaffListForm_Load;
        }

        // ── Load ──────────────────────────────────────────────────────────────
        private void StaffListForm_Load(object sender, EventArgs e)
        {
            RefreshGrid();
        }

        // ── Data refresh ──────────────────────────────────────────────────────
        private void RefreshGrid()
        {
            string keyword = txtSearch.Text.Trim();
            var vm = _ctrl.GetStaffListVM(string.IsNullOrEmpty(keyword) ? null : keyword);

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("System Control  \u203a  Staff List");

            // Full unfiltered list — used by KPI bar
            _allStaff = _ctrl.GetStaffListVM().Staffs;

            // Apply department filter on top of keyword result
            _currentStaff = string.IsNullOrEmpty(_deptFilter)
                ? vm.Staffs
                : vm.Staffs.FindAll(s => s.Department == _deptFilter);

            dgvStaff.Rows.Clear();
            foreach (var s in _currentStaff)
                dgvStaff.Rows.Add(s.StaffId, s.StaffName, s.Role, s.Department, s.Email);

            RefreshKpi();
        }

        private void ResetFilters()
        {
            txtSearch.Text = string.Empty;
            _deptFilter    = null;
            RefreshGrid();
        }

        // ── KPI strip — aligned to ViewOrderForm baseline ─────────────────────
        private void RefreshKpi()
        {
            pnlKpi.Controls.Clear();

            int total = _allStaff.Count;

            // Group by Department, sorted alphabetically
            var deptGroups = _allStaff
                .GroupBy(s => string.IsNullOrWhiteSpace(s.Department) ? "(Unknown)" : s.Department)
                .OrderBy(g => g.Key)
                .ToList();

            var flow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent,
                Padding       = new Padding(0),
                AutoScroll    = false
            };

            // ── Baseline constants (identical to ViewOrderForm) ──
            const int PillW   = 290;
            const int PillH   = 60;
            const int Gap     = 8;
            const int NumColW = 80;

            // ── 1. Total Staff pill (fixed — resets dept filter on click)
            var totalPill = MakePill(
                "Total Staff", total.ToString(),
                Color.FromArgb(47, 111, 237), Color.FromArgb(219, 234, 254),
                PillW, PillH, Gap, NumColW);

            AttachClickFilter(totalPill, null);
            flow.Controls.Add(totalPill);

            // ── 2. One pill per Department
            int colorIdx = 0;
            foreach (var grp in deptGroups)
            {
                var (fg, bg) = DeptColors[colorIdx % DeptColors.Length];
                colorIdx++;

                var deptPill = MakePill(
                    grp.Key, grp.Count().ToString(),
                    fg, bg, PillW, PillH, Gap, NumColW);

                string deptName = grp.Key; // capture for lambda
                AttachClickFilter(deptPill, deptName);
                flow.Controls.Add(deptPill);
            }

            pnlKpi.Controls.Add(flow);
        }

        // Attach click-to-filter handler to pill + all its children
        // (mirrors ViewOrderForm pattern: pill.Click + tlp.Click + foreach child)
        private void AttachClickFilter(Panel pill, string deptName)
        {
            EventHandler handler = (s, e) =>
            {
                _deptFilter = deptName;   // null => show all
                RefreshGrid();
            };

            pill.Click += handler;
            foreach (Control child in pill.Controls)
            {
                child.Click += handler;
                foreach (Control grandChild in child.Controls)
                    grandChild.Click += handler;
            }
        }

        // ── Pill factory — identical spec to ViewOrderForm ────────────────────
        private static Panel MakePill(
            string label, string count,
            Color fg, Color bg,
            int width, int height, int gap, int numColW)
        {
            var pill = new Panel
            {
                BackColor = bg,
                Size      = new Size(width, height),
                Margin    = new Padding(0, 0, gap, 0),
                Cursor    = Cursors.Hand
            };

            // Rounded rect rendered via Paint + AntiAlias
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
                Padding         = new Padding(10, 0, 8, 0)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, numColW));  // number
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));     // label
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            tlp.Controls.Add(new Label
            {
                Text      = count,
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = fg,
                BackColor = Color.Transparent,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize  = false
            }, 0, 0);

            tlp.Controls.Add(new Label
            {
                Text         = label,
                Font         = new Font("Segoe UI", 12f),
                ForeColor    = fg,
                BackColor    = Color.Transparent,
                Dock         = DockStyle.Fill,
                TextAlign    = ContentAlignment.MiddleLeft,
                AutoSize     = false,
                AutoEllipsis = true
            }, 1, 0);

            pill.Controls.Add(tlp);
            return pill;
        }

        // ── Rounded-rectangle path helper (identical to ViewOrderForm) ─────────
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

        // ── Grid events ───────────────────────────────────────────────────────
        private void dgvStaff_SelectionChanged(object sender, EventArgs e) { /* reserved */ }

        private void dgvStaff_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            ShowDetailDialog(e.RowIndex);
        }

        // ── Detail dialog ─────────────────────────────────────────────────────
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

        // ── Label helpers ─────────────────────────────────────────────────────
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

        // ── Navigation & Logout ───────────────────────────────────────────────
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }
    }
}
