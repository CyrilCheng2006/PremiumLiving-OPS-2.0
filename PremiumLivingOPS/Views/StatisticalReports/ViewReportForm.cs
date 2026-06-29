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
    /// Rendering baseline: HandlingGoodsReceivedForm (Logistics Processing)
    ///   - Tab Bar:    pnlTabOuter  Height=69,  Padding=(20,4,20,0)
    ///   - Filter Bar: pnlFilterOuter Height=240, Padding=(20,14,20,8)
    ///     3-row tblCard (Percent-based rows, no Absolute overflow):
    ///       row0 = 26%  (title + divider)
    ///       row1 = 37%  (filter fields)
    ///       row2 = 37%  (action buttons, BtnH=44)
    ///
    /// Tab index map:
    ///   0 = Sales Performance      3 = Logistics Overview
    ///   1 = Inventory Status       4 = After-Service Summary
    ///   2 = Procurement Summary    5 = Finance Overview
    /// </summary>
    public partial class ViewReportForm : Form
    {
        private readonly StatisticalReportsController _ctrl = new StatisticalReportsController();
        private int    _activeTab         = -1;
        private bool   _salesChart        = false;
        private bool   _inventoryChart    = false;
        private bool   _procurementChart  = false;
        private bool   _logisticsChart    = false;
        private bool   _afterServiceChart = false;
        private bool   _financeChart      = false;
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

        public ViewReportForm()
        {
            InitializeComponent();
            _tabButtons = new Button[] { btnTab0, btnTab1, btnTab2, btnTab3, btnTab4, btnTab5 };
            this.Load += (s, e) => SwitchToReport(0);
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
        //
        //  Uses Percent-based rows so WinForms distributes height
        //  proportionally — no Absolute overflow regardless of outer size.
        //
        //  tblCard inside white CardPanel inside pnlFilterOuter (Height=240):
        //    Row 0 — title + divider   26% of card height
        //    Row 1 — filter fields     37% of card height
        //    Row 2 — action buttons    37% of card height
        //  tblCard Padding = (18, 10, 18, 10)
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
                Padding         = new Padding(18, 10, 18, 10)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            // Percent-based rows — never overflow regardless of outer Height
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 26f));  // title
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 37f));  // fields
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 37f));  // buttons

            // Row 0 — title + bottom divider
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

        // ────────────────────────────────────────────────────────────────
        //  BuildFieldsRow  — single horizontal row of labelled controls.
        //  Each col is Percent(100/n) so no Absolute columns steal space.
        // ────────────────────────────────────────────────────────────────
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
                Padding         = new Padding(0, 4, 0, 4)
            };
            for (int i = 0; i < n; i++)
                tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / n));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            for (int i = 0; i < cols.Length; i++)
            {
                var (caption, ctrl) = cols[i];
                bool last = i == cols.Length - 1;

                var cell = new TableLayoutPanel
                {
                    Dock            = DockStyle.Fill,
                    RowCount        = 2,
                    ColumnCount     = 1,
                    BackColor       = Color.Transparent,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding         = last ? Padding.Empty : new Padding(0, 0, 12, 0)
                };
                cell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                cell.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));
                cell.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                if (!string.IsNullOrEmpty(caption))
                    cell.Controls.Add(new Label
                    {
                        Text      = caption,
                        Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                        ForeColor = Color.FromArgb(98, 112, 135),
                        Dock      = DockStyle.Fill,
                        TextAlign = ContentAlignment.BottomLeft
                    }, 0, 0);

                ctrl.Dock = DockStyle.Fill;
                cell.Controls.Add(ctrl, 0, 1);
                tbl.Controls.Add(cell, i, 0);
            }
            return tbl;
        }

        // ────────────────────────────────────────────────────────────────
        //  BuildDateRangeRow
        //  All columns Percent-based. Layout (L→R):
        //    col 0 : dtpFrom  caption="Date Range"   28%
        //    col 1 : “to” separator                   8%
        //    col 2 : dtpTo    no caption              28%
        //    col 3+: extra cols                  share 36%
        // ────────────────────────────────────────────────────────────────
        private static Panel BuildDateRangeRow(
            DateTimePicker dtpFrom,
            DateTimePicker dtpTo,
            params (string caption, Control ctrl)[] extraCols)
        {
            int extraCount = extraCols == null ? 0 : extraCols.Length;
            int totalCols  = 3 + extraCount;
            float extraPct = extraCount > 0 ? 36f / extraCount : 0f;

            var tbl = new TableLayoutPanel
            {
                Dock