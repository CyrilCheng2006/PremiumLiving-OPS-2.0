using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.StatisticalReports
{
    // ════════════════════════════════════════════════════════════════════════════
    //  ViewReportForm — Helpers partial
    //
    //  NOTE: PaintCardBorder, DgvCellFormatting, and ExportGrid are defined
    //  once in ViewReportForm.cs.  This file only contains:
    //    • Palette  — local alias mapping to Shared.Palette property names
    //    • QuoteCsv — CSV escaping helper (used by ExportGrid in ViewReportForm.cs)
    //    • AppShell event handlers
    //
    //  Palette alias mapping (Shared.Palette actual names):
    //    Surface     → BgCard       (Color.White)
    //    Border      → BorderColor  (221, 227, 236)
    //    TextPrimary → TextMain     (15,  31,  53)
    //    TextMuted   → TextMuted    (same name, no change needed)
    //    BgPage      → BgPage       (same name, no change needed)
    //    Primary     → Primary      (same name, no change needed)
    // ════════════════════════════════════════════════════════════════════════════

    partial class ViewReportForm
    {
        // ────────────────────────────────────────────────────────────────────────
        //  Palette — aliases mapped to actual Shared.Palette property names.
        //
        //  Shared.Palette (authoritative source) defines:
        //    BgPage, BgCard, BorderColor, Primary, PrimaryDark,
        //    Danger, Success, Warning, Info, TextMain, TextMuted, ...
        //
        //  ViewReportForm.cs references: BgPage, Primary, Surface, Border,
        //  TextMuted, TextPrimary.  The three that differ are mapped below.
        // ────────────────────────────────────────────────────────────────────────
        internal static class Palette
        {
            // Direct forwards (same name in Shared.Palette)
            public static Color BgPage    => PremiumLivingOPS.Views.Shared.Palette.BgPage;
            public static Color Primary   => PremiumLivingOPS.Views.Shared.Palette.Primary;
            public static Color TextMuted => PremiumLivingOPS.Views.Shared.Palette.TextMuted;

            // Renamed forwards
            // Shared.Palette.BgCard      → local alias "Surface"
            public static Color Surface     => PremiumLivingOPS.Views.Shared.Palette.BgCard;
            // Shared.Palette.BorderColor  → local alias "Border"
            public static Color Border      => PremiumLivingOPS.Views.Shared.Palette.BorderColor;
            // Shared.Palette.TextMain     → local alias "TextPrimary"
            public static Color TextPrimary => PremiumLivingOPS.Views.Shared.Palette.TextMain;
        }

        // ────────────────────────────────────────────────────────────────────────
        //  QuoteCsv — wraps a CSV field in double-quotes and escapes inner quotes.
        //  Called by ExportGrid (defined in ViewReportForm.cs).
        // ────────────────────────────────────────────────────────────────────────
        private static string QuoteCsv(string value)
            => $"\"{value.Replace("\"", "\"\"")}\"";

        // ────────────────────────────────────────────────────────────────────────
        //  AppShell event handlers
        //
        //  FormNavigator.NavigateTo(Form current, string menuLabel, string subItem="")
        //  Logout route: pass "Logout" → FormNavigator calls Application.Restart().
        // ────────────────────────────────────────────────────────────────────────
        private void OnTopNavMenuItemClicked(object sender, string menuTag)
        {
            FormNavigator.NavigateTo(this, menuTag);
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            FormNavigator.NavigateTo(this, "Logout");
        }
    }
}
