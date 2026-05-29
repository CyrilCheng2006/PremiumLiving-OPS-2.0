using PremiumLivingOPS.Models.Entities;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Centralised session store (Controller layer).
    ///
    /// Holds the currently authenticated <see cref="Staff"/> for the lifetime
    /// of the application process.  All modules that need to know "who is
    /// logged in" read from here — no View may query the database directly
    /// for session state.
    ///
    /// MVC contract:
    ///   LoginForm  (View)  → calls SessionManager.SetUser()  after authentication.
    ///   DashboardController → reads SessionManager.CurrentUser to build the ViewModel.
    ///   DashboardForm (View) → receives user display data through the ViewModel only.
    /// </summary>
    public static class SessionManager
    {
        // ── State ─────────────────────────────────────────────────────
        private static Staff _currentUser;

        /// <summary>The staff member who is currently logged in, or <c>null</c> if nobody is.</summary>
        public static Staff CurrentUser => _currentUser;

        /// <summary>Returns <c>true</c> when a staff member is authenticated.</summary>
        public static bool IsLoggedIn => _currentUser != null;

        // ── Mutators ──────────────────────────────────────────────────

        /// <summary>
        /// Called by the Auth controller / LoginForm after a successful login.
        /// </summary>
        public static void SetUser(Staff staff)
        {
            _currentUser = staff;
        }

        /// <summary>
        /// Clears the session (called on logout).
        /// </summary>
        public static void Clear()
        {
            _currentUser = null;
        }
    }
}
