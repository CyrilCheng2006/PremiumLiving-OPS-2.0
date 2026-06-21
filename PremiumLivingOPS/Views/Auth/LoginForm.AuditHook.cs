// Partial class companion — LoginForm.AuditHook.cs
// Hooks successful login event to write an AuditLogger entry.
// No additional UI code in this file; the hook is called from LoginForm.cs
// after SessionManager.Login() succeeds.
using PremiumLivingOPS.Services;

namespace PremiumLivingOPS.Views.Auth
{
    public partial class LoginForm
    {
        /// <summary>
        /// Call this immediately after a successful login (SessionManager.Login succeeded).
        /// Writes a Login audit entry: LogType=Login, TargetTable=Staff, NewValue=staffId.
        /// </summary>
        private static void WriteLoginAudit(string staffId)
            => AuditLogger.Write(
                AuditLogger.TYPE_LOGIN,
                targetTable: "Staff",
                oldValue: null,
                newValue: AuditLogger.Snapshot(("StaffID", staffId)));
    }
}
