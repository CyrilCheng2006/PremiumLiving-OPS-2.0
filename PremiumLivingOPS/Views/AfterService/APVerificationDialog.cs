using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.AfterService
{
    /// <summary>
    /// AP 3-Way Match Verification Dialog
    /// ─────────────────────────────────────────────────────────────────────
    /// Opened via [AP Verification] button on AccountPayableForm (210×60).
    /// Renders as a modal dialog (FormBorderStyle.FixedDialog, 1400×900).
    ///
    /// Visual baseline: LogisticsProcessing → View Shipment → ShowDetailDialog
    /// CardPanel 3-layer nesting: grey bg → white card → content.
    ///
    /// Layout (top → bottom, DockStyle):
    ///   pnlHeader       Top  80   — dark navy, Invoice ID + match badge
    ///   pnlInfo         Top  260  — 4-col TLP: PO / Supplier / Invoice fields
    ///   pnlMatchBar     Top  60   — 3-way match result banner
    ///   pnlLineLabel    Top  40   — "PURCHASE ORDER LINES" bar
    ///   dgv             Fill      — line items grid (POLine, Item, OrderQty, UnitPrice, LineTotal, QtyReceived)
    ///   pnlTotalRow     Bottom 50 — three totals side-by-side
    ///   pnlFooter       Bottom 80 — [Close]
    /// </summary>
    public class APVerificationDialog : Form
    {
        private readonly APVerificationDetailVM _vm;

        // ── Status colours (reused from AccountPayableForm) ────────────────
        private static readonly Color NavyBg    = Color.FromArgb(19, 35, 61);
        private static readonly Color GreenBg   = Color.FromArgb(6, 95, 70);
        private static readonly Color GreenLight = Color.FromArgb(209, 250, 229);
        private static readonly Color GreenFg   = Color.FromArgb(22, 101, 52);
        private static readonly Color RedLight   = Color.FromArgb(254, 226, 226);
        private static readonly Color RedFg      = Color.FromArgb(185, 28, 28);
        private static readonly Color HeaderBg   = Color.FromArgb(246, 249, 255);
        private static readonly Color BorderCol  = Color.FromArgb(221, 227, 236);
        private static readonly Color TextPrimary = Color.FromArgb(15, 31, 53);
        private static readonly Color TextMuted   = Color.FromArgb(98, 112, 135);

        public APVerificationDialog(APVerificationDetailVM vm)
        {
            _vm = vm ?? throw new ArgumentNullException(nameof(vm));
            BuildUI();
        }

        private void BuildUI()
        {
            Text            = $"AP Verification  —  {_vm.PurInvoiceID}";
            Size            = new Size(1400, 900);
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = Color.White;
            Font            = new Font("Segoe UI", 13f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;

            // ── Header ────────────────────────────────────────────────────────────
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = NavyBg };
            var tblHdr = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(24, 0, 24, 0)
            };
            tblHdr.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblHdr.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280f));
            tblHdr.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblHdr.Controls.Add(new Label
            {
                Text = $"AP Verification  —  {_vm.PurInvoiceID}",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);

            bool matched = _vm.IsMatched;
            tblHdr.Controls.Add(new Label
            {
                Text      = matched ? "✔  3-WAY MATCHED" : "⚠  MISMATCH",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = matched ? GreenFg : RedFg,
                BackColor = matched ? GreenLight : RedLight,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false, Padding = new Padding(8, 4, 8, 4)
            }, 1, 0);
            pnlHeader.Controls.Add(tblHdr);

            // ── Info panel (4-col TLP, mirrors ShowDetailDialog pnlInfo) ───────
            var pnlInfo = new Panel
            {
                Dock = DockStyle.Top, Height = 260,
                Padding = new Padding(28, 18, 28, 8), BackColor = Color.White
            };
            pnlInfo.Paint += PaintBottomBorder;

            var tblInfo = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 5,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            for (int r = 0; r < 5; r++)
                tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));

            // Left column: PO info
            AddRow(tblInfo, 0, "Purchase ID:",    _vm.PurchaseID,
                            "Invoice ID:",     _vm.PurInvoiceID);
            AddRow(tblInfo, 1, "Supplier:",       _vm.SupplierName,
                            "PO Status:",      _vm.PurchaseStatus);
            AddRow(tblInfo, 2, "Supplier Phone:", _vm.SupplierPhone,
                            "Order Date:",     _vm.OrderDate.ToString("yyyy-MM-dd"));
            AddRow(tblInfo, 3, "Supplier Addr.:", _vm.SupplierAddress,
                            "Expected Date:",  _vm.ExpectedDate.ToString("yyyy-MM-dd"));
            AddRow(tblInfo, 4, "Inv. Pay Status:", _vm.InvPayStatus,
                            "", "");
            pnlInfo.Controls.Add(tblInfo);

            // ── 3-Way Match Banner ─────────────────────────────────────────
            var pnlMatchBar = new Panel
            {
                Dock      = DockStyle.Top, Height = 60,
                BackColor = matched ? Color.FromArgb(240, 253, 244) : Color.FromArgb(255, 241, 242),
                Padding   = new Padding(28, 0, 28, 0)
            };
            pnlMatchBar.Paint += PaintBottomBorder;

            var tblMatch = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            for (int c = 0; c < 3; c++)
                tblMatch.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            tblMatch.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            Color matchFg = matched ? GreenFg : RedFg;
            tblMatch.Controls.Add(MakeMatchCell(
                "PO Total", $"HK$ {_vm.POTotalAmount:N2}", matchFg), 0, 0);
            tblMatch.Controls.Add(MakeMatchCell(
                "Supplier Receipt Total", $"HK$ {_vm.SupplierReceiptTotal:N2}", matchFg), 1, 0);
            tblMatch.Controls.Add(MakeMatchCell(
                "Invoice Total", $"HK$ {_vm.InvTotalAmount:N2}", matchFg), 2, 0);
            pnlMatchBar.Controls.Add(tblMatch);

            // ── "PURCHASE ORDER LINES" label bar ──────────────────────────
            var pnlLineLabel = new Panel
            {
                Dock = DockStyle.Top, Height = 40,
                BackColor = HeaderBg, Padding = new Padding(28, 0, 0, 0)
            };
            pnlLineLabel.Controls.Add(new Label
            {
                Text = "PURCHASE ORDER LINES",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = TextMuted, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            });
            pnlLineLabel.Paint += PaintBottomBorder;

            // ── Line items DataGridView ─────────────────────────────────────────
            var dgv = new DataGridView
            {
                ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false,
                SelectionMode   = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                GridColor       = BorderCol,
                Font            = new Font("Segoe UI", 12f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle     = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate         = { Height = 44 }, Dock = DockStyle.Fill,
                ColumnHeadersHeight = 40, EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = HeaderBg, ForeColor = TextMuted,
                    Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                    Padding   = new Padding(12, 0, 0, 0)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor            = Color.White, ForeColor = TextPrimary,
                    SelectionBackColor   = Color.FromArgb(219, 234, 254),
                    SelectionForeColor   = TextPrimary,
                    Padding              = new Padding(12, 6, 12, 6)
                }
            };

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cLine",  HeaderText = "LINE ID",      FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cItem",  HeaderText = "ITEM ID",      FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cName",  HeaderText = "ITEM NAME",    FillWeight = 30 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cOQty",  HeaderText = "ORDER QTY",    FillWeight = 10 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cPrice", HeaderText = "UNIT PRICE",   FillWeight = 13 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cTotal", HeaderText = "LINE TOTAL",   FillWeight = 13 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cRcvd",  HeaderText = "QTY RECEIVED", FillWeight = 10 });

            foreach (var line in _vm.Lines)
            {
                int idx = dgv.Rows.Add(
                    line.POLineID,
                    line.RawMaterialItemID,
                    line.ItemName,
                    line.OrderQty,
                    $"HK$ {line.UnitPrice:N2}",
                    $"HK$ {line.LineTotal:N2}",
                    line.QtyReceived);

                // Highlight rows where received < ordered
                if (line.QtyReceived < line.OrderQty)
                {
                    dgv.Rows[idx].DefaultCellStyle.BackColor          = Color.FromArgb(255, 251, 235);
                    dgv.Rows[idx].DefaultCellStyle.SelectionBackColor = Color.FromArgb(254, 240, 199);
                }
            }

            // ── Total row (3 amounts side-by-side) ──────────────────────────
            var pnlTotalRow = new Panel
            {
                Dock      = DockStyle.Bottom, Height = 50,
                BackColor = HeaderBg, Padding = new Padding(28, 0, 28, 0)
            };
            pnlTotalRow.Paint += PaintTopBorder;

            var tblTotal = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            for (int c = 0; c < 3; c++)
                tblTotal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            tblTotal.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            tblTotal.Controls.Add(MakeTotalLabel(
                $"PO Total:  HK$ {_vm.POTotalAmount:N2}", TextPrimary), 0, 0);
            tblTotal.Controls.Add(MakeTotalLabel(
                $"Receipt Total:  HK$ {_vm.SupplierReceiptTotal:N2}", TextPrimary), 1, 0);
            tblTotal.Controls.Add(MakeTotalLabel(
                $"Invoice Total:  HK$ {_vm.InvTotalAmount:N2}",
                matched ? GreenFg : RedFg), 2, 0);
            pnlTotalRow.Controls.Add(tblTotal);

            // ── Footer: [Close] ──────────────────────────────────────────────────
            var pnlFooter = new Panel
            {
                Dock      = DockStyle.Bottom, Height = 80,
                BackColor = Color.White, Padding = new Padding(0, 12, 28, 12)
            };
            pnlFooter.Paint += PaintTopBorder;

            var btnClose = new Button
            {
                Text      = "Close",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = TextPrimary, BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock      = DockStyle.Right, Width = 140, Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor        = BorderCol;
            btnClose.FlatAppearance.BorderSize         = 1;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnClose.Click += (s, e) => Close();
            pnlFooter.Controls.Add(btnClose);

            // ── Matched info label in footer (when matched) ────────────────
            if (matched)
            {
                var lblMatchNote = new Label
                {
                    Text      = "✔  All three amounts match. This purchase invoice is eligible to be recorded as Account Payable.",
                    Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                    ForeColor = GreenFg, BackColor = Color.Transparent,
                    Dock      = DockStyle.Left, AutoSize = false,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Width     = 860
                };
                pnlFooter.Controls.Add(lblMatchNote);
            }
            else
            {
                var lblMismatch = new Label
                {
                    Text      = "⚠  Amounts do not match. Please verify PurchaseOrder, Supplier Receipt, and PurchaseInvoice before recording as Account Payable.",
                    Font      = new Font("Segoe UI", 11f),
                    ForeColor = RedFg, BackColor = Color.Transparent,
                    Dock      = DockStyle.Left, AutoSize = false,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Width     = 860
                };
                pnlFooter.Controls.Add(lblMismatch);
            }

            // ── Assemble (Bottom → Fill → Top order for DockStyle) ────────────
            Controls.Add(dgv);
            Controls.Add(pnlTotalRow);
            Controls.Add(pnlLineLabel);
            Controls.Add(pnlMatchBar);
            Controls.Add(pnlInfo);
            Controls.Add(pnlHeader);
            Controls.Add(pnlFooter);
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private static void AddRow(TableLayoutPanel t, int row,
            string key1, string val1, string key2, string val2)
        {
            t.Controls.Add(MakeKey(key1), 0, row);
            t.Controls.Add(MakeVal(val1), 1, row);
            if (!string.IsNullOrEmpty(key2))
            {
                t.Controls.Add(MakeKey(key2), 2, row);
                t.Controls.Add(MakeVal(val2), 3, row);
            }
        }

        private static Label MakeKey(string text) => new Label
        {
            Text = text, Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = Color.FromArgb(98, 112, 135), Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
        };

        private static Label MakeVal(string text) => new Label
        {
            Text = text ?? "", Font = new Font("Segoe UI", 12f),
            ForeColor = Color.FromArgb(15, 31, 53), Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
        };

        private static Panel MakeMatchCell(string label, string value, Color fg)
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(12, 4, 12, 4) };
            var inner = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            inner.RowStyles.Add(new RowStyle(SizeType.Percent, 40f));
            inner.RowStyles.Add(new RowStyle(SizeType.Percent, 60f));
            inner.Controls.Add(new Label
            {
                Text = label, Font = new Font("Segoe UI", 10f),
                ForeColor = fg, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft, AutoSize = false
            }, 0, 0);
            inner.Controls.Add(new Label
            {
                Text = value, Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = fg, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopLeft, AutoSize = false
            }, 0, 1);
            p.Controls.Add(inner);
            return p;
        }

        private static Label MakeTotalLabel(string text, Color fg) => new Label
        {
            Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = fg, Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight, AutoSize = false
        };

        private static void PaintBottomBorder(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            var p = (Panel)sender;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
        }

        private static void PaintTopBorder(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            var p = (Panel)sender;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, 0, p.Width, 0);
        }
    }
}
