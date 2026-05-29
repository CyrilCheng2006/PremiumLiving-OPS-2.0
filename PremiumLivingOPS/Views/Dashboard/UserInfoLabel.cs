using System;
using System.ComponentModel;
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

        [DefaultValue("")]
        public string UserName
        {
            get => _userName;
            set { _userName = value ?? string.Empty; RecalcSize(); Invalidate(); }
        }

        [DefaultValue("")]
        public string Department
        {
            get => _department;
            set { _department = value ?? string.Empty; RecalcSize(); Invalidate(); }
        }

        public UserInfoLabel()
        {
            // AllPaintingInWmErase was removed in .NET 6+ WinForms; use AllPaintingInWmPaint instead.
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true);
            BackColor = Color.Transparent;
        }

        /// <summary>Recalculate Width/Height so the parent can read .Width for positioning.</summary>
        private void RecalcSize()
        {
            using Graphics g = CreateGraphics();
            if (g == null) return;

            SizeF szName = g.MeasureString(_userName, FontName);
            SizeF szDept = _department.Length > 0
                ? g.MeasureString($" ({_department})", FontDept)
                : SizeF.Empty;

            int h = (int)Math.Ceiling(szName.Height);
            int w = (int)Math.Ceiling(szName.Width + szDept.Width) + 4;

            Size = new Size(w, h);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using SolidBrush brName = new SolidBrush(DashboardForm.Palette.TextMain);
            g.DrawString(_userName, FontName, brName, 0f, 0f);

            if (_department.Length > 0)
            {
                float nameW = g.MeasureString(_userName, FontName).Width;
                using SolidBrush brDept = new SolidBrush(DashboardForm.Palette.TextMuted);

                float deptH = g.MeasureString(_department, FontDept).Height;
                float nameH = g.MeasureString(_userName,   FontName).Height;
                float deptY = (nameH - deptH) / 2f;

                g.DrawString($" ({_department})", FontDept, brDept, nameW, deptY);
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            RecalcSize();
        }
    }
}
