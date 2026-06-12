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

        // ── View Order ─────────────────────────────────────────────────────────────────────────────────────

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

        public OrderDetailViewModel GetOrderDetail(string orderId)
        {
            return new OrderDetailViewModel
            {
                Order = _repo.GetOrderById(orderId),
                Lines = _repo.GetOrderLines(orderId)
            };
        }

        public List<OrderLineEntity> GetOrderLines(string orderId)
            => _repo.GetOrderLines(orderId);

        // ── Quotation ─────────────────────────────────────────────────────────────────────────────────

        public QuotationViewModel GetQuotationListVM(
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
                    (q.QuotationID  ?? "").ToLowerInvariant().Contains(kw) ||
                    (q.CustomerName ?? "").ToLowerInvariant().Contains(kw));
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

        public QuotationEntity GetQuotationDetail(string quotationId)
        {
            if (string.IsNullOrEmpty(quotationId)) return null;
            var q = _repo.GetQuotationById(quotationId);
            if (q == null) return null;
            q.Items = _repo.GetQuotationItems(quotationId);
            return q;
        }

        public bool UpdateQuotationStatus(string quotationId, string newStatus)
            => _repo.UpdateQuotationStatus(quotationId, newStatus);

        // ── Create New Quotation ────────────────────────────────────────────────────────────

        public CreateQuotationViewModel GetCreateQuotationVM()
        {
            var user = SessionManager.CurrentUser;
            return new CreateQuotationViewModel
            {
                UserBar = new UserBarViewModel
                {
                    DisplayName = user?.StaffName ?? "Unknown",
                    Department  = user?.Department ?? ""
                },
                AllowedMenus    = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                Customers       = _repo.GetAllCustomers(),
                Products        = _repo.GetAllProducts(),
                NextQuotationId = GenerateQuotationId(),
                SalesStaffName  = user?.StaffName ?? "",
                SalesStaffId    = user?.StaffID   ?? ""
            };
        }

        public string GenerateQuotationId()
        {
            string prefix   = "QUO-" + DateTime.Today.ToString("yyyyMMdd") + "-";
            var    existing = _repo.GetQuotationIdsByPrefix(prefix);
            int    next     = 1;
            foreach (var id in existing)
            {
                if (id.Length >= prefix.Length + 4 &&
                    int.TryParse(id.Substring(prefix.Length, 4), out int seq))
                {
                    if (seq >= next) next = seq + 1;
                }
            }
            return $"{prefix}{next:D4}";
        }

        public bool SaveNewQuotation(QuotationEntity quotation,
                                     List<QuotationItemEntity> items,
                                     string salesStaffId)
        {
            if (!_repo.CreateQuotation(quotation, salesStaffId)) return false;
            foreach (var item in items)
            {
                item.QuotationID = quotation.QuotationID;
                _repo.CreateQuotationItem(item);
            }
            return true;
        }

        // ── Create Order ──────────────────────────────────────────────────────────────────────────

        public CreateOrderViewModel GetCreateOrderVM()
        {
            var user = SessionManager.CurrentUser;
            var allQ = _repo.GetAllQuotations();
            return new CreateOrderViewModel
            {
                UserBar = new UserBarViewModel
                {
                    DisplayName = user?.StaffName ?? "Unknown",
                    Department  = user?.Department ?? ""
                },
                AllowedMenus      = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                Customers         = _repo.GetAllCustomers(),
                Addresses         = _repo.GetAllAddresses(),
                Products          = _repo.GetAllProducts(),
                Quotations        = allQ,
                PendingQuotations = allQ.FindAll(q => q.QuotationStatus == "Pending"),
                NextOrderId       = GenerateOrderId()
            };
        }

        public List<AddressLookup> GetAddressesByCustomer(string customerId,
            List<AddressLookup> allAddresses)
        {
            if (string.IsNullOrEmpty(customerId) || allAddresses == null)
                return new List<AddressLookup>();
            return allAddresses.FindAll(a => a.CustomerId == customerId);
        }

        public string GenerateOrderId()
        {
            string prefix    = "ORD-" + DateTime.Today.ToString("yyyyMMdd") + "-";
            var    existing  = _repo.GetOrderIdsByPrefix(prefix);
            int    next      = 1;
            foreach (var id in existing)
            {
                if (id.Length >= prefix.Length + 4 &&
                    int.TryParse(id.Substring(prefix.Length, 4), out int seq))
                {
                    if (seq >= next) next = seq + 1;
                }
            }
            return $"{prefix}{next:D4}";
        }

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

        // ── Modify Order ─────────────────────────────────────────────────────────────────────────────────

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
                Products      = _repo.GetAllProducts(),
                Customers     = _repo.GetAllCustomers(),
                Addresses     = _repo.GetAllAddresses()
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
