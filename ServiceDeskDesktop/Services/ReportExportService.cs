using ClosedXML.Excel;
using ServiceDeskDesktop.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xceed.Words.NET;
using Xceed.Document.NET;

namespace ServiceDeskDesktop.Services
{
    public class ReportExportService
    {
        public string ExportToExcel(List<ServiceRequest> requests, bool byStatus, bool byType, bool byDistrict, bool byPriority)
        {
            string filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"Отчёт_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

            using var wb = new XLWorkbook();

            var wsAll = wb.Worksheets.Add("Все данные");
            WriteSheet(wsAll, requests, "Все заявки");

            if (byStatus)
            {
                var statuses = new[] { "Новая", "Назначена", "В работе", "Выполнена", "Отложена", "Отменена" };
                foreach (var status in statuses)
                {
                    var group = requests.Where(r => r.Status == status).ToList();
                    if (group.Any())
                        WriteSheet(wb.Worksheets.Add(status), group, $"Статус: {status}");
                }
            }

            if (byType)
            {
                var types = new[] { "ONT-модем", "Wi-Fi маршрутизатор", "ТВ-приставка", "Видеокамера" };
                foreach (var type in types)
                {
                    var group = requests.Where(r => r.EquipmentType == type).ToList();
                    if (group.Any())
                        WriteSheet(wb.Worksheets.Add(type), group, $"Тип: {type}");
                }
            }

            if (byDistrict)
            {
                var districts = requests
                    .Select(r => r.Address?.Split(',', ' ').FirstOrDefault() ?? "Не указан")
                    .Distinct();
                foreach (var district in districts)
                {
                    var group = requests.Where(r => (r.Address?.Split(',', ' ').FirstOrDefault() ?? "Не указан") == district).ToList();
                    if (group.Any())
                        WriteSheet(wb.Worksheets.Add(district.Replace("/", "-")), group, $"Район: {district}");
                }
            }

            if (byPriority)
            {
                var priorities = new[] { "Плановый", "Срочный", "Аварийный" };
                foreach (var priority in priorities)
                {
                    var group = requests.Where(r => r.Priority == priority).ToList();
                    if (group.Any())
                        WriteSheet(wb.Worksheets.Add(priority), group, $"Приоритет: {priority}");
                }
            }

            wb.SaveAs(filePath);
            return filePath;
        }

        private void WriteSheet(IXLWorksheet ws, List<ServiceRequest> requests, string title)
        {
            string[] headers = {
        "ID", "Клиент/счет", "Адрес", "Телефон", "Тип оборудования",
        "Описание", "Статус", "Приоритет", "Дата создания",
        "Серийный номер", "Инженер", "Комментарий"
    };

            ws.Cell(1, 1).Value = title;
            ws.Range(1, 1, 1, headers.Length).Merge();
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#9B30FF");
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(3, i + 1).Value = headers[i];
                ws.Cell(3, i + 1).Style.Font.Bold = true;
                ws.Cell(3, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#E8E0F0");
            }

            int row = 4;
            foreach (var r in requests)
            {
                ws.Cell(row, 1).Value = r.Id;
                ws.Cell(row, 2).Value = r.ClientInfo;
                ws.Cell(row, 3).Value = r.Address;
                ws.Cell(row, 4).Value = r.Phone;
                ws.Cell(row, 5).Value = r.EquipmentType;
                ws.Cell(row, 6).Value = r.IssueDescription;
                ws.Cell(row, 7).Value = r.Status;
                ws.Cell(row, 8).Value = r.Priority;
                ws.Cell(row, 9).Value = r.CreatedAt.ToString("dd.MM.yyyy HH:mm");
                ws.Cell(row, 10).Value = r.SerialNumber;
                ws.Cell(row, 11).Value = string.IsNullOrEmpty(r.AssignedEngineerEmail) ? "—" : r.AssignedEngineerEmail;
                ws.Cell(row, 12).Value = r.ExternalComment;
                row++;
            }
            ws.Columns().AdjustToContents();
        }

        public string ExportToWord(List<ServiceRequest> requests, bool byStatus, bool byType, bool byDistrict, bool byPriority)
        {
            string filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"Отчёт_{DateTime.Now:yyyyMMdd_HHmmss}.docx");

            using var doc = DocX.Create(filePath);
            doc.InsertParagraph("ОТЧЁТ ПО ЗАЯВКАМ").FontSize(16).Bold().Alignment = Alignment.center;
            doc.InsertParagraph($"Сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(10);
            doc.InsertParagraph("");

            doc.InsertParagraph("ВСЕ ЗАЯВКИ").FontSize(13).Bold();
            WriteWordTable(doc, requests);
            doc.InsertParagraph("");

            if (byStatus)
            {
                var statuses = new[] { "Новая", "Назначена", "В работе", "Выполнена", "Отложена", "Отменена" };
                foreach (var status in statuses)
                {
                    var group = requests.Where(r => r.Status == status).ToList();
                    if (group.Any())
                    {
                        doc.InsertParagraph($"СТАТУС: {status.ToUpper()}").FontSize(13).Bold();
                        WriteWordTable(doc, group);
                        doc.InsertParagraph("");
                    }
                }
            }

            if (byType)
            {
                var types = new[] { "ONT-модем", "Wi-Fi маршрутизатор", "ТВ-приставка", "Видеокамера" };
                foreach (var type in types)
                {
                    var group = requests.Where(r => r.EquipmentType == type).ToList();
                    if (group.Any())
                    {
                        doc.InsertParagraph($"ТИП: {type.ToUpper()}").FontSize(13).Bold();
                        WriteWordTable(doc, group);
                        doc.InsertParagraph("");
                    }
                }
            }

            if (byDistrict)
            {
                var districts = requests
                    .Select(r => r.Address?.Split(',', ' ').FirstOrDefault() ?? "Не указан")
                    .Distinct();
                foreach (var district in districts)
                {
                    var group = requests.Where(r => (r.Address?.Split(',', ' ').FirstOrDefault() ?? "Не указан") == district).ToList();
                    if (group.Any())
                    {
                        doc.InsertParagraph($"РАЙОН: {district.ToUpper()}").FontSize(13).Bold();
                        WriteWordTable(doc, group);
                        doc.InsertParagraph("");
                    }
                }
            }

            if (byPriority)
            {
                var priorities = new[] { "Плановый", "Срочный", "Аварийный" };
                foreach (var priority in priorities)
                {
                    var group = requests.Where(r => r.Priority == priority).ToList();
                    if (group.Any())
                    {
                        doc.InsertParagraph($"ПРИОРИТЕТ: {priority.ToUpper()}").FontSize(13).Bold();
                        WriteWordTable(doc, group);
                        doc.InsertParagraph("");
                    }
                }
            }

            doc.Save();
            return filePath;
        }

        private void WriteWordTable(DocX doc, List<ServiceRequest> requests)
        {
            var table = doc.AddTable(requests.Count + 1, 8);
            table.Design = TableDesign.LightShadingAccent1;
            table.SetWidths(new float[] { 80, 120, 140, 100, 80, 70, 100, 120 });

            string[] headers = { "ID", "Клиент", "Адрес", "Телефон", "Тип", "Статус", "Приоритет", "Инженер" };
            for (int i = 0; i < headers.Length; i++)
                table.Rows[0].Cells[i].Paragraphs[0].Append(headers[i]).Bold();

            int row = 1;
            foreach (var r in requests)
            {
                table.Rows[row].Cells[0].Paragraphs[0].Append(r.Id ?? "");
                table.Rows[row].Cells[1].Paragraphs[0].Append(r.ClientInfo ?? "");
                table.Rows[row].Cells[2].Paragraphs[0].Append(r.Address ?? "");
                table.Rows[row].Cells[3].Paragraphs[0].Append(r.Phone ?? "");
                table.Rows[row].Cells[4].Paragraphs[0].Append(r.EquipmentType ?? "");
                table.Rows[row].Cells[5].Paragraphs[0].Append(r.Status ?? "");
                table.Rows[row].Cells[6].Paragraphs[0].Append(r.Priority ?? "");
                table.Rows[row].Cells[7].Paragraphs[0].Append(string.IsNullOrEmpty(r.AssignedEngineerEmail) ? "—" : r.AssignedEngineerEmail);
                row++;
            }
            doc.InsertTable(table);
        }
    }
}