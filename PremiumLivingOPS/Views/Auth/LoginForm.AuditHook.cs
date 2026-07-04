// Partial class companion — LoginForm.AuditHook.cs
// Provides WriteLoginAudit() called from LoginForm.cs after successful authentication.
using PremiumLivingOPS.Services;

namespace PremiumLivingOPS.Views.Auth
{
    public partial class LoginForm
    {
        /// <summary>
        /// Writes a Login audit row to the MySQL Log table.
        /// Uses WriteAs() because SessionManager.SetUser() has already been called
        /// and we want to record the exact staffId that just logged in.
        /// </summary>
        private static void WriteLoginAudit(string staffId)
            => AuditLogger.WriteAs(
                staffId,
                AuditLogger.TYPE_LOGIN,
                targetTable: "Staff",
                oldValue:    null,
                newValue:    AuditLogger.Snapshot(("Action", "Login"), ("StaffID", staffId)));
    }
}
