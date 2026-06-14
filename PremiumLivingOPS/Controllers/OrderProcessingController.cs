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

        // ── In-memory Quotation item store ──────────────────────────────────────────────────────────────────────────────────────────────────────
        // Schema has no QuotationItem table. Items entered during Create/Modify are kept
        // in this dictionary so Detail can display them within the same session.
        // Key = QuotationID, Value = list of items at time of last save.
        private static readonly Dictionary<string, List<QuotationItemEntity>> _quotationItemCache
            = new Dictionary<string, List<QuotationItemEntity>>(StringComparer.OrdinalIgnoreCase);

        // ── View Order ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

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

        // ── Quotation ──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

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
        ///   1. _quotationItemCache — populated in the current session by
        ///      SaveNewQuotation / SaveModifiedQuotation. Always up-to-date for
        ///      quotations the user just created or edited.
        ///   2. DB fallback via GetOrderLinesByQuotationId() — synthesises items
        ///      from OrderLine rows linked through Order.QuotationID FK.
        ///      Covers quotations that were converted to orders in a previous
        ///      session. Result is warmed into cache to avoid repeat DB calls.
        ///   3. Empty list — for Pending/Rejected quotations that have never had
        ///      an Order created from them and were loaded from a prior session.
        /// </summary>
        public QuotationEntity GetQuotationDetail(string quotationId)
        {
            if (string.IsNullOrEmpty(quotationId)) return null;

            var q = _repo.GetQuotationById(quotationId);
            if (q == null) return null;

            if (_quotationItemCache.TryGetValue(quotationId, out var cached))
            {
                // Session cache hit — most current data.
                q.Items = new List<QuotationItemEntity>(cached);
            }
            else
            {
                // Cache miss: attempt DB fallback via OrderLine → Order.QuotationID.
                var fromDb = _repo.GetOrderLinesByQuotationId(quotationId);
                if (fromDb != null && fromDb.Count > 0)
                {
                    // Warm cache so subsequent Detail opens are served in-memory.
                    _quotationItemCache[quotationId] = new List<QuotationItemEntity>(fromDb);
                    q.Items = new List<QuotationItemEntity>(fromDb);
                }
                else
                {
                    // Quotation is Pending/Rejected with no linked Order — no items available.
                    q.Items = new List<QuotationItemEntity>();
                }
            }

            return q;
        }

        public bool UpdateQuotationStatus(string quotationId, string newStatus)
            => _repo.UpdateQuotationStatus(quotationId, newStatus);

        // ── Modify Quotation ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true when the given Quotation has already been linked to at least
        /// one Order (i.e. its status is "Converted" or an Order row references it).
        /// Used by QuotationForm to guard the Modify action.
        /// </summary>
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
        /// Schema has no QuotationItem table — TotalAmount on the header is updated,
        /// and items are kept in _quotationItemCache for the current session.
        /// Returns true on success.
        /// </summary>
        public bool SaveModifiedQuotation(string quotationId, List<QuotationItemEntity> items)
        {
            if (string.IsNullOrEmpty(quotationId) || items == null) return false;
            double newTotal = 0;
            foreach (var i in items) newTotal += i.Subtotal;
            bool ok = _repo.UpdateQuotationTotalAmount(quotationId, newTotal);
            if (ok)
                _quotationItemCache[quotationId] = new List<QuotationItemEntity>(items);
            return ok;
        }

        /// <summary>
        /// Returns the product list available to add as Quotation items.
        /// </summary>
        public List<ProductLookup> GetAvailableItemsForQuotation(string customerId)
            => _repo.GetAllProducts();

        // ── Create New Quotation ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

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

        /// <summary>
        /// Saves a new Quotation header to DB and caches its items in-memory.
        /// Schema has no QuotationItem table — only the Quotation header row is written.
        /// Items are stored in _quotationItemCache so Detail can display them this session.
        /// </summary>
        public bool SaveNewQuotation(QuotationEntity quotation,
                                     List<QuotationItemEntity> items,
                                     string salesStaffId)
        {
            // salesStaffId is not a Quotation column in schema; ignored.
            bool ok = _repo.CreateQuotation(quotation);
            if (ok && items != null && items.Count > 0)
                _quotationItemCache[quotation.QuotationID] = new List<QuotationItemEntity>(items);
            return ok;
        }

        // ── Create Order ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

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

        // ── Modify Order ──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

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
