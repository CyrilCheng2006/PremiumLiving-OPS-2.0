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
    /// View \u2014 Customer List page (Master Data Maintenance module).
    /// Search block and KPI Bar are rewritten to match SupplierListForm baseline:
    ///   \u2022 Search: 4-field keyword grid (ID / Name / Email / Phone), section-title bar,
    ///     blue Search btn + outline Reset btn
    ///   \u2022 KPI: rounded pills (340\u00d760, SmoothingMode.AntiAlias, 14pt/12pt, NumColW=90),
    ///     action buttons (Add New green + Modify amber, 210\u00d760) docked Right
    /// MVC role: pure View. All data access delegated to MasterDataController.
    /// </summary>
    public partial class CustomerListForm : Form
    {
        private readonly MasterDataController _ctrl = new MasterDataController();
        private List<CustomerEntity> _currentCustomers = new List<CustomerEntity>();

        public CustomerListForm()
        {
            InitializeComponent();
            this.Load += CustomerListForm_Load;
        }

        private void CustomerListForm_Load(object sender, EventArgs e) => RefreshGrid();

        // ── Data refresh ─────────────────────────────────────────────────────
        private void RefreshGrid()
        {
            // Collect non-empty keywords from the 4 search fields
            string idKw    = txtSearchID.Text.Trim();
            string nameKw  = txtSearchName.Text.Trim();
            string emailKw = txtSearchEmail.Text.Trim();
            string phoneKw = txtSearchPhone.Text.Trim();

            // Priority: first non-empty field wins (single-keyword API same as Supplier)
            string keyword = !string.IsNullOrEmpty(idKw)    ? idKw
                           : !string.IsNullOrEmpty(nameKw)  ? nameKw
                           : !string.IsNullOrEmpty(emailKw) ? emailKw
                           : !string.IsNullOrEmpty(phoneKw) ? phoneKw
                           : null;

            var vm = _ctrl.GetCustomerListVM(keyword);

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Master Data Maintenance  \u203a  Customer List");

            _currentCustomers = vm.Customers;

            dgvCustomers.Rows.Clear();
            foreach (var c in _currentCustomers)
                dgvCustomers.Rows.Add(c.CustomerID, c.CustomerName, c.EmailAddress, c.PhoneNumber);

            RefreshKpi();
        }

        private void ResetFilters()
        {
            txtSearchID.Text    = string.Empty;
            txtSearchName.Text  = string.Empty;
            txtSearchEmail.Text = string.Empty;
            txtSearchPhone.Text = string.Empty;
            RefreshGrid();
        }

        // ════════════════════════════════════════════════════════════════
        //  KPI Bar  \u2014 mirrors SupplierListForm.RefreshKpi() spec exactly
        //  Pills: 340\u00d760, SmoothingMode.AntiAlias, RoundedRect r=8, Gap=8
        //  Number col 90px, 14pt Bold; Label col fill, 12pt
        //  Buttons: Add New (green #16A34A, 210\u00d760) | Modify (amber #EAB308, 210\u00d760)
        // ════════════════════════════════════════════════════════════════
        private void RefreshKpi()
        {
            pnlKpi.Controls.Clear();

            var allVm = _ctrl.GetCustomerListVM();
            int total = allVm.Customers.Count;
            int shown = _currentCustomers.Count;

            var pills = new[]
            {
                ("Total Customers", total.ToString(),
                 Color.FromArgb(19,  35,  61), Color.FromArgb(219, 234, 254)),
                ("Showing",         shown.ToString(),
                 Color.FromArgb( 6,  95,  70), Color.FromArgb(209, 250, 229)),
            };

            // Left-side pill flow
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
                var pill = new Panel
                {
                    BackColor = bg,
                    Size      = new Size(PillW, PillH),
                    Margin    = new Padding(0, 0, Gap, 0)
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
                    Padding         = new Padding(10, 0, 8, 0)
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, NumColW));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                tlp.Controls.Add(new Label
                {
                    Text      = value,
                    Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                    ForeColor = fg, BackColor = Color.Transparent,
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize  = false
                }, 0, 0);
                tlp.Controls.Add(new Label
                {
                    Text      = label,
                    Font      = new Font("Segoe UI", 12f),
                    ForeColor = fg, BackColor = Color.Transparent,
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoSize  = false
                }, 1, 0);

                pill.Controls.Add(tlp);
                flow.Controls.Add(pill);
            }

            pnlKpi.Controls.Add(flow);

            // ── Action buttons (docked Right, FlowDirection LTR)
            //    mirrors SupplierListForm: Add New green, Modify amber, each 210\u00d760
            Button btnModify = null;

            var btnAdd = MakeKpiButton("\u2795  Add New Customer",
                Color.White, Color.FromArgb(22, 163, 74), Color.FromArgb(21, 128, 61));
            btnAdd.Click += (s, e) => ShowAddDialog();

            btnModify = MakeKpiButton("\u270F\uFE0F  Modify",
                Color.FromArgb(92, 60, 0), Color.FromArgb(234, 179, 8), Color.FromArgb(202, 152, 0));
            btnModify.Enabled = dgvCustomers.CurrentRow != null;
            btnModify.Click += (s, e) =>
            {
                int idx = dgvCustomers.CurrentRow?.Index ?? -1;
                if (idx >= 0 && idx < _currentCustomers.Count)
                    ShowModifyDialog(idx);
            };

            var pnlBtns = new FlowLayoutPanel
            {
                Dock          = DockStyle.Right,
                AutoSize      = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor     = Color.Transparent,
                Padding       = new Padding(0, 0, 0, 0),
                WrapContents  = false
            };
            pnlBtns.Controls.Add(btnAdd);
            pnlBtns.Controls.Add(btnModify);
            pnlKpi.Controls.Add(pnlBtns);

            // Update Modify enabled state when grid selection changes
            dgvCustomers.SelectionChanged += (s, e) =>
            {
                if (btnModify != null)
                    btnModify.Enabled = dgvCustomers.CurrentRow != null;
            };
        }

        // ── KPI action-button factory (210\u00d760, mirrors SupplierListForm MakeBtn) ──
        private static Button MakeKpiButton(string text, Color fg, Color bg, Color hover)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = fg,
                BackColor = bg,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(210, 60),
                Margin    = new Padding(8, 0, 0, 0),
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize         = 0;
            b.FlatAppearance.MouseOverBackColor = hover;
            return b;
        }

        // ── Grid events ───────────────────────────────────────────────────────
        private void dgvCustomers_SelectionChanged(object sender, EventArgs e) { /* handled in RefreshKpi */ }

        private void dgvCustomers_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            ShowDetailDialog(e.RowIndex);
        }

        // ════════════════════════════════════════════════════════════════
        //  Add New dialog (styled to SupplierListForm ShowAddDialog spec)
        // ════════════════════════════════════════════════════════════════
        private void ShowAddDialog()
        {
            string nextId = _ctrl.GetNextCustomerID();

            using var dlg = new Form
            {
                Text            = "Add New Customer",
                Size            = new Size(1400, 800),
                MinimumSize     = new Size(1100, 800),
                StartPosition   = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false,
                BackColor       = Color.FromArgb(240, 244, 249),
                Font            = new Font("Segoe UI", 13f)
            };

            var pnlHeader = BuildHeader("\u2795  Add New Customer");
            var pnlSection = BuildSectionTitle("\uD83D\uDCCB  Customer Information");

            var txtId = new TextBox
            {
                Text        = nextId,
                ReadOnly    = true,
                Font        = new Font("Segoe UI", 12f, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor   = Color.FromArgb(240, 244, 249),
                ForeColor   = Color.FromArgb(47, 111, 237)
            };
            var txtName  = MakeDlgInput();
            var txtEmail = MakeDlgInput();
            var txtPhone = MakeDlgInput();

            var rows = new Panel[]
            {
                DlgFieldRow("Customer ID  (auto)", txtId),
                DlgFieldRow("Customer Name *",     txtName),
                DlgFieldRow("Email Address *",      txtEmail),
                DlgFieldRow("Phone Number *",        txtPhone, lastRow: true)
            };

            var cardOuter = BuildCardOuter(rows);

            var pnlFoot  = BuildFooter();
            bool confirmed = false;

            var btnCreate = MakeDlgBtn("\u2714  Add Customer",
                Color.White, Color.FromArgb(5, 150, 105), Color.FromArgb(4, 120, 87));
            btnCreate.Click += (s, ev) =>
            {
                if (!ValidateDlgFields(
                    (txtName.Text,  "Customer Name"),
                    (txtEmail.Text, "Email Address"),
                    (txtPhone.Text, "Phone Number"))) return;
                confirmed = true;
                dlg.Close();
            };

            var btnCancel = MakeDlgOutlineBtn("Cancel");
            btnCancel.Click += (s, ev) => dlg.Close();

            AttachFooterBtns(pnlFoot, btnCreate, btnCancel);

            var pnlFill = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };
            dlg.Controls.Add(pnlFill);
            dlg.Controls.Add(cardOuter);
            dlg.Controls.Add(pnlSection);
            dlg.Controls.Add(pnlHeader);
            dlg.Controls.Add(pnlFoot);
            dlg.ShowDialog(this);

            if (!confirmed) return;

            bool ok = _ctrl.AddCustomer(new CustomerEntity
            {
                CustomerID   = nextId,
                CustomerName = txtName.Text.Trim(),
                EmailAddress = txtEmail.Text.Trim(),
                PhoneNumber  = txtPhone.Text.Trim()
            });

            if (ok)
            {
                MessageBox.Show("Customer created successfully.", "Created",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshGrid();
            }
            else
                MessageBox.Show("Failed to add customer. Please try again.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // ════════════════════════════════════════════════════════════════
        //  Modify dialog
        // ════════════════════════════════════════════════════════════════
        private void ShowModifyDialog(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _currentCustomers.Count) return;
            var c = _currentCustomers[rowIndex];

            using var dlg = new Form
            {
                Text            = $"Modify Customer \u2014 {c.CustomerID}",
                Size            = new Size(1400, 800),
                MinimumSize     = new Size(1100, 800),
                StartPosition   = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false,
                BackColor       = Color.FromArgb(240, 244, 249),
                Font            = new Font("Segoe UI", 13f)
            };

            // Header with amber CustomerID badge (same as SupplierListForm Modify)
            var badgeFont = new Font("Segoe UI", 13f, FontStyle.Bold);
            int badgeW    = TextRenderer.MeasureText(c.CustomerID, badgeFont).Width + 80;
            var badgeLbl  = new Label
            {
                Text      = c.CustomerID,
                Font      = badgeFont,
                ForeColor = Color.FromArgb(92, 60, 0),
                BackColor = Color.FromArgb(254, 243, 199),
                Dock      = DockStyle.Fill, AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter
            };
            badgeLbl.Paint += (s, pe) =>
            {
                var lb = (Label)s;
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
                Text      = $"\u270F\uFE0F  Modify Customer \u2014 {c.CustomerID}",
                Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.Transparent,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(32, 0, 0, 0)
            }, 0, 0);
            headerTlp.Controls.Add(badgeLbl, 1, 0);

            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            pnlHeader.Controls.Add(headerTlp);

            var pnlSection = BuildSectionTitle("\uD83D\uDCDD  Edit Customer Information");

            var txtId = new TextBox
            {
                Text        = c.CustomerID,
                ReadOnly    = true,
                Font        = new Font("Segoe UI", 12f, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor   = Color.FromArgb(240, 244, 249),
                ForeColor   = Color.FromArgb(47, 111, 237)
            };
            var txtName  = MakeDlgInput(c.CustomerName);
            var txtEmail = MakeDlgInput(c.EmailAddress);
            var txtPhone = MakeDlgInput(c.PhoneNumber);

            var rows = new Panel[]
            {
                DlgFieldRow("Customer ID  (read-only)", txtId),
                DlgFieldRow("Customer Name *",          txtName),
                DlgFieldRow("Email Address *",           txtEmail),
                DlgFieldRow("Phone Number *",             txtPhone, lastRow: true)
            };
            var cardOuter = BuildCardOuter(rows);

            var pnlFoot  = BuildFooter();
            bool confirmed = false;

            var btnSave   = MakeDlgBtn("\u2714  Save Changes",
                Color.FromArgb(92, 60, 0), Color.FromArgb(234, 179, 8), Color.FromArgb(202, 152, 0));
            btnSave.Click += (s, ev) =>
            {
                if (!ValidateDlgFields(
                    (txtName.Text,  "Customer Name"),
                    (txtEmail.Text, "Email Address"),
                    (txtPhone.Text, "Phone Number"))) return;
                confirmed = true;
                dlg.Close();
            };
            var btnCancel = MakeDlgOutlineBtn("Cancel");
            btnCancel.Click += (s, ev) => dlg.Close();
            AttachFooterBtns(pnlFoot, btnSave, btnCancel);

            var pnlFill = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };
            dlg.Controls.Add(pnlFill);
            dlg.Controls.Add(cardOuter);
            dlg.Controls.Add(pnlSection);
            dlg.Controls.Add(pnlHeader);
            dlg.Controls.Add(pnlFoot);
            dlg.ShowDialog(this);

            if (!confirmed) return;

            bool ok = _ctrl.UpdateCustomer(c.CustomerID,
                txtName.Text.Trim(), txtEmail.Text.Trim(), txtPhone.Text.Trim());

            if (ok)
            {
                MessageBox.Show("Customer updated successfully.", "Updated",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshGrid();
            }
            else
                MessageBox.Show("Failed to update customer. Please try again.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // ════════════════════════════════════════════════════════════════
        //  Detail dialog (double-click row)
        // ════════════════════════════════════════════════════════════════
        private void ShowDetailDialog(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _currentCustomers.Count) return;
            var c = _currentCustomers[rowIndex];

            using var dlg = new Form
            {
                Text            = $"Customer \u2014 {c.CustomerID}",
                Size            = new Size(1400, 700),
                MinimumSize     = new Size(900, 700),
                StartPosition   = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false, MinimizeBox = false,
                BackColor       = Color.FromArgb(240, 244, 249),
                Font            = new Font("Segoe UI", 13f)
            };

            // Header with blue badge
            var badgeFont = new Font("Segoe UI", 13f, FontStyle.Bold);
            int badgeW    = TextRenderer.MeasureText(c.CustomerID, badgeFont).Width + 80;
            var badgeLbl  = new Label
            {
                Text = c.CustomerID, Font = badgeFont,
                ForeColor = Color.FromArgb(19, 35, 61), BackColor = Color.FromArgb(219, 234, 254),
                Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleCenter
            };
            badgeLbl.Paint += (s, pe) =>
            {
                var lb = (Label)s;
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
                Text = $"Customer Details \u2014 {c.CustomerID}",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold), ForeColor = Color.White,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent, Padding = new Padding(32, 0, 0, 0)
            }, 0, 0);
            headerTlp.Controls.Add(badgeLbl, 1, 0);
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            pnlHeader.Controls.Add(headerTlp);

            var pnlSection = BuildSectionTitle("\uD83D\uDCCB  Customer Information");

            Label ReadOnly(string val) => new Label
            {
                Text = val ?? "\u2014", Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.White, AutoEllipsis = true
            };

            var rows = new Panel[]
            {
                DlgFieldRow("Customer ID",   ReadOnly(c.CustomerID)),
                DlgFieldRow("Customer Name", ReadOnly(c.CustomerName)),
                DlgFieldRow("Email Address", ReadOnly(c.EmailAddress)),
                DlgFieldRow("Phone Number",  ReadOnly(c.PhoneNumber), lastRow: true)
            };
            var cardOuter = BuildCardOuter(rows);

            var pnlFoot  = BuildFooter();
            var btnClose = MakeDlgOutlineBtn("Close");
            btnClose.Click += (s, ev) => dlg.Close();
            AttachFooterBtns(pnlFoot, btnClose);

            var pnlFill = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };
            dlg.Controls.Add(pnlFill);
            dlg.Controls.Add(cardOuter);
            dlg.Controls.Add(pnlSection);
            dlg.Controls.Add(pnlHeader);
            dlg.Controls.Add(pnlFoot);
            dlg.ShowDialog(this);
        }

        // ════════════════════════════════════════════════════════════════
        //  Shared dialog builders (identical spec to SupplierListForm)
        // ════════════════════════════════════════════════════════════════

        private const int DLG_LabelW = 340;
        private const int DLG_RowH   = 80;
        private const int DLG_BtnW   = 210;
        private const int DLG_BtnH   = 60;

        private static Panel BuildHeader(string title)
        {
            var p = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            p.Controls.Add(new Label
            {
                Text = title, Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false,
                Padding = new Padding(32, 0, 0, 0)
            });
            return p;
        }

        private static Panel BuildSectionTitle(string text)
        {
            var p = new Panel
            {
                Dock = DockStyle.Top, Height = 44,
                BackColor = Color.FromArgb(241, 245, 255),
                Padding = new Padding(32, 0, 16, 0)
            };
            p.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawLine(pen, 0, ((Panel)s).Height - 1, ((Panel)s).Width, ((Panel)s).Height - 1);
            };
            p.Controls.Add(new Label
            {
                Text = text, Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(47, 111, 237),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            });
            return p;
        }

        private Panel DlgFieldRow(string labelText, Control input, bool lastRow = false)
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

            tlp.Controls.Add(new Label
            {
                Text = labelText,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 85, 110),
                BackColor = Color.FromArgb(248, 250, 252),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false, Padding = new Padding(24, 0, 8, 0)
            }, 0, 0);

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
            var cardOuter = new Panel
            {
                Dock = DockStyle.Top, Height = cardH + 32,
                BackColor = Color.Transparent, Padding = new Padding(20, 16, 20, 16)
            };
            var cardInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            cardInner.Paint += (s, e) =>
            {
                var p = (Panel)s;
                using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            };
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
            return cardOuter;
        }

        private static Panel BuildFooter()
        {
            var p = new Panel
            {
                Dock = DockStyle.Bottom, Height = 96,
                BackColor = Color.White, Padding = new Padding(0, 18, 28, 18)
            };
            p.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0);
            };
            return p;
        }

        private static void AttachFooterBtns(Panel footer, params Button[] btns)
        {
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight, BackColor = Color.Transparent
            };
            foreach (var b in btns) flow.Controls.Add(b);
            footer.Controls.Add(flow);
        }

        private Button MakeDlgBtn(string text, Color fg, Color bg, Color hover)
        {
            var b = new Button
            {
                Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = fg, BackColor = bg, FlatStyle = FlatStyle.Flat,
                Width = DLG_BtnW, Height = DLG_BtnH, Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0)
            };
            b.FlatAppearance.BorderSize         = 0;
            b.FlatAppearance.MouseOverBackColor = hover;
            return b;
        }

        private Button MakeDlgOutlineBtn(string text)
        {
            var b = new Button
            {
                Text = text, Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat, Width = DLG_BtnW, Height = DLG_BtnH,
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize         = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            return b;
        }

        private static TextBox MakeDlgInput(string initial = "", string placeholder = "")
        {
            return new TextBox
            {
                Text = initial, Font = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 31, 53), PlaceholderText = placeholder
            };
        }

        private static bool ValidateDlgFields(params (string val, string fieldName)[] fields)
        {
            foreach (var (val, name) in fields)
                if (string.IsNullOrWhiteSpace(val))
                {
                    MessageBox.Show($"{name} is required.", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            return true;
        }

        // ── Rounded rect helper (KPI pill painting) ─────────────────────────
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

        // ── Navigation & Logout ──────────────────────────────────────────────
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }
    }
}
