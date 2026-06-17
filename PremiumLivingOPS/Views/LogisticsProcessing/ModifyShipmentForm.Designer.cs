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
            // SEARCH BAR CARD  (fixed height, DockStyle.Top)
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
            // DETAILS CARD  (fill remaining space, DockStyle.Fill)
            //
            // TLP layout — 2 columns (Left 50% | Right 50%), 12 rows:
            //
            //   Col 0 = Left field   Col 1 = Right field
            //
            //   Row  0  hdr  Shipment ID        | Order ID          32px
            //   Row  1  inp  Shipment ID        | Order ID          84px
            //   Row  2  hdr  Customer           | Tracking No.      32px
            //   Row  3  inp  Customer           | Tracking No.      84px
            //   Row  4  hdr  Ship Date          | Type              32px
            //   Row  5  inp  Ship Date          | Type              84px
            //   Row  6  hdr  Delivery Method    | Status *          32px
            //   Row  7  inp  Delivery Method    | Status *          84px
            //   Row  8  ─── SPACER ──────────────────────────────── 50px
            //   Row  9  hdr  Actual Recipient * | Remark            32px
            //   Row 10  ─── SPACER (label / input) ──────────────── 50px
            //   Row 11  inp  Actual Recipient * | Remark            84px
            //
            // Each cell is colSpan=1 so left and right fields never overlap.
            // Left-column inputs use PadCtrlRight(rightPad:24) for a clear
            // horizontal gap from the right-column field.
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

            const int HeaderRowH        = 32;
            const int InputRowH         = 84;
            const int StatusRemarkSpacer = 50;  // Row 8: gap between Status block and bottom fields
            const int LabelInputSpacer  = 50;   // Row 10: gap between Actual Recipient label and its input

            var tblInfo = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 2,
                RowCount        = 12,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(18, 0, 18, 8)
            };
            // 2 equal columns — each field gets its own column, no colSpan confusion
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            // Rows 0–7: 4 field pairs (header + input each)
            for (int i = 0; i < 4; i++)
            {
                tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, HeaderRowH));
                tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, InputRowH));
            }
            // Row 8: spacer between Status block and Actual Recipient / Remark
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, StatusRemarkSpacer));
            // Row 9: Actual Recipient * | Remark — header labels
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, HeaderRowH));
            // Row 10: spacer between labels and their inputs
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, LabelInputSpacer));
            // Row 11: Actual Recipient * | Remark — inputs
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, InputRowH));

            // Helper: place a control at (col, row); colSpan=1 by default (no span needed in 2-col layout)
            void AddCell(Control ctrl, int col, int row)
            {
                tblInfo.Controls.Add(ctrl, col, row);
            }

            // Rows 0–1: Shipment ID | Order ID
            AddCell(FieldLabel("Shipment ID",  false), 0, 0);
            AddCell(FieldLabel("Order ID",     false), 1, 0);
            AddCell(PadCtrlRight(PadLabel(lblShipmentIdValue), 24), 0, 1);
            AddCell(PadLabel(lblOrderIdValue),                      1, 1);

            // Rows 2–3: Customer | Tracking No.
            AddCell(FieldLabel("Customer",     false), 0, 2);
            AddCell(FieldLabel("Tracking No.", false), 1, 2);
            AddCell(PadCtrlRight(PadLabel(lblCustomerValue), 24), 0, 3);
            AddCell(PadLabel(lblTrackingValue),                   1, 3);

            // Rows 4–5: Ship Date | Type
            AddCell(FieldLabel("Ship Date",    false), 0, 4);
            AddCell(FieldLabel("Type",         false), 1, 4);
            AddCell(PadCtrlRight(PadLabel(lblShipDateValue), 24), 0, 5);
            AddCell(PadLabel(lblShipTypeValue),                   1, 5);

            // Rows 6–7: Delivery Method | Status *
            AddCell(FieldLabel("Delivery Method", false), 0, 6);
            AddCell(FieldLabel("Status *",        true),  1, 6);
            AddCell(PadCtrlRight(PadLabel(lblDeliveryMethodValue), 24), 0, 7);
            AddCell(PadCtrl(cboStatus),                                 1, 7);

            // Row 8: intentionally empty spacer

            // Row 9: Actual Recipient * | Remark — header labels
            AddCell(FieldLabel("Actual Recipient *", true),  0, 9);
            AddCell(FieldLabel("Remark",             false), 1, 9);

            // Row 10: intentionally empty spacer (50px between labels and inputs)

            // Row 11: inputs — each in its own column, no overlap possible
            AddCell(PadCtrlRight(txtActualRecipient, 24), 0, 11);
            AddCell(PadCtrl(txtRemark),                   1, 11);

            // Card title bar
            var pnlDetailsTitle = CardTitlePanel("Edit Shipment");

            var (pnlDetailsOuter, pnlDetailsInner) = CardPanel.CreateFill();
            pnlDetailsInner.Controls.Add(tblInfo);
            pnlDetailsInner.Controls.Add(pnlDetailsTitle);

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

            // Assemble — Bottom → Fill → Top order (WinForms dock priority)
            pnlMain.Controls.Add(pnlDetailsOuter);
            pnlMain.Controls.Add(footerOuter);
            pnlMain.Controls.Add(pnlSearchOuter);
            pnlMain.Controls.Add(_shell);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        // ── Builder helpers ──────────────────────────────────────────────────

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

        /// <summary>
        /// Wraps a control (or panel) with extra right padding to create a
        /// clear horizontal gap between left and right column fields.
        /// </summary>
        private static Panel PadCtrlRight(Control ctrl, int rightPad)
        {
            ctrl.Dock = DockStyle.Fill;
            var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 0, rightPad, 0) };
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
