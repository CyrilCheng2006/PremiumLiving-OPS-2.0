using PremiumLivingOPS.Views.OrderProcessing;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.Shared
{
    /// <summary>
    /// Handles top-navigation routing for every Form in the application.
    ///
    /// Single-Window Pattern:
    ///   The destination Form is shown maximised at the same screen position
    ///   as the current Form, then the current Form is closed.
    ///   This gives the illusion of in-place navigation with no extra window.
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

            // Already on the same page — do nothing.
            if (target.GetType() == current.GetType())
            {
                target.Dispose();
                return;
            }

            // Carry over window state so the transition feels seamless.
            target.StartPosition = FormStartPosition.Manual;
            target.Bounds        = current.Bounds;
            target.WindowState   = current.WindowState;

            // Show the new form, then close (not just hide) the old one.
            // Using Load event ensures target is fully initialised before
            // the current form disappears.
            target.Load += (s, e) =>
            {
                current.Close();
            };

            target.Show();
        }

        // ── Routing table ─────────────────────────────────────────────────
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
