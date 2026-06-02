using ClosedXML.Excel;
using ServiceDeskDesktop.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xceed.Words.NET;
using Xceed.Document.NET;
using Xceed.Drawing;
using Google.Cloud.Firestore;

namespace ServiceDeskDesktop.Services
{
    public class ExportService
    {
        public string ExportToExcel(List<ServiceRequest> requests)
        {
            string filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"Заявки_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            );

            using var wb = new XLWorkbook();

            var statuses = new[] { "Новая", "Назначена", "В работе", "Выполнена", "Отложена", "Отменена" };

            foreach (var status in statuses)
            {
                var group = requests.Where(r => r.Status == status).ToList();
                if (!group.Any()) continue;

                var ws = wb.Worksheets.Add(status);

                string[] headers = {
            "ID", "Клиент/счет", "Адрес", "Телефон", "Тип оборудования",
            "Описание", "Приоритет", "Дата создания", "Серийный номер", "Инженер", "Комментарий"
        };

                ws.Cell(1, 1).Value = $"Статус: {status}";
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
                foreach (var r in group)
                {
                    ws.Cell(row, 1).Value = r.Id;
                    ws.Cell(row, 2).Value = r.ClientInfo;
                    ws.Cell(row, 3).Value = r.Address;
                    ws.Cell(row, 4).Value = r.Phone;
                    ws.Cell(row, 5).Value = r.EquipmentType;
                    ws.Cell(row, 6).Value = r.IssueDescription;
                    ws.Cell(row, 7).Value = r.Priority;
                    ws.Cell(row, 8).Value = r.CreatedAt.ToString("dd.MM.yyyy HH:mm");
                    ws.Cell(row, 9).Value = r.SerialNumber;
                    ws.Cell(row, 10).Value = string.IsNullOrEmpty(r.AssignedEngineerEmail) ? "—" : r.AssignedEngineerEmail;
                    ws.Cell(row, 11).Value = r.ExternalComment;
                    row++;
                }

                ws.Columns().AdjustToContents();
            }

            if (requests.Any())
            {
                var wsAll = wb.Worksheets.Add("Все заявки");
                string[] headers = {
            "ID", "Клиент/счет", "Адрес", "Телефон", "Тип оборудования",
            "Описание", "Статус", "Приоритет", "Дата создания",
            "Серийный номер", "Инженер", "Комментарий"
        };

                for (int i = 0; i < headers.Length; i++)
                {
                    wsAll.Cell(1, i + 1).Value = headers[i];
                    wsAll.Cell(1, i + 1).Style.Font.Bold = true;
                    wsAll.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                }

                int row = 2;
                foreach (var r in requests)
                {
                    wsAll.Cell(row, 1).Value = r.Id;
                    wsAll.Cell(row, 2).Value = r.ClientInfo;
                    wsAll.Cell(row, 3).Value = r.Address;
                    wsAll.Cell(row, 4).Value = r.Phone;
                    wsAll.Cell(row, 5).Value = r.EquipmentType;
                    wsAll.Cell(row, 6).Value = r.IssueDescription;
                    wsAll.Cell(row, 7).Value = r.Status;
                    wsAll.Cell(row, 8).Value = r.Priority;
                    wsAll.Cell(row, 9).Value = r.CreatedAt.ToString("dd.MM.yyyy HH:mm");
                    wsAll.Cell(row, 10).Value = r.SerialNumber;
                    wsAll.Cell(row, 11).Value = string.IsNullOrEmpty(r.AssignedEngineerEmail) ? "—" : r.AssignedEngineerEmail;
                    wsAll.Cell(row, 12).Value = r.ExternalComment;
                    row++;
                }
                wsAll.Columns().AdjustToContents();
            }

            wb.SaveAs(filePath);
            return filePath;
        }

        public string ExportToWord(List<ServiceRequest> requests)
        {
            string filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"Заявки_{DateTime.Now:yyyyMMdd_HHmmss}.docx"
            );

            using var doc = DocX.Create(filePath);

            doc.InsertParagraph("Список заявок на обслуживание")
                .FontSize(16).Bold().Alignment = Alignment.center;
            doc.InsertParagraph("");

            var statuses = new[] { "Новая", "Назначена", "В работе", "Выполнена", "Отложена", "Отменена" };

            foreach (var status in statuses)
            {
                var group = requests.Where(r => r.Status == status).ToList();
                if (!group.Any()) continue;

                doc.InsertParagraph($"Статус: {status}").FontSize(13).Bold();

                var table = doc.AddTable(group.Count + 1, 11);
                table.Design = TableDesign.LightShadingAccent1;
                table.SetWidths(new float[] { 80, 120, 140, 100, 80, 160, 70, 100, 80, 100, 120 });

                string[] headers = {
            "ID", "Клиент/счет", "Адрес", "Телефон", "Тип",
            "Описание", "Приоритет", "Дата", "SN", "Инженер", "Комментарий"
        };

                for (int i = 0; i < headers.Length; i++)
                    table.Rows[0].Cells[i].Paragraphs[0].Append(headers[i]).Bold();

                int row = 1;
                foreach (var r in group)
                {
                    table.Rows[row].Cells[0].Paragraphs[0].Append(r.Id ?? "");
                    table.Rows[row].Cells[1].Paragraphs[0].Append(r.ClientInfo ?? "");
                    table.Rows[row].Cells[2].Paragraphs[0].Append(r.Address ?? "");
                    table.Rows[row].Cells[3].Paragraphs[0].Append(r.Phone ?? "");
                    table.Rows[row].Cells[4].Paragraphs[0].Append(r.EquipmentType ?? "");
                    table.Rows[row].Cells[5].Paragraphs[0].Append(r.IssueDescription ?? "");
                    table.Rows[row].Cells[6].Paragraphs[0].Append(r.Priority ?? "");
                    table.Rows[row].Cells[7].Paragraphs[0].Append(r.CreatedAt.ToString("dd.MM.yyyy HH:mm"));
                    table.Rows[row].Cells[8].Paragraphs[0].Append(r.SerialNumber ?? "");
                    table.Rows[row].Cells[9].Paragraphs[0].Append(string.IsNullOrEmpty(r.AssignedEngineerEmail) ? "—" : r.AssignedEngineerEmail);
                    table.Rows[row].Cells[10].Paragraphs[0].Append(r.ExternalComment ?? "");
                    row++;
                }

                doc.InsertTable(table);
                doc.InsertParagraph("");
            }

            doc.Save();
            return filePath;
        }
        [FirestoreProperty]
        public string AssignedEngineerEmail { get; set; }
    }
}