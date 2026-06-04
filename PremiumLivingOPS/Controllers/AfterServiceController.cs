using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;
using System;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Controller (MVC middle layer) for After-Service module.
    /// Accepts requests from the View layer, delegates to AfterServiceRepo,
    /// and returns typed ViewModels.  Contains NO UI code.
    /// </summary>
    public class AfterServiceController
    {
        private readonly AfterServiceRepo _repo = new AfterServiceRepo();

        // ════════════════════════════════════════════════════════════════
        //  Shared helper — current user shortcut
        // ════════════════════════════════════════════════════════════════
        private static UserBarViewModel BuildUserBar()
        {
            var user = SessionManager.CurrentUser;
            return new UserBarViewModel
            {
                DisplayName = user?.StaffName  ?? "Unknown",
                Department  = user?.Department ?? ""
            };
        }

        private static string[] GetMenus()
        {
            var dept = SessionManager.CurrentUser?.Department ?? "";
            return NavAccessPolicy.GetAllowedMenus(dept);
        }

        // ════════════════════════════════════════════════════════════════
        //  CREATE INVOICE
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Builds the ViewModel for the Create Invoice page.
        /// Includes all orders that do not yet have an invoice.
        /// </summary>
        public CreateInvoiceViewModel GetCreateInvoiceVM()
        {
            return new CreateInvoiceViewModel
            {
                AllowedMenus = GetMenus(),
                UserBar      = BuildUserBar(),
                Orders       = _repo.GetOrdersWithoutInvoice()
            };
        }

        /// <summary>
        /// Persists a new invoice.
        /// If InvoiceID is empty, one is auto-generated (INV-YYYYMMDD-NNNN).
        /// </summary>
        public bool SaveInvoice(InvoiceEntity inv)
        {
            if (string.IsNullOrWhiteSpace(inv.InvoiceID))
                inv.InvoiceID = _repo.GenerateInvoiceId();

            if (inv.InvoiceDate == DateTime.MinValue)
                inv.InvoiceDate = DateTime.Today;

            return _repo.CreateInvoice(inv);
        }

        // ════════════════════════════════════════════════════════════════
        //  COMPLAINT LIST
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns the ViewModel for the Complaint List page.
        /// Optional filters: status (Pending|Processing|Escalated|Completed) and keyword.
        /// </summary>
        public ComplaintListViewModel GetComplaintListVM(string status = null, string keyword = null)
        {
            return new ComplaintListViewModel
            {
                AllowedMenus = GetMenus(),
                UserBar      = BuildUserBar(),
                Complaints   = _repo.SearchComplaints(status, keyword)
            };
        }

        /// <summary>Updates the status of a complaint. Returns true on success.</summary>
        public bool UpdateComplaintStatus(string id, string status)
            => _repo.UpdateComplaintStatus(id, status);

        // ════════════════════════════════════════════════════════════════
        //  RETURN ORDER LIST
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns the ViewModel for the Return Order List page.
        /// Optional filters: status (Pending|Approved|Processing|Rejected|Completed) and keyword.
        /// </summary>
        public ReturnOrderListViewModel GetReturnOrderListVM(string status = null, string keyword = null)
        {
            return new ReturnOrderListViewModel
            {
                AllowedMenus = GetMenus(),
                UserBar      = BuildUserBar(),
                ReturnOrders = _repo.SearchReturnOrders(status, keyword)
            };
        }

        /// <summary>Updates the status of a return order. Returns true on success.</summary>
        public bool UpdateReturnOrderStatus(string id, string status)
            => _repo.UpdateReturnOrderStatus(id, status);

        // ════════════════════════════════════════════════════════════════
        //  ACCOUNTS RECEIVABLE
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns the ViewModel for the Accounts Receivable page.
        /// Optional status filter: "Partial" | "Full" | "Overdue" (null = all).
        /// </summary>
        public AccountReceivableViewModel GetAccountReceivableVM(string status = null)
        {
            return new AccountReceivableViewModel
            {
                AllowedMenus = GetMenus(),
                UserBar      = BuildUserBar(),
                Items        = _repo.GetAccountReceivables(status)
            };
        }

        // ════════════════════════════════════════════════════════════════
        //  ACCOUNTS PAYABLE
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns the ViewModel for the Accounts Payable page.
        /// Optional status filter: "Partial" | "Full" | "Overdue" (null = all).
        /// </summary>
        public AccountPayableViewModel GetAccountPayableVM(string status = null)
        {
            return new AccountPayableViewModel
            {
                AllowedMenus = GetMenus(),
                UserBar      = BuildUserBar(),
                Items        = _repo.GetAccountPayables(status)
            };
        }
    }
}
