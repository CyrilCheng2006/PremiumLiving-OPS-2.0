using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.StatisticalReports
{
    // ════════════════════════════════════════════════════════════════════════════
    //  ViewReportForm — Helpers partial
    //
    //  Resolves CS0103 / CS0117 / CS1503 errors.
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
        //  PaintCardBorder
        //  Draws a 1 px border + subtle inset shadow around any Panel.
        // ────────────────────────────────────────────────────────────────────────
        private void PaintCardBorder(object sender, PaintEventArgs e)
        {
            if (sender is not Panel pnl) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);

            // Subtle inset shadow
            using var shadowPen = new Pen(Color.FromArgb(18, 0, 0, 0), 2f);
            g.DrawRectangle(shadowPen,
                new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 1, rect.Height - 1));

            // Main 1 px border — uses the "Border" alias above
            using var borderPen = new Pen(Palette.Border, 1f);
            g.DrawRectangle(borderPen, rect);
        }

        // ────────────────────────────────────────────────────────────────────────
        //  DgvCellFormatting
        //  Applies status badge colours, right-aligns numeric cols,
        //  centre-aligns tick columns (DN / RS).
        // ────────────────────────────────────────────────────────────────────────
        private void DgvCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (sender is not DataGridView dgv) return;
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var col = dgv.Columns[e.ColumnIndex];

            // ── Status badge colouring ──
            if (col.Name == "colStatus" && e.Value is string status)
            {
                if (StatusColors.TryGetValue(status, out var colors))
                {
                    e.CellStyle.BackColor          = colors.bg;
                    e.CellStyle.ForeColor          = colors.fg;
                    e.CellStyle.SelectionBackColor = colors.bg;
                    e.CellStyle.SelectionForeColor = colors.fg;
                    e.CellStyle.Font               = new Font("Segoe UI", 11f, FontStyle.Bold);
                    e.CellStyle.Alignment          = DataGridViewContentAlignment.MiddleCenter;
                    e.FormattingApplied            = true;
                }
                return;
            }

            // ── Right-align numeric columns ──
            if (col.Name is "colRevenue" or "colAmt" or "colRefund" or "colStock" or "colReorder")
            {
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                e.FormattingApplied   = true;
            }

            // ── Centre-align tick columns (DN / RS) ──
            if (col.Name is "colHasDN" or "colHasRS")
            {
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                e.FormattingApplied   = true;
            }
        }

        // ────────────────────────────────────────────────────────────────────────
        //  ExportGrid — exports all visible rows to a UTF-8 CSV file.
        // ────────────────────────────────────────────────────────────────────────
        private static void ExportGrid(DataGridView dgv, string defaultName)
        {
            using var dlg = new SaveFileDialog
            {
                Title            = "Export to CSV",
                Filter           = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                FileName         = $"{defaultName}_{DateTime.Today:yyyyMMdd}.csv",
                DefaultExt       = "csv",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                var sb = new StringBuilder();

                // Header row
                for (int c = 0; c < dgv.Columns.Count; c++)
                {
                    if (c > 0) sb.Append(',');
                    sb.Append(QuoteCsv(dgv.Columns[c].HeaderText));
                }
                sb.AppendLine();

                // Data rows
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    if (row.IsNewRow) continue;
                    for (int c = 0; c < dgv.Columns.Count; c++)
                    {
                        if (c > 0) sb.Append(',');
                        sb.Append(QuoteCsv(row.Cells[c].FormattedValue?.ToString() ?? string.Empty));
                    }
                    sb.AppendLine();
                }

                File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show(
                    $"Exported {dgv.Rows.Count} row(s) to:\n{dlg.FileName}",
                    "Export Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Export failed:\n{ex.Message}",
                    "Export Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>Wraps a CSV field in double-quotes and escapes inner quotes.</summary>
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
