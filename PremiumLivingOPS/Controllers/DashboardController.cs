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
    ///   2. Apply NavAccessPolicy to produce the AllowedMenus list.
    ///   3. Call DashboardRepo to fetch raw data.
    ///   4. Apply business logic (formatting, derived fields).
    ///   5. Return a fully-populated DashboardViewModel to the View.
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

            // ── 1. User Bar & Nav Access ────────────────────────────────
            string department = string.Empty;
            if (SessionManager.IsLoggedIn)
            {
                vm.UserBar = new UserBarInfo
                {
                    DisplayName = SessionManager.CurrentUser.StaffName ?? string.Empty,
                    Department  = SessionManager.CurrentUser.Department ?? string.Empty
                };
                department = SessionManager.CurrentUser.Department ?? string.Empty;
            }
            else
            {
                vm.UserBar = new UserBarInfo { DisplayName = "Guest", Department = string.Empty };
            }

            // NavAccessPolicy is pure business logic — lives in Controller layer
            vm.AllowedMenus = NavAccessPolicy.GetAllowedMenus(department);

            // ── 2. Raw data from DAL ─────────────────────────────────────
            var statusCounts = SafeCall(() => _repo.GetOrderStatusCounts(),
                                        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase));

            int totalOrders = statusCounts.Values.Sum();
            int delivered   = statusCounts.GetValueOrDefault("Delivered",  0);
            int pending     = statusCounts.GetValueOrDefault("Pending",    0);
            int processing  = statusCounts.GetValueOrDefault("Processing", 0);
            int shipped     = statusCounts.GetValueOrDefault("Shipped",    0);

            var lowStockItems = SafeCall(() => _repo.GetLowStockItems(),       new List<LowStockRow>());
            decimal revenue   = SafeCall(() => _repo.GetMonthlyRevenue(),      0m);
            decimal ar        = SafeCall(() => _repo.GetOutstandingAR(),       0m);
            int suppliers     = SafeCall(() => _repo.GetActiveSupplierCount(), 0);
            int customers     = SafeCall(() => _repo.GetCustomerCount(),       0);

            // ── 3. KPI list ───────────────────────────────────────────
            string month = DateTime.Now.ToString("MMM").ToUpper();

            vm.Kpis = new List<DashboardKpi>
            {
                new DashboardKpi
                {
                    Label     = $"TOTAL ORDERS ({month})",
                    Value     = totalOrders.ToString(),
                    SubText   = $"{pending} Pending · {processing} Processing · {shipped} Shipped",
                    AccentKey = "Primary"
                },
                new DashboardKpi
                {
                    Label     = "DELIVERED THIS MONTH",
                    Value     = delivered.ToString(),
                    SubText   = delivered == 0 ? "None this month" : $"{delivered} order(s) completed",
                    AccentKey = "Success"
                },
                new DashboardKpi
                {
                    Label     = "PENDING QUOTATIONS",
                    Value     = "–",
                    SubText   = "",
                    AccentKey = "Warning"
                },
                new DashboardKpi
                {
                    Label     = "LOW STOCK ALERTS",
                    Value     = lowStockItems.Count.ToString(),
                    SubText   = lowStockItems.Count > 0
                                    ? "Immediate procurement action needed"
                                    : "All items within threshold",
                    AccentKey = "Danger"
                },
                new DashboardKpi
                {
                    Label     = "REVENUE THIS MONTH",
                    Value     = FormatHKD(revenue),
                    SubText   = "Based on delivered orders",
                    AccentKey = "Info"
                },
                new DashboardKpi
                {
                    Label     = "OUTSTANDING AR",
                    Value     = FormatHKD(ar),
                    SubText   = "Unpaid / overdue invoices",
                    AccentKey = "Warning"
                },
                new DashboardKpi
                {
                    Label     = "ACTIVE SUPPLIERS",
                    Value     = suppliers.ToString(),
                    SubText   = "",
                    AccentKey = "Primary"
                },
                new DashboardKpi
                {
                    Label     = "TOTAL CUSTOMERS",
                    Value     = customers.ToString(),
                    SubText   = "",
                    AccentKey = "Primary"
                }
            };

            // ── 4. Tabular data ───────────────────────────────────────
            vm.Orders    = SafeCall(() => _repo.GetRecentOrders(5),       new List<OrderSummaryRow>());
            vm.LowStock  = lowStockItems;

            var quotations   = SafeCall(() => _repo.GetPendingQuotations(5), new List<QuotationSummaryRow>());
            vm.Quotations    = quotations;
            vm.Kpis[2].Value   = quotations.Count.ToString();
            vm.Kpis[2].SubText = quotations.Count > 0
                ? string.Join(" · ", quotations.Take(2).Select(q => q.QuotationId))
                : "No pending quotations";

            vm.Shipments = SafeCall(() => _repo.GetActiveShipments(5),    new List<ShipmentSummaryRow>());
            vm.Suppliers = SafeCall(() => _repo.GetSupplierPayments(5),   new List<SupplierPaymentRow>());

            // ── 5. Activity feed ──────────────────────────────────────
            vm.Activities = BuildActivityFeed(vm.Orders, vm.Shipments, vm.Suppliers);

            return vm;
        }

        // ── Fault-isolation helper ───────────────────────────────────
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

        // ── Private helpers ──────────────────────────────────────────
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
                    NormalText  = $" — {o.Customer} · {o.Total} · {o.Status}",
                    TimeLabel   = "Recent"
                });

            foreach (var s in shipments.Take(2))
                feed.Add(new ActivityRow
                {
                    CategoryKey = MapShipStatusToCategory(s.Status),
                    BoldText    = s.ShipmentId,
                    NormalText  = $" status: {s.Status} · {s.Customer}",
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
                case "Delivered":  return "Success";
                case "Shipped":    return "Primary";
                case "Processing": return "Primary";
                case "Pending":    return "Warning";
                default:           return "Primary";
            }
        }

        private static string MapShipStatusToCategory(string status)
        {
            switch (status)
            {
                case "Delivered":  return "Success";
                case "In Transit": return "Primary";
                case "Scheduled":  return "Warning";
                default:           return "Primary";
            }
        }
    }
}
