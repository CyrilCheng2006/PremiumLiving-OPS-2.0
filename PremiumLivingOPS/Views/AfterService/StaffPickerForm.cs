using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.AfterService
{
    /// <summary>
    /// Popup staff picker — rendered to match ComplaintListForm.ShowStaffPicker baseline.
    /// Structure: dark header (19,35,61) → white search bar (bottom-bordered)
    ///            → DataGridView [StaffID | StaffName] (fill) → white footer with Select / Cancel.
    /// </summary>
    public class StaffPickerForm : Form
    {
        public string SelectedStaffID   { get; private set; }
        public string SelectedStaffName { get; private set; }

        private readonly List<(string StaffID, string StaffName, string Department, string StaffRole)> _allStaff;
        private DataGridView _grid;
        private TextBox      _txtSearch;

        public StaffPickerForm(List<(string StaffID, string StaffName, string Department, string StaffRole)> staffList)
        {
            _allStaff = staffList ?? new List<(string, string, string, string)>();
            // Sort by StaffID ascending (mirrors ComplaintListForm)
            _allStaff.Sort((a, b) => string.Compare(a.StaffID, b.StaffID, StringComparison.Ordinal));
            InitUI();
            Populate(string.Empty);
        }

        private void InitUI()
        {
            Text            = "Select Staff Member";
            Size            = new Size(700, 560);
            MinimumSize     = new Size(500, 400);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = Color.FromArgb(240, 244, 249);
            Font            = new Font("Segoe UI", 12f);

            // ── Header ───────────────────────────────────────────────────
            var hdr = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(19, 35, 61) };
            hdr.Controls.Add(new Label
            {
                Text      = "\uD83D\uDD0D  Select Handled By (Staff)",
                Font      = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(20, 0, 0, 0)
            });

            // ── Search bar ───────────────────────────────────────────────
            var pnlSearch = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 56,
                BackColor = Color.White,
                Padding   = new Padding(16, 10, 16, 10)
            };
            PaintBottomBorder(pnlSearch);
            _txtSearch = new TextBox
            {
                Dock            = DockStyle.Fill,
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                PlaceholderText = "Type staff name or ID..."
            };
            _txtSearch.TextChanged += (_, __) => Populate(_txtSearch.Text.Trim());
            pnlSearch.Controls.Add(_txtSearch);

            // ── DataGridView (fill) — mirrors ComplaintListForm grid ─────
            _grid = new DataGridView
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
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colID",   HeaderText = "Staff ID",   FillWeight = 40 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Staff Name", FillWeight = 60 });

            // Header style (mirrors ComplaintListForm exactly)
            _grid.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 11f, FontStyle.Bold);
            _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 255);
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(47, 111, 237);
            _grid.EnableHeadersVisualStyles = false;

            _grid.CellDoubleClick += (_, __) => Confirm();

            // ── Footer ───────────────────────────────────────────────────
            var foot = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 72,
                BackColor = Color.White,
                Padding   = new Padding(0, 12, 20, 12)
            };
            foot.Paint += (s, pe) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                pe.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0);
            };

            var btnSelect = new Button
            {
                Text      = "\u2714  Select",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                BackColor = Color.FromArgb(47, 111, 237),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Width     = 160,
                Height    = 48,
                Cursor    = Cursors.Hand,
                Margin    = new Padding(0, 0, 10, 0)
            };
            btnSelect.FlatAppearance.BorderSize            = 0;
            btnSelect.FlatAppearance.MouseOverBackColor    = Color.FromArgb(29, 78, 216);

            var btnCancel = new Button
            {
                Text      = "Cancel",
                Font      = new Font("Segoe UI", 12f),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 31, 53),
                FlatStyle = FlatStyle.Flat,
                Width     = 120,
                Height    = 48,
                Cursor    = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor       = Color.FromArgb(221, 227, 236);
            btnCancel.FlatAppearance.BorderSize        = 1;
            btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);

            btnSelect.Click += (_, __) =>
            {
                if (_grid.SelectedRows.Count > 0) Confirm();
                else MessageBox.Show("Please select a staff member.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };
            btnCancel.Click += (_, __) => Close();

            var footFlow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Right,
                AutoSize      = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor     = Color.Transparent
            };
            footFlow.Controls.Add(btnSelect);
            footFlow.Controls.Add(btnCancel);
            foot.Controls.Add(footFlow);

            // ── Body ─────────────────────────────────────────────────────
            var body = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            body.Controls.Add(_grid);

            Controls.Add(body);
            Controls.Add(foot);
            Controls.Add(pnlSearch);
            Controls.Add(hdr);
        }

        private void Populate(string kw)
        {
            _grid.Rows.Clear();
            foreach (var s in _allStaff)
            {
                bool match = string.IsNullOrEmpty(kw)
                    || s.StaffID.IndexOf(kw,   StringComparison.OrdinalIgnoreCase) >= 0
                    || s.StaffName.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0;
                if (match) _grid.Rows.Add(s.StaffID, s.StaffName);
            }
        }

        private void Confirm()
        {
            if (_grid.SelectedRows.Count == 0) return;
            var row = _grid.SelectedRows[0];
            SelectedStaffID   = row.Cells["colID"].Value?.ToString();
            SelectedStaffName = row.Cells["colName"].Value?.ToString();
            DialogResult = DialogResult.OK;
        }

        private static void PaintBottomBorder(Panel p)
        {
            p.Paint += (s, e) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawLine(pen, 0, ((Panel)s).Height - 1, ((Panel)s).Width, ((Panel)s).Height - 1);
            };
        }
    }
}
