using System.Drawing;

namespace PremiumLivingOPS.Views.Shared
{
    /// <summary>
    /// Application-wide colour palette.
    /// Centralised here so every Form/View can reference Palette.xxx
    /// without depending on DashboardForm's internal class.
    ///
    /// MVC note (View layer): this file belongs to the View layer because
    /// it contains only UI colour constants with no business logic.
    /// </summary>
    public static class Palette
    {
        // ── Surfaces ──────────────────────────────────────────────────
        public static readonly Color BgPage      = Color.FromArgb(240, 244, 249);
        public static readonly Color BgCard      = Color.White;

        // ── Borders ───────────────────────────────────────────────────
        public static readonly Color BorderColor = Color.FromArgb(221, 227, 236);

        // ── Accent colours ────────────────────────────────────────────
        public static readonly Color Primary     = Color.FromArgb(47,  111, 237);
        public static readonly Color PrimaryDark = Color.FromArgb(26,  77,  192);
        public static readonly Color Danger      = Color.FromArgb(232, 64,  64);
        public static readonly Color Success     = Color.FromArgb(30,  184, 122);
        public static readonly Color Warning     = Color.FromArgb(245, 158, 11);
        public static readonly Color Info        = Color.FromArgb(6,   182, 212);

        // ── Text ──────────────────────────────────────────────────────
        public static readonly Color TextMain    = Color.FromArgb(15,  31,  53);
        public static readonly Color TextMuted   = Color.FromArgb(98,  112, 135);

        // ── Sidebar (kept for DashboardForm compatibility) ─────────────
        public static readonly Color SidebarBg    = Color.FromArgb(19,  35,  61);
        public static readonly Color SidebarText  = Color.FromArgb(205, 216, 234);
        public static readonly Color SidebarHover = Color.FromArgb(30,  53,  88);

        // ── Status badge colour pairs ─────────────────────────────────
        public static readonly Color TagBlueBg    = Color.FromArgb(219, 234, 254);
        public static readonly Color TagBlueFg    = Color.FromArgb(29,  78,  216);
        public static readonly Color TagGreenBg   = Color.FromArgb(209, 250, 229);
        public static readonly Color TagGreenFg   = Color.FromArgb(6,   95,  70);
        public static readonly Color TagRedBg     = Color.FromArgb(254, 226, 226);
        public static readonly Color TagRedFg     = Color.FromArgb(153, 27,  27);
        public static readonly Color TagYellowBg  = Color.FromArgb(254, 243, 199);
        public static readonly Color TagYellowFg  = Color.FromArgb(146, 64,  14);
        public static readonly Color TagGrayBg    = Color.FromArgb(241, 245, 249);
        public static readonly Color TagGrayFg    = Color.FromArgb(71,  85,  105);
        public static readonly Color TagOrangeBg  = Color.FromArgb(255, 237, 213);
        public static readonly Color TagOrangeFg  = Color.FromArgb(154, 52,  18);

        // ── Helpers ───────────────────────────────────────────────────
        /// <summary>Returns an accent colour by semantic key name.</summary>
        public static Color FromKey(string key)
        {
            switch (key)
            {
                case "Primary": return Primary;
                case "Success": return Success;
                case "Warning": return Warning;
                case "Danger":  return Danger;
                case "Info":    return Info;
                default:        return Primary;
            }
        }

        /// <summary>
        /// Returns (background, foreground) badge colours for a given status string.
        /// </summary>
        public static (Color bg, Color fg) TagColours(string status)
        {
            switch (status)
            {
                case "Processing":
                case "In Transit":
                case "Shipped":
                case "Converted":
                    return (TagBlueBg,   TagBlueFg);

                case "Delivered":
                case "Paid":
                case "Completed":
                    return (TagGreenBg,  TagGreenFg);

                case "Pending":
                case "Scheduled":
                    return (TagYellowBg, TagYellowFg);

                case "Cancelled":
                case "Critical":
                case "Overdue":
                case "Rejected":
                    return (TagRedBg,    TagRedFg);

                case "Low":
                    return (TagOrangeBg, TagOrangeFg);

                default:
                    return (TagGrayBg,   TagGrayFg);
            }
        }
    }
}
