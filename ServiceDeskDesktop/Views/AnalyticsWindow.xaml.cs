using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using ServiceDeskDesktop.Services;
using SkiaSharp;
using System;
using System.Linq;
using System.Windows;

namespace ServiceDeskDesktop.Views
{
    public partial class AnalyticsWindow : Window
    {
        private readonly LocalDatabaseService _localDb = new();
        private string _currentGroup = "status";
        private string _currentPeriod = "week";

        public AnalyticsWindow()
        {
            InitializeComponent();
            LoadChart();
        }

        private void LoadChart()
        {
            var all = _localDb.GetAllRequests();
            DateTime from = _currentPeriod switch
            {
                "day" => DateTime.Today,
                "week" => DateTime.Today.AddDays(-7),
                "month" => DateTime.Today.AddMonths(-1),
                _ => DateTime.Today.AddDays(-7)
            };

            var filtered = all.Where(r => r.CreatedAt >= from).ToList();

            string[] labels;
            int[] values;

            switch (_currentGroup)
            {
                case "type":
                    var types = new[] { "ONT-модем", "Wi-Fi маршрутизатор", "ТВ-приставка", "Видеокамера" };
                    labels = types;
                    values = types.Select(t => filtered.Count(r => r.EquipmentType == t)).ToArray();
                    break;

                case "priority":
                    var priorities = new[] { "Плановый", "Срочный", "Аварийный" };
                    labels = priorities;
                    values = priorities.Select(p => filtered.Count(r => r.Priority == p)).ToArray();
                    break;

                default:
                    var statuses = new[] { "Новая", "Назначена", "В работе", "Выполнена", "Отложена", "Отменена" };
                    labels = statuses;
                    values = statuses.Select(s => filtered.Count(r => r.Status == s)).ToArray();
                    break;
            }

            var isDark = App.IsDarkTheme;
            var textColor = SKColor.Parse(isDark ? "#E8E0FF" : "#2D004D");

            Chart.Series = new ISeries[]
            {
        new ColumnSeries<int>
        {
            Name = "Количество",
            Values = values,
            Fill = new SolidColorPaint(SKColor.Parse("#9B30FF"))
        }
            };

            Chart.XAxes = new[]
            {
        new Axis
        {
            Labels = labels,
            LabelsRotation = 0,
            LabelsPaint = new SolidColorPaint(textColor)
        }
    };

            Chart.YAxes = new[]
            {
        new Axis
        {
            MinLimit = 0,
            LabelsPaint = new SolidColorPaint(textColor)
        }
    };
        }
        private void SetPeriodButton(string active)
        {
            DayButton.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(
                    active == "day" ? "#6A0DAD" : "#9B30FF"));
            WeekButton.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(
                    active == "week" ? "#6A0DAD" : "#9B30FF"));
            MonthButton.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(
                    active == "month" ? "#6A0DAD" : "#9B30FF"));
        }

        private void SetGroupButton(string active)
        {
            ByStatusButton.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(
                    active == "status" ? "#6A0DAD" : "#9B30FF"));
            ByTypeButton.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(
                    active == "type" ? "#6A0DAD" : "#9B30FF"));
            ByPriorityButton.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(
                    active == "priority" ? "#6A0DAD" : "#9B30FF"));
        }

        private void DayButton_Click(object sender, RoutedEventArgs e) { _currentPeriod = "day"; SetPeriodButton("day"); LoadChart(); }
        private void WeekButton_Click(object sender, RoutedEventArgs e) { _currentPeriod = "week"; SetPeriodButton("week"); LoadChart(); }
        private void MonthButton_Click(object sender, RoutedEventArgs e) { _currentPeriod = "month"; SetPeriodButton("month"); LoadChart(); }
        private void ByStatus_Click(object sender, RoutedEventArgs e) { _currentGroup = "status"; SetGroupButton("status"); LoadChart(); }
        private void ByType_Click(object sender, RoutedEventArgs e) { _currentGroup = "type"; SetGroupButton("type"); LoadChart(); }
        private void ByPriority_Click(object sender, RoutedEventArgs e) { _currentGroup = "priority"; SetGroupButton("priority"); LoadChart(); }

        private void Chart_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}