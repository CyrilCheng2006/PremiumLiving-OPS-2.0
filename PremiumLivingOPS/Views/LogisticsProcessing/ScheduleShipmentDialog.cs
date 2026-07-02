using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    // Schedule Shipment dialog (MVC View layer).
    // Layout mirrors CreateQuotationDialog visual language:
    //   pnlHeader  Top 80   dark navy, title + status badge
    //   CardPanel  Fill     grey outer > white card > 4-col form grid
    //   pnlFooter  Bottom 80  [Schedule 210x60]  [Cancel 210x60]
    // Window: 1800 x 800  (MinimumSize 1400 x 700)
    public class ScheduleShipmentDialog : Form
    {
        private readonly LogisticsProcessingController _ctrl;
        private readonly ShipmentEntity _shipment;

        private DateTimePicker _dtpScheduledDate;
        private ComboBox       _cboDeliveryMethod;
        private TextBox        _txtContactPerson;
        private TextBox        _txtNotes;

        public ScheduleShipmentDialog(
            LogisticsProcessingController ctrl,
            ShipmentEntity shipment)
        {
            _ctrl     = ctrl     ?? throw new ArgumentNullException(nameof(ctrl));
            _shipment = shipment ?? throw new ArgumentNullException(nameof(shipment));
            BuildUI();
        }

        // ----------------------------------------------------------------
        //  Build UI
        // ----------------------------------------------------------------
        private void BuildUI()
        {
            Text            = "Schedule Shipment  —  " + _shipment.ShipmentID;
            Size            = new Size(1800, 800);
            MinimumSize     = new Size(1400, 700);
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = Color.FromArgb(240, 244, 249);
            Font            = new Font("Segoe UI", 13f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;

            // -- Header ---------------------------------------------------
            var pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 80,
                BackColor = Color.FromArgb(19, 35, 61)
            };
            var tblHeader = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 2,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(24, 0, 24, 0)
            };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            tblHeader.Controls.Add(new Label
            {
                Text      = "Schedule Shipment  —  " + _shipment.ShipmentID,
                Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false
            }, 0, 0);

            StatusColors.TryGetValue(_shipment.ShipmentStatus ?? string.Empty, out var sc);
            tblHeader.Controls.Add(new Label
            {
                Text      = _shipment.ShipmentStatus ?? "Unknown",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = sc.fg != default ? sc.fg : Color.White,
                BackColor = sc.bg != default ? sc.bg : Color.FromArgb(80, 80, 80),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize  = false,
                Padding   = new Padding(8, 4, 8, 4)
            }, 1, 0);

            pnlHeader.Controls.Add(tblHeader);

            // -- Footer ---------------------------------------------------
            var pnlFooter = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 80,
                BackColor = Color.White,
                Padding   = new Padding(0, 12, 28, 12)
            };
            pnlFooter.Paint += PaintTopBorder;

            var btnSchedule = new Button
            {
                Text      = "\u2714  Schedule",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(109, 40, 217),
                FlatStyle = FlatStyle.Flat,
                Dock      = DockStyle.Right,
                Width     = 210,
                Height    = 56,
                Cursor    = Cursors.Hand
            };
            btnSchedule.FlatAppearance.BorderSize         = 0;
            btnSchedule.FlatAppearance.MouseOverBackColor = Color.FromArgb(91, 25, 180);
            btnSchedule.FlatAppearance.MouseDownBackColor = Color.FromArgb(69, 17, 140);
            btnSchedule.Click += BtnSchedule_Click;

            var btnCancel = new Button
            {
                Text      = "Cancel",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock      = DockStyle.Right,
                Width     = 210,
                Height    = 56,
                Cursor    = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnCancel.FlatAppearance.BorderSize         = 1;
            btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            pnlFooter.Controls.Add(btnSchedule);
            pnlFooter.Controls.Add(btnCancel);

            // -- Card (three-layer CardPanel) -----------------------------
            var (cardOuter, cardInner) = CardPanel.Create(
                outerHeight:  560,
                outerPadding: new Padding(20, 14, 20, 8));

            var tblForm = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 4,
                RowCount        = 4,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(28, 24, 28, 16)
            };
            for (int c = 0; c < 4; c++)
                tblForm.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            tblForm.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));  // row 0: section label
            tblForm.RowStyles.Add(new RowStyle(SizeType.Percent,  38f));  // row 1: read-only fields
            tblForm.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));  // row 2: section label
            tblForm.RowStyles.Add(new RowStyle(SizeType.Percent,  62f));  // row 3: editable fields

            // Row 0: section label
            var lblSection0 = new Label
            {
                Text      = "SHIPMENT INFORMATION",
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(29, 78, 216),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false,
                Padding   = new Padding(0, 0, 0, 4)
            };
            tblForm.SetColumnSpan(lblSection0, 4);
            tblForm.Controls.Add(lblSection0, 0, 0);

            // Row 1: read-only fields
            tblForm.Controls.Add(MakeFieldCell("Shipment ID",
                MakeReadOnlyLabel(_shipment.ShipmentID)), 0, 1);
            tblForm.Controls.Add(MakeFieldCell("Order ID",
                MakeReadOnlyLabel(_shipment.OrderID)), 1, 1);
            tblForm.Controls.Add(MakeFieldCell("Customer",
                MakeReadOnlyLabel(_shipment.CustomerName ?? "\u2014")), 2, 1);
            tblForm.Controls.Add(MakeFieldCell("Current Status",
                MakeReadOnlyLabel(_shipment.ShipmentStatus ?? "\u2014")), 3, 1);

            // Row 2: section label
            var lblSection1 = new Label
            {
                Text      = "SCHEDULE DETAILS",
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(109, 40, 217),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false,
                Padding   = new Padding(0, 0, 0, 4)
            };
            tblForm.SetColumnSpan(lblSection1, 4);
            tblForm.Controls.Add(lblSection1, 0, 2);

            // Row 3: editable fields
            _dtpScheduledDate = new DateTimePicker
            {
                Dock    = DockStyle.Fill,
                Format  = DateTimePickerFormat.Short,
                Font    = new Font("Segoe UI", 12f),
                Value   = _shipment.DeliveryDate.HasValue
                              ? _shipment.DeliveryDate.Value.Date
                              : DateTime.Today.AddDays(1),
                MinDate = DateTime.Today
            };

            _cboDeliveryMethod = new ComboBox
            {
                Dock          = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 12f)
            };
            _cboDeliveryMethod.Items.AddRange(new object[] { "Courier", "SelfPickup" });
            int dmIdx = _cboDeliveryMethod.FindStringExact(
                _shipment.DeliveryMethod ?? "Courier");
            _cboDeliveryMethod.SelectedIndex = dmIdx >= 0 ? dmIdx : 0;

            _txtContactPerson = new TextBox
            {
                Dock            = DockStyle.Fill,
                Font            = new Font("Segoe UI", 12f),
                PlaceholderText = "e.g. Chan Siu Ming",
                BorderStyle     = BorderStyle.FixedSingle
            };

            _txtNotes = new TextBox
            {
                Dock            = DockStyle.Fill,
                Font            = new Font("Segoe UI", 12f),
                Multiline       = false,
                PlaceholderText = "Optional delivery notes",
                BorderStyle     = BorderStyle.FixedSingle
            };

            tblForm.Controls.Add(MakeFieldCell("Scheduled Date *",  _dtpScheduledDate),  0, 3);
            tblForm.Controls.Add(MakeFieldCell("Delivery Method *", _cboDeliveryMethod), 1, 3);
            tblForm.Controls.Add(MakeFieldCell("Contact Person",    _txtContactPerson),  2, 3);
            tblForm.Controls.Add(MakeFieldCell("Notes",             _txtNotes),          3, 3);

            cardInner.Controls.Add(tblForm);

            // Assemble (Bottom first, then Top, then Fill)
            Controls.Add(cardOuter);
            Controls.Add(pnlFooter);
            Controls.Add(pnlHeader);
        }

        // ----------------------------------------------------------------
        //  Schedule button handler
        // ----------------------------------------------------------------
        private void BtnSchedule_Click(object sender, EventArgs e)
        {
            DateTime scheduledDate  = _dtpScheduledDate.Value.Date;
            string   deliveryMethod = _cboDeliveryMethod.SelectedItem?.ToString() ?? "Courier";
            string   contactPerson  = _txtContactPerson.Text.Trim();
            string   notes          = _txtNotes.Text.Trim();

            if (scheduledDate < DateTime.Today)
            {
                MessageBox.Show(
                    "Scheduled date cannot be in the past.",
                    "Validation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                _dtpScheduledDate.Focus();
                return;
            }

            try
            {
                _ctrl.ScheduleShipment(
                    _shipment.ShipmentID,
                    scheduledDate,
                    deliveryMethod,
                    contactPerson,
                    notes);

                MessageBox.Show(
                    "Shipment " + _shipment.ShipmentID +
                    " has been scheduled for " + scheduledDate.ToString("yyyy-MM-dd") + ".",
                    "Scheduled",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to schedule shipment:\n" + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // ----------------------------------------------------------------
        //  UI helpers
        // ----------------------------------------------------------------

        // Caption row height 36px prevents text clipping at any DPI scaling.
        private static TableLayoutPanel MakeFieldCell(string caption, Control ctrl)
        {
            var tlp = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                RowCount        = 2,
                ColumnCount     = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(0, 0, 14, 0)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent,  100f));

            tlp.Controls.Add(new Label
            {
                Text      = caption,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft,
                Padding   = new Padding(0, 0, 0, 3)
            }, 0, 0);

            ctrl.Dock = DockStyle.Fill;
            tlp.Controls.Add(ctrl, 0, 1);
            return tlp;
        }

        private static Label MakeReadOnlyLabel(string text)
        {
            return new Label
            {
                Text         = text,
                Font         = new Font("Segoe UI", 12f),
                ForeColor    = Color.FromArgb(15, 31, 53),
                Dock         = DockStyle.Fill,
                TextAlign    = ContentAlignment.MiddleLeft,
                AutoSize     = false,
                AutoEllipsis = true
            };
        }

        private static void PaintTopBorder(object s, PaintEventArgs e)
        {
            using (var pen = new Pen(Color.FromArgb(221, 227, 236), 1))
            {
                e.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0);
            }
        }

        private static readonly System.Collections.Generic.Dictionary<string, (Color bg, Color fg)>
            StatusColors = new System.Collections.Generic.Dictionary<string, (Color, Color)>
            {
                { "Pending",    (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "In Transit", (Color.FromArgb(219, 234, 254), Color.FromArgb( 29,  78, 216)) },
                { "Completed",  (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) }
            };
    }
}
