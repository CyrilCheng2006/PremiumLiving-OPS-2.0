using PremiumLivingOPS.Views.AfterService;
using PremiumLivingOPS.Views.InventoryControl;
using PremiumLivingOPS.Views.LogisticsProcessing;
using PremiumLivingOPS.Views.OrderProcessing;
using System;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.Shared
{
    /// <summary>
    /// Handles top-navigation routing for every Form in the application.
    ///
    /// Single-Window Pattern:
    ///   1. Hide the current form immediately.
    ///   2. Show the target form (inheriting current window bounds/state).
    ///   3. When the target is closed, dispose it and re-show the previous
    ///      form so Application.Run() keeps the message loop alive.
    ///
    /// This ensures only ONE window is visible at any time — no new-window
    /// flash, no brief double-window state.
    /// </summary>
    public static class FormNavigator
    {
        public static void NavigateTo(Form current, string menuLabel, string subItem = "")
        {
            Form target = Resolve(menuLabel, subItem);

            if (target == null)
            {
                // Unimplemented routes — graceful fallback with Coming Soon message.
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

            // Inherit window position and state so the transition feels seamless.
            target.StartPosition = FormStartPosition.Manual;
            target.Bounds        = current.Bounds;
            target.WindowState   = current.WindowState;

            // Hide current form BEFORE showing target — only one window visible at a time.
            current.Hide();
            target.Show();

            // When the target is closed, re-show the form it replaced so the
            // application is never left with no visible window.
            target.FormClosed += (s, e) =>
            {
                current.Show();
                target.Dispose();
            };
        }

        // ── Routing table ───────────────────────────────────────────────────────────────
        private static Form Resolve(string menu, string sub)
        {
            menu = menu?.Trim() ?? "";
            sub  = sub?.Trim()  ?? "";

            switch (menu)
            {
                case "Dashboard":
                    return new Dashboard.DashboardForm();

                // ── Order Processing ───────────────────────────────────────────────
                case "Order Processing":
                    switch (sub)
                    {
                        case "View Order":   return new ViewOrderForm();
                        case "Quotation":    return new QuotationForm();
                        case "Create Order": return new CreateOrderForm();
                        case "Modify Order": return new ModifyOrderForm();
                        default:             return new ViewOrderForm();
                    }

                // ── Inventory Control ───────────────────────────────────────────────
                case "Inventory Control":
                    switch (sub)
                    {
                        case "View Product / Raw Material":
                        case "View Product":
                        case "View Raw Material":
                            return sub == "View Raw Material"
                                ? (Form) new ViewRawMaterialForm()
                                : (Form) new ViewProductForm();
                        default:
                            return new ViewProductForm();
                    }

                // ── Logistics Processing ────────────────────────────────────────────
                case "Logistics Processing":
                    switch (sub)
                    {
                        case "View Shipment":
                            return new ViewShipmentForm();
                        case "Handling Goods Received":
                            return new HandlingGoodsReceivedForm();
                        default:
                            return new ViewShipmentForm();
                    }

                // ── After-Service ───────────────────────────────────────────────────
                case "After-Service":
                    switch (sub)
                    {
                        case "Create Invoice":      return new CreateInvoiceForm();
                        case "Complaint List":       return new ComplaintListForm();
                        case "Return Order List":    return new ReturnOrderListForm();
                        case "Account Receivable":   return new AccountReceivableForm();
                        case "Account Payable":      return new AccountPayableForm();
                        default:                     return new CreateInvoiceForm();
                    }

                default:
                    return null;  // Coming Soon for any unimplemented menu
            }
        }
    }
}
