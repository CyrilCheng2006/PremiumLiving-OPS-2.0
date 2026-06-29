using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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
                { "Received",            (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
                { "Escalated",           (Color.FromArgb(254, 226, 226), Color.FromArgb(185,  28,  28)) },
                { "Approved",            (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
                { "Rejected",            (Color.FromArgb(254, 226, 226), Color.FromArgb(185,  28,  28)) },
                { "Revenue",             (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
                { "Expense",             (Color.FromArgb(254, 226, 226), Color.FromArgb(185,  28,  28)) },
                { "Refund",              (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
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
        //  LOAD  — mirrors ViewOrderForm_Load pattern exactly
        // ════════════════════════════════════════════════════════════════

        private void ViewReportForm_Load(object sender, EventArgs e)
        {
            // Wire navigation/logout here (not in Designer) — same as ViewOrderForm
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;

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
        //  CONTENT SWAP  — the actual fix: replaces pnlContent child
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Swaps pnlContent between <paramref name="dgvCard"/> and
        /// <paramref name="chartCard"/> based on <paramref name="showChart"/>.
        /// Both cards must already be built; only one is parented at a time.
        /// </summary>
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

            var cellFrom = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(0, 0, 8, 0)
            };
            cellFrom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            cellFrom.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            cellFrom.RowStyles.Add(new RowStyle(SizeType.Percent,  70f));
            cellFrom.Controls.Add(new Label { Text = "Date Range", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(98, 112, 135), Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft, Padding = new Padding(0, 0, 0, 2) }, 0, 0);
            dtpFrom.Dock = DockStyle.Fill;
            cellFrom.Controls.Add(dtpFrom, 0, 1);
            tbl.Controls.Add(cellFrom, 0, 0);

            var cellSep = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = Padding.Empty
            };
            cellSep.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            cellSep.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            cellSep.RowStyles.Add(new RowStyle(SizeType.Percent,  70f));
            cellSep.Controls.Add(new Label { Text = "", Dock = DockStyle.Fill }, 0, 0);
            cellSep.Controls.Add(new Label { Text = "to", Font = new Font("Segoe UI", 11f), ForeColor = Color.FromArgb(98, 112, 135), BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, AutoSize = false }, 0, 1);
            tbl.Controls.Add(cellSep, 1, 0);

            var cellTo = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(8, 0, 0, 0)
            };
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

        /// <summary>
        /// Builds a white card containing a pure GDI+ chart panel.
        /// No DataVisualization / NuGet package required.
        /// </summary>
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
        //  GDI+ CHART PANEL  (inner private class — no extra file needed)
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// A lightweight Panel that paints Bar, Column or Pie charts
        /// using only System.Drawing — no DataVisualization dependency.
        /// </summary>
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
                if (n == 0)
                {
                    DrawEmpty(g);
                    return;
                }

                // Title
                var titleFont = new Font("Segoe UI", 13f, FontStyle.Bold);
                var titleBrush = new SolidBrush(Color.FromArgb(15, 31, 53));
                g.DrawString(_title, titleFont, titleBrush, new PointF(20, 14));
                float titleH = titleFont.GetHeight(g) + 20;

                var plotRect = new RectangleF(
                    60, titleH,
                    Width  - 80,
                    Height - titleH - 60);

                switch (_style)
                {
                    case ChartStyle.Pie:
                        DrawPie(g, plotRect, n);
                        break;
                    case ChartStyle.Column:
                        DrawBars(g, plotRect, n, vertical: true);
                        break;
                    default: // Bar
                        DrawBars(g, plotRect, n, vertical: false);
                        break;
                }

                titleFont.Dispose();
                titleBrush.Dispose();
            }

            private void DrawEmpty(Graphics g)
            {
                var f = new Font("Segoe UI", 11f);
                var b = new SolidBrush(Color.FromArgb(150, 150, 150));
                string msg = "No data available";
                var sz  = g.MeasureString(msg, f);
                g.DrawString(msg, f, b, (Width - sz.Width) / 2f, (Height - sz.Height) / 2f);
                f.Dispose(); b.Dispose();
            }

            private void DrawBars(Graphics g, RectangleF plotRect, int n, bool vertical)
            {
                double max = 0;
                foreach (var v in _values) if (v > max) max = v;
                if (max == 0) max = 1;

                var axisColor = Color.FromArgb(221, 227, 236);
                var gridPen   = new Pen(axisColor, 1f);
                var axisFont  = new Font("Segoe UI", 9f);
                var axisBrush = new SolidBrush(Color.FromArgb(98, 112, 135));
                var valFont   = new Font("Segoe UI", 8f, FontStyle.Bold);

                const int GridLines = 4;
                if (vertical)
                {
                    // Column chart — bars go upward
                    float barAreaW = plotRect.Width  - 40;
                    float barAreaH = plotRect.Height - 20;
                    float barX0    = plotRect.Left   + 40;
                    float barY0    = plotRect.Top;

                    // Grid lines (horizontal)
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
                    float barWidth = (barAreaW - gap * (n + 1)) / n;
                    barWidth = Math.Max(4, barWidth);

                    for (int i = 0; i < n; i++)
                    {
                        float barH  = (float)(barAreaH * (_values[i] / max));
                        float bx    = barX0 + gap * (i + 1) + barWidth * i;
                        float by    = barY0 + barAreaH - barH;
                        var   color = _palette[i % _palette.Length];

                        using var brush = new SolidBrush(color);
                        g.FillRectangle(brush, bx, by, barWidth, barH);

                        // X label
                        string xl   = _labels[i];
                        var    xlSz = g.MeasureString(xl, axisFont);
                        float  xlX  = bx + (barWidth - xlSz.Width) / 2f;
                        float  xlY  = barY0 + barAreaH + 4;
                        g.DrawString(xl, axisFont, axisBrush, xlX, xlY);

                        // Value label on bar
                        string vl   = _values[i] >= 1000 ? $"{_values[i] / 1000:N1}k" : $"{_values[i]:N0}";
                        var    vlSz = g.MeasureString(vl, valFont);
                        if (barH > vlSz.Height + 4)
                        {
                            using var wBrush = new SolidBrush(Color.White);
                            g.DrawString(vl, valFont, wBrush, bx + (barWidth - vlSz.Width) / 2f, by + 4);
                        }
                    }
                }
                else
                {
                    // Bar chart — bars go rightward
                    float barAreaW = plotRect.Width  - 50;
                    float barAreaH = plotRect.Height - 10;
                    float barX0    = plotRect.Left   + 50;
                    float barY0    = plotRect.Top;

                    // Grid lines (vertical)
                    for (int i = 0; i <= GridLines; i++)
                    {
                        float x = barX0 + barAreaW * i / GridLines;
                        g.DrawLine(gridPen, x, barY0, x, barY0 + barAreaH);
                        double val = max * i / GridLines;
                        string lbl = val >= 1000 ? $"{val / 1000:N1}k" : $"{val:N0}";
                        var sz = g.MeasureString(lbl, axisFont);
                        g.DrawString(lbl, axisFont, axisBrush, x - sz.Width / 2f, barY0 + barAreaH + 2);
                    }

                    float gap      = barAreaH / (n * 1.4f + 0.4f) * 0.4f;
                    float barHeight = (barAreaH - gap * (n + 1)) / n;
                    barHeight = Math.Max(4, barHeight);

                    for (int i = 0; i < n; i++)
                    {
                        float barW = (float)(barAreaW * (_values[i] / max));
                        float bx   = barX0;
                        float by   = barY0 + gap * (i + 1) + barHeight * i;
                        var   color = _palette[i % _palette.Length];

                        using var brush = new SolidBrush(color);
                        g.FillRectangle(brush, bx, by, barW, barHeight);

                        // Y label (left side)
                        string yl   = _labels[i];
                        var    ylSz = g.MeasureString(yl, axisFont);
                        g.DrawString(yl, axisFont, axisBrush,
                            barX0 - ylSz.Width - 4,
                            by + (barHeight - ylSz.Height) / 2f);

                        // Value label
                        string vl   = _values[i] >= 1000 ? $"{_values[i] / 1000:N1}k" : $"{_values[i]:N0}";
                        var    vlSz = g.MeasureString(vl, valFont);
                        if (barW > vlSz.Width + 8)
                        {
                            using var wBrush = new SolidBrush(Color.White);
                            g.DrawString(vl, valFont, wBrush,
                                bx + barW - vlSz.Width - 6,
                                by + (barHeight - vlSz.Height) / 2f);
                        }
                    }
                }

                gridPen.Dispose();
                axisFont.Dispose();
                axisBrush.Dispose();
                valFont.Dispose();
            }

            private void DrawPie(Graphics g, RectangleF plotRect, int n)
            {
                double total = 0;
                foreach (var v in _values) total += v;
                if (total == 0) { DrawEmpty(g); return; }

                float legW   = 160;
                float pieW   = Math.Min(plotRect.Width - legW - 20, plotRect.Height - 20);
                float pieH   = pieW;
                float pieX   = plotRect.Left + (plotRect.Width - legW - 20 - pieW) / 2f;
                float pieY   = plotRect.Top  + (plotRect.Height - pieH) / 2f;
                var   pieRect = new RectangleF(pieX, pieY, pieW, pieH);

                var   labelFont  = new Font("Segoe UI", 9f);
                var   labelBrush = new SolidBrush(Color.FromArgb(15, 31, 53));

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

                // Legend
                float legX = pieX + pieW + 20;
                float legY = plotRect.Top + 10;
                for (int i = 0; i < n; i++)
                {
                    float pct = (float)(_values[i] / total * 100.0);
                    using var dotBrush = new SolidBrush(_palette[i % _palette.Length]);
                    g.FillEllipse(dotBrush, legX, legY + 3, 12, 12);
                    string txt = $"{_labels[i]}  {pct:N1}%";
                    g.DrawString(txt, labelFont, labelBrush, legX + 18, legY);
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
            return new DataGridView
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
        //  REPORT RENDERERS  — each builds BOTH cards up front, then
        //  btnToggle.Click calls SwapContent to actually switch them.
        // ════════════════════════════════════════════════════════════════

        // ── 0. Sales Performance ────────────────────────────────────────
        private void RenderSales()
        {
            var dtpFrom     = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-3), Font = new Font("Segoe UI", 12f) };
            var dtpTo       = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today, Font = new Font("Segoe UI", 12f) };
            var cboCategory = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            cboCategory.Items.AddRange(new object[] { "All Categories", "Furniture", "Lighting", "Textiles", "Accessories", "Outdoor" });
            cboCategory.SelectedIndex = 0;

            var btnApply  = MakePrimaryBtn("\U0001F50D  Apply");
            var btnReset  = MakeOutlineBtn("\u21BA  Reset");
            var btnToggle = MakeAmberBtn("\U0001F4CA  Chart");
            var btnExport = MakeExportBtn("\U0001F4E4  Export");
            ApplyToggleStyle(btnToggle, _salesChart);

            var dgv = MakeDgv();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",    HeaderText = "DATE",         FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrder",   HeaderText = "ORDER ID",     FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCust",    HeaderText = "CUSTOMER",     FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLines",   HeaderText = "LINES",        FillWeight =  8 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRevenue", HeaderText = "REVENUE",      FillWeight = 13 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",  HeaderText = "ORDER STATUS", FillWeight = 14 });
            dgv.CellFormatting += DgvCellFormatting;

            LoadSalesData(dgv, dtpFrom, dtpTo);

            var dgvCard   = BuildGridCard(dgv);
            var chartCard = BuildSalesChartCard(dtpFrom, dtpTo);

            btnApply.Click += (s, e) =>
            {
                LoadSalesData(dgv, dtpFrom, dtpTo);
                if (_salesChart)
                {
                    pnlContent.SuspendLayout();
                    pnlContent.Controls.Clear();
                    pnlContent.Controls.Add(BuildSalesChartCard(dtpFrom, dtpTo));
                    pnlContent.ResumeLayout(true);
                }
                else
                {
                    SwapContent(dgvCard, chartCard, false);
                }
            };
            btnReset.Click += (s, e) =>
            {
                dtpFrom.Value = DateTime.Today.AddMonths(-3);
                dtpTo.Value   = DateTime.Today;
                cboCategory.SelectedIndex = 0;
                LoadSalesData(dgv, dtpFrom, dtpTo);
                SwapContent(dgvCard, chartCard, _salesChart);
            };
            btnToggle.Click += (s, e) =>
            {
                _salesChart = !_salesChart;
                ApplyToggleStyle(btnToggle, _salesChart);
                if (_salesChart)
                {
                    pnlContent.SuspendLayout();
                    pnlContent.Controls.Clear();
                    pnlContent.Controls.Add(BuildSalesChartCard(dtpFrom, dtpTo));
                    pnlContent.ResumeLayout(true);
                }
                else
                {
                    SwapContent(dgvCard, chartCard, false);
                }
            };
            btnExport.Click += (s, e) => ExportGrid(dgv, "SalesPerformance");

            SetFilterBar("Sales Performance",
                BuildDateRangeRow(dtpFrom, dtpTo, ("Category", cboCategory)),
                BuildButtonsRow(btnApply, btnReset, btnToggle, btnExport));

            SwapContent(dgvCard, chartCard, _salesChart);
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
                        revenueByStatus[key] += (double)r.GrandTotal;
                    }
            }
            catch { }

            return BuildChartCard("Sales Revenue by Status",
                new List<string>(revenueByStatus.Keys).ToArray(),
                new List<double>(revenueByStatus.Values).ToArray(),
                ChartStyle.Bar);
        }

        // ── 1. Inventory Status ─────────────────────────────────────────
        private void RenderInventory()
        {
            var txtKeyword      = new TextBox { Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "Item ID / Item Name" };
            var cboCategory     = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            cboCategory.Items.AddRange(new object[] { "All Categories", "Product", "Raw Material" });
            cboCategory.SelectedIndex = 0;
            var chkBelowReorder = new CheckBox { Text = "Below Reorder Only", Font = new Font("Segoe UI", 12f), BackColor = Color.Transparent };

            var btnApply  = MakePrimaryBtn("\U0001F50D  Apply");
            var btnReset  = MakeOutlineBtn("\u21BA  Reset");
            var btnToggle = MakeAmberBtn("\U0001F4CA  Chart");
            var btnExport = MakeExportBtn("\U0001F4E4  Export");
            ApplyToggleStyle(btnToggle, _inventoryChart);

            var dgv = MakeDgv();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colWHItemID", HeaderText = "WH ITEM ID",    FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItemID",   HeaderText = "ITEM ID",       FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName",     HeaderText = "ITEM NAME",     FillWeight = 25 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCategory", HeaderText = "CATEGORY",      FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colWH",       HeaderText = "WAREHOUSE",     FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStock",    HeaderText = "CURRENT STOCK", FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colReorder",  HeaderText = "REORDER LEVEL", FillWeight = 12 });
            dgv.CellFormatting += DgvCellFormatting;

            LoadInventoryData(dgv, cboCategory, chkBelowReorder);

            var dgvCard   = BuildGridCard(dgv);
            var chartCard = BuildInventoryChartCard(cboCategory, chkBelowReorder);

            btnApply.Click += (s, e) =>
            {
                LoadInventoryData(dgv, cboCategory, chkBelowReorder);
                if (_inventoryChart) { pnlContent.SuspendLayout(); pnlContent.Controls.Clear(); pnlContent.Controls.Add(BuildInventoryChartCard(cboCategory, chkBelowReorder)); pnlContent.ResumeLayout(true); }
                else SwapContent(dgvCard, chartCard, false);
            };
            btnReset.Click += (s, e) => { txtKeyword.Clear(); cboCategory.SelectedIndex = 0; chkBelowReorder.Checked = false; LoadInventoryData(dgv, cboCategory, chkBelowReorder); SwapContent(dgvCard, chartCard, _inventoryChart); };
            btnToggle.Click += (s, e) =>
            {
                _inventoryChart = !_inventoryChart;
                ApplyToggleStyle(btnToggle, _inventoryChart);
                if (_inventoryChart) { pnlContent.SuspendLayout(); pnlContent.Controls.Clear(); pnlContent.Controls.Add(BuildInventoryChartCard(cboCategory, chkBelowReorder)); pnlContent.ResumeLayout(true); }
                else SwapContent(dgvCard, chartCard, false);
            };
            btnExport.Click += (s, e) => ExportGrid(dgv, "InventoryStatus");

            SetFilterBar("Inventory Status",
                BuildFieldsRow(("Keyword", txtKeyword), ("Category", cboCategory), ("Filter", chkBelowReorder)),
                BuildButtonsRow(btnApply, btnReset, btnToggle, btnExport));

            SwapContent(dgvCard, chartCard, _inventoryChart);
        }

        private Panel BuildInventoryChartCard(ComboBox cboCat, CheckBox chkBelow)
        {
            var stockByCategory = new Dictionary<string, double>();
            try
            {
                string cat = cboCat.SelectedIndex == 0 ? null : cboCat.SelectedItem?.ToString();
                var vm = _ctrl.GetInventoryReportVM(cat, chkBelow.Checked);
                if (vm.InventoryRows != null)
                    foreach (var r in vm.InventoryRows)
                    {
                        string key = r.ItemCategory ?? "Unknown";
                        if (!stockByCategory.ContainsKey(key)) stockByCategory[key] = 0;
                        stockByCategory[key] += r.CurrentStock;
                    }
            }
            catch { }

            return BuildChartCard("Current Stock by Category",
                new List<string>(stockByCategory.Keys).ToArray(),
                new List<double>(stockByCategory.Values).ToArray(),
                ChartStyle.Column);
        }

        // ── 2. Procurement Summary ───────────────────────────────────────
        private void RenderProcurement()
        {
            var dtpFrom   = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-3), Font = new Font("Segoe UI", 12f) };
            var dtpTo     = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today, Font = new Font("Segoe UI", 12f) };
            var cboStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            cboStatus.Items.AddRange(new object[] { "All", "Pending", "Processing", "Received", "Cancelled" });
            cboStatus.SelectedIndex = 0;

            var btnApply  = MakePrimaryBtn("\U0001F50D  Apply");
            var btnReset  = MakeOutlineBtn("\u21BA  Reset");
            var btnToggle = MakeAmberBtn("\U0001F4CA  Chart");
            var btnExport = MakeExportBtn("\U0001F4E4  Export");
            ApplyToggleStyle(btnToggle, _procurementChart);

            var dgv = MakeDgv();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPOID",     HeaderText = "PO ID",        FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSupplier", HeaderText = "SUPPLIER",     FillWeight = 22 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",     HeaderText = "ORDER DATE",   FillWeight = 13 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItems",    HeaderText = "ITEMS",        FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAmt",      HeaderText = "TOTAL AMOUNT", FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",   HeaderText = "PO STATUS",    FillWeight = 15 });
            dgv.CellFormatting += DgvCellFormatting;

            LoadProcurementData(dgv, cboStatus);

            var dgvCard   = BuildGridCard(dgv);
            var chartCard = BuildProcurementChartCard(cboStatus);

            btnApply.Click += (s, e) =>
            {
                LoadProcurementData(dgv, cboStatus);
                if (_procurementChart) { pnlContent.SuspendLayout(); pnlContent.Controls.Clear(); pnlContent.Controls.Add(BuildProcurementChartCard(cboStatus)); pnlContent.ResumeLayout(true); }
                else SwapContent(dgvCard, chartCard, false);
            };
            btnReset.Click += (s, e) => { dtpFrom.Value = DateTime.Today.AddMonths(-3); dtpTo.Value = DateTime.Today; cboStatus.SelectedIndex = 0; LoadProcurementData(dgv, cboStatus); SwapContent(dgvCard, chartCard, _procurementChart); };
            btnToggle.Click += (s, e) =>
            {
                _procurementChart = !_procurementChart;
                ApplyToggleStyle(btnToggle, _procurementChart);
                if (_procurementChart) { pnlContent.SuspendLayout(); pnlContent.Controls.Clear(); pnlContent.Controls.Add(BuildProcurementChartCard(cboStatus)); pnlContent.ResumeLayout(true); }
                else SwapContent(dgvCard, chartCard, false);
            };
            btnExport.Click += (s, e) => ExportGrid(dgv, "ProcurementSummary");

            SetFilterBar("Procurement Summary",
                BuildDateRangeRow(dtpFrom, dtpTo, ("PO Status", cboStatus)),
                BuildButtonsRow(btnApply, btnReset, btnToggle, btnExport));

            SwapContent(dgvCard, chartCard, _procurementChart);
        }

        private Panel BuildProcurementChartCard(ComboBox cboStatus)
        {
            var amtByStatus = new Dictionary<string, double>();
            try
            {
                string st = cboStatus.SelectedIndex == 0 ? null : cboStatus.SelectedItem?.ToString();
                var vm = _ctrl.GetProcurementReportVM(st);
                if (vm.ProcRows != null)
                    foreach (var r in vm.ProcRows)
                    {
                        string key = r.PurchaseStatus ?? "Unknown";
                        if (!amtByStatus.ContainsKey(key)) amtByStatus[key] = 0;
                        amtByStatus[key] += (double)r.POTotalAmount;
                    }
            }
            catch { }

            return BuildChartCard("Procurement Amount by Status",
                new List<string>(amtByStatus.Keys).ToArray(),
                new List<double>(amtByStatus.Values).ToArray(),
                ChartStyle.Pie);
        }

        // ── 3. Logistics Overview ────────────────────────────────────────
        private void RenderLogistics()
        {
            var dtpFrom   = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-3), Font = new Font("Segoe UI", 12f) };
            var dtpTo     = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today, Font = new Font("Segoe UI", 12f) };
            var cboStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            cboStatus.Items.AddRange(new object[] { "All", "Pending", "In Transit", "Delivered", "Cancelled" });
            cboStatus.SelectedIndex = 0;

            var btnApply  = MakePrimaryBtn("\U0001F50D  Apply");
            var btnReset  = MakeOutlineBtn("\u21BA  Reset");
            var btnToggle = MakeAmberBtn("\U0001F4CA  Chart");
            var btnExport = MakeExportBtn("\U0001F4E4  Export");
            ApplyToggleStyle(btnToggle, _logisticsChart);

            var dgv = MakeDgv();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colShipID",  HeaderText = "SHIPMENT ID", FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrderID", HeaderText = "ORDER ID",    FillWeight = 13 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCust",    HeaderText = "CUSTOMER",    FillWeight = 18 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colType",    HeaderText = "SHIP TYPE",   FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMethod",  HeaderText = "METHOD",      FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",    HeaderText = "SHIP DATE",   FillWeight = 13 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAmt",     HeaderText = "AMOUNT",      FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",  HeaderText = "STATUS",      FillWeight = 13 });
            dgv.CellFormatting += DgvCellFormatting;

            LoadLogisticsData(dgv, cboStatus);

            var dgvCard   = BuildGridCard(dgv);
            var chartCard = BuildLogisticsChartCard(cboStatus);

            btnApply.Click += (s, e) =>
            {
                LoadLogisticsData(dgv, cboStatus);
                if (_logisticsChart) { pnlContent.SuspendLayout(); pnlContent.Controls.Clear(); pnlContent.Controls.Add(BuildLogisticsChartCard(cboStatus)); pnlContent.ResumeLayout(true); }
                else SwapContent(dgvCard, chartCard, false);
            };
            btnReset.Click += (s, e) => { dtpFrom.Value = DateTime.Today.AddMonths(-3); dtpTo.Value = DateTime.Today; cboStatus.SelectedIndex = 0; LoadLogisticsData(dgv, cboStatus); SwapContent(dgvCard, chartCard, _logisticsChart); };
            btnToggle.Click += (s, e) =>
            {
                _logisticsChart = !_logisticsChart;
                ApplyToggleStyle(btnToggle, _logisticsChart);
                if (_logisticsChart) { pnlContent.SuspendLayout(); pnlContent.Controls.Clear(); pnlContent.Controls.Add(BuildLogisticsChartCard(cboStatus)); pnlContent.ResumeLayout(true); }
                else SwapContent(dgvCard, chartCard, false);
            };
            btnExport.Click += (s, e) => ExportGrid(dgv, "LogisticsOverview");

            SetFilterBar("Logistics Overview",
                BuildDateRangeRow(dtpFrom, dtpTo, ("Shipment Status", cboStatus)),
                BuildButtonsRow(btnApply, btnReset, btnToggle, btnExport));

            SwapContent(dgvCard, chartCard, _logisticsChart);
        }

        private Panel BuildLogisticsChartCard(ComboBox cboStatus)
        {
            var countByStatus = new Dictionary<string, double>();
            try
            {
                string st = cboStatus.SelectedIndex == 0 ? null : cboStatus.SelectedItem?.ToString();
                var vm = _ctrl.GetLogisticsReportVM(st);
                if (vm.LogRows != null)
                    foreach (var r in vm.LogRows)
                    {
                        string key = r.ShipmentStatus ?? "Unknown";
                        if (!countByStatus.ContainsKey(key)) countByStatus[key] = 0;
                        countByStatus[key]++;
                    }
            }
            catch { }

            return BuildChartCard("Shipments by Status",
                new List<string>(countByStatus.Keys).ToArray(),
                new List<double>(countByStatus.Values).ToArray(),
                ChartStyle.Pie);
        }

        // ── 4. After-Service Summary ─────────────────────────────────────
        private void RenderAfterService()
        {
            var dtpFrom       = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-3), Font = new Font("Segoe UI", 12f) };
            var dtpTo         = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today, Font = new Font("Segoe UI", 12f) };
            var cboCmplStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            cboCmplStatus.Items.AddRange(new object[] { "All", "Pending", "Processing", "Completed", "Escalated", "Cancelled" });
            cboCmplStatus.SelectedIndex = 0;

            var btnApply  = MakePrimaryBtn("\U0001F50D  Apply");
            var btnReset  = MakeOutlineBtn("\u21BA  Reset");
            var btnToggle = MakeAmberBtn("\U0001F4CA  Chart");
            var btnExport = MakeExportBtn("\U0001F4E4  Export");
            ApplyToggleStyle(btnToggle, _afterServiceChart);

            var dgv = MakeDgv();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCmplID",  HeaderText = "COMPLAINT ID", FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrderID", HeaderText = "ORDER ID",     FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCust",    HeaderText = "CUSTOMER",     FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDesc",    HeaderText = "DESCRIPTION",  FillWeight = 25 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",  HeaderText = "STATUS",       FillWeight = 15 });
            dgv.CellFormatting += DgvCellFormatting;

            LoadAfterServiceData(dgv, cboCmplStatus);

            var dgvCard   = BuildGridCard(dgv);
            var chartCard = BuildAfterServiceChartCard(cboCmplStatus);

            btnApply.Click += (s, e) =>
            {
                LoadAfterServiceData(dgv, cboCmplStatus);
                if (_afterServiceChart) { pnlContent.SuspendLayout(); pnlContent.Controls.Clear(); pnlContent.Controls.Add(BuildAfterServiceChartCard(cboCmplStatus)); pnlContent.ResumeLayout(true); }
                else SwapContent(dgvCard, chartCard, false);
            };
            btnReset.Click += (s, e) => { dtpFrom.Value = DateTime.Today.AddMonths(-3); dtpTo.Value = DateTime.Today; cboCmplStatus.SelectedIndex = 0; LoadAfterServiceData(dgv, cboCmplStatus); SwapContent(dgvCard, chartCard, _afterServiceChart); };
            btnToggle.Click += (s, e) =>
            {
                _afterServiceChart = !_afterServiceChart;
                ApplyToggleStyle(btnToggle, _afterServiceChart);
                if (_afterServiceChart) { pnlContent.SuspendLayout(); pnlContent.Controls.Clear(); pnlContent.Controls.Add(BuildAfterServiceChartCard(cboCmplStatus)); pnlContent.ResumeLayout(true); }
                else SwapContent(dgvCard, chartCard, false);
            };
            btnExport.Click += (s, e) => ExportGrid(dgv, "AfterServiceSummary");

            SetFilterBar("After-Service Summary",
                BuildDateRangeRow(dtpFrom, dtpTo, ("Complaint Status", cboCmplStatus)),
                BuildButtonsRow(btnApply, btnReset, btnToggle, btnExport));

            SwapContent(dgvCard, chartCard, _afterServiceChart);
        }

        private Panel BuildAfterServiceChartCard(ComboBox cboCmplStatus)
        {
            var countByStatus = new Dictionary<string, double>();
            try
            {
                string st = cboCmplStatus.SelectedIndex == 0 ? null : cboCmplStatus.SelectedItem?.ToString();
                var vm = _ctrl.GetAfterServiceReportVM(st, null);
                if (vm.Complaints != null)
                    foreach (var r in vm.Complaints)
                    {
                        string key = r.ComplaintStatus ?? "Unknown";
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

        // ── 5. Finance Overview ──────────────────────────────────────────
        private void RenderFinance()
        {
            var dtpFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-3), Font = new Font("Segoe UI", 12f) };
            var dtpTo   = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today, Font = new Font("Segoe UI", 12f) };
            var cboType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            cboType.Items.AddRange(new object[] { "All", "Revenue", "Expense", "Refund" });
            cboType.SelectedIndex = 0;

            var btnApply  = MakePrimaryBtn("\U0001F50D  Apply");
            var btnReset  = MakeOutlineBtn("\u21BA  Reset");
            var btnToggle = MakeAmberBtn("\U0001F4CA  Chart");
            var btnExport = MakeExportBtn("\U0001F4E4  Export");
            ApplyToggleStyle(btnToggle, _financeChart);

            var dgv = MakeDgv();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTxnID",   HeaderText = "TXN ID",   FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",    HeaderText = "DATE",     FillWeight = 13 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colType",    HeaderText = "TYPE",     FillWeight = 13 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDoc",     HeaderText = "DOCUMENT", FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDocType", HeaderText = "DOC TYPE", FillWeight = 18 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAmt",     HeaderText = "AMOUNT",   FillWeight = 21 });
            dgv.CellFormatting += DgvCellFormatting;

            LoadFinanceData(dgv, dtpFrom, dtpTo);

            var dgvCard   = BuildGridCard(dgv);
            var chartCard = BuildFinanceChartCard(dtpFrom, dtpTo);

            btnApply.Click += (s, e) =>
            {
                LoadFinanceData(dgv, dtpFrom, dtpTo);
                if (_financeChart) { pnlContent.SuspendLayout(); pnlContent.Controls.Clear(); pnlContent.Controls.Add(BuildFinanceChartCard(dtpFrom, dtpTo)); pnlContent.ResumeLayout(true); }
                else SwapContent(dgvCard, chartCard, false);
            };
            btnReset.Click += (s, e) => { dtpFrom.Value = DateTime.Today.AddMonths(-3); dtpTo.Value = DateTime.Today; cboType.SelectedIndex = 0; LoadFinanceData(dgv, dtpFrom, dtpTo); SwapContent(dgvCard, chartCard, _financeChart); };
            btnToggle.Click += (s, e) =>
            {
                _financeChart = !_financeChart;
                ApplyToggleStyle(btnToggle, _financeChart);
                if (_financeChart) { pnlContent.SuspendLayout(); pnlContent.Controls.Clear(); pnlContent.Controls.Add(BuildFinanceChartCard(dtpFrom, dtpTo)); pnlContent.ResumeLayout(true); }
                else SwapContent(dgvCard, chartCard, false);
            };
            btnExport.Click += (s, e) => ExportGrid(dgv, "FinanceOverview");

            SetFilterBar("Finance Overview",
                BuildDateRangeRow(dtpFrom, dtpTo, ("Transaction Type", cboType)),
                BuildButtonsRow(btnApply, btnReset, btnToggle, btnExport));

            SwapContent(dgvCard, chartCard, _financeChart);
        }

        private Panel BuildFinanceChartCard(DateTimePicker dtpFrom, DateTimePicker dtpTo)
        {
            var amtByType = new Dictionary<string, double>();
            try
            {
                var vm = _ctrl.GetFinanceReportVM(dtpFrom.Value, dtpTo.Value);
                if (vm.FinanceRows != null)
                    foreach (var r in vm.FinanceRows)
                    {
                        string key = r.TransactionType ?? "Unknown";
                        if (!amtByType.ContainsKey(key)) amtByType[key] = 0;
                        amtByType[key] += (double)r.Amount;
                    }
            }
            catch { }

            return BuildChartCard("Finance by Transaction Type",
                new List<string>(amtByType.Keys).ToArray(),
                new List<double>(amtByType.Values).ToArray(),
                ChartStyle.Column);
        }

        // ════════════════════════════════════════════════════════════════
        //  DATA LOADERS
        // ════════════════════════════════════════════════════════════════

        private void LoadSalesData(DataGridView dgv, DateTimePicker from, DateTimePicker to)
        {
            dgv.Rows.Clear();
            try
            {
                var vm = _ctrl.GetSalesReportVM(from.Value, to.Value);
                if (vm.SalesRows == null) return;
                foreach (var r in vm.SalesRows)
                    dgv.Rows.Add(r.IssuedTime.ToShortDateString(), r.OrderID, r.CustomerName, r.LineCount, r.GrandTotal.ToString("C"), r.OrderStatus);
            }
            catch { }
        }

        private void LoadInventoryData(DataGridView dgv, ComboBox cboCat, CheckBox chkBelow)
        {
            dgv.Rows.Clear();
            try
            {
                string cat = cboCat.SelectedIndex == 0 ? null : cboCat.SelectedItem?.ToString();
                var vm = _ctrl.GetInventoryReportVM(cat, chkBelow.Checked);
                if (vm.InventoryRows == null) return;
                foreach (var r in vm.InventoryRows)
                    dgv.Rows.Add(r.WarehouseItemID, r.ItemID, r.ItemName, r.ItemCategory, r.WarehouseLocation, r.CurrentStock, r.ReorderLevel);
            }
            catch { }
        }

        private void LoadProcurementData(DataGridView dgv, ComboBox cboStatus)
        {
            dgv.Rows.Clear();
            try
            {
                string st = cboStatus.SelectedIndex == 0 ? null : cboStatus.SelectedItem?.ToString();
                var vm = _ctrl.GetProcurementReportVM(st);
                if (vm.ProcRows == null) return;
                foreach (var r in vm.ProcRows)
                    dgv.Rows.Add(r.PurchaseID, r.SupplierName, r.OrderDate.ToShortDateString(), r.MaterialNames, r.POTotalAmount.ToString("C"), r.PurchaseStatus);
            }
            catch { }
        }

        private void LoadLogisticsData(DataGridView dgv, ComboBox cboStatus)
        {
            dgv.Rows.Clear();
            try
            {
                string st = cboStatus.SelectedIndex == 0 ? null : cboStatus.SelectedItem?.ToString();
                var vm = _ctrl.GetLogisticsReportVM(st);
                if (vm.LogRows == null) return;
                foreach (var r in vm.LogRows)
                    dgv.Rows.Add(r.ShipmentID, r.OrderID, r.CustomerName, r.ShipmentType, r.DeliveryMethod, r.ShipDate.ToShortDateString(), r.TotalAmount.ToString("C"), r.ShipmentStatus);
            }
            catch { }
        }

        private void LoadAfterServiceData(DataGridView dgv, ComboBox cboCmplStatus)
        {
            dgv.Rows.Clear();
            try
            {
                string st = cboCmplStatus.SelectedIndex == 0 ? null : cboCmplStatus.SelectedItem?.ToString();
                var vm = _ctrl.GetAfterServiceReportVM(st, null);
                if (vm.Complaints == null) return;
                foreach (var r in vm.Complaints)
                    dgv.Rows.Add(r.ComplaintID, r.OrderID, r.CustomerName, r.ComplaintDescription, r.ComplaintStatus);
            }
            catch { }
        }

        private void LoadFinanceData(DataGridView dgv, DateTimePicker from, DateTimePicker to)
        {
            dgv.Rows.Clear();
            try
            {
                var vm = _ctrl.GetFinanceReportVM(from.Value, to.Value);
                if (vm.FinanceRows == null) return;
                foreach (var r in vm.FinanceRows)
                    dgv.Rows.Add(r.TransactionID, r.TransactionDate.ToShortDateString(), r.TransactionType, r.LinkedDocument, r.DocumentType, r.Amount.ToString("C"));
            }
            catch { }
        }

        // ════════════════════════════════════════════════════════════════
        //  CELL FORMATTING
        // ════════════════════════════════════════════════════════════════

        private void DgvCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.Value == null) return;
            string val = e.Value.ToString();
            if (StatusColors.TryGetValue(val, out var c))
            {
                e.CellStyle.BackColor = c.bg;
                e.CellStyle.ForeColor = c.fg;
                e.CellStyle.Font      = new Font("Segoe UI", 10f, FontStyle.Bold);
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  EXPORT HELPER
        // ════════════════════════════════════════════════════════════════

        private static void ExportGrid(DataGridView dgv, string reportName)
        {
            using var dlg = new SaveFileDialog
            {
                Title            = "Export Report",
                Filter           = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                FileName         = $"{reportName}_{DateTime.Today:yyyyMMdd}.csv",
                DefaultExt       = "csv",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            try
            {
                var sb = new System.Text.StringBuilder();
                var headers = new List<string>();
                foreach (DataGridViewColumn col in dgv.Columns) headers.Add(col.HeaderText);
                sb.AppendLine(string.Join(",", headers));
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    var cells = new List<string>();
                    foreach (DataGridViewCell cell in row.Cells)
                        cells.Add('"' + (cell.Value?.ToString() ?? "").Replace('"', '\u2019') + '"');
                    sb.AppendLine(string.Join(",", cells));
                }
                System.IO.File.WriteAllText(dlg.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                MessageBox.Show($"Exported to:\n{dlg.FileName}", "Export Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed:\n{ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  NAVIGATION / LOGOUT
        // ════════════════════════════════════════════════════════════════

        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }
    }
}
