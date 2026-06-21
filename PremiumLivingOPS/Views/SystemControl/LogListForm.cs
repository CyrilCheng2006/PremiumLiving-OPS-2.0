using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Services;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.SystemControl
{
    /// <summary>
    /// View — Log List page (System Control module).
    /// Displays all staff Add / Modify / Delete operations recorded by AuditLogger.
    ///
    /// Features:
    ///   • Grid: LogID, StaffID, Type (colour-coded), TargetTable, Timestamp
    ///   • KPI bar: Total Logs / Showing + [Export TXT] button
    ///   • Double-click row → Detail dialog (CardPanel 3-layer spec)
    ///   • Filter by keyword (searches StaffID, LogType, TargetTable)
    ///   • Export TXT: saves filtered log to user-chosen file
    /// </summary>
    public partial class LogListForm : Form
    {
        private readonly SystemControlController _ctrl = new SystemControlController();
        private List<LogEntry> _currentLogs = new List<LogEntry>();
        private Panel pnlKpi;

        public LogListForm()
        {
            InitializeComponent();
            this.Load += LogListForm_Load;
        }

        private void LogListForm_Load(object sender, EventArgs e) => RefreshGrid();

        // ── Data refresh ──────────────────────────────────────────────────────
        private void RefreshGrid()
        {
            string keyword = txtSearch.Text.Trim();
            var vm = _ctrl.GetLogListVM(string.IsNullOrEmpty(keyword) ? null : keyword);

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("System Control  \u203a  Log List");

            _currentLogs = vm.Logs;

            dgvLogs.Rows.Clear();
            foreach (var log in _currentLogs)
                dgvLogs.Rows.Add(
                    log.LogId,
                    log.StaffId,
                    log.LogType,
                    log.TargetTable,
                    log.TimeStamp);

            // Colour-code the LogType cell
            foreach (DataGridViewRow row in dgvLogs.Rows)
            {
                if (row.Cells["colLogType"].Value is string lt)
                    row.Cells["colLogType"].Style.ForeColor = LogTypeColor(lt);
            }

            RefreshKpi();
        }

        private void ResetFilters()
        {
            txtSearch.Text = string.Empty;
            RefreshGrid();
        }

        // ── KPI Bar + Export button ───────────────────────────────────────────
        private void RefreshKpi()
        {
            pnlKpi.Controls.Clear();

            var allVm = _ctrl.GetLogListVM();
            int total = allVm.Logs.Count;
            int shown = _currentLogs.Count;

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent,
                Padding = new Padding(0), AutoScroll = false
            };

            // KPI pills
            var pills = new[]
            {
                ("Total Logs", total.ToString(), Color.FromArgb(47, 111, 237), Color.FromArgb(219, 234, 254)),
                ("Showing",    shown.ToString(), Color.FromArgb(  6,  95,  70), Color.FromArgb(209, 250, 229))
            };
            foreach (var (label, count, fg, bg) in pills)
                flow.Controls.Add(MakePill(label, count, fg, bg));

            // Spacer
            flow.Controls.Add(new Panel { Width = 12, Height = 1, BackColor = Color.Transparent });

            // Export TXT button (210×60, right of pills)
            var btnExport = new Button
            {
                Text      = "\uD83D\uDCBE  Export TXT",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(19, 35, 61),
                FlatStyle = FlatStyle.Flat,
                Width     = 210, Height = 60,
                Cursor    = Cursors.Hand,
                Margin    = new Padding(0)
            };
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.FlatAppearance.MouseOverBackColor = Color.FromArgb(47, 68, 110);
            btnExport.Click += BtnExport_Click;
            flow.Controls.Add(btnExport);

            pnlKpi.Controls.Add(flow);
        }

        // ── Export TXT ────────────────────────────────────────────────────────
        private void BtnExport_Click(object sender, EventArgs e)
        {
            if (_currentLogs.Count == 0)
            {
                MessageBox.Show("No log entries to export.", "Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dlg = new SaveFileDialog
            {
                Title            = "Export Audit Log",
                Filter           = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                FileName         = $"audit_export_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                DefaultExt       = "txt",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            var sb = new StringBuilder();
            sb.AppendLine("PremiumLiving OPS 2.0 — Audit Log Export");
            sb.AppendLine($"Exported : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Records  : {_currentLogs.Count}");
            sb.AppendLine(new string('=', 80));
            sb.AppendLine();

            foreach (var log in _currentLogs)
            {
                sb.AppendLine($"[{log.TimeStamp}] [{log.LogType?.ToUpper()}] " +
                              $"LogID={log.LogId} Staff={log.StaffId} Table={log.TargetTable}");
                if (!string.IsNullOrWhiteSpace(log.OldValue))
                    sb.AppendLine($"  OLD: {log.OldValue}");
                if (!string.IsNullOrWhiteSpace(log.NewValue))
                    sb.AppendLine($"  NEW: {log.NewValue}");
                sb.AppendLine(new string('-', 80));
            }

            try
            {
                File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show($"Log exported successfully.\n{dlg.FileName}",
                    "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Grid events ───────────────────────────────────────────────────────
        private void dgvLogs_SelectionChanged(object sender, EventArgs e) { }

        private void dgvLogs_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            ShowDetailDialog(e.RowIndex);
        }

        // ── Detail dialog (CardPanel 3-layer nested spec) ─────────────────────
        private void ShowDetailDialog(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _currentLogs.Count) return;
            var log = _currentLogs[rowIndex];

            using var dlg = new Form
            {
                Text            = $"Log — {log.LogId}",
                Size            = new Size(1100, 680),
                MinimumSize     = new Size(900, 600),
                StartPosition   = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false, MinimizeBox = false,
                BackColor       = Color.FromArgb(240, 244, 249),
                Font            = new Font("Segoe UI", 13f)
            };

            // Header
            var typeColor = LogTypeColor(log.LogType);
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            var hdrTlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None };
            int badgeW = 160;
            hdrTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            hdrTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, badgeW));
            hdrTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            hdrTlp.Controls.Add(new Label { Text = $"Log Details  —  {log.LogId}", Font = new Font("Segoe UI", 16f, FontStyle.Bold), ForeColor = Color.White, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(32, 0, 0, 0), BackColor = Color.Transparent }, 0, 0);

            // Type badge
            var badgeBg = LogTypeBadgeBg(log.LogType);
            var badgeLbl = new Label { Text = log.LogType?.ToUpper() ?? "", Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = typeColor, BackColor = badgeBg, Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleCenter };
            badgeLbl.Paint += (s, pe) => { var lb = (Label)s; using var pen = new Pen(Color.FromArgb(60, typeColor), 1); pe.Graphics.DrawRectangle(pen, 0, 0, lb.Width - 1, lb.Height - 1); };
            hdrTlp.Controls.Add(badgeLbl, 1, 0);
            pnlHeader.Controls.Add(hdrTlp);

            // Section bar
            var pnlSection = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.FromArgb(241, 245, 255), Padding = new Padding(32, 0, 16, 0) };
            pnlSection.Paint += (s, pe) => { using var pen = new Pen(Color.FromArgb(221, 227, 236), 1); pe.Graphics.DrawLine(pen, 0, ((Panel)s).Height - 1, ((Panel)s).Width, ((Panel)s).Height - 1); };
            pnlSection.Controls.Add(new Label { Text = "\uD83D\uDCCB  Log Entry Information", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = Color.FromArgb(47, 111, 237), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false });

            // Card outer
            var fieldDefs = new[]
            {
                ("Log ID",        log.LogId),
                ("Staff ID",      log.StaffId),
                ("Log Type",      log.LogType),
                ("Target Table",  log.TargetTable),
                ("Timestamp",     log.TimeStamp),
            };
            int rowH   = 60;
            int cardRows = fieldDefs.Length + (string.IsNullOrWhiteSpace(log.OldValue) ? 0 : 1)
                                            + (string.IsNullOrWhiteSpace(log.NewValue) ? 0 : 1);
            var cardOuter = new Panel { Dock = DockStyle.Top, Height = cardRows * rowH + 32, BackColor = Color.Transparent, Padding = new Padding(20, 16, 20, 8) };
            var cardInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            cardInner.Paint += (s, pe) => { var p = (Panel)s; using var pen = new Pen(Color.FromArgb(221, 227, 236), 1); pe.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1); };

            int yPos = 0;
            foreach (var (lbl, val) in fieldDefs)
            {
                bool isType = lbl == "Log Type";
                var row = DetailRow(lbl, val, isType ? typeColor : Color.FromArgb(15, 31, 53), yPos, rowH);
                row.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                row.Width  = 1050;
                cardInner.Controls.Add(row);
                yPos += rowH;
            }
            if (!string.IsNullOrWhiteSpace(log.OldValue))
            {
                var row = DetailRow("Old Value", log.OldValue, Color.FromArgb(180, 60, 30), yPos, rowH);
                row.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; row.Width = 1050;
                cardInner.Controls.Add(row); yPos += rowH;
            }
            if (!string.IsNullOrWhiteSpace(log.NewValue))
            {
                var row = DetailRow("New Value", log.NewValue, Color.FromArgb(5, 120, 70), yPos, rowH);
                row.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; row.Width = 1050;
                cardInner.Controls.Add(row); yPos += rowH;
            }
            cardInner.Resize += (s2, _) => { var p = (Panel)s2; foreach (Control r in p.Controls) r.Width = p.Width; };
            cardOuter.Controls.Add(cardInner);

            // Footer
            var pnlFoot = new Panel { Dock = DockStyle.Bottom, Height = 80, BackColor = Color.White, Padding = new Padding(0, 10, 28, 10) };
            pnlFoot.Paint += (s, pe) => { using var pen = new Pen(Color.FromArgb(221, 227, 236), 1); pe.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0); };
            var btnClose = new Button { Text = "Close", Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White, FlatStyle = FlatStyle.Flat, Width = 180, Height = 60, Dock = DockStyle.Right, Cursor = Cursors.Hand };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236); btnClose.FlatAppearance.BorderSize = 1; btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnClose.Click += (s, ev) => dlg.Close();
            pnlFoot.Controls.Add(btnClose);

            var pnlFill = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };
            dlg.Controls.Add(pnlFill); dlg.Controls.Add(cardOuter); dlg.Controls.Add(pnlSection); dlg.Controls.Add(pnlHeader); dlg.Controls.Add(pnlFoot);
            dlg.ShowDialog(this);
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static Panel DetailRow(string label, string value, Color valColor, int top, int height)
        {
            var row = new Panel { Location = new Point(0, top), Height = height, BackColor = Color.White };
            row.Paint += (s, pe) => { using var pen = new Pen(Color.FromArgb(221, 227, 236), 1); pe.Graphics.DrawLine(pen, 0, ((Panel)s).Height - 1, ((Panel)s).Width, ((Panel)s).Height - 1); };
            var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200f));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tlp.Controls.Add(new Label { Text = label, Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = Color.FromArgb(70, 85, 110), BackColor = Color.FromArgb(248, 250, 252), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(24, 0, 8, 0) }, 0, 0);
            tlp.Controls.Add(new Label { Text = value ?? "—", Font = new Font("Segoe UI", 11f), ForeColor = valColor, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, BackColor = Color.White, Padding = new Padding(12, 0, 8, 0) }, 1, 0);
            row.Controls.Add(tlp);
            return row;
        }

        private static Panel MakePill(string label, string count, Color fg, Color bg)
        {
            var pill = new Panel { BackColor = bg, Size = new Size(220, 60), Margin = new Padding(0, 0, 10, 0) };
            var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None, Padding = new Padding(10, 0, 8, 0) };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60f));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tlp.Controls.Add(new Label { Text = count, Font = new Font("Segoe UI", 13f, FontStyle.Bold), ForeColor = fg, BackColor = Color.Transparent, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, AutoSize = false }, 0, 0);
            tlp.Controls.Add(new Label { Text = label, Font = new Font("Segoe UI", 11f),                ForeColor = fg, BackColor = Color.Transparent, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,   AutoSize = false }, 1, 0);
            pill.Controls.Add(tlp);
            return pill;
        }

        private static Color LogTypeColor(string logType) => logType switch
        {
            "Create" => Color.FromArgb(5,  150, 105),
            "Edit"   => Color.FromArgb(180, 100,   0),
            "Delete" => Color.FromArgb(185,  28,  28),
            "Login"  => Color.FromArgb( 37, 99,  235),
            _        => Color.FromArgb( 70,  85, 110)
        };

        private static Color LogTypeBadgeBg(string logType) => logType switch
        {
            "Create" => Color.FromArgb(209, 250, 229),
            "Edit"   => Color.FromArgb(254, 243, 199),
            "Delete" => Color.FromArgb(254, 226, 226),
            "Login"  => Color.FromArgb(219, 234, 254),
            _        => Color.FromArgb(240, 244, 249)
        };

        // ── Navigation & Logout ───────────────────────────────────────────────
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        { SessionManager.Clear(); Application.Restart(); }
    }
}
