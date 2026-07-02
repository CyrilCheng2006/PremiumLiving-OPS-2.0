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

        // ── In-memory Quotation item cache ───────────────────────────────────
        // Used for fast within-session reads (Detail view, Modify dialog).
        // Items are also persisted to DB via a staging Order row so they
        // survive application restarts — see SaveNewQuotation / SaveModifiedQuotation.
        private static readonly Dictionary<string, List<QuotationItemEntity>> _quotationItemCache
            = new Dictionary<string, List<QuotationItemEntity>>(StringComparer.OrdinalIgnoreCase);

        // ── View Order ──────────────────────────────────────────────────────────────────────

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

        // ── Quotation ──────────────────────────────────────────────────────────────────────

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

        /// <summary>
        /// Returns a single Quotation for detail view, with Items populated.
        ///
        /// Priority:
        ///   1. _quotationItemCache — fast in-session hit.
        ///   2. DB via GetOrderLinesByQuotationId() — reads from both staging
        ///      Order (STG-{QuotationID}) and any real converted Orders.
        ///   3. Empty list — fallback.
        /// </summary>
        public QuotationEntity GetQuotationDetail(string quotationId)
        {
            if (string.IsNullOrEmpty(quotationId)) return null;

            var q = _repo.GetQuotationById(quotationId);
            if (q == null) return null;

            if (_quotationItemCache.TryGetValue(quotationId, out var cached))
            {
                q.Items = new List<QuotationItemEntity>(cached);
            }
            else
            {
                var fromDb = _repo.GetOrderLinesByQuotationId(quotationId);
                if (fromDb != null && fromDb.Count > 0)
                {
                    _quotationItemCache[quotationId] = new List<QuotationItemEntity>(fromDb);
                    q.Items = new List<QuotationItemEntity>(fromDb);
                }
                else
                {
                    q.Items = new List<QuotationItemEntity>();
                }
            }

            return q;
        }

        public bool UpdateQuotationStatus(string quotationId, string newStatus)
            => _repo.UpdateQuotationStatus(quotationId, newStatus);

        // ── Modify Quotation ───────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true only when a REAL (non-staging) Order references this
        /// QuotationID. Staging rows (STG- prefix) are intentionally excluded
        /// so that a Pending Quotation with only a staging row is NOT blocked
        /// from the Modify flow.
        /// </summary>
        public bool IsQuotationLinkedToOrder(string quotationId)
        {
            if (string.IsNullOrEmpty(quotationId)) return false;
            var q = _repo.GetQuotationById(quotationId);
            if (q == null) return false;
            if (q.QuotationStatus == "Converted") return true;
            // Use the dedicated repo method that excludes STG- rows
            return _repo.HasRealOrderLinkedToQuotation(quotationId);
        }

        /// <summary>
        /// Persists the updated item list for a Quotation.
        /// Steps:
        ///   1. Update Quotation.TotalAmount in DB.
        ///   2. Recreate the staging Order + OrderLine rows (idempotent).
        ///   3. Warm the in-memory cache.
        /// </summary>
        public bool SaveModifiedQuotation(string quotationId, List<QuotationItemEntity> items)
        {
            if (string.IsNullOrEmpty(quotationId) || items == null) return false;

            double newTotal = items.Sum(i => i.Subtotal);

            bool ok = _repo.UpdateQuotationTotalAmount(quotationId, newTotal);
            if (!ok) return false;

            if (items.Count > 0)
            {
                var q = _repo.GetQuotationById(quotationId);
                if (q != null)
                {
                    var user = SessionManager.CurrentUser;
                    _repo.CreateStagingOrderForQuotation(
                        quotationId,
                        q.CustomerID,
                        user?.StaffID ?? "",
                        newTotal,
                        items);
                }
            }

            _quotationItemCache[quotationId] = new List<QuotationItemEntity>(items);
            return true;
        }

        public List<ProductLookup> GetAvailableItemsForQuotation(string customerId)
            => _repo.GetAllProducts();

        // ── Create New Quotation ──────────────────────────────────────────────────────────────

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
            string prefix   = "QT-" + DateTime.Today.ToString("yyyyMMdd") + "-";
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

        /// <summary>
        /// Saves a new Quotation and persists its items to the DB.
        /// Steps:
        ///   1. INSERT Quotation header row.
        ///   2. INSERT a staging Order (STG-{QuotationID}) + OrderLine rows.
        ///   3. Warm in-memory cache.
        /// </summary>
        public bool SaveNewQuotation(QuotationEntity quotation,
                                     List<QuotationItemEntity> items,
                                     string salesStaffId)
        {
            bool ok = _repo.CreateQuotation(quotation);
            if (!ok) return false;

            if (items != null && items.Count > 0)
            {
                _repo.CreateStagingOrderForQuotation(
                    quotation.QuotationID,
                    quotation.CustomerID,
                    string.IsNullOrEmpty(salesStaffId) ? (SessionManager.CurrentUser?.StaffID ?? "") : salesStaffId,
                    quotation.TotalAmount,
                    items);

                _quotationItemCache[quotation.QuotationID] = new List<QuotationItemEntity>(items);
            }

            return true;
        }

        // ── Create Order ──────────────────────────────────────────────────────────────────

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
            string prefix   = "ORD-" + DateTime.Today.ToString("yyyyMMdd") + "-";
            var    existing = _repo.GetOrderIdsByPrefix(prefix);
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

        /// <summary>
        /// Creates a new real Order. If converted from a Quotation, the
        /// staging row is cleaned up automatically.
        /// </summary>
        public bool SaveNewOrder(OrderEntity order, List<OrderLineEntity> lines)
        {
            if (!_repo.CreateOrder(order)) return false;

            foreach (var l in lines)
            {
                l.OrderID = order.OrderID;
                _repo.CreateOrderLine(l);
            }

            if (!string.IsNullOrEmpty(order.QuotationID))
            {
                _repo.DeleteStagingOrderByQuotationId(order.QuotationID);
                _quotationItemCache.Remove(order.QuotationID);
            }

            return true;
        }

        // ── Modify Order ──────────────────────────────────────────────────────────────────

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
