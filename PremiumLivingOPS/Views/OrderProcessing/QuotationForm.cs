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
    ///   • Layout uses CardPanel 三層巢層卡片結構 (參考 ViewOrderForm).
    ///
    /// Modify Quotation rules:
    ///   • A Quotation that is already linked to an Order (QuotationStatus == "Converted"
    ///     or controller reports IsLinkedToOrder == true) is NOT editable — the Modify
    ///     button is disabled with a tooltip explaining the lock.
    ///   • Inside ModifyQuotationDialog only Quotation Items (Add / Delete) are editable;
    ///     all header fields (Customer, ExpiryDate, TotalAmount, etc.) are read-only.
    ///   • Footer buttons are 210 × 60.
    ///
    /// Designer note:
    ///   The KPI bar "Modify" button is declared as btnAddFrom in the Designer
    ///   (QuotationForm.Designer.cs). Its Click event is wired to btnAddFrom_Click
    ///   which opens ModifyQuotationDialog. There is no separate btnModify field.
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

        // ── Core refresh
        private void RefreshGrid()
        {
            string keyword      = txtSearchKeyword.Text.Trim();
            string statusSelect = cboStatus.SelectedItem?.ToString();
            string statusFilter = (statusSelect == "All" || string.IsNullOrEmpty(statusSelect))
                                  ? null : statusSelect;

            var vm = _ctrl.GetQuotationListVM(statusFilter, keyword);

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Order Processing  ›  Quotation");

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

        // ── KPI bar
        private void RefreshKpi()
        {
            pnlKpi.Controls.Clear();

            var allQuotations = _ctrl.GetQuotationListVM().Quotations;

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
            btnAddFrom.Enabled      = sel;   // Designer field — this is the "Modify" button
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

        private void dgvQuotations_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) OpenDetailDialog();
        }

        private void btnViewDetail_Click(object sender, EventArgs e) => OpenDetailDialog();

        // ──────────────────────────────────────────────────────────────────
        //  MODIFY QUOTATION  (btnAddFrom in Designer — text: "✎ Modify")
        //
        //  Rules:
        //    1. Quotation linked to an Order (status == "Converted" or
        //       IsQuotationLinkedToOrder returns true) → blocked, show warning.
        //    2. Inside ModifyQuotationDialog only Items may be added / deleted;
        //       all header fields are read-only.
        //    3. Footer action buttons are 210 × 60.
        // ──────────────────────────────────────────────────────────────────
        private void btnAddFrom_Click(object sender, EventArgs e)
        {
            string qid = SelectedQuotationId();
            if (qid == null) return;

            var q = _ctrl.GetQuotationDetail(qid);
            if (q == null)
            {
                MessageBox.Show("Quotation not found.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Guard: already linked to an Order → cannot modify
            bool linkedToOrder = q.QuotationStatus == "Converted"
                                 || _ctrl.IsQuotationLinkedToOrder(qid);
            if (linkedToOrder)
            {
                MessageBox.Show(
                    $"Quotation {qid} has already been linked to an Order and cannot be modified.",
                    "Modification Not Allowed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            using var dlg = new ModifyQuotationDialog(q, _ctrl);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                MessageBox.Show(
                    $"Quotation {qid} has been updated successfully.",
                    "Saved",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                RefreshGrid();
            }
        }

        private string SelectedQuotationId()
        {
            if (dgvQuotations.SelectedRows.Count == 0) return null;
            return dgvQuotations.SelectedRows[0].Cells["colQuotationID"].Value?.ToString();
        }

        private void OpenDetailDialog()
        {
            string qid = SelectedQuotationId();
            if (qid == null) return;

            var q = _ctrl.GetQuotationDetail(qid);
            if (q == null)
            {
                MessageBox.Show("Quotation not found.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ShowDetailDialog(q, q.Items);
        }

        // ──────────────────────────────────────────────────────────────────
        //  VIEW DETAIL DIALOG  (read-only snapshot of one Quotation record)
        // ──────────────────────────────────────────────────────────────────
        private void ShowDetailDialog(QuotationEntity q, List<QuotationItemEntity> items)
        {
            bool hasTnC = !string.IsNullOrWhiteSpace(q.TermsandCondition);

            using var dlg = new Form
            {
                Text            = $"Quotation Detail — {q.QuotationID}",
                Size            = new Size(2500, 1100),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 13f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false
            };

            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            var tblHeader = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor       = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(24, 0, 24, 0)
            };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblHeader.Controls.Add(new Label
            {
                Text      = $"Quotation Details  —  {q.QuotationID}",
                Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);
            StatusColors.TryGetValue(q.QuotationStatus ?? "", out var sc);
            tblHeader.Controls.Add(new Label
            {
                Text      = q.QuotationStatus ?? "Unknown",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = sc.fg != default ? sc.fg : Color.White,
                BackColor = sc.bg != default ? sc.bg : Color.FromArgb(80, 80, 80),
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                AutoSize  = false, Padding = new Padding(8, 4, 8, 4)
            }, 1, 0);
            pnlHeader.Controls.Add(tblHeader);

            var pnlInfo = new Panel
            {
                Dock      = DockStyle.Top, Height = 280,
                Padding   = new Padding(28, 18, 28, 8), BackColor = Color.White
            };
            pnlInfo.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawLine(pen, 28, ((Panel)s).Height - 1, ((Panel)s).Width - 28, ((Panel)s).Height - 1);
            };

            var tblInfo = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill, ColumnCount = 4, RowCount = 4,
                BackColor       = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            for (int r = 0; r < 4; r++)
                tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));

            var leftFields = new[]
            {
                ("Quotation ID",  q.QuotationID),
                ("Customer",      q.CustomerName),
                ("Lead Time",     q.LeadTimeEstimated ?? "—"),
                ("Sales Staff",   q.SalesStaffName    ?? "—"),
            };
            for (int i = 0; i < leftFields.Length; i++)
            {
                tblInfo.Controls.Add(MakeLabelKey(leftFields[i].Item1), 0, i);
                tblInfo.Controls.Add(MakeLabelVal(leftFields[i].Item2), 1, i);
            }
            var rightFields = new (string, string)[]
            {
                ("Expiry Date",      q.ExpiryDate.ToString("yyyy-MM-dd")),
                ("Total Amount",     $"HK$ {q.TotalAmount:N2}"),
                ("Deposit Required", $"HK$ {q.DepositRequired:N2}"),
                ("Status",           q.QuotationStatus ?? "—"),
            };
            for (int i = 0; i < rightFields.Length; i++)
            {
                tblInfo.Controls.Add(MakeLabelKey(rightFields[i].Item1), 2, i);
                tblInfo.Controls.Add(MakeLabelVal(rightFields[i].Item2), 3, i);
            }
            pnlInfo.Controls.Add(tblInfo);

            Panel pnlTnC = null;
            if (hasTnC)
            {
                pnlTnC = new Panel
                {
                    Dock      = DockStyle.Top, Height = 60,
                    Padding   = new Padding(28, 0, 28, 0), BackColor = Color.FromArgb(255, 251, 235)
                };
                pnlTnC.Paint += PaintBottomBorderStatic;
                var tblTnC = new TableLayoutPanel
                {
                    Dock            = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                    BackColor       = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
                };
                tblTnC.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
                tblTnC.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 85f));
                tblTnC.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                tblTnC.Controls.Add(MakeLabelKey("Terms & Conditions"), 0, 0);
                tblTnC.Controls.Add(MakeLabelVal(q.TermsandCondition),  1, 0);
                pnlTnC.Controls.Add(tblTnC);
            }

            var pnlLineLabel = new Panel
            {
                Dock      = DockStyle.Top, Height = 40,
                BackColor = Color.FromArgb(246, 249, 255),
                Padding   = new Padding(28, 0, 0, 0)
            };
            pnlLineLabel.Controls.Add(new Label
            {
                Text      = "QUOTATION ITEMS",
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            });
            pnlLineLabel.Paint += PaintBottomBorderStatic;

            var dgv = new DataGridView
            {
                ReadOnly              = true,
                AllowUserToAddRows    = false,
                RowHeadersVisible     = false,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor       = Color.White,
                BorderStyle           = BorderStyle.None,
                GridColor             = Color.FromArgb(221, 227, 236),
                Font                  = new Font("Segoe UI", 12f),
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate           = { Height = 44 },
                Dock                  = DockStyle.Fill,
                ColumnHeadersHeight   = 40,
                EnableHeadersVisualStyles = false,
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
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cItemID",    HeaderText = "ITEM ID",    FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cProduct",   HeaderText = "PRODUCT",    FillWeight = 30 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cQty",       HeaderText = "QTY",        FillWeight = 10 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cUnit",      HeaderText = "UNIT",       FillWeight = 10 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cUnitPrice", HeaderText = "UNIT PRICE", FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cDiscount",  HeaderText = "DISCOUNT %", FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cSubtotal",  HeaderText = "SUBTOTAL",   FillWeight = 15 });

            if (items != null)
                foreach (var item in items)
                    dgv.Rows.Add(
                        item.ItemID,
                        item.ProductName,
                        item.Quantity,
                        item.Unit,
                        $"HK$ {item.UnitPrice:N2}",
                        $"{item.DiscountPercent:N1}%",
                        $"HK$ {item.Subtotal:N2}");

            var pnlTotalRow = new Panel
            {
                Dock      = DockStyle.Bottom, Height = 50,
                BackColor = Color.FromArgb(246, 249, 255),
                Padding   = new Padding(28, 0, 28, 0)
            };
            var tblTotal = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor       = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblTotal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblTotal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblTotal.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblTotal.Controls.Add(new Label
            {
                Text      = $"Deposit Required:   HK$ {q.DepositRequired:N2}",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);
            tblTotal.Controls.Add(new Label
            {
                Text      = $"Total Amount:   HK$ {q.TotalAmount:N2}",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, AutoSize = false
            }, 1, 0);
            pnlTotalRow.Controls.Add(tblTotal);

            var pnlFooter = new Panel
            {
                Dock    = DockStyle.Bottom, Height = 80,
                BackColor = Color.White, Padding = new Padding(0, 10, 28, 10)
            };
            pnlFooter.Paint += PaintTopBorderStatic;
            var btnClose = new Button
            {
                Text      = "Close",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock      = DockStyle.Right,
                Width     = 140,
                Cursor    = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnClose.Click += (s, ev) => dlg.Close();
            pnlFooter.Controls.Add(btnClose);

            dlg.Controls.Add(dgv);
            dlg.Controls.Add(pnlTotalRow);
            dlg.Controls.Add(pnlLineLabel);
            if (hasTnC) dlg.Controls.Add(pnlTnC);
            dlg.Controls.Add(pnlInfo);
            dlg.Controls.Add(pnlHeader);
            dlg.Controls.Add(pnlFooter);
            dlg.ShowDialog(this);
        }

        // ── Label factory helpers
        private static Label MakeLabelKey(string text) => new Label
        {
            Text         = text,
            Font         = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor    = Color.FromArgb(98, 112, 135),
            Dock         = DockStyle.Fill,
            TextAlign    = ContentAlignment.MiddleLeft,
            Padding      = new Padding(0, 0, 8, 0),
            AutoEllipsis = false
        };
        private static Label MakeLabelVal(string text) => new Label
        {
            Text         = text ?? "—",
            Font         = new Font("Segoe UI", 12f),
            ForeColor    = Color.FromArgb(15, 31, 53),
            Dock         = DockStyle.Fill,
            TextAlign    = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        private static void PaintBottomBorderStatic(object s, PaintEventArgs e)
        {
            var p = (Panel)s;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
        }
        private static void PaintTopBorderStatic(object s, PaintEventArgs e)
        {
            var p = (Panel)s;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, 0, p.Width, 0);
        }

        // ── Status update
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
                MessageBox.Show("Failed to update quotation status. Please try again.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
