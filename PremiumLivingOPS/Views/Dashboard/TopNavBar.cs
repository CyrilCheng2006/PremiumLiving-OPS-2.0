using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.Dashboard
{
    /// <summary>
    /// Apple-style dark top navigation bar:
    ///  1. Sticky at the top of the form (DockStyle.Top)
    ///  2. Dark background (#1d1d1f) + white text
    ///  3. Nav items CENTRED horizontally
    ///  4. Hover highlight on nav items
    ///  5. Mega-menu dropdown on hover
    ///  6. All sub-items show a "Coming Soon" message when clicked
    /// </summary>
    public class TopNavBar : Panel
    {
        // ── Colours ───────────────────────────────────────────────────────────
        private static readonly Color NavBg        = Color.FromArgb(29,  29,  31);
        private static readonly Color NavText      = Color.FromArgb(245, 245, 247);
        private static readonly Color DropBg       = Color.FromArgb(29,  29,  31);
        private static readonly Color DropText     = Color.FromArgb(210, 210, 215);
        private static readonly Color DropTextBold = Color.FromArgb(245, 245, 247);

        // ── Fonts ─────────────────────────────────────────────────────────────
        private static readonly Font FontNav      = new Font("Segoe UI", 11f,   FontStyle.Regular);
        private static readonly Font FontDropHead = new Font("Segoe UI", 10f,   FontStyle.Bold);
        private static readonly Font FontDropItem = new Font("Segoe UI", 10.5f, FontStyle.Regular);

        // ── Menu definition ───────────────────────────────────────────────────
        private readonly (string Label, (string Category, string[] Items)[] Groups)[] _menus =
        {
            // Dashboard — no dropdown; clicking goes directly home
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
        private readonly Panel       _megaPopup;
        private int                  _activeIdx = -1;
        private System.Windows.Forms.Timer _hideTimer;

        // ── Public Events ─────────────────────────────────────────────────────
        public event Action<string> MenuItemClicked;

        // ── Constructor ───────────────────────────────────────────────────────
        public TopNavBar()
        {
            Height    = 44;
            Dock      = DockStyle.Top;
            BackColor = NavBg;
            Padding   = new Padding(0);

            _megaPopup = new Panel
            {
                Visible   = false,
                BackColor = DropBg,
                AutoSize  = false,
                Padding   = new Padding(20, 14, 20, 18)
            };
            _megaPopup.Paint += MegaPopup_Paint;

            _hideTimer = new System.Windows.Forms.Timer { Interval = 120 };
            _hideTimer.Tick += (s, e) => { _hideTimer.Stop(); HideMegaMenu(); };

            // Build after handle is created so Width is available for centring
            HandleCreated += (s, e) => BuildNavItems();
        }

        // ── Build nav items (centred) ─────────────────────────────────────────
        private void BuildNavItems()
        {
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

            // Pre-calculate each item's width
            var widths = new int[_menus.Length];
            int totalW = 0;
            for (int i = 0; i < _menus.Length; i++)
            {
                widths[i] = TextRenderer.MeasureText(_menus[i].Label, FontNav).Width + 24;
                totalW   += widths[i];
            }

            // Centre: start X so that the whole group is in the middle
            int startX = Math.Max(100, (Width - totalW) / 2);

            int x = startX;
            for (int i = 0; i < _menus.Length; i++)
            {
                int   idx     = i;
                bool  hasDrop = _menus[i].Groups.Length > 0;

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

                // Hover
                item.MouseEnter += (s, e) => { _hideTimer.Stop(); HighlightItem(idx); if (hasDrop) ShowMegaMenu(idx, item); else HideMegaMenu(); };
                item.MouseLeave += (s, e) => { if (hasDrop) _hideTimer.Start(); else ClearHighlight(); };
                lbl.MouseEnter  += (s, e) => { _hideTimer.Stop(); HighlightItem(idx); if (hasDrop) ShowMegaMenu(idx, item); else HideMegaMenu(); };
                lbl.MouseLeave  += (s, e) => { if (hasDrop) _hideTimer.Start(); else ClearHighlight(); };

                // Dashboard top-level — click goes home (no popup)
                if (!hasDrop)
                {
                    EventHandler goHome = (s, e) => { ClearHighlight(); MenuItemClicked?.Invoke("Dashboard"); };
                    item.Click += goHome;
                    lbl.Click  += goHome;
                }

                Controls.Add(item);
                _navItems.Add(item);
                x += widths[i];
            }

            Resize += (s, e) => RecentreItems();
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

            const int colWidth = 220;
            int cols   = groups.Length;
            int popupW = cols * colWidth + 40;
            int popupH = 0;
            foreach (var g in groups)
            {
                int h = 26 + 8 + g.Items.Length * 32 + 20;
                if (h > popupH) popupH = h;
            }
            popupH += 32;

            _megaPopup.Controls.Clear();
            _megaPopup.Size = new Size(popupW, popupH);

            int cx = 0;
            foreach (var (category, items) in groups)
            {
                _megaPopup.Controls.Add(new Label
                {
                    Text      = category,
                    Font      = FontDropHead,
                    ForeColor = DropTextBold,
                    AutoSize  = false,
                    Size      = new Size(colWidth - 8, 24),
                    Location  = new Point(cx, 0)
                });

                _megaPopup.Controls.Add(new Panel
                {
                    Size      = new Size(colWidth - 16, 1),
                    Location  = new Point(cx, 28),
                    BackColor = Color.FromArgb(80, 255, 255, 255)
                });

                int iy = 38;
                foreach (string sub in items)
                {
                    string captured = sub;
                    Panel row = new Panel
                    {
                        Size      = new Size(colWidth - 8, 30),
                        Location  = new Point(cx, iy),
                        BackColor = Color.Transparent,
                        Cursor    = Cursors.Hand
                    };
                    Label rowLbl = new Label
                    {
                        Text      = captured,
                        Font      = FontDropItem,
                        ForeColor = DropText,
                        Dock      = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleLeft,
                        Padding   = new Padding(6, 0, 0, 0)
                    };
                    row.Controls.Add(rowLbl);

                    row.MouseEnter    += (s, e) => { row.BackColor = Color.FromArgb(45, 255, 255, 255); rowLbl.ForeColor = Color.White; };
                    row.MouseLeave    += (s, e) => { row.BackColor = Color.Transparent; rowLbl.ForeColor = DropText; };
                    rowLbl.MouseEnter += (s, e) => { row.BackColor = Color.FromArgb(45, 255, 255, 255); rowLbl.ForeColor = Color.White; };
                    rowLbl.MouseLeave += (s, e) => { row.BackColor = Color.Transparent; rowLbl.ForeColor = DropText; };

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
                    iy += 32;
                }
                cx += colWidth;
            }

            // Position popup below the hovered nav item
            Form  owner  = FindForm();
            if (owner == null) return;
            Point formPt = owner.PointToClient(navItem.PointToScreen(new Point(0, navItem.Height)));

            int left = formPt.X;
            if (left + popupW > owner.ClientSize.Width - 10)
                left = owner.ClientSize.Width - popupW - 10;
            if (left < 0) left = 0;

            _megaPopup.Location = new Point(left, formPt.Y);

            if (!owner.Controls.Contains(_megaPopup))
            {
                owner.Controls.Add(_megaPopup);
                owner.Controls.SetChildIndex(_megaPopup, 0);
            }
            _megaPopup.BringToFront();
            _megaPopup.Visible = true;

            _megaPopup.MouseLeave -= MegaPopup_MouseLeave;
            _megaPopup.MouseLeave += MegaPopup_MouseLeave;
        }

        private void MegaPopup_MouseLeave(object sender, EventArgs e)
        {
            if (!_megaPopup.ClientRectangle.Contains(_megaPopup.PointToClient(Cursor.Position)))
                _hideTimer.Start();
        }

        private void HideMegaMenu()
        {
            _megaPopup.Visible = false;
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
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, _megaPopup.Width - 1, _megaPopup.Height - 1);
            using (GraphicsPath path = RoundedRect(rect, 10))
            using (SolidBrush br   = new SolidBrush(DropBg))
            using (Pen border      = new Pen(Color.FromArgb(70, 255, 255, 255), 1))
            {
                e.Graphics.FillPath(br, path);
                e.Graphics.DrawPath(border, path);
            }
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
