using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.StatisticalReports
{
    public partial class ViewReportForm : Form
    {
        // ── Active tab index (0-based) ────────────────────────────────────────
        private int _activeTab = -1;

        private readonly StatisticalReportsController _ctrl =
            new StatisticalReportsController();

        public ViewReportForm()
        {
            InitializeComponent();
            this.Load += ViewReportForm_Load;
        }

        // ────────────────────────────────────────────────────────────────────
        //  Load  — mirrors HGR pattern exactly:
        //          1. Populate AppShell UserBar (via any report VM)
        //          2. Switch to default tab
        // ────────────────────────────────────────────────────────────────────
        private void ViewReportForm_Load(object sender, EventArgs e)
        {
            RefreshShell();
            SwitchToReport(0);
        }

        // ────────────────────────────────────────────────────────────────────
        //  AppShell population
        //  Uses GetSalesReportVM() — the lightest report call — just to obtain
        //  UserBar (DisplayName, Department) and AllowedMenus from the session.
        //  No report-specific data from the returned VM is consumed here.
        // ────────────────────────────────────────────────────────────────────
        private void RefreshShell()
        {
            var vm = _ctrl.GetSalesReportVM();
            if (vm == null) return;

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Statistical Reports  \u203a  View Report");
        }

        // ────────────────────────────────────────────────────────────────────
        //  Tab switcher
        // ────────────────────────────────────────────────────────────────────
        internal void SwitchToReport(int tabIndex)
        {
            if (_activeTab == tabIndex) return;
            _activeTab = tabIndex;

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
                tabBtns[i].Font = new Font("Segoe UI", 12f,
                    isActive ? FontStyle.Bold : FontStyle.Regular);
                tabBtns[i].Padding = isActive
                    ? new Padding(0)
                    : new Padding(0, 0, 0, 3);
                tabBtns[i].Invalidate();
            }

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

        // ────────────────────────────────────────────────────────────────────
        //  Report builders (placeholder — each will call the relevant ctrl method)
        // ────────────────────────────────────────────────────────────────────
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

        // ────────────────────────────────────────────────────────────────────
        //  Helpers
        // ────────────────────────────────────────────────────────────────────
        private static Label MakePlaceholderLabel(string text) => new Label
        {
            Text      = text,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = Color.FromArgb(98, 112, 135)
        };

        // ────────────────────────────────────────────────────────────────────
        //  AppShell event handlers  (subscribed ONCE in Designer.cs — RULE 4)
        // ────────────────────────────────────────────────────────────────────
        private void OnTopNavMenuItemClicked(string menu, string subItem)
            => FormNavigator.NavigateTo(this, menu, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to logout?",
                    "Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                FormNavigator.NavigateTo(this, "Logout");
        }
    }
}
