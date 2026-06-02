using FirebaseAdmin.Auth;
using ServiceDeskDesktop.Models;
using ServiceDeskDesktop.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualBasic;

namespace ServiceDeskDesktop.Views
{
    public partial class AdminWindow : Window
    {
        private readonly AuthService _authService = new();
        private List<UserModel> _users;

        public AdminWindow()
        {
            InitializeComponent();
            LoadUsers();
        }

        private async void LoadUsers()
        {
            try
            {
                var listUsers = FirebaseAuth.DefaultInstance.ListUsersAsync(null);
                var enumerator = listUsers.GetAsyncEnumerator();
                _users = new List<UserModel>();

                while (await enumerator.MoveNextAsync())
                {
                    var userRecord = enumerator.Current;

                    string role = "field_engineer";
                    if (userRecord.CustomClaims != null && userRecord.CustomClaims.ContainsKey("role"))
                        role = userRecord.CustomClaims["role"].ToString();

                    // Получаем подробную информацию о пользователе (включая время последнего входа)
                    DateTime? lastSignIn = null;
                    try
                    {
                        var fullUser = await FirebaseAuth.DefaultInstance.GetUserAsync(userRecord.Uid);
                        if (fullUser.UserMetaData?.LastSignInTimestamp != null)
                            lastSignIn = fullUser.UserMetaData.LastSignInTimestamp.Value.ToLocalTime();
                    }
                    catch
                    {
                        lastSignIn = null;
                    }

                    _users.Add(new UserModel
                    {
                        Uid = userRecord.Uid,
                        Email = userRecord.Email,
                        Role = role switch
                        {
                            "admin" => "Администратор",
                            "dispatcher" => "Диспетчер",
                            "field_engineer" => "Полевой инженер",
                            _ => role
                        },
                        IsBlocked = userRecord.Disabled,
                        LastSignIn = lastSignIn
                    });
                }

                UsersGrid.ItemsSource = _users;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пользователей: {ex.Message}");
            }
        }

        private UserModel GetSelectedUser()
        {
            var user = UsersGrid.SelectedItem as UserModel;
            if (user == null)
                MessageBox.Show("Выберите пользователя в таблице.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return user;
        }

        private bool IsSelf(UserModel user)
        {
            return user.Uid == App.CurrentUserId;
        }

        private async void BlockUser_Click(object sender, RoutedEventArgs e)
        {
            var user = GetSelectedUser();
            if (user == null) return;

            if (IsSelf(user))
            {
                MessageBox.Show("Нельзя заблокировать самого себя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            await _authService.DisableUserAsync(user.Uid);
            user.IsBlocked = true;
            UsersGrid.Items.Refresh();
        }

        private async void UnblockUser_Click(object sender, RoutedEventArgs e)
        {
            var user = GetSelectedUser();
            if (user == null) return;

            await _authService.EnableUserAsync(user.Uid);
            user.IsBlocked = false;
            UsersGrid.Items.Refresh();
        }

        private async void ResetPassword_Click(object sender, RoutedEventArgs e)
        {
            var user = GetSelectedUser();
            if (user == null) return;

            // Запрашиваем новый пароль у администратора
            string newPassword = Microsoft.VisualBasic.Interaction.InputBox(
                "Введите новый пароль для пользователя:",
                "Сброс пароля",
                "");

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                MessageBox.Show("Пароль не может быть пустым.", "Отмена", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPassword.Length < 6)
            {
                MessageBox.Show("Пароль должен содержать минимум 6 символов.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            await _authService.ResetPasswordAsync(user.Uid, newPassword);
            MessageBox.Show($"Пароль для {user.Email} изменён!\n\nНовый пароль: {newPassword}\n\nСохраните его, он больше не будет показан.",
                "Пароль сброшен", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            var user = GetSelectedUser();
            if (user == null) return;

            if (IsSelf(user))
            {
                MessageBox.Show("Нельзя удалить самого себя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show($"Вы уверены, что хотите удалить пользователя {user.Email}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            await _authService.DeleteUserAsync(user.Uid);
            _users.Remove(user);
            UsersGrid.ItemsSource = null;
            UsersGrid.ItemsSource = _users;
            MessageBox.Show("Пользователь удалён.");
        }

        private string GeneratePassword(int length = 10)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private async void CreateUser_Click(object sender, RoutedEventArgs e)
        {
            string email = NewEmailTextBox.Text.Trim();
            string password = NewPasswordTextBox.Text.Trim();
            var roleItem = (ComboBoxItem)NewRoleCombo.SelectedItem;
            string roleTag = roleItem.Tag.ToString();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Email и пароль обязательны!");
                return;
            }
            if (password.Length < 6)
            {
                MessageBox.Show("Пароль минимум 6 символов!");
                return;
            }

            try
            {
                string uid = await _authService.CreateUserAsync(email, password, email, roleTag);
                MessageBox.Show($"Пользователь {email} создан.\n\nПароль: {password}\n\nСохраните пароль! Он больше не будет показан.",
                    "Пользователь создан", MessageBoxButton.OK, MessageBoxImage.Information);
                NewEmailTextBox.Clear();
                NewPasswordTextBox.Clear();
                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }
    }
}