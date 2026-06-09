using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    /// <summary>
    /// Modify Shipment — View layer (MVC).
    ///
    /// Responsibilities:
    ///   • Edit Shipment  — update ShipmentStatus + ActualRecipient (+ optional Remark).
    ///   • Delete Shipment — permanently remove shipment and all child rows.
    ///
    /// MVC contract:
    ///   • ALL DB access delegated to LogisticsProcessingController (zero SQL here).
    ///   • AppShell events subscribed ONCE in Designer.cs (RULE 4). Load() does NOT re-subscribe.
    ///   • ComboItem is a private inner class (same pattern as ModifyOrderForm).
    /// </summary>
    public partial class ModifyShipmentForm : Form
    {
        // ---- Static entry-point (set by caller before opening this form) ----
        public static string PendingShipmentId { get; set; } = null;

        private readonly LogisticsProcessingController _ctrl =
            new LogisticsProcessingController();

        private ShipmentEntity _currentShipment;

        public ModifyShipmentForm()
        {
            InitializeComponent();
            this.Load += ModifyShipmentForm_Load;
        }

        // ====================================================================
        //  Form Load
        // ====================================================================
        private void ModifyShipmentForm_Load(object sender, EventArgs e)
        {
            // NOTE: MenuItemClicked and LogoutClicked are wired in Designer.cs (RULE 4).
            //       Do NOT subscribe here to avoid duplicate firing.
            var vm = _ctrl.GetViewShipmentVM();
            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Logistics Processing  ›  Modify Shipment");

            ReloadShipmentCombo();

            if (!string.IsNullOrEmpty(PendingShipmentId))
            {
                SelectAndLoadShipment(PendingShipmentId);
                PendingShipmentId = null;
            }
        }

        // ====================================================================
        //  Combo helpers
        // ====================================================================
        private void ReloadShipmentCombo()
        {
            cboSearchShipment.Items.Clear();
            cboSearchShipment.Items.Add(new ComboItem("-- Select Shipment --", ""));

            var list = _ctrl.GetViewShipmentVM().Shipments;
            foreach (var s in list)
                cboSearchShipment.Items.Add(
                    new ComboItem(
                        $"{s.ShipmentID}  –  {s.CustomerName}  [{s.ShipmentStatus}]",
                        s.ShipmentID));

            cboSearchShipment.SelectedIndex = 0;
        }

        // ====================================================================
        //  Load Shipment button
        // ====================================================================
        private void btnLoadShipment_Click(object sender, EventArgs e)
        {
            var sel = cboSearchShipment.SelectedItem as ComboItem;
            if (sel == null || string.IsNullOrEmpty(sel.Value))
            {
                MessageBox.Show("Please select a shipment to load.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            SelectAndLoadShipment(sel.Value);
        }

        private void SelectAndLoadShipment(string shipmentId)
        {
            var detail = _ctrl.GetShipmentDetail(shipmentId);
            _currentShipment = detail?.Shipment;

            if (_currentShipment == null)
            {
                MessageBox.Show($"Shipment '{shipmentId}' not found.",
                    "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Sync combo selection
            for (int i = 0; i < cboSearchShipment.Items.Count; i++)
                if (cboSearchShipment.Items[i] is ComboItem ci && ci.Value == shipmentId)
                { cboSearchShipment.SelectedIndex = i; break; }

            // Populate read-only info labels
            lblShipmentIdValue.Text     = _currentShipment.ShipmentID;
            lblOrderIdValue.Text        = _currentShipment.OrderID;
            lblCustomerValue.Text       = _currentShipment.CustomerName;
            lblTrackingValue.Text       = _currentShipment.TrackingNumber;
            lblShipDateValue.Text       = _currentShipment.ShipDate.ToString("yyyy-MM-dd");
            lblShipTypeValue.Text       = _currentShipment.ShipmentType;
            lblDeliveryMethodValue.Text = _currentShipment.DeliveryMethod;

            // Editable: status
            int si = cboStatus.FindStringExact(_currentShipment.ShipmentStatus);
            cboStatus.SelectedIndex = si >= 0 ? si : 0;

            // Editable: actual recipient + remark from ReplySlip
            txtActualRecipient.Text = detail.ReplySlip?.ActualRecipient ?? string.Empty;
            txtRemark.Text          = detail.ReplySlip?.RecipientRemark  ?? string.Empty;

            // Enable action buttons
            btnSaveChanges.Enabled    = true;
            btnDeleteShipment.Enabled = true;
            btnDiscardChanges.Enabled = true;
        }

        // ====================================================================
        //  Save Changes (Edit Shipment)
        // ====================================================================
        private void btnSaveChanges_Click(object sender, EventArgs e)
        {
            if (_currentShipment == null)
            {
                MessageBox.Show("Please load a shipment first.",
                    "No Shipment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string newStatus       = cboStatus.SelectedItem?.ToString() ?? string.Empty;
            string actualRecipient = txtActualRecipient.Text.Trim();
            string remark          = txtRemark.Text.Trim();

            if (string.IsNullOrEmpty(newStatus))
            {
                MessageBox.Show("Please select a status.",
                    "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _ctrl.UpdateShipment(_currentShipment.ShipmentID,
                                     newStatus, actualRecipient, remark);

                MessageBox.Show(
                    $"Shipment {_currentShipment.ShipmentID} updated successfully.",
                    "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);

                SelectAndLoadShipment(_currentShipment.ShipmentID);
                ReloadShipmentCombo();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save changes:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ====================================================================
        //  Delete Shipment
        // ====================================================================
        private void btnDeleteShipment_Click(object sender, EventArgs e)
        {
            if (_currentShipment == null)
            {
                MessageBox.Show("Please load a shipment first.",
                    "No Shipment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Are you sure you want to permanently delete shipment\n" +
                $"{_currentShipment.ShipmentID} ({_currentShipment.CustomerName})?\n\n" +
                "This will also delete all associated Delivery Notes and Reply Slips.",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            try
            {
                string deletedId = _currentShipment.ShipmentID;
                _ctrl.DeleteShipment(deletedId);

                MessageBox.Show(
                    $"Shipment {deletedId} has been deleted.",
                    "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);

                ClearForm();
                ReloadShipmentCombo();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete shipment:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ====================================================================
        //  Discard Changes
        // ====================================================================
        private void btnDiscardChanges_Click(object sender, EventArgs e)
        {
            if (_currentShipment != null)
                SelectAndLoadShipment(_currentShipment.ShipmentID);
        }

        // ====================================================================
        //  Clear form
        // ====================================================================
        private void ClearForm()
        {
            _currentShipment = null;

            lblShipmentIdValue.Text     = "—";
            lblOrderIdValue.Text        = "—";
            lblCustomerValue.Text       = "—";
            lblTrackingValue.Text       = "—";
            lblShipDateValue.Text       = "—";
            lblShipTypeValue.Text       = "—";
            lblDeliveryMethodValue.Text = "—";

            cboStatus.SelectedIndex     = 0;
            txtActualRecipient.Text     = string.Empty;
            txtRemark.Text              = string.Empty;

            btnSaveChanges.Enabled    = false;
            btnDeleteShipment.Enabled = false;
            btnDiscardChanges.Enabled = false;
        }

        // ====================================================================
        //  Nav / Logout  (handlers wired in Designer.cs RULE 4)
        // ====================================================================
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }

        // ====================================================================
        //  ComboItem — private inner class (same pattern as ModifyOrderForm)
        // ====================================================================
        private class ComboItem
        {
            public string Text  { get; }
            public string Value { get; }
            public ComboItem(string text, string value) { Text = text; Value = value; }
            public override string ToString() => Text;
        }
    }
}
