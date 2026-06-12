using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Models.Entities;

namespace PremiumLivingOPS.Views.Shared
{
    /// <summary>
    /// AppShell — reusable chrome panel that composes TopNavBar + UserBar.
    ///
    /// Structure
    /// ─────────
    ///   AppShell (Panel, DockStyle.Top, Height = 116 px)
    ///   ├── TopNavBar  (44 px, self-locking via TopNavBar.OnLayout + ScaleControl)
    ///   └── UserBar    (72 px, self-locking via UserBar.OnLayout  + ScaleControl)
    ///
    /// Usage (any Form)
    /// ────────────────
    ///   1. Declare and add to the form (see Designer.cs canonical rules below):
    ///          private AppShell _shell;
    ///          _shell = new AppShell();
    ///          pnlMain.Controls.Add(_shell);
    ///
    ///   2. After loading your ViewModel, call:
    ///          _shell.SetUser(displayName, department);
    ///          _shell.SetVisibleMenus(vm.AllowedMenus);
    ///          _shell.SetBreadcrumb("Dashboard");
    ///
    ///   3. Subscribe to events:
    ///          _shell.MenuItemClicked += OnMenuItemClicked;   // (menuLabel, subItem)
    ///          _shell.LogoutClicked   += OnLogoutClicked;
    ///
    /// HEIGHT CONTRACT
    /// ───────────────
    ///   AppShell.Height is ALWAYS TotalHeight (116 px), enforced by:
    ///     1. OnLayout     — re-locks after every layout pass.
    ///     2. ScaleControl — vetoes AutoScaleMode = Font from shrinking
    ///                        MinimumSize/Height during PerformLayout.
    ///   Each child is independently self-locking:
    ///     TopNavBar : 44 px (TopNavBar.OnLayout + TopNavBar.ScaleControl)
    ///     UserBar   : 72 px (UserBar.OnLayout   + UserBar.ScaleControl)
    ///   AppShell.OnLayout no longer needs to re-lock children; their own
    ///   overrides handle it. AppShell only locks its own outer height.
    ///
    /// Breadcrumb auto-update
    /// ──────────────────────
    ///   AppShell subscribes internally to TopNavBar.MenuItemClicked and
    ///   automatically forwards the breadcrumb update to UserBar:
    ///     • Dashboard (top-level, no sub-item)  →  "Dashboard"
    ///     • Module click with sub-item           →  "Order Processing  ›  View Order"
    ///   The host form only calls SetBreadcrumb() once for the initial page;
    ///   subsequent nav clicks update it automatically.
    /// </summary>
    public class AppShell : Panel
    {
        // ── Heights ──────────────────────────────────────────────────────────────────
        public const int NavBarHeight  = TopNavBar.FixedHeight;  //  44 px
        public const int UserBarHeight = UserBar.FixedHeight;    //  72 px
        public const int TotalHeight   = NavBarHeight + UserBarHeight; // 116 px

        // ── Child controls ───────────────────────────────────────────────────────────
        private readonly TopNavBar _topNavBar;
        private readonly UserBar   _userBar;

        // ── Public events ────────────────────────────────────────────────────────────
        public event Action<string, string> MenuItemClicked;
        public event EventHandler           LogoutClicked;

        // ── Constructor ──────────────────────────────────────────────────────────────
        public AppShell()
        {
            Dock        = DockStyle.Top;
            Height      = TotalHeight;
            MinimumSize = new Size(0, TotalHeight);
            BackColor   = Color.White;
            Padding     = new Padding(0);

            // ── TopNavBar ────────────────────────────────────────────────────────────
            _topNavBar = new TopNavBar();
            _topNavBar.MenuItemClicked += (menu, sub) =>
            {
                _userBar.UpdateBreadcrumb(menu, sub);
                MenuItemClicked?.Invoke(menu, sub);
            };

            // ── UserBar ──────────────────────────────────────────────────────────────
            _userBar = new UserBar();
            _userBar.LogoutClicked += (s, e) => LogoutClicked?.Invoke(s, e);

            // ── Compose ──────────────────────────────────────────────────────────────
            Controls.Add(_userBar);   // added first  → bottom of the Top stack
            Controls.Add(_topNavBar); // added second → top    of the Top stack
        }

        // ── ScaleControl override ────────────────────────────────────────────────────
        protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
        {
            base.ScaleControl(
                new SizeF(factor.Width, 1.0f),
                specified & ~BoundsSpecified.Height);

            MinimumSize = new Size(0, TotalHeight);
            if (Height != TotalHeight) Height = TotalHeight;
        }

        // ── Height lock ──────────────────────────────────────────────────────────────
        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (Height != TotalHeight)
            {
                Height      = TotalHeight;
                MinimumSize = new Size(0, TotalHeight);
            }
        }

        // ── Public API ───────────────────────────────────────────────────────────────

        /// <summary>Delegates to UserBar.SetUser().</summary>
        public void SetUser(string displayName, string department)
            => _userBar.SetUser(displayName, department);

        /// <summary>Delegates to TopNavBar.SetVisibleMenus().</summary>
        public void SetVisibleMenus(string[] allowedLabels)
            => _topNavBar.SetVisibleMenus(allowedLabels);

        /// <summary>Delegates to UserBar.SetBreadcrumb().</summary>
        public void SetBreadcrumb(string text)
            => _userBar.SetBreadcrumb(text);

        /// <summary>Gets the current breadcrumb from UserBar.</summary>
        public string Breadcrumb => _userBar.Breadcrumb;

        /// <summary>Delegates to TopNavBar.SetPopupContainer().</summary>
        public void SetPopupContainer(Control container)
            => _topNavBar.SetPopupContainer(container);

        /// <summary>
        /// Convenience method used by View forms that carry a UserBarViewModel.
        /// Calls SetUser + SetVisibleMenus + SetBreadcrumb in one call.
        /// </summary>
        public void ApplyViewModel(UserBarViewModel vm)
        {
            if (vm == null) return;
            SetUser(vm.DisplayName, vm.Department);
            if (vm.AllowedMenus != null)
                SetVisibleMenus(vm.AllowedMenus);
            if (!string.IsNullOrEmpty(vm.Breadcrumb))
                SetBreadcrumb(vm.Breadcrumb);
        }
    }
}
