using ServiceDeskDesktop.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ServiceDeskDesktop.Views
{
    public partial class ReportWindow : Window
    {
        private readonly LocalDatabaseService _localDb = new();

        public ReportWindow()
        {
            InitializeComponent();
        }

        private DateTime GetFromDate()
        {
            var tag = ((ComboBoxItem)PeriodCombo.SelectedItem).Tag.ToString();
            return tag switch
            {
                "day" => DateTime.Today,
                "week" => DateTime.Today.AddDays(-7),
                "month" => DateTime.Today.AddMonths(-1),
                _ => DateTime.MinValue
            };
        }

        private void GenerateExcel_Click(object sender, RoutedEventArgs e)
        {
            var from = GetFromDate();
            var requests = _localDb.GetAllRequests()
                .Where(r => r.CreatedAt >= from)
                .ToList();

            bool byStatus = GroupByStatus.IsChecked == true;
            bool byType = GroupByType.IsChecked == true;
            bool byDistrict = GroupByDistrict.IsChecked == true;
            bool byPriority = GroupByPriority.IsChecked == true;

            string path;
            if (byStatus || byType || byDistrict || byPriority)
            {
                var service = new ReportExportService();
                path = service.ExportToExcel(requests, byStatus, byType, byDistrict, byPriority);
            }
            else
            {
                var service = new ExportService();
                path = service.ExportToExcel(requests);
            }

            MessageBox.Show($"Файл сохранён на рабочем столе:\n{path}", "Готово",
                MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        private void GenerateWord_Click(object sender, RoutedEventArgs e)
        {
            var from = GetFromDate();
            var requests = _localDb.GetAllRequests()
                .Where(r => r.CreatedAt >= from)
                .ToList();

            bool byStatus = GroupByStatus.IsChecked == true;
            bool byType = GroupByType.IsChecked == true;
            bool byDistrict = GroupByDistrict.IsChecked == true;
            bool byPriority = GroupByPriority.IsChecked == true;

            string path;
            if (byStatus || byType || byDistrict || byPriority)
            {
                var service = new ReportExportService();
                path = service.ExportToWord(requests, byStatus, byType, byDistrict, byPriority);
            }
            else
            {
                var service = new ExportService();
                path = service.ExportToWord(requests);
            }

            MessageBox.Show($"Файл сохранён на рабочем столе:\n{path}", "Готово",
                MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
    }
}