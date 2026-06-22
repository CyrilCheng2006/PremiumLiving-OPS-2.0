using System;
using System.Collections.Generic;
using PremiumLivingOPS.Models.DAL;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Services;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Controller (MVC middle layer) for After-Service module.
    /// All DB-write operations are audit-logged via AuditLogger.
    /// Contains NO UI code.
    ///
    /// Partial split:
    ///   AfterServiceController.cs              ← this file (Invoice, Complaint, ReturnOrder READ, AccountPayable, AccountReceivable)
    ///   AfterServiceController.ReturnOrder.cs  ← ReturnOrder WRITE methods
    ///   AfterServiceController.APVerification.cs ← GetAPVerificationDetail
    /// </summary>
    public partial class AfterServiceController
    {
        private readonly AfterServiceRepo _repo = new AfterServiceRepo();

        // ── Return Order READ ───────────────────────────────────────────────
        // NOTE: CreateReturnOrder / UpdateReturnOrderStatus live in
        //       AfterServiceController.ReturnOrder.cs (partial). Only READ here.

        public List<ReturnOrderEntity> GetReturnOrders(string keyword = null, string status = null)
            => _repo.SearchReturnOrders(keyword, status);

        public string GenerateNextReturnOrderId() => _repo.GenerateNextReturnOrderId();

        // ── Account Payable ───────────────────────────────────────────────

        public List<AccountPayableEntity> GetAccountPayables(string keyword = null, string status = null)
            => _repo.SearchAccountPayables(keyword, status);

        // ── Account Receivable ────────────────────────────────────────────

        public List<AccountReceivableEntity> GetAccountReceivables(string keyword = null)
            => _repo.SearchAccountReceivables(keyword);

        // ── Invoice (Customer Invoice / A/R) ──────────────────────────

        public List<InvoiceEntity> GetInvoices(string keyword = null)
            => _repo.SearchInvoices(keyword);

        public string GenerateNextInvoiceId() => _repo.GenerateNextInvoiceId();

        /// <summary>Creates a Customer Invoice and logs the CREATE.</summary>
        public bool CreateInvoice(InvoiceEntity invoice)
        {
            bool ok = _repo.CreateInvoice(invoice);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_CREATE, "Invoice",
                    oldValue: null,
                    newValue: AuditLogger.Snapshot(
                        ("ID",      invoice.InvoiceID),
                        ("Order",   invoice.OrderID ?? ""),
                        ("Total",   invoice.TotalAmount.ToString("F2")),
                        ("Status",  invoice.PaymentStatus ?? ""),
                        ("DueDate", invoice.DueDate.ToString("yyyy-MM-dd"))));
            return ok;
        }

        /// <summary>Updates Invoice status and logs the EDIT.</summary>
        public bool UpdateInvoiceStatus(string invoiceId, string newStatus)
        {
            bool ok = _repo.UpdateInvoiceStatus(invoiceId, newStatus);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_EDIT, "Invoice",
                    oldValue: AuditLogger.Snapshot(("ID", invoiceId)),
                    newValue: AuditLogger.Snapshot(
                        ("ID",     invoiceId),
                        ("Status", newStatus)));
            return ok;
        }

        // ── Complaint ──────────────────────────────────────────────────

        public List<ComplaintEntity> GetComplaints(string keyword = null, string status = null)
            => _repo.SearchComplaints(keyword, status);

        /// <summary>Creates a Complaint and logs the CREATE.</summary>
        public bool CreateComplaint(ComplaintEntity complaint)
        {
            bool ok = _repo.CreateComplaint(complaint);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_CREATE, "Complaint",
                    oldValue: null,
                    newValue: AuditLogger.Snapshot(
                        ("ID",     complaint.ComplaintID),
                        ("Order",  complaint.OrderID ?? ""),
                        ("Status", complaint.ComplaintStatus ?? "")));
            return ok;
        }

        /// <summary>Updates Complaint status and logs the EDIT.</summary>
        public bool UpdateComplaintStatus(string complaintId, string newStatus)
        {
            bool ok = _repo.UpdateComplaintStatus(complaintId, newStatus);
            if (ok)
                AuditLogger.Write(AuditLogger.TYPE_EDIT, "Complaint",
                    oldValue: AuditLogger.Snapshot(("ID", complaintId)),
                    newValue: AuditLogger.Snapshot(
                        ("ID",     complaintId),
                        ("Status", newStatus)));
            return ok;
        }

        // NOTE: GetAPVerificationDetail is defined in
        //       AfterServiceController.APVerification.cs (partial).
    }
}
