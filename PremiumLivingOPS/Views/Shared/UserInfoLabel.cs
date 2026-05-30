using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.Shared
{
    /// <summary>
    /// Owner-drawn label that renders:
    ///   StaffName  (Department)
    /// where StaffName uses a larger font and (Department) uses a smaller, muted font.
    ///
    /// Reuse note
    /// ──────────
    /// Colour constants are defined inline (no dependency on DashboardForm.Palette)
    /// so this control can be hosted on any Form without pulling in Dashboard code.
    /// </summary>
    public class UserInfoLabel : Control
    {
        private static readonly Font FontName = new Font("Segoe UI", 14.4f, FontStyle.Regular);
        private static readonly Font FontDept = new Font("Segoe UI", 11f,   FontStyle.Regular);

        // Stand-alone colour definitions (mirror DashboardForm.Palette values)
        private static readonly Color ColorTextMain  = Color.FromArgb(15,  31,  53);
        private static readonly Color ColorTextMuted = Color.FromArgb(98, 112, 135);

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
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor,
                true);
            UpdateStyles();
            BackColor = Color.Transparent;
        }

        private void RecalcSize()
        {
            if (!IsHandleCreated) return;
            using Graphics g = CreateGraphics();
            SizeF szName = g.MeasureString(string.IsNullOrEmpty(_userName) ? " " : _userName, FontName);
            SizeF szDept = _department.Length > 0
                ? g.MeasureString($" ({_department})", FontDept)
                : SizeF.Empty;
            Size = new Size((int)Math.Ceiling(szName.Width + szDept.Width) + 4,
                            (int)Math.Ceiling(szName.Height));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using SolidBrush brName = new SolidBrush(ColorTextMain);
            g.DrawString(_userName, FontName, brName, 0f, 0f);

            if (_department.Length > 0)
            {
                float nameW = g.MeasureString(_userName, FontName).Width;
                using SolidBrush brDept = new SolidBrush(ColorTextMuted);
                float deptH = g.MeasureString(_department, FontDept).Height;
                float nameH = g.MeasureString(_userName,   FontName).Height;
                g.DrawString($" ({_department})", FontDept, brDept, nameW, (nameH - deptH) / 2f);
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            RecalcSize();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000020; // WS_EX_TRANSPARENT
                return cp;
            }
        }
    }
}
