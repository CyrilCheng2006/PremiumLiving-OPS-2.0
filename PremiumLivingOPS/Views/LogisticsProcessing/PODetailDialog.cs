using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
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

            // 1. Dark header bar
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
                Text = $"Purchase Order Details  —  {po?.PurchaseID}",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
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

            // 2. Footer strip
            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.White, Padding = new Padding(0, 10, 28, 10) };
            pnlFooter.Paint += (s, e) => { using var pen = new Pen(Color.FromArgb(221, 227, 236), 1); e.Graphics.DrawLine(pen, 0, 0, ((Control)s).Width, 0); };
            var btnClose = new Button
            {
                Text = "Close", Size = new Size(110, 38),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                BackColor = Color.FromArgb(47, 111, 237), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 12f, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Location = new Point(pnlFooter.Width - 110 - 28, (pnlFooter.Height - 38) / 2);
            btnClose.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            btnClose.Click += (s, e) => Close();
            pnlFooter.Controls.Add(btnClose);
            Controls.Add(pnlFooter);

            // 3. Main content
            var pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            Controls.Add(pnlContent);

            // 3a. Info panel
            var pnlInfo = new Panel { Dock = DockStyle.Top, Height = 210, BackColor = Color.White, Padding = new Padding(28, 18, 28, 10) };
            pnlInfo.Paint += (s, e) => { using var pen = new Pen(Color.FromArgb(221, 227, 236), 1); e.Graphics.DrawLine(pen, 0, ((Control)s).Height - 1, ((Control)s).Width, ((Control)s).Height - 1); };
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

            var leftFields = new[] {
                ("PO ID",        po?.PurchaseID    ?? ""),
                ("Order Date",   po?.OrderDate == default ? "" : po.OrderDate.ToString("yyyy-MM-dd")),
                ("Total Amount", $"HK$ {po?.POTotalAmount:F2}"),
                ("Status",       po?.PurchaseStatus ?? "")
            };
            for (int i = 0; i < leftFields.Length; i++)
            {
                tblInfo.Controls.Add(MakeLabelKey(leftFields[i].Item1), 0, i);
                tblInfo.Controls.Add(MakeLabelVal(leftFields[i].Item2), 1, i);
            }
            var rightFields = new[] {
                ("Supplier",      po?.SupplierName ?? ""),
                ("Expected Date", po?.OrderDate == default ? "" : po.OrderDate.AddDays(14).ToString("yyyy-MM-dd")),
                ("Lines",         (_vm.Lines?.Count ?? 0).ToString() + " item(s)"),
                ("",              "")
            };
            for (int i = 0; i < rightFields.Length; i++)
            {
                tblInfo.Controls.Add(MakeLabelKey(rightFields[i].Item1), 2, i);
                tblInfo.Controls.Add(MakeLabelVal(rightFields[i].Item2), 3, i);
            }
            pnlInfo.Controls.Add(tblInfo);
            pnlContent.Controls.Add(pnlInfo);

            // 3b. Section title
            var pnlGridTitle = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.White, Padding = new Padding(28, 6, 28, 0) };
            pnlGridTitle.Controls.Add(new Label
            {
                Text = "Line Items", Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59), Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            });
            pnlContent.Controls.Add(pnlGridTitle);

            // 3c. DataGrid
            var pnlGrid = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(28, 0, 28, 0) };
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

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLineID",   HeaderText = "Line ID",          FillWeight = 12f });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItem",     HeaderText = "Item",             FillWeight = 38f });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQty",      HeaderText = "Qty Ordered",      FillWeight = 15f });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUnit",     HeaderText = "Unit Price (HK$)", FillWeight = 17f });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSubtotal", HeaderText = "Subtotal (HK$)",  FillWeight = 18f });
            foreach (DataGridViewColumn col in dgv.Columns)
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            if (_vm.Lines != null)
                foreach (var line in _vm.Lines)
                    dgv.Rows.Add(
                        line.POLineID,
                        line.MaterialName ?? line.RawMaterialItemID,
                        line.OrderQty,
                        $"{line.UnitPrice:F2}",
                        $"{line.OrderQty * line.UnitPrice:F2}");

            if (dgv.Rows.Count == 0)
            {
                dgv.Rows.Add("", "(No line items found)", "", "", "");
                dgv.Rows[0].DefaultCellStyle.ForeColor = Color.FromArgb(148, 163, 184);
            }
            pnlGrid.Controls.Add(dgv);
            pnlContent.Controls.Add(pnlGrid);
        }

        private static Label MakeLabelKey(string text) => new Label
        {
            Text = text, Font = new Font("Segoe UI", 10f),
            ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft, AutoSize = false, Padding = new Padding(4, 0, 0, 0)
        };

        private static Label MakeLabelVal(string text) => new Label
        {
            Text = text ?? "", Font = new Font("Segoe UI", 12f),
            ForeColor = Color.FromArgb(30, 41, 59), Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft, AutoSize = false, Padding = new Padding(4, 0, 0, 0)
        };
    }
}
