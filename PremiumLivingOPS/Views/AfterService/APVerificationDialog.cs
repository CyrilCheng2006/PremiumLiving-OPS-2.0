using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.AfterService
{
    /// <summary>
    /// AP 3-Way Match Verification Dialog.
    /// Rendering pattern mirrors AccountReceivableForm.ShowRecordPaymentDialog:
    ///   ─ Header (teal / navy bar)
    ///   ─ CardPanel: Invoice / PO / Supplier Info  (outerHeight 210)
    ///   ─ CardPanel: 3-Way Match Summary           (outerHeight 130)
    ///   ─ CardPanel: Line Items grid               (Fill)
    ///   ─ Footer (Close button, right-docked)
    /// </summary>
    public sealed class APVerificationDialog : Form
    {
        private readonly APVerificationDetailVM _vm;

        public APVerificationDialog(APVerificationDetailVM vm)
        {
            _vm = vm ?? throw new ArgumentNullException(nameof(vm));
            BuildUI();
        }

        // ───────────────────────────────────────────────────────────────────────────
        private void BuildUI()
        {
            Text            = $"AP Verification  —  {_vm.PurInvoiceID}";
            Size            = new Size(1800, 1400);
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = Color.FromArgb(240, 244, 249);
            Font            = new Font("Segoe UI", 13f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;

            // ══ HEADER — navy title bar (same structure as RecordPayment teal bar)
            var pnlHeader = BuildHeader();

            // ══ CARD 1 — Invoice / PO / Supplier Info
            var (infoOuter, infoInner) = CardPanel.Create(outerHeight: 210);
            infoInner.Controls.Add(BuildInfoTable());

            // ══ CARD 2 — 3-Way Match Amount Summary
            var (matchOuter, matchInner) = CardPanel.Create(outerHeight: 130);
            matchInner.Controls.Add(BuildMatchBanner());

            // ══ CARD 3 — Line Items DataGridView (Fill)
            var (lineOuter, lineInner) = CardPanel.CreateFill();
            lineInner.Controls.Add(BuildLineItemsTitle());
            lineInner.Controls.Add(BuildLineGrid());

            // ══ FOOTER
            var pnlFooter = BuildFooter();

            // ══ Assemble (same stacking order as RecordPayment)
            Controls.Add(lineOuter);   // Fill
            Controls.Add(matchOuter);  // Top
            Controls.Add(infoOuter);   // Top
            Controls.Add(pnlHeader);   // Top — topmost
            Controls.Add(pnlFooter);   // Bottom
        }

        // ───────────────────────────────────────────────────────────────────────────
        // HEADER
        // ───────────────────────────────────────────────────────────────────────────
        private Panel BuildHeader()
        {
            // Navy bar (AP uses dark navy; AR uses teal)
            Color headerBg = Color.FromArgb(19, 35, 61);

            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = headerBg };

            var tblHeader = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 2,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(28, 0, 0, 0)
            };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320f)); // badge column
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            tblHeader.Controls.Add(new Label
            {
                Text      = $"AP 3-Way Match Verification  —  {_vm.PurInvoiceID}",
                Font      = new Font("Segoe UI", 17f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false
            }, 0, 0);

            // Match status badge (green = matched, red = mismatch)
            Color badgeBg = _vm.IsMatched
                ? Color.FromArgb(22, 101, 52)
                : Color.FromArgb(185, 28, 28);
            string badgeText = _vm.IsMatched
                ? "✔  3-WAY MATCHED"
                : "⚠  MISMATCH";

            tblHeader.Controls.Add(new Label
            {
                Text      = badgeText,
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = badgeBg,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize  = false,
                Margin    = new Padding(0, 8, 0, 8)
            }, 1, 0);

            pnlHeader.Controls.Add(tblHeader);
            return pnlHeader;
        }

        // ───────────────────────────────────────────────────────────────────────────
        // CARD 1: Info rows  (mirrors AddInfoRow pattern in RecordPayment)
        // ───────────────────────────────────────────────────────────────────────────
        private TableLayoutPanel BuildInfoTable()
        {
            var tbl = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 4,
                RowCount        = 3,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(24, 16, 24, 16)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36f));
            for (int r = 0; r < 3; r++)
                tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f));

            AddInfoRow(tbl, 0, "Purchase Invoice:", _vm.PurInvoiceID,  "Purchase Order:",  _vm.PurchaseID);
            AddInfoRow(tbl, 1, "Supplier:",         _vm.SupplierName,  "Invoice Date:",    _vm.InvoiceDate.ToString("yyyy-MM-dd"));
            AddInfoRow(tbl, 2, "Expected Date:",    _vm.ExpectedDate.ToString("yyyy-MM-dd"), "Payment Status:", _vm.PaymentStatus);

            return tbl;
        }

        // ───────────────────────────────────────────────────────────────────────────
        // CARD 2: 3-Way Match banner
        //   Three columns: PO Total | Supplier Receipt Total | Invoice Total
        //   Accent colour follows match result
        // ───────────────────────────────────────────────────────────────────────────
        private Panel BuildMatchBanner()
        {
            Color accentFg  = _vm.IsMatched ? Color.FromArgb(22, 101, 52)  : Color.FromArgb(185, 28, 28);
            Color accentBg  = _vm.IsMatched ? Color.FromArgb(220, 252, 231): Color.FromArgb(254, 226, 226);

            var tbl = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 5,   // label | amt | sep | amt | sep | amt
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(24, 10, 24, 10)
            };
            // 3 amount blocks + 2 arrow separators
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f)); // PO
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50f));// arrow
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f)); // Receipt
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50f));// arrow
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f)); // Invoice
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            tbl.Controls.Add(MakeAmountBlock("Purchase Order Total",       $"HK$ {_vm.POTotalAmount:N2}",           accentFg, accentBg), 0, 0);
            tbl.Controls.Add(MakeArrow(),                                                                                                  1, 0);
            tbl.Controls.Add(MakeAmountBlock("Supplier Receipt Total",     $"HK$ {_vm.SupplierReceiptTotal:N2}",    accentFg, accentBg), 2, 0);
            tbl.Controls.Add(MakeArrow(),                                                                                                  3, 0);
            tbl.Controls.Add(MakeAmountBlock("Purchase Invoice Total",     $"HK$ {_vm.InvoiceTotalAmount:N2}",      accentFg, accentBg), 4, 0);

            return tbl;
        }

        private static Panel MakeAmountBlock(string label, string value, Color fg, Color bg)
        {
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = bg, Margin = new Padding(4, 0, 4, 0) };
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path  = RoundedRect(((Panel)s).ClientRectangle, 8);
                using var brush = new SolidBrush(((Panel)s).BackColor);
                e.Graphics.FillPath(brush, path);
            };

            var inner = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                RowCount    = 2,
                ColumnCount = 1,
                BackColor   = Color.Transparent,
                Padding     = new Padding(12, 6, 12, 6)
            };
            inner.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            inner.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            inner.Controls.Add(new Label
            {
                Text      = label,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = fg,
                BackColor = Color.Transparent,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomCenter,
                AutoSize  = false
            }, 0, 0);
            inner.Controls.Add(new Label
            {
                Text      = value,
                Font      = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = fg,
                BackColor = Color.Transparent,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.TopCenter,
                AutoSize  = false
            }, 0, 1);

            pnl.Controls.Add(inner);
            return pnl;
        }

        private static Label MakeArrow() => new Label
        {
            Text      = "=",
            Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = Color.FromArgb(156, 163, 175),
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize  = false
        };

        // ───────────────────────────────────────────────────────────────────────────
        // CARD 3: Line Items title panel (mirrors Payment History title pattern)
        // ───────────────────────────────────────────────────────────────────────────
        private static Panel BuildLineItemsTitle()
        {
            var pnl = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 48,
                BackColor = Color.FromArgb(246, 249, 255),
                Padding   = new Padding(24, 0, 0, 0)
            };
            pnl.Paint += PaintBottomBorder;
            pnl.Controls.Add(new Label
            {
                Text      = "📋  Purchase Order Line Items",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false
            });
            return pnl;
        }

        // ───────────────────────────────────────────────────────────────────────────
        // CARD 3: Line Items DataGridView (exact same style as dgvTxn in RecordPayment)
        // ───────────────────────────────────────────────────────────────────────────
        private DataGridView BuildLineGrid()
        {
            var dgv = new DataGridView
            {
                ReadOnly              = true,
                AllowUserToAddRows    = false,
                RowHeadersVisible     = false,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect           = false,
                BackgroundColor       = Color.White,
                BorderStyle           = BorderStyle.None,
                GridColor             = Color.FromArgb(221, 227, 236),
                Font                  = new Font("Segoe UI", 12f),
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate           = { Height = 44 },
                Dock                  = DockStyle.Fill,
                ColumnHeadersHeight   = 42,
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255),
                    ForeColor = Color.FromArgb(98, 112, 135),
                    Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                    Padding   = new Padding(12, 0, 0, 0)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor          = Color.White,
                    ForeColor          = Color.FromArgb(15, 31, 53),
                    SelectionBackColor = Color.FromArgb(219, 234, 254),
                    SelectionForeColor = Color.FromArgb(15, 31, 53),
                    Padding            = new Padding(12, 6, 12, 6)
                }
            };

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cItem",     HeaderText = "ITEM ID",           FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cName",     HeaderText = "ITEM NAME",         FillWeight = 28 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cQtyOrd",   HeaderText = "QTY ORDERED",       FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cQtyRcv",   HeaderText = "QTY RECEIVED",      FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cUnit",     HeaderText = "UNIT PRICE (HK$)",  FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cLineTotal",HeaderText = "LINE TOTAL (HK$)",  FillWeight = 16 });

            // Right-align numeric columns
            dgv.Columns["cQtyOrd"].DefaultCellStyle.Alignment    = DataGridViewContentAlignment.MiddleRight;
            dgv.Columns["cQtyRcv"].DefaultCellStyle.Alignment    = DataGridViewContentAlignment.MiddleRight;
            dgv.Columns["cUnit"].DefaultCellStyle.Alignment      = DataGridViewContentAlignment.MiddleRight;
            dgv.Columns["cLineTotal"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            Color warnBg = Color.FromArgb(254, 249, 195); // amber — under-received rows

            if (_vm.Lines == null || _vm.Lines.Count == 0)
            {
                dgv.Rows.Add("—", "No line items found", "—", "—", "—", "—");
                dgv.Rows[0].DefaultCellStyle.ForeColor = Color.FromArgb(180, 180, 180);
                dgv.Rows[0].DefaultCellStyle.Font      = new Font("Segoe UI", 11f, FontStyle.Italic);
            }
            else
            {
                foreach (var ln in _vm.Lines)
                {
                    int idx = dgv.Rows.Add(
                        ln.ItemID,
                        ln.ItemName,
                        ln.OrderQty,
                        ln.QtyReceived,
                        $"{ln.UnitPrice:N2}",
                        $"{ln.LineTotal:N2}"
                    );
                    // Highlight rows where received < ordered
                    if (ln.QtyReceived < ln.OrderQty)
                    {
                        dgv.Rows[idx].DefaultCellStyle.BackColor          = warnBg;
                        dgv.Rows[idx].DefaultCellStyle.SelectionBackColor = Color.FromArgb(253, 230, 138);
                    }
                }
            }

            return dgv;
        }

        // ───────────────────────────────────────────────────────────────────────────
        // FOOTER  (mirrors RecordPayment footer exactly)
        // ───────────────────────────────────────────────────────────────────────────
        private Panel BuildFooter()
        {
            var pnlFooter = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 88,
                BackColor = Color.White,
                Padding   = new Padding(0, 14, 28, 14)
            };
            pnlFooter.Paint += PaintTopBorder;

            // Result text (left side)
            string resultText = _vm.IsMatched
                ? "✔  All three amounts match. This invoice qualifies as an Account Payable."
                : "⚠  Amounts do not match. Please review before recording as Account Payable.";
            Color resultFg = _vm.IsMatched
                ? Color.FromArgb(22, 101, 52)
                : Color.FromArgb(185, 28, 28);

            var lblResult = new Label
            {
                Text      = resultText,
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = resultFg,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(28, 0, 0, 0),
                AutoSize  = false
            };

            var btnClose = new Button
            {
                Text      = "Close",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(210, 60),
                Dock      = DockStyle.Right,
                Cursor    = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnClose.FlatAppearance.BorderSize         = 1;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnClose.Click += (o, ev) => Close();

            pnlFooter.Controls.Add(btnClose);
            pnlFooter.Controls.Add(lblResult); // Fill — added after Right so it fills remainder
            return pnlFooter;
        }

        // ───────────────────────────────────────────────────────────────────────────
        // Shared helpers  (identical signatures to those in AccountReceivableForm)
        // ───────────────────────────────────────────────────────────────────────────
        private static void AddInfoRow(TableLayoutPanel tbl, int row,
            string lbl1, string val1, string lbl2, string val2)
        {
            tbl.Controls.Add(MakeLabelKey(lbl1), 0, row);
            tbl.Controls.Add(MakeLabelVal(val1), 1, row);
            tbl.Controls.Add(MakeLabelKey(lbl2), 2, row);
            tbl.Controls.Add(MakeLabelVal(val2), 3, row);
        }

        private static Label MakeLabelKey(string text) => new Label
        {
            Text      = text,
            Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(98, 112, 135),
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize  = false
        };

        private static Label MakeLabelVal(string text) => new Label
        {
            Text      = text,
            Font      = new Font("Segoe UI", 12f),
            ForeColor = Color.FromArgb(15, 31, 53),
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize  = false
        };

        private static void PaintBottomBorder(object s, PaintEventArgs e)
        {
            var p = (Control)s;
            using var pen = new Pen(Color.FromArgb(221, 227, 236));
            e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
        }

        private static void PaintTopBorder(object s, PaintEventArgs e)
        {
            using var pen = new Pen(Color.FromArgb(221, 227, 236));
            e.Graphics.DrawLine(pen, 0, 0, ((Control)s).Width, 0);
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X,         r.Y,          d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y,          d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d,   0, 90);
            path.AddArc(r.X,         r.Bottom - d, d, d,  90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
