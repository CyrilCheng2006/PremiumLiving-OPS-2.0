using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    /// <summary>
    /// Modal dialog for recording a new Purchase Invoice against a PO.
    /// Visual style mirrors ReceiptDetailDialog:
    ///   • Dark navy header bar (72 px)
    ///   • Light-blue-gray page background (244, 246, 250)
    ///   • 75 px breathing gap between header and white card
    ///   • White card with subtle border floats on gray bg
    ///   • White footer strip (80 px) with Cancel + Confirm buttons
    ///   • Size: 2200 × 920  (identical to ReceiptDetailDialog)
    ///   • MinimumSize: 1200 × 680
    ///
    /// DOCK ORDER RULE (WinForms): Fill must be added FIRST, then Top controls.
    /// Controls are rendered in reverse add-order for Top/Bottom docking.
    /// </summary>
    public class RecordInvoiceDialog : Form
    {
        private readonly PurchaseOrderEntity _po;

        /// <summary>Populated when the user clicks Confirm (DialogResult.OK).</summary>
        public RecordPurchaseInvoiceVM Result { get; private set; }

        private NumericUpDown  _nudTotal;
        private ComboBox       _cboPayStatus;
        private DateTimePicker _dtpExpected;

        public RecordInvoiceDialog(PurchaseOrderEntity po)
        {
            _po = po ?? throw new ArgumentNullException(nameof(po));
            BuildUI();
        }

        private void BuildUI()
        {
            Text            = $"Record Invoice  —  {_po.PurchaseID}";
            Size            = new Size(2200, 920);
            MinimumSize     = new Size(1200, 680);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = Color.FromArgb(244, 246, 250);
            Font            = new Font("Segoe UI", 12f);

            // ── 1. Header (Top) ──────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 72,
                BackColor = Color.FromArgb(19, 35, 61)
            };
            var tblHeader = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 2,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(28, 0, 28, 0)
            };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblHeader.Controls.Add(new Label
            {
                Text      = $"Record Invoice  —  {_po.PurchaseID}",
                Font      = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false
            }, 0, 0);
            tblHeader.Controls.Add(new Label
            {
                Text      = _po.PurchaseStatus ?? "Pending",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(186, 230, 253),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize  = false
            }, 1, 0);
            pnlHeader.Controls.Add(tblHeader);

            // ── 2. Footer (Bottom) ───────────────────────────────────
            var pnlFooter = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 80,
                BackColor = Color.White,
                Padding   = new Padding(0, 10, 32, 10)
            };
            pnlFooter.Paint += PaintTopBorder;

            var btnCancel = new Button
            {
                Text      = "Cancel",
                Size      = new Size(160, 60),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 13f),
                Cursor    = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnCancel.FlatAppearance.BorderSize  = 1;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var btnConfirm = new Button
            {
                Text      = "Confirm",
                Size      = new Size(210, 60),
                BackColor = Color.FromArgb(47, 111, 237),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnConfirm.FlatAppearance.BorderSize = 0;
            btnConfirm.Click += BtnConfirm_Click;

            pnlFooter.Controls.Add(btnCancel);
            pnlFooter.Controls.Add(btnConfirm);
            pnlFooter.Layout += (s, e) =>
            {
                int right  = pnlFooter.Width - 32;
                int btnTop = (pnlFooter.Height - 60) / 2;
                btnConfirm.Location = new Point(right - 210,             btnTop);
                btnCancel.Location  = new Point(right - 210 - 12 - 160, btnTop);
            };

            // ── 3. Body (Fill) ───────────────────────────────────────
            var pnlBody = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(244, 246, 250),
                Padding   = new Padding(24, 75, 24, 16)
            };

            // DOCK ORDER: Fill → then Top controls (added last = rendered first/top)
            // Add Fill controls first ────────────────────────────────
            var cardInput = BuildInputCard();   // DockStyle.Fill — add first

            // Add Top controls after (render on top in reverse order) ─
            var spacer = new Panel { Dock = DockStyle.Top, Height = 16, BackColor = Color.Transparent };
            var cardContext = BuildContextCard();  // DockStyle.Top — add last = topmost

            pnlBody.Controls.Add(cardInput);    // Fill  — first
            pnlBody.Controls.Add(spacer);       // Top   — second
            pnlBody.Controls.Add(cardContext);  // Top   — third (rendered topmost)

            // Form-level dock order: Fill first, then Top/Bottom
            Controls.Add(pnlBody);      // Fill
            Controls.Add(pnlFooter);    // Bottom
            Controls.Add(pnlHeader);    // Top (last added = visually on top)
        }

        // ── Context card (read-only PO info) ────────────────────────
        private Panel BuildContextCard()
        {
            var card = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 100,
                BackColor = Color.White,
                Padding   = new Padding(28, 14, 28, 14)
            };
            card.Paint += PaintRoundedCard;

            var tbl = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 4,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            tbl.Controls.Add(MakeLabelKey("Purchase ID"), 0, 0);
            tbl.Controls.Add(MakeLabelVal(_po.PurchaseID ?? ""), 1, 0);
            tbl.Controls.Add(MakeLabelKey("Supplier"), 2, 0);
            var lblSupplier = MakeLabelVal(_po.SupplierName ?? "");
            lblSupplier.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            tbl.Controls.Add(lblSupplier, 3, 0);

            card.Controls.Add(tbl);
            return card;
        }

        // ── Input card (Invoice Details + 3 fields) ─────────────────
        private Panel BuildInputCard()
        {
            var card = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.White,
                Padding   = new Padding(28, 20, 28, 20)
            };
            card.Paint += PaintRoundedCard;

            // Build field rows (DockStyle.Top inside card)
            // DOCK ORDER inside card: Fill first (none here), then Top last-in = topmost
            // We want: Title → Divider → Spacer → Fields (top-to-bottom)
            // So add in REVERSE: fields last → spacer → divider → title first

            var rowDate    = BuildFieldRow("Expected Date",     BuildDatePicker());  // row 3
            var spacer3    = new Panel { Dock = DockStyle.Top, Height = 12, BackColor = Color.Transparent };
            var rowStatus  = BuildFieldRow("Payment Status",    BuildPayStatus());   // row 2
            var spacer2    = new Panel { Dock = DockStyle.Top, Height = 12, BackColor = Color.Transparent };
            var rowTotal   = BuildFieldRow("Total Amount (HK$)", BuildNud());        // row 1
            var spacer1    = new Panel { Dock = DockStyle.Top, Height = 16, BackColor = Color.Transparent };
            var divider    = new Panel { Dock = DockStyle.Top, Height = 1,  BackColor = Color.FromArgb(226, 232, 240) };
            var pnlTitle   = BuildCardTitlePanel("Invoice Details");

            // Add in reverse visual order (last added = topmost for DockStyle.Top)
            card.Controls.Add(rowDate);    // bottom-most field
            card.Controls.Add(spacer3);
            card.Controls.Add(rowStatus);
            card.Controls.Add(spacer2);
            card.Controls.Add(rowTotal);   // top-most field
            card.Controls.Add(spacer1);
            card.Controls.Add(divider);
            card.Controls.Add(pnlTitle);   // last added = visually topmost

            return card;
        }

        private static Panel BuildCardTitlePanel(string title)
        {
            var pnl = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 48,
                BackColor = Color.White,
                Padding   = new Padding(0, 12, 0, 0)
            };
            pnl.Controls.Add(new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false
            });
            return pnl;
        }

        // Each field row: label (top, 28 px) + input control (top, 46 px)
        private static Panel BuildFieldRow(string labelText, Control input)
        {
            var row = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 82,
                BackColor = Color.Transparent
            };

            input.Dock   = DockStyle.Top;
            input.Height = 46;

            var lbl = new Label
            {
                Text      = labelText,
                Font      = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock      = DockStyle.Top,
                Height    = 28,
                TextAlign = ContentAlignment.BottomLeft,
                AutoSize  = false,
                Padding   = new Padding(2, 0, 0, 0)
            };

            // Inside the row: input goes below label
            // Add input first (bottom), label second (top)
            row.Controls.Add(input);
            row.Controls.Add(lbl);
            return row;
        }

        // ── Control builders ─────────────────────────────────────────
        private NumericUpDown BuildNud()
        {
            _nudTotal = new NumericUpDown
            {
                Minimum       = 0,
                Maximum       = 9_999_999,
                DecimalPlaces = 2,
                Value         = (decimal)_po.POTotalAmount,
                Font          = new Font("Segoe UI", 12f),
                BackColor     = Color.FromArgb(248, 250, 252),
                ForeColor     = Color.FromArgb(30, 41, 59)
            };
            return _nudTotal;
        }

        private ComboBox BuildPayStatus()
        {
            _cboPayStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 12f),
                BackColor     = Color.FromArgb(248, 250, 252),
                ForeColor     = Color.FromArgb(30, 41, 59)
            };
            _cboPayStatus.Items.AddRange(new[] { "Full", "Partial", "Unpaid" });
            _cboPayStatus.SelectedIndex = 0;
            return _cboPayStatus;
        }

        private DateTimePicker BuildDatePicker()
        {
            _dtpExpected = new DateTimePicker
            {
                Format                  = DateTimePickerFormat.Short,
                Value                   = DateTime.Today.AddDays(30),
                Font                    = new Font("Segoe UI", 12f),
                CalendarForeColor       = Color.FromArgb(30, 41, 59),
                CalendarMonthBackground = Color.White
            };
            return _dtpExpected;
        }

        // ── Confirm handler ──────────────────────────────────────────
        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            Result = new RecordPurchaseInvoiceVM
            {
                PurchaseID    = _po.PurchaseID,
                SupplierName  = _po.SupplierName,
                TotalAmount   = (double)_nudTotal.Value,
                PaymentStatus = _cboPayStatus.SelectedItem?.ToString() ?? "Full",
                ExpectedDate  = _dtpExpected.Value.Date
            };
            DialogResult = DialogResult.OK;
            Close();
        }

        // ── Paint helpers ────────────────────────────────────────────
        private static void PaintRoundedCard(object s, PaintEventArgs e)
        {
            var c = (Control)s;
            using var pen = new Pen(Color.FromArgb(226, 232, 240), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, c.Width - 1, c.Height - 1);
        }

        private static void PaintTopBorder(object s, PaintEventArgs e)
        {
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, 0, ((Control)s).Width, 0);
        }

        // ── Label factories ──────────────────────────────────────────
        private static Label MakeLabelKey(string text) => new Label
        {
            Text      = text,
            Font      = new Font("Segoe UI", 10f),
            ForeColor = Color.FromArgb(100, 116, 139),
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize  = false,
            Padding   = new Padding(4, 0, 0, 0)
        };

        private static Label MakeLabelVal(string text) => new Label
        {
            Text      = text ?? "",
            Font      = new Font("Segoe UI", 12f),
            ForeColor = Color.FromArgb(30, 41, 59),
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize  = false,
            Padding   = new Padding(4, 0, 0, 0)
        };
    }
}
