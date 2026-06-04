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
        internal TextBox        txtPurchaseID;
        internal DateTimePicker dtpOrderDate;
        internal ComboBox       cboStatus;

        // ── CARD 2: Material Request & Supplier
        internal ComboBox cboMaterialRequest;
        internal ComboBox cboSupplier;
        internal TextBox  txtRawMaterialID;
        internal TextBox  txtRequestedQty;

        // ── CARD 3: Order Line Details
        internal ComboBox      cboWarehouse;
        internal NumericUpDown nudOrderQty;
        internal NumericUpDown nudUnitPrice;
        internal TextBox       txtLineTotal;

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
            this.BackColor     = Color.FromArgb(240, 244, 249);
            this.WindowState   = FormWindowState.Maximized;
            this.Font          = new Font("Segoe UI", 13f);

            // ── Root panel (Fill) — matches ViewOrderForm pnlMain pattern
            var pnlMain = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 244, 249)
            };

            // ── AppShell — CRITICAL: SetPopupContainer BEFORE adding to Controls
            _shell = new AppShell();
            _shell.SetPopupContainer(pnlMain);

            // ════════════════════════════════════════════════════════════
            // Scroll panel — holds all cards, sits below the AppShell
            // ════════════════════════════════════════════════════════════
            var pnlScroll = new Panel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                BackColor  = Color.FromArgb(240, 244, 249)
            };

            // ════════════════════════════════════════════════════════════
            // CARD 1 — Purchase Order Info
            //   Row: Purchase ID (auto) | Order Date | Status
            // ════════════════════════════════════════════════════════════
            txtPurchaseID = new TextBox
            {
                Font        = new Font("Segoe UI", 12f),
                ReadOnly    = true,
                BackColor   = Color.FromArgb(248, 250, 252),
                ForeColor   = Color.FromArgb(98, 112, 135),
                BorderStyle = BorderStyle.FixedSingle
            };
            dtpOrderDate = new DateTimePicker
            {
                Font   = new Font("Segoe UI", 12f),
                Format = DateTimePickerFormat.Short,
                Value  = DateTime.Today
            };
            cboStatus = new ComboBox
            {
                Font          = new Font("Segoe UI", 12f),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboStatus.Items.AddRange(new object[] { "Sent", "Cancelled", "Partially Received", "Received", "Completed" });
            cboStatus.SelectedIndex = 0;

            var tblHdrFields = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblHdrFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            tblHdrFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            tblHdrFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.4f));
            tblHdrFields.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblHdrFields.Controls.Add(MakeCell("Purchase ID (Auto)", txtPurchaseID,  true),  0, 0);
            tblHdrFields.Controls.Add(MakeCell("Order Date",         dtpOrderDate,   true),  1, 0);
            tblHdrFields.Controls.Add(MakeCell("Status",             cboStatus,      false), 2, 0);

            var pnlCard1 = BuildCard("Create Purchase Order", isSectionTitle: false, contentHeight: 90,
                                     outerPadding: new Padding(20, 14, 20, 0),
                                     content: tblHdrFields);

            // ════════════════════════════════════════════════════════════
            // CARD 2 — Material Request & Supplier
            //   Row 1: Material Request | Supplier
            //   Row 2: Raw Material ID (auto) | Requested Qty (ref)
            // ════════════════════════════════════════════════════════════
            cboMaterialRequest = new ComboBox { Font = new Font("Segoe UI", 12f), DropDownStyle = ComboBoxStyle.DropDownList };
            cboSupplier        = new ComboBox { Font = new Font("Segoe UI", 12f), DropDownStyle = ComboBoxStyle.DropDownList };
            txtRawMaterialID   = new TextBox  { Font = new Font("Segoe UI", 12f), ReadOnly = true, BackColor = Color.FromArgb(248,250,252), ForeColor = Color.FromArgb(98,112,135), BorderStyle = BorderStyle.FixedSingle };
            txtRequestedQty    = new TextBox  { Font = new Font("Segoe UI", 12f), ReadOnly = true, BackColor = Color.FromArgb(248,250,252), ForeColor = Color.FromArgb(98,112,135), BorderStyle = BorderStyle.FixedSingle };

            var tblReqRow1 = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblReqRow1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblReqRow1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblReqRow1.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblReqRow1.Controls.Add(MakeCell("Material Request", cboMaterialRequest, true),  0, 0);
            tblReqRow1.Controls.Add(MakeCell("Supplier",         cboSupplier,        false), 1, 0);

            var tblReqRow2 = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblReqRow2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblReqRow2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblReqRow2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblReqRow2.Controls.Add(MakeCell("Raw Material ID (Auto)", txtRawMaterialID, true),  0, 0);
            tblReqRow2.Controls.Add(MakeCell("Requested Qty (Ref)",    txtRequestedQty,  false), 1, 0);

            var tblReqContent = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblReqContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblReqContent.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            tblReqContent.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            tblReqContent.Controls.Add(tblReqRow1, 0, 0);
            tblReqContent.Controls.Add(tblReqRow2, 0, 1);

            var pnlCard2 = BuildCard("Material Request & Supplier", isSectionTitle: true, contentHeight: 165,
                                     outerPadding: new Padding(20, 12, 20, 0),
                                     content: tblReqContent);

            // ════════════════════════════════════════════════════════════
            // CARD 3 — Order Line Details
            //   Row 1: Delivery Warehouse | Order Qty | Unit Price
            //   Row 2: PO Total Amount
            // ════════════════════════════════════════════════════════════
            cboWarehouse = new ComboBox { Font = new Font("Segoe UI", 12f), DropDownStyle = ComboBoxStyle.DropDownList };
            nudOrderQty  = new NumericUpDown { Font = new Font("Segoe UI", 12f), Minimum = 1,  Maximum = 99999,     Value = 1,   DecimalPlaces = 0 };
            // Minimum = 0 so Value = 0 (blank state) is valid; > 0 enforced in Controller on submit
            nudUnitPrice = new NumericUpDown { Font = new Font("Segoe UI", 12f), Minimum = 0m, Maximum = 9_999_999m, Value = 0m, DecimalPlaces = 2 };
            txtLineTotal = new TextBox
            {
                Font = new Font("Segoe UI", 12f), ReadOnly = true, Text = "HK$ 0.00",
                BackColor = Color.FromArgb(248, 250, 252), ForeColor = Color.FromArgb(22, 163, 74),
                BorderStyle = BorderStyle.FixedSingle
            };

            var tblLineRow1 = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblLineRow1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            tblLineRow1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            tblLineRow1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            tblLineRow1.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblLineRow1.Controls.Add(MakeCell("Delivery Warehouse", cboWarehouse,  true),  0, 0);
            tblLineRow1.Controls.Add(MakeCell("Order Quantity",     nudOrderQty,   true),  1, 0);
            tblLineRow1.Controls.Add(MakeCell("Unit Price (HK$)",   nudUnitPrice,  false), 2, 0);

            var tblLineRow2 = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblLineRow2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            tblLineRow2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            tblLineRow2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblLineRow2.Controls.Add(MakeCell("PO Total Amount (HK$)", txtLineTotal, false), 0, 0);

            var tblLineContent = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblLineContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblLineContent.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            tblLineContent.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            tblLineContent.Controls.Add(tblLineRow1, 0, 0);
            tblLineContent.Controls.Add(tblLineRow2, 0, 1);

            var pnlCard3 = BuildCard("Order Line Details", isSectionTitle: true, contentHeight: 165,
                                     outerPadding: new Padding(20, 12, 20, 0),
                                     content: tblLineContent);

            // ════════════════════════════════════════════════════════════
            // CARD 4 — Action Buttons
            // ════════════════════════════════════════════════════════════
            btnSubmit = MakePrimaryBtn("\u2714  Submit Purchase Order", Point.Empty, 320, 60);
            btnReset  = MakeOutlineBtn("\u21ba  Reset Form",            Point.Empty, 220, 60);

            var pnlActBtns = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlActBtns.Controls.Add(btnSubmit);
            pnlActBtns.Controls.Add(btnReset);
            pnlActBtns.Resize += (s, ev) =>
            {
                var p = (Panel)s;
                btnSubmit.Left = p.Width - btnSubmit.Width - btnReset.Width - 16;
                btnSubmit.Top  = (p.Height - btnSubmit.Height) / 2;
                btnReset.Left  = p.Width - btnReset.Width - 8;
                btnReset.Top   = (p.Height - btnReset.Height) / 2;
            };

            var pnlCard4 = BuildCard(null, isSectionTitle: false, contentHeight: 60,
                                     outerPadding: new Padding(20, 12, 20, 20),
                                     content: pnlActBtns);

            // ════════════════════════════════════════════════════════════
            // Assemble scroll content
            // DockStyle.Top stacks in reverse add-order, so add BOTTOM-FIRST
            // ════════════════════════════════════════════════════════════
            pnlScroll.Controls.Add(pnlCard4);   // bottom
            pnlScroll.Controls.Add(pnlCard3);
            pnlScroll.Controls.Add(pnlCard2);
            pnlScroll.Controls.Add(pnlCard1);   // top

            // ════════════════════════════════════════════════════════════
            // Assemble pnlMain — ViewOrderForm pattern:
            //   Fill controls first, then Top controls in REVERSE visual order
            //   (last Top added = topmost on screen)
            // ════════════════════════════════════════════════════════════
            pnlMain.Controls.Add(pnlScroll);  // DockStyle.Fill — content area
            pnlMain.Controls.Add(_shell);     // DockStyle.Top  — AppShell (TopNavBar + UserBar)

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        // ── Card builder — mirrors CardPanel.Create() but uses plain Panels
        //    so the outer wrapper is DockStyle.Top (fixed height) and the inner
        //    is DockStyle.Fill (white card with border)
        private Panel BuildCard(string title, bool isSectionTitle, int contentHeight,
                                Padding outerPadding, Control content)
        {
            // Title row height (0 when no title)
            const int TitleH = 46;
            int titleRowH = (title != null) ? TitleH : 0;
            int outerH    = outerPadding.Top + outerPadding.Bottom + titleRowH + contentHeight + 28;

            // Outer wrapper — DockStyle.Top, coloured background + padding
            var pnlOuter = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = outerH,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding   = outerPadding
            };

            // White card with painted border
            var pnlCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlCard.Paint += PaintCardBorder;

            // Inner TLP: optional title row + content row
            int rowCount = (title != null) ? 2 : 1;
            var tbl = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                RowCount        = rowCount,
                ColumnCount     = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(20, 12, 20, 12)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            if (title != null)
            {
                tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, TitleH));
                tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                // Title panel
                var pnlTitle = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
                var lblTitle = new Label
                {
                    Text      = title,
                    Font      = isSectionTitle
                                    ? new Font("Segoe UI", 13f, FontStyle.Bold)
                                    : new Font("Segoe UI", 14f, FontStyle.Bold),
                    ForeColor = isSectionTitle
                                    ? Color.FromArgb(47, 111, 237)
                                    : Color.FromArgb(15, 31, 53),
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                var divider = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(221, 227, 236) };
                pnlTitle.Controls.Add(lblTitle);
                pnlTitle.Controls.Add(divider);
                tbl.Controls.Add(pnlTitle, 0, 0);
                tbl.Controls.Add(content,  0, 1);
            }
            else
            {
                tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                tbl.Controls.Add(content, 0, 0);
            }

            pnlCard.Controls.Add(tbl);
            pnlOuter.Controls.Add(pnlCard);
            return pnlOuter;
        }

        // ── Labelled-cell helper
        private static TableLayoutPanel MakeCell(string caption, Control ctrl, bool rightPad)
        {
            var tlp = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                RowCount        = 2,
                ColumnCount     = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = rightPad ? new Padding(0, 0, 12, 0) : Padding.Empty
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute,  34f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent,  100f));
            tlp.Controls.Add(new Label
            {
                Text      = caption,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft,
                Padding   = new Padding(0, 0, 0, 2)
            }, 0, 0);
            ctrl.Dock = DockStyle.Fill;
            tlp.Controls.Add(ctrl, 0, 1);
            return tlp;
        }

        // ── Button factories
        private static Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text      = text, Font = new Font("Segoe UI", 11f),
                ForeColor = Color.White, BackColor = Color.FromArgb(47, 111, 237),
                FlatStyle = FlatStyle.Flat, Location = loc, Size = new Size(w, h), Cursor = Cursors.Hand
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
                Text      = text, Font = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(98, 112, 135), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat, Location = loc, Size = new Size(w, h), Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize         = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            return b;
        }

        // ── Border painter
        private static void PaintCardBorder(object s, PaintEventArgs e)
        {
            var p = (Panel)s;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        }
    }
}
