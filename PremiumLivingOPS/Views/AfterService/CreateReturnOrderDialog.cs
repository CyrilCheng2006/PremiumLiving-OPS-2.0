using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.AfterService
{
    /// <summary>
    /// Create Return Order dialog.
    /// Layout baseline: ComplaintListForm.btnAddNew_Click dialog.
    /// Picker popups: ComplaintListForm.ShowOrderPicker / ShowStaffPicker baseline.
    /// </summary>
    public class CreateReturnOrderDialog : Form
    {
        // ── Constants (mirror ComplaintListForm DLG_* exactly) ────────────
        private const int DLG_LabelW = 340;
        private const int DLG_RowH   = 80;
        private const int DLG_BtnW   = 210;
        private const int DLG_BtnH   = 60;

        // ── State ─────────────────────────────────────────────────────────
        private readonly AfterServiceController _ctrl;

        private List<OrderEntity>                                                             _orderList;
        private List<(string StaffID, string StaffName, string Department, string StaffRole)> _staffList;

        private string _selectedOrderID;
        private string _selectedStaffID;
        private string _selectedStaffName;

        public CreateReturnOrderDialog(AfterServiceController ctrl)
        {
            _ctrl      = ctrl;
            _orderList = _ctrl.GetOrdersForReturnPicker();
            _staffList = _ctrl.GetStaffListForPicker();
            InitUI();
        }

        // ════════════════════════════════════════════════════════════════
        //  ID generation  RTN-YYYYMMDD-XXXX
        // ════════════════════════════════════════════════════════════════
        private string GenerateReturnId()
        {
            string prefix   = "RTN-" + DateTime.Today.ToString("yyyyMMdd") + "-";
            var    existing = _ctrl.GetReturnIdsByPrefix(prefix);   // returns List<string>
            int    next     = 1;
            foreach (var id in existing)
            {
                // suffix starts after fixed prefix length
                int suffixStart = prefix.Length;
                if (id.Length >= suffixStart + 4 &&
                    int.TryParse(id.Substring(suffixStart, 4), out int seq) &&
                    seq >= next)
                    next = seq + 1;
            }
            return $"{prefix}{next:D4}";
        }

        // ════════════════════════════════════════════════════════════════
        //  InitUI
        // ════════════════════════════════════════════════════════════════
        private void InitUI()
        {
            Text            = "Create Return Order";
            Size            = new Size(1500, 1200);
            MinimumSize     = new Size(1100, 800);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = Color.FromArgb(240, 244, 249);
            Font            = new Font("Segoe UI", 13f);

            string autoId = GenerateReturnId();

            // ── 1. Dark header ───────────────────────────────────────────
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            pnlHeader.Controls.Add(new Label
            {
                Text      = "\u2795  Create Return Order",
                Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false,
                Padding   = new Padding(32, 0, 0, 0)
            });

            // ── 2. Section title bar ─────────────────────────────────────
            var pnlSectionTitle = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 44,
                BackColor = Color.FromArgb(241, 245, 255),
                Padding   = new Padding(32, 0, 16, 0)
            };
            PaintBottomBorder(pnlSectionTitle);
            pnlSectionTitle.Controls.Add(new Label
            {
                Text      = "\uD83D\uDCCB  Return Order Information",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(47, 111, 237),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false
            });

            // ── 3. White footer ──────────────────────────────────────────
            var pnlFoot = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 96,
                BackColor = Color.White,
                Padding   = new Padding(0, 18, 28, 18)
            };
            pnlFoot.Paint += (s, pe) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                pe.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0);
            };

            var btnCreate = new Button
            {
                Text      = "\u2714  Create Return Order",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(5, 150, 105),
                FlatStyle = FlatStyle.Flat,
                Width     = DLG_BtnW + 80,
                Height    = DLG_BtnH,
                Cursor    = Cursors.Hand,
                Margin    = new Padding(0, 0, 10, 0)
            };
            btnCreate.FlatAppearance.BorderSize            = 0;
            btnCreate.FlatAppearance.MouseOverBackColor    = Color.FromArgb(4, 120, 87);
            btnCreate.FlatAppearance.MouseDownBackColor    = Color.FromArgb(3, 90, 68);

            var btnCancelDlg = new Button
            {
                Text      = "Cancel",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Width     = DLG_BtnW,
                Height    = DLG_BtnH,
                Cursor    = Cursors.Hand
            };
            btnCancelDlg.FlatAppearance.BorderColor       = Color.FromArgb(221, 227, 236);
            btnCancelDlg.FlatAppearance.BorderSize        = 1;
            btnCancelDlg.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);

            var footFlow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Right,
                AutoSize      = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor     = Color.Transparent
            };
            footFlow.Controls.Add(btnCreate);
            footFlow.Controls.Add(btnCancelDlg);
            pnlFoot.Controls.Add(footFlow);

            // ── 4. Row helpers (identical to ComplaintListForm) ──────────
            Panel MakeRow(string lText, Control input, bool last = false)
            {
                var row = new Panel { Height = DLG_RowH, BackColor = Color.White };
                if (!last)
                    row.Paint += (s, pe) =>
                    {
                        using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                        pe.Graphics.DrawLine(pen, 0, ((Panel)s).Height - 1, ((Panel)s).Width, ((Panel)s).Height - 1);
                    };
                var tlp = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                    BackColor = Color.White, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DLG_LabelW));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                var lbl = new Label
                {
                    Text      = lText,
                    Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(70, 85, 110),
                    BackColor = Color.FromArgb(248, 250, 252),
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoSize  = false,
                    Padding   = new Padding(24, 0, 8, 0)
                };
                var wrap = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(20, 14, 24, 14) };
                input.Dock = DockStyle.Fill;
                wrap.Controls.Add(input);
                tlp.Controls.Add(lbl,  0, 0);
                tlp.Controls.Add(wrap, 1, 0);
                row.Controls.Add(tlp);
                return row;
            }

            Panel MakePickerRow(string labelText, out Label valueDisplay, Action onBrowse, bool last = false)
            {
                var row = new Panel { Height = DLG_RowH, BackColor = Color.White };
                if (!last)
                    row.Paint += (s, pe) =>
                    {
                        using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                        pe.Graphics.DrawLine(pen, 0, ((Panel)s).Height - 1, ((Panel)s).Width, ((Panel)s).Height - 1);
                    };
                var outer = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                    BackColor = Color.White, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
                };
                outer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DLG_LabelW));
                outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
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
                var inner = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                    BackColor = Color.White, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding = new Padding(20, 14, 24, 14)
                };
                inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                inner.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
                inner.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                valueDisplay = new Label
                {
                    Text      = "(none selected)",
                    Font      = new Font("Segoe UI", 12f, FontStyle.Italic),
                    ForeColor = Color.FromArgb(150, 160, 175),
                    BackColor = Color.FromArgb(248, 250, 252),
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoSize  = false,
                    Padding   = new Padding(8, 0, 4, 0)
                };
                var btnBrowse = new Button
                {
                    Text      = "\uD83D\uDD0D  Browse",
                    Font      = new Font("Segoe UI", 11f),
                    BackColor = Color.FromArgb(47, 111, 237),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Dock      = DockStyle.Fill,
                    Cursor    = Cursors.Hand,
                    Margin    = new Padding(4, 0, 0, 0)
                };
                btnBrowse.FlatAppearance.BorderSize            = 0;
                btnBrowse.FlatAppearance.MouseOverBackColor    = Color.FromArgb(29, 78, 216);
                btnBrowse.Click += (_, __) => onBrowse();
                inner.Controls.Add(valueDisplay, 0, 0);
                inner.Controls.Add(btnBrowse,    1, 0);
                outer.Controls.Add(lbl,   0, 0);
                outer.Controls.Add(inner, 1, 0);
                row.Controls.Add(outer);
                return row;
            }

            // ── 5. Field declarations ────────────────────────────────────
            Label lblOrderVal = null;
            Label lblStaffVal = null;
            Label lblCustDisp = null;

            // Row 0 — Return ID (auto, read-only)
            var txtReturnID = new TextBox
            {
                Text        = autoId,
                Font        = new Font("Segoe UI", 12f, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly    = true,
                BackColor   = Color.FromArgb(240, 244, 249),
                ForeColor   = Color.FromArgb(47, 111, 237)
            };
            var rowReturnID = MakeRow("Return ID  (auto)", txtReturnID);

            // Row 1 — Order ID (picker, required)
            var rowOrder = MakePickerRow("Order ID *", out lblOrderVal, () =>
            {
                using var picker = new OrderPickerForm(_orderList);
                if (picker.ShowDialog(this) != DialogResult.OK) return;
                _selectedOrderID          = picker.SelectedOrderID;
                lblOrderVal.Text          = _selectedOrderID;
                lblOrderVal.Font          = new Font("Segoe UI", 12f, FontStyle.Bold);
                lblOrderVal.ForeColor     = Color.FromArgb(15, 31, 53);
                lblOrderVal.BackColor     = Color.White;
                lblCustDisp.Text          = picker.SelectedCustomer;
                lblCustDisp.Font          = new Font("Segoe UI", 12f);
                lblCustDisp.ForeColor     = Color.FromArgb(15, 31, 53);
                lblCustDisp.BackColor     = Color.White;
            });

            // Row 2 — Customer (auto-filled)
            lblCustDisp = new Label
            {
                Text      = "(auto-filled after selecting Order ID)",
                Font      = new Font("Segoe UI", 12f, FontStyle.Italic),
                ForeColor = Color.FromArgb(150, 160, 175),
                BackColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false
            };
            var rowCustomer = MakeRow("Customer  (auto)", lblCustDisp);

            // Row 3 — Handled By (picker, required)
            var rowStaff = MakePickerRow("Handled By *", out lblStaffVal, () =>
            {
                using var picker = new StaffPickerForm(_staffList);
                if (picker.ShowDialog(this) != DialogResult.OK) return;
                _selectedStaffID          = picker.SelectedStaffID;
                _selectedStaffName        = picker.SelectedStaffName;
                lblStaffVal.Text          = $"{picker.SelectedStaffName}  [{picker.SelectedStaffID}]";
                lblStaffVal.Font          = new Font("Segoe UI", 12f, FontStyle.Bold);
                lblStaffVal.ForeColor     = Color.FromArgb(15, 31, 53);
                lblStaffVal.BackColor     = Color.White;
            });

            // Row 4 — Return Date
            var dtpReturnDate = new DateTimePicker
            {
                Format    = DateTimePickerFormat.Short,
                Value     = DateTime.Today,
                Font      = new Font("Segoe UI", 12f),
                BackColor = Color.White
            };
            var rowDate = MakeRow("Return Date *", dtpReturnDate);

            // Row 5 — Refund Amount
            var txtRefund = new TextBox
            {
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                PlaceholderText = "0.00"
            };
            var rowRefund = MakeRow("Refund Amount (HK$) *", txtRefund);

            // Row 6 — Status
            var cboStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 12f)
            };
            cboStatus.Items.AddRange(new object[] { "Pending", "Processing", "Refunded", "Rejected" });
            cboStatus.SelectedIndex = 0;
            var rowStatus = MakeRow("Status *", cboStatus);

            // Row 7 — Reason (last, no bottom border)
            var txtReason = new TextBox
            {
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                PlaceholderText = "Describe the reason for the return"
            };
            var rowReason = MakeRow("Reason", txtReason, last: true);

            // ── 6. Card assembly ─────────────────────────────────────────
            var allRows  = new Panel[] { rowReturnID, rowOrder, rowCustomer, rowStaff, rowDate, rowRefund, rowStatus, rowReason };
            int cardH    = allRows.Length * DLG_RowH;

            var cardOuter = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = cardH + 32,
                BackColor = Color.Transparent,
                Padding   = new Padding(20, 16, 20, 16)
            };
            var cardInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            cardInner.Paint += (s, pe) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                pe.Graphics.DrawRectangle(pen, 0, 0, ((Panel)s).Width - 1, ((Panel)s).Height - 1);
            };
            int y = 0;
            foreach (var r in allRows)
            {
                r.Location = new Point(0, y);
                r.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                r.Width    = 1400;
                cardInner.Controls.Add(r);
                y += DLG_RowH;
            }
            cardInner.Resize += (s, _) => { var p = (Panel)s; foreach (Control r in p.Controls) r.Width = p.Width; };
            cardOuter.Controls.Add(cardInner);

            var pnlFill = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249), AutoScroll = true };
            pnlFill.Controls.Add(cardOuter);

            // ── 7. Wire buttons ──────────────────────────────────────────
            btnCreate.Click += (s, ev) =>
            {
                if (string.IsNullOrWhiteSpace(_selectedOrderID))
                { MessageBox.Show("Please select an Order ID.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                if (string.IsNullOrWhiteSpace(_selectedStaffID))
                { MessageBox.Show("Please select a staff member for Handled By.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                if (!double.TryParse(txtRefund.Text.Trim(),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.CurrentCulture, out double refund) || refund < 0)
                { MessageBox.Show("Please enter a valid Refund Amount.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtRefund.Focus(); return; }

                try
                {
                    var entity = new ReturnOrderEntity
                    {
                        ReturnID     = txtReturnID.Text.Trim(),
                        OrderID      = _selectedOrderID,
                        ReturnDate   = dtpReturnDate.Value.Date,
                        Reason       = txtReason.Text.Trim(),
                        RefundAmount = refund,
                        ReturnStatus = cboStatus.SelectedItem?.ToString() ?? "Pending"
                    };
                    if (_ctrl.CreateReturnOrder(entity))
                    {
                        MessageBox.Show($"Return Order {entity.ReturnID} created successfully.",
                            "Created", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                    else
                        MessageBox.Show("Failed to create return order. Please try again.",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to create return order:\n{ex.Message}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            btnCancelDlg.Click += (_, __) => { DialogResult = DialogResult.Cancel; Close(); };

            // ── 8. Compose ───────────────────────────────────────────────
            Controls.Add(pnlFill);
            Controls.Add(pnlFoot);
            Controls.Add(pnlSectionTitle);
            Controls.Add(pnlHeader);
        }

        private static void PaintBottomBorder(Panel p)
        {
            p.Paint += (s, e) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawLine(pen, 0, ((Panel)s).Height - 1, ((Panel)s).Width, ((Panel)s).Height - 1);
            };
        }
    }
}
