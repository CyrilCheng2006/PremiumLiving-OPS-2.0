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
    /// Dialogs (Add New / Modify) are rendered to the same spec as
    /// ComplaintListForm.btnAddNew_Click (Create New Complaint):
    ///   • FixedDialog 1400×800, dark-navy header 80px, section-title bar 44px
    ///   • Each field = FieldRow(): left label col 340px (bg #f8fafc) + right wrap panel (bg White, padding 20,14,24,14)
    ///   • Card outer (DockStyle.Top, transparent) wraps card inner (White, 1px border-paint)
    ///   • Footer: White 96px, 1px top border, buttons in FlowPanel docked Right
    ///   • Confirm button: green #059669, Cancel button: outline
    /// </summary>
    public partial class SupplierListForm : Form
    {
        private readonly MasterDataController _ctrl = new MasterDataController();
        private List<SupplierEntity> _currentSuppliers = new List<SupplierEntity>();

        // Action buttons declared as fields so RefreshKpi can toggle Enabled
        private Button _btnAddNew;
        private Button _btnModify;

        // ── Dialog layout constants — mirror ComplaintListForm constants
        private const int DLG_LabelW = 340;   // left label column width
        private const int DLG_RowH   = 80;    // each field-row height
        private const int DLG_BtnW   = 210;   // footer button width
        private const int DLG_BtnH   = 60;    // footer button height

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

        // ────────────────────────────────────────────────────────────────
        // KPI Bar
        // ────────────────────────────────────────────────────────────────
        private void RefreshKpi()
        {
            pnlKpi.Controls.Clear();

            var allVm  = _ctrl.GetSupplierListVM();
            int total  = allVm.Suppliers.Count;
            int shown  = _currentSuppliers.Count;

            var pills = new[]
            {
                ("Total Suppliers", total.ToString(),
                 Color.FromArgb( 19,  35,  61), Color.FromArgb(219, 234, 254)),
                ("Showing",         shown.ToString(),
                 Color.FromArgb(  6,  95,  70), Color.FromArgb(209, 250, 229)),
            };

            var flow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent,
                Padding       = new Padding(0),
                AutoScroll    = false
            };

            const int PillW   = 340;
            const int PillH   = 60;
            const int Gap     = 8;
            const int NumColW = 90;

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
                tlp.Controls.Add(new Label
                {
                    Text = value, Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                    ForeColor = fg, BackColor = Color.Transparent,
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, AutoSize = false
                }, 0, 0);
                tlp.Controls.Add(new Label
                {
                    Text = label, Font = new Font("Segoe UI", 12f),
                    ForeColor = fg, BackColor = Color.Transparent,
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
                }, 1, 0);
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
            if (_btnModify != null)
                _btnModify.Enabled = dgvSuppliers.CurrentRow != null;
        }

        private void dgvSuppliers_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            ShowDetailDialog(e.RowIndex);
        }

        // ════════════════════════════════════════════════════════════════
        // ADD NEW SUPPLIER — styled to ComplaintListForm.btnAddNew_Click spec
        // ════════════════════════════════════════════════════════════════
        private void ShowAddDialog()
        {
            string nextId = _ctrl.GetNextSupplierID();

            using var dlg = new Form
            {
                Text            = "Add New Supplier",
                Size            = new Size(1400, 800),
                MinimumSize     = new Size(1100, 800),
                StartPosition   = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false,
                BackColor       = Color.FromArgb(240, 244, 249),
                Font            = new Font("Segoe UI", 13f)
            };

            // ── Header (80px dark navy, same as Create Complaint)
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            pnlHeader.Controls.Add(new Label
            {
                Text      = "\u2795  Add New Supplier",
                Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false,
                Padding   = new Padding(32, 0, 0, 0)
            });

            // ── Section title bar (44px, blue tint, same spec)
            var pnlSection = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 44,
                BackColor = Color.FromArgb(241, 245, 255),
                Padding   = new Padding(32, 0, 16, 0)
            };
            PaintBottomBorder(pnlSection);
            pnlSection.Controls.Add(new Label
            {
                Text      = "\uD83D\uDCCB  Supplier Information",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(47, 111, 237),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false
            });

            // ── Field inputs
            var txtId = new TextBox
            {
                Text        = nextId,
                ReadOnly    = true,
                Font        = new Font("Segoe UI", 12f, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor   = Color.FromArgb(240, 244, 249),
                ForeColor   = Color.FromArgb(47, 111, 237)
            };
            var txtName    = MakeInput(placeholderText: "e.g. Premium Supplies Co.");
            var txtPhone   = MakeInput(placeholderText: "e.g. +852 1234 5678");
            var txtAddress = MakeInput(placeholderText: "Full mailing address");

            // ── Field rows (FieldRow = same left-label + right-wrap layout as Complaint)
            var rowId      = FieldRow("Supplier ID  (auto)", txtId);
            var rowName    = FieldRow("Supplier Name *",     txtName);
            var rowPhone   = FieldRow("Phone Number *",      txtPhone);
            var rowAddress = FieldRow("Address *",           txtAddress, lastRow: true);

            // ── Card
            var allRows   = new Panel[] { rowId, rowName, rowPhone, rowAddress };
            int cardH     = allRows.Length * DLG_RowH;
            var cardOuter = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = cardH + 32,
                BackColor = Color.Transparent,
                Padding   = new Padding(20, 16, 20, 16)
            };
            var cardInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            cardInner.Paint += PaintCardRect;
            int yPos = 0;
            foreach (var r in allRows)
            {
                r.Location = new Point(0, yPos);
                r.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                r.Width    = 1300;
                cardInner.Controls.Add(r);
                yPos += DLG_RowH;
            }
            cardInner.Resize += (s2, _) =>
            { var p = (Panel)s2; foreach (Control r in p.Controls) r.Width = p.Width; };
            cardOuter.Controls.Add(cardInner);

            // ── Footer (96px White, 1px top border)
            var pnlFoot = BuildFooter();

            bool confirmed = false;

            var btnCreate = MakeBtn("\u2714  Add Supplier",
                Color.White, Color.FromArgb(5, 150, 105),
                Color.FromArgb(4, 120, 87));
            btnCreate.Click += (s2, ev) =>
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                { ShowValidation("Supplier Name is required."); return; }
                if (string.IsNullOrWhiteSpace(txtPhone.Text))
                { ShowValidation("Phone Number is required."); return; }
                if (string.IsNullOrWhiteSpace(txtAddress.Text))
                { ShowValidation("Address is required."); return; }
                confirmed = true;
                dlg.Close();
            };

            var btnCancel = MakeOutlineBtn("Cancel");
            btnCancel.Click += (s2, ev) => dlg.Close();

            var footFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight, BackColor = Color.Transparent
            };
            footFlow.Controls.Add(btnCreate);
            footFlow.Controls.Add(btnCancel);
            pnlFoot.Controls.Add(footFlow);

            var pnlFill = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };
            dlg.Controls.Add(pnlFill);
            dlg.Controls.Add(cardOuter);
            dlg.Controls.Add(pnlSection);
            dlg.Controls.Add(pnlHeader);
            dlg.Controls.Add(pnlFoot);
            dlg.ShowDialog(this);

            if (!confirmed) return;

            bool ok = _ctrl.AddSupplier(new SupplierEntity
            {
                SupplierID      = nextId,
                SupplierName    = txtName.Text.Trim(),
                PhoneNumber     = txtPhone.Text.Trim(),
                SupplierAddress = txtAddress.Text.Trim()
            });

            if (ok)
            {
                MessageBox.Show("Supplier created successfully.", "Created",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshGrid();
            }
            else
                MessageBox.Show("Failed to add supplier. Please try again.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // ════════════════════════════════════════════════════════════════
        // MODIFY SUPPLIER — same dialog shell, amber action button, pre-filled fields
        // ════════════════════════════════════════════════════════════════
        private void ShowModifyDialog(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _currentSuppliers.Count) return;
            var s = _currentSuppliers[rowIndex];

            using var dlg = new Form
            {
                Text            = $"Modify Supplier \u2014 {s.SupplierID}",
                Size            = new Size(1400, 800),
                MinimumSize     = new Size(1100, 800),
                StartPosition   = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false,
                BackColor       = Color.FromArgb(240, 244, 249),
                Font            = new Font("Segoe UI", 13f)
            };

            // ── Header
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };

            // Right status badge: shows Supplier ID in amber pill (same pattern as Complaint status pill)
            var badgeFont = new Font("Segoe UI", 13f, FontStyle.Bold);
            int badgeW    = TextRenderer.MeasureText(s.SupplierID, badgeFont).Width + 80;
            var badgeLbl  = new Label
            {
                Text      = s.SupplierID,
                Font      = badgeFont,
                ForeColor = Color.FromArgb(92, 60, 0),
                BackColor = Color.FromArgb(254, 243, 199),
                Dock      = DockStyle.Fill,
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleCenter
            };
            badgeLbl.Paint += (s2, pe) =>
            {
                var lb  = (Label)s2;
                using var pen = new Pen(Color.FromArgb(120, 146, 64, 14), 1);
                pe.Graphics.DrawRectangle(pen, 0, 0, lb.Width - 1, lb.Height - 1);
            };

            var headerTlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            headerTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            headerTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, badgeW));
            headerTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            headerTlp.Controls.Add(new Label
            {
                Text      = $"\u270F\uFE0F  Modify Supplier \u2014 {s.SupplierID}",
                Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                Padding   = new Padding(32, 0, 0, 0)
            }, 0, 0);
            headerTlp.Controls.Add(badgeLbl, 1, 0);
            pnlHeader.Controls.Add(headerTlp);

            // ── Section title bar
            var pnlSection = new Panel
            {
                Dock = DockStyle.Top, Height = 44,
                BackColor = Color.FromArgb(241, 245, 255),
                Padding = new Padding(32, 0, 16, 0)
            };
            PaintBottomBorder(pnlSection);
            pnlSection.Controls.Add(new Label
            {
                Text      = "\uD83D\uDCDD  Edit Supplier Information",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(47, 111, 237),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false
            });

            // ── Fields (ID read-only, others editable)
            var txtId = new TextBox
            {
                Text        = s.SupplierID,
                ReadOnly    = true,
                Font        = new Font("Segoe UI", 12f, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor   = Color.FromArgb(240, 244, 249),
                ForeColor   = Color.FromArgb(47, 111, 237)
            };
            var txtName    = MakeInput(s.SupplierName);
            var txtPhone   = MakeInput(s.PhoneNumber);
            var txtAddress = MakeInput(s.SupplierAddress);

            var rowId      = FieldRow("Supplier ID  (read-only)", txtId);
            var rowName    = FieldRow("Supplier Name *",          txtName);
            var rowPhone   = FieldRow("Phone Number *",           txtPhone);
            var rowAddress = FieldRow("Address *",                txtAddress, lastRow: true);

            // ── Card
            var allRows   = new Panel[] { rowId, rowName, rowPhone, rowAddress };
            int cardH     = allRows.Length * DLG_RowH;
            var cardOuter = new Panel
            {
                Dock = DockStyle.Top, Height = cardH + 32,
                BackColor = Color.Transparent, Padding = new Padding(20, 16, 20, 16)
            };
            var cardInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            cardInner.Paint += PaintCardRect;
            int yPos = 0;
            foreach (var r in allRows)
            {
                r.Location = new Point(0, yPos);
                r.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                r.Width    = 1300;
                cardInner.Controls.Add(r);
                yPos += DLG_RowH;
            }
            cardInner.Resize += (s2, _) =>
            { var p = (Panel)s2; foreach (Control r in p.Controls) r.Width = p.Width; };
            cardOuter.Controls.Add(cardInner);

            // ── Footer
            var pnlFoot = BuildFooter();
            bool confirmed = false;

            var btnSave   = MakeBtn("\u2714  Save Changes",
                Color.White, Color.FromArgb(234, 179, 8),
                Color.FromArgb(202, 152, 0));
            btnSave.ForeColor = Color.FromArgb(92, 60, 0);
            btnSave.Click += (s2, ev) =>
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                { ShowValidation("Supplier Name is required."); return; }
                if (string.IsNullOrWhiteSpace(txtPhone.Text))
                { ShowValidation("Phone Number is required."); return; }
                if (string.IsNullOrWhiteSpace(txtAddress.Text))
                { ShowValidation("Address is required."); return; }
                confirmed = true;
                dlg.Close();
            };

            var btnCancel = MakeOutlineBtn("Cancel");
            btnCancel.Click += (s2, ev) => dlg.Close();

            var footFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight, BackColor = Color.Transparent
            };
            footFlow.Controls.Add(btnSave);
            footFlow.Controls.Add(btnCancel);
            pnlFoot.Controls.Add(footFlow);

            var pnlFill = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };
            dlg.Controls.Add(pnlFill);
            dlg.Controls.Add(cardOuter);
            dlg.Controls.Add(pnlSection);
            dlg.Controls.Add(pnlHeader);
            dlg.Controls.Add(pnlFoot);
            dlg.ShowDialog(this);

            if (!confirmed) return;

            bool ok = _ctrl.UpdateSupplier(s.SupplierID,
                txtName.Text.Trim(), txtPhone.Text.Trim(), txtAddress.Text.Trim());

            if (ok)
            {
                MessageBox.Show("Supplier updated successfully.", "Updated",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshGrid();
            }
            else
                MessageBox.Show("Failed to update supplier. Please try again.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // ════════════════════════════════════════════════════════════════
        // DETAIL dialog (double-click on row)
        // ════════════════════════════════════════════════════════════════
        private void ShowDetailDialog(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _currentSuppliers.Count) return;
            var s = _currentSuppliers[rowIndex];

            using var dlg = new Form
            {
                Text            = $"Supplier \u2014 {s.SupplierID}",
                Size            = new Size(1400, 700),
                MinimumSize     = new Size(900, 700),
                StartPosition   = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false,
                BackColor       = Color.FromArgb(240, 244, 249),
                Font            = new Font("Segoe UI", 13f)
            };

            // Header with status badge
            var badgeFont = new Font("Segoe UI", 13f, FontStyle.Bold);
            int badgeW    = TextRenderer.MeasureText(s.SupplierID, badgeFont).Width + 80;
            var badgeLbl  = new Label
            {
                Text = s.SupplierID, Font = badgeFont,
                ForeColor = Color.FromArgb(19, 35, 61), BackColor = Color.FromArgb(219, 234, 254),
                Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleCenter
            };
            badgeLbl.Paint += (s2, pe) =>
            {
                var lb = (Label)s2;
                using var pen = new Pen(Color.FromArgb(120, 47, 111, 237), 1);
                pe.Graphics.DrawRectangle(pen, 0, 0, lb.Width - 1, lb.Height - 1);
            };

            var headerTlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            headerTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            headerTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, badgeW));
            headerTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            headerTlp.Controls.Add(new Label
            {
                Text = $"Supplier Details \u2014 {s.SupplierID}",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold), ForeColor = Color.White,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent, Padding = new Padding(32, 0, 0, 0)
            }, 0, 0);
            headerTlp.Controls.Add(badgeLbl, 1, 0);

            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            pnlHeader.Controls.Add(headerTlp);

            // Section title bar
            var pnlSection = new Panel
            {
                Dock = DockStyle.Top, Height = 44,
                BackColor = Color.FromArgb(241, 245, 255), Padding = new Padding(32, 0, 16, 0)
            };
            PaintBottomBorder(pnlSection);
            pnlSection.Controls.Add(new Label
            {
                Text = "\uD83D\uDCCB  Supplier Information",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(47, 111, 237),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            });

            // Read-only field rows
            Label ReadOnly(string val) => new Label
            {
                Text = val ?? "\u2014", Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.White, AutoEllipsis = true
            };

            var rows = new Panel[]
            {
                FieldRow("Supplier ID",   ReadOnly(s.SupplierID)),
                FieldRow("Supplier Name", ReadOnly(s.SupplierName)),
                FieldRow("Phone Number",  ReadOnly(s.PhoneNumber)),
                FieldRow("Address",       ReadOnly(s.SupplierAddress), lastRow: true)
            };

            int cardH     = rows.Length * DLG_RowH;
            var cardOuter = new Panel
            {
                Dock = DockStyle.Top, Height = cardH + 32,
                BackColor = Color.Transparent, Padding = new Padding(20, 16, 20, 16)
            };
            var cardInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            cardInner.Paint += PaintCardRect;
            int yPos = 0;
            foreach (var r in rows)
            {
                r.Location = new Point(0, yPos);
                r.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                r.Width    = 1300;
                cardInner.Controls.Add(r);
                yPos += DLG_RowH;
            }
            cardInner.Resize += (s2, _) =>
            { var p = (Panel)s2; foreach (Control r in p.Controls) r.Width = p.Width; };
            cardOuter.Controls.Add(cardInner);

            // Footer
            var pnlFoot = BuildFooter();
            var btnClose = MakeOutlineBtn("Close");
            btnClose.Click += (s2, ev) => dlg.Close();
            var footFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight, BackColor = Color.Transparent
            };
            footFlow.Controls.Add(btnClose);
            pnlFoot.Controls.Add(footFlow);

            var pnlFill = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };
            dlg.Controls.Add(pnlFill);
            dlg.Controls.Add(cardOuter);
            dlg.Controls.Add(pnlSection);
            dlg.Controls.Add(pnlHeader);
            dlg.Controls.Add(pnlFoot);
            dlg.ShowDialog(this);
        }

        // ════════════════════════════════════════════════════════════════
        // Dialog shared builders — identical spec to ComplaintListForm helpers
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// FieldRow: left label col (DLG_LabelW, bg #f8fafc) + right wrap panel (White, padding 20,14,24,14).
        /// Bottom divider border on all rows except the last.
        /// Mirrors ComplaintListForm.MakeRow().
        /// </summary>
        private Panel FieldRow(string labelText, Control input, bool lastRow = false)
        {
            var row = new Panel { Height = DLG_RowH, BackColor = Color.White };
            if (!lastRow)
                row.Paint += (s, pe) =>
                {
                    using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
                    pe.Graphics.DrawLine(pen, 0, ((Panel)s).Height - 1, ((Panel)s).Width, ((Panel)s).Height - 1);
                };

            var tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.White, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
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

        /// <summary>Footer panel: 96px White, 1px top border, right-padding 28px.</summary>
        private static Panel BuildFooter()
        {
            var p = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 96,
                BackColor = Color.White,
                Padding   = new Padding(0, 18, 28, 18)
            };
            p.Paint += (s, pe) =>
            {
                using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
                pe.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0);
            };
            return p;
        }

        /// <summary>Solid action button (DLG_BtnW × DLG_BtnH, no border).</summary>
        private Button MakeBtn(string text, Color fg, Color bg, Color hover)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = fg,
                BackColor = bg,
                FlatStyle = FlatStyle.Flat,
                Width     = DLG_BtnW,
                Height    = DLG_BtnH,
                Cursor    = Cursors.Hand,
                Margin    = new Padding(0, 0, 10, 0)
            };
            b.FlatAppearance.BorderSize         = 0;
            b.FlatAppearance.MouseOverBackColor = hover;
            return b;
        }

        /// <summary>Outline cancel button (DLG_BtnW × DLG_BtnH, 1px border).</summary>
        private Button MakeOutlineBtn(string text)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Width     = DLG_BtnW,
                Height    = DLG_BtnH,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize         = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            return b;
        }

        /// <summary>Editable TextBox, Segoe UI 12pt, FixedSingle border.</summary>
        private static TextBox MakeInput(string initial = "", string placeholderText = "")
        {
            var tb = new TextBox
            {
                Text            = initial,
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                BackColor       = Color.White,
                ForeColor       = Color.FromArgb(15, 31, 53),
                PlaceholderText = placeholderText
            };
            return tb;
        }

        /// <summary>Paint 1px card border (PaintEventHandler-compatible).</summary>
        private static void PaintCardRect(object s, PaintEventArgs e)
        {
            var p = (Panel)s;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        }

        /// <summary>Paint bottom-only 1px border on a panel.</summary>
        private static void PaintBottomBorder(Panel p)
        {
            p.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawLine(pen, 0, ((Panel)s).Height - 1, ((Panel)s).Width, ((Panel)s).Height - 1);
            };
        }

        private static void ShowValidation(string msg)
            => MessageBox.Show(msg, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        // ── Navigation & Logout
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        { SessionManager.Clear(); Application.Restart(); }

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
