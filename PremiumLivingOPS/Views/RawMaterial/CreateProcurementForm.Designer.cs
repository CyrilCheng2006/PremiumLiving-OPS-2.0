using PremiumLivingOPS.Views.Shared;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.RawMaterial
{
    partial class CreateProcurementForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            SuspendLayout();   // RULE 1

            // ── Form properties ────────────────────────────────────────────
            Name          = "CreateProcurementForm";
            Text          = "Premium Living OPS — Raw Material";
            Size          = new Size(1440, 900);
            MinimumSize   = new Size(1280, 800);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor     = Color.FromArgb(240, 244, 249);
            WindowState   = FormWindowState.Maximized;
            Font          = new Font("Segoe UI", 13f);
            AutoScaleMode = AutoScaleMode.Font;
            AutoScaleDimensions = new SizeF(7F, 15F);

            // ── Root panel ─────────────────────────────────────────────────
            pnlRoot = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 244, 249)
            };

            // ── AppShell (RULE 2) ──────────────────────────────────────────
            _shell = new AppShell();
            _shell.Dock        = DockStyle.Top;
            _shell.Height      = AppShell.TotalHeight;
            _shell.MinimumSize = new Size(0, AppShell.TotalHeight);
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;   // RULE 4
            _shell.LogoutClicked   += BtnLogout_Click;           // RULE 4
            _shell.SetPopupContainer(pnlRoot);

            // ── Scroll panel ──────────────────────────────────────────────
            pnlScroll = new Panel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                BackColor  = Color.FromArgb(240, 244, 249)
            };

            // ════════════════════════════════════════════════════════════════
            // CARD 1 — Page Header / Purchase Order Info (auto-generated ID)
            //   Row 1 : Purchase ID (read-only auto), Order Date, Status
            // ════════════════════════════════════════════════════════════════
            var (hdrOuter, hdrInner) = CardPanel.Create(outerHeight: 148,
                outerPadding: new Padding(20, 14, 20, 0));

            var tblHdr = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(20, 12, 20, 12)
            };
            tblHdr.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblHdr.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
            tblHdr.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // Title
            var pnlHdrTitle = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlHdrTitle.Controls.Add(new Label
            {
                Text      = "Create Purchase Order",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            });
            pnlHdrTitle.Controls.Add(new Panel
            {
                Dock = DockStyle.Bottom, Height = 1,
                BackColor = Color.FromArgb(221, 227, 236)
            });

            // Three fields: Purchase ID | Order Date | Status
            var tblHdrFields = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblHdrFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            tblHdrFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            tblHdrFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.4f));
            tblHdrFields.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            txtPurchaseID = new TextBox
            {
                Font = new Font("Segoe UI", 12f),
                ReadOnly = true,
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = Color.FromArgb(98, 112, 135),
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
                Font = new Font("Segoe UI", 12f),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboStatus.Items.AddRange(new object[]
                { "Sent", "Cancelled", "Partially Received", "Received", "Completed" });
            cboStatus.SelectedIndex = 0;

            tblHdrFields.Controls.Add(MakeCell("Purchase ID (Auto)",  txtPurchaseID,  true),  0, 0);
            tblHdrFields.Controls.Add(MakeCell("Order Date",          dtpOrderDate,   true),  1, 0);
            tblHdrFields.Controls.Add(MakeCell("Status",              cboStatus,      false), 2, 0);

            tblHdr.Controls.Add(pnlHdrTitle,  0, 0);
            tblHdr.Controls.Add(tblHdrFields, 0, 1);
            hdrInner.Controls.Add(tblHdr);

            // ════════════════════════════════════════════════════════════════
            // CARD 2 — Material Request & Supplier
            //   Row 1 : Material Request (dropdown), Supplier (dropdown)
            //   Row 2 : Raw Material ID (read-only auto-fill), Requested Qty (read-only)
            // ════════════════════════════════════════════════════════════════
            var (reqOuter, reqInner) = CardPanel.Create(outerHeight: 240,
                outerPadding: new Padding(20, 12, 20, 0));

            var tblReq = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(20, 12, 20, 12)
            };
            tblReq.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblReq.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
            tblReq.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            tblReq.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            var pnlReqTitle = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlReqTitle.Controls.Add(new Label
            {
                Text      = "Material Request & Supplier",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(47, 111, 237),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            });
            pnlReqTitle.Controls.Add(new Panel
            {
                Dock = DockStyle.Bottom, Height = 1,
                BackColor = Color.FromArgb(221, 227, 236)
            });

            cboMaterialRequest = new ComboBox
            {
                Font = new Font("Segoe UI", 12f),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboSupplier = new ComboBox
            {
                Font = new Font("Segoe UI", 12f),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            var tblRow1 = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblRow1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblRow1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblRow1.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblRow1.Controls.Add(MakeCell("Material Request",  cboMaterialRequest, true),  0, 0);
            tblRow1.Controls.Add(MakeCell("Supplier",          cboSupplier,        false), 1, 0);

            txtRawMaterialID = new TextBox
            {
                Font = new Font("Segoe UI", 12f), ReadOnly = true,
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = Color.FromArgb(98, 112, 135),
                BorderStyle = BorderStyle.FixedSingle
            };
            txtRequestedQty = new TextBox
            {
                Font = new Font("Segoe UI", 12f), ReadOnly = true,
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = Color.FromArgb(98, 112, 135),
                BorderStyle = BorderStyle.FixedSingle
            };

            var tblRow2 = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblRow2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblRow2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblRow2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblRow2.Controls.Add(MakeCell("Raw Material ID (Auto)", txtRawMaterialID, true),  0, 0);
            tblRow2.Controls.Add(MakeCell("Requested Qty (Ref)",    txtRequestedQty,  false), 1, 0);

            tblReq.Controls.Add(pnlReqTitle, 0, 0);
            tblReq.Controls.Add(tblRow1,     0, 1);
            tblReq.Controls.Add(tblRow2,     0, 2);
            reqInner.Controls.Add(tblReq);

            // ════════════════════════════════════════════════════════════════
            // CARD 3 — Order Line Details
            //   Row 1 : Warehouse, Order Qty, Unit Price
            //   Row 2 : Line Total (read-only computed)
            // ════════════════════════════════════════════════════════════════
            var (lineOuter, lineInner) = CardPanel.Create(outerHeight: 240,
                outerPadding: new Padding(20, 12, 20, 0));

            var tblLine = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(20, 12, 20, 12)
            };
            tblLine.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblLine.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
            tblLine.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            tblLine.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            var pnlLineTitle = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlLineTitle.Controls.Add(new Label
            {
                Text      = "Order Line Details",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(47, 111, 237),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            });
            pnlLineTitle.Controls.Add(new Panel
            {
                Dock = DockStyle.Bottom, Height = 1,
                BackColor = Color.FromArgb(221, 227, 236)
            });

            cboWarehouse = new ComboBox
            {
                Font = new Font("Segoe UI", 12f),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            nudOrderQty = new NumericUpDown
            {
                Font = new Font("Segoe UI", 12f),
                Minimum = 1, Maximum = 99999, Value = 1, DecimalPlaces = 0
            };
            nudUnitPrice = new NumericUpDown
            {
                Font = new Font("Segoe UI", 12f),
                Minimum = 0.01m, Maximum = 9999999m, Value = 0m,
                DecimalPlaces = 2
            };

            var tblLineRow1 = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblLineRow1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            tblLineRow1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            tblLineRow1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            tblLineRow1.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblLineRow1.Controls.Add(MakeCell("Delivery Warehouse", cboWarehouse,  true),  0, 0);
            tblLineRow1.Controls.Add(MakeCell("Order Quantity",     nudOrderQty,   true),  1, 0);
            tblLineRow1.Controls.Add(MakeCell("Unit Price (HK$)",   nudUnitPrice,  false), 2, 0);

            txtLineTotal = new TextBox
            {
                Font = new Font("Segoe UI", 12f), ReadOnly = true,
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = Color.FromArgb(22, 163, 74),
                BorderStyle = BorderStyle.FixedSingle,
                Text = "HK$ 0.00"
            };

            var tblLineRow2 = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblLineRow2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            tblLineRow2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            tblLineRow2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 0f));
            tblLineRow2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblLineRow2.Controls.Add(MakeCell("PO Total Amount (HK$)", txtLineTotal, false), 0, 0);

            tblLine.Controls.Add(pnlLineTitle, 0, 0);
            tblLine.Controls.Add(tblLineRow1,  0, 1);
            tblLine.Controls.Add(tblLineRow2,  0, 2);
            lineInner.Controls.Add(tblLine);

            // ════════════════════════════════════════════════════════════════
            // CARD 4 — Action Buttons
            // ════════════════════════════════════════════════════════════════
            var (actOuter, actInner) = CardPanel.Create(outerHeight: 96,
                outerPadding: new Padding(20, 12, 20, 20));

            var pnlActBtns = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            btnSubmit = MakePrimaryBtn("✔  Submit Purchase Order", Point.Empty, 320, 60);
            btnReset  = MakeOutlineBtn("↺  Reset Form",            Point.Empty, 220, 60);

            pnlActBtns.Layout += (s, ev) =>
            {
                var p = (Panel)s;
                btnSubmit.Left = p.Width - btnSubmit.Width - btnReset.Width - 16;
                btnSubmit.Top  = (p.Height - btnSubmit.Height) / 2;
                btnReset.Left  = p.Width - btnReset.Width - 8;
                btnReset.Top   = (p.Height - btnReset.Height) / 2;
            };
            pnlActBtns.Controls.Add(btnSubmit);
            pnlActBtns.Controls.Add(btnReset);
            actInner.Controls.Add(pnlActBtns);

            // ── Assemble scroll content (DockStyle.Top stacks bottom-up) ──
            pnlScroll.Controls.Add(actOuter);
            pnlScroll.Controls.Add(lineOuter);
            pnlScroll.Controls.Add(reqOuter);
            pnlScroll.Controls.Add(hdrOuter);

            // RULE 5: Fill first, Top second
            pnlRoot.Controls.Add(pnlScroll);   // Fill
            pnlRoot.Controls.Add(_shell);       // Top

            Controls.Add(pnlRoot);
            ResumeLayout(false);
            PerformLayout();

            // RULE 3 — post-layout re-enforcement
            _shell.Height      = AppShell.TotalHeight;
            _shell.MinimumSize = new Size(0, AppShell.TotalHeight);
        }

        // ── Button factories ──────────────────────────────────────────────
        private static Button MakePrimaryBtn(string text, Point loc, int w, int h)
        {
            var b = new Button
            {
                Text = text, Font = new Font("Segoe UI", 11f),
                ForeColor = Color.White, BackColor = Color.FromArgb(47, 111, 237),
                FlatStyle = FlatStyle.Flat, Location = loc, Size = new Size(w, h), Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
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
            b.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize  = 1;
            return b;
        }

        // ── Labelled-cell helper (same pattern as ViewRawMaterialForm) ─────
        private static TableLayoutPanel MakeCell(string caption, Control ctrl, bool rightPad)
        {
            var tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = rightPad ? new Padding(0, 0, 12, 0) : Padding.Empty
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
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

        // ── Field declarations ─────────────────────────────────────────────
        private Panel           pnlRoot;
        private AppShell        _shell;
        private Panel           pnlScroll;

        internal TextBox        txtPurchaseID;
        internal DateTimePicker dtpOrderDate;
        internal ComboBox       cboStatus;
        internal ComboBox       cboMaterialRequest;
        internal ComboBox       cboSupplier;
        internal TextBox        txtRawMaterialID;
        internal TextBox        txtRequestedQty;
        internal ComboBox       cboWarehouse;
        internal NumericUpDown  nudOrderQty;
        internal NumericUpDown  nudUnitPrice;
        internal TextBox        txtLineTotal;
        private  Button         btnSubmit;
        private  Button         btnReset;
    }
}
