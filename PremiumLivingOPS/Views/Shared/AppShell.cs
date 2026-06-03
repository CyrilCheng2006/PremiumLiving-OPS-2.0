using System;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.Shared
{
    /// <summary>
    /// AppShell — reusable chrome panel containing TopNavBar + User Bar.
    ///
    /// Usage (any Form)
    /// ────────────────
    /// 1. Declare and add to the form:
    ///        private AppShell _shell;
    ///        _shell = new AppShell();
    ///        pnlMain.Controls.Add(_shell);
    ///
    /// 2. After loading your ViewModel, call:
    ///        _shell.SetUser(displayName, department);
    ///        _shell.SetVisibleMenus(vm.AllowedMenus);
    ///        _shell.SetBreadcrumb("Dashboard");
    ///
    /// 3. Subscribe to events:
    ///        _shell.MenuItemClicked += OnMenuItemClicked;   // (menuLabel, subItem)
    ///        _shell.LogoutClicked   += OnLogoutClicked;
    ///
    /// HEIGHT CONTRACT
    /// ───────────────
    /// AppShell.Height is ALWAYS TotalHeight (116 px), enforced by:
    ///   1. OnLayout   — re-locks after every layout pass
    ///   2. ScaleControl override — vetoes AutoScaleMode=Font from shrinking
    ///                              MinimumSize/Height during PerformLayout
    /// Inner panels are also re-locked on every layout pass:
    ///   TopNavBar  = NavBarHeight  = 44 px  (enforced inside TopNavBar.OnLayout)
    ///   pnlUserBar = UserBarHeight = 72 px  (enforced inside AppShell.OnLayout)
    /// AutoScaleMode = Font and DPI scaling cannot change these values.
    ///
    /// Breadcrumb auto-update
    /// ──────────────────────
    /// AppShell subscribes internally to TopNavBar.MenuItemClicked and
    /// automatically formats the breadcrumb:
    ///   • Dashboard (top-level, no sub-item)  →  "Dashboard"
    ///   • Module click with sub-item          →  "Order Processing  ›  View Order"
    /// The host form only needs to call SetBreadcrumb() for the initial page;
    /// subsequent nav clicks update the breadcrumb automatically.
    ///
    /// Layout strategy
    /// ───────────────
    /// The UserBar uses a 3-column TableLayoutPanel:
    ///   ┌────────────────┬──────────────────────┬──────────────────────┐
    ///   │  Breadcrumb    │      (stretch)        │  UserInfo | Logout   │
    ///   └────────────────┴──────────────────────┴──────────────────────┘
    ///   AutoSize           100% stretch                AutoSize
    ///
    /// Vertical centring
    /// ─────────────────
    /// tlpBar row is 100% height (72 px).  Every cell child uses
    /// Anchor = AnchorStyles.None so the TableLayoutPanel centres it
    /// both horizontally and vertically inside the cell.
    /// </summary>
    /// <remarks>
    /// ══════════════════════════════════════════════════════════════════════
    /// CANONICAL Designer.cs WIRING RULES  (apply to EVERY Form using AppShell)
    /// ══════════════════════════════════════════════════════════════════════
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
    /// Quick reference — height constants (defined as public const int above):
    ///   AppShell.NavBarHeight  =  44 px   (TopNavBar — also TopNavBar.FixedHeight)
    ///   AppShell.UserBarHeight =  72 px   (UserBar)
    ///   AppShell.TotalHeight   = 116 px
    ///
    /// ══════════════════════════════════════════════════════════════════════
    /// TEMPLATE — paste into every new Form's Designer.cs InitializeComponent
    /// ══════════════════════════════════════════════════════════════════════
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
    /// ══════════════════════════════════════════════════════════════════════
    /// </remarks>
    public class AppShell : Panel
    {
        // ── Heights ──────────────────────────────────────────────────
        public const int NavBarHeight  = 44;
        public const int UserBarHeight = 72;
        public const int TotalHeight   = NavBarHeight + UserBarHeight;

        // ── Child controls ───────────────────────────────────────────
        private readonly TopNavBar     _topNavBar;
        private readonly Panel         _pnlUserBar;   // kept for OnLayout height lock
        private readonly UserInfoLabel _lblUser;
        private readonly Label         _lblBreadcrumb;
        private readonly Button        _btnLogout;

        // ── Colours ────────────────────────────────────────────────
        private static readonly Color TextMain    = Color.FromArgb(15,  31,  53);
        private static readonly Color TextMuted   = Color.FromArgb(98,  112, 135);
        private static readonly Color BorderColor = Color.FromArgb(221, 227, 236);
        private static readonly Color Danger      = Color.FromArgb(232, 64,  64);

        // ── Public events ────────────────────────────────────────────
        public event Action<string, string> MenuItemClicked;
        public event EventHandler           LogoutClicked;

        // ── Constructor ──────────────────────────────────────────────
        public AppShell()
        {
            Dock        = DockStyle.Top;
            Height      = TotalHeight;
            MinimumSize = new Size(0, TotalHeight);
            BackColor   = Color.White;
            Padding     = new Padding(0);

            // ── TopNavBar (height locked internally via TopNavBar.OnLayout) ───
            _topNavBar = new TopNavBar();
            _topNavBar.MenuItemClicked += (menu, sub) =>
            {
                UpdateBreadcrumb(menu, sub);
                MenuItemClicked?.Invoke(menu, sub);
            };

            // ── Breadcrumb label ──────────────────────────────────
            _lblBreadcrumb = new Label
            {
                Text      = "Dashboard",
                Font      = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = TextMain,
                AutoSize  = true,
                Anchor    = AnchorStyles.None,
                Margin    = new Padding(22, 0, 0, 0)
            };

            // ── UserInfoLabel ─────────────────────────────────────
            _lblUser = new UserInfoLabel { UserName = "...", Department = "" };

            // ── Logout button ─────────────────────────────────────
            _btnLogout = new Button
            {
                Text         = "Log Out",
                Font         = new Font("Segoe UI", 9f),
                ForeColor    = Danger,
                BackColor    = Color.Transparent,
                FlatStyle    = FlatStyle.Flat,
                AutoSize     = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding      = new Padding(12, 0, 12, 0),
                Cursor       = Cursors.Hand
            };
            _btnLogout.FlatAppearance.BorderColor = Danger;
            _btnLogout.FlatAppearance.BorderSize  = 1;
            _btnLogout.Click += (s, e) => LogoutClicked?.Invoke(s, e);

            // ── Right sub-panel ─────────────────────────────────────
            Panel pnlRight = new Panel
            {
                AutoSize     = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor    = Color.Transparent,
                Anchor       = AnchorStyles.None
            };
            pnlRight.Controls.Add(_lblUser);
            pnlRight.Controls.Add(_btnLogout);
            pnlRight.Layout += (s, e) =>
            {
                _lblUser.PerformLayout();
                _btnLogout.PerformLayout();
                int h = pnlRight.Height;
                _lblUser.Left   = 0;
                _lblUser.Top    = (h - _lblUser.Height) / 2;
                _btnLogout.Left = _lblUser.Right + 8;
                _btnLogout.Top  = (h - _btnLogout.Height) / 2;
                pnlRight.Width  = _btnLogout.Right + 16;
            };

            // ── Bottom border ─────────────────────────────────────
            Panel border = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 1,
                BackColor = BorderColor
            };

            // ── UserBar TableLayoutPanel ─────────────────────────────
            TableLayoutPanel tlpBar = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 3,
                RowCount    = 1,
                BackColor   = Color.Transparent,
                Padding     = new Padding(0),
                Margin      = new Padding(0)
            };
            tlpBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tlpBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpBar.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tlpBar.Controls.Add(_lblBreadcrumb, 0, 0);
            tlpBar.Controls.Add(new Panel { BackColor = Color.Transparent }, 1, 0);
            tlpBar.Controls.Add(pnlRight, 2, 0);

            // ── UserBar panel (height locked by OnLayout + ScaleControl below) ──
            _pnlUserBar = new Panel
            {
                Dock        = DockStyle.Top,
                Height      = UserBarHeight,
                MinimumSize = new Size(0, UserBarHeight),
                BackColor   = Color.White
            };
            _pnlUserBar.Controls.Add(tlpBar);
            _pnlUserBar.Controls.Add(border);

            Controls.Add(_pnlUserBar);
            Controls.Add(_topNavBar);
        }

        // ── ScaleControl override ──────────────────────────────────────
        /// <summary>
        /// Vetoes WinForms AutoScaleMode=Font from scaling AppShell's own
        /// MinimumSize or Height.  Child controls (TopNavBar, _pnlUserBar)
        /// are also protected: TopNavBar via its own OnLayout; _pnlUserBar
        /// via AppShell.OnLayout below.  Rendering is never touched.
        /// </summary>
        protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
        {
            // Allow width scaling (horizontal layout is flexible).
            // Block height scaling entirely — the height contract is pixel-exact.
            base.ScaleControl(
                new SizeF(factor.Width, 1.0f),
                specified & ~BoundsSpecified.Height);

            // Re-enforce absolute values after base may have touched MinimumSize.
            MinimumSize = new Size(0, TotalHeight);
            if (Height != TotalHeight) Height = TotalHeight;
        }

        // ── Height lock ────────────────────────────────────────────────
        /// <summary>
        /// Called by WinForms after every layout pass.
        /// Re-locks AppShell outer height, UserBar height, and TopNavBar height
        /// so that AutoScaleMode = Font / DPI scaling can never shrink them.
        /// </summary>
        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);

            // Lock outer shell
            if (Height != TotalHeight)
            {
                Height      = TotalHeight;
                MinimumSize = new Size(0, TotalHeight);
            }

            // Lock UserBar (TopNavBar is self-locking via its own OnLayout)
            if (_pnlUserBar != null && _pnlUserBar.Height != UserBarHeight)
            {
                _pnlUserBar.Height      = UserBarHeight;
                _pnlUserBar.MinimumSize = new Size(0, UserBarHeight);
            }
        }

        // ── Breadcrumb ────────────────────────────────────────────────
        private void UpdateBreadcrumb(string menu, string sub)
        {
            _lblBreadcrumb.Text = string.IsNullOrEmpty(sub) ? menu : $"{menu}  ›  {sub}";
        }

        // ── Public API ───────────────────────────────────────────────
        public void SetUser(string displayName, string department)
        {
            _lblUser.UserName   = displayName;
            _lblUser.Department = department;
        }

        public void SetVisibleMenus(string[] allowedLabels)
            => _topNavBar.SetVisibleMenus(allowedLabels);

        public void SetBreadcrumb(string text)
            => _lblBreadcrumb.Text = text;

        public string Breadcrumb => _lblBreadcrumb.Text;

        public void SetPopupContainer(Control container)
            => _topNavBar.SetPopupContainer(container);
    }
}
