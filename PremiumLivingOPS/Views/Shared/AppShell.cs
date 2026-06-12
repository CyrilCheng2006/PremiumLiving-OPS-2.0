using System;
using System.Drawing;
using System.Windows.Forms;

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
    /// <remarks>
    /// ══════════════════════════════════════════════════════════════════
    /// CANONICAL Designer.cs WIRING RULES  (apply to EVERY Form using AppShell)
    /// ══════════════════════════════════════════════════════════════════
    ///
    /// RULE 1 — SuspendLayout() must be the VERY FIRST statement inside
    ///          InitializeComponent().  Every control is created while layout
    ///          is suspended.  Violating this causes AutoScaleMode = Font to
    ///          re-calculate control sizes on each Controls.Add() call.
    ///
    ///          // ✅ correct
    ///          private void InitializeComponent()
    ///          {
    ///              SuspendLayout();
    ///              // ... build all controls ...
    ///
    /// RULE 2 — AppShell must be constructed INSIDE the SuspendLayout scope
    ///          (i.e. before ResumeLayout / PerformLayout).  This prevents
    ///          AutoScaleMode = Font from resizing _shell during PerformLayout.
    ///
    ///          _shell = new AppShell();
    ///          _shell.Dock        = DockStyle.Top;                  // explicit
    ///          _shell.Height      = AppShell.TotalHeight;          // 116 px
    ///          _shell.MinimumSize = new Size(0, AppShell.TotalHeight);
    ///
    /// RULE 3 — After ResumeLayout(false) + PerformLayout(), set _shell.Height
    ///          AGAIN as a safety net against high-DPI scaling side-effects.
    ///
    ///          Controls.Add(pnlMain);
    ///          ResumeLayout(false);
    ///          PerformLayout();
    ///          // ↓ mandatory post-layout re-enforcement
    ///          _shell.Height      = AppShell.TotalHeight;
    ///          _shell.MinimumSize = new Size(0, AppShell.TotalHeight);
    ///
    /// RULE 4 — Subscribe MenuItemClicked and LogoutClicked HERE in Designer.cs,
    ///          ONCE.  The .cs Load / constructor must NOT repeat these.
    ///          Duplicate subscriptions cause every click to fire twice.
    ///
    ///          _shell.MenuItemClicked += OnTopNavMenuItemClicked;  // once only
    ///          _shell.LogoutClicked   += btnLogout_Click;          // once only
    ///
    /// RULE 5 — pnlMain.Controls add order: Fill first, Top second.
    ///          DockStyle.Top controls stack in reverse add-order; adding _shell
    ///          last guarantees it sits at the very top of pnlMain.
    ///
    ///          pnlMain.Controls.Add(pnlPage);   // DockStyle.Fill — content area
    ///          pnlMain.Controls.Add(_shell);    // DockStyle.Top  — chrome (wins)
    ///
    /// Quick reference — height constants (public const int):
    ///   AppShell.NavBarHeight  =  44 px   (TopNavBar.FixedHeight)
    ///   AppShell.UserBarHeight =  72 px   (UserBar.FixedHeight)
    ///   AppShell.TotalHeight   = 116 px
    ///
    /// ══════════════════════════════════════════════════════════════════
    /// TEMPLATE — paste into every new Form's Designer.cs InitializeComponent
    /// ══════════════════════════════════════════════════════════════════
    ///
    ///   private void InitializeComponent()
    ///   {
    ///       SuspendLayout();                                        // RULE 1
    ///
    ///       // ... build pnlPage, pnlScroll, cards, grids here ...
    ///
    ///       _shell = new AppShell();                               // RULE 2
    ///       _shell.Dock        = DockStyle.Top;                    // RULE 2 — explicit
    ///       _shell.Height      = AppShell.TotalHeight;
    ///       _shell.MinimumSize = new Size(0, AppShell.TotalHeight);
    ///       _shell.MenuItemClicked += OnTopNavMenuItemClicked;     // RULE 4
    ///       _shell.LogoutClicked   += btnLogout_Click;             // RULE 4
    ///
    ///       var pnlMain = new Panel { Dock = DockStyle.Fill, ... };
    ///       _shell.SetPopupContainer(pnlMain);
    ///       pnlMain.Controls.Add(pnlPage);                        // RULE 5 — Fill first
    ///       pnlMain.Controls.Add(_shell);                         // RULE 5 — Top second
    ///
    ///       Text          = "Module – Page Title";
    ///       MinimumSize   = new Size(1280, 800);
    ///       WindowState   = FormWindowState.Maximized;
    ///       AutoScaleMode = AutoScaleMode.Font;
    ///       AutoScaleDimensions = new SizeF(7F, 15F);
    ///
    ///       Controls.Add(pnlMain);
    ///       ResumeLayout(false);
    ///       PerformLayout();
    ///       _shell.Height      = AppShell.TotalHeight;            // RULE 3
    ///       _shell.MinimumSize = new Size(0, AppShell.TotalHeight); // RULE 3
    ///   }
    ///
    /// ══════════════════════════════════════════════════════════════════
    /// </remarks>
    public class AppShell : Panel
    {
        // ── Heights ────────────────────────────────────────────────────────────────
        public const int NavBarHeight  = TopNavBar.FixedHeight;  //  44 px
        public const int UserBarHeight = UserBar.FixedHeight;    //  72 px
        public const int TotalHeight   = NavBarHeight + UserBarHeight; // 116 px

        // ── Child controls ───────────────────────────────────────────────────────────
        private readonly TopNavBar _topNavBar;
        private readonly UserBar   _userBar;

        // ── Public events ───────────────────────────────────────────────────────────
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
            // Height-locked internally by TopNavBar.OnLayout + ScaleControl.
            _topNavBar = new TopNavBar();
            _topNavBar.MenuItemClicked += (menu, sub) =>
            {
                _userBar.UpdateBreadcrumb(menu, sub); // auto-update breadcrumb
                MenuItemClicked?.Invoke(menu, sub);
            };

            // ── UserBar ──────────────────────────────────────────────────────────────
            // Height-locked internally by UserBar.OnLayout + ScaleControl.
            _userBar = new UserBar();
            _userBar.LogoutClicked += (s, e) => LogoutClicked?.Invoke(s, e);

            // ── Compose: TopNavBar (Top) then UserBar (Top) ──────────────────────
            // DockStyle.Top controls stack in the order they are added;
            // adding TopNavBar first pins it to the very top, UserBar below.
            Controls.Add(_userBar);   // added first  → bottom of the Top stack
            Controls.Add(_topNavBar); // added second → top    of the Top stack
        }

        // ── ScaleControl override ────────────────────────────────────────────────────
        /// <summary>
        /// Vetoes WinForms AutoScaleMode = Font from scaling AppShell's own
        /// MinimumSize or Height.  Child panels are already self-protecting
        /// via their own ScaleControl overrides (TopNavBar, UserBar).
        /// </summary>
        protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
        {
            base.ScaleControl(
                new SizeF(factor.Width, 1.0f),
                specified & ~BoundsSpecified.Height);

            MinimumSize = new Size(0, TotalHeight);
            if (Height != TotalHeight) Height = TotalHeight;
        }

        // ── Height lock ──────────────────────────────────────────────────────────────
        /// <summary>
        /// Locks AppShell's own outer height only.
        /// Children (TopNavBar, UserBar) self-lock via their own OnLayout.
        /// </summary>
        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (Height != TotalHeight)
            {
                Height      = TotalHeight;
                MinimumSize = new Size(0, TotalHeight);
            }
        }

        // ── Public API ──────────────────────────────────────────────────────────────
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
    }
}
