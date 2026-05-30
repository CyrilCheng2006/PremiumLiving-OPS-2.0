using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Controller for the Order Processing management module.
    ///
    /// MVC contract:
    ///   • Reads session state from SessionManager (never from UI).
    ///   • Calls NavAccessPolicy to determine permitted menus.
    ///   • Delegates all DB access to OrderProcessingRepo.
    ///   • Returns ViewModels to the View layer; contains NO UI dependencies.
    /// </summary>
    public class OrderProcessingController
    {
        private readonly OrderProcessingRepo _repo = new OrderProcessingRepo();

        // ════════════════════════════════════════════════════════════════
        //  Tab 1 — View Order
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Builds the ViewModel for ViewOrderForm.
        /// Optionally filters by status (""/null = all orders).
        /// </summary>
        public ViewOrderViewModel GetViewOrderVM(string statusFilter = null)
        {
            var user    = SessionManager.CurrentUser;
            var allowed = NavAccessPolicy.GetAllowedMenus(user?.Department);

            var orders = string.IsNullOrEmpty(statusFilter)
                ? _repo.GetAllOrders()
                : _repo.GetOrdersByStatus(statusFilter);

            return new ViewOrderViewModel
            {
                UserBar = new UserBarInfo
                {
                    DisplayName    = user != null ? $"{user.FirstName} {user.LastName}" : "Guest",
                    Role           = user?.JobTitle ?? "",
                    AvatarInitials = GetInitials(user)
                },
                AllowedMenus = allowed,
                Orders       = orders
            };
        }

        /// <summary>Returns the line items for a specific order.</summary>
        public List<OrderLineEntity> GetOrderLines(string orderId)
            => _repo.GetOrderLines(orderId);

        // ════════════════════════════════════════════════════════════════
        //  Tab 2 — Quotation
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Builds the ViewModel for QuotationForm.
        /// Returns all quotations.
        /// </summary>
        public QuotationViewModel GetQuotationVM()
        {
            var user    = SessionManager.CurrentUser;
            var allowed = NavAccessPolicy.GetAllowedMenus(user?.Department);

            return new QuotationViewModel
            {
                UserBar = new UserBarInfo
                {
                    DisplayName    = user != null ? $"{user.FirstName} {user.LastName}" : "Guest",
                    Role           = user?.JobTitle ?? "",
                    AvatarInitials = GetInitials(user)
                },
                AllowedMenus = allowed,
                Quotations   = _repo.GetAllQuotations()
            };
        }

        /// <summary>Updates the status of a quotation. Returns true on success.</summary>
        public bool UpdateQuotationStatus(string quotationId, string newStatus)
            => _repo.UpdateQuotationStatus(quotationId, newStatus);

        // ════════════════════════════════════════════════════════════════
        //  Tab 3 — Create Order
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Builds the ViewModel for CreateOrderForm.
        /// Loads drop-down data: customers, products, pending quotations.
        /// </summary>
        public CreateOrderViewModel GetCreateOrderVM()
        {
            var user    = SessionManager.CurrentUser;
            var allowed = NavAccessPolicy.GetAllowedMenus(user?.Department);

            return new CreateOrderViewModel
            {
                UserBar = new UserBarInfo
                {
                    DisplayName    = user != null ? $"{user.FirstName} {user.LastName}" : "Guest",
                    Role           = user?.JobTitle ?? "",
                    AvatarInitials = GetInitials(user)
                },
                AllowedMenus      = allowed,
                Customers         = _repo.GetAllCustomers(),
                Products          = _repo.GetAllProducts(),
                PendingQuotations = _repo.GetPendingQuotations()
            };
        }

        /// <summary>
        /// Persists a new order and its line items.
        /// Automatically stamps IssuedTime, assigns SalesID from session, and
        /// sets OrderStatus to "Pending".
        /// Returns (success, message).
        /// </summary>
        public (bool ok, string message) SubmitCreateOrder(
            OrderEntity          header,
            List<OrderLineEntity> lines)
        {
            var user = SessionManager.CurrentUser;
            if (user == null)
                return (false, "Session expired. Please log in again.");

            if (string.IsNullOrWhiteSpace(header.OrderID))
                return (false, "Order ID cannot be empty.");
            if (string.IsNullOrWhiteSpace(header.CustomerID))
                return (false, "Please select a customer.");
            if (lines == null || lines.Count == 0)
                return (false, "At least one order line is required.");

            header.SalesID     = user.StaffID;
            header.IssuedTime  = DateTime.Now;
            header.OrderStatus = "Pending";

            // Recalculate totals server-side for integrity
            double sub = 0;
            foreach (var l in lines) sub += l.Quantity * l.Price;
            header.SubTotal   = sub;
            header.GrandTotal = sub - header.DiscountAmount;

            try
            {
                if (!_repo.CreateOrder(header))
                    return (false, "Failed to save order header.");

                foreach (var line in lines)
                {
                    line.OrderID = header.OrderID;
                    if (!_repo.CreateOrderLine(line))
                        return (false, $"Order header saved but failed to insert line {line.ItemID}.");
                }

                // Mark source quotation as Converted if linked
                if (!string.IsNullOrEmpty(header.QuotationID))
                    _repo.UpdateQuotationStatus(header.QuotationID, "Converted");

                return (true, "Order created successfully.");
            }
            catch (Exception ex)
            {
                return (false, "Database error: " + ex.Message);
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  Tab 4 — Modify Order
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Builds the ViewModel for ModifyOrderForm.
        /// Loads all orders and the product catalogue for line-item editing.
        /// </summary>
        public ModifyOrderViewModel GetModifyOrderVM()
        {
            var user    = SessionManager.CurrentUser;
            var allowed = NavAccessPolicy.GetAllowedMenus(user?.Department);

            return new ModifyOrderViewModel
            {
                UserBar = new UserBarInfo
                {
                    DisplayName    = user != null ? $"{user.FirstName} {user.LastName}" : "Guest",
                    Role           = user?.JobTitle ?? "",
                    AvatarInitials = GetInitials(user)
                },
                AllowedMenus = allowed,
                Orders       = _repo.GetAllOrders(),
                Customers    = _repo.GetAllCustomers(),
                Products     = _repo.GetAllProducts()
            };
        }

        /// <summary>
        /// Persists changes to an existing order (header + lines).
        /// Only status, delivery date, addresses, discount, contact, and lines may change.
        /// Returns (success, message).
        /// </summary>
        public (bool ok, string message) SubmitModifyOrder(
            OrderEntity           header,
            List<OrderLineEntity> lines)
        {
            if (string.IsNullOrWhiteSpace(header.OrderID))
                return (false, "Order ID is missing.");
            if (lines == null || lines.Count == 0)
                return (false, "At least one order line is required.");

            // Guard: Cancelled orders cannot be edited
            if (header.OrderStatus == "Cancelled")
                return (false, "Cancelled orders cannot be modified.");

            // Recalculate totals server-side for integrity
            double sub = 0;
            foreach (var l in lines) sub += l.Quantity * l.Price;
            header.SubTotal   = sub;
            header.GrandTotal = sub - header.DiscountAmount;

            try
            {
                if (!_repo.UpdateOrder(header))
                    return (false, "Failed to update order header.");
                if (!_repo.ReplaceOrderLines(header.OrderID, lines))
                    return (false, "Header updated but failed to replace order lines.");

                return (true, "Order updated successfully.");
            }
            catch (Exception ex)
            {
                return (false, "Database error: " + ex.Message);
            }
        }

        /// <summary>
        /// Cancels an existing order by setting its OrderStatus to "Cancelled".
        /// Business rules enforced:
        ///   • Only orders in "Pending" or "Confirmed" status may be cancelled.
        ///   • "Delivered" and "Completed" orders cannot be cancelled.
        /// Returns (success, message).
        /// </summary>
        public (bool ok, string message) CancelOrder(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
                return (false, "Order ID is missing.");

            // Load current status to enforce business rules
            var order = _repo.GetOrderById(orderId);
            if (order == null)
                return (false, $"Order '{orderId}' not found.");

            if (order.OrderStatus == "Cancelled")
                return (false, "This order is already cancelled.");

            if (order.OrderStatus == "Delivered" || order.OrderStatus == "Completed")
                return (false,
                    $"Orders with status '{order.OrderStatus}' cannot be cancelled.");

            try
            {
                bool ok = _repo.UpdateOrderStatus(orderId, "Cancelled");
                return ok
                    ? (true,  $"Order '{orderId}' has been cancelled.")
                    : (false, "Failed to cancel the order. Please try again.");
            }
            catch (Exception ex)
            {
                return (false, "Database error: " + ex.Message);
            }
        }

        // ── Helper ──────────────────────────────────────────────────────────────────────
        private static string GetInitials(Staff user)
        {
            if (user == null) return "?";
            string f = user.FirstName?.Length > 0 ? user.FirstName[0].ToString() : "";
            string l = user.LastName?.Length  > 0 ? user.LastName[0].ToString()  : "";
            return (f + l).ToUpper();
        }
    }
}
