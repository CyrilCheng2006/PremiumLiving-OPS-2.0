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
    //  Resolves all CS0103 / CS0117 / CS1503 errors that arose because the
    //  following members were referenced in ViewReportForm.cs and
    //  ViewReportForm.Designer.cs but were never declared:
    //
    //    • Palette                  (re-exports PremiumLivingOPS.Views.Shared.Palette)
    //    • PaintCardBorder          (Paint handler — white card with border)
    //    • DgvCellFormatting        (CellFormatting handler — status badge colour)
    //    • ExportGrid               (CSV export helper)
    //    • OnTopNavMenuItemClicked  (AppShell.MenuItemClicked delegate)
    //    • btnLogout_Click          (AppShell.LogoutClicked delegate)
    //
    //  Fix notes for the three new errors:
    //
    //    CS1503  FormNavigator.NavigateTo signature is (Form current, string menu, string sub)
    //            — first arg is the CURRENT form, not a string.
    //    CS0103  SessionManager does not exist; logout is handled entirely by
    //            FormNavigator.NavigateTo(this, "Logout") → Application.Restart().
    //    CS0117  FormNavigator.GoToLogin does not exist; same reason as above.
    // ════════════════════════════════════════════════════════════════════════════

    partial class ViewReportForm
    {
        // ────────────────────────────────────────────────────────────────────────
        //  Palette — thin wrapper so ViewReportForm.cs can reference "Palette.BgPage"
        //  without a fully-qualified name.  All values are forwarded directly from
        //  PremiumLivingOPS.Views.Shared.Palette which is the authoritative source.
        // ────────────────────────────────────────────────────────────────────────
        internal static class Palette
        {
            public static Color BgPage      => PremiumLivingOPS.Views.Shared.Palette.BgPage;
            public static Color Primary     => PremiumLivingOPS.Views.Shared.Palette.Primary;
            public static Color Surface     => PremiumLivingOPS.Views.Shared.Palette.Surface;
            public static Color Border      => PremiumLivingOPS.Views.Shared.Palette.Border;
            public static Color TextMuted   => PremiumLivingOPS.Views.Shared.Palette.TextMuted;
            public static Color TextPrimary => PremiumLivingOPS.Views.Shared.Palette.TextPrimary;
        }

        // ────────────────────────────────────────────────────────────────────────
        //  PaintCardBorder
        //  Draws a 1 px border + subtle inset shadow around any Panel
        //  that registers this as its Paint handler.
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

            // Main 1 px border
            using var borderPen = new Pen(Palette.Border, 1f);
            g.DrawRectangle(borderPen, rect);
        }

        // ────────────────────────────────────────────────────────────────────────
        //  DgvCellFormatting
        //  Colours the STATUS column cell to match the badge palette defined in
        //  ViewReportForm.cs → StatusColors dictionary.
        //  Also right-aligns numeric columns and centres tick columns.
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
        //  ExportGrid
        //  Exports all visible rows of a DataGridView to a CSV file chosen via
        //  SaveFileDialog.  Invoked by every tab's "Export" button.
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
        //  Wired in ViewReportForm_Load (ViewReportForm.cs line 86/87).
        //
        //  Correct FormNavigator API (from Views/Shared/FormNavigator.cs):
        //    NavigateTo(Form current, string menuLabel, string subItem = "")
        //
        //  Logout is handled by passing "Logout" as menuLabel — FormNavigator
        //  calls Application.Restart() internally.  No SessionManager exists.
        // ────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Handles navigation menu item clicks forwarded from AppShell.
        /// Signature matches AppShell.MenuItemClicked: (object sender, string menuTag).
        /// Calls FormNavigator.NavigateTo(Form current, string menuLabel).
        /// </summary>
        private void OnTopNavMenuItemClicked(object sender, string menuTag)
        {
            // Correct call: first arg = this (current Form), second = menu label.
            FormNavigator.NavigateTo(this, menuTag);
        }

        /// <summary>
        /// Handles the Logout button click forwarded from AppShell.
        /// FormNavigator treats "Logout" as a special route and calls
        /// Application.Restart() — no SessionManager required.
        /// </summary>
        private void btnLogout_Click(object sender, EventArgs e)
        {
            // "Logout" is the reserved route tag; FormNavigator calls Application.Restart().
            FormNavigator.NavigateTo(this, "Logout");
        }
    }
}
