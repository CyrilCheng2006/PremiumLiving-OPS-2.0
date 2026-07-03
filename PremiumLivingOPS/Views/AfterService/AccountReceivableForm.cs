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

        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        { SessionManager.Clear(); Application.Restart(); }

        // ── Grid refresh
        private void RefreshGrid()
        {
            string statusSel = cboStatus.SelectedItem?.ToString();
            bool filterOverdue = string.Equals(statusSel, "Overdue", StringComparison.OrdinalIgnoreCase);
            string statusFilter = (string.IsNullOrEmpty(statusSel) || statusSel == "All" || filterOverdue)
                ? null
                : statusSel;

            string keyword = txtKeyword.Text.Trim();

            var arVm = _ctrl.GetAccountReceivableVM(statusFilter, string.IsNullOrEmpty(keyword) ? null : keyword);
            _shell.SetUser(arVm.UserBar.DisplayName, arVm.UserBar.Department);
            _shell.SetVisibleMenus(arVm.AllowedMenus);
            _shell.SetBreadcrumb("After-Service  ›  Account Receivable");

            _invoices = _ctrl.GetInvoiceListVM(string.IsNullOrEmpty(keyword) ? null : keyword).Invoices;

            if (filterOverdue)
                _invoices = _invoices.FindAll(i => i.IsOverdue);
            else if (!string.IsNullOrEmpty(statusFilter))
                _invoices = _invoices.FindAll(i =>
                    string.Equals(i.PaymentStatus, statusFilter, StringComparison.OrdinalIgnoreCase));

            dgvAR.Rows.Clear();
            foreach (var inv in _invoices)
                dgvAR.Rows.Add(
                    inv.InvoiceID,
                    inv.OrderID,
                    inv.InvoiceDate.ToString("yyyy-MM-dd"),
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

        private void dgvAR_SelectionChanged(object sender, EventArgs e)
        {
            bool hasRow = dgvAR.SelectedRows.Count > 0 && dgvAR.SelectedRows[0].Index >= 0;
            btnRecord.Enabled     = hasRow;
            btnViewDetail.Enabled = hasRow;
        }

        // ── KPI Pills
        private void RefreshKpi()
        {
            pnlKpi.Controls.Clear();

            var all = _ctrl.GetAccountReceivableVM().Items;

            int totalCount   = all.Count;
            int overdueCount = 0;
            int partialCount = 0;
            int fullCount    = 0;

            foreach (var i in all)
            {
                if (i.IsOverdue)                 overdueCount++;
                if (i.PaymentStatus == "Partial") partialCount++;
                if (i.PaymentStatus == "Full")    fullCount++;
            }

            var pills = new[]
            {
                ("Total Invoices", totalCount.ToString(),   Color.FromArgb( 19,  35,  61), Color.FromArgb(219, 234, 254)),
                ("Partial",        partialCount.ToString(), Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199)),
                ("Full",           fullCount.ToString(),    Color.FromArgb( 22, 101,  52), Color.FromArgb(220, 252, 231)),
                ("Overdue",        overdueCount.ToString(), Color.FromArgb(185,  28,  28), Color.FromArgb(254, 226, 226)),
            };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent,
                Padding = new Padding(0), AutoScroll = false,
            };

            const int PillW   = 340;
            const int PillH   =  60;
            const int Gap     =   8;
            const int NumColW =  90;

            foreach (var (label, value, fg, bg) in pills)
            {
                var pill = new Panel
                {
                    BackColor = bg, Size = new Size(PillW, PillH),
                    Margin = new Padding(0, 0, Gap, 0), Cursor = Cursors.Hand
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
                    Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                    BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding = new Padding(10, 0, 8, 0),
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
                    Text = label, Font = new Font("Segoe UI", 11f),
                    ForeColor = fg, BackColor = Color.Transparent,
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
                }, 1, 0);
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

        // ── Open View Detail (KPI bar button + double-click)
        private void OpenViewDetail()
        {
            if (dgvAR.SelectedRows.Count == 0) return;
            int idx = dgvAR.SelectedRows[0].Index;
            if (idx < 0 || idx >= _invoices.Count) return;
            ShowViewDetailDialog(_invoices[idx]);
        }

        // ── Open Record Payment
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

        // ═══════════════════════════════════════════════════════════════════════
        // VIEW DETAIL DIALOG — Transaction History
        // Rendering pattern: ViewShipment → ShowDetailDialog
        // ═══════════════════════════════════════════════════════════════════════
        private void ShowViewDetailDialog(InvoiceDetailEntity inv)
        {
            using var dlg = new Form
            {
                Text            = $"Invoice Detail  —  {inv.InvoiceID}",
                Size            = new Size(2500, 1050),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 13f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false
            };

            // ── HEADER (dark navy)
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            var tblHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(24, 0, 24, 0)
            };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 420f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblHeader.Controls.Add(new Label
            {
                Text      = $"Invoice Detail  —  {inv.CustomerName}",
                Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);
            Color badgeBg = inv.IsOverdue
                ? Color.FromArgb(185,  28,  28)
                : inv.PaymentStatus == "Full"
                    ? Color.FromArgb( 22, 101,  52)
                    : Color.FromArgb(146,  64,  14);
            tblHeader.Controls.Add(new Label
            {
                Text      = inv.IsOverdue ? "Overdue" : inv.PaymentStatus,
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = badgeBg,
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                AutoSize  = false, Margin = new Padding(0, 14, 0, 14)
            }, 1, 0);
            pnlHeader.Controls.Add(tblHeader);

            // ── INFO PANEL — 3-row × 4-col key/value grid
            var pnlInfo = new Panel
            {
                Dock = DockStyle.Top, Height = 190,
                BackColor = Color.White, Padding = new Padding(28, 16, 28, 8)
            };
            pnlInfo.Paint += (sender, e) =>
            {
                using var pen = new Pen(Palette.BorderColor, 1);
                e.Graphics.DrawLine(pen, 28, ((Panel)sender).Height - 1,
                                    ((Panel)sender).Width - 28, ((Panel)sender).Height - 1);
            };
            var tblInfo = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 3,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36f));
            for (int r = 0; r < 3; r++) tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f));
            AddInfoRow(tblInfo, 0, "Invoice ID:",  inv.InvoiceID,                        "Order No.:",    inv.OrderID);
            AddInfoRow(tblInfo, 1, "Customer:",    inv.CustomerName,                     "Invoice Date:", inv.InvoiceDate.ToString("yyyy-MM-dd"));
            AddInfoRow(tblInfo, 2, "Due Date:",    inv.DueDate.ToString("yyyy-MM-dd"),   "Balance:",      $"HK$ {inv.RemainingBalance:N2}");
            pnlInfo.Controls.Add(tblInfo);

            // ── AMOUNT SUMMARY STRIP — 3 pills (Total / Paid / Balance)
            // Pill width = 380 × 1.8 = 684px; label col = 140 × 1.8 = 252px
            var pnlAmounts = new Panel
            {
                Dock = DockStyle.Top, Height = 82,
                BackColor = Color.FromArgb(246, 249, 255),
                Padding = new Padding(28, 10, 28, 10)
            };
            pnlAmounts.Paint += (sender, e) =>
            {
                using var pen = new Pen(Palette.BorderColor, 1);
                var p2 = (Panel)sender;
                e.Graphics.DrawLine(pen, 0, p2.Height - 1, p2.Width, p2.Height - 1);
            };
            var amtFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent, AutoScroll = false
            };
            var amtPills = new[]
            {
                ("Total Amount", $"HK$ {inv.TotalAmount:N2}",      Color.FromArgb(19, 35, 61),  Color.FromArgb(219, 234, 254)),
                ("Paid Amount",  $"HK$ {inv.PaidAmount:N2}",       Color.FromArgb(22, 101, 52), Color.FromArgb(220, 252, 231)),
                ("Balance",      $"HK$ {inv.RemainingBalance:N2}", badgeBg,                     Color.FromArgb(254, 226, 226)),
            };
            foreach (var (alabel, aval, afg, abg) in amtPills)
            {
                // ★ 1.8x wider: 380 → 684
                var pill = new Panel { BackColor = abg, Size = new Size(684, 58), Margin = new Padding(0, 0, 10, 0) };
                pill.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path  = RoundedRect(((Panel)s).ClientRectangle, 8);
                    using var brush = new SolidBrush(((Panel)s).BackColor);
                    e.Graphics.FillPath(brush, path);
                };
                var atl = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                    BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding = new Padding(12, 0, 8, 0)
                };
                // ★ 1.8x wider label col: 140 → 252
                atl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 252f));
                atl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
                atl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                atl.Controls.Add(new Label { Text = alabel, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = afg, BackColor = Color.Transparent, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,   AutoSize = false }, 0, 0);
                atl.Controls.Add(new Label { Text = aval,   Font = new Font("Segoe UI", 13f, FontStyle.Bold), ForeColor = afg, BackColor = Color.Transparent, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, AutoSize = false }, 1, 0);
                pill.Controls.Add(atl);
                amtFlow.Controls.Add(pill);
            }
            pnlAmounts.Controls.Add(amtFlow);

            // ── SECTION LABEL
            var pnlLineLabel = new Panel
            {
                Dock = DockStyle.Top, Height = 40,
                BackColor = Color.FromArgb(246, 249, 255),
                Padding = new Padding(28, 0, 0, 0)
            };
            pnlLineLabel.Controls.Add(new Label
            {
                Text = "TRANSACTION HISTORY",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Palette.TextMuted,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            });
            pnlLineLabel.Paint += (s, e) =>
            {
                var p2 = (Panel)s;
                using var pen = new Pen(Palette.BorderColor, 1);
                e.Graphics.DrawLine(pen, 0, p2.Height - 1, p2.Width, p2.Height - 1);
            };

            // ── TRANSACTION DataGridView
            var dgvTxn = new DataGridView
            {
                ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                GridColor = Palette.BorderColor, Font = new Font("Segoe UI", 12f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate = { Height = 46 }, Dock = DockStyle.Fill,
                ColumnHeadersHeight = 42, EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255), ForeColor = Palette.TextMuted,
                    Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                    Padding = new Padding(12, 0, 0, 0)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.White, ForeColor = Palette.TextMain,
                    SelectionBackColor = Color.FromArgb(219, 234, 254),
                    SelectionForeColor = Palette.TextMain,
                    Padding = new Padding(12, 6, 12, 6)
                }
            };
            dgvTxn.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTxnID",   HeaderText = "TXN ID",       FillWeight = 28 });
            dgvTxn.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTxnDate", HeaderText = "DATE",         FillWeight = 20 });
            dgvTxn.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTxnType", HeaderText = "TYPE",         FillWeight = 24 });
            dgvTxn.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTxnAmt",  HeaderText = "AMOUNT (HK$)", FillWeight = 28 });

            if (inv.Transactions == null || inv.Transactions.Count == 0)
            {
                dgvTxn.Rows.Add("—", "—", "No transactions recorded", "—");
            }
            else
            {
                foreach (var t in inv.Transactions)
                    dgvTxn.Rows.Add(
                        t.TransactionID,
                        t.TransactionDate.ToString("yyyy-MM-dd"),
                        t.TransactionType,
                        $"HK$ {t.Amount:N2}");
            }

            var txnTypeColors = new Dictionary<string, (Color bg, Color fg)>
            {
                { "Installment", (Color.FromArgb(219, 234, 254), Color.FromArgb( 29,  78, 216)) },
                { "Full",        (Color.FromArgb(220, 252, 231), Color.FromArgb( 22, 101,  52)) },
                { "Deposit",     (Color.FromArgb(237, 233, 254), Color.FromArgb( 91,  33, 182)) },
            };
            dgvTxn.CellFormatting += (sender, e) =>
            {
                if (e.RowIndex < 0 || e.Value == null) return;
                if (dgvTxn.Columns[e.ColumnIndex].Name != "colTxnType") return;
                if (!txnTypeColors.TryGetValue(e.Value.ToString(), out var sc)) return;
                e.CellStyle.BackColor          = sc.bg;
                e.CellStyle.ForeColor          = sc.fg;
                e.CellStyle.SelectionBackColor = sc.bg;
                e.CellStyle.SelectionForeColor = sc.fg;
                e.CellStyle.Font               = new Font("Segoe UI", 11f, FontStyle.Bold);
                e.CellStyle.Alignment          = DataGridViewContentAlignment.MiddleCenter;
                e.FormattingApplied            = true;
            };

            // ── TOTALS ROW
            var pnlTotals = new Panel { Dock = DockStyle.Bottom, Height = 62, BackColor = Color.White };
            pnlTotals.Paint += (s, e) =>
            {
                using var pen = new Pen(Palette.BorderColor, 1);
                e.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0);
            };
            double txnSum = 0;
            if (inv.Transactions != null)
                foreach (var t in inv.Transactions) txnSum += t.Amount;

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
                Text      = $"Transactions:   {inv.Transactions?.Count ?? 0}",
                Dock      = DockStyle.Fill, AutoSize = false,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Palette.TextMain, TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(28, 0, 0, 0)
            }, 0, 0);
            tblTotals.Controls.Add(new Label
            {
                Text      = $"Total Paid:   HK$ {txnSum:N2}",
                Dock      = DockStyle.Fill, AutoSize = false,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 101, 52),
                TextAlign = ContentAlignment.MiddleRight,
                Padding   = new Padding(0, 0, 28, 0)
            }, 1, 0);
            pnlTotals.Controls.Add(tblTotals);

            // ── FOOTER — Close button only (read-only view)
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom, Height = 86,
                BackColor = Color.White, Padding = new Padding(28, 14, 28, 14)
            };
            pnlFooter.Paint += (s, e) =>
            {
                using var pen = new Pen(Palette.BorderColor, 1);
                e.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0);
            };
            var btnClose = new Button
            {
                Text = "Close",
                Font = new Font("Segoe UI", 12f),
                ForeColor = Palette.TextMain, BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(160, 56), Cursor = Cursors.Hand,
                Anchor    = AnchorStyles.Right | AnchorStyles.Top,
                Location  = new Point(2500 - 28 - 160 - 16, 14)
            };
            btnClose.FlatAppearance.BorderColor        = Palette.BorderColor;
            btnClose.FlatAppearance.BorderSize         = 1;
            btnClose.FlatAppearance.MouseOverBackColor = Palette.BgPage;
            btnClose.Click += (_, __) => dlg.Close();
            pnlFooter.Controls.Add(btnClose);

            // ── ASSEMBLE — Dock rule: Fill first, Bottom next, Top in reverse
            dlg.Controls.Add(dgvTxn);       // Fill
            dlg.Controls.Add(pnlFooter);    // Bottom
            dlg.Controls.Add(pnlTotals);    // Bottom
            dlg.Controls.Add(pnlLineLabel); // Top
            dlg.Controls.Add(pnlAmounts);   // Top
            dlg.Controls.Add(pnlInfo);      // Top
            dlg.Controls.Add(pnlHeader);    // Top
            dlg.ShowDialog(this);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // RECORD PAYMENT DIALOG  (unchanged)
        // ═══════════════════════════════════════════════════════════════════════
        private void ShowRecordPaymentDialog(InvoiceDetailEntity inv)
        {
            using var dlg = new Form
            {
                Text            = $"Record Payment  —  {inv.InvoiceID}",
                Size            = new Size(1800, 1400),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.FromArgb(240, 244, 249),
                Font            = new Font("Segoe UI", 13f),
                FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false
            };

            // ══ HEADER
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Color.FromArgb(1, 105, 111) };
            var tblHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(28, 0, 0, 0)
            };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 640f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblHeader.Controls.Add(new Label
            {
                Text = $"Record Payment  —  {inv.InvoiceID}",
                Font = new Font("Segoe UI", 17f, FontStyle.Bold), ForeColor = Color.White,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);
            Color rpBadgeBg = inv.IsOverdue ? Color.FromArgb(185, 28, 28) : Color.FromArgb(146, 64, 14);
            tblHeader.Controls.Add(new Label
            {
                Text = $"Balance: HK$ {inv.RemainingBalance:N2}",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold), ForeColor = Color.White,
                BackColor = rpBadgeBg, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter, AutoSize = false,
                Margin = new Padding(0, 8, 0, 8)
            }, 1, 0);
            pnlHeader.Controls.Add(tblHeader);

            // ══ CARD: Invoice Info
            var (infoOuter, infoInner) = CardPanel.Create(outerHeight: 260);
            var tblInfo = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 5,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(24, 16, 24, 16)
            };
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36f));
            for (int r = 0; r < 5; r++) tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));
            AddInfoRow(tblInfo, 0, "Customer:",      inv.CustomerName,                       "Order No.:",   inv.OrderID);
            AddInfoRow(tblInfo, 1, "Invoice Date:",  inv.InvoiceDate.ToString("yyyy-MM-dd"), "Due Date:",    inv.DueDate.ToString("yyyy-MM-dd"));
            AddInfoRow(tblInfo, 2, "Total Amount:",  $"HK$ {inv.TotalAmount:N2}",            "Paid Amount:", $"HK$ {inv.PaidAmount:N2}");
            AddInfoRow(tblInfo, 3, "Deposit Paid:",  $"HK$ {inv.DepositAmount:N2}",          "Balance:",     $"HK$ {inv.RemainingBalance:N2}");
            AddInfoRow(tblInfo, 4, "Status:",        inv.IsOverdue ? "Overdue" : inv.PaymentStatus, "",      "");
            infoInner.Controls.Add(tblInfo);

            // ══ CARD: Record New Payment
            var (inputOuter, inputInner) = CardPanel.Create(outerHeight: 370);
            var pnlInputTitle = new Panel
            {
                Dock = DockStyle.Top, Height = 52,
                BackColor = Color.FromArgb(240, 253, 250), Padding = new Padding(24, 0, 0, 0)
            };
            pnlInputTitle.Paint += PaintBottomBorder;
            pnlInputTitle.Controls.Add(new Label
            {
                Text = "💳  Record New Payment",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.FromArgb(1, 105, 111),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            });

            var tblInput = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(24, 16, 24, 16)
            };
            tblInput.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 700f));
            tblInput.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblInput.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f));
            tblInput.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f));
            tblInput.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f));

            var nudAmount = new NumericUpDown
            {
                Font = new Font("Segoe UI", 13f),
                Minimum = 0.01m, Maximum = (decimal)Math.Max(inv.RemainingBalance, 0.01),
                DecimalPlaces = 2,
                Value = (decimal)(inv.RemainingBalance > 0 ? inv.RemainingBalance : 0.01),
                ThousandsSeparator = true, Dock = DockStyle.Fill
            };
            tblInput.Controls.Add(MakeInputLabel("Payment Amount (HK$) *"), 0, 0);
            tblInput.Controls.Add(nudAmount,                                 1, 0);

            var cboType = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 13f), Dock = DockStyle.Fill
            };
            cboType.Items.AddRange(new object[] { "Installment", "Full", "Deposit" });
            cboType.SelectedIndex = inv.RemainingBalance <= inv.TotalAmount * 0.5 ? 1 : 0;
            tblInput.Controls.Add(MakeInputLabel("Payment Type *"), 0, 1);
            tblInput.Controls.Add(cboType,                          1, 1);

            var lblDate = new Label
            {
                Text      = DateTime.Today.ToString("yyyy-MM-dd"),
                Font      = new Font("Segoe UI", 13f),
                ForeColor = Color.FromArgb(60, 60, 60),
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            };
            tblInput.Controls.Add(MakeInputLabel("Payment Date"), 0, 2);
            tblInput.Controls.Add(lblDate,                        1, 2);

            inputInner.Controls.Add(tblInput);
            inputInner.Controls.Add(pnlInputTitle);

            // ══ CARD: Transaction History
            var (histOuter, histInner) = CardPanel.CreateFill();

            var pnlHistTitle = new Panel
            {
                Dock = DockStyle.Top, Height = 52,
                BackColor = Color.FromArgb(246, 249, 255), Padding = new Padding(24, 0, 0, 0)
            };
            pnlHistTitle.Paint += PaintBottomBorder;
            pnlHistTitle.Controls.Add(new Label
            {
                Text = "📤  Transaction History",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(19, 35, 61),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            });

            var dgvTxnRP = new DataGridView
            {
                ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                GridColor = Palette.BorderColor, Font = new Font("Segoe UI", 12f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate = { Height = 44 }, Dock = DockStyle.Fill,
                ColumnHeadersHeight = 42, EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255), ForeColor = Palette.TextMuted,
                    Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                    Padding = new Padding(10, 0, 0, 0), Alignment = DataGridViewContentAlignment.MiddleLeft
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.White, ForeColor = Palette.TextMain,
                    SelectionBackColor = Color.FromArgb(219, 234, 254), SelectionForeColor = Palette.TextMain,
                    Padding = new Padding(10, 4, 10, 4)
                }
            };
            dgvTxnRP.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTxnID",   HeaderText = "TXN ID",  FillWeight = 30 });
            dgvTxnRP.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTxnDate", HeaderText = "DATE",    FillWeight = 20 });
            dgvTxnRP.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTxnType", HeaderText = "TYPE",    FillWeight = 20 });
            dgvTxnRP.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTxnAmt",  HeaderText = "AMOUNT",  FillWeight = 30 });

            foreach (var t in inv.Transactions)
                dgvTxnRP.Rows.Add(
                    t.TransactionID,
                    t.TransactionDate.ToString("yyyy-MM-dd"),
                    t.TransactionType,
                    $"HK$ {t.Amount:N2}");

            histInner.Controls.Add(dgvTxnRP);
            histInner.Controls.Add(pnlHistTitle);

            // ══ FOOTER — Confirm / Cancel
            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 80, BackColor = Color.White };
            pnlFooter.Paint += PaintTopBorder;

            var btnConfirm = new Button
            {
                Text = "✔  Confirm Payment",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(1, 105, 111),
                FlatStyle = FlatStyle.Flat, Width = 240, Height = 52, Cursor = Cursors.Hand
            };
            btnConfirm.FlatAppearance.BorderSize = 0;
            btnConfirm.FlatAppearance.MouseOverBackColor = Color.FromArgb(12, 78, 84);

            var btnCancel = new Button
            {
                Text = "Cancel",
                Font = new Font("Segoe UI", 13f),
                ForeColor = Palette.TextMain, BackColor = Color.White,
                FlatStyle = FlatStyle.Flat, Width = 160, Height = 52, Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Palette.BorderColor;
            btnCancel.FlatAppearance.BorderSize = 1;
            btnCancel.Click += (s, e) => dlg.Close();

            pnlFooter.Controls.Add(btnConfirm);
            pnlFooter.Controls.Add(btnCancel);
            pnlFooter.Resize += (s, e) =>
            {
                btnConfirm.Location = new Point(pnlFooter.Width - 420, (pnlFooter.Height - 52) / 2);
                btnCancel.Location  = new Point(pnlFooter.Width - 180, (pnlFooter.Height - 52) / 2);
            };

            btnConfirm.Click += (s, e) =>
            {
                double amount = (double)nudAmount.Value;
                string type   = cboType.SelectedItem?.ToString() ?? "Installment";
                bool ok = _ctrl.RecordPayment(inv.InvoiceID, amount, type);
                if (ok)
                {
                    MessageBox.Show(
                        $"Payment of HK$ {amount:N2} recorded successfully.",
                        "Payment Recorded", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.Close();
                }
                else
                {
                    MessageBox.Show("Failed to record payment. Please try again.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            // Assemble dialog — Fill first, then Top, then Bottom
            dlg.Controls.Add(histOuter);    // Fill
            dlg.Controls.Add(inputOuter);   // Top
            dlg.Controls.Add(infoOuter);    // Top
            dlg.Controls.Add(pnlHeader);    // Top
            dlg.Controls.Add(pnlFooter);    // Bottom
            dlg.ShowDialog(this);
        }

        // ── Helpers
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

        private static void AddInfoRow(TableLayoutPanel tbl, int row,
            string lbl1, string val1, string lbl2, string val2)
        {
            tbl.Controls.Add(new Label { Text = lbl1, Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = Palette.TextMuted, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,  AutoSize = false }, 0, row);
            tbl.Controls.Add(new Label { Text = val1, Font = new Font("Segoe UI", 12f),                 ForeColor = Palette.TextMain,  Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,  AutoSize = false }, 1, row);
            tbl.Controls.Add(new Label { Text = lbl2, Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = Palette.TextMuted, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,  AutoSize = false }, 2, row);
            tbl.Controls.Add(new Label { Text = val2, Font = new Font("Segoe UI", 12f),                 ForeColor = Palette.TextMain,  Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,  AutoSize = false }, 3, row);
        }

        private static Label MakeInputLabel(string text) =>
            new Label
            {
                Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Palette.TextMuted, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            };

        private static void PaintBottomBorder(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            var p = (Panel)sender;
            using var pen = new Pen(Palette.BorderColor, 1);
            e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
        }

        private static void PaintTopBorder(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            using var pen = new Pen(Palette.BorderColor, 1);
            e.Graphics.DrawLine(pen, 0, 0, ((Panel)sender).Width, 0);
        }
    }
}
