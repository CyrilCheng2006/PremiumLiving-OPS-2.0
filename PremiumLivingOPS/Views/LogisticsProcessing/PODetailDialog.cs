using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    /// <summary>
    /// Read-only dialog that displays a Purchase Order header and its line items.
    /// Opened from HandlingGoodsReceivedForm via ShowPODetail().
    /// Layout: CardPanel outer (grey) → white card → header fields + DataGridView lines.
    /// </summary>
    public class PODetailDialog : Form
    {
        private readonly PODetailVM _vm;

        public PODetailDialog(PODetailVM vm)
        {
            _vm = vm ?? throw new ArgumentNullException(nameof(vm));
            InitForm();
        }

        private void InitForm()
        {
            Text            = $"Purchase Order Detail — {_vm.PurchaseOrder?.PurchaseID}";
            Size            = new Size(860, 560);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = Color.FromArgb(243, 244, 246);
            Font            = new Font("Segoe UI", 11f);

            // ── Outer grey padding panel ──────────────────────────────
            var outer = new Panel
            {
                Dock    = DockStyle.Fill,
                Padding = new Padding(16),
                BackColor = Color.FromArgb(243, 244, 246)
            };
            Controls.Add(outer);

            // ── White card ────────────────────────────────────────────
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

            // ── Layout inside card ────────────────────────────────────
            var layout = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                RowCount    = 2,
                ColumnCount = 1,
                BackColor   = Color.Transparent
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 140f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent,  100f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            card.Controls.Add(layout);

            // ── PO Header fields ──────────────────────────────────────
            var header = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 4,
                RowCount    = 2,
                BackColor   = Color.Transparent,
                Padding     = new Padding(0, 0, 0, 12)
            };
            for (int c = 0; c < 4; c++)
                header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            header.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            header.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            var po = _vm.PurchaseOrder;
            AddHeaderField(header, "PO ID",         po?.PurchaseID    ?? "",       0, 0);
            AddHeaderField(header, "Supplier",       po?.SupplierName  ?? "",       1, 0);
            AddHeaderField(header, "Order Date",     po?.OrderDate.ToString("yyyy-MM-dd") ?? "", 2, 0);
            AddHeaderField(header, "Total Amount",   $"${po?.POTotalAmount:F2}",    3, 0);
            AddHeaderField(header, "Status",         po?.PurchaseStatus ?? "",      0, 1);

            layout.Controls.Add(header, 0, 0);

            // ── PO Lines grid ─────────────────────────────────────────
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

            dgv.Columns.Add("colLineID",   "Line ID");
            dgv.Columns.Add("colItem",     "Item");
            dgv.Columns.Add("colQty",      "Qty Ordered");
            dgv.Columns.Add("colUnit",     "Unit Price");
            dgv.Columns.Add("colSubtotal", "Subtotal");

            foreach (var line in _vm.Lines)
            {
                // fix CS1061: use MaterialName and OrderQty (matching PurchaseOrderLineEntity)
                dgv.Rows.Add(
                    line.POLineID,
                    line.MaterialName ?? line.RawMaterialItemID,
                    line.OrderQty,
                    $"${line.UnitPrice:F2}",
                    $"${line.OrderQty * line.UnitPrice:F2}");
            }

            layout.Controls.Add(dgv, 0, 1);

            // ── Close button ──────────────────────────────────────────
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

        private static void AddHeaderField(TableLayoutPanel tlp,
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
