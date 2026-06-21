using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.AfterService
{
    public partial class AccountReceivableForm : Form
    {
        private readonly AfterServiceController _ctrl = new AfterServiceController();
        private List<InvoiceDetailEntity> _invoices = new List<InvoiceDetailEntity>();

        private static readonly Dictionary<string, (Color bg, Color fg)> StatusColors =
            new Dictionary<string, (Color, Color)>
            {
                { "Partial",  (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "Full",     (Color.FromArgb(220, 252, 231), Color.FromArgb( 22, 101,  52)) },
                { "Overdue",  (Color.FromArgb(254, 226, 226), Color.FromArgb(185,  28,  28)) },
            };

        private static readonly Color OverdueBg = Color.FromArgb(255, 242, 242);

        public AccountReceivableForm()
        {
            InitializeComponent();
            this.Load += AccountReceivableForm_Load;
        }

        private void AccountReceivableForm_Load(object sender, EventArgs e) => RefreshGrid();

        // ── Grid refresh
        private void RefreshGrid()
        {
            string statusSel    = cboStatus.SelectedItem?.ToString();
            string statusFilter = (statusSel == "All" || string.IsNullOrEmpty(statusSel)) ? null : statusSel;
            string keyword      = txtKeyword.Text.Trim();

            var arVm = _ctrl.GetAccountReceivableVM(statusFilter, string.IsNullOrEmpty(keyword) ? null : keyword);
            _shell.SetUser(arVm.UserBar.DisplayName, arVm.UserBar.Department);
            _shell.SetVisibleMenus(arVm.AllowedMenus);
            _shell.SetBreadcrumb("After-Service  ›  Account Receivable");

            _invoices = _ctrl.GetInvoiceListVM(string.IsNullOrEmpty(keyword) ? null : keyword).Invoices;

            if (!string.IsNullOrEmpty(statusFilter))
                _invoices = _invoices.FindAll(i =>
                    string.Equals(i.IsOverdue ? "Overdue" : i.PaymentStatus,
                                  statusFilter, StringComparison.OrdinalIgnoreCase));

            dgvAR.Rows.Clear();
            foreach (var inv in _invoices)
                dgvAR.Rows.Add(
                    inv.InvoiceID,
                    inv.OrderID,
                    inv.CustomerName,
                    $"HK$ {inv.TotalAmount:N2}",
                    $"HK$ {inv.PaidAmount:N2}",
                    $"HK$ {inv.RemainingBalance:N2}",
                    inv.IsOverdue ? "Overdue" : inv.PaymentStatus,
                    inv.DueDate.ToString("yyyy-MM-dd"));

            RefreshKpi();
        }

        private void ResetSearch()
        {
            txtKeyword.Text         = string.Empty;
            cboStatus.SelectedIndex = 0;
            RefreshGrid();
        }

        // ── SelectionChanged — enable/disable Record button
        private void dgvAR_SelectionChanged(object sender, EventArgs e)
        {
            bool hasRow = dgvAR.SelectedRows.Count > 0 && dgvAR.SelectedRows[0].Index >= 0;
            btnRecord.Enabled = hasRow;
        }

        // ── KPI Pills
        private void RefreshKpi()
        {
            pnlKpi.Controls.Clear();

            var all = _ctrl.GetAccountReceivableVM().Items;

            int    totalCount   = all.Count;
            double outstanding  = 0;
            int    overdueCount = 0;
            int    partialCount = 0;
            int    fullCount    = 0;

            foreach (var i in all)
            {
                outstanding += i.RemainingBalance;
                if (i.IsOverdue)                 overdueCount++;
                if (i.PaymentStatus == "Partial") partialCount++;
                if (i.PaymentStatus == "Full")    fullCount++;
            }

            var pills = new[]
            {
                ("Total Invoices",    totalCount.ToString(),   Color.FromArgb( 19,  35,  61), Color.FromArgb(219, 234, 254)),
                ("Outstanding (HK$)", $"{outstanding:N0}",     Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199)),
                ("Partial",           partialCount.ToString(), Color.FromArgb( 29,  78, 216), Color.FromArgb(219, 234, 254)),
                ("Fully Paid",        fullCount.ToString(),    Color.FromArgb( 22, 101,  52), Color.FromArgb(220, 252, 231)),
                ("Overdue",           overdueCount.ToString(), Color.FromArgb(185,  28,  28), Color.FromArgb(254, 226, 226)),
            };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent,
                Padding = new Padding(0), AutoScroll = false,
            };

            const int PillW   = 340;  // updated: 270 → 340
            const int PillH   =  60;
            const int Gap     =   8;
            const int NumColW =  90;

            foreach (var (label, value, fg, bg) in pills)
            {
                var pill = new Panel { BackColor = bg, Size = new Size(PillW, PillH), Margin = new Padding(0, 0, Gap, 0), Cursor = Cursors.Hand };
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
                    Padding = new Padding(10, 0, 8, 0),
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, NumColW));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                tlp.Controls.Add(new Label { Text = value, Font = new Font("Segoe UI", 14f, FontStyle.Bold), ForeColor = fg, BackColor = Color.Transparent, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, AutoSize = false }, 0, 0);
                tlp.Controls.Add(new Label { Text = label, Font = new Font("Segoe UI", 11f),                ForeColor = fg, BackColor = Color.Transparent, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,   AutoSize = false }, 1, 0);
                pill.Controls.Add(tlp);
                flow.Controls.Add(pill);
            }
            pnlKpi.Controls.Add(flow);
        }

        // ── CellFormatting
        private void dgvAR_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _invoices.Count) return;
            var inv = _invoices[e.RowIndex];

            if (inv.IsOverdue)
            {
                e.CellStyle.BackColor          = OverdueBg;
                e.CellStyle.SelectionBackColor = Color.FromArgb(255, 220, 220);
            }

            if (dgvAR.Columns[e.ColumnIndex].Name == "colStatus" && e.Value != null)
            {
                if (StatusColors.TryGetValue(e.Value.ToString(), out var c))
                {
                    e.CellStyle.BackColor          = c.bg;
                    e.CellStyle.ForeColor          = c.fg;
                    e.CellStyle.SelectionBackColor = c.bg;
                    e.CellStyle.SelectionForeColor = c.fg;
                    e.CellStyle.Font      = new Font("Segoe UI", 11f, FontStyle.Bold);
                    e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                e.FormattingApplied = true;
            }
        }

        // ── Record Payment
        private void OpenRecordPayment()
        {
            if (dgvAR.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select an invoice from the list.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int idx = dgvAR.SelectedRows[0].Index;
            if (idx < 0 || idx >= _invoices.Count) return;
            var inv = _invoices[idx];

            if (inv.PaymentStatus == "Full" && !inv.IsOverdue)
            {
                MessageBox.Show(
                    $"Invoice {inv.InvoiceID} is already fully paid.\nNo further payment is required.",
                    "Already Paid", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ShowRecordPaymentDialog(inv);
            RefreshGrid();
        }

        // ── Record Payment Dialog  1800 × 800
        private void ShowRecordPaymentDialog(InvoiceDetailEntity inv)
        {
            using var dlg = new Form
            {
                Text = $"Record Payment  —  {inv.InvoiceID}",
                Size = new Size(1800, 800), StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.White, Font = new Font("Segoe UI", 13f),
                FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false
            };

            // Header
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Color.FromArgb(1, 105, 111) };
            var tblHeader = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None, Padding = new Padding(28, 0, 24, 0) };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblHeader.Controls.Add(new Label { Text = $"Record Payment  —  {inv.InvoiceID}", Font = new Font("Segoe UI", 17f, FontStyle.Bold), ForeColor = Color.White, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false }, 0, 0);
            Color badgeBg = inv.IsOverdue ? Color.FromArgb(185, 28, 28) : Color.FromArgb(146, 64, 14);
            tblHeader.Controls.Add(new Label { Text = $"Balance: HK$ {inv.RemainingBalance:N2}", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = Color.White, BackColor = badgeBg, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, AutoSize = false, Padding = new Padding(8, 4, 8, 4) }, 1, 0);
            pnlHeader.Controls.Add(tblHeader);

            // Info panel
            var pnlInfo = new Panel { Dock = DockStyle.Top, Height = 140, Padding = new Padding(28, 16, 28, 8), BackColor = Color.White };
            pnlInfo.Paint += PaintBottomBorder;
            var tblInfo = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 3, BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None };
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            for (int r = 0; r < 3; r++) tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f));
            AddInfoRow(tblInfo, 0, "Customer:",     inv.CustomerName,                 "Order No.:",   inv.OrderID);
            AddInfoRow(tblInfo, 1, "Total Amount:", $"HK$ {inv.TotalAmount:N2}",      "Paid Amount:", $"HK$ {inv.PaidAmount:N2}");
            AddInfoRow(tblInfo, 2, "Balance:",      $"HK$ {inv.RemainingBalance:N2}", "Due Date:",    inv.DueDate.ToString("yyyy-MM-dd"));
            pnlInfo.Controls.Add(tblInfo);

            // Payment History label
            var pnlTxnLabel = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.FromArgb(246, 249, 255), Padding = new Padding(28, 0, 0, 0) };
            pnlTxnLabel.Controls.Add(new Label { Text = "PAYMENT HISTORY", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(98, 112, 135), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft });
            pnlTxnLabel.Paint += PaintBottomBorder;

            // Txn grid
            var dgvTxn = new DataGridView
            {
                ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(221, 227, 236), Font = new Font("Segoe UI", 12f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate = { Height = 44 }, Dock = DockStyle.Fill,
                ColumnHeadersHeight = 40, EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(246, 249, 255), ForeColor = Color.FromArgb(98, 112, 135), Font = new Font("Segoe UI", 10f, FontStyle.Bold), Padding = new Padding(12, 0, 0, 0) },
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.White, ForeColor = Color.FromArgb(15, 31, 53), SelectionBackColor = Color.FromArgb(219, 234, 254), SelectionForeColor = Color.FromArgb(15, 31, 53), Padding = new Padding(12, 6, 12, 6) }
            };
            dgvTxn.Columns.Add(new DataGridViewTextBoxColumn { Name = "cTxnID",  HeaderText = "TRANSACTION ID", FillWeight = 28 });
            dgvTxn.Columns.Add(new DataGridViewTextBoxColumn { Name = "cDate",   HeaderText = "DATE",           FillWeight = 18 });
            dgvTxn.Columns.Add(new DataGridViewTextBoxColumn { Name = "cType",   HeaderText = "TYPE",           FillWeight = 18 });
            dgvTxn.Columns.Add(new DataGridViewTextBoxColumn { Name = "cAmount", HeaderText = "AMOUNT (HK$)",   FillWeight = 20 });
            if (inv.Transactions.Count == 0)
            {
                dgvTxn.Rows.Add("—", "—", "—", "No payment recorded yet");
                dgvTxn.Rows[0].DefaultCellStyle.ForeColor = Color.FromArgb(180, 180, 180);
                dgvTxn.Rows[0].DefaultCellStyle.Font      = new Font("Segoe UI", 11f, FontStyle.Italic);
            }
            else
            {
                foreach (var t in inv.Transactions)
                    dgvTxn.Rows.Add(t.TransactionID, t.TransactionDate.ToString("yyyy-MM-dd"), t.TransactionType, $"HK$ {t.Amount:N2}");
            }

            // Input label
            var pnlInputLabel = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.FromArgb(240, 253, 250), Padding = new Padding(28, 0, 16, 0) };
            pnlInputLabel.Paint += PaintBottomBorder;
            pnlInputLabel.Controls.Add(new Label { Text = "💳  Record New Payment", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = Color.FromArgb(1, 105, 111), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false });

            // Input body
            var pnlInputBody = new Panel { Dock = DockStyle.Top, Height = 130, BackColor = Color.FromArgb(249, 254, 253), Padding = new Padding(28, 16, 28, 12) };
            pnlInputBody.Paint += PaintBottomBorder;
            var nudAmount = new NumericUpDown { Font = new Font("Segoe UI", 12f), Minimum = 0.01m, Maximum = (decimal)Math.Max(inv.RemainingBalance, 0.01), DecimalPlaces = 2, Value = (decimal)Math.Min(inv.RemainingBalance, inv.RemainingBalance > 0 ? inv.RemainingBalance : 0.01), Location = new Point(200, 10), Size = new Size(200, 36), ThousandsSeparator = true };
            var cboType   = new ComboBox    { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f), Location = new Point(610, 10), Size = new Size(200, 36) };
            cboType.Items.AddRange(new object[] { "Installment", "Full", "Deposit" });
            cboType.SelectedIndex = inv.RemainingBalance <= inv.TotalAmount * 0.5 ? 1 : 0;
            pnlInputBody.Controls.Add(new Label { Text = "Payment Amount *", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(98, 112, 135), AutoSize = true, Location = new Point(0, 14) });
            pnlInputBody.Controls.Add(nudAmount);
            pnlInputBody.Controls.Add(new Label { Text = "Payment Type *",   Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(98, 112, 135), AutoSize = true, Location = new Point(430, 14) });
            pnlInputBody.Controls.Add(cboType);
            pnlInputBody.Controls.Add(new Label { Text = "Transaction Date", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(98, 112, 135), AutoSize = true, Location = new Point(0, 72) });
            pnlInputBody.Controls.Add(new Label { Text = DateTime.Today.ToString("yyyy-MM-dd"), Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.FromArgb(1, 105, 111), Location = new Point(200, 68), Size = new Size(200, 36), TextAlign = ContentAlignment.MiddleLeft });

            // Footer
            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 70, BackColor = Color.White, Padding = new Padding(0, 12, 28, 12) };
            pnlFooter.Paint += PaintTopBorder;
            var btnConfirm = new Button { Text = "✔  Confirm Payment", Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(5, 150, 105), FlatStyle = FlatStyle.Flat, Dock = DockStyle.Right, Width = 220, Cursor = Cursors.Hand };
            btnConfirm.FlatAppearance.BorderSize = 0; btnConfirm.FlatAppearance.MouseOverBackColor = Color.FromArgb(4, 120, 87);
            var btnCancel = new Button { Text = "Cancel", Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White, FlatStyle = FlatStyle.Flat, Dock = DockStyle.Right, Width = 140, Cursor = Cursors.Hand };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236); btnCancel.FlatAppearance.BorderSize = 1; btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnCancel.Click += (o, ev) => dlg.Close();
            btnConfirm.Click += (o, ev) =>
            {
                double amount = (double)nudAmount.Value;
                if (amount <= 0) { MessageBox.Show("Payment amount must be greater than zero.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); nudAmount.Focus(); return; }
                if (amount > inv.RemainingBalance + 0.005) { MessageBox.Show($"Payment amount (HK$ {amount:N2}) exceeds the remaining balance (HK$ {inv.RemainingBalance:N2}).", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); nudAmount.Focus(); return; }
                if (cboType.SelectedIndex < 0) { MessageBox.Show("Please select a payment type.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); cboType.Focus(); return; }
                try
                {
                    _ctrl.RecordPayment(inv.InvoiceID, amount, cboType.SelectedItem.ToString());
                    MessageBox.Show($"Payment of HK$ {amount:N2} recorded successfully for {inv.InvoiceID}.", "Payment Recorded", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.Close();
                }
                catch (Exception ex) { MessageBox.Show($"Failed to record payment:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            };
            pnlFooter.Controls.Add(btnConfirm);
            pnlFooter.Controls.Add(btnCancel);

            // Assemble
            dlg.Controls.Add(dgvTxn);
            dlg.Controls.Add(pnlTxnLabel);
            dlg.Controls.Add(pnlInputBody);
            dlg.Controls.Add(pnlInputLabel);
            dlg.Controls.Add(pnlInfo);
            dlg.Controls.Add(pnlHeader);
            dlg.Controls.Add(pnlFooter);
            dlg.ShowDialog(this);
        }

        // ── Shell events
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);
        private void btnLogout_Click(object sender, EventArgs e)
        { SessionManager.Clear(); Application.Restart(); }

        // ── Paint helpers
        private static void PaintBottomBorder(object s, PaintEventArgs e)
        { var p = (Control)s; using var pen = new Pen(Color.FromArgb(221, 227, 236)); e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1); }
        private static void PaintTopBorder(object s, PaintEventArgs e)
        { using var pen = new Pen(Color.FromArgb(221, 227, 236)); e.Graphics.DrawLine(pen, 0, 0, ((Control)s).Width, 0); }

        // ── Info row helpers
        private static void AddInfoRow(TableLayoutPanel tbl, int row, string lbl1, string val1, string lbl2, string val2)
        {
            tbl.Controls.Add(MakeLabelKey(lbl1), 0, row); tbl.Controls.Add(MakeLabelVal(val1), 1, row);
            tbl.Controls.Add(MakeLabelKey(lbl2), 2, row); tbl.Controls.Add(MakeLabelVal(val2), 3, row);
        }
        private static Label MakeLabelKey(string text) => new Label { Text = text, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(98, 112, 135), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false };
        private static Label MakeLabelVal(string text) => new Label { Text = text, Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(15, 31, 53), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false };

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath(); int d = radius * 2;
            path.AddArc(r.X,         r.Y,          d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y,          d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d,   0, 90);
            path.AddArc(r.X,         r.Bottom - d, d, d,  90, 90);
            path.CloseFigure(); return path;
        }
    }
}
