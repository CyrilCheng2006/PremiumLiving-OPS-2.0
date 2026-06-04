using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.Shared
{
    // ================================================================
    //  FILE: Views/Shared/ChartRenderer.cs
    //
    //  Pure GDI+ chart engine — no third-party libraries required.
    //  MVC role: View layer (rendering only, no business logic).
    //
    //  Supported chart types:
    //    BarChart           – vertical bars, labelled axes
    //    HorizontalBarChart – horizontal bars (good for ranked lists)
    //    DonutChart         – percentage ring with legend
    //    LineChart          – multi-series line / area chart
    //
    //  Usage:
    //    var ctrl = ChartRenderer.CreateBarChart(series, title, xLabel, yLabel);
    //    panel.Controls.Add(ctrl);
    // ================================================================

    public static class ChartRenderer
    {
        // ── Brand palette ────────────────────────────────────────────────
        private static readonly Color[] SeriesColors =
        {
            Color.FromArgb(47,  111, 237),   // blue
            Color.FromArgb(22,  163, 74),    // green
            Color.FromArgb(245, 158, 11),    // amber
            Color.FromArgb(232, 64,  64),    // red
            Color.FromArgb(139, 92,  246),   // purple
            Color.FromArgb(6,   182, 212),   // cyan
            Color.FromArgb(251, 113, 133),   // rose
            Color.FromArgb(251, 146, 60),    // orange
        };

        private static readonly Color BgColor     = Color.White;
        private static readonly Color GridColor    = Color.FromArgb(229, 231, 235);
        private static readonly Color AxisColor    = Color.FromArgb(156, 163, 175);
        private static readonly Color LabelColor   = Color.FromArgb(75,  85,  99);
        private static readonly Color TitleColor   = Color.FromArgb(17,  24,  39);
        private static readonly Font  TitleFont    = new Font("Segoe UI", 12f, FontStyle.Bold);
        private static readonly Font  AxisFont     = new Font("Segoe UI", 9f);
        private static readonly Font  LegendFont   = new Font("Segoe UI", 10f);
        private static readonly Font  ValueFont    = new Font("Segoe UI", 8f, FontStyle.Bold);

        // ================================================================
        //  DATA STRUCTURES
        // ================================================================

        /// <summary>One named series with (Label, Value) pairs.</summary>
        public class ChartSeries
        {
            public string         Name   { get; set; }
            public List<(string label, double value)> Points { get; set; }
                = new List<(string, double)>();
        }

        // ================================================================
        //  PUBLIC FACTORY METHODS — return a Panel that hosts the chart
        // ================================================================

        /// <summary>Vertical bar chart.</summary>
        public static Panel CreateBarChart(
            List<(string label, double value)> data,
            string title,
            string yLabel  = "",
            string yFormat = "N0",
            Color? barColor = null)
        {
            var pnl = new ChartPanel();
            pnl.PaintChart += (g, r) => DrawBarChart(g, r, data, title, yLabel, yFormat, barColor ?? SeriesColors[0]);
            return pnl;
        }

        /// <summary>Horizontal bar chart — good for ranking / comparison.</summary>
        public static Panel CreateHorizontalBarChart(
            List<(string label, double value)> data,
            string title,
            string valueFormat = "N0",
            Color? barColor    = null)
        {
            var pnl = new ChartPanel();
            pnl.PaintChart += (g, r) => DrawHorizontalBarChart(g, r, data, title, valueFormat, barColor ?? SeriesColors[0]);
            return pnl;
        }

        /// <summary>Donut (ring) chart with legend.</summary>
        public static Panel CreateDonutChart(
            List<(string label, double value)> data,
            string title)
        {
            var pnl = new ChartPanel();
            pnl.PaintChart += (g, r) => DrawDonutChart(g, r, data, title);
            return pnl;
        }

        /// <summary>Multi-series line chart.</summary>
        public static Panel CreateLineChart(
            List<ChartSeries> series,
            string title,
            string yLabel  = "",
            string yFormat = "N0")
        {
            var pnl = new ChartPanel();
            pnl.PaintChart += (g, r) => DrawLineChart(g, r, series, title, yLabel, yFormat);
            return pnl;
        }

        // ================================================================
        //  BAR CHART
        // ================================================================

        private static void DrawBarChart(
            Graphics g, Rectangle bounds,
            List<(string label, double value)> data,
            string title, string yLabel, string yFormat, Color barColor)
        {
            if (data == null || data.Count == 0) { DrawEmpty(g, bounds, title); return; }

            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.SmoothingMode     = SmoothingMode.AntiAlias;

            const int PadT = 48; const int PadB = 56; const int PadL = 70; const int PadR = 20;

            var plot = new Rectangle(bounds.Left + PadL, bounds.Top + PadT,
                                     bounds.Width - PadL - PadR,
                                     bounds.Height - PadT - PadB);
            if (plot.Width <= 0 || plot.Height <= 0) return;

            // Title
            DrawCentredString(g, title, TitleFont, TitleColor, new RectangleF(bounds.Left, bounds.Top, bounds.Width, PadT));

            double maxVal  = data.Max(d => d.value);
            double niceMax = NiceMax(maxVal);
            int    gridLines = 5;

            // Grid lines + Y-axis labels
            for (int i = 0; i <= gridLines; i++)
            {
                double v  = niceMax * i / gridLines;
                float  y  = plot.Bottom - (float)(plot.Height * v / niceMax);
                using var gridPen = new Pen(GridColor, 1) { DashStyle = DashStyle.Dash };
                g.DrawLine(gridPen, plot.Left, y, plot.Right, y);
                var lbl = v.ToString(yFormat);
                var sz  = g.MeasureString(lbl, AxisFont);
                g.DrawString(lbl, AxisFont, new SolidBrush(AxisColor),
                             plot.Left - sz.Width - 4, y - sz.Height / 2);
            }

            // Y-axis label
            if (!string.IsNullOrEmpty(yLabel))
            {
                var state = g.Save();
                g.TranslateTransform(bounds.Left + 12, bounds.MidY());
                g.RotateTransform(-90);
                g.DrawString(yLabel, AxisFont, new SolidBrush(LabelColor), -g.MeasureString(yLabel, AxisFont).Width / 2, 0);
                g.Restore(state);
            }

            // Bars
            int    n       = data.Count;
            float  barW    = Math.Min(60f, (plot.Width * 0.7f) / n);
            float  groupW  = (float)plot.Width / n;

            for (int i = 0; i < n; i++)
            {
                var (label, value) = data[i];
                float x  = plot.Left + i * groupW + (groupW - barW) / 2;
                float h  = niceMax > 0 ? (float)(plot.Height * value / niceMax) : 0;
                float y  = plot.Bottom - h;

                // Bar gradient
                using var brush = new LinearGradientBrush(
                    new PointF(x, y), new PointF(x, plot.Bottom),
                    AdjustBrightness(barColor, 1.15f), barColor);
                g.FillRectangle(brush, x, y, barW, h);

                // Value label on top
                var valStr = value.ToString(yFormat);
                var valSz  = g.MeasureString(valStr, ValueFont);
                if (h > valSz.Height + 4)
                    g.DrawString(valStr, ValueFont, new SolidBrush(Color.White),
                                 x + (barW - valSz.Width) / 2, y + 4);
                else
                    g.DrawString(valStr, ValueFont, new SolidBrush(LabelColor),
                                 x + (barW - valSz.Width) / 2, y - valSz.Height - 2);

                // X-axis label (rotated if long)
                var lblSz = g.MeasureString(label, AxisFont);
                if (lblSz.Width > groupW - 4)
                {
                    var state = g.Save();
                    g.TranslateTransform(x + barW / 2, plot.Bottom + 6);
                    g.RotateTransform(30);
                    g.DrawString(label, AxisFont, new SolidBrush(LabelColor), 0, 0);
                    g.Restore(state);
                }
                else
                    g.DrawString(label, AxisFont, new SolidBrush(LabelColor),
                                 x + (barW - lblSz.Width) / 2, plot.Bottom + 6);
            }

            // Axes
            using var axisPen = new Pen(AxisColor, 1.5f);
            g.DrawLine(axisPen, plot.Left, plot.Top,    plot.Left,  plot.Bottom);
            g.DrawLine(axisPen, plot.Left, plot.Bottom, plot.Right, plot.Bottom);
        }

        // ================================================================
        //  HORIZONTAL BAR CHART
        // ================================================================

        private static void DrawHorizontalBarChart(
            Graphics g, Rectangle bounds,
            List<(string label, double value)> data,
            string title, string valueFormat, Color barColor)
        {
            if (data == null || data.Count == 0) { DrawEmpty(g, bounds, title); return; }

            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.SmoothingMode     = SmoothingMode.AntiAlias;

            const int PadT = 48; const int PadB = 20; const int PadL = 180; const int PadR = 80;

            var plot = new Rectangle(bounds.Left + PadL, bounds.Top + PadT,
                                     bounds.Width - PadL - PadR,
                                     bounds.Height - PadT - PadB);
            if (plot.Width <= 0 || plot.Height <= 0) return;

            DrawCentredString(g, title, TitleFont, TitleColor,
                              new RectangleF(bounds.Left, bounds.Top, bounds.Width, PadT));

            double maxVal  = data.Max(d => d.value);
            double niceMax = NiceMax(maxVal);

            int   n    = data.Count;
            float rowH = (float)plot.Height / n;
            float barH = Math.Min(28f, rowH * 0.65f);

            for (int i = 0; i < n; i++)
            {
                var (label, value) = data[i];
                float yC = plot.Top + i * rowH + rowH / 2;
                float w  = niceMax > 0 ? (float)(plot.Width * value / niceMax) : 0;

                // Alternating row background
                if (i % 2 == 1)
                    g.FillRectangle(new SolidBrush(Color.FromArgb(248, 250, 252)),
                                    plot.Left - PadL + 4, yC - rowH / 2, bounds.Width - 8, rowH);

                // Bar
                Color c = SeriesColors[i % SeriesColors.Length];
                using var brush = new LinearGradientBrush(
                    new PointF(plot.Left, yC), new PointF(plot.Left + w, yC),
                    AdjustBrightness(c, 1.15f), c);
                g.FillRectangle(brush, plot.Left, yC - barH / 2, w, barH);

                // Label (left)
                var lblSz = g.MeasureString(label, AxisFont);
                g.DrawString(label, AxisFont, new SolidBrush(LabelColor),
                             plot.Left - lblSz.Width - 8, yC - lblSz.Height / 2);

                // Value (right of bar)
                var valStr = value.ToString(valueFormat);
                g.DrawString(valStr, ValueFont, new SolidBrush(LabelColor),
                             plot.Left + w + 6, yC - g.MeasureString(valStr, ValueFont).Height / 2);
            }

            // Vertical grid lines
            for (int i = 1; i <= 4; i++)
            {
                float x = plot.Left + plot.Width * i / 4f;
                using var pen = new Pen(GridColor, 1) { DashStyle = DashStyle.Dash };
                g.DrawLine(pen, x, plot.Top, x, plot.Bottom);
            }

            // Axis
            using var axisPen = new Pen(AxisColor, 1.5f);
            g.DrawLine(axisPen, plot.Left, plot.Top, plot.Left, plot.Bottom);
        }

        // ================================================================
        //  DONUT CHART
        // ================================================================

        private static void DrawDonutChart(
            Graphics g, Rectangle bounds,
            List<(string label, double value)> data,
            string title)
        {
            if (data == null || data.Count == 0) { DrawEmpty(g, bounds, title); return; }

            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.SmoothingMode     = SmoothingMode.AntiAlias;

            const int PadT     = 48;
            const int LegendW  = 200;
            const int Margin   = 20;

            DrawCentredString(g, title, TitleFont, TitleColor,
                              new RectangleF(bounds.Left, bounds.Top, bounds.Width, PadT));

            double total = data.Sum(d => d.value);
            if (total <= 0) { DrawEmpty(g, bounds, title); return; }

            // Donut area
            int donutSize = Math.Min(bounds.Width - LegendW - Margin * 3, bounds.Height - PadT - Margin * 2);
            donutSize = Math.Max(donutSize, 80);
            int donutLeft = bounds.Left + Margin;
            int donutTop  = bounds.Top + PadT + (bounds.Height - PadT - donutSize) / 2;
            var donutRect = new Rectangle(donutLeft, donutTop, donutSize, donutSize);
            int hole      = (int)(donutSize * 0.55f);
            var holeRect  = new Rectangle(donutLeft + (donutSize - hole) / 2,
                                          donutTop  + (donutSize - hole) / 2, hole, hole);

            float startAngle = -90f;
            for (int i = 0; i < data.Count; i++)
            {
                float sweep = (float)(data[i].value / total * 360.0);
                Color c     = SeriesColors[i % SeriesColors.Length];
                g.FillPie(new SolidBrush(c), donutRect, startAngle, sweep);
                startAngle += sweep;
            }
            // Punch hole
            g.FillEllipse(new SolidBrush(BgColor), holeRect);

            // Centre label
            string totStr = total.ToString("N0");
            var totSz = g.MeasureString(totStr, TitleFont);
            g.DrawString(totStr, TitleFont, new SolidBrush(TitleColor),
                         donutLeft + (donutSize - totSz.Width)  / 2,
                         donutTop  + (donutSize - totSz.Height) / 2);

            // Legend
            int legX = donutLeft + donutSize + Margin;
            int legY = donutTop  + 10;
            const int BoxS = 14; const int RowH = 26;

            for (int i = 0; i < data.Count && legY + RowH <= bounds.Bottom - Margin; i++)
            {
                Color c = SeriesColors[i % SeriesColors.Length];
                g.FillRectangle(new SolidBrush(c), legX, legY + (RowH - BoxS) / 2, BoxS, BoxS);

                double pct = total > 0 ? data[i].value / total * 100 : 0;
                string lbl = $"{data[i].label}  ({pct:F1}%)";
                g.DrawString(lbl, LegendFont, new SolidBrush(LabelColor), legX + BoxS + 6, legY + (RowH - 14) / 2);
                legY += RowH;
            }
        }

        // ================================================================
        //  LINE CHART
        // ================================================================

        private static void DrawLineChart(
            Graphics g, Rectangle bounds,
            List<ChartSeries> series,
            string title, string yLabel, string yFormat)
        {
            if (series == null || series.Count == 0 || series[0].Points.Count == 0)
            { DrawEmpty(g, bounds, title); return; }

            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.SmoothingMode     = SmoothingMode.AntiAlias;

            const int PadT = 48; const int PadB = 60; const int PadL = 72; const int PadR = 20;
            const int LegH = 28;

            var plot = new Rectangle(bounds.Left + PadL, bounds.Top + PadT,
                                     bounds.Width - PadL - PadR,
                                     bounds.Height - PadT - PadB - LegH);
            if (plot.Width <= 0 || plot.Height <= 0) return;

            DrawCentredString(g, title, TitleFont, TitleColor,
                              new RectangleF(bounds.Left, bounds.Top, bounds.Width, PadT));

            double maxVal  = series.SelectMany(s2 => s2.Points).Max(p => p.value);
            double niceMax = NiceMax(maxVal);
            int    labels  = series[0].Points.Count;

            // Grid + Y labels
            for (int i = 0; i <= 4; i++)
            {
                double v = niceMax * i / 4;
                float  y = plot.Bottom - (float)(plot.Height * v / niceMax);
                using var gPen = new Pen(GridColor, 1) { DashStyle = DashStyle.Dash };
                g.DrawLine(gPen, plot.Left, y, plot.Right, y);
                var lbl = v.ToString(yFormat);
                var sz  = g.MeasureString(lbl, AxisFont);
                g.DrawString(lbl, AxisFont, new SolidBrush(AxisColor),
                             plot.Left - sz.Width - 4, y - sz.Height / 2);
            }

            // X labels
            for (int i = 0; i < labels; i++)
            {
                float x   = plot.Left + (float)plot.Width * i / Math.Max(labels - 1, 1);
                var   lbl = series[0].Points[i].label;
                var   sz  = g.MeasureString(lbl, AxisFont);
                g.DrawString(lbl, AxisFont, new SolidBrush(AxisColor),
                             x - sz.Width / 2, plot.Bottom + 6);
            }

            // Series
            for (int si = 0; si < series.Count; si++)
            {
                Color c = SeriesColors[si % SeriesColors.Length];
                var pts = series[si].Points;

                // Area fill
                var areaPoints = new List<PointF>();
                areaPoints.Add(new PointF(plot.Left, plot.Bottom));
                for (int i = 0; i < pts.Count; i++)
                {
                    float x = plot.Left + (float)plot.Width * i / Math.Max(pts.Count - 1, 1);
                    float y = niceMax > 0 ? plot.Bottom - (float)(plot.Height * pts[i].value / niceMax) : plot.Bottom;
                    areaPoints.Add(new PointF(x, y));
                }
                areaPoints.Add(new PointF(plot.Left + (float)plot.Width * (pts.Count - 1) / Math.Max(pts.Count - 1, 1), plot.Bottom));
                using var areaBrush = new SolidBrush(Color.FromArgb(30, c));
                g.FillPolygon(areaBrush, areaPoints.ToArray());

                // Line
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    float x1 = plot.Left + (float)plot.Width * i     / Math.Max(pts.Count - 1, 1);
                    float y1 = niceMax > 0 ? plot.Bottom - (float)(plot.Height * pts[i].value     / niceMax) : plot.Bottom;
                    float x2 = plot.Left + (float)plot.Width * (i+1) / Math.Max(pts.Count - 1, 1);
                    float y2 = niceMax > 0 ? plot.Bottom - (float)(plot.Height * pts[i+1].value   / niceMax) : plot.Bottom;
                    using var lPen = new Pen(c, 2.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                    g.DrawLine(lPen, x1, y1, x2, y2);
                }

                // Dots
                for (int i = 0; i < pts.Count; i++)
                {
                    float x = plot.Left + (float)plot.Width * i / Math.Max(pts.Count - 1, 1);
                    float y = niceMax > 0 ? plot.Bottom - (float)(plot.Height * pts[i].value / niceMax) : plot.Bottom;
                    g.FillEllipse(new SolidBrush(BgColor), x - 5, y - 5, 10, 10);
                    g.FillEllipse(new SolidBrush(c),       x - 4, y - 4,  8,  8);
                }
            }

            // Legend
            float legX = bounds.Left + PadL;
            float legY = plot.Bottom + 34;
            foreach (var s2 in series)
            {
                int idx = series.IndexOf(s2);
                Color c = SeriesColors[idx % SeriesColors.Length];
                g.FillRectangle(new SolidBrush(c), legX, legY + 5, 18, 10);
                g.DrawString(s2.Name, LegendFont, new SolidBrush(LabelColor), legX + 22, legY);
                legX += g.MeasureString(s2.Name, LegendFont).Width + 46;
            }

            // Axes
            using var axisPen = new Pen(AxisColor, 1.5f);
            g.DrawLine(axisPen, plot.Left, plot.Top, plot.Left,  plot.Bottom);
            g.DrawLine(axisPen, plot.Left, plot.Bottom, plot.Right, plot.Bottom);
        }

        // ================================================================
        //  HELPERS
        // ================================================================

        private static void DrawEmpty(Graphics g, Rectangle b, string title)
        {
            g.FillRectangle(new SolidBrush(BgColor), b);
            DrawCentredString(g, title, TitleFont, TitleColor,
                              new RectangleF(b.Left, b.Top, b.Width, 48));
            DrawCentredString(g, "No data available", LegendFont, AxisColor,
                              new RectangleF(b.Left, b.Top + b.Height / 2 - 20, b.Width, 40));
        }

        private static void DrawCentredString(Graphics g, string text, Font font, Color color, RectangleF rect)
        {
            using var sf  = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(text, font, new SolidBrush(color), rect, sf);
        }

        private static double NiceMax(double raw)
        {
            if (raw <= 0) return 10;
            double mag    = Math.Pow(10, Math.Floor(Math.Log10(raw)));
            double nice   = Math.Ceiling(raw / mag) * mag;
            return nice < raw ? nice * 2 : nice;
        }

        private static Color AdjustBrightness(Color c, float factor)
        {
            return Color.FromArgb(c.A,
                Math.Min(255, (int)(c.R * factor)),
                Math.Min(255, (int)(c.G * factor)),
                Math.Min(255, (int)(c.B * factor)));
        }

        private static float MidY(this Rectangle r) => r.Top + r.Height / 2f;

        // ── Custom Panel subclass that raises a PaintChart event ──────────
        private class ChartPanel : Panel
        {
            public event Action<Graphics, Rectangle> PaintChart;

            public ChartPanel()
            {
                this.DoubleBuffered = true;
                this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                              ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.FillRectangle(new SolidBrush(BgColor), ClientRectangle);
                PaintChart?.Invoke(e.Graphics, ClientRectangle);
            }
        }
    }
}
