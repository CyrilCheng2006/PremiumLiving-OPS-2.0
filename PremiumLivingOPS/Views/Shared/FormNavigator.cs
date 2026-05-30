using PremiumLivingOPS.Views.OrderProcessing;
using System;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.Shared
{
    /// <summary>
    /// Handles top-navigation routing for every Form in the application.
    ///
    /// Single-Window Pattern:
    ///   1. Show the target form (inheriting current window bounds/state).
    ///   2. Once target is fully painted, hide the current form.
    ///   3. When target is closed, dispose it — the current form is never
    ///      closed so Application.Run() keeps the message loop alive.
    /// </summary>
    public static class FormNavigator
    {
        public static void NavigateTo(Form current, string menuLabel, string subItem = "")
        {
            Form target = Resolve(menuLabel, subItem);

            if (target == null)
            {
                string display = string.IsNullOrEmpty(subItem)
                    ? menuLabel
                    : $"{menuLabel}  ›  {subItem}";
                MessageBox.Show(
                    $"⌛  {display}\n\nThis feature is currently under development.",
                    "Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Already on the same page — nothing to do.
            if (target.GetType() == current.GetType())
            {
                target.Dispose();
                return;
            }

            // Inherit window position and state for a seamless transition.
            target.StartPosition = FormStartPosition.Manual;
            target.Bounds        = current.Bounds;
            target.WindowState   = current.WindowState;

            // When the new page finishes loading and is first painted,
            // hide the old page. Using Shown (fired after first paint) so
            // there is no visible gap between the two forms.
            target.Shown += (s, e) =>
            {
                current.Hide();
            };

            // When the target is closed by the user, re-show the form it
            // replaced so the application is never left with no visible window.
            target.FormClosed += (s, e) =>
            {
                current.Show();
                target.Dispose();
            };

            target.Show();
        }

        // ── Routing table ─────────────────────────────────────────
        private static Form Resolve(string menu, string sub)
        {
            menu = menu?.Trim() ?? "";
            sub  = sub?.Trim()  ?? "";

            switch (menu)
            {
                case "Dashboard":
                    return new Dashboard.DashboardForm();

                case "Order Processing":
                    switch (sub)
                    {
                        case "View Order":   return new ViewOrderForm();
                        case "Quotation":    return new QuotationForm();
                        case "Create Order": return new CreateOrderForm();
                        case "Modify Order": return new ModifyOrderForm();
                        default:             return new ViewOrderForm();
                    }

                default:
                    return null;  // Coming Soon
            }
        }
    }
}
