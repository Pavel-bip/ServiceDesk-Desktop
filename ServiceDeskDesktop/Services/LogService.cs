using Serilog;
using System.IO;

namespace ServiceDeskDesktop.Services
{
    public static class LogService
    {
        public static void Initialize()
        {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "app.log");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    path: logPath,
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}"
                )
                .CreateLogger();

            Log.Information("=== Приложение запущено ===");
        }

        public static void Close()
        {
            Log.Information("=== Приложение закрыто ===");
            Log.CloseAndFlush();
        }
    }
}