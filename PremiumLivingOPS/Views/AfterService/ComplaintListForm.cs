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
                { "Completed",  (Color.FromArgb(220, 252, 231), Color.FromArgb( 22, 101,  52)) },
            };

        private const int D_RowH   = 80;
        private const int D_LabelW = 260;
        private const int D_BtnW   = 200;
        private const int D_BtnH   = 56;

        // ── Dialog field layout constants
        private const int DLG_LabelW = 340;   // wider label column
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
        //  KPI Pills  (pills only — action buttons live in pnlActionBtns in Designer)
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

        // ════════════════════════════════════════════════════════════════
        //  ★ ADD NEW — Create Complaint Dialog
        //  Fields (one per row):
        //    Complaint ID  — auto-generated, read-only
        //    Order No.     — optional text input
        //    Handled By    — staff ComboBox (required)
        //    Status        — ENUM ComboBox (required)
        //    Description   — text input (required)
        // ════════════════════════════════════════════════════════════════
        private void btnAddNew_Click(object sender, EventArgs e)
        {
            // ── Auto-generate Complaint ID ─────────────────────────────────────────
            string autoId;
            try   { autoId = _ctrl.CreateComplaint(null) ? "" : ""; } // just a probe; real call below
            catch { autoId = ""; }
            // Use the repo-level generator via a temporary entity (ID only)
            autoId = GeneratePreviewComplaintId();

            // ── Dialog shell ─────────────────────────────────────────────────────
            using var dlg = new Form
            {
                Text            = "Create New Complaint",
                Size            = new Size(1400, 800),
                MinimumSize     = new Size(1100, 800),
                StartPosition   = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false,
                BackColor       = Color.FromArgb(240, 244, 249),
                Font            = new Font("Segoe UI", 13f)
            };

            // ── Header ──────────────────────────────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top, Height = 80,
                BackColor = Color.FromArgb(19, 35, 61)
            };
            pnlHeader.Controls.Add(new Label
            {
                Text      = "\u2795  Create New Complaint",
                Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false,
                Padding   = new Padding(32, 0, 0, 0)
            });

            // ── Section title bar ───────────────────────────────────────────────────
            var pnlSectionTitle = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 44,
                BackColor = Color.FromArgb(241, 245, 255),
                Padding   = new Padding(32, 0, 16, 0)
            };
            PaintBottomBorderStatic(pnlSectionTitle);
            pnlSectionTitle.Controls.Add(new Label
            {
                Text      = "\uD83D\uDCCB  Complaint Information",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(47, 111, 237),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false
            });

            // ── Field builder (one per row, label left / input right) ────────────────
            Panel MakeRow(string labelText, Control input, bool lastRow = false)
            {
                var row = new Panel { Height = DLG_RowH, BackColor = Color.White };
                if (!lastRow)
                    row.Paint += (s2, pe) =>
                    {
                        using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                        pe.Graphics.DrawLine(pen, 0, ((Panel)s2).Height - 1, ((Panel)s2).Width, ((Panel)s2).Height - 1);
                    };

                var tlp = new TableLayoutPanel
                {
                    Dock            = DockStyle.Fill,
                    ColumnCount     = 2,
                    RowCount        = 1,
                    BackColor       = Color.White,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DLG_LabelW));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                var lbl = new Label
                {
                    Text      = labelText,
                    Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(70, 85, 110),
                    BackColor = Color.FromArgb(248, 250, 252),
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoSize  = false,
                    Padding   = new Padding(24, 0, 8, 0)
                };
                var wrap = new Panel
                {
                    Dock      = DockStyle.Fill,
                    BackColor = Color.White,
                    Padding   = new Padding(20, 14, 24, 14)
                };
                input.Dock = DockStyle.Fill;
                wrap.Controls.Add(input);
                tlp.Controls.Add(lbl,  0, 0);
                tlp.Controls.Add(wrap, 1, 0);
                row.Controls.Add(tlp);
                return row;
            }

            // ── Controls ────────────────────────────────────────────────────────────
            // 1. Complaint ID (read-only)
            var txtComplaintId = new TextBox
            {
                Text        = autoId,
                Font        = new Font("Segoe UI", 12f, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly    = true,
                BackColor   = Color.FromArgb(240, 244, 249),
                ForeColor   = Color.FromArgb(47, 111, 237)
            };

            // 2. Order No. (optional)
            var txtOrderNo = new TextBox
            {
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                PlaceholderText = "e.g. ORD-20260311-0048  (optional)"
            };

            // 3. Handled By staff (required)
            var cboStaff = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 12f),
                DisplayMember = "Display"
            };
            try
            {
                foreach (var s in _ctrl.GetStaffList())
                    cboStaff.Items.Add(new StaffItem { StaffID = s.StaffID, StaffName = s.StaffName });
            }
            catch
            {
                cboStaff.Items.Add(new StaffItem { StaffID = "", StaffName = "(No staff loaded)" });
            }
            if (cboStaff.Items.Count > 0) cboStaff.SelectedIndex = 0;

            // 4. Status (required ENUM)
            var cboStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 12f)
            };
            cboStatus.Items.AddRange(new object[] { "Pending", "Processing", "Escalated", "Completed" });
            cboStatus.SelectedIndex = 0;

            // 5. Description (required)
            var txtDesc = new TextBox
            {
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                Multiline       = false,
                PlaceholderText = "Describe the complaint in detail"
            };

            // ── Build rows (one field per row) ──────────────────────────────────────
            var rows = new Panel[]
            {
                MakeRow("Complaint ID  (auto)", txtComplaintId),
                MakeRow("Order No.",            txtOrderNo),
                MakeRow("Handled By *",         cboStaff),
                MakeRow("Status *",             cboStatus),
                MakeRow("Description *",        txtDesc, lastRow: true)
            };

            // ── White card wrapping the rows ────────────────────────────────────────
            int cardHeight = rows.Length * DLG_RowH;
            var cardOuter = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = cardHeight + 32,
                BackColor = Color.Transparent,
                Padding   = new Padding(20, 16, 20, 16)
            };
            var cardInner = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.White,
                Padding   = new Padding(0)
            };
            cardInner.Paint += (s2, pe) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                pe.Graphics.DrawRectangle(pen, 0, 0, ((Panel)s2).Width - 1, ((Panel)s2).Height - 1);
            };

            int y2 = 0;
            foreach (var r in rows)
            {
                r.Location = new Point(0, y2);
                r.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                r.Width    = 1200; // will be resized by Resize event
                cardInner.Controls.Add(r);
                y2 += DLG_RowH;
            }
            cardInner.Resize += (s2, _) =>
            {
                var p = (Panel)s2;
                foreach (Control r in p.Controls) r.Width = p.Width;
            };
            cardOuter.Controls.Add(cardInner);

            // ── Footer ──────────────────────────────────────────────────────────────
            var pnlFoot = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 96,
                BackColor = Color.White,
                Padding   = new Padding(0, 18, 28, 18)
            };
            pnlFoot.Paint += (s2, pe) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                pe.Graphics.DrawLine(pen, 0, 0, ((Panel)s2).Width, 0);
            };

            var btnCreate = new Button
            {
                Text      = "\u2714  Create Complaint",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(5, 150, 105),
                FlatStyle = FlatStyle.Flat,
                Width     = DLG_BtnW,
                Height    = DLG_BtnH,
                Cursor    = Cursors.Hand,
                Margin    = new Padding(0, 0, 10, 0)
            };
            btnCreate.FlatAppearance.BorderSize         = 0;
            btnCreate.FlatAppearance.MouseOverBackColor = Color.FromArgb(4, 120, 87);

            var btnCancel = new Button
            {
                Text      = "Cancel",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Width     = DLG_BtnW,
                Height    = DLG_BtnH,
                Cursor    = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnCancel.FlatAppearance.BorderSize         = 1;
            btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);

            bool confirmed = false;

            btnCreate.Click += (s2, ev) =>
            {
                if (string.IsNullOrWhiteSpace(txtDesc.Text))
                {
                    MessageBox.Show("Description is required.",
                        "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtDesc.Focus(); return;
                }
                if (cboStaff.SelectedItem == null)
                {
                    MessageBox.Show("Please select a staff member.",
                        "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cboStaff.Focus(); return;
                }
                confirmed = true;
                dlg.Close();
            };
            btnCancel.Click += (s2, ev) => dlg.Close();

            var footFlow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Right,
                AutoSize      = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor     = Color.Transparent
            };
            footFlow.Controls.Add(btnCreate);
            footFlow.Controls.Add(btnCancel);
            pnlFoot.Controls.Add(footFlow);

            // ── Assemble dialog ─────────────────────────────────────────────────────
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
                var selectedStaff = cboStaff.SelectedItem as StaffItem;
                var entity = new ComplaintEntity
                {
                    ComplaintID          = txtComplaintId.Text.Trim(),
                    OrderID              = string.IsNullOrWhiteSpace(txtOrderNo.Text)
                                              ? null
                                              : txtOrderNo.Text.Trim(),
                    StaffID              = selectedStaff?.StaffID ?? string.Empty,
                    ComplaintStatus      = cboStatus.SelectedItem?.ToString() ?? "Pending",
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
                {
                    MessageBox.Show("Failed to create complaint. Please try again.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create complaint:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Generates a preview ComplaintID using the same format as the repo
        /// (CMP-YYYYMMDD-NNNN) without touching the database insert.
        /// </summary>
        private string GeneratePreviewComplaintId()
        {
            try
            {
                // Delegate to controller which calls repo's GenerateComplaintId
                // We create a dummy entity so the controller returns an ID.
                // But CreateComplaint would insert — use a workaround:
                // Reconstruct the same logic here (date + sequence placeholder).
                string prefix = "CMP-" + DateTime.Today.ToString("yyyyMMdd") + "-";
                // Ask the controller to generate the ID (repo reads MAX sequence)
                // We expose it via a lightweight wrapper call:
                return ComplaintIdPreview(prefix);
            }
            catch
            {
                return "CMP-" + DateTime.Today.ToString("yyyyMMdd") + "-XXXX";
            }
        }

        private string ComplaintIdPreview(string prefix)
        {
            // Mirror of AfterServiceRepo.GenerateComplaintId logic:
            // Read existing IDs from DB and find next sequence.
            var existing = new List<string>();
            try
            {
                var connStr = PremiumLivingOPS.Models.DAL.DBHelper.ConnectionString;
                using var conn = new MySql.Data.MySqlClient.MySqlConnection(connStr);
                conn.Open();
                using var cmd = new MySql.Data.MySqlClient.MySqlCommand(
                    "SELECT ComplaintID FROM Complaint WHERE ComplaintID LIKE @p", conn);
                cmd.Parameters.AddWithValue("@p", prefix + "%");
                using var rdr = cmd.ExecuteReader();
                while (rdr.Read()) existing.Add(rdr.GetString(0));
            }
            catch { }

            int next = 1;
            foreach (var id in existing)
            {
                if (id.Length >= prefix.Length + 4 &&
                    int.TryParse(id.Substring(prefix.Length, 4), out int seq) &&
                    seq >= next)
                    next = seq + 1;
            }
            return $"{prefix}{next:D4}";
        }

        // ════════════════════════════════════════════════════════════════
        //  Update Status Dialog
        // ════════════════════════════════════════════════════════════════
        private void btnUpdateStatus_Click(object sender, EventArgs e)
        {
            if (dgvComplaints.SelectedRows.Count == 0) return;
            string id  = dgvComplaints.SelectedRows[0].Cells["colComplaintID"].Value?.ToString();
            var    ent = _currentComplaints.Find(x => x.ComplaintID == id);
            if (ent == null) return;

            Label ReadLabel(string text) => new Label
            {
                Text      = text ?? "\u2014",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.White
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
                var wrap = new Panel
                {
                    Dock = DockStyle.Fill, BackColor = Color.White,
                    Padding = new Padding(20, 12, 20, 12)
                };
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
                Font = new Font("Segoe UI", 12f),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 31, 53),
            };
            cboNew.Items.AddRange(new object[] { "Pending", "Processing", "Escalated", "Completed" });
            cboNew.SelectedItem = ent.ComplaintStatus;

            var rows = new Panel[]
            {
                FieldRow("Complaint ID",  ReadLabel(ent.ComplaintID)),
                FieldRow("Order No.",      ReadLabel(ent.OrderID ?? "\u2014")),
                FieldRow("Current Status", ReadLabel(ent.ComplaintStatus)),
                FieldRow("New Status",     cboNew, lastRow: true)
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
                Dock = DockStyle.Fill, AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter
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
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent, Padding = new Padding(40, 0, 0, 0)
            }, 0, 0);
            headerTlp.Controls.Add(statusLbl, 1, 0);

            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top, Height = 88,
                BackColor = Color.FromArgb(19, 35, 61)
            };
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

            var scroll = new Panel
            {
                Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249), AutoScroll = true
            };
            scroll.Controls.Add(cardOuter);

            dlg.Controls.Add(scroll);
            dlg.Controls.Add(pnlFoot);
            dlg.Controls.Add(pnlHeader);

            if (dlg.ShowDialog(this) == DialogResult.OK) RefreshGrid();
        }

        // ════════════════════════════════════════════════════════════════
        //  View Detail Dialog
        // ════════════════════════════════════════════════════════════════
        private void btnViewDetail_Click(object sender, EventArgs e) => ShowDetailDialog();

        private void ShowDetailDialog()
        {
            if (dgvComplaints.SelectedRows.Count == 0) return;
            string id = dgvComplaints.SelectedRows[0].Cells["colComplaintID"].Value?.ToString();
            var c = _currentComplaints.Find(x => x.ComplaintID == id);
            if (c == null) return;

            Label ReadLabel(string text) => new Label
            {
                Text      = text ?? "\u2014",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.White
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
                var wrap = new Panel
                {
                    Dock = DockStyle.Fill, BackColor = Color.White,
                    Padding = new Padding(20, 12, 20, 12)
                };
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
                Dock = DockStyle.Fill, AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter
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
            headerTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            headerTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, statusColW));
            headerTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            headerTlp.Controls.Add(new Label
            {
                Text = $"Complaint  \u2014  {c.ComplaintID}",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent, Padding = new Padding(40, 0, 0, 0)
            }, 0, 0);
            headerTlp.Controls.Add(statusLbl, 1, 0);

            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top, Height = 88,
                BackColor = Color.FromArgb(19, 35, 61)
            };
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

            var scroll = new Panel
            {
                Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249), AutoScroll = true
            };
            scroll.Controls.Add(c2Outer);
            scroll.Controls.Add(c1Outer);

            dlg.Controls.Add(scroll);
            dlg.Controls.Add(pnlFoot);
            dlg.Controls.Add(pnlHeader);
            dlg.ShowDialog(this);
        }

        // ════════════════════════════════════════════════════════════════
        //  Shared helpers
        // ════════════════════════════════════════════════════════════════
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

        private static Label MakeLabelKey(string text) => new Label
        {
            Text      = text,
            Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = Color.FromArgb(98, 112, 135),
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize  = false,
            Padding   = new Padding(0, 0, 8, 0)
        };

        private static void PaintBottomBorderStatic(Panel p)
        {
            p.Paint += (s, e) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawLine(pen, 0, ((Panel)s).Height - 1, ((Panel)s).Width, ((Panel)s).Height - 1);
            };
        }

        // ════════════════════════════════════════════════════════════════
        //  Navigation / logout
        // ════════════════════════════════════════════════════════════════
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        { SessionManager.Clear(); Application.Restart(); }

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
