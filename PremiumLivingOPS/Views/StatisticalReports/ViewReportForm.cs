using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.StatisticalReports
{
    public partial class ViewReportForm : Form
    {
        // ── Active tab index (0-based, matches btnTab* order) ─────────────────
        private int _activeTab = -1;

        // ── Active tab button (for underline indicator) ───────────────────────
        private Button _activeTabBtn;

        public ViewReportForm()
        {
            InitializeComponent();
            this.Load += ViewReportForm_Load;
        }

        // ──────────────────────────────────────────────────────────────────────
        //  Load
        // ──────────────────────────────────────────────────────────────────────
        private void ViewReportForm_Load(object sender, EventArgs e)
        {
            // Default to first tab: Sales & Revenue
            SwitchToReport(0);
        }

        // ──────────────────────────────────────────────────────────────────────
        //  Tab switcher
        // ──────────────────────────────────────────────────────────────────────
        internal void SwitchToReport(int tabIndex)
        {
            if (_activeTab == tabIndex) return;
            _activeTab = tabIndex;

            // Update tab button visual states
            var tabBtns = new Button[]
            {
                btnTabSalesRevenue,
                btnTabInventory,
                btnTabProduction,
                btnTabLogistics,
                btnTabAfterService
            };

            for (int i = 0; i < tabBtns.Length; i++)
            {
                bool isActive = i == tabIndex;
                tabBtns[i].ForeColor = isActive
                    ? Color.FromArgb(47, 111, 237)
                    : Color.FromArgb(98, 112, 135);
                tabBtns[i].Font = new Font("Segoe UI",
                    12f,
                    isActive ? FontStyle.Bold : FontStyle.Regular);
                tabBtns[i].Padding = isActive
                    ? new Padding(0, 0, 0, 0)
                    : new Padding(0, 0, 0, 3);
            }

            // Rebuild the content area
            pnlContent.Controls.Clear();

            switch (tabIndex)
            {
                case 0: BuildSalesRevenueReport(); break;
                case 1: BuildInventoryReport();    break;
                case 2: BuildProductionReport();   break;
                case 3: BuildLogisticsReport();    break;
                case 4: BuildAfterServiceReport(); break;
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        //  Report builders  (placeholder — fill with real chart / grid panels)
        // ──────────────────────────────────────────────────────────────────────
        private void BuildSalesRevenueReport()
        {
            var (outer, inner) = CardPanel.CreateFill();
            inner.Controls.Add(MakePlaceholderLabel("Sales & Revenue Report"));
            pnlContent.Controls.Add(outer);
        }

        private void BuildInventoryReport()
        {
            var (outer, inner) = CardPanel.CreateFill();
            inner.Controls.Add(MakePlaceholderLabel("Inventory Report"));
            pnlContent.Controls.Add(outer);
        }

        private void BuildProductionReport()
        {
            var (outer, inner) = CardPanel.CreateFill();
            inner.Controls.Add(MakePlaceholderLabel("Production Report"));
            pnlContent.Controls.Add(outer);
        }

        private void BuildLogisticsReport()
        {
            var (outer, inner) = CardPanel.CreateFill();
            inner.Controls.Add(MakePlaceholderLabel("Logistics Report"));
            pnlContent.Controls.Add(outer);
        }

        private void BuildAfterServiceReport()
        {
            var (outer, inner) = CardPanel.CreateFill();
            inner.Controls.Add(MakePlaceholderLabel("After-Service Report"));
            pnlContent.Controls.Add(outer);
        }

        // ──────────────────────────────────────────────────────────────────────
        //  Helpers
        // ──────────────────────────────────────────────────────────────────────
        private static Label MakePlaceholderLabel(string text) => new Label
        {
            Text      = text,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = Color.FromArgb(98, 112, 135)
        };

        // ──────────────────────────────────────────────────────────────────────
        //  AppShell event handlers
        // ──────────────────────────────────────────────────────────────────────
        private void OnTopNavMenuItemClicked(string menu, string sub)
        {
            FormNavigator.Navigate(this, menu, sub);
        }

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            FormNavigator.Logout(this);
        }
    }
}
