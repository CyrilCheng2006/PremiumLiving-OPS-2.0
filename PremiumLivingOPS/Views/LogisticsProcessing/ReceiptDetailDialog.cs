using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    /// <summary>
    /// Read-only dialog that displays a Goods Received receipt header
    /// plus all its detail lines.
    /// Opened from HandlingGoodsReceivedForm via ShowReceiptDetail().
    /// </summary>
    public class ReceiptDetailDialog : Form
    {
        private readonly GoodsReceivedEntity       _receipt;
        private readonly List<GoodsReceivedEntity> _lines;

        public ReceiptDetailDialog(GoodsReceivedEntity receipt,
                                   List<GoodsReceivedEntity> lines)
        {
            _receipt = receipt ?? throw new ArgumentNullException(nameof(receipt));
            _lines   = lines   ?? new List<GoodsReceivedEntity>();
            InitForm();
        }

        private void InitForm()
        {
            Text            = $"Receipt Detail — {_receipt.ReceiptID}";
            Size            = new Size(860, 520);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = Color.FromArgb(243, 244, 246);
            Font            = new Font("Segoe UI", 11f);

            var outer = new Panel
            {
                Dock      = DockStyle.Fill,
                Padding   = new Padding(16),
                BackColor = Color.FromArgb(243, 244, 246)
            };
            Controls.Add(outer);

            var card = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.White,
                Padding   = new Padding(20)
            };
            card.Paint += (s, e) =>
            {
                var b = ((Control)s).ClientRectangle;
                b.Width--; b.Height--;
                using var pen = new Pen(Color.FromArgb(221, 227, 236), 1f);
                e.Graphics.DrawRectangle(pen, b);
            };
            outer.Controls.Add(card);

            var layout = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                RowCount    = 2,
                ColumnCount = 1,
                BackColor   = Color.Transparent
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 110f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent,  100f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            card.Controls.Add(layout);

            // ── Header ───────────────────────────────────────────────
            var header = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 4,
                RowCount    = 2,
                BackColor   = Color.Transparent,
                Padding     = new Padding(0, 0, 0, 10)
            };
            for (int c = 0; c < 4; c++)
                header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            header.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            header.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            AddField(header, "Receipt ID",     _receipt.ReceiptID,                          0, 0);
            AddField(header, "Purchase ID",    _receipt.PurchaseID ?? "",                  1, 0);
            AddField(header, "Supplier",       _receipt.SupplierName ?? "",                2, 0);
            AddField(header, "Receipt Date",   _receipt.ReceiptDate.ToString("yyyy-MM-dd"), 3, 0);
            AddField(header, "Warehouse",      _receipt.WarehouseLocation ?? "",           0, 1);
            AddField(header, "Status",         _receipt.PurchaseStatus ?? "",              1, 1);

            layout.Controls.Add(header, 0, 0);

            // ── Lines grid ────────────────────────────────────────────
            var dgv = new DataGridView
            {
                Dock                  = DockStyle.Fill,
                ReadOnly              = true,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible     = false,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor       = Color.White,
                BorderStyle           = BorderStyle.None,
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                Font                  = new Font("Segoe UI", 11f)
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 11f, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles               = false;

            dgv.Columns.Add("colItem",    "Item ID");
            dgv.Columns.Add("colName",    "Item Name");
            dgv.Columns.Add("colQtyRcvd", "Qty Received");
            dgv.Columns.Add("colOutQty",  "Outstanding Qty");
            dgv.Columns.Add("colPrice",   "Unit Price");

            foreach (var line in _lines)
            {
                dgv.Rows.Add(
                    line.RawMaterialItemID,
                    line.ItemName,
                    line.QtyReceived,
                    line.OutstandingQty,
                    $"${line.UnitPrice:F2}");
            }

            layout.Controls.Add(dgv, 0, 1);

            var btnClose = new Button
            {
                Text      = "Close",
                Size      = new Size(100, 36),
                BackColor = Color.FromArgb(47, 111, 237),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock      = DockStyle.Bottom,
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold)
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => Close();
            card.Controls.Add(btnClose);
        }

        private static void AddField(TableLayoutPanel tlp,
                                     string label, string value,
                                     int col, int row)
        {
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnl.Controls.Add(new Label
            {
                Text      = value,
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(30, 41, 59),
                AutoSize  = false,
                Dock      = DockStyle.Bottom,
                Height    = 22
            });
            pnl.Controls.Add(new Label
            {
                Text      = label,
                Font      = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize  = false,
                Dock      = DockStyle.Top,
                Height    = 18
            });
            tlp.Controls.Add(pnl, col, row);
        }
    }
}
