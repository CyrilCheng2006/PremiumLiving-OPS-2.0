using PremiumLivingOPS.Models.Entities;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Partial controller extension for the AP 3-Way Match Verification feature.
    /// All DB access is delegated to AfterServiceRepo.APVerification.cs (zero SQL here).
    /// </summary>
    public partial class AfterServiceController
    {
        // ══════════════════════════════════════════════════════════════════════════════
        //  GetAPVerificationDetail
        //
        //  Returns the full 3-way match detail for a given PurchaseInvoice ID.
        //  The View layer calls this when the user clicks [AP Verification] and
        //  selects a row in the Account Payable grid.
        //
        //  Returns null when no matching record exists.
        // ══════════════════════════════════════════════════════════════════════════════
        public APVerificationDetailVM GetAPVerificationDetail(string purInvoiceId)
            => _repo.GetAPVerificationDetail(purInvoiceId);
    }
}
