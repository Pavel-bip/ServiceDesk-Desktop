using FirebaseAdmin.Auth;
using ServiceDeskDesktop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace ServiceDeskDesktop.Views
{
    public partial class RequestEditWindow : Window
    {
        public ServiceRequest Result { get; private set; }
        private readonly ServiceRequest _original;
        private bool _isFioMode = true;
        private List<UserModel> _engineers;

        private StackPanel _fioPanel;
        private StackPanel _accountPanel;

        public RequestEditWindow(ServiceRequest request)
        {
            InitializeComponent();

            _fioPanel = FindName("FioPanel") as StackPanel;
            _accountPanel = FindName("AccountPanel") as StackPanel;

            _original = request;

            if (request != null)
            {
                Title = "Редактирование заявки";
                IdTextBox.Text = request.Id;
                AddressTextBox.Text = request.Address;
                PhoneTextBox.Text = request.Phone;
                DescriptionTextBox.Text = request.IssueDescription;
                SerialTextBox.Text = request.SerialNumber;
                InternalCommentTextBox.Text = request.InternalComment;
                ExternalCommentTextBox.Text = request.ExternalComment;

                SelectComboItem(EquipmentTypeCombo, request.EquipmentType);
                SelectComboItem(StatusCombo, request.Status);
                SelectComboItem(PriorityCombo, request.Priority);

                if (request.DispatcherFlags != null)
                {
                    RequiresApprovalCheck.IsChecked = request.DispatcherFlags.ContainsKey("requiresClientApproval")
                        && request.DispatcherFlags["requiresClientApproval"];
                    AwaitingPartsCheck.IsChecked = request.DispatcherFlags.ContainsKey("awaitingParts")
                        && request.DispatcherFlags["awaitingParts"];
                }

                if (!string.IsNullOrEmpty(request.ClientInfo))
                {
                    if (request.ClientInfo.All(c => char.IsDigit(c) || c == '-' || c == ' '))
                    {
                        AccountRadio.IsChecked = true;
                        AccountTextBox.Text = request.ClientInfo;
                        _isFioMode = false;
                    }
                    else
                    {
                        FioRadio.IsChecked = true;
                        var parts = request.ClientInfo.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 1) LastNameTextBox.Text = parts[0];
                        if (parts.Length >= 2) FirstNameTextBox.Text = parts[1];
                        if (parts.Length >= 3) MiddleNameTextBox.Text = parts[2];
                        _isFioMode = true;
                    }
                }
                UpdatePanelsVisibility();
            }
            else
            {
                Title = "Добавление заявки";
                StatusCombo.SelectedIndex = 0;
                PriorityCombo.SelectedIndex = 0;
                EquipmentTypeCombo.SelectedIndex = 0;
            }

            LoadEngineers();
        }

        private void SelectComboItem(ComboBox combo, string value)
        {
            foreach (ComboBoxItem item in combo.Items)
            {
                if (item.Content.ToString() == value)
                {
                    item.IsSelected = true;
                    return;
                }
            }
            combo.SelectedIndex = 0;
        }

        private void UpdatePanelsVisibility()
        {
            if (_fioPanel != null)
                _fioPanel.Visibility = _isFioMode ? Visibility.Visible : Visibility.Collapsed;
            if (_accountPanel != null)
                _accountPanel.Visibility = _isFioMode ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ClientType_Changed(object sender, RoutedEventArgs e)
        {
            _isFioMode = FioRadio.IsChecked == true;
            UpdatePanelsVisibility();
        }

        private async void LoadEngineers()
        {
            try
            {
                var listUsers = FirebaseAuth.DefaultInstance.ListUsersAsync(null);
                var enumerator = listUsers.GetAsyncEnumerator();
                _engineers = new List<UserModel>();

                while (await enumerator.MoveNextAsync())
                {
                    var user = enumerator.Current;
                    if (user.CustomClaims != null && user.CustomClaims.ContainsKey("role") &&
                        user.CustomClaims["role"].ToString() == "field_engineer")
                    {
                        _engineers.Add(new UserModel
                        {
                            Uid = user.Uid,
                            Email = user.Email,
                            FullName = user.DisplayName ?? user.Email,
                            Role = "field_engineer"
                        });
                    }
                }

                EngineerCombo.Items.Clear();
                EngineerCombo.Items.Add(new ComboBoxItem { Content = "Не назначен", Tag = "" });

                foreach (var eng in _engineers)
                {
                    var item = new ComboBoxItem
                    {
                        Content = $"{eng.Email} (Полевой инженер)",
                        Tag = eng.Uid
                    };
                    EngineerCombo.Items.Add(item);
                }

                if (_original != null && !string.IsNullOrEmpty(_original.AssignedEngineerId))
                {
                    foreach (ComboBoxItem item in EngineerCombo.Items)
                    {
                        if (item.Tag?.ToString() == _original.AssignedEngineerId)
                        {
                            item.IsSelected = true;
                            break;
                        }
                    }
                }
            }
            catch
            {
                EngineerCombo.Items.Clear();
                EngineerCombo.Items.Add(new ComboBoxItem { Content = "Нет соединения", Tag = "" });
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string clientInfo;
            if (_isFioMode)
            {
                string lastName = LastNameTextBox.Text.Trim();
                string firstName = FirstNameTextBox.Text.Trim();
                string middleName = MiddleNameTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(lastName) && string.IsNullOrWhiteSpace(firstName))
                {
                    MessageBox.Show("Введите фамилию и имя клиента или выберите 'Лицевой счёт'!",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                clientInfo = $"{lastName} {firstName} {middleName}".Trim();
            }
            else
            {
                clientInfo = AccountTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(clientInfo))
                {
                    MessageBox.Show("Введите номер лицевого счёта или выберите 'ФИО'!",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(AddressTextBox.Text))
            {
                MessageBox.Show("Поле 'Адрес подключения' обязательно для заполнения!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!Regex.IsMatch(PhoneTextBox.Text ?? "", @"^\+7\d{10}$"))
            {
                MessageBox.Show("Телефон должен быть в формате +7XXXXXXXXXX!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string newStatus = ((ComboBoxItem)StatusCombo.SelectedItem).Content.ToString();
            string oldStatus = _original?.Status;

            Result = new ServiceRequest
            {
                DocumentId = _original?.DocumentId,
                Id = _original?.Id ?? IdTextBox.Text,
                ClientInfo = clientInfo,
                Address = AddressTextBox.Text.Trim(),
                Phone = PhoneTextBox.Text.Trim(),
                EquipmentType = ((ComboBoxItem)EquipmentTypeCombo.SelectedItem).Content.ToString(),
                IssueDescription = DescriptionTextBox.Text.Trim(),
                Status = newStatus,
                Priority = ((ComboBoxItem)PriorityCombo.SelectedItem).Content.ToString(),
                SerialNumber = SerialTextBox.Text.Trim(),
                InternalComment = InternalCommentTextBox.Text.Trim(),
                ExternalComment = ExternalCommentTextBox.Text.Trim(),
                CreatedAt = DateTime.Now,
                LastModified = DateTime.Now,
                WorkStartedAt = newStatus == "В работе" && oldStatus != "В работе"
                    ? DateTime.Now
                    : _original?.WorkStartedAt,
                WorkCompletedAt = newStatus == "Выполнена" && oldStatus != "Выполнена"
                    ? DateTime.Now
                    : _original?.WorkCompletedAt,
                AssignedEngineerId = ((ComboBoxItem)EngineerCombo.SelectedItem)?.Tag?.ToString(),
                AssignedEngineerEmail = ((ComboBoxItem)EngineerCombo.SelectedItem)?.Content?.ToString(),
                DispatcherFlags = new Dictionary<string, bool>
                {
                    { "requiresClientApproval", RequiresApprovalCheck.IsChecked == true },
                    { "awaitingParts", AwaitingPartsCheck.IsChecked == true }
                },
                History = _original?.History ?? new List<HistoryEntry>()
            };

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}