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
        // Items are NOW also persisted to DB via a staging Order row so they
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
        ///   2. DB via GetOrderLinesByQuotationId() — reads from both the
        ///      staging Order (STG-{QuotationID}) created at save time AND
        ///      any real converted Orders that reference this Quotation.
        ///      This covers cross-session restarts for Pending quotations.
        ///   3. Empty list — fallback when no data exists.
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

        public bool IsQuotationLinkedToOrder(string quotationId)
        {
            if (string.IsNullOrEmpty(quotationId)) return false;
            var q = _repo.GetQuotationById(quotationId);
            if (q == null) return false;
            if (q.QuotationStatus == "Converted") return true;
            var linkedOrders = _repo.GetOrderLinesByQuotationId(quotationId);
            return linkedOrders != null && linkedOrders.Count > 0;
        }

        /// <summary>
        /// Persists the updated item list for a Quotation.
        ///
        /// Steps:
        ///   1. Update Quotation.TotalAmount in DB.
        ///   2. Recreate the staging Order + OrderLine rows in DB so items
        ///      survive restarts (CreateStagingOrderForQuotation is idempotent
        ///      — it deletes existing staging rows before re-inserting).
        ///   3. Warm the in-memory cache.
        /// </summary>
        public bool SaveModifiedQuotation(string quotationId, List<QuotationItemEntity> items)
        {
            if (string.IsNullOrEmpty(quotationId) || items == null) return false;

            double newTotal = items.Sum(i => i.Subtotal);

            // 1. Update header total
            bool ok = _repo.UpdateQuotationTotalAmount(quotationId, newTotal);
            if (!ok) return false;

            // 2. Re-persist items to DB via staging Order
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

            // 3. Warm cache
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
        ///
        /// Steps:
        ///   1. INSERT Quotation header row (CreateQuotation).
        ///   2. INSERT a staging Order (OrderStatus = 'Pending',
        ///      OrderID = "STG-{QuotationID}", QuotationID = this Quotation)
        ///      plus OrderLine rows for each item. This is the DB workaround
        ///      for the missing QuotationItem table in schema.sql.
        ///   3. Warm the in-memory cache for the current session.
        ///
        /// The staging Order is invisible to order lists because callers filter
        /// by status; 'Pending' orders are only surfaced when specifically
        /// needed for quotation detail retrieval (GetOrderLinesByQuotationId).
        /// When the Quotation is later converted, the staging row should be
        /// cleaned up via DeleteStagingOrderByQuotationId.
        /// </summary>
        public bool SaveNewQuotation(QuotationEntity quotation,
                                     List<QuotationItemEntity> items,
                                     string salesStaffId)
        {
            // 1. Write Quotation header
            bool ok = _repo.CreateQuotation(quotation);
            if (!ok) return false;

            if (items != null && items.Count > 0)
            {
                // 2. Persist items to DB via staging Order
                _repo.CreateStagingOrderForQuotation(
                    quotation.QuotationID,
                    quotation.CustomerID,
                    string.IsNullOrEmpty(salesStaffId) ? (SessionManager.CurrentUser?.StaffID ?? "") : salesStaffId,
                    quotation.TotalAmount,
                    items);

                // 3. Warm cache
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
        /// Creates a new real Order from scratch (or converted from Quotation).
        /// If the order references a QuotationID, the staging Order created by
        /// SaveNewQuotation is cleaned up automatically.
        /// </summary>
        public bool SaveNewOrder(OrderEntity order, List<OrderLineEntity> lines)
        {
            if (!_repo.CreateOrder(order)) return false;

            foreach (var l in lines)
            {
                l.OrderID = order.OrderID;
                _repo.CreateOrderLine(l);
            }

            // Clean up staging Order if this Order was converted from a Quotation
            if (!string.IsNullOrEmpty(order.QuotationID))
            {
                _repo.DeleteStagingOrderByQuotationId(order.QuotationID);
                // Evict cache so GetQuotationDetail now reads from real Order lines
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
