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

        // ── Helper: current user ──────────────────────────────────────────────
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

        // ── Return Order: Create ─────────────────────────────────────────────

        /// <summary>
        /// Returns existing ReturnIDs that start with the given prefix.
        /// Used by CreateReturnOrderDialog to compute the next daily sequence number
        /// in the format RTN-YYYYMMDD-XXXX.
        /// </summary>
        public List<string> GetReturnIdsByPrefix(string prefix)
            => _repo.GetReturnIdsByPrefix(prefix);

        /// <summary>
        /// Generates the next ReturnID in the format RTN-YYYYMMDD-XXXX.
        /// </summary>
        public string GenerateReturnId()
        {
            string prefix   = "RTN-" + DateTime.Today.ToString("yyyyMMdd") + "-";
            var    existing = GetReturnIdsByPrefix(prefix);
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

        /// <summary>
        /// Returns orders eligible for return (Delivered / Completed / Partially Delivered),
        /// with optional keyword filter on OrderID or CustomerName.
        /// Used to populate the Order ID Picker in Create Return Order.
        /// </summary>
        public List<OrderEntity> GetOrdersForReturnPicker(string keyword = null)
            => _repo.GetOrdersForReturnPicker(keyword);

        /// <summary>
        /// Returns staff list with Department and Role columns.
        /// Used to populate the Staff Picker in Create Return Order.
        /// </summary>
        public List<(string StaffID, string StaffName, string Department, string StaffRole)> GetStaffListForPicker()
            => _repo.GetStaffListForPicker();

        /// <summary>
        /// Saves a new ReturnOrder. Generates ReturnID automatically if not supplied.
        /// </summary>
        public bool CreateReturnOrder(ReturnOrderEntity r)
        {
            if (string.IsNullOrWhiteSpace(r.ReturnID))
                r.ReturnID = GenerateReturnId();
            if (string.IsNullOrWhiteSpace(r.ReturnStatus))
                r.ReturnStatus = "Pending";
            return _repo.CreateReturnOrder(r);
        }

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
