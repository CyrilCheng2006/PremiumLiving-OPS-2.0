using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    // =========================================================================
    // ModifyShipmentDialog
    // -------------------------------------------------------------------------
    // Standalone dialog launched from the KPI-Bar "Modify Shipment" button in
    // ViewShipmentForm.  Rendering baseline: ShowDetailDialog in ViewShipmentForm.cs
    //
    // Window size: 2500 × 1100  (matches View Detail exactly)
    //
    // Structure
    // ──────────────────────────────────────────────────────────────────────────
    //  ┌─ Header 80px ───────────────────────────────────────────────────────┐
    //  │  Modify Shipment — SHP-XXXX                       [ STATUS BADGE ]  │
    //  ├─ Info panel 400px (read-only, same 4-col TLP as View Detail) ───────┤
    //  ├─ Divider: "EDIT FIELDS" label 40px ──────────────────────────────── │
    //  ├─ Edit panel  Fill  (each cell: 56px caption + Fill input) ──────── │
    //  │  [ New Status * ]  [ New Tracking No. ]                            │
    //  │  [ Actual Recipient ]  [ Remark ]                                  │
    //  ├─ Total row 64px ───────────────────────────────────────────────────┤
    //  ├─ Footer 86px ─────────────────────────────────────────────────────┤
    //  │  [ 🗑 Delete ]          [ Cancel ]   [ ✔ Save Changes ]            │
    //  └────────────────────────────────────────────────────────────────────┘
    // =========================================================================
    public class ModifyShipmentDialog : Form
    {
        // ── Dependencies ─────────────────────────────────────────────────────
        private readonly LogisticsProcessingController _ctrl;
        private readonly ShipmentDetailVM              _detail;
        private readonly ShipmentEntity                _ship;

        // ── Editable controls ────────────────────────────────────────────────
        private ComboBox _cboStatus;
        private TextBox  _txtTracking;
        private TextBox  _txtRecipient;
        private TextBox  _txtRemark;

        // ── Edit cell caption height (doubled to prevent input overlapping label) ─
        private const int CaptionH = 56;   // was 28, now 2× to give caption full room
        private const int InputH   = 40;   // fixed height for ComboBox / TextBox

        // ── Status colour palette (matches ViewShipmentForm) ─────────────────
        private static readonly Dictionary<string, (Color bg, Color fg)> StatusColors =
            new Dictionary<string, (Color, Color)>
            {
                { "Pending",    (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "In Transit", (Color.FromArgb(219, 234, 254), Color.FromArgb( 29,  78, 216)) },
                { "Completed",  (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
            };

        // =========================================================================
        //  Constructor
        // =========================================================================
        public ModifyShipmentDialog(
            LogisticsProcessingController ctrl,
            ShipmentDetailVM              detail)
        {
            _ctrl   = ctrl   ?? throw new ArgumentNullException(nameof(ctrl));
            _detail = detail ?? throw new ArgumentNullException(nameof(detail));
            _ship   = detail.Shipment
                      ?? throw new ArgumentException("detail.Shipment must not be null");

            BuildUI();
            PopulateFields();
        }

        // =========================================================================
        //  UI construction  — mirrors ShowDetailDialog in ViewShipmentForm.cs
        // =========================================================================
        private void BuildUI()
        {
            Text            = $"Modify Shipment \u2014 {_ship.ShipmentID}";
            Size            = new Size(2500, 1100);
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = Color.White;
            Font            = new Font("Segoe UI", 13f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;

            // ── Header ──────────────────────────────────────────────────────────
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
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 264f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            tblHeader.Controls.Add(new Label
            {
                Text      = $"Modify Shipment  \u2014  {_ship.ShipmentID}",
                Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false
            }, 0, 0);

            StatusColors.TryGetValue(_ship.ShipmentStatus ?? string.Empty, out var sc);
            tblHeader.Controls.Add(new Label
            {
                Text      = _ship.ShipmentStatus ?? "Unknown",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = sc.fg != default ? sc.fg : Color.White,
                BackColor = sc.bg != default ? sc.bg : Color.FromArgb(80, 80, 80),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize  = false,
                Padding   = new Padding(8, 4, 8, 4)
            }, 1, 0);
            pnlHeader.Controls.Add(tblHeader);

            // ── Info panel (read-only, mirrors ShowDetailDialog exactly) ─────────
            var pnlInfo = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 400,
                Padding   = new Padding(28, 18, 28, 8),
                BackColor = Color.White
            };
            pnlInfo.Paint += (sender, e) =>
            {
                using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawLine(pen, 28, ((Panel)sender).Height - 1,
                                    ((Panel)sender).Width - 28, ((Panel)sender).Height - 1);
            };

            var tblInfo = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 4,
                RowCount        = 6,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            for (int r = 0; r < 5; r++)
                tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 14f));
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 30f));

            string dlvDate = _ship.DeliveryDate.HasValue
                ? _ship.DeliveryDate.Value.ToString("yyyy-MM-dd")
                : _ship.ShipDate.ToString("yyyy-MM-dd");

            var leftFields = new[]
            {
                ("Shipment ID",   _ship.ShipmentID),
                ("Order ID",      _ship.OrderID),
                ("Ship Date",     _ship.ShipDate.ToString("yyyy-MM-dd")),
                ("Delivery Date", dlvDate),
                ("Tracking No.",  string.IsNullOrWhiteSpace(_ship.TrackingNumber) ? "\u2014" : _ship.TrackingNumber),
                ("Address",       _ship.ShippingAddress ?? "\u2014"),
            };
            for (int i = 0; i < leftFields.Length; i++)
            {
                tblInfo.Controls.Add(MakeLabelKey(leftFields[i].Item1), 0, i);
                tblInfo.Controls.Add(
                    i == 5 ? MakeLabelValMultiLine(leftFields[i].Item2 ?? "\u2014")
                           : MakeLabelVal(leftFields[i].Item2 ?? "\u2014"),
                    1, i);
            }

            var rightFields = new[]
            {
                ("Customer",        _ship.CustomerName   ?? "\u2014"),
                ("Delivery Method", _ship.DeliveryMethod ?? "\u2014"),
                ("Shipment Type",   _ship.ShipmentType   ?? "\u2014"),
                ("Status",          _ship.ShipmentStatus ?? "\u2014"),
                ("Total Amount",    $"HK$ {_ship.TotalAmount:N2}"),
                ("",                ""),
            };
            for (int i = 0; i < rightFields.Length; i++)
            {
                tblInfo.Controls.Add(MakeLabelKey(rightFields[i].Item1), 2, i);
                tblInfo.Controls.Add(MakeLabelVal(rightFields[i].Item2 ?? "\u2014"), 3, i);
            }
            pnlInfo.Controls.Add(tblInfo);

            // ── Section label: EDIT FIELDS ───────────────────────────────────────
            var pnlEditLabel = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 40,
                BackColor = Color.FromArgb(246, 249, 255),
                Padding   = new Padding(28, 0, 0, 0)
            };
            pnlEditLabel.Controls.Add(new Label
            {
                Text      = "EDIT FIELDS",
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(29, 78, 216),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            });
            pnlEditLabel.Paint += PaintBottomBorderStatic;

            // ── Edit panel (fill) ────────────────────────────────────────────────
            var pnlEdit = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.White,
                Padding   = new Padding(28, 18, 28, 8)
            };

            // Each TLP row is Absolute 120px = CaptionH(56) + InputH(40) + 24px padding
            var tblEdit = new TableLayoutPanel
            {
                Dock            = DockStyle.Top,
                Height          = 120 * 2,   // 2 rows × 120px each
                ColumnCount     = 4,
                RowCount        = 2,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            for (int c = 0; c < 4; c++)
                tblEdit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblEdit.RowStyles.Add(new RowStyle(SizeType.Absolute, 120f));
            tblEdit.RowStyles.Add(new RowStyle(SizeType.Absolute, 120f));

            // Row 0: New Status | Tracking No.
            _cboStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 12f),
                Height        = InputH
            };
            _cboStatus.Items.AddRange(new object[] { "Pending", "In Transit", "Completed" });
            tblEdit.Controls.Add(MakeEditCell("New Status *", _cboStatus), 0, 0);

            _txtTracking = new TextBox
            {
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                PlaceholderText = "e.g. SF1234567890",
                Height          = InputH
            };
            tblEdit.Controls.Add(MakeEditCell("Tracking No.", _txtTracking), 1, 0);

            // Row 1: Actual Recipient | Remark
            _txtRecipient = new TextBox
            {
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                PlaceholderText = "Full name of recipient",
                Height          = InputH
            };
            tblEdit.Controls.Add(MakeEditCell("Actual Recipient", _txtRecipient), 0, 1);

            _txtRemark = new TextBox
            {
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                PlaceholderText = "Optional remark",
                Height          = InputH
            };
            tblEdit.Controls.Add(MakeEditCell("Remark", _txtRemark), 1, 1);

            pnlEdit.Controls.Add(tblEdit);

            // ── Total row (mirrors ShowDetailDialog) ─────────────────────────────
            var pnlTotalRow = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 64,
                BackColor = Color.White
            };
            pnlTotalRow.Paint += PaintTopBorderStatic;

            var tblTotals = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 2,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblTotals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblTotals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblTotals.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            tblTotals.Controls.Add(new Label
            {
                Text      = $"Shipment Lines:   {_detail.Lines.Count}",
                Dock      = DockStyle.Fill,
                AutoSize  = false,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(28, 0, 0, 0)
            }, 0, 0);

            tblTotals.Controls.Add(new Label
            {
                Text      = $"Total Amount:   HK$ {_ship.TotalAmount:N2}",
                Dock      = DockStyle.Fill,
                AutoSize  = false,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(47, 111, 237),
                TextAlign = ContentAlignment.MiddleRight,
                Padding   = new Padding(0, 0, 28, 0)
            }, 1, 0);
            pnlTotalRow.Controls.Add(tblTotals);

            // ── Footer ───────────────────────────────────────────────────────────
            var pnlFooter = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 86,
                BackColor = Color.White,
                Padding   = new Padding(28, 14, 28, 14)
            };
            pnlFooter.Paint += PaintTopBorderStatic;

            // Save Changes — blue, 210×56, right-anchored
            var btnSave = MakeBtn(
                "\u2714  Save Changes",
                Color.FromArgb(47, 111, 237),
                Color.FromArgb(26,  77, 192),
                Color.FromArgb(15,  55, 155),
                Color.White);
            btnSave.Anchor   = AnchorStyles.Right | AnchorStyles.Top;
            btnSave.Location = new Point(2500 - 28 - 210, 14);
            btnSave.Click   += BtnSave_Click;

            // Cancel — outline, 150×56, left of Save
            var btnCancel = MakeBtn(
                "Cancel",
                Color.White,
                Color.FromArgb(240, 244, 249),
                Color.FromArgb(220, 228, 240),
                Color.FromArgb(15,  31,  53));
            btnCancel.Size                              = new Size(150, 56);
            btnCancel.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnCancel.FlatAppearance.BorderSize         = 1;
            btnCancel.Anchor                            = AnchorStyles.Right | AnchorStyles.Top;
            btnCancel.Location                          = new Point(2500 - 28 - 210 - 8 - 150, 14);
            btnCancel.Click                            += (_, __) => { DialogResult = DialogResult.Cancel; Close(); };

            // Delete — red, 160×56, left-anchored
            var btnDelete = MakeBtn(
                "\uD83D\uDDD1  Delete",
                Color.FromArgb(185, 28, 28),
                Color.FromArgb(153, 27, 27),
                Color.FromArgb(120, 20, 20),
                Color.White);
            btnDelete.Size     = new Size(160, 56);
            btnDelete.Anchor   = AnchorStyles.Left | AnchorStyles.Top;
            btnDelete.Location = new Point(28, 14);
            btnDelete.Click   += BtnDelete_Click;

            pnlFooter.Controls.Add(btnSave);
            pnlFooter.Controls.Add(btnCancel);
            pnlFooter.Controls.Add(btnDelete);

            // ── Assemble — same order as ShowDetailDialog ────────────────────────
            Controls.Add(pnlEdit);
            Controls.Add(pnlTotalRow);
            Controls.Add(pnlEditLabel);
            Controls.Add(pnlFooter);
            Controls.Add(pnlInfo);
            Controls.Add(pnlHeader);
        }

        // =========================================================================
        //  Data population
        // =========================================================================
        private void PopulateFields()
        {
            int si = _cboStatus.FindStringExact(_ship.ShipmentStatus);
            _cboStatus.SelectedIndex = si >= 0 ? si : 0;

            _txtTracking.Text  = _ship.TrackingNumber              ?? string.Empty;
            _txtRecipient.Text = _detail.ReplySlip?.ActualRecipient ?? string.Empty;
            _txtRemark.Text    = _detail.ReplySlip?.RecipientRemark  ?? string.Empty;
        }

        // =========================================================================
        //  Save handler
        // =========================================================================
        private void BtnSave_Click(object sender, EventArgs e)
        {
            string newStatus = _cboStatus.SelectedItem?.ToString() ?? string.Empty;
            string tracking  = _txtTracking.Text.Trim();
            string recipient = _txtRecipient.Text.Trim();
            string remark    = _txtRemark.Text.Trim();

            if (string.IsNullOrEmpty(newStatus))
            {
                MessageBox.Show("Please select a status.",
                    "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _cboStatus.Focus();
                return;
            }

            try
            {
                _ctrl.UpdateShipment(_ship.ShipmentID, newStatus, recipient, remark);

                MessageBox.Show(
                    $"Shipment {_ship.ShipmentID} updated successfully.",
                    "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save changes:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =========================================================================
        //  Delete handler
        // =========================================================================
        private void BtnDelete_Click(object sender, EventArgs e)
        {
            var confirm = MessageBox.Show(
                $"Permanently delete Shipment {_ship.ShipmentID}\n" +
                $"({_ship.CustomerName})?\n\n" +
                "This will also remove all associated Delivery Notes and Reply Slips.",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            try
            {
                _ctrl.DeleteShipment(_ship.ShipmentID);

                MessageBox.Show(
                    $"Shipment {_ship.ShipmentID} has been deleted.",
                    "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =========================================================================
        //  UI helpers — exact copies from ViewShipmentForm for consistency
        // =========================================================================
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
            Text         = text,
            Font         = new Font("Segoe UI", 12f),
            ForeColor    = Color.FromArgb(15, 31, 53),
            Dock         = DockStyle.Fill,
            TextAlign    = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        private static Label MakeLabelValMultiLine(string text) => new Label
        {
            Text         = text,
            Font         = new Font("Segoe UI", 12f),
            ForeColor    = Color.FromArgb(15, 31, 53),
            Dock         = DockStyle.Fill,
            TextAlign    = ContentAlignment.TopLeft,
            AutoEllipsis = false,
            AutoSize     = false,
            Padding      = new Padding(0, 8, 8, 4)
        };

        /// <summary>
        /// Edit cell: caption label (56px, top-docked) + input control (bottom-docked).
        /// Using explicit heights instead of DockStyle.Fill on both prevents the input
        /// from obscuring the caption when TLP row height is limited.
        /// </summary>
        private static Panel MakeEditCell(string caption, Control ctrl)
        {
            var cell = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(0, 4, 14, 4)   // top/bottom 4px breathing room
            };

            var lbl = new Label
            {
                Text      = caption,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Top,
                Height    = CaptionH,                  // 56px — doubled from original 28px
                TextAlign = ContentAlignment.BottomLeft,
                Padding   = new Padding(0, 0, 0, 6)    // 6px gap between text and input
            };

            // Anchor input to bottom so it sits below the caption with natural height
            ctrl.Dock = DockStyle.Bottom;
            ctrl.Height = InputH;                      // 40px fixed; overrides Dock.Fill stretch

            cell.Controls.Add(ctrl);   // added first → bottom
            cell.Controls.Add(lbl);    // added second → top (WinForms reverse-z order)
            return cell;
        }

        private static Button MakeBtn(
            string text, Color bg, Color hover, Color down, Color fg)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = fg,
                BackColor = bg,
                FlatStyle = FlatStyle.Flat,
                Width     = 210,
                Height    = 56,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize         = 0;
            b.FlatAppearance.MouseOverBackColor = hover;
            b.FlatAppearance.MouseDownBackColor = down;
            return b;
        }

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
    }
}
