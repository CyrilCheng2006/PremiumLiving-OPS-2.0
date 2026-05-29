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
    /// </summary>
    public class TopNavBar : Panel
    {
        // ── Colours ──────────────────────────────────────────────────────────
        private static readonly Color NavBg         = Color.FromArgb(29,  29,  31);   // #1d1d1f
        private static readonly Color NavText       = Color.FromArgb(245, 245, 247);  // near-white
        private static readonly Color NavHover      = Color.FromArgb(255, 255, 255, 25); // subtle white tint
        private static readonly Color DropBg        = Color.FromArgb(29,  29,  31);
        private static readonly Color DropItemHover = Color.FromArgb(255, 255, 255, 20);
        private static readonly Color DropText      = Color.FromArgb(210, 210, 215);
        private static readonly Color DropTextBold  = Color.FromArgb(245, 245, 247);
        private static readonly Color Divider       = Color.FromArgb(255, 255, 255, 18);

        // ── Fonts ─────────────────────────────────────────────────────────────
        private static readonly Font FontNav      = new Font("Segoe UI", 12f, FontStyle.Regular);
        private static readonly Font FontDropHead = new Font("Segoe UI", 11f, FontStyle.Bold);
        private static readonly Font FontDropItem = new Font("Segoe UI", 11f, FontStyle.Regular);

        // ── Menu definition ──────────────────────────────────────────────────
        // Each top-level item maps to a list of (category, items[]) pairs.
        private readonly (string Label, (string Category, string[] Items)[] Groups)[] _menus =
        {
            ("訂單管理", new[]
            {
                ("訂單",        new[] { "查看 & 搜索訂單", "建立訂單", "修改訂單" }),
                ("報價",        new[] { "報價管理", "待處理報價" })
            }),
            ("生產 & 物流", new[]
            {
                ("生產",        new[] { "原材料申請", "搜索原材料申請" }),
                ("物流",        new[] { "查看出貨", "處理收貨" })
            }),
            ("庫存 & 採購", new[]
            {
                ("庫存",        new[] { "查看產品 / 原材料" }),
                ("採購",        new[] { "建立採購", "搜索採購" })
            }),
            ("售後服務", new[]
            {
                ("財務",        new[] { "建立發票", "應收帳款", "應付帳款" }),
                ("服務",        new[] { "投訴列表", "退貨列表" })
            }),
            ("系統管理", new[]
            {
                ("主資料",      new[] { "供應商列表", "客戶列表" }),
                ("安全",        new[] { "員工列表", "日誌列表", "統計報告" })
            })
        };

        // ── State ─────────────────────────────────────────────────────────────
        private readonly List<Panel>  _navItems   = new List<Panel>();
        private readonly Panel        _megaPopup;
        private int                   _activeIdx  = -1;
        private System.Windows.Forms.Timer _hideTimer;

        // ── Public Events ─────────────────────────────────────────────────────
        /// <summary>Fires when the user clicks a sub-menu item.</summary>
        public event Action<string> MenuItemClicked;

        // ── Constructor ───────────────────────────────────────────────────────
        public TopNavBar()
        {
            Height          = 44;
            Dock            = DockStyle.Top;
            BackColor       = NavBg;
            Padding         = new Padding(0);

            // Mega-menu popup panel (floats above everything)
            _megaPopup = new Panel
            {
                Visible    = false,
                BackColor  = DropBg,
                AutoSize   = false,
                Padding    = new Padding(24, 16, 24, 20)
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
                Text      = "🪑 PLF",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize  = false, Width = 90, Height = 44,
                TextAlign = ContentAlignment.MiddleCenter,
                Location  = new Point(10, 0)
            };
            Controls.Add(logo);

            int x = 110;
            for (int i = 0; i < _menus.Length; i++)
            {
                int idx = i; // capture for lambda
                string label = _menus[i].Label;

                Panel item = new Panel
                {
                    Location  = new Point(x, 0),
                    Size      = new Size(TextRenderer.MeasureText(label, FontNav).Width + 28, 44),
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

                // Hover: show mega menu
                item.MouseEnter += (s, e) => { _hideTimer.Stop(); ShowMegaMenu(idx, item); HighlightItem(idx); };
                item.MouseLeave += (s, e) => { _hideTimer.Start(); };
                lbl.MouseEnter  += (s, e) => { _hideTimer.Stop(); ShowMegaMenu(idx, item); HighlightItem(idx); };
                lbl.MouseLeave  += (s, e) => { _hideTimer.Start(); };

                Controls.Add(item);
                _navItems.Add(item);
                x += item.Width;
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

            // Calculate popup width: each group column = 180px
            int cols      = groups.Length;
            int colWidth  = 180;
            int popupW    = cols * colWidth + 48;   // 48 = left+right padding
            int popupH    = 0;

            // Find tallest column
            foreach (var g in groups)
            {
                int h = 26 + 6 + g.Items.Length * 32 + 20; // heading + gap + items + bottom
                if (h > popupH) popupH = h;
            }
            popupH += 36; // top padding

            _megaPopup.Controls.Clear();
            _megaPopup.Size = new Size(popupW, popupH);

            int cx = 0;
            foreach (var (category, items) in groups)
            {
                // Category heading
                Label catLbl = new Label
                {
                    Text      = category,
                    Font      = FontDropHead,
                    ForeColor = DropTextBold,
                    AutoSize  = false,
                    Size      = new Size(colWidth - 8, 26),
                    Location  = new Point(cx, 0)
                };
                _megaPopup.Controls.Add(catLbl);

                // Divider under heading
                Panel div = new Panel
                {
                    Size      = new Size(colWidth - 16, 1),
                    Location  = new Point(cx, 30),
                    BackColor = Color.FromArgb(80, 255, 255, 255)
                };
                _megaPopup.Controls.Add(div);

                int iy = 40;
                foreach (string item in items)
                {
                    string capturedItem = item;
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
                        Padding   = new Padding(4, 0, 0, 0)
                    };
                    row.Controls.Add(rowLbl);

                    // Hover glow
                    row.MouseEnter += (s, e) => { row.BackColor = Color.FromArgb(45, 255, 255, 255); rowLbl.ForeColor = Color.White; };
                    row.MouseLeave += (s, e) => { row.BackColor = Color.Transparent; rowLbl.ForeColor = DropText; };
                    rowLbl.MouseEnter += (s, e) => { row.BackColor = Color.FromArgb(45, 255, 255, 255); rowLbl.ForeColor = Color.White; };
                    rowLbl.MouseLeave += (s, e) => { row.BackColor = Color.Transparent; rowLbl.ForeColor = DropText; };

                    // Click
                    row.Click    += (s, e) => { HideMegaMenu(); MenuItemClicked?.Invoke(capturedItem); };
                    rowLbl.Click += (s, e) => { HideMegaMenu(); MenuItemClicked?.Invoke(capturedItem); };

                    _megaPopup.Controls.Add(row);
                    iy += 32;
                }
                cx += colWidth;
            }

            // Position: below nav item, anchored to left edge of nav item
            Point screenPt = navItem.PointToScreen(new Point(0, navItem.Height));
            Form owner = FindForm();
            if (owner == null) return;
            Point formPt = owner.PointToClient(screenPt);

            // Clamp so popup doesn't go off-screen
            int left = formPt.X;
            if (left + popupW > owner.ClientSize.Width - 10)
                left = owner.ClientSize.Width - popupW - 10;
            if (left < 0) left = 0;

            _megaPopup.Location = new Point(left, formPt.Y);

            // Attach to form if not already
            if (!owner.Controls.Contains(_megaPopup))
            {
                owner.Controls.Add(_megaPopup);
                owner.Controls.SetChildIndex(_megaPopup, 0); // bring to front
            }

            _megaPopup.BringToFront();
            _megaPopup.Visible = true;

            // Mouse leave on popup → start hide timer
            _megaPopup.MouseLeave -= MegaPopup_MouseLeave;
            _megaPopup.MouseLeave += MegaPopup_MouseLeave;
        }

        private void MegaPopup_MouseLeave(object sender, EventArgs e)
        {
            // Only hide if mouse truly left the popup area
            Point mouse = _megaPopup.PointToClient(Cursor.Position);
            if (!_megaPopup.ClientRectangle.Contains(mouse))
                _hideTimer.Start();
        }

        private void HideMegaMenu()
        {
            _megaPopup.Visible = false;
            ClearHighlight();
        }

        // ── Paint: bottom border + semi-transparent backdrop ──────────────────
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen p = new Pen(Color.FromArgb(60, 255, 255, 255), 1))
                e.Graphics.DrawLine(p, 0, Height - 1, Width, Height - 1);
        }

        private void MegaPopup_Paint(object sender, PaintEventArgs e)
        {
            // Rounded corners
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
            path.AddArc(r.X,             r.Y,              d, d, 180, 90);
            path.AddArc(r.Right - d,     r.Y,              d, d, 270, 90);
            path.AddArc(r.Right - d,     r.Bottom - d,     d, d,   0, 90);
            path.AddArc(r.X,             r.Bottom - d,     d, d,  90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
