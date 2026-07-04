using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.MasterData
{
    /// <summary>
    /// View — Supplier List page (Master Data Maintenance module).
    /// ShowModifyDialog and ShowDetailDialog are rewritten to match
    /// CustomerListForm Modify baseline:
    ///   • Full-width Header (no badge), 80px #13233D, 18pt Bold white Label, left pad 32px
    ///   • DlgFieldRow / BuildCardOuter / BuildFooter / AttachFooterBtns shared builders
    ///   • ValidateDlgFields() params validation pattern
    ///   • MakeDlgBtn / MakeDlgOutlineBtn button factories
    /// MVC role: pure View. All data access delegated to MasterDataController.
    /// </summary>
    public partial class SupplierListForm : Form
    {
        private readonly MasterDataController _ctrl = new MasterDataController();
        private List<SupplierEntity> _currentSuppliers = new List<SupplierEntity>();

        private Button _btnAddNew;
        private Button _btnModify;

        private const int DLG_LabelW = 340;
        private const int DLG_RowH   = 80;
        private const int DLG_BtnW   = 210;
        private const int DLG_BtnH   = 60;

        public SupplierListForm()
        {
            InitializeComponent();
            this.Load += SupplierListForm_Load;
        }

        private void SupplierListForm_Load(object sender, EventArgs e) => RefreshGrid();

        // ── Data refresh
        private void RefreshGrid()
        {
            string idKw    = txtSearchID.Text.Trim();
            string nameKw  = txtSearchName.Text.Trim();
            string phoneKw = txtSearchPhone.Text.Trim();
            string addrKw  = txtSearchAddress.Text.Trim();

            string keyword = !string.IsNullOrEmpty(idKw)    ? idKw
                           : !string.IsNullOrEmpty(nameKw)  ? nameKw
                           : !string.IsNullOrEmpty(phoneKw) ? phoneKw
                           : !string.IsNullOrEmpty(addrKw)  ? addrKw
                           : null;

            var vm = _ctrl.GetSupplierListVM(keyword);
            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Master Data Maintenance  \u203a  Supplier List");

            _currentSuppliers = vm.Suppliers;
            dgvSuppliers.Rows.Clear();
            foreach (var s in _currentSuppliers)
                dgvSuppliers.Rows.Add(s.SupplierID, s.SupplierName, s.PhoneNumber, s.SupplierAddress);

            RefreshKpi();
        }

        private void ResetFilters()
        {
            txtSearchID.Text      = string.Empty;
            txtSearchName.Text    = string.Empty;
            txtSearchPhone.Text   = string.Empty;
            txtSearchAddress.Text = string.Empty;
            RefreshGrid();
        }

        // ── KPI Bar
        private void RefreshKpi()
        {
            pnlKpi.Controls.Clear();

            var allVm = _ctrl.GetSupplierListVM();
            int total = allVm.Suppliers.Count;
            int shown = _currentSuppliers.Count;

            var pills = new[]
            {
                ("Total Suppliers", total.ToString(),
                 Color.FromArgb(19,  35,  61), Color.FromArgb(219, 234, 254)),
                ("Showing",         shown.ToString(),
                 Color.FromArgb( 6,  95,  70), Color.FromArgb(209, 250, 229)),
            };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent,
                Padding = new Padding(0), AutoScroll = false
            };

            const int PillW = 340, PillH = 60, Gap = 8, NumColW = 90;

            foreach (var (label, value, fg, bg) in pills)
            {
                var pill = new Panel { BackColor = bg, Size = new Size(PillW, PillH), Margin = new Padding(0, 0, Gap, 0) };
                pill.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path  = RoundedRect(((Panel)s).ClientRectangle, 8);
                    using var brush = new SolidBrush(((Panel)s).BackColor);
                    e.Graphics.FillPath(brush, path);
                };
                var tlp = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                    BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding = new Padding(10, 0, 8, 0)
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, NumColW));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                tlp.Controls.Add(new Label { Text = value, Font = new Font("Segoe UI", 14f, FontStyle.Bold), ForeColor = fg, BackColor = Color.Transparent, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, AutoSize = false }, 0, 0);
                tlp.Controls.Add(new Label { Text = label, Font = new Font("Segoe UI", 12f),                ForeColor = fg, BackColor = Color.Transparent, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,   AutoSize = false }, 1, 0);
                pill.Controls.Add(tlp);
                flow.Controls.Add(pill);
            }

            pnlKpi.Controls.Add(flow);

            if (_btnModify != null)
                _btnModify.Enabled = dgvSuppliers.CurrentRow != null;
        }

        // ── Grid events
        private void dgvSuppliers_SelectionChanged(object sender, EventArgs e)
        {
            if (_btnModify != null) _btnModify.Enabled = dgvSuppliers.CurrentRow != null;
        }

        private void dgvSuppliers_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            ShowDetailDialog(e.RowIndex);
        }

        // ══ Add New Supplier
        private void ShowAddDialog()
        {
            string nextId = _ctrl.GetNextSupplierID();
            using var dlg = new Form
            {
                Text = "Add New Supplier", Size = new Size(1400, 800), MinimumSize = new Size(1100, 800),
                StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false,
                BackColor = Color.FromArgb(240, 244, 249), Font = new Font("Segoe UI", 13f)
            };

            var pnlHeader  = BuildHeader("➕  Add New Supplier");
            var pnlSection = BuildSectionTitle("📋  Supplier Information");

            var txtId      = new TextBox { Text = nextId, ReadOnly = true, Font = new Font("Segoe UI", 12f, FontStyle.Bold), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(240, 244, 249), ForeColor = Color.FromArgb(47, 111, 237) };
            var txtName    = MakeDlgInput(placeholderText: "e.g. Premium Supplies Co.");
            var txtPhone   = MakeDlgInput(placeholderText: "e.g. +852 1234 5678");
            var txtAddress = MakeDlgInput(placeholderText: "Full mailing address");

            var rows      = new Panel[] { DlgFieldRow("Supplier ID  (auto)", txtId), DlgFieldRow("Supplier Name *", txtName), DlgFieldRow("Phone Number *", txtPhone), DlgFieldRow("Address *", txtAddress, lastRow: true) };
            var cardOuter = BuildCardOuter(rows);
            var pnlFoot   = BuildFooter();
            bool confirmed = false;

            var btnCreate = MakeDlgBtn("✔  Add Supplier", Color.White, Color.FromArgb(5, 150, 105), Color.FromArgb(4, 120, 87));
            btnCreate.Click += (s, ev) =>
            {
                if (!ValidateDlgFields((txtName.Text, "Supplier Name"), (txtPhone.Text, "Phone Number"), (txtAddress.Text, "Address"))) return;
                confirmed = true;
                dlg.Close();
            };
            var btnCancel = MakeDlgOutlineBtn("Cancel");
            btnCancel.Click += (s, ev) => dlg.Close();
            AttachFooterBtns(pnlFoot, btnCreate, btnCancel);

            var pnlFill = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };
            dlg.Controls.Add(pnlFill); dlg.Controls.Add(cardOuter); dlg.Controls.Add(pnlSection); dlg.Controls.Add(pnlHeader); dlg.Controls.Add(pnlFoot);
            dlg.ShowDialog(this);
            if (!confirmed) return;

            bool ok = _ctrl.AddSupplier(new SupplierEntity { SupplierID = nextId, SupplierName = txtName.Text.Trim(), PhoneNumber = txtPhone.Text.Trim(), SupplierAddress = txtAddress.Text.Trim() });
            if (ok) { MessageBox.Show("Supplier created successfully.", "Created", MessageBoxButtons.OK, MessageBoxIcon.Information); RefreshGrid(); }
            else      MessageBox.Show("Failed to add supplier. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // ══ Modify Supplier — CustomerListForm baseline (full-width header, no badge)
        private void ShowModifyDialog(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _currentSuppliers.Count) return;
            var s = _currentSuppliers[rowIndex];

            using var dlg = new Form
            {
                Text = $"Modify Supplier \u2014 {s.SupplierID}",
                Size = new Size(1400, 800), MinimumSize = new Size(1100, 800),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false,
                BackColor = Color.FromArgb(240, 244, 249), Font = new Font("Segoe UI", 13f)
            };

            // Header — full-width, no badge (CustomerListForm baseline)
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            pnlHeader.Controls.Add(new Label
            {
                Text = $"✏️  Modify Supplier \u2014 {s.SupplierID}",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.Transparent,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(32, 0, 0, 0)
            });

            var pnlSection = BuildSectionTitle("📝  Edit Supplier Information");

            var txtId      = new TextBox { Text = s.SupplierID, ReadOnly = true, Font = new Font("Segoe UI", 12f, FontStyle.Bold), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(240, 244, 249), ForeColor = Color.FromArgb(47, 111, 237) };
            var txtName    = MakeDlgInput(s.SupplierName);
            var txtPhone   = MakeDlgInput(s.PhoneNumber);
            var txtAddress = MakeDlgInput(s.SupplierAddress);

            var rows      = new Panel[] { DlgFieldRow("Supplier ID  (read-only)", txtId), DlgFieldRow("Supplier Name *", txtName), DlgFieldRow("Phone Number *", txtPhone), DlgFieldRow("Address *", txtAddress, lastRow: true) };
            var cardOuter = BuildCardOuter(rows);
            var pnlFoot   = BuildFooter();
            bool confirmed = false;

            var btnSave = MakeDlgBtn("✔  Save Changes", Color.FromArgb(92, 60, 0), Color.FromArgb(234, 179, 8), Color.FromArgb(202, 152, 0));
            btnSave.Click += (s2, ev) =>
            {
                if (!ValidateDlgFields((txtName.Text, "Supplier Name"), (txtPhone.Text, "Phone Number"), (txtAddress.Text, "Address"))) return;
                confirmed = true;
                dlg.Close();
            };
            var btnCancel = MakeDlgOutlineBtn("Cancel");
            btnCancel.Click += (s2, ev) => dlg.Close();
            AttachFooterBtns(pnlFoot, btnSave, btnCancel);

            var pnlFill = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };
            dlg.Controls.Add(pnlFill); dlg.Controls.Add(cardOuter); dlg.Controls.Add(pnlSection); dlg.Controls.Add(pnlHeader); dlg.Controls.Add(pnlFoot);
            dlg.ShowDialog(this);
            if (!confirmed) return;

            bool ok = _ctrl.UpdateSupplier(s.SupplierID, txtName.Text.Trim(), txtPhone.Text.Trim(), txtAddress.Text.Trim());
            if (ok) { MessageBox.Show("Supplier updated successfully.", "Updated", MessageBoxButtons.OK, MessageBoxIcon.Information); RefreshGrid(); }
            else      MessageBox.Show("Failed to update supplier. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // ══ Detail dialog (double-click) — CustomerListForm baseline (full-width header, no badge)
        private void ShowDetailDialog(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _currentSuppliers.Count) return;
            var s = _currentSuppliers[rowIndex];

            using var dlg = new Form
            {
                Text = $"Supplier \u2014 {s.SupplierID}",
                Size = new Size(1400, 700), MinimumSize = new Size(900, 700),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false,
                BackColor = Color.FromArgb(240, 244, 249), Font = new Font("Segoe UI", 13f)
            };

            // Header — full-width, no badge (CustomerListForm baseline)
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            pnlHeader.Controls.Add(new Label
            {
                Text = $"Supplier Details \u2014 {s.SupplierID}",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.Transparent,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(32, 0, 0, 0)
            });

            var pnlSection = BuildSectionTitle("📋  Supplier Information");

            Label ReadOnly(string val) => new Label { Text = val ?? "\u2014", Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(15, 31, 53), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.White, AutoEllipsis = true };

            var rows      = new Panel[] { DlgFieldRow("Supplier ID", ReadOnly(s.SupplierID)), DlgFieldRow("Supplier Name", ReadOnly(s.SupplierName)), DlgFieldRow("Phone Number", ReadOnly(s.PhoneNumber)), DlgFieldRow("Address", ReadOnly(s.SupplierAddress), lastRow: true) };
            var cardOuter = BuildCardOuter(rows);
            var pnlFoot   = BuildFooter();
            var btnClose  = MakeDlgOutlineBtn("Close");
            btnClose.Click += (s2, ev) => dlg.Close();
            AttachFooterBtns(pnlFoot, btnClose);

            var pnlFill = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };
            dlg.Controls.Add(pnlFill); dlg.Controls.Add(cardOuter); dlg.Controls.Add(pnlSection); dlg.Controls.Add(pnlHeader); dlg.Controls.Add(pnlFoot);
            dlg.ShowDialog(this);
        }

        // ══ Shared dialog builders (CustomerListForm baseline)
        private static Panel BuildHeader(string title)
        {
            var p = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            p.Controls.Add(new Label { Text = title, Font = new Font("Segoe UI", 18f, FontStyle.Bold), ForeColor = Color.White, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false, Padding = new Padding(32, 0, 0, 0) });
            return p;
        }

        private static Panel BuildSectionTitle(string text)
        {
            var p = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.FromArgb(241, 245, 255), Padding = new Padding(32, 0, 16, 0) };
            p.Paint += (s, e) => { using var pen = new Pen(Color.FromArgb(221, 227, 236), 1); e.Graphics.DrawLine(pen, 0, ((Panel)s).Height - 1, ((Panel)s).Width, ((Panel)s).Height - 1); };
            p.Controls.Add(new Label { Text = text, Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = Color.FromArgb(47, 111, 237), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false });
            return p;
        }

        private Panel DlgFieldRow(string labelText, Control input, bool lastRow = false)
        {
            var row = new Panel { Height = DLG_RowH, BackColor = Color.White };
            if (!lastRow)
                row.Paint += (s, pe) => { using var pen = new Pen(Color.FromArgb(221, 227, 236), 1); pe.Graphics.DrawLine(pen, 0, ((Panel)s).Height - 1, ((Panel)s).Width, ((Panel)s).Height - 1); };

            var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.White, CellBorderStyle = TableLayoutPanelCellBorderStyle.None };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DLG_LabelW));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tlp.Controls.Add(new Label { Text = labelText, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.FromArgb(70, 85, 110), BackColor = Color.FromArgb(248, 250, 252), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false, Padding = new Padding(24, 0, 8, 0) }, 0, 0);
            var wrap = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(20, 14, 24, 14) };
            input.Dock = DockStyle.Fill;
            wrap.Controls.Add(input);
            tlp.Controls.Add(wrap, 1, 0);
            row.Controls.Add(tlp);
            return row;
        }

        private Panel BuildCardOuter(Panel[] rows)
        {
            int cardH     = rows.Length * DLG_RowH;
            var cardOuter = new Panel { Dock = DockStyle.Top, Height = cardH + 32, BackColor = Color.Transparent, Padding = new Padding(20, 16, 20, 16) };
            var cardInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            cardInner.Paint += (s, e) => { var p = (Panel)s; using var pen = new Pen(Color.FromArgb(221, 227, 236), 1); e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1); };
            int yPos = 0;
            foreach (var r in rows) { r.Location = new Point(0, yPos); r.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; r.Width = 1300; cardInner.Controls.Add(r); yPos += DLG_RowH; }
            cardInner.Resize += (s2, _) => { var p = (Panel)s2; foreach (Control r in p.Controls) r.Width = p.Width; };
            cardOuter.Controls.Add(cardInner);
            return cardOuter;
        }

        private static Panel BuildFooter()
        {
            var p = new Panel { Dock = DockStyle.Bottom, Height = 96, BackColor = Color.White, Padding = new Padding(0, 18, 28, 18) };
            p.Paint += (s, e) => { using var pen = new Pen(Color.FromArgb(221, 227, 236), 1); e.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0); };
            return p;
        }

        private static void AttachFooterBtns(Panel footer, params Button[] btns)
        {
            var flow = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, BackColor = Color.Transparent };
            foreach (var b in btns) flow.Controls.Add(b);
            footer.Controls.Add(flow);
        }

        private Button MakeDlgBtn(string text, Color fg, Color bg, Color hover)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = fg, BackColor = bg, FlatStyle = FlatStyle.Flat, Width = DLG_BtnW, Height = DLG_BtnH, Cursor = Cursors.Hand, Margin = new Padding(0, 0, 10, 0) };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = hover;
            return b;
        }

        private Button MakeDlgOutlineBtn(string text)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White, FlatStyle = FlatStyle.Flat, Width = DLG_BtnW, Height = DLG_BtnH, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize  = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            return b;
        }

        private static TextBox MakeDlgInput(string initial = "", string placeholderText = "")
            => new TextBox { Text = initial, Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White, ForeColor = Color.FromArgb(15, 31, 53), PlaceholderText = placeholderText };

        private static bool ValidateDlgFields(params (string val, string fieldName)[] fields)
        {
            foreach (var (val, name) in fields)
                if (string.IsNullOrWhiteSpace(val))
                {
                    MessageBox.Show($"{name} is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            return true;
        }

        // ── Legacy helpers
        private Panel FieldRow(string labelText, Control input, bool lastRow = false)
            => DlgFieldRow(labelText, input, lastRow);

        private static void PaintCardRect(object s, PaintEventArgs e)
        {
            var p = (Panel)s;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        }

        private static void PaintBottomBorder(Panel p)
        {
            p.Paint += (s, e) => { using var pen = new Pen(Color.FromArgb(221, 227, 236), 1); e.Graphics.DrawLine(pen, 0, ((Panel)s).Height - 1, ((Panel)s).Width, ((Panel)s).Height - 1); };
        }

        private static void ShowValidation(string msg)
            => MessageBox.Show(msg, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        // ── Navigation & Logout
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to log out?",
                                "Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                SessionManager.Clear();
                Application.Restart();
            }
        }

        // ── RoundedRect (KPI pill painting)
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
