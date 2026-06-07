using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.Shared
{
    /// <summary>
    /// Generic searchable popup picker dialog.
    ///
    /// Usage:
    ///   var rows = items.Select(x => new PickerRow(x.ID, x.DisplayText, ...extra cols...));
    ///   using var dlg = new PickerDialog("Select Customer", new[]{"ID","Name"}, rows);
    ///   if (dlg.ShowDialog(owner) == DialogResult.OK)
    ///       Console.WriteLine(dlg.SelectedId + " / " + dlg.SelectedText);
    ///
    /// MVC contract: pure View helper — no Controller or DB access.
    /// </summary>
    public sealed class PickerDialog : Form
    {
        // ── Public result ──────────────────────────────────────────────────────
        /// <summary>The ID (Value) of the row the user confirmed.</summary>
        public string SelectedId   { get; private set; }

        /// <summary>The primary display text of the confirmed row.</summary>
        public string SelectedText { get; private set; }

        // ── Private state ──────────────────────────────────────────────────────
        private readonly IReadOnlyList<PickerRow>    _allRows;
        private readonly IReadOnlyList<string>       _headers;
        private readonly TextBox                     _txtSearch;
        private readonly DataGridView                _dgv;
        private readonly Button                      _btnOk;
        private readonly Button                      _btnCancel;

        // ── Colours (match global Palette where possible) ──────────────────────
        private static readonly Color ColBg        = Color.FromArgb(246, 249, 255);
        private static readonly Color ColSurface   = Color.White;
        private static readonly Color ColBorder    = Color.FromArgb(221, 227, 236);
        private static readonly Color ColText      = Color.FromArgb(15,  31,  53);
        private static readonly Color ColMuted     = Color.FromArgb(98,  112, 135);
        private static readonly Color ColPrimary   = Color.FromArgb(47,  111, 237);
        private static readonly Color ColSelBg     = Color.FromArgb(219, 234, 254);

        // ── Constructor ────────────────────────────────────────────────────────
        /// <param name="title">Dialog title shown in the header bar.</param>
        /// <param name="columnHeaders">
        ///   Column header labels.  The first column always maps to PickerRow.Id,
        ///   the second to PickerRow.PrimaryText; additional columns map to
        ///   PickerRow.ExtraColumns in order.
        /// </param>
        /// <param name="rows">All selectable rows.</param>
        public PickerDialog(
            string                  title,
            IReadOnlyList<string>   columnHeaders,
            IEnumerable<PickerRow>  rows)
        {
            _headers = columnHeaders;
            _allRows = new List<PickerRow>(rows);

            // ── Form shell ──────────────────────────────────────────────────
            Text            = title;
            Size            = new Size(860, 580);
            MinimumSize     = new Size(600, 420);
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = ColSurface;
            Font            = new Font("Segoe UI", 12f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;

            // ── Header bar ──────────────────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 60,
                BackColor = Color.FromArgb(19, 35, 61),
                Padding   = new Padding(20, 0, 20, 0)
            };
            pnlHeader.Controls.Add(new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false
            });

            // ── Search bar ──────────────────────────────────────────────────
            var pnlSearch = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 56,
                BackColor = ColBg,
                Padding   = new Padding(16, 10, 16, 8)
            };
            pnlSearch.Paint += (s, e) =>
            {
                using var pen = new Pen(ColBorder, 1);
                e.Graphics.DrawLine(pen, 0, ((Panel)s).Height - 1,
                                         ((Panel)s).Width, ((Panel)s).Height - 1);
            };

            _txtSearch = new TextBox
            {
                Dock        = DockStyle.Fill,
                Font        = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Type to search..."
            };
            _txtSearch.TextChanged += (s, e) => ApplyFilter();
            _txtSearch.KeyDown     += TxtSearch_KeyDown;
            pnlSearch.Controls.Add(_txtSearch);

            // ── DataGridView ─────────────────────────────────────────────────
            _dgv = new DataGridView
            {
                ReadOnly              = true,
                AllowUserToAddRows    = false,
                RowHeadersVisible     = false,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect           = false,
                BackgroundColor       = ColSurface,
                BorderStyle           = BorderStyle.None,
                GridColor             = ColBorder,
                Font                  = new Font("Segoe UI", 12f),
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate           = { Height = 42 },
                Dock                  = DockStyle.Fill,
                ColumnHeadersHeight   = 40,
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = ColBg,
                    ForeColor = ColMuted,
                    Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                    Padding   = new Padding(10, 0, 0, 0),
                    Alignment = DataGridViewContentAlignment.MiddleLeft
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor          = ColSurface,
                    ForeColor          = ColText,
                    SelectionBackColor = ColSelBg,
                    SelectionForeColor = ColText,
                    Padding            = new Padding(10, 4, 10, 4)
                }
            };

            // Build columns from headers
            for (int i = 0; i < _headers.Count; i++)
            {
                _dgv.Columns.Add(new DataGridViewTextBoxColumn
                {
                    HeaderText = _headers[i].ToUpperInvariant(),
                    Name       = $"col{i}",
                    // Give first col less weight; extra cols share the rest
                    FillWeight = i == 0 ? 20f : 80f / Math.Max(1, _headers.Count - 1)
                });
            }

            _dgv.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) ConfirmSelection(); };
            _dgv.KeyDown         += Dgv_KeyDown;

            // ── Footer ──────────────────────────────────────────────────────
            var pnlFooter = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 64,
                BackColor = ColSurface,
                Padding   = new Padding(0, 10, 20, 10)
            };
            pnlFooter.Paint += (s, e) =>
            {
                using var pen = new Pen(ColBorder, 1);
                e.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0);
            };

            _btnOk = new Button
            {
                Text      = "Select",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = ColPrimary,
                FlatStyle = FlatStyle.Flat,
                Dock      = DockStyle.Right,
                Width     = 130,
                Cursor    = Cursors.Hand
            };
            _btnOk.FlatAppearance.BorderSize           = 0;
            _btnOk.FlatAppearance.MouseOverBackColor   = Color.FromArgb(26,  77, 192);
            _btnOk.FlatAppearance.MouseDownBackColor   = Color.FromArgb(21,  60, 155);
            _btnOk.Click += (s, e) => ConfirmSelection();

            _btnCancel = new Button
            {
                Text      = "Cancel",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = ColText,
                BackColor = ColSurface,
                FlatStyle = FlatStyle.Flat,
                Dock      = DockStyle.Right,
                Width     = 110,
                Cursor    = Cursors.Hand
            };
            _btnCancel.FlatAppearance.BorderColor         = ColBorder;
            _btnCancel.FlatAppearance.BorderSize          = 1;
            _btnCancel.FlatAppearance.MouseOverBackColor  = ColBg;
            _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            pnlFooter.Controls.Add(_btnOk);
            pnlFooter.Controls.Add(_btnCancel);

            // ── Assemble ────────────────────────────────────────────────────
            Controls.Add(_dgv);        // Fill
            Controls.Add(pnlSearch);   // Top
            Controls.Add(pnlHeader);   // Top
            Controls.Add(pnlFooter);   // Bottom

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;

            // Populate grid on first open
            PopulateGrid(_allRows);

            Shown += (s, e) => _txtSearch.Focus();
        }

        // ── Filter ─────────────────────────────────────────────────────────────
        private void ApplyFilter()
        {
            string kw = _txtSearch.Text.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(kw))
            {
                PopulateGrid(_allRows);
                return;
            }

            var filtered = new List<PickerRow>();
            foreach (var r in _allRows)
            {
                bool match = r.Id.ToLowerInvariant().Contains(kw)
                          || r.PrimaryText.ToLowerInvariant().Contains(kw);
                if (!match)
                    foreach (var ex in r.ExtraColumns)
                        if ((ex ?? "").ToLowerInvariant().Contains(kw)) { match = true; break; }
                if (match) filtered.Add(r);
            }
            PopulateGrid(filtered);
        }

        private void PopulateGrid(IReadOnlyList<PickerRow> rows)
        {
            _dgv.Rows.Clear();
            foreach (var r in rows)
            {
                var cells = new List<object> { r.Id, r.PrimaryText };
                cells.AddRange(r.ExtraColumns);
                // Pad or trim to match column count
                while (cells.Count < _headers.Count) cells.Add("");
                if (cells.Count > _headers.Count) cells.RemoveRange(_headers.Count, cells.Count - _headers.Count);
                _dgv.Rows.Add(cells.ToArray());
            }
            if (_dgv.Rows.Count > 0)
                _dgv.Rows[0].Selected = true;
        }

        // ── Confirm ────────────────────────────────────────────────────────────
        private void ConfirmSelection()
        {
            if (_dgv.SelectedRows.Count == 0) return;
            var row = _dgv.SelectedRows[0];
            SelectedId   = row.Cells["col0"].Value?.ToString() ?? "";
            SelectedText = row.Cells["col1"].Value?.ToString() ?? "";
            DialogResult = DialogResult.OK;
            Close();
        }

        // ── Keyboard nav ───────────────────────────────────────────────────────
        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down && _dgv.Rows.Count > 0)
            {
                _dgv.Focus();
                _dgv.Rows[0].Selected = true;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter && _dgv.Rows.Count > 0)
            {
                ConfirmSelection();
                e.Handled = true;
            }
        }

        private void Dgv_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ConfirmSelection();
                e.Handled = true;
            }
        }
    }

    // ── Row data model ─────────────────────────────────────────────────────────
    /// <summary>One selectable row in a PickerDialog.</summary>
    public sealed class PickerRow
    {
        /// <summary>The unique identifier returned as SelectedId.</summary>
        public string       Id           { get; }

        /// <summary>The main display text returned as SelectedText.</summary>
        public string       PrimaryText  { get; }

        /// <summary>Optional additional columns (3rd header onwards).</summary>
        public string[]     ExtraColumns { get; }

        public PickerRow(string id, string primaryText, params string[] extraColumns)
        {
            Id           = id ?? "";
            PrimaryText  = primaryText ?? "";
            ExtraColumns = extraColumns ?? Array.Empty<string>();
        }
    }
}
