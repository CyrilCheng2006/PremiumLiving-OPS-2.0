using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.Shared
{
    /// <summary>
    /// Reusable searchable popup picker.
    ///
    /// Usage:
    ///   var items = source.Select(x => new SearchPickerDialog.PickerItem { Id = x.Id, Display = x.Text }).ToList();
    ///   using var dlg = new SearchPickerDialog("Select Customer", items);
    ///   if (dlg.ShowDialog(this) == DialogResult.OK &amp;&amp; dlg.SelectedItem != null)
    ///       // use dlg.SelectedItem.Id / dlg.SelectedItem.Display
    /// </summary>
    public class SearchPickerDialog : Form
    {
        // ── Public API ──────────────────────────────────────────────────────────
        public class PickerItem
        {
            public string Id      { get; set; }
            public string Display { get; set; }
        }

        public PickerItem SelectedItem { get; private set; }

        // ── Private state ───────────────────────────────────────────────────────
        private readonly List<PickerItem> _allItems;
        private TextBox   _txtSearch;
        private ListBox   _lstItems;
        private Button    _btnOk;
        private Button    _btnCancel;

        // ── Constructor ─────────────────────────────────────────────────────────
        public SearchPickerDialog(string title, List<PickerItem> items)
        {
            _allItems = items ?? new List<PickerItem>();

            // ── Form chrome ────────────────────────────────────────────────────
            this.Text            = title;
            this.Size            = new Size(560, 480);
            this.MinimumSize     = new Size(400, 360);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;
            this.BackColor       = Color.FromArgb(247, 248, 252);
            this.Font            = new Font("Segoe UI", 11f);
            this.Padding         = new Padding(16);

            // ── Title label ────────────────────────────────────────────────────
            var lblTitle = new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Top,
                Height    = 40,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(4, 0, 0, 0)
            };

            // ── Search box ─────────────────────────────────────────────────────
            var pnlSearch = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 6) };
            _txtSearch = new TextBox
            {
                PlaceholderText = "🔍  Search...",
                Dock            = DockStyle.Fill,
                Font            = new Font("Segoe UI", 11f),
                BorderStyle     = BorderStyle.FixedSingle
            };
            _txtSearch.TextChanged += OnSearchChanged;
            pnlSearch.Controls.Add(_txtSearch);

            // ── List box ───────────────────────────────────────────────────────
            _lstItems = new ListBox
            {
                Dock          = DockStyle.Fill,
                Font          = new Font("Segoe UI", 11f),
                BorderStyle   = BorderStyle.FixedSingle,
                IntegralHeight = false,
                ItemHeight    = 28
            };
            _lstItems.DoubleClick += (s, e) => AcceptSelection();
            _lstItems.KeyDown     += (s, e) => { if (e.KeyCode == Keys.Enter) AcceptSelection(); };

            // ── Footer buttons ─────────────────────────────────────────────────
            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.Transparent };

            _btnOk = new Button
            {
                Text      = "Select",
                Width     = 130, Height = 40,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(47, 111, 237),
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                Anchor    = AnchorStyles.Right | AnchorStyles.Bottom
            };
            _btnOk.FlatAppearance.BorderSize         = 0;
            _btnOk.FlatAppearance.MouseOverBackColor = Color.FromArgb(26, 77, 192);
            _btnOk.Click += (s, e) => AcceptSelection();

            _btnCancel = new Button
            {
                Text      = "Cancel",
                Width     = 110, Height = 40,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(98, 112, 135),
                Font      = new Font("Segoe UI", 11f),
                Cursor    = Cursors.Hand,
                Anchor    = AnchorStyles.Right | AnchorStyles.Bottom
            };
            _btnCancel.FlatAppearance.BorderColor    = Color.FromArgb(221, 227, 236);
            _btnCancel.FlatAppearance.BorderSize     = 1;
            _btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            _btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            // Position buttons in footer
            pnlFooter.Resize += (s, e) =>
            {
                int top = (pnlFooter.Height - 40) / 2;
                _btnCancel.Location = new Point(pnlFooter.Width - 110 - 8, top);
                _btnOk.Location     = new Point(pnlFooter.Width - 110 - 8 - 130 - 8, top);
            };
            pnlFooter.Controls.Add(_btnOk);
            pnlFooter.Controls.Add(_btnCancel);

            // ── Divider ────────────────────────────────────────────────────────
            var divider = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(221, 227, 236) };

            // ── Assemble ───────────────────────────────────────────────────────
            this.Controls.Add(_lstItems);
            this.Controls.Add(pnlSearch);
            this.Controls.Add(lblTitle);
            this.Controls.Add(divider);
            this.Controls.Add(pnlFooter);

            // ── Keyboard: Escape closes, Down-Arrow moves focus to list ────────
            this.KeyPreview = true;
            this.KeyDown   += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape) { this.DialogResult = DialogResult.Cancel; this.Close(); }
                if (e.KeyCode == Keys.Down && _txtSearch.Focused && _lstItems.Items.Count > 0)
                {
                    _lstItems.Focus();
                    _lstItems.SelectedIndex = 0;
                    e.Handled = true;
                }
            };

            PopulateList("");

            // Auto-focus search box when dialog opens
            this.Shown += (s, e) => _txtSearch.Focus();
        }

        // ── Filtering ───────────────────────────────────────────────────────────
        private void OnSearchChanged(object sender, EventArgs e)
            => PopulateList(_txtSearch.Text);

        private void PopulateList(string keyword)
        {
            _lstItems.BeginUpdate();
            _lstItems.Items.Clear();

            var filtered = string.IsNullOrWhiteSpace(keyword)
                ? _allItems
                : _allItems.Where(i =>
                    i.Display.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    i.Id.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0
                  ).ToList();

            foreach (var item in filtered)
                _lstItems.Items.Add(item);

            _lstItems.DisplayMember = "Display";
            _lstItems.EndUpdate();

            if (_lstItems.Items.Count > 0)
                _lstItems.SelectedIndex = 0;
        }

        // ── Accept ──────────────────────────────────────────────────────────────
        private void AcceptSelection()
        {
            if (_lstItems.SelectedItem is PickerItem picked)
            {
                SelectedItem       = picked;
                this.DialogResult  = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Please select an item from the list.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
