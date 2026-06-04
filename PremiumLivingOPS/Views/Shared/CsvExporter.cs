using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.Shared
{
    // ================================================================
    //  FILE: Views/Shared/CsvExporter.cs
    //
    //  Generic CSV export utility for DataGridView controls.
    //  MVC role: View-layer helper (presentation / IO only).
    //
    //  Usage:
    //    CsvExporter.Export(myDataGridView, "SalesReport");
    //    CsvExporter.Export(myDataGridView, "TopProducts");
    //
    //  Writes UTF-8 with BOM so Excel opens it correctly without
    //  needing an import wizard.
    // ================================================================

    public static class CsvExporter
    {
        /// <summary>
        /// Opens a SaveFileDialog and writes all rows of <paramref name="grid"/>
        /// to a CSV file.  Handles commas, double-quotes and newlines in cell
        /// values per RFC 4180.
        /// </summary>
        /// <param name="grid">The DataGridView whose visible data to export.</param>
        /// <param name="defaultFileName">Suggested file name (without extension).</param>
        public static void Export(DataGridView grid, string defaultFileName = "Report")
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));

            using var dlg = new SaveFileDialog
            {
                Title            = "Export to CSV",
                Filter           = "CSV File (*.csv)|*.csv|All Files (*.*)|*.*",
                DefaultExt       = "csv",
                FileName         = $"{defaultFileName}_{DateTime.Now:yyyyMMdd_HHmmss}",
                OverwritePrompt  = true,
            };

            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                // UTF-8 with BOM — required for Excel to auto-detect encoding
                using var writer = new StreamWriter(dlg.FileName, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

                // ── Header row ──────────────────────────────────────────────
                bool firstCol = true;
                foreach (DataGridViewColumn col in grid.Columns)
                {
                    if (!col.Visible) continue;
                    if (!firstCol) writer.Write(',');
                    writer.Write(Escape(col.HeaderText));
                    firstCol = false;
                }
                writer.WriteLine();

                // ── Data rows ───────────────────────────────────────────────
                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.IsNewRow) continue;
                    firstCol = true;
                    foreach (DataGridViewColumn col in grid.Columns)
                    {
                        if (!col.Visible) continue;
                        if (!firstCol) writer.Write(',');
                        var cell = row.Cells[col.Index];
                        writer.Write(Escape(cell.FormattedValue?.ToString() ?? string.Empty));
                        firstCol = false;
                    }
                    writer.WriteLine();
                }

                MessageBox.Show(
                    $"Exported successfully to:\n{dlg.FileName}",
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

        // ── RFC 4180 cell escaping ────────────────────────────────────────
        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            // Must quote if the value contains comma, double-quote, or newline
            bool needsQuoting = value.IndexOf(',')  >= 0
                             || value.IndexOf('"')  >= 0
                             || value.IndexOf('\n') >= 0
                             || value.IndexOf('\r') >= 0;

            if (!needsQuoting) return value;

            // Double any embedded double-quotes, then wrap in quotes
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
