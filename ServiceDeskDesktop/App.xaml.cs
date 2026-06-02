using Newtonsoft.Json;
using ServiceDeskDesktop.Services;
using ServiceDeskDesktop.Views;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace ServiceDeskDesktop
{
    public partial class App : Application
    {
        public static string CurrentUserId { get; set; } = "";
        public static string CurrentUserRole { get; set; } = "";
        public static string CurrentUserEmail { get; set; } = "";
        public static bool IsDarkTheme { get; set; } = false;

        private static readonly string SessionFile = "session.dat";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            LogService.Initialize();
            _ = FirebaseService.Instance;

            if (File.Exists("theme.txt"))
                IsDarkTheme = File.ReadAllText("theme.txt") == "dark";
            ApplyTheme();

            // Первый запуск — создание админа
            if (!File.Exists("admin_created.txt"))
            {
                var firstRun = new FirstRunWindow();
                firstRun.ShowDialog();

                if (!File.Exists("admin_created.txt"))
                {
                    Shutdown();
                    return;
                }

                // После создания админа — открываем окно входа
                var loginAfterFirstRun = new LoginWindow();
                if (loginAfterFirstRun.ShowDialog() == true)
                {
                    CurrentUserId = loginAfterFirstRun.LoggedInUid;
                    CurrentUserEmail = loginAfterFirstRun.LoggedInEmail;
                    CurrentUserRole = loginAfterFirstRun.LoggedInRole;
                    SaveSession(CurrentUserEmail, CurrentUserRole, CurrentUserId);
                    ShowMainWindow();
                }
                else
                {
                    Shutdown();
                }
                return;
            }

            // Автовход
            if (TryAutoLogin())
            {
                ShowMainWindow();
                return;
            }

            // Ручной вход
            var login = new LoginWindow();
            if (login.ShowDialog() == true)
            {
                CurrentUserId = login.LoggedInUid;
                CurrentUserEmail = login.LoggedInEmail;
                CurrentUserRole = login.LoggedInRole;
                SaveSession(CurrentUserEmail, CurrentUserRole, CurrentUserId);
                ShowMainWindow();
            }
            else
            {
                Shutdown();
            }
        }

        private void ShowMainWindow()
        {
            var mainWindow = new MainWindow();
            mainWindow.Closed += (s, args) => Shutdown();
            mainWindow.Show();
        }

        public static void ToggleTheme()
        {
            IsDarkTheme = !IsDarkTheme;
            File.WriteAllText("theme.txt", IsDarkTheme ? "dark" : "light");
            ApplyTheme();
        }

        private static void ApplyTheme()
        {
            Current.Resources.MergedDictionaries.Clear();
            var dict = new ResourceDictionary();
            dict.Source = new Uri(IsDarkTheme ? "pack://application:,,,/Themes/DarkTheme.xaml"
                                              : "pack://application:,,,/Themes/LightTheme.xaml");
            Current.Resources.MergedDictionaries.Add(dict);
        }

        private void SaveSession(string email, string role, string uid)
        {
            var data = JsonConvert.SerializeObject(new { email, role, uid });
            var bytes = Encoding.UTF8.GetBytes(data);
            var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(SessionFile, encrypted);
        }

        private bool TryAutoLogin()
        {
            if (!File.Exists(SessionFile)) return false;
            try
            {
                var encrypted = File.ReadAllBytes(SessionFile);
                var bytes = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                var json = Encoding.UTF8.GetString(bytes);
                var session = JsonConvert.DeserializeAnonymousType(json, new { email = "", role = "", uid = "" });
                if (session == null || string.IsNullOrEmpty(session.email)) return false;
                CurrentUserEmail = session.email;
                CurrentUserRole = session.role;
                CurrentUserId = session.uid;
                return true;
            }
            catch { return false; }
        }

        public static void Logout()
        {
            if (File.Exists(SessionFile)) File.Delete(SessionFile);
            CurrentUserEmail = "";
            CurrentUserRole = "";
            CurrentUserId = "";
        }

        protected override void OnExit(ExitEventArgs e)
        {
            LogService.Close();
            base.OnExit(e);
        }
    }
}