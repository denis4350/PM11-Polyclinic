using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.IO;

namespace PolyclinicApp.Services
{
    public static class PdfReportService
    {
        public static void CreateAppointmentsReport(
            string filePath,
            DateTime dateFrom,
            DateTime dateTo,
            string statusText,
            List<AppointmentPdfItem> items)
        {
            Document document = new Document(PageSize.A4.Rotate(), 30, 30, 30, 30);

            PdfWriter.GetInstance(document, new FileStream(filePath, FileMode.Create));
            document.Open();

            string fontPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
                "arial.ttf");

            BaseFont baseFont = BaseFont.CreateFont(
                fontPath,
                BaseFont.IDENTITY_H,
                BaseFont.EMBEDDED);

            Font titleFont = new Font(baseFont, 16, Font.BOLD);
            Font normalFont = new Font(baseFont, 10, Font.NORMAL);
            Font boldFont = new Font(baseFont, 10, Font.BOLD);
            Font smallFont = new Font(baseFont, 8, Font.NORMAL);
            Font tableHeaderFont = new Font(baseFont, 9, Font.BOLD);
            Font tableFont = new Font(baseFont, 8, Font.NORMAL);

            Paragraph title = new Paragraph("Отчёт по приёмам пациентов", titleFont);
            title.Alignment = Element.ALIGN_CENTER;
            title.SpacingAfter = 12;
            document.Add(title);

            Paragraph info = new Paragraph(
                "Период: " + dateFrom.ToString("dd.MM.yyyy") +
                " - " + dateTo.ToString("dd.MM.yyyy") +
                "\nСтатус: " + statusText +
                "\nДата формирования: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm") +
                "\nКоличество записей: " + items.Count,
                normalFont);

            info.SpacingAfter = 14;
            document.Add(info);

            PdfPTable table = new PdfPTable(7);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 0.7f, 1.5f, 2.4f, 2.4f, 1.4f, 1.3f, 3.0f });

            AddHeaderCell(table, "ID", tableHeaderFont);
            AddHeaderCell(table, "Дата и время", tableHeaderFont);
            AddHeaderCell(table, "Пациент", tableHeaderFont);
            AddHeaderCell(table, "Врач", tableHeaderFont);
            AddHeaderCell(table, "Кабинет", tableHeaderFont);
            AddHeaderCell(table, "Статус", tableHeaderFont);
            AddHeaderCell(table, "Жалобы", tableHeaderFont);

            foreach (AppointmentPdfItem item in items)
            {
                AddBodyCell(table, item.AppointmentId.ToString(), tableFont);
                AddBodyCell(table, item.DateTimeText, tableFont);
                AddBodyCell(table, item.PatientName, tableFont);
                AddBodyCell(table, item.DoctorName, tableFont);
                AddBodyCell(table, item.CabinetName, tableFont);
                AddBodyCell(table, item.StatusText, tableFont);
                AddBodyCell(table, item.Complaints, tableFont);
            }

            document.Add(table);

            Paragraph footer = new Paragraph(
                "\nДокумент сформирован автоматически информационной системой поликлиники.",
                smallFont);

            footer.Alignment = Element.ALIGN_RIGHT;
            document.Add(footer);

            document.Close();
        }

        private static void AddHeaderCell(PdfPTable table, string text, Font font)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            cell.VerticalAlignment = Element.ALIGN_MIDDLE;
            cell.Padding = 5;
            cell.BackgroundColor = new BaseColor(230, 230, 230);
            table.AddCell(cell);
        }

        private static void AddBodyCell(PdfPTable table, string text, Font font)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text ?? "", font));
            cell.HorizontalAlignment = Element.ALIGN_LEFT;
            cell.VerticalAlignment = Element.ALIGN_TOP;
            cell.Padding = 4;
            table.AddCell(cell);
        }
        public static void CreateTableReport(
    string filePath,
    string titleText,
    string infoText,
    string[] headers,
    float[] widths,
    List<string[]> rows)
        {
            Document document = new Document(PageSize.A4.Rotate(), 30, 30, 30, 30);

            PdfWriter.GetInstance(document, new FileStream(filePath, FileMode.Create));
            document.Open();

            string fontPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
                "arial.ttf");

            BaseFont baseFont = BaseFont.CreateFont(
                fontPath,
                BaseFont.IDENTITY_H,
                BaseFont.EMBEDDED);

            Font titleFont = new Font(baseFont, 16, Font.BOLD);
            Font normalFont = new Font(baseFont, 10, Font.NORMAL);
            Font headerFont = new Font(baseFont, 9, Font.BOLD);
            Font tableFont = new Font(baseFont, 8, Font.NORMAL);

            Paragraph title = new Paragraph(titleText, titleFont);
            title.Alignment = Element.ALIGN_CENTER;
            title.SpacingAfter = 12;
            document.Add(title);

            Paragraph info = new Paragraph(infoText, normalFont);
            info.SpacingAfter = 14;
            document.Add(info);

            PdfPTable table = new PdfPTable(headers.Length);
            table.WidthPercentage = 100;
            table.SetWidths(widths);

            for (int i = 0; i < headers.Length; i++)
            {
                PdfPCell cell = new PdfPCell(new Phrase(headers[i], headerFont));
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                cell.VerticalAlignment = Element.ALIGN_MIDDLE;
                cell.Padding = 5;
                cell.BackgroundColor = new BaseColor(230, 230, 230);
                table.AddCell(cell);
            }

            foreach (string[] row in rows)
            {
                for (int i = 0; i < row.Length; i++)
                {
                    PdfPCell cell = new PdfPCell(new Phrase(row[i] ?? "", tableFont));
                    cell.HorizontalAlignment = Element.ALIGN_LEFT;
                    cell.VerticalAlignment = Element.ALIGN_TOP;
                    cell.Padding = 4;
                    table.AddCell(cell);
                }
            }

            document.Add(table);

            Paragraph footer = new Paragraph(
                "\nДокумент сформирован автоматически информационной системой поликлиники.",
                normalFont);

            footer.Alignment = Element.ALIGN_RIGHT;
            document.Add(footer);

            document.Close();
        }
    }

    public class AppointmentPdfItem
    {
        public int AppointmentId { get; set; }

        public string DateTimeText { get; set; }

        public string PatientName { get; set; }

        public string DoctorName { get; set; }

        public string CabinetName { get; set; }

        public string StatusText { get; set; }

        public string Complaints { get; set; }
    }
}