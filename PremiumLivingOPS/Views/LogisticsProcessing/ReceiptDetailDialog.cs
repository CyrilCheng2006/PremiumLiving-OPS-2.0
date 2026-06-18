using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    /// <summary>
    /// Read-only Receipt detail dialog.
    /// Header: ReceiptID, PurchaseID, POLineID, Item Name, SupplierName,
    ///         ReceiptDate, QtyReceived, OutstandingQty, WarehouseLocation, PO Status.
    /// Grid:   All Receipt rows sharing the same PurchaseID, sorted by ReceiptDate ASC.
    ///         Columns include Line Amount (Qty × UnitPrice).
    ///         Footer row shows grand totals.
    /// </summary>
    public class ReceiptDetailDialog : Form
    {
        private readonly ReceiptDetailVM _vm;

        private static readonly Dictionary<string, (Color bg, Color fg)>
            StatusColors = new Dictionary<string, (Color, Color)>(StringComparer.OrdinalIgnoreCase)
        {
            ["Sent"]               = (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)),
            ["Partially Received"] = (Color.FromArgb(219, 234, 254), Color.FromArgb( 29,  78, 216)),
            ["Received"]           = (Color.FromArgb(204, 251, 241), Color.FromArgb( 15, 118, 110)),
            ["Completed"]          = (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)),
            ["Cancelled"]          = (Color.FromArgb(241, 245, 249), Color.FromArgb( 71,  85, 105)),
        };

        public ReceiptDetailDialog(ReceiptDetailVM vm)
        {
            _vm = vm ?? throw new ArgumentNullException(nameof(vm));
            BuildUI();
        }

        private void BuildUI()
        {
            var r = _vm.Receipt;

            Text            = $"Receipt Detail  —  {r?.ReceiptID}  (PO: {r?.PurchaseID})";
            Size            = new Size(2200, 920);
            MinimumSize     = new Size(1200, 680);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = Color.FromArgb(244, 246, 250);  // light page bg
            Font            = new Font("Segoe UI", 12f);

            StatusColors.TryGetValue(r?.PurchaseStatus ?? "", out var sc);

            // ── 1. Dark header bar ───────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 72,
                BackColor = Color.FromArgb(19, 35, 61)
            };
            var tblHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(28, 0, 28, 0)
            };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblHeader.Controls.Add(new Label
            {
                Text      = $"Receipt Details  —  {r?.ReceiptID}",
                Font      = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);
            tblHeader.Controls.Add(new Label
            {
                Text      = r?.PurchaseStatus ?? "Unknown",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = sc.fg != default ? sc.fg : Color.White,
                BackColor = sc.bg != default ? sc.bg : Color.FromArgb(80, 80, 80),
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                AutoSize  = false, Padding = new Padding(8, 4, 8, 4)
            }, 1, 0);
            pnlHeader.Controls.Add(tblHeader);
            Controls.Add(pnlHeader);

            // ── 2. Footer strip ──────────────────────────────────────
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom, Height = 64,
                BackColor = Color.White,
                Padding = new Padding(0, 12, 32, 12)
            };
            pnlFooter.Paint += PaintTopBorder;
            var btnClose = new Button
            {
                Text = "Close", Size = new Size(110, 38),
                BackColor = Color.FromArgb(47, 111, 237), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            btnClose.Click += (s, e) => Close();
            pnlFooter.Controls.Add(btnClose);
            pnlFooter.Layout += (s, e) =>
                btnClose.Location = new Point(pnlFooter.Width - 110 - 32,
                                              (pnlFooter.Height - 38) / 2);
            Controls.Add(pnlFooter);

            // ── 3. Scrollable body ─────────────────────────────────
            var pnlBody = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(244, 246, 250),
                Padding   = new Padding(24, 20, 24, 16)   // ← top:20 = breathing space below header
            };
            Controls.Add(pnlBody);

            // ── 3a. White info card ───────────────────────────────
            // 5 rows × 4 cols
            // Row 0: Receipt ID       | Purchase ID
            // Row 1: PO Line ID       | Item Name
            // Row 2: Supplier         | Warehouse Location
            // Row 3: Receipt Date     | Outstanding Qty
            // Row 4: Qty Received     | (blank)
            var cardInfo = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 300,
                BackColor = Color.White,
                Padding   = new Padding(28, 20, 28, 16)
            };
            cardInfo.Paint += PaintRoundedCard;
            pnlBody.Controls.Add(cardInfo);

            var tblInfo = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 5,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            for (int i = 0; i < 5; i++)
                tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));

            string itemName = r?.ItemName ?? r?.RawMaterialItemID ?? "";

            var leftFields = new[]
            {
                ("Receipt ID",   r?.ReceiptID   ?? ""),
                ("PO Line ID",   r?.POLineID    ?? ""),
                ("Supplier",     r?.SupplierName ?? ""),
                ("Receipt Date", r?.ReceiptDate == default ? "" : r.ReceiptDate.ToString("yyyy-MM-dd")),
                ("Qty Received", (r?.QtyReceived ?? 0).ToString())
            };
            for (int i = 0; i < leftFields.Length; i++)
            {
                tblInfo.Controls.Add(MakeLabelKey(leftFields[i].Item1), 0, i);
                tblInfo.Controls.Add(MakeLabelVal(leftFields[i].Item2), 1, i);
            }

            var rightFields = new[]
            {
                ("Purchase ID",        r?.PurchaseID        ?? ""),
                ("Item Name",          itemName),
                ("Warehouse Location", r?.WarehouseLocation ?? ""),
                ("Outstanding Qty",    r?.OutstandingQty?.ToString() ?? "N/A"),
                ("",                   "")
            };
            for (int i = 0; i < rightFields.Length; i++)
            {
                tblInfo.Controls.Add(MakeLabelKey(rightFields[i].Item1), 2, i);
                var lbl = MakeLabelVal(rightFields[i].Item2);
                if (i == 1) lbl.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
                tblInfo.Controls.Add(lbl, 3, i);
            }
            cardInfo.Controls.Add(tblInfo);

            // ── 3b. Gap between cards ─────────────────────────────
            pnlBody.Controls.Add(new Panel
            {
                Dock = DockStyle.Top, Height = 16,
                BackColor = Color.Transparent
            });

            // ── 3c. White grid card ───────────────────────────────
            var cardGrid = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.White,
                Padding   = new Padding(0)
            };
            cardGrid.Paint += PaintRoundedCard;
            pnlBody.Controls.Add(cardGrid);

            var pnlGridTitle = new Panel
            {
                Dock = DockStyle.Top, Height = 48,
                BackColor = Color.White,
                Padding = new Padding(28, 12, 28, 0)
            };
            pnlGridTitle.Controls.Add(new Label
            {
                Text = $"All Receipts under PO  {r?.PurchaseID}",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            });
            cardGrid.Controls.Add(pnlGridTitle);

            var pnlDivider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(226, 232, 240) };
            cardGrid.Controls.Add(pnlDivider);

            var pnlGridWrap = new Panel
            {
                Dock = DockStyle.Fill, BackColor = Color.White,
                Padding = new Padding(28, 0, 28, 12)
            };
            var dgv = BuildDataGrid();

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colReceiptID",   HeaderText = "Receipt ID",         FillWeight = 12f });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colReceiptDate", HeaderText = "Receipt Date",        FillWeight = 10f });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPOLineID",    HeaderText = "PO Line ID",         FillWeight = 10f });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItem",        HeaderText = "Item",               FillWeight = 22f });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQtyRcvd",     HeaderText = "Qty Received",       FillWeight =  9f });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOutQty",      HeaderText = "Outstanding Qty",    FillWeight =  9f });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUnitPrice",   HeaderText = "Unit Price (HK$)",   FillWeight = 10f });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLineAmt",     HeaderText = "Line Amount (HK$)",  FillWeight = 10f });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colWarehouse",   HeaderText = "Warehouse Location", FillWeight =  8f });
            AlignGridColumns(dgv);
            AlignRightColumns(dgv, "colQtyRcvd", "colOutQty", "colUnitPrice", "colLineAmt");

            if (_vm.AllReceipts != null)
                foreach (var row in _vm.AllReceipts)
                {
                    double lineAmt = row.QtyReceived * row.UnitPrice;
                    int idx = dgv.Rows.Add(
                        row.ReceiptID,
                        row.ReceiptDate.ToString("yyyy-MM-dd"),
                        row.POLineID,
                        row.ItemName ?? row.RawMaterialItemID,
                        row.QtyReceived,
                        row.OutstandingQty?.ToString() ?? "N/A",
                        $"{row.UnitPrice:F2}",
                        $"{lineAmt:F2}",
                        row.WarehouseLocation ?? "");

                    if (row.ReceiptID == r?.ReceiptID)
                    {
                        dgv.Rows[idx].DefaultCellStyle.BackColor = Color.FromArgb(219, 234, 254);
                        dgv.Rows[idx].DefaultCellStyle.Font      = new Font("Segoe UI", 12f, FontStyle.Bold);
                    }
                }

            if (dgv.Rows.Count == 0)
                AddEmptyRow(dgv, 9);
            else
                AddTotalRow(dgv, _vm.TotalQtyReceived, _vm.TotalOutstanding, _vm.TotalLineAmount);

            pnlGridWrap.Controls.Add(dgv);
            cardGrid.Controls.Add(pnlGridWrap);
        }

        // ── Helpers ──────────────────────────────────────────────
        private static DataGridView BuildDataGrid()
        {
            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true,
                AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 12f), RowTemplate = { Height = 38 }
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
            dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 12f, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Padding   = new Padding(10, 0, 0, 0);
            dgv.DefaultCellStyle.Padding                = new Padding(10, 0, 0, 0);
            dgv.EnableHeadersVisualStyles               = false;
            dgv.ColumnHeadersHeight                     = 42;
            dgv.ColumnHeadersHeightSizeMode             = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            return dgv;
        }

        private static void AlignGridColumns(DataGridView dgv)
        {
            foreach (DataGridViewColumn col in dgv.Columns)
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        }

        private static void AlignRightColumns(DataGridView dgv, params string[] names)
        {
            foreach (var name in names)
                if (dgv.Columns.Contains(name))
                    dgv.Columns[name].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        private static void AddEmptyRow(DataGridView dgv, int colCount)
        {
            var row = new object[colCount];
            row[0] = ""; row[1] = "(No receipts found)";
            for (int i = 2; i < colCount; i++) row[i] = "";
            dgv.Rows.Add(row);
            dgv.Rows[0].DefaultCellStyle.ForeColor = Color.FromArgb(148, 163, 184);
        }

        private static void AddTotalRow(DataGridView dgv,
            int totalQtyRcvd, int totalOutstanding, double totalLineAmt)
        {
            int idx = dgv.Rows.Add(
                "", "", "", "TOTAL",
                totalQtyRcvd, totalOutstanding, "",
                $"{totalLineAmt:F2}", "");
            var style = dgv.Rows[idx].DefaultCellStyle;
            style.BackColor = Color.FromArgb(241, 245, 249);
            style.Font      = new Font("Segoe UI", 12f, FontStyle.Bold);
            style.ForeColor = Color.FromArgb(30, 41, 59);
            dgv.Rows[idx].Cells["colQtyRcvd"].Style.Alignment  = DataGridViewContentAlignment.MiddleRight;
            dgv.Rows[idx].Cells["colOutQty"].Style.Alignment   = DataGridViewContentAlignment.MiddleRight;
            dgv.Rows[idx].Cells["colLineAmt"].Style.Alignment  = DataGridViewContentAlignment.MiddleRight;
        }

        private static void PaintRoundedCard(object s, System.Windows.Forms.PaintEventArgs e)
        {
            var ctrl = (Control)s;
            using var pen = new Pen(Color.FromArgb(226, 232, 240), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, ctrl.Width - 1, ctrl.Height - 1);
        }

        private static void PaintTopBorder(object s, System.Windows.Forms.PaintEventArgs e)
        {
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, 0, ((Control)s).Width, 0);
        }

        private static Label MakeLabelKey(string text) => new Label
        {
            Text = text, Font = new Font("Segoe UI", 10f),
            ForeColor = Color.FromArgb(100, 116, 139),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft, AutoSize = false,
            Padding = new Padding(4, 0, 0, 0)
        };

        private static Label MakeLabelVal(string text) => new Label
        {
            Text = text ?? "", Font = new Font("Segoe UI", 12f),
            ForeColor = Color.FromArgb(30, 41, 59),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft, AutoSize = false,
            Padding = new Padding(4, 0, 0, 0)
        };
    }
}
