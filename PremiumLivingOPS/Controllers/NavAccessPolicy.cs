using System;
using System.Collections.Generic;
using System.Linq;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Defines which top-navigation menu labels are visible for each Department.
    ///
    /// MVC contract (Controller layer — pure business rule, no UI dependency):
    ///   DashboardController calls GetAllowedMenus(department) and stores the
    ///   result in DashboardViewModel.AllowedMenus.
    ///   TopNavBar (View) receives the list via SetVisibleMenus() and renders
    ///   only the permitted items — it never reads SessionManager directly.
    ///
    /// Menu labels must match the Label strings defined in TopNavBar.AllMenus
    /// exactly (case-sensitive).
    /// </summary>
    public static class NavAccessPolicy
    {
        // All menu labels that exist in TopNavBar.AllMenus
        private const string Dashboard      = "Dashboard";
        private const string OrderProc      = "Order Processing";
        private const string ProductionProc = "Production Processing";
        private const string LogisticsProc  = "Logistics Processing";
        private const string InventoryCtrl  = "Inventory Control";
        private const string RawMaterial    = "Raw Material";
        private const string AfterService   = "After-Service";
        private const string MasterData     = "Master Data Maintenance";
        private const string SystemControl  = "System Control";   // renamed from "System Security & Control"
        private const string StatReports    = "Statistical Reports";

        // ── Access matrix ────────────────────────────────────────────
        // Key   : Department value stored in Staff.Department (DB ENUM)
        // Value : Set of menu labels the department is permitted to see
        private static readonly Dictionary<string, HashSet<string>> _matrix =
            new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["IT"] = new HashSet<string>
            {
                Dashboard, OrderProc, ProductionProc, LogisticsProc,
                InventoryCtrl, RawMaterial, AfterService,
                MasterData, SystemControl, StatReports
            },
            ["Production"] = new HashSet<string>
            {
                Dashboard, ProductionProc,
                InventoryCtrl, RawMaterial
            },
            ["Sales"] = new HashSet<string>
            {
                Dashboard, OrderProc, AfterService,
                MasterData, StatReports
            },
            ["Inventory"] = new HashSet<string>
            {
                Dashboard, InventoryCtrl, RawMaterial, MasterData
            },
            ["Finance"] = new HashSet<string>
            {
                Dashboard, AfterService, MasterData, StatReports
            },
            ["Logistics"] = new HashSet<string>
            {
                Dashboard, LogisticsProc, MasterData
            }
        };

        /// <summary>
        /// Returns the ordered list of menu labels the given department may see.
        /// Unknown / null departments receive Dashboard only as a safe fallback.
        /// The order mirrors the canonical TopNavBar menu order.
        /// </summary>
        public static string[] GetAllowedMenus(string department)
        {
            if (string.IsNullOrWhiteSpace(department) ||
                !_matrix.TryGetValue(department, out HashSet<string> allowed))
            {
                return new[] { Dashboard };
            }

            // Return in canonical display order
            string[] ordered =
            {
                Dashboard, OrderProc, ProductionProc, LogisticsProc,
                InventoryCtrl, RawMaterial, AfterService,
                MasterData, SystemControl, StatReports
            };

            return ordered.Where(m => allowed.Contains(m)).ToArray();
        }
    }
}
