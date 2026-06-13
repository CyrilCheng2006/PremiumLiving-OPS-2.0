using PremiumLivingOPS.Models.Entities;
using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.OrderProcessing
{
    /// <summary>
    /// Read-only detail dialog for a single Quotation.
    /// MVC View — receives a populated QuotationEntity and renders read-only.
    ///
    /// Header layout (pnlHeader, Height=80, dark navy):
    ///   Both lblTitle and lblBadge are positioned via a shared repositionHeader()
    ///   Action subscribed to pnlHeader.Resize and Form.Shown.
    ///   NO Dock is used on either label — Dock=Fill on lblTitle caused lblBadge
    ///   to be visually clipped even when Width was correct.
    ///
    ///   lblTitle : Left=24, fills from left up to badge left edge minus 8px gap
    ///   lblBadge : Width=270 (hard), right edge 16px from panel right, vertically centred
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

            var lblTitle = new Label
            {
                Text      = string.Format("Quotation Detail  \u2014  {0}", _q.QuotationID),
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false
            };

            var (scBg, scFg) = GetStatusColor(_q.QuotationStatus);
            var lblBadge = new Label
            {
                Text         = _q.QuotationStatus ?? "Unknown",
                Font         = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor    = scFg,
                BackColor    = scBg,
                TextAlign    = ContentAlignment.MiddleCenter,
                AutoSize     = false,
                AutoEllipsis = false,
                Width        = 270,
                Height       = 44,
                Padding      = new Padding(12, 0, 12, 0)
            };

            // Shared repositioning — called on every Resize and on Shown
            Action repositionHeader = () =>
            {
                int badgeRight  = 16;                          // gap from panel right edge
                int badgeLeft   = pnlHeader.Width - lblBadge.Width - badgeRight;
                int badgeTop    = (pnlHeader.Height - lblBadge.Height) / 2;

                lblBadge.Left = badgeLeft;
                lblBadge.Top  = badgeTop;

                int titleLeft  = 24;
                int titleRight = badgeLeft - 8;                // 8px gap between title and badge
                lblTitle.Left   = titleLeft;
                lblTitle.Top    = 0;
                lblTitle.Width  = Math.Max(0, titleRight - titleLeft);
                lblTitle.Height = pnlHeader.Height;
            };

            pnlHeader.Resize += (s, ev) => repositionHeader();
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblBadge);

            // ── Body
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

            this.Shown += (s, ev) => repositionHeader();
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

            if (_q.Items != null)
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
