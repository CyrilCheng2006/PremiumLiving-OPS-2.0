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
    /// Generate Reply Slip — inline dialog, mirrors GenerateDeliveryNoteForm layout.
    /// Layout (Top-to-Bottom, then Fill, then Bottom):
    ///   pnlHeader    Top  80  — dark navy + status badge
    ///   pnlInfo      Top  220 — 4-col TLP read-only fields
    ///   pnlRSTitle   Top  44  — blue title bar
    ///   pnlRSBody    Top  200 — reply slip preview (Ship Address 2-line, row 50% height)
    ///   pnlWarn      Top  48/0— warning strip (visible if RS already exists)
    ///   pnlLineLabel Top  40  — "SHIPMENT ITEMS" bar
    ///   dgv          Fill     — shipment items grid
    ///   pnlTotalRow  Bottom 64— left: Lines Count / right: Total Amount (blue)
    ///   pnlFooter    Bottom 86— [✔ Confirm Generate 210×56]  [Cancel 160×56]
    /// </summary>
    public partial class GenerateReplySlipForm : Form
    {
        private readonly LogisticsProcessingController _ctrl =
            new LogisticsProcessingController();
        private readonly ShipmentDetailVM _vm;

        public string GeneratedSlipID { get; private set; }

        private static readonly Dictionary<string, (Color bg, Color fg)> StatusColors =
            new Dictionary<string, (Color, Color)>
            {
                { "Pending",    (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "In Transit", (Color.FromArgb(219, 234, 254), Color.FromArgb( 29,  78, 216)) },
                { "Completed",  (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
            };

        public GenerateReplySlipForm(ShipmentDetailVM vm)
        {
            _vm = vm ?? throw new ArgumentNullException(nameof(vm));
            InitializeComponent();
            BuildDialog();
        }

        private void BuildDialog()
        {
            var s = _vm.Shipment;
            (Color bg, Color fg) sc;
            StatusColors.TryGetValue(s != null ? (s.ShipmentStatus ?? "") : "", out sc);

            this.Text            = string.Format("Generate Reply Slip  —  {0}", s != null ? s.ShipmentID : "");
            this.Size            = new Size(1900, 1200);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.BackColor       = Color.White;
            this.Font            = new Font("Segoe UI", 13f);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;

            // ── Header
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
                Text      = string.Format("Generate Reply Slip  —  {0}", s != null ? s.ShipmentID : ""),
                Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);
            tblHeader.Controls.Add(new Label
            {
                Text      = s != null ? (s.ShipmentStatus ?? "Unknown") : "Unknown",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = sc.fg != Color.Empty ? sc.fg : Color.White,
                BackColor = sc.bg != Color.Empty ? sc.bg : Color.FromArgb(80, 80, 80),
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                AutoSize  = false, Margin = new Padding(0, 14, 0, 14)
            }, 1, 0);
            pnlHeader.Controls.Add(tblHeader);

            // ── Info Panel
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
            AddDetailRow(tblInfo, 0, "Shipment ID:",  s != null ? s.ShipmentID : "—",                         "Order ID:",        s != null ? s.OrderID : "—");
            AddDetailRow(tblInfo, 1, "Customer:",     s != null ? s.CustomerName : "—",                        "Tracking No.:",    s != null ? s.TrackingNumber : "—");
            AddDetailRow(tblInfo, 2, "Ship Date:",    s != null ? s.ShipDate.ToString("yyyy-MM-dd") : "—",    "Delivery Method:", s != null ? s.DeliveryMethod : "—");
            AddDetailRow(tblInfo, 3, "Status:",       s != null ? s.ShipmentStatus : "—",                     "Ship Type:",       s != null ? s.ShipmentType : "—");
            pnlInfo.Controls.Add(tblInfo);

            // ── RS Preview title bar (blue theme)
            var pnlRSTitle = new Panel
            {
                Dock = DockStyle.Top, Height = 44,
                BackColor = Color.FromArgb(239, 246, 255), Padding = new Padding(28, 0, 16, 0)
            };
            pnlRSTitle.Paint += PaintBottomBorder;
            pnlRSTitle.Controls.Add(new Label
            {
                Text      = "\U0001F9FE  Reply Slip Preview  —  Fields to be Generated",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(29, 78, 216),
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            });

            // ── RS Preview body
            // Row 1 (Ship Address) uses 50% height so there is enough vertical space for 2 lines.
            var pnlRSBody = new Panel
            {
                Dock = DockStyle.Top, Height = 200,
                BackColor = Color.FromArgb(248, 250, 255), Padding = new Padding(28, 12, 28, 12)
            };
            pnlRSBody.Paint += PaintBottomBorder;
            var tblRS = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 3,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblRS.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblRS.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblRS.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblRS.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblRS.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));  // row 0 — Slip Date / Ship To
            tblRS.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));  // row 1 — Ship Address (2-line)
            tblRS.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));  // row 2 — Delivery Method / Shipment Type

            AddDetailRow(tblRS, 0, "Slip Date:",
                DateTime.Today.ToString("yyyy-MM-dd"),
                "Ship To:", s != null ? s.CustomerName : "—");

            // Ship Address — multiline label (WordWrap = true, TopLeft aligned)
            tblRS.Controls.Add(MakeLabelKey("Ship Address:"), 0, 1);
            tblRS.Controls.Add(MakeLabelValMultiline(s != null ? s.ShippingAddress : null), 1, 1);
            tblRS.Controls.Add(MakeLabelKey("Tracking No.:"), 2, 1);
            tblRS.Controls.Add(MakeLabelVal(s != null ? s.TrackingNumber : "—"), 3, 1);

            AddDetailRow(tblRS, 2, "Delivery Method:", s != null ? s.DeliveryMethod : "—",
                "Shipment Type:", s != null ? s.ShipmentType : "—");
            pnlRSBody.Controls.Add(tblRS);

            // ── Warning strip
            bool alreadyExists = _vm.ReplySlip != null;
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
                    Text      = string.Format(
                        "\u26A0  A Reply Slip already exists: {0}  (Date: {1:yyyy-MM-dd})  —  Generation is blocked.",
                        _vm.ReplySlip.SlipID, _vm.ReplySlip.SlipDate),
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

            // ── Total row
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
                Text      = string.Format("Shipment Lines:   {0}", _vm.Lines != null ? _vm.Lines.Count : 0),
                Dock      = DockStyle.Fill, AutoSize = false,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(28, 0, 0, 0)
            }, 0, 0);
            tblTotals.Controls.Add(new Label
            {
                Text      = string.Format("Total Amount:   HK$ {0:N2}", s != null ? s.TotalAmount : 0),
                Dock      = DockStyle.Fill, AutoSize = false,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(47, 111, 237),
                TextAlign = ContentAlignment.MiddleRight,
                Padding   = new Padding(0, 0, 28, 0)
            }, 1, 0);
            pnlTotalRow.Controls.Add(tblTotals);

            // ── Footer
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
                ForeColor = Color.White, BackColor = Color.FromArgb(47, 111, 237),
                FlatStyle = FlatStyle.Flat, Size = new Size(210, 56), Cursor = Cursors.Hand,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right, Enabled = !alreadyExists
            };
            btnConfirm.FlatAppearance.BorderSize         = 0;
            btnConfirm.FlatAppearance.MouseOverBackColor = Color.FromArgb(26, 77, 192);

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

            pnlFooter.SizeChanged += delegate(object o, EventArgs ev)
            {
                int top   = (pnlFooter.ClientSize.Height - 56) / 2;
                int rEdge = pnlFooter.ClientSize.Width - 28;
                btnConfirm.Location = new Point(rEdge - 210,            top);
                btnCancel.Location  = new Point(rEdge - 210 - 16 - 160, top);
            };
            btnConfirm.Location = new Point(1900 - 28 - 210,             (86 - 56) / 2);
            btnCancel.Location  = new Point(1900 - 28 - 210 - 16 - 160, (86 - 56) / 2);

            btnConfirm.Click += delegate(object o, EventArgs ev)
            {
                try
                {
                    btnConfirm.Enabled = false;
                    GeneratedSlipID    = _ctrl.GenerateReplySlip(_vm.Shipment.ShipmentID);
                    MessageBox.Show(
                        string.Format("Reply Slip generated successfully!\n\nReply Slip ID: {0}", GeneratedSlipID),
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
            btnCancel.Click += delegate(object o, EventArgs ev)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            pnlFooter.Controls.Add(btnConfirm);
            pnlFooter.Controls.Add(btnCancel);

            // Assemble
            this.Controls.Add(dgv);
            this.Controls.Add(pnlTotalRow);
            this.Controls.Add(pnlFooter);
            this.Controls.Add(pnlLineLabel);
            this.Controls.Add(pnlWarn);
            this.Controls.Add(pnlRSBody);
            this.Controls.Add(pnlRSTitle);
            this.Controls.Add(pnlInfo);
            this.Controls.Add(pnlHeader);
        }

        // ── Helpers
        private static void AddDetailRow(
            TableLayoutPanel tbl, int row,
            string keyL, string valL, string keyR, string valR)
        {
            tbl.Controls.Add(MakeLabelKey(keyL),        0, row);
            tbl.Controls.Add(MakeLabelVal(valL ?? "—"), 1, row);
            tbl.Controls.Add(MakeLabelKey(keyR),        2, row);
            tbl.Controls.Add(MakeLabelVal(valR ?? "—"), 3, row);
        }

        private static Label MakeLabelKey(string text)
        {
            return new Label
            {
                Text = text, Font = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            };
        }

        private static Label MakeLabelVal(string text)
        {
            return new Label
            {
                Text = text ?? "—", Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            };
        }

        /// <summary>
        /// Multi-line label for Ship Address.
        /// WordWrap=true + TopLeft + the row at 50% panel height = 2-line display.
        /// </summary>
        private static Label MakeLabelValMultiline(string text)
        {
            return new Label
            {
                Text      = text ?? "—",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.TopLeft,
                AutoSize  = false,
                WordWrap  = true,
                Padding   = new Padding(0, 8, 8, 0)
            };
        }

        private static void PaintBottomBorder(object sender, PaintEventArgs e)
        {
            var p = (Panel)sender;
            Pen pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
            pen.Dispose();
        }

        private static void PaintTopBorder(object sender, PaintEventArgs e)
        {
            var p = (Panel)sender;
            Pen pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, 0, p.Width, 0);
            pen.Dispose();
        }
    }
}
