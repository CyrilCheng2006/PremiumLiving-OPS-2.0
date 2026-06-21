// LogListForm.cs — System Control > Log List  (partial — UI defined in Designer.cs)
// Displays every audit log entry written by AuditLogger.
// All UI construction is in LogListForm.Designer.cs (AppShell, cards, DGV, KPI strip).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        // ── KPI label refs (populated in RefreshGrid) ────────────────────────────────
        private Label _lblTotal;
        private Label _lblToday;
        private Label _lblCreate;
        private Label _lblEdit;
        private Label _lblDelete;
        private Label _lblLogin;

        // ── Data ──────────────────────────────────────────────────────────────────
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
        // KPI strip builder
        // =====================================================================
        private void RefreshKpi()
        {
            pnlKpi.Controls.Clear();

            var flow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent,
                Padding       = new Padding(0),
                AutoScroll    = false
            };

            _lblTotal  = AddKpiPill(flow, "Total Logs",
                Color.FromArgb( 47, 111, 237), Color.FromArgb(219, 234, 254));
            _lblToday  = AddKpiPill(flow, "Today",
                Color.FromArgb(  6,  95,  70), Color.FromArgb(209, 250, 229));
            _lblCreate = AddKpiPill(flow, "CREATE",
                Color.FromArgb( 22, 163,  74), Color.FromArgb(220, 252, 231));
            _lblEdit   = AddKpiPill(flow, "EDIT",
                Color.FromArgb(161, 110,   0), Color.FromArgb(254, 243, 199));
            _lblDelete = AddKpiPill(flow, "DELETE",
                Color.FromArgb(185,  28,  28), Color.FromArgb(254, 226, 226));
            _lblLogin  = AddKpiPill(flow, "LOGIN",
                Color.FromArgb( 91,  33, 182), Color.FromArgb(237, 233, 254));

            // ── Export TXT button — right side of KPI bar (210 × 60) ──────────
            var btnExport = new Button
            {
                Text      = "\uD83D\uDCE5  Export Log TXT",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(19, 35, 61),
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(210, 60),
                Margin    = new Padding(16, 0, 0, 0),
                Cursor    = Cursors.Hand
            };
            btnExport.FlatAppearance.BorderSize         = 0;
            btnExport.FlatAppearance.MouseOverBackColor = Color.FromArgb(31, 54, 96);
            btnExport.FlatAppearance.MouseDownBackColor = Color.FromArgb(10, 22, 41);
            btnExport.Click += BtnExport_Click;

            flow.Controls.Add(btnExport);
            pnlKpi.Controls.Add(flow);
        }

        // =====================================================================
        // Export TXT
        // =====================================================================
        private void BtnExport_Click(object sender, EventArgs e)
        {
            // Collect current filtered / all logs from the grid display
            string kw = txtSearch.Text.Trim().ToLowerInvariant();
            var toExport = string.IsNullOrEmpty(kw)
                ? _allLogs
                : _allLogs.Where(l =>
                    (l.StaffID     ?? "").ToLowerInvariant().Contains(kw) ||
                    (l.LogType     ?? "").ToLowerInvariant().Contains(kw) ||
                    (l.TargetTable ?? "").ToLowerInvariant().Contains(kw) ||
                    (l.StaffName   ?? "").ToLowerInvariant().Contains(kw)
                  ).ToList();

            if (toExport.Count == 0)
            {
                MessageBox.Show("No log entries to export.",
                    "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Title            = "Export Audit Log",
                Filter           = "Text File (*.txt)|*.txt",
                FileName         = $"AuditLog_Export_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("==============================================================");
                sb.AppendLine($"  PREMIUM LIVING OPS — AUDIT LOG EXPORT");
                sb.AppendLine($"  Exported by : {SessionManager.CurrentUser?.StaffName ?? "Unknown"}");
                sb.AppendLine($"  Exported at : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"  Total Entries: {toExport.Count}");
                if (!string.IsNullOrEmpty(kw))
                    sb.AppendLine($"  Filter Keyword: \"{txtSearch.Text.Trim()}\"");
                sb.AppendLine("==============================================================");
                sb.AppendLine();

                // Column headers
                sb.AppendLine(
                    $"{"Timestamp",-22}" +
                    $"{"Type",-10}" +
                    $"{"Staff ID",-12}" +
                    $"{"Staff Name",-20}" +
                    $"{"Module/Table",-20}" +
                    "Details");
                sb.AppendLine(new string('-', 120));

                foreach (var l in toExport)
                {
                    string details = string.Empty;
                    if (!string.IsNullOrWhiteSpace(l.OldValue) || !string.IsNullOrWhiteSpace(l.NewValue))
                        details = $"OLD: {(string.IsNullOrWhiteSpace(l.OldValue) ? "-" : l.OldValue)}  |  NEW: {(string.IsNullOrWhiteSpace(l.NewValue) ? "-" : l.NewValue)}";

                    sb.AppendLine(
                        $"{l.Timestamp:yyyy-MM-dd HH:mm:ss,-22}" +
                        $"{(l.LogType ?? ""),-10}" +
                        $"{(l.StaffID ?? ""),-12}" +
                        $"{(l.StaffName ?? ""),-20}" +
                        $"{(l.TargetTable ?? ""),-20}" +
                        details);
                }

                sb.AppendLine();
                sb.AppendLine("-- END OF EXPORT --");

                File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);

                // Write audit entry for the export action itself
                AuditLogger.Write(AuditLogger.TYPE_EDIT, "AuditLog",
                    oldValue: null,
                    newValue: AuditLogger.Snapshot(
                        ("Action",  "Export"),
                        ("Entries", toExport.Count.ToString()),
                        ("File",    Path.GetFileName(sfd.FileName))));

                MessageBox.Show(
                    $"Export completed.\n\n{toExport.Count} entries saved to:\n{sfd.FileName}",
                    "Export Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed.\n\n{ex.Message}",
                    "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(24, 0, 0, 0)
            });

            var tbl = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 2,
                RowCount        = 7,
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
            pnlFtr.Paint += (fps, fpe) => { using var pen = new Pen(Color.FromArgb(221, 227, 236), 1); fpe.Graphics.DrawLine(pen, 0, 0, ((Panel)fps).Width, 0); };
            var btnClose = new Button { Text = "Close", Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White, FlatStyle = FlatStyle.Flat, Dock = DockStyle.Right, Width = 130, Cursor = Cursors.Hand };
            btnClose.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
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
            Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(98, 112, 135),
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(0, 0, 8, 0)
        };

        private static Label MakeLblVal(string text) => new Label
        {
            Text      = text ?? "\u2014",
            Font      = new Font("Segoe UI", 12f),
            ForeColor = Color.FromArgb(15, 31, 53),
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
                Size      = new Size(w, h),
                BackColor = Color.FromArgb(19, 35, 61),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize         = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(31, 54, 96);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(10, 22, 41);
            return btn;
        }

        private Button MakeOutlineBtn(string text, Point loc, int w, int h)
        {
            var btn = new Button
            {
                Text      = text,
                Location  = loc,
                Size      = new Size(w, h),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 31, 53),
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 12f),
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btn.FlatAppearance.BorderSize         = 1;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            return btn;
        }

        private static void PaintCardBorder(object sender, PaintEventArgs e)
        {
            var pnl = (Panel)sender;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
        }

        // =====================================================================
        // Navigation & Logout
        // =====================================================================
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }
    }
}
