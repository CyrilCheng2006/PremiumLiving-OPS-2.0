using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.StatisticalReports
{
    // ════════════════════════════════════════════════════════════════════════════
    //  ViewReportForm — Helpers partial
    //
    //  Contains:
    //    • Palette               — local alias mapping to Shared.Palette property names
    //    • QuoteCsv              — CSV escaping helper (used by ExportGrid in ViewReportForm.cs)
    //    • OnTopNavMenuItemClicked — AppShell top-nav handler
    //
    //  NOTE: PaintCardBorder, DgvCellFormatting, ExportGrid and btnLogout_Click
    //        are defined in ViewReportForm.cs — do NOT redeclare here (CS0111).
    // ════════════════════════════════════════════════════════════════════════════

    partial class ViewReportForm
    {
        // ────────────────────────────────────────────────────────────────────────
        //  Palette — aliases mapped to actual Shared.Palette property names.
        // ────────────────────────────────────────────────────────────────────────
        internal static class Palette
        {
            public static Color BgPage      => PremiumLivingOPS.Views.Shared.Palette.BgPage;
            public static Color Primary     => PremiumLivingOPS.Views.Shared.Palette.Primary;
            public static Color TextMuted   => PremiumLivingOPS.Views.Shared.Palette.TextMuted;
            public static Color Surface     => PremiumLivingOPS.Views.Shared.Palette.BgCard;
            public static Color Border      => PremiumLivingOPS.Views.Shared.Palette.BorderColor;
            public static Color TextPrimary => PremiumLivingOPS.Views.Shared.Palette.TextMain;
        }

        // ────────────────────────────────────────────────────────────────────────
        //  QuoteCsv — wraps a CSV field in double-quotes and escapes inner quotes.
        //  Used by ExportGrid (defined in ViewReportForm.cs).
        // ────────────────────────────────────────────────────────────────────────
        private static string QuoteCsv(string value)
            => $"\"{value.Replace("\"", "\"\"")}\"";

        // ────────────────────────────────────────────────────────────────────────
        //  AppShell event handlers
        // ────────────────────────────────────────────────────────────────────────
        private void OnTopNavMenuItemClicked(object sender, string menuTag)
        {
            FormNavigator.NavigateTo(this, menuTag);
        }
    }
}
