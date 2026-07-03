using PremiumLivingOPS.Models.ViewModels;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Services
{
    /// <summary>
    /// Generates PDF documents for Delivery Notes and Reply Slips.
    /// Uses PdfSharp 6.x (MIT licence).
    /// Fonts are resolved via WindowsFontResolver (reads TTF from %WINDIR%\Fonts).
    /// </summary>
    public static class PdfExporter
    {
        // ── Page geometry (A4 landscape)
        private const double PageW    = 841.89;
        private const double PageH    = 595.28;
        private const double Margin   = 40.0;
        private const double ContentW = PageW - Margin * 2;

        // ── Colours
        private static readonly XColor NavyBg   = XColor.FromArgb(19,  35,  61);
        private static readonly XColor GreenFg  = XColor.FromArgb( 6,  95,  70);
        private static readonly XColor GreenBg  = XColor.FromArgb(240, 253, 244);
        private static readonly XColor BlueFg   = XColor.FromArgb(47, 111, 237);
        private static readonly XColor LabelFg  = XColor.FromArgb(98, 112, 135);
        private static readonly XColor BodyFg   = XColor.FromArgb(15,  31,  53);
        private static readonly XColor BorderCl = XColor.FromArgb(221, 227, 236);
        private static readonly XColor White    = XColors.White;

        // ── String formats
        private static readonly XStringFormat FmtCenterLeft  = new XStringFormat { Alignment = XStringAlignment.Near,   LineAlignment = XLineAlignment.Center };
        private static readonly XStringFormat FmtCenter      = new XStringFormat { Alignment = XStringAlignment.Center, LineAlignment = XLineAlignment.Center };
        private static readonly XStringFormat FmtCenterRight = new XStringFormat { Alignment = XStringAlignment.Far,    LineAlignment = XLineAlignment.Center };
        private static readonly XStringFormat FmtBottomLeft  = new XStringFormat { Alignment = XStringAlignment.Near,   LineAlignment = XLineAlignment.Far    };
        private static readonly XStringFormat FmtTopLeft     = new XStringFormat { Alignment = XStringAlignment.Near,   LineAlignment = XLineAlignment.Near   };

        // ── Fonts (expression-body properties: new instance created AFTER FontResolver is set)
        private static XFont FontTitle  => new XFont("Arial", 18, XFontStyleEx.Bold);
        private static XFont FontSub    => new XFont("Arial", 12, XFontStyleEx.Bold);
        private static XFont FontBody   => new XFont("Arial", 10, XFontStyleEx.Regular);
        private static XFont FontBold   => new XFont("Arial", 10, XFontStyleEx.Bold);
        private static XFont FontSmall  => new XFont("Arial",  8, XFontStyleEx.Regular);
        private static XFont FontHeader => new XFont("Arial",  9, XFontStyleEx.Bold);

        // ── Address wrapping config
        private static readonly HashSet<string> WrapKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Ship Address:", "Ship Address"
        };
        private const double RowH     = 16;
        private const double AddrRowH = RowH * 2;   // 32 px – room for two lines

        // ══ Public export entry-points ══════════════════════════════════════════

        public static void ExportDeliveryNote(ShipmentDetailVM s, string filePath)
        {
            WindowsFontResolver.EnsureRegistered();

            var ship = s.Shipment;
            var dn   = s.DeliveryNote
                ?? throw new InvalidOperationException("No Delivery Note on this shipment.");

            int outQty = 0;
            foreach (var ln in s.Lines ?? new List<PremiumLivingOPS.Models.Entities.ShipmentLineEntity>())
                outQty += ln.QtyOutstanding ?? 0;

            using var doc  = new PdfDocument();
            doc.Info.Title = $"Delivery Note {dn.DeliveryID}";
            var page = doc.AddPage();
            page.Width  = PageW;
            page.Height = PageH;
            using var gfx = XGraphics.FromPdfPage(page);

            double y = Margin;
            y = DrawNavyHeader(gfx, y,
                $"Delivery Note  \u2014  {dn.DeliveryID}", ship.ShipmentStatus ?? "");
            y = DrawInfoBlock(gfx, y, new[]
            {
                ("Shipment ID:",     ship.ShipmentID        ?? ""),
                ("Order ID:",        ship.OrderID           ?? ""),
                ("Customer:",        ship.CustomerName      ?? ""),
                ("Ship Date:",       ship.ShipDate.ToString("yyyy-MM-dd")),
                ("Status:",          ship.ShipmentStatus    ?? ""),
                ("Delivery Method:", ship.DeliveryMethod    ?? ""),
                ("Ship Type:",       ship.ShipmentType      ?? ""),
                ("Tracking No.:",    ship.TrackingNumber    ?? "\u2014"),
            });
            y = DrawSectionBadge(gfx, y,
                $"Delivery Note  \u2014  {dn.DeliveryID}   (Date: {dn.DeliveryDate:yyyy-MM-dd})");
            y = DrawInfoBlock(gfx, y, new[]
            {
                ("Delivery Date:",   dn.DeliveryDate.ToString("yyyy-MM-dd")),
                ("Ship To:",         ship.CustomerName     ?? ""),
                ("Ship Address:",    ship.ShippingAddress  ?? "\u2014"),
                ("Outstanding Qty:", outQty.ToString()),
                ("Delivery Method:", ship.DeliveryMethod   ?? ""),
                ("Shipment Type:",   ship.ShipmentType     ?? ""),
            });
            y = DrawItemsTable(gfx, y, s.Lines);
            DrawTotalsFooter(gfx, s.Lines?.Count ?? 0, ship.TotalAmount);
            doc.Save(filePath);
        }

        public static void ExportReplySlip(ShipmentDetailVM s, string filePath)
        {
            WindowsFontResolver.EnsureRegistered();

            var ship = s.Shipment;
            var rs   = s.ReplySlip
                ?? throw new InvalidOperationException("No Reply Slip on this shipment.");

            using var doc  = new PdfDocument();
            doc.Info.Title = $"Reply Slip {rs.SlipID}";
            var page = doc.AddPage();
            page.Width  = PageW;
            page.Height = PageH;
            using var gfx = XGraphics.FromPdfPage(page);

            double y = Margin;
            y = DrawNavyHeader(gfx, y,
                $"Reply Slip  \u2014  {rs.SlipID}", ship.ShipmentStatus ?? "");
            y = DrawInfoBlock(gfx, y, new[]
            {
                ("Shipment ID:",     ship.ShipmentID      ?? ""),
                ("Order ID:",        ship.OrderID         ?? ""),
                ("Customer:",        ship.CustomerName    ?? ""),
                ("Ship Date:",       ship.ShipDate.ToString("yyyy-MM-dd")),
                ("Delivery Note:",   s.DeliveryNote?.DeliveryID ?? "\u2014"),
                ("Delivery Method:", ship.DeliveryMethod  ?? ""),
                ("Ship Type:",       ship.ShipmentType    ?? ""),
                ("Tracking No.:",    ship.TrackingNumber  ?? "\u2014"),
            });
            y = DrawSectionBadge(gfx, y,
                $"Reply Slip  \u2014  {rs.SlipID}   (Received: {rs.ReceivedDate:yyyy-MM-dd})");
            y = DrawInfoBlock(gfx, y, new[]
            {
                ("Actual Recipient:", rs.ActualRecipient   ?? "\u2014"),
                ("Remark:",           rs.RecipientRemark   ?? "\u2014"),
                ("Ship Address:",     ship.ShippingAddress ?? "\u2014"),
                ("Total Amount:",     $"HK$ {ship.TotalAmount:N2}"),
            });
            y = DrawItemsTable(gfx, y, s.Lines);
            DrawTotalsFooter(gfx, s.Lines?.Count ?? 0, ship.TotalAmount);
            doc.Save(filePath);
        }

        // ══ Drawing helpers ════════════════════════════════════════════════════

        private static double DrawNavyHeader(
            XGraphics gfx, double y, string title, string status)
        {
            const double h = 44;
            gfx.DrawRectangle(new XSolidBrush(NavyBg), Margin, y, ContentW, h);
            gfx.DrawString(title, FontTitle, new XSolidBrush(White),
                new XRect(Margin + 12, y, ContentW - 120, h), FmtCenterLeft);
            if (!string.IsNullOrEmpty(status))
                gfx.DrawString(status, FontSub, new XSolidBrush(GreenFg),
                    new XRect(PageW - Margin - 100, y, 88, h), FmtCenter);
            return y + h + 6;
        }

        private static double DrawSectionBadge(
            XGraphics gfx, double y, string text)
        {
            const double h = 22;
            gfx.DrawRectangle(new XSolidBrush(GreenBg), Margin, y, ContentW, h);
            gfx.DrawString(text, FontBold, new XSolidBrush(GreenFg),
                new XRect(Margin + 8, y, ContentW, h), FmtCenterLeft);
            return y + h + 4;
        }

        /// <summary>
        /// Renders a two-column info block.
        /// Fields whose key is in <see cref="WrapKeys"/> get double row height
        /// and their value is word-wrapped across two lines.
        /// </summary>
        private static double DrawInfoBlock(
            XGraphics gfx, double y, (string key, string val)[] fields)
        {
            int    half   = (fields.Length + 1) / 2;
            double colW   = ContentW / 2;
            double startY = y;

            // Pre-compute cumulative Y offsets for left and right columns
            double[] leftY  = new double[half];
            double[] rightY = new double[fields.Length - half];
            double accumL = 0, accumR = 0;
            for (int i = 0; i < fields.Length; i++)
            {
                double h = WrapKeys.Contains(fields[i].key) ? AddrRowH : RowH;
                if (i < half) { leftY[i]        = accumL; accumL += h; }
                else          { rightY[i - half] = accumR; accumR += h; }
            }
            double blockH = Math.Max(accumL, accumR);

            for (int i = 0; i < fields.Length; i++)
            {
                bool   isWrap = WrapKeys.Contains(fields[i].key);
                double cx     = (i < half) ? Margin : Margin + colW;
                double offset = (i < half) ? leftY[i] : rightY[i - half];
                double cy     = startY + offset;

                // Label
                gfx.DrawString(fields[i].key, FontBold, new XSolidBrush(LabelFg),
                    new XRect(cx, cy, colW * 0.38, RowH), FmtCenterLeft);

                if (isWrap)
                {
                    // FontBody is a property (not a method) — no parentheses
                    var wrappedLines = WrapText(gfx, fields[i].val, FontBody, colW * 0.60);
                    for (int li = 0; li < Math.Min(wrappedLines.Count, 2); li++)
                    {
                        gfx.DrawString(wrappedLines[li], FontBody, new XSolidBrush(BodyFg),
                            new XRect(cx + colW * 0.38, cy + li * RowH, colW * 0.60, RowH),
                            FmtCenterLeft);
                    }
                }
                else
                {
                    gfx.DrawString(fields[i].val, FontBody, new XSolidBrush(BodyFg),
                        new XRect(cx + colW * 0.38, cy, colW * 0.60, RowH), FmtCenterLeft);
                }
            }

            gfx.DrawLine(new XPen(BorderCl, 0.5),
                Margin, startY + blockH, Margin + ContentW, startY + blockH);
            return startY + blockH + 8;
        }

        /// <summary>
        /// Word-wraps <paramref name="text"/> to fit within <paramref name="maxWidth"/>.
        /// Returns at most 2 lines; overflow on line 2 is trimmed with ….
        /// </summary>
        private static List<string> WrapText(
            XGraphics gfx, string text, XFont font, double maxWidth)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(text)) { result.Add(""); return result; }

            string[] words   = text.Split(' ');
            string   current = "";

            foreach (string word in words)
            {
                string candidate = string.IsNullOrEmpty(current)
                    ? word
                    : current + " " + word;

                if (gfx.MeasureString(candidate, font).Width <= maxWidth)
                {
                    current = candidate;
                }
                else
                {
                    if (!string.IsNullOrEmpty(current))
                        result.Add(current);
                    current = word;
                    if (result.Count >= 2) break;
                }
            }

            if (!string.IsNullOrEmpty(current))
            {
                if (result.Count < 2)
                    result.Add(current);
                else
                {
                    // Trim line 2 to fit, then append ellipsis
                    string last = result[1];
                    while (last.Length > 0 &&
                           gfx.MeasureString(last + "\u2026", font).Width > maxWidth)
                        last = last.Substring(0, last.Length - 1);
                    result[1] = last + "\u2026";
                }
            }

            if (result.Count == 0) result.Add(text);
            return result;
        }

        private static double DrawItemsTable(
            XGraphics gfx, double y,
            List<PremiumLivingOPS.Models.Entities.ShipmentLineEntity> lines)
        {
            double[] colW  = { 90, 80, ContentW - 90 - 80 - 80 - 80, 80, 80 };
            string[] heads = { "LINE ID", "ITEM ID", "ITEM NAME", "QTY SHIPPED", "OUTSTANDING" };
            const double rowH = 18, headH = 22;

            gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(246, 249, 255)),
                Margin, y, ContentW, headH);
            double cx = Margin + 6;
            for (int i = 0; i < colW.Length; i++)
            {
                gfx.DrawString(heads[i], FontHeader, new XSolidBrush(LabelFg),
                    new XRect(cx, y, colW[i] - 4, headH), FmtCenterLeft);
                cx += colW[i];
            }
            y += headH;

            bool alt = false;
            foreach (var ln in lines
                ?? new List<PremiumLivingOPS.Models.Entities.ShipmentLineEntity>())
            {
                if (alt)
                    gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(250, 252, 255)),
                        Margin, y, ContentW, rowH);

                string[] vals =
                {
                    ln.ShipmentLineID ?? "",
                    ln.ItemID        ?? "",
                    ln.ItemName      ?? "",
                    ln.QtyShipped.ToString(),
                    ln.QtyOutstanding?.ToString() ?? "\u2014"
                };
                cx = Margin + 6;
                for (int i = 0; i < colW.Length; i++)
                {
                    gfx.DrawString(vals[i], FontBody, new XSolidBrush(BodyFg),
                        new XRect(cx, y, colW[i] - 4, rowH), FmtCenterLeft);
                    cx += colW[i];
                }
                gfx.DrawLine(new XPen(BorderCl, 0.3),
                    Margin, y + rowH, Margin + ContentW, y + rowH);
                y += rowH;
                alt = !alt;
            }
            return y + 4;
        }

        private static void DrawTotalsFooter(
            XGraphics gfx, int lineCount, double total)
        {
            double y = PageH - Margin - 20;
            gfx.DrawLine(new XPen(BorderCl, 0.5),
                Margin, y - 2, Margin + ContentW, y - 2);
            gfx.DrawString($"Shipment Lines:  {lineCount}",
                FontBold, new XSolidBrush(BodyFg),
                new XRect(Margin, y, ContentW / 2, 16), FmtCenterLeft);
            gfx.DrawString($"Total Amount:  HK$ {total:N2}",
                FontBold, new XSolidBrush(BlueFg),
                new XRect(Margin + ContentW / 2, y, ContentW / 2, 16), FmtCenterRight);
            gfx.DrawString(
                $"Generated by PremiumLiving OPS  \u2022  {DateTime.Now:yyyy-MM-dd HH:mm}",
                FontSmall, new XSolidBrush(LabelFg),
                new XRect(Margin, PageH - Margin - 2, ContentW, 10), FmtBottomLeft);
        }
    }
}
