using PremiumLivingOPS.Controllers;
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
    /// 1. Add AppShell to the form's top:
    ///        private AppShell _shell;
    ///        _shell = new AppShell();
    ///        Controls.Add(_shell);
    ///
    /// 2. After loading your ViewModel, call:
    ///        _shell.SetUser(displayName, department);
    ///        _shell.SetVisibleMenus(vm.AllowedMenus);
    ///        _shell.SetBreadcrumb("Dashboard");
    ///
    /// 3. Subscribe to events:
    ///        _shell.MenuItemClicked += OnMenuItemClicked;
    ///        _shell.LogoutClicked   += OnLogoutClicked;
    ///
    /// AppShell height = TopNavBar (44 px) + UserBar (72 px) = 116 px.
    /// </summary>
    public class AppShell : Panel
    {
        // ── Heights ──────────────────────────────────────────────────
        public const int NavBarHeight = 44;
        public const int UserBarHeight = 72;
        public const int TotalHeight   = NavBarHeight + UserBarHeight;

        // ── Child controls ───────────────────────────────────────────
        private readonly TopNavBar     _topNavBar;
        private readonly UserInfoLabel _lblUser;
        private readonly Label         _lblBreadcrumb;
        private readonly Button        _btnLogout;
        private readonly Panel         _pnlUserBar;

        // ── Colours (mirrors DashboardForm.Palette) ──────────────────
        private static readonly Color TextMain   = Color.FromArgb(15,  31,  53);
        private static readonly Color TextMuted  = Color.FromArgb(98, 112, 135);
        private static readonly Color BorderColor = Color.FromArgb(221, 227, 236);
        private static readonly Color Danger      = Color.FromArgb(232, 64,  64);

        // ── Public events ─────────────────────────────────────────────
        /// <summary>Raised when any nav menu item or sub-item is clicked.</summary>
        public event Action<string> MenuItemClicked;

        /// <summary>Raised when the Log Out button is clicked.</summary>
        public event EventHandler LogoutClicked;

        // ── Constructor ──────────────────────────────────────────────
        public AppShell()
        {
            Dock      = DockStyle.Top;
            Height    = TotalHeight;
            BackColor = Color.White;
            Padding   = new Padding(0);

            // ── TopNavBar ──────────────────────────────────────────
            _topNavBar = new TopNavBar();
            _topNavBar.MenuItemClicked += label => MenuItemClicked?.Invoke(label);

            // ── User Bar ───────────────────────────────────────────
            _pnlUserBar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = UserBarHeight,
                BackColor = Color.White
            };

            Panel border = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 1,
                BackColor = BorderColor
            };

            _lblBreadcrumb = new Label
            {
                Text      = "Dashboard",
                Font      = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = TextMain,
                AutoSize  = true
            };

            _lblUser = new UserInfoLabel
            {
                UserName   = "...",
                Department = ""
            };

            _btnLogout = new Button
            {
                Text         = "Log Out",
                Font         = new Font("Segoe UI", 12.8f),
                ForeColor    = Danger,
                BackColor    = Color.Transparent,
                FlatStyle    = FlatStyle.Flat,
                AutoSize     = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding      = new Padding(14, 4, 14, 4),
                Cursor       = Cursors.Hand
            };
            _btnLogout.FlatAppearance.BorderColor = Danger;
            _btnLogout.FlatAppearance.BorderSize  = 1;
            _btnLogout.Click += (s, e) => LogoutClicked?.Invoke(s, e);

            _pnlUserBar.Controls.Add(_lblBreadcrumb);
            _pnlUserBar.Controls.Add(_lblUser);
            _pnlUserBar.Controls.Add(_btnLogout);
            _pnlUserBar.Controls.Add(border);

            // UserBar layout: re-run on every resize
            _pnlUserBar.Resize += (s, e) => LayoutUserBar();

            // Controls added bottom-first so Dock.Top stacks correctly:
            // pnlUserBar docks Top first, then TopNavBar docks Top on top of it.
            Controls.Add(_pnlUserBar);
            Controls.Add(_topNavBar);

            HandleCreated += (s, e) => LayoutUserBar();
        }

        // ── Public API ────────────────────────────────────────────────

        /// <summary>Sets the user name and department shown in the User Bar.</summary>
        public void SetUser(string displayName, string department)
        {
            _lblUser.UserName   = displayName;
            _lblUser.Department = department;
            LayoutUserBar();
        }

        /// <summary>Restricts TopNavBar to the allowed menu labels.</summary>
        public void SetVisibleMenus(string[] allowedLabels)
            => _topNavBar.SetVisibleMenus(allowedLabels);

        /// <summary>Updates the breadcrumb text in the User Bar.</summary>
        public void SetBreadcrumb(string text)
            => _lblBreadcrumb.Text = text;

        /// <summary>Returns current breadcrumb text.</summary>
        public string Breadcrumb => _lblBreadcrumb.Text;

        /// <summary>
        /// Must be called once so the mega-menu popup can escape the AppShell clip region.
        /// Pass the form's root Panel (or the Form itself) as the container.
        /// </summary>
        public void SetPopupContainer(Control container)
            => _topNavBar.SetPopupContainer(container);

        // ── Layout ────────────────────────────────────────────────────
        private void LayoutUserBar()
        {
            const int RightPad = 16;
            const int ItemGap  = 12;
            int h  = _pnlUserBar.ClientSize.Height;
            int bw = _pnlUserBar.ClientSize.Width;

            if (bw == 0 || h == 0) return;

            int logoutX  = bw - RightPad - _btnLogout.Width;
            int userLblX = logoutX - ItemGap - _lblUser.Width;

            _btnLogout.Location     = new Point(logoutX,  (h - _btnLogout.Height)     / 2);
            _lblUser.Location       = new Point(userLblX, (h - _lblUser.Height)       / 2);
            _lblBreadcrumb.Location = new Point(22,       (h - _lblBreadcrumb.Height) / 2);
        }
    }
}
