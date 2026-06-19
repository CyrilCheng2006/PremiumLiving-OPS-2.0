using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.AfterService
{
    /// <summary>
    /// Popup picker for selecting a Staff member (Handed By) in Create Return Order.
    /// </summary>
    public class StaffPickerForm : Form
    {
        // ── public result ─────────────────────────────────────────────────
        public string SelectedStaffID   { get; private set; }
        public string SelectedStaffName { get; private set; }

        // ── injected data source ──────────────────────────────────────────
        private readonly List<(string StaffID, string StaffName, string Department, string StaffRole)> _allStaff;

        // ── controls ─────────────────────────────────────────────────────
        private TextBox      txtSearch;
        private DataGridView dgv;
        private Button       btnSelect;
        private Button       btnCancel;
        private CardPanel    card;

        public StaffPickerForm(
            List<(string StaffID, string StaffName, string Department, string StaffRole)> staffList)
        {
            _allStaff = staffList ?? new List<(string, string, string, string)>();
            InitUI();
            PopulateGrid(_allStaff);
        }

        private void InitUI()
        {
            Text            = "Select Staff (Handed By)";
            Size            = new Size(760, 480);
            MinimumSize     = new Size(640, 400);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = Color.FromArgb(243, 244, 246);
            Font            = new Font("Segoe UI", 9.5f);

            card = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(16) };
            Controls.Add(card);

            var lblTitle = new Label
            {
                Text      = "Select Handed By (Staff)",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30),
                AutoSize  = true,
                Location  = new Point(16, 14)
            };
            card.Controls.Add(lblTitle);

            var lblSearch = new Label
            {
                Text      = "Search:",
                AutoSize  = true,
                Location  = new Point(16, 50),
                ForeColor = Color.FromArgb(80, 80, 80)
            };
            card.Controls.Add(lblSearch);

            txtSearch = new TextBox
            {
                Location        = new Point(70, 47),
                Size            = new Size(300, 26),
                PlaceholderText = "Staff ID, Name, Department..."
            };
            txtSearch.TextChanged += (s, e) => FilterGrid(txtSearch.Text.Trim());
            card.Controls.Add(txtSearch);

            dgv = new DataGridView
            {
                Location            = new Point(16, 84),
                Size                = new Size(710, 310),
                ReadOnly            = true,
                AllowUserToAddRows  = false,
                SelectionMode       = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect         = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                BackgroundColor     = Color.White,
                BorderStyle         = BorderStyle.None,
                RowHeadersVisible   = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgv.DoubleClick += (s, e) => ConfirmSelection();
            card.Controls.Add(dgv);

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "StaffID",    HeaderText = "Staff ID",   FillWeight = 22 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "StaffName",  HeaderText = "Name",       FillWeight = 30 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Department", HeaderText = "Department", FillWeight = 28 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "StaffRole",  HeaderText = "Role",       FillWeight = 20 });

            btnSelect = new Button
            {
                Text      = "Select",
                Size      = new Size(90, 34),
                Location  = new Point(552, 404),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnSelect.FlatAppearance.BorderSize = 0;
            btnSelect.Click += (s, e) => ConfirmSelection();
            card.Controls.Add(btnSelect);

            btnCancel = new Button
            {
                Text      = "Cancel",
                Size      = new Size(90, 34),
                Location  = new Point(648, 404),
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9.5f)
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            card.Controls.Add(btnCancel);
        }

        private void PopulateGrid(
            IEnumerable<(string StaffID, string StaffName, string Department, string StaffRole)> source)
        {
            dgv.Rows.Clear();
            foreach (var st in source)
                dgv.Rows.Add(st.StaffID, st.StaffName, st.Department, st.StaffRole);
        }

        private void FilterGrid(string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) { PopulateGrid(_allStaff); return; }
            var filtered = _allStaff.Where(st =>
                st.StaffID.IndexOf(keyword,    StringComparison.OrdinalIgnoreCase) >= 0 ||
                st.StaffName.IndexOf(keyword,  StringComparison.OrdinalIgnoreCase) >= 0 ||
                st.Department.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                st.StaffRole.IndexOf(keyword,  StringComparison.OrdinalIgnoreCase) >= 0);
            PopulateGrid(filtered);
        }

        private void ConfirmSelection()
        {
            if (dgv.CurrentRow == null) return;
            SelectedStaffID   = dgv.CurrentRow.Cells["StaffID"].Value?.ToString();
            SelectedStaffName = dgv.CurrentRow.Cells["StaffName"].Value?.ToString();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
