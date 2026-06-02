using FirebaseAdmin.Auth;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ServiceDeskDesktop.Views
{
    public partial class LoginWindow : Window
    {
        public string LoggedInEmail { get; private set; }
        public string LoggedInRole { get; private set; }
        public string LoggedInUid { get; private set; }

        private static readonly HttpClient client = new HttpClient();
        private const string ApiKey = "AIzaSyCfCVTIS9uMbGPWRci-j99A9JP60cPnGaU";

        public LoginWindow()
        {
            InitializeComponent();
        }
        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("Заполните email и пароль!");
                return;
            }

            try
            {
                // 1. Проверяем пароль через Firebase REST API
                var payload = new { email, password, returnSecureToken = true };
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(
                    $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={ApiKey}",
                    content);

                if (!response.IsSuccessStatusCode)
                {
                    ShowError("Неверный email или пароль!");
                    return;
                }

                // 2. Получаем данные пользователя из Admin SDK (роль и Uid)
                var user = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(email);

                if (user.Disabled)
                {
                    ShowError("Пользователь заблокирован!");
                    return;
                }

                string role = "field_engineer";
                if (user.CustomClaims != null && user.CustomClaims.ContainsKey("role"))
                    role = user.CustomClaims["role"].ToString();

                if (role != "admin" && role != "dispatcher")
                {
                    ShowError("Доступ разрешён только диспетчерам и администраторам!");
                    return;
                }

                string roleDisplay = role switch
                {
                    "admin" => "Администратор",
                    "dispatcher" => "Диспетчер",
                    "field_engineer" => "Полевой инженер",
                    _ => role
                };

                MessageBox.Show($"Вход выполнен! Роль: {roleDisplay}", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                LoggedInEmail = email;
                LoggedInRole = role;
                LoggedInUid = user.Uid;

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка: {ex.Message}");
            }

        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}