using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Controller (MVC middle layer) for Order Processing.
    /// Accepts requests from View layer, delegates to Repo, returns ViewModels.
    /// Contains NO UI code.
    /// </summary>
    public class OrderProcessingController
    {
        private readonly OrderProcessingRepo _repo = new OrderProcessingRepo();

        // ── View Order ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns ViewModel for the View Order page.
        /// Supports optional status filter, keyword search, and date range filter.
        /// </summary>
        public ViewOrderViewModel GetViewOrderVM(
            string    status   = null,
            string    keyword  = null,
            DateTime? dateFrom = null,
            DateTime? dateTo   = null)
        {
            var user = SessionManager.CurrentUser;
            return new ViewOrderViewModel
            {
                UserBar = new UserBarViewModel
                {
                    DisplayName = user?.StaffName ?? "Unknown",
                    Department  = user?.Department ?? ""
                },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                Orders       = _repo.SearchOrders(status, keyword, dateFrom, dateTo)
            };
        }

        /// <summary>Returns full detail of one order including its line items.</summary>
        public OrderDetailViewModel GetOrderDetail(string orderId)
        {
            return new OrderDetailViewModel
            {
                Order = _repo.GetOrderById(orderId),
                Lines = _repo.GetOrderLines(orderId)
            };
        }

        /// <summary>Returns order line items for a given order.</summary>
        public List<OrderLineEntity> GetOrderLines(string orderId)
            => _repo.GetOrderLines(orderId);

        // ── Quotation ─────────────────────────────────────────────────────────

        /// <summary>
        /// Returns ViewModel for the Quotation page.
        /// Supports optional status filter and keyword search (QuotationID or CustomerName).
        /// Passing no arguments returns all quotations (used by RefreshKpi to get unfiltered counts).
        /// </summary>
        public QuotationViewModel GetQuotationVM(
            string status  = null,
            string keyword = null)
        {
            var user = SessionManager.CurrentUser;
            var all  = _repo.GetAllQuotations();

            // Apply status filter
            if (!string.IsNullOrEmpty(status))
                all = all.FindAll(q => q.QuotationStatus == status);

            // Apply keyword filter (QuotationID or CustomerName, case-insensitive)
            if (!string.IsNullOrEmpty(keyword))
            {
                string kw = keyword.ToLowerInvariant();
                all = all.FindAll(q =>
                    (q.QuotationID   ?? "").ToLowerInvariant().Contains(kw) ||
                    (q.CustomerName  ?? "").ToLowerInvariant().Contains(kw));
            }

            return new QuotationViewModel
            {
                UserBar = new UserBarViewModel
                {
                    DisplayName = user?.StaffName ?? "Unknown",
                    Department  = user?.Department ?? ""
                },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                Quotations   = all
            };
        }

        public bool UpdateQuotationStatus(string quotationId, string newStatus)
            => _repo.UpdateQuotationStatus(quotationId, newStatus);

        // ── Create Order ──────────────────────────────────────────────────────

        public CreateOrderViewModel GetCreateOrderVM()
        {
            var user  = SessionManager.CurrentUser;
            var allQ  = _repo.GetAllQuotations();
            return new CreateOrderViewModel
            {
                UserBar = new UserBarViewModel
                {
                    DisplayName = user?.StaffName ?? "Unknown",
                    Department  = user?.Department ?? ""
                },
                AllowedMenus      = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                Customers         = _repo.GetAllCustomers(),
                Products          = _repo.GetAllProducts(),
                Quotations        = allQ,
                PendingQuotations = allQ
                    .FindAll(q => q.QuotationStatus == "Pending")
            };
        }

        /// <summary>Saves a new order header + all line items. Returns true on success.</summary>
        public bool SaveNewOrder(OrderEntity order, List<OrderLineEntity> lines)
        {
            if (!_repo.CreateOrder(order)) return false;
            foreach (var l in lines) _repo.CreateOrderLine(l);
            return true;
        }

        // ── Modify Order ──────────────────────────────────────────────────────

        /// <summary>
        /// Returns ViewModel for the Modify Order page.
        /// Pass orderId to pre-load an existing order (e.g. launched from View Order).
        /// </summary>
        public ModifyOrderViewModel GetModifyOrderVM(string orderId = null)
        {
            var user = SessionManager.CurrentUser;
            return new ModifyOrderViewModel
            {
                UserBar = new UserBarViewModel
                {
                    DisplayName = user?.StaffName ?? "Unknown",
                    Department  = user?.Department ?? ""
                },
                AllowedMenus  = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                SelectedOrder = orderId != null ? _repo.GetOrderById(orderId)  : null,
                Lines         = orderId != null ? _repo.GetOrderLines(orderId) : new List<OrderLineEntity>(),
                Products      = _repo.GetAllProducts()
            };
        }

        public bool SaveOrderChanges(OrderEntity order, List<OrderLineEntity> lines)
        {
            if (!_repo.UpdateOrder(order)) return false;
            return _repo.ReplaceOrderLines(order.OrderID, lines);
        }

        public bool CancelOrder(string orderId)
            => _repo.UpdateOrderStatus(orderId, "Cancelled");
    }
}
