using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    /// <summary>
    /// Logistics Processing – View Shipment page.
    /// Follows the same MVC + CardPanel three-layer nesting pattern as Order Processing forms.
    /// Layout: UserBar (top) → TopNavBar → filter bar → shipment DataGridView (CardPanel layer 3)
    ///         → detail panel (CardPanel layer 3) showing lines + delivery note.
    /// </summary>
    public partial class ViewShipmentForm : Form
    {
        // ── Fields ─────────────────────────────────────────────────────
        private readonly LogisticsProcessingController _controller = new LogisticsProcessingController();
        private ViewShipmentVM _vm;

        // ── Constructor ──────────────────────────────────────────────────
        public ViewShipmentForm()
        {
            InitializeComponent();
            Load += ViewShipmentForm_Load;
        }

        // ── Load ──────────────────────────────────────────────────────────
        private void ViewShipmentForm_Load(object sender, EventArgs e)
        {
            RefreshData();
        }

        // ── RefreshData ──────────────────────────────────────────────────
        private void RefreshData(string status = null, string keyword = null, DateTime? from = null)
        {
            try
            {
                _vm = _controller.GetViewShipmentVM(status, keyword, from);

                // User bar
                lblUserName.Text   = _vm.UserBar.DisplayName;
                lblDepartment.Text = _vm.UserBar.Department;

                // Nav bar allowed menus
                topNavBar.SetAllowedMenus(_vm.AllowedMenus);

                // Bind grid
                BindShipmentGrid(_vm.Shipments);

                // Clear detail
                ClearDetail();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading shipments:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── BindShipmentGrid ───────────────────────────────────────────
        private void BindShipmentGrid(System.Collections.Generic.List<ShipmentEntity> data)
        {
            dgvShipments.Rows.Clear();
            foreach (var s in data)
            {
                int i = dgvShipments.Rows.Add(
                    s.ShipmentID,
                    s.OrderID,
                    s.CustomerName,
                    s.TrackingNumber,
                    s.ShipDate.ToString("yyyy-MM-dd"),
                    s.ShipmentStatus,
                    s.ShipmentType,
                    s.DeliveryMethod,
                    s.TotalAmount.ToString("C"));

                // Status badge colour
                var cell = dgvShipments.Rows[i].Cells["colStatus"];
                switch (s.ShipmentStatus)
                {
                    case "Completed":   cell.Style.ForeColor = Color.FromArgb(67, 122, 34);  break; // green
                    case "In Transit":  cell.Style.ForeColor = Color.FromArgb(0, 100, 148);  break; // blue
                    default:            cell.Style.ForeColor = Color.FromArgb(154, 66, 25);  break; // amber
                }
            }

            lblRecordCount.Text = $"{data.Count} record(s)";
        }

        // ── Row selection ────────────────────────────────────────────────
        private void dgvShipments_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvShipments.SelectedRows.Count == 0) { ClearDetail(); return; }

            string shipmentId = dgvShipments.SelectedRows[0].Cells["colShipmentID"].Value?.ToString();
            if (string.IsNullOrEmpty(shipmentId)) return;

            try
            {
                var detail = _controller.GetShipmentDetail(shipmentId);
                ShowDetail(detail);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading detail:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── ShowDetail ─────────────────────────────────────────────────────
        private void ShowDetail(ShipmentDetailVM d)
        {
            if (d.Shipment == null) { ClearDetail(); return; }

            lblDetailShipmentID.Text  = d.Shipment.ShipmentID;
            lblDetailOrderID.Text     = d.Shipment.OrderID;
            lblDetailCustomer.Text    = d.Shipment.CustomerName;
            lblDetailTracking.Text    = d.Shipment.TrackingNumber;
            lblDetailStatus.Text      = d.Shipment.ShipmentStatus;
            lblDetailType.Text        = d.Shipment.ShipmentType;
            lblDetailMethod.Text      = d.Shipment.DeliveryMethod;
            lblDetailShipDate.Text    = d.Shipment.ShipDate.ToString("yyyy-MM-dd");
            lblDetailAmount.Text      = d.Shipment.TotalAmount.ToString("C");
            lblDetailAddress.Text     = d.Shipment.ShippingAddress;

            // Delivery Note
            if (d.DeliveryNote != null)
            {
                lblDNID.Text       = d.DeliveryNote.DeliveryID;
                lblDNShipTo.Text   = d.DeliveryNote.ShipToName;
                lblDNDate.Text     = d.DeliveryNote.DeliveryDate.ToString("yyyy-MM-dd");
                lblDNOutQty.Text   = d.DeliveryNote.OutstandingQty?.ToString() ?? "0";
                pnlDeliveryNote.Visible = true;
            }
            else
            {
                pnlDeliveryNote.Visible = false;
            }

            // Lines
            dgvLines.Rows.Clear();
            foreach (var line in d.Lines)
            {
                dgvLines.Rows.Add(
                    line.ShipmentLineID,
                    line.ItemID,
                    line.ItemName,
                    line.QtyShipped,
                    line.QtyOutstanding?.ToString() ?? "0");
            }

            pnlDetail.Visible = true;
        }

        private void ClearDetail()
        {
            pnlDetail.Visible = false;
        }

        // ── Filter / Search ───────────────────────────────────────────────
        private void btnSearch_Click(object sender, EventArgs e)
        {
            string status  = cmbStatusFilter.SelectedIndex > 0 ? cmbStatusFilter.SelectedItem.ToString() : null;
            string keyword = txtSearch.Text.Trim();
            DateTime? from = dtpFrom.Checked ? dtpFrom.Value.Date : (DateTime?)null;
            RefreshData(status, string.IsNullOrEmpty(keyword) ? null : keyword, from);
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            cmbStatusFilter.SelectedIndex = 0;
            txtSearch.Clear();
            dtpFrom.Checked = false;
            RefreshData();
        }

        // ── Navigation (TopNavBar event) ─────────────────────────────────
        private void topNavBar_MenuItemClicked(object sender, MenuItemClickedEventArgs e)
        {
            FormNavigator.NavigateTo(this, e.MenuLabel, e.SubItem);
        }
    }
}
