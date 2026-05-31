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

        // ── View Order ────────────────────────────────────────────────────────────

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

        // ── Quotation ───────────────────────────────────────────────────────────

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

            if (!string.IsNullOrEmpty(status))
                all = all.FindAll(q => q.QuotationStatus == status);

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

        // ── Create Order ───────────────────────────────────────────────────────

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
                PendingQuotations = allQ.FindAll(q => q.QuotationStatus == "Pending"),
                NextOrderId       = GenerateOrderId()
            };
        }

        /// <summary>
        /// Generates the next Order ID in the format ORD-YYYYMMDD-NNNN.
        /// Queries the DB for the highest sequence number used today and increments it.
        /// Pure business logic — lives in Controller, not in View.
        /// </summary>
        public string GenerateOrderId()
        {
            string prefix = "ORD-" + DateTime.Today.ToString("yyyyMMdd") + "-";
            // Fetch all existing OrderIDs that start with today’s prefix
            var existing = _repo.GetOrderIdsByPrefix(prefix);
            int next = 1;
            if (existing.Count > 0)
            {
                // Parse the 4-digit sequence from each matching ID and take the max
                foreach (var id in existing)
                {
                    if (id.Length >= prefix.Length + 4 &&
                        int.TryParse(id.Substring(prefix.Length, 4), out int seq))
                    {
                        if (seq >= next) next = seq + 1;
                    }
                }
            }
            return $"{prefix}{next:D4}";
        }

        /// <summary>Saves a new order header + all line items. Returns true on success.</summary>
        public bool SaveNewOrder(OrderEntity order, List<OrderLineEntity> lines)
        {
            if (!_repo.CreateOrder(order)) return false;
            foreach (var l in lines)
            {
                l.OrderID = order.OrderID;
                _repo.CreateOrderLine(l);
            }
            return true;
        }

        // ── Modify Order ──────────────────────────────────────────────────────────

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
