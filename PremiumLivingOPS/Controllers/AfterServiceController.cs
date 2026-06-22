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
    public partial class AfterServiceController
    {
        private readonly AfterServiceRepo _repo = new AfterServiceRepo();

        // ── Helper: current user ─────────────────────────────────────────
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

        public CreateInvoiceViewModel GetCreateInvoiceVM()
        {
            return new CreateInvoiceViewModel
            {
                UserBar      = CurrentUserBar(),
                AllowedMenus = CurrentMenus(),
                Orders       = _repo.GetOrdersWithoutInvoice()
            };
        }

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
                    next = seq + 1;
            }
            return $"{prefix}{next:D4}";
        }

        public bool SaveInvoice(InvoiceEntity inv)
        {
            if (string.IsNullOrWhiteSpace(inv.InvoiceID))
                inv.InvoiceID = GenerateInvoiceId();
            inv.RemainingBalance = Math.Max(0, inv.TotalAmount - inv.PaidAmount);
            inv.PaymentStatus    = inv.RemainingBalance <= 0 ? "Full" : "Partial";
            return _repo.CreateInvoice(inv);
        }

        // ════════════════════════════════════════════════════════════════════
        //  Complaint List
        // ════════════════════════════════════════════════════════════════════

        public ComplaintListViewModel GetComplaintListVM(string status = null, string keyword = null)
        {
            return new ComplaintListViewModel
            {
                UserBar      = CurrentUserBar(),
                AllowedMenus = CurrentMenus(),
                Complaints   = _repo.SearchComplaints(status, keyword)
            };
        }

        public bool UpdateComplaintStatus(string complaintId, string newStatus)
            => _repo.UpdateComplaintStatus(complaintId, newStatus);

        public List<(string StaffID, string StaffName)> GetStaffList()
            => _repo.GetStaffList();

        public bool CreateComplaint(ComplaintEntity c)
        {
            if (string.IsNullOrWhiteSpace(c.ComplaintID))
                c.ComplaintID = _repo.GenerateComplaintId();
            if (string.IsNullOrWhiteSpace(c.ComplaintStatus))
                c.ComplaintStatus = "Pending";
            return _repo.CreateComplaint(c);
        }

        // ════════════════════════════════════════════════════════════════════
        //  Return Order List
        // ════════════════════════════════════════════════════════════════════

        public ReturnOrderListViewModel GetReturnOrderListVM(string status = null, string keyword = null)
        {
            return new ReturnOrderListViewModel
            {
                UserBar      = CurrentUserBar(),
                AllowedMenus = CurrentMenus(),
                ReturnOrders = _repo.SearchReturnOrders(status, keyword)
            };
        }

        public bool UpdateReturnOrderStatus(string returnId, string newStatus)
            => _repo.UpdateReturnOrderStatus(returnId, newStatus);

        // NOTE: Create / Picker / GenerateId methods live in AfterServiceController.ReturnOrder.cs

        // ════════════════════════════════════════════════════════════════════
        //  Account Receivable
        // ════════════════════════════════════════════════════════════════════

        public AccountReceivableViewModel GetAccountReceivableVM(string status = null, string keyword = null)
        {
            return new AccountReceivableViewModel
            {
                UserBar      = CurrentUserBar(),
                AllowedMenus = CurrentMenus(),
                Items        = _repo.SearchAccountReceivables(status, keyword)
            };
        }

        // ════════════════════════════════════════════════════════════════════
        //  Invoice List + Record Payment  (Account Receivable popup)
        // ════════════════════════════════════════════════════════════════════

        public InvoiceListViewModel GetInvoiceListVM(string keyword = null)
        {
            return new InvoiceListViewModel
            {
                UserBar      = CurrentUserBar(),
                AllowedMenus = CurrentMenus(),
                Invoices     = _repo.GetInvoiceDetails(keyword)
            };
        }

        public string GenerateTransactionId()
            => _repo.GenerateTransactionId();

        /// <summary>
        /// Records a payment transaction for an invoice.
        /// Amount must be > 0 and ≤ invoice RemainingBalance.
        /// </summary>
        public bool RecordPayment(string invoiceId, double amount, string txnType)
        {
            if (amount <= 0)
                throw new ArgumentException("Payment amount must be greater than zero.");

            var txn = new TransactionEntity
            {
                TransactionID   = GenerateTransactionId(),
                InvoiceID       = invoiceId,
                Amount          = amount,
                TransactionDate = DateTime.Today,
                TransactionType = txnType
            };
            return _repo.RecordPayment(txn);
        }

        // ════════════════════════════════════════════════════════════════════
        //  Account Payable
        // ════════════════════════════════════════════════════════════════════

        public AccountPayableViewModel GetAccountPayableVM(string status = null, string keyword = null)
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
