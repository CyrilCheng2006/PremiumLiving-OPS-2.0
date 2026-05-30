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
    ///        _shell.MenuItemClicked += OnMenuItemClicked;
    ///        _shell.LogoutClicked   += OnLogoutClicked;
    ///
    /// AppShell height = TopNavBar (44 px) + UserBar (72 px) = 116 px.
    ///
    /// Layout strategy
    /// ───────────────
    /// The UserBar uses a 3-column TableLayoutPanel instead of manual
    /// coordinate arithmetic.  This prevents overlap regardless of font
    /// scaling, DPI, or the order in which controls are measured:
    ///
    ///   ┌────────────────┬──────────────────────┬──────────────────────┐
    ///   │  Breadcrumb    │      (stretch)        │  UserInfo | Logout   │
    ///   └────────────────┴──────────────────────┴──────────────────────┘
    ///   AutoSize           100% stretch                AutoSize
    /// </summary>
    public class AppShell : Panel
    {
        // ── Heights ──────────────────────────────────────────────────
        public const int NavBarHeight  = 44;
        public const int UserBarHeight = 72;
        public const int TotalHeight   = NavBarHeight + UserBarHeight;

        // ── Child controls ───────────────────────────────────────────
        private readonly TopNavBar     _topNavBar;
        private readonly UserInfoLabel _lblUser;
        private readonly Label         _lblBreadcrumb;
        private readonly Button        _btnLogout;

        // ── Colours (mirrors DashboardForm.Palette) ──────────────────
        private static readonly Color TextMain    = Color.FromArgb(15,  31,  53);
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

            // ── Breadcrumb label ───────────────────────────────────
            _lblBreadcrumb = new Label
            {
                Text      = "Dashboard",
                Font      = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = TextMain,
                AutoSize  = true,
                Anchor    = AnchorStyles.None,
                Margin    = new Padding(22, 0, 0, 0)
            };

            // ── UserInfoLabel ──────────────────────────────────────
            _lblUser = new UserInfoLabel
            {
                UserName   = "...",
                Department = "",
                Margin     = new Padding(0, 0, 8, 0)
            };

            // ── Logout button ──────────────────────────────────────
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
                Cursor       = Cursors.Hand,
                Margin       = new Padding(0, 0, 16, 0)
            };
            _btnLogout.FlatAppearance.BorderColor = Danger;
            _btnLogout.FlatAppearance.BorderSize  = 1;
            _btnLogout.Click += (s, e) => LogoutClicked?.Invoke(s, e);

            // ── Right sub-panel: UserInfo + Logout side by side ────
            // FlowLayoutPanel keeps the two controls glued together and
            // naturally sizes to their combined width — no manual Width reads.
            FlowLayoutPanel pnlRight = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize      = true,
                AutoSizeMode  = AutoSizeMode.GrowAndShrink,
                BackColor     = Color.Transparent,
                Anchor        = AnchorStyles.None,
                WrapContents  = false
            };
            pnlRight.Controls.Add(_lblUser);
            pnlRight.Controls.Add(_btnLogout);

            // ── Bottom border ──────────────────────────────────────
            Panel border = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 1,
                BackColor = BorderColor
            };

            // ── UserBar: 3-column TableLayoutPanel ─────────────────
            // Col 0: Breadcrumb  (AutoSize)
            // Col 1: Stretch filler (100%)
            // Col 2: pnlRight    (AutoSize)
            TableLayoutPanel tlpBar = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 3,
                RowCount    = 1,
                BackColor   = Color.Transparent,
                Padding     = new Padding(0),
                Margin      = new Padding(0)
            };
            tlpBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));       // col 0
            tlpBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f)); // col 1
            tlpBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));       // col 2
            tlpBar.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            tlpBar.Controls.Add(_lblBreadcrumb, 0, 0);
            tlpBar.Controls.Add(new Panel { BackColor = Color.Transparent }, 1, 0); // spacer
            tlpBar.Controls.Add(pnlRight, 2, 0);

            Panel pnlUserBar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = UserBarHeight,
                BackColor = Color.White
            };
            pnlUserBar.Controls.Add(tlpBar);
            pnlUserBar.Controls.Add(border);

            // ── Stack: TopNavBar on top, UserBar below ─────────────
            // Controls added last-in = docked to top first.
            Controls.Add(pnlUserBar);
            Controls.Add(_topNavBar);
        }

        // ── Public API ────────────────────────────────────────────────

        /// <summary>Sets the user name and department shown in the User Bar.</summary>
        public void SetUser(string displayName, string department)
        {
            _lblUser.UserName   = displayName;
            _lblUser.Department = department;
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
        /// Pass the form's root Panel as the container.
        /// </summary>
        public void SetPopupContainer(Control container)
            => _topNavBar.SetPopupContainer(container);
    }
}
