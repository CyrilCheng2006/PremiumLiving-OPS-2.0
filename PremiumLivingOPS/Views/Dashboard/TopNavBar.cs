using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.Dashboard
{
    /// <summary>
    /// Apple-style dark top navigation bar:
    ///  1. Sticky / always-on-top within the form
    ///  2. Dark background (#1d1d1f) + white text
    ///  3. Hover highlight on nav items
    ///  4. Mega-menu dropdown on hover
    ///  5. Navigation structure mirrors the original Sidebar (all English)
    /// </summary>
    public class TopNavBar : Panel
    {
        // ── Colours ──────────────────────────────────────────────────────────
        private static readonly Color NavBg         = Color.FromArgb(29,  29,  31);   // #1d1d1f
        private static readonly Color NavText       = Color.FromArgb(245, 245, 247);
        private static readonly Color DropBg        = Color.FromArgb(29,  29,  31);
        private static readonly Color DropText      = Color.FromArgb(210, 210, 215);
        private static readonly Color DropTextBold  = Color.FromArgb(245, 245, 247);

        // ── Fonts ─────────────────────────────────────────────────────────────
        private static readonly Font FontNav      = new Font("Segoe UI", 11f, FontStyle.Regular);
        private static readonly Font FontDropHead = new Font("Segoe UI", 10f, FontStyle.Bold);
        private static readonly Font FontDropItem = new Font("Segoe UI", 10.5f, FontStyle.Regular);

        // ── Menu definition (mirrors original Sidebar, all English) ──────────
        private readonly (string Label, (string Category, string[] Items)[] Groups)[] _menus =
        {
            ("Dashboard", new (string, string[])[]
            {
                ("Home", new[] { "Dashboard" })
            }),
            ("1. Order Processing", new (string, string[])[]
            {
                ("ORDER PROCESSING MGT", new[]
                {
                    "View & Search Order",
                    "Quotation",
                    "Create Order",
                    "Modify Order"
                })
            }),
            ("2. Production", new (string, string[])[]
            {
                ("PRODUCTION PROCESSING MGT", new[]
                {
                    "Search Raw Material Request",
                    "Create Raw Material Request"
                })
            }),
            ("3. Logistics", new (string, string[])[]
            {
                ("LOGISTICS PROCESSING MGT", new[]
                {
                    "View Shipment",
                    "Handling Goods Received"
                })
            }),
            ("4. Inventory", new (string, string[])[]
            {
                ("INVENTORY CONTROL MGT", new[]
                {
                    "View Product / Raw Material"
                })
            }),
            ("5. Raw Material", new (string, string[])[]
            {
                ("RAW MATERIAL MGT", new[]
                {
                    "Create Procurement",
                    "Search & List Procurement"
                })
            }),
            ("6. After-Service", new (string, string[])[]
            {
                ("AFTER-SERVICE MGT", new[]
                {
                    "Create Invoice",
                    "Complaint List",
                    "Return Order List",
                    "Account Receivable",
                    "Account Payable"
                })
            }),
            ("7. Master Data", new (string, string[])[]
            {
                ("MASTER DATA MAINTENANCE", new[]
                {
                    "Supplier List",
                    "Customer List"
                })
            }),
            ("8. Security", new (string, string[])[]
            {
                ("SYSTEM SECURITY & CONTROL", new[]
                {
                    "Staff List",
                    "Log List"
                })
            }),
            ("9. Reports", new (string, string[])[]
            {
                ("STATISTICAL REPORTS", new[]
                {
                    "View Report"
                })
            })
        };

        // ── State ─────────────────────────────────────────────────────────────
        private readonly List<Panel>  _navItems  = new List<Panel>();
        private readonly Panel        _megaPopup;
        private int                   _activeIdx = -1;
        private System.Windows.Forms.Timer _hideTimer;

        // ── Public Events ─────────────────────────────────────────────────────
        /// <summary>Fires when the user clicks a sub-menu item.</summary>
        public event Action<string> MenuItemClicked;

        // ── Constructor ───────────────────────────────────────────────────────
        public TopNavBar()
        {
            Height    = 44;
            Dock      = DockStyle.Top;
            BackColor = NavBg;
            Padding   = new Padding(0);

            // Mega-menu popup panel (floats above everything)
            _megaPopup = new Panel
            {
                Visible   = false,
                BackColor = DropBg,
                AutoSize  = false,
                Padding   = new Padding(20, 14, 20, 18)
            };
            _megaPopup.Paint += MegaPopup_Paint;

            // Hide-timer: gives user time to move into the popup
            _hideTimer = new System.Windows.Forms.Timer { Interval = 120 };
            _hideTimer.Tick += (s, e) => { _hideTimer.Stop(); HideMegaMenu(); };

            BuildNavItems();
        }

        // ── Build nav item buttons ─────────────────────────────────────────────
        private void BuildNavItems()
        {
            // Logo label on far left
            Label logo = new Label
            {
                Text      = "\uD83E\uDE91 PLF",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize  = false, Width = 80, Height = 44,
                TextAlign = ContentAlignment.MiddleCenter,
                Location  = new Point(8, 0)
            };
            Controls.Add(logo);

            int x = 96;
            for (int i = 0; i < _menus.Length; i++)
            {
                int idx   = i;
                string label = _menus[i].Label;

                int itemW = TextRenderer.MeasureText(label, FontNav).Width + 22;

                Panel item = new Panel
                {
                    Location  = new Point(x, 0),
                    Size      = new Size(itemW, 44),
                    BackColor = Color.Transparent,
                    Cursor    = Cursors.Hand,
                    Tag       = idx
                };

                Label lbl = new Label
                {
                    Text      = label,
                    Font      = FontNav,
                    ForeColor = NavText,
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };

                item.Controls.Add(lbl);

                item.MouseEnter += (s, e) => { _hideTimer.Stop(); ShowMegaMenu(idx, item); HighlightItem(idx); };
                item.MouseLeave += (s, e) => { _hideTimer.Start(); };
                lbl.MouseEnter  += (s, e) => { _hideTimer.Stop(); ShowMegaMenu(idx, item); HighlightItem(idx); };
                lbl.MouseLeave  += (s, e) => { _hideTimer.Start(); };

                Controls.Add(item);
                _navItems.Add(item);
                x += itemW;
            }
        }

        // ── Highlight active nav item ──────────────────────────────────────────
        private void HighlightItem(int idx)
        {
            for (int i = 0; i < _navItems.Count; i++)
            {
                bool active = i == idx;
                _navItems[i].BackColor = active
                    ? Color.FromArgb(60, 255, 255, 255)
                    : Color.Transparent;
                foreach (Control c in _navItems[i].Controls)
                    if (c is Label l) l.ForeColor = active ? Color.White : NavText;
            }
            _activeIdx = idx;
        }

        private void ClearHighlight()
        {
            foreach (Panel item in _navItems)
            {
                item.BackColor = Color.Transparent;
                foreach (Control c in item.Controls)
                    if (c is Label l) l.ForeColor = NavText;
            }
            _activeIdx = -1;
        }

        // ── Mega Menu show/hide ────────────────────────────────────────────────
        private void ShowMegaMenu(int idx, Panel navItem)
        {
            var menu   = _menus[idx];
            var groups = menu.Groups;

            int colWidth = 220;
            int cols     = groups.Length;
            int popupW   = cols * colWidth + 40;
            int popupH   = 0;

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
                Label catLbl = new Label
                {
                    Text      = category,
                    Font      = FontDropHead,
                    ForeColor = DropTextBold,
                    AutoSize  = false,
                    Size      = new Size(colWidth - 8, 24),
                    Location  = new Point(cx, 0)
                };
                _megaPopup.Controls.Add(catLbl);

                Panel div = new Panel
                {
                    Size      = new Size(colWidth - 16, 1),
                    Location  = new Point(cx, 28),
                    BackColor = Color.FromArgb(80, 255, 255, 255)
                };
                _megaPopup.Controls.Add(div);

                int iy = 38;
                foreach (string itemLabel in items)
                {
                    string capturedItem = itemLabel;
                    Panel row = new Panel
                    {
                        Size      = new Size(colWidth - 8, 30),
                        Location  = new Point(cx, iy),
                        BackColor = Color.Transparent,
                        Cursor    = Cursors.Hand
                    };
                    Label rowLbl = new Label
                    {
                        Text      = capturedItem,
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

                    row.Click    += (s, e) => { HideMegaMenu(); MenuItemClicked?.Invoke(capturedItem); };
                    rowLbl.Click += (s, e) => { HideMegaMenu(); MenuItemClicked?.Invoke(capturedItem); };

                    _megaPopup.Controls.Add(row);
                    iy += 32;
                }
                cx += colWidth;
            }

            Point screenPt = navItem.PointToScreen(new Point(0, navItem.Height));
            Form owner = FindForm();
            if (owner == null) return;
            Point formPt = owner.PointToClient(screenPt);

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
            Point mouse = _megaPopup.PointToClient(Cursor.Position);
            if (!_megaPopup.ClientRectangle.Contains(mouse))
                _hideTimer.Start();
        }

        private void HideMegaMenu()
        {
            _megaPopup.Visible = false;
            ClearHighlight();
        }

        // ── Paint: bottom border ──────────────────────────────────────────────
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen p = new Pen(Color.FromArgb(60, 255, 255, 255), 1))
                e.Graphics.DrawLine(p, 0, Height - 1, Width, Height - 1);
        }

        private void MegaPopup_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, _megaPopup.Width - 1, _megaPopup.Height - 1);
            using (GraphicsPath path = RoundedRect(rect, 10))
            using (SolidBrush br = new SolidBrush(DropBg))
            using (Pen border = new Pen(Color.FromArgb(70, 255, 255, 255), 1))
            {
                g.FillPath(br, path);
                g.DrawPath(border, path);
            }
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(r.X,         r.Y,          d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y,          d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d,   0, 90);
            path.AddArc(r.X,         r.Bottom - d, d, d,  90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
