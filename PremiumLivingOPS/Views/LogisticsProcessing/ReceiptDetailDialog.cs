using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
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
            BackColor       = Color.FromArgb(244, 246, 250);
            Font            = new Font("Segoe UI", 12f);

            StatusColors.TryGetValue(r?.PurchaseStatus ?? "", out var sc);

            // 1. Header bar
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

            // 2. Footer strip — height accommodates 60px button
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom, Height = 80,
                BackColor = Color.White,
                Padding = new Padding(0, 10, 32, 10)
            };
            pnlFooter.Paint += PaintTopBorder;
            var btnClose = new Button
            {
                Text = "Close", Size = new Size(210, 60),
                BackColor = Color.FromArgb(47, 111, 237), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            btnClose.Click += (s, e) => Close();
            pnlFooter.Controls.Add(btnClose);
            pnlFooter.Layout += (s, e) =>
                btnClose.Location = new Point(pnlFooter.Width - 210 - 32,
                                              (pnlFooter.Height - 60) / 2);
            Controls.Add(pnlFooter);

            // 3. Body — top:75 gap between HeaderBar and InfoPanel
            var pnlBody = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(244, 246, 250),
                Padding   = new Padding(24, 75, 24, 16)
            };
            Controls.Add(pnlBody);

            // 3a. White info card
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
                ("Receipt ID",   r?.ReceiptID    ?? ""),
                ("PO Line ID",   r?.POLineID     ?? ""),
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

            // 3b. Spacer between cards
            pnlBody.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 16, BackColor = Color.Transparent });

            // 3c. White grid card
            var cardGrid = new Panel
            {
                Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(0)
            };
            cardGrid.Paint += PaintRoundedCard;
            pnlBody.Controls.Add(cardGrid);

            var pnlGridTitle = new Panel
            {
                Dock = DockStyle.Top, Height = 48, BackColor = Color.White,
                Padding = new Padding(28, 12, 28, 0)
            };
            pnlGridTitle.Controls.Add(new Label
            {
                Text = $"All Receipts under PO  {r?.PurchaseID}",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            });
            cardGrid.Controls.Add(pnlGridTitle);
            cardGrid.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(226, 232, 240) });

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
                        row.ReceiptID, row.ReceiptDate.ToString("yyyy-MM-dd"),
                        row.POLineID, row.ItemName ?? row.RawMaterialItemID,
                        row.QtyReceived, row.OutstandingQty?.ToString() ?? "N/A",
                        $"{row.UnitPrice:F2}", $"{lineAmt:F2}",
                        row.WarehouseLocation ?? "");

                    if (row.ReceiptID == r?.ReceiptID)
                    {
                        dgv.Rows[idx].DefaultCellStyle.BackColor = Color.FromArgb(219, 234, 254);
                        dgv.Rows[idx].DefaultCellStyle.Font      = new Font("Segoe UI", 12f, FontStyle.Bold);
                    }
                }

            if (dgv.Rows.Count == 0) AddEmptyRow(dgv, 9);
            else AddTotalRow(dgv, _vm.TotalQtyReceived, _vm.TotalOutstanding, _vm.TotalLineAmount);

            pnlGridWrap.Controls.Add(dgv);
            cardGrid.Controls.Add(pnlGridWrap);
        }

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
        { foreach (DataGridViewColumn col in dgv.Columns) col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft; }

        private static void AlignRightColumns(DataGridView dgv, params string[] names)
        { foreach (var n in names) if (dgv.Columns.Contains(n)) dgv.Columns[n].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight; }

        private static void AddEmptyRow(DataGridView dgv, int cols)
        { var row = new object[cols]; row[0] = ""; row[1] = "(No receipts found)"; for (int i = 2; i < cols; i++) row[i] = ""; dgv.Rows.Add(row); dgv.Rows[0].DefaultCellStyle.ForeColor = Color.FromArgb(148, 163, 184); }

        private static void AddTotalRow(DataGridView dgv, int totalQty, int totalOut, double totalAmt)
        {
            int idx = dgv.Rows.Add("", "", "", "TOTAL", totalQty, totalOut, "", $"{totalAmt:F2}", "");
            var s = dgv.Rows[idx].DefaultCellStyle;
            s.BackColor = Color.FromArgb(241, 245, 249);
            s.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            s.ForeColor = Color.FromArgb(30, 41, 59);
            dgv.Rows[idx].Cells["colQtyRcvd"].Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgv.Rows[idx].Cells["colOutQty"].Style.Alignment  = DataGridViewContentAlignment.MiddleRight;
            dgv.Rows[idx].Cells["colLineAmt"].Style.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        private static void PaintRoundedCard(object s, System.Windows.Forms.PaintEventArgs e)
        { var c = (Control)s; using var p = new Pen(Color.FromArgb(226, 232, 240), 1); e.Graphics.DrawRectangle(p, 0, 0, c.Width - 1, c.Height - 1); }

        private static void PaintTopBorder(object s, System.Windows.Forms.PaintEventArgs e)
        { using var p = new Pen(Color.FromArgb(221, 227, 236), 1); e.Graphics.DrawLine(p, 0, 0, ((Control)s).Width, 0); }

        private static Label MakeLabelKey(string text) => new Label
        { Text = text, Font = new Font("Segoe UI", 10f), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false, Padding = new Padding(4, 0, 0, 0) };

        private static Label MakeLabelVal(string text) => new Label
        { Text = text ?? "", Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(30, 41, 59), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false, Padding = new Padding(4, 0, 0, 0) };
    }
}
