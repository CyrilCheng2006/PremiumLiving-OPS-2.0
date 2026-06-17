using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    /// <summary>
    /// Modal dialog for recording a new Purchase Invoice against a PO.
    /// On OK the caller reads dialog.Result and passes it to the controller.
    /// </summary>
    public class RecordInvoiceDialog : Form
    {
        private readonly PurchaseOrderEntity _po;

        /// <summary>Populated when the user clicks Confirm (DialogResult.OK).</summary>
        public RecordPurchaseInvoiceVM Result { get; private set; }

        // ── Controls ─────────────────────────────────────────────────
        private NumericUpDown nudTotal;
        private ComboBox      cboPayStatus;
        private DateTimePicker dtpExpected;

        public RecordInvoiceDialog(PurchaseOrderEntity po)
        {
            _po           = po ?? throw new ArgumentNullException(nameof(po));
            InitForm();
        }

        private void InitForm()
        {
            Text            = $"Record Invoice — {_po.PurchaseID}";
            Size            = new Size(500, 420);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = Color.FromArgb(243, 244, 246);
            Font            = new Font("Segoe UI", 11f);

            var outer = new Panel
            {
                Dock      = DockStyle.Fill,
                Padding   = new Padding(16),
                BackColor = Color.FromArgb(243, 244, 246)
            };
            Controls.Add(outer);

            var card = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.White,
                Padding   = new Padding(24)
            };
            card.Paint += (s, e) =>
            {
                var b = ((Control)s).ClientRectangle;
                b.Width--; b.Height--;
                using var pen = new Pen(Color.FromArgb(221, 227, 236), 1f);
                e.Graphics.DrawRectangle(pen, b);
            };
            outer.Controls.Add(card);

            // ── Form fields ───────────────────────────────────────────
            var tlp = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                RowCount    = 6,
                BackColor   = Color.Transparent
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150f));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            for (int i = 0; i < 6; i++)
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
            card.Controls.Add(tlp);

            AddLabel(tlp, "Purchase ID",   0);
            AddLabel(tlp, "Supplier",       1);
            AddLabel(tlp, "Total Amount",   2);
            AddLabel(tlp, "Payment Status", 3);
            AddLabel(tlp, "Expected Date",  4);

            AddReadOnly(tlp, _po.PurchaseID,   0);
            AddReadOnly(tlp, _po.SupplierName, 1);

            nudTotal = new NumericUpDown
            {
                Minimum       = 0,
                Maximum       = 9999999,
                DecimalPlaces = 2,
                Value         = (decimal)(_po.POTotalAmount),
                Dock          = DockStyle.Fill,
                Font          = new Font("Segoe UI", 11f)
            };
            tlp.Controls.Add(nudTotal, 1, 2);

            cboPayStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock          = DockStyle.Fill,
                Font          = new Font("Segoe UI", 11f)
            };
            cboPayStatus.Items.AddRange(new[] { "Full", "Partial", "Unpaid" });
            cboPayStatus.SelectedIndex = 0;
            tlp.Controls.Add(cboPayStatus, 1, 3);

            dtpExpected = new DateTimePicker
            {
                Format   = DateTimePickerFormat.Short,
                Value    = DateTime.Today.AddDays(30),
                Dock     = DockStyle.Fill,
                Font     = new Font("Segoe UI", 11f)
            };
            tlp.Controls.Add(dtpExpected, 1, 4);

            // ── Buttons ───────────────────────────────────────────────
            var btnRow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height        = 50,
                BackColor     = Color.Transparent,
                Padding       = new Padding(0, 8, 0, 0)
            };
            card.Controls.Add(btnRow);

            var btnCancel = new Button
            {
                Text      = "Cancel",
                Size      = new Size(100, 36),
                BackColor = Color.FromArgb(243, 244, 246),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 11f)
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var btnConfirm = new Button
            {
                Text      = "Confirm",
                Size      = new Size(110, 36),
                BackColor = Color.FromArgb(47, 111, 237),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold)
            };
            btnConfirm.FlatAppearance.BorderSize = 0;
            btnConfirm.Click += BtnConfirm_Click;

            btnRow.Controls.Add(btnCancel);
            btnRow.Controls.Add(btnConfirm);
        }

        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            Result = new RecordPurchaseInvoiceVM
            {
                PurchaseID    = _po.PurchaseID,
                SupplierName  = _po.SupplierName,
                TotalAmount   = (double)nudTotal.Value,
                PaymentStatus = cboPayStatus.SelectedItem?.ToString() ?? "Full",
                ExpectedDate  = dtpExpected.Value.Date
            };
            DialogResult = DialogResult.OK;
            Close();
        }

        private static void AddLabel(TableLayoutPanel tlp, string text, int row)
        {
            tlp.Controls.Add(new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock      = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            }, 0, row);
        }

        private static void AddReadOnly(TableLayoutPanel tlp, string value, int row)
        {
            tlp.Controls.Add(new Label
            {
                Text      = value,
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock      = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            }, 1, row);
        }
    }
}
