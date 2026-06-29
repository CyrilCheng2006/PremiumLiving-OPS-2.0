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
    ///
    /// Filter Bar structure (HGR-exact):
    ///   pnlFilterOuter  Height=300, Padding=(20,14,20,8)
    ///   └─ white CardPanel (Fill)
    ///      └─ tblCard  Padding=(18,14,18,14)
    ///         ├─ Row 0  60 px  — title + divider
    ///         ├─ Row 1 125 px  — filter fields   (MakeCell caption=40px Absolute)
    ///         └─ Row 2  65 px  — action buttons  (vertically centred via Resize)
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
        //  FILTER BAR BUILDER  — HGR baseline exact
        //
        //  pnlFilterOuter  Height=300, Padding=(20,14,20,8)   [Designer]
        //  white card  Fill
        //  tblCard  Padding=(18,14,18,14)  3 rows:
        //    Row 0  Absolute  60 px  — title label + 1px divider
        //    Row 1  Absolute 125 px  — filter fields
        //    Row 2  Absolute  65 px  — action buttons (vertically centred)
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
                Padding         = new Padding(18, 14, 18, 14)   // HGR tblCard exact
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute,  60f));  // title   — HGR Row0
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 125f));  // fields  — HGR Row1
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute,  65f));  // buttons — HGR Row2

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
        //  MakeCell  — mirrors HGR MakeCell exactly
        //  2-row cell: row0=40px Absolute (caption), row1=Percent 70% (control)
        //  rightPad=true adds Padding(0,0,12,0) on all but last column
        // ────────────────────────────────────────────────────────────────
        private static TableLayoutPanel MakeCell(
            string caption, Control ctrl, bool rightPad = true)
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
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));   // HGR: 40px caption
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent,  70f));   // HGR: 70% control

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

        // ────────────────────────────────────────────────────────────────
        //  BuildFieldsRow  — wraps MakeCell columns in a Percent-split TLP
        //  Each column gets SizeType.Percent(100/n) — no Absolute columns
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

        // ────────────────────────────────────────────────────────────────
        //  BuildDateRangeRow
        //  col 0 : dtpFrom  caption="Date Range"   28%
        //  col 1 : "to" separator                   8%
        //  col 2 : dtpTo    no caption             28%
        //  col 3+: extra cols                    share 36%
        //
        //  All cells use same 2-row structure as MakeCell:
        //    row 0  40px Absolute — caption (or blank spacer)
        //    row 1  70% Percent   — control / "to" label
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

            // ── col 0: From DTP — caption "Date Range" ─────────────────
            var cellFrom = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                RowCount        = 2,
                ColumnCount     = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(0, 0, 8, 0)
            };
            cellFrom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            cellFrom.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));  // HGR: 40px
            cellFrom.RowStyles.Add(new RowStyle(SizeType.Percent,  70f));
            cellFrom.Controls.Add(new Label
            {
                Text      = "Date Range",
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft,
                Padding   = new Padding(0, 0, 0, 2)
            }, 0, 0);
            dtpFrom.Dock = DockStyle.Fill;
            cellFrom.Controls.Add(dtpFrom, 0, 1);
            tbl.Controls.Add(cellFrom, 0, 0);

            // ── col 1: "to" separator ──────────────────────────────────
            var cellSep = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                RowCount        = 2,
                ColumnCount     = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = Padding.Empty
            };
            cellSep.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            cellSep.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));  // blank spacer
            cellSep.RowStyles.Add(new RowStyle(SizeType.Percent,  70f));  // "to" label
            cellSep.Controls.Add(new Label { Text = "", Dock = DockStyle.Fill }, 0, 0);
            cellSep.Controls.Add(new Label
            {
                Text      = "to",
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(98, 112, 135),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock      = DockStyle.Fill,
                AutoSize  = false
            }, 0, 1);
            tbl.Controls.Add(cellSep, 1, 0);

            // ── col 2: To DTP — blank caption spacer ──────────────────
            var cellTo = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                RowCount        = 2,
                ColumnCount     = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(8, 0, 0, 0)
            };
            cellTo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            cellTo.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));  // HGR: 40px
            cellTo.RowStyles.Add(new RowStyle(SizeType.Percent,  70f));
            cellTo.Controls.Add(new Label { Text = "", Dock = DockStyle.Fill }, 0, 0);
            dtpTo.Dock = DockStyle.Fill;
            cellTo.Controls.Add(dtpTo, 0, 1);
            tbl.Controls.Add(cellTo, 2, 0);

            // ── extra cols ─────────────────────────────────────────────
            if (extraCols != null)
            {
                for (int i = 0; i < extraCols.Length; i++)
                {
                    bool last = i == extraCols.Length - 1;
                    tbl.Controls.Add(
                        MakeCell(extraCols[i].caption, extraCols[i].ctrl,
                                 rightPad: !last),
                        3 + i, 0);
                }
            }
            return tbl;
        }

        // ────────────────────────────────────────────────────────────────
        //  BuildButtonsRow  — mirrors HGR pnlBtns
        //  Layout: [Apply] [Reset]  ·········  [Toggle] [Export]
        //  Buttons vertically centred in row via Resize event (HGR pattern)
        //  BtnW=210, BtnH=50, Gap=8
        // ────────────────────────────────────────────────────────────────
        private static Panel BuildButtonsRow(
            Button btnApply, Button btnReset,
            Button btnToggleView, Button btnExport)
        {
            const int BtnW = 210;
            const int BtnH =  50;  // fits comfortably inside 65px row
            const int Gap  =   8;

            btnApply.Size      = new Size(BtnW, BtnH);
            btnReset.Size      = new Size(BtnW, BtnH);
            btnToggleView.Size = new Size(BtnW, BtnH);
            btnExport.Size     = new Size(BtnW, BtnH);

            // Left group: Apply + Reset — absolute positions, Y centred on Resize
            var pnlLeft = new Panel
            {
                Dock      = DockStyle.Left,
                Width     = BtnW * 2 + Gap,
                BackColor = Color.Transparent
            };
            pnlLeft.Controls.Add(btnApply);
            pnlLeft.Controls.Add(btnReset);
            pnlLeft.Resize += (s, e) =>
            {
                int top = Math.Max(0, (pnlLeft.Height - BtnH) / 2);
                btnApply.Location = new Point(0,          top);
                btnReset.Location = new Point(BtnW + Gap, top);
            };

            // Right group: Toggle + Export — right-aligned, Y centred on Resize
            var pnlRight = new Panel
            {
                Dock      = DockStyle.Right,
                Width     = BtnW * 2 + Gap,
                BackColor = Color.Transparent
            };
            pnlRight.Controls.Add(btnToggleView);
            pnlRight.Controls.Add(btnExport);
            pnlRight.Resize += (s, e) =>
            {
                int top = Math.Max(0, (pnlRight.Height - BtnH) / 2);
                btnToggleView.Location = new Point(0,          top);
                btnExport.Location     = new Point(BtnW + Gap, top);
            };

            var pnl = new Panel { BackColor = Color.Transparent, Dock = DockStyle.Fill };
            pnl.Controls.Add(pnlRight);   // Right first — Fill won't push it
            pnl.Controls.Add(pnlLeft);
            return pnl;
        }

        // ════════════════════════════════════════════════════════════════
        //  GRID CARD BUILDER
        // ════════════════════════════════════════════════════════════════

        private Panel BuildGridCard(DataGridView dgv)
        {
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            inner.Paint += PaintCardBorder;
            inner.Controls.Add(dgv);

            var outer = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Palette.BgPage,
                Padding   = new Padding(20, 6, 20, 10)
            };
            outer.Controls.Add(inner);
            return outer;
        }

        // ════════════════════════════════════════════════════════════════
        //  COMMON DATAGRIDVIEW FACTORY
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
        //  BUTTON FACTORIES  — HGR-aligned
        // ════════════════════════════════════════════════════════════════

        private static Button MakePrimaryBtn(string text)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(55, 48, 163),   // Indigo-800
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize         = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(49, 46, 129);  // Indigo-900
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(38, 35, 100);
            return b;
        }

        private static Button MakeOutlineBtn(string text)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(71, 85, 105),   // Slate-600
                BackColor = Color.FromArgb(241, 245, 249), // Slate-100
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize         = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(226, 232, 240);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(203, 213, 225);
            return b;
        }

        private static Button MakeAmberBtn(string text)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(217, 119, 6),   // Amber-600
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize         = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(180, 95, 4);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(146, 75, 2);
            return b;
        }

        private static Button MakeSkyBtn(string text)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(2, 132, 199),   // Sky-600
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize         = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(3, 105, 161);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(7, 89, 133);
            return b;
        }

        private static Button MakeExportBtn(string text)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(6, 95, 70),     // Emerald-800
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize         = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(4, 78, 56);   // Emerald-900
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(2, 60, 43);
            return b;
        }

        // ════════════════════════════════════════════════════════════════
        //  TOGGLE HELPER
        //  Flips btnToggleView between Amber (show chart) / Sky (show table)
        // ════════════════════════════════════════════════════════════════

        private static void ApplyToggleStyle(Button btn, bool currentlyChart)
        {
            if (currentlyChart)
            {
                // currently showing chart → clicking switches to table
                btn.Text      = "\U0001F4CB  Table";
                btn.BackColor = Color.FromArgb(2, 132, 199);   // Sky
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(3, 105, 161);
            }
            else
            {
                // currently showing table → clicking switches to chart
                btn.Text      = "\U0001F4CA  Chart";
                btn.BackColor = Color.FromArgb(217, 119, 6);   // Amber
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(180, 95, 4);
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  REPORT RENDERERS
        // ════════════════════════════════════════════════════════════════

        // ── 0. Sales Performance ────────────────────────────────────────
        private void RenderSales()
        {
            var dtpFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-3), Font = new Font("Segoe UI", 12f) };
            var dtpTo   = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today,               Font = new Font("Segoe UI", 12f) };
            var cboCategory = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            cboCategory.Items.AddRange(new object[] { "All Categories", "Furniture", "Lighting", "Textiles", "Accessories", "Outdoor" });
            cboCategory.SelectedIndex = 0;

            var btnApply  = MakePrimaryBtn("\U0001F50D  Apply");
            var btnReset  = MakeOutlineBtn("\u21BA  Reset");
            var btnToggle = MakeAmberBtn("\U0001F4CA  Chart");
            var btnExport = MakeExportBtn("\U0001F4E4  Export");
            ApplyToggleStyle(btnToggle, _salesChart);

            var dgv = MakeDgv();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",     HeaderText = "DATE",          FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrder",    HeaderText = "ORDER ID",      FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCust",     HeaderText = "CUSTOMER",      FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCategory", HeaderText = "CATEGORY",      FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQty",      HeaderText = "QTY",           FillWeight =  8 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRevenue",  HeaderText = "REVENUE",       FillWeight = 13 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",   HeaderText = "ORDER STATUS",  FillWeight = 14 });
            dgv.CellFormatting += DgvCellFormatting;

            btnApply.Click  += (s, e) => LoadSalesData(dgv, dtpFrom, dtpTo, cboCategory);
            btnReset.Click  += (s, e) => { dtpFrom.Value = DateTime.Today.AddMonths(-3); dtpTo.Value = DateTime.Today; cboCategory.SelectedIndex = 0; LoadSalesData(dgv, dtpFrom, dtpTo, cboCategory); };
            btnToggle.Click += (s, e) => { _salesChart = !_salesChart; ApplyToggleStyle(btnToggle, _salesChart); };
            btnExport.Click += (s, e) => ExportGrid(dgv, "SalesPerformance");

            SetFilterBar("Sales Performance",
                BuildDateRangeRow(dtpFrom, dtpTo, ("Category", cboCategory)),
                BuildButtonsRow(btnApply, btnReset, btnToggle, btnExport));

            pnlContent.Controls.Add(BuildGridCard(dgv));
            LoadSalesData(dgv, dtpFrom, dtpTo, cboCategory);
        }

        // ── 1. Inventory Status ─────────────────────────────────────────
        private void RenderInventory()
        {
            var txtKeyword = new TextBox { Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "Material ID / Item Name" };
            var cboWarehouse = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            cboWarehouse.Items.AddRange(new object[] { "All Warehouses", "WH-001 Central", "WH-002 North", "WH-003 South" });
            cboWarehouse.SelectedIndex = 0;
            var cboStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            cboStatus.Items.AddRange(new object[] { "All", "In Stock", "Low Stock", "Out of Stock" });
            cboStatus.SelectedIndex = 0;

            var btnApply  = MakePrimaryBtn("\U0001F50D  Apply");
            var btnReset  = MakeOutlineBtn("\u21BA  Reset");
            var btnToggle = MakeAmberBtn("\U0001F4CA  Chart");
            var btnExport = MakeExportBtn("\U0001F4E4  Export");
            ApplyToggleStyle(btnToggle, _inventoryChart);

            var dgv = MakeDgv();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMatID",  HeaderText = "MATERIAL ID",    FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName",   HeaderText = "ITEM NAME",      FillWeight = 25 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colWH",     HeaderText = "WAREHOUSE",      FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQty",    HeaderText = "QTY IN STOCK",   FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMin",    HeaderText = "MIN LEVEL",      FillWeight = 10 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "STOCK STATUS",   FillWeight = 13 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colVal",    HeaderText = "STOCK VALUE",    FillWeight = 10 });
            dgv.CellFormatting += DgvCellFormatting;

            btnApply.Click  += (s, e) => LoadInventoryData(dgv, txtKeyword, cboWarehouse, cboStatus);
            btnReset.Click  += (s, e) => { txtKeyword.Clear(); cboWarehouse.SelectedIndex = 0; cboStatus.SelectedIndex = 0; LoadInventoryData(dgv, txtKeyword, cboWarehouse, cboStatus); };
            btnToggle.Click += (s, e) => { _inventoryChart = !_inventoryChart; ApplyToggleStyle(btnToggle, _inventoryChart); };
            btnExport.Click += (s, e) => ExportGrid(dgv, "InventoryStatus");

            SetFilterBar("Inventory Status",
                BuildFieldsRow(("Keyword", txtKeyword), ("Warehouse", cboWarehouse), ("Stock Status", cboStatus)),
                BuildButtonsRow(btnApply, btnReset, btnToggle, btnExport));

            pnlContent.Controls.Add(BuildGridCard(dgv));
            LoadInventoryData(dgv, txtKeyword, cboWarehouse, cboStatus);
        }

        // ── 2. Procurement Summary ───────────────────────────────────────
        private void RenderProcurement()
        {
            var dtpFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-3), Font = new Font("Segoe UI", 12f) };
            var dtpTo   = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today,               Font = new Font("Segoe UI", 12f) };
            var cboStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            cboStatus.Items.AddRange(new object[] { "All", "Pending", "Processing", "Received", "Cancelled" });
            cboStatus.SelectedIndex = 0;

            var btnApply  = MakePrimaryBtn("\U0001F50D  Apply");
            var btnReset  = MakeOutlineBtn("\u21BA  Reset");
            var btnToggle = MakeAmberBtn("\U0001F4CA  Chart");
            var btnExport = MakeExportBtn("\U0001F4E4  Export");
            ApplyToggleStyle(btnToggle, _procurementChart);

            var dgv = MakeDgv();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPOID",    HeaderText = "PO ID",         FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSupplier",HeaderText = "SUPPLIER",      FillWeight = 25 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",    HeaderText = "ORDER DATE",    FillWeight = 13 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItems",   HeaderText = "ITEMS",         FillWeight =  8 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAmt",     HeaderText = "TOTAL AMOUNT",  FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",  HeaderText = "PO STATUS",     FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colExpected",HeaderText = "EXPECTED DATE", FillWeight = 10 });
            dgv.CellFormatting += DgvCellFormatting;

            btnApply.Click  += (s, e) => LoadProcurementData(dgv, dtpFrom, dtpTo, cboStatus);
            btnReset.Click  += (s, e) => { dtpFrom.Value = DateTime.Today.AddMonths(-3); dtpTo.Value = DateTime.Today; cboStatus.SelectedIndex = 0; LoadProcurementData(dgv, dtpFrom, dtpTo, cboStatus); };
            btnToggle.Click += (s, e) => { _procurementChart = !_procurementChart; ApplyToggleStyle(btnToggle, _procurementChart); };
            btnExport.Click += (s, e) => ExportGrid(dgv, "ProcurementSummary");

            SetFilterBar("Procurement Summary",
                BuildDateRangeRow(dtpFrom, dtpTo, ("PO Status", cboStatus)),
                BuildButtonsRow(btnApply, btnReset, btnToggle, btnExport));

            pnlContent.Controls.Add(BuildGridCard(dgv));
            LoadProcurementData(dgv, dtpFrom, dtpTo, cboStatus);
        }

        // ── 3. Logistics Overview ────────────────────────────────────────
        private void RenderLogistics()
        {
            var dtpFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-3), Font = new Font("Segoe UI", 12f) };
            var dtpTo   = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today,               Font = new Font("Segoe UI", 12f) };
            var cboStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            cboStatus.Items.AddRange(new object[] { "All", "Pending", "In Transit", "Delivered", "Cancelled" });
            cboStatus.SelectedIndex = 0;

            var btnApply  = MakePrimaryBtn("\U0001F50D  Apply");
            var btnReset  = MakeOutlineBtn("\u21BA  Reset");
            var btnToggle = MakeAmberBtn("\U0001F4CA  Chart");
            var btnExport = MakeExportBtn("\U0001F4E4  Export");
            ApplyToggleStyle(btnToggle, _logisticsChart);

            var dgv = MakeDgv();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colShipID",  HeaderText = "SHIPMENT ID",   FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrderID", HeaderText = "ORDER ID",      FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDest",    HeaderText = "DESTINATION",   FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCarrier", HeaderText = "CARRIER",       FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDepart",  HeaderText = "DEPART DATE",   FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colETA",     HeaderText = "ETA",           FillWeight = 10 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",  HeaderText = "STATUS",        FillWeight = 13 });
            dgv.CellFormatting += DgvCellFormatting;

            btnApply.Click  += (s, e) => LoadLogisticsData(dgv, dtpFrom, dtpTo, cboStatus);
            btnReset.Click  += (s, e) => { dtpFrom.Value = DateTime.Today.AddMonths(-3); dtpTo.Value = DateTime.Today; cboStatus.SelectedIndex = 0; LoadLogisticsData(dgv, dtpFrom, dtpTo, cboStatus); };
            btnToggle.Click += (s, e) => { _logisticsChart = !_logisticsChart; ApplyToggleStyle(btnToggle, _logisticsChart); };
            btnExport.Click += (s, e) => ExportGrid(dgv, "LogisticsOverview");

            SetFilterBar("Logistics Overview",
                BuildDateRangeRow(dtpFrom, dtpTo, ("Shipment Status", cboStatus)),
                BuildButtonsRow(btnApply, btnReset, btnToggle, btnExport));

            pnlContent.Controls.Add(BuildGridCard(dgv));
            LoadLogisticsData(dgv, dtpFrom, dtpTo, cboStatus);
        }

        // ── 4. After-Service Summary ─────────────────────────────────────
        private void RenderAfterService()
        {
            var dtpFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-3), Font = new Font("Segoe UI", 12f) };
            var dtpTo   = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today,               Font = new Font("Segoe UI", 12f) };
            var cboStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            cboStatus.Items.AddRange(new object[] { "All", "Pending", "Processing", "Completed", "Escalated", "Cancelled" });
            cboStatus.SelectedIndex = 0;

            var btnApply  = MakePrimaryBtn("\U0001F50D  Apply");
            var btnReset  = MakeOutlineBtn("\u21BA  Reset");
            var btnToggle = MakeAmberBtn("\U0001F4CA  Chart");
            var btnExport = MakeExportBtn("\U0001F4E4  Export");
            ApplyToggleStyle(btnToggle, _afterServiceChart);

            var dgv = MakeDgv();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colReqID",   HeaderText = "REQUEST ID",    FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCust",    HeaderText = "CUSTOMER",      FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colType",    HeaderText = "SERVICE TYPE",  FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",    HeaderText = "REQUEST DATE",  FillWeight = 13 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colResolved",HeaderText = "RESOLVED DATE", FillWeight = 13 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",  HeaderText = "STATUS",        FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSat",     HeaderText = "SATISFACTION",  FillWeight = 10 });
            dgv.CellFormatting += DgvCellFormatting;

            btnApply.Click  += (s, e) => LoadAfterServiceData(dgv, dtpFrom, dtpTo, cboStatus);
            btnReset.Click  += (s, e) => { dtpFrom.Value = DateTime.Today.AddMonths(-3); dtpTo.Value = DateTime.Today; cboStatus.SelectedIndex = 0; LoadAfterServiceData(dgv, dtpFrom, dtpTo, cboStatus); };
            btnToggle.Click += (s, e) => { _afterServiceChart = !_afterServiceChart; ApplyToggleStyle(btnToggle, _afterServiceChart); };
            btnExport.Click += (s, e) => ExportGrid(dgv, "AfterServiceSummary");

            SetFilterBar("After-Service Summary",
                BuildDateRangeRow(dtpFrom, dtpTo, ("Service Status", cboStatus)),
                BuildButtonsRow(btnApply, btnReset, btnToggle, btnExport));

            pnlContent.Controls.Add(BuildGridCard(dgv));
            LoadAfterServiceData(dgv, dtpFrom, dtpTo, cboStatus);
        }

        // ── 5. Finance Overview ──────────────────────────────────────────
        private void RenderFinance()
        {
            var dtpFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-3), Font = new Font("Segoe UI", 12f) };
            var dtpTo   = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today,               Font = new Font("Segoe UI", 12f) };
            var cboType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            cboType.Items.AddRange(new object[] { "All", "Revenue", "Expense", "Refund" });
            cboType.SelectedIndex = 0;

            var btnApply  = MakePrimaryBtn("\U0001F50D  Apply");
            var btnReset  = MakeOutlineBtn("\u21BA  Reset");
            var btnToggle = MakeAmberBtn("\U0001F4CA  Chart");
            var btnExport = MakeExportBtn("\U0001F4E4  Export");
            ApplyToggleStyle(btnToggle, _financeChart);

            var dgv = MakeDgv();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTxnID",  HeaderText = "TXN ID",        FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",   HeaderText = "DATE",          FillWeight = 13 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colType",   HeaderText = "TYPE",          FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDesc",   HeaderText = "DESCRIPTION",   FillWeight = 25 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAmt",    HeaderText = "AMOUNT",        FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRef",    HeaderText = "REFERENCE",     FillWeight = 20 });
            dgv.CellFormatting += DgvCellFormatting;

            btnApply.Click  += (s, e) => LoadFinanceData(dgv, dtpFrom, dtpTo, cboType);
            btnReset.Click  += (s, e) => { dtpFrom.Value = DateTime.Today.AddMonths(-3); dtpTo.Value = DateTime.Today; cboType.SelectedIndex = 0; LoadFinanceData(dgv, dtpFrom, dtpTo, cboType); };
            btnToggle.Click += (s, e) => { _financeChart = !_financeChart; ApplyToggleStyle(btnToggle, _financeChart); };
            btnExport.Click += (s, e) => ExportGrid(dgv, "FinanceOverview");

            SetFilterBar("Finance Overview",
                BuildDateRangeRow(dtpFrom, dtpTo, ("Transaction Type", cboType)),
                BuildButtonsRow(btnApply, btnReset, btnToggle, btnExport));

            pnlContent.Controls.Add(BuildGridCard(dgv));
            LoadFinanceData(dgv, dtpFrom, dtpTo, cboType);
        }

        // ════════════════════════════════════════════════════════════════
        //  DATA LOADERS
        // ════════════════════════════════════════════════════════════════

        private void LoadSalesData(DataGridView dgv, DateTimePicker from, DateTimePicker to, ComboBox cat)
        {
            dgv.Rows.Clear();
            try
            {
                var rows = _ctrl.GetSalesPerformance(from.Value, to.Value,
                    cat.SelectedIndex == 0 ? null : cat.SelectedItem?.ToString());
                foreach (var r in rows)
                    dgv.Rows.Add(r.Date, r.OrderId, r.Customer, r.Category, r.Qty, r.Revenue.ToString("C"), r.Status);
            }
            catch { }
        }

        private void LoadInventoryData(DataGridView dgv, TextBox kw, ComboBox wh, ComboBox st)
        {
            dgv.Rows.Clear();
            try
            {
                var rows = _ctrl.GetInventoryStatus(
                    kw.Text.Trim(),
                    wh.SelectedIndex == 0 ? null : wh.SelectedItem?.ToString(),
                    st.SelectedIndex == 0 ? null : st.SelectedItem?.ToString());
                foreach (var r in rows)
                    dgv.Rows.Add(r.MaterialId, r.ItemName, r.Warehouse, r.QtyInStock, r.MinLevel, r.StockStatus, r.StockValue.ToString("C"));
            }
            catch { }
        }

        private void LoadProcurementData(DataGridView dgv, DateTimePicker from, DateTimePicker to, ComboBox st)
        {
            dgv.Rows.Clear();
            try
            {
                var rows = _ctrl.GetProcurementSummary(from.Value, to.Value,
                    st.SelectedIndex == 0 ? null : st.SelectedItem?.ToString());
                foreach (var r in rows)
                    dgv.Rows.Add(r.PoId, r.Supplier, r.OrderDate, r.Items, r.TotalAmount.ToString("C"), r.Status, r.ExpectedDate);
            }
            catch { }
        }

        private void LoadLogisticsData(DataGridView dgv, DateTimePicker from, DateTimePicker to, ComboBox st)
        {
            dgv.Rows.Clear();
            try
            {
                var rows = _ctrl.GetLogisticsOverview(from.Value, to.Value,
                    st.SelectedIndex == 0 ? null : st.SelectedItem?.ToString());
                foreach (var r in rows)
                    dgv.Rows.Add(r.ShipmentId, r.OrderId, r.Destination, r.Carrier, r.DepartDate, r.Eta, r.Status);
            }
            catch { }
        }

        private void LoadAfterServiceData(DataGridView dgv, DateTimePicker from, DateTimePicker to, ComboBox st)
        {
            dgv.Rows.Clear();
            try
            {
                var rows = _ctrl.GetAfterServiceSummary(from.Value, to.Value,
                    st.SelectedIndex == 0 ? null : st.SelectedItem?.ToString());
                foreach (var r in rows)
                    dgv.Rows.Add(r.RequestId, r.Customer, r.ServiceType, r.RequestDate, r.ResolvedDate, r.Status, r.Satisfaction);
            }
            catch { }
        }

        private void LoadFinanceData(DataGridView dgv, DateTimePicker from, DateTimePicker to, ComboBox type)
        {
            dgv.Rows.Clear();
            try
            {
                var rows = _ctrl.GetFinanceOverview(from.Value, to.Value,
                    type.SelectedIndex == 0 ? null : type.SelectedItem?.ToString());
                foreach (var r in rows)
                    dgv.Rows.Add(r.TxnId, r.Date, r.Type, r.Description, r.Amount.ToString("C"), r.Reference);
            }
            catch { }
        }

        // ════════════════════════════════════════════════════════════════
        //  CELL FORMATTING  — shared status-pill renderer
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
                foreach (DataGridViewColumn col in dgv.Columns)
                    headers.Add(col.HeaderText);
                sb.AppendLine(string.Join(",", headers));
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    var cells = new List<string>();
                    foreach (DataGridViewCell cell in row.Cells)
                        cells.Add('"' + (cell.Value?.ToString() ?? "").Replace('"', '\u2019') + '"');
                    sb.AppendLine(string.Join(",", cells));
                }
                System.IO.File.WriteAllText(dlg.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                MessageBox.Show($"Exported to:\n{dlg.FileName}", "Export Successful",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed:\n{ex.Message}", "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  NAVIGATION / LOGOUT
        // ════════════════════════════════════════════════════════════════

        private void OnTopNavMenuItemClicked(object sender, string menuItem) { }
        private void btnLogout_Click(object sender, EventArgs e)
        {
            this.Hide();
            var login = new PremiumLivingOPS.Views.Authentication.LoginForm();
            login.Show();
        }
    }
}
