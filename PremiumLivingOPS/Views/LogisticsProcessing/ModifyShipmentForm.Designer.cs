namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    partial class ModifyShipmentForm
    {
        private System.ComponentModel.IContainer components = null;

        // ---- AppShell (mandatory shared component) ----------
        private PremiumLivingOPS.Views.Shared.AppShell _shell;

        // ---- Search row -----
        private System.Windows.Forms.ComboBox cboSearchShipment;
        private System.Windows.Forms.Button   btnLoadShipment;

        // ---- Info Labels ----
        private System.Windows.Forms.Label lblShipmentIdValue;
        private System.Windows.Forms.Label lblOrderIdValue;
        private System.Windows.Forms.Label lblCustomerValue;
        private System.Windows.Forms.Label lblTrackingValue;
        private System.Windows.Forms.Label lblShipDateValue;
        private System.Windows.Forms.Label lblShipTypeValue;
        private System.Windows.Forms.Label lblDeliveryMethodValue;

        // ---- Editable fields ----
        private System.Windows.Forms.ComboBox cboStatus;
        private System.Windows.Forms.TextBox  txtActualRecipient;
        private System.Windows.Forms.TextBox  txtRemark;

        // ---- Action buttons ----
        private System.Windows.Forms.Button btnSaveChanges;
        private System.Windows.Forms.Button btnDeleteShipment;
        private System.Windows.Forms.Button btnDiscardChanges;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            // ---- AppShell ----
            _shell = new PremiumLivingOPS.Views.Shared.AppShell();
            _shell.Dock = System.Windows.Forms.DockStyle.Fill;

            // ===========================================================
            //  OUTER card (grey page background)
            // ===========================================================
            var outerCard = new PremiumLivingOPS.Views.Shared.CardPanel();
            outerCard.Dock    = System.Windows.Forms.DockStyle.Fill;
            outerCard.Padding = new System.Windows.Forms.Padding(20);
            outerCard.BackColor = System.Drawing.Color.FromArgb(242, 242, 242);

            // ===========================================================
            //  MIDDLE card (white, floating)
            // ===========================================================
            var middleCard = new PremiumLivingOPS.Views.Shared.CardPanel();
            middleCard.Dock    = System.Windows.Forms.DockStyle.Fill;
            middleCard.Padding = new System.Windows.Forms.Padding(20);
            middleCard.BackColor = System.Drawing.Color.White;

            // ===========================================================
            //  INNER content
            // ===========================================================
            var innerPanel = new System.Windows.Forms.Panel();
            innerPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            innerPanel.BackColor = System.Drawing.Color.White;

            // ---- Page title ----
            var lblTitle = new System.Windows.Forms.Label();
            lblTitle.Text      = "Modify Shipment";
            lblTitle.Font      = new System.Drawing.Font("Segoe UI", 16f, System.Drawing.FontStyle.Bold);
            lblTitle.AutoSize  = true;
            lblTitle.Location  = new System.Drawing.Point(0, 0);
            lblTitle.ForeColor = System.Drawing.Color.FromArgb(30, 30, 30);

            // ---- Search row ----
            var lblSearch = MakeLbl("Select Shipment:");
            lblSearch.Location = new System.Drawing.Point(0, 50);

            cboSearchShipment = new System.Windows.Forms.ComboBox();
            cboSearchShipment.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboSearchShipment.Font   = new System.Drawing.Font("Segoe UI", 10f);
            cboSearchShipment.Size   = new System.Drawing.Size(420, 28);
            cboSearchShipment.Location = new System.Drawing.Point(140, 47);

            btnLoadShipment = new System.Windows.Forms.Button();
            btnLoadShipment.Text      = "Load";
            btnLoadShipment.Font      = new System.Drawing.Font("Segoe UI", 10f);
            btnLoadShipment.Size      = new System.Drawing.Size(90, 30);
            btnLoadShipment.Location  = new System.Drawing.Point(570, 47);
            btnLoadShipment.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            btnLoadShipment.ForeColor = System.Drawing.Color.White;
            btnLoadShipment.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnLoadShipment.Click    += btnLoadShipment_Click;

            // ---- Read-only info section ----
            int row = 100;
            int rowH = 36;

            var fields = new[]
            {
                ("Shipment ID:",     ref lblShipmentIdValue),
                ("Order ID:",        ref lblOrderIdValue),
                ("Customer:",        ref lblCustomerValue),
                ("Tracking No.:",    ref lblTrackingValue),
                ("Ship Date:",       ref lblShipDateValue),
                ("Type:",            ref lblShipTypeValue),
                ("Delivery Method:", ref lblDeliveryMethodValue),
            };

            innerPanel.Controls.Add(lblTitle);
            innerPanel.Controls.Add(lblSearch);
            innerPanel.Controls.Add(cboSearchShipment);
            innerPanel.Controls.Add(btnLoadShipment);

            foreach (var (caption, valueRef) in fields)
            {
                var lbl = MakeLbl(caption);
                lbl.Location = new System.Drawing.Point(0, row);

                var val = new System.Windows.Forms.Label();
                val.Text      = "—";
                val.Font      = new System.Drawing.Font("Segoe UI", 10f);
                val.ForeColor = System.Drawing.Color.FromArgb(60, 60, 60);
                val.AutoSize  = true;
                val.Location  = new System.Drawing.Point(180, row + 2);
                valueRef = val;

                innerPanel.Controls.Add(lbl);
                innerPanel.Controls.Add(val);
                row += rowH;
            }

            // ---- Editable: Status ----
            row += 10;
            var lblStatus = MakeLbl("Status:");
            lblStatus.Location = new System.Drawing.Point(0, row);
            cboStatus = new System.Windows.Forms.ComboBox();
            cboStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboStatus.Font     = new System.Drawing.Font("Segoe UI", 10f);
            cboStatus.Size     = new System.Drawing.Size(200, 28);
            cboStatus.Location = new System.Drawing.Point(180, row - 2);
            cboStatus.Items.AddRange(new object[] { "Pending", "In Transit", "Completed" });
            cboStatus.SelectedIndex = 0;

            innerPanel.Controls.Add(lblStatus);
            innerPanel.Controls.Add(cboStatus);
            row += rowH;

            // ---- Editable: Actual Recipient ----
            var lblRecip = MakeLbl("Actual Recipient:");
            lblRecip.Location = new System.Drawing.Point(0, row);
            txtActualRecipient = new System.Windows.Forms.TextBox();
            txtActualRecipient.Font     = new System.Drawing.Font("Segoe UI", 10f);
            txtActualRecipient.Size     = new System.Drawing.Size(300, 28);
            txtActualRecipient.Location = new System.Drawing.Point(180, row - 2);

            innerPanel.Controls.Add(lblRecip);
            innerPanel.Controls.Add(txtActualRecipient);
            row += rowH;

            // ---- Editable: Remark ----
            var lblRemark = MakeLbl("Remark:");
            lblRemark.Location = new System.Drawing.Point(0, row);
            txtRemark = new System.Windows.Forms.TextBox();
            txtRemark.Font      = new System.Drawing.Font("Segoe UI", 10f);
            txtRemark.Size      = new System.Drawing.Size(400, 28);
            txtRemark.Location  = new System.Drawing.Point(180, row - 2);

            innerPanel.Controls.Add(lblRemark);
            innerPanel.Controls.Add(txtRemark);
            row += rowH + 16;

            // ---- Action buttons ----
            btnSaveChanges    = MakeBtn("Save Changes",   System.Drawing.Color.FromArgb(0, 128, 0));
            btnDeleteShipment = MakeBtn("Delete Shipment",System.Drawing.Color.FromArgb(192, 0, 0));
            btnDiscardChanges = MakeBtn("Discard",         System.Drawing.Color.FromArgb(100, 100, 100));

            btnSaveChanges.Location    = new System.Drawing.Point(0,   row);
            btnDeleteShipment.Location = new System.Drawing.Point(160, row);
            btnDiscardChanges.Location = new System.Drawing.Point(320, row);

            btnSaveChanges.Enabled    = false;
            btnDeleteShipment.Enabled = false;
            btnDiscardChanges.Enabled = false;

            btnSaveChanges.Click    += btnSaveChanges_Click;
            btnDeleteShipment.Click += btnDeleteShipment_Click;
            btnDiscardChanges.Click += btnDiscardChanges_Click;

            innerPanel.Controls.Add(btnSaveChanges);
            innerPanel.Controls.Add(btnDeleteShipment);
            innerPanel.Controls.Add(btnDiscardChanges);

            // ---- Nest: inner → middleCard → outerCard → shell content area ----
            middleCard.Controls.Add(innerPanel);
            outerCard.Controls.Add(middleCard);
            _shell.SetContent(outerCard);

            // ---- Form properties ----
            this.Text          = "Modify Shipment — PremiumLiving OPS";
            this.Size          = new System.Drawing.Size(1280, 800);
            this.MinimumSize   = new System.Drawing.Size(900, 600);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.WindowState   = System.Windows.Forms.FormWindowState.Maximized;
            this.Controls.Add(_shell);
        }

        // ---- helpers ----
        private static System.Windows.Forms.Label MakeLbl(string text)
        {
            return new System.Windows.Forms.Label
            {
                Text      = text,
                Font      = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(50, 50, 50),
                AutoSize  = true
            };
        }

        private static System.Windows.Forms.Button MakeBtn(
            string text, System.Drawing.Color backColor)
        {
            return new System.Windows.Forms.Button
            {
                Text      = text,
                Font      = new System.Drawing.Font("Segoe UI", 10f),
                Size      = new System.Drawing.Size(150, 34),
                BackColor = backColor,
                ForeColor = System.Drawing.Color.White,
                FlatStyle = System.Windows.Forms.FlatStyle.Flat
            };
        }

        // Small helper used in foreach above (C# ref-in-anonymous-type workaround)
        private (string, ref System.Windows.Forms.Label) Pair(
            string s, ref System.Windows.Forms.Label l) => (s, ref l);
    }
}
