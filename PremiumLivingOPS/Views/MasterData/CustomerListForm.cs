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
    public partial class CustomerListForm : Form
    {
        private readonly MasterDataController _ctrl = new MasterDataController();
        private List<CustomerEntity> _currentCustomers = new List<CustomerEntity>();

        // ── Order status colour palette (mirrors ViewShipmentForm / ViewOrderForm)
        private static readonly Dictionary<string, (Color bg, Color fg)> OrderStatusColors =
            new Dictionary<string, (Color, Color)>
            {
                { "Pending",             (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "Processing",          (Color.FromArgb(219, 234, 254), Color.FromArgb( 29,  78, 216)) },
                { "Delivered",           (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
                { "Partially Delivered", (Color.FromArgb(237, 233, 254), Color.FromArgb( 91,  33, 182)) },
                { "Cancelled",           (Color.FromArgb(254, 226, 226), Color.FromArgb(185,  28,  28)) },
                { "Completed",           (Color.FromArgb(220, 252, 231), Color.FromArgb( 22, 101,  52)) },
            };

        public CustomerListForm()
        {
            InitializeComponent();
            this.Load += CustomerListForm_Load;
        }

        private void CustomerListForm_Load(object sender, EventArgs e) => RefreshGrid();

        // ── Data refresh
        private void RefreshGrid()
        {
            string idKw    = txtSearchID.Text.Trim();
            string nameKw  = txtSearchName.Text.Trim();
            string emailKw = txtSearchEmail.Text.Trim();
            string phoneKw = txtSearchPhone.Text.Trim();

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

        // ── KPI Bar
        private void RefreshKpi()
        {
            pnlKpi.Controls.Clear();

            var allVm = _ctrl.GetCustomerListVM();
            int total = allVm.Customers.Count;
            int shown = _currentCustomers.Count;

            var pills = new[]
            {
                ("Total Customers", total.ToString(),
                 Color.FromArgb(47, 111, 237), Color.FromArgb(219, 234, 254)),
                ("Showing",         shown.ToString(),
                 Color.FromArgb(6,  95,  70),  Color.FromArgb(209, 250, 229)),
            };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent,
                Padding = new Padding(0), AutoScroll = false
            };

            const int PillW = 290, PillH = 60, Gap = 8, NumColW = 80;

            foreach (var (label, count, fg, bg) in pills)
            {
                var pill = new Panel { BackColor = bg, Size = new Size(PillW, PillH), Margin = new Padding(0, 0, Gap, 0), Cursor = Cursors.Default };
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
                tlp.Controls.Add(new Label { Text = count, Font = new Font("Segoe UI", 14f, FontStyle.Bold), ForeColor = fg, BackColor = Color.Transparent, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, AutoSize = false }, 0, 0);
                tlp.Controls.Add(new Label { Text = label, Font = new Font("Segoe UI", 12f),                ForeColor = fg, BackColor = Color.Transparent, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,   AutoSize = false }, 1, 0);
                pill.Controls.Add(tlp);
                flow.Controls.Add(pill);
            }
            pnlKpi.Controls.Add(flow);
        }

        // ── Grid events
        private void dgvCustomers_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            ShowDetailDialog(e.RowIndex);
        }

        // ══ Customer Order History dialog (ViewShipment Detail pattern)
        private void ShowCustomerOrdersDialog(CustomerEntity c)
        {
            var orders = _ctrl.GetCustomerOrders(c.CustomerID);

            using var dlg = new Form
            {
                Text            = $"Order History \u2014 {c.CustomerID}",
                Size            = new Size(2500, 1050),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 13f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false
            };

            // ── Header (dark navy — same as ShowDetailDialog in ViewShipmentForm)
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            var tblHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(24, 0, 24, 0)
            };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 380f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblHeader.Controls.Add(new Label
            {
                Text      = $"Order History  \u2014  {c.CustomerName}",
                Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);
            tblHeader.Controls.Add(new Label
            {
                Text      = c.CustomerID,
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(147, 197, 253),
                BackColor = Color.Transparent,
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight,
                AutoSize  = false
            }, 1, 0);
            pnlHeader.Controls.Add(tblHeader);

            // ── Customer info panel (2-col key-value)
            var pnlInfo = new Panel
            {
                Dock = DockStyle.Top, Height = 160,
                Padding = new Padding(28, 16, 28, 8), BackColor = Color.White
            };
            pnlInfo.Paint += (sender, e) =>
            {
                using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawLine(pen, 28, ((Panel)sender).Height - 1,
                                    ((Panel)sender).Width - 28, ((Panel)sender).Height - 1);
            };

            var tblInfo = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            tblInfo.Controls.Add(DlgLabelKey("Customer ID"),   0, 0);
            tblInfo.Controls.Add(DlgLabelVal(c.CustomerID),     1, 0);
            tblInfo.Controls.Add(DlgLabelKey("Customer Name"),  2, 0);
            tblInfo.Controls.Add(DlgLabelVal(c.CustomerName),   3, 0);
            tblInfo.Controls.Add(DlgLabelKey("Email"),          0, 1);
            tblInfo.Controls.Add(DlgLabelVal(c.EmailAddress),   1, 1);
            tblInfo.Controls.Add(DlgLabelKey("Phone"),          2, 1);
            tblInfo.Controls.Add(DlgLabelVal(c.PhoneNumber),    3, 1);
            pnlInfo.Controls.Add(tblInfo);

            // ── Section label for grid
            var pnlLineLabel = new Panel
            {
                Dock = DockStyle.Top, Height = 40,
                BackColor = Color.FromArgb(246, 249, 255),
                Padding = new Padding(28, 0, 0, 0)
            };
            pnlLineLabel.Controls.Add(new Label
            {
                Text = "ORDER RECORDS",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            });
            pnlLineLabel.Paint += PaintBottomBorderStatic;

            // ── Orders DataGridView (same styling as ViewShipmentForm inner DGV)
            var dgv = new DataGridView
            {
                ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(221, 227, 236),
                Font = new Font("Segoe UI", 12f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate = { Height = 44 }, Dock = DockStyle.Fill,
                ColumnHeadersHeight = 40, EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255),
                    ForeColor = Color.FromArgb(98, 112, 135),
                    Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                    Padding   = new Padding(12, 0, 0, 0)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor          = Color.White,
                    ForeColor          = Color.FromArgb(15, 31, 53),
                    SelectionBackColor = Color.FromArgb(219, 234, 254),
                    SelectionForeColor = Color.FromArgb(15, 31, 53),
                    Padding            = new Padding(12, 6, 12, 6)
                }
            };
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrderID",      HeaderText = "ORDER NO.",      FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colContact",      HeaderText = "CONTACT",        FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIssuedTime",   HeaderText = "ORDER DATE",     FillWeight = 18 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDeliveryDate", HeaderText = "DELIVERY DATE",  FillWeight = 18 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colGrandTotal",   HeaderText = "GRAND TOTAL",    FillWeight = 18 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",       HeaderText = "STATUS",         FillWeight = 16 });

            foreach (var o in orders)
                dgv.Rows.Add(
                    o.OrderID,
                    o.OrderContactName ?? "\u2014",
                    o.IssuedTime.ToString("yyyy-MM-dd"),
                    o.DeliveryDate?.ToString("yyyy-MM-dd") ?? "\u2014",
                    $"HK$ {o.GrandTotal:N2}",
                    o.OrderStatus ?? "\u2014");

            // Colour the Status badge (same palette as CreateInvoiceForm / ViewOrderForm)
            dgv.CellFormatting += (sender, e) =>
            {
                if (e.RowIndex < 0 || dgv.Columns[e.ColumnIndex].Name != "colStatus" || e.Value == null) return;
                if (!OrderStatusColors.TryGetValue(e.Value.ToString(), out var sc)) return;
                e.CellStyle.BackColor          = sc.bg;
                e.CellStyle.ForeColor          = sc.fg;
                e.CellStyle.SelectionBackColor = sc.bg;
                e.CellStyle.SelectionForeColor = sc.fg;
                e.CellStyle.Font               = new Font("Segoe UI", 11f, FontStyle.Bold);
                e.CellStyle.Alignment          = DataGridViewContentAlignment.MiddleCenter;
                e.FormattingApplied            = true;
            };

            // ── Total row (mirrors ViewShipmentForm.ShowDetailDialog footer strip)
            var pnlTotalRow = new Panel
            {
                Dock = DockStyle.Bottom, Height = 64, BackColor = Color.White
            };
            pnlTotalRow.Paint += PaintTopBorderStatic;

            var tblTotals = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblTotals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblTotals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblTotals.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblTotals.Controls.Add(new Label
            {
                Text = $"Total Orders:   {orders.Count}",
                Dock = DockStyle.Fill, AutoSize = false,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(28, 0, 0, 0)
            }, 0, 0);

            double grandSum = 0;
            foreach (var o in orders) grandSum += o.GrandTotal;
            tblTotals.Controls.Add(new Label
            {
                Text = $"Total Value:   HK$ {grandSum:N2}",
                Dock = DockStyle.Fill, AutoSize = false,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(47, 111, 237),
                TextAlign = ContentAlignment.MiddleRight,
                Padding   = new Padding(0, 0, 28, 0)
            }, 1, 0);
            pnlTotalRow.Controls.Add(tblTotals);

            // ── Footer — Close button only
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom, Height = 86,
                BackColor = Color.White, Padding = new Padding(28, 14, 28, 14)
            };
            pnlFooter.Paint += PaintTopBorderStatic;

            var btnClose = new Button
            {
                Text = "Close",
                Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(150, 56), Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Location = new Point(2500 - 28 - 150 - 16, 14)
            };
            btnClose.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnClose.FlatAppearance.BorderSize         = 1;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnClose.Click += (_, __) => dlg.Close();
            pnlFooter.Controls.Add(btnClose);

            // ── Assemble (DOCK RULE: Fill first, then Top/Bottom in reverse render order)
            dlg.Controls.Add(dgv);
            dlg.Controls.Add(pnlFooter);
            dlg.Controls.Add(pnlTotalRow);
            dlg.Controls.Add(pnlLineLabel);
            dlg.Controls.Add(pnlInfo);
            dlg.Controls.Add(pnlHeader);
            dlg.ShowDialog(this);
        }

        // ══ Add New dialog
        private void ShowAddDialog()
        {
            string nextId = _ctrl.GetNextCustomerID();
            using var dlg = new Form
            {
                Text = "Add New Customer", Size = new Size(1400, 800), MinimumSize = new Size(1100, 800),
                StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false,
                BackColor = Color.FromArgb(240, 244, 249), Font = new Font("Segoe UI", 13f)
            };

            var pnlHeader  = BuildHeader("\u2795  Add New Customer");
            var pnlSection = BuildSectionTitle("\uD83D\uDCCB  Customer Information");

            var txtId    = new TextBox { Text = nextId, ReadOnly = true, Font = new Font("Segoe UI", 12f, FontStyle.Bold), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(240, 244, 249), ForeColor = Color.FromArgb(47, 111, 237) };
            var txtName  = MakeDlgInput();
            var txtEmail = MakeDlgInput();
            var txtPhone = MakeDlgInput();

            var rows      = new Panel[] { DlgFieldRow("Customer ID  (auto)", txtId), DlgFieldRow("Customer Name *", txtName), DlgFieldRow("Email Address *", txtEmail), DlgFieldRow("Phone Number *", txtPhone, lastRow: true) };
            var cardOuter = BuildCardOuter(rows);
            var pnlFoot   = BuildFooter();
            bool confirmed = false;

            var btnCreate = MakeDlgBtn("\u2714  Add Customer", Color.White, Color.FromArgb(5, 150, 105), Color.FromArgb(4, 120, 87));
            btnCreate.Click += (s, ev) => { if (!ValidateDlgFields((txtName.Text, "Customer Name"), (txtEmail.Text, "Email Address"), (txtPhone.Text, "Phone Number"))) return; confirmed = true; dlg.Close(); };
            var btnCancel = MakeDlgOutlineBtn("Cancel");
            btnCancel.Click += (s, ev) => dlg.Close();
            AttachFooterBtns(pnlFoot, btnCreate, btnCancel);

            var pnlFill = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };
            dlg.Controls.Add(pnlFill); dlg.Controls.Add(cardOuter); dlg.Controls.Add(pnlSection); dlg.Controls.Add(pnlHeader); dlg.Controls.Add(pnlFoot);
            dlg.ShowDialog(this);
            if (!confirmed) return;

            bool ok = _ctrl.AddCustomer(new CustomerEntity { CustomerID = nextId, CustomerName = txtName.Text.Trim(), EmailAddress = txtEmail.Text.Trim(), PhoneNumber = txtPhone.Text.Trim() });
            if (ok) { MessageBox.Show("Customer created successfully.", "Created", MessageBoxButtons.OK, MessageBoxIcon.Information); RefreshGrid(); }
            else      MessageBox.Show("Failed to add customer. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // ══ Modify dialog
        private void ShowModifyDialog(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _currentCustomers.Count) return;
            var c = _currentCustomers[rowIndex];

            using var dlg = new Form
            {
                Text = $"Modify Customer \u2014 {c.CustomerID}",
                Size = new Size(1400, 800), MinimumSize = new Size(1100, 800),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false,
                BackColor = Color.FromArgb(240, 244, 249), Font = new Font("Segoe UI", 13f)
            };

            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            pnlHeader.Controls.Add(new Label
            {
                Text = $"\u270F\uFE0F  Modify Customer \u2014 {c.CustomerID}",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.Transparent,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(32, 0, 0, 0)
            });

            var pnlSection = BuildSectionTitle("\uD83D\uDCDD  Edit Customer Information");

            var txtId    = new TextBox { Text = c.CustomerID, ReadOnly = true, Font = new Font("Segoe UI", 12f, FontStyle.Bold), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(240, 244, 249), ForeColor = Color.FromArgb(47, 111, 237) };
            var txtName  = MakeDlgInput(c.CustomerName);
            var txtEmail = MakeDlgInput(c.EmailAddress);
            var txtPhone = MakeDlgInput(c.PhoneNumber);

            var rows      = new Panel[] { DlgFieldRow("Customer ID  (read-only)", txtId), DlgFieldRow("Customer Name *", txtName), DlgFieldRow("Email Address *", txtEmail), DlgFieldRow("Phone Number *", txtPhone, lastRow: true) };
            var cardOuter = BuildCardOuter(rows);
            var pnlFoot   = BuildFooter();
            bool confirmed = false;

            var btnSave = MakeDlgBtn("\u2714  Save Changes", Color.FromArgb(92, 60, 0), Color.FromArgb(234, 179, 8), Color.FromArgb(202, 152, 0));
            btnSave.Click += (s, ev) => { if (!ValidateDlgFields((txtName.Text, "Customer Name"), (txtEmail.Text, "Email Address"), (txtPhone.Text, "Phone Number"))) return; confirmed = true; dlg.Close(); };
            var btnCancel = MakeDlgOutlineBtn("Cancel");
            btnCancel.Click += (s, ev) => dlg.Close();
            AttachFooterBtns(pnlFoot, btnSave, btnCancel);

            var pnlFill = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };
            dlg.Controls.Add(pnlFill); dlg.Controls.Add(cardOuter); dlg.Controls.Add(pnlSection); dlg.Controls.Add(pnlHeader); dlg.Controls.Add(pnlFoot);
            dlg.ShowDialog(this);
            if (!confirmed) return;

            bool ok = _ctrl.UpdateCustomer(c.CustomerID, txtName.Text.Trim(), txtEmail.Text.Trim(), txtPhone.Text.Trim());
            if (ok) { MessageBox.Show("Customer updated successfully.", "Updated", MessageBoxButtons.OK, MessageBoxIcon.Information); RefreshGrid(); }
            else      MessageBox.Show("Failed to update customer. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // ══ Detail dialog (double-click on grid row)
        private void ShowDetailDialog(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _currentCustomers.Count) return;
            var c = _currentCustomers[rowIndex];

            using var dlg = new Form
            {
                Text = $"Customer \u2014 {c.CustomerID}",
                Size = new Size(1400, 700), MinimumSize = new Size(900, 700),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false,
                BackColor = Color.FromArgb(240, 244, 249), Font = new Font("Segoe UI", 13f)
            };

            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            pnlHeader.Controls.Add(new Label
            {
                Text = $"Customer Details \u2014 {c.CustomerID}",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.Transparent,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(32, 0, 0, 0)
            });

            var pnlSection = BuildSectionTitle("\uD83D\uDCCB  Customer Information");

            Label ReadOnly(string val) => new Label { Text = val ?? "\u2014", Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(15, 31, 53), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.White, AutoEllipsis = true };

            var rows      = new Panel[] { DlgFieldRow("Customer ID", ReadOnly(c.CustomerID)), DlgFieldRow("Customer Name", ReadOnly(c.CustomerName)), DlgFieldRow("Email Address", ReadOnly(c.EmailAddress)), DlgFieldRow("Phone Number", ReadOnly(c.PhoneNumber), lastRow: true) };
            var cardOuter = BuildCardOuter(rows);
            var pnlFoot   = BuildFooter();
            var btnClose  = MakeDlgOutlineBtn("Close");
            btnClose.Click += (s, ev) => dlg.Close();
            AttachFooterBtns(pnlFoot, btnClose);

            var pnlFill = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };
            dlg.Controls.Add(pnlFill); dlg.Controls.Add(cardOuter); dlg.Controls.Add(pnlSection); dlg.Controls.Add(pnlHeader); dlg.Controls.Add(pnlFoot);
            dlg.ShowDialog(this);
        }

        // ══ Shared dialog builders
        private const int DLG_LabelW = 340, DLG_RowH = 80, DLG_BtnW = 210, DLG_BtnH = 60;

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

        private static TextBox MakeDlgInput(string initial = "", string placeholder = "")
            => new TextBox { Text = initial, Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White, ForeColor = Color.FromArgb(15, 31, 53), PlaceholderText = placeholder };

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

        // ── Dialog label helpers (mirrors ViewShipmentForm.MakeLabelKey / MakeLabelVal)
        private static Label DlgLabelKey(string text) => new Label
        {
            Text = text, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(98, 112, 135),
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(0, 0, 8, 0), AutoEllipsis = false
        };

        private static Label DlgLabelVal(string text) => new Label
        {
            Text = text ?? "\u2014", Font = new Font("Segoe UI", 12f),
            ForeColor = Color.FromArgb(15, 31, 53),
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        // ── Border/separator painters (same as ViewShipmentForm)
        private static void PaintBottomBorderStatic(object s, PaintEventArgs e)
        {
            var p = (Panel)s;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
        }

        private static void PaintTopBorderStatic(object s, PaintEventArgs e)
        {
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, 0, ((Control)s).Width, 0);
        }

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

        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }
    }
}
