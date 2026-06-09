using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Controller (MVC middle layer) for the After-Service module.
    /// Accepts requests from View layer, delegates to AfterServiceRepo,
    /// and returns ViewModels. Contains NO UI code.
    /// </summary>
    public class AfterServiceController
    {
        private readonly AfterServiceRepo _repo = new AfterServiceRepo();

        // ── Helper: current user ────────────────────────────────────────────────────────
        private static UserBarViewModel CurrentUserBar()
        {
            var u = SessionManager.CurrentUser;
            return new UserBarViewModel
            {
                DisplayName = u?.StaffName  ?? "Unknown",
                Department  = u?.Department ?? ""
            };
        }

        private static string[] CurrentMenus()
            => NavAccessPolicy.GetAllowedMenus(SessionManager.CurrentUser?.Department ?? "");

        // ════════════════════════════════════════════════════════════════════
        //  Create Invoice
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns ViewModel for the Create Invoice page.
        /// Orders list contains only orders that have no Invoice yet.
        /// </summary>
        public CreateInvoiceViewModel GetCreateInvoiceVM()
        {
            return new CreateInvoiceViewModel
            {
                UserBar      = CurrentUserBar(),
                AllowedMenus = CurrentMenus(),
                Orders       = _repo.GetOrdersWithoutInvoice()
            };
        }

        /// <summary>
        /// Generates the next Invoice ID in the format INV-YYYYMMDD-NNNN.
        /// Queries the DB for the highest sequence number used today and increments it.
        /// </summary>
        public string GenerateInvoiceId()
        {
            string prefix   = "INV-" + DateTime.Today.ToString("yyyyMMdd") + "-";
            var    existing = _repo.GetInvoiceIdsByPrefix(prefix);
            int    next     = 1;
            foreach (var id in existing)
            {
                if (id.Length >= prefix.Length + 4 &&
                    int.TryParse(id.Substring(prefix.Length, 4), out int seq) &&
                    seq >= next)
                {
                    next = seq + 1;
                }
            }
            return $"{prefix}{next:D4}";
        }

        /// <summary>
        /// Saves a new invoice. Auto-generates InvoiceID if blank.
        /// Returns true on success.
        /// </summary>
        public bool SaveInvoice(InvoiceEntity inv)
        {
            if (string.IsNullOrWhiteSpace(inv.InvoiceID))
                inv.InvoiceID = GenerateInvoiceId();

            // Derive RemainingBalance and PaymentStatus from the amounts
            inv.RemainingBalance = Math.Max(0, inv.TotalAmount - inv.PaidAmount);
            inv.PaymentStatus    = inv.RemainingBalance <= 0 ? "Full" : "Partial";

            return _repo.CreateInvoice(inv);
        }

        // ════════════════════════════════════════════════════════════════════
        //  Complaint List
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns ViewModel for the Complaint List page.
        /// Supports optional status filter and keyword search.
        /// </summary>
        public ComplaintListViewModel GetComplaintListVM(
            string status  = null,
            string keyword = null)
        {
            return new ComplaintListViewModel
            {
                UserBar      = CurrentUserBar(),
                AllowedMenus = CurrentMenus(),
                Complaints   = _repo.SearchComplaints(status, keyword)
            };
        }

        /// <summary>Updates the status of a single complaint.</summary>
        public bool UpdateComplaintStatus(string complaintId, string newStatus)
            => _repo.UpdateComplaintStatus(complaintId, newStatus);

        // ════════════════════════════════════════════════════════════════════
        //  Return Order List
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns ViewModel for the Return Order List page.
        /// Supports optional status filter and keyword search.
        /// </summary>
        public ReturnOrderListViewModel GetReturnOrderListVM(
            string status  = null,
            string keyword = null)
        {
            return new ReturnOrderListViewModel
            {
                UserBar      = CurrentUserBar(),
                AllowedMenus = CurrentMenus(),
                ReturnOrders = _repo.SearchReturnOrders(status, keyword)
            };
        }

        /// <summary>Updates the status of a single return order.</summary>
        public bool UpdateReturnOrderStatus(string returnId, string newStatus)
            => _repo.UpdateReturnOrderStatus(returnId, newStatus);

        // ════════════════════════════════════════════════════════════════════
        //  Account Receivable
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns ViewModel for the Accounts Receivable page.
        /// status: null = All | 'Partial' | 'Full' | 'Overdue'
        /// keyword: searches InvoiceID, OrderID, CustomerName
        /// </summary>
        public AccountReceivableViewModel GetAccountReceivableVM(
            string status  = null,
            string keyword = null)
        {
            return new AccountReceivableViewModel
            {
                UserBar      = CurrentUserBar(),
                AllowedMenus = CurrentMenus(),
                Items        = _repo.SearchAccountReceivables(status, keyword)
            };
        }

        // ════════════════════════════════════════════════════════════════════
        //  Account Payable
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns ViewModel for the Accounts Payable page.
        /// status: null = All | 'Partial' | 'Full' | 'Overdue'
        /// keyword: searches PurInvoiceID, PurchaseID, SupplierName
        /// </summary>
        public AccountPayableViewModel GetAccountPayableVM(
            string status  = null,
            string keyword = null)
        {
            return new AccountPayableViewModel
            {
                UserBar      = CurrentUserBar(),
                AllowedMenus = CurrentMenus(),
                Items        = _repo.SearchAccountPayables(status, keyword)
            };
        }
    }
}
