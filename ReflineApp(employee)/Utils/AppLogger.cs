using Refline.Data.Settings;
using System.IO;

namespace Refline.Utils;

public static class AppLogger
{
    private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "refline_debug.log");
    private static readonly ISettingsDataService SettingsDataService = new SettingsDataService();

    public static void Log(string message, string level = "INFO")
    {
        try
        {
            var settingsResult = SettingsDataService.Load();
            if (settingsResult.IsSuccess && settingsResult.Value != null && !settingsResult.Value.EnableLocalLog)
            {
                return;
            }

            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}{Environment.NewLine}";
            File.AppendAllText(LogFilePath, logEntry);
        }
        catch
        {
            // Logging failures must not crash the app.
        }
    }
}
