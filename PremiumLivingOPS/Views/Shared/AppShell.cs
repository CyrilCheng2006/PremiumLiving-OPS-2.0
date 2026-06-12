using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.Shared
{
    /// <summary>
    /// Main application shell.  Contains the Tab Navigation Bar (left) and the User Bar (top-right).
    /// Host forms call <see cref="ApplyViewModel"/> once to wire up session data.
    /// </summary>
    public partial class AppShell : UserControl
    {
        // ── Public events ──────────────────────────────────────────────────────
        public event EventHandler<string> MenuItemClicked;

        // ── Internal state ─────────────────────────────────────────────────────
        private string[] _allowedMenus = Array.Empty<string>();
        private string   _staffName    = string.Empty;
        private string   _staffRole    = string.Empty;

        public AppShell()
        {
            InitializeComponent();
        }

        // ── ApplyViewModel ─────────────────────────────────────────────────────

        /// <summary>
        /// Wires session data into the shell's User Bar and filters the Tab Nav Bar
        /// so only menus in <paramref name="vm"/>.AllowedMenus are visible.
        /// Call once in the host Form's Load handler after InitializeComponent().
        /// </summary>
        public void ApplyViewModel(UserBarViewModel vm)
        {
            if (vm == null) throw new ArgumentNullException("vm");

            _staffName    = vm.StaffName  ?? string.Empty;
            _staffRole    = vm.StaffRole  ?? string.Empty;
            _allowedMenus = vm.AllowedMenus ?? Array.Empty<string>();

            // Update User Bar labels
            lblStaffName?.Let(l => l.Text = _staffName);
            lblStaffRole?.Let(l => l.Text = _staffRole);

            // Filter Tab Nav Bar items
            ApplyMenuFilter(_allowedMenus);
        }

        // ── Menu filter ────────────────────────────────────────────────────────

        private void ApplyMenuFilter(string[] allowed)
        {
            if (pnlNavBar == null) return;
            var set = new HashSet<string>(allowed, StringComparer.OrdinalIgnoreCase);
            foreach (Control c in pnlNavBar.Controls)
            {
                if (c is Button btn)
                    btn.Visible = set.Count == 0 || set.Contains(btn.Tag?.ToString() ?? string.Empty);
            }
        }

        // ── Nav button click ───────────────────────────────────────────────────

        private void NavButton_Click(object sender, EventArgs e)
        {
            if (sender is Button btn)
                MenuItemClicked?.Invoke(this, btn.Tag?.ToString() ?? string.Empty);
        }
    }

    // ── Internal extension helper (avoids null-ref on designer-generated controls) ──
    internal static class ControlExt
    {
        internal static void Let<T>(this T obj, Action<T> action) where T : class
        {
            if (obj != null) action(obj);
        }
    }
}
