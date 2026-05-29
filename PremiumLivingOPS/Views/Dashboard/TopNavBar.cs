using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.Dashboard
{
    /// <summary>
    /// Apple-style dark top navigation bar.
    ///
    /// Layer-overlap fix (2026-05-29)
    /// ─────────────────────────────
    /// Previously _megaPopup was added to the root Form's Controls collection
    /// and brought to front with BringToFront().  Because pnlUserBar is a
    /// sibling Dock=Top panel that was added AFTER pnlTopNav, Windows always
    /// painted it on top of the popup regardless of z-order calls.
    ///
    /// Fix: the popup is now injected into the same Panel that owns both
    /// pnlTopNav and pnlUserBar (i.e. pnlMain).  The caller must supply this
    /// container via SetPopupContainer() immediately after construction.
    /// The popup is then raised to index 0 of that container so it sits above
    /// every Dock=Top sibling including the UserBar.
    ///
    /// Other features preserved:
    ///  1. Nav items centred horizontally, generous spacing
    ///  2. Stable dropdown via 80 ms poll timer
    ///  3. Opaque mega-menu panel (no text bleed-through)
    ///  4. No category sub-headers inside the dropdown
    ///  5. Dropdown row height increased for better vertical spacing
    ///  6. Dropdown width = nav-item panel width (matches highlight footprint)
    /// </summary>
    public class TopNavBar : Panel
    {
        // ── Colours ───────────────────────────────────────────────────────────────────
        private static readonly Color NavBg    = Color.FromArgb(29,  29,  31);
        private static readonly Color NavText  = Color.FromArgb(245, 245, 247);
        private static readonly Color DropBg   = Color.FromArgb(38,  38,  40);
        private static readonly Color DropText = Color.FromArgb(210, 210, 215);

        // ── Fonts ───────────────────────────────────────────────────────────────────────
        private static readonly Font FontNav      = new Font("Segoe UI", 11f,   FontStyle.Regular);
        private static readonly Font FontDropItem = new Font("Segoe UI", 10.5f, FontStyle.Regular);

        private const int ItemPadH = 20;  // horizontal padding each side of a nav button
        private const int RowH     = 42;  // sub-item row height
        private const int PadV     = 10;  // top/bottom inset inside popup

        // ── Menu definition ─────────────────────────────────────────────────────────────
        private readonly (string Label, string[] Items)[] _menus =
        {
            ("Dashboard",                 new string[] { }),

            ("Order Processing",          new[]
            {
                "View & Search Order",
                "Quotation",
                "Create Order",
                "Modify Order"
            }),

            ("Production Processing",     new[]
            {
                "Search Raw Material Request",
                "Create Raw Material Request"
            }),

            ("Logistics Processing",      new[]
            {
                "View Shipment",
                "Handling Goods Received"
            }),

            ("Inventory Control",         new[]
            {
                "View Product / Raw Material"
            }),

            ("Raw Material",              new[]
            {
                "Create Procurement",
                "Search & List Procurement"
            }),

            ("After-Service",             new[]
            {
                "Create Invoice",
                "Complaint List",
                "Return Order List",
                "Account Receivable",
                "Account Payable"
            }),

            ("Master Data Maintenance",   new[]
            {
                "Supplier List",
                "Customer List"
            }),

            ("System Security & Control", new[]
            {
                "Staff List",
                "Log List"
            }),

            ("Statistical Reports",       new[]
            {
                "View Report"
            })
        };

        // ── State ───────────────────────────────────────────────────────────────────────
        private readonly List<Panel>                _navItems  = new List<Panel>();
        private readonly List<int>                  _navWidths = new List<int>();
        private          Panel                      _megaPopup;
        private          Control                    _popupContainer;   // set by caller
        private          int                        _activeIdx = -1;
        private          System.Windows.Forms.Timer _pollTimer;

        // ── Public Events ─────────────────────────────────────────────────────────────
        public event Action<string> MenuItemClicked;

        // ── Constructor ─────────────────────────────────────────────────────────────────
        public TopNavBar()
        {
            Height    = 44;
            Dock      = DockStyle.Top;
            BackColor = NavBg;
            Padding   = new Padding(0);

            _pollTimer = new System.Windows.Forms.Timer { Interval = 80 };
            _pollTimer.Tick += PollTimer_Tick;

            HandleCreated += (s, e) => { BuildMegaPopup(); BuildNavItems(); };
        }

        // ── Public: caller supplies the shared container for the popup ──────────────
        /// <summary>
        /// Must be called once by the host form/panel BEFORE the nav bar
        /// is shown.  Pass the Panel that is the common parent of both the
        /// TopNavBar and the UserBar (e.g. pnlMain).
        /// The popup will be added to this container at z-index 0 so it
        /// always renders above every Dock sibling.
        /// </summary>
        public void SetPopupContainer(Control container)
        {
            _popupContainer = container;
        }

        // ── Opaque popup panel ───────────────────────────────────────────────────
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

        // ── Build nav items ───────────────────────────────────────────────────────────────
        private void BuildNavItems()
        {
            Controls.Clear();
            _navItems.Clear();
            _navWidths.Clear();

            Label logo = new Label
            {
                Text      = "\uD83E\uDE91 PLF",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize  = false, Width = 80, Height = 44,
                TextAlign = ContentAlignment.MiddleCenter,
                Location  = new Point(12, 0)
            };
            Controls.Add(logo);

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

                Panel item = new Panel
                {
                    Location  = new Point(x, 0),
                    Size      = new Size(_navWidths[i], 44),
                    BackColor = Color.Transparent,
                    Cursor    = Cursors.Hand
                };
                Label lbl = new Label
                {
                    Text      = _menus[i].Label,
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
                    EventHandler goHome = (s, e) => { HideMegaMenu(); MenuItemClicked?.Invoke("Dashboard"); };
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

        // ── Poll timer ────────────────────────────────────────────────────────────────────
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

        // ── Recentre ──────────────────────────────────────────────────────────────────────
        private void RecentreItems()
        {
            if (_navItems.Count == 0) return;
            int totalW = 0;
            foreach (Panel p in _navItems) totalW += p.Width;
            int startX = Math.Max(100, (Width - totalW) / 2);
            int x = startX;
            foreach (Panel p in _navItems) { p.Location = new Point(x, 0); x += p.Width; }
        }

        // ── Highlight ───────────────────────────────────────────────────────────────────────
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

        // ── Mega Menu ──────────────────────────────────────────────────────────────────────
        private void ShowMegaMenu(int idx, Panel navItem)
        {
            string[] items = _menus[idx].Items;
            if (items.Length == 0) return;

            // Popup width = nav-item width, but at least wide enough for longest sub-item
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
                string captured = sub;

                Panel row = new Panel
                {
                    Size      = new Size(popupW, RowH),
                    Location  = new Point(0, iy),
                    BackColor = DropBg,
                    Cursor    = Cursors.Hand
                };
                Label rowLbl = new Label
                {
                    Text      = captured,
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

                EventHandler onClick = (s, e) =>
                {
                    HideMegaMenu();
                    MenuItemClicked?.Invoke(captured);
                    MessageBox.Show(
                        $"\u231B  {captured}\n\nThis feature is currently under development.\nPlease check back in a later version.",
                        "Coming Soon",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                };
                row.Click    += onClick;
                rowLbl.Click += onClick;

                _megaPopup.Controls.Add(row);
                iy += RowH;
            }

            // ── Position the popup ──────────────────────────────────────────────────────
            //
            // The popup MUST be a child of _popupContainer (pnlMain), NOT the
            // root Form.  This guarantees the popup is painted in the same
            // layer as pnlTopNav and pnlUserBar, so BringToFront() correctly
            // lifts it above both Dock=Top siblings.
            //
            // Coordinate mapping:
            //   navItem lives inside TopNavBar which is inside _popupContainer.
            //   PointToScreen gives the screen coordinate of the navItem's
            //   bottom-left corner; _popupContainer.PointToClient converts it
            //   back to _popupContainer-relative coords.
            // ─────────────────────────────────────────────────────────────────────────────
            Control container = _popupContainer ?? FindForm();
            if (container == null) return;

            Point screenBottomLeft = navItem.PointToScreen(new Point(0, Height));
            Point localPt          = container.PointToClient(screenBottomLeft);

            int left = localPt.X;
            if (left + popupW > container.ClientSize.Width - 4)
                left = container.ClientSize.Width - popupW - 4;
            if (left < 0) left = 0;

            _megaPopup.Location = new Point(left, localPt.Y);

            if (!container.Controls.Contains(_megaPopup))
                container.Controls.Add(_megaPopup);

            // z-index 0 = topmost child in the container
            container.Controls.SetChildIndex(_megaPopup, 0);
            _megaPopup.BringToFront();
            _megaPopup.Visible = true;
        }

        private void HideMegaMenu()
        {
            if (_megaPopup != null) _megaPopup.Visible = false;
            ClearHighlight();
        }

        // ── Paint ─────────────────────────────────────────────────────────────────────────────
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
