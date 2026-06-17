using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    partial class ModifyShipmentForm
    {
        private System.ComponentModel.IContainer components = null;

        private AppShell _shell;

        private ComboBox cboSearchShipment;
        private Button   btnLoadShipment;

        private Label lblShipmentIdValue;
        private Label lblOrderIdValue;
        private Label lblCustomerValue;
        private Label lblTrackingValue;
        private Label lblShipDateValue;
        private Label lblShipTypeValue;
        private Label lblDeliveryMethodValue;

        private ComboBox cboStatus;
        private TextBox  txtActualRecipient;
        private TextBox  txtRemark;

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
            this.SuspendLayout();

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
            btnLoadShipment       = MakePrimaryBtn("Load Shipment", Point.Empty, 210, 60);
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
            // DETAILS CARD
            // Row layout (11 rows total):
            //   rows 0-9  : 5 pairs of (header 32px + input 84px)  for the first 4 field pairs
            //   row  10   : 50px spacer between Status block and Actual Recipient block
            //   rows 11-12: header 32px + input 84px  for Actual Recipient
            //   rows 13-14: header 32px + input 84px  for Remark
            //
            // Concretely:
            //   0  header: Shipment ID | Order ID
            //   1  input:  Shipment ID | Order ID
            //   2  header: Customer    | Tracking No.
            //   3  input:  Customer    | Tracking No.
            //   4  header: Ship Date   | Type
            //   5  input:  Ship Date   | Type
            //   6  header: Delivery Method | Status *
            //   7  input:  Delivery Method | Status *
            //   8  SPACER  50px
            //   9  header: Actual Recipient * | Remark
            //  10  input:  Actual Recipient * | Remark
            // ==================================================================
            lblShipmentIdValue     = MakeValueLabel();
            lblOrderIdValue        = MakeValueLabel();
            lblCustomerValue       = MakeValueLabel();
            lblTrackingValue       = MakeValueLabel();
            lblShipDateValue       = MakeValueLabel();
            lblShipTypeValue       = MakeValueLabel();
            lblDeliveryMethodValue = MakeValueLabel();

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

            const int HeaderRowH = 32;
            const int InputRowH  = 84;
            const int SpacerH    = 50;

            var tblInfo = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 4,
                RowCount        = 11,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(18, 0, 18, 8)
            };
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28f));

            // Rows 0-7: 4 field pairs
            for (int i = 0; i < 4; i++)
            {
                tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, HeaderRowH));
                tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, InputRowH));
            }
            // Row 8: spacer
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, SpacerH));
            // Rows 9-10: Actual Recipient + Remark
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, HeaderRowH));
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, InputRowH));

            // Helper: add a cell with ColSpan=2 in one call
            void AddCell(Control ctrl, int col, int row, int colSpan = 2)
            {
                tblInfo.Controls.Add(ctrl, col, row);
                if (colSpan > 1) tblInfo.SetColumnSpan(ctrl, colSpan);
            }

            // Row 0-1: Shipment ID | Order ID
            AddCell(FieldLabel("Shipment ID",  false), 0, 0);
            AddCell(FieldLabel("Order ID",     false), 2, 0);
            AddCell(PadLabel(lblShipmentIdValue),      0, 1);
            AddCell(PadLabel(lblOrderIdValue),         2, 1);

            // Row 2-3: Customer | Tracking No.
            AddCell(FieldLabel("Customer",     false), 0, 2);
            AddCell(FieldLabel("Tracking No.", false), 2, 2);
            AddCell(PadLabel(lblCustomerValue),        0, 3);
            AddCell(PadLabel(lblTrackingValue),        2, 3);

            // Row 4-5: Ship Date | Type
            AddCell(FieldLabel("Ship Date",    false), 0, 4);
            AddCell(FieldLabel("Type",         false), 2, 4);
            AddCell(PadLabel(lblShipDateValue),        0, 5);
            AddCell(PadLabel(lblShipTypeValue),        2, 5);

            // Row 6-7: Delivery Method | Status *
            AddCell(FieldLabel("Delivery Method", false), 0, 6);
            AddCell(FieldLabel("Status *",        true),  2, 6);
            AddCell(PadLabel(lblDeliveryMethodValue),     0, 7);
            AddCell(PadCtrl(cboStatus),                   2, 7);

            // Row 8: spacer (empty — no controls added, just row height)

            // Row 9-10: Actual Recipient * | Remark
            AddCell(FieldLabel("Actual Recipient *", true),  0, 9);
            AddCell(FieldLabel("Remark",             false), 2, 9);
            AddCell(PadCtrl(txtActualRecipient),             0, 10);
            AddCell(PadCtrl(txtRemark),                      2, 10);

            // Card title + tblInfo: Fill MUST be added before Top
            var pnlDetailsTitle = CardTitlePanel("Edit Shipment");
            var (pnlDetailsOuter, pnlDetailsInner) = CardPanel.Create(outerHeight: 0);
            pnlDetailsOuter.Dock = DockStyle.Fill;

            pnlDetailsInner.Controls.Add(tblInfo);         // Fill — first
            pnlDetailsInner.Controls.Add(pnlDetailsTitle); // Top  — second

            // ==================================================================
            // FOOTER
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

            // Assemble (Bottom → Fill → Top order)
            pnlMain.Controls.Add(pnlDetailsOuter);
            pnlMain.Controls.Add(footerOuter);
            pnlMain.Controls.Add(pnlSearchOuter);
            pnlMain.Controls.Add(_shell);

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
            Text      = text,
            Font      = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(98, 112, 135),
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft,
            Padding   = new Padding(6, 0, 0, 2)
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
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(47, 111, 237), FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0; b.FlatAppearance.MouseOverBackColor = Color.FromArgb(26, 77, 192); b.FlatAppearance.MouseDownBackColor = Color.FromArgb(21, 60, 155);
            return b;
        }

        private static Button MakeGreenBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(34, 139, 34), FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0; b.FlatAppearance.MouseOverBackColor = Color.FromArgb(22, 111, 22); b.FlatAppearance.MouseDownBackColor = Color.FromArgb(14, 85, 14);
            return b;
        }

        private static Button MakeDangerBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(185, 28, 28), FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0; b.FlatAppearance.MouseOverBackColor = Color.FromArgb(153, 20, 20); b.FlatAppearance.MouseDownBackColor = Color.FromArgb(120, 14, 14);
            return b;
        }

        private static Button MakeOutlineBtn(string text, Point loc, int w, int h)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White, FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236); b.FlatAppearance.BorderSize = 1; b.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            return b;
        }
    }
}
