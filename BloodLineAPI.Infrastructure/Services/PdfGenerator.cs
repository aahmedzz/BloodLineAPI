using System;
using System.Collections.Generic;
using System.IO;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Features.Inventory.Queries.GetOutflowHistory;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore;

namespace BloodLineAPI.Infrastructure.Services;

public sealed class PdfGenerator : IPdfGenerator
{
    public byte[] GenerateOutflowReport(List<OutflowListDto> items, string performedByName, DateTime generatedAt)
    {
        // Create new PDF document
        using var document = new PdfDocument();
        document.Info.Title = "BloodLine Outflow History Report";
        document.Info.Author = performedByName;
        document.Info.Subject = "Inventory Outflow History";

        // Margins and Layout
        const double marginLeft = 40;
        const double marginRight = 40;
        const double marginTop = 40;
        const double maxPageHeight = 780; // height of A4 is 842, stop before bottom margin
        const double rowHeight = 22;

        int pageIndex = 1;
        PdfPage page = document.AddPage();
        page.Size = PageSize.A4;
        XGraphics gfx = XGraphics.FromPdfPage(page);

        // Fonts
        var unicodeOptions = new XPdfFontOptions(PdfFontEncoding.Unicode);
        XFont fontTitle = new XFont("Arial", 16, XFontStyle.Bold, unicodeOptions);
        XFont fontSubtitle = new XFont("Arial", 11, XFontStyle.Regular, unicodeOptions);
        XFont fontMeta = new XFont("Arial", 9, XFontStyle.Regular, unicodeOptions);
        XFont fontHeader = new XFont("Arial", 9.5, XFontStyle.Bold, unicodeOptions);
        XFont fontData = new XFont("Arial", 9, XFontStyle.Regular, unicodeOptions);
        XFont fontFooter = new XFont("Arial", 8, XFontStyle.Regular, unicodeOptions);

        // Branding colors
        XSolidBrush headerBrush = new XSolidBrush(XColor.FromArgb(30, 58, 138)); // Dark Blue #1E3A8A
        XSolidBrush textWhite = new XSolidBrush(XColors.White);
        XSolidBrush textDark = new XSolidBrush(XColor.FromArgb(31, 41, 55)); // Charcoal #1F2937
        XSolidBrush textGray = new XSolidBrush(XColors.Gray);
        XSolidBrush rowLightGray = new XSolidBrush(XColor.FromArgb(249, 250, 251)); // #F9FAFB
        XSolidBrush rowWhite = new XSolidBrush(XColors.White);

        XPen borderPen = new XPen(XColor.FromArgb(229, 231, 235), 0.75); // Light Gray #E5E7EB
        XPen dividerPen = new XPen(XColor.FromArgb(30, 58, 138), 1.5);

        // Column X coordinate starts and widths
        // Total printable width = 515 (595 - 80)
        double[] colWidths = { 75, 75, 60, 110, 110, 85 };
        double[] colX = new double[6];
        colX[0] = marginLeft;
        for (int i = 1; i < colX.Length; i++)
        {
            colX[i] = colX[i - 1] + colWidths[i - 1];
        }

        // Action translation helper
        string FormatAction(string action) => action.ToLowerInvariant() switch
        {
            "issued" => "Issued (صرف)",
            "disposed" => "Disposed (إتلاف)",
            _ => action
        };

        // Draw page template (header, title, meta)
        void DrawHeader(double yStart)
        {
            // Title
            gfx.DrawString("BloodLine - Inventory Outflow Report", fontTitle, headerBrush, marginLeft, yStart);
            gfx.DrawString(ArabicSupport.ArabicFixer.Fix("تقرير سجل حركة الصرف والإتلاف للمخزن"), fontSubtitle, textGray, marginLeft, yStart + 18);

            // Divider
            gfx.DrawLine(dividerPen, marginLeft, yStart + 26, page.Width - marginRight, yStart + 26);

            // Metadata block
            double metaY = yStart + 42;
            gfx.DrawString($"Generated Date: {generatedAt:yyyy-MM-dd HH:mm}", fontMeta, textDark, marginLeft, metaY);
            gfx.DrawString(ArabicSupport.ArabicFixer.Fix($"Exported By: {performedByName}", false), fontMeta, textDark, marginLeft, metaY + 14);
            gfx.DrawString($"Total Records: {items.Count}", fontMeta, textDark, page.Width - marginRight - 120, metaY);

            // Table headers block starting Y = yStart + 70
        }

        void DrawTableHeaders(double y)
        {
            // Header row rectangle background
            gfx.DrawRectangle(headerBrush, marginLeft, y, page.Width - marginLeft - marginRight, 22);

            string[] headers = { "Record Code", "Blood Bag ID", "Blood Type", "Action Type", "Recipient", "Date Performed" };
            XStringFormat format = new XStringFormat
            {
                Alignment = XStringAlignment.Center,
                LineAlignment = XLineAlignment.Center
            };

            for (int i = 0; i < headers.Length; i++)
            {
                // Draw header text
                gfx.DrawString(headers[i], fontHeader, textWhite, new XRect(colX[i], y, colWidths[i], 22), format);
            }

            // Draw vertical separator lines in the header
            XPen headerBorderPen = new XPen(XColor.FromArgb(255, 255, 255, 50), 0.75); // semi-transparent white
            for (int i = 1; i < colX.Length; i++)
            {
                gfx.DrawLine(headerBorderPen, colX[i], y, colX[i], y + 22);
            }
        }

        void DrawFooter()
        {
            string footerText = $"Page {pageIndex}";
            gfx.DrawString(footerText, fontFooter, textGray, new XRect(marginLeft, page.Height - 30, page.Width - marginLeft - marginRight, 15), XStringFormats.Center);
        }

        // Draw First Page Header
        DrawHeader(marginTop);
        double currentY = 125;
        DrawTableHeaders(currentY);
        currentY += 22;

        XStringFormat cellFormat = new XStringFormat
        {
            Alignment = XStringAlignment.Center,
            LineAlignment = XLineAlignment.Center
        };

        int itemIndex = 0;
        foreach (var item in items)
        {
            // Page break check
            if (currentY + rowHeight > maxPageHeight)
            {
                DrawFooter();

                // Add new page
                page = document.AddPage();
                page.Size = PageSize.A4;
                gfx = XGraphics.FromPdfPage(page);
                pageIndex++;

                // Draw standard header on subsequent pages (lighter version)
                gfx.DrawString("BloodLine - Inventory Outflow Report (Continued)", fontSubtitle, headerBrush, marginLeft, 30);
                gfx.DrawLine(borderPen, marginLeft, 44, page.Width - marginRight, 44);

                currentY = 55;
                DrawTableHeaders(currentY);
                currentY += 22;
            }

            // Draw alternate row background
            XSolidBrush rowBg = (itemIndex % 2 == 0) ? rowWhite : rowLightGray;
            gfx.DrawRectangle(rowBg, marginLeft, currentY, page.Width - marginLeft - marginRight, rowHeight);

            // Draw borders
            gfx.DrawRectangle(borderPen, marginLeft, currentY, page.Width - marginLeft - marginRight, rowHeight);

            // Draw vertical separator lines between columns
            for (int i = 1; i < colX.Length; i++)
            {
                gfx.DrawLine(borderPen, colX[i], currentY, colX[i], currentY + rowHeight);
            }

            // Values
            string actionText = ArabicSupport.ArabicFixer.Fix(FormatAction(item.ActionType), false);
            string recipientOrReason = item.ActionType.ToLowerInvariant() == "issued"
                ? (item.RecipientName ?? "-")
                : "-";
            recipientOrReason = ArabicSupport.ArabicFixer.Fix(recipientOrReason, false);

            string[] values = {
                item.RecordCode,
                item.BagCode,
                item.BloodType,
                actionText,
                recipientOrReason,
                item.PerformedAt.ToString("yyyy-MM-dd HH:mm")
            };

            for (int i = 0; i < values.Length; i++)
            {
                gfx.DrawString(values[i] ?? "-", fontData, textDark, new XRect(colX[i], currentY, colWidths[i], rowHeight), cellFormat);
            }

            currentY += rowHeight;
            itemIndex++;
        }

        // Draw footer on final page
        DrawFooter();

        // Save PDF to memory stream
        using var stream = new MemoryStream();
        document.Save(stream);
        return stream.ToArray();
    }
}
