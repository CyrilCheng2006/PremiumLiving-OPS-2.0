using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Models.Entities;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    /// <summary>
    /// Read-only dialog: Goods Received receipt header + detail lines.
    /// Visual language mirrors ViewShipmentForm.ShowDetailDialog:
    ///   • Dark header bar with Receipt ID + status badge
    ///   • 4-column info panel (label-key / label-val × 2 sides)
    ///   • Divider line, then full-width DataGrid for line items
    ///   • Footer strip with right-aligned Close button
    /// </summary>
    public class ReceiptDetailDialog : Form
    {
        private readonly GoodsReceivedEntity       _receipt;
        private readonly List<GoodsReceivedEntity> _lines;

        // Status badge colours — mirrors StatusTheme in HandlingGoodsReceivedForm
        private static readonly System.Collections.Generic.Dictionary<string, (Color bg, Color fg)>
            StatusColors = new System.Collections.Generic.Dictionary<string, (Color, Color)>(StringComparer.OrdinalIgnoreCase)
        {
            ["Sent"]               = (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)),
            ["Partially Received"] = (Color.FromArgb(219, 234, 254), Color.FromArgb( 29,  78, 216)),
            ["Received"]           = (Color.FromArgb(204, 251, 241), Color.FromArgb( 15, 118, 110)),
            ["Completed"]          = (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)),
            ["Cancelled"]          = (Color.FromArgb(241, 245, 249), Color.FromArgb( 71,  85, 105)),
        };

        public ReceiptDetailDialog(GoodsReceivedEntity receipt,
                                   List<GoodsReceivedEntity> lines)
        {
            _receipt = receipt ?? throw new ArgumentNullException(nameof(receipt));
            _lines   = lines   ?? new List<GoodsReceivedEntity>();
            BuildUI();
        }

        // ─────────────────────────────────────────────────────────────────
        //  UI construction
        // ─────────────────────────────────────────────────────────────────
        private void BuildUI()
        {
            // ―― Form shell ――
            Text            = $"Receipt Detail  —  {_receipt.ReceiptID}";
            Size            = new Size(1100, 700);
            MinimumSize     = new Size(900, 580);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = Color.White;
            Font            = new Font("Segoe UI", 12f);

            StatusColors.TryGetValue(_receipt.PurchaseStatus ?? "", out var sc);

            // ―― 1. Dark header bar ――
            var pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 72,
                BackColor = Color.FromArgb(19, 35, 61)
            };

            var tblHeader = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 2,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(28, 0, 28, 0)
            };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            tblHeader.Controls.Add(new Label
            {
                Text      = $"Receipt Details  —  {_receipt.ReceiptID}",
                Font      = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false
            }, 0, 0);

            var lblBadge = new Label
            {
                Text      = _receipt.PurchaseStatus ?? "Unknown",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = sc.fg != default ? sc.fg : Color.White,
                BackColor = sc.bg != default ? sc.bg : Color.FromArgb(80, 80, 80),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize  = false,
                Padding   = new Padding(8, 4, 8, 4)
            };
            tblHeader.Controls.Add(lblBadge, 1, 0);
            pnlHeader.Controls.Add(tblHeader);
            Controls.Add(pnlHeader);

            // ―― 2. Footer strip with Close button ――
            // Add footer BEFORE the content panels so DockStyle.Bottom works correctly.
            var pnlFooter = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 60,
                BackColor = Color.White,
                Padding   = new Padding(0, 10, 28, 10)
            };
            pnlFooter.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawLine(pen, 0, 0, ((Control)s).Width, 0);
            };

            var btnClose = new Button
            {
                Text      = "Close",
                Size      = new Size(110, 38),
                Anchor    = AnchorStyles.Right | AnchorStyles.Top,
                BackColor = Color.FromArgb(47, 111, 237),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Location = new Point(
                pnlFooter.Width - 110 - 28,
                (pnlFooter.Height - 38) / 2);
            btnClose.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            btnClose.Click += (s, e) => Close();
            pnlFooter.Controls.Add(btnClose);
            Controls.Add(pnlFooter);

            // ―― 3. Main content area (fills between header and footer) ――
            var pnlContent = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.White
            };
            Controls.Add(pnlContent);

            // ―― 3a. Info panel (4-column label-key / label-val grid) ――
            var pnlInfo = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 210,
                BackColor = Color.White,
                Padding   = new Padding(28, 18, 28, 10)
            };
            // Bottom separator line
            pnlInfo.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawLine(pen, 0, ((Control)s).Height - 1,
                                    ((Control)s).Width, ((Control)s).Height - 1);
            };

            var tblInfo = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 4,
                RowCount        = 4,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            // col 0 = left key (15%), col 1 = left val (35%)
            // col 2 = right key (15%), col 3 = right val (35%)
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            for (int r = 0; r < 4; r++)
                tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));

            // Left column fields
            var leftFields = new[]
            {
                ("Receipt ID",   _receipt.ReceiptID ?? ""),
                ("Supplier",     _receipt.SupplierName ?? ""),
                ("Qty Received", _receipt.QtyReceived.ToString()),
                ("Unit Price",   $"HK$ {_receipt.UnitPrice:F2}")
            };
            for (int i = 0; i < leftFields.Length; i++)
            {
                tblInfo.Controls.Add(MakeLabelKey(leftFields[i].Item1), 0, i);
                tblInfo.Controls.Add(MakeLabelVal(leftFields[i].Item2), 1, i);
            }

            // Right column fields
            var rightFields = new[]
            {
                ("Purchase ID",      _receipt.PurchaseID ?? ""),
                ("Receipt Date",     _receipt.ReceiptDate == default
                                        ? ""
                                        : _receipt.ReceiptDate.ToString("yyyy-MM-dd")),
                ("Outstanding Qty",  _receipt.OutstandingQty.ToString()),
                ("Warehouse",        _receipt.WarehouseLocation ?? "")
            };
            for (int i = 0; i < rightFields.Length; i++)
            {
                tblInfo.Controls.Add(MakeLabelKey(rightFields[i].Item1), 2, i);
                tblInfo.Controls.Add(MakeLabelVal(rightFields[i].Item2), 3, i);
            }
            pnlInfo.Controls.Add(tblInfo);
            pnlContent.Controls.Add(pnlInfo);

            // ―― 3b. Section title: "Line Items" ――
            var pnlGridTitle = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 40,
                BackColor = Color.White,
                Padding   = new Padding(28, 6, 28, 0)
            };
            pnlGridTitle.Controls.Add(new Label
            {
                Text      = "Line Items",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false
            });
            pnlContent.Controls.Add(pnlGridTitle);

            // ―― 3c. DataGrid ――
            var pnlGrid = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.White,
                Padding   = new Padding(28, 0, 28, 0)
            };

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
                Font                  = new Font("Segoe UI", 12f),
                RowTemplate           = { Height = 36 }
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor  = Color.FromArgb(248, 250, 252);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor  = Color.FromArgb(30, 41, 59);
            dgv.ColumnHeadersDefaultCellStyle.Font       = new Font("Segoe UI", 12f, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Padding    = new Padding(10, 0, 0, 0);
            dgv.DefaultCellStyle.Padding                 = new Padding(10, 0, 0, 0);
            dgv.EnableHeadersVisualStyles                = false;
            dgv.ColumnHeadersHeight                      = 40;
            dgv.ColumnHeadersHeightSizeMode              = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            // Alternating row shade
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItem",    HeaderText = "Item ID",          FillWeight = 15f });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName",    HeaderText = "Item Name",        FillWeight = 35f });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQtyRcvd", HeaderText = "Qty Received",     FillWeight = 15f });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOutQty",  HeaderText = "Outstanding Qty",  FillWeight = 15f });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPrice",   HeaderText = "Unit Price (HK$)", FillWeight = 20f });

            foreach (DataGridViewColumn col in dgv.Columns)
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            foreach (var line in _lines)
            {
                dgv.Rows.Add(
                    line.RawMaterialItemID,
                    line.ItemName,
                    line.QtyReceived,
                    line.OutstandingQty,
                    $"{line.UnitPrice:F2}");
            }

            // Empty-state placeholder
            if (_lines.Count == 0)
            {
                dgv.Rows.Add("", "(No line items found)", "", "", "");
                dgv.Rows[0].DefaultCellStyle.ForeColor = Color.FromArgb(148, 163, 184);
            }

            pnlGrid.Controls.Add(dgv);
            pnlContent.Controls.Add(pnlGrid);
        }

        // ─────────────────────────────────────────────────────────────────
        //  Label factory methods (mirror ViewShipmentForm helpers)
        // ─────────────────────────────────────────────────────────────────
        private static Label MakeLabelKey(string text)
            => new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false,
                Padding   = new Padding(4, 0, 0, 0)
            };

        private static Label MakeLabelVal(string text)
            => new Label
            {
                Text      = text ?? "",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false,
                Padding   = new Padding(4, 0, 0, 0)
            };
    }
}
