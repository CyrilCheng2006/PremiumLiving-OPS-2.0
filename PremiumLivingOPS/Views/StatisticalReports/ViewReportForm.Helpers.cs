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
    //  Contains:
    //    • Palette         — local alias mapping to Shared.Palette property names
    //    • PaintCardBorder — 1 px border + inset shadow for any Panel
    //    • DgvCellFormatting — status badge colours, numeric alignment
    //    • ExportGrid      — export all visible rows to a UTF-8 CSV file
    //    • QuoteCsv        — CSV escaping helper
    //    • AppShell event handlers
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
        //  PaintCardBorder
        //  Draws a 1 px border + subtle inset shadow around any Panel.
        // ────────────────────────────────────────────────────────────────────────
        private void PaintCardBorder(object sender, PaintEventArgs e)
        {
            if (sender is not Panel pnl) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);

            using var shadowPen = new Pen(Color.FromArgb(18, 0, 0, 0), 2f);
            g.DrawRectangle(shadowPen,
                new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 1, rect.Height - 1));

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
            if ((col.Name == "colStatus" ||
                 col.Name == "colCStatus" ||
                 col.Name == "colRStatus" ||
                 col.Name == "colPoStatus" ||
                 col.Name == "colRtStatus") &&
                e.Value is string status)
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
            if (col.Name is "colRevenue" or "colAmt" or "colRefund"
                         or "colStock"   or "colReorder" or "colTotal")
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

                for (int c = 0; c < dgv.Columns.Count; c++)
                {
                    if (c > 0) sb.Append(',');
                    sb.Append(QuoteCsv(dgv.Columns[c].HeaderText));
                }
                sb.AppendLine();

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
        //  AppShell.MenuItemClicked is Action<string, string> — (menu, sub).
        //  Both arguments must be forwarded to FormNavigator so the correct
        //  target Form is resolved instead of falling back to "Coming Soon".
        // ────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Handles TopNavBar menu clicks forwarded by AppShell.
        /// Signature matches Action&lt;string, string&gt; (menu, sub).
        /// </summary>
        private void OnTopNavMenuItemClicked(string menu, string sub)
        {
            FormNavigator.NavigateTo(this, menu, sub);
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            FormNavigator.NavigateTo(this, "Logout");
        }
    }
}
