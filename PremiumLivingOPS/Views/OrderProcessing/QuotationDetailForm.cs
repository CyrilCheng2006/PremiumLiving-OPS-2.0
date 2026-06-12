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

        // ── UI Construction ───────────────────────────────────────────────────────

        private void BuildUI()
        {
            this.Text            = string.Format("Quotation Detail  —  {0}", _q.QuotationID);
            this.Size            = new Size(900, 700);
            this.MinimumSize     = new Size(800, 600);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.BackColor       = Palette.BgPage;
            this.Font            = new Font("Segoe UI", 12f);
            this.FormBorderStyle = FormBorderStyle.Sizable;

            var tblOuter = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                RowCount    = 3,
                ColumnCount = 1,
                Padding     = new Padding(18, 14, 18, 12),
                BackColor   = Color.Transparent
            };
            tblOuter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblOuter.RowStyles.Add(new RowStyle(SizeType.Absolute, 220f));
            tblOuter.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblOuter.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f));

            // ── Header card
            var headerCards = CardPanel.Create(outerHeight: 220);
            headerCards.Item2.Controls.Add(BuildHeaderPanel());

            // ── Lines card
            var linesCards = CardPanel.CreateFill();
            linesCards.Item2.Controls.Add(BuildLinesGrid());

            // ── Footer close button
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
            {
                btnClose.Location = new Point(pnlFooter.Width - 122, Math.Max(0, (pnlFooter.Height - 40) / 2));
            };

            tblOuter.Controls.Add(headerCards.Item1, 0, 0);
            tblOuter.Controls.Add(linesCards.Item1,  0, 1);
            tblOuter.Controls.Add(pnlFooter,         0, 2);
            this.Controls.Add(tblOuter);
        }

        private TableLayoutPanel BuildHeaderPanel()
        {
            var tbl = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                RowCount    = 3,
                ColumnCount = 4,
                BackColor   = Color.Transparent,
                Padding     = new Padding(12, 8, 12, 8)
            };
            for (int i = 0; i < 4; i++)
                tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            var lblTitle = new Label
            {
                Text      = string.Format("Quotation  {0}    ●  {1}", _q.QuotationID, _q.QuotationStatus),
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Palette.Primary,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            tbl.Controls.Add(lblTitle, 0, 0);
            tbl.SetColumnSpan(lblTitle, 4);

            // Row 1
            tbl.Controls.Add(MakeReadField("Customer",    _q.CustomerName),                       0, 1);
            tbl.Controls.Add(MakeReadField("Sales Staff", _q.SalesStaffName ?? ""),               1, 1);
            tbl.Controls.Add(MakeReadField("Issued Date", _q.IssuedDate.ToString("yyyy-MM-dd")),  2, 1);
            tbl.Controls.Add(MakeReadField("Expiry Date", _q.ExpiryDate.ToString("yyyy-MM-dd")),  3, 1);

            // Row 2
            tbl.Controls.Add(MakeReadField("Total Amount",  string.Format("HK$ {0:N2}", _q.TotalAmount)),    0, 2);
            tbl.Controls.Add(MakeReadField("Deposit Req.",  string.Format("HK$ {0:N2}", _q.DepositRequired)),1, 2);
            tbl.Controls.Add(MakeReadField("Lead Time",     _q.LeadTimeEstimated ?? ""),                     2, 2);
            tbl.Controls.Add(MakeReadField("Notes",         _q.Notes ?? ""),                                 3, 2);

            return tbl;
        }

        private DataGridView BuildLinesGrid()
        {
            var dgv = new DataGridView
            {
                ReadOnly                  = true,
                AllowUserToAddRows        = false,
                AllowUserToDeleteRows     = false,
                RowHeadersVisible         = false,
                SelectionMode             = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor           = Color.White,
                BorderStyle               = BorderStyle.None,
                Dock                      = DockStyle.Fill,
                Font                      = new Font("Segoe UI", 11f),
                AutoSizeColumnsMode       = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight       = 38,
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255),
                    ForeColor = Color.FromArgb(98,  112, 135),
                    Font      = new Font("Segoe UI", 10f, FontStyle.Bold)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    SelectionBackColor = Color.FromArgb(219, 234, 254),
                    SelectionForeColor = Palette.TextMain
                },
                RowTemplate = { Height = 38 }
            };
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ITEM ID",    FillWeight = 10 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "PRODUCT",    FillWeight = 25 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "QTY",        FillWeight = 6  });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "UNIT",       FillWeight = 7  });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "UNIT PRICE", FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "DISC%",      FillWeight = 7  });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "SUBTOTAL",   FillWeight = 13 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "NOTE",       FillWeight = 20 });

            if (_q.Items != null)
            {
                foreach (var li in _q.Items)
                {
                    dgv.Rows.Add(
                        li.ItemID,
                        li.ProductName,
                        li.Quantity,
                        li.Unit,
                        li.UnitPrice.ToString("N2"),
                        li.DiscountPercent.ToString("N1"),
                        li.Subtotal.ToString("N2"),
                        li.ItemNote);
                }
            }
            return dgv;
        }

        // ── Helpers

        private static Panel MakeReadField(string caption, string value)
        {
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 0, 8, 0) };
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
