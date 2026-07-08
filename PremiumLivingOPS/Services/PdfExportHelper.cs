using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace PremiumLivingOPS.Services
{
    /// <summary>
    /// Lightweight PDF export helper using only .NET built-in APIs (GDI+).
    /// Output: A4 Landscape (297mm × 210mm @ 96 DPI = 1123 × 794 px).
    ///
    /// Covers:
    ///   - ExportDeliveryNote  (Delivery Note window)
    ///   - ExportReplySlip     (Reply Slip window)
    /// </summary>
    public static class PdfExportHelper
    {
        // A4 Landscape @ 96 DPI
        private const int PageW = 1123;  // 297mm wide
        private const int PageH = 794;   // 210mm tall
        private const int Margin = 48;

        // ─────────────────────────────────────────────────────────────
        // Public entry points
        // ─────────────────────────────────────────────────────────────

        public static void ExportDeliveryNote(
            string filePath,
            ShipmentEntity s,
            List<ShipmentLineEntity> lines,
            int outQty)
        {
            using var bmp = new Bitmap(PageW, PageH);
            bmp.SetResolution(96, 96);
            using (var g = Graphics.FromImage(bmp))
                DrawDeliveryNote(g, s, lines, outQty);
            SaveBitmapAsPdf(bmp, filePath, $"DeliveryNote_{s.ShipmentID}");
        }

        public static void ExportReplySlip(
            string filePath,
            ShipmentEntity s,
            List<ShipmentLineEntity> lines)
        {
            using var bmp = new Bitmap(PageW, PageH);
            bmp.SetResolution(96, 96);
            using (var g = Graphics.FromImage(bmp))
                DrawReplySlip(g, s, lines);
            SaveBitmapAsPdf(bmp, filePath, $"ReplySlip_{s.ShipmentID}");
        }

        // ─────────────────────────────────────────────────────────────
        // Drawing — Delivery Note
        // ─────────────────────────────────────────────────────────────

        private static void DrawDeliveryNote(
            Graphics g,
            ShipmentEntity s,
            List<ShipmentLineEntity> lines,
            int outQty)
        {
            g.Clear(Color.White);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            int x = Margin, y = Margin;
            int cw = PageW - Margin * 2;

            // ── Company header band
            using (var hBrush = new SolidBrush(Color.FromArgb(19, 35, 61)))
                g.FillRectangle(hBrush, x, y, cw, 52);
            using var fHead = new Font("Segoe UI", 14f, FontStyle.Bold);
            g.DrawString("PREMIUM LIVING OPS", fHead, Brushes.White,
                new RectangleF(x + 14, y, cw - 14, 52),
                new StringFormat { LineAlignment = StringAlignment.Center });
            using var fSub = new Font("Segoe UI", 10f);
            g.DrawString("DELIVERY NOTE", fSub,
                new SolidBrush(Color.FromArgb(209, 250, 229)),
                new RectangleF(x + 14, y, cw - 14, 52),
                new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center });
            y += 62;

            // ── Info grid — 3 rows × 2 columns (wider layout for landscape)
            DrawInfoPair(g, x, ref y, cw, "Shipment ID",     s.ShipmentID,                          "Order ID",        s.OrderID);
            DrawInfoPair(g, x, ref y, cw, "Customer",        s.CustomerName,                        "Tracking No.",    s.TrackingNumber ?? "—");
            DrawInfoPair(g, x, ref y, cw, "Ship Date",       s.ShipDate.ToString("yyyy-MM-dd"),     "Delivery Method", s.DeliveryMethod);
            DrawInfoPair(g, x, ref y, cw, "Shipment Status", s.ShipmentStatus,                      "Ship Type",       s.ShipmentType);
            DrawInfoPair(g, x, ref y, cw, "Ship Address",    s.ShippingAddress,                     "Outstanding Qty", outQty.ToString());
            DrawInfoPair(g, x, ref y, cw, "Delivery Date",   DateTime.Today.ToString("yyyy-MM-dd"), "Ship To",         s.CustomerName);

            y += 8;
            DrawSectionBar(g, x, ref y, cw, "SHIPMENT ITEMS",
                Color.FromArgb(240, 253, 244), Color.FromArgb(6, 95, 70));

            // ── Items table
            string[] headers = { "Line ID", "Item ID", "Item Name", "Qty Shipped", "Outstanding" };
            float[]  weights = { 0.12f, 0.11f, 0.42f, 0.18f, 0.17f };
            DrawTableHeader(g, x, ref y, cw, headers, weights);
            foreach (var ln in lines)
            {
                string[] vals = {
                    ln.ShipmentLineID,
                    ln.ItemID,
                    ln.ItemName,
                    ln.QtyShipped.ToString(),
                    ln.QtyOutstanding?.ToString() ?? "—"
                };
                DrawTableRow(g, x, ref y, cw, vals, weights);
            }

            // ── Total footer
            y += 14;
            DrawTotalLine(g, x, y, cw,
                $"Lines: {lines.Count}",
                $"Total Amount:  HK$ {s.TotalAmount:N2}");

            // ── Footer watermark
            DrawFooter(g, x, PageH - Margin, cw, "PremiumLiving Operations System — Confidential");
        }

        // ─────────────────────────────────────────────────────────────
        // Drawing — Reply Slip
        // ─────────────────────────────────────────────────────────────

        private static void DrawReplySlip(
            Graphics g,
            ShipmentEntity s,
            List<ShipmentLineEntity> lines)
        {
            g.Clear(Color.White);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            int x = Margin, y = Margin;
            int cw = PageW - Margin * 2;

            // ── Header band (purple-navy for Reply Slip)
            using (var hBrush = new SolidBrush(Color.FromArgb(30, 27, 75)))
                g.FillRectangle(hBrush, x, y, cw, 52);
            using var fHead = new Font("Segoe UI", 14f, FontStyle.Bold);
            g.DrawString("PREMIUM LIVING OPS", fHead, Brushes.White,
                new RectangleF(x + 14, y, cw - 14, 52),
                new StringFormat { LineAlignment = StringAlignment.Center });
            using var fSub = new Font("Segoe UI", 10f);
            g.DrawString("REPLY SLIP", fSub,
                new SolidBrush(Color.FromArgb(216, 180, 254)),
                new RectangleF(x + 14, y, cw - 14, 52),
                new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center });
            y += 62;

            // ── Info grid
            DrawInfoPair(g, x, ref y, cw, "Shipment ID",  s.ShipmentID,                      "Order ID",        s.OrderID);
            DrawInfoPair(g, x, ref y, cw, "Customer",     s.CustomerName,                    "Tracking No.",    s.TrackingNumber ?? "—");
            DrawInfoPair(g, x, ref y, cw, "Ship Date",    s.ShipDate.ToString("yyyy-MM-dd"), "Delivery Method", s.DeliveryMethod);
            DrawInfoPair(g, x, ref y, cw, "Ship Address", s.ShippingAddress,                 "Ship Type",       s.ShipmentType);

            // ── Reply section
            y += 8;
            DrawSectionBar(g, x, ref y, cw, "RECEIPT CONFIRMATION",
                Color.FromArgb(238, 242, 255), Color.FromArgb(67, 56, 202));

            DrawInfoPair(g, x, ref y, cw, "Received By",  "______________________", "Date Received", "______________________");
            DrawInfoPair(g, x, ref y, cw, "Signature",    "______________________", "Contact No.",   "______________________");
            DrawInfoPair(g, x, ref y, cw, "Remarks",
                "___________________________________________________",
                "", "");

            y += 8;
            DrawSectionBar(g, x, ref y, cw, "ITEMS RECEIVED",
                Color.FromArgb(238, 242, 255), Color.FromArgb(67, 56, 202));

            // ── Items table with Condition column
            string[] headers = { "Line ID", "Item Name", "Qty Shipped", "Qty Received", "Condition" };
            float[]  weights = { 0.13f, 0.40f, 0.16f, 0.15f, 0.16f };
            DrawTableHeader(g, x, ref y, cw, headers, weights);
            foreach (var ln in lines)
            {
                string[] vals = {
                    ln.ShipmentLineID,
                    ln.ItemName,
                    ln.QtyShipped.ToString(),
                    "______",
                    "______"
                };
                DrawTableRow(g, x, ref y, cw, vals, weights);
            }

            DrawFooter(g, x, PageH - Margin, cw,
                "Please return this slip to PremiumLiving Logistics upon delivery. PremiumLiving Operations System.");
        }

        // ─────────────────────────────────────────────────────────────
        // Drawing primitives
        // ─────────────────────────────────────────────────────────────

        private static void DrawInfoPair(
            Graphics g, int x, ref int y, int cw,
            string keyL, string valL, string keyR, string valR,
            int rowH = 28)
        {
            using var fKey = new Font("Segoe UI", 8f, FontStyle.Bold);
            using var fVal = new Font("Segoe UI", 9f);
            var clrKey = Color.FromArgb(100, 116, 139);
            var clrVal = Color.FromArgb(15, 31, 53);
            int half = cw / 2;

            g.DrawString(keyL, fKey, new SolidBrush(clrKey), x,          y + 2);
            g.DrawString(valL, fVal, new SolidBrush(clrVal), x + 110,    y + 2);
            if (!string.IsNullOrEmpty(keyR))
            {
                g.DrawString(keyR, fKey, new SolidBrush(clrKey), x + half,      y + 2);
                g.DrawString(valR, fVal, new SolidBrush(clrVal), x + half + 110, y + 2);
            }

            y += rowH;
            using var pen = new Pen(Color.FromArgb(226, 232, 240), 0.5f);
            g.DrawLine(pen, x, y - 2, x + cw, y - 2);
        }

        private static void DrawSectionBar(
            Graphics g, int x, ref int y, int cw,
            string title, Color bgColor, Color fgColor,
            int h = 26)
        {
            using (var br = new SolidBrush(bgColor))
                g.FillRectangle(br, x, y, cw, h);
            using var f = new Font("Segoe UI", 9f, FontStyle.Bold);
            g.DrawString(title, f, new SolidBrush(fgColor),
                new RectangleF(x + 10, y, cw - 10, h),
                new StringFormat { LineAlignment = StringAlignment.Center });
            y += h + 4;
        }

        private static void DrawTableHeader(
            Graphics g, int x, ref int y, int cw,
            string[] headers, float[] weights,
            int rowH = 26)
        {
            using (var br = new SolidBrush(Color.FromArgb(246, 249, 255)))
                g.FillRectangle(br, x, y, cw, rowH);
            using var f = new Font("Segoe UI", 8f, FontStyle.Bold);
            var clr = new SolidBrush(Color.FromArgb(71, 85, 105));
            int cx = x + 6;
            for (int i = 0; i < headers.Length; i++)
            {
                int colW = (int)(cw * weights[i]);
                g.DrawString(headers[i], f, clr, cx, y + 6);
                cx += colW;
            }
            y += rowH;
            using var pen = new Pen(Color.FromArgb(203, 213, 225), 0.5f);
            g.DrawLine(pen, x, y, x + cw, y);
        }

        private static void DrawTableRow(
            Graphics g, int x, ref int y, int cw,
            string[] vals, float[] weights,
            int rowH = 24)
        {
            using var f = new Font("Segoe UI", 8.5f);
            var clr = new SolidBrush(Color.FromArgb(30, 41, 59));
            int cx = x + 6;
            for (int i = 0; i < vals.Length; i++)
            {
                int colW = (int)(cw * weights[i]);
                var rect = new RectangleF(cx, y + 4, colW - 4, rowH);
                g.DrawString(vals[i] ?? "—", f, clr, rect,
                    new StringFormat { Trimming = StringTrimming.EllipsisCharacter });
                cx += colW;
            }
            y += rowH;
            using var pen = new Pen(Color.FromArgb(226, 232, 240), 0.5f);
            g.DrawLine(pen, x, y, x + cw, y);
        }

        private static void DrawTotalLine(
            Graphics g, int x, int y, int cw,
            string leftText, string rightText)
        {
            using var f = new Font("Segoe UI", 10f, FontStyle.Bold);
            g.DrawString(leftText,  f, new SolidBrush(Color.FromArgb(15, 31, 53)),
                new RectangleF(x, y, cw / 2f, 28f),
                new StringFormat { LineAlignment = StringAlignment.Center });
            g.DrawString(rightText, f, new SolidBrush(Color.FromArgb(47, 111, 237)),
                new RectangleF(x + cw / 2f, y, cw / 2f, 28f),
                new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center });
        }

        private static void DrawFooter(
            Graphics g, int x, int y, int cw, string text)
        {
            using var pen = new Pen(Color.FromArgb(203, 213, 225), 0.5f);
            g.DrawLine(pen, x, y - 12, x + cw, y - 12);
            using var f = new Font("Segoe UI", 7.5f);
            g.DrawString(text, f, new SolidBrush(Color.FromArgb(148, 163, 184)),
                new RectangleF(x, y - 10, cw, 16),
                new StringFormat { Alignment = StringAlignment.Center });
        }

        // ─────────────────────────────────────────────────────────────
        // PDF binary writer (PDF 1.4, single-page, bitmap image)
        // ─────────────────────────────────────────────────────────────

        private static void SaveBitmapAsPdf(Bitmap bmp, string filePath, string title)
        {
            // Convert bitmap to JPEG bytes
            byte[] imgBytes;
            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                imgBytes = ms.ToArray();
            }

            // PDF dimensions: convert 96dpi pixels to 72dpi points
            // Width > Height confirms landscape orientation in PDF viewer
            double pdfW = bmp.Width  * 72.0 / 96.0;   // ~842pt (A4 landscape width)
            double pdfH = bmp.Height * 72.0 / 96.0;   // ~595pt (A4 landscape height)

            var sb   = new StringBuilder();
            var xref = new List<int>();

            sb.Append("%PDF-1.4\n");
            sb.Append("%\u00e2\u00e3\u00cf\u00d3\n");

            // Obj 1 — Catalog
            xref.Add(sb.Length);
            sb.Append("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

            // Obj 2 — Pages
            xref.Add(sb.Length);
            sb.Append("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");

            // Obj 3 — Page (MediaBox width > height = landscape)
            xref.Add(sb.Length);
            sb.Append($"3 0 obj\n<< /Type /Page /Parent 2 0 R "
                    + $"/MediaBox [0 0 {pdfW:F2} {pdfH:F2}] "
                    + $"/Contents 4 0 R /Resources << /XObject << /Im1 5 0 R >> >> >>\nendobj\n");

            // Obj 4 — Content stream
            string contentStr = $"q {pdfW:F2} 0 0 {pdfH:F2} 0 0 cm /Im1 Do Q\n";
            byte[] contentBytes = Encoding.ASCII.GetBytes(contentStr);
            xref.Add(sb.Length);
            sb.Append($"4 0 obj\n<< /Length {contentBytes.Length} >>\nstream\n");
            sb.Append(contentStr);
            sb.Append("endstream\nendobj\n");

            // Obj 5 — Image XObject (JPEG)
            xref.Add(sb.Length);
            string imgHeader = $"5 0 obj\n<< /Type /XObject /Subtype /Image "
                             + $"/Width {bmp.Width} /Height {bmp.Height} "
                             + $"/ColorSpace /DeviceRGB /BitsPerComponent 8 "
                             + $"/Filter /DCTDecode /Length {imgBytes.Length} >>\nstream\n";

            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            using var bw = new BinaryWriter(fs, Encoding.ASCII);

            byte[] prefix = Encoding.ASCII.GetBytes(sb.ToString());
            bw.Write(prefix);

            xref[4] = (int)fs.Position;
            byte[] imgHeaderBytes = Encoding.ASCII.GetBytes(imgHeader);
            bw.Write(imgHeaderBytes);
            bw.Write(imgBytes);
            byte[] imgTrailer = Encoding.ASCII.GetBytes("\nendstream\nendobj\n");
            bw.Write(imgTrailer);

            int xrefOffset = (int)fs.Position;
            bw.Write(Encoding.ASCII.GetBytes(
                $"xref\n0 6\n0000000000 65535 f \n"));
            foreach (int offset in xref)
                bw.Write(Encoding.ASCII.GetBytes($"{offset:D10} 00000 n \n"));

            bw.Write(Encoding.ASCII.GetBytes(
                $"trailer\n<< /Size 6 /Root 1 0 R >>\n"
              + $"startxref\n{xrefOffset}\n%%EOF\n"));
        }
    }
}
