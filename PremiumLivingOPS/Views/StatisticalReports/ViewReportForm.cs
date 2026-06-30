using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using PremiumLivingOPS.Controllers;
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
            pnlTitle.Controls.Add(new Label
            {
                Text      = $"As of {DateTime.Now:dd MMM yyyy}",
                Font      = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Right,
                AutoSize  = true,
                TextAlign = ContentAlignment.MiddleRight
            });

            var filterCard = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.White,
                Padding   = new Padding(18, 10, 18, 10)
            };
            filterCard.Paint += PaintCardBorder;
            filterCard.Controls.Add(fieldRow);

            var btnCard = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(0, 6, 0, 0)
            };
            btnCard.Controls.Add(btnRow);

            tbl.Controls.Add(pnlTitle,  0, 0);
            tbl.Controls.Add(filterCard, 0, 1);
            tbl.Controls.Add(btnCard,    0, 2);

            var outer = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Palette.BgPage,
                Padding   = new Padding(20, 8, 20, 0)
            };
            outer.Controls.Add(tbl);

            pnlFilterOuter.Controls.Clear();
            pnlFilterOuter.Controls.Add(outer);
        }

        // ────────────────────────────────────────────────────────────────
        //  Row builders used by SetFilterBar
        // ────────────────────────────────────────────────────────────────

        /// <summary>Builds a FlowLayoutPanel with date-range pickers plus optional extra control.</summary>
        private static Panel BuildDateRangeRow(
            DateTimePicker dtpFrom,
            DateTimePicker dtpTo,
            (string label, Control ctrl)? extra = null)
        {
            var flow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                AutoSize      = false,
                BackColor     = Color.Transparent
            };

            void AddPair(string lbl, Control ctl)
            {
                var lblCtrl = new Label
                {
                    Text      = lbl,
                    Font      = new Font("Segoe UI", 11f),
                    ForeColor = Color.FromArgb(55, 65, 81),
                    AutoSize  = true,
                    Margin    = new Padding(0, 10, 6, 0)
                };
                ctl.Margin = new Padding(0, 4, 24, 0);
                ctl.Width  = 140;
                flow.Controls.Add(lblCtrl);
                flow.Controls.Add(ctl);
            }

            AddPair("From:", dtpFrom);
            AddPair("To:",   dtpTo);

            if (extra.HasValue)
            {
                var (lbl, ctl) = extra.Value;
                ctl.Width  = 180;
                ctl.Margin = new Padding(0, 4, 24, 0);
                flow.Controls.Add(new Label
                {
                    Text      = lbl + ":",
                    Font      = new Font("Segoe UI", 11f),
                    ForeColor = Color.FromArgb(55, 65, 81),
                    AutoSize  = true,
                    Margin    = new Padding(0, 10, 6, 0)
                });
                flow.Controls.Add(ctl);
            }

            return flow;
        }

        private static Panel BuildSingleRow(params (string label, Control ctrl)[] pairs)
        {
            var flow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                AutoSize      = false,
                BackColor     = Color.Transparent
            };
            foreach (var (lbl, ctl) in pairs)
            {
                flow.Controls.Add(new Label
                {
                    Text      = lbl,
                    Font      = new Font("Segoe UI", 11f),
                    ForeColor = Color.FromArgb(55, 65, 81),
                    AutoSize  = true,
                    Margin    = new Padding(0, 10, 6, 0)
                });
                ctl.Margin = new Padding(0, 4, 24, 0);
                ctl.Width  = 200;
                flow.Controls.Add(ctl);
            }
            return flow;
        }

        private static Panel BuildButtonsRow(Button btnApply, Button btnReset, Button btnToggleView, Button btnExport)
        {
            var flow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent
            };
            foreach (var b in new[] { btnApply, btnReset, btnToggleView, btnExport })
            {
                b.Margin = new Padding(0, 0, 12, 0);
                flow.Controls.Add(b);
            }
            return flow;
        }

        // ════════════════════════════════════════════════════════════════
        //  BUTTON FACTORIES
        // ════════════════════════════════════════════════════════════════

        private static Button MakePrimaryBtn(string text)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Palette.Primary,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(130, 42),
                Cursor    = Cursors.Hand,
                Padding   = new Padding(8, 0, 8, 0)
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private static Button MakeOutlineBtn(string text)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(55, 65, 81),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(130, 42),
                Cursor    = Cursors.Hand,
                Padding   = new Padding(8, 0, 8, 0)
            };
            b.FlatAppearance.BorderSize  = 1;
            b.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);
            return b;
        }

        private static Button MakeAmberBtn(string text)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(217, 119, 6),
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(130, 42),
                Cursor    = Cursors.Hand,
                Padding   = new Padding(8, 0, 8, 0)
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private static void ApplyToggleStyle(Button btn, bool active)
        {
            btn.BackColor = active ? Color.FromArgb(6, 95, 70)   : Color.FromArgb(217, 119, 6);
            btn.Text      = active ? "\U0001F4CB  Table"          : "\U0001F4CA  Chart";
        }

        // ════════════════════════════════════════════════════════════════
        //  GDI+ CHART CARD
        // ════════════════════════════════════════════════════════════════

        // ── Inner chart renderer ──────────────────────────────────────
        private sealed class ChartCanvas : Panel
        {
            private readonly string[]   _labels;
            private readonly double[]   _values;
            private readonly ChartStyle _style;
            private const int   LegendH  = 24;
            private const int   Pad      = 40;
            private const float FontSize = 10f;

            public ChartCanvas(string[] labels, double[] values, ChartStyle style)
            {
                _labels  = labels;
                _values  = values;
                _style   = style;
                DoubleBuffered = true;
                ResizeRedraw   = true;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                g.SmoothingMode     = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                int n = _labels?.Length ?? 0;
                if (n == 0) { DrawEmpty(g); return; }

                var plotRect = new RectangleF(
                    Pad, Pad,
                    Width  - Pad * 2,
                    Height - Pad * 2 - LegendH * (float)Math.Ceiling(n / 4.0));

                if (plotRect.Width < 10 || plotRect.Height < 10) return;

                switch (_style)
                {
                    case ChartStyle.Bar:    DrawBars(g, plotRect, n, false); break;
                    case ChartStyle.Column: DrawBars(g, plotRect, n, true);  break;
                    case ChartStyle.Pie:    DrawPie (g, plotRect, n);        break;
                }

                DrawLegend(g, n);
            }

            private void DrawEmpty(Graphics g)
            {
                using var f = new Font("Segoe UI", FontSize);
                using var b = new SolidBrush(Color.FromArgb(156, 163, 175));
                var r = new RectangleF(0, 0, Width, Height);
                var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("No data available", f, b, r, fmt);
            }

            private void DrawBars(Graphics g, RectangleF plotRect, int n, bool vertical)
            {
                double max = 0;
                foreach (var v in _values) if (v > max) max = v;
                if (max == 0) max = 1;

                float gap    = vertical ? plotRect.Width  / n : plotRect.Height / n;
                float barSz  = gap * 0.6f;
                float offset = gap * 0.2f;

                using var labelFont = new Font("Segoe UI", FontSize - 1f);
                using var valFont   = new Font("Segoe UI", FontSize - 1f, FontStyle.Bold);
                var fmtC = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                var fmtN = new StringFormat { Alignment = StringAlignment.Near,   LineAlignment = StringAlignment.Center };
                var fmtF = new StringFormat { Alignment = StringAlignment.Far,    LineAlignment = StringAlignment.Center };

                // Grid lines (5 divisions)
                using var gridPen = new Pen(Color.FromArgb(229, 231, 235), 1f);
                for (int d = 0; d <= 5; d++)
                {
                    float t = d / 5f;
                    if (vertical)
                    {
                        float gy = plotRect.Bottom - plotRect.Height * t;
                        g.DrawLine(gridPen, plotRect.Left, gy, plotRect.Right, gy);
                        string sv = FormatVal(max * t);
                        using var lb = new SolidBrush(Color.FromArgb(156, 163, 175));
                        g.DrawString(sv, labelFont, lb,
                            new RectangleF(0, gy - 10, plotRect.Left - 4, 20), fmtF);
                    }
                    else
                    {
                        float gx = plotRect.Left + plotRect.Width * t;
                        g.DrawLine(gridPen, gx, plotRect.Top, gx, plotRect.Bottom);
                        string sv = FormatVal(max * t);
                        using var lb = new SolidBrush(Color.FromArgb(156, 163, 175));
                        g.DrawString(sv, labelFont, lb,
                            new RectangleF(gx - 30, plotRect.Bottom + 4, 60, 16), fmtC);
                    }
                }

                for (int i = 0; i < n; i++)
                {
                    double ratio = _values[i] / max;
                    Color  col   = ChartPalette[i % ChartPalette.Length];

                    RectangleF bar;
                    if (vertical)
                    {
                        float bx = plotRect.Left + i * gap + offset;
                        float bh = (float)(plotRect.Height * ratio);
                        bar = new RectangleF(bx, plotRect.Bottom - bh, barSz, bh);
                    }
                    else
                    {
                        float by = plotRect.Top + i * gap + offset;
                        float bw = (float)(plotRect.Width * ratio);
                        bar = new RectangleF(plotRect.Left, by, bw, barSz);
                    }

                    using var brush = new SolidBrush(col);
                    g.FillRectangle(brush, bar);

                    // value label
                    string val = FormatVal(_values[i]);
                    using var valBrush = new SolidBrush(Color.FromArgb(31, 41, 55));
                    if (vertical)
                        g.DrawString(val, valFont, valBrush,
                            new RectangleF(bar.X - 10, bar.Y - 18, bar.Width + 20, 18), fmtC);
                    else
                        g.DrawString(val, valFont, valBrush,
                            new RectangleF(bar.Right + 4, bar.Y, 60, bar.Height), fmtN);
                }
            }

            private void DrawPie(Graphics g, RectangleF plotRect, int n)
            {
                double total = 0;
                foreach (var v in _values) total += v;
                if (total == 0) { DrawEmpty(g); return; }

                float side   = Math.Min(plotRect.Width, plotRect.Height) * 0.85f;
                var   pieRect = new RectangleF(
                    plotRect.Left + (plotRect.Width  - side) / 2f,
                    plotRect.Top  + (plotRect.Height - side) / 2f,
                    side, side);

                float startAngle = -90f;
                using var labelFont = new Font("Segoe UI", FontSize - 1f, FontStyle.Bold);
                var fmtC = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                for (int i = 0; i < n; i++)
                {
                    float sweep = (float)(_values[i] / total * 360.0);
                    using var brush = new SolidBrush(ChartPalette[i % ChartPalette.Length]);
                    g.FillPie(brush, pieRect.X, pieRect.Y, pieRect.Width, pieRect.Height, startAngle, sweep);
                    using var borderPen = new Pen(Color.White, 2f);
                    g.DrawPie(borderPen, pieRect.X, pieRect.Y, pieRect.Width, pieRect.Height, startAngle, sweep);

                    if (sweep > 15)
                    {
                        double mid = (startAngle + sweep / 2.0) * Math.PI / 180.0;
                        float  r2  = side * 0.33f;
                        float  lx  = pieRect.X + side / 2f + (float)(r2 * Math.Cos(mid));
                        float  ly  = pieRect.Y + side / 2f + (float)(r2 * Math.Sin(mid));
                        string pct = $"{_values[i] / total:P0}";
                        using var lb = new SolidBrush(Color.White);
                        g.DrawString(pct, labelFont, lb, new RectangleF(lx - 25, ly - 10, 50, 20), fmtC);
                    }

                    startAngle += sweep;
                }
            }

            private void DrawLegend(Graphics g, int n)
            {
                int   cols   = 4;
                float cellW  = Width / (float)cols;
                float startY = Height - LegendH * (float)Math.Ceiling(n / (float)cols);
                using var f = new Font("Segoe UI", FontSize - 1f);

                for (int i = 0; i < n; i++)
                {
                    int   col = i % cols;
                    int   row = i / cols;
                    float x   = col * cellW + 8;
                    float y   = startY + row * LegendH + 4;

                    using var b = new SolidBrush(ChartPalette[i % ChartPalette.Length]);
                    g.FillRectangle(b, x, y + 5, 12, 12);

                    string lbl = _labels[i];
                    if (lbl.Length > 18) lbl = lbl.Substring(0, 16) + "..";
                    using var tb = new SolidBrush(Color.FromArgb(55, 65, 81));
                    g.DrawString(lbl, f, tb, x + 16, y);
                }
            }

            private static string FormatVal(double v)
                => v >= 1_000_000 ? $"{v / 1_000_000:F1}M"
                 : v >= 1_000     ? $"{v / 1_000:F1}K"
                 : v == Math.Floor(v) ? ((int)v).ToString()
                 : v.ToString("F1");
        }

        private Panel BuildChartCard(string title, string[] labels, double[] values, ChartStyle style)
        {
            var canvas = new ChartCanvas(labels, values, style)
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.White
            };

            var titleLbl = new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Top,
                Height    = 40,
                Padding   = new Padding(16, 10, 0, 0)
            };

            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            inner.Paint += PaintCardBorder;
            inner.Controls.Add(canvas);
            inner.Controls.Add(titleLbl);

            var outer = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Palette.BgPage,
                Padding   = new Padding(20, 14, 20, 14)
            };
            outer.Controls.Add(inner);
            return outer;
        }

        // ════════════════════════════════════════════════════════════════
        //  GRID CARD BUILDER
        // ════════════════════════════════════════════════════════════════

        private static Panel BuildGridCard(DataGridView dgv)
        {
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            inner.Paint += PaintCardBorder;
            inner.Controls.Add(dgv);

            var outer = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Palette.BgPage,
                Padding   = new Padding(20, 14, 20, 14)
            };
            outer.Controls.Add(inner);
            return outer;
        }

        private static Panel BuildGridCard2(DataGridView dgvTop, DataGridView dgvBottom)
        {
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            inner.Paint += PaintCardBorder;

            var split = new SplitContainer
            {
                Dock        = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                Panel1MinSize = 100,
                Panel2MinSize = 100,
                SplitterWidth = 6
            };
            split.Panel1.Controls.Add(dgvTop);
            split.Panel2.Controls.Add(dgvBottom);
            inner.Controls.Add(split);

            var outer = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Palette.BgPage,
                Padding   = new Padding(20, 14, 20, 14)
            };
            outer.Controls.Add(inner);
            return outer;
        }

        // ════════════════════════════════════════════════════════════════
        //  SHARED DGV + EXPORT BUTTON FACTORIES
        // ════════════════════════════════════════════════════════════════

        private static DataGridView MakeDgv()
        {
            var dgv = new DataGridView
            {
                Dock                  = DockStyle.Fill,
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight   = 40,
                RowTemplate           = { Height = 36 },
                BackgroundColor       = Color.White,
                GridColor             = Color.FromArgb(229, 231, 235),
                BorderStyle           = BorderStyle.None,
                CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                EnableHeadersVisualStyles = false,
                ReadOnly              = true,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                Font                  = new Font("Segoe UI", 11f),
                Margin                = new Padding(0)
            };
            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = Color.FromArgb(55, 65, 81),
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding   = new Padding(8, 0, 0, 0)
            };
            dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor          = Color.White,
                ForeColor          = Color.FromArgb(31, 41, 55),
                SelectionBackColor = Color.FromArgb(219, 234, 254),
                SelectionForeColor = Color.FromArgb(29, 78, 216),
                Padding            = new Padding(8, 0, 0, 0)
            };
            dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor          = Color.FromArgb(248, 250, 252),
                ForeColor          = Color.FromArgb(31, 41, 55),
                SelectionBackColor = Color.FromArgb(219, 234, 254),
                SelectionForeColor = Color.FromArgb(29, 78, 216),
                Padding            = new Padding(8, 0, 0, 0)
            };
            return dgv;
        }

        private static Button MakeExportBtn(string text)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(55, 65, 81),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(130, 42),
                Cursor    = Cursors.Hand,
                Padding   = new Padding(8, 0, 8, 0)
            };
            b.FlatAppearance.BorderSize  = 1;
            b.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);
            return b;
        }

        // ════════════════════════════════════════════════════════════════
        //  0. SALES PERFORMANCE
        // ════════════════════════════════════════════════════════════════

        private void RenderSales()
        {
            var dtpFrom   = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DefaultDateFrom, Font = new Font("Segoe UI", 12f) };
            var dtpTo     = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DefaultDateTo,   Font = new Font("Segoe UI", 12f) };
            var btnApply  = MakePrimaryBtn("\U0001F50D  Apply");
            var btnReset  = MakeOutlineBtn("\u21BA  Reset");
            var btnToggle = MakeAmberBtn("\U0001F4CA  Chart");
            var btnExport = MakeExportBtn("\U0001F4E4  Export");
            ApplyToggleStyle(btnToggle, _salesChart);

            var dgv = MakeDgv();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrderID",   HeaderText = "ORDER ID",    FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomer",  HeaderText = "CUSTOMER",    FillWeight = 24 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",    HeaderText = "STATUS",      FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",      HeaderText = "DATE",        FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal",     HeaderText = "GRAND TOTAL", FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLines",     HeaderText = "LINES",       FillWeight = 8  });
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
                dtpFrom.Value = DefaultDateFrom; dtpTo.Value = DefaultDateTo;
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
                    dgv.Rows.Add(r.OrderID, r.CustomerName, r.OrderStatus,
                                 r.IssuedTime.ToString("yyyy-MM-dd"),
                                 r.GrandTotal.ToString("N2"), r.LineCount);
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
            return BuildChartCard("Revenue by Order Status",
                new List<string>(revenueByStatus.Keys).ToArray(),
                new List<double>(revenueByStatus.Values).ToArray(),
                ChartStyle.Column);
        }

        // ════════════════════════════════════════════════════════════════
        //  1. INVENTORY STATUS
        // ════════════════════════════════════════════════════════════════

        private void RenderInventory()
        {
            var cboCat    = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            cboCat.Items.AddRange(new object[] { "All", "Product", "Raw Material" });
            cboCat.SelectedIndex = 0;

            var chkBelow  = new CheckBox { Text = "Below Reorder Only", Font = new Font("Segoe UI", 11f), AutoSize = true, Margin = new Padding(0, 8, 0, 0) };
            var txtKw     = new TextBox  { Font = new Font("Segoe UI", 12f), PlaceholderText = "Search keyword..." };

            var btnApply  = MakePrimaryBtn("\U0001F50D  Apply");
            var btnReset  = MakeOutlineBtn("\u21BA  Reset");
            var btnToggle = MakeAmberBtn("\U0001F4CA  Chart");
            var btnExport = MakeExportBtn("\U0001F4E4  Export");
            ApplyToggleStyle(btnToggle, _inventoryChart);

            var dgv = MakeDgv();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItemID",    HeaderText = "ITEM ID",       FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItemName",  HeaderText = "ITEM NAME",     FillWeight = 24 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCategory",  HeaderText = "CATEGORY",      FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colWarehouse", HeaderText = "WAREHOUSE",     FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStock",     HeaderText = "CURRENT STOCK", FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colReorder",   HeaderText = "REORDER LEVEL", FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",    HeaderText = "STATUS",        FillWeight = 12 });
            dgv.CellFormatting += DgvCellFormatting;

            LoadInventoryData(dgv, cboCat, chkBelow, txtKw);

            var dgvCard   = BuildGridCard(dgv);
            var chartCard = BuildInventoryChartCard(cboCat, chkBelow, txtKw);

            btnApply.Click += (s, e) =>
            {
                LoadInventoryData(dgv, cboCat, chkBelow, txtKw);
                if (_inventoryChart) { pnlContent.SuspendLayout(); pnlContent.Controls.Clear(); pnlContent.Controls.Add(BuildInventoryChartCard(cboCat, chkBelow, txtKw)); pnlContent.ResumeLayout(true); }
                else SwapContent(dgvCard, chartCard, false);
            };
            btnReset.Click += (s, e) =>
            {
                cboCat.SelectedIndex = 0; chkBelow.Checked = false; txtKw.Text = string.Empty;
                LoadInventoryData(dgv, cboCat, chkBelow, txtKw);
                SwapContent(dgvCard, chartCard, _inventoryChart);
            };
            btnToggle.Click += (s, e) =>
            {
                _inventoryChart = !_inventoryChart;
                ApplyToggleStyle(btnToggle, _inventoryChart);
                if (_inventoryChart) { pnlContent.SuspendLayout(); pnlContent.Controls.Clear(); pnlContent.Controls.Add(BuildInventoryChartCard(cboCat, chkBelow, txtKw)); pnlContent.ResumeLayout(true); }
                else SwapContent(dgvCard, chartCard, false);
            };
            btnExport.Click += (s, e) => ExportGrid(dgv, "InventoryStatus");

            SetFilterBar("Inventory Status",
                BuildSingleRow(("Category", cboCat), ("Keyword", txtKw)),
                BuildButtonsRow(btnApply, btnReset, btnToggle, btnExport));

            SwapContent(dgvCard, chartCard, _inventoryChart);
        }

        private void LoadInventoryData(DataGridView dgv, ComboBox cboCat, CheckBox chkBelow, TextBox txtKw)
        {
            dgv.Rows.Clear();
            try
            {
                string cat = cboCat.SelectedIndex == 0 ? null : cboCat.SelectedItem?.ToString();
                var vm = _ctrl.GetInventoryReportVM(cat, chkBelow.Checked, txtKw.Text.Trim());
                if (vm.InventoryRows == null) return;
                foreach (var r in vm.InventoryRows)
                    dgv.Rows.Add(r.ItemID, r.ItemName, r.ItemCategory, r.WarehouseLocation,
                                 r.CurrentStock, r.ReorderLevel,
                                 r.BelowReorder ? "Below Reorder" : "OK");
            }
            catch { }
        }

        private Panel BuildInventoryChartCard(ComboBox cboCat, CheckBox chkBelow, TextBox txtKw)
        {
            var countByCategory = new Dictionary<string, double>();
            try
            {
                string cat = cboCat.SelectedIndex == 0 ? null : cboCat.SelectedItem?.ToString();
                var vm = _ctrl.GetInventoryReportVM(cat, chkBelow.Checked, txtKw.Text.Trim());
                if (vm.InventoryRows != null)
                    foreach (var r in vm.InventoryRows)
                    {
                        string key = r.ItemCategory ?? "Unknown";
                        if (!countByCategory.ContainsKey(key)) countByCategory[key] = 0;
                        countByCategory[key]++;
                    }
            }
            catch { }
            return BuildChartCard("Items by Category",
                new List<string>(countByCategory.Keys).ToArray(),
                new List<double>(countByCategory.Values).ToArray(),
                ChartStyle.Pie);
        }

        // ════════════════════════════════════════════════════════════════
        //  2. PROCUREMENT SUMMARY
        // ════════════════════════════════════════════════════════════════

        private void RenderProcurement()
        {
            var dtpFrom   = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DefaultDateFrom, Font = new Font("Segoe UI", 12f) };
            var dtpTo     = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DefaultDateTo,   Font = new Font("Segoe UI", 12f) };
            var cboStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            cboStatus.Items.AddRange(new object[] { "All", "Pending", "Sent", "Partially Received", "Fully Received", "Cancelled" });
            cboStatus.SelectedIndex = 0;

            var btnApply  = MakePrimaryBtn("\U0001F50D  Apply");
            var btnReset  = MakeOutlineBtn("\u21BA  Reset");
            var btnToggle = MakeAmberBtn("\U0001F4CA  Chart");
            var btnExport = MakeExportBtn("\U0001F4E4  Export");
            ApplyToggleStyle(btnToggle, _procurementChart);

            var dgv = MakeDgv();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPOID",      HeaderText = "PO ID",         FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSupplier",  HeaderText = "SUPPLIER",      FillWeight = 22 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",    HeaderText = "STATUS",        FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colReceipt",   HeaderText = "RECEIPT",       FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",      HeaderText = "ORDER DATE",    FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAmount",    HeaderText = "TOTAL AMOUNT",  FillWeight = 14 });
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
                string st = cboStatus.SelectedIndex == 0 ? null : cboStatus.SelectedItem?.ToString();
                var vm = _ctrl.GetProcurementReportVM(dtpFrom.Value, dtpTo.Value, st);
                if (vm.ProcurementRows == null) return;
                foreach (var r in vm.ProcurementRows)
                    dgv.Rows.Add(r.PurchaseOrderID, r.SupplierName, r.PurchaseStatus, r.ReceiptStatus,
                                 r.OrderDate.ToString("yyyy-MM-dd"), r.TotalAmount.ToString("N2"));
            }
            catch { }
        }

        private Panel BuildProcurementChartCard(DateTimePicker dtpFrom, DateTimePicker dtpTo, ComboBox cboStatus)
        {
            var spendBySupplier = new Dictionary<string, double>();
            try
            {
                string st = cboStatus.SelectedIndex == 0 ? null : cboStatus.SelectedItem?.ToString();
                var vm = _ctrl.GetProcurementReportVM(dtpFrom.Value, dtpTo.Value, st);
                if (vm.ProcurementRows != null)
                    foreach (var r in vm.ProcurementRows)
                    {
                        string key = r.SupplierName ?? "Unknown";
                        if (!spendBySupplier.ContainsKey(key)) spendBySupplier[key] = 0;
                        spendBySupplier[key] += r.TotalAmount;
                    }
            }
            catch { }
            return BuildChartCard("Spend by Supplier",
                new List<string>(spendBySupplier.Keys).ToArray(),
                new List<double>(spendBySupplier.Values).ToArray(),
                ChartStyle.Bar);
        }

        // ════════════════════════════════════════════════════════════════
        //  3. LOGISTICS OVERVIEW
        // ════════════════════════════════════════════════════════════════

        private void RenderLogistics()
        {
            var dtpFrom   = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DefaultDateFrom, Font = new Font("Segoe UI", 12f) };
            var dtpTo     = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DefaultDateTo,   Font = new Font("Segoe UI", 12f) };
            var cboStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            cboStatus.Items.AddRange(new object[] { "All", "Pending", "In Transit", "Delivered", "Cancelled" });
            cboStatus.SelectedIndex = 0;

            var btnApply  = MakePrimaryBtn("\U0001F50D  Apply");
            var btnReset  = MakeOutlineBtn("\u21BA  Reset");
            var btnToggle = MakeAmberBtn("\U0001F4CA  Chart");
            var btnExport = MakeExportBtn("\U0001F4E4  Export");
            ApplyToggleStyle(btnToggle, _logisticsChart);

            var dgv = MakeDgv();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDOID",      HeaderText = "DO ID",           FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSOID",      HeaderText = "SO ID",           FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomer",  HeaderText = "CUSTOMER",        FillWeight = 22 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",    HeaderText = "STATUS",          FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDriver",    HeaderText = "DRIVER",          FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",      HeaderText = "DELIVERY DATE",   FillWeight = 14 });
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
                string st = cboStatus.SelectedIndex == 0 ? null : cboStatus.SelectedItem?.ToString();
                var vm = _ctrl.GetLogisticsReportVM(dtpFrom.Value, dtpTo.Value, st);
                if (vm.LogisticsRows == null) return;
                foreach (var r in vm.LogisticsRows)
                    dgv.Rows.Add(r.DeliveryOrderID, r.SalesOrderID, r.CustomerName,
                                 r.DeliveryStatus, r.DriverName,
                                 r.DeliveryDate.ToString("yyyy-MM-dd"));
            }
            catch { }
        }

        private Panel BuildLogisticsChartCard(DateTimePicker dtpFrom, DateTimePicker dtpTo, ComboBox cboStatus)
        {
            var countByStatus = new Dictionary<string, double>();
            try
            {
                string st = cboStatus.SelectedIndex == 0 ? null : cboStatus.SelectedItem?.ToString();
                var vm = _ctrl.GetLogisticsReportVM(dtpFrom.Value, dtpTo.Value, st);
                if (vm.LogisticsRows != null)
                    foreach (var r in vm.LogisticsRows)
                    {
                        string key = r.DeliveryStatus ?? "Unknown";
                        if (!countByStatus.ContainsKey(key)) countByStatus[key] = 0;
                        countByStatus[key]++;
                    }
            }
            catch { }
            return BuildChartCard("Deliveries by Status",
                new List<string>(countByStatus.Keys).ToArray(),
                new List<double>(countByStatus.Values).ToArray(),
                ChartStyle.Column);
        }

        // ════════════════════════════════════════════════════════════════
        //  4. AFTER-SERVICE SUMMARY
        // ════════════════════════════════════════════════════════════════

        private void RenderAfterService()
        {
            var dtpFrom      = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DefaultDateFrom, Font = new Font("Segoe UI", 12f) };
            var dtpTo        = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DefaultDateTo,   Font = new Font("Segoe UI", 12f) };
            var cboCmpStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            cboCmpStatus.Items.AddRange(new object[] { "All", "Pending", "Processing", "Escalated", "Completed", "Cancelled" });
            cboCmpStatus.SelectedIndex = 0;

            var btnApply  = MakePrimaryBtn("\U0001F50D  Apply");
            var btnReset  = MakeOutlineBtn("\u21BA  Reset");
            var btnToggle = MakeAmberBtn("\U0001F4CA  Chart");
            var btnExport = MakeExportBtn("\U0001F4E4  Export");
            ApplyToggleStyle(btnToggle, _afterServiceChart);

            // Complaints DGV
            var dgvC = MakeDgv();
            dgvC.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",     HeaderText = "DATE",     FillWeight = 13 });
            dgvC.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCmpID",    HeaderText = "COMPLAINT ID", FillWeight = 16 });
            dgvC.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomer", HeaderText = "CUSTOMER", FillWeight = 22 });
            dgvC.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSubject",  HeaderText = "SUBJECT",  FillWeight = 28 });
            dgvC.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",   HeaderText = "STATUS",   FillWeight = 14 });
            dgvC.CellFormatting += DgvCellFormatting;

            // Returns DGV
            var dgvR = MakeDgv();
            dgvR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",     HeaderText = "DATE",          FillWeight = 12 });
            dgvR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colReturnID", HeaderText = "RETURN ID",     FillWeight = 15 });
            dgvR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSOID",     HeaderText = "SO ID",         FillWeight = 14 });
            dgvR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomer", HeaderText = "CUSTOMER",      FillWeight = 20 });
            dgvR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colReason",   HeaderText = "REASON",        FillWeight = 22 });
            dgvR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",   HeaderText = "STATUS",        FillWeight = 14 });
            dgvR.CellFormatting += DgvCellFormatting;

            LoadAfterServiceData(dgvC, dgvR, dtpFrom, dtpTo, cboCmpStatus);

            var dgvCard   = BuildGridCard2(dgvC, dgvR);
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
                        dgvC.Rows.Add(r.ComplaintDate.ToString("yyyy-MM-dd"), r.ComplaintID, r.CustomerName, r.Subject, r.ComplaintStatus);
                if (vm.ReturnRows != null)
                    foreach (var r in vm.ReturnRows)
                        dgvR.Rows.Add(r.ReturnDate.ToString("yyyy-MM-dd"), r.ReturnOrderID, r.SalesOrderID, r.CustomerName, r.Reason, r.ReturnStatus);
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
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colInvoiceID", HeaderText = "TRANSACTION ID",FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colType",      HeaderText = "TYPE",          FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colParty",     HeaderText = "DOCUMENT",      FillWeight = 22 });
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
                        r.TransactionDate.ToString("yyyy-MM-dd"),
                        r.TransactionID,
                        r.DocumentType,
                        r.LinkedDocument,
                        r.Amount.ToString("N2"),
                        r.PaymentMethod,
                        r.ApprovalStatus);
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
                        string key = r.DocumentType ?? "Unknown";
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
        //  Card Border Paint (used by Designer.cs via partial class)
        // ════════════════════════════════════════════════════════════════
        private static void PaintCardBorder(object sender, PaintEventArgs e)
        {
            var p = (Panel)sender;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        }

        // ════════════════════════════════════════════════════════════════
        //  DataGridView Cell Formatting (status badge colouring)
        // ════════════════════════════════════════════════════════════════
        private void DgvCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var dgv = (DataGridView)sender;
            if (e.ColumnIndex < 0 || e.ColumnIndex >= dgv.Columns.Count) return;
            string colName = dgv.Columns[e.ColumnIndex].Name;
            if ((colName == "colStatus" || colName == "colPayStatus") && e.Value != null)
            {
                if (StatusColors.TryGetValue(e.Value.ToString(), out var c))
                {
                    e.CellStyle.BackColor          = c.bg;
                    e.CellStyle.ForeColor          = c.fg;
                    e.CellStyle.SelectionBackColor = c.bg;
                    e.CellStyle.SelectionForeColor = c.fg;
                    e.CellStyle.Font      = new Font("Segoe UI", 11f, FontStyle.Bold);
                    e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                e.FormattingApplied = true;
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  CSV Export
        // ════════════════════════════════════════════════════════════════
        private static void ExportGrid(DataGridView dgv, string reportName)
        {
            try
            {
                using var dlg = new SaveFileDialog
                {
                    Filter   = "CSV Files (*.csv)|*.csv",
                    FileName = $"{reportName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };
                if (dlg.ShowDialog() != DialogResult.OK) return;

                var sb = new System.Text.StringBuilder();
                var headers = new List<string>();
                foreach (DataGridViewColumn col in dgv.Columns)
                    if (col.Visible) headers.Add($"\"{col.HeaderText}\"");
                sb.AppendLine(string.Join(",", headers));

                foreach (DataGridViewRow row in dgv.Rows)
                {
                    if (row.IsNewRow) continue;
                    var cells = new List<string>();
                    foreach (DataGridViewCell cell in row.Cells)
                        if (dgv.Columns[cell.ColumnIndex].Visible)
                            cells.Add($"\"{cell.Value?.ToString()?.Replace("\"", "\"\"")}\"");
                    sb.AppendLine(string.Join(",", cells));
                }
                System.IO.File.WriteAllText(dlg.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                MessageBox.Show("Exported successfully.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  Navigation / Logout  (AppShell RULE 4 handlers)
        // ════════════════════════════════════════════════════════════════
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        { SessionManager.Clear(); Application.Restart(); }

    }
}
