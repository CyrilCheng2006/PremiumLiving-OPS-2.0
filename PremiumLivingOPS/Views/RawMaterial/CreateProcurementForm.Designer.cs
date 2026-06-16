using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.RawMaterial
{
    partial class CreateProcurementForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── AppShell (contains TopNavBar + UserBar)
        private AppShell _shell;

        // ── CARD 1: Purchase Order Info
        internal Label         lblPurchaseIDValue;
        internal DateTimePicker dtpOrderDate;
        internal ComboBox       cboStatus;

        // ── CARD 2: Material Request & Supplier
        internal ComboBox cboMaterialRequest;
        internal ComboBox cboSupplier;
        internal Label    lblRawMaterialID;
        internal Label    lblRequestedQty;

        // ── CARD 3: Order Line Details
        internal ComboBox      cboWarehouse;
        internal NumericUpDown nudOrderQty;
        internal NumericUpDown nudUnitPrice;
        internal Label         lblLineTotal;

        // ── CARD 4: Actions
        private Button btnSubmit;
        private Button btnReset;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // ── Form
            this.Text          = "Premium Living OPS — Raw Material";
            this.Size          = new Size(1440, 900);
            this.MinimumSize   = new Size(1280, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = Palette.BgPage;
            this.WindowState   = FormWindowState.Maximized;
            this.Font          = new Font("Segoe UI", 13f);

            // ── Root panel (Fill)
            var pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage };

            // ── AppShell
            _shell = new AppShell();
            _shell.SetPopupContainer(pnlMain);
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += BtnLogout_Click;

            // ════════════════════════════════════════════════════════════
            // Scroll panel
            // ════════════════════════════════════════════════════════════
            var pnlScroll = new Panel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                BackColor  = Palette.BgPage
            };

            // ════════════════════════════════════════════════════════════
            // CARD 1 — Purchase Order Info
            // ────────────────────────────────────────────────────────────
            // PurchaseID chip  (matches CreateOrderForm lblOrderIdValue chip)
            lblPurchaseIDValue = new Label
            {
                Text      = "",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Palette.Primary,
                BackColor = Color.FromArgb(219, 234, 254),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(10, 0, 0, 0)
            };
            var pnlPurchaseIDChip = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(0, 8, 12, 8)
            };
            pnlPurchaseIDChip.Controls.Add(lblPurchaseIDValue);

            // OrderDate
            dtpOrderDate = new DateTimePicker
            {
                Font   = new Font("Segoe UI", 12f),
                Format = DateTimePickerFormat.Short,
                Value  = DateTime.Today,
                Dock   = DockStyle.Fill
            };

            // Status
            cboStatus = MakeCombo();
            cboStatus.Items.AddRange(new object[] { "Sent", "Cancelled", "Partially Received", "Received", "Completed" });
            cboStatus.SelectedIndex = 0;

            // 3-column TLP: PurchaseID | OrderDate | Status
            var tblCard1 = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 3,
                RowCount        = 2,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(18, 8, 18, 8)
            };
            tblCard1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            tblCard1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            tblCard1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.4f));
            tblCard1.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            tblCard1.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f));

            tblCard1.Controls.Add(FieldLabel("Purchase Order ID", false), 0, 0);
            tblCard1.Controls.Add(FieldLabel("Order Date",        true),  1, 0);
            tblCard1.Controls.Add(FieldLabel("Status",            true),  2, 0);
            tblCard1.Controls.Add(pnlPurchaseIDChip,                      0, 1);
            tblCard1.Controls.Add(Pad(dtpOrderDate),                      1, 1);
            tblCard1.Controls.Add(Pad(cboStatus),                         2, 1);

            var pnlCard1Title   = CardTitlePanel("Create Purchase Order");
            var pnlCard1Content = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlCard1Content.Controls.Add(tblCard1);
            pnlCard1Content.Controls.Add(pnlCard1Title);
            var (pnlCard1Outer, pnlCard1Inner) = CardPanel.Create(outerHeight: 200);
            pnlCard1Inner.Controls.Add(pnlCard1Content);

            // ════════════════════════════════════════════════════════════
            // CARD 2 — Material Request & Supplier
            // ════════════════════════════════════════════════════════════
            cboMaterialRequest = MakeCombo();
            cboSupplier        = MakeCombo();

            // Auto-filled read-only labels (styled like lblOrderIdValue chip)
            lblRawMaterialID = MakeReadOnlyChip();
            lblRequestedQty  = MakeReadOnlyChip();

            var pnlRawMatChip = ChipPanel(lblRawMaterialID);
            var pnlReqQtyChip = ChipPanel(lblRequestedQty);

            var tblCard2 = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 2,
                RowCount        = 4,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(18, 8, 18, 8)
            };
            tblCard2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblCard2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblCard2.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            tblCard2.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f));
            tblCard2.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            tblCard2.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f));

            tblCard2.Controls.Add(FieldLabel("Material Request", true),  0, 0);
            tblCard2.Controls.Add(FieldLabel("Supplier",         true),  1, 0);
            tblCard2.Controls.Add(Pad(cboMaterialRequest),                0, 1);
            tblCard2.Controls.Add(Pad(cboSupplier),                       1, 1);
            tblCard2.Controls.Add(FieldLabel("Raw Material ID (Auto)", false), 0, 2);
            tblCard2.Controls.Add(FieldLabel("Requested Qty (Ref)",    false), 1, 2);
            tblCard2.Controls.Add(pnlRawMatChip,                               0, 3);
            tblCard2.Controls.Add(pnlReqQtyChip,                               1, 3);

            var pnlCard2Title   = CardTitlePanel("Material Request & Supplier");
            var pnlCard2Content = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlCard2Content.Controls.Add(tblCard2);
            pnlCard2Content.Controls.Add(pnlCard2Title);
            var (pnlCard2Outer, pnlCard2Inner) = CardPanel.Create(outerHeight: 320);
            pnlCard2Inner.Controls.Add(pnlCard2Content);

            // ════════════════════════════════════════════════════════════
            // CARD 3 — Order Line Details
            // ════════════════════════════════════════════════════════════
            cboWarehouse = MakeCombo();
            nudOrderQty  = new NumericUpDown
            {
                Font          = new Font("Segoe UI", 12f),
                Minimum       = 1,
                Maximum       = 99999,
                Value         = 1,
                DecimalPlaces = 0,
                Dock          = DockStyle.Fill
            };
            nudUnitPrice = new NumericUpDown
            {
                Font          = new Font("Segoe UI", 12f),
                Minimum       = 0m,
                Maximum       = 9_999_999m,
                Value         = 0m,
                DecimalPlaces = 2,
                Dock          = DockStyle.Fill
            };
            // PO Total Amount — green chip matching lblGrandTotalValue style
            lblLineTotal = new Label
            {
                Text      = "HK$ 0.00",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 163, 74),
                BackColor = Color.FromArgb(220, 252, 231),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(10, 0, 0, 0)
            };
            var pnlLineTotalChip = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(0, 8, 12, 8)
            };
            pnlLineTotalChip.Controls.Add(lblLineTotal);

            var tblCard3 = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 3,
                RowCount        = 4,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(18, 8, 18, 8)
            };
            tblCard3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            tblCard3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            tblCard3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            tblCard3.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            tblCard3.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f));
            tblCard3.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            tblCard3.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f));

            tblCard3.Controls.Add(FieldLabel("Delivery Warehouse", true),  0, 0);
            tblCard3.Controls.Add(FieldLabel("Order Quantity",     true),  1, 0);
            tblCard3.Controls.Add(FieldLabel("Unit Price (HK$)",   true),  2, 0);
            tblCard3.Controls.Add(Pad(cboWarehouse),                        0, 1);
            tblCard3.Controls.Add(Pad(nudOrderQty),                         1, 1);
            tblCard3.Controls.Add(Pad(nudUnitPrice),                        2, 1);

            var lblTotalLabel = FieldLabel("PO Total Amount (HK$)", false);
            tblCard3.Controls.Add(lblTotalLabel, 0, 2);
            tblCard3.Controls.Add(pnlLineTotalChip, 0, 3);
            tblCard3.SetColumnSpan(pnlLineTotalChip, 3);

            var pnlCard3Title   = CardTitlePanel("Order Line Details");
            var pnlCard3Content = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlCard3Content.Controls.Add(tblCard3);
            pnlCard3Content.Controls.Add(pnlCard3Title);
            var (pnlCard3Outer, pnlCard3Inner) = CardPanel.Create(outerHeight: 340);
            pnlCard3Inner.Controls.Add(pnlCard3Content);

            // ════════════════════════════════════════════════════════════
            // FOOTER — matches CreateOrderForm footer pattern
            // ════════════════════════════════════════════════════════════
            const int BtnW   = 260;
            const int BtnH   = 60;
            const int BtnGap = 8;
            const int BtnPad = 12;

            btnSubmit = MakePrimaryBtn("✔  Submit Purchase Order", Point.Empty, BtnW, BtnH);
            btnReset  = MakeOutlineBtn("↺  Reset Form",            Point.Empty, 180,  BtnH);

            var pnlActionBtns = new Panel
            {
                Dock      = DockStyle.Right,
                Width     = BtnPad + BtnW + BtnGap + 180 + BtnPad,
                BackColor = Color.Transparent
            };
            void CentreFooterBtns()
            {
                int top = (pnlActionBtns.Height - BtnH) / 2;
                if (top < 0) top = 0;
                btnSubmit.Location = new Point(BtnPad, top);
                btnReset.Location  = new Point(BtnPad + BtnW + BtnGap, top);
            }
            pnlActionBtns.Controls.Add(btnSubmit);
            pnlActionBtns.Controls.Add(btnReset);
            pnlActionBtns.Resize += (s, e) => CentreFooterBtns();

            var pnlFooterContent = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(4, 0, 0, 0)
            };
            pnlFooterContent.Controls.Add(pnlActionBtns);

            var footerInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            footerInner.Paint += (s, e) =>
            {
                var p = (Panel)s;
                using var pen = new Pen(Palette.BorderColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            };
            footerInner.Controls.Add(pnlFooterContent);

            var footerOuter = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 108,
                BackColor = Palette.BgPage,
                Padding   = new Padding(20, 14, 20, 14)
            };
            footerOuter.Controls.Add(footerInner);

            // ════════════════════════════════════════════════════════════
            // Assemble scroll content (DockStyle.Top stacks bottom-first)
            // ════════════════════════════════════════════════════════════
            pnlScroll.Controls.Add(pnlCard3Outer);   // bottom card
            pnlScroll.Controls.Add(pnlCard2Outer);
            pnlScroll.Controls.Add(pnlCard1Outer);   // top card

            // ════════════════════════════════════════════════════════════
            // Assemble pnlMain
            // ════════════════════════════════════════════════════════════
            pnlMain.Controls.Add(pnlScroll);    // DockStyle.Fill — content
            pnlMain.Controls.Add(footerOuter);  // DockStyle.Bottom — action bar
            pnlMain.Controls.Add(_shell);       // DockStyle.Top  — AppShell (topmost)

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        // ── Helpers aligned with CreateOrderForm pattern ─────────────────────

        private static ComboBox MakeCombo() =>
            new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f), Dock = DockStyle.Fill };

        private static Panel Pad(Control ctrl)
        {
            ctrl.Dock = DockStyle.Fill;
            var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 8, 12, 8) };
            p.Controls.Add(ctrl);
            return p;
        }

        private static Label FieldLabel(string text, bool required) =>
            new Label
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

        /// <summary>Read-only auto-filled chip — same visual as lblOrderIdValue.</summary>
        private static Label MakeReadOnlyChip() =>
            new Label
            {
                Text      = "—",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                BackColor = Color.FromArgb(235, 240, 250),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(10, 0, 0, 0)
            };

        private static Panel ChipPanel(Label chip)
        {
            chip.Dock = DockStyle.Fill;
            var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 8, 12, 8) };
            p.Controls.Add(chip);
            return p;
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
                Location  = loc,
                Width     = w,
                Height    = h,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize             = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(26, 77, 192);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(21, 60, 155);
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
                Location  = loc,
                Width     = w,
                Height    = h,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderColor        = Palette.BorderColor;
            b.FlatAppearance.BorderSize         = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            return b;
        }
    }
}
