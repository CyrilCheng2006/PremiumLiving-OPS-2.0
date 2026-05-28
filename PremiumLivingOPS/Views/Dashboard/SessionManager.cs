using PremiumLivingOPS.Models.Entities;

namespace PremiumLivingOPS.Views.Dashboard
{
    /// <summary>
    /// Holds the currently logged-in staff member for the lifetime of the session.
    /// Set by LoginForm after successful authentication.
    /// </summary>
    public static class SessionManager
    {
        public static Staff CurrentUser { get; set; }

        public static bool IsLoggedIn => CurrentUser != null;

        public static void Clear() => CurrentUser = null;
    }
}
