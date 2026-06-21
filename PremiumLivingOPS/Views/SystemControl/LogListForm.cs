// LogListForm.cs — System Control > Log List
// Displays every audit log entry written by AuditLogger.
// Architecture:
//   AppShell (TapNavBar + UserBar)
//   CardPanel three-layer card
//   KPI Bar
//   Filter bar (keyword, date range, [Search], [Export TXT])
//   DataGridView (colour-coded by LogType)
//
// Rendering baseline: ViewShipmentForm / ShipmentDetailsDialog card structure.
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
using PremiumLivingOPS.Shared;

namespace PremiumLivingOPS.Views.SystemControl
{
    public class LogListForm : Form
    {
        // ── AppShell refs ─────────────────────────────────────────────────────────
        private AppShell _shell;

        // ── Controls ──────────────────────────────────────────────────────────────
        private DataGridView _grid;
        private TextBox      _txtKeyword;
        private DateTimePicker _dtFrom, _dtTo;
        private Label _lblTotal, _lblToday, _lblCreate, _lblEdit, _lblDelete, _lblLogin;

        // ── Data ──────────────────────────────────────────────────────────────────
        private List<AuditLogEntity> _allLogs = new List<AuditLogEntity>();
        private readonly SystemControlController _ctrl = new SystemControlController();

        public LogListForm()
        {
            Text            = "System Control — Log List";
            Size            = new Size(1600, 950);
            MinimumSize     = new Size(1200, 700);
            StartPosition   = FormStartPosition.CenterScreen;
            BackColor       = Color.FromArgb(240, 240, 245); // --color-bg
            Font            = new Font("Segoe UI", 9f);
            WindowState     = FormWindowState.Maximized;

            BuildShell();
            BuildContent();
            LoadData();
        }

        // =========================================================================
        // AppShell
        // =========================================================================
        private void BuildShell()
        {
            var vm = _ctrl.GetLogListVM();
            _shell = new AppShell(vm.AllowedMenus, vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.Dock = DockStyle.Fill;
            Controls.Add(_shell);
        }

        // =========================================================================
        // Content (placed inside AppShell.ContentArea)
        // =========================================================================
        private void BuildContent()
        {
            var content = _shell.ContentArea;

            // ── Outer card (layer 1) — white, shadow ──────────────────────────────
            var outer = CardPanel.CreateOuter();
            outer.Dock    = DockStyle.Fill;
            outer.Padding = new Padding(18);
            content.Controls.Add(outer);

            // ── Middle card (layer 2) ─────────────────────────────────────────────
            var mid = CardPanel.CreateMiddle();
            mid.Dock    = DockStyle.Fill;
            mid.Padding = new Padding(14);
            outer.Controls.Add(mid);

            // ── Inner card (layer 3) ──────────────────────────────────────────────
            var inner = CardPanel.CreateInner();
            inner.Dock    = DockStyle.Fill;
            inner.Padding = new Padding(12);
            mid.Controls.Add(inner);

            // Build sections inside inner
            BuildPageHeader(inner);
            BuildKpiBar(inner);
            BuildFilterBar(inner);
            BuildGrid(inner);
        }

        // =========================================================================
        // Page header
        // =========================================================================
        private void BuildPageHeader(Panel parent)
        {
            var pnl = new Panel { Dock = DockStyle.Top, Height = 52 };

            var lbl = new Label
            {
                Text      = "System Audit Log",
                Font      = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 45),
                AutoSize  = true,
                Location  = new Point(4, 10)
            };
            var sub = new Label
            {
                Text      = "Complete record of all Add / Modify / Delete operations performed by staff",
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(110, 110, 130),
                AutoSize  = true,
                Location  = new Point(6, 34)
            };
            pnl.Controls.AddRange(new Control[] { lbl, sub });
            parent.Controls.Add(pnl);
        }

        // =========================================================================
        // KPI Bar  (Total | Today | CREATE | EDIT | DELETE | LOGIN)
        // =========================================================================
        private void BuildKpiBar(Panel parent)
        {
            var bar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 82,
                BackColor = Color.FromArgb(247, 248, 252),
                Padding   = new Padding(0, 8, 0, 8)
            };
            var flow = new FlowLayoutPanel
            {
                Dock      = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                Padding       = new Padding(4, 0, 4, 0)
            };

            _lblTotal  = MakeKpi("Total Logs",      "0",  Color.FromArgb(40,  120, 200));
            _lblToday  = MakeKpi("Today",           "0",  Color.FromArgb(20,  160, 120));
            _lblCreate = MakeKpi("CREATE",          "0",  Color.FromArgb(30,  160,  80));
            _lblEdit   = MakeKpi("EDIT",            "0",  Color.FromArgb(200, 140,  30));
            _lblDelete = MakeKpi("DELETE",          "0",  Color.FromArgb(200,  60,  60));
            _lblLogin  = MakeKpi("LOGIN",           "0",  Color.FromArgb(100,  80, 200));

            flow.Controls.AddRange(new Control[]
                { _lblTotal.Parent, _lblToday.Parent,
                  _lblCreate.Parent, _lblEdit.Parent, _lblDelete.Parent, _lblLogin.Parent });
            bar.Controls.Add(flow);
            parent.Controls.Add(bar);
        }

        private Label MakeKpi(string title, string value, Color accent)
        {
            var pill = new Panel
            {
                Width      = 140,
                Height     = 62,
                Margin     = new Padding(4, 0, 4, 0),
                BackColor  = Color.White
            };
            pill.Paint += (s, e) =>
            {
                using var p = new System.Drawing.Pen(accent, 2);
                e.Graphics.DrawLine(p, 0, pill.Height - 2, pill.Width, pill.Height - 2);
            };

            var lTitle = new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI", 7.5f),
                ForeColor = Color.FromArgb(120, 120, 140),
                AutoSize  = false,
                Width     = 136, Height = 18,
                Location  = new Point(6, 6),
                TextAlign = ContentAlignment.MiddleLeft
            };
            var lVal = new Label
            {
                Text      = value,
                Font      = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = accent,
                AutoSize  = false,
                Width     = 136, Height = 34,
                Location  = new Point(6, 22),
                TextAlign = ContentAlignment.MiddleLeft
            };
            pill.Controls.AddRange(new Control[] { lTitle, lVal });
            return lVal;   // return value label for later update
        }

        // =========================================================================
        // Filter bar
        // =========================================================================
        private void BuildFilterBar(Panel parent)
        {
            var bar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 48,
                BackColor = Color.FromArgb(248, 249, 252),
                Padding   = new Padding(4, 6, 4, 4)
            };

            int x = 8;

            // Keyword
            bar.Controls.Add(MakeLabel("Keyword:", x, 14)); x += 60;
            _txtKeyword = new TextBox { Location = new Point(x, 10), Width = 180, Height = 26 };
            bar.Controls.Add(_txtKeyword); x += 188;

            // Date From
            bar.Controls.Add(MakeLabel("From:", x, 14)); x += 40;
            _dtFrom = new DateTimePicker { Location = new Point(x, 8), Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30) };
            bar.Controls.Add(_dtFrom); x += 138;

            // Date To
            bar.Controls.Add(MakeLabel("To:", x, 14)); x += 26;
            _dtTo = new DateTimePicker { Location = new Point(x, 8), Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            bar.Controls.Add(_dtTo); x += 138;

            // Search button
            var btnSearch = MakeButton("\uD83D\uDD0D  Search", x, 7, 100, 30,
                Color.FromArgb(40, 120, 200), Color.White);
            btnSearch.Click += (s, e) => ApplyFilter();
            bar.Controls.Add(btnSearch); x += 108;

            // Reset button
            var btnReset = MakeButton("Reset", x, 7, 70, 30,
                Color.FromArgb(200, 200, 210), Color.FromArgb(40, 40, 60));
            btnReset.Click += (s, e) =>
            {
                _txtKeyword.Clear();
                _dtFrom.Value = DateTime.Today.AddDays(-30);
                _dtTo.Value   = DateTime.Today;
                ApplyFilter();
            };
            bar.Controls.Add(btnReset); x += 78;

            // Export button
            var btnExport = MakeButton("\uD83D\uDCBE  Export TXT", x, 7, 120, 30,
                Color.FromArgb(60, 160, 90), Color.White);
            btnExport.Click += BtnExport_Click;
            bar.Controls.Add(btnExport);

            parent.Controls.Add(bar);
        }

        private Label MakeLabel(string text, int x, int y)
            => new Label { Text = text, Location = new Point(x, y), AutoSize = true,
                           Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(80, 80, 100) };

        private Button MakeButton(string text, int x, int y, int w, int h, Color back, Color fore)
        {
            var btn = new Button
            {
                Text      = text, Location = new Point(x, y),
                Width     = w,    Height   = h,
                BackColor = back, ForeColor = fore,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        // =========================================================================
        // DataGridView
        // =========================================================================
        private void BuildGrid(Panel parent)
        {
            _grid = new DataGridView
            {
                Dock              = DockStyle.Fill,
                ReadOnly          = true,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible     = false,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor       = Color.White,
                GridColor             = Color.FromArgb(220, 222, 230),
                BorderStyle           = BorderStyle.None,
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                Font                  = new Font("Segoe UI", 8.5f)
            };

            _grid.ColumnHeadersDefaultCellStyle.BackColor  = Color.FromArgb(40, 48, 70);
            _grid.ColumnHeadersDefaultCellStyle.ForeColor  = Color.White;
            _grid.ColumnHeadersDefaultCellStyle.Font       = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            _grid.ColumnHeadersHeight = 32;
            _grid.RowTemplate.Height  = 28;

            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColTime",  HeaderText = "Timestamp",    FillWeight = 130, DefaultCellStyle = { Format = "yyyy-MM-dd HH:mm:ss" } });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColType",  HeaderText = "Type",         FillWeight =  70 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColSID",   HeaderText = "Staff ID",     FillWeight =  70 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColSName", HeaderText = "Staff Name",   FillWeight = 120 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColTable", HeaderText = "Module/Table", FillWeight = 100 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColOld",   HeaderText = "Before (Old)", FillWeight = 200 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColNew",   HeaderText = "After (New)",  FillWeight = 200 });

            _grid.CellFormatting += Grid_CellFormatting;
            _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 252);

            parent.Controls.Add(_grid);
        }

        // Colour-code by LogType column
        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (_grid.Columns[e.ColumnIndex].Name != "ColType") return;
            string t = e.Value?.ToString() ?? "";
            e.CellStyle.BackColor = t switch
            {
                AuditLogger.TYPE_CREATE => Color.FromArgb(220, 250, 230),
                AuditLogger.TYPE_EDIT   => Color.FromArgb(255, 248, 220),
                AuditLogger.TYPE_DELETE => Color.FromArgb(255, 225, 225),
                AuditLogger.TYPE_LOGIN  => Color.FromArgb(225, 225, 255),
                _                       => Color.White
            };
            e.CellStyle.ForeColor = t switch
            {
                AuditLogger.TYPE_CREATE => Color.FromArgb(30, 130, 70),
                AuditLogger.TYPE_EDIT   => Color.FromArgb(160, 110, 0),
                AuditLogger.TYPE_DELETE => Color.FromArgb(180, 40, 40),
                AuditLogger.TYPE_LOGIN  => Color.FromArgb(80, 60, 180),
                _                       => Color.FromArgb(60, 60, 60)
            };
            e.CellStyle.Font      = new Font("Segoe UI", 8f, FontStyle.Bold);
            e.FormattingApplied   = true;
        }

        // =========================================================================
        // Data Loading & Filter
        // =========================================================================
        private void LoadData(string keyword = null)
        {
            _allLogs = _ctrl.GetLogListVM(keyword).Logs;
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            string kw      = _txtKeyword.Text.ToLowerInvariant();
            var    fromDate = _dtFrom.Value.Date;
            var    toDate   = _dtTo.Value.Date.AddDays(1).AddTicks(-1);

            var filtered = _allLogs
                .Where(l => l.Timestamp >= fromDate && l.Timestamp <= toDate)
                .Where(l => string.IsNullOrEmpty(kw) ||
                            l.RawLine.ToLowerInvariant().Contains(kw))
                .ToList();

            BindGrid(filtered);
            UpdateKpis(filtered);
        }

        private void BindGrid(List<AuditLogEntity> logs)
        {
            _grid.Rows.Clear();
            foreach (var l in logs)
            {
                int ri = _grid.Rows.Add();
                var row = _grid.Rows[ri];
                row.Cells["ColTime" ].Value = l.Timestamp;
                row.Cells["ColType" ].Value = l.LogType;
                row.Cells["ColSID"  ].Value = l.StaffID;
                row.Cells["ColSName"].Value = l.StaffName;
                row.Cells["ColTable"].Value = l.TargetTable;
                row.Cells["ColOld"  ].Value = l.OldValue;
                row.Cells["ColNew"  ].Value = l.NewValue;
            }
        }

        private void UpdateKpis(List<AuditLogEntity> logs)
        {
            var today = DateTime.Today;
            _lblTotal .Text = logs.Count.ToString();
            _lblToday .Text = logs.Count(l => l.Timestamp.Date == today).ToString();
            _lblCreate.Text = logs.Count(l => l.LogType == AuditLogger.TYPE_CREATE).ToString();
            _lblEdit  .Text = logs.Count(l => l.LogType == AuditLogger.TYPE_EDIT).ToString();
            _lblDelete.Text = logs.Count(l => l.LogType == AuditLogger.TYPE_DELETE).ToString();
            _lblLogin .Text = logs.Count(l => l.LogType == AuditLogger.TYPE_LOGIN).ToString();
        }

        // =========================================================================
        // Export to TXT
        // =========================================================================
        private void BtnExport_Click(object sender, EventArgs e)
        {
            using var dlg = new SaveFileDialog
            {
                Title            = "Export Audit Log",
                Filter           = "Text File (*.txt)|*.txt",
                FileName         = $"AuditLog_Export_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"PremiumLiving OPS 2.0 — Audit Log Export");
                sb.AppendLine($"Generated : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Exported by: {SessionManager.CurrentUser?.StaffName ?? "Unknown"}");
                sb.AppendLine(new string('-', 120));
                sb.AppendLine();

                // Header
                sb.AppendLine($"{"Timestamp",-22} {"Type",-8} {"StaffID",-10} {"StaffName",-20} {"Table",-16} {"Old Value",-40} New Value");
                sb.AppendLine(new string('-', 140));

                foreach (DataGridViewRow row in _grid.Rows)
                {
                    if (row.IsNewRow) continue;
                    string ts    = row.Cells["ColTime" ].Value is DateTime dt ? dt.ToString("yyyy-MM-dd HH:mm:ss") : "";
                    string type  = row.Cells["ColType" ].Value?.ToString() ?? "";
                    string sid   = row.Cells["ColSID"  ].Value?.ToString() ?? "";
                    string sname = row.Cells["ColSName"].Value?.ToString() ?? "";
                    string tbl   = row.Cells["ColTable"].Value?.ToString() ?? "";
                    string old   = row.Cells["ColOld"  ].Value?.ToString() ?? "";
                    string @new  = row.Cells["ColNew"  ].Value?.ToString() ?? "";

                    sb.AppendLine($"{ts,-22} {type,-8} {sid,-10} {sname,-20} {tbl,-16} {old,-40} {@new}");
                }

                File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show($"Export successful!\n{dlg.FileName}",
                                "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed:\n{ex.Message}", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
