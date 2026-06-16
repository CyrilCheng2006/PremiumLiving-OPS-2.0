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

            // ── AppShell
            _shell = new AppShell();
            _shell.SetPopupContainer(pnlMain);
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += BtnLogout_Click;

            // ── Scroll panel — sits BELOW the AppShell via DockStyle.Fill
            //    AutoScroll lets it handle all card content regardless of window height
            var pnlScroll = new Panel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                BackColor  = Color.FromArgb(240, 244, 249)
            };

            // ════════════════════════════════════════════════════════════
            // CARD 1 — Request Header (ID + Urgency + Trigger)
            //
            // Schema: MaterialRequest.RequestID (auto), UrgencyLevel, TriggerType
            // contentHeight: label(38) + control(42) = 80 → use 100 with padding
            // ════════════════════════════════════════════════════════════
            txtRequestID = new TextBox
            {
                Font        = new Font("Segoe UI", 12f),
                ReadOnly    = true,
                BackColor   = Color.FromArgb(248, 250, 252),
                ForeColor   = Color.FromArgb(98, 112, 135),
                BorderStyle = BorderStyle.FixedSingle
            };
            cboUrgency = new ComboBox
            {
                Font          = new Font("Segoe UI", 12f),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboUrgency.Items.AddRange(new object[] { "Critical", "High", "Medium" });
            cboUrgency.SelectedIndex = 0;

            cboTrigger = new ComboBox
            {
                Font          = new Font("Segoe UI", 12f),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboTrigger.Items.AddRange(new object[] { "Reorder", "OrderDemand" });
            cboTrigger.SelectedIndex = 0;

            var tblHdr = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 3,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblHdr.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            tblHdr.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            tblHdr.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.4f));
            tblHdr.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblHdr.Controls.Add(MakeCell("Request ID (Auto)", txtRequestID, true),  0, 0);
            tblHdr.Controls.Add(MakeCell("Urgency Level",     cboUrgency,   true),  1, 0);
            tblHdr.Controls.Add(MakeCell("Trigger Type",      cboTrigger,   false), 2, 0);

            // contentHeight = label 38px + ComboBox 36px + vertical padding 10px = 84 → 100
            var pnlCard1 = BuildCard(
                "Create Raw Material Request",
                isSectionTitle: false,
                contentHeight:  100,
                outerPadding:   new Padding(20, 14, 20, 0),
                content:        tblHdr);

            // ════════════════════════════════════════════════════════════
            // CARD 2 — Material & Warehouse Selection
            //
            // Schema: RawMaterial (ItemID, ItemName, MaterialType),
            //         WarehouseItem (WarehouseItemID, WarehouseItemQuantity, ReorderLevel),
            //         Warehouse (WarehouseID, WarehouseLocation)
            //
            // Two sub-rows, each: label(38) + control(36) = 74 → 80 each → total 160 + gap 10 = 170
            // ════════════════════════════════════════════════════════════
            cboRawMaterial  = new ComboBox
            {
                Font          = new Font("Segoe UI", 12f),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            txtMaterialType = new TextBox
            {
                Font        = new Font("Segoe UI", 12f),
                ReadOnly    = true,
                BackColor   = Color.FromArgb(248, 250, 252),
                ForeColor   = Color.FromArgb(98, 112, 135),
                BorderStyle = BorderStyle.FixedSingle
            };
            cboWarehouse = new ComboBox
            {
                Font          = new Font("Segoe UI", 12f),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled       = false
            };
            txtCurrentStock = new TextBox
            {
                Font        = new Font("Segoe UI", 12f),
                ReadOnly    = true,
                BackColor   = Color.FromArgb(248, 250, 252),
                ForeColor   = Color.FromArgb(98, 112, 135),
                BorderStyle = BorderStyle.FixedSingle
            };
            txtReorderLevel = new TextBox
            {
                Font        = new Font("Segoe UI", 12f),
                ReadOnly    = true,
                BackColor   = Color.FromArgb(248, 250, 252),
                ForeColor   = Color.FromArgb(98, 112, 135),
                BorderStyle = BorderStyle.FixedSingle
            };

            cboWarehouse.SelectedIndexChanged += CboWarehouse_Changed;

            // Row 1: Raw Material (60%) | Material Type (40%)
            var tblMatRow1 = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 2,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblMatRow1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            tblMatRow1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            tblMatRow1.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblMatRow1.Controls.Add(MakeCell("Raw Material",  cboRawMaterial,  true),  0, 0);
            tblMatRow1.Controls.Add(MakeCell("Material Type", txtMaterialType, false), 1, 0);

            // Row 2: Warehouse (50%) | Current Stock (25%) | Reorder Level (25%)
            var tblMatRow2 = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 3,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblMatRow2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblMatRow2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblMatRow2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblMatRow2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblMatRow2.Controls.Add(MakeCell("Warehouse / Stock Location (Auto)", cboWarehouse,    true),  0, 0);
            tblMatRow2.Controls.Add(MakeCell("Current Stock (Ref)",               txtCurrentStock, true),  1, 0);
            tblMatRow2.Controls.Add(MakeCell("Reorder Level (Ref)",               txtReorderLevel, false), 2, 0);

            // Stack both rows: each row = 85px (label 38 + ctrl 36 + gap 11)
            // Total content = 85 + 8 (row gap) + 85 = 178 → use 180
            var tblMatContent = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                RowCount        = 2,
                ColumnCount     = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblMatContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblMatContent.RowStyles.Add(new RowStyle(SizeType.Absolute, 90f));   // Row 1 fixed height
            tblMatContent.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // Row 2 takes rest
            tblMatContent.Controls.Add(tblMatRow1, 0, 0);
            tblMatContent.Controls.Add(tblMatRow2, 0, 1);

            var pnlCard2 = BuildCard(
                "Material & Warehouse",
                isSectionTitle: true,
                contentHeight:  190,
                outerPadding:   new Padding(20, 12, 20, 0),
                content:        tblMatContent);

            // ════════════════════════════════════════════════════════════
            // CARD 3 — Linked Order (visible only for OrderDemand)
            //
            // Schema: Order.OrderID  (only when TriggerType = OrderDemand)
            // Height must be explicitly set so DockStyle.Top does not collapse it
            // ════════════════════════════════════════════════════════════
            cboOrder = new ComboBox
            {
                Font          = new Font("Segoe UI", 12f),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            var tblOrderContent = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 2,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblOrderContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            tblOrderContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            tblOrderContent.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblOrderContent.Controls.Add(MakeCell("Linked Sales Order", cboOrder, false), 0, 0);

            pnlOrderRow = BuildCard(
                "Linked Order",
                isSectionTitle: true,
                contentHeight:  100,
                outerPadding:   new Padding(20, 12, 20, 0),
                content:        tblOrderContent);
            pnlOrderRow.Visible = false;

            // ════════════════════════════════════════════════════════════
            // CARD 4 — Request Details
            //
            // Schema: MaterialRequest.RequestedQty
            // ════════════════════════════════════════════════════════════
            nudRequestedQty = new NumericUpDown
            {
                Font          = new Font("Segoe UI", 12f),
                Minimum       = 1,
                Maximum       = 99999,
                Value         = 1,
                DecimalPlaces = 0
            };

            var tblQtyContent = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 3,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblQtyContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            tblQtyContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            tblQtyContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.4f));
            tblQtyContent.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblQtyContent.Controls.Add(MakeCell("Requested Quantity", nudRequestedQty, false), 0, 0);

            var pnlCard4 = BuildCard(
                "Request Details",
                isSectionTitle: true,
                contentHeight:  100,
                outerPadding:   new Padding(20, 12, 20, 0),
                content:        tblQtyContent);

            // ════════════════════════════════════════════════════════════
            // CARD 5 — Action Buttons
            //
            // Uses a FlowLayoutPanel (right-aligned) so buttons never overlap
            // regardless of window resize — replaces manual Left/Top arithmetic
            // that caused overlapping in the old Resize lambda.
            // ════════════════════════════════════════════════════════════
            btnSubmit = MakePrimaryBtn("✔  Submit Request", 300, 52);
            btnReset  = MakeOutlineBtn("↺  Reset Form",     220, 52);

            var flpAct = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,   // right-to-left so Reset is leftmost
                WrapContents  = false,
                BackColor     = Color.Transparent
            };
            // Add Reset first (it ends up on the right in RightToLeft, which is leftmost visually)
            flpAct.Controls.Add(btnReset);
            flpAct.Controls.Add(btnSubmit);
            // Centre buttons vertically inside the FlowLayoutPanel
            flpAct.Resize += (s, ev) =>
            {
                var fp = (FlowLayoutPanel)s;
                int topPad = Math.Max(0, (fp.Height - btnSubmit.Height) / 2);
                fp.Padding = new Padding(0, topPad, 8, 0);
            };

            var pnlCard5 = BuildCard(
                null,
                isSectionTitle: false,
                contentHeight:  72,
                outerPadding:   new Padding(20, 12, 20, 20),
                content:        flpAct);

            // ════════════════════════════════════════════════════════════
            // Assemble scroll content
            //
            // CRITICAL: DockStyle.Top cards stack in reverse Controls.Add order.
            // The LAST card added appears at the TOP of the panel.
            // Add order (bottom to top) = Card5 → Card4 → Card3 → Card2 → Card1
            // ════════════════════════════════════════════════════════════
            pnlScroll.Controls.Add(pnlCard5);      // added 1st → appears LAST  (bottom)
            pnlScroll.Controls.Add(pnlCard4);      // added 2nd
            pnlScroll.Controls.Add(pnlOrderRow);   // added 3rd (conditional, toggled by Trigger)
            pnlScroll.Controls.Add(pnlCard2);      // added 4th
            pnlScroll.Controls.Add(pnlCard1);      // added LAST → appears FIRST (top)

            // ════════════════════════════════════════════════════════════
            // Assemble pnlMain
            //
            // AppShell must be added LAST so it renders on top of pnlScroll.
            // ════════════════════════════════════════════════════════════
            pnlMain.Controls.Add(pnlScroll);   // DockStyle.Fill — added 1st
            pnlMain.Controls.Add(_shell);      // DockStyle.Top  — added last → sits above pnlScroll

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
            this.PerformLayout();

            _shell.Height      = AppShell.TotalHeight;
            _shell.MinimumSize = new System.Drawing.Size(0, AppShell.TotalHeight);
        }

        // ────────────────────────────────────────────────────────────────
        //  BuildCard
        //  Three-layer nested card (outer grey padding → white card → TLP)
        //  matching CardPanel.cs three-layer nesting rule.
        //
        //  outerH = outerPadding + titleRow(if any) + contentHeight + inner card padding(28px)
        // ────────────────────────────────────────────────────────────────
        private Panel BuildCard(string title, bool isSectionTitle, int contentHeight,
                                Padding outerPadding, Control content)
        {
            const int TitleH    = 48;   // title row height
            const int CardPadV  = 28;   // inner TLP vertical padding (top 12 + bottom 16)
            int titleRowH = title != null ? TitleH : 0;
            int outerH    = outerPadding.Top + outerPadding.Bottom
                          + titleRowH + contentHeight + CardPadV;

            // Layer 1 — grey outer padding
            var pnlOuter = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = outerH,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding   = outerPadding
            };

            // Layer 2 — white card surface
            var pnlCard = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.White
            };
            pnlCard.Paint += PaintCardBorder;

            // Layer 3 — TLP inside the white card
            int rowCount = title != null ? 2 : 1;
            var tbl = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                RowCount        = rowCount,
                ColumnCount     = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(20, 12, 20, 16)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            if (title != null)
            {
                tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, TitleH));
                tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                // Title panel with bottom border line
                var pnlTitle = new Panel
                {
                    Dock      = DockStyle.Fill,
                    BackColor = Color.Transparent
                };
                pnlTitle.Controls.Add(new Label
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
                });
                pnlTitle.Controls.Add(new Panel
                {
                    Dock      = DockStyle.Bottom,
                    Height    = 1,
                    BackColor = Color.FromArgb(221, 227, 236)
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

        // ────────────────────────────────────────────────────────────────
        //  MakeCell — label on top, control below
        //
        //  Fixed issue: old code used RowStyle(Absolute, 34) for the label
        //  which was too small and caused the control row to intrude into
        //  the label area.  Now uses Absolute 38 for the label and Percent
        //  100 for the control so it fills whatever space remains.
        // ────────────────────────────────────────────────────────────────
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
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute,  38f));   // label row — was 34, now 38
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent,  100f));   // control fills remaining height

            tlp.Controls.Add(new Label
            {
                Text      = caption,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft,
                Padding   = new Padding(0, 0, 0, 4)   // 4px breathing room above control
            }, 0, 0);

            ctrl.Dock = DockStyle.Fill;
            tlp.Controls.Add(ctrl, 0, 1);
            return tlp;
        }

        // ── Button factories ─────────────────────────────────────────────
        private static Button MakePrimaryBtn(string text, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(47, 111, 237),
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(w, h),
                Cursor    = Cursors.Hand,
                Margin    = new Padding(8, 0, 0, 0)
            };
            b.FlatAppearance.BorderSize         = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(26, 77, 192);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(21, 60, 155);
            return b;
        }

        private static Button MakeOutlineBtn(string text, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(98, 112, 135),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(w, h),
                Cursor    = Cursors.Hand,
                Margin    = new Padding(0, 0, 0, 0)
            };
            b.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize         = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            return b;
        }

        // ── Card border painter ──────────────────────────────────────────
        private static void PaintCardBorder(object s, PaintEventArgs e)
        {
            var p = (Panel)s;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        }
    }
}
