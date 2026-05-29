using System;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.Dashboard
{
    /// <summary>
    /// Owner-drawn label that renders:
    ///   StaffName  (Department)
    /// where StaffName uses a larger font and (Department) uses a smaller, muted font.
    /// Width is auto-calculated from the two text segments so layoutUserBar can position
    /// the control correctly.
    /// </summary>
    public class UserInfoLabel : Control
    {
        private static readonly Font FontName = new Font("Segoe UI", 14.4f, FontStyle.Regular);
        private static readonly Font FontDept = new Font("Segoe UI", 11f,   FontStyle.Regular);

        private string _userName   = string.Empty;
        private string _department = string.Empty;

        public string UserName
        {
            get => _userName;
            set { _userName = value ?? string.Empty; RecalcSize(); Invalidate(); }
        }

        public string Department
        {
            get => _department;
            set { _department = value ?? string.Empty; RecalcSize(); Invalidate(); }
        }

        public UserInfoLabel()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmErase |
                ControlStyles.OptimizedDoubleBuffer,
                true);
            BackColor = Color.Transparent;
        }

        // Recalculate Width/Height so the parent can read .Width for positioning.
        private void RecalcSize()
        {
            using Graphics g = CreateGraphics();

            // Guard: CreateGraphics may fail if handle not yet created
            if (g == null) return;

            SizeF szName = g.MeasureString(_userName, FontName);
            SizeF szDept = _department.Length > 0
                ? g.MeasureString($" ({_department})", FontDept)
                : SizeF.Empty;

            // Height = tallest of the two fonts, anchored to name baseline
            int h = (int)Math.Ceiling(szName.Height);
            int w = (int)Math.Ceiling(szName.Width + szDept.Width) + 4; // +4 anti-clip

            Size = new Size(w, h);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Draw StaffName
            using SolidBrush brName = new SolidBrush(
                DashboardForm.Palette.TextMain);
            g.DrawString(_userName, FontName, brName, 0f, 0f);

            // Draw (Department) offset by the width of the name
            if (_department.Length > 0)
            {
                float nameW = g.MeasureString(_userName, FontName).Width;
                using SolidBrush brDept = new SolidBrush(
                    DashboardForm.Palette.TextMuted);

                // Vertically align (Department) to the name baseline
                float deptH  = g.MeasureString(_department, FontDept).Height;
                float nameH  = g.MeasureString(_userName, FontName).Height;
                float deptY  = (nameH - deptH) / 2f;   // centre smaller text on name row

                g.DrawString($" ({_department})", FontDept, brDept, nameW, deptY);
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            RecalcSize(); // now CreateGraphics() is valid
        }
    }
}
