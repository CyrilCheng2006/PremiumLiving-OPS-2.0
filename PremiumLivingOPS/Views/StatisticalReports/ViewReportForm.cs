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
    public partial class ViewReportForm : Form
    {
        private readonly StatisticalReportsController _ctrl = new StatisticalReportsController();
        private int _activeTab = -1;
        private bool _salesChart = false;
        private bool _inventoryChart = false;
        private bool _procurementChart = false;
        private bool _logisticsChart = false;
        private bool _afterServiceChart = false;
        private bool _financeChart = false;
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

        private void SwitchToReport(int tabIndex)
        {
            if (_activeTab == tabIndex && pnlContent.Controls.Count > 0) return;
            _activeTab = tabIndex;

            pnlContent.SuspendLayout();
            pnlContent.Controls.Clear();
            HighlightTab(tabIndex);
            pnlTabOuter.Invalidate();

            switch (tabIndex)
            {
                case 0: RenderSales(); break;
                case 1: RenderInventory(); break;
                case 2: RenderProcurement(); break;
                case 3: RenderLogistics(); break;
                case 4: RenderAfterService(); break;
                case 5: RenderFinance(); break;
            }

            pnlContent.ResumeLayout(true);
        }

        private void HighlightTab(int activeIndex)
        {
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                bool active = i == activeIndex;
                _tabButtons[i].ForeColor = active ? Palette.Primary : Color.FromArgb(98, 112, 135);
                _tabButtons[i].Font = active ? new Font("Segoe UI", 12f, FontStyle.Bold) : new Font("Segoe UI", 12f, FontStyle.Regular);
                _tabButtons[i].BackColor = Color.White;
            }
        }

        private void PaintTabUnderline(object sender, PaintEventArgs e)
        {
            if (_activeTab < 0 || _activeTab >= _tabButtons.Length) return;
            var btn = _tabButtons[_activeTab];
            Rectangle rect = btn.Bounds;
            using var brush = new SolidBrush(Palette.Primary);
            e.Graphics.FillRectangle(brush, rect.X + 24, pnlTabOuter.Height - 4, Math.Max(0, rect.Width - 48), 4);
        }

        private void RenderSales() { }
        private void RenderInventory() { }
        private void RenderProcurement() { }
        private void RenderLogistics() { }
        private void RenderAfterService() { }
        private void RenderFinance() { }

        private void BuildLayout(
            Panel pnlKpi, Panel filterBar,
            string title1, DataGridView grid1, Panel chart1, bool showChart1,
            string title2, DataGridView grid2, Panel chart2, bool showChart2,
            int grid2Height)
        {
            bool hasSecondary = !string.IsNullOrEmpty(title2) && grid2Height > 0;

            if (hasSecondary)
            {
                var dOuter = WrapCard(DockStyle.Bottom, grid2Height + 62, 20, 0, 20, 10);
                var dInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
                dInner.Paint += PaintCardBorder;
                var dTbl = MakeCardTable(title2);
                if (grid2 != null) { grid2.Dock = DockStyle.Fill; grid2.Visible = !showChart2; dTbl.Controls.Add(grid2, 0, 1); }
                if (chart2 != null) { chart2.Dock = DockStyle.Fill; chart2.Visible = showChart2; dTbl.Controls.Add(chart2, 0, 1); }
                dInner.Controls.Add(dTbl);
                dOuter.Controls.Add(dInner);
                pnlContent.Controls.Add(dOuter);
            }

            var cOuter = WrapCard(DockStyle.Fill, 0, 20, 0, 20, hasSecondary ? 0 : 10);
            var cInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            cInner.Paint += PaintCardBorder;
            var cTbl = MakeCardTable(title1);
            if (grid1 != null) { grid1.Dock = DockStyle.Fill; grid1.Visible = !showChart1; cTbl.Controls.Add(grid1, 0, 1); }
            if (chart1 != null) { chart1.Dock = DockStyle.Fill; chart1.Visible = showChart1; cTbl.Controls.Add(chart1, 0, 1); }
            cInner.Controls.Add(cTbl);
            cOuter.Controls.Add(cInner);
            pnlContent.Controls.Add(cOuter);

            var bOuter = WrapCard(DockStyle.Top, 118, 20, 0, 20, 8);
            var bInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            bInner.Paint += PaintCardBorder;
            filterBar.Dock = DockStyle.Fill;
            bInner.Controls.Add(filterBar);
            bOuter.Controls.Add(bInner);
            pnlContent.Controls.Add(bOuter);

            var aOuter = WrapCard(DockStyle.Top, 90, 20, 8, 20, 8);
            var aInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            aInner.Paint += PaintCardBorder;
            pnlKpi.Dock = DockStyle.Fill;
            aInner.Controls.Add(pnlKpi);
            aOuter.Controls.Add(aInner);
            pnlContent.Controls.Add(aOuter);
        }

        private static Panel WrapCard(DockStyle dock, int height, int padL, int padT, int padR, int padB)
        {
            var p = new Panel { Dock = dock, BackColor = Palette.BgPage, Padding = new Padding(padL, padT, padR, padB) };
            if (height > 0) p.Height = height;
            return p;
        }

        private static TableLayoutPanel MakeCardTable(string title)
        {
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(14, 8, 14, 8)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var hdr = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            hdr.Controls.Add(new Label
            {
                Text = title ?? string.Empty,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(19, 35, 61),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            });
            hdr.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(221, 227, 236) });
            tbl.Controls.Add(hdr, 0, 0);
            return tbl;
        }

        private static void PaintCardBorder(object sender, PaintEventArgs e)
        {
            var p = (Panel)sender;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        }

        private void OnTopNavMenuItemClicked(string menu, string sub)
            => FormNavigator.NavigateTo(this, menu, sub);
    }
}
