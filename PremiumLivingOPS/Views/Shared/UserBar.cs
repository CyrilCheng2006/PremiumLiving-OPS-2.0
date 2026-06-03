using System;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.Shared
{
    /// <summary>
    /// UserBar — the 72 px chrome strip that sits directly below TopNavBar.
    ///
    /// Responsibilities
    /// ────────────────
    /// • Displays the current breadcrumb (page title) on the left.
    /// • Displays the logged-in user’s name + department on the right
    ///   via <see cref="UserInfoLabel"/>.
    /// • Hosts the Log Out button and surfaces a <see cref="LogoutClicked"/> event.
    /// • Enforces its own fixed height (72 px) through OnLayout and ScaleControl,
    ///   so AutoScaleMode = Font and DPI scaling can never collapse it.
    ///
    /// Layout
    /// ──────
    ///   ┌────────────────────────────────────────────────────────────────────────┐
    ///   │  Breadcrumb (AutoSize)    [stretch]    UserInfoLabel │ Log Out │
    ///   └────────────────────────────────────────────────────────────────────────┘
    ///   A 1 px bottom border separates this bar from the page content.
    ///
    /// Vertical centring
    /// ─────────────────
    ///   The single TableLayoutPanel row uses SizeType.Percent 100 %, so its
    ///   height equals UserBarHeight.  Every cell child uses Anchor = None,
    ///   which causes the TLP to centre each child both horizontally and
    ///   vertically inside its cell.
    ///
    /// Height contract
    /// ───────────────
    ///   <see cref="FixedHeight"/> = 72 px, enforced by:
    ///     1. <see cref="OnLayout"/>     — re-locks after every layout pass.
    ///     2. <see cref="ScaleControl"/> — vetoes AutoScaleMode = Font height scaling.
    ///
    /// Usage (inside AppShell)
    /// ───────────────────────
    ///   var _userBar = new UserBar();
    ///   Controls.Add(_userBar);   // DockStyle.Top, added after TopNavBar
    ///
    ///   _userBar.SetUser(displayName, department);
    ///   _userBar.SetBreadcrumb("Module  ›  Page");
    ///   _userBar.LogoutClicked += handler;
    /// </summary>
    public sealed class UserBar : Panel
    {
        // ──────────────────────────────────────────────────────────────
        /// <summary>Pixel height of the UserBar.  Must equal AppShell.UserBarHeight.</summary>
        public const int FixedHeight = 72;

        // ──────────────────────────────────────────────────────────────
        // Colours  (match AppShell palette exactly)
        // ──────────────────────────────────────────────────────────────
        private static readonly Color TextMain    = Color.FromArgb( 15,  31,  53);
        private static readonly Color BorderColor = Color.FromArgb(221, 227, 236);
        private static readonly Color Danger      = Color.FromArgb(232,  64,  64);

        // ──────────────────────────────────────────────────────────────
        // Child controls
        // ──────────────────────────────────────────────────────────────
        private readonly Label         _lblBreadcrumb;
        private readonly UserInfoLabel _lblUser;
        private readonly Button        _btnLogout;

        // ──────────────────────────────────────────────────────────────
        // Public event
        // ──────────────────────────────────────────────────────────────
        /// <summary>Raised when the user clicks “Log Out”.</summary>
        public event EventHandler LogoutClicked;

        // ──────────────────────────────────────────────────────────────
        // Constructor
        // ──────────────────────────────────────────────────────────────
        public UserBar()
        {
            // ── Panel self properties ───────────────────────────────────────
            Dock        = DockStyle.Top;
            Height      = FixedHeight;
            MinimumSize = new Size(0, FixedHeight);
            BackColor   = Color.White;
            Padding     = new Padding(0);

            // ── Breadcrumb label ────────────────────────────────────────
            _lblBreadcrumb = new Label
            {
                Text      = "Dashboard",
                Font      = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = TextMain,
                AutoSize  = true,
                Anchor    = AnchorStyles.None,
                Margin    = new Padding(22, 0, 0, 0)
            };

            // ── UserInfoLabel ────────────────────────────────────────────
            _lblUser = new UserInfoLabel { UserName = "...", Department = "" };

            // ── Log Out button ──────────────────────────────────────────
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

            // ── Right sub-panel  (UserInfoLabel + Log Out side by side) ────
            var pnlRight = new Panel
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
                int h         = pnlRight.Height;
                _lblUser.Left   = 0;
                _lblUser.Top    = (h - _lblUser.Height)   / 2;
                _btnLogout.Left = _lblUser.Right + 8;
                _btnLogout.Top  = (h - _btnLogout.Height) / 2;
                pnlRight.Width  = _btnLogout.Right + 16;
            };

            // ── 3-column TableLayoutPanel: breadcrumb | stretch | right ────
            var tlp = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 3,
                RowCount    = 1,
                BackColor   = Color.Transparent,
                Padding     = new Padding(0),
                Margin      = new Padding(0)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));       // breadcrumb
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));  // stretch
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));       // right panel
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tlp.Controls.Add(_lblBreadcrumb, 0, 0);
            tlp.Controls.Add(new Panel { BackColor = Color.Transparent }, 1, 0);
            tlp.Controls.Add(pnlRight, 2, 0);

            // ── 1 px bottom border ───────────────────────────────────────
            var border = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 1,
                BackColor = BorderColor
            };

            // Add in reverse Dock order: Fill first, Bottom (border) second
            Controls.Add(tlp);    // DockStyle.Fill  — content
            Controls.Add(border); // DockStyle.Bottom — border
        }

        // ──────────────────────────────────────────────────────────────
        // Height-lock overrides  (mirrors TopNavBar pattern exactly)
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Re-locks UserBar height to <see cref="FixedHeight"/> after every
        /// layout pass, preventing AutoScaleMode = Font from collapsing the bar.
        /// </summary>
        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (Height != FixedHeight)
            {
                Height      = FixedHeight;
                MinimumSize = new Size(0, FixedHeight);
            }
        }

        /// <summary>
        /// Vetoes height scaling from AutoScaleMode = Font / DPI while still
        /// allowing width to scale normally.
        /// </summary>
        protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
        {
            base.ScaleControl(
                new SizeF(factor.Width, 1.0f),
                specified & ~BoundsSpecified.Height);

            MinimumSize = new Size(0, FixedHeight);
            if (Height != FixedHeight) Height = FixedHeight;
        }

        // ──────────────────────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────────────────────

        /// <summary>Update the displayed user name and department.</summary>
        public void SetUser(string displayName, string department)
        {
            _lblUser.UserName   = displayName;
            _lblUser.Department = department;
        }

        /// <summary>Set the breadcrumb text (e.g. "Order Processing  ›  View Order").</summary>
        public void SetBreadcrumb(string text) => _lblBreadcrumb.Text = text;

        /// <summary>Gets the current breadcrumb text.</summary>
        public string Breadcrumb => _lblBreadcrumb.Text;

        /// <summary>Update breadcrumb from a menu/sub-item pair.</summary>
        internal void UpdateBreadcrumb(string menu, string sub)
            => _lblBreadcrumb.Text = string.IsNullOrEmpty(sub) ? menu : $"{menu}  ›  {sub}";
    }
}
