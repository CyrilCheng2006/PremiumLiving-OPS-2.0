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

        // ── CARD 1: Request Overview (header)
        internal ComboBox cboUrgency;
        internal ComboBox cboTrigger;

        // ── CARD 2: Linked Order (OrderDemand only)
        internal Panel    pnlOrderRow;
        internal ComboBox cboOrder;

        // ── CARD 3: Add Material Line picker
        internal ComboBox      cboRawMaterial;
        internal TextBox       txtMaterialType;
        internal ComboBox      cboWarehouse;
        internal TextBox       txtCurrentStock;
        internal TextBox       txtReorderLevel;
        internal NumericUpDown nudRequestedQty;
        private  Button        btnAddLine;

        // ── CARD 4: Request Lines
        internal DataGridView dgvLines;
        private  Button       btnRemoveLine;
        private  Label        lblLineCount;

        // ── Footer
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

            var pnlScroll = new Panel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                BackColor  = Palette.BgPage
            };

            // ==================================================================
            // CARD 1 — Request Overview
            // Schema: MaterialRequest.UrgencyLevel, TriggerType (shared by all lines)
            // ==================================================================
            cboUrgency = MakeCombo();
            cboUrgency.Items.AddRange(new object[] { "Critical", "High", "Medium" });
            cboUrgency.SelectedIndex = 0;

            cboTrigger = MakeCombo();
            cboTrigger.Items.AddRange(new object[] { "Reorder", "OrderDemand" });
            cboTrigger.SelectedIndex = 0;

            var tblHdr = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(18, 4, 18, 8)
            };
            tblHdr.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblHdr.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblHdr.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            tblHdr.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f));
            tblHdr.Controls.Add(FieldLabel("Urgency Level", true),  0, 0);
            tblHdr.Controls.Add(FieldLabel("Trigger Type",  true),  1, 0);
            tblHdr.Controls.Add(Pad(cboUrgency),   0, 1);
            tblHdr.Controls.Add(Pad(cboTrigger),   1, 1);

            var (pnlCard1Outer, pnlCard1Inner) = CardPanel.Create(outerHeight: 200);
            var pnlHdrContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlHdrContent.Controls.Add(tblHdr);
            pnlHdrContent.Controls.Add(CardTitlePanel("Request Overview"));
            pnlCard1Inner.Controls.Add(pnlHdrContent);

            // ==================================================================
            // CARD 2 — Linked Order (OrderDemand only)
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

            var (pnlOrderOuter, pnlOrderInner) = CardPanel.Create(outerHeight: 200);
            var pnlOrdContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlOrdContent.Controls.Add(tblOrd);
            pnlOrdContent.Controls.Add(CardTitlePanel("Linked Order"));
            pnlOrderInner.Controls.Add(pnlOrdContent);
            pnlOrderRow         = pnlOrderOuter;
            pnlOrderRow.Visible = false;

            // ==================================================================
            // CARD 3 — Add Material Line
            // Schema: RawMaterial, WarehouseItem per line
            // ==================================================================
            cboRawMaterial  = MakeCombo();
            txtMaterialType = MakeReadOnlyBox();
            cboWarehouse    = MakeCombo();
            cboWarehouse.Enabled = false;
            txtCurrentStock = MakeReadOnlyBox();
            txtReorderLevel = MakeReadOnlyBox();

            nudRequestedQty = new NumericUpDown
            {
                Font          = new Font("Segoe UI", 12f),
                Minimum       = 1,
                Maximum       = 99999,
                Value         = 1,
                DecimalPlaces = 0,
                Dock          = DockStyle.Fill
            };

            btnAddLine = MakePrimaryBtn("＋  Add Line", Point.Empty, 180, 50);

            // Row 1: Raw Material + Type labels
            var tblMatLbl1 = MakeColTlp(2, new float[] { 60f, 40f });
            tblMatLbl1.Controls.Add(FieldLabel("Raw Material *", false), 0, 0);
            tblMatLbl1.Controls.Add(FieldLabel("Material Type",  false), 1, 0);

            // Row 2: Raw Material + Type controls
            var tblMatCtrl1 = MakeColTlp(2, new float[] { 60f, 40f });
            tblMatCtrl1.Controls.Add(Pad(cboRawMaterial),  0, 0);
            tblMatCtrl1.Controls.Add(Pad(txtMaterialType), 1, 0);

            // Row 3: Warehouse + Stock + Reorder labels
            var tblMatLbl2 = MakeColTlp(3, new float[] { 40f, 20f, 20f });
            tblMatLbl2.Controls.Add(FieldLabel("Warehouse / Stock Location *", false), 0, 0);
            tblMatLbl2.Controls.Add(FieldLabel("Current Stock (Ref)",          false), 1, 0);
            tblMatLbl2.Controls.Add(FieldLabel("Reorder Level (Ref)",          false), 2, 0);

            // Row 4: Warehouse + Stock + Reorder controls (+ Add button spanning right)
            var tblMatCtrl2 = MakeColTlp(4, new float[] { 40f, 20f, 20f, 20f });
            tblMatCtrl2.Controls.Add(Pad(cboWarehouse),    0, 0);
            tblMatCtrl2.Controls.Add(Pad(txtCurrentStock), 1, 0);
            tblMatCtrl2.Controls.Add(Pad(txtReorderLevel), 2, 0);

            // Row 5: Qty label
            var tblQtyLbl = MakeColTlp(2, new float[] { 30f, 70f });
            tblQtyLbl.Controls.Add(FieldLabel("Requested Qty *", false), 0, 0);

            // Row 6: Qty + Add button
            var tblQtyCtrl = MakeColTlp(2, new float[] { 30f, 70f });
            tblQtyCtrl.Controls.Add(Pad(nudRequestedQty), 0, 0);
            var pnlAddBtn = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 8, 12, 8) };
            btnAddLine.Dock = DockStyle.Left;
            pnlAddBtn.Controls.Add(btnAddLine);
            tblQtyCtrl.Controls.Add(pnlAddBtn, 1, 0);

            var tblMat = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(18, 4, 18, 8)
            };
            tblMat.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblMat.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));  // lbl row1
            tblMat.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f));  // ctrl row1
            tblMat.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));  // lbl row2
            tblMat.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f));  // ctrl row2
            tblMat.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));  // qty lbl
            tblMat.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f));  // qty ctrl
            tblMat.Controls.Add(tblMatLbl1,  0, 0);
            tblMat.Controls.Add(tblMatCtrl1, 0, 1);
            tblMat.Controls.Add(tblMatLbl2,  0, 2);
            tblMat.Controls.Add(tblMatCtrl2, 0, 3);
            tblMat.Controls.Add(tblQtyLbl,   0, 4);
            tblMat.Controls.Add(tblQtyCtrl,  0, 5);

            var (pnlCard3Outer, pnlCard3Inner) = CardPanel.Create(outerHeight: 450);
            var pnlMatContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlMatContent.Controls.Add(tblMat);
            pnlMatContent.Controls.Add(CardTitlePanel("Add Material Line"));
            pnlCard3Inner.Controls.Add(pnlMatContent);

            // ==================================================================
            // CARD 4 — Request Lines (DataGridView)
            // ==================================================================
            dgvLines = new DataGridView
            {
                Dock                  = DockStyle.Fill,
                BackgroundColor       = Color.White,
                BorderStyle           = BorderStyle.None,
                RowHeadersVisible     = false,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                ReadOnly              = true,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect           = false,
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                Font                  = new Font("Segoe UI", 11f),
                ColumnHeadersHeight   = 40
            };
            dgvLines.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 11f, FontStyle.Bold);
            dgvLines.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 244, 249);
            dgvLines.ColumnHeadersDefaultCellStyle.ForeColor = Palette.TextMain;
            dgvLines.DefaultCellStyle.SelectionBackColor     = Color.FromArgb(210, 225, 255);
            dgvLines.DefaultCellStyle.SelectionForeColor     = Palette.TextMain;
            dgvLines.RowTemplate.Height = 44;

            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLineNo",      HeaderText = "#",                    FillWeight = 5  });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMaterial",    HeaderText = "Raw Material",          FillWeight = 30 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colType",        HeaderText = "Type",                  FillWeight = 12 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colWarehouse",   HeaderText = "Warehouse / Location",  FillWeight = 33 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQty",         HeaderText = "Requested Qty",         FillWeight = 12 });

            btnRemoveLine = MakeOutlineBtn("✕  Remove Line", Point.Empty, 180, 44);
            lblLineCount  = new Label
            {
                Text      = "0 line(s) staged",
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(4, 0, 0, 0)
            };

            var pnlGridToolbar = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = Color.Transparent };
            btnRemoveLine.Dock = DockStyle.Right;
            pnlGridToolbar.Controls.Add(lblLineCount);
            pnlGridToolbar.Controls.Add(btnRemoveLine);

            var pnlGridInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(18, 0, 18, 12) };
            pnlGridInner.Controls.Add(dgvLines);
            pnlGridInner.Controls.Add(pnlGridToolbar);

            var (pnlCard4Outer, pnlCard4Inner) = CardPanel.Create(outerHeight: 340);
            var pnlGridContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlGridContent.Controls.Add(pnlGridInner);
            pnlGridContent.Controls.Add(CardTitlePanel("Request Lines"));
            pnlCard4Inner.Controls.Add(pnlGridContent);

            // ==================================================================
            // FOOTER — Submit + Reset
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
            pnlActionBtns.Resize += (s, ev) => CentreFooterBtns();

            var pnlFooterContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(4, 0, 0, 0) };
            pnlFooterContent.Controls.Add(pnlActionBtns);

            var footerInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            footerInner.Paint += (s, ev) =>
            {
                var p = (Panel)s;
                using var pen = new Pen(Palette.BorderColor, 1);
                ev.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
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

            // Stack cards (bottom-up in WinForms DockStyle.Top stacking)
            pnlScroll.Controls.Add(pnlCard4Outer);
            pnlScroll.Controls.Add(pnlCard3Outer);
            pnlScroll.Controls.Add(pnlOrderRow);
            pnlScroll.Controls.Add(pnlCard1Outer);

            pnlMain.Controls.Add(pnlScroll);
            pnlMain.Controls.Add(footerOuter);
            pnlMain.Controls.Add(_shell);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
            this.PerformLayout();

            _shell.Height      = AppShell.TotalHeight;
            _shell.MinimumSize = new System.Drawing.Size(0, AppShell.TotalHeight);
        }

        // ────────────────────────────────────────────────────────────────
        //  Shared builder helpers
        // ────────────────────────────────────────────────────────────────

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

        private static ComboBox MakeCombo() =>
            new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f), Dock = DockStyle.Fill };

        private static TextBox MakeReadOnlyBox() =>
            new TextBox
            {
                Font        = new Font("Segoe UI", 12f),
                ReadOnly    = true,
                BackColor   = Color.FromArgb(235, 240, 250),
                ForeColor   = Color.FromArgb(98, 112, 135),
                BorderStyle = BorderStyle.FixedSingle,
                Dock        = DockStyle.Fill
            };

        private static TableLayoutPanel MakeColTlp(int cols, float[] percents)
        {
            var tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = cols, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            foreach (var pct in percents)
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, pct));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            return tlp;
        }

        private static Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Palette.Primary,
                FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand
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
                Text = text, Font = new Font("Segoe UI", 12f),
                ForeColor = Palette.TextMain, BackColor = Color.White,
                FlatStyle = FlatStyle.Flat, Location = loc, Width = w, Height = h, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderColor        = Palette.BorderColor;
            b.FlatAppearance.BorderSize         = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            return b;
        }
    }
}
