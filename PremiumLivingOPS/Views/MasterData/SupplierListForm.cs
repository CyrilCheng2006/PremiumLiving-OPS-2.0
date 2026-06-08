using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.MasterData
{
    /// <summary>
    /// View — Supplier List page (Master Data Maintenance module).
    ///
    /// MVC role: pure View. Delegates all data access to MasterDataController.
    /// UI structure follows CardPanel three-layer nested card standard:
    ///   • Card 1 — Search bar
    ///   • Card 2 — Summary KPI strip (total supplier count) + Add New / Modify buttons
    ///   • Card 3 — DataGridView listing all suppliers
    /// </summary>
    public partial class SupplierListForm : Form
    {
        private readonly MasterDataController _ctrl = new MasterDataController();
        private List<SupplierEntity> _currentSuppliers = new List<SupplierEntity>();

        // pnlKpi is declared here; its reference is shared with Designer.cs
        private Panel pnlKpi;

        public SupplierListForm()
        {
            InitializeComponent();
            this.Load += SupplierListForm_Load;
        }

        // ── Load ───────────────────────────────────────────────────────────────
        private void SupplierListForm_Load(object sender, EventArgs e)
        {
            RefreshGrid();
        }

        // ── Data refresh ────────────────────────────────────────────────────
        private void RefreshGrid()
        {
            string keyword = txtSearch.Text.Trim();
            var vm = _ctrl.GetSupplierListVM(string.IsNullOrEmpty(keyword) ? null : keyword);

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Master Data Maintenance  ›  Supplier List");

            _currentSuppliers = vm.Suppliers;

            dgvSuppliers.Rows.Clear();
            foreach (var s in _currentSuppliers)
                dgvSuppliers.Rows.Add(
                    s.SupplierID,
                    s.SupplierName,
                    s.PhoneNumber,
                    s.SupplierAddress);

            RefreshKpi();
        }

        private void ResetFilters()
        {
            txtSearch.Text = string.Empty;
            RefreshGrid();
        }

        // ── KPI strip ─────────────────────────────────────────────────────
        private void RefreshKpi()
        {
            pnlKpi.Controls.Clear();

            var allVm = _ctrl.GetSupplierListVM();
            int total = allVm.Suppliers.Count;
            int shown = _currentSuppliers.Count;

            // ── outer flow: pills left ─────────────────────────────────
            var outerFlow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent,
                Padding       = new Padding(0),
                AutoScroll    = false
            };

            // ── KPI pills ─────────────────────────────────────────────
            var pills = new[]
            {
                ("Total Suppliers", total.ToString(),
                 Color.FromArgb(47, 111, 237), Color.FromArgb(219, 234, 254)),
                ("Showing",         shown.ToString(),
                 Color.FromArgb(6,  95,  70),  Color.FromArgb(209, 250, 229)),
            };

            foreach (var (label, count, fg, bg) in pills)
            {
                var pill = new Panel
                {
                    BackColor = bg,
                    Size      = new Size(220, 50),
                    Margin    = new Padding(0, 0, 10, 0)
                };

                var tlp = new TableLayoutPanel
                {
                    Dock            = DockStyle.Fill,
                    ColumnCount     = 2,
                    RowCount        = 1,
                    BackColor       = Color.Transparent,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding         = new Padding(10, 0, 8, 0)
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60f));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                tlp.Controls.Add(new Label
                {
                    Text      = count,
                    Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                    ForeColor = fg,
                    BackColor = Color.Transparent,
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize  = false
                }, 0, 0);
                tlp.Controls.Add(new Label
                {
                    Text      = label,
                    Font      = new Font("Segoe UI", 11f),
                    ForeColor = fg,
                    BackColor = Color.Transparent,
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoSize  = false
                }, 1, 0);

                pill.Controls.Add(tlp);
                outerFlow.Controls.Add(pill);
            }

            outerFlow.Controls.Add(new Panel { BackColor = Color.Transparent, Size = new Size(10, 50), Margin = new Padding(0) });
            pnlKpi.Controls.Add(outerFlow);

            // ── action buttons (right-docked, 290×60) ─────────────────
            var pnlBtns = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent,
                Dock          = DockStyle.Right,
                AutoSize      = true,
                Padding       = new Padding(0, 5, 8, 5)
            };

            // Modify button (orange, requires selection)
            var btnModify = MakeKpiButton("Modify",
                Color.FromArgb(234, 88, 12),
                Color.FromArgb(255, 247, 237));
            btnModify.Enabled = false;
            btnModify.Click  += (s, e) =>
            {
                int idx = dgvSuppliers.CurrentRow?.Index ?? -1;
                if (idx < 0 || idx >= _currentSuppliers.Count) return;
                ShowModifyDialog(idx);
            };

            // Add New button (brand navy)
            var btnAdd = MakeKpiButton("+ Add New",
                Color.FromArgb(19, 35, 61),
                Color.FromArgb(219, 234, 254));
            btnAdd.Click += (s, e) => ShowAddDialog();

            pnlBtns.Controls.Add(btnModify);
            pnlBtns.Controls.Add(btnAdd);
            pnlKpi.Controls.Add(pnlBtns);

            // Enable Modify when selection changes
            dgvSuppliers.SelectionChanged += (s, e) =>
                btnModify.Enabled = dgvSuppliers.CurrentRow != null;
        }

        // ── KPI button factory  (290 × 60) ───────────────────────────────────
        private static Button MakeKpiButton(string text, Color fg, Color bg)
        {
            var btn = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = fg,
                BackColor = bg,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(290, 60),
                Margin    = new Padding(0, 0, 8, 0),
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = fg;
            btn.FlatAppearance.BorderSize  = 1;
            return btn;
        }

        // ── Add New dialog  (1000 × 700) ──────────────────────────────────────
        private void ShowAddDialog()
        {
            string nextId = _ctrl.GetNextSupplierID();

            using var dlg = new Form
            {
                Text            = "Add New Supplier",
                Size            = new Size(1000, 700),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 11f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false
            };

            var pnlHdr = BuildDialogHeader("Add New Supplier");
            var (pnlBody, tbl) = BuildDialogBody(4);

            var txtId = new TextBox
            {
                Text      = nextId,
                ReadOnly  = true,
                BackColor = Color.FromArgb(240, 240, 240),
                Font      = new Font("Segoe UI", 11f),
                Dock      = DockStyle.Fill
            };

            var txtName    = MakeTextInput();
            var txtPhone   = MakeTextInput();
            var txtAddress = MakeTextInput();

            var rows = new (string Label, Control Ctrl)[]
            {
                ("Supplier ID",   txtId),
                ("Supplier Name", txtName),
                ("Phone Number",  txtPhone),
                ("Address",       txtAddress),
            };

            for (int i = 0; i < rows.Length; i++)
            {
                tbl.Controls.Add(MakeLblKey(rows[i].Label), 0, i);
                tbl.Controls.Add(rows[i].Ctrl,              1, i);
                tbl.SetRow(rows[i].Ctrl, i);
            }

            var pnlFtr = BuildDialogFooter(dlg, () =>
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                { MessageBox.Show("Supplier Name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
                if (string.IsNullOrWhiteSpace(txtPhone.Text))
                { MessageBox.Show("Phone Number is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
                if (string.IsNullOrWhiteSpace(txtAddress.Text))
                { MessageBox.Show("Address is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }

                bool ok = _ctrl.AddSupplier(new SupplierEntity
                {
                    SupplierID      = nextId,
                    SupplierName    = txtName.Text.Trim(),
                    PhoneNumber     = txtPhone.Text.Trim(),
                    SupplierAddress = txtAddress.Text.Trim()
                });
                if (!ok) { MessageBox.Show("Failed to add supplier. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return false; }
                return true;
            });

            dlg.Controls.Add(pnlBody);
            dlg.Controls.Add(pnlHdr);
            dlg.Controls.Add(pnlFtr);
            if (dlg.ShowDialog(this) == DialogResult.OK)
                RefreshGrid();
        }

        // ── Modify dialog  (1000 × 700) ───────────────────────────────────────
        private void ShowModifyDialog(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _currentSuppliers.Count) return;
            var s = _currentSuppliers[rowIndex];

            using var dlg = new Form
            {
                Text            = $"Modify Supplier — {s.SupplierID}",
                Size            = new Size(1000, 700),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 11f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false
            };

            var pnlHdr = BuildDialogHeader($"Modify Supplier  —  {s.SupplierID}");
            var (pnlBody, tbl) = BuildDialogBody(4);

            var txtId = new TextBox
            {
                Text      = s.SupplierID,
                ReadOnly  = true,
                BackColor = Color.FromArgb(240, 240, 240),
                Font      = new Font("Segoe UI", 11f),
                Dock      = DockStyle.Fill
            };

            var txtName    = MakeTextInput(s.SupplierName);
            var txtPhone   = MakeTextInput(s.PhoneNumber);
            var txtAddress = MakeTextInput(s.SupplierAddress);

            var rows = new (string Label, Control Ctrl)[]
            {
                ("Supplier ID",   txtId),
                ("Supplier Name", txtName),
                ("Phone Number",  txtPhone),
                ("Address",       txtAddress),
            };

            for (int i = 0; i < rows.Length; i++)
            {
                tbl.Controls.Add(MakeLblKey(rows[i].Label), 0, i);
                tbl.Controls.Add(rows[i].Ctrl,              1, i);
                tbl.SetRow(rows[i].Ctrl, i);
            }

            var pnlFtr = BuildDialogFooter(dlg, () =>
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                { MessageBox.Show("Supplier Name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
                if (string.IsNullOrWhiteSpace(txtPhone.Text))
                { MessageBox.Show("Phone Number is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
                if (string.IsNullOrWhiteSpace(txtAddress.Text))
                { MessageBox.Show("Address is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }

                bool ok = _ctrl.UpdateSupplier(s.SupplierID,
                    txtName.Text.Trim(),
                    txtPhone.Text.Trim(),
                    txtAddress.Text.Trim());
                if (!ok) { MessageBox.Show("Failed to update supplier. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return false; }
                return true;
            });

            dlg.Controls.Add(pnlBody);
            dlg.Controls.Add(pnlHdr);
            dlg.Controls.Add(pnlFtr);
            if (dlg.ShowDialog(this) == DialogResult.OK)
                RefreshGrid();
        }

        // ── Grid events ───────────────────────────────────────────────────────
        private void dgvSuppliers_SelectionChanged(object sender, EventArgs e) { /* handled in RefreshKpi */ }

        private void dgvSuppliers_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            ShowDetailDialog(e.RowIndex);
        }

        // ── Detail dialog ─────────────────────────────────────────────────────
        private void ShowDetailDialog(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _currentSuppliers.Count) return;
            var s = _currentSuppliers[rowIndex];

            using var dlg = new Form
            {
                Text            = $"Supplier — {s.SupplierID}",
                Size            = new Size(640, 360),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 12f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false
            };

            var pnlHdr = BuildDialogHeader($"Supplier Details  —  {s.SupplierID}");

            var pnlBody = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 16, 24, 8), BackColor = Color.White };
            var tbl = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 2,
                RowCount        = 4,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            for (int r = 0; r < 4; r++)
                tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));

            var fields = new[] {
                ("Supplier ID",   s.SupplierID),
                ("Supplier Name", s.SupplierName),
                ("Phone",         s.PhoneNumber),
                ("Address",       s.SupplierAddress)
            };
            for (int i = 0; i < fields.Length; i++)
            {
                tbl.Controls.Add(MakeLblKey(fields[i].Item1), 0, i);
                tbl.Controls.Add(MakeLblVal(fields[i].Item2), 1, i);
            }
            pnlBody.Controls.Add(tbl);

            var pnlFtr = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.White, Padding = new Padding(0, 8, 20, 8) };
            pnlFtr.Paint += (snd, ev) =>
            {
                using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
                ev.Graphics.DrawLine(pen, 0, 0, ((Panel)snd).Width, 0);
            };
            var btnClose = new Button
            {
                Text      = "Close",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock      = DockStyle.Right,
                Width     = 130,
                Cursor    = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnClose.Click += (snd, ev) => dlg.Close();
            pnlFtr.Controls.Add(btnClose);

            dlg.Controls.Add(pnlBody);
            dlg.Controls.Add(pnlHdr);
            dlg.Controls.Add(pnlFtr);
            dlg.ShowDialog(this);
        }

        // ── Dialog builders ───────────────────────────────────────────────────
        private static Panel BuildDialogHeader(string title)
        {
            var pnl = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(19, 35, 61) };
            pnl.Controls.Add(new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(20, 0, 0, 0)
            });
            return pnl;
        }

        private static (Panel body, TableLayoutPanel tbl) BuildDialogBody(int rowCount)
        {
            var pnlBody = new Panel
            {
                Dock      = DockStyle.Fill,
                Padding   = new Padding(24, 12, 24, 8),
                BackColor = Color.White
            };
            var tbl = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 2,
                RowCount        = rowCount,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            for (int r = 0; r < rowCount; r++)
                tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 64f));
            pnlBody.Controls.Add(tbl);
            return (pnlBody, tbl);
        }

        private static Panel BuildDialogFooter(Form dlg, Func<bool> onSave)
        {
            var pnl = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 60,
                BackColor = Color.White,
                Padding   = new Padding(0, 8, 20, 8)
            };
            pnl.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0);
            };

            var btnCancel = new Button
            {
                Text      = "Cancel",
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock      = DockStyle.Right,
                Width     = 110,
                Cursor    = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnCancel.Click += (s, e) => dlg.Close();

            var btnSave = new Button
            {
                Text      = "Save",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(19, 35, 61),
                FlatStyle = FlatStyle.Flat,
                Dock      = DockStyle.Right,
                Width     = 110,
                Cursor    = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize         = 0;
            btnSave.FlatAppearance.MouseOverBackColor = Color.FromArgb(37, 60, 96);
            btnSave.Click += (s, e) =>
            {
                if (onSave()) dlg.DialogResult = DialogResult.OK;
            };

            pnl.Controls.Add(btnCancel);
            pnl.Controls.Add(btnSave);
            return pnl;
        }

        // ── Label / input helpers ─────────────────────────────────────────────
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
            Text         = text ?? "—",
            Font         = new Font("Segoe UI", 12f),
            ForeColor    = Color.FromArgb(15, 31, 53),
            Dock         = DockStyle.Fill,
            TextAlign    = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        private static TextBox MakeTextInput(string initial = "")
        {
            var tb = new TextBox
            {
                Text        = initial,
                Font        = new Font("Segoe UI", 11f),
                Dock        = DockStyle.Fill,
                BackColor   = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            tb.Margin = new Padding(0, 6, 0, 6);
            return tb;
        }

        // ── Navigation & Logout ───────────────────────────────────────────────
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }
    }
}
