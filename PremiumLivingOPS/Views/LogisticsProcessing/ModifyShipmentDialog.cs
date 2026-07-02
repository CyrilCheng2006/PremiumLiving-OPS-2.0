using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    // =====================================================================
    // ModifyShipmentDialog
    // ---------------------------------------------------------------------
    // Standalone dialog launched from the KPI-Bar "Modify Shipment" button
    // in ViewShipmentForm.  Replaces the retired ModifyShipmentForm (full-
    // page AppShell variant).
    //
    // Layout (mirrors CreateQuotationDialog design language):
    //
    //  ┌─ Deep-blue header 80 px ──────────────────────────────────────────┐
    //  │  Modify Shipment — SHP-XXXX                   [STATUS BADGE]      │
    //  ├─ Grey outer panel ────────────────────────────────────────────────┤
    //  │  ┌─ Layer 1 (CardPanel white) ──────────────────────────────────┐ │
    //  │  │  ┌─ Layer 2 (light-grey inner) ──────────────────────────┐   │ │
    //  │  │  │  ┌─ Layer 3 (content TLP) ──────────────────────────┐ │   │ │
    //  │  │  │  │  SHIPMENT INFORMATION  (section label)           │ │   │ │
    //  │  │  │  │  [ SHP ID ] [ Order ] [ Customer ] [ Ship Date ] │ │   │ │
    //  │  │  │  │  [ DlvDate] [ DlvMethod ] (2 cols)               │ │   │ │
    //  │  │  │  │  EDIT FIELDS  (section label)                    │ │   │ │
    //  │  │  │  │  [ Status* ] [ Tracking ] [ Recipient ] [ Note ] │ │   │ │
    //  │  │  │  └─────────────────────────────────────────────────┘ │   │ │
    //  │  │  └───────────────────────────────────────────────────────┘   │ │
    //  │  └──────────────────────────────────────────────────────────────┘ │
    //  ├─ White footer 80 px ───────────────────────────────────────────────┤
    //  │  [🗑 Delete]                     [Cancel]   [✔ Save Changes]       │
    //  └───────────────────────────────────────────────────────────────────┘
    // =====================================================================
    public class ModifyShipmentDialog : Form
    {
        private readonly LogisticsProcessingController _ctrl;
        private readonly ShipmentDetailVM              _detail;
        private readonly ShipmentEntity                _ship;

        // ── Editable controls ──────────────────────────────────────────
        private ComboBox _cboStatus;
        private TextBox  _txtTracking;
        private TextBox  _txtRecipient;
        private TextBox  _txtRemark;

        // ── Status colour palette (matches ViewShipmentForm) ───────────
        private static readonly System.Collections.Generic.Dictionary<string,(Color bg,Color fg)> StatusColors =
            new System.Collections.Generic.Dictionary<string,(Color,Color)>
            {
                { "Pending",    (Color.FromArgb(254,243,199), Color.FromArgb(146, 64, 14)) },
                { "In Transit", (Color.FromArgb(219,234,254), Color.FromArgb( 29, 78,216)) },
                { "Completed",  (Color.FromArgb(209,250,229), Color.FromArgb(  6, 95, 70)) },
            };

        // ==================================================================
        //  Constructor
        // ==================================================================
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

        // ==================================================================
        //  UI construction
        // ==================================================================
        private void BuildUI()
        {
            Text            = $"Modify Shipment \u2014 {_ship.ShipmentID}";
            Size            = new Size(1080, 600);
            MinimumSize     = new Size(960, 560);
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = Color.FromArgb(240, 244, 249);
            Font            = new Font("Segoe UI", 12f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;

            // Paint order: header(top) → body(fill) → footer(bottom)
            Controls.Add(BuildFooter());
            Controls.Add(BuildBody());
            Controls.Add(BuildHeader());
        }

        // ── Header ─────────────────────────────────────────────────────
        private Panel BuildHeader()
        {
            var pnl = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 80,
                BackColor = Color.FromArgb(19, 35, 61)
            };

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

        // ── Body — 3-layer CardPanel ────────────────────────────────────
        private Panel BuildBody()
        {
            // ── Layer 1: outer grey ──
            var outer = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding   = new Padding(20, 14, 20, 8)
            };

            // ── Layer 2: white card ──
            var card = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.White,
                Padding   = new Padding(12)
            };
            card.Paint += PaintCardBorder;

            // ── Layer 3: light-grey inner surface ──
            var inner = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(247, 249, 252),
                Padding   = new Padding(16, 12, 16, 12)
            };

            // ── Content TLP (4 cols, 5 rows) ──
            //   Row 0: section label READ-ONLY    (auto)
            //   Row 1: read-only fields row A     (percent 30)
            //   Row 2: read-only fields row B     (percent 20)
            //   Row 3: section label EDIT         (auto)
            //   Row 4: editable fields             (percent 50)
            var tbl = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 4,
                RowCount    = 5,
                BackColor   = Color.Transparent,
                Padding     = new Padding(4, 4, 4, 4)
            };
            for (int c = 0; c < 4; c++)
                tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));   // R0 label
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent,  35f));   // R1 ro-A
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent,  20f));   // R2 ro-B
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));   // R3 label
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent,  45f));   // R4 editable

            // ── Section label: READ-ONLY INFO ──
            var lblRo = MakeSectionLabel("SHIPMENT INFORMATION", Color.FromArgb(98, 112, 135));
            tbl.SetColumnSpan(lblRo, 4);
            tbl.Controls.Add(lblRo, 0, 0);

            // ── Row 1: 4 read-only fields (Shipment ID / Order ID / Customer / Ship Date) ──
            tbl.Controls.Add(MakeReadField("Shipment ID",
                _ship.ShipmentID ?? "\u2014"),                                            0, 1);
            tbl.Controls.Add(MakeReadField("Order ID",
                _ship.OrderID ?? "\u2014"),                                                1, 1);
            tbl.Controls.Add(MakeReadField("Customer",
                _ship.CustomerName ?? "\u2014"),                                           2, 1);
            tbl.Controls.Add(MakeReadField("Ship Date",
                _ship.ShipDate.ToString("yyyy-MM-dd")),                                    3, 1);

            // ── Row 2: 2 read-only fields (Delivery Date / Delivery Method) ──
            string dlvDate = _ship.DeliveryDate.HasValue
                ? _ship.DeliveryDate.Value.ToString("yyyy-MM-dd")
                : "\u2014";
            tbl.Controls.Add(MakeReadField("Scheduled Delivery Date", dlvDate),            0, 2);
            tbl.Controls.Add(MakeReadField("Delivery Method",
                _ship.DeliveryMethod ?? "\u2014"),                                          1, 2);
            tbl.Controls.Add(MakeReadField("Shipment Type",
                _ship.ShipmentType ?? "\u2014"),                                           2, 2);
            tbl.Controls.Add(MakeReadField("Total Amount",
                $"HK$ {_ship.TotalAmount:N2}"),                                            3, 2);

            // ── Section label: EDITABLE FIELDS ──
            var lblEd = MakeSectionLabel("EDIT FIELDS", Color.FromArgb(29, 78, 216));
            tbl.SetColumnSpan(lblEd, 4);
            tbl.Controls.Add(lblEd, 0, 3);

            // ── Row 4: 4 editable controls ──
            _cboStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 12f),
                Height        = 36
            };
            _cboStatus.Items.AddRange(new object[] { "Pending", "In Transit", "Completed" });
            tbl.Controls.Add(MakeEditCell("New Status *", _cboStatus), 0, 4);

            _txtTracking = new TextBox
            {
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                PlaceholderText = "e.g. SF1234567890"
            };
            tbl.Controls.Add(MakeEditCell("Tracking No.", _txtTracking), 1, 4);

            _txtRecipient = new TextBox
            {
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                PlaceholderText = "Full name of recipient"
            };
            tbl.Controls.Add(MakeEditCell("Actual Recipient", _txtRecipient), 2, 4);

            _txtRemark = new TextBox
            {
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                PlaceholderText = "Optional remark"
            };
            tbl.Controls.Add(MakeEditCell("Remark", _txtRemark), 3, 4);

            // ── Assemble layers ──
            inner.Controls.Add(tbl);
            card.Controls.Add(inner);
            outer.Controls.Add(card);
            return outer;
        }

        // ── Footer ─────────────────────────────────────────────────────
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

            // ── Save Changes — blue 210×56, anchored right ──
            var btnSave = MakeBtn(
                "\u2714  Save Changes",
                Color.FromArgb( 47, 111, 237),
                Color.FromArgb( 26,  77, 192),
                Color.FromArgb( 15,  55, 155),
                Color.White);
            btnSave.Anchor   = AnchorStyles.Right | AnchorStyles.Top;
            btnSave.Location = new Point(pnl.Width - 20 - 210, 12);
            btnSave.Click   += BtnSave_Click;

            // ── Cancel — outline 130×56, left of Save ──
            var btnCancel = MakeBtn(
                "Cancel",
                Color.White,
                Color.FromArgb(240, 244, 249),
                Color.FromArgb(220, 228, 240),
                Color.FromArgb( 15,  31,  53));
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            btnCancel.FlatAppearance.BorderSize  = 1;
            btnCancel.Width    = 130;
            btnCancel.Anchor   = AnchorStyles.Right | AnchorStyles.Top;
            btnCancel.Location = new Point(pnl.Width - 20 - 210 - 8 - 130, 12);
            btnCancel.Click   += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            // ── Delete — red 160×56, anchored left ──
            var btnDelete = MakeBtn(
                "\uD83D\uDDD1  Delete",
                Color.FromArgb(185,  28,  28),
                Color.FromArgb(153,  27,  27),
                Color.FromArgb(120,  20,  20),
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

        // ==================================================================
        //  Data population
        // ==================================================================
        private void PopulateFields()
        {
            // Status combo — select current status
            int si = _cboStatus.FindStringExact(_ship.ShipmentStatus);
            _cboStatus.SelectedIndex = si >= 0 ? si : 0;

            // Tracking number from Shipment entity
            _txtTracking.Text = _ship.TrackingNumber ?? string.Empty;

            // Actual recipient & remark from ReplySlip (may be null)
            _txtRecipient.Text = _detail.ReplySlip?.ActualRecipient ?? string.Empty;
            _txtRemark.Text    = _detail.ReplySlip?.RecipientRemark  ?? string.Empty;
        }

        // ==================================================================
        //  Save handler
        // ==================================================================
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

        // ==================================================================
        //  Delete handler
        // ==================================================================
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

        // ==================================================================
        //  UI helpers
        // ==================================================================

        /// <summary>Section divider label (upper-case, small, coloured).</summary>
        private static Label MakeSectionLabel(string text, Color fg) => new Label
        {
            Text      = text,
            Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = fg,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft
        };

        /// <summary>Read-only field: caption above, value below.</summary>
        private static Panel MakeReadField(string caption, string value)
        {
            var cell = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(0, 0, 14, 0)
            };
            var valLbl = new Label
            {
                Text         = value,
                Font         = new Font("Segoe UI", 12f),
                ForeColor    = Color.FromArgb(15, 31, 53),
                Dock         = DockStyle.Fill,
                TextAlign    = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
            var capLbl = new Label
            {
                Text      = caption,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Top,
                Height    = 22,
                TextAlign = ContentAlignment.BottomLeft
            };
            cell.Controls.Add(valLbl);
            cell.Controls.Add(capLbl);
            return cell;
        }

        /// <summary>Editable field cell: caption label + control stacked.</summary>
        private static Panel MakeEditCell(string caption, Control ctrl)
        {
            var cell = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(0, 0, 14, 0)
            };
            ctrl.Dock = DockStyle.Fill;
            var capLbl = new Label
            {
                Text      = caption,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Top,
                Height    = 22,
                TextAlign = ContentAlignment.BottomLeft
            };
            cell.Controls.Add(ctrl);
            cell.Controls.Add(capLbl);
            return cell;
        }

        /// <summary>Standard flat button factory (width 210, height 56 by default).</summary>
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

        private static void PaintCardBorder(object s, PaintEventArgs e)
        {
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            var rc = ((Control)s).ClientRectangle;
            rc.Width--;  rc.Height--;
            e.Graphics.DrawRectangle(pen, rc);
        }

        private static void PaintTopBorder(object s, PaintEventArgs e)
        {
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, 0, ((Control)s).Width, 0);
        }
    }
}
