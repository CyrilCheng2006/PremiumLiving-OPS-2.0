using PremiumLivingOPS.Models.Entities;
using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.OrderProcessing
{
    /// <summary>
    /// Read-only detail dialog for a single Quotation.
    /// Receives a populated QuotationEntity (header + items) and renders it.
    ///
    /// Fields shown map directly to schema.sql Quotation table:
    ///   QuotationID, CustomerID (→CustomerName), ExpiryDate, TotalAmount,
    ///   DepositRequired, LeadTimeEstimated, TermsandCondition, QuotationStatus.
    ///
    /// Columns: ITEM ID | PRODUCT | QTY | UNIT PRICE | SUBTOTAL
    ///
    /// Header: pnlHeader (dark navy, Height 80) + TableLayoutPanel 2-col
    ///   Col 0 — SizeType.Percent 100f  : title label, Font 13f Bold, white, Dock=Fill
    ///   Col 1 — SizeType.Absolute 220f : status badge, Font 14f Bold, MiddleCenter
    ///     220f = ~160px text ("Converted" @ 14f Bold) + 16px padding each side + margin
    ///     Badge Label: Dock=Fill, AutoEllipsis=false — matches ModifyQuotationDialog.
    /// </summary>
    public class QuotationDetailForm : Form
    {
        private readonly QuotationEntity _q;

        public QuotationDetailForm(QuotationEntity quotation)
        {
            if (quotation == null) throw new ArgumentNullException("quotation");
            _q = quotation;
            BuildUI();
        }

        private void BuildUI()
        {
            this.Text            = string.Format("Quotation Detail  \u2014  {0}", _q.QuotationID);
            this.Size            = new Size(900, 700);
            this.MinimumSize     = new Size(800, 600);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.BackColor       = Palette.BgPage;
            this.Font            = new Font("Segoe UI", 12f);
            this.FormBorderStyle = FormBorderStyle.Sizable;

            // ── Header
            var pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 80,
                BackColor = Color.FromArgb(19, 35, 61)
            };
            var tblHeader = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 2,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(24, 0, 24, 0)
            };
            // Col 0: title text — takes all remaining width
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            // Col 1: status badge — fixed 220px; wide enough for "Converted" @ Font 14f Bold
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            tblHeader.Controls.Add(new Label
            {
                Text      = string.Format("Quotation Detail  \u2014  {0}", _q.QuotationID),
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false
            }, 0, 0);

            var (scBg, scFg) = GetStatusColor(_q.QuotationStatus);
            tblHeader.Controls.Add(new Label
            {
                Text         = _q.QuotationStatus ?? "Unknown",
                Font         = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor    = scFg,
                BackColor    = scBg,
                Dock         = DockStyle.Fill,
                TextAlign    = ContentAlignment.MiddleCenter,
                AutoSize     = false,
                AutoEllipsis = false,
                Padding      = new Padding(16, 4, 16, 4)
            }, 1, 0);

            pnlHeader.Controls.Add(tblHeader);

            // ── Body (CardPanel rows)
            var tblOuter = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                RowCount    = 3,
                ColumnCount = 1,
                Padding     = new Padding(18, 14, 18, 12),
                BackColor   = Color.Transparent
            };
            tblOuter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblOuter.RowStyles.Add(new RowStyle(SizeType.Absolute, 200f));
            tblOuter.RowStyles.Add(new RowStyle(SizeType.Percent,  100f));
            tblOuter.RowStyles.Add(new RowStyle(SizeType.Absolute,  56f));

            var headerCards = CardPanel.Create(outerHeight: 200);
            headerCards.Item2.Controls.Add(BuildHeaderPanel());

            var linesCards = CardPanel.CreateFill();
            linesCards.Item2.Controls.Add(BuildLinesGrid());

            var btnClose = new Button
            {
                Text      = "Close",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Palette.TextMain,
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Width     = 110,
                Height    = 40,
                Cursor    = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor        = Palette.BorderColor;
            btnClose.FlatAppearance.BorderSize         = 1;
            btnClose.FlatAppearance.MouseOverBackColor = Palette.BgPage;
            btnClose.Click += (s, ev) => Close();

            var pnlFooter = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlFooter.Controls.Add(btnClose);
            pnlFooter.Resize += (s, ev) =>
                btnClose.Location = new Point(pnlFooter.Width - 122, Math.Max(0, (pnlFooter.Height - 40) / 2));

            tblOuter.Controls.Add(headerCards.Item1, 0, 0);
            tblOuter.Controls.Add(linesCards.Item1,  0, 1);
            tblOuter.Controls.Add(pnlFooter,         0, 2);

            this.Controls.Add(tblOuter);
            this.Controls.Add(pnlHeader);
        }

        private TableLayoutPanel BuildHeaderPanel()
        {
            var tbl = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                RowCount    = 2,
                ColumnCount = 4,
                BackColor   = Color.Transparent,
                Padding     = new Padding(12, 8, 12, 8)
            };
            for (int i = 0; i < 4; i++)
                tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            tbl.Controls.Add(MakeReadField("Customer",    _q.CustomerName),                      0, 0);
            tbl.Controls.Add(MakeReadField("Sales Staff", _q.SalesStaffName ?? ""),              1, 0);
            tbl.Controls.Add(MakeReadField("Issued Date", _q.IssuedDate.ToString("yyyy-MM-dd")), 2, 0);
            tbl.Controls.Add(MakeReadField("Expiry Date", _q.ExpiryDate.ToString("yyyy-MM-dd")), 3, 0);

            tbl.Controls.Add(MakeReadField("Total Amount", string.Format("HK$ {0:N2}", _q.TotalAmount)),     0, 1);
            tbl.Controls.Add(MakeReadField("Deposit Req.", string.Format("HK$ {0:N2}", _q.DepositRequired)), 1, 1);
            tbl.Controls.Add(MakeReadField("Lead Time",    _q.LeadTimeEstimated ?? ""),                      2, 1);
            tbl.Controls.Add(MakeReadField("Status",       _q.QuotationStatus   ?? ""),                      3, 1);

            return tbl;
        }

        private DataGridView BuildLinesGrid()
        {
            var dgv = new DataGridView
            {
                ReadOnly              = true,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible     = false,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor       = Color.White,
                BorderStyle           = BorderStyle.None,
                Dock                  = DockStyle.Fill,
                Font                  = new Font("Segoe UI", 11f),
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight   = 38,
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255),
                    ForeColor = Color.FromArgb(98, 112, 135),
                    Font      = new Font("Segoe UI", 10f, FontStyle.Bold)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    SelectionBackColor = Color.FromArgb(219, 234, 254),
                    SelectionForeColor = Palette.TextMain
                },
                RowTemplate = { Height = 38 }
            };
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ITEM ID",    FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "PRODUCT",    FillWeight = 40 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "QTY",        FillWeight = 10 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "UNIT PRICE", FillWeight = 18 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "SUBTOTAL",   FillWeight = 17 });

            if (_q.Items != null)"
                foreach (var li in _q.Items)
                    dgv.Rows.Add(
                        li.ItemID, li.ProductName, li.Quantity,
                        li.UnitPrice.ToString("N2"), li.Subtotal.ToString("N2"));
            return dgv;
        }

        private static (Color bg, Color fg) GetStatusColor(string status)
            => status switch
            {
                "Pending"   => (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)),
                "Converted" => (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)),
                "Rejected"  => (Color.FromArgb(254, 226, 226), Color.FromArgb(153,  27,  27)),
                _           => (Color.FromArgb(230, 230, 230), Color.FromArgb( 80,  80,  80))
            };

        private static Panel MakeReadField(string caption, string value)
        {
            var pnl = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(0, 0, 8, 0)
            };
            pnl.Controls.Add(new Label
            {
                Text      = caption,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Top,
                Height    = 22,
                TextAlign = ContentAlignment.BottomLeft
            });
            pnl.Controls.Add(new Label
            {
                Text      = value,
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Palette.TextMain,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.TopLeft
            });
            return pnl;
        }
    }
}
