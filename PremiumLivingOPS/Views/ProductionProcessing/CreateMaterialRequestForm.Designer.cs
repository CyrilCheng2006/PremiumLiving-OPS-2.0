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

        // ── CARD 3: Order (shown only for OrderDemand trigger)
        internal Panel    pnlOrderRow;
        internal ComboBox cboOrder;

        // ── CARD 4: Request Details
        internal NumericUpDown nudRequestedQty;

        // ── CARD 5: Actions
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
            this.Text          = "Premium Living OPS — Production Processing";
            this.Size          = new Size(1440, 900);
            this.MinimumSize   = new Size(1280, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = Color.FromArgb(240, 244, 249);
            this.WindowState   = FormWindowState.Maximized;
            this.Font          = new Font("Segoe UI", 13f);

            // ── Root panel (Fill)
            var pnlMain = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 244, 249)
            };

            // ── AppShell (RULE 2)
            _shell = new AppShell();
            _shell.SetPopupContainer(pnlMain);
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += BtnLogout_Click;

            // ── Scroll panel
            var pnlScroll = new Panel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                BackColor  = Color.FromArgb(240, 244, 249)
            };

            // ════════════════════════════════════════════════════════════
            // CARD 1 — Request Header (ID + Urgency + Trigger)
            // ════════════════════════════════════════════════════════════
            txtRequestID = new TextBox
            {
                Font        = new Font("Segoe UI", 12f),
                ReadOnly    = true,
                BackColor   = Color.FromArgb(248, 250, 252),
                ForeColor   = Color.FromArgb(98, 112, 135),
                BorderStyle = BorderStyle.FixedSingle
            };
            cboUrgency = new ComboBox { Font = new Font("Segoe UI", 12f), DropDownStyle = ComboBoxStyle.DropDownList };
            cboUrgency.Items.AddRange(new object[] { "Critical", "High", "Medium" });
            cboUrgency.SelectedIndex = 0;

            cboTrigger = new ComboBox { Font = new Font("Segoe UI", 12f), DropDownStyle = ComboBoxStyle.DropDownList };
            cboTrigger.Items.AddRange(new object[] { "Reorder", "OrderDemand" });
            cboTrigger.SelectedIndex = 0;

            var tblHdr = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblHdr.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            tblHdr.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            tblHdr.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.4f));
            tblHdr.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblHdr.Controls.Add(MakeCell("Request ID (Auto)", txtRequestID, true),  0, 0);
            tblHdr.Controls.Add(MakeCell("Urgency Level",     cboUrgency,   true),  1, 0);
            tblHdr.Controls.Add(MakeCell("Trigger Type",      cboTrigger,   false), 2, 0);

            var pnlCard1 = BuildCard("Create Raw Material Request", isSectionTitle: false, contentHeight: 90,
                                     outerPadding: new Padding(20, 14, 20, 0), content: tblHdr);

            // ════════════════════════════════════════════════════════════
            // CARD 2 — Material & Warehouse Selection
            // ════════════════════════════════════════════════════════════
            cboRawMaterial  = new ComboBox { Font = new Font("Segoe UI", 12f), DropDownStyle = ComboBoxStyle.DropDownList };
            txtMaterialType = new TextBox  { Font = new Font("Segoe UI", 12f), ReadOnly = true, BackColor = Color.FromArgb(248,250,252), ForeColor = Color.FromArgb(98,112,135), BorderStyle = BorderStyle.FixedSingle };
            cboWarehouse    = new ComboBox { Font = new Font("Segoe UI", 12f), DropDownStyle = ComboBoxStyle.DropDownList, Enabled = false };
            txtCurrentStock = new TextBox  { Font = new Font("Segoe UI", 12f), ReadOnly = true, BackColor = Color.FromArgb(248,250,252), ForeColor = Color.FromArgb(98,112,135), BorderStyle = BorderStyle.FixedSingle };
            txtReorderLevel = new TextBox  { Font = new Font("Segoe UI", 12f), ReadOnly = true, BackColor = Color.FromArgb(248,250,252), ForeColor = Color.FromArgb(98,112,135), BorderStyle = BorderStyle.FixedSingle };

            cboWarehouse.SelectedIndexChanged += CboWarehouse_Changed;

            var tblMatRow1 = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblMatRow1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            tblMatRow1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            tblMatRow1.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblMatRow1.Controls.Add(MakeCell("Raw Material",    cboRawMaterial,  true),  0, 0);
            tblMatRow1.Controls.Add(MakeCell("Material Type",   txtMaterialType, false), 1, 0);

            var tblMatRow2 = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblMatRow2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblMatRow2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblMatRow2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblMatRow2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblMatRow2.Controls.Add(MakeCell("Warehouse / Stock Location (Auto)", cboWarehouse,    true),  0, 0);
            tblMatRow2.Controls.Add(MakeCell("Current Stock (Ref)",               txtCurrentStock, true),  1, 0);
            tblMatRow2.Controls.Add(MakeCell("Reorder Level (Ref)",               txtReorderLevel, false), 2, 0);

            var tblMatContent = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblMatContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblMatContent.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            tblMatContent.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            tblMatContent.Controls.Add(tblMatRow1, 0, 0);
            tblMatContent.Controls.Add(tblMatRow2, 0, 1);

            var pnlCard2 = BuildCard("Material & Warehouse", isSectionTitle: true, contentHeight: 165,
                                     outerPadding: new Padding(20, 12, 20, 0), content: tblMatContent);

            // ════════════════════════════════════════════════════════════
            // CARD 3 — Linked Order (visible only for OrderDemand)
            // ════════════════════════════════════════════════════════════
            cboOrder = new ComboBox { Font = new Font("Segoe UI", 12f), DropDownStyle = ComboBoxStyle.DropDownList };

            var tblOrderContent = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblOrderContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            tblOrderContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            tblOrderContent.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblOrderContent.Controls.Add(MakeCell("Linked Sales Order", cboOrder, false), 0, 0);

            pnlOrderRow = BuildCard("Linked Order", isSectionTitle: true, contentHeight: 90,
                                    outerPadding: new Padding(20, 12, 20, 0), content: tblOrderContent);
            pnlOrderRow.Visible = false;

            // ════════════════════════════════════════════════════════════
            // CARD 4 — Request Details
            // ════════════════════════════════════════════════════════════
            nudRequestedQty = new NumericUpDown
            {
                Font = new Font("Segoe UI", 12f),
                Minimum = 1, Maximum = 99999, Value = 1, DecimalPlaces = 0
            };

            var tblQtyContent = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblQtyContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            tblQtyContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            tblQtyContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.4f));
            tblQtyContent.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblQtyContent.Controls.Add(MakeCell("Requested Quantity", nudRequestedQty, false), 0, 0);

            var pnlCard4 = BuildCard("Request Details", isSectionTitle: true, contentHeight: 90,
                                     outerPadding: new Padding(20, 12, 20, 0), content: tblQtyContent);

            // ════════════════════════════════════════════════════════════
            // CARD 5 — Action Buttons
            // ════════════════════════════════════════════════════════════
            btnSubmit = MakePrimaryBtn("✔  Submit Request", Point.Empty, 300, 60);
            btnReset  = MakeOutlineBtn("↺  Reset Form",     Point.Empty, 220, 60);

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

            var pnlCard5 = BuildCard(null, isSectionTitle: false, contentHeight: 60,
                                     outerPadding: new Padding(20, 12, 20, 20), content: pnlActBtns);

            // ════════════════════════════════════════════════════════════
            // Assemble scroll content — Top stacks in reverse add-order
            // ════════════════════════════════════════════════════════════
            pnlScroll.Controls.Add(pnlCard5);      // bottom (action buttons)
            pnlScroll.Controls.Add(pnlCard4);      // request details
            pnlScroll.Controls.Add(pnlOrderRow);   // linked order (conditional)
            pnlScroll.Controls.Add(pnlCard2);      // material & warehouse
            pnlScroll.Controls.Add(pnlCard1);      // top (header)

            // ════════════════════════════════════════════════════════════
            // Assemble pnlMain (RULE 5)
            // ════════════════════════════════════════════════════════════
            pnlMain.Controls.Add(pnlScroll);  // DockStyle.Fill
            pnlMain.Controls.Add(_shell);     // DockStyle.Top — AppShell last = topmost

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
            this.PerformLayout();
            _shell.Height      = AppShell.TotalHeight;                          // RULE 3
            _shell.MinimumSize = new System.Drawing.Size(0, AppShell.TotalHeight); // RULE 3
        }

        // ── Card builder (mirrors CreateProcurementForm pattern)
        private Panel BuildCard(string title, bool isSectionTitle, int contentHeight,
                                Padding outerPadding, Control content)
        {
            const int TitleH   = 46;
            int titleRowH = title != null ? TitleH : 0;
            int outerH    = outerPadding.Top + outerPadding.Bottom + titleRowH + contentHeight + 28;

            var pnlOuter = new Panel
            {
                Dock = DockStyle.Top, Height = outerH,
                BackColor = Color.FromArgb(240, 244, 249), Padding = outerPadding
            };
            var pnlCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlCard.Paint += PaintCardBorder;

            int rowCount = title != null ? 2 : 1;
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = rowCount, ColumnCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(20, 12, 20, 12)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            if (title != null)
            {
                tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, TitleH));
                tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                var pnlTitle = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
                pnlTitle.Controls.Add(new Label
                {
                    Text = title,
                    Font = isSectionTitle
                               ? new Font("Segoe UI", 13f, FontStyle.Bold)
                               : new Font("Segoe UI", 14f, FontStyle.Bold),
                    ForeColor = isSectionTitle
                                    ? Color.FromArgb(47, 111, 237)
                                    : Color.FromArgb(15, 31, 53),
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
                });
                pnlTitle.Controls.Add(new Panel
                {
                    Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(221, 227, 236)
                });
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
                Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = rightPad ? new Padding(0, 0, 12, 0) : Padding.Empty
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute,  34f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent,  100f));
            tlp.Controls.Add(new Label
            {
                Text = caption, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135), Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft, Padding = new Padding(0, 0, 0, 2)
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
                Text = text, Font = new Font("Segoe UI", 11f),
                ForeColor = Color.White, BackColor = Color.FromArgb(47, 111, 237),
                FlatStyle = FlatStyle.Flat, Location = loc, Size = new Size(w, h), Cursor = Cursors.Hand
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
                Text = text, Font = new Font("Segoe UI", 11f),
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
