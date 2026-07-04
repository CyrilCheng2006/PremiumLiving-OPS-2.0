// LogListForm.cs — System Control > Log List  (partial — UI defined in Designer.cs)
// Displays every audit log entry written by AuditLogger.
// All UI construction is in LogListForm.Designer.cs (AppShell, cards, DGV, KPI strip).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Services;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.SystemControl
{
    public partial class LogListForm : Form
    {
        // ── KPI label refs (populated in RefreshGrid) ─────────────────────────────────────────
        private Label _lblTotal;
        private Label _lblToday;
        private Label _lblCreate;
        private Label _lblEdit;
        private Label _lblDelete;
        private Label _lblLogin;

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
                    (l.StaffID    ?? "").ToLowerInvariant().Contains(kw) ||
                    (l.LogType    ?? "").ToLowerInvariant().Contains(kw) ||
                    (l.TargetTable?? "").ToLowerInvariant().Contains(kw) ||
                    (l.StaffName  ?? "").ToLowerInvariant().Contains(kw)
                  ).ToList();

            BindGrid(filtered);
            UpdateKpis(filtered);
        }

        private void BindGrid(List<AuditLogEntity> logs)
        {
            dgvLogs.Rows.Clear();
            foreach (var l in logs)
            {
                // AuditLogEntity has no LogID — use timestamp+staffID as a display key
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

        private void UpdateKpis(List<AuditLogEntity> logs)
        {
            var today = DateTime.Today;
            if (_lblTotal  != null) _lblTotal .Text = logs.Count.ToString();
            if (_lblToday  != null) _lblToday .Text = logs.Count(l => l.Timestamp.Date == today).ToString();
            if (_lblCreate != null) _lblCreate.Text = logs.Count(l => l.LogType == AuditLogger.TYPE_CREATE).ToString();
            if (_lblEdit   != null) _lblEdit  .Text = logs.Count(l => l.LogType == AuditLogger.TYPE_EDIT  ).ToString();
            if (_lblDelete != null) _lblDelete.Text = logs.Count(l => l.LogType == AuditLogger.TYPE_DELETE).ToString();
            if (_lblLogin  != null) _lblLogin .Text = logs.Count(l => l.LogType == AuditLogger.TYPE_LOGIN ).ToString();
        }

        // =====================================================================
        // KPI strip builder (called after InitializeComponent)
        // =====================================================================
        private void RefreshKpi()
        {
            pnlKpi.Controls.Clear();

            var flow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = System.Drawing.Color.Transparent,
                Padding       = new Padding(0),
                AutoScroll    = false
            };

            _lblTotal  = AddKpiPill(flow, "Total Logs",
                System.Drawing.Color.FromArgb( 47, 111, 237), System.Drawing.Color.FromArgb(219, 234, 254));
            _lblToday  = AddKpiPill(flow, "Today",
                System.Drawing.Color.FromArgb(  6,  95,  70), System.Drawing.Color.FromArgb(209, 250, 229));
            _lblCreate = AddKpiPill(flow, "CREATE",
                System.Drawing.Color.FromArgb( 22, 163,  74), System.Drawing.Color.FromArgb(220, 252, 231));
            _lblEdit   = AddKpiPill(flow, "EDIT",
                System.Drawing.Color.FromArgb(161, 110,   0), System.Drawing.Color.FromArgb(254, 243, 199));
            _lblDelete = AddKpiPill(flow, "DELETE",
                System.Drawing.Color.FromArgb(185,  28,  28), System.Drawing.Color.FromArgb(254, 226, 226));
            _lblLogin  = AddKpiPill(flow, "LOGIN",
                System.Drawing.Color.FromArgb( 91,  33, 182), System.Drawing.Color.FromArgb(237, 233, 254));

            pnlKpi.Controls.Add(flow);
        }

        private static Label AddKpiPill(
            FlowLayoutPanel flow, string title,
            System.Drawing.Color fg, System.Drawing.Color bg)
        {
            const int PillW = 160, PillH = 60, Gap = 8, NumW = 60;

            var pill = new Panel
            {
                BackColor = bg,
                Size      = new System.Drawing.Size(PillW, PillH),
                Margin    = new Padding(0, 0, Gap, 0)
            };

            var tlp = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2, RowCount = 1,
                BackColor   = System.Drawing.Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding     = new Padding(8, 0, 6, 0)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, NumW));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var lblNum = new Label
            {
                Text      = "0",
                Font      = new System.Drawing.Font("Segoe UI", 14f, System.Drawing.FontStyle.Bold),
                ForeColor = fg,
                BackColor = System.Drawing.Color.Transparent,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize  = false
            };
            var lblTitle = new Label
            {
                Text      = title,
                Font      = new System.Drawing.Font("Segoe UI", 11f),
                ForeColor = fg,
                BackColor = System.Drawing.Color.Transparent,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false,
                AutoEllipsis = true
            };

            tlp.Controls.Add(lblNum,   0, 0);
            tlp.Controls.Add(lblTitle, 1, 0);
            pill.Controls.Add(tlp);
            flow.Controls.Add(pill);

            return lblNum;   // caller stores reference to update count
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
            // Derive a display key from timestamp + staffID (AuditLogEntity has no LogID field)
            string logKey = $"{log.Timestamp:yyyyMMdd-HHmmss} {log.StaffID ?? ""}".Trim();

            using var dlg = new Form
            {
                Text            = $"Log Detail  \u2014  {logKey}",
                Size            = new System.Drawing.Size(740, 460),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = System.Drawing.Color.White,
                Font            = new System.Drawing.Font("Segoe UI", 12f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false
            };

            var pnlHdr = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = System.Drawing.Color.FromArgb(19, 35, 61) };
            pnlHdr.Controls.Add(new Label
            {
                Text      = $"Log Detail  \u2014  {log.LogType} on {log.TargetTable}",
                Font      = new System.Drawing.Font("Segoe UI", 14f, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(24, 0, 0, 0)
            });

            var tbl = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 2,
                RowCount        = 7,
                BackColor       = System.Drawing.Color.White,
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

            var pnlFtr = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = System.Drawing.Color.White, Padding = new Padding(0, 8, 20, 8) };
            pnlFtr.Paint += (fps, fpe) => { using var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(221, 227, 236), 1); fpe.Graphics.DrawLine(pen, 0, 0, ((Panel)fps).Width, 0); };
            var btnClose = new Button { Text = "Close", Font = new System.Drawing.Font("Segoe UI", 12f), ForeColor = System.Drawing.Color.FromArgb(15, 31, 53), BackColor = System.Drawing.Color.White, FlatStyle = FlatStyle.Flat, Dock = DockStyle.Right, Width = 130, Cursor = Cursors.Hand };
            btnClose.FlatAppearance.BorderColor        = System.Drawing.Color.FromArgb(221, 227, 236);
            btnClose.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(240, 244, 249);
            btnClose.Click += (bcs, bce) => dlg.Close();
            pnlFtr.Controls.Add(btnClose);

            dlg.Controls.Add(tbl);
            dlg.Controls.Add(pnlHdr);
            dlg.Controls.Add(pnlFtr);
            dlg.ShowDialog(this);
        }

        private static Label MakeLblKey(string text) => new Label
        {
            Text      = text,
            Font      = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Bold),
            ForeColor = System.Drawing.Color.FromArgb(98, 112, 135),
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(0, 0, 8, 0)
        };

        private static Label MakeLblVal(string text) => new Label
        {
            Text      = text ?? "\u2014",
            Font      = new System.Drawing.Font("Segoe UI", 12f),
            ForeColor = System.Drawing.Color.FromArgb(15, 31, 53),
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        // =====================================================================
        // AppShell helper buttons
        // =====================================================================
        private Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var btn = new Button
            {
                Text      = text,
                Location  = loc,
                Size      = new System.Drawing.Size(w, h),
                BackColor = System.Drawing.Color.FromArgb(19, 35, 61),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new System.Drawing.Font("Segoe UI", 12f, System.Drawing.FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize         = 0;
            btn.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(31, 54, 96);
            btn.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(10, 22, 41);
            return btn;
        }

        private Button MakeOutlineBtn(string text, Point loc, int w, int h)
        {
            var btn = new Button
            {
                Text      = text,
                Location  = loc,
                Size      = new System.Drawing.Size(w, h),
                BackColor = System.Drawing.Color.White,
                ForeColor = System.Drawing.Color.FromArgb(15, 31, 53),
                FlatStyle = FlatStyle.Flat,
                Font      = new System.Drawing.Font("Segoe UI", 12f),
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor        = System.Drawing.Color.FromArgb(221, 227, 236);
            btn.FlatAppearance.BorderSize         = 1;
            btn.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(240, 244, 249);
            return btn;
        }

        private static void PaintCardBorder(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            var pnl = (Panel)sender;
            using var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
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
