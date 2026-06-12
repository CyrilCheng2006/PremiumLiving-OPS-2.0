using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Controller for the Order Processing module.
    /// Bridges Views ↔ OrderProcessingRepo (DAL).
    /// </summary>
    public class OrderProcessingController
    {
        private readonly OrderProcessingRepo _repo = new OrderProcessingRepo();

        // ── Session (set by AppShell after login) ───────────────────────────────
        public static Staff   CurrentStaff { get; set; }

        // ── Quotation ───────────────────────────────────────────────────────────

        /// <summary>Returns the list-view ViewModel for the Quotation Form.</summary>
        public QuotationViewModel GetQuotationListVM()
        {
            var staff      = CurrentStaff;
            var quotations = _repo.GetAllQuotations();
            return new QuotationViewModel
            {
                UserBar = new UserBarViewModel
                {
                    StaffName    = staff?.StaffName ?? "Unknown",
                    StaffRole    = staff?.Role      ?? string.Empty,
                    AllowedMenus = new string[0]
                },
                AllowedMenus = new string[0],
                Quotations   = quotations
            };
        }

        /// <summary>
        /// Builds the ViewModel needed to open the Create New Quotation dialog.
        /// Includes lookup data: customers, products, next QuotationID, staff info.
        /// </summary>
        public CreateQuotationViewModel GetCreateQuotationVM()
        {
            // Line 116 fix: use StaffID (correct property) not StaffId
            var staff = CurrentStaff;
            return new CreateQuotationViewModel
            {
                UserBar = new UserBarViewModel
                {
                    StaffName    = staff?.StaffName ?? "Unknown",
                    StaffRole    = staff?.Role      ?? string.Empty,
                    AllowedMenus = new string[0]
                },
                AllowedMenus   = new string[0],
                Customers      = _repo.GetAllCustomers(),
                Products       = _repo.GetAllProducts(),
                NextQuotationId= _repo.GenerateNextQuotationId(),
                SalesStaffName = staff?.StaffName ?? "Unknown",
                SalesStaffId   = staff?.StaffID   ?? string.Empty   // StaffID — correct property
            };
        }

        /// <summary>Persists a new Quotation + its line items to the database.</summary>
        public bool SaveNewQuotation(QuotationEntity quotation,
                                     List<QuotationItemEntity> items,
                                     string salesStaffId)
        {
            try
            {
                return _repo.InsertQuotation(quotation, items, salesStaffId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[OrderProcessingController] SaveNewQuotation: " + ex.Message);
                return false;
            }
        }

        // ── Order ─────────────────────────────────────────────────────────────────

        public ViewOrderViewModel GetViewOrderVM()
        {
            var staff  = CurrentStaff;
            var orders = _repo.GetAllOrders();
            return new ViewOrderViewModel
            {
                UserBar = new UserBarViewModel
                {
                    StaffName    = staff?.StaffName ?? "Unknown",
                    StaffRole    = staff?.Role      ?? string.Empty,
                    AllowedMenus = new string[0]
                },
                AllowedMenus = new string[0],
                Orders       = orders
            };
        }

        public CreateOrderViewModel GetCreateOrderVM()
        {
            var staff = CurrentStaff;
            return new CreateOrderViewModel
            {
                UserBar = new UserBarViewModel
                {
                    StaffName    = staff?.StaffName ?? "Unknown",
                    StaffRole    = staff?.Role      ?? string.Empty,
                    AllowedMenus = new string[0]
                },
                AllowedMenus      = new string[0],
                Customers         = _repo.GetAllCustomers(),
                Addresses         = new List<AddressLookup>(),
                Products          = _repo.GetAllProducts(),
                Quotations        = _repo.GetAllQuotations(),
                PendingQuotations = _repo.GetPendingQuotations(),
                NextOrderId       = _repo.GenerateNextOrderId()
            };
        }

        public bool SaveNewOrder(OrderEntity order, List<OrderLineEntity> lines)
        {
            try   { return _repo.InsertOrder(order, lines); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[OrderProcessingController] SaveNewOrder: " + ex.Message);
                return false;
            }
        }

        public List<AddressLookup> GetAddressesByCustomer(string customerId)
            => _repo.GetAddressesByCustomer(customerId);

        public QuotationEntity GetQuotationDetail(string quotationId)
            => _repo.GetQuotationDetail(quotationId);

        public OrderDetailViewModel GetOrderDetail(string orderId)
        {
            var order = _repo.GetOrderById(orderId);
            var lines = _repo.GetOrderLines(orderId);
            return new OrderDetailViewModel { Order = order, Lines = lines };
        }

        public bool UpdateOrderStatus(string orderId, string newStatus)
        {
            try   { return _repo.UpdateOrderStatus(orderId, newStatus); }
            catch { return false; }
        }

        public bool UpdateQuotationStatus(string quotationId, string newStatus)
        {
            try   { return _repo.UpdateQuotationStatus(quotationId, newStatus); }
            catch { return false; }
        }
    }
}
