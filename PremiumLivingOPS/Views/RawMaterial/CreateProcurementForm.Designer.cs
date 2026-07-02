using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.RawMaterial
{
    partial class CreateProcurementForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── AppShell
        private AppShell _shell;

        // ── CARD 1: Purchase Order Header
        internal Label          lblPurchaseIDValue;
        internal DateTimePicker dtpOrderDate;
        internal ComboBox       cboStatus;
        internal ComboBox       cboSupplier;

        // ── CARD 2: Material Request selection
        internal ComboBox cboBatchPrefix;
        internal Label    lblBatchInfo;

        // ── CARD 3: Line Items Grid
        internal DataGridView dgvLines;
        internal Label        lblLineCount;
        internal Label        lblGrandTotal;

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

            this.Text          = "Premium Living OPS — Raw Material";
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
                Dock = DockStyle.Fill, AutoScroll = true, BackColor = Palette.BgPage
            };

            // ==================================================================
            // CARD 1 — Purchase Order Header
            //   Row 0 (lbl) : PO ID (auto)  |  Order Date  |  Status  |  Supplier
            //   Row 1 (ctrl): chip          |  dtp          |  cbo     |  cbo
            // ==================================================================
            lblPurchaseIDValue = new Label
            {
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Palette.Primary,
                BackColor = Color.FromArgb(219, 234, 254),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(10, 0, 0, 0)
            };
            var pnlIDChip = ChipPanel(lblPurchaseIDValue);

            dtpOrderDate = new DateTimePicker
            {
                Font = new Font("Segoe UI", 12f), Format = DateTimePickerFormat.Short,
                Value = DateTime.Today, Dock = DockStyle.Fill
            };

            cboStatus = MakeCombo();
            cboStatus.Items.AddRange(new object[] {
                "Sent", "Cancelled", "Partially Received", "Received", "Completed" });
            cboStatus.SelectedIndex = 0;

            cboSupplier = MakeCombo();

            var tblCard1 = MakeTlp(4, 2,
                new float[] { 25f, 20f, 20f, 35f },
                new float[] { 40f, 72f });
            tblCard1.Padding = new Padding(18, 8, 18, 8);
            tblCard1.Controls.Add(FieldLabel("Purchase Order ID",  false), 0, 0);
            tblCard1.Controls.Add(FieldLabel("Order Date",         true),  1, 0);
            tblCard1.Controls.Add(FieldLabel("Status",             true),  2, 0);
            tblCard1.Controls.Add(FieldLabel("Supplier",           true),  3, 0);
            tblCard1.Controls.Add(pnlIDChip,            0, 1);
            tblCard1.Controls.Add(Pad(dtpOrderDate),    1, 1);
            tblCard1.Controls.Add(Pad(cboStatus),       2, 1);
            tblCard1.Controls.Add(Pad(cboSupplier),     3, 1);

            var (c1Outer, c1Inner) = CardPanel.Create(outerHeight: 200);
            var c1Content = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            c1Content.Controls.Add(tblCard1);
            c1Content.Controls.Add(CardTitlePanel("Create Purchase Order"));
            c1Inner.Controls.Add(c1Content);

            // ==================================================================
            // CARD 2 — Material Request Selection
            //
            // Layout (inside CardPanel, below 54px CardTitle):
            //   Row 0  40px  — field label "Material Request (MRQ Batch) *"
            //   Row 1  72px  — cboBatchPrefix (full width)
            //   Row 2  20px  — sub-label "Request Info"
            //   Row 3  64px  — lblBatchInfo info panel
            //
            // Card outerHeight = 54 (title) + 40+72+20+64 (rows) + 16 (padding) = 266
            // → set to 290 for comfortable breathing room.
            // ==================================================================
            cboBatchPrefix = MakeCombo();

            // Info panel: styled box that shows Urgency / Trigger / item count
            // Uses a Panel wrapper so we can give it a visible background + border.
            lblBatchInfo = new Label
            {
                Text        = "— select a Material Request above —",
                Font        = new Font("Segoe UI", 11f),
                ForeColor   = Color.FromArgb(98, 112, 135),
                BackColor   = Color.FromArgb(235, 240, 250),
                Dock        = DockStyle.Fill,
                TextAlign   = ContentAlignment.MiddleLeft,
                Padding     = new Padding(14, 0, 0, 0),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Wrap label in a padded panel so left/right/bottom gaps are visible
            var pnlBatchInfoWrap = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(0, 4, 12, 8)
            };
            pnlBatchInfoWrap.Controls.Add(lblBatchInfo);

            var tblCard2 = MakeTlp(1, 4,
                new float[] { 100f },
                new float[] { 40f, 72f, 22f, 64f });
            tblCard2.Padding = new Padding(18, 8, 18, 8);
            tblCard2.Controls.Add(FieldLabel("Material Request (MRQ Batch)", true), 0, 0);
            tblCard2.Controls.Add(Pad(cboBatchPrefix),                               0, 1);
            tblCard2.Controls.Add(FieldLabel("Request Info",                false),  0, 2);
            tblCard2.Controls.Add(pnlBatchInfoWrap,                                  0, 3);

            // outerHeight = CardTitle(54) + rows(40+72+22+64) + tblPadding(16) + card margins(22) = 290
            var (c2Outer, c2Inner) = CardPanel.Create(outerHeight: 290);
            var c2Content = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            c2Content.Controls.Add(tblCard2);
            c2Content.Controls.Add(CardTitlePanel("Material Request"));
            c2Inner.Controls.Add(c2Content);

            // ==================================================================
            // CARD 3 — Line Items Grid
            //   # | Request ID | Raw Material | Type | Warehouse | Req Qty
            //   | Order Qty (editable) | Unit Price (editable) | Line Total
            // ==================================================================
            dgvLines = new DataGridView
            {
                Dock                  = DockStyle.Fill,
                BackgroundColor       = Color.White,
                BorderStyle           = BorderStyle.None,
                RowHeadersVisible     = false,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect           = false,
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                Font                  = new Font("Segoe UI", 11f),
                ColumnHeadersHeight   = 42,
                CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal,
                EnableHeadersVisualStyles = false
            };
            dgvLines.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 11f, FontStyle.Bold);
            dgvLines.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 244, 249);
            dgvLines.ColumnHeadersDefaultCellStyle.ForeColor = Palette.TextMain;
            dgvLines.ColumnHeadersDefaultCellStyle.Padding   = new Padding(8, 0, 0, 0);
            dgvLines.DefaultCellStyle.BackColor          = Color.White;
            dgvLines.DefaultCellStyle.ForeColor          = Palette.TextMain;
            dgvLines.DefaultCellStyle.SelectionBackColor = Color.FromArgb(210, 225, 255);
            dgvLines.DefaultCellStyle.SelectionForeColor = Palette.TextMain;
            dgvLines.DefaultCellStyle.Padding            = new Padding(8, 6, 8, 6);
            dgvLines.RowTemplate.Height = 52;

            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNo",        HeaderText = "#",           FillWeight =  4, ReadOnly = true });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colReqID",     HeaderText = "REQUEST ID",  FillWeight = 18, ReadOnly = true });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMaterial",  HeaderText = "RAW MATERIAL",FillWeight = 22, ReadOnly = true });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colType",      HeaderText = "TYPE",        FillWeight =  9, ReadOnly = true });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colWarehouse", HeaderText = "WAREHOUSE",   FillWeight = 18, ReadOnly = true });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colReqQty",    HeaderText = "REQ QTY",     FillWeight =  7, ReadOnly = true });

            var colOrderQty = new DataGridViewTextBoxColumn
            {
                Name = "colOrderQty", HeaderText = "ORDER QTY ✏", FillWeight = 8, ReadOnly = false
            };
            colOrderQty.DefaultCellStyle.BackColor = Color.FromArgb(255, 255, 235);
            colOrderQty.DefaultCellStyle.Font      = new Font("Segoe UI", 11f, FontStyle.Bold);
            dgvLines.Columns.Add(colOrderQty);

            var colUnitPrice = new DataGridViewTextBoxColumn
            {
                Name = "colUnitPrice", HeaderText = "UNIT PRICE ✏", FillWeight = 10, ReadOnly = false
            };
            colUnitPrice.DefaultCellStyle.BackColor = Color.FromArgb(255, 255, 235);
            colUnitPrice.DefaultCellStyle.Font      = new Font("Segoe UI", 11f, FontStyle.Bold);
            dgvLines.Columns.Add(colUnitPrice);

            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLineTotal", HeaderText = "LINE TOTAL",  FillWeight = 12, ReadOnly = true });

            lblLineCount = new Label
            {
                Text      = "0 line(s) loaded",
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(4, 0, 0, 0)
            };

            lblGrandTotal = new Label
            {
                Text      = "HK$ 0.00",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 163, 74),
                BackColor = Color.FromArgb(220, 252, 231),
                TextAlign = ContentAlignment.MiddleRight,
                Padding   = new Padding(0, 0, 16, 0),
                Dock      = DockStyle.Right,
                Width     = 240
            };

            var pnlGridToolbar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 54,
                BackColor = Color.Transparent,
                Padding   = new Padding(18, 6, 18, 6)
            };
            pnlGridToolbar.Controls.Add(lblGrandTotal);
            pnlGridToolbar.Controls.Add(lblLineCount);

            var pnlGridInner = new Panel
            {
                Dock = DockStyle.Fill, BackColor = Color.Transparent,
                Padding = new Padding(18, 0, 18, 12)
            };
            pnlGridInner.Controls.Add(dgvLines);
            pnlGridInner.Controls.Add(pnlGridToolbar);

            var (c3Outer, c3Inner) = CardPanel.Create(outerHeight: 420);
            var c3Content = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            c3Content.Controls.Add(pnlGridInner);
            c3Content.Controls.Add(CardTitlePanel("Request Line Items"));
            c3Inner.Controls.Add(c3Content);

            // ==================================================================
            // FOOTER
            // ==================================================================
            const int BtnW   = 260;
            const int BtnH   = 60;
            const int BtnGap = 8;
            const int BtnPad = 12;

            btnSubmit = MakePrimaryBtn("\u2714  Submit Purchase Order(s)", Point.Empty, BtnW, BtnH);
            btnReset  = MakeOutlineBtn("\u21ba  Reset Form",               Point.Empty, 180, BtnH);

            var pnlActionBtns = new Panel
            {
                Dock  = DockStyle.Right,
                Width = BtnPad + BtnW + BtnGap + 180 + BtnPad,
                BackColor = Color.Transparent
            };
            void CentreFooterBtns()
            {
                int top = Math.Max(0, (pnlActionBtns.Height - BtnH) / 2);
                btnSubmit.Location = new Point(BtnPad, top);
                btnReset.Location  = new Point(BtnPad + BtnW + BtnGap, top);
            }
            pnlActionBtns.Controls.Add(btnSubmit);
            pnlActionBtns.Controls.Add(btnReset);
            pnlActionBtns.Resize += (s, ev) => CentreFooterBtns();

            var pnlFooterContent = new Panel
            { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(4, 0, 0, 0) };
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
                Dock = DockStyle.Bottom, Height = 108,
                BackColor = Palette.BgPage, Padding = new Padding(20, 14, 20, 14)
            };
            footerOuter.Controls.Add(footerInner);

            // Assemble (DockStyle.Top stacks bottom-first in Controls.Add order)
            pnlScroll.Controls.Add(c3Outer);
            pnlScroll.Controls.Add(c2Outer);
            pnlScroll.Controls.Add(c1Outer);

            pnlMain.Controls.Add(pnlScroll);
            pnlMain.Controls.Add(footerOuter);
            pnlMain.Controls.Add(_shell);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);

            _shell.Height      = AppShell.TotalHeight;
            _shell.MinimumSize = new System.Drawing.Size(0, AppShell.TotalHeight);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static Panel Pad(Control ctrl)
        {
            ctrl.Dock = DockStyle.Fill;
            var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 8, 12, 8) };
            p.Controls.Add(ctrl);
            return p;
        }

        private static Panel ChipPanel(Label chip)
        {
            chip.Dock = DockStyle.Fill;
            var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 8, 12, 8) };
            p.Controls.Add(chip);
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
            pnl.Controls.Add(new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Palette.TextMain,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(18, 0, 0, 0)
            });
            pnl.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Palette.BorderColor });
            return pnl;
        }

        private static ComboBox MakeCombo() =>
            new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f), Dock = DockStyle.Fill };

        private static TableLayoutPanel MakeTlp(int cols, int rows, float[] colPcts, float[] rowHeights)
        {
            var tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = cols, RowCount = rows,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            foreach (var pct in colPcts)    tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, pct));
            foreach (var h   in rowHeights) tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, h));
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
