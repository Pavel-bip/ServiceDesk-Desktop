using FirebaseAdmin.Auth;
using ServiceDeskDesktop.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace ServiceDeskDesktop.Views
{
    public partial class FirstRunWindow : Window
    {
        public FirstRunWindow()
        {
            InitializeComponent();
        }

        private async void CreateAdmin_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox.Text.Trim();
            string password = PasswordBox.Password;
            string confirm = ConfirmPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Заполните все поля!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!email.Contains("@") || !email.Contains("."))
            {
                MessageBox.Show("Введите корректный email!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (password.Length < 6)
            {
                MessageBox.Show("Пароль должен содержать минимум 6 символов!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (password != confirm)
            {
                MessageBox.Show("Пароли не совпадают!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Создаём пользователя через Firebase Admin SDK
                var args = new UserRecordArgs
                {
                    Email = email,
                    Password = password,
                    DisplayName = "Администратор"
                };
                var userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(args);

                // Устанавливаем роль admin
                var claims = new Dictionary<string, object> { { "role", "admin" } };
                await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(userRecord.Uid, claims);

                // Сохраняем метку, что админ создан
                File.WriteAllText("admin_created.txt", userRecord.Uid);

                MessageBox.Show("Администратор успешно создан!\nТеперь войдите в систему.",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (FirebaseAuthException ex)
            {
                if (ex.Message.Contains("EMAIL_EXISTS"))
                    MessageBox.Show("Пользователь с таким email уже существует!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка соединения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}