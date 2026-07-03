using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using PremiumLivingOPS.Services;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    /// <summary>
    /// Generate Delivery Note — layout (DockStyle.Top chain, bottom-up):
    ///   pnlHeader    Top  80  — navy header
    ///   pnlInfo      Top  220 — shipment summary (4-row, 4-col)
    ///   pnlDNTitle   Top  44  — green title bar
    ///   pnlDNBody    Top  220 — delivery note preview (Ship Address multi-line)
    ///   [warning]    Top  48  — amber bar (only when DN already exists)
    ///   pnlLineLabel Top  40  — "SHIPMENT ITEMS"
    ///   dgv          Fill     — line items grid
    ///   pnlTotalRow  Bottom 64
    ///   pnlFooter    Bottom 100
    /// </summary>
    public partial class GenerateDeliveryNoteForm : Form
    {
        private readonly LogisticsProcessingController _ctrl =
            new LogisticsProcessingController();
        private readonly ShipmentDetailVM _detail;

        public GenerateDeliveryNoteForm(ShipmentDetailVM detail)
        {
            InitializeComponent();
            _detail = detail;
            BuildUI();
        }

        private void BuildUI()
        {
            var s      = _detail?.Shipment;
            var lines  = _detail?.Lines  ?? new System.Collections.Generic.List<ShipmentLineEntity>();
            bool dnExists = _detail?.DeliveryNote != null;

            int outQty = 0;
            foreach (var ln in lines) outQty += ln.QtyOutstanding ?? 0;

            this.Text            = $"Generate Delivery Note  \u2014  {s?.ShipmentID}";
            this.Size            = new Size(2500, 1100);
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
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 264f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblHeader.Controls.Add(new Label
            {
                Text = $"Generate Delivery Note  \u2014  {s?.ShipmentID}",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);
            tblHeader.Controls.Add(new Label
            {
                Text = s?.ShipmentStatus ?? "Unknown",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(6, 95, 70), BackColor = Color.FromArgb(209, 250, 229),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false, Margin = new Padding(0, 14, 0, 14)
            }, 1, 0);
            pnlHeader.Controls.Add(tblHeader);

            // ── Shipment summary info
            var pnlInfo = new Panel
            {
                Dock = DockStyle.Top, Height = 220,
                Padding = new Padding(28, 18, 28, 8), BackColor = Color.White
            };
            pnlInfo.Paint += PaintBottomBorder;
            var tblInfo = Build4ColTlp(4);
            AddInfoRow(tblInfo, 0, "Shipment ID:",  s?.ShipmentID,                              "Order ID:",        s?.OrderID);
            AddInfoRow(tblInfo, 1, "Customer:",     s?.CustomerName,                            "Tracking No.:",    s?.TrackingNumber ?? "\u2014");
            AddInfoRow(tblInfo, 2, "Ship Date:",    s?.ShipDate.ToString("yyyy-MM-dd"),         "Delivery Method:", s?.DeliveryMethod);
            AddInfoRow(tblInfo, 3, "Status:",       s?.ShipmentStatus,                          "Ship Type:",       s?.ShipmentType);
            pnlInfo.Controls.Add(tblInfo);

            // ── DN green title bar
            var pnlDNTitle = new Panel
            {
                Dock = DockStyle.Top, Height = 44,
                BackColor = Color.FromArgb(240, 253, 244), Padding = new Padding(28, 0, 16, 0)
            };
            pnlDNTitle.Paint += PaintBottomBorder;
            pnlDNTitle.Controls.Add(new Label
            {
                Text      = "\u2709  Delivery Note Preview",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(6, 95, 70),
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            });

            // ── DN body (Ship Address multi-line)
            var pnlDNBody = new Panel
            {
                Dock = DockStyle.Top, Height = 220,
                BackColor = Color.FromArgb(249, 254, 251), Padding = new Padding(28, 12, 28, 12)
            };
            pnlDNBody.Paint += PaintBottomBorder;
            var tblDN = Build4ColTlp(3, 28f, 44f, 28f);
            AddInfoRow(tblDN, 0, "Delivery Date:",  DateTime.Today.ToString("yyyy-MM-dd"), "Ship To:",       s?.CustomerName);
            tblDN.Controls.Add(MakeLabelKey("Ship Address:"),                              0, 1);
            tblDN.Controls.Add(MakeLabelValMultiLine(s?.ShippingAddress ?? "\u2014"),      1, 1);
            tblDN.Controls.Add(MakeLabelKey("Outstanding Qty:"),                           2, 1);
            tblDN.Controls.Add(MakeLabelVal(outQty.ToString()),                            3, 1);
            AddInfoRow(tblDN, 2, "Delivery Method:", s?.DeliveryMethod, "Shipment Type:", s?.ShipmentType);
            pnlDNBody.Controls.Add(tblDN);

            // ── Warning bar (existing DN)
            var pnlWarn = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = dnExists ? 48 : 0,
                BackColor = Color.FromArgb(255, 251, 235),
                Padding   = new Padding(28, 0, 16, 0),
                Visible   = dnExists
            };
            pnlWarn.Paint += PaintBottomBorder;
            if (dnExists)
                pnlWarn.Controls.Add(new Label
                {
                    Text      = $"\u26A0  A Delivery Note already exists ({_detail.DeliveryNote.DeliveryID}). Generating again will overwrite it.",
                    Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(146, 64, 14),
                    Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
                });

            // ── Items section label
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
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "LINE ID",     FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ITEM ID",     FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ITEM NAME",   FillWeight = 34 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "QTY SHIPPED", FillWeight = 18 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "OUTSTANDING", FillWeight = 18 });
            foreach (var ln in lines)
                dgv.Rows.Add(ln.ShipmentLineID, ln.ItemID, ln.ItemName,
                             ln.QtyShipped, ln.QtyOutstanding?.ToString() ?? "\u2014");

            // ── Total row
            var pnlTotalRow = new Panel { Dock = DockStyle.Bottom, Height = 64, BackColor = Color.White };
            pnlTotalRow.Paint += PaintTopBorder;
            var tblTotal = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblTotal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblTotal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblTotal.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblTotal.Controls.Add(new Label
            {
                Text = $"Shipment Lines:   {lines.Count}",
                Dock = DockStyle.Fill, AutoSize = false,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(28, 0, 0, 0)
            }, 0, 0);
            tblTotal.Controls.Add(new Label
            {
                Text = $"Total Amount:   HK$ {s?.TotalAmount:N2}",
                Dock = DockStyle.Fill, AutoSize = false,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(47, 111, 237),
                TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 0, 28, 0)
            }, 1, 0);
            pnlTotalRow.Controls.Add(tblTotal);

            // ── Footer
            const int BtnW = 210, BtnH = 60;
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom, Height = 100,
                BackColor = Color.White, Padding = new Padding(28, 20, 28, 20)
            };
            pnlFooter.Paint += PaintTopBorder;

            var btnGen = new Button
            {
                Text      = "\u2714  Generate Delivery Note",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(22, 163, 74),
                FlatStyle = FlatStyle.Flat, Size = new Size(260, BtnH), Cursor = Cursors.Hand,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };
            btnGen.FlatAppearance.BorderSize         = 0;
            btnGen.FlatAppearance.MouseOverBackColor = Color.FromArgb(16, 131, 58);
            btnGen.FlatAppearance.MouseDownBackColor = Color.FromArgb(10, 100, 40);

            var btnCancel = new Button
            {
                Text      = "Cancel",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat, Size = new Size(160, BtnH), Cursor = Cursors.Hand,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };
            btnCancel.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnCancel.FlatAppearance.BorderSize         = 1;
            btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);

            pnlFooter.SizeChanged += (o, ev) =>
            {
                int top   = (pnlFooter.ClientSize.Height - BtnH) / 2;
                int rEdge = pnlFooter.ClientSize.Width - 28;
                btnGen.Location    = new Point(rEdge - 260,             top);
                btnCancel.Location = new Point(rEdge - 260 - 16 - 160, top);
            };
            btnGen.Location    = new Point(2500 - 28 - 260,             (100 - BtnH) / 2);
            btnCancel.Location = new Point(2500 - 28 - 260 - 16 - 160, (100 - BtnH) / 2);

            btnGen.Click += BtnGen_Click;
            btnCancel.Click += (_, __) => this.Close();

            pnlFooter.Controls.Add(btnGen);
            pnlFooter.Controls.Add(btnCancel);

            // ── Assemble (DockStyle.Top renders first-added at top)
            this.Controls.Add(pnlFooter);
            this.Controls.Add(pnlTotalRow);
            this.Controls.Add(dgv);
            this.Controls.Add(pnlLineLabel);
            this.Controls.Add(pnlWarn);
            this.Controls.Add(pnlDNBody);
            this.Controls.Add(pnlDNTitle);
            this.Controls.Add(pnlInfo);
            this.Controls.Add(pnlHeader);
        }

        private void BtnGen_Click(object sender, EventArgs e)
        {
            var s = _detail?.Shipment;
            if (s == null) return;
            try
            {
                string dnId = _ctrl.GenerateDeliveryNote(s.ShipmentID);
                MessageBox.Show($"Delivery Note {dnId} generated successfully.",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to generate Delivery Note:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── UI helpers
        private static TableLayoutPanel Build4ColTlp(int rows, params float[] rowHeights)
        {
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = rows,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            if (rowHeights.Length == 0)
                for (int r = 0; r < rows; r++)
                    tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rows));
            else
                foreach (float h in rowHeights)
                    tbl.RowStyles.Add(new RowStyle(SizeType.Percent, h));
            return tbl;
        }

        private static Label MakeLabelKey(string text) => new Label
        {
            Text = text, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(98, 112, 135),
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(0, 0, 8, 0), AutoEllipsis = false
        };

        private static Label MakeLabelVal(string text) => new Label
        {
            Text = text, Font = new Font("Segoe UI", 12f),
            ForeColor = Color.FromArgb(15, 31, 53),
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true
        };

        private static Label MakeLabelValMultiLine(string text) => new Label
        {
            Text = text, Font = new Font("Segoe UI", 12f),
            ForeColor = Color.FromArgb(15, 31, 53),
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopLeft,
            AutoEllipsis = false, AutoSize = false, Padding = new Padding(0, 8, 8, 4)
        };

        private static void AddInfoRow(
            TableLayoutPanel tbl, int row,
            string keyL, string valL, string keyR, string valR)
        {
            tbl.Controls.Add(MakeLabelKey(keyL),             0, row);
            tbl.Controls.Add(MakeLabelVal(valL ?? "\u2014"),  1, row);
            tbl.Controls.Add(MakeLabelKey(keyR),             2, row);
            tbl.Controls.Add(MakeLabelVal(valR ?? "\u2014"),  3, row);
        }

        private static void PaintBottomBorder(object s, PaintEventArgs e)
        {
            var p = (Panel)s;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
        }

        private static void PaintTopBorder(object s, PaintEventArgs e)
        {
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, 0, ((Control)s).Width, 0);
        }
    }
}
