using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    /// <summary>
    /// Generate Delivery Note — standalone Form
    ///
    /// MVC contract
    /// ─────────────────────────────────────────────────────────────────
    /// • Receives a pre-loaded ShipmentDetailVM from the caller (ViewShipmentForm).
    /// • Displays a read-only preview of the Delivery Note to be generated:
    ///     – Shipment info (4-col TLP): ShipmentID, OrderID, ShipDate, Status
    ///     – Delivery Note info: DeliveryDate (= ShipDate), ShipToName (= CustomerName),
    ///       ShippingAddress, Outstanding Qty (= sum QtyOutstanding)
    ///     – ShipmentLine items grid (read-only)
    /// • Two action buttons: ✔ Confirm Generate  |  ✕ Cancel
    /// • On Confirm: calls LogisticsProcessingController.GenerateDeliveryNote()
    ///   then shows success message and closes with DialogResult.OK.
    /// • Blocked (Confirm disabled + warning label) if a Delivery Note already exists.
    /// • CardPanel three-layer nesting on all content blocks.
    /// • Size: 1200 × 780, StartPosition CenterParent.
    /// </summary>
    public partial class GenerateDeliveryNoteForm : Form
    {
        private readonly LogisticsProcessingController _ctrl =
            new LogisticsProcessingController();
        private readonly ShipmentDetailVM _vm;

        public string GeneratedDeliveryID { get; private set; }

        public GenerateDeliveryNoteForm(ShipmentDetailVM vm)
        {
            _vm = vm ?? throw new ArgumentNullException(nameof(vm));
            InitializeComponent();
            this.Load += GenerateDeliveryNoteForm_Load;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Load
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void GenerateDeliveryNoteForm_Load(object sender, EventArgs e)
        {
            PopulateShipmentInfo();
            PopulateDeliveryNotePreview();
            PopulateItemsGrid();
            ApplyBlockGuard();
        }

        // ── Shipment Info ─────────────────────────────────────────────
        private void PopulateShipmentInfo()
        {
            var s = _vm.Shipment;
            if (s == null) return;

            lblShipmentID.Text    = s.ShipmentID;
            lblOrderID.Text       = s.OrderID;
            lblShipDate.Text      = s.ShipDate.ToString("yyyy-MM-dd");
            lblShipStatus.Text    = s.ShipmentStatus;

            // Status colour
            ApplyStatusChip(lblShipStatus, s.ShipmentStatus);
        }

        // ── Delivery Note Preview ─────────────────────────────────────
        private void PopulateDeliveryNotePreview()
        {
            var s = _vm.Shipment;
            if (s == null) return;

            // Calculate Outstanding Qty
            int outQty = 0;
            foreach (var line in _vm.Lines ?? new List<ShipmentLineEntity>())
                outQty += line.QtyOutstanding ?? 0;

            lblDeliveryDate.Text      = s.ShipDate.ToString("yyyy-MM-dd");
            lblShipToName.Text        = s.CustomerName;
            lblShippingAddress.Text   = s.ShippingAddress;
            lblOutstandingQty.Text    = outQty.ToString();
        }

        // ── Items Grid ────────────────────────────────────────────────
        private void PopulateItemsGrid()
        {
            dgvLines.Rows.Clear();
            foreach (var line in _vm.Lines ?? new List<ShipmentLineEntity>())
                dgvLines.Rows.Add(
                    line.ShipmentLineID,
                    line.ItemID,
                    line.ItemName,
                    line.QtyShipped,
                    line.QtyOutstanding ?? 0);
        }

        // ── Block Guard ───────────────────────────────────────────────
        private void ApplyBlockGuard()
        {
            if (_vm.DeliveryNote != null)
            {
                btnConfirm.Enabled        = false;
                pnlAlreadyExists.Visible  = true;
                lblExistingDN.Text        =
                    $"A Delivery Note already exists: {_vm.DeliveryNote.DeliveryID}  "
                    + $"(Date: {_vm.DeliveryNote.DeliveryDate:yyyy-MM-dd})";
            }
            else
            {
                btnConfirm.Enabled       = true;
                pnlAlreadyExists.Visible = false;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Helpers
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private static void ApplyStatusChip(Label lbl, string status)
        {
            switch (status)
            {
                case "Pending":
                    lbl.BackColor = Color.FromArgb(254, 243, 199);
                    lbl.ForeColor = Color.FromArgb(146, 64, 14); break;
                case "In Transit":
                    lbl.BackColor = Color.FromArgb(219, 234, 254);
                    lbl.ForeColor = Color.FromArgb(29, 78, 216); break;
                case "Completed":
                    lbl.BackColor = Color.FromArgb(209, 250, 229);
                    lbl.ForeColor = Color.FromArgb(6, 95, 70); break;
                default:
                    lbl.BackColor = Color.FromArgb(243, 244, 246);
                    lbl.ForeColor = Color.FromArgb(75, 85, 99); break;
            }
            lbl.AutoSize    = false;
            lbl.TextAlign   = ContentAlignment.MiddleCenter;
            lbl.Padding     = new Padding(6, 2, 6, 2);
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.Left,           r.Top,            d, d, 180, 90);
            path.AddArc(r.Right - d,      r.Top,            d, d, 270, 90);
            path.AddArc(r.Right - d,      r.Bottom - d,     d, d,   0, 90);
            path.AddArc(r.Left,           r.Bottom - d,     d, d,  90, 90);
            path.CloseFigure();
            return path;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Button Handlers
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void btnConfirm_Click(object sender, EventArgs e)
        {
            try
            {
                btnConfirm.Enabled = false;
                GeneratedDeliveryID = _ctrl.GenerateDeliveryNote(_vm.Shipment.ShipmentID);

                MessageBox.Show(
                    $"Delivery Note generated successfully!\n\nDelivery Note ID: {GeneratedDeliveryID}",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                btnConfirm.Enabled = true;
                MessageBox.Show(
                    ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
