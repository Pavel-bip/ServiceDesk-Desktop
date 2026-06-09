using ServiceDeskDesktop.Models;
using ServiceDeskDesktop.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Net.NetworkInformation;

namespace ServiceDeskDesktop.Views
{

    public partial class MainWindow : Window
    {
        private readonly LocalDatabaseService localDb = new();
        private readonly SyncService syncService = new();
        private readonly List<string> _conflictNotifications = new();
        private ObservableCollection<ServiceRequest> _requests;
        private ServiceRequest _selectedRequest;
        private readonly DispatcherTimer _onlineTimer;

        public MainWindow()
        {
            InitializeComponent();
            string roleDisplay = App.CurrentUserRole switch
            {
                "admin" => "Администратор",
                "dispatcher" => "Диспетчер",
                _ => App.CurrentUserRole
            };
            UserInfoText.Text = $"👤 {App.CurrentUserEmail} ({roleDisplay})";
            if (App.CurrentUserRole != "admin")
                AdminButton.Visibility = Visibility.Collapsed;
            ThemeButton.Content = App.IsDarkTheme ? "🔆" : "🌙";
            syncService.OnConflictDetected += (id, time) =>
            {
                Dispatcher.Invoke(() =>
                {
                    _conflictNotifications.Add($"Заявка {id} была обновлена с сервера ({time:dd.MM.yyyy HH:mm}), т.к. версия в облаке новее.");
                    UpdateConflictIndicator();
                });
            };
            LoadData();

            // Проверяем интернет сразу при запуске
            UpdateOnlineStatus(CheckInternetConnection());

            // Если онлайн — синхронизируемся
            if (CheckInternetConnection())
                _ = RefreshFromCloudAsync();

            // Таймер проверки каждые 10 секунд (быстрее)
            _onlineTimer = new DispatcherTimer();
            _onlineTimer.Interval = TimeSpan.FromSeconds(10);
            _onlineTimer.Tick += async (s, e) =>
            {
                bool online = CheckInternetConnection();
                UpdateOnlineStatus(online);
                if (online)
                    await CheckOnlineStatus();
            };
            _onlineTimer.Start();

        }

        private void LoadData()
        {
            var list = localDb.GetAllRequests();
            _requests = new ObservableCollection<ServiceRequest>(list);
            RequestsGrid.ItemsSource = _requests;
            UpdateRecordCount();
        }

        private async Task RefreshFromCloudAsync()
        {
            try
            {
                await syncService.SyncAllAsync();
                LoadData();
            }
            catch { }
        }

        private async Task CheckOnlineStatus()
        {
            try
            {
                await syncService.SyncAllAsync();
                LoadData();
            }
            catch { }
        }

        private void UpdateOnlineStatus(bool isOnline)
        {
            OnlineIndicator.Fill = isOnline ? new SolidColorBrush(Colors.LimeGreen) : new SolidColorBrush(Colors.Red);
            StatusText.Text = isOnline ? "Онлайн" : "Офлайн";
        }

        private void UpdateRecordCount()
        {
            RecordCount.Text = $"Записей: {_requests.Count}";
        }

        private void ApplyFilters()
        {
            var filtered = localDb.GetAllRequests().AsEnumerable();

            if (!string.IsNullOrEmpty(SearchTextBox.Text))
            {
                string s = SearchTextBox.Text.ToLower();
                filtered = filtered.Where(r =>
                    (r.Id?.ToLower().Contains(s) == true) ||
                    (r.ClientInfo?.ToLower().Contains(s) == true) ||
                    (r.Address?.ToLower().Contains(s) == true) ||
                    (r.SerialNumber?.ToLower().Contains(s) == true)
                );
            }

            if (StatusFilter.SelectedIndex > 0)
            {
                string status = ((ComboBoxItem)StatusFilter.SelectedItem).Content.ToString();
                filtered = filtered.Where(r => r.Status == status);
            }

            if (PriorityFilter.SelectedIndex > 0)
            {
                string priority = ((ComboBoxItem)PriorityFilter.SelectedItem).Content.ToString();
                filtered = filtered.Where(r => r.Priority == priority);
            }

            _requests = new ObservableCollection<ServiceRequest>(filtered);
            RequestsGrid.ItemsSource = _requests;
            UpdateRecordCount();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
        private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();

        private void RequestsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedRequest = RequestsGrid.SelectedItem as ServiceRequest;
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var editWindow = new RequestEditWindow(null);
            editWindow.Owner = this;
            if (editWindow.ShowDialog() == true)
            {
                var newRequest = editWindow.Result;
                newRequest.DocumentId = Guid.NewGuid().ToString();
                newRequest.Id = GenerateRequestId();
                newRequest.CreatedAt = DateTime.Now;
                newRequest.LastModified = DateTime.Now;
                newRequest.History = new List<HistoryEntry>
        {
            new HistoryEntry
            {
                Action = "Создание",
                UserId = App.CurrentUserId,
                Timestamp = DateTime.Now,
                Details = "Заявка создана"
            }
        };

                try
                {
                    await syncService.AddRequestAsync(newRequest);
                    MessageBox.Show("Заявка создана!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch
                {
                    localDb.SaveRequest(newRequest, true, "create");
                    MessageBox.Show("Заявка сохранена локально.", "Офлайн-режим", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                LoadData();
                if ((newRequest.Priority == "Аварийный" || newRequest.Priority == "Срочный")
    && !string.IsNullOrEmpty(newRequest.AssignedEngineerId))
                {
                    _ = new NotificationService().SendPushAsync(newRequest.AssignedEngineerId, newRequest.Id, newRequest.Priority);
                }
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRequest == null)
            {
                MessageBox.Show("Выберите заявку для удаления!", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_selectedRequest.DocumentId))
            {
                MessageBox.Show("Ошибка: у заявки отсутствует идентификатор.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show(
                $"Удалить заявку {_selectedRequest.Id}?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    localDb.DeleteRequest(_selectedRequest.DocumentId);

                    try
                    {
                        await syncService.DeleteRequestAsync(_selectedRequest.DocumentId);
                        MessageBox.Show("Заявка удалена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch
                    {
                        MessageBox.Show("Заявка удалена локально. Изменения синхронизируются при подключении к сети.",
                            "Офлайн-режим", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }

                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AnalyticsButton_Click(object sender, RoutedEventArgs e)
        {
            var analyticsWindow = new AnalyticsWindow();
            analyticsWindow.Owner = this;
            analyticsWindow.ShowDialog();
        }

        private void AdminButton_Click(object sender, RoutedEventArgs e)
        {
            var adminWindow = new AdminWindow();
            adminWindow.Owner = this;
            adminWindow.ShowDialog();
            _ = RefreshFromCloudAsync();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти?", "Выход",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                App.Logout();
                Application.Current.Shutdown();
            }
        }

        private string GenerateRequestId()
        {
            string date = DateTime.Now.ToString("yyMMdd");
            int count = _requests.Count + localDb.GetAllRequests().Count + 1;
            return $"ОБ-{date}-{count:D3}";
        }
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRequest == null)
            {
                MessageBox.Show("Выберите заявку для редактирования!", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var editWindow = new RequestEditWindow(_selectedRequest);
            editWindow.Owner = this;
            if (editWindow.ShowDialog() == true)
            {
                var updated = editWindow.Result;
                updated.LastModified = DateTime.Now;
                updated.History.Add(new HistoryEntry
                {
                    Action = "Редактирование",
                    UserId = App.CurrentUserId,
                    Timestamp = DateTime.Now,
                    Details = "Изменены поля заявки"
                });

                localDb.SaveRequest(updated);
                try
                {
                    _ = syncService.UpdateRequestAsync(updated);
                    MessageBox.Show("Заявка обновлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch
                {
                    localDb.SaveRequest(updated, true, "update");
                    MessageBox.Show("Изменения сохранены локально.", "Офлайн-режим", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                LoadData();
                if ((updated.Priority == "Аварийный" || updated.Priority == "Срочный")
    && !string.IsNullOrEmpty(updated.AssignedEngineerId))
                {
                    _ = new NotificationService().SendPushAsync(updated.AssignedEngineerId, updated.Id, updated.Priority);
                }
            }


        }
        private bool CheckInternetConnection()
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = ping.Send("8.8.8.8", 1000); // Google DNS, таймаут 1 сек
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }
        private void UpdateConflictIndicator()
        {
            if (_conflictNotifications.Count > 0)
            {
                StatusText.Text = $"⚠ Конфликтов: {_conflictNotifications.Count}";
                StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange);
            }
        }
        private void StatusText_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_conflictNotifications.Count > 0)
            {
                string message = string.Join("\n\n", _conflictNotifications);
                MessageBox.Show(message, "Конфликты синхронизации (LWW)",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                _conflictNotifications.Clear();
                UpdateOnlineStatus(CheckInternetConnection());
            }
        }
        private void ReportButton_Click(object sender, RoutedEventArgs e) 
        {
            var reportWindow = new ReportWindow();
            reportWindow.Owner = this;
            reportWindow.ShowDialog();
        }
        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            App.ToggleTheme();
            ThemeButton.Content = App.IsDarkTheme ? "🔆" : "🌙";
        }
    }
}
