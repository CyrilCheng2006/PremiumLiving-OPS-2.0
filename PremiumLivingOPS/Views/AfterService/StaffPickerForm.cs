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
    /// Uses CardPanel.Create() / CardPanel.CreateFill() — CardPanel is a static class.
    /// </summary>
    public class StaffPickerForm : Form
    {
        public string SelectedStaffID   { get; private set; }
        public string SelectedStaffName { get; private set; }

        private readonly List<(string StaffID, string StaffName, string Department, string StaffRole)> _allStaff;

        private TextBox      txtSearch;
        private DataGridView dgv;
        private Button       btnSelect;
        private Button       btnCancel;

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
            BackColor       = Color.FromArgb(240, 244, 249);
            Font            = new Font("Segoe UI", 9.5f);

            var layout = new Panel { Dock = DockStyle.Fill };
            Controls.Add(layout);

            // ── search bar card ──────────────────────────────────────────────
            var (searchOuter, searchInner) = CardPanel.Create(outerHeight: 70,
                outerPadding: new Padding(12, 8, 12, 4));
            searchOuter.Dock = DockStyle.Top;
            layout.Controls.Add(searchOuter);

            var lblTitle = new Label
            {
                Text      = "Select Handed By (Staff)",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30),
                AutoSize  = true,
                Location  = new Point(10, 8)
            };
            searchInner.Controls.Add(lblTitle);

            var lblSearch = new Label
            {
                Text      = "Search:",
                AutoSize  = true,
                Location  = new Point(10, 38),
                ForeColor = Color.FromArgb(80, 80, 80)
            };
            searchInner.Controls.Add(lblSearch);

            txtSearch = new TextBox
            {
                Location        = new Point(66, 35),
                Size            = new Size(300, 26),
                PlaceholderText = "Staff ID, Name, Department..."
            };
            txtSearch.TextChanged += (s, e) => FilterGrid(txtSearch.Text.Trim());
            searchInner.Controls.Add(txtSearch);

            // ── button panel ─────────────────────────────────────────────────
            var (btnOuter, btnInner) = CardPanel.Create(outerHeight: 56,
                outerPadding: new Padding(12, 6, 12, 6));
            btnOuter.Dock = DockStyle.Bottom;
            layout.Controls.Add(btnOuter);

            btnSelect = new Button
            {
                Text      = "Select",
                Size      = new Size(90, 34),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnSelect.FlatAppearance.BorderSize = 0;
            btnSelect.Click += (s, e) => ConfirmSelection();

            btnCancel = new Button
            {
                Text      = "Cancel",
                Size      = new Size(90, 34),
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9.5f)
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var btnFlow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents  = false
            };
            btnFlow.Controls.Add(btnCancel);
            btnFlow.Controls.Add(btnSelect);
            btnInner.Controls.Add(btnFlow);

            // ── grid card ────────────────────────────────────────────────────
            var (gridOuter, gridInner) = CardPanel.CreateFill(
                outerPadding: new Padding(12, 4, 12, 4));
            layout.Controls.Add(gridOuter);

            dgv = new DataGridView
            {
                Dock                = DockStyle.Fill,
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
            gridInner.Controls.Add(dgv);

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "StaffID",    HeaderText = "Staff ID",   FillWeight = 22 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "StaffName",  HeaderText = "Name",       FillWeight = 32 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Department", HeaderText = "Department", FillWeight = 28 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "StaffRole",  HeaderText = "Role",       FillWeight = 18 });
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
