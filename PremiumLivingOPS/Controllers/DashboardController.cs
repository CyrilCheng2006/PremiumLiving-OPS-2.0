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
    ///   1. Call DashboardRepo to fetch raw data from the database.
    ///   2. Apply business logic (formatting, derived fields, sorting).
    ///   3. Assemble a DashboardViewModel and return it to the View.
    ///
    /// The View (DashboardForm) must NOT contain any of the above logic.
    /// </summary>
    public class DashboardController
    {
        private readonly DashboardRepo _repo;

        public DashboardController()
        {
            _repo = new DashboardRepo();
        }

        /// <summary>
        /// Loads all data required by DashboardForm and returns a fully
        /// populated DashboardViewModel ready for UI binding.
        /// </summary>
        public DashboardViewModel LoadDashboard()
        {
            var vm = new DashboardViewModel();

            // ── 1. Raw data from DAL ──────────────────────────────────
            var statusCounts  = _repo.GetOrderStatusCounts();
            int totalOrders   = statusCounts.Values.Sum();
            int delivered     = statusCounts.ContainsKey("Delivered")  ? statusCounts["Delivered"]  : 0;
            int pending       = statusCounts.ContainsKey("Pending")    ? statusCounts["Pending"]    : 0;
            int processing    = statusCounts.ContainsKey("Processing") ? statusCounts["Processing"] : 0;
            int shipped       = statusCounts.ContainsKey("Shipped")    ? statusCounts["Shipped"]    : 0;

            var lowStockItems = _repo.GetLowStockItems();
            decimal revenue   = _repo.GetMonthlyRevenue();
            decimal ar        = _repo.GetOutstandingAR();
            int suppliers     = _repo.GetActiveSupplierCount();
            int customers     = _repo.GetCustomerCount();

            // ── 2. Build KPI list ─────────────────────────────────────
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
                    Value     = "–",   // filled by GetPendingQuotations count below
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

            // ── 3. Tabular data ───────────────────────────────────────
            vm.Orders     = _repo.GetRecentOrders(5);
            vm.LowStock   = lowStockItems;

            var quotations    = _repo.GetPendingQuotations(5);
            vm.Quotations     = quotations;

            // Patch Pending Quotation KPI value now that we have the count
            vm.Kpis[2].Value   = quotations.Count.ToString();
            vm.Kpis[2].SubText = quotations.Count > 0
                ? string.Join(" · ", quotations.Take(2).Select(q => q.QuotationId))
                : "No pending quotations";

            vm.Shipments  = _repo.GetActiveShipments(5);
            vm.Suppliers  = _repo.GetSupplierPayments(5);

            // ── 4. Activity feed (derived from orders + shipments) ────
            vm.Activities = BuildActivityFeed(vm.Orders, vm.Shipments, vm.Suppliers);

            return vm;
        }

        // ── Private helpers ───────────────────────────────────────────

        /// <summary>Formats a decimal as compact HKD, e.g. HK$221K or HK$1.2M.</summary>
        private static string FormatHKD(decimal amount)
        {
            if (amount >= 1_000_000m)
                return $"HK${(amount / 1_000_000m):0.#}M";
            if (amount >= 1_000m)
                return $"HK${(amount / 1_000m):0.#}K";
            return $"HK${amount:N0}";
        }

        /// <summary>
        /// Derives an activity feed from the data already loaded.
        /// In a production system this would come from a dedicated AuditLog table.
        /// </summary>
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
