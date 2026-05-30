using PremiumLivingOPS.Views.OrderProcessing;
using System;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.Shared
{
    /// <summary>
    /// Handles top-navigation routing for every Form in the application.
    ///
    /// MVC contract (View layer helper):
    ///   Each Form's OnTopNavMenuItemClicked handler delegates to
    ///   FormNavigator.NavigateTo(currentForm, menuLabel, subItem).
    ///   This class opens the correct destination Form and hides the
    ///   caller, so the application always has exactly one visible window.
    ///
    /// Pattern:
    ///   1. Open the new form.
    ///   2. Subscribe to the new form's FormClosed event.
    ///   3. On close, re-show the caller (so Back navigation works).
    ///   4. Hide (not Close) the caller while the child is open.
    /// </summary>
    public static class FormNavigator
    {
        /// <summary>
        /// Navigate from <paramref name="current"/> to the page identified by
        /// (<paramref name="menuLabel"/>, <paramref name="subItem"/>).
        /// </summary>
        public static void NavigateTo(Form current, string menuLabel, string subItem = "")
        {
            Form target = Resolve(menuLabel, subItem);
            if (target == null)
            {
                // Module not yet implemented
                string display = string.IsNullOrEmpty(subItem)
                    ? menuLabel
                    : $"{menuLabel}  ›  {subItem}";
                MessageBox.Show(
                    $"⌛  {display}\n\nThis feature is currently under development.",
                    "Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Same form — do nothing
            if (target.GetType() == current.GetType())
            {
                target.Dispose();
                return;
            }

            target.FormClosed += (s, e) => current.Show();
            current.Hide();
            target.Show();
        }

        // ── Routing table ─────────────────────────────────────────────────────
        private static Form Resolve(string menu, string sub)
        {
            // Normalise
            menu = menu?.Trim() ?? "";
            sub  = sub?.Trim()  ?? "";

            switch (menu)
            {
                case "Dashboard":
                    return new Dashboard.DashboardForm();

                case "Order Processing":
                    switch (sub)
                    {
                        case "View Order":    return new ViewOrderForm();
                        case "Quotation":     return new QuotationForm();
                        case "Create Order":  return new CreateOrderForm();
                        case "Modify Order":  return new ModifyOrderForm();
                        default:              return new ViewOrderForm(); // default tab
                    }

                // ── Additional modules added here as they are implemented ──────
                default:
                    return null;  // "Coming Soon" message shown by caller
            }
        }
    }
}
