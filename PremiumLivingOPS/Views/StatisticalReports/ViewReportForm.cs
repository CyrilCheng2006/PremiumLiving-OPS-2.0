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
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;

            var vm = _ctrl.GetSalesReportVM();
            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Statistical Reports  ›  View Report");
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

            tbl.Controls.Add(MakeCell("Date From", dtpFrom, true),  0, 0);
            tbl.Controls.Add(MakeCell("Date To",   dtpTo,   extraCount > 0), 2, 0);

            if (extraCols != null)
                for (int i = 0; i < extraCols.Length; i++)
                    tbl.Controls.Add(MakeCell(extraCols[i].caption, extraCols[i].ctrl, i < extraCols.Length - 1), 3 + i, 0);

            return tbl;
        }

        private static DateTimePicker MakeDatePicker(DateTime value)
        {
            return new DateTimePicker
            {
                Format        = DateTimePickerFormat.Short,
                Value         = value,
                Font          = new Font("Segoe UI", 11f),
                CalendarFont  = new Font("Segoe UI", 10f)
            };
        }

        private static ComboBox MakeComboBox(string[] items, string selectedValue = null)
        {
            var cmb = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 11f)
            };
            cmb.Items.AddRange(items);
            if (selectedValue != null && cmb.Items.Contains(selectedValue))
                cmb.SelectedItem = selectedValue;
            else if (cmb.Items.Count > 0)
                cmb.SelectedIndex = 0;
            return cmb;
        }

        // ════════════════════════════════════════════════════════════════
        //  KPI ROW BUILDER
        // ════════════════════════════════════════════════════════════════

        private static Panel BuildKpiRow(params (string label, string value, Color accent)[] kpis)
        {
            int n = Math.Max(1, kpis.Length);
            var tbl = new TableLayoutPanel
            {
                Dock        = DockStyle.Top,
                Height      = 100,
                ColumnCount = n,
                RowCount    = 1,
                BackColor   = Color.Transparent,
                Padding     = new Padding(0, 0, 0, 12)
            };
            for (int i = 0; i < n; i++)
                tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / n));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            foreach (var (label, value, accent) in kpis)
            {
                var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(0, 0, 10, 0) };
                card.Paint += (s, e) =>
                {
                    var g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    using var accentPen = new Pen(accent, 3f);
                    g.DrawLine(accentPen, 0, card.Height - 3, card.Width, card.Height - 3);
                    using var borderPen = new Pen(Color.FromArgb(221, 227, 236), 1f);
                    g.DrawRectangle(borderPen, 0, 0, card.Width - 1, card.Height - 1);
                };

                var inner = new TableLayoutPanel
                {
                    Dock        = DockStyle.Fill,
                    RowCount    = 2,
                    ColumnCount = 1,
                    BackColor   = Color.Transparent,
                    Padding     = new Padding(16, 10, 16, 10)
                };
                inner.RowStyles.Add(new RowStyle(SizeType.Percent, 45f));
                inner.RowStyles.Add(new RowStyle(SizeType.Percent, 55f));

                inner.Controls.Add(new Label
                {
                    Text      = label,
                    Font      = new Font("Segoe UI", 10f),
                    ForeColor = Color.FromArgb(98, 112, 135),
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.BottomLeft
                }, 0, 0);
                inner.Controls.Add(new Label
                {
                    Text      = value,
                    Font      = new Font("Segoe UI", 15f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(15, 31, 53),
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                }, 0, 1);

                card.Controls.Add(inner);
                tbl.Controls.Add(card);
            }

            var outer = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = Color.Transparent };
            outer.Controls.Add(tbl);
            return outer;
        }

        // ════════════════════════════════════════════════════════════════
        //  DGV BUILDER
        // ════════════════════════════════════════════════════════════════

        private DataGridView BuildDgv()
        {
            var dgv = new DataGridView
            {
                Dock                  = DockStyle.Fill,
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible     = false,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                ReadOnly              = true,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor       = Color.White,
                BorderStyle           = BorderStyle.None,
                GridColor             = Color.FromArgb(235, 238, 245),
                Font                  = new Font("Segoe UI", 11f),
                RowTemplate           = { Height = 42 }
            };
            dgv.DefaultCellStyle.Padding              = new Padding(8, 0, 8, 0);
            dgv.DefaultCellStyle.SelectionBackColor   = Color.FromArgb(235, 241, 255);
            dgv.DefaultCellStyle.SelectionForeColor   = Color.FromArgb(15, 31, 53);
            dgv.ColumnHeadersDefaultCellStyle.Font    = new Font("Segoe UI", 10f, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(247, 249, 252);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(98, 112, 135);
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
            dgv.ColumnHeadersHeight                   = 40;
            dgv.ColumnHeadersBorderStyle              = DataGridViewHeaderBorderStyle.Single;
            dgv.EnableHeadersVisualStyles             = false;
            dgv.CellFormatting += DgvCellFormatting;
            return dgv;
        }

        // ════════════════════════════════════════════════════════════════
        //  CHART CARD BUILDER  (pure GDI+)
        // ════════════════════════════════════════════════════════════════

        private static Panel BuildChartCard(
            string title,
            string[] labels,
            double[] values,
            ChartStyle style = ChartStyle.Bar)
        {
            var outer = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 12, 0, 0) };

            var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode     = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                int W = card.Width;
                int H = card.Height;
                int padL = 60, padR = 30, padT = 50, padB = 60;
                int chartW = W - padL - padR;
                int chartH = H - padT - padB;

                // Title
                using var titleFont = new Font("Segoe UI", 13f, FontStyle.Bold);
                g.DrawString(title, titleFont, Brushes.Black, new PointF(padL, 14));

                if (labels == null || labels.Length == 0 || values == null || values.Length == 0)
                {
                    g.DrawString("No data", new Font("Segoe UI", 11f), Brushes.Gray,
                        new PointF(W / 2f - 30, H / 2f));
                    return;
                }

                int n = Math.Min(labels.Length, values.Length);

                if (style == ChartStyle.Pie)
                {
                    double total = 0;
                    for (int i = 0; i < n; i++) total += Math.Abs(values[i]);
                    if (total == 0) return;

                    int pieSize = Math.Min(chartW, chartH) - 20;
                    int pieX    = padL + (chartW - pieSize) / 2;
                    int pieY    = padT + (chartH - pieSize) / 2;

                    float startAngle = -90f;
                    for (int i = 0; i < n; i++)
                    {
                        float sweep = (float)(Math.Abs(values[i]) / total * 360.0);
                        using var brush = new SolidBrush(ChartPalette[i % ChartPalette.Length]);
                        g.FillPie(brush, pieX, pieY, pieSize, pieSize, startAngle, sweep);
                        using var pen = new Pen(Color.White, 1.5f);
                        g.DrawPie(pen, pieX, pieY, pieSize, pieSize, startAngle, sweep);
                        startAngle += sweep;
                    }

                    // Legend
                    int legX = padL;
                    int legY = padT + chartH + 8;
                    using var legFont = new Font("Segoe UI", 9f);
                    for (int i = 0; i < n; i++)
                    {
                        using var brush = new SolidBrush(ChartPalette[i % ChartPalette.Length]);
                        g.FillRectangle(brush, legX, legY, 12, 12);
                        string txt = $"{labels[i]} ({values[i]:N0})";
                        g.DrawString(txt, legFont, Brushes.Black, legX + 16, legY - 1);
                        legX += (int)g.MeasureString(txt, legFont).Width + 28;
                        if (legX > W - 120) { legX = padL; legY += 18; }
                    }
                    return;
                }

                // Bar / Column
                double maxVal = 0;
                for (int i = 0; i < n; i++) if (values[i] > maxVal) maxVal = values[i];
                if (maxVal == 0) maxVal = 1;

                using var axisFont  = new Font("Segoe UI", 9f);
                using var axisPen   = new Pen(Color.FromArgb(200, 200, 200), 1f);
                using var labelBrush = new SolidBrush(Color.FromArgb(98, 112, 135));

                if (style == ChartStyle.Column)
                {
                    int barW  = Math.Max(8, chartW / n - 8);
                    int step  = chartW / n;
                    int baseY = padT + chartH;

                    // Y gridlines
                    for (int t = 0; t <= 4; t++)
                    {
                        int gy = padT + (int)(chartH * t / 4.0);
                        g.DrawLine(axisPen, padL, gy, padL + chartW, gy);
                        double yv = maxVal * (4 - t) / 4.0;
                        string yLabel = yv >= 1000 ? $"{yv / 1000:N1}k" : yv.ToString("N0");
                        g.DrawString(yLabel, axisFont, labelBrush, padL - 50, gy - 8, new StringFormat { Alignment = StringAlignment.Far });
                    }

                    for (int i = 0; i < n; i++)
                    {
                        int barH = (int)(values[i] / maxVal * chartH);
                        int x    = padL + i * step + (step - barW) / 2;
                        int y    = baseY - barH;
                        using var brush = new SolidBrush(ChartPalette[i % ChartPalette.Length]);
                        g.FillRectangle(brush, x, y, barW, barH);

                        // X label
                        var sf = new StringFormat { Alignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
                        g.DrawString(labels[i], axisFont, labelBrush, new RectangleF(x - 4, baseY + 4, barW + 8, 40), sf);

                        // Value on top
                        string vLabel = values[i] >= 1000 ? $"{values[i] / 1000:N1}k" : values[i].ToString("N0");
                        var sf2 = new StringFormat { Alignment = StringAlignment.Center };
                        g.DrawString(vLabel, axisFont, Brushes.Black, new PointF(x + barW / 2f, y - 16), sf2);
                    }
                }
                else // Bar (horizontal)
                {
                    int rowH = Math.Max(8, chartH / n - 6);
                    int step = chartH / n;

                    // X gridlines
                    for (int t = 0; t <= 4; t++)
                    {
                        int gx = padL + (int)(chartW * t / 4.0);
                        g.DrawLine(axisPen, gx, padT, gx, padT + chartH);
                        double xv = maxVal * t / 4.0;
                        string xLabel = xv >= 1000 ? $"{xv / 1000:N1}k" : xv.ToString("N0");
                        g.DrawString(xLabel, axisFont, labelBrush, gx - 12, padT + chartH + 4);
                    }

                    for (int i = 0; i < n; i++)
                    {
                        int barW = (int)(values[i] / maxVal * chartW);
                        int y    = padT + i * step + (step - rowH) / 2;

                        using var brush = new SolidBrush(ChartPalette[i % ChartPalette.Length]);
                        g.FillRectangle(brush, padL, y, barW, rowH);

                        // Y label
                        var sf = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
                        g.DrawString(labels[i], axisFont, labelBrush, new RectangleF(0, y, padL - 4, rowH), sf);

                        // Value
                        string vLabel = values[i] >= 1000 ? $"{values[i] / 1000:N1}k" : values[i].ToString("N0");
                        g.DrawString(vLabel, axisFont, Brushes.Black, padL + barW + 4, y + (rowH - 14) / 2f);
                    }
                }
            };

            outer.Controls.Add(card);
            return outer;
        }

        // ════════════════════════════════════════════════════════════════
        //  CONTENT CARD WRAPPER
        // ════════════════════════════════════════════════════════════════

        private Panel WrapInContentCard(Control inner)
        {
            var card = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.White,
                Padding   = new Padding(20, 14, 20, 14)
            };
            card.Paint += PaintCardBorder;
            inner.Dock = DockStyle.Fill;
            card.Controls.Add(inner);

            var outer = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Palette.BgPage,
                Padding   = new Padding(20, 14, 20, 14)
            };
            outer.Controls.Add(card);
            return outer;
        }

        // ════════════════════════════════════════════════════════════════
        //  SECTION HEADER HELPER
        // ════════════════════════════════════════════════════════════════

        private static Panel BuildSectionHeader(string title, Button toggleBtn, Button exportBtn)
        {
            var pnl = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 52,
                BackColor = Color.Transparent
            };

            var lbl = new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Left,
                AutoSize  = false,
                Width     = 420,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(0, 0, 0, 0)
            };

            var btnPanel = new Panel
            {
                Dock      = DockStyle.Right,
                Width     = 300,
                BackColor = Color.Transparent
            };

            if (exportBtn != null)
            {
                exportBtn.Dock  = DockStyle.Right;
                exportBtn.Width = 130;
                btnPanel.Controls.Add(exportBtn);
            }
            if (toggleBtn != null)
            {
                toggleBtn.Dock  = DockStyle.Right;
                toggleBtn.Width = 150;
                btnPanel.Controls.Add(toggleBtn);
            }

            pnl.Controls.Add(lbl);
            pnl.Controls.Add(btnPanel);
            return pnl;
        }

        // ════════════════════════════════════════════════════════════════
        //  1. SALES PERFORMANCE
        // ════════════════════════════════════════════════════════════════

        private void RenderSales()
        {
            var dtFrom = MakeDatePicker(DefaultDateFrom);
            var dtTo   = MakeDatePicker(DefaultDateTo);
            var cmbStatus = MakeComboBox(new[] { "All", "Pending", "Processing", "Delivered", "Partially Delivered", "Cancelled" });

            var btnSearch = MakeFilterButton("Search", Palette.Primary, Color.White);
            var btnReset  = MakeFilterButton("Reset",  Color.FromArgb(241, 244, 249), Color.FromArgb(15, 31, 53));

            var btnRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnSearch.Width = 110; btnSearch.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            btnReset.Width  = 90;  btnReset.Anchor  = AnchorStyles.Left | AnchorStyles.Bottom;
            btnSearch.Location = new Point(0,   10);
            btnReset.Location  = new Point(118, 10);
            btnRow.Controls.Add(btnSearch);
            btnRow.Controls.Add(btnReset);

            SetFilterBar("Sales Performance",
                BuildFieldsRow(
                    ("Date From", dtFrom),
                    ("Date To",   dtTo),
                    ("Status",    cmbStatus)),
                btnRow);

            ViewReportViewModel vm = null;
            DataGridView dgv = null;
            Panel dgvCard  = null;
            Panel chartCard = null;

            void LoadData()
            {
                vm = _ctrl.GetSalesReportVM(dtFrom.Value, dtTo.Value,
                    cmbStatus.SelectedItem?.ToString() == "All" ? null : cmbStatus.SelectedItem?.ToString());

                var kpi = vm.SalesKpi ?? new SalesKpiEntity();
                var rows = vm.SalesRows ?? new List<SalesOrderRowEntity>();

                var kpiRow = BuildKpiRow(
                    ("Total Orders",       kpi.TotalOrders.ToString(),            Color.FromArgb(29,  78, 216)),
                    ("Total Revenue",      $"HKD {kpi.TotalRevenue:N0}",          Color.FromArgb(6,   95,  70)),
                    ("Avg Order Value",    $"HKD {kpi.AverageOrderValue:N0}",     Color.FromArgb(55,  48, 163)),
                    ("Delivered",          kpi.DeliveredOrders.ToString(),         Color.FromArgb(22, 101,  52)),
                    ("Pending/Processing", $"{kpi.PendingOrders + kpi.ProcessingOrders}", Color.FromArgb(146, 64, 14)),
                    ("Cancelled",          kpi.CancelledOrders.ToString(),         Color.FromArgb(185, 28,  28)));

                dgv = BuildDgv();
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrderID",   HeaderText = "Order ID",       FillWeight = 12 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomer",  HeaderText = "Customer",       FillWeight = 22 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",    HeaderText = "Status",         FillWeight = 14 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",      HeaderText = "Issued Date",    FillWeight = 14 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRevenue",   HeaderText = "Grand Total",    FillWeight = 14 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLines",     HeaderText = "Lines",          FillWeight =  8 });

                foreach (var r in rows)
                    dgv.Rows.Add(r.OrderID, r.CustomerName, r.OrderStatus,
                        r.IssuedTime.ToString("yyyy-MM-dd"), $"HKD {r.GrandTotal:N0}", r.LineCount);

                var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, BackColor = Color.White };
                tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 100f));
                tbl.RowStyles.Add(new RowStyle(SizeType.Absolute,  52f));
                tbl.RowStyles.Add(new RowStyle(SizeType.Percent,  100f));

                var btnToggle = MakeFilterButton("View Chart", Color.FromArgb(235, 241, 255), Palette.Primary);
                var btnExport = MakeFilterButton("Export CSV", Color.FromArgb(241, 244, 249), Color.FromArgb(15, 31, 53));

                btnExport.Click += (s, e) => ExportGrid(dgv, "SalesPerformance");
                btnToggle.Click += (s, e) =>
                {
                    _salesChart = !_salesChart;
                    btnToggle.Text = _salesChart ? "View Table" : "View Chart";
                    if (chartCard == null) RebuildSalesChart();
                    SwapContent(dgvCard, chartCard, _salesChart);
                };

                tbl.Controls.Add(kpiRow, 0, 0);
                tbl.Controls.Add(BuildSectionHeader("Order List", btnToggle, btnExport), 0, 1);
                tbl.Controls.Add(dgv, 0, 2);

                dgvCard = WrapInContentCard(tbl);
                chartCard = null;   // rebuilt lazily on first toggle
                _salesChart = false;
                SwapContent(dgvCard, chartCard, false);
            }

            void RebuildSalesChart()
            {
                if (vm?.TopProducts == null) return;
                var prods = vm.TopProducts;
                var lbls  = prods.ConvertAll(p => p.ItemName).ToArray();
                var vals  = prods.ConvertAll(p => p.TotalRevenue).ToArray();
                chartCard = WrapInContentCard(BuildChartCard("Top Products by Revenue", lbls, vals, ChartStyle.Bar));
            }

            btnSearch.Click += (s, e) => LoadData();
            btnReset.Click  += (s, e) =>
            {
                dtFrom.Value = DefaultDateFrom;
                dtTo.Value   = DefaultDateTo;
                cmbStatus.SelectedIndex = 0;
                LoadData();
            };

            LoadData();
        }

        // ════════════════════════════════════════════════════════════════
        //  2. INVENTORY STATUS
        // ════════════════════════════════════════════════════════════════

        private void RenderInventory()
        {
            var cmbCategory = MakeComboBox(new[] { "All", "Product", "Raw Material" });
            var cmbWarehouse = MakeComboBox(new[] { "All" }); // populated after load
            var chkBelowReorder = new CheckBox
            {
                Text      = "Below Reorder Level Only",
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                Checked   = false
            };

            var btnSearch = MakeFilterButton("Search", Palette.Primary, Color.White);
            var btnReset  = MakeFilterButton("Reset",  Color.FromArgb(241, 244, 249), Color.FromArgb(15, 31, 53));

            var btnRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnSearch.Width = 110; btnSearch.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            btnReset.Width  = 90;  btnReset.Anchor  = AnchorStyles.Left | AnchorStyles.Bottom;
            btnSearch.Location = new Point(0,   10);
            btnReset.Location  = new Point(118, 10);
            btnRow.Controls.Add(btnSearch);
            btnRow.Controls.Add(btnReset);

            SetFilterBar("Inventory Status",
                BuildFieldsRow(
                    ("Category",           cmbCategory),
                    ("Warehouse",          cmbWarehouse),
                    ("Filter",             chkBelowReorder)),
                btnRow);

            DataGridView dgv  = null;
            Panel dgvCard     = null;
            Panel chartCard   = null;
            List<InventoryStatusRowEntity> allRows = null;

            void LoadData()
            {
                var vm = _ctrl.GetInventoryReportVM();
                allRows = vm.InventoryRows ?? new List<InventoryStatusRowEntity>();

                var warehouses = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "All" };
                foreach (var r in allRows) warehouses.Add(r.WarehouseLocation);
                string prevWh = cmbWarehouse.SelectedItem?.ToString() ?? "All";
                cmbWarehouse.Items.Clear();
                foreach (var w in warehouses) cmbWarehouse.Items.Add(w);
                cmbWarehouse.SelectedItem = cmbWarehouse.Items.Contains(prevWh) ? prevWh : "All";

                var kpi = vm.InventoryKpi ?? new InventoryKpiEntity();
                var kpiRow = BuildKpiRow(
                    ("Total SKUs",       kpi.TotalSKUs.ToString(),         Color.FromArgb(29, 78, 216)),
                    ("Below Reorder",    kpi.BelowReorderCount.ToString(), Color.FromArgb(185, 28, 28)),
                    ("Products",         kpi.ProductCount.ToString(),      Color.FromArgb(6, 95, 70)),
                    ("Raw Materials",    kpi.RawMaterialCount.ToString(),  Color.FromArgb(91, 33, 182)));

                void FilterAndShow()
                {
                    string cat  = cmbCategory.SelectedItem?.ToString()  ?? "All";
                    string wh   = cmbWarehouse.SelectedItem?.ToString() ?? "All";
                    bool below  = chkBelowReorder.Checked;

                    var filtered = allRows.FindAll(r =>
                        (cat == "All"  || r.ItemCategory == cat) &&
                        (wh  == "All"  || r.WarehouseLocation == wh) &&
                        (!below        || r.BelowReorder));

                    dgv = BuildDgv();
                    dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colWhItemID",  HeaderText = "WH Item ID",   FillWeight = 12 });
                    dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItemID",    HeaderText = "Item ID",      FillWeight = 10 });
                    dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItemName",  HeaderText = "Item Name",    FillWeight = 22 });
                    dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCat",       HeaderText = "Category",     FillWeight = 12 });
                    dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colWarehouse", HeaderText = "Warehouse",    FillWeight = 16 });
                    dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStock",     HeaderText = "Stock",        FillWeight =  8 });
                    dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colReorder",   HeaderText = "Reorder Lvl", FillWeight =  8 });
                    dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAlert",     HeaderText = "Alert",        FillWeight =  8 });

                    foreach (var r in filtered)
                        dgv.Rows.Add(r.WarehouseItemID, r.ItemID, r.ItemName,
                            r.ItemCategory, r.WarehouseLocation,
                            r.CurrentStock, r.ReorderLevel,
                            r.BelowReorder ? "⚠ Low" : "OK");

                    var btnToggle = MakeFilterButton("View Chart", Color.FromArgb(235, 241, 255), Palette.Primary);
                    var btnExport = MakeFilterButton("Export CSV", Color.FromArgb(241, 244, 249), Color.FromArgb(15, 31, 53));
                    btnExport.Click += (s, e) => ExportGrid(dgv, "InventoryStatus");
                    btnToggle.Click += (s, e) =>
                    {
                        _inventoryChart = !_inventoryChart;
                        btnToggle.Text = _inventoryChart ? "View Table" : "View Chart";
                        if (chartCard == null)
                        {
                            var lbls = filtered.GetRange(0, Math.Min(8, filtered.Count)).ConvertAll(r => r.ItemName).ToArray();
                            var vals = filtered.GetRange(0, Math.Min(8, filtered.Count)).ConvertAll(r => (double)r.CurrentStock).ToArray();
                            chartCard = WrapInContentCard(BuildChartCard("Stock Levels (Top Items)", lbls, vals, ChartStyle.Column));
                        }
                        SwapContent(dgvCard, chartCard, _inventoryChart);
                    };

                    var tbl2 = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, BackColor = Color.White };
                    tbl2.RowStyles.Add(new RowStyle(SizeType.Absolute, 100f));
                    tbl2.RowStyles.Add(new RowStyle(SizeType.Absolute,  52f));
                    tbl2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                    tbl2.Controls.Add(kpiRow,   0, 0);
                    tbl2.Controls.Add(BuildSectionHeader("Inventory List", btnToggle, btnExport), 0, 1);
                    tbl2.Controls.Add(dgv, 0, 2);

                    dgvCard   = WrapInContentCard(tbl2);
                    chartCard = null;
                    _inventoryChart = false;
                    SwapContent(dgvCard, chartCard, false);
                }

                FilterAndShow();
                btnSearch.Click += (s, e) => FilterAndShow();
            }

            btnReset.Click += (s, e) =>
            {
                cmbCategory.SelectedIndex  = 0;
                cmbWarehouse.SelectedIndex = 0;
                chkBelowReorder.Checked    = false;
                LoadData();
            };

            LoadData();
        }

        // ════════════════════════════════════════════════════════════════
        //  3. PROCUREMENT SUMMARY
        // ════════════════════════════════════════════════════════════════

        private void RenderProcurement()
        {
            var dtFrom    = MakeDatePicker(DefaultDateFrom);
            var dtTo      = MakeDatePicker(DefaultDateTo);
            var cmbStatus = MakeComboBox(new[] { "All", "Pending", "Processing", "Completed", "Cancelled" });

            var btnSearch = MakeFilterButton("Search", Palette.Primary, Color.White);
            var btnReset  = MakeFilterButton("Reset",  Color.FromArgb(241, 244, 249), Color.FromArgb(15, 31, 53));

            var btnRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnSearch.Width = 110; btnSearch.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            btnReset.Width  = 90;  btnReset.Anchor  = AnchorStyles.Left | AnchorStyles.Bottom;
            btnSearch.Location = new Point(0,   10);
            btnReset.Location  = new Point(118, 10);
            btnRow.Controls.Add(btnSearch);
            btnRow.Controls.Add(btnReset);

            SetFilterBar("Procurement Summary",
                BuildFieldsRow(
                    ("Date From", dtFrom),
                    ("Date To",   dtTo),
                    ("PO Status", cmbStatus)),
                btnRow);

            DataGridView dgv = null;
            Panel dgvCard    = null;
            Panel chartCard  = null;

            void LoadData()
            {
                var vm = _ctrl.GetProcurementReportVM(dtFrom.Value, dtTo.Value,
                    cmbStatus.SelectedItem?.ToString() == "All" ? null : cmbStatus.SelectedItem?.ToString());

                var kpi  = vm.ProcKpi ?? new ProcurementKpiEntity();
                var rows = vm.ProcurementRows ?? new List<ProcurementRowEntity>();

                var kpiRow = BuildKpiRow(
                    ("Total POs",         kpi.TotalPOs.ToString(),          Color.FromArgb(29, 78, 216)),
                    ("Total Spend",        $"HKD {kpi.TotalSpend:N0}",      Color.FromArgb(185, 28, 28)),
                    ("Completed POs",      kpi.CompletedPOs.ToString(),     Color.FromArgb(6, 95, 70)),
                    ("Pending POs",        kpi.PendingPOs.ToString(),       Color.FromArgb(146, 64, 14)),
                    ("Unique Suppliers",   kpi.UniqueSuppliers.ToString(),  Color.FromArgb(91, 33, 182)));

                dgv = BuildDgv();
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPoID",     HeaderText = "PO ID",         FillWeight = 12 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSupplier", HeaderText = "Supplier",      FillWeight = 22 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPoStatus", HeaderText = "PO Status",     FillWeight = 12 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRtStatus", HeaderText = "Receipt",       FillWeight = 12 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",     HeaderText = "Order Date",    FillWeight = 12 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal",    HeaderText = "Total Amount",  FillWeight = 14 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItems",    HeaderText = "Items",         FillWeight =  6 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colReqID",    HeaderText = "Request ID",    FillWeight = 10 });

                foreach (var r in rows)
                    dgv.Rows.Add(r.PurchaseOrderID, r.SupplierName, r.PurchaseStatus,
                        r.ReceiptStatus, r.OrderDate.ToString("yyyy-MM-dd"),
                        $"HKD {r.TotalAmount:N0}", r.ItemCount, r.RequestID);

                var btnToggle = MakeFilterButton("View Chart", Color.FromArgb(235, 241, 255), Palette.Primary);
                var btnExport = MakeFilterButton("Export CSV", Color.FromArgb(241, 244, 249), Color.FromArgb(15, 31, 53));
                btnExport.Click += (s, e) => ExportGrid(dgv, "ProcurementSummary");
                btnToggle.Click += (s, e) =>
                {
                    _procurementChart = !_procurementChart;
                    btnToggle.Text = _procurementChart ? "View Table" : "View Chart";
                    if (chartCard == null)
                    {
                        // Bar chart: spend grouped by supplier
                        var grp = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                        foreach (var r in rows) { grp.TryGetValue(r.SupplierName, out double v); grp[r.SupplierName] = v + r.TotalAmount; }
                        var lbls = new List<string>(grp.Keys).ToArray();
                        var vals = new List<double>(grp.Values).ToArray();
                        chartCard = WrapInContentCard(BuildChartCard("Spend by Supplier", lbls, vals, ChartStyle.Bar));
                    }
                    SwapContent(dgvCard, chartCard, _procurementChart);
                };

                var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, BackColor = Color.White };
                tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 100f));
                tbl.RowStyles.Add(new RowStyle(SizeType.Absolute,  52f));
                tbl.RowStyles.Add(new RowStyle(SizeType.Percent,  100f));
                tbl.Controls.Add(kpiRow, 0, 0);
                tbl.Controls.Add(BuildSectionHeader("Purchase Order List", btnToggle, btnExport), 0, 1);
                tbl.Controls.Add(dgv, 0, 2);

                dgvCard   = WrapInContentCard(tbl);
                chartCard = null;
                _procurementChart = false;
                SwapContent(dgvCard, chartCard, false);
            }

            btnSearch.Click += (s, e) => LoadData();
            btnReset.Click  += (s, e) =>
            {
                dtFrom.Value = DefaultDateFrom;
                dtTo.Value   = DefaultDateTo;
                cmbStatus.SelectedIndex = 0;
                LoadData();
            };

            LoadData();
        }

        // ════════════════════════════════════════════════════════════════
        //  4. LOGISTICS OVERVIEW
        // ════════════════════════════════════════════════════════════════

        private void RenderLogistics()
        {
            var dtFrom    = MakeDatePicker(DefaultDateFrom);
            var dtTo      = MakeDatePicker(DefaultDateTo);
            var cmbStatus = MakeComboBox(new[] { "All", "Pending", "In Transit", "Delivered", "Cancelled" });

            var btnSearch = MakeFilterButton("Search", Palette.Primary, Color.White);
            var btnReset  = MakeFilterButton("Reset",  Color.FromArgb(241, 244, 249), Color.FromArgb(15, 31, 53));

            var btnRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnSearch.Width = 110; btnSearch.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            btnReset.Width  = 90;  btnReset.Anchor  = AnchorStyles.Left | AnchorStyles.Bottom;
            btnSearch.Location = new Point(0,   10);
            btnReset.Location  = new Point(118, 10);
            btnRow.Controls.Add(btnSearch);
            btnRow.Controls.Add(btnReset);

            SetFilterBar("Logistics Overview",
                BuildFieldsRow(
                    ("Date From",        dtFrom),
                    ("Date To",          dtTo),
                    ("Delivery Status",  cmbStatus)),
                btnRow);

            DataGridView dgv = null;
            Panel dgvCard    = null;
            Panel chartCard  = null;

            void LoadData()
            {
                var vm = _ctrl.GetLogisticsReportVM(dtFrom.Value, dtTo.Value,
                    cmbStatus.SelectedItem?.ToString() == "All" ? null : cmbStatus.SelectedItem?.ToString());

                var kpi  = vm.LogKpi ?? new LogisticsKpiEntity();
                var rows = vm.LogisticsRows ?? new List<LogisticsRowEntity>();

                var kpiRow = BuildKpiRow(
                    ("Total Shipments", kpi.TotalShipments.ToString(), Color.FromArgb(29, 78, 216)),
                    ("Delivered",       kpi.Completed.ToString(),      Color.FromArgb(6, 95, 70)),
                    ("In Transit",      kpi.InTransit.ToString(),      Color.FromArgb(55, 48, 163)),
                    ("Pending",         kpi.Pending.ToString(),        Color.FromArgb(146, 64, 14)),
                    ("With Reply Slip", kpi.WithReplySlip.ToString(),  Color.FromArgb(91, 33, 182)));

                dgv = BuildDgv();
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDoID",     HeaderText = "DO ID",           FillWeight = 12 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSoID",     HeaderText = "Sales Order",     FillWeight = 12 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomer", HeaderText = "Customer",        FillWeight = 20 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",   HeaderText = "Status",          FillWeight = 14 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDriver",   HeaderText = "Driver",          FillWeight = 14 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",     HeaderText = "Delivery Date",   FillWeight = 12 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colHasDN",   HeaderText = "DN",              FillWeight =  6 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colHasRS",   HeaderText = "RS",              FillWeight =  6 });

                foreach (var r in rows)
                    dgv.Rows.Add(r.DeliveryOrderID, r.SalesOrderID, r.CustomerName,
                        r.DeliveryStatus, r.DriverName,
                        r.DeliveryDate.ToString("yyyy-MM-dd"),
                        r.HasDeliveryNote ? "✓" : "—",
                        r.HasReplySlip    ? "✓" : "—");

                var btnToggle = MakeFilterButton("View Chart", Color.FromArgb(235, 241, 255), Palette.Primary);
                var btnExport = MakeFilterButton("Export CSV", Color.FromArgb(241, 244, 249), Color.FromArgb(15, 31, 53));
                btnExport.Click += (s, e) => ExportGrid(dgv, "LogisticsOverview");
                btnToggle.Click += (s, e) =>
                {
                    _logisticsChart = !_logisticsChart;
                    btnToggle.Text = _logisticsChart ? "View Table" : "View Chart";
                    if (chartCard == null)
                    {
                        chartCard = WrapInContentCard(BuildChartCard(
                            "Shipment Status Distribution",
                            new[] { "Delivered", "In Transit", "Pending" },
                            new double[] { kpi.Completed, kpi.InTransit, kpi.Pending },
                            ChartStyle.Pie));
                    }
                    SwapContent(dgvCard, chartCard, _logisticsChart);
                };

                var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, BackColor = Color.White };
                tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 100f));
                tbl.RowStyles.Add(new RowStyle(SizeType.Absolute,  52f));
                tbl.RowStyles.Add(new RowStyle(SizeType.Percent,  100f));
                tbl.Controls.Add(kpiRow, 0, 0);
                tbl.Controls.Add(BuildSectionHeader("Delivery Order List", btnToggle, btnExport), 0, 1);
                tbl.Controls.Add(dgv, 0, 2);

                dgvCard   = WrapInContentCard(tbl);
                chartCard = null;
                _logisticsChart = false;
                SwapContent(dgvCard, chartCard, false);
            }

            btnSearch.Click += (s, e) => LoadData();
            btnReset.Click  += (s, e) =>
            {
                dtFrom.Value = DefaultDateFrom;
                dtTo.Value   = DefaultDateTo;
                cmbStatus.SelectedIndex = 0;
                LoadData();
            };

            LoadData();
        }

        // ════════════════════════════════════════════════════════════════
        //  5. AFTER-SERVICE SUMMARY
        // ════════════════════════════════════════════════════════════════

        private void RenderAfterService()
        {
            var dtFrom          = MakeDatePicker(DefaultDateFrom);
            var dtTo            = MakeDatePicker(DefaultDateTo);
            var cmbComplaintSt  = MakeComboBox(new[] { "All", "Pending", "In Progress", "Resolved", "Escalated" });

            var btnSearch = MakeFilterButton("Search", Palette.Primary, Color.White);
            var btnReset  = MakeFilterButton("Reset",  Color.FromArgb(241, 244, 249), Color.FromArgb(15, 31, 53));

            var btnRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnSearch.Width = 110; btnSearch.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            btnReset.Width  = 90;  btnReset.Anchor  = AnchorStyles.Left | AnchorStyles.Bottom;
            btnSearch.Location = new Point(0,   10);
            btnReset.Location  = new Point(118, 10);
            btnRow.Controls.Add(btnSearch);
            btnRow.Controls.Add(btnReset);

            SetFilterBar("After-Service Summary",
                BuildFieldsRow(
                    ("Date From",         dtFrom),
                    ("Date To",           dtTo),
                    ("Complaint Status",  cmbComplaintSt)),
                btnRow);

            DataGridView dgvC  = null;
            DataGridView dgvR  = null;
            Panel dgvCard      = null;
            Panel chartCard    = null;

            void LoadData()
            {
                var vm = _ctrl.GetAfterServiceReportVM(dtFrom.Value, dtTo.Value,
                    cmbComplaintSt.SelectedItem?.ToString() == "All" ? null : cmbComplaintSt.SelectedItem?.ToString());

                var kpi      = vm.AfterKpi      ?? new AfterServiceKpiEntity();
                var compRows = vm.ComplaintRows  ?? new List<ComplaintRowEntity>();
                var retRows  = vm.ReturnRows     ?? new List<ReturnOrderRowEntity>();

                var kpiRow = BuildKpiRow(
                    ("Total Complaints",  kpi.TotalComplaints.ToString(), Color.FromArgb(185, 28, 28)),
                    ("Open Complaints",   kpi.OpenComplaints.ToString(),  Color.FromArgb(146, 64, 14)),
                    ("Total Returns",     kpi.TotalReturns.ToString(),    Color.FromArgb(91, 33, 182)),
                    ("Total Refunded",    $"HKD {kpi.TotalRefunded:N0}", Color.FromArgb(29, 78, 216)));

                // Complaints grid
                dgvC = BuildDgv();
                dgvC.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCmpID",    HeaderText = "Complaint ID",   FillWeight = 14 });
                dgvC.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomer", HeaderText = "Customer",       FillWeight = 22 });
                dgvC.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSubject",  HeaderText = "Subject",        FillWeight = 28 });
                dgvC.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCStatus",  HeaderText = "Status",         FillWeight = 12 });
                dgvC.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",     HeaderText = "Date",           FillWeight = 12 });
                dgvC.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrderID",  HeaderText = "Order ID",       FillWeight = 12 });
                foreach (var r in compRows)
                    dgvC.Rows.Add(r.ComplaintID, r.CustomerName, r.Subject,
                        r.ComplaintStatus, r.ComplaintDate.ToString("yyyy-MM-dd"), r.OrderID);

                // Returns grid
                dgvR = BuildDgv();
                dgvR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRetID",    HeaderText = "Return ID",      FillWeight = 14 });
                dgvR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSoID",     HeaderText = "Sales Order",    FillWeight = 14 });
                dgvR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomer", HeaderText = "Customer",       FillWeight = 20 });
                dgvR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colReason",   HeaderText = "Reason",         FillWeight = 22 });
                dgvR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRefund",   HeaderText = "Refund Amount",  FillWeight = 14 });
                dgvR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRStatus",  HeaderText = "Status",         FillWeight = 10 });
                dgvR.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",     HeaderText = "Date",           FillWeight = 10 });
                foreach (var r in retRows)
                    dgvR.Rows.Add(r.ReturnOrderID, r.SalesOrderID, r.CustomerName,
                        r.Reason, $"HKD {r.RefundAmount:N0}", r.ReturnStatus,
                        r.ReturnDate.ToString("yyyy-MM-dd"));

                var btnToggle = MakeFilterButton("View Chart", Color.FromArgb(235, 241, 255), Palette.Primary);
                var btnExportC = MakeFilterButton("Export Complaints", Color.FromArgb(241, 244, 249), Color.FromArgb(15, 31, 53));
                var btnExportR = MakeFilterButton("Export Returns",    Color.FromArgb(241, 244, 249), Color.FromArgb(15, 31, 53));
                btnExportC.Width = 160;
                btnExportR.Width = 150;
                btnExportC.Click += (s, e) => ExportGrid(dgvC, "Complaints");
                btnExportR.Click += (s, e) => ExportGrid(dgvR, "ReturnOrders");
                btnToggle.Click += (s, e) =>
                {
                    _afterServiceChart = !_afterServiceChart;
                    btnToggle.Text = _afterServiceChart ? "View Table" : "View Chart";
                    if (chartCard == null)
                    {
                        chartCard = WrapInContentCard(BuildChartCard(
                            "After-Service Overview",
                            new[] { "Total Complaints", "Open Complaints", "Total Returns" },
                            new double[] { kpi.TotalComplaints, kpi.OpenComplaints, kpi.TotalReturns },
                            ChartStyle.Column));
                    }
                    SwapContent(dgvCard, chartCard, _afterServiceChart);
                };

                // Two-section layout: Complaints on top, Returns on bottom
                var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, ColumnCount = 1, BackColor = Color.White };
                tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 100f));  // KPI
                tbl.RowStyles.Add(new RowStyle(SizeType.Absolute,  52f));  // Complaints header
                tbl.RowStyles.Add(new RowStyle(SizeType.Percent,   50f));  // Complaints grid
                tbl.RowStyles.Add(new RowStyle(SizeType.Absolute,  52f));  // Returns header
                tbl.RowStyles.Add(new RowStyle(SizeType.Percent,   50f));  // Returns grid

                var btnExportPanel = new Panel { Dock = DockStyle.Right, Width = 330, BackColor = Color.Transparent };
                btnExportC.Dock = DockStyle.Right;
                btnExportR.Dock = DockStyle.Right;
                btnExportPanel.Controls.Add(btnExportR);
                btnExportPanel.Controls.Add(btnExportC);

                tbl.Controls.Add(kpiRow, 0, 0);
                tbl.Controls.Add(BuildSectionHeader("Complaint List",    btnToggle, btnExportC), 0, 1);
                tbl.Controls.Add(dgvC, 0, 2);
                tbl.Controls.Add(BuildSectionHeader("Return Order List", null,       btnExportR), 0, 3);
                tbl.Controls.Add(dgvR, 0, 4);

                dgvCard   = WrapInContentCard(tbl);
                chartCard = null;
                _afterServiceChart = false;
                SwapContent(dgvCard, chartCard, false);
            }

            btnSearch.Click += (s, e) => LoadData();
            btnReset.Click  += (s, e) =>
            {
                dtFrom.Value = DefaultDateFrom;
                dtTo.Value   = DefaultDateTo;
                cmbComplaintSt.SelectedIndex = 0;
                LoadData();
            };

            LoadData();
        }

        // ════════════════════════════════════════════════════════════════
        //  6. FINANCE OVERVIEW
        // ════════════════════════════════════════════════════════════════

        private void RenderFinance()
        {
            var dtFrom    = MakeDatePicker(DefaultDateFrom);
            var dtTo      = MakeDatePicker(DefaultDateTo);
            var cmbType   = MakeComboBox(new[] { "All", "Revenue", "Expense", "Refund" });

            var btnSearch = MakeFilterButton("Search", Palette.Primary, Color.White);
            var btnReset  = MakeFilterButton("Reset",  Color.FromArgb(241, 244, 249), Color.FromArgb(15, 31, 53));

            var btnRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnSearch.Width = 110; btnSearch.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            btnReset.Width  = 90;  btnReset.Anchor  = AnchorStyles.Left | AnchorStyles.Bottom;
            btnSearch.Location = new Point(0,   10);
            btnReset.Location  = new Point(118, 10);
            btnRow.Controls.Add(btnSearch);
            btnRow.Controls.Add(btnReset);

            SetFilterBar("Finance Overview",
                BuildFieldsRow(
                    ("Date From",          dtFrom),
                    ("Date To",            dtTo),
                    ("Transaction Type",   cmbType)),
                btnRow);

            DataGridView dgv = null;
            Panel dgvCard    = null;
            Panel chartCard  = null;

            void LoadData()
            {
                var vm = _ctrl.GetFinanceReportVM(dtFrom.Value, dtTo.Value,
                    cmbType.SelectedItem?.ToString() == "All" ? null : cmbType.SelectedItem?.ToString());

                var kpi  = vm.FinanceKpi ?? new FinanceKpiEntity();
                var rows = vm.FinanceRows ?? new List<FinanceTransactionRowEntity>();

                var kpiRow = BuildKpiRow(
                    ("Sales Revenue",    $"HKD {kpi.TotalSalesRevenue:N0}",     Color.FromArgb(6,   95,  70)),
                    ("Procurement Spend",$"HKD {kpi.TotalProcurementSpend:N0}", Color.FromArgb(185, 28,  28)),
                    ("Total Refunds",    $"HKD {kpi.TotalRefunds:N0}",          Color.FromArgb(146, 64,  14)),
                    ("AR Outstanding",   $"HKD {kpi.AROutstanding:N0}",         Color.FromArgb(29,  78, 216)),
                    ("AP Outstanding",   $"HKD {kpi.APOutstanding:N0}",         Color.FromArgb(91,  33, 182)));

                dgv = BuildDgv();
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTxID",   HeaderText = "Transaction ID",  FillWeight = 14 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "Type",            FillWeight = 12 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAmt",    HeaderText = "Amount",          FillWeight = 14 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",   HeaderText = "Date",            FillWeight = 12 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDoc",    HeaderText = "Document Type",   FillWeight = 16 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMethod", HeaderText = "Payment Method",  FillWeight = 14 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAppr",   HeaderText = "Approval",        FillWeight = 10 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLinked", HeaderText = "Linked Document", FillWeight = 14 });

                foreach (var r in rows)
                    dgv.Rows.Add(r.TransactionID, r.TransactionType,
                        $"HKD {r.Amount:N0}", r.TransactionDate.ToString("yyyy-MM-dd"),
                        r.DocumentType, r.PaymentMethod, r.ApprovalStatus, r.LinkedDocument);

                var btnToggle = MakeFilterButton("View Chart", Color.FromArgb(235, 241, 255), Palette.Primary);
                var btnExport = MakeFilterButton("Export CSV", Color.FromArgb(241, 244, 249), Color.FromArgb(15, 31, 53));
                btnExport.Click += (s, e) => ExportGrid(dgv, "FinanceOverview");
                btnToggle.Click += (s, e) =>
                {
                    _financeChart = !_financeChart;
                    btnToggle.Text = _financeChart ? "View Table" : "View Chart";
                    if (chartCard == null)
                    {
                        chartCard = WrapInContentCard(BuildChartCard(
                            "Finance Overview",
                            new[] { "Sales Revenue", "Procurement Spend", "Total Refunds", "AR Outstanding", "AP Outstanding" },
                            new double[] { kpi.TotalSalesRevenue, kpi.TotalProcurementSpend, kpi.TotalRefunds, kpi.AROutstanding, kpi.APOutstanding },
                            ChartStyle.Column));
                    }
                    SwapContent(dgvCard, chartCard, _financeChart);
                };

                var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, BackColor = Color.White };
                tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 100f));
                tbl.RowStyles.Add(new RowStyle(SizeType.Absolute,  52f));
                tbl.RowStyles.Add(new RowStyle(SizeType.Percent,  100f));
                tbl.Controls.Add(kpiRow, 0, 0);
                tbl.Controls.Add(BuildSectionHeader("Transaction List", btnToggle, btnExport), 0, 1);
                tbl.Controls.Add(dgv, 0, 2);

                dgvCard   = WrapInContentCard(tbl);
                chartCard = null;
                _financeChart = false;
                SwapContent(dgvCard, chartCard, false);
            }

            btnSearch.Click += (s, e) => LoadData();
            btnReset.Click  += (s, e) =>
            {
                dtFrom.Value = DefaultDateFrom;
                dtTo.Value   = DefaultDateTo;
                cmbType.SelectedIndex = 0;
                LoadData();
            };

            LoadData();
        }
    }
}
