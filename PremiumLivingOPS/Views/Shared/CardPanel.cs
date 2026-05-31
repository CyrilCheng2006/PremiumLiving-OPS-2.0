using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.Shared
{
    /// <summary>
    /// CardPanel — 標準三層巢狀卡片包裝器
    ///
    /// 視覺效果：「白色卡片浮在灰色頁面上」
    ///
    /// 層次結構：
    ///   Outer (灰底 + Padding)
    ///     └── Inner / pnlCard (白底 + 1px 灰邊框)
    ///                   └── Content (你的實際內容 Panel / TLP)
    ///
    /// 使用方法：
    ///   // 建立卡片並放入內容
    ///   var (outer, inner) = CardPanel.Create(dockStyle: DockStyle.Top, outerHeight: 300);
    ///   inner.Controls.Add(myContentPanel);
    ///   parentPanel.Controls.Add(outer);
    ///
    ///   // 或使用 Fill 版本（用於 Grid 區域等填滿剩餘空間的卡片）
    ///   var (outer, inner) = CardPanel.CreateFill();
    ///   inner.Controls.Add(dgvOrders);
    ///   parentPanel.Controls.Add(outer);
    /// </summary>
    public static class CardPanel
    {
        // ── 顏色常數（與 Palette.cs 保持一致）────────────────────────────────
        private static readonly Color PageBg   = Color.FromArgb(240, 244, 249);  // #F0F4F9
        private static readonly Color CardBg   = Color.White;
        private static readonly Color BorderCl = Color.FromArgb(221, 227, 236);  // #DDE3EC

        // ── 預設 Padding（外層灰底距卡片邊緣的間距）───────────────────────────
        private static readonly Padding DefaultOuterPadding = new Padding(20, 14, 20, 8);
        private static readonly Padding DefaultFillPadding  = new Padding(20, 12, 20, 0);

        /// <summary>
        /// 建立一個固定高度的卡片（適用於 DockStyle.Top 區塊，如搜尋欄、KPI bar）。
        /// </summary>
        /// <param name="outerHeight">外層 Panel 的總高度（px），包含上下 Padding。</param>
        /// <param name="outerPadding">外層灰底的 Padding，預設 (20,14,20,8)。</param>
        /// <returns>(outer, inner) — outer 加入父容器，inner 加入你的內容。</returns>
        public static (Panel outer, Panel inner) Create(
            int outerHeight,
            Padding? outerPadding = null)
        {
            var padding = outerPadding ?? DefaultOuterPadding;

            var outer = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = outerHeight,
                BackColor = PageBg,
                Padding   = padding
            };

            var inner = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = CardBg
            };
            inner.Paint += PaintCardBorder;

            outer.Controls.Add(inner);
            return (outer, inner);
        }

        /// <summary>
        /// 建立一個填滿剩餘空間的卡片（適用於 DockStyle.Fill 區塊，如 DataGridView）。
        /// </summary>
        /// <param name="outerPadding">外層灰底的 Padding，預設 (20,12,20,0)。</param>
        /// <returns>(outer, inner) — outer 加入父容器，inner 加入你的內容。</returns>
        public static (Panel outer, Panel inner) CreateFill(
            Padding? outerPadding = null)
        {
            var padding = outerPadding ?? DefaultFillPadding;

            var outer = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = PageBg,
                Padding   = padding
            };

            var inner = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = CardBg
            };
            inner.Paint += PaintCardBorder;

            outer.Controls.Add(inner);
            return (outer, inner);
        }

        // ── 邊框繪製（1px #DDE3EC 矩形）─────────────────────────────────────
        private static void PaintCardBorder(object s, System.Windows.Forms.PaintEventArgs e)
        {
            var p = (Panel)s;
            using var pen = new System.Drawing.Pen(BorderCl, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        }
    }
}
