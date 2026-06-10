using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    /// <summary>
    /// Generate Delivery Note — inline-rendered dialog
    ///
    /// MVC contract
    /// ─────────────────────────────────────────────────────────────────
    /// • Receives a pre-loaded ShipmentDetailVM from the caller (ViewShipmentForm).
    /// • Fully inline-rendered dialog following ShowDetailDialog visual language:
    ///     – pnlHeader      Top  80   — dark navy, Shipment ID + status badge
    ///     – pnlInfo        Top  220  — read-only 4-col TLP: Shipment fields
    ///     – pnlDNTitle     Top  44   — green title bar
    ///     – pnlDNBody      Top  140  — delivery note preview fields
    ///     – pnlWarn        Top  48/0 — warning strip (visible only if DN exists)
    ///     – pnlLineLabel   Top  40   — "SHIPMENT ITEMS" bar
    ///     – dgv            Fill      — shipment items grid
    ///     – pnlTotalRow    Bottom 50 — total amount
    ///     – pnlFooter      Bottom 80 — [✔ Confirm Generate] [Cancel]
    /// • Blocked (Confirm disabled + warning strip) if a Delivery Note already exists.
    /// • On Confirm: calls _ctrl.GenerateDeliveryNote(), sets DialogResult.OK.
    /// • Size: 1500 × 700, StartPosition CenterParent.
    /// </summary>
    public partial class GenerateDeliveryNoteForm : Form
    {
        private readonly LogisticsProcessingController _ctrl =
            new LogisticsProcessingController();
        private readonly ShipmentDetailVM _vm;

        public string GeneratedDeliveryID { get; private set; }

        // Status colour palette (matches schema ENUM values)
        private static readonly Dictionary<string, (Color bg, Color fg)> StatusColors =
            new Dictionary<string, (Color, Color)>
            {
                { "Pending",    (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "In Transit", (Color.FromArgb(219, 234, 254), Color.FromArgb( 29,  78, 216)) },
                { "Completed",  (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
            };

        public GenerateDeliveryNoteForm(ShipmentDetailVM vm)
        {
            _vm = vm ?? throw new ArgumentNullException(nameof(vm));
            InitializeComponent();
            BuildDialog();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Full inline build — mirrors ShowDetailDialog construction order
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void BuildDialog()
        {
            var s = _vm.Shipment;
            StatusColors.TryGetValue(s?.ShipmentStatus ?? "", out var sc);

            // Calculate outstanding qty from lines
            int outQty = 0;
            foreach (var line in _vm.Lines ?? new List<ShipmentLineEntity>())
                outQty += line.QtyOutstanding ?? 0;

            // ── Form properties ────────────────────────────────────────────────
            this.Text            = $"Generate Delivery Note  —  {s?.ShipmentID}";
            this.Size            = new Size(1500, 700);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.BackColor       = Color.White;
            this.Font            = new Font("Segoe UI", 13f);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;

            // ── Header ─────────────────────────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 80,
                BackColor = Color.FromArgb(19, 35, 61)
            };
            var tblHeader = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 2,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(24, 0, 24, 0)
            };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblHeader.Controls.Add(new Label
            {
                Text      = $"Generate Delivery Note  —  {s?.ShipmentID}",
                Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false
            }, 0, 0);
            tblHeader.Controls.Add(new Label
            {
                Text      = s?.ShipmentStatus ?? "Unknown",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = sc.fg != default ? sc.fg : Color.White,
                BackColor = sc.bg != default ? sc.bg : Color.FromArgb(80, 80, 80),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize  = false,
                Padding   = new Padding(8, 4, 8, 4)
            }, 1, 0);
            pnlHeader.Controls.Add(tblHeader);

            // ── Shipment Info panel (read-only, 4-col TLP) ────────────────────
            var pnlInfo = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 220,
                Padding   = new Padding(28, 18, 28, 8),
                BackColor = Color.White
            };
            pnlInfo.Paint += PaintBottomBorder;

            var tblInfo = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 4,
                RowCount        = 4,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            for (int r = 0; r < 4; r++)
                tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));

            AddDetailRow(tblInfo, 0, "Shipment ID:",    s?.ShipmentID,                       "Order ID:",        s?.OrderID);
            AddDetailRow(tblInfo, 1, "Customer:",       s?.CustomerName,                     "Tracking No.:",    s?.TrackingNumber);
            AddDetailRow(tblInfo, 2, "Ship Date:",      s?.ShipDate.ToString("yyyy-MM-dd"),  "Delivery Method:", s?.DeliveryMethod);
            AddDetailRow(tblInfo, 3, "Status:",         s?.ShipmentStatus,                   "Ship Type:",       s?.ShipmentType);
            pnlInfo.Controls.Add(tblInfo);

            // ── Delivery Note Preview title bar ────────────────────────────────
            var pnlDNTitle = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 44,
                BackColor = Color.FromArgb(240, 253, 244),
                Padding   = new Padding(28, 0, 16, 0)
            };
            pnlDNTitle.Paint += PaintBottomBorder;
            pnlDNTitle.Controls.Add(new Label
            {
                Text      = "\u2709  Delivery Note Preview  —  Fields to be Generated",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(6, 95, 70),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false
            });

            // ── Delivery Note Preview body ───────────────────────────────────
            var pnlDNBody = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 140,
                BackColor = Color.FromArgb(249, 254, 251),
                Padding   = new Padding(28, 12, 28, 12)
            };
            pnlDNBody.Paint += PaintBottomBorder;

            var tblDN = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 4,
                RowCount        = 3,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            for (int r = 0; r < 3; r++)
                tblDN.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f));

            // DeliveryDate = ShipDate; ShipToName = CustomerName (per schema logic)
            AddDetailRow(tblDN, 0, "Delivery Date:",   s?.ShipDate.ToString("yyyy-MM-dd"),  "Ship To:",         s?.CustomerName);
            AddDetailRow(tblDN, 1, "Ship Address:",    s?.ShippingAddress,                  "Outstanding Qty:", outQty.ToString());
            AddDetailRow(tblDN, 2, "Delivery Method:", s?.DeliveryMethod,                   "Shipment Type:",   s?.ShipmentType);
            pnlDNBody.Controls.Add(tblDN);

            // ── Already-Exists Warning strip ─────────────────────────────────
            bool alreadyExists = _vm.DeliveryNote != null;
            var pnlWarn = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = alreadyExists ? 48 : 0,
                BackColor = Color.FromArgb(254, 243, 199),
                Padding   = new Padding(28, 0, 28, 0),
                Visible   = alreadyExists
            };
            pnlWarn.Paint += PaintBottomBorder;
            if (alreadyExists)
            {
                pnlWarn.Controls.Add(new Label
                {
                    Text      = $"\u26A0  A Delivery Note already exists: {_vm.DeliveryNote.DeliveryID}  " +
                                $"(Date: {_vm.DeliveryNote.DeliveryDate:yyyy-MM-dd})  —  Generation is blocked.",
                    Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(146, 64, 14),
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoSize  = false
                });
            }

            // ── SHIPMENT ITEMS label bar ─────────────────────────────────────
            var pnlLineLabel = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 40,
                BackColor = Color.FromArgb(246, 249, 255),
                Padding   = new Padding(28, 0, 0, 0)
            };
            pnlLineLabel.Controls.Add(new Label
            {
                Text      = "SHIPMENT ITEMS",
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            });
            pnlLineLabel.Paint += PaintBottomBorder;

            // ── Items grid ─────────────────────────────────────────────────
            var dgv = new DataGridView
            {
                ReadOnly                  = true,
                AllowUserToAddRows        = false,
                RowHeadersVisible         = false,
                SelectionMode             = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor           = Color.White,
                BorderStyle               = BorderStyle.None,
                GridColor                 = Color.FromArgb(221, 227, 236),
                Font                      = new Font("Segoe UI", 12f),
                AutoSizeColumnsMode       = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle           = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate               = { Height = 44 },
                Dock                      = DockStyle.Fill,
                ColumnHeadersHeight       = 40,
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255),
                    ForeColor = Color.FromArgb(98, 112, 135),
                    Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                    Padding   = new Padding(12, 0, 0, 0)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor          = Color.White,
                    ForeColor          = Color.FromArgb(15, 31, 53),
                    SelectionBackColor = Color.FromArgb(219, 234, 254),
                    SelectionForeColor = Color.FromArgb(15, 31, 53),
                    Padding            = new Padding(12, 6, 12, 6)
                }
            };
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cLID",  HeaderText = "LINE ID",         FillWeight = 18 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cItem", HeaderText = "ITEM ID",         FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cName", HeaderText = "ITEM NAME",       FillWeight = 42 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cQty",  HeaderText = "QTY SHIPPED",     FillWeight = 13 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cOut",  HeaderText = "QTY OUTSTANDING", FillWeight = 13 });
            foreach (var line in _vm.Lines ?? new List<ShipmentLineEntity>())
                dgv.Rows.Add(line.ShipmentLineID, line.ItemID, line.ItemName,
                             line.QtyShipped, line.QtyOutstanding ?? 0);

            // ── Total row ──────────────────────────────────────────────────
            var pnlTotalRow = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 50,
                BackColor = Color.FromArgb(246, 249, 255),
                Padding   = new Padding(0, 0, 28, 0)
            };
            pnlTotalRow.Controls.Add(new Label
            {
                Text      = $"Total Amount:   HK$ {s?.TotalAmount:N2}",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize  = false
            });

            // ── Footer — [✔ Confirm Generate] [Cancel] ───────────────────
            var pnlFooter = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 80,
                BackColor = Color.White,
                Padding   = new Padding(0, 12, 28, 12)
            };
            pnlFooter.Paint += PaintTopBorder;

            var btnConfirm = new Button
            {
                Text      = "\u2714  Confirm Generate",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(5, 150, 105),
                FlatStyle = FlatStyle.Flat,
                Dock      = DockStyle.Right,
                Width     = 220,
                Cursor    = Cursors.Hand,
                Enabled   = !alreadyExists
            };
            btnConfirm.FlatAppearance.BorderSize         = 0;
            btnConfirm.FlatAppearance.MouseOverBackColor = Color.FromArgb(4, 120, 87);
            btnConfirm.Margin = new Padding(0, 0, 8, 0);

            var btnCancel = new Button
            {
                Text      = "Cancel",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock      = DockStyle.Right,
                Width     = 140,
                Cursor    = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnCancel.FlatAppearance.BorderSize         = 1;
            btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);

            btnConfirm.Click += (o, ev) =>
            {
                try
                {
                    btnConfirm.Enabled  = false;
                    GeneratedDeliveryID = _ctrl.GenerateDeliveryNote(_vm.Shipment.ShipmentID);

                    MessageBox.Show(
                        $"Delivery Note generated successfully!\n\nDelivery Note ID: {GeneratedDeliveryID}",
                        "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (Exception ex)
                {
                    btnConfirm.Enabled = true;
                    MessageBox.Show(ex.Message, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnCancel.Click += (o, ev) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            pnlFooter.Controls.Add(btnConfirm);
            pnlFooter.Controls.Add(btnCancel);

            // ── Assemble (Bottom → Fill → Top in DockStyle priority order) ───
            this.Controls.Add(dgv);
            this.Controls.Add(pnlTotalRow);
            this.Controls.Add(pnlLineLabel);
            this.Controls.Add(pnlWarn);
            this.Controls.Add(pnlDNBody);
            this.Controls.Add(pnlDNTitle);
            this.Controls.Add(pnlInfo);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlFooter);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Shared helpers — identical to ViewShipmentForm helpers
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private static void AddDetailRow(
            TableLayoutPanel tbl, int row,
            string keyL, string valL,
            string keyR, string valR)
        {
            tbl.Controls.Add(MakeLabelKey(keyL), 0, row);
            tbl.Controls.Add(MakeLabelVal(valL ?? "—"), 1, row);
            tbl.Controls.Add(MakeLabelKey(keyR), 2, row);
            tbl.Controls.Add(MakeLabelVal(valR ?? "—"), 3, row);
        }

        private static Label MakeLabelKey(string text, Color? fg = null) => new Label
        {
            Text      = text,
            Font      = new Font("Segoe UI", 11f),
            ForeColor = fg ?? Color.FromArgb(98, 112, 135),
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize  = false
        };

        private static Label MakeLabelVal(string text, Color? fg = null) => new Label
        {
            Text      = text ?? "—",
            Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = fg ?? Color.FromArgb(15, 31, 53),
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize  = false
        };

        private static void PaintBottomBorder(object sender, PaintEventArgs e)
        {
            var p = (Panel)sender;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
        }

        private static void PaintTopBorder(object sender, PaintEventArgs e)
        {
            var p = (Panel)sender;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, 0, p.Width, 0);
        }
    }
}
