using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using PremiumLivingOPS.Services;
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Controller (MVC middle layer) for After-Service module.
    /// All DB-write operations (PurchaseInvoice, ReturnOrder, AccountPayable) are audit-logged.
    /// Contains NO UI code.
    /// </summary>
    public partial class AfterServiceController
    {
        private readonly AfterServiceRepo _repo = new AfterServiceRepo();

        // ── Purchase Invoice ───────────────────────────────────────────

        public PurchaseInvoiceListViewModel GetPurchaseInvoiceListVM(
            string keyword = null,
            string status  = null)
        {
            var user = SessionManager.CurrentUser;
            return new PurchaseInvoiceListViewModel
            {
                UserBar      = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                Invoices     = _repo.SearchPurchaseInvoices(keyword, status)
            };
        }

        public PurchaseInvoiceDetailViewModel GetPurchaseInvoiceDetailVM(string invoiceId)
        {
            var user = SessionManager.CurrentUser;
            return new PurchaseInvoiceDetailViewModel
            {
                UserBar      = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                Invoice      = _repo.GetPurchaseInvoiceById(invoiceId)
            };
        }

        public string GenerateNextInvoiceId() => _repo.GenerateNextInvoiceId();

        /// <summary>Creates a Purchase Invoice and logs the CREATE.</summary>
        public bool CreatePurchaseInvoice(PurchaseInvoiceEntity invoice)
        {
            bool ok = _repo.CreatePurchaseInvoice(invoice);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_CREATE, "PurchaseInvoice",
                    oldValue: null,
                    newValue: AuditLogger.Snapshot(
                        ("ID",       invoice.PurchaseInvoiceID),
                        ("PO",       invoice.PurchaseOrderID ?? ""),
                        ("Total",    invoice.TotalAmount.ToString("F2")),
                        ("Status",   invoice.InvoiceStatus ?? ""),
                        ("DueDate",  invoice.DueDate?.ToString("yyyy-MM-dd") ?? "")));
            return ok;
        }

        /// <summary>Updates Invoice status and logs the EDIT.</summary>
        public bool UpdateInvoiceStatus(string invoiceId, string newStatus)
        {
            var old = _repo.GetPurchaseInvoiceById(invoiceId);
            string oldSnap = old == null ? invoiceId
                : AuditLogger.Snapshot(
                    ("ID",     old.PurchaseInvoiceID),
                    ("Status", old.InvoiceStatus ?? ""));

            bool ok = _repo.UpdateInvoiceStatus(invoiceId, newStatus);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_EDIT, "PurchaseInvoice",
                    oldValue: oldSnap,
                    newValue: AuditLogger.Snapshot(
                        ("ID",     invoiceId),
                        ("Status", newStatus)));
            return ok;
        }

        // ── Return Order ───────────────────────────────────────────────

        public ReturnOrderListViewModel GetReturnOrderListVM(
            string keyword = null,
            string status  = null)
        {
            var user = SessionManager.CurrentUser;
            return new ReturnOrderListViewModel
            {
                UserBar      = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                ReturnOrders = _repo.SearchReturnOrders(keyword, status)
            };
        }

        public string GenerateNextReturnOrderId() => _repo.GenerateNextReturnOrderId();

        /// <summary>Creates a Return Order and logs the CREATE.</summary>
        public bool CreateReturnOrder(ReturnOrderEntity ro)
        {
            bool ok = _repo.CreateReturnOrder(ro);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_CREATE, "ReturnOrder",
                    oldValue: null,
                    newValue: AuditLogger.Snapshot(
                        ("ID",      ro.ReturnOrderID),
                        ("Order",   ro.OrderID ?? ""),
                        ("Reason",  ro.ReturnReason ?? ""),
                        ("Status",  ro.ReturnStatus ?? ""),
                        ("Total",   ro.TotalRefund.ToString("F2"))));
            return ok;
        }

        /// <summary>Updates Return Order status and logs the EDIT.</summary>
        public bool UpdateReturnOrderStatus(string returnId, string newStatus)
        {
            var old = _repo.GetReturnOrderById(returnId);
            string oldSnap = old == null ? returnId
                : AuditLogger.Snapshot(
                    ("ID",     old.ReturnOrderID),
                    ("Status", old.ReturnStatus ?? ""),
                    ("Order",  old.OrderID ?? ""));

            bool ok = _repo.UpdateReturnOrderStatus(returnId, newStatus);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_EDIT, "ReturnOrder",
                    oldValue: oldSnap,
                    newValue: AuditLogger.Snapshot(
                        ("ID",     returnId),
                        ("Status", newStatus)));
            return ok;
        }

        // ── Account Payable ────────────────────────────────────────────

        public AccountPayableListViewModel GetAccountPayableListVM(
            string keyword = null,
            string status  = null)
        {
            var user = SessionManager.CurrentUser;
            return new AccountPayableListViewModel
            {
                UserBar         = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus    = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                AccountPayables = _repo.SearchAccountPayables(keyword, status)
            };
        }

        // ── Supplier Receipt ───────────────────────────────────────────

        public SupplierReceiptListViewModel GetSupplierReceiptListVM(string keyword = null)
        {
            var user = SessionManager.CurrentUser;
            return new SupplierReceiptListViewModel
            {
                UserBar      = new UserBarViewModel { DisplayName = user?.StaffName ?? "Unknown", Department = user?.Department ?? "" },
                AllowedMenus = NavAccessPolicy.GetAllowedMenus(user?.Department ?? ""),
                Receipts     = _repo.SearchSupplierReceipts(keyword)
            };
        }

        public string GenerateNextReceiptId() => _repo.GenerateNextReceiptId();

        /// <summary>Records a Supplier Receipt and logs the CREATE.</summary>
        public bool CreateSupplierReceipt(SupplierReceiptEntity receipt)
        {
            bool ok = _repo.CreateSupplierReceipt(receipt);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_CREATE, "SupplierReceipt",
                    oldValue: null,
                    newValue: AuditLogger.Snapshot(
                        ("ID",       receipt.SupplierReceiptID),
                        ("PO",       receipt.PurchaseOrderID ?? ""),
                        ("Supplier", receipt.SupplierID ?? ""),
                        ("Total",    receipt.TotalAmount.ToString("F2")),
                        ("Date",     receipt.ReceiptDate.ToString("yyyy-MM-dd"))));
            return ok;
        }
    }
}
