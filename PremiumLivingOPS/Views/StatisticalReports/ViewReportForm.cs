using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.StatisticalReports
{
    /// <summary>
    /// View — Statistical Reports › View Report
    ///
    /// Chart/Table toggle: btnToggle flips _xxxChart flag, then calls
    /// SwapContent(dgvCard, chartCard, flag) which physically replaces the
    /// control in pnlContent so the view actually changes.
    ///
    /// Charts are rendered with pure GDI+ (no DataVisualization NuGet required).
    /// </summary>
    public partial class ViewReportForm : Form
    {
        private readonly StatisticalReportsController _ctrl = new StatisticalReportsController();
        private int      _activeTab         = -1;
        private bool     _salesChart        = false;
        private bool     _inventoryChart    = false;
        private bool     _procurementChart  = false;
        private bool     _logisticsChart    = false;
        private bool     _afterServiceChart = false;
        private bool     _financeChart      = false;
        private Button[] _tabButtons;

        // Default date range covers all sample_data (2024-01-01 to today)
        private static readonly DateTime DefaultDateFrom = new DateTime(2024, 1, 1);
        private static DateTime DefaultDateTo => DateTime.Today;

        private static readonly Dictionary<string, (Color bg, Color fg)> StatusColors =
            new Dictionary<string, (Color, Color)>
            {
                { "Pending",             (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "Processing",          (Color.FromArgb(219, 234, 254), Color.FromArgb( 29,  78, 216)) },
                { "Delivered",           (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
                { "Partially Delivered", (Color.FromArgb(237, 233, 254), Color.FromArgb( 91,  33, 182)) },
                { "Cancelled",           (Color.FromArgb(254, 226, 226), Color.FromArgb(185,  28,  28)) },
                { "Completed",           (Color.FromArgb(220, 252, 231), Color.FromArgb( 22, 101,  52)) },
                { "In Transit",          (Color.FromArgb(219, 234, 254), Color.FromArgb( 29,  78, 216)) },
                { "Sent",                (Color.FromArgb(219, 234, 254), Color.FromArgb( 29,  78, 216)) },
                { "Partially Received",  (Color.FromArgb(237, 233, 254), Color.FromArgb( 91,  33, 182)) },
                { "Fully Received",      (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
                { "Not Received",        (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "Received",            (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
                { "Escalated",           (Color.FromArgb(254, 226, 226), Color.FromArgb(185,  28,  28)) },
                { "Approved",            (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
                { "Rejected",            (Color.FromArgb(254, 226, 226), Color.FromArgb(185,  28,  28)) },
                { "Revenue",             (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
                { "Expense",             (Color.FromArgb(254, 226, 226), Color.FromArgb(185,  28,  28)) },
                { "Refund",              (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "Deposit",             (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
                { "Installment",         (Color.FromArgb(219, 234, 254), Color.FromArgb( 29,  78, 216)) },
                { "Full",                (Color.FromArgb(220, 252, 231), Color.FromArgb( 22, 101,  52)) },
                { "Sales Invoice",       (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
                { "Purchase Invoice",    (Color.FromArgb(254, 226, 226), Color.FromArgb(185,  28,  28)) },
                { "Return Refund",       (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
            };

        // Palette of bar/segment colours used by the GDI+ chart
        private static readonly Color[] ChartPalette = new Color[]
        {
            Color.FromArgb( 55,  48, 163),  // indigo
            Color.FromArgb(  6,  95,  70),  // green
            Color.FromArgb(185,  28,  28),  // red
            Color.FromArgb(146,  64,  14),  // amber
            Color.FromArgb( 29,  78, 216),  // blue
            Color.FromArgb( 91,  33, 182),  // purple
            Color.FromArgb(  3, 105, 161),  // sky
            Color.FromArgb( 22, 101,  52),  // emerald
        };

        // Supported chart styles passed to BuildChartCard
        private enum ChartStyle { Bar, Column, Pie }

        public ViewReportForm()
        {
            InitializeComponent();
            _tabButtons = new Button[] { btnTab0, btnTab1, btnTab2, btnTab3, btnTab4, btnTab5 };
            this.Load += ViewReportForm_Load;
        }

        // ════════════════════════════════════════════════════════════════
        //  LOAD
        // ════════════════════════════════════════════════════════════════

        private void ViewReportForm_Load(object sender, EventArgs e)
        {
            // NOTE: MenuItemClicked and LogoutClicked are subscribed in
            // InitializeComponent (Designer.cs) per AppShell RULE 4.
            // Do NOT re-subscribe here to avoid duplicate-fire.

            var vm = _ctrl.GetSalesReportVM();
            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Statistical Reports  \u203a  View Report");
            SwitchToReport(0);
        }

        // ════════════════════════════════════════════════════════════════
        //  REPORT SWITCHER
        // ════════════════════════════════════════════════════════════════

        private void SwitchToReport(int tabIndex)
        {
            if (_activeTab == tabIndex && pnlContent.Controls.Count > 0) return;
            _activeTab = tabIndex;

            pnlContent.SuspendLayout();
            pnlContent.Controls.Clear();
            pnlFilterOuter.Controls.Clear();

            HighlightTab(tabIndex);
            pnlTabOuter.Invalidate();

            switch (tabIndex)
            {
                case 0: RenderSales();        break;
                case 1: RenderInventory();    break;
                case 2: RenderProcurement();  break;
                case 3: RenderLogistics();    break;
                case 4: RenderAfterService(); break;
                case 5: RenderFinance();      break;
            }

            pnlContent.ResumeLayout(true);
        }

        // ════════════════════════════════════════════════════════════════
        //  CONTENT SWAP
        // ════════════════════════════════════════════════════════════════

        private void SwapContent(Panel dgvCard, Panel chartCard, bool showChart)
        {
            pnlContent.SuspendLayout();
            pnlContent.Controls.Clear();
            pnlContent.Controls.Add(showChart ? chartCard : dgvCard);
            pnlContent.ResumeLayout(true);
        }

        // ════════════════════════════════════════════════════════════════
        //  TAB HIGHLIGHT + UNDERLINE
        // ════════════════════════════════════════════════════════════════

        private void HighlightTab(int activeIndex)
        {
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                bool active = i == activeIndex;
                _tabButtons[i].ForeColor = active ? Palette.Primary : Color.FromArgb(98, 112, 135);
                _tabButtons[i].Font      = active
                    ? new Font("Segoe UI", 12f, FontStyle.Bold)
                    : new Font("Segoe UI", 12f, FontStyle.Regular);
                _tabButtons[i].BackColor = Color.White;
            }
        }

        private void PaintTabUnderline(object sender, PaintEventArgs e)
        {
            if (_activeTab < 0 || _activeTab >= _tabButtons.Length) return;
            var btn = _tabButtons[_activeTab];
            int padL = pnlTabOuter.Padding.Left;
            int x    = padL + btn.Bounds.X + 24;
            int w    = Math.Max(0, btn.Bounds.Width - 48);
            int y    = pnlTabOuter.Height - 4;
            using var brush = new SolidBrush(Palette.Primary);
            e.Graphics.FillRectangle(brush, x, y, w, 4);
        }

        // ════════════════════════════════════════════════════════════════
        //  FILTER BAR BUILDER
        // ════════════════════════════════════════════════════════════════

        private void SetFilterBar(string titleText, Panel fieldRow, Panel btnRow)
        {
            var tbl = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                RowCount        = 3,
                ColumnCount     = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(18, 14, 18, 14)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute,  60f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 125f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute,  65f));

            var pnlTitle = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlTitle.Controls.Add(new Label
            {
                Text      = titleText,
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            });
            pnlTitle.Controls.Add(new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 1,
                BackColor = Color.FromArgb(221, 227, 236)
            });

            fieldRow.Dock = DockStyle.Fill;
            btnRow.Dock   = DockStyle.Fill;

            tbl.Controls.Add(pnlTitle, 0, 0);
            tbl.Controls.Add(fieldRow, 0, 1);
            tbl.Controls.Add(btnRow,   0, 2);

            var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            card.Paint += PaintCardBorder;
            card.Controls.Add(tbl);

            pnlFilterOuter.Controls.Add(card);
        }

        private static TableLayoutPanel MakeCell(string caption, Control ctrl, bool rightPad = true)
        {
            var tlp = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                RowCount        = 2,
                ColumnCount     = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = rightPad ? new Padding(0, 0, 12, 0) : Padding.Empty
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent,  70f));

            var lbl = new Label
            {
                Text      = caption,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft,
                Padding   = new Padding(0, 0, 0, 2)
            };
            ctrl.Dock = DockStyle.Fill;
            tlp.Controls.Add(lbl,  0, 0);
            tlp.Controls.Add(ctrl, 0, 1);
            return tlp;
        }

        private static Panel BuildFieldsRow(params (string caption, Control ctrl)[] cols)
        {
            int n = Math.Max(1, cols.Length);
            var tbl = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = n,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = Padding.Empty
            };
            for (int i = 0; i < n; i++)
                tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / n));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            for (int i = 0; i < cols.Length; i++)
            {
                bool last = i == cols.Length - 1;
                tbl.Controls.Add(MakeCell(cols[i].caption, cols[i].ctrl, !last), i, 0);
            }
            return tbl;
        }

        private static Panel BuildDateRangeRow(
            DateTimePicker dtpFrom, DateTimePicker dtpTo,
            params (string caption, Control ctrl)[] extraCols)
        {
            int extraCount = extraCols == null ? 0 : extraCols.Length;
            int totalCols  = 3 + extraCount;
            float extraPct = extraCount > 0 ? 36f / extraCount : 0f;

            var tbl = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = totalCols,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = Padding.Empty
            };
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  8f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28f));
            for (int i = 0; i < extraCount; i++)
                tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, extraPct));

            var cellFrom = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None, Padding = new Padding(0, 0, 8, 0) };
            cellFrom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            cellFrom.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            cellFrom.RowStyles.Add(new RowStyle(SizeType.Percent,  70f));
            cellFrom.Controls.Add(new Label { Text = "Date Range", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(98, 112, 135), Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft, Padding = new Padding(0, 0, 0, 2) }, 0, 0);
            dtpFrom.Dock = DockStyle.Fill;
            cellFrom.Controls.Add(dtpFrom, 0, 1);
            tbl.Controls.Add(cellFrom, 0, 0);

            var cellSep = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None, Padding = Padding.Empty };
            cellSep.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            cellSep.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            cellSep.RowStyles.Add(new RowStyle(SizeType.Percent,  70f));
            cellSep.Controls.Add(new Label { Text = "", Dock = DockStyle.Fill }, 0, 0);
            cellSep.Controls.Add(new Label { Text = "to", Font = new Font("Segoe UI", 11f), ForeColor = Color.FromArgb(98, 112, 135), BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, AutoSize = false }, 0, 1);
            tbl.Controls.Add(cellSep, 1, 0);

            var cellTo = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None, Padding = new Padding(8, 0, 0, 0) };
            cellTo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            cellTo.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            cellTo.RowStyles.Add(new RowStyle(SizeType.Percent,  70f));
            cellTo.Controls.Add(new Label { Text = "", Dock = DockStyle.Fill }, 0, 0);
            dtpTo.Dock = DockStyle.Fill;
            cellTo.Controls.Add(dtpTo, 0, 1);
            tbl.Controls.Add(cellTo, 2, 0);

            if (extraCols != null)
                for (int i = 0; i < extraCols.Length; i++)
                    tbl.Controls.Add(MakeCell(extraCols[i].caption, extraCols[i].ctrl, i < extraCols.Length - 1), 3 + i, 0);

            return tbl;
        }

        private static Panel BuildButtonsRow(Button btnApply, Button btnReset, Button btnToggleView, Button btnExport)
        {
            const int BtnW = 210, BtnH = 50, Gap = 8;
            btnApply.Size = btnReset.Size = btnToggleView.Size = btnExport.Size = new Size(BtnW, BtnH);

            var pnlLeft = new Panel { Dock = DockStyle.Left, Width = BtnW * 2 + Gap, BackColor = Color.Transparent };
            pnlLeft.Controls.AddRange(new Control[] { btnApply, btnReset });
            pnlLeft.Resize += (s, e) =>
            {
                int top = Math.Max(0, (pnlLeft.Height - BtnH) / 2);
                btnApply.Location = new Point(0, top);
                btnReset.Location = new Point(BtnW + Gap, top);
            };

            var pnlRight = new Panel { Dock = DockStyle.Right, Width = BtnW * 2 + Gap, BackColor = Color.Transparent };
            pnlRight.Controls.AddRange(new Control[] { btnToggleView, btnExport });
            pnlRight.Resize += (s, e) =>
            {
                int top = Math.Max(0, (pnlRight.Height - BtnH) / 2);
                btnToggleView.Location = new Point(0, top);
                btnExport.Location     = new Point(BtnW + Gap, top);
            };

            var pnl = new Panel { BackColor = Color.Transparent, Dock = DockStyle.Fill };
            pnl.Controls.Add(pnlRight);
            pnl.Controls.Add(pnlLeft);
            return pnl;
        }

        // ════════════════════════════════════════════════════════════════
        //  GRID CARD + CHART CARD BUILDERS
        // ════════════════════════════════════════════════════════════════

        private Panel BuildGridCard(DataGridView dgv)
        {
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            inner.Paint += PaintCardBorder;
            inner.Controls.Add(dgv);

            var outer = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage, Padding = new Padding(20, 6, 20, 10) };
            outer.Controls.Add(inner);
            return outer;
        }

        // ════════════════════════════════════════════════════════════════
        //  AFTER-SERVICE — dual DGV card (Complaints + Return Orders)
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Builds the After-Service dual-table card:
        /// top half = Complaints DGV, bottom half = Return Orders DGV.
        /// </summary>
        private Panel BuildAfterServiceDualCard(
            DataGridView dgvComplaints,
            DataGridView dgvReturns)
        {
            var lblComplaints = new Label
            {
                Text      = "Complaints",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(55, 48, 163),
                Dock      = DockStyle.Top,
                Height    = 32,
                TextAlign = ContentAlignment.BottomLeft,
                Padding   = new Padding(4, 0, 0, 0)
            };
            var lblReturns = new Label
            {
                Text      = "Return Orders",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(185, 28, 28),
                Dock      = DockStyle.Top,
                Height    = 32,
                TextAlign = ContentAlignment.BottomLeft,
                Padding   = new Padding(4, 0, 0, 0)
            };

            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 0 };
            pnlTop.Controls.Add(dgvComplaints);
            pnlTop.Controls.Add(lblComplaints);
            dgvComplaints.Dock = DockStyle.Fill;

            var pnlBottom = new Panel { Dock = DockStyle.Fill };
            pnlBottom.Controls.Add(dgvReturns);
            pnlBottom.Controls.Add(lblReturns);
            dgvReturns.Dock = DockStyle.Fill;

            var divider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(221, 227, 236) };

            var split = new Panel { Dock = DockStyle.Fill };
            split.Controls.Add(pnlBottom);
            split.Controls.Add(divider);
            split.Controls.Add(pnlTop);

            // Give top half 50% on resize
            split.Resize += (s, e) => pnlTop.Height = split.Height / 2;

            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            inner.Paint += PaintCardBorder;
            inner.Controls.Add(split);

            var outer = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage, Padding = new Padding(20, 6, 20, 10) };
            outer.Controls.Add(inner);
            return outer;
        }

        private Panel BuildChartCard(
            string chartTitle,
            string[] labels,
            double[] values,
            ChartStyle style = ChartStyle.Bar)
        {
            var chartPanel = new GdiChartPanel(chartTitle, labels, values, style, ChartPalette)
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.White
            };

            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            inner.Paint += PaintCardBorder;
            inner.Controls.Add(chartPanel);

            var outer = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage, Padding = new Padding(20, 6, 20, 10) };
            outer.Controls.Add(inner);
            return outer;
        }

        // ════════════════════════════════════════════════════════════════
        //  GDI+ CHART PANEL
        // ════════════════════════════════════════════════════════════════

        private sealed class GdiChartPanel : Panel
        {
            private readonly string   _title;
            private readonly string[] _labels;
            private readonly double[] _values;
            private readonly ChartStyle _style;
            private readonly Color[]  _palette;

            public GdiChartPanel(string title, string[] labels, double[] values,
                                 ChartStyle style, Color[] palette)
            {
                _title   = title ?? string.Empty;
                _labels  = labels  ?? Array.Empty<string>();
                _values  = values  ?? Array.Empty<double>();
                _style   = style;
                _palette = palette;
                DoubleBuffered = true;
                ResizeRedraw   = true;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                g.SmoothingMode     = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                int n = Math.Min(_labels.Length, _values.Length);
                if (n == 0) { DrawEmpty(g); return; }

                var titleFont  = new Font("Segoe UI", 13f, FontStyle.Bold);
                var titleBrush = new SolidBrush(Color.FromArgb(15, 31, 53));
                g.DrawString(_title, titleFont, titleBrush, new PointF(20, 14));
                float titleH = titleFont.GetHeight(g) + 20;

                var plotRect = new RectangleF(60, titleH, Width - 80, Height - titleH - 60);

                switch (_style)
                {
                    case ChartStyle.Pie:    DrawPie(g, plotRect, n);                  break;
                    case ChartStyle.Column: DrawBars(g, plotRect, n, vertical: true); break;
                    default:                DrawBars(g, plotRect, n, vertical: false); break;
                }

                titleFont.Dispose();
                titleBrush.Dispose();
            }

            private void DrawEmpty(Graphics g)
            {
                var f = new Font("Segoe UI", 11f);
                var b = new SolidBrush(Color.FromArgb(150, 150, 150));
                string msg = "No data available";
                var sz = g.MeasureString(msg, f);
                g.DrawString(msg, f, b, (Width - sz.Width) / 2f, (Height - sz.Height) / 2f);
                f.Dispose(); b.Dispose();
            }

            private void DrawBars(Graphics g, RectangleF plotRect, int n, bool vertical)
            {
                double max = 0;
                foreach (var v in _values) if (v > max) max = v;
                if (max == 0) max = 1;

                var gridPen   = new Pen(Color.FromArgb(221, 227, 236), 1f);
                var axisFont  = new Font("Segoe UI", 9f);
                var axisBrush = new SolidBrush(Color.FromArgb(98, 112, 135));
                var valFont   = new Font("Segoe UI", 8f, FontStyle.Bold);
                const int GridLines = 4;

                if (vertical)
                {
                    float barAreaW = plotRect.Width - 40;
                    float barAreaH = plotRect.Height - 20;
                    float barX0    = plotRect.Left + 40;
                    float barY0    = plotRect.Top;

                    for (int i = 0; i <= GridLines; i++)
                    {
                        float y = barY0 + barAreaH - (barAreaH * i / GridLines);
                        g.DrawLine(gridPen, barX0, y, barX0 + barAreaW, y);
                        double val = max * i / GridLines;
                        string lbl = val >= 1000 ? $"{val / 1000:N1}k" : $"{val:N0}";
                        var sz = g.MeasureString(lbl, axisFont);
                        g.DrawString(lbl, axisFont, axisBrush, barX0 - sz.Width - 4, y - sz.Height / 2f);
                    }

                    float gap      = barAreaW / (n * 1.4f + 0.4f) * 0.4f;
                    float barWidth = Math.Max(4, (barAreaW - gap * (n + 1)) / n);

                    for (int i = 0; i < n; i++)
                    {
                        float barH = (float)(barAreaH * (_values[i] / max));
                        float bx   = barX0 + gap * (i + 1) + barWidth * i;
                        float by   = barY0 + barAreaH - barH;
                        using var brush = new SolidBrush(_palette[i % _palette.Length]);
                        g.FillRectangle(brush, bx, by, barWidth, barH);
                        string xl = _labels[i];
                        var xlSz  = g.MeasureString(xl, axisFont);
                        g.DrawString(xl, axisFont, axisBrush, bx + (barWidth - xlSz.Width) / 2f, barY0 + barAreaH + 4);
                        string vl = _values[i] >= 1000 ? $"{_values[i] / 1000:N1}k" : $"{_values[i]:N0}";
                        var vlSz  = g.MeasureString(vl, valFont);
                        if (barH > vlSz.Height + 4)
                        { using var wb = new SolidBrush(Color.White); g.DrawString(vl, valFont, wb, bx + (barWidth - vlSz.Width) / 2f, by + 4); }
                    }
                }
                else
                {
                    float barAreaW = plotRect.Width - 50;
                    float barAreaH = plotRect.Height - 10;
                    float barX0    = plotRect.Left + 50;
                    float barY0    = plotRect.Top;

                    for (int i = 0; i <= GridLines; i++)
                    {
                        float x = barX0 + barAreaW * i / GridLines;
                        g.DrawLine(gridPen, x, barY0, x, barY0 + barAreaH);
                        double val = max * i / GridLines;
                        string lbl = val >= 1000 ? $"{val / 1000:N1}k" : $"{val:N0}";
                        var sz = g.MeasureString(lbl, axisFont);
                        g.DrawString(lbl, axisFont, axisBrush, x - sz.Width / 2f, barY0 + barAreaH + 2);
                    }

                    float gap       = barAreaH / (n * 1.4f + 0.4f) * 0.4f;
                    float barHeight = Math.Max(4, (barAreaH - gap * (n + 1)) / n);

                    for (int i = 0; i < n; i++)
                    {
                        float barW = (float)(barAreaW * (_values[i] / max));
                        float bx   = barX0;
                        float by   = barY0 + gap * (i + 1) + barHeight * i;
                        using var brush = new SolidBrush(_palette[i % _palette.Length]);
                        g.FillRectangle(brush, bx, by, barW, barHeight);
                        string yl = _labels[i];
                        var ylSz  = g.MeasureString(yl, axisFont);
                        g.DrawString(yl, axisFont, axisBrush, barX0 - ylSz.Width - 4, by + (barHeight - ylSz.Height) / 2f);
                        string vl = _values[i] >= 1000 ? $"{_values[i] / 1000:N1}k" : $"{_values[i]:N0}";
                        var vlSz  = g.MeasureString(vl, valFont);
                        if (barW > vlSz.Width + 8)
                        { using var wb = new SolidBrush(Color.White); g.DrawString(vl, valFont, wb, bx + barW - vlSz.Width - 6, by + (barHeight - ylSz.Height) / 2f); }
                    }
                }

                gridPen.Dispose(); axisFont.Dispose(); axisBrush.Dispose(); valFont.Dispose();
            }

            private void DrawPie(Graphics g, RectangleF plotRect, int n)
            {
                double total = 0;
                foreach (var v in _values) total += v;
                if (total == 0) { DrawEmpty(g); return; }

                float legW  = 160;
                float pieW  = Math.Min(plotRect.Width - legW - 20, plotRect.Height - 20);
                float pieX  = plotRect.Left + (plotRect.Width - legW - 20 - pieW) / 2f;
                float pieY  = plotRect.Top  + (plotRect.Height - pieW) / 2f;
                var pieRect = new RectangleF(pieX, pieY, pieW, pieW);

                var labelFont  = new Font("Segoe UI", 9f);
                var labelBrush = new SolidBrush(Color.FromArgb(15, 31, 53));
                float startAngle = -90f;
                for (int i = 0; i < n; i++)
                {
                    float sweep = (float)(_values[i] / total * 360.0);
                    using var brush = new SolidBrush(_palette[i % _palette.Length]);
                    g.FillPie(brush, pieRect.X, pieRect.Y, pieRect.Width, pieRect.Height, startAngle, sweep);
                    using var pen = new Pen(Color.White, 1.5f);
                    g.DrawPie(pen, pieRect.X, pieRect.Y, pieRect.Width, pieRect.Height, startAngle, sweep);
                    startAngle += sweep;
                }

                float legX = pieX + pieW + 20;
                float legY = plotRect.Top + 10;
                for (int i = 0; i < n; i++)
                {
                    float pct = (float)(_values[i] / total * 100.0);
                    using var dotBrush = new SolidBrush(_palette[i % _palette.Length]);
                    g.FillEllipse(dotBrush, legX, legY + 3, 12, 12);
                    g.DrawString($"{_labels[i]}  {pct:N1}%", labelFont, labelBrush, legX + 18, legY);
                    legY += labelFont.GetHeight(g) + 8;
                }

                labelFont.Dispose();
                labelBrush.Dispose();
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  TOGGLE HELPER
        // ════════════════════════════════════════════════════════════════

        private static void ApplyToggleStyle(Button btn, bool currentlyChart)
        {
            if (currentlyChart)
            {
                btn.Text      = "\U0001F4CB  Table";
                btn.BackColor = Color.FromArgb(2, 132, 199);
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(3, 105, 161);
            }
            else
            {
                btn.Text      = "\U0001F4CA  Chart";
                btn.BackColor = Color.FromArgb(217, 119, 6);
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(180, 95, 4);
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  DATAGRIDVIEW FACTORY
        // ════════════════════════════════════════════════════════════════

        private static DataGridView MakeDgv()
        {
            var dgv = new DataGridView
            {
                ReadOnly                  = true,
                AllowUserToAddRows        = false,
                AllowUserToDeleteRows     = false,
                RowHeadersVisible         = false,
                SelectionMode             = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect               = false,
                BackgroundColor           = Color.White,
                BorderStyle               = BorderStyle.None,
                GridColor                 = Color.FromArgb(221, 227, 236),
                Font                      = new Font("Segoe UI", 13f),
                AutoSizeColumnsMode       = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle           = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate               = { Height = 48 },
                Dock                      = DockStyle.Fill,
                ColumnHeadersHeight       = 46,
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255),
                    ForeColor = Color.FromArgb(98, 112, 135),
                    Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                    Padding   = new Padding(12, 0, 0, 0),
                    Alignment = DataGridViewContentAlignment.MiddleLeft
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

            // Suppress the default DataGridView error dialog
            dgv.DataError += (sender, e) => { e.Cancel = true; };

            return dgv;
        }

        // ════════════════════════════════════════════════════════════════
        //  BUTTON FACTORIES
        // ════════════════════════════════════════════════════════════════

        private static Button MakePrimaryBtn(string text)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(55, 48, 163), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(49, 46, 129);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(38, 35, 100);
            return b;
        }

        private static Button MakeOutlineBtn(string text)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(71, 85, 105), BackColor = Color.FromArgb(241, 245, 249), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(226, 232, 240);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(203, 213, 225);
            return b;
        }

        private static Button MakeAmberBtn(string text)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(217, 119, 6), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(180, 95, 4);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(146, 75, 2);
            return b;
        }

        private static Button MakeExportBtn(string text)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(6, 95, 70), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(4, 78, 56);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(2, 60, 43);
            return b;
        }

        // ════════════════════════════════════════════════════════════════
        //  REPORT RENDERERS
        // ════════════════════════════════════════════════════════════════

        // ── 0. Sales Performance ───────────────────────────────────────────────
        private void RenderSales()
        {
            var dtpFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DefaultDateFrom, Font = new Font("Segoe UI", 12f) };
            var dtpTo   = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DefaultDateTo,   Font = new Font("Segoe UI", 12f) };

            var btnApply  = MakePrimaryBtn("\U0001F50D  Apply");
            var btnReset  = MakeOutlineBtn("\u21BA  Reset");
            var btnToggle = MakeAmberBtn("\U0001F4CA  Chart");
            var btnExport = MakeExportBtn("\U0001F4E4  Export");
            ApplyToggleStyle(btnToggle, _salesChart);

            var dgv = MakeDgv();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",    HeaderText = "DATE",         FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrder",   HeaderText = "ORDER ID",     FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCust",    HeaderText = "CUSTOMER",     FillWeight = 22 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLines",   HeaderText = "LINES",        FillWeight =  8 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRevenue", HeaderText = "REVENUE",      FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",  HeaderText = "ORDER STATUS", FillWeight = 15 });
            dgv.CellFormatting += DgvCellFormatting;

            LoadSalesData(dgv, dtpFrom, dtpTo);

            var dgvCard   = BuildGridCard(dgv);
            var chartCard = BuildSalesChartCard(dtpFrom, dtpTo);

            btnApply.Click += (s, e) =>
            {
                LoadSalesData(dgv, dtpFrom, dtpTo);
                if (_salesChart) { pnlContent.SuspendLayout(); pnlContent.Controls.Clear(); pnlContent.Controls.Add(BuildSalesChartCard(dtpFrom, dtpTo)); pnlContent.ResumeLayout(true); }
                else SwapContent(dgvCard, chartCard, false);
            };
            btnReset.Click += (s, e) =>
            {
                dtpFrom.Value = DefaultDateFrom;
                dtpTo.Value   = DefaultDateTo;
                LoadSalesData(dgv, dtpFrom, dtpTo);
                SwapContent(dgvCard, chartCard, _salesChart);
            };
            btnToggle.Click += (s, e) =>
            {
                _salesChart = !_salesChart;
                ApplyToggleStyle(btnToggle, _salesChart);
                if (_salesChart) { pnlContent.SuspendLayout(); pnlContent.Controls.Clear(); pnlContent.Controls.Add(BuildSalesChartCard(dtpFrom, dtpTo)); pnlContent.ResumeLayout(true); }
                else SwapContent(dgvCard, chartCard, false);
            };
            btnExport.Click += (s, e) => ExportGrid(dgv, "SalesPerformance");

            SetFilterBar("Sales Performance",
                BuildDateRangeRow(dtpFrom, dtpTo),
                BuildButtonsRow(btnApply, btnReset, btnToggle, btnExport));

            SwapContent(dgvCard, chartCard, _salesChart);
        }

        private void LoadSalesData(DataGridView dgv, DateTimePicker dtpFrom, DateTimePicker dtpTo)
        {
            dgv.Rows.Clear();
            try
            {
                var vm = _ctrl.GetSalesReportVM(dtpFrom.Value, dtpTo.Value);
                if (vm.SalesRows == null) return;
                foreach (var r in vm.SalesRows)
                    dgv.Rows.Add(
                        r.IssuedTime.ToString("yyyy-MM-dd"),
                        r.OrderID,
                        r.CustomerName,
                        r.LineCount,
                        r.GrandTotal.ToString("N2"),
                        r.OrderStatus);
            }
            catch { }
        }

        private Panel BuildSalesChartCard(DateTimePicker dtpFrom, DateTimePicker dtpTo)
        {
            var revenueByStatus = new Dictionary<string, double>();
            try
            {
                var vm = _ctrl.GetSalesReportVM(dtpFrom.Value, dtpTo.Value);
                if (vm.SalesRows != null)
                    foreach (var r in vm.SalesRows)
                    {
                        string key = r.OrderStatus ?? "Unknown";
                        if (!revenueByStatus.ContainsKey(key)) revenueByStatus[key] = 0;
                        revenueByStatus[key] += r.GrandTotal;
                    }
            }
            catch { }
            return BuildChartCard("Sales Revenue by Status",
                new List<string>(revenueByStatus.Keys).ToArray(),
                new List<double>(revenueByStatus.Values).ToArray(),
                ChartStyle.Bar);
        }

        // ── 1. Inventory Status ─────────────────────────────────────────────
        private void RenderInventory()
        {
            var txtKeyword  = new TextBox { Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "Item ID / Item Name" };
            var cboCategory = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            cboCategory.Items.AddRange(new object[] { "All", "Product", "Raw Material" });
            cboCategory.SelectedIndex = 0;
            var chkBelowReorder = new CheckBox { Text = "Below Reorder Only", Font = new Font("Segoe UI", 12f), BackColor = Color.Transparent };

            var btnApply  = MakePrimaryBtn("\U0001F50D  Apply");
            var btnReset  = MakeOutlineBtn("\u21BA  Reset");
            var btnToggle = MakeAmberBtn("\U0001F4CA  Chart");
            var btnExport = MakeExportBtn("\U0001F4E4  Export");
            ApplyToggleStyle(btnToggle, _inventoryChart);

            var dgv = MakeDgv();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colWHItemID",  HeaderText = "WH ITEM ID",    FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItemID",    HeaderText = "ITEM ID",       FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName",      HeaderText = "ITEM NAME",     FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCategory",  HeaderText = "CATEGORY",      FillWeight = 11 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMatType",   HeaderText = "MATERIAL TYPE", FillWeight = 11 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colWH",        HeaderText = "WAREHOUSE",     FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStock",     HeaderText = "CURRENT STOCK", FillWeight = 10 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colReorder",   HeaderText = "REORDER LEVEL", FillWeight = 10 });
            dgv.CellFormatting += DgvCellFormatting;

            LoadInventoryData(dgv, cboCategory, chkBelowReorder, txtKeyword);

            var dgvCard   = BuildGridCard(dgv);
            var chartCard = BuildInventoryChartCard(cboCategory, chkBelowReorder, txtKeyword);

            btnApply.Click += (s, e) =>
            {
                LoadInventoryData(dgv, cboCategory, chkBelowReorder, txtKeyword);
                if (_inventoryChart) { pnlContent.SuspendLayout(); pnlContent.Controls.Clear(); pnlContent.Controls.Add(BuildInventoryChartCard(cboCategory, chkBelowReorder, txtKeyword)); pnlContent.ResumeLayout(true); }
                else SwapContent(dgvCard, chartCard, false);
            };
            btnReset.Click += (s, e) =>
            {
                txtKeyword.Clear(); cboCategory.SelectedIndex = 0; chkBelowReorder.Checked = false;
                LoadInventoryData(dgv, cboCategory, chkBelowReorder, txtKeyword);
                SwapContent(dgvCard, chartCard, _inventoryChart);
            };
            btnToggle.Click += (s, e) =>
            {
                _inventoryChart = !_inventoryChart;
                ApplyToggleStyle(btnToggle, _inventoryChart);
                if (_inventoryChart) { pnlContent.SuspendLayout(); pnlContent.Controls.Clear(); pnlContent.Controls.Add(BuildInventoryChartCard(cboCategory, chkBelowReorder, txtKeyword)); pnlContent.ResumeLayout(true); }
                else SwapContent(dgvCard, chartCard, false);
            };
            btnExport.Click += (s, e) => ExportGrid(dgv, "InventoryStatus");

            SetFilterBar("Inventory Status",
                BuildFieldsRow(("Keyword", txtKeyword), ("Category", cboCategory), ("Filter", chkBelowReorder)),
                BuildButtonsRow(btnApply, btnReset, btnToggle, btnExport));

            SwapContent(dgvCard, chartCard, _inventoryChart);
        }

        private void LoadInventoryData(DataGridView dgv, ComboBox cboCat, CheckBox chkBelow, TextBox txtKw)
        {
            dgv.Rows.Clear();
            try
            {
                string cat     = cboCat.SelectedIndex == 0 ? null : cboCat.SelectedItem?.ToString();
                string keyword = string.IsNullOrWhiteSpace(txtKw?.Text) ? null : txtKw.Text.Trim();
                var vm = _ctrl.GetInventoryReportVM(cat, chkBelow.Checked, keyword);
                if (vm.InventoryRows == null) return;
                foreach (var r in vm.InventoryRows)
                    dgv.Rows.Add(
                        r.WarehouseItemID,
                        r.ItemID,
                        r.ItemName,
                        r.ItemCategory,
                        string.IsNullOrEmpty(r.MaterialType) ? "\u2014" : r.MaterialType,
                        r.WarehouseLocation,
                        r.CurrentStock.ToString(),
                        r.ReorderLevel.ToString());
            }
            catch { }
        }

        private Panel BuildInventoryChartCard(ComboBox cboCat, CheckBox chkBelow, TextBox txtKw)
        {
            var stockByCategory = new Dictionary<string, double>();
            try
            {
                string cat     = cboCat.SelectedIndex == 0 ? null : cboCat.SelectedItem?.ToString();
                string keyword = string.IsNullOrWhiteSpace(txtKw?.Text) ? null : txtKw.Text.Trim();
                var vm = _ctrl.GetInventoryReportVM(cat, chkBelow.Checked, keyword);
                if (vm.InventoryRows != null)
                    foreach (var r in vm.InventoryRows)
                    {
                        string key = r.ItemCategory ?? "Unknown";
                        if (!stockByCategory.ContainsKey(key)) stockByCategory[key] = 0;
                        stockByCategory[key] += Convert.ToDouble(r.CurrentStock);
                    }
            }
            catch { }
            return BuildChartCard("Current Stock by Category",
                new List<string>(stockByCategory.Keys).ToArray(),
                new List<double>(stockByCategory.Values).ToArray(),
                ChartStyle.Column);
        }

        // ── 2. Procurement Summary ──────────────────────────────────────────
        private void RenderProcurement()
        {
            var dtpFrom   = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DefaultDateFrom, Font = new Font("Segoe UI", 12f) };
            var dtpTo     = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DefaultDateTo,   Font = new Font("Segoe UI", 12f) };
            var cboStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            cboStatus.Items.AddRange(new object[] { "All", "Sent", "Partially Received", "Fully Received", "Not Received", "Completed", "Cancelled" });
            cboStatus.SelectedIndex = 0;

            var btnApply  = MakePrimaryBtn("\U0001F50D  Apply");
            var btnReset  = MakeOutlineBtn("\u21BA  Reset");
            var btnToggle = MakeAmberBtn("\U0001F4CA  Chart");
            var btnExport = MakeExportBtn("\U0001F4E4  Export");
            ApplyToggleStyle(btnToggle, _procurementChart);

            var dgv = MakeDgv();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",       HeaderText = "DATE",            FillWeight = 13 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPO",         HeaderText = "PO ID",           FillWeight = 13 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSupplier",   HeaderText = "SUPPLIER",        FillWeight = 22 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItems",      HeaderText = "ITEMS",           FillWeight =  8 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal",      HeaderText = "TOTAL AMOUNT",    FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRtStatus",   HeaderText = "RECEIPT STATUS",  FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPoStatus",   HeaderText = "PO STATUS",       FillWeight = 14 });
            dgv.CellFormatting += DgvCellFormatting;

            LoadProcurementData(dgv, dtpFrom, dtpTo, cboStatus);

            var dgvCard   = BuildGridCard(dgv);
            var chartCard = BuildProcurementChartCard(dtpFrom, dtpTo, cboStatus);

            btnApply.Click += (s, e) =>
            {
                LoadProcurementData(dgv, dtpFrom, dtpTo, cboStatus);
                if (_procurementChart) { pnlContent.SuspendLayout(); pnlContent.Controls.Clear(); pnlContent.Controls.Add(BuildProcurementChartCard(dtpFrom, dtpTo, cboStatus)); pnlContent.ResumeLayout(true); }
                else SwapContent(dgvCard, chartCard, false);
            };
            btnReset.Click += (s, e) =>
            {
                dtpFrom.Value = DefaultDateFrom; dtpTo.Value = DefaultDateTo; cboStatus.SelectedIndex = 0;
                LoadProcurementData(dgv, dtpFrom, dtpTo, cboStatus);
                SwapContent(dgvCard, chartCard, _procurementChart);
            };
            btnToggle.Click += (s, e) =>
            {
                _procurementChart = !_procurementChart;
                ApplyToggleStyle(btnToggle, _procurementChart);
                if (_procurementChart) { pnlContent.SuspendLayout(); pnlContent.Controls.Clear(); pnlContent.Controls.Add(BuildProcurementChartCard(dtpFrom, dtpTo, cboStatus)); pnlContent.ResumeLayout(true); }
                else SwapContent(dgvCard, chartCard, false);
            };
            btnExport.Click += (s, e) => ExportGrid(dgv, "ProcurementSummary");

            SetFilterBar("Procurement Summary",
                BuildDateRangeRow(dtpFrom, dtpTo, ("PO Status", cboStatus)),
                BuildButtonsRow(btnApply, btnReset, btnToggle, btnExport));

            SwapContent(dgvCard, chartCard, _procurementChart);
        }

        private void LoadProcurementData(DataGridView dgv, DateTimePicker dtpFrom, DateTimePicker dtpTo, ComboBox cboStatus)
        {
            dgv.Rows.Clear();
            try
            {
                string status = cboStatus.SelectedIndex == 0 ? null : cboStatus.SelectedItem?.ToString();
                var vm = _ctrl.GetProcurementReportVM(dtpFrom.Value, dtpTo.Value, status);
                if (vm.ProcurementRows == null) return;
                foreach (var r in vm.ProcurementRows)
                    dgv.Rows.Add(
                        r.OrderDate.ToString("yyyy-MM-dd"),
                        r.PurchaseOrderID,
                        r.SupplierName,
                        r.ItemCount,
                        r.TotalAmount.ToString("N2"),
                        r.ReceiptStatus,
                        r.PurchaseStatus);
            }
            catch { }
        }

        private Panel BuildProcurementChartCard(DateTimePicker dtpFrom, DateTimePicker dtpTo, ComboBox cboStatus)
        {
            var spendBySupplier = new Dictionary<string, double>();
            try
            {
                string status = cboStatus.SelectedIndex == 0 ? null : cboStatus.SelectedItem?.ToString();
                var vm = _ctrl.GetProcurementReportVM(dtpFrom.Value, dtpTo.Value, status);
                if (vm.ProcurementRows != null)
                    foreach (var r in vm.ProcurementRows)
                    {
                        string key = r.SupplierName ?? "Unknown";
                        if (!spendBySupplier.ContainsKey(key)) spendBySupplier[key] = 0;
                        spendBySupplier[key] += r.TotalAmount;
                    }
            }
            catch { }
            return BuildChartCard("Procurement Spend by Supplier",
                new List<string>(spendBySupplier.Keys).ToArray(),
                new List<double>(spendBySupplier.Values).ToArray(),
                ChartStyle.Bar);
        }

        // ── 3. Logistics Overview ───────────────────────────────────────────
        private void RenderLogistics()
        {
            var dtpFrom   = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DefaultDateFrom, Font = new Font("Segoe UI", 12f) };
            var dtpTo     = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DefaultDateTo,   Font = new Font("Segoe UI", 12f) };
            var cboStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            cboStatus.Items.AddRange(new object[] { "All", "Pending", "Processing", "In Transit", "Delivered", "Partially Delivered", "Cancelled" });
            cboStatus.SelectedIndex = 0;

            var btnApply  = MakePrimaryBtn("\U0001F50D  Apply");
            var btnReset  = MakeOutlineBtn("\u21BA  Reset");
            var btnToggle = MakeAmberBtn("\U0001F4CA  Chart");
            var btnExport = MakeExportBtn("\U0001F4E4  Export");
            ApplyToggleStyle(btnToggle, _logisticsChart);

            var dgv = MakeDgv();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",    HeaderText = "DELIVERY DATE",    FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDO",      HeaderText = "DO ID",            FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrder",   HeaderText = "SALES ORDER",      FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCust",    HeaderText = "CUSTOMER",         FillWeight = 22 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDriver",  HeaderText = "DRIVER",           FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",  HeaderText = "STATUS",           FillWeight = 14 });
            dgv.CellFormatting += DgvCellFormatting;

            LoadLogisticsData(dgv, dtpFrom, dtpTo, cboStatus);

            var dgvCard   = BuildGridCard(dgv);
            var chartCard = BuildLogisticsChartCard(dtpFrom, dtpTo, cboStatus);

            btnApply.Click += (s, e) =>
            {
                LoadLogisticsData(dgv, dtpFrom, dtpTo, cboStatus);
                if (_logisticsChart) { pnlContent.SuspendLayout(); pnlContent.Controls.Clear(); pnlContent.Controls.Add(BuildLogisticsChartCard(dtpFrom, dtpTo, cboStatus)); pnlContent.ResumeLayout(true); }
                else SwapContent(dgvCard, chartCard, false);
            };
            btnReset.Click += (s, e) =>
            {
                dtpFrom.Value = DefaultDateFrom; dtpTo.Value = DefaultDateTo; cboStatus.SelectedIndex = 0;
                LoadLogisticsData(dgv, dtpFrom, dtpTo, cboStatus);
                SwapContent(dgvCard, chartCard, _logisticsChart);
            };
            btnToggle.Click += (s, e) =>
            {
                _logisticsChart = !_logisticsChart;
                ApplyToggleStyle(btnToggle, _logisticsChart);
                if (_logisticsChart) { pnlContent.SuspendLayout(); pnlContent.Controls.Clear(); pnlContent.Controls.Add(BuildLogisticsChartCard(dtpFrom, dtpTo, cboStatus)); pnlContent.ResumeLayout(true); }
                else SwapContent(dgvCard, chartCard, false);
            };
            btnExport.Click += (s, e) => ExportGrid(dgv, "LogisticsOverview");

            SetFilterBar("Logistics Overview",
                BuildDateRangeRow(dtpFrom, dtpTo, ("Delivery Status", cboStatus)),
                BuildButtonsRow(btnApply, btnReset, btnToggle, btnExport));

            SwapContent(dgvCard, chartCard, _logisticsChart);
        }

        private void LoadLogisticsData(DataGridView dgv, DateTimePicker dtpFrom, DateTimePicker dtpTo, ComboBox cboStatus)
        {
            dgv.Rows.Clear();
            try
            {
                string status = cboStatus.SelectedIndex == 0 ? null : cboStatus.SelectedItem?.ToString();
                var vm = _ctrl.GetLogisticsReportVM(dtpFrom.Value, dtpTo.Value, status);
                if (vm.LogisticsRows == null) return;
                foreach (var r in vm.LogisticsRows)
                    dgv.Rows.Add(
                        r.DeliveryDate.ToString("yyyy-MM-dd"),
                        r.DeliveryOrderID,
                        r.SalesOrderID,
                        r.CustomerName,
                        r.DriverName,
                        r.DeliveryStatus);
            }
            catch { }
        }

        private Panel BuildLogisticsChartCard(DateTimePicker dtpFrom, DateTimePicker dtpTo, ComboBox cboStatus)
        {
            var countByStatus = new Dictionary<string, double>();
            try
            {
                string status = cboStatus.SelectedIndex == 0 ? null : cboStatus.SelectedItem?.ToString();
                var vm = _ctrl.GetLogisticsReportVM(dtpFrom.Value, dtpTo.Value, status);
                if (vm.LogisticsRows != null)
                    foreach (var r in vm.LogisticsRows)
                    {
                        string key = r.DeliveryStatus ?? "Unknown";
                        if (!countByStatus.ContainsKey(key)) countByStatus[key] = 0;
                        countByStatus[key]++;
                    }
            }
            catch { }
            return BuildChartCard("Shipments by Status",
                new List<string>(countByStatus.Keys).ToArray(),
                new List<double>(countByStatus.Values).ToArray(),
                ChartStyle.Column);
        }

        // ── 4. After-Service Summary ────────────────────────────────────────
        private void RenderAfterService()
        {
            var dtpFrom   = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DefaultDateFrom, Font = new Font("Segoe UI", 12f) };
            var dtpTo     = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DefaultDateTo,   Font = new Font("Segoe UI", 12f) };
            var cboCmpStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            cboCmpStatus.Items.AddRange(new object[] { "All", "Open", "In Progress", "Resolved", "Escalated", "Closed" });
            cboCmpStatus.SelectedIndex = 0;

            var btnApply  = MakePrimaryBtn("\U0001F50D  Apply");
            var btnReset  = MakeOutlineBtn("\u21BA  Reset");
            var btnToggle = MakeAmberBtn("\U0001F4CA  Chart");
            var btnExport = MakeExportBtn("\U0001F4E4  Export");
            ApplyToggleStyle(btnToggle, _afterServiceChart);

            var dgvC = MakeDgv();
            dgvC.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCDate",    HeaderText = "DATE",          FillWeight = 14 });
            dgvC.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCID",      HeaderText = "COMPLAINT ID",  FillWeight = 16 });
            dgvC.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCCust",    HeaderText = "CUSTOMER",      FillWeight = 22 });
            dgvC.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCSubject", HeaderText = "SUBJECT",       FillWeight = 26 });
            dgvC.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCStatus",  HeaderText = "STATUS",        FillWeight = 13 });
            dgvC.CellFormatting += DgvCellFormatting;

            var dgvR = MakeDgv();
            dgvR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRDate",    HeaderText = "DATE",          FillWeight = 14 });
            dgvR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRID",      HeaderText = "RETURN ID",     FillWeight = 16 });
            dgvR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colROrder",   HeaderText = "ORDER ID",      FillWeight = 16 });
            dgvR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRCust",    HeaderText = "CUSTOMER",      FillWeight = 22 });
            dgvR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRReason",  HeaderText = "REASON",        FillWeight = 22 });
            dgvR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRStatus",  HeaderText = "STATUS",        FillWeight = 10 });
            dgvR.CellFormatting += DgvCellFormatting;

            LoadAfterServiceData(dgvC, dgvR, dtpFrom, dtpTo, cboCmpStatus);

            var dgvCard   = BuildAfterServiceDualCard(dgvC, dgvR);
            var chartCard = BuildAfterServiceChartCard(dtpFrom, dtpTo, cboCmpStatus);

            btnApply.Click += (s, e) =>
            {
                LoadAfterServiceData(dgvC, dgvR, dtpFrom, dtpTo, cboCmpStatus);
                if (_afterServiceChart) { pnlContent.SuspendLayout(); pnlContent.Controls.Clear(); pnlContent.Controls.Add(BuildAfterServiceChartCard(dtpFrom, dtpTo, cboCmpStatus)); pnlContent.ResumeLayout(true); }
                else SwapContent(dgvCard, chartCard, false);
            };
            btnReset.Click += (s, e) =>
            {
                dtpFrom.Value = DefaultDateFrom; dtpTo.Value = DefaultDateTo; cboCmpStatus.SelectedIndex = 0;
                LoadAfterServiceData(dgvC, dgvR, dtpFrom, dtpTo, cboCmpStatus);
                SwapContent(dgvCard, chartCard, _afterServiceChart);
            };
            btnToggle.Click += (s, e) =>
            {
                _afterServiceChart = !_afterServiceChart;
                ApplyToggleStyle(btnToggle, _afterServiceChart);
                if (_afterServiceChart) { pnlContent.SuspendLayout(); pnlContent.Controls.Clear(); pnlContent.Controls.Add(BuildAfterServiceChartCard(dtpFrom, dtpTo, cboCmpStatus)); pnlContent.ResumeLayout(true); }
                else SwapContent(dgvCard, chartCard, false);
            };
            btnExport.Click += (s, e) => ExportGrid(dgvC, "AfterServiceComplaints");

            SetFilterBar("After-Service Summary",
                BuildDateRangeRow(dtpFrom, dtpTo, ("Complaint Status", cboCmpStatus)),
                BuildButtonsRow(btnApply, btnReset, btnToggle, btnExport));

            SwapContent(dgvCard, chartCard, _afterServiceChart);
        }

        private void LoadAfterServiceData(DataGridView dgvC, DataGridView dgvR,
            DateTimePicker dtpFrom, DateTimePicker dtpTo, ComboBox cboCmpStatus)
        {
            dgvC.Rows.Clear();
            dgvR.Rows.Clear();
            try
            {
                string cmpStatus = cboCmpStatus.SelectedIndex == 0 ? null : cboCmpStatus.SelectedItem?.ToString();
                var vm = _ctrl.GetAfterServiceReportVM(dtpFrom.Value, dtpTo.Value, cmpStatus);
                if (vm.ComplaintRows != null)
                    foreach (var r in vm.ComplaintRows)
                        dgvC.Rows.Add(r.CreatedAt.ToString("yyyy-MM-dd"), r.ComplaintID, r.CustomerName, r.Subject, r.Status);
                if (vm.ReturnRows != null)
                    foreach (var r in vm.ReturnRows)
                        dgvR.Rows.Add(r.ReturnDate.ToString("yyyy-MM-dd"), r.ReturnOrderID, r.SalesOrderID, r.CustomerName, r.Reason, r.Status);
            }
            catch { }
        }

        private Panel BuildAfterServiceChartCard(DateTimePicker dtpFrom, DateTimePicker dtpTo, ComboBox cboCmpStatus)
        {
            var countByStatus = new Dictionary<string, double>();
            try
            {
                string cmpStatus = cboCmpStatus.SelectedIndex == 0 ? null : cboCmpStatus.SelectedItem?.ToString();
                var vm = _ctrl.GetAfterServiceReportVM(dtpFrom.Value, dtpTo.Value, cmpStatus);
                if (vm.ComplaintRows != null)
                    foreach (var r in vm.ComplaintRows)
                    {
                        string key = r.Status ?? "Unknown";
                        if (!countByStatus.ContainsKey(key)) countByStatus[key] = 0;
                        countByStatus[key]++;
                    }
            }
            catch { }
            return BuildChartCard("Complaints by Status",
                new List<string>(countByStatus.Keys).ToArray(),
                new List<double>(countByStatus.Values).ToArray(),
                ChartStyle.Pie);
        }

        // ── 5. Finance Overview ─────────────────────────────────────────────
        private void RenderFinance()
        {
            var dtpFrom   = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DefaultDateFrom, Font = new Font("Segoe UI", 12f) };
            var dtpTo     = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DefaultDateTo,   Font = new Font("Segoe UI", 12f) };
            var cboType   = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            cboType.Items.AddRange(new object[] { "All", "Sales Invoice", "Purchase Invoice", "Return Refund" });
            cboType.SelectedIndex = 0;

            var btnApply  = MakePrimaryBtn("\U0001F50D  Apply");
            var btnReset  = MakeOutlineBtn("\u21BA  Reset");
            var btnToggle = MakeAmberBtn("\U0001F4CA  Chart");
            var btnExport = MakeExportBtn("\U0001F4E4  Export");
            ApplyToggleStyle(btnToggle, _financeChart);

            var dgv = MakeDgv();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",      HeaderText = "DATE",          FillWeight = 13 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colInvoiceID", HeaderText = "INVOICE ID",    FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colType",      HeaderText = "TYPE",          FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colParty",     HeaderText = "PARTY",         FillWeight = 22 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAmount",    HeaderText = "AMOUNT",        FillWeight = 13 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPayMethod", HeaderText = "PAYMENT METHOD",FillWeight = 13 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",    HeaderText = "STATUS",        FillWeight = 10 });
            dgv.CellFormatting += DgvCellFormatting;

            LoadFinanceData(dgv, dtpFrom, dtpTo, cboType);

            var dgvCard   = BuildGridCard(dgv);
            var chartCard = BuildFinanceChartCard(dtpFrom, dtpTo, cboType);

            btnApply.Click += (s, e) =>
            {
                LoadFinanceData(dgv, dtpFrom, dtpTo, cboType);
                if (_financeChart) { pnlContent.SuspendLayout(); pnlContent.Controls.Clear(); pnlContent.Controls.Add(BuildFinanceChartCard(dtpFrom, dtpTo, cboType)); pnlContent.ResumeLayout(true); }
                else SwapContent(dgvCard, chartCard, false);
            };
            btnReset.Click += (s, e) =>
            {
                dtpFrom.Value = DefaultDateFrom; dtpTo.Value = DefaultDateTo; cboType.SelectedIndex = 0;
                LoadFinanceData(dgv, dtpFrom, dtpTo, cboType);
                SwapContent(dgvCard, chartCard, _financeChart);
            };
            btnToggle.Click += (s, e) =>
            {
                _financeChart = !_financeChart;
                ApplyToggleStyle(btnToggle, _financeChart);
                if (_financeChart) { pnlContent.SuspendLayout(); pnlContent.Controls.Clear(); pnlContent.Controls.Add(BuildFinanceChartCard(dtpFrom, dtpTo, cboType)); pnlContent.ResumeLayout(true); }
                else SwapContent(dgvCard, chartCard, false);
            };
            btnExport.Click += (s, e) => ExportGrid(dgv, "FinanceOverview");

            SetFilterBar("Finance Overview",
                BuildDateRangeRow(dtpFrom, dtpTo, ("Invoice Type", cboType)),
                BuildButtonsRow(btnApply, btnReset, btnToggle, btnExport));

            SwapContent(dgvCard, chartCard, _financeChart);
        }

        private void LoadFinanceData(DataGridView dgv, DateTimePicker dtpFrom, DateTimePicker dtpTo, ComboBox cboType)
        {
            dgv.Rows.Clear();
            try
            {
                string type = cboType.SelectedIndex == 0 ? null : cboType.SelectedItem?.ToString();
                var vm = _ctrl.GetFinanceReportVM(dtpFrom.Value, dtpTo.Value, type);
                if (vm.FinanceRows == null) return;
                foreach (var r in vm.FinanceRows)
                    dgv.Rows.Add(
                        r.InvoiceDate.ToString("yyyy-MM-dd"),
                        r.InvoiceID,
                        r.InvoiceType,
                        r.PartyName,
                        r.Amount.ToString("N2"),
                        r.PaymentMethod,
                        r.PaymentStatus);
            }
            catch { }
        }

        private Panel BuildFinanceChartCard(DateTimePicker dtpFrom, DateTimePicker dtpTo, ComboBox cboType)
        {
            var amountByType = new Dictionary<string, double>();
            try
            {
                string type = cboType.SelectedIndex == 0 ? null : cboType.SelectedItem?.ToString();
                var vm = _ctrl.GetFinanceReportVM(dtpFrom.Value, dtpTo.Value, type);
                if (vm.FinanceRows != null)
                    foreach (var r in vm.FinanceRows)
                    {
                        string key = r.InvoiceType ?? "Unknown";
                        if (!amountByType.ContainsKey(key)) amountByType[key] = 0;
                        amountByType[key] += r.Amount;
                    }
            }
            catch { }
            return BuildChartCard("Invoice Amount by Type",
                new List<string>(amountByType.Keys).ToArray(),
                new List<double>(amountByType.Values).ToArray(),
                ChartStyle.Pie);
        }

        // ════════════════════════════════════════════════════════════════
        //  DGV CELL FORMATTING (status badges)
        // ════════════════════════════════════════════════════════════════

        private void DgvCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.Value == null) return;
            string val = e.Value.ToString();
            if (StatusColors.TryGetValue(val, out var colors))
            {
                e.CellStyle.BackColor          = colors.bg;
                e.CellStyle.ForeColor          = colors.fg;
                e.CellStyle.SelectionBackColor = colors.bg;
                e.CellStyle.SelectionForeColor = colors.fg;
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  EXPORT
        // ════════════════════════════════════════════════════════════════

        private void ExportGrid(DataGridView dgv, string baseName)
        {
            try
            {
                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"{baseName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

                using var sw = new StreamWriter(path, false, System.Text.Encoding.UTF8);

                // Header
                var headers = new List<string>();
                foreach (DataGridViewColumn col in dgv.Columns)
                    headers.Add("\"" + col.HeaderText + "\"");
                sw.WriteLine(string.Join(",", headers));

                // Rows
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    if (row.IsNewRow) continue;
                    var cells = new List<string>();
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        string cellVal = cell.Value != null
                            ? cell.Value.ToString().Replace("\"", "\"\"")
                            : string.Empty;
                        cells.Add("\"" + cellVal + "\"");
                    }
                    sw.WriteLine(string.Join(",", cells));
                }

                MessageBox.Show(
                    "Exported to:\n" + path,
                    "Export Successful",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Export failed:\n" + ex.Message,
                    "Export Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  PAINT — card border
        // ════════════════════════════════════════════════════════════════

        private void PaintCardBorder(object sender, PaintEventArgs e)
        {
            var ctrl = (Control)sender;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, ctrl.Width - 1, ctrl.Height - 1);
        }

        // ════════════════════════════════════════════════════════════════
        //  TOP NAV BAR — navigation + logout
        // ════════════════════════════════════════════════════════════════

        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
            => FormNavigator.Logout(this);
    }
}
