using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.AfterService
{
    /// <summary>
    /// Invoice List + Record Payment — 強出式視窗 (1400×860)
    ///
    /// Layout (top → bottom, DockStyle):
    ///   pnlHeader  Top  80  — dark-blue header, title + live Outstanding amount
    ///   pnlSearch  Top  70  — search field + [Search] [Reset]
    ///   pnlGrid    Fill     — Invoice DataGridView (main list)
    ///   pnlFooter  Bottom 70— [Record Payment] [Close]
    ///
    /// Record Payment sub-dialog opened by ShowRecordPaymentDialog():
    ///   Header   (Top 70)  — teal header, Invoice ID + balance badge
    ///   Info     (Top 140) — 4-col TLP: Customer / OrderID / Total / Paid / Balance / DueDate
    ///   TxnLabel (Top 40)  — "PAYMENT HISTORY" bar
    ///   dgvTxn   (Fill)    — Transaction history grid (read-only)
    ///   InputLbl (Top 44)  — "Record New Payment" bar
    ///   InputBody(Top 130) — Amount (NumericUpDown) + Type (ComboBox) + Date (readonly today)
    ///   Footer   (Bottom 70)— [✔ Confirm Payment] [Cancel]
    ///
    /// MVC: all DB calls delegated to AfterServiceController. Zero SQL here.
    /// Visual language: aligned to ViewShipmentForm.ShowDetailDialog render baseline.
    /// </summary>
    public class InvoiceListDialog : Form
    {
        private readonly AfterServiceController _ctrl = new AfterServiceController();
        private List<InvoiceDetailEntity>        _invoices = new List<InvoiceDetailEntity>();

        private TextBox      _txtSearch;
        private DataGridView _dgv;
        private Label        _lblOutstanding;

        private static readonly Dictionary<string, (Color bg, Color fg)> StatusColors =
            new Dictionary<string, (Color, Color)>
            {
                { "Partial", (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "Full",    (Color.FromArgb(220, 252, 231), Color.FromArgb( 22, 101,  52)) },
                { "Overdue", (Color.FromArgb(254, 226, 226), Color.FromArgb(185,  28,  28)) },
            };

        public InvoiceListDialog()
        {
            BuildUI();
            this.Load += (s, e) => RefreshGrid();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  UI Construction
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void BuildUI()
        {
            this.Text            = "Invoice List  —  Account Receivable";
            this.Size            = new Size(1400, 860);
            this.MinimumSize     = new Size(1100, 660);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.BackColor       = Color.White;
            this.Font            = new Font("Segoe UI", 13f);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;

            // ── Header ─────────────────────────────────────────────────────────
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(15, 31, 53) };
            var tblHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(28, 0, 28, 0)
            };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            tblHeader.Controls.Add(new Label
            {
                Text = "📋  Invoice List  —  Account Receivable",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold), ForeColor = Color.White,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);

            _lblOutstanding = new Label
            {
                Text = "Outstanding: HK$ —",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(254, 243, 199), BackColor = Color.Transparent,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, AutoSize = false
            };
            tblHeader.Controls.Add(_lblOutstanding, 1, 0);
            pnlHeader.Controls.Add(tblHeader);

            // ── Search bar ─────────────────────────────────────────────────────
            var pnlSearch = new Panel
            {
                Dock = DockStyle.Top, Height = 70,
                BackColor = Color.FromArgb(246, 249, 255), Padding = new Padding(24, 12, 24, 10)
            };
            pnlSearch.Paint += PaintBottomBorderStatic;

            _txtSearch = new TextBox
            {
                Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Invoice ID / Order No. / Customer",
                Location = new Point(0, 0), Size = new Size(400, 36)
            };
            _txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGrid(); };

            var btnSearch = MakePrimaryBtn("🔍  Search", new Point(412, 0), 160, 36);
            var btnReset  = MakeOutlineBtn("↺  Reset",  new Point(580, 0), 120, 36);
            btnSearch.Click += (s, e) => RefreshGrid();
            btnReset.Click  += (s, e) => { _txtSearch.Text = string.Empty; RefreshGrid(); };

            pnlSearch.Controls.Add(_txtSearch);
            pnlSearch.Controls.Add(btnSearch);
            pnlSearch.Controls.Add(btnReset);

            // ── Footer ──────────────────────────────────────────────────────
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom, Height = 70, BackColor = Color.White,
                Padding = new Padding(0, 12, 28, 12)
            };
            pnlFooter.Paint += PaintTopBorderStatic;

            var btnRecord = new Button
            {
                Text = "💳  Record Payment",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.White,
                BackColor = Color.FromArgb(1, 105, 111), FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Right, Width = 220, Cursor = Cursors.Hand
            };
            btnRecord.FlatAppearance.BorderSize = 0;
            btnRecord.FlatAppearance.MouseOverBackColor = Color.FromArgb(12, 78, 84);

            var btnClose = new Button
            {
                Text = "Close", Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat, Dock = DockStyle.Right, Width = 130, Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnClose.FlatAppearance.BorderSize         = 1;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnClose.Click  += (s, e) => this.Close();
            btnRecord.Click += (s, e) => OpenRecordPayment();

            pnlFooter.Controls.Add(btnRecord);
            pnlFooter.Controls.Add(btnClose);

            // ── Grid ──────────────────────────────────────────────────────────
            var pnlGridWrap = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 12, 24, 0), BackColor = Color.White };

            _dgv = new DataGridView
            {
                ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(221, 227, 236), Font = new Font("Segoe UI", 12f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate = { Height = 48 }, Dock = DockStyle.Fill,
                ColumnHeadersHeight = 44, EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255), ForeColor = Color.FromArgb(98, 112, 135),
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    Padding = new Padding(12, 0, 0, 0), Alignment = DataGridViewContentAlignment.MiddleLeft
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.White, ForeColor = Color.FromArgb(15, 31, 53),
                    SelectionBackColor = Color.FromArgb(219, 234, 254), SelectionForeColor = Color.FromArgb(15, 31, 53),
                    Padding = new Padding(12, 6, 12, 6)
                }
            };

            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colInvoiceID", HeaderText = "INVOICE ID",    FillWeight = 16 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrderID",   HeaderText = "ORDER NO.",     FillWeight = 14 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomer",  HeaderText = "CUSTOMER",      FillWeight = 18 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal",     HeaderText = "TOTAL (HK$)",   FillWeight = 12 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPaid",      HeaderText = "PAID (HK$)",    FillWeight = 12 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colBalance",   HeaderText = "BALANCE (HK$)", FillWeight = 12 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",    HeaderText = "STATUS",        FillWeight = 10 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDueDate",   HeaderText = "DUE DATE",      FillWeight = 10 });

            _dgv.CellFormatting  += DgvCellFormatting;
            _dgv.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) OpenRecordPayment(); };

            pnlGridWrap.Controls.Add(_dgv);

            // ── Assemble (Bottom first, then Fill, then Top panels) ─────────────────
            this.Controls.Add(pnlGridWrap);  // Fill
            this.Controls.Add(pnlSearch);    // Top (added after Fill -> renders below header)
            this.Controls.Add(pnlHeader);    // Top — topmost
            this.Controls.Add(pnlFooter);    // Bottom
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Grid refresh
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void RefreshGrid()
        {
            string kw = _txtSearch.Text.Trim();
            _invoices = _ctrl.GetInvoiceListVM(string.IsNullOrEmpty(kw) ? null : kw).Invoices;

            _dgv.Rows.Clear();
            double outstanding = 0;
            foreach (var inv in _invoices)
            {
                outstanding += inv.RemainingBalance;
                _dgv.Rows.Add(
                    inv.InvoiceID,
                    inv.OrderID,
                    inv.CustomerName,
                    $"HK$ {inv.TotalAmount:N2}",
                    $"HK$ {inv.PaidAmount:N2}",
                    $"HK$ {inv.RemainingBalance:N2}",
                    inv.IsOverdue ? "Overdue" : inv.PaymentStatus,
                    inv.DueDate.ToString("yyyy-MM-dd"));
            }
            _lblOutstanding.Text = $"Outstanding: HK$ {outstanding:N0}";
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Cell Formatting
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private static readonly Color OverdueBg = Color.FromArgb(255, 242, 242);

        private void DgvCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _invoices.Count) return;
            var inv = _invoices[e.RowIndex];

            if (inv.IsOverdue)
            {
                e.CellStyle.BackColor          = OverdueBg;
                e.CellStyle.SelectionBackColor = Color.FromArgb(255, 220, 220);
            }

            if (_dgv.Columns[e.ColumnIndex].Name == "colStatus" && e.Value != null)
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Open Record Payment sub-dialog
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void OpenRecordPayment()
        {
            if (_dgv.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select an invoice first.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int rowIdx = _dgv.SelectedRows[0].Index;
            if (rowIdx < 0 || rowIdx >= _invoices.Count) return;
            var inv = _invoices[rowIdx];

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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  RECORD PAYMENT DIALOG  (inline Form, shown via ShowDialog)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void ShowRecordPaymentDialog(InvoiceDetailEntity inv)
        {
            using var dlg = new Form
            {
                Text = $"Record Payment  —  {inv.InvoiceID}",
                Size = new Size(1200, 820), StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.White, Font = new Font("Segoe UI", 13f),
                FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false
            };

            // ── Header ───────────────────────────────────────────────────────────
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Color.FromArgb(1, 105, 111) };
            var tblHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(28, 0, 24, 0)
            };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            tblHeader.Controls.Add(new Label
            {
                Text = $"Record Payment  —  {inv.InvoiceID}",
                Font = new Font("Segoe UI", 17f, FontStyle.Bold), ForeColor = Color.White,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);

            Color badgeBg = inv.IsOverdue ? Color.FromArgb(185, 28, 28) : Color.FromArgb(146, 64, 14);
            tblHeader.Controls.Add(new Label
            {
                Text = $"Balance: HK$ {inv.RemainingBalance:N2}",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = Color.White,
                BackColor = badgeBg, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter, AutoSize = false,
                Padding = new Padding(8, 4, 8, 4)
            }, 1, 0);
            pnlHeader.Controls.Add(tblHeader);

            // ── Info panel (4-col TLP) ───────────────────────────────────────────────
            var pnlInfo = new Panel { Dock = DockStyle.Top, Height = 140, Padding = new Padding(28, 16, 28, 8), BackColor = Color.White };
            pnlInfo.Paint += PaintBottomBorderStatic;

            var tblInfo = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 3,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            for (int r = 0; r < 3; r++) tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f));

            AddInfoRow(tblInfo, 0, "Customer:",      inv.CustomerName,                 "Order No.:",    inv.OrderID);
            AddInfoRow(tblInfo, 1, "Total Amount:",  $"HK$ {inv.TotalAmount:N2}",      "Paid Amount:",  $"HK$ {inv.PaidAmount:N2}");
            AddInfoRow(tblInfo, 2, "Balance:",       $"HK$ {inv.RemainingBalance:N2}", "Due Date:",     inv.DueDate.ToString("yyyy-MM-dd"));
            pnlInfo.Controls.Add(tblInfo);

            // ── Payment History label bar ───────────────────────────────────────────────
            var pnlTxnLabel = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.FromArgb(246, 249, 255), Padding = new Padding(28, 0, 0, 0) };
            pnlTxnLabel.Controls.Add(new Label
            {
                Text = "PAYMENT HISTORY",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(98, 112, 135),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            });
            pnlTxnLabel.Paint += PaintBottomBorderStatic;

            // ── Transaction History Grid (Fill) ─────────────────────────────────────────
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
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255), ForeColor = Color.FromArgb(98, 112, 135),
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold), Padding = new Padding(12, 0, 0, 0)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.White, ForeColor = Color.FromArgb(15, 31, 53),
                    SelectionBackColor = Color.FromArgb(219, 234, 254), SelectionForeColor = Color.FromArgb(15, 31, 53),
                    Padding = new Padding(12, 6, 12, 6)
                }
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
                    dgvTxn.Rows.Add(t.TransactionID, t.TransactionDate.ToString("yyyy-MM-dd"),
                                    t.TransactionType, $"HK$ {t.Amount:N2}");
            }

            // ── Input Card label bar ─────────────────────────────────────────────────
            var pnlInputLabel = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.FromArgb(240, 253, 250), Padding = new Padding(28, 0, 16, 0) };
            pnlInputLabel.Paint += PaintBottomBorderStatic;
            pnlInputLabel.Controls.Add(new Label
            {
                Text = "💳  Record New Payment",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = Color.FromArgb(1, 105, 111),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            });

            // ── Input Card body ─────────────────────────────────────────────────────
            var pnlInputBody = new Panel { Dock = DockStyle.Top, Height = 130, BackColor = Color.FromArgb(249, 254, 253), Padding = new Padding(28, 16, 28, 12) };
            pnlInputBody.Paint += PaintBottomBorderStatic;

            var lblAmount = new Label { Text = "Payment Amount *", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(98, 112, 135), AutoSize = true, Location = new Point(0, 14) };
            var nudAmount = new NumericUpDown
            {
                Font = new Font("Segoe UI", 12f),
                Minimum = 0.01m, Maximum = (decimal)Math.Max(inv.RemainingBalance, 0.01),
                DecimalPlaces = 2, Value = (decimal)Math.Min(inv.RemainingBalance, inv.RemainingBalance > 0 ? inv.RemainingBalance : 0.01),
                Location = new Point(200, 10), Size = new Size(200, 36), ThousandsSeparator = true
            };

            var lblType = new Label { Text = "Payment Type *", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(98, 112, 135), AutoSize = true, Location = new Point(430, 14) };
            var cboType = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f),
                Location = new Point(610, 10), Size = new Size(200, 36)
            };
            cboType.Items.AddRange(new object[] { "Installment", "Full", "Deposit" });
            cboType.SelectedIndex = inv.RemainingBalance <= inv.TotalAmount * 0.5 ? 1 : 0;

            var lblDate    = new Label { Text = "Transaction Date", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(98, 112, 135), AutoSize = true, Location = new Point(0, 72) };
            var lblDateVal = new Label { Text = DateTime.Today.ToString("yyyy-MM-dd"), Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.FromArgb(1, 105, 111), Location = new Point(200, 68), Size = new Size(200, 36), TextAlign = ContentAlignment.MiddleLeft };

            pnlInputBody.Controls.Add(lblAmount);
            pnlInputBody.Controls.Add(nudAmount);
            pnlInputBody.Controls.Add(lblType);
            pnlInputBody.Controls.Add(cboType);
            pnlInputBody.Controls.Add(lblDate);
            pnlInputBody.Controls.Add(lblDateVal);

            // ── Footer ─────────────────────────────────────────────────────────
            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 70, BackColor = Color.White, Padding = new Padding(0, 12, 28, 12) };
            pnlFooter.Paint += PaintTopBorderStatic;

            var btnConfirm = new Button
            {
                Text = "✔  Confirm Payment",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.White,
                BackColor = Color.FromArgb(5, 150, 105), FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Right, Width = 220, Cursor = Cursors.Hand
            };
            btnConfirm.FlatAppearance.BorderSize = 0;
            btnConfirm.FlatAppearance.MouseOverBackColor = Color.FromArgb(4, 120, 87);

            var btnCancel = new Button
            {
                Text = "Cancel", Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat, Dock = DockStyle.Right, Width = 140, Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnCancel.FlatAppearance.BorderSize         = 1;
            btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnCancel.Click += (o, ev) => dlg.Close();

            btnConfirm.Click += (o, ev) =>
            {
                double amount = (double)nudAmount.Value;
                if (amount <= 0)
                {
                    MessageBox.Show("Payment amount must be greater than zero.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    nudAmount.Focus(); return;
                }
                if (amount > inv.RemainingBalance + 0.005)
                {
                    MessageBox.Show(
                        $"Payment amount (HK$ {amount:N2}) exceeds the remaining balance (HK$ {inv.RemainingBalance:N2}).",
                        "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    nudAmount.Focus(); return;
                }
                if (cboType.SelectedIndex < 0)
                {
                    MessageBox.Show("Please select a payment type.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cboType.Focus(); return;
                }
                try
                {
                    _ctrl.RecordPayment(inv.InvoiceID, amount, cboType.SelectedItem.ToString());
                    MessageBox.Show(
                        $"Payment of HK$ {amount:N2} recorded successfully for {inv.InvoiceID}.",
                        "Payment Recorded", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to record payment:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            pnlFooter.Controls.Add(btnConfirm);
            pnlFooter.Controls.Add(btnCancel);

            // ── Assemble ──────────────────────────────────────────────────────────
            dlg.Controls.Add(dgvTxn);        // Fill
            dlg.Controls.Add(pnlTxnLabel);   // Top
            dlg.Controls.Add(pnlInputBody);  // Top
            dlg.Controls.Add(pnlInputLabel); // Top
            dlg.Controls.Add(pnlInfo);       // Top
            dlg.Controls.Add(pnlHeader);     // Top — topmost
            dlg.Controls.Add(pnlFooter);     // Bottom

            dlg.ShowDialog(this);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Helpers — mirrors ViewShipmentForm helper pattern
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private static void AddInfoRow(TableLayoutPanel tbl, int row,
            string lbl1, string val1, string lbl2, string val2)
        {
            tbl.Controls.Add(MakeLabelKey(lbl1), 0, row);
            tbl.Controls.Add(MakeLabelVal(val1), 1, row);
            tbl.Controls.Add(MakeLabelKey(lbl2), 2, row);
            tbl.Controls.Add(MakeLabelVal(val2), 3, row);
        }

        private static Label MakeLabelKey(string text) => new Label
        {
            Text = text, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(98, 112, 135),
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
        };

        private static Label MakeLabelVal(string text) => new Label
        {
            Text = text, Font = new Font("Segoe UI", 12f),
            ForeColor = Color.FromArgb(15, 31, 53),
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
        };

        private static Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(1, 105, 111),
                FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(12, 78, 84);
            return b;
        }

        private static Button MakeOutlineBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text = text, Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize         = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            return b;
        }

        private static void PaintBottomBorderStatic(object s, PaintEventArgs e)
        {
            var p = (Control)s;
            using var pen = new Pen(Color.FromArgb(221, 227, 236));
            e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
        }

        private static void PaintTopBorderStatic(object s, PaintEventArgs e)
        {
            using var pen = new Pen(Color.FromArgb(221, 227, 236));
            e.Graphics.DrawLine(pen, 0, 0, ((Control)s).Width, 0);
        }
    }
}
