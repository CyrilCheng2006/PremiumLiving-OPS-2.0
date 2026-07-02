using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    // =====================================================================
    // ModifyShipmentDialog
    // ---------------------------------------------------------------------
    // Independent dialog (no AppShell) that lets the operator edit or delete
    // an existing Shipment.  Design language mirrors CreateQuotationDialog:
    //
    //   ┌─ Deep-blue header (80 px) ────────────────────────────────────┐
    //   │  Modify Shipment — SHP-XXXX              [ STATUS BADGE ]     │
    //   ├─ Outer grey panel ────────────────────────────────────────────┤
    //   │  ┌─ White card (CardPanel 3-layer) ─────────────────────────┐ │
    //   │  │  Row 1 (read-only)  : Shipment ID | Order ID | Customer  │ │
    //   │  │                       Ship Date   | Delivery Method      │ │
    //   │  │  Row 2 (editable)   : New Status  | Tracking No.        │ │
    //   │  │                       Actual Recipient | Remark         │ │
    //   │  └──────────────────────────────────────────────────────────┘ │
    //   ├─ White footer (80 px) ────────────────────────────────────────┤
    //   │  [🗑 Delete]                  [Cancel]  [✔ Save Changes]      │
    //   └───────────────────────────────────────────────────────────────┘
    // =====================================================================
    public class ModifyShipmentDialog : Form
    {
        private readonly LogisticsProcessingController _ctrl;
        private readonly ShipmentDetailVM              _detail;
        private readonly ShipmentEntity                _ship;

        // Editable controls
        private ComboBox _cboStatus;
        private TextBox  _txtTracking;
        private TextBox  _txtRecipient;
        private TextBox  _txtRemark;

        // Status palette (same as ViewShipmentForm)
        private static readonly System.Collections.Generic.Dictionary<string, (Color bg, Color fg)> StatusColors =
            new System.Collections.Generic.Dictionary<string, (Color, Color)>
            {
                { "Pending",    (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "In Transit", (Color.FromArgb(219, 234, 254), Color.FromArgb( 29,  78, 216)) },
                { "Completed",  (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
            };

        // ---------------------------------------------------------------
        public ModifyShipmentDialog(
            LogisticsProcessingController ctrl,
            ShipmentDetailVM detail)
        {
            _ctrl   = ctrl   ?? throw new ArgumentNullException(nameof(ctrl));
            _detail = detail ?? throw new ArgumentNullException(nameof(detail));
            _ship   = detail.Shipment;

            BuildUI();
            PopulateFields();
        }

        // ===============================================================
        //  UI Construction
        // ===============================================================
        private void BuildUI()
        {
            Text            = $"Modify Shipment \u2014 {_ship.ShipmentID}";
            Size            = new Size(1080, 560);
            MinimumSize     = new Size(960, 520);
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = Color.FromArgb(240, 244, 249);
            Font            = new Font("Segoe UI", 12f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;

            Controls.Add(BuildFooter());
            Controls.Add(BuildBody());
            Controls.Add(BuildHeader());
        }

        // ── Header ──────────────────────────────────────────────────────
        private Panel BuildHeader()
        {
            var pnl = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };

            var tbl = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 2,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(28, 0, 20, 0)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            tbl.Controls.Add(new Label
            {
                Text      = $"Modify Shipment  \u2014  {_ship.ShipmentID}",
                Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false
            }, 0, 0);

            StatusColors.TryGetValue(_ship.ShipmentStatus ?? string.Empty, out var sc);
            var badge = new Label
            {
                Text      = _ship.ShipmentStatus ?? "Unknown",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = sc.fg != default ? sc.fg : Color.White,
                BackColor = sc.bg != default ? sc.bg : Color.FromArgb(80, 80, 80),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize  = false
            };
            tbl.Controls.Add(badge, 1, 0);

            pnl.Controls.Add(tbl);
            return pnl;
        }

        // ── Body  (3-layer CardPanel) ────────────────────────────────────
        private Panel BuildBody()
        {
            // Outer grey layer
            var outer = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding   = new Padding(20, 14, 20, 8)
            };

            // Middle white card
            var card = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.White
            };
            card.Paint += PaintCardBorder;

            // Inner content TLP
            var tbl = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 4,
                RowCount    = 4,
                BackColor   = Color.Transparent,
                Padding     = new Padding(28, 20, 28, 12)
            };
            for (int c = 0; c < 4; c++)
                tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));  // row 0: section label READ-ONLY
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent,  40f));  // row 1: read-only fields
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));  // row 2: section label EDITABLE
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent,  60f));  // row 3: editable fields

            // ── Section label: READ-ONLY INFO ──
            var lblRo = new Label
            {
                Text      = "SHIPMENT INFORMATION",
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft
            };
            tbl.SetColumnSpan(lblRo, 4);
            tbl.Controls.Add(lblRo, 0, 0);

            // ── Row 1: read-only 4-column grid ──
            // Col 0  Shipment ID
            tbl.Controls.Add(MakeField("Shipment ID",    _ship.ShipmentID),  0, 1);
            // Col 1  Order ID
            tbl.Controls.Add(MakeField("Order ID",       _ship.OrderID),     1, 1);
            // Col 2  Customer
            tbl.Controls.Add(MakeField("Customer",       _ship.CustomerName), 2, 1);
            // Col 3  Ship Date
            tbl.Controls.Add(MakeField("Ship Date",
                _ship.ShipDate.ToString("yyyy-MM-dd")),                        3, 1);

            // ── Section label: EDITABLE FIELDS ──
            var lblEd = new Label
            {
                Text      = "EDIT FIELDS",
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(29, 78, 216),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft
            };
            tbl.SetColumnSpan(lblEd, 4);
            tbl.Controls.Add(lblEd, 0, 2);

            // ── Row 3: editable controls ──
            // Status
            _cboStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 12f),
                Dock          = DockStyle.Top,
                Height        = 36
            };
            _cboStatus.Items.AddRange(new object[] { "Pending", "In Transit", "Completed" });
            tbl.Controls.Add(MakeEditCell("New Status *", _cboStatus), 0, 3);

            // Tracking No.
            _txtTracking = new TextBox
            {
                Font        = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle
            };
            tbl.Controls.Add(MakeEditCell("Tracking No.", _txtTracking), 1, 3);

            // Actual Recipient
            _txtRecipient = new TextBox
            {
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                PlaceholderText = "Full name of recipient"
            };
            tbl.Controls.Add(MakeEditCell("Actual Recipient", _txtRecipient), 2, 3);

            // Remark
            _txtRemark = new TextBox
            {
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                PlaceholderText = "Optional remark"
            };
            tbl.Controls.Add(MakeEditCell("Remark", _txtRemark), 3, 3);

            card.Controls.Add(tbl);
            outer.Controls.Add(card);
            return outer;
        }

        // ── Footer ──────────────────────────────────────────────────────
        private Panel BuildFooter()
        {
            var pnl = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 80,
                BackColor = Color.White,
                Padding   = new Padding(20, 12, 20, 12)
            };
            pnl.Paint += PaintTopBorder;

            // Save — blue, 210×56, anchored right
            var btnSave = MakeBtn("\u2714  Save Changes",
                Color.FromArgb(47, 111, 237),
                Color.FromArgb(26,  77, 192),
                Color.FromArgb(15,  55, 155),
                Color.White);
            btnSave.Anchor   = AnchorStyles.Right | AnchorStyles.Top;
            btnSave.Location = new Point(pnl.Width - 20 - 210, 12);
            btnSave.Click   += BtnSave_Click;

            // Cancel — outline, 130×56
            var btnCancel = MakeBtn("Cancel",
                Color.White,
                Color.FromArgb(240, 244, 249),
                Color.FromArgb(220, 228, 240),
                Color.FromArgb(15,  31,  53));
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            btnCancel.FlatAppearance.BorderSize  = 1;
            btnCancel.Width    = 130;
            btnCancel.Anchor   = AnchorStyles.Right | AnchorStyles.Top;
            btnCancel.Location = new Point(pnl.Width - 20 - 210 - 8 - 130, 12);
            btnCancel.Click   += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            // Delete — red, 160×56, anchored left
            var btnDelete = MakeBtn("\uD83D\uDDD1  Delete",
                Color.FromArgb(185, 28, 28),
                Color.FromArgb(153, 27, 27),
                Color.FromArgb(120, 20, 20),
                Color.White);
            btnDelete.Width    = 160;
            btnDelete.Anchor   = AnchorStyles.Left | AnchorStyles.Top;
            btnDelete.Location = new Point(20, 12);
            btnDelete.Click   += BtnDelete_Click;

            pnl.Controls.Add(btnSave);
            pnl.Controls.Add(btnCancel);
            pnl.Controls.Add(btnDelete);
            return pnl;
        }

        // ===============================================================
        //  Populate fields from loaded detail
        // ===============================================================
        private void PopulateFields()
        {
            // Status combo
            int si = _cboStatus.FindStringExact(_ship.ShipmentStatus);
            _cboStatus.SelectedIndex = si >= 0 ? si : 0;

            // Tracking number
            _txtTracking.Text = _ship.TrackingNumber ?? string.Empty;

            // Actual recipient + remark from ReplySlip if available
            _txtRecipient.Text = _detail.ReplySlip?.ActualRecipient ?? string.Empty;
            _txtRemark.Text    = _detail.ReplySlip?.RecipientRemark  ?? string.Empty;
        }

        // ===============================================================
        //  Save handler
        // ===============================================================
        private void BtnSave_Click(object sender, EventArgs e)
        {
            string newStatus  = _cboStatus.SelectedItem?.ToString() ?? string.Empty;
            string tracking   = _txtTracking.Text.Trim();
            string recipient  = _txtRecipient.Text.Trim();
            string remark     = _txtRemark.Text.Trim();

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

        // ===============================================================
        //  Delete handler
        // ===============================================================
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

        // ===============================================================
        //  UI helpers
        // ===============================================================
        private static Panel MakeField(string caption, string value)
        {
            var cell = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 0, 16, 0) };
            cell.Controls.Add(new Label
            {
                Text      = value ?? "\u2014",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            });
            cell.Controls.Add(new Label
            {
                Text      = caption,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Top,
                Height    = 22,
                TextAlign = ContentAlignment.BottomLeft
            });
            return cell;
        }

        private static Panel MakeEditCell(string caption, Control ctrl)
        {
            var cell = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 0, 16, 0) };
            ctrl.Dock = DockStyle.Fill;
            cell.Controls.Add(ctrl);
            cell.Controls.Add(new Label
            {
                Text      = caption,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Top,
                Height    = 22,
                TextAlign = ContentAlignment.BottomLeft
            });
            return cell;
        }

        private static Button MakeBtn(string text, Color bg, Color hover, Color down, Color fg)
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
            b.FlatAppearance.BorderSize        = 0;
            b.FlatAppearance.MouseOverBackColor = hover;
            b.FlatAppearance.MouseDownBackColor = down;
            return b;
        }

        private static void PaintCardBorder(object s, PaintEventArgs e)
        {
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            var rc = ((Control)s).ClientRectangle;
            rc.Width--; rc.Height--;
            e.Graphics.DrawRectangle(pen, rc);
        }

        private static void PaintTopBorder(object s, PaintEventArgs e)
        {
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, 0, ((Control)s).Width, 0);
        }
    }
}
