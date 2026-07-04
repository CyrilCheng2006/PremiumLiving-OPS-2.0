// LogListForm.cs — System Control > Log List  (partial — UI defined in Designer.cs)
// Displays every audit log entry written by AuditLogger.
// All UI construction is in LogListForm.Designer.cs (AppShell, cards, DGV, KPI strip).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Services;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.SystemControl
{
    public partial class LogListForm : Form
    {
        // ── Data ──────────────────────────────────────────────────────────────────────────
        private List<AuditLogEntity> _allLogs = new List<AuditLogEntity>();
        private readonly SystemControlController _ctrl = new SystemControlController();

        public LogListForm()
        {
            InitializeComponent();
            this.Load += LogListForm_Load;
        }

        private void LogListForm_Load(object sender, EventArgs e) => RefreshGrid();

        // =====================================================================
        // Data & Filter
        // =====================================================================
        private void RefreshGrid()
        {
            var vm = _ctrl.GetLogListVM(txtSearch.Text.Trim());

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("System Control  \u203a  Log List");

            _allLogs = vm.Logs;
            ApplyFilter();
            RefreshKpi();
        }

        private void ResetFilters()
        {
            txtSearch.Text = string.Empty;
            RefreshGrid();
        }

        private void ApplyFilter()
        {
            string kw = txtSearch.Text.Trim().ToLowerInvariant();

            var filtered = string.IsNullOrEmpty(kw)
                ? _allLogs
                : _allLogs.Where(l =>
                    (l.StaffID     ?? "").ToLowerInvariant().Contains(kw) ||
                    (l.LogType     ?? "").ToLowerInvariant().Contains(kw) ||
                    (l.TargetTable ?? "").ToLowerInvariant().Contains(kw) ||
                    (l.StaffName   ?? "").ToLowerInvariant().Contains(kw)
                  ).ToList();

            BindGrid(filtered);
        }

        private void BindGrid(List<AuditLogEntity> logs)
        {
            dgvLogs.Rows.Clear();
            foreach (var l in logs)
            {
                string logKey = $"{l.Timestamp:yyyyMMdd-HHmmss} {l.StaffID ?? ""}".Trim();

                int ri  = dgvLogs.Rows.Add();
                var row = dgvLogs.Rows[ri];
                row.Cells["colLogID"      ].Value = logKey;
                row.Cells["colStaffID"    ].Value = l.StaffID     ?? "";
                row.Cells["colLogType"    ].Value = l.LogType     ?? "";
                row.Cells["colTargetTable"].Value = l.TargetTable ?? "";
                row.Cells["colTimestamp"  ].Value = l.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }

        // =====================================================================
        // KPI strip — ViewOrderForm standard:
        //   PillW=290, PillH=60, RoundedRect(r,8), Cursor.Hand, click-to-filter
        // =====================================================================
        private void RefreshKpi()
        {
            pnlKpi.Controls.Clear();

            var today = DateTime.Today;
            int total  = _allLogs.Count;
            int tday   = _allLogs.Count(l => l.Timestamp.Date == today);
            int create = _allLogs.Count(l => l.LogType == AuditLogger.TYPE_CREATE);
            int edit   = _allLogs.Count(l => l.LogType == AuditLogger.TYPE_EDIT);
            int delete = _allLogs.Count(l => l.LogType == AuditLogger.TYPE_DELETE);
            int login  = _allLogs.Count(l => l.LogType == AuditLogger.TYPE_LOGIN);

            var pills = new[]
            {
                ("Total",  total .ToString(), Color.FromArgb( 47, 111, 237), Color.FromArgb(219, 234, 254), (string)null),
                ("Today",  tday  .ToString(), Color.FromArgb(  6,  95,  70), Color.FromArgb(209, 250, 229), (string)null),
                ("CREATE", create.ToString(), Color.FromArgb( 22, 163,  74), Color.FromArgb(220, 252, 231), AuditLogger.TYPE_CREATE),
                ("EDIT",   edit  .ToString(), Color.FromArgb(161, 110,   0), Color.FromArgb(254, 243, 199), AuditLogger.TYPE_EDIT),
                ("DELETE", delete.ToString(), Color.FromArgb(185,  28,  28), Color.FromArgb(254, 226, 226), AuditLogger.TYPE_DELETE),
                ("LOGIN",  login .ToString(), Color.FromArgb( 91,  33, 182), Color.FromArgb(237, 233, 254), AuditLogger.TYPE_LOGIN),
            };

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

            foreach (var (label, count, fg, bg, filterKw) in pills)
            {
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

                var tlp = new TableLayoutPanel
                {
                    Dock            = DockStyle.Fill,
                    ColumnCount     = 2,
                    RowCount        = 1,
                    BackColor       = Color.Transparent,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding         = new Padding(10, 0, 8, 0)
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, NumColW));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                tlp.Controls.Add(new Label
                {
                    Text      = count,
                    Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                    ForeColor = fg, BackColor = Color.Transparent,
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize  = false
                }, 0, 0);
                tlp.Controls.Add(new Label
                {
                    Text         = label,
                    Font         = new Font("Segoe UI", 12f),
                    ForeColor    = fg, BackColor = Color.Transparent,
                    Dock         = DockStyle.Fill,
                    TextAlign    = ContentAlignment.MiddleLeft,
                    AutoSize     = false,
                    AutoEllipsis = true
                }, 1, 0);

                string localKw = filterKw;
                EventHandler clickHandler = (s, e) =>
                {
                    txtSearch.Text = localKw ?? string.Empty;
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

        // =====================================================================
        // Grid events
        // =====================================================================
        private void dgvLogs_SelectionChanged(object sender, EventArgs e) { /* reserved */ }

        private void dgvLogs_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _allLogs.Count) return;
            ShowDetailDialog(_allLogs[e.RowIndex]);
        }

        private void ShowDetailDialog(AuditLogEntity log)
        {
            string logKey = $"{log.Timestamp:yyyyMMdd-HHmmss} {log.StaffID ?? ""}".Trim();

            using var dlg = new Form
            {
                Text            = $"Log Detail  \u2014  {logKey}",
                Size            = new Size(740, 460),
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
                Text      = $"Log Detail  \u2014  {log.LogType} on {log.TargetTable}",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(24, 0, 0, 0)
            });

            var tbl = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill, ColumnCount = 2, RowCount = 7,
                BackColor       = Color.White,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(24, 16, 24, 8)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            for (int r = 0; r < 7; r++) tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 7f));

            var fields = new[]
            {
                ("Log Key",      logKey),
                ("Staff ID",     log.StaffID     ?? ""),
                ("Staff Name",   log.StaffName   ?? ""),
                ("Log Type",     log.LogType     ?? ""),
                ("Module/Table", log.TargetTable ?? ""),
                ("Timestamp",    log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")),
                ("Raw Line",     log.RawLine     ?? "")
            };
            for (int i = 0; i < fields.Length; i++)
            {
                tbl.Controls.Add(MakeLblKey(fields[i].Item1), 0, i);
                tbl.Controls.Add(MakeLblVal(fields[i].Item2), 1, i);
            }

            var pnlFtr = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.White, Padding = new Padding(0, 8, 20, 8) };
            pnlFtr.Paint += (fps, fpe) =>
            {
                using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
                fpe.Graphics.DrawLine(pen, 0, 0, ((Panel)fps).Width, 0);
            };
            var btnClose = new Button
            {
                Text = "Close", Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat, Dock = DockStyle.Right, Width = 130, Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnClose.Click += (bcs, bce) => dlg.Close();
            pnlFtr.Controls.Add(btnClose);

            dlg.Controls.Add(tbl);
            dlg.Controls.Add(pnlHdr);
            dlg.Controls.Add(pnlFtr);
            dlg.ShowDialog(this);
        }

        // ── Label helpers ──────────────────────────────────────────────────────
        private static Label MakeLblKey(string text) => new Label
        {
            Text = text, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(98, 112, 135), Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(0, 0, 8, 0)
        };

        private static Label MakeLblVal(string text) => new Label
        {
            Text = text ?? "\u2014", Font = new Font("Segoe UI", 12f),
            ForeColor = Color.FromArgb(15, 31, 53), Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true
        };

        // ── Button factories (required by Designer.cs) ──────────────────────────
        private static Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text = text, Location = loc, Size = new Size(w, h),
                BackColor = Color.FromArgb(19, 35, 61), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize         = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(31, 54, 96);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(10, 22, 41);
            return b;
        }

        private static Button MakeOutlineBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text = text, Location = loc, Size = new Size(w, h),
                BackColor = Color.White, ForeColor = Color.FromArgb(15, 31, 53),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12f),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize         = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            return b;
        }

        // ── Card border painter (required by Designer.cs) ──────────────────────
        private static void PaintCardBorder(object sender, PaintEventArgs e)
        {
            var pnl = (Panel)sender;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
        }

        // ── Rounded rect helper (same as ViewOrderForm) ────────────────────────
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

        // =====================================================================
        // Navigation & Logout
        // =====================================================================
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
    }
}
