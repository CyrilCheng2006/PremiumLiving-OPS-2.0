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
    /// Generate Delivery Note—inline dialog aligned to ShowDetailDialog visual standard.
    /// Layout (Top-to-Bottom, then Fill, then Bottom):
    ///   pnlHeader    Top  80  — dark navy + status badge (Margin 14px top/bottom)
    ///   pnlInfo      Top  220 — 4-col TLP read-only fields
    ///   pnlDNTitle   Top  44  — green title bar
    ///   pnlDNBody    Top  180 — delivery note preview (Ship Address multi-line)
    ///   pnlWarn      Top  48/0— warning strip (visible only if DN already exists)
    ///   pnlLineLabel Top  40  — “SHIPMENT ITEMS” bar
    ///   dgv          Fill     — shipment items grid
    ///   pnlTotalRow  Bottom 64— left: Lines Count / right: Total Amount (blue)
    ///   pnlFooter    Bottom 100— [✔ Confirm Generate 210×60]  [Cancel 210×60]
    /// </summary>
    public partial class GenerateDeliveryNoteForm : Form
    {
        private readonly LogisticsProcessingController _ctrl =
            new LogisticsProcessingController();
        private readonly ShipmentDetailVM _vm;

        public string GeneratedDeliveryID { get; private set; }

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

        private void BuildDialog()
        {
            var s = _vm.Shipment;
            StatusColors.TryGetValue(s?.ShipmentStatus ?? "", out var sc);

            int outQty = 0;
            foreach (var line in _vm.Lines ?? new List<ShipmentLineEntity>())
                outQty += line.QtyOutstanding ?? 0;

            this.Text            = $"Generate Delivery Note  —  {s?.ShipmentID}";
            this.Size            = new Size(1900, 1200);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.BackColor       = Color.White;
            this.Font            = new Font("Segoe UI", 13f);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;

            // ── Header — navy + status badge aligned to ShowDetailDialog
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            var tblHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(24, 0, 24, 0)
            };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 264f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblHeader.Controls.Add(new Label
            {
                Text      = $"Generate Delivery Note  —  {s?.ShipmentID}",
                Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);
            tblHeader.Controls.Add(new Label
            {
                Text      = s?.ShipmentStatus ?? "Unknown",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = sc.fg != default ? sc.fg : Color.White,
                BackColor = sc.bg != default ? sc.bg : Color.FromArgb(80, 80, 80),
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                AutoSize  = false, Margin = new Padding(0, 14, 0, 14)   // ★ badge margin (ShowDetailDialog standard)
            }, 1, 0);
            pnlHeader.Controls.Add(tblHeader);

            // ── Info Panel — 4-col read-only TLP
            var pnlInfo = new Panel
            {
                Dock = DockStyle.Top, Height = 220,
                Padding = new Padding(28, 18, 28, 8), BackColor = Color.White
            };
            pnlInfo.Paint += PaintBottomBorder;
            var tblInfo = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 4,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            for (int r = 0; r < 4; r++) tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));
            AddDetailRow(tblInfo, 0, "Shipment ID:",  s?.ShipmentID,                      "Order ID:",        s?.OrderID);
            AddDetailRow(tblInfo, 1, "Customer:",     s?.CustomerName,                    "Tracking No.:",    s?.TrackingNumber);
            AddDetailRow(tblInfo, 2, "Ship Date:",    s?.ShipDate.ToString("yyyy-MM-dd"), "Delivery Method:", s?.DeliveryMethod);
            AddDetailRow(tblInfo, 3, "Status:",       s?.ShipmentStatus,                  "Ship Type:",       s?.ShipmentType);
            pnlInfo.Controls.Add(tblInfo);

            // ── DN Preview title bar
            var pnlDNTitle = new Panel
            {
                Dock = DockStyle.Top, Height = 44,
                BackColor = Color.FromArgb(240, 253, 244), Padding = new Padding(28, 0, 16, 0)
            };
            pnlDNTitle.Paint += PaintBottomBorder;
            pnlDNTitle.Controls.Add(new Label
            {
                Text      = "\u2709  Delivery Note Preview  —  Fields to be Generated",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(6, 95, 70),
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            });

            // ── DN Preview body
            var pnlDNBody = new Panel
            {
                Dock = DockStyle.Top, Height = 180,
                BackColor = Color.FromArgb(249, 254, 251), Padding = new Padding(28, 12, 28, 12)
            };
            pnlDNBody.Paint += PaintBottomBorder;
            var tblDN = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 3,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblDN.RowStyles.Add(new RowStyle(SizeType.Percent, 28f));
            tblDN.RowStyles.Add(new RowStyle(SizeType.Percent, 44f));
            tblDN.RowStyles.Add(new RowStyle(SizeType.Percent, 28f));
            AddDetailRow(tblDN, 0, "Delivery Date:",   s?.ShipDate.ToString("yyyy-MM-dd"), "Ship To:",       s?.CustomerName);
            tblDN.Controls.Add(MakeLabelKey("Ship Address:"),                0, 1);
            tblDN.Controls.Add(MakeLabelValMultiline(s?.ShippingAddress ?? "—"), 1, 1);
            tblDN.Controls.Add(MakeLabelKey("Outstanding Qty:"),             2, 1);
            tblDN.Controls.Add(MakeLabelVal(outQty.ToString()),              3, 1);
            AddDetailRow(tblDN, 2, "Delivery Method:", s?.DeliveryMethod,                  "Shipment Type:", s?.ShipmentType);
            pnlDNBody.Controls.Add(tblDN);

            // ── Warning strip
            bool alreadyExists = _vm.DeliveryNote != null;
            var pnlWarn = new Panel
            {
                Dock = DockStyle.Top, Height = alreadyExists ? 48 : 0,
                BackColor = Color.FromArgb(254, 243, 199),
                Padding = new Padding(28, 0, 28, 0), Visible = alreadyExists
            };
            pnlWarn.Paint += PaintBottomBorder;
            if (alreadyExists)
                pnlWarn.Controls.Add(new Label
                {
                    Text      = $"\u26A0  A Delivery Note already exists: {_vm.DeliveryNote.DeliveryID}  " +
                                $"(Date: {_vm.DeliveryNote.DeliveryDate:yyyy-MM-dd})  —  Generation is blocked.",
                    Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(146, 64, 14),
                    Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
                });

            // ── Section label
            var pnlLineLabel = new Panel
            {
                Dock = DockStyle.Top, Height = 40,
                BackColor = Color.FromArgb(246, 249, 255), Padding = new Padding(28, 0, 0, 0)
            };
            pnlLineLabel.Controls.Add(new Label
            {
                Text = "SHIPMENT ITEMS",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            });
            pnlLineLabel.Paint += PaintBottomBorder;

            // ── Items grid
            var dgv = new DataGridView
            {
                ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(221, 227, 236), Font = new Font("Segoe UI", 12f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate = { Height = 44 }, Dock = DockStyle.Fill,
                ColumnHeadersHeight = 40, EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255), ForeColor = Color.FromArgb(98, 112, 135),
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold), Padding = new Padding(12, 0, 0, 0)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.White, ForeColor = Color.FromArgb(15, 31, 53),
                    SelectionBackColor = Color.FromArgb(219, 234, 254),
                    SelectionForeColor = Color.FromArgb(15, 31, 53),
                    Padding = new Padding(12, 6, 12, 6)
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

            // ── Total row — two-col (left: Lines Count / right: Total Amount in blue)
            // Matches ShowDetailDialog pnlTotalRow exactly
            var pnlTotalRow = new Panel { Dock = DockStyle.Bottom, Height = 64, BackColor = Color.White };
            pnlTotalRow.Paint += PaintTopBorder;
            var tblTotals = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblTotals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblTotals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblTotals.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblTotals.Controls.Add(new Label
            {
                Text      = $"Shipment Lines:   {(_vm.Lines?.Count ?? 0)}",
                Dock      = DockStyle.Fill, AutoSize = false,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(28, 0, 0, 0)
            }, 0, 0);
            tblTotals.Controls.Add(new Label
            {
                Text      = $"Total Amount:   HK$ {s?.TotalAmount:N2}",
                Dock      = DockStyle.Fill, AutoSize = false,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(47, 111, 237),
                TextAlign = ContentAlignment.MiddleRight,
                Padding   = new Padding(0, 0, 28, 0)
            }, 1, 0);
            pnlTotalRow.Controls.Add(tblTotals);

            // ── Footer — buttons positioned via SizeChanged (ShowDetailDialog standard)
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom, Height = 86,
                BackColor = Color.White, Padding = new Padding(28, 14, 28, 14)
            };
            pnlFooter.Paint += PaintTopBorder;

            var btnConfirm = new Button
            {
                Text      = "\u2714  Confirm Generate",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(5, 150, 105),
                FlatStyle = FlatStyle.Flat, Size = new Size(210, 56), Cursor = Cursors.Hand,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right, Enabled = !alreadyExists
            };
            btnConfirm.FlatAppearance.BorderSize         = 0;
            btnConfirm.FlatAppearance.MouseOverBackColor = Color.FromArgb(4, 120, 87);

            var btnCancel = new Button
            {
                Text      = "Cancel",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat, Size = new Size(160, 56), Cursor = Cursors.Hand,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };
            btnCancel.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnCancel.FlatAppearance.BorderSize         = 1;
            btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);

            // Dynamic right-anchored positioning
            pnlFooter.SizeChanged += (o, ev) =>
            {
                int top  = (pnlFooter.ClientSize.Height - 56) / 2;
                int rEdge = pnlFooter.ClientSize.Width - 28;
                btnConfirm.Location = new Point(rEdge - 210,            top);
                btnCancel.Location  = new Point(rEdge - 210 - 16 - 160, top);
            };
            btnConfirm.Location = new Point(1900 - 28 - 210,                 (86 - 56) / 2);
            btnCancel.Location  = new Point(1900 - 28 - 210 - 16 - 160,     (86 - 56) / 2);

            btnConfirm.Click += (o, ev) =>
            {
                try
                {
                    btnConfirm.Enabled  = false;
                    GeneratedDeliveryID = _ctrl.GenerateDeliveryNote(_vm.Shipment.ShipmentID);
                    MessageBox.Show(
                        $"Delivery Note generated successfully!\n\nDelivery Note ID: {GeneratedDeliveryID}",
                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (Exception ex)
                {
                    btnConfirm.Enabled = true;
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            btnCancel.Click += (o, ev) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            pnlFooter.Controls.Add(btnConfirm);
            pnlFooter.Controls.Add(btnCancel);

            // Assemble: Fill → Bottom (reverse) → Top (reverse)
            this.Controls.Add(dgv);
            this.Controls.Add(pnlTotalRow);
            this.Controls.Add(pnlFooter);
            this.Controls.Add(pnlLineLabel);
            this.Controls.Add(pnlWarn);
            this.Controls.Add(pnlDNBody);
            this.Controls.Add(pnlDNTitle);
            this.Controls.Add(pnlInfo);
            this.Controls.Add(pnlHeader);
        }

        // ── Helpers
        private static void AddDetailRow(
            TableLayoutPanel tbl, int row,
            string keyL, string valL, string keyR, string valR)
        {
            tbl.Controls.Add(MakeLabelKey(keyL),          0, row);
            tbl.Controls.Add(MakeLabelVal(valL ?? "—"),   1, row);
            tbl.Controls.Add(MakeLabelKey(keyR),          2, row);
            tbl.Controls.Add(MakeLabelVal(valR ?? "—"),   3, row);
        }

        private static Label MakeLabelKey(string text) => new Label
        {
            Text = text, Font = new Font("Segoe UI", 11f),
            ForeColor = Color.FromArgb(98, 112, 135),
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
        };

        private static Label MakeLabelVal(string text) => new Label
        {
            Text = text ?? "—", Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 31, 53),
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
        };

        private static Label MakeLabelValMultiline(string text) => new Label
        {
            Text = text ?? "—", Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 31, 53),
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopLeft,
            AutoSize = false, AutoEllipsis = false,
            MaximumSize = new Size(0, 0), Padding = new Padding(0, 6, 0, 0)
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
