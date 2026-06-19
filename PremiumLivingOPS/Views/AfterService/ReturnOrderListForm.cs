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
    public partial class ReturnOrderListForm : Form
    {
        private readonly AfterServiceController _ctrl = new AfterServiceController();
        private List<ReturnOrderEntity> _currentReturns = new List<ReturnOrderEntity>();

        private static readonly Dictionary<string, (Color bg, Color fg)> StatusColors =
            new Dictionary<string, (Color, Color)>
            {
                { "Pending",    (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "Approved",   (Color.FromArgb(219, 234, 254), Color.FromArgb( 29,  78, 216)) },
                { "Processing", (Color.FromArgb(237, 233, 254), Color.FromArgb( 91,  33, 182)) },
                { "Rejected",   (Color.FromArgb(254, 226, 226), Color.FromArgb(185,  28,  28)) },
                { "Completed",  (Color.FromArgb(220, 252, 231), Color.FromArgb( 22, 101,  52)) },
            };

        // ── Layout constants shared by both dialogs (mirrors ComplaintListForm)
        private const int D_RowH   = 80;
        private const int D_LabelW = 260;
        private const int D_BtnW   = 200;
        private const int D_BtnH   = 56;

        public ReturnOrderListForm()
        {
            InitializeComponent();
            this.Load += ReturnOrderListForm_Load;
        }

        private void ReturnOrderListForm_Load(object sender, EventArgs e) => RefreshGrid();

        // ════════════════════════════════════════════════════════════════
        //  Refresh
        // ════════════════════════════════════════════════════════════════
        private void RefreshGrid()
        {
            string statusSel    = cboStatus.SelectedItem?.ToString();
            string statusFilter = (statusSel == "All" || string.IsNullOrEmpty(statusSel)) ? null : statusSel;
            string keyword      = txtKeyword.Text.Trim();

            var vm = _ctrl.GetReturnOrderListVM(statusFilter, string.IsNullOrEmpty(keyword) ? null : keyword);

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("After-Service  \u203a  Return Order List");

            _currentReturns = vm.ReturnOrders;

            dgvReturns.Rows.Clear();
            foreach (var r in _currentReturns)
                dgvReturns.Rows.Add(
                    r.ReturnID,
                    r.OrderID,
                    r.CustomerName,
                    r.ReturnDate.ToString("yyyy-MM-dd"),
                    r.Reason ?? "\u2014",
                    $"HK$ {r.RefundAmount:N2}",
                    r.ReturnStatus);

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

            var all = _ctrl.GetReturnOrderListVM().ReturnOrders;

            int total      = all.Count;
            int pending    = all.FindAll(r => r.ReturnStatus == "Pending").Count;
            int approved   = all.FindAll(r => r.ReturnStatus == "Approved").Count;
            int processing = all.FindAll(r => r.ReturnStatus == "Processing").Count;
            int rejected   = all.FindAll(r => r.ReturnStatus == "Rejected").Count;
            int completed  = all.FindAll(r => r.ReturnStatus == "Completed").Count;

            var pills = new[]
            {
                ("Total",      total.ToString(),      Color.FromArgb( 47, 111, 237), Color.FromArgb(219, 234, 254), "All"),
                ("Pending",    pending.ToString(),    Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199), "Pending"),
                ("Approved",   approved.ToString(),   Color.FromArgb( 29,  78, 216), Color.FromArgb(219, 234, 254), "Approved"),
                ("Processing", processing.ToString(), Color.FromArgb( 91,  33, 182), Color.FromArgb(237, 233, 254), "Processing"),
                ("Rejected",   rejected.ToString(),   Color.FromArgb(185,  28,  28), Color.FromArgb(254, 226, 226), "Rejected"),
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
            bool sel = dgvReturns.SelectedRows.Count > 0;
            btnUpdateStatus.Enabled = sel;
            btnViewDetail.Enabled   = sel;
            // btnAddNew is always enabled
        }

        private void dgvReturns_SelectionChanged(object sender, EventArgs e) => UpdateActionButtons();

        private void dgvReturns_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvReturns.Columns[e.ColumnIndex].Name != "colStatus" || e.Value == null) return;
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
        //  Add New — Create Return Order
        // ════════════════════════════════════════════════════════════════
        private void btnAddNew_Click(object sender, EventArgs e)
        {
            using var dlg = new CreateReturnOrderDialog(_ctrl);
            if (dlg.ShowDialog(this) == DialogResult.OK)
                RefreshGrid();
        }

        // ════════════════════════════════════════════════════════════════
        //  Update Status Dialog  (mirrors ComplaintListForm pattern)
        // ════════════════════════════════════════════════════════════════
        private void btnUpdateStatus_Click(object sender, EventArgs e)
        {
            if (dgvReturns.SelectedRows.Count == 0) return;
            string id  = dgvReturns.SelectedRows[0].Cells["colReturnID"].Value?.ToString();
            var    ent = _currentReturns.Find(x => x.ReturnID == id);
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
                    Dock            = DockStyle.Fill,
                    ColumnCount     = 2,
                    RowCount        = 1,
                    BackColor       = Color.White,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D_LabelW));
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
                    Padding   = new Padding(20, 0, 8, 0)
                };
                var wrap = new Panel
                {
                    Dock      = DockStyle.Fill,
                    BackColor = Color.White,
                    Padding   = new Padding(20, 12, 20, 12)
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
                Font          = new Font("Segoe UI", 12f),
                FlatStyle     = FlatStyle.Flat,
                BackColor     = Color.White,
                ForeColor     = Color.FromArgb(15, 31, 53),
            };
            cboNew.Items.AddRange(new object[] { "Pending", "Approved", "Processing", "Rejected", "Completed" });
            cboNew.SelectedItem = ent.ReturnStatus;

            var rows = new Panel[]
            {
                FieldRow("Return ID",      ReadLabel(ent.ReturnID)),
                FieldRow("Order No.",      ReadLabel(ent.OrderID)),
                FieldRow("Current Status", ReadLabel(ent.ReturnStatus)),
                FieldRow("New Status",     cboNew, lastRow: true)
            };
            var (cardOuter, cardInner) = CardPanel.Create(
                outerHeight: rows.Length * D_RowH + 22,
                outerPadding: new Padding(20, 14, 20, 8));
            cardInner.Padding = new Padding(0);
            cardInner.Controls.Add(BuildStack(rows));

            // ── Dialog: 1800 × 700 ───────────────────────────────────────────────────────
            using var dlg = new Form
            {
                Text            = $"Update Return Order Status  \u2014  {ent.ReturnID}",
                Size            = new Size(1800, 700),
                MinimumSize     = new Size(1800, 700),
                StartPosition   = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false,
                BackColor       = Color.FromArgb(240, 244, 249),
                Font            = new Font("Segoe UI", 12f)
            };

            Color pillBg = Color.FromArgb(229, 231, 235);
            Color pillFg = Color.FromArgb(55, 65, 81);
            if (StatusColors.TryGetValue(ent.ReturnStatus ?? "", out var hsc))
            { pillBg = hsc.bg; pillFg = hsc.fg; }

            var statusFont = new Font("Segoe UI", 13f, FontStyle.Bold);
            int textW      = TextRenderer.MeasureText(ent.ReturnStatus ?? "\u2014", statusFont).Width;
            int statusColW = textW + 80;

            var statusLbl = new Label
            {
                Text      = ent.ReturnStatus ?? "\u2014",
                Font      = statusFont,
                ForeColor = pillFg,
                BackColor = pillBg,
                Dock      = DockStyle.Fill,
                AutoSize  = false,
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
                Dock            = DockStyle.Fill,
                ColumnCount     = 2,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            headerTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            headerTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, statusColW));
            headerTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            headerTlp.Controls.Add(new Label
            {
                Text      = $"Update Status  \u2014  {ent.ReturnID}",
                Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                Padding   = new Padding(40, 0, 0, 0)
            }, 0, 0);
            headerTlp.Controls.Add(statusLbl, 1, 0);

            var pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 88,
                BackColor = Color.FromArgb(19, 35, 61)
            };
            pnlHeader.Controls.Add(headerTlp);

            var pnlFoot = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 96,
                BackColor = Color.White,
                Padding   = new Padding(0, 18, 40, 18)
            };
            pnlFoot.Paint += (s2, pe) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                pe.Graphics.DrawLine(pen, 0, 0, ((Panel)s2).Width, 0);
            };

            var btnConfirm = new Button
            {
                Text      = "Confirm",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                BackColor = Color.FromArgb(19, 35, 61),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Width     = D_BtnW,
                Height    = D_BtnH,
                Cursor    = Cursors.Hand,
                Margin    = new Padding(0, 0, 12, 0)
            };
            btnConfirm.FlatAppearance.BorderSize = 0;

            var btnCancel = new Button
            {
                Text      = "Cancel",
                Font      = new Font("Segoe UI", 13f),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 31, 53),
                FlatStyle = FlatStyle.Flat,
                Width     = D_BtnW,
                Height    = D_BtnH,
                Cursor    = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(200, 207, 220);
            btnCancel.FlatAppearance.BorderSize  = 1;

            btnConfirm.Click += (s2, ev) =>
            {
                if (cboNew.SelectedItem == null) return;
                bool ok = _ctrl.UpdateReturnOrderStatus(id, cboNew.SelectedItem.ToString());
                if (ok) { dlg.DialogResult = DialogResult.OK; dlg.Close(); }
                else MessageBox.Show("Update failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            btnCancel.Click += (s2, ev) => dlg.Close();

            var footFlow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Right,
                AutoSize      = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor     = Color.Transparent
            };
            footFlow.Controls.Add(btnConfirm);
            footFlow.Controls.Add(btnCancel);
            pnlFoot.Controls.Add(footFlow);

            var scroll = new Panel
            {
                Dock       = DockStyle.Fill,
                BackColor  = Color.FromArgb(240, 244, 249),
                AutoScroll = true
            };
            scroll.Controls.Add(cardOuter);

            dlg.Controls.Add(scroll);
            dlg.Controls.Add(pnlFoot);
            dlg.Controls.Add(pnlHeader);

            if (dlg.ShowDialog(this) == DialogResult.OK) RefreshGrid();
        }

        // ════════════════════════════════════════════════════════════════
        //  View Detail Dialog  (mirrors ComplaintListForm pattern)
        // ════════════════════════════════════════════════════════════════
        private void btnViewDetail_Click(object sender, EventArgs e) => ShowDetailDialog();

        private void ShowDetailDialog()
        {
            if (dgvReturns.SelectedRows.Count == 0) return;
            string id = dgvReturns.SelectedRows[0].Cells["colReturnID"].Value?.ToString();
            var r = _currentReturns.Find(x => x.ReturnID == id);
            if (r == null) return;

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

            // Card 1 — Identity
            var c1Rows = new Panel[]
            {
                FieldRow("Return ID",  ReadLabel(r.ReturnID)),
                FieldRow("Order No.",  ReadLabel(r.OrderID)),
                FieldRow("Status",     ReadLabel(r.ReturnStatus), lastRow: true)
            };
            var (c1Outer, c1Inner) = CardPanel.Create(
                outerHeight: c1Rows.Length * D_RowH + 22,
                outerPadding: new Padding(20, 14, 20, 8));
            c1Inner.Padding = new Padding(0);
            c1Inner.Controls.Add(BuildStack(c1Rows));

            // Card 2 — Details
            var c2Rows = new Panel[]
            {
                FieldRow("Customer",      ReadLabel(r.CustomerName)),
                FieldRow("Return Date",   ReadLabel(r.ReturnDate.ToString("yyyy-MM-dd"))),
                FieldRow("Refund Amount", ReadLabel($"HK$ {r.RefundAmount:N2}")),
                FieldRow("Reason",        ReadLabel(r.Reason ?? "\u2014"), lastRow: true)
            };
            var (c2Outer, c2Inner) = CardPanel.Create(
                outerHeight: c2Rows.Length * D_RowH + 30,
                outerPadding: new Padding(20, 8, 20, 16));
            c2Inner.Padding = new Padding(0);
            c2Inner.Controls.Add(BuildStack(c2Rows));

            using var dlg = new Form
            {
                Text            = $"Return Order Detail  \u2014  {r.ReturnID}",
                Size            = new Size(1800, 1300),
                MinimumSize     = new Size(1100, 1300),
                StartPosition   = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false, MinimizeBox = false,
                BackColor       = Color.FromArgb(240, 244, 249),
                Font            = new Font("Segoe UI", 12f)
            };

            Color pillBg = Color.FromArgb(229, 231, 235);
            Color pillFg = Color.FromArgb(55, 65, 81);
            if (StatusColors.TryGetValue(r.ReturnStatus ?? "", out var hsc))
            { pillBg = hsc.bg; pillFg = hsc.fg; }

            var statusFont = new Font("Segoe UI", 13f, FontStyle.Bold);
            int textW      = TextRenderer.MeasureText(r.ReturnStatus ?? "\u2014", statusFont).Width;
            int statusColW = textW + 80;

            var statusLbl = new Label
            {
                Text = r.ReturnStatus ?? "\u2014",
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
                Text = $"Return Order  \u2014  {r.ReturnID}",
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
            foreach (var row in rows)
            {
                row.Location = new Point(0, y);
                row.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                stack.Controls.Add(row);
                y += D_RowH;
            }
            content.Controls.Add(stack);
            content.Resize += (s, _) =>
            {
                var p = (Panel)s;
                stack.Width = p.Width; stack.Left = 0; stack.Top = 0;
                foreach (Panel row in stack.Controls) row.Width = p.Width;
            };
            return content;
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
    }
}
