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
    //  Resolves all CS0103 "name does not exist in current context" errors that
    //  arise because the following members are referenced in ViewReportForm.cs
    //  and ViewReportForm.Designer.cs but were never declared:
    //
    //    • Palette            (static colour constants — BgPage, Primary, etc.)
    //    • PaintCardBorder    (Paint event handler — white card with shadow border)
    //    • DgvCellFormatting  (CellFormatting handler — status badge colouring)
    //    • ExportGrid         (CSV export helper)
    //    • OnTopNavMenuItemClicked  (AppShell MenuItemClicked delegate)
    //    • btnLogout_Click          (AppShell LogoutClicked delegate)
    // ════════════════════════════════════════════════════════════════════════════

    partial class ViewReportForm
    {
        // ────────────────────────────────────────────────────────────────────────
        //  Palette — design-token colours used by Designer.cs and ViewReportForm.cs
        //  (mirrors the same Palette class used across the rest of the Views layer)
        // ────────────────────────────────────────────────────────────────────────
        internal static class Palette
        {
            /// <summary>Page background — matches the grey shell background.</summary>
            public static readonly Color BgPage = Color.FromArgb(243, 246, 250);

            /// <summary>Primary accent (indigo) used for active tab underline, etc.</summary>
            public static readonly Color Primary = Color.FromArgb(55, 48, 163);

            /// <summary>Surface white used for card backgrounds.</summary>
            public static readonly Color Surface = Color.White;

            /// <summary>Border colour for cards.</summary>
            public static readonly Color Border = Color.FromArgb(221, 227, 236);

            /// <summary>Muted text (labels, secondary info).</summary>
            public static readonly Color TextMuted = Color.FromArgb(98, 112, 135);

            /// <summary>Primary text.</summary>
            public static readonly Color TextPrimary = Color.FromArgb(15, 31, 53);
        }

        // ────────────────────────────────────────────────────────────────────────
        //  PaintCardBorder
        //  Draws a 1 px rounded-corner border + subtle drop-shadow around any Panel
        //  that registers this as its Paint handler.
        // ────────────────────────────────────────────────────────────────────────
        private void PaintCardBorder(object sender, PaintEventArgs e)
        {
            if (sender is not Panel pnl) return;

            var g   = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);

            // Subtle shadow (2 px inset so it stays inside the panel boundary)
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
        //  Also right-aligns numeric columns (REVENUE, AMOUNT, TOTAL AMOUNT, etc.)
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
        // ────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Handles navigation menu item clicks forwarded from AppShell.
        /// Opens the target form in the same application shell pattern used by
        /// all other forms in the project.
        /// </summary>
        private void OnTopNavMenuItemClicked(object sender, string menuTag)
        {
            // Use the central navigation helper (same pattern as ViewOrderForm, etc.)
            FormNavigator.NavigateTo(menuTag, this);
        }

        /// <summary>
        /// Handles the Logout button click forwarded from AppShell.
        /// Clears the session and returns to the login screen.
        /// </summary>
        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Logout();
            FormNavigator.GoToLogin(this);
        }
    }
}
