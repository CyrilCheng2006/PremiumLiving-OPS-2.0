using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.ProductionProcessing
{
    partial class CreateMaterialRequestForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── AppShell
        private AppShell _shell;

        // ── CARD 1: Request Header
        internal TextBox  txtRequestID;
        internal ComboBox cboUrgency;
        internal ComboBox cboTrigger;

        // ── CARD 2: Material & Warehouse
        internal ComboBox cboRawMaterial;
        internal TextBox  txtMaterialType;
        internal ComboBox cboWarehouse;
        internal TextBox  txtCurrentStock;
        internal TextBox  txtReorderLevel;

        // ── CARD 3: Linked Order (OrderDemand only)
        internal Panel    pnlOrderRow;
        internal ComboBox cboOrder;

        // ── CARD 4: Request Details
        internal NumericUpDown nudRequestedQty;

        // ── Footer buttons
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

            this.Text          = "Premium Living OPS — Production Processing";
            this.Size          = new Size(1440, 900);
            this.MinimumSize   = new Size(1280, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = Palette.BgPage;
            this.WindowState   = FormWindowState.Maximized;
            this.Font          = new Font("Segoe UI", 13f);

            var pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage };

            _shell = new AppShell();
            _shell.SetPopupContainer(pnlMain);
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += BtnLogout_Click;

            // ── Scroll panel ────────────────────────────────────────────────────────────
            var pnlScroll = new Panel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                BackColor  = Palette.BgPage
            };

            // ==================================================================
            // CARD 1 — Request Header
            // Schema: MaterialRequest.RequestID (auto), UrgencyLevel, TriggerType
            // TLP: 3 cols × [label 40px | control 72px] = 1 field row → total 200px card
            // ==================================================================
            txtRequestID = MakeReadOnlyBox();
            cboUrgency   = MakeCombo();
            cboUrgency.Items.AddRange(new object[] { "Critical", "High", "Medium" });
            cboUrgency.SelectedIndex = 0;

            cboTrigger = MakeCombo();
            cboTrigger.Items.AddRange(new object[] { "Reorder", "OrderDemand" });
            cboTrigger.SelectedIndex = 0;

            var tblHdr = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(18, 4, 18, 8)
            };
            tblHdr.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            tblHdr.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            tblHdr.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.4f));
            tblHdr.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));  // labels
            tblHdr.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f));  // controls
            tblHdr.Controls.Add(FieldLabel("Request ID",    false), 0, 0);
            tblHdr.Controls.Add(FieldLabel("Urgency Level", true),  1, 0);
            tblHdr.Controls.Add(FieldLabel("Trigger Type",  true),  2, 0);
            tblHdr.Controls.Add(Pad(txtRequestID), 0, 1);
            tblHdr.Controls.Add(Pad(cboUrgency),   1, 1);
            tblHdr.Controls.Add(Pad(cboTrigger),   2, 1);

            var pnlHdrTitle   = CardTitlePanel("Create Raw Material Request");
            var pnlHdrContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlHdrContent.Controls.Add(tblHdr);
            pnlHdrContent.Controls.Add(pnlHdrTitle);
            // CardPanel.Create height = titleH(54) + labelRow(40) + ctrlRow(72) + padding(24) = 190
            var (pnlCard1Outer, pnlCard1Inner) = CardPanel.Create(outerHeight: 200);
            pnlCard1Inner.Controls.Add(pnlHdrContent);

            // ==================================================================
            // CARD 2 — Material & Warehouse Selection
            // Schema: RawMaterial (ItemID, MaterialType), WarehouseItem
            //         (WarehouseItemID, WarehouseItemQuantity, ReorderLevel),
            //         Warehouse (WarehouseID, WarehouseLocation)
            // TLP: 2 cols, 4 rows [label|ctrl|label|ctrl] = 2 field rows → ~320px card
            // Row 1: Raw Material (60%) | Material Type (40%)
            // Row 2: Warehouse (50%) | Current Stock (25%) | Reorder Level (25%)
            // ==================================================================
            cboRawMaterial  = MakeCombo();
            txtMaterialType = MakeReadOnlyBox();
            cboWarehouse    = MakeCombo();
            cboWarehouse.Enabled = false;
            txtCurrentStock = MakeReadOnlyBox();
            txtReorderLevel = MakeReadOnlyBox();

            cboWarehouse.SelectedIndexChanged += CboWarehouse_Changed;

            // 5-column TLP for two field rows
            // Row 0 (labels) + Row 1 (controls) + Row 2 (labels) + Row 3 (controls)
            var tblMat = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 4,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(18, 4, 18, 8)
            };
            // Row 1: Raw Material 60%, Material Type 40%  → split across 2 of 4 cols
            // Row 2: Warehouse 50%, Current 25%, Reorder 25% → 3 of 4 cols
            // Use 4 cols so percentages work out: 60/40 vs 50/25/25
            // Col widths (percent): 50, 10, 25, 25 won't work cleanly.
            // Better: use two separate inner TLPs for each row, wrapped in a 1-col outer TLP.
            // Outer TLP: 1 col, 4 rows.
            tblMat = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(18, 4, 18, 8)
            };
            tblMat.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblMat.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));   // Row 1 labels
            tblMat.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f));   // Row 1 controls
            tblMat.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));   // Row 2 labels
            tblMat.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f));   // Row 2 controls

            // Row 1 labels: two labels in a 2-col TLP
            var tblMatLbl1 = MakeColTlp(2, new float[] { 60f, 40f });
            tblMatLbl1.Controls.Add(FieldLabel("Raw Material *",  false), 0, 0);
            tblMatLbl1.Controls.Add(FieldLabel("Material Type",   false), 1, 0);

            // Row 1 controls
            var tblMatCtrl1 = MakeColTlp(2, new float[] { 60f, 40f });
            tblMatCtrl1.Controls.Add(Pad(cboRawMaterial),  0, 0);
            tblMatCtrl1.Controls.Add(Pad(txtMaterialType), 1, 0);

            // Row 2 labels: three labels in a 3-col TLP
            var tblMatLbl2 = MakeColTlp(3, new float[] { 50f, 25f, 25f });
            tblMatLbl2.Controls.Add(FieldLabel("Warehouse / Stock Location *", false), 0, 0);
            tblMatLbl2.Controls.Add(FieldLabel("Current Stock (Ref)",          false), 1, 0);
            tblMatLbl2.Controls.Add(FieldLabel("Reorder Level (Ref)",          false), 2, 0);

            // Row 2 controls
            var tblMatCtrl2 = MakeColTlp(3, new float[] { 50f, 25f, 25f });
            tblMatCtrl2.Controls.Add(Pad(cboWarehouse),    0, 0);
            tblMatCtrl2.Controls.Add(Pad(txtCurrentStock), 1, 0);
            tblMatCtrl2.Controls.Add(Pad(txtReorderLevel), 2, 0);

            tblMat.Controls.Add(tblMatLbl1,  0, 0);
            tblMat.Controls.Add(tblMatCtrl1, 0, 1);
            tblMat.Controls.Add(tblMatLbl2,  0, 2);
            tblMat.Controls.Add(tblMatCtrl2, 0, 3);

            var pnlMatTitle   = CardTitlePanel("Material & Warehouse");
            var pnlMatContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlMatContent.Controls.Add(tblMat);
            pnlMatContent.Controls.Add(pnlMatTitle);
            // titleH(54) + 2×(label40+ctrl72) + padding(24) = 54+224+24 = 302 → 320
            var (pnlCard2Outer, pnlCard2Inner) = CardPanel.Create(outerHeight: 320);
            pnlCard2Inner.Controls.Add(pnlMatContent);

            // ==================================================================
            // CARD 3 — Linked Order  (visible only when TriggerType = OrderDemand)
            // Schema: Order.OrderID  (nullable FK in MaterialRequest)
            // ==================================================================
            cboOrder = MakeCombo();

            var tblOrd = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(18, 4, 18, 8)
            };
            tblOrd.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            tblOrd.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            tblOrd.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            tblOrd.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f));
            tblOrd.Controls.Add(FieldLabel("Linked Sales Order *", false), 0, 0);
            tblOrd.Controls.Add(Pad(cboOrder), 0, 1);

            var pnlOrdTitle   = CardTitlePanel("Linked Order");
            var pnlOrdContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlOrdContent.Controls.Add(tblOrd);
            pnlOrdContent.Controls.Add(pnlOrdTitle);
            // titleH(54) + label(40) + ctrl(72) + padding(24) = 190
            var (pnlOrderOuter, pnlOrderInner) = CardPanel.Create(outerHeight: 200);
            pnlOrderInner.Controls.Add(pnlOrdContent);
            pnlOrderRow         = pnlOrderOuter;
            pnlOrderRow.Visible = false;

            // ==================================================================
            // CARD 4 — Request Details
            // Schema: MaterialRequest.RequestedQty
            // ==================================================================
            nudRequestedQty = new NumericUpDown
            {
                Font          = new Font("Segoe UI", 12f),
                Minimum       = 1,
                Maximum       = 99999,
                Value         = 1,
                DecimalPlaces = 0,
                Dock          = DockStyle.Fill
            };

            var tblQty = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(18, 4, 18, 8)
            };
            tblQty.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            tblQty.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            tblQty.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.4f));
            tblQty.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            tblQty.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f));
            tblQty.Controls.Add(FieldLabel("Requested Quantity *", false), 0, 0);
            tblQty.Controls.Add(Pad(nudRequestedQty), 0, 1);

            var pnlQtyTitle   = CardTitlePanel("Request Details");
            var pnlQtyContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlQtyContent.Controls.Add(tblQty);
            pnlQtyContent.Controls.Add(pnlQtyTitle);
            var (pnlCard4Outer, pnlCard4Inner) = CardPanel.Create(outerHeight: 200);
            pnlCard4Inner.Controls.Add(pnlQtyContent);

            // ==================================================================
            // FOOTER — Submit + Reset  (mirrors CreateOrderForm footer pattern)
            // ==================================================================
            const int BtnW   = 210;
            const int BtnH   = 60;
            const int BtnGap = 8;
            const int BtnPad = 12;

            btnSubmit = MakePrimaryBtn("✔  Submit Request", Point.Empty, BtnW, BtnH);
            btnReset  = MakeOutlineBtn("↺  Reset Form",     Point.Empty, BtnW, BtnH);

            var pnlActionBtns = new Panel
            {
                Dock      = DockStyle.Right,
                Width     = BtnPad + BtnW + BtnGap + BtnW + BtnPad,
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

            // ==================================================================
            // Assemble scroll content
            // DockStyle.Top stacks last-added-first; add bottom → top.
            // ==================================================================
            pnlScroll.Controls.Add(pnlCard4Outer);    // added 1st → bottom
            pnlScroll.Controls.Add(pnlOrderRow);      // added 2nd (conditional)
            pnlScroll.Controls.Add(pnlCard2Outer);    // added 3rd
            pnlScroll.Controls.Add(pnlCard1Outer);    // added last → top

            // ==================================================================
            // Assemble pnlMain  (AppShell added last → renders on top)
            // ==================================================================
            pnlMain.Controls.Add(pnlScroll);    // Fill
            pnlMain.Controls.Add(footerOuter);  // Bottom
            pnlMain.Controls.Add(_shell);       // Top — last added, sits above all

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
            this.PerformLayout();

            _shell.Height      = AppShell.TotalHeight;
            _shell.MinimumSize = new System.Drawing.Size(0, AppShell.TotalHeight);
        }

        // ────────────────────────────────────────────────────────────────
        //  Shared builder helpers  —  exact same signatures as CreateOrderForm
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Wraps a control in a padding panel (Padding 0,8,12,8).
        /// Identical to CreateOrderForm.Pad().
        /// </summary>
        private static Panel Pad(Control ctrl)
        {
            ctrl.Dock = DockStyle.Fill;
            var p = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(0, 8, 12, 8)
            };
            p.Controls.Add(ctrl);
            return p;
        }

        /// <summary>
        /// Field label: grey, bold, 10.5pt, bottom-left, optional required star.
        /// Identical to CreateOrderForm.FieldLabel().
        /// </summary>
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

        /// <summary>
        /// Card title panel (height 54, bottom divider).
        /// Identical to CreateOrderForm.CardTitlePanel().
        /// </summary>
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

        /// <summary>Read-write ComboBox factory.</summary>
        private static ComboBox MakeCombo() =>
            new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 12f),
                Dock          = DockStyle.Fill
            };

        /// <summary>Read-only TextBox (auto-filled / reference field).</summary>
        private static TextBox MakeReadOnlyBox() =>
            new TextBox
            {
                Font        = new Font("Segoe UI", 12f),
                ReadOnly    = true,
                BackColor   = Color.FromArgb(235, 240, 250),   // matches CreateOrderForm billing addr tint
                ForeColor   = Color.FromArgb(98, 112, 135),
                BorderStyle = BorderStyle.FixedSingle,
                Dock        = DockStyle.Fill
            };

        /// <summary>
        /// Single-row TLP with N percent-width columns.
        /// Used to keep label rows and control rows in perfect column alignment.
        /// </summary>
        private static TableLayoutPanel MakeColTlp(int cols, float[] percents)
        {
            var tlp = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = cols,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            foreach (var pct in percents)
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, pct));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            return tlp;
        }

        // ── Button factories  (same signature as CreateOrderForm) ───────────────────
        private static Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Palette.Primary,
                FlatStyle = FlatStyle.Flat,
                Location  = loc,
                Width     = w,
                Height    = h,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize         = 0;
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
                ForeColor = Palette.TextMain,
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
