using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Controller for the Dashboard screen.
    ///
    /// Responsibilities:
    ///   1. Read session state from SessionManager; build UserBarInfo.
    ///   2. Apply NavAccessPolicy → AllowedMenus + DashboardSections.
    ///   3. Call DashboardRepo only for the data the current department can see.
    ///   4. Apply business logic (formatting, derived fields).
    ///   5. Return a fully-populated DashboardViewModel to the View.
    ///
    /// Department → Visible Sections matrix (mirrors NavAccessPolicy):
    /// ┌─────────────┬────────────────────────────────────────────────────────────────────┐
    /// │ Department  │ KPI Cards                         │ Section Cards                  │
    /// ├─────────────┼───────────────────────────────────┼────────────────────────────────┤
    /// │ IT          │ All 8                             │ All 6                          │
    /// │ Sales       │ Orders/Delivered/Quotations/      │ RecentOrders, Quotations,      │
    /// │             │ Revenue/AR/Customers              │ SupplierPayments, Activity     │
    /// │ Production  │ LowStock/Suppliers                │ LowStock, Activity             │
    /// │ Inventory   │ LowStock/Suppliers                │ LowStock, Activity             │
    /// │ Finance     │ Revenue/AR/Customers              │ SupplierPayments, Activity     │
    /// │ Logistics   │ Delivered/Suppliers               │ ActiveShipments, Activity      │
    /// └─────────────┴───────────────────────────────────┴────────────────────────────────┘
    /// </summary>
    public class DashboardController
    {
        private readonly DashboardRepo _repo;

        public DashboardController()
        {
            _repo = new DashboardRepo();
        }

        public DashboardViewModel LoadDashboard()
        {
            var vm = new DashboardViewModel();

            // ── 1. User Bar & Nav Access ────────────────────────────────────────
            string department = string.Empty;
            if (SessionManager.IsLoggedIn)
            {
                vm.UserBar = new UserBarInfo
                {
                    DisplayName = SessionManager.CurrentUser.StaffName  ?? string.Empty,
                    Department  = SessionManager.CurrentUser.Department ?? string.Empty
                };
                department = SessionManager.CurrentUser.Department ?? string.Empty;
            }
            else
            {
                vm.UserBar = new UserBarInfo { DisplayName = "Guest", Department = string.Empty };
            }

            vm.AllowedMenus = NavAccessPolicy.GetAllowedMenus(department);

            // ── 2. Section visibility (role-gated) ────────────────────────────
            vm.Sections = BuildSections(department);

            // ── 3. Raw data — only fetch what this role can see ───────────────
            var sec = vm.Sections;

            // Orders (needed by Sales / IT)
            var statusCounts = (sec.ShowKpiOrders || sec.ShowKpiDelivered)
                ? SafeCall(() => _repo.GetOrderStatusCounts(),
                           new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase))
                : new Dictionary<string, int>();

            int totalOrders      = statusCounts.Values.Sum();
            int delivered        = statusCounts.GetValueOrDefault("Delivered",           0);
            int completed        = statusCounts.GetValueOrDefault("Completed",           0);
            int pending          = statusCounts.GetValueOrDefault("Pending",             0);
            int processing       = statusCounts.GetValueOrDefault("Processing",          0);
            int partialDelivered = statusCounts.GetValueOrDefault("Partially Delivered", 0);
            int deliveredTotal   = delivered + completed;

            // Low stock (needed by Production / Inventory / IT)
            var lowStockItems = sec.ShowKpiLowStock
                ? SafeCall(() => _repo.GetLowStockItems(),  new List<LowStockRow>())
                : new List<LowStockRow>();

            // Finance figures
            decimal revenue   = sec.ShowKpiRevenue    ? SafeCall(() => _repo.GetMonthlyRevenue(),      0m) : 0m;
            decimal ar        = sec.ShowKpiAR         ? SafeCall(() => _repo.GetOutstandingAR(),       0m) : 0m;
            int     suppliers = sec.ShowKpiSuppliers  ? SafeCall(() => _repo.GetActiveSupplierCount(), 0)  : 0;
            int     customers = sec.ShowKpiCustomers  ? SafeCall(() => _repo.GetCustomerCount(),       0)  : 0;

            // ── 4. KPI list (only include cards this role may see) ────────────
            string month = DateTime.Now.ToString("MMM").ToUpper();
            vm.Kpis      = new List<DashboardKpi>();

            if (sec.ShowKpiOrders)
                vm.Kpis.Add(new DashboardKpi
                {
                    Label     = $"TOTAL ORDERS ({month})",
                    Value     = totalOrders.ToString(),
                    SubText   = $"{pending} Pending · {processing} Processing · {partialDelivered} Part. Delivered",
                    AccentKey = "Primary"
                });

            if (sec.ShowKpiDelivered)
                vm.Kpis.Add(new DashboardKpi
                {
                    Label     = "DELIVERED THIS MONTH",
                    Value     = deliveredTotal.ToString(),
                    SubText   = deliveredTotal == 0 ? "None this month" : $"{deliveredTotal} order(s) completed",
                    AccentKey = "Success"
                });

            if (sec.ShowKpiQuotations)
                vm.Kpis.Add(new DashboardKpi
                {
                    Label     = "PENDING QUOTATIONS",
                    Value     = "–",   // filled after quotation query below
                    SubText   = "",
                    AccentKey = "Warning"
                });

            if (sec.ShowKpiLowStock)
                vm.Kpis.Add(new DashboardKpi
                {
                    Label     = "LOW STOCK ALERTS",
                    Value     = lowStockItems.Count.ToString(),
                    SubText   = lowStockItems.Count > 0
                                    ? "Immediate procurement action needed"
                                    : "All items within threshold",
                    AccentKey = "Danger"
                });

            if (sec.ShowKpiRevenue)
                vm.Kpis.Add(new DashboardKpi
                {
                    Label     = "REVENUE THIS MONTH",
                    Value     = FormatHKD(revenue),
                    SubText   = "Based on delivered / completed orders",
                    AccentKey = "Info"
                });

            if (sec.ShowKpiAR)
                vm.Kpis.Add(new DashboardKpi
                {
                    Label     = "OUTSTANDING AR",
                    Value     = FormatHKD(ar),
                    SubText   = "Partially paid customer invoices",
                    AccentKey = "Warning"
                });

            if (sec.ShowKpiSuppliers)
                vm.Kpis.Add(new DashboardKpi
                {
                    Label     = "TOTAL SUPPLIERS",
                    Value     = suppliers.ToString(),
                    SubText   = "Registered in system",
                    AccentKey = "Primary"
                });

            if (sec.ShowKpiCustomers)
                vm.Kpis.Add(new DashboardKpi
                {
                    Label     = "TOTAL CUSTOMERS",
                    Value     = customers.ToString(),
                    SubText   = "Registered in system",
                    AccentKey = "Primary"
                });

            // ── 5. Tabular data ───────────────────────────────────────────────
            vm.LowStock = lowStockItems;

            vm.Orders = sec.ShowRecentOrders
                ? SafeCall(() => _repo.GetRecentOrders(5), new List<OrderSummaryRow>())
                : new List<OrderSummaryRow>();

            // Quotations (also updates KPI)
            if (sec.ShowPendingQuotations)
            {
                var quotations = SafeCall(() => _repo.GetPendingQuotations(5), new List<QuotationSummaryRow>());
                vm.Quotations  = quotations;

                // Back-fill the KPI card value
                var kpiQ = vm.Kpis.Find(k => k.Label == "PENDING QUOTATIONS");
                if (kpiQ != null)
                {
                    kpiQ.Value   = quotations.Count.ToString();
                    kpiQ.SubText = quotations.Count > 0
                        ? string.Join(" · ", quotations.Take(2).Select(q => q.QuotationId))
                        : "No pending quotations";
                }
            }

            vm.Shipments = sec.ShowActiveShipments
                ? SafeCall(() => _repo.GetActiveShipments(5), new List<ShipmentSummaryRow>())
                : new List<ShipmentSummaryRow>();

            vm.Suppliers = sec.ShowSupplierPayments
                ? SafeCall(() => _repo.GetSupplierPayments(5), new List<SupplierPaymentRow>())
                : new List<SupplierPaymentRow>();

            // ── 6. Activity feed ──────────────────────────────────────────────
            if (sec.ShowRecentActivity)
                vm.Activities = BuildActivityFeed(vm.Orders, vm.Shipments, vm.Suppliers);

            return vm;
        }

        // ── Section-visibility builder ────────────────────────────────────────
        /// <summary>
        /// Returns a <see cref="DashboardSections"/> flag-set that mirrors the
        /// NavAccessPolicy department matrix.
        /// Unknown/null departments see only the Activity feed as a safe fallback.
        /// </summary>
        private static DashboardSections BuildSections(string department)
        {
            switch ((department ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "it":
                    return new DashboardSections
                    {
                        ShowKpiOrders = true, ShowKpiDelivered = true,
                        ShowKpiQuotations = true, ShowKpiLowStock = true,
                        ShowKpiRevenue = true, ShowKpiAR = true,
                        ShowKpiSuppliers = true, ShowKpiCustomers = true,
                        ShowRecentOrders = true, ShowLowStock = true,
                        ShowPendingQuotations = true, ShowActiveShipments = true,
                        ShowSupplierPayments = true, ShowRecentActivity = true
                    };

                case "sales":
                    // Nav: Order Processing, After-Service, Master Data, Statistical Reports
                    return new DashboardSections
                    {
                        ShowKpiOrders = true, ShowKpiDelivered = true,
                        ShowKpiQuotations = true,
                        ShowKpiRevenue = true, ShowKpiAR = true,
                        ShowKpiCustomers = true,
                        ShowRecentOrders = true,
                        ShowPendingQuotations = true,
                        ShowSupplierPayments = true,
                        ShowRecentActivity = true
                    };

                case "production":
                    // Nav: Production Processing, Inventory Control, Raw Material
                    return new DashboardSections
                    {
                        ShowKpiLowStock = true, ShowKpiSuppliers = true,
                        ShowLowStock = true,
                        ShowRecentActivity = true
                    };

                case "inventory":
                    // Nav: Inventory Control, Raw Material, Master Data
                    return new DashboardSections
                    {
                        ShowKpiLowStock = true, ShowKpiSuppliers = true,
                        ShowLowStock = true,
                        ShowRecentActivity = true
                    };

                case "finance":
                    // Nav: After-Service, Master Data, Statistical Reports
                    return new DashboardSections
                    {
                        ShowKpiRevenue = true, ShowKpiAR = true,
                        ShowKpiCustomers = true,
                        ShowSupplierPayments = true,
                        ShowRecentActivity = true
                    };

                case "logistics":
                    // Nav: Logistics Processing, Master Data
                    return new DashboardSections
                    {
                        ShowKpiDelivered = true, ShowKpiSuppliers = true,
                        ShowActiveShipments = true,
                        ShowRecentActivity = true
                    };

                default:
                    // Unknown department — minimal safe view
                    return new DashboardSections { ShowRecentActivity = true };
            }
        }

        // ── Fault-isolation helper ────────────────────────────────────────────
        private static T SafeCall<T>(Func<T> fn, T fallback)
        {
            try   { return fn(); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[DashboardController] Query failed: {ex.Message}");
                return fallback;
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────
        private static string FormatHKD(decimal amount)
        {
            if (amount >= 1_000_000m) return $"HK${(amount / 1_000_000m):0.#}M";
            if (amount >= 1_000m)     return $"HK${(amount / 1_000m):0.#}K";
            return $"HK${amount:N0}";
        }

        private static List<ActivityRow> BuildActivityFeed(
            List<OrderSummaryRow>    orders,
            List<ShipmentSummaryRow> shipments,
            List<SupplierPaymentRow> suppliers)
        {
            var feed = new List<ActivityRow>();

            foreach (var o in orders.Take(3))
                feed.Add(new ActivityRow
                {
                    CategoryKey = MapOrderStatusToCategory(o.Status),
                    BoldText    = o.OrderId,
                    NormalText  = $" — {o.Customer} · HK${o.Total} · {o.Status}",
                    TimeLabel   = "Recent"
                });

            foreach (var s in shipments.Take(2))
                feed.Add(new ActivityRow
                {
                    CategoryKey = MapShipmentStatusToCategory(s.Status),
                    BoldText    = s.ShipmentId,
                    NormalText  = $" — status: {s.Status} · {s.Customer}",
                    TimeLabel   = s.SchedDate
                });

            foreach (var p in suppliers.Where(p => p.Status == "Overdue").Take(2))
                feed.Add(new ActivityRow
                {
                    CategoryKey = "Danger",
                    BoldText    = p.Supplier,
                    NormalText  = $" invoice {p.InvoiceId} is Overdue",
                    TimeLabel   = "Overdue"
                });

            return feed;
        }

        private static string MapOrderStatusToCategory(string status)
        {
            switch (status)
            {
                case "Delivered":           return "Success";
                case "Completed":           return "Success";
                case "Partially Delivered": return "Info";
                case "Processing":          return "Primary";
                case "Pending":             return "Warning";
                case "Cancelled":           return "Danger";
                default:                    return "Primary";
            }
        }

        private static string MapShipmentStatusToCategory(string status)
        {
            switch (status)
            {
                case "Completed":  return "Success";
                case "In Transit": return "Primary";
                case "Pending":    return "Warning";
                default:           return "Primary";
            }
        }
    }
}
