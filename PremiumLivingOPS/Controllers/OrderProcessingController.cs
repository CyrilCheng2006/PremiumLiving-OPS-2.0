using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Controller (MVC middle layer) for Order Processing.
    /// Accepts requests from View layer, delegates to Repo, returns ViewModels.
    /// Contains NO UI code.
    /// All DB-write operations (Quotation / Order) are audit-logged.
    /// </summary>
    public class OrderProcessingController
    {
        private readonly OrderProcessingRepo _repo = new OrderProcessingRepo();

        // ── In-memory Quotation item store ──────────────────────────────────────────
        // Schema has no QuotationItem table. Items entered during Create/Modify are kept
        // in this dictionary so Detail can display them within the same session.
        private static readonly Dictionary<string, List<QuotationItemEntity>> _quotationItemCache
            = new Dictionary<string, List<QuotationItemEntity>>(StringComparer.OrdinalIgnoreCase);

        // ── View Order ─────────────────────────────────────────────────────────────

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

        // ── Quotation ──────────────────────────────────────────────────────────────

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
        /// Priority: session cache → DB fallback → empty list.
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
        {
            var old = _repo.GetQuotationById(quotationId);
            string oldSnap = old == null ? quotationId
                : AuditLogger.Snapshot(
                    ("ID",     old.QuotationID),
                    ("Status", old.QuotationStatus ?? ""),
                    ("Cust",   old.CustomerName ?? ""));

            bool ok = _repo.UpdateQuotationStatus(quotationId, newStatus);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_EDIT, "Quotation",
                    oldValue: oldSnap,
                    newValue: AuditLogger.Snapshot(("ID", quotationId), ("Status", newStatus)));
            return ok;
        }

        // ── Modify Quotation ───────────────────────────────────────────────────────

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
        /// Persists the updated item list for a Quotation and logs the EDIT.
        /// </summary>
        public bool SaveModifiedQuotation(string quotationId, List<QuotationItemEntity> items)
        {
            if (string.IsNullOrEmpty(quotationId) || items == null) return false;
            double newTotal = 0;
            foreach (var i in items) newTotal += i.Subtotal;

            string oldSnap = AuditLogger.Snapshot(
                ("ID",    quotationId),
                ("Items", "modified"));

            bool ok = _repo.UpdateQuotationTotalAmount(quotationId, newTotal);
            if (ok)
            {
                _quotationItemCache[quotationId] = new List<QuotationItemEntity>(items);
                AuditLogger.Write(AuditLogger.TYPE_EDIT, "Quotation",
                    oldValue: oldSnap,
                    newValue: AuditLogger.Snapshot(
                        ("ID",    quotationId),
                        ("Total", newTotal.ToString("F2")),
                        ("Items", items.Count.ToString())));
            }
            return ok;
        }

        public List<ProductLookup> GetAvailableItemsForQuotation(string customerId)
            => _repo.GetAllProducts();

        // ── Create New Quotation ───────────────────────────────────────────────────

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
        /// Saves a new Quotation header to DB, caches items, and logs the CREATE.
        /// </summary>
        public bool SaveNewQuotation(QuotationEntity quotation,
                                     List<QuotationItemEntity> items,
                                     string salesStaffId)
        {
            bool ok = _repo.CreateQuotation(quotation);
            if (ok)
            {
                if (items != null && items.Count > 0)
                    _quotationItemCache[quotation.QuotationID] = new List<QuotationItemEntity>(items);

                double total = items?.Sum(i => i.Subtotal) ?? 0;
                AuditLogger.Write(AuditLogger.TYPE_CREATE, "Quotation",
                    oldValue: null,
                    newValue: AuditLogger.Snapshot(
                        ("ID",     quotation.QuotationID),
                        ("Cust",   quotation.CustomerID ?? ""),
                        ("Status", quotation.QuotationStatus ?? ""),
                        ("Total",  total.ToString("F2")),
                        ("Items",  (items?.Count ?? 0).ToString())));
            }
            return ok;
        }

        // ── Create Order ───────────────────────────────────────────────────────────

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

        /// <summary>Saves a new Order + its lines and logs the CREATE.</summary>
        public bool SaveNewOrder(OrderEntity order, List<OrderLineEntity> lines)
        {
            if (!_repo.CreateOrder(order)) return false;
            foreach (var l in lines)
            {
                l.OrderID = order.OrderID;
                _repo.CreateOrderLine(l);
            }

            AuditLogger.Write(AuditLogger.TYPE_CREATE, "SalesOrder",
                oldValue: null,
                newValue: AuditLogger.Snapshot(
                    ("ID",     order.OrderID),
                    ("Cust",   order.CustomerID ?? ""),
                    ("Status", order.OrderStatus ?? ""),
                    ("Total",  order.TotalAmount.ToString("F2")),
                    ("Lines",  lines.Count.ToString())));
            return true;
        }

        // ── Modify Order ───────────────────────────────────────────────────────────

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

        /// <summary>Saves order header + line changes and logs the EDIT.</summary>
        public bool SaveOrderChanges(OrderEntity order, List<OrderLineEntity> lines)
        {
            var old = _repo.GetOrderById(order.OrderID);
            string oldSnap = old == null ? order.OrderID
                : AuditLogger.Snapshot(
                    ("ID",     old.OrderID),
                    ("Status", old.OrderStatus ?? ""),
                    ("Total",  old.TotalAmount.ToString("F2")));

            if (!_repo.UpdateOrder(order)) return false;
            bool linesOk = _repo.ReplaceOrderLines(order.OrderID, lines);

            AuditLogger.Write(AuditLogger.TYPE_EDIT, "SalesOrder",
                oldValue: oldSnap,
                newValue: AuditLogger.Snapshot(
                    ("ID",     order.OrderID),
                    ("Status", order.OrderStatus ?? ""),
                    ("Total",  order.TotalAmount.ToString("F2")),
                    ("Lines",  lines.Count.ToString())));
            return linesOk;
        }

        /// <summary>Cancels an order and logs the DELETE (status → Cancelled).</summary>
        public bool CancelOrder(string orderId)
        {
            var old = _repo.GetOrderById(orderId);
            string oldSnap = old == null ? orderId
                : AuditLogger.Snapshot(
                    ("ID",     old.OrderID),
                    ("Status", old.OrderStatus ?? ""),
                    ("Cust",   old.CustomerID ?? ""));

            bool ok = _repo.UpdateOrderStatus(orderId, "Cancelled");
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_DELETE, "SalesOrder",
                    oldValue: oldSnap,
                    newValue: AuditLogger.Snapshot(("ID", orderId), ("Status", "Cancelled")));
            return ok;
        }
    }
}
