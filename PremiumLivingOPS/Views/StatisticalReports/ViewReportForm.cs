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

        private static readonly Color[] ChartPalette = new Color[]
        {
            Color.FromArgb( 55,  48, 163),
            Color.FromArgb(  6,  95,  70),
            Color.FromArgb(185,  28,  28),
            Color.FromArgb(146,  64,  14),
            Color.FromArgb( 29,  78, 216),
            Color.FromArgb( 91,  33, 182),
            Color.FromArgb(  3, 105, 161),
            Color.FromArgb( 22, 101,  52),
        };

        private enum ChartStyle { Bar, Column, Pie }

        public ViewReportForm()
        {
            InitializeComponent();
            _tabButtons = new Button[] { btnTab0, btnTab1, btnTab2, btnTab3, btnTab4, btnTab5 };
            this.Load += ViewReportForm_Load;
        }

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

        private void SwapContent(Panel dgvCard, Panel chartCard, bool showChart)
        {
            pnlContent.SuspendLayout();
            pnlContent.Controls.Clear();
            pnlContent.Controls.Add(showChart ? chartCard : dgvCard);
            pnlContent.ResumeLayout(true);
        }

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

        private static Button MakePrimaryBtn(string text)
        {
            return new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Palette.Primary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Height    = 36,
                Cursor    = Cursors.Hand
            };
        }

        private static Button MakeOutlineBtn(string text)
        {
            var btn = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 10f),
                BackColor = Color.FromArgb(241, 244, 249),
                ForeColor = Color.FromArgb(15, 31, 53),
                FlatStyle = FlatStyle.Flat,
                Height    = 36,
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            return btn;
        }

        private static Button MakeAmberBtn(string text)
        {
            return new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(235, 241, 255),
                ForeColor = Palette.Primary,
                FlatStyle = FlatStyle.Flat,
                Height    = 36,
                Cursor    = Cursors.Hand
            };
        }

        private static Button MakeExportBtn(string text)
        {
            var btn = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 10f),
                BackColor = Color.FromArgb(241, 244, 249),
                ForeColor = Color.FromArgb(15, 31, 53),
                FlatStyle = FlatStyle.Flat,
                Height    = 36,
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            return btn;
        }

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

        private static Panel BuildChartCard(string title, string[] labels, double[] values, ChartStyle style = ChartStyle.Bar)
        {
            var outer = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 12, 0, 0) };
            var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                int W = card.Width;
                int H = card.Height;
                int padL = 60, padR = 30, padT = 50, padB = 60;
                int chartW = W - padL - padR;
                int chartH = H - padT - padB;

                using var titleFont = new Font("Segoe UI", 13f, FontStyle.Bold);
                g.DrawString(title, titleFont, Brushes.Black, new PointF(padL, 14));

                if (labels == null || labels.Length == 0 || values == null || values.Length == 0)
                {
                    g.DrawString("No data", new Font("Segoe UI", 11f), Brushes.Gray, new PointF(W / 2f - 30, H / 2f));
                    return;
                }

                int n = Math.Min(labels.Length, values.Length);
                if (style == ChartStyle.Pie)
                {
                    double total = 0;
                    for (int i = 0; i < n; i++) total += Math.Abs(values[i]);
                    if (total == 0) return;

                    int pieSize = Math.Min(chartW, chartH) - 20;
                    int pieX = padL + (chartW - pieSize) / 2;
                    int pieY = padT + (chartH - pieSize) / 2;

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
                    return;
                }
            };
            outer.Controls.Add(card);
            return outer;
        }

        private Panel WrapInContentCard(Control inner)
        {
            var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(20, 14, 20, 14) };
            card.Paint += PaintCardBorder;
            inner.Dock = DockStyle.Fill;
            card.Controls.Add(inner);

            var outer = new Panel { Dock = DockStyle.Fill, BackColor = Palette.BgPage, Padding = new Padding(20, 14, 20, 14) };
            outer.Controls.Add(card);
            return outer;
        }

        private static Panel BuildSectionHeader(string title, Button toggleBtn, Button exportBtn)
        {
            var pnl = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.Transparent };
            var lbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock = DockStyle.Left,
                AutoSize = false,
                Width = 420,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var btnPanel = new Panel { Dock = DockStyle.Right, Width = 300, BackColor = Color.Transparent };
            if (exportBtn != null)
            {
                exportBtn.Dock = DockStyle.Right;
                exportBtn.Width = 130;
                btnPanel.Controls.Add(exportBtn);
            }
            if (toggleBtn != null)
            {
                toggleBtn.Dock = DockStyle.Right;
                toggleBtn.Width = 150;
                btnPanel.Controls.Add(toggleBtn);
            }

            pnl.Controls.Add(lbl);
            pnl.Controls.Add(btnPanel);
            return pnl;
        }

        private void RenderSales() { }
        private void RenderInventory() { }
        private void RenderProcurement() { }
        private void RenderLogistics() { }
        private void RenderAfterService() { }

        private void RenderFinance()
        {
            var dtFrom = MakeDatePicker(DefaultDateFrom);
            var dtTo = MakeDatePicker(DefaultDateTo);
            var cmbTxType = MakeComboBox(new[] { "All", "Revenue", "Expense", "Refund" });

            var btnSearch = MakePrimaryBtn("Search");
            var btnReset = MakeOutlineBtn("Reset");

            var btnRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnSearch.Width = 110; btnSearch.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            btnReset.Width = 90; btnReset.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            btnSearch.Location = new Point(0, 10);
            btnReset.Location = new Point(118, 10);
            btnRow.Controls.Add(btnSearch);
            btnRow.Controls.Add(btnReset);

            SetFilterBar("Finance Summary",
                BuildFieldsRow(
                    ("Date From", dtFrom),
                    ("Date To", dtTo),
                    ("Transaction Type", cmbTxType)),
                btnRow);

            DataGridView dgv = null;
            Panel dgvCard = null;
            Panel chartCard = null;

            void LoadData()
            {
                var vm = _ctrl.GetFinanceReportVM(dtFrom.Value, dtTo.Value,
                    cmbTxType.SelectedItem?.ToString() == "All" ? null : cmbTxType.SelectedItem?.ToString());

                var kpi = vm.FinanceKpi ?? new FinanceKpiEntity();
                var rows = vm.FinanceRows ?? new List<FinanceTransactionRowEntity>();
                var netProfit = kpi.TotalSalesRevenue - kpi.TotalProcurementSpend - kpi.TotalRefunds;

                var kpiRow = BuildKpiRow(
                    ("Sales Revenue", $"HKD {kpi.TotalSalesRevenue:N0}", Color.FromArgb(6, 95, 70)),
                    ("Procurement Spend", $"HKD {kpi.TotalProcurementSpend:N0}", Color.FromArgb(185, 28, 28)),
                    ("Net", $"HKD {netProfit:N0}", netProfit >= 0 ? Color.FromArgb(6, 95, 70) : Color.FromArgb(185, 28, 28)),
                    ("Refunds", $"HKD {kpi.TotalRefunds:N0}", Color.FromArgb(146, 64, 14)),
                    ("AR Outstanding", $"HKD {kpi.AROutstanding:N0}", Color.FromArgb(29, 78, 216)));

                dgv = BuildDgv();
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTxID", HeaderText = "Tx ID", FillWeight = 12 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTxType", HeaderText = "Type", FillWeight = 12 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDocType", HeaderText = "Document Type", FillWeight = 16 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTxDate", HeaderText = "Date", FillWeight = 12 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTxAmt", HeaderText = "Amount", FillWeight = 14 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTxMethod", HeaderText = "Payment Method", FillWeight = 14 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTxStatus", HeaderText = "Status", FillWeight = 12 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLinkedDoc", HeaderText = "Linked Doc", FillWeight = 12 });

                foreach (var r in rows)
                    dgv.Rows.Add(r.TransactionID, r.TransactionType, r.DocumentType,
                        r.TransactionDate.ToString("yyyy-MM-dd"),
                        $"HKD {r.Amount:N0}", r.PaymentMethod, r.ApprovalStatus, r.LinkedDocument);

                var btnToggle = MakeAmberBtn("View Chart");
                var btnExport = MakeExportBtn("Export CSV");
                btnExport.Click += (s, e) => ExportGrid(dgv, "FinanceSummary");
                btnToggle.Click += (s, e) =>
                {
                    _financeChart = !_financeChart;
                    btnToggle.Text = _financeChart ? "View Table" : "View Chart";
                    if (chartCard == null)
                    {
                        chartCard = WrapInContentCard(BuildChartCard(
                            "Revenue vs Procurement vs Refunds",
                            new[] { "Sales Revenue", "Procurement Spend", "Refunds" },
                            new double[] { kpi.TotalSalesRevenue, kpi.TotalProcurementSpend, kpi.TotalRefunds },
                            ChartStyle.Column));
                    }
                    SwapContent(dgvCard, chartCard, _financeChart);
                };

                var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, BackColor = Color.White };
                tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 100f));
                tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
                tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                tbl.Controls.Add(kpiRow, 0, 0);
                tbl.Controls.Add(BuildSectionHeader("Transaction List", btnToggle, btnExport), 0, 1);
                tbl.Controls.Add(dgv, 0, 2);

                dgvCard = WrapInContentCard(tbl);
                chartCard = null;
                _financeChart = false;
                SwapContent(dgvCard, chartCard, false);
            }

            btnSearch.Click += (s, e) => LoadData();
            btnReset.Click += (s, e) =>
            {
                dtFrom.Value = DefaultDateFrom;
                dtTo.Value = DefaultDateTo;
                cmbTxType.SelectedIndex = 0;
                LoadData();
            };

            LoadData();
        }

        private static void PaintCardBorder(object sender, PaintEventArgs e)
        {
            if (sender is not Control ctrl) return;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, ctrl.Width - 1, ctrl.Height - 1);
        }

        private void DgvCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (sender is not DataGridView dgv) return;
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var col = dgv.Columns[e.ColumnIndex];
            if ((col.Name == "colStatus" || col.Name == "colCStatus" || col.Name == "colRStatus" || col.Name == "colPoStatus" || col.Name == "colRtStatus" || col.Name == "colTxStatus") && e.Value is string status)
            {
                if (StatusColors.TryGetValue(status, out var colors))
                {
                    e.CellStyle.BackColor = colors.bg;
                    e.CellStyle.ForeColor = colors.fg;
                    e.CellStyle.SelectionBackColor = colors.bg;
                    e.CellStyle.SelectionForeColor = colors.fg;
                    e.CellStyle.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
                    e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    e.FormattingApplied = true;
                }
                return;
            }

            if (col.Name is "colRevenue" or "colAmt" or "colRefund" or "colStock" or "colReorder" or "colTotal" or "colTxAmt")
            {
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                e.FormattingApplied = true;
            }

            if (col.Name is "colHasDN" or "colHasRS")
            {
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                e.FormattingApplied = true;
            }
        }

        private static void ExportGrid(DataGridView dgv, string defaultName)
        {
            using var dlg = new SaveFileDialog
            {
                Title = "Export to CSV",
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                FileName = $"{defaultName}_{DateTime.Today:yyyyMMdd}.csv",
                DefaultExt = "csv",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (dlg.ShowDialog() != DialogResult.OK) return;
            try
            {
                var sb = new System.Text.StringBuilder();
                for (int c = 0; c < dgv.Columns.Count; c++)
                {
                    if (c > 0) sb.Append(',');
                    sb.Append(QuoteCsv(dgv.Columns[c].HeaderText));
                }
                sb.AppendLine();

                foreach (DataGridViewRow row in dgv.Rows)
                {
                    if (row.IsNewRow) continue;
                    for (int c = 0; c < dgv.Columns.Count; c++)
                    {
                        if (c > 0) sb.Append(',');
                        sb.Append(QuoteCsv(row.Cells[c].FormattedValue?.ToString() ?? string.Empty));
                    }
                    sb.AppendLine();
                }

                System.IO.File.WriteAllText(dlg.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                MessageBox.Show($"Exported {dgv.Rows.Count} row(s) to:\n{dlg.FileName}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed:\n{ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            FormNavigator.NavigateTo(this, "Logout");
        }
    }
}
