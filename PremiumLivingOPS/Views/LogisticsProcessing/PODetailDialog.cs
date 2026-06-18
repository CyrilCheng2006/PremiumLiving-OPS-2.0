using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    /// <summary>
    /// Read-only Purchase Order detail dialog.
    /// Shows header (PO ID, RequestID, Supplier with contact, Order Date,
    /// Total Amount, Invoice Status, PO Status) and all line items
    /// (POLineID, Item, Material Type, Warehouse, Qty, Unit Price, Subtotal).
    /// </summary>
    public class PODetailDialog : Form
    {
        private readonly PODetailVM _vm;

        private static readonly Dictionary<string, (Color bg, Color fg)>
            StatusColors = new Dictionary<string, (Color, Color)>(StringComparer.OrdinalIgnoreCase)
        {
            ["Sent"]               = (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)),
            ["Partially Received"] = (Color.FromArgb(219, 234, 254), Color.FromArgb( 29,  78, 216)),
            ["Received"]           = (Color.FromArgb(204, 251, 241), Color.FromArgb( 15, 118, 110)),
            ["Completed"]          = (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)),
            ["Cancelled"]          = (Color.FromArgb(241, 245, 249), Color.FromArgb( 71,  85, 105)),
        };

        public PODetailDialog(PODetailVM vm)
        {
            _vm = vm ?? throw new ArgumentNullException(nameof(vm));
            BuildUI();
        }

        private void BuildUI()
        {
            var po = _vm.PurchaseOrder;

            Text            = $"Purchase Order Detail  —  {po?.PurchaseID}";
            Size            = new Size(2200, 800);
            MinimumSize     = new Size(1200, 600);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = Color.White;
            Font            = new Font("Segoe UI", 12f);

            StatusColors.TryGetValue(po?.PurchaseStatus ?? "", out var sc);

            // ── 1. Dark header bar ───────────────────────────────────
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Color.FromArgb(19, 35, 61) };
            var tblHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(28, 0, 28, 0)
            };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblHeader.Controls.Add(new Label
            {
                Text      = $"Purchase Order Details  —  {po?.PurchaseID}",
                Font      = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);
            tblHeader.Controls.Add(new Label
            {
                Text      = po?.PurchaseStatus ?? "Unknown",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = sc.fg != default ? sc.fg : Color.White,
                BackColor = sc.bg != default ? sc.bg : Color.FromArgb(80, 80, 80),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false, Padding = new Padding(8, 4, 8, 4)
            }, 1, 0);
            pnlHeader.Controls.Add(tblHeader);
            Controls.Add(pnlHeader);

            // ── 2. Footer strip ──────────────────────────────────────
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom, Height = 60, BackColor = Color.White,
                Padding = new Padding(0, 10, 28, 10)
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
                btnClose.Location = new Point(pnlFooter.Width - 110 - 28, (pnlFooter.Height - 38) / 2);
            Controls.Add(pnlFooter);

            // ── 3. Main content panel ────────────────────────────────
            var pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            Controls.Add(pnlContent);

            // ── 3a. Info panel — 4 rows × 4 cols (key/val pairs) ────
            // Row 0: PO ID            | Order Date
            // Row 1: Request ID       | Supplier
            // Row 2: Supplier Phone   | Supplier Address
            // Row 3: Total Amount     | Invoice Status
            var pnlInfo = new Panel
            {
                Dock = DockStyle.Top, Height = 240,
                BackColor = Color.White, Padding = new Padding(28, 18, 28, 10)
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

            // Left column (0,1)
            var leftFields = new[]
            {
                ("PO ID",         po?.PurchaseID   ?? ""),
                ("Request ID",    po?.RequestID    ?? "(none)"),
                ("Supplier Phone",_vm.SupplierPhone ?? ""),
                ("Total Amount",  $"HK$ {po?.POTotalAmount:F2}")
            };
            for (int i = 0; i < leftFields.Length; i++)
            {
                tblInfo.Controls.Add(MakeLabelKey(leftFields[i].Item1), 0, i);
                tblInfo.Controls.Add(MakeLabelVal(leftFields[i].Item2), 1, i);
            }

            // Right column (2,3)
            var rightFields = new[]
            {
                ("Order Date",       po?.OrderDate == default ? "" : po.OrderDate.ToString("yyyy-MM-dd")),
                ("Supplier",         po?.SupplierName    ?? ""),
                ("Supplier Address", _vm.SupplierAddress ?? ""),
                ("Invoice Status",   _vm.InvoiceStatus   ?? "N/A")
            };
            for (int i = 0; i < rightFields.Length; i++)
            {
                tblInfo.Controls.Add(MakeLabelKey(rightFields[i].Item1), 2, i);
                tblInfo.Controls.Add(MakeLabelVal(rightFields[i].Item2), 3, i);
            }
            pnlInfo.Controls.Add(tblInfo);
            pnlContent.Controls.Add(pnlInfo);

            // ── 3b. Section title ────────────────────────────────────
            var pnlGridTitle = new Panel
            {
                Dock = DockStyle.Top, Height = 40,
                BackColor = Color.White, Padding = new Padding(28, 6, 28, 0)
            };
            pnlGridTitle.Controls.Add(new Label
            {
                Text = "Line Items",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59), Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            });
            pnlContent.Controls.Add(pnlGridTitle);

            // ── 3c. DataGrid ─────────────────────────────────────────
            var pnlGrid = new Panel
            {
                Dock = DockStyle.Fill, BackColor = Color.White,
                Padding = new Padding(28, 0, 28, 8)
            };
            var dgv = BuildDataGrid();

            // Columns: POLineID | Item Name | Mat. Type | WarehouseID | Warehouse Location | Qty | Unit Price | Subtotal
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLineID",   HeaderText = "Line ID",            FillWeight =  10f });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItem",     HeaderText = "Item",               FillWeight =  28f });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMatType",  HeaderText = "Material Type",      FillWeight =  10f });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colWHID",     HeaderText = "Warehouse ID",       FillWeight =  10f });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colWHLoc",    HeaderText = "Warehouse Location", FillWeight =  16f });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQty",      HeaderText = "Qty Ordered",        FillWeight =   8f });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUnit",     HeaderText = "Unit Price (HK$)",   FillWeight =   9f });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSubtotal", HeaderText = "Subtotal (HK$)",     FillWeight =   9f });
            AlignGridColumns(dgv);

            if (_vm.Lines != null)
                foreach (var line in _vm.Lines)
                    dgv.Rows.Add(
                        line.POLineID,
                        line.MaterialName      ?? line.RawMaterialItemID,
                        line.MaterialType      ?? "",
                        line.WarehouseID       ?? "",
                        line.WarehouseLocation ?? "",
                        line.OrderQty,
                        $"{line.UnitPrice:F2}",
                        $"{line.LineTotal:F2}");

            if (dgv.Rows.Count == 0)
                AddEmptyRow(dgv, 8);

            pnlGrid.Controls.Add(dgv);
            pnlContent.Controls.Add(pnlGrid);
        }

        // ── Helpers ──────────────────────────────────────────────────
        private static DataGridView BuildDataGrid()
        {
            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true,
                AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 12f), RowTemplate = { Height = 36 }
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
            dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 12f, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Padding   = new Padding(10, 0, 0, 0);
            dgv.DefaultCellStyle.Padding                = new Padding(10, 0, 0, 0);
            dgv.EnableHeadersVisualStyles               = false;
            dgv.ColumnHeadersHeight                     = 40;
            dgv.ColumnHeadersHeightSizeMode             = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            return dgv;
        }

        private static void AlignGridColumns(DataGridView dgv)
        {
            foreach (DataGridViewColumn col in dgv.Columns)
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        }

        private static void AddEmptyRow(DataGridView dgv, int colCount)
        {
            var row = new object[colCount];
            row[0] = "";
            row[1] = "(No line items found)";
            for (int i = 2; i < colCount; i++) row[i] = "";
            dgv.Rows.Add(row);
            dgv.Rows[0].DefaultCellStyle.ForeColor = Color.FromArgb(148, 163, 184);
        }

        private static void PaintTopBorder(object s, System.Windows.Forms.PaintEventArgs e)
        {
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, 0, ((Control)s).Width, 0);
        }

        private static void PaintBottomBorder(object s, System.Windows.Forms.PaintEventArgs e)
        {
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, ((Control)s).Height - 1, ((Control)s).Width, ((Control)s).Height - 1);
        }

        private static Label MakeLabelKey(string text) => new Label
        {
            Text = text, Font = new Font("Segoe UI", 10f),
            ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft, AutoSize = false,
            Padding = new Padding(4, 0, 0, 0)
        };

        private static Label MakeLabelVal(string text) => new Label
        {
            Text = text ?? "", Font = new Font("Segoe UI", 12f),
            ForeColor = Color.FromArgb(30, 41, 59), Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft, AutoSize = false,
            Padding = new Padding(4, 0, 0, 0)
        };
    }
}
