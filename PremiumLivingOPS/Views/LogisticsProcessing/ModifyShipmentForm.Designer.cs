using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    partial class ModifyShipmentForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── AppShell (mandatory shared component) ──────────────────────────────
        private AppShell _shell;

        // ── Search row ─────────────────────────────────────────────────────────
        private ComboBox cboSearchShipment;
        private Button   btnLoadShipment;

        // ── Read-only info labels ───────────────────────────────────────────────
        private Label lblShipmentIdValue;
        private Label lblOrderIdValue;
        private Label lblCustomerValue;
        private Label lblTrackingValue;
        private Label lblShipDateValue;
        private Label lblShipTypeValue;
        private Label lblDeliveryMethodValue;

        // ── Editable fields ────────────────────────────────────────────────────
        private ComboBox cboStatus;
        private TextBox  txtActualRecipient;
        private TextBox  txtRemark;

        // ── Action buttons ─────────────────────────────────────────────────────
        private Button btnSaveChanges;
        private Button btnDeleteShipment;
        private Button btnDiscardChanges;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout(); // RULE 1

            this.Text          = "Premium Living OPS — Modify Shipment";
            this.Size          = new Size(1280, 800);
            this.MinimumSize   = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = Palette.BgPage;
            this.WindowState   = FormWindowState.Maximized;
            this.Font          = new Font("Segoe UI", 13f);

            var pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage };
            _shell = new AppShell();
            _shell.SetPopupContainer(pnlMain);

            // ==================================================================
            // SEARCH BAR CARD
            // ==================================================================
            var (pnlSearchOuter, pnlSearchInner) = CardPanel.Create(outerHeight: 90);

            var lblSearchLbl = new Label
            {
                Text      = "Select Shipment to Modify:",
                Font      = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Left,
                AutoSize  = false, Width = 340,
                TextAlign = ContentAlignment.MiddleRight,
                Padding   = new Padding(0, 0, 12, 0)
            };
            cboSearchShipment = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 12f),
                Dock          = DockStyle.Fill
            };
            btnLoadShipment      = MakePrimaryBtn("Load Shipment", Point.Empty, 210, 60);
            btnLoadShipment.Dock  = DockStyle.Right;
            btnLoadShipment.Click += btnLoadShipment_Click;

            var pnlSearchRow = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(16, 10, 16, 10)
            };
            pnlSearchRow.Controls.Add(cboSearchShipment);
            pnlSearchRow.Controls.Add(btnLoadShipment);
            pnlSearchRow.Controls.Add(lblSearchLbl);
            pnlSearchInner.Controls.Add(pnlSearchRow);

            // ==================================================================
            // DETAILS CARD  (read-only info + editable fields)
            // ==================================================================
            // ── Read-only value labels ─────────────────────────────────────────
            lblShipmentIdValue     = MakeValueLabel();
            lblOrderIdValue        = MakeValueLabel();
            lblCustomerValue       = MakeValueLabel();
            lblTrackingValue       = MakeValueLabel();
            lblShipDateValue       = MakeValueLabel();
            lblShipTypeValue       = MakeValueLabel();
            lblDeliveryMethodValue = MakeValueLabel();

            // ── Editable controls ──────────────────────────────────────────────
            cboStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 12f),
                Dock          = DockStyle.Fill
            };
            cboStatus.Items.AddRange(new object[] { "Pending", "In Transit", "Completed" });
            cboStatus.SelectedIndex = 0;

            txtActualRecipient = new TextBox
            {
                Font        = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle,
                Dock        = DockStyle.Fill
            };

            txtRemark = new TextBox
            {
                Font        = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle,
                Dock        = DockStyle.Fill
            };

            // ── TableLayoutPanel (2 columns × 10 rows) ─────────────────────────
            var tblInfo = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 2,
                RowCount        = 10,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(18, 8, 18, 8)
            };
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            for (int i = 0; i < 5; i++)
            {
                tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
                tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));
            }

            // Row 0-1: Shipment ID | Order ID
            tblInfo.Controls.Add(FieldLabel("Shipment ID", false), 0, 0);
            tblInfo.Controls.Add(FieldLabel("Order ID",    false), 1, 0);
            tblInfo.Controls.Add(PadLabel(lblShipmentIdValue),     0, 1);
            tblInfo.Controls.Add(PadLabel(lblOrderIdValue),        1, 1);

            // Row 2-3: Customer | Tracking No.
            tblInfo.Controls.Add(FieldLabel("Customer",    false), 0, 2);
            tblInfo.Controls.Add(FieldLabel("Tracking No.", false), 1, 2);
            tblInfo.Controls.Add(PadLabel(lblCustomerValue),       0, 3);
            tblInfo.Controls.Add(PadLabel(lblTrackingValue),       1, 3);

            // Row 4-5: Ship Date | Type
            tblInfo.Controls.Add(FieldLabel("Ship Date", false), 0, 4);
            tblInfo.Controls.Add(FieldLabel("Type",      false), 1, 4);
            tblInfo.Controls.Add(PadLabel(lblShipDateValue),     0, 5);
            tblInfo.Controls.Add(PadLabel(lblShipTypeValue),     1, 5);

            // Row 6-7: Delivery Method (full width) | Status (editable)
            tblInfo.Controls.Add(FieldLabel("Delivery Method", false), 0, 6);
            tblInfo.Controls.Add(FieldLabel("Status *",        true),  1, 6);
            tblInfo.Controls.Add(PadLabel(lblDeliveryMethodValue),     0, 7);
            tblInfo.Controls.Add(PadCtrl(cboStatus),                   1, 7);

            // Row 8-9: Actual Recipient | Remark
            tblInfo.Controls.Add(FieldLabel("Actual Recipient", true), 0, 8);
            tblInfo.Controls.Add(FieldLabel("Remark",           false), 1, 8);
            tblInfo.Controls.Add(PadCtrl(txtActualRecipient),          0, 9);
            tblInfo.Controls.Add(PadCtrl(txtRemark),                   1, 9);

            var pnlDetailsTitle   = CardTitlePanel("Shipment Details");
            var pnlDetailsContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlDetailsContent.Controls.Add(tblInfo);
            pnlDetailsContent.Controls.Add(pnlDetailsTitle);

            var (pnlDetailsOuter, pnlDetailsInner) = CardPanel.Create(outerHeight: 560);
            pnlDetailsInner.Controls.Add(pnlDetailsContent);

            // ==================================================================
            // FOOTER  —  [Save Changes]  [Delete Shipment]  [Discard]
            // ==================================================================
            const int BtnW   = 200;
            const int BtnH   = 54;
            const int BtnGap = 8;
            const int BtnPad = 16;

            btnSaveChanges    = MakeGreenBtn  ("✔  Save Changes",    Point.Empty, BtnW, BtnH);
            btnDeleteShipment = MakeDangerBtn ("✕  Delete Shipment", Point.Empty, BtnW, BtnH);
            btnDiscardChanges = MakeOutlineBtn("↺  Discard",         Point.Empty, BtnW, BtnH);

            btnSaveChanges.Enabled    = false;
            btnDeleteShipment.Enabled = false;
            btnDiscardChanges.Enabled = false;

            btnSaveChanges.Click    += btnSaveChanges_Click;
            btnDeleteShipment.Click += btnDeleteShipment_Click;
            btnDiscardChanges.Click += btnDiscardChanges_Click;

            var pnlActionBtns = new Panel
            {
                Dock      = DockStyle.Right,
                Width     = BtnPad + BtnW + BtnGap + BtnW + BtnGap + BtnW + BtnPad,
                BackColor = Color.Transparent
            };
            void CentreFooterBtns()
            {
                int top = (pnlActionBtns.Height - BtnH) / 2; if (top < 0) top = 0;
                btnSaveChanges.Location    = new Point(BtnPad, top);
                btnDeleteShipment.Location = new Point(BtnPad + BtnW + BtnGap, top);
                btnDiscardChanges.Location = new Point(BtnPad + BtnW + BtnGap + BtnW + BtnGap, top);
            }
            pnlActionBtns.Controls.Add(btnSaveChanges);
            pnlActionBtns.Controls.Add(btnDeleteShipment);
            pnlActionBtns.Controls.Add(btnDiscardChanges);
            pnlActionBtns.Resize += (s, e) => CentreFooterBtns();

            var pnlFooterContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlFooterContent.Controls.Add(pnlActionBtns);

            var footerInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            footerInner.Paint += (s, e) =>
            {
                var p = (Panel)s;
                using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            };
            footerInner.Controls.Add(pnlFooterContent);

            var footerOuter = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 88,
                BackColor = Palette.BgPage,
                Padding   = new Padding(20, 14, 20, 14)
            };
            footerOuter.Controls.Add(footerInner);

            // ==================================================================
            // Assemble into pnlMain (Bottom → Fill → Top order for Dock)
            // ==================================================================
            pnlMain.Controls.Add(pnlDetailsOuter);  // Fill
            pnlMain.Controls.Add(footerOuter);       // Bottom
            pnlMain.Controls.Add(pnlSearchOuter);    // Top (search bar)
            pnlMain.Controls.Add(_shell);            // Top (AppShell — last so it wins)

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        // ── Builder helpers ──────────────────────────────────────────────────────

        private static Label MakeValueLabel() => new Label
        {
            Text      = "—",
            Font      = new Font("Segoe UI", 11f),
            ForeColor = Color.FromArgb(15, 31, 53),
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(10, 0, 0, 0)
        };

        private static Panel PadLabel(Label lbl)
        {
            lbl.Dock = DockStyle.Fill;
            var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(246, 249, 255), Padding = new Padding(0, 8, 12, 8) };
            p.Controls.Add(lbl);
            return p;
        }

        private static Panel PadCtrl(Control ctrl)
        {
            ctrl.Dock = DockStyle.Fill;
            var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 8, 12, 8) };
            p.Controls.Add(ctrl);
            return p;
        }

        private static Label FieldLabel(string text, bool required) => new Label
        {
            Text      = required ? text + " *" : text,
            Font      = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(98, 112, 135),
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft,
            Padding   = new Padding(18, 0, 0, 2)
        };

        private static Panel CardTitlePanel(string title)
        {
            var pnl = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = Color.Transparent };
            var lbl = new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Palette.TextMain,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(18, 0, 0, 0)
            };
            var div = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Palette.BorderColor };
            pnl.Controls.Add(lbl);
            pnl.Controls.Add(div);
            return pnl;
        }

        private static Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(47, 111, 237),
                FlatStyle = FlatStyle.Flat,
                Location  = loc, Width = w, Height = h,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize         = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(26, 77, 192);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(21, 60, 155);
            return b;
        }

        private static Button MakeGreenBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(34, 139, 34),
                FlatStyle = FlatStyle.Flat,
                Location  = loc, Width = w, Height = h,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize         = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(22, 111, 22);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(14, 85, 14);
            return b;
        }

        private static Button MakeDangerBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(185, 28, 28),
                FlatStyle = FlatStyle.Flat,
                Location  = loc, Width = w, Height = h,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize         = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(153, 20, 20);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(120, 14, 14);
            return b;
        }

        private static Button MakeOutlineBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location  = loc, Width = w, Height = h,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize         = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            return b;
        }
    }
}
