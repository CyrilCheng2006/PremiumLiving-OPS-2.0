using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.Shared
{
    /// <summary>
    /// Apple-style dark top navigation bar.
    ///
    /// Reuse contract
    /// ──────────────
    /// • Place this control (via AppShell) at the top of ANY form.
    /// • Call SetVisibleMenus(string[]) after the ViewModel is loaded
    ///   to restrict visible items to those permitted for the current user.
    /// • Subscribe to MenuItemClicked for navigation callbacks.
    ///   The event now carries TWO strings: (menuLabel, subItemLabel).
    ///   - Top-level click (Dashboard):   menuLabel="Dashboard", subItemLabel=""
    ///   - Sub-item click:                menuLabel="Order Processing", subItemLabel="View Order"
    /// • TopNavBar is a pure View: it never reads SessionManager,
    ///   NavAccessPolicy, or any department/role data directly.
    /// </summary>
    public class TopNavBar : Panel
    {
        // ── Colours ─────────────────────────────────────────────────
        private static readonly Color NavBg    = Color.FromArgb(29,  29,  31);
        private static readonly Color NavText  = Color.FromArgb(245, 245, 247);
        private static readonly Color DropBg   = Color.FromArgb(38,  38,  40);
        private static readonly Color DropText = Color.FromArgb(210, 210, 215);

        // ── Fonts ───────────────────────────────────────────────────
        private static readonly Font FontNav      = new Font("Segoe UI", 11f,   FontStyle.Regular);
        private static readonly Font FontDropItem = new Font("Segoe UI", 10.5f, FontStyle.Regular);

        private const int ItemPadH = 20;
        private const int RowH     = 42;
        private const int PadV     = 10;

        // ── Full menu catalogue (canonical order) ────────────────────
        private static readonly (string Label, string[] Items)[] AllMenus =
        {
            ("Dashboard",                 new string[] { }),
            ("Order Processing",          new[] { "View Order", "Quotation", "Create Order", "Modify Order" }),
            ("Production Processing",     new[] { "Search Raw Material Request", "Create Raw Material Request" }),
            ("Logistics Processing",      new[] { "View Shipment", "Handling Goods Received" }),
            ("Inventory Control",         new[] { "View Product / Raw Material" }),
            ("Raw Material",              new[] { "Create Procurement", "Search Procurement" }),
            ("After-Service",             new[] { "Create Invoice", "Complaint List", "Return Order List", "Account Receivable", "Account Payable" }),
            ("Master Data Maintenance",   new[] { "Supplier List", "Customer List" }),
            ("System Control",            new[] { "Staff List", "Log List" }),
            ("Statistical Reports",       new[] { "View Report" })
        };

        // ── Active (filtered) menu ──────────────────────────────────
        private (string Label, string[] Items)[] _menus;

        // ── State ───────────────────────────────────────────────────
        private readonly List<Panel>                _navItems  = new List<Panel>();
        private readonly List<int>                  _navWidths = new List<int>();
        private          Panel                      _megaPopup;
        private          int                        _activeIdx = -1;
        private          System.Windows.Forms.Timer _pollTimer;

        // ── Public Events ────────────────────────────────────────────
        /// <summary>
        /// Fired whenever a navigation item is clicked.
        /// arg1 = parent menu label (e.g. "Order Processing")
        /// arg2 = sub-item label   (e.g. "View Order"; empty string for top-level items)
        /// </summary>
        public event Action<string, string> MenuItemClicked;

        // ── Constructor ──────────────────────────────────────────────
        public TopNavBar()
        {
            Height    = 44;
            Dock      = DockStyle.Top;
            BackColor = NavBg;
            Padding   = new Padding(0);

            _menus = AllMenus;

            _pollTimer = new System.Windows.Forms.Timer { Interval = 80 };
            _pollTimer.Tick += PollTimer_Tick;

            HandleCreated += (s, e) => { BuildMegaPopup(); BuildNavItems(); };
        }

        // ── Public API ───────────────────────────────────────────────

        /// <summary>
        /// Restricts the visible menus to those permitted for the current user.
        /// Pass null or empty to show all menus (useful for IT / super-user).
        /// </summary>
        public void SetVisibleMenus(string[] allowedLabels)
        {
            if (allowedLabels == null || allowedLabels.Length == 0)
            {
                _menus = AllMenus;
            }
            else
            {
                var allowed = new HashSet<string>(allowedLabels, StringComparer.OrdinalIgnoreCase);
                _menus = AllMenus.Where(m => allowed.Contains(m.Label)).ToArray();
            }

            if (IsHandleCreated)
            {
                HideMegaMenu();
                BuildNavItems();
            }
        }

        /// <summary>No-op kept for source compatibility.</summary>
        public void SetPopupContainer(Control container) { /* intentional no-op */ }

        // ── Popup panel ──────────────────────────────────────────────
        private void BuildMegaPopup()
        {
            _megaPopup = new OpaquePanel
            {
                Visible   = false,
                BackColor = DropBg,
                AutoSize  = false
            };
            _megaPopup.Paint += MegaPopup_Paint;
        }

        private class OpaquePanel : Panel
        {
            protected override CreateParams CreateParams
            {
                get { var cp = base.CreateParams; cp.ExStyle &= ~0x20; return cp; }
            }
            protected override void OnPaintBackground(PaintEventArgs e)
            {
                e.Graphics.Clear(BackColor);
            }
        }

        // ── Build nav items ──────────────────────────────────────────
        private void BuildNavItems()
        {
            Controls.Clear();
            _navItems.Clear();
            _navWidths.Clear();

            int totalW = 0;
            for (int i = 0; i < _menus.Length; i++)
            {
                int w = TextRenderer.MeasureText(_menus[i].Label, FontNav).Width + ItemPadH * 2;
                _navWidths.Add(w);
                totalW += w;
            }

            int startX = Math.Max(100, (Width - totalW) / 2);
            int x      = startX;

            for (int i = 0; i < _menus.Length; i++)
            {
                int  idx     = i;
                bool hasDrop = _menus[i].Items.Length > 0;
                string menuLabel = _menus[i].Label;

                Panel item = new Panel
                {
                    Location  = new Point(x, 0),
                    Size      = new Size(_navWidths[i], 44),
                    BackColor = Color.Transparent,
                    Cursor    = Cursors.Hand
                };
                Label lbl = new Label
                {
                    Text      = menuLabel,
                    Font      = FontNav,
                    ForeColor = NavText,
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                item.Controls.Add(lbl);

                item.MouseEnter += (s, e) => OnNavItemHover(idx, item, hasDrop);
                lbl.MouseEnter  += (s, e) => OnNavItemHover(idx, item, hasDrop);

                if (!hasDrop)
                {
                    // Top-level item with no sub-menu (e.g. Dashboard)
                    // Fire with empty sub-item string.
                    EventHandler goHome = (s, e) =>
                    {
                        HideMegaMenu();
                        MenuItemClicked?.Invoke(menuLabel, string.Empty);
                    };
                    item.Click += goHome;
                    lbl.Click  += goHome;
                }

                Controls.Add(item);
                _navItems.Add(item);
                x += _navWidths[i];
            }

            Resize += (s, e) => RecentreItems();
        }

        private void OnNavItemHover(int idx, Panel navItem, bool hasDrop)
        {
            HighlightItem(idx);
            if (hasDrop) ShowMegaMenu(idx, navItem);
            else         HideMegaMenu();
            _pollTimer.Start();
        }

        // ── Poll timer ───────────────────────────────────────────────
        private void PollTimer_Tick(object sender, EventArgs e)
        {
            if (_megaPopup == null || !_megaPopup.Visible)
            {
                if (!IsOverNavBar()) { ClearHighlight(); _pollTimer.Stop(); }
                return;
            }
            bool overPopup = _megaPopup.ClientRectangle.Contains(
                                 _megaPopup.PointToClient(Cursor.Position));
            if (!overPopup && !IsOverNavBar())
            {
                HideMegaMenu();
                _pollTimer.Stop();
            }
        }

        private bool IsOverNavBar()
        {
            if (!IsHandleCreated) return false;
            return ClientRectangle.Contains(PointToClient(Cursor.Position));
        }

        // ── Recentre ─────────────────────────────────────────────────
        private void RecentreItems()
        {
            if (_navItems.Count == 0) return;
            int totalW = 0;
            foreach (Panel p in _navItems) totalW += p.Width;
            int startX = Math.Max(100, (Width - totalW) / 2);
            int x = startX;
            foreach (Panel p in _navItems) { p.Location = new Point(x, 0); x += p.Width; }
        }

        // ── Highlight ────────────────────────────────────────────────
        private void HighlightItem(int idx)
        {
            for (int i = 0; i < _navItems.Count; i++)
            {
                bool on = i == idx;
                _navItems[i].BackColor = on ? Color.FromArgb(60, 255, 255, 255) : Color.Transparent;
                foreach (Control c in _navItems[i].Controls)
                    if (c is Label l) l.ForeColor = on ? Color.White : NavText;
            }
            _activeIdx = idx;
        }

        private void ClearHighlight()
        {
            foreach (Panel p in _navItems)
            {
                p.BackColor = Color.Transparent;
                foreach (Control c in p.Controls)
                    if (c is Label l) l.ForeColor = NavText;
            }
            _activeIdx = -1;
        }

        // ── Mega Menu ────────────────────────────────────────────────
        private void ShowMegaMenu(int idx, Panel navItem)
        {
            string[] items    = _menus[idx].Items;
            string   menuLabel = _menus[idx].Label;
            if (items.Length == 0) return;

            int navW     = _navWidths[idx];
            int minTextW = 0;
            foreach (string s in items)
            {
                int tw = TextRenderer.MeasureText(s, FontDropItem).Width + 24;
                if (tw > minTextW) minTextW = tw;
            }
            int popupW = Math.Max(navW, minTextW);
            int popupH = PadV * 2 + items.Length * RowH;

            _megaPopup.Controls.Clear();
            _megaPopup.Size = new Size(popupW, popupH);

            int iy = PadV;
            foreach (string sub in items)
            {
                string capturedSub  = sub;
                string capturedMenu = menuLabel;

                Panel row = new Panel
                {
                    Size      = new Size(popupW, RowH),
                    Location  = new Point(0, iy),
                    BackColor = DropBg,
                    Cursor    = Cursors.Hand
                };
                Label rowLbl = new Label
                {
                    Text      = capturedSub,
                    Font      = FontDropItem,
                    ForeColor = DropText,
                    BackColor = Color.Transparent,
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding   = new Padding(12, 0, 0, 0)
                };
                row.Controls.Add(rowLbl);

                Color hoverBg = Color.FromArgb(60, 255, 255, 255);
                row.MouseEnter    += (s, e) => { row.BackColor = hoverBg; rowLbl.ForeColor = Color.White; };
                row.MouseLeave    += (s, e) => { row.BackColor = DropBg;  rowLbl.ForeColor = DropText;   };
                rowLbl.MouseEnter += (s, e) => { row.BackColor = hoverBg; rowLbl.ForeColor = Color.White; };
                rowLbl.MouseLeave += (s, e) => { row.BackColor = DropBg;  rowLbl.ForeColor = DropText;   };

                // Fire MenuItemClicked with both parent-menu and sub-item labels.
                EventHandler onClick = (s, e) =>
                {
                    HideMegaMenu();
                    MenuItemClicked?.Invoke(capturedMenu, capturedSub);
                };
                row.Click    += onClick;
                rowLbl.Click += onClick;

                _megaPopup.Controls.Add(row);
                iy += RowH;
            }

            Form form = FindForm();
            if (form == null) return;

            Point screenPt = navItem.PointToScreen(new Point(0, navItem.Height));
            Point formPt   = form.PointToClient(screenPt);

            int left = formPt.X;
            if (left + popupW > form.ClientSize.Width - 4)
                left = form.ClientSize.Width - popupW - 4;
            if (left < 0) left = 0;

            _megaPopup.Location = new Point(left, formPt.Y);

            if (!form.Controls.Contains(_megaPopup))
                form.Controls.Add(_megaPopup);

            _megaPopup.BringToFront();
            _megaPopup.Visible = true;
        }

        private void HideMegaMenu()
        {
            if (_megaPopup != null) _megaPopup.Visible = false;
            ClearHighlight();
        }

        // ── Paint ────────────────────────────────────────────────────
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen p = new Pen(Color.FromArgb(60, 255, 255, 255), 1))
                e.Graphics.DrawLine(p, 0, Height - 1, Width, Height - 1);
        }

        private void MegaPopup_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, _megaPopup.Width - 1, _megaPopup.Height - 1);
            using (GraphicsPath path = RoundedRect(rect, 10))
            using (Pen border = new Pen(Color.FromArgb(80, 255, 255, 255), 1))
                e.Graphics.DrawPath(border, path);
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            var p = new GraphicsPath();
            p.AddArc(r.X,         r.Y,          d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y,          d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d,   0, 90);
            p.AddArc(r.X,         r.Bottom - d, d, d,  90, 90);
            p.CloseFigure();
            return p;
        }
    }
}
