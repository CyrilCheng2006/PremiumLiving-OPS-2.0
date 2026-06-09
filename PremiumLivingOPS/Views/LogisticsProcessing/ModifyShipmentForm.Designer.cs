using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    partial class ModifyShipmentForm
    {
        private System.ComponentModel.IContainer components = null;

        // ---- AppShell (mandatory shared component) ----------
        private PremiumLivingOPS.Views.Shared.AppShell _shell;

        // ---- Search row -----
        private ComboBox cboSearchShipment;
        private Button   btnLoadShipment;

        // ---- Info Labels ----
        private Label lblShipmentIdValue;
        private Label lblOrderIdValue;
        private Label lblCustomerValue;
        private Label lblTrackingValue;
        private Label lblShipDateValue;
        private Label lblShipTypeValue;
        private Label lblDeliveryMethodValue;

        // ---- Editable fields ----
        private ComboBox cboStatus;
        private TextBox  txtActualRecipient;
        private TextBox  txtRemark;

        // ---- Action buttons ----
        private Button btnSaveChanges;
        private Button btnDeleteShipment;
        private Button btnDiscardChanges;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // ---- AppShell ----
            _shell      = new PremiumLivingOPS.Views.Shared.AppShell();
            _shell.Dock = DockStyle.Fill;

            // ── Root panel ──────────────────────────────────────────────
            var pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249) };
            _shell.SetPopupContainer(pnlMain);

            // ===========================================================
            //  Three-layer CardPanel nesting (grey → white → content)
            // ===========================================================
            var outerCard = new PremiumLivingOPS.Views.Shared.CardPanel
            {
                Dock      = DockStyle.Fill,
                Padding   = new Padding(20),
                BackColor = Color.FromArgb(240, 244, 249)
            };

            var middleCard = new PremiumLivingOPS.Views.Shared.CardPanel
            {
                Dock      = DockStyle.Fill,
                Padding   = new Padding(24),
                BackColor = Color.White
            };

            var innerPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            // ── Page title ──────────────────────────────────────────────
            var lblTitle = new Label
            {
                Text      = "Modify Shipment",
                Font      = new Font("Segoe UI", 16f, FontStyle.Bold),
                AutoSize  = true,
                Location  = new Point(0, 0),
                ForeColor = Color.FromArgb(15, 31, 53)
            };

            // ── Search row ──────────────────────────────────────────────
            var lblSearch = MakeLbl("Select Shipment:");
            lblSearch.Location = new Point(0, 50);

            cboSearchShipment = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 11f),
                Size          = new Size(420, 30),
                Location      = new Point(180, 47)
            };

            btnLoadShipment = new Button
            {
                Text      = "Load",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                Size      = new Size(100, 32),
                Location  = new Point(610, 46),
                BackColor = Color.FromArgb(47, 111, 237),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            btnLoadShipment.FlatAppearance.BorderSize       = 0;
            btnLoadShipment.FlatAppearance.MouseOverBackColor = Color.FromArgb(26, 77, 192);
            btnLoadShipment.Click += btnLoadShipment_Click;

            // ── Read-only info section ──────────────────────────────────
            //  FIX: replaced ref-in-array-initialiser (CS1525) with a
            //       plain string[] for captions and explicit indexed assignment.
            int row  = 100;
            int rowH = 36;

            var captions = new[]
            {
                "Shipment ID:",
                "Order ID:",
                "Customer:",
                "Tracking No.:",
                "Ship Date:",
                "Type:",
                "Delivery Method:"
            };

            innerPanel.Controls.Add(lblTitle);
            innerPanel.Controls.Add(lblSearch);
            innerPanel.Controls.Add(cboSearchShipment);
            innerPanel.Controls.Add(btnLoadShipment);

            // Initialise all value labels to a placeholder first
            lblShipmentIdValue    = MakeValLbl(); lblShipmentIdValue.Location    = new Point(210, row + 2);
            lblOrderIdValue       = MakeValLbl(); lblOrderIdValue.Location       = new Point(210, row + 2 + rowH);
            lblCustomerValue      = MakeValLbl(); lblCustomerValue.Location      = new Point(210, row + 2 + rowH * 2);
            lblTrackingValue      = MakeValLbl(); lblTrackingValue.Location      = new Point(210, row + 2 + rowH * 3);
            lblShipDateValue      = MakeValLbl(); lblShipDateValue.Location      = new Point(210, row + 2 + rowH * 4);
            lblShipTypeValue      = MakeValLbl(); lblShipTypeValue.Location      = new Point(210, row + 2 + rowH * 5);
            lblDeliveryMethodValue= MakeValLbl(); lblDeliveryMethodValue.Location= new Point(210, row + 2 + rowH * 6);

            Label[] valueLabels =
            {
                lblShipmentIdValue,
                lblOrderIdValue,
                lblCustomerValue,
                lblTrackingValue,
                lblShipDateValue,
                lblShipTypeValue,
                lblDeliveryMethodValue
            };

            for (int i = 0; i < captions.Length; i++)
            {
                var lbl = MakeLbl(captions[i]);
                lbl.Location = new Point(0, row);
                innerPanel.Controls.Add(lbl);
                innerPanel.Controls.Add(valueLabels[i]);
                row += rowH;
            }

            // ── Divider ─────────────────────────────────────────────────
            var divider = new Panel
            {
                Location  = new Point(0, row + 6),
                Size      = new Size(700, 1),
                BackColor = Color.FromArgb(221, 227, 236)
            };
            innerPanel.Controls.Add(divider);
            row += 20;

            // ── Editable: Status ────────────────────────────────────────
            var lblStatus = MakeLbl("Status:");
            lblStatus.Location = new Point(0, row);
            cboStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 11f),
                Size          = new Size(220, 30),
                Location      = new Point(210, row - 2)
            };
            cboStatus.Items.AddRange(new object[] { "Pending", "In Transit", "Completed" });
            cboStatus.SelectedIndex = 0;
            innerPanel.Controls.Add(lblStatus);
            innerPanel.Controls.Add(cboStatus);
            row += rowH;

            // ── Editable: Actual Recipient ──────────────────────────────
            var lblRecip = MakeLbl("Actual Recipient:");
            lblRecip.Location = new Point(0, row);
            txtActualRecipient = new TextBox
            {
                Font     = new Font("Segoe UI", 11f),
                Size     = new Size(320, 30),
                Location = new Point(210, row - 2)
            };
            innerPanel.Controls.Add(lblRecip);
            innerPanel.Controls.Add(txtActualRecipient);
            row += rowH;

            // ── Editable: Remark ────────────────────────────────────────
            var lblRemark = MakeLbl("Remark:");
            lblRemark.Location = new Point(0, row);
            txtRemark = new TextBox
            {
                Font      = new Font("Segoe UI", 11f),
                Size      = new Size(440, 30),
                Location  = new Point(210, row - 2)
            };
            innerPanel.Controls.Add(lblRemark);
            innerPanel.Controls.Add(txtRemark);
            row += rowH + 20;

            // ── Action buttons ──────────────────────────────────────────
            btnSaveChanges    = MakeBtn("Save Changes",    Color.FromArgb(5, 150, 105));
            btnDeleteShipment = MakeBtn("Delete Shipment", Color.FromArgb(185, 28, 28));
            btnDiscardChanges = MakeBtn("Discard",         Color.FromArgb(100, 116, 139));

            btnSaveChanges.Location    = new Point(0,   row);
            btnDeleteShipment.Location = new Point(168, row);
            btnDiscardChanges.Location = new Point(336, row);

            btnSaveChanges.Enabled    = false;
            btnDeleteShipment.Enabled = false;
            btnDiscardChanges.Enabled = false;

            btnSaveChanges.Click    += btnSaveChanges_Click;
            btnDeleteShipment.Click += btnDeleteShipment_Click;
            btnDiscardChanges.Click += btnDiscardChanges_Click;

            innerPanel.Controls.Add(btnSaveChanges);
            innerPanel.Controls.Add(btnDeleteShipment);
            innerPanel.Controls.Add(btnDiscardChanges);

            // ── Nest: inner → middleCard → outerCard → shell ────────────
            middleCard.Controls.Add(innerPanel);
            outerCard.Controls.Add(middleCard);

            pnlMain.Controls.Add(outerCard);
            pnlMain.Controls.Add(_shell);

            // ── Form properties ─────────────────────────────────────────
            this.Text          = "Modify Shipment — PremiumLiving OPS";
            this.Size          = new System.Drawing.Size(1280, 800);
            this.MinimumSize   = new System.Drawing.Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState   = FormWindowState.Maximized;
            this.BackColor     = Color.FromArgb(240, 244, 249);
            this.Font          = new Font("Segoe UI", 13f);
            this.Controls.Add(pnlMain);

            this.ResumeLayout(false);
        }

        // ── Helpers ─────────────────────────────────────────────────────
        private static Label MakeLbl(string text) => new Label
        {
            Text      = text,
            Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = Color.FromArgb(98, 112, 135),
            AutoSize  = true
        };

        private static Label MakeValLbl() => new Label
        {
            Text      = "\u2014",
            Font      = new Font("Segoe UI", 11f),
            ForeColor = Color.FromArgb(15, 31, 53),
            AutoSize  = true
        };

        private static Button MakeBtn(string text, Color backColor)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                Size      = new Size(158, 38),
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
    }
}
