using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.OrderProcessing
{
    /// <summary>
    /// Quotation — Tab 2 of Order Processing Management.
    /// Lists all quotations and allows status updates via KPI-pill filtering.
    ///
    /// MVC contract (View layer):
    ///   • Calls OrderProcessingController to obtain QuotationViewModel.
    ///   • Uses AppShell (TopNavBar + UserBar) for navigation chrome.
    ///   • Contains NO business logic and NO direct DB calls.
    ///   • Layout uses CardPanel 三層巢狀卡片結構 (參考 ViewOrderForm).
    /// </summary>
    public partial class QuotationForm : Form
    {
        private readonly OrderProcessingController _ctrl = new OrderProcessingController();
        private List<QuotationEntity> _currentQuotations = new List<QuotationEntity>();

        private static readonly Dictionary<string, (Color bg, Color fg)> StatusColors =
            new Dictionary<string, (Color, Color)>
            {
                { "Pending",   (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "Converted", (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
                { "Rejected",  (Color.FromArgb(254, 226, 226), Color.FromArgb(153,  27,  27)) },
            };

        public QuotationForm()
        {
            InitializeComponent();
            this.Load += QuotationForm_Load;
        }

        // ── Load
        private void QuotationForm_Load(object sender, EventArgs e)
        {
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;
            RefreshGrid();
        }

        // ── Core refresh (mirrors ViewOrderForm.RefreshGrid)
        private void RefreshGrid()
        {
            string keyword      = txtSearchKeyword.Text.Trim();
            string statusSelect = cboStatus.SelectedItem?.ToString();
            string statusFilter = (statusSelect == "All" || string.IsNullOrEmpty(statusSelect))
                                  ? null : statusSelect;

            var vm = _ctrl.GetQuotationVM(statusFilter, keyword);

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Order Processing  \u203A  Quotation");

            _currentQuotations = vm.Quotations;

            dgvQuotations.Rows.Clear();
            foreach (var q in _currentQuotations)
                dgvQuotations.Rows.Add(
                    q.QuotationID,
                    q.CustomerName,
                    q.ExpiryDate.ToString("yyyy-MM-dd"),
                    $"HK$ {q.TotalAmount:N2}",
                    $"HK$ {q.DepositRequired:N2}",
                    q.LeadTimeEstimated,
                    q.QuotationStatus);

            RefreshKpi();
            UpdateActionButtons();
        }

        private void ResetFilters()
        {
            txtSearchKeyword.Text   = string.Empty;
            cboStatus.SelectedIndex = 0;
            RefreshGrid();
        }

        // ── KPI bar (mirrors ViewOrderForm.RefreshKpi)
        private void RefreshKpi()
        {
            pnlKpi.Controls.Clear();

            var allQuotations = _ctrl.GetQuotationVM().Quotations;

            int total     = allQuotations.Count;
            int pending   = allQuotations.FindAll(q => q.QuotationStatus == "Pending").Count;
            int converted = allQuotations.FindAll(q => q.QuotationStatus == "Converted").Count;
            int rejected  = allQuotations.FindAll(q => q.QuotationStatus == "Rejected").Count;

            var pills = new[]
            {
                ("Total",     total.ToString(),     Color.FromArgb( 47, 111, 237), Color.FromArgb(219, 234, 254), "All"),
                ("Pending",   pending.ToString(),   Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199), "Pending"),
                ("Converted", converted.ToString(), Color.FromArgb(  6,  95,  70), Color.FromArgb(209, 250, 229), "Converted"),
                ("Rejected",  rejected.ToString(),  Color.FromArgb(153,  27,  27), Color.FromArgb(254, 226, 226), "Rejected"),
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

            const int PillW   = 290;
            const int PillH   = 60;
            const int Gap     = 8;
            const int NumColW = 80;

            foreach (var (label, count, fg, bg, filterItem) in pills)
            {
                var pill = new Panel
                {
                    BackColor = bg,
                    Size      = new Size(PillW, PillH),
                    Margin    = new Padding(0, 0, Gap, 0),
                    Cursor    = Cursors.Hand
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
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                tlp.Controls.Add(new Label
                {
                    Text      = count,
                    Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                    ForeColor = fg, BackColor = Color.Transparent,
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, AutoSize = false
                }, 0, 0);
                tlp.Controls.Add(new Label
                {
                    Text      = label,
                    Font      = new Font("Segoe UI", 12f),
                    ForeColor = fg, BackColor = Color.Transparent,
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
                }, 1, 0);

                string localFilter = filterItem;
                EventHandler click = (s, e) =>
                {
                    int idx = cboStatus.FindStringExact(localFilter);
                    if (idx >= 0) cboStatus.SelectedIndex = idx;
                    RefreshGrid();
                };
                pill.Click += click;
                tlp.Click  += click;
                foreach (Control c in tlp.Controls) c.Click += click;

                pill.Controls.Add(tlp);
                flow.Controls.Add(pill);
            }
            pnlKpi.Controls.Add(flow);
        }

        private void UpdateActionButtons()
        {
            bool sel = dgvQuotations.SelectedRows.Count > 0;
            btnViewDetail.Enabled   = sel;
            btnUpdateStatus.Enabled = sel;
            cboNewStatus.Enabled    = sel;
        }

        // ── Event handlers
        private void dgvQuotations_SelectionChanged(object sender, EventArgs e)
        {
            UpdateActionButtons();
            if (dgvQuotations.SelectedRows.Count > 0)
            {
                string current = dgvQuotations.SelectedRows[0]
                    .Cells["colStatus"].Value?.ToString();
                int idx = cboNewStatus.FindStringExact(current);
                if (idx >= 0) cboNewStatus.SelectedIndex = idx;
            }
        }

        private void dgvQuotations_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvQuotations.Columns[e.ColumnIndex].Name != "colStatus" || e.Value == null) return;
            string dbValue = e.Value.ToString();
            e.FormattingApplied = true;
            if (StatusColors.TryGetValue(dbValue, out var colors))
            {
                e.CellStyle.ForeColor            = colors.fg;
                e.CellStyle.BackColor            = colors.bg;
                e.CellStyle.SelectionForeColor   = colors.fg;
                e.CellStyle.SelectionBackColor   = colors.bg;
                e.CellStyle.Font                 = new Font("Segoe UI", 11f, FontStyle.Bold);
                e.CellStyle.Alignment            = DataGridViewContentAlignment.MiddleCenter;
            }
        }

        /// <summary>Double-click a row = open View Detail dialog.</summary>
        private void dgvQuotations_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) ShowDetailDialog();
        }

        /// <summary>View Detail button click.</summary>
        private void btnViewDetail_Click(object sender, EventArgs e) => ShowDetailDialog();

        // ────────────────────────────────────────────────────────────────────
        //  DETAIL DIALOG  (mirrors ViewOrderForm.ShowDetailDialog)
        // ────────────────────────────────────────────────────────────────────
        private void ShowDetailDialog()
        {
            if (dgvQuotations.SelectedRows.Count == 0) return;

            string qid = dgvQuotations.SelectedRows[0]
                .Cells["colQuotationID"].Value?.ToString();
            if (string.IsNullOrEmpty(qid)) return;

            var q = _ctrl.GetQuotationDetail(qid);
            if (q == null)
            {
                MessageBox.Show("Quotation not found.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // ── helpers ──────────────────────────────────────────────────────
            static Label MakeLabelKey(string text) => new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = false
            };
            static Label MakeLabelVal(string text) => new Label
            {
                Text         = text ?? "—",
                Font         = new Font("Segoe UI", 12f),
                ForeColor    = Color.FromArgb(15, 31, 53),
                Dock         = DockStyle.Fill,
                TextAlign    = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
            static Label MakeLabelValMultiLine(string text) => new Label
            {
                Text         = text ?? "—",
                Font         = new Font("Segoe UI", 12f),
                ForeColor    = Color.FromArgb(15, 31, 53),
                Dock         = DockStyle.Fill,
                TextAlign    = ContentAlignment.TopLeft,
                AutoEllipsis = false,
                AutoSize     = false
            };

            // ── Dialog shell ─────────────────────────────────────────────────
            var dlg = new Form
            {
                Text            = $"Quotation Detail — {q.QuotationID}",
                Size            = new Size(1100, 750),
                MinimumSize     = new Size(900, 600),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.FromArgb(240, 244, 249),
                Font            = new Font("Segoe UI", 13f),
                FormBorderStyle = FormBorderStyle.Sizable,
                MaximizeBox     = false
            };

            var pnlDlg = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249),
                                     Padding = new Padding(20) };

            // ── Title row ────────────────────────────────────────────────────
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 60,
                                        BackColor = Color.Transparent };
            var lblDlgTitle = new Label
            {
                Text      = $"Quotation  {q.QuotationID}",
                Font      = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            if (StatusColors.TryGetValue(q.QuotationStatus ?? "", out var sc))
            {
                var lblBadge = new Label
                {
                    Text      = q.QuotationStatus,
                    Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                    ForeColor = sc.fg,
                    BackColor = sc.bg,
                    AutoSize  = true,
                    Padding   = new Padding(10, 4, 10, 4),
                    Dock      = DockStyle.Right,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                pnlHeader.Controls.Add(lblBadge);
            }
            pnlHeader.Controls.Add(lblDlgTitle);

            // ── Info card ────────────────────────────────────────────────────
            // 5 rows × 2 columns (Key | Value) on each side, total 4 columns
            // Row 3 (index 3) is taller for Notes (multiline)
            var tblInfo = new TableLayoutPanel
            {
                Dock            = DockStyle.Top,
                Height          = 220,
                ColumnCount     = 4,
                RowCount        = 5,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(18, 12, 18, 12)
            };
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170f));  // Left Key
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,   50f));  // Left Value
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190f));  // Right Key
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,   50f));  // Right Value

            tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 17f));
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 17f));
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 17f));
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 32f)); // Notes row — taller
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 17f));

            // Left column fields
            var leftFields = new[]
            {
                ("Quotation ID",   q.QuotationID),
                ("Customer",       q.CustomerName),
                ("Sales Staff",    q.SalesStaffName),
                ("Notes",          q.Notes),
                ("Lead Time",      q.LeadTimeEstimated)
            };
            // Right column fields
            var rightFields = new[]
            {
                ("Issued Date",    q.IssuedDate.ToString("yyyy-MM-dd")),
                ("Expiry Date",    q.ExpiryDate.ToString("yyyy-MM-dd")),
                ("Total Amount",   $"HK$ {q.TotalAmount:N2}"),
                ("Deposit Req.",   $"HK$ {q.DepositRequired:N2}"),
                ("Status",         q.QuotationStatus)
            };

            for (int r = 0; r < 5; r++)
            {
                // Left side
                tblInfo.Controls.Add(MakeLabelKey(leftFields[r].Item1), 0, r);
                if (r == 3) // Notes: multiline
                    tblInfo.Controls.Add(MakeLabelValMultiLine(leftFields[r].Item2), 1, r);
                else
                    tblInfo.Controls.Add(MakeLabelVal(leftFields[r].Item2), 1, r);

                // Right side
                tblInfo.Controls.Add(MakeLabelKey(rightFields[r].Item1), 2, r);
                tblInfo.Controls.Add(MakeLabelVal(rightFields[r].Item2), 3, r);
            }

            // Wrap info in white card
            var pnlInfoCard = new Panel { Dock = DockStyle.Top, Height = 220,
                                          BackColor = Color.White };
            pnlInfoCard.Paint += (s, e) =>
            {
                var p = (Panel)s;
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            };
            pnlInfoCard.Controls.Add(tblInfo);

            // ── Items grid card ──────────────────────────────────────────────
            var dgvItems = new DataGridView
            {
                ReadOnly              = true,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible     = false,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect           = false,
                BackgroundColor       = Color.White,
                BorderStyle           = BorderStyle.None,
                GridColor             = Color.FromArgb(221, 227, 236),
                Font                  = new Font("Segoe UI", 12f),
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate           = { Height = 44 },
                Dock                  = DockStyle.Fill,
                ColumnHeadersHeight   = 44,
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255),
                    ForeColor = Color.FromArgb(98, 112, 135),
                    Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                    Padding   = new Padding(12, 0, 0, 0),
                    Alignment = DataGridViewContentAlignment.MiddleLeft
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
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItemProduct",  HeaderText = "PRODUCT",      FillWeight = 30 });
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItemQty",       HeaderText = "QTY",          FillWeight = 10 });
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItemUnit",      HeaderText = "UNIT",         FillWeight = 10 });
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItemUnitPrice", HeaderText = "UNIT PRICE",   FillWeight = 15 });
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItemDiscount",  HeaderText = "DISCOUNT %",   FillWeight = 12 });
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItemSubtotal",  HeaderText = "SUBTOTAL",     FillWeight = 15 });
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItemNote",      HeaderText = "ITEM NOTE",    FillWeight = 18 });

            if (q.Items != null)
            {
                foreach (var item in q.Items)
                    dgvItems.Rows.Add(
                        item.ProductName,
                        item.Quantity,
                        item.Unit,
                        $"HK$ {item.UnitPrice:N2}",
                        $"{item.DiscountPercent:N1}%",
                        $"HK$ {item.Subtotal:N2}",
                        item.ItemNote);
            }

            var pnlGridCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlGridCard.Paint += (s, e) =>
            {
                var p = (Panel)s;
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            };
            pnlGridCard.Controls.Add(dgvItems);

            // ── Total row ────────────────────────────────────────────────────
            var pnlTotalRow = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 56,
                BackColor = Color.FromArgb(246, 249, 255)
            };
            var tblTotal = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                RowCount    = 1,
                BackColor   = Color.Transparent,
                Padding     = new Padding(16, 0, 16, 0)
            };
            tblTotal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblTotal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblTotal.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblTotal.Controls.Add(new Label
            {
                Text      = $"Subtotal:   HK$ {q.TotalAmount:N2}",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);
            tblTotal.Controls.Add(new Label
            {
                Text      = $"Total Amount:   HK$ {q.TotalAmount:N2}",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            }, 1, 0);
            pnlTotalRow.Controls.Add(tblTotal);

            // ── Section label ────────────────────────────────────────────────
            var pnlItemsLabel = new Panel { Dock = DockStyle.Top, Height = 44,
                                            BackColor = Color.Transparent };
            var lblItems = new Label
            {
                Text      = "Quotation Items",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlItemsLabel.Controls.Add(lblItems);

            // ── Close button ─────────────────────────────────────────────────
            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 64,
                                        BackColor = Color.Transparent };
            var btnClose = new Button
            {
                Text      = "Close",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(160, 48),
                Cursor    = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            btnClose.FlatAppearance.BorderSize  = 1;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnClose.Click += (s, e) => dlg.Close();
            btnClose.Location = new Point(
                pnlFooter.Width - btnClose.Width - 20,
                (pnlFooter.Height - btnClose.Height) / 2);
            btnClose.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            pnlFooter.Controls.Add(btnClose);

            // ── Assemble dialog ──────────────────────────────────────────────
            pnlGridCard.Controls.Add(pnlTotalRow);   // Bottom of grid card
            pnlDlg.Controls.Add(pnlFooter);          // Bottom
            pnlDlg.Controls.Add(pnlGridCard);        // Fill
            pnlDlg.Controls.Add(pnlItemsLabel);      // Top (after info card)
            pnlDlg.Controls.Add(pnlInfoCard);        // Top
            pnlDlg.Controls.Add(pnlHeader);          // Top
            dlg.Controls.Add(pnlDlg);
            dlg.ShowDialog(this);
        }

        private void btnUpdateStatus_Click(object sender, EventArgs e)
        {
            if (dgvQuotations.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a quotation first.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string quotationId = dgvQuotations.SelectedRows[0]
                .Cells["colQuotationID"].Value?.ToString();
            string newStatus = cboNewStatus.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(newStatus))
            {
                MessageBox.Show("Please select a new status.",
                    "No Status", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool ok = _ctrl.UpdateQuotationStatus(quotationId, newStatus);
            if (ok)
            {
                MessageBox.Show($"Quotation {quotationId} updated to '{newStatus}'.",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshGrid();
            }
            else
            {
                MessageBox.Show("Failed to update quotation status. Please try again.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Helpers
        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
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
