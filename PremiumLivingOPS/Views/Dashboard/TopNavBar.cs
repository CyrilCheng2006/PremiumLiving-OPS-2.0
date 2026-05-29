using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.Dashboard
{
    /// <summary>
    /// Apple-style dark top navigation bar.
    /// Fixes applied:
    ///   1. Wider padding on nav items (horizontal spacing increased)
    ///   2. Stable dropdown: uses a single polling Timer instead of MouseLeave
    ///      so moving the cursor through the gap between nav bar and popup
    ///      no longer closes the menu.
    ///   3. Mega-menu popup uses a real opaque Panel drawn via OnPaintBackground
    ///      so child label text never bleeds through the background.
    /// </summary>
    public class TopNavBar : Panel
    {
        // ── Colours ───────────────────────────────────────────────────────────
        private static readonly Color NavBg        = Color.FromArgb(29,  29,  31);
        private static readonly Color NavText      = Color.FromArgb(245, 245, 247);
        private static readonly Color DropBg       = Color.FromArgb(38,  38,  40);   // slightly lighter for contrast
        private static readonly Color DropText     = Color.FromArgb(210, 210, 215);
        private static readonly Color DropTextBold = Color.FromArgb(245, 245, 247);

        // ── Fonts ─────────────────────────────────────────────────────────────
        private static readonly Font FontNav      = new Font("Segoe UI", 11f,   FontStyle.Regular);
        private static readonly Font FontDropHead = new Font("Segoe UI", 10f,   FontStyle.Bold);
        private static readonly Font FontDropItem = new Font("Segoe UI", 10.5f, FontStyle.Regular);

        // FIX 1: wider horizontal padding per nav item
        private const int ItemPadH = 20;   // padding each side inside a nav button

        // ── Menu definition ───────────────────────────────────────────────────
        private readonly (string Label, (string Category, string[] Items)[] Groups)[] _menus =
        {
            ("Dashboard", new (string, string[])[] { }),

            ("Order Processing", new (string, string[])[]
            {
                ("ORDER PROCESSING", new[]
                {
                    "View & Search Order",
                    "Quotation",
                    "Create Order",
                    "Modify Order"
                })
            }),

            ("Production Processing", new (string, string[])[]
            {
                ("PRODUCTION PROCESSING", new[]
                {
                    "Search Raw Material Request",
                    "Create Raw Material Request"
                })
            }),

            ("Logistics Processing", new (string, string[])[]
            {
                ("LOGISTICS PROCESSING", new[]
                {
                    "View Shipment",
                    "Handling Goods Received"
                })
            }),

            ("Inventory Control", new (string, string[])[]
            {
                ("INVENTORY CONTROL", new[]
                {
                    "View Product / Raw Material"
                })
            }),

            ("Raw Material", new (string, string[])[]
            {
                ("RAW MATERIAL", new[]
                {
                    "Create Procurement",
                    "Search & List Procurement"
                })
            }),

            ("After-Service", new (string, string[])[]
            {
                ("AFTER-SERVICE", new[]
                {
                    "Create Invoice",
                    "Complaint List",
                    "Return Order List",
                    "Account Receivable",
                    "Account Payable"
                })
            }),

            ("Master Data Maintenance", new (string, string[])[]
            {
                ("MASTER DATA", new[]
                {
                    "Supplier List",
                    "Customer List"
                })
            }),

            ("System Security & Control", new (string, string[])[]
            {
                ("SECURITY & CONTROL", new[]
                {
                    "Staff List",
                    "Log List"
                })
            }),

            ("Statistical Reports", new (string, string[])[]
            {
                ("REPORTS", new[]
                {
                    "View Report"
                })
            })
        };

        // ── State ─────────────────────────────────────────────────────────────
        private readonly List<Panel> _navItems  = new List<Panel>();
        private          Panel       _megaPopup;
        private          int         _activeIdx = -1;

        // FIX 2: single polling timer replaces unreliable MouseLeave
        private System.Windows.Forms.Timer _pollTimer;

        // ── Public Events ─────────────────────────────────────────────────────
        public event Action<string> MenuItemClicked;

        // ── Constructor ───────────────────────────────────────────────────────
        public TopNavBar()
        {
            Height    = 44;
            Dock      = DockStyle.Top;
            BackColor = NavBg;
            Padding   = new Padding(0);

            // FIX 2: poll every 80 ms whether the cursor is still over the
            // nav bar OR the popup; if not, close the popup.
            _pollTimer = new System.Windows.Forms.Timer { Interval = 80 };
            _pollTimer.Tick += PollTimer_Tick;

            HandleCreated += (s, e) => BuildMegaPopup();
            HandleCreated += (s, e) => BuildNavItems();
        }

        // ── Build the shared mega-popup panel ─────────────────────────────────
        // FIX 3: use an opaque OpaquePanel subclass so GDI+ does NOT punch
        // transparent holes behind child Label controls.
        private void BuildMegaPopup()
        {
            _megaPopup = new OpaquePanel
            {
                Visible   = false,
                BackColor = DropBg,
                AutoSize  = false,
                Padding   = new Padding(20, 14, 20, 18)
            };
            _megaPopup.Paint += MegaPopup_Paint;
        }

        // ── OpaquePanel: overrides CreateParams to prevent WS_EX_TRANSPARENT ──
        private class OpaquePanel : Panel
        {
            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams cp = base.CreateParams;
                    // Remove WS_EX_TRANSPARENT; force opaque painting
                    cp.ExStyle &= ~0x20;
                    return cp;
                }
            }
            // Always paint the background first so labels cannot see through it
            protected override void OnPaintBackground(PaintEventArgs e)
            {
                e.Graphics.Clear(BackColor);
            }
        }

        // ── Build nav items (centred, wider spacing) ──────────────────────────
        private void BuildNavItems()
        {
            // Remove old items (logo stays at index 0 if already added)
            for (int i = Controls.Count - 1; i >= 0; i--)
                if (Controls[i] != null && !(Controls[i] is Label lc && lc.Text.Contains("PLF")))
                    Controls.RemoveAt(i);
            Controls.Clear();
            _navItems.Clear();

            // Logo — fixed left
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

            // FIX 1: ItemPadH*2 gives generous horizontal breathing room
            var widths = new int[_menus.Length];
            int totalW = 0;
            for (int i = 0; i < _menus.Length; i++)
            {
                widths[i] = TextRenderer.MeasureText(_menus[i].Label, FontNav).Width + ItemPadH * 2;
                totalW   += widths[i];
            }

            int startX = Math.Max(100, (Width - totalW) / 2);
            int x      = startX;

            for (int i = 0; i < _menus.Length; i++)
            {
                int  idx     = i;
                bool hasDrop = _menus[i].Groups.Length > 0;

                Panel item = new Panel
                {
                    Location  = new Point(x, 0),
                    Size      = new Size(widths[i], 44),
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

                // FIX 2: on hover simply show/update the popup; the poll timer
                // decides when to close it — no more relying on MouseLeave.
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
                x += widths[i];
            }

            Resize += (s, e) => RecentreItems();
        }

        private void OnNavItemHover(int idx, Panel navItem, bool hasDrop)
        {
            HighlightItem(idx);
            if (hasDrop)
                ShowMegaMenu(idx, navItem);
            else
                HideMegaMenu();
            _pollTimer.Start();   // begin polling
        }

        // ── Polling timer: close popup when cursor leaves both zones ──────────
        private void PollTimer_Tick(object sender, EventArgs e)
        {
            if (!_megaPopup.Visible)
            {
                // Highlight the nav bar item under the cursor, or clear
                bool overNav = IsOverNavBar();
                if (!overNav) { ClearHighlight(); _pollTimer.Stop(); }
                return;
            }

            bool overPopup = _megaPopup.ClientRectangle.Contains(
                                 _megaPopup.PointToClient(Cursor.Position));
            bool overBar   = IsOverNavBar();

            if (!overPopup && !overBar)
            {
                HideMegaMenu();
                _pollTimer.Stop();
            }
        }

        private bool IsOverNavBar()
        {
            if (!IsHandleCreated) return false;
            Point local = PointToClient(Cursor.Position);
            return ClientRectangle.Contains(local);
        }

        // ── Re-centre on form resize ──────────────────────────────────────────
        private void RecentreItems()
        {
            if (_navItems.Count == 0) return;
            int totalW = 0;
            foreach (Panel p in _navItems) totalW += p.Width;
            int startX = Math.Max(100, (Width - totalW) / 2);
            int x = startX;
            foreach (Panel p in _navItems) { p.Location = new Point(x, 0); x += p.Width; }
        }

        // ── Highlight helpers ─────────────────────────────────────────────────
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

        // ── Mega Menu ──────────────────────────────────────────────────────────
        private void ShowMegaMenu(int idx, Panel navItem)
        {
            var groups = _menus[idx].Groups;
            if (groups.Length == 0) return;

            const int colWidth  = 230;   // slightly wider columns
            const int padLeft   = 16;
            const int padTop    = 16;

            int cols   = groups.Length;
            int popupW = cols * colWidth + padLeft * 2;
            int popupH = padTop;

            // Find tallest column
            foreach (var g in groups)
            {
                int h = padTop + 28 + 4 + g.Items.Length * 34 + 16;
                if (h > popupH) popupH = h;
            }

            _megaPopup.Controls.Clear();
            _megaPopup.Size = new Size(popupW, popupH);

            int cx = padLeft;
            foreach (var (category, items) in groups)
            {
                // FIX 3: use explicit non-transparent background for labels
                Label catLbl = new Label
                {
                    Text      = category,
                    Font      = FontDropHead,
                    ForeColor = DropTextBold,
                    BackColor = DropBg,         // opaque background
                    AutoSize  = false,
                    Size      = new Size(colWidth - 8, 22),
                    Location  = new Point(cx, padTop)
                };
                _megaPopup.Controls.Add(catLbl);

                Panel div = new Panel
                {
                    Size      = new Size(colWidth - 8, 1),
                    Location  = new Point(cx, padTop + 26),
                    BackColor = Color.FromArgb(90, 255, 255, 255)
                };
                _megaPopup.Controls.Add(div);

                int iy = padTop + 34;
                foreach (string sub in items)
                {
                    string captured = sub;

                    Panel row = new Panel
                    {
                        Size      = new Size(colWidth - 8, 32),
                        Location  = new Point(cx, iy),
                        BackColor = DropBg,      // opaque so hover colour shows correctly
                        Cursor    = Cursors.Hand
                    };
                    Label rowLbl = new Label
                    {
                        Text      = captured,
                        Font      = FontDropItem,
                        ForeColor = DropText,
                        BackColor = Color.Transparent,  // inherits row.BackColor
                        Dock      = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleLeft,
                        Padding   = new Padding(8, 0, 0, 0)
                    };
                    row.Controls.Add(rowLbl);

                    Color hoverBg = Color.FromArgb(60, 255, 255, 255);
                    row.MouseEnter    += (s, e) => { row.BackColor = hoverBg; rowLbl.ForeColor = Color.White; };
                    row.MouseLeave    += (s, e) => { row.BackColor = DropBg;  rowLbl.ForeColor = DropText;  };
                    rowLbl.MouseEnter += (s, e) => { row.BackColor = hoverBg; rowLbl.ForeColor = Color.White; };
                    rowLbl.MouseLeave += (s, e) => { row.BackColor = DropBg;  rowLbl.ForeColor = DropText;  };

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
                    iy += 34;
                }
                cx += colWidth;
            }

            // Position popup flush below the nav bar
            Form  owner  = FindForm();
            if (owner == null) return;
            Point formPt = owner.PointToClient(navItem.PointToScreen(new Point(0, Height)));

            int left = formPt.X;
            if (left + popupW > owner.ClientSize.Width - 8)
                left = owner.ClientSize.Width - popupW - 8;
            if (left < 0) left = 0;

            _megaPopup.Location = new Point(left, formPt.Y);

            if (!owner.Controls.Contains(_megaPopup))
            {
                owner.Controls.Add(_megaPopup);
                owner.Controls.SetChildIndex(_megaPopup, 0);
            }
            _megaPopup.BringToFront();
            _megaPopup.Visible = true;
        }

        private void HideMegaMenu()
        {
            if (_megaPopup != null) _megaPopup.Visible = false;
            ClearHighlight();
        }

        // ── Paint ─────────────────────────────────────────────────────────────
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen p = new Pen(Color.FromArgb(60, 255, 255, 255), 1))
                e.Graphics.DrawLine(p, 0, Height - 1, Width, Height - 1);
        }

        private void MegaPopup_Paint(object sender, PaintEventArgs e)
        {
            // Draw rounded border on top of the already-filled OpaquePanel
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
