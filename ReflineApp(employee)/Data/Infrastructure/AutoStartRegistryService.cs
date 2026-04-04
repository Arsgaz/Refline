using System.Reflection;
using Microsoft.Win32;

namespace Refline.Data.Infrastructure;

public class AutoStartRegistryService : IAutoStartRegistryService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public OperationResult ApplyAutoStart(string appName, bool enable)
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            if (key == null)
            {
                return OperationResult.Failure("Не удалось открыть ветку автозапуска Windows.", "REGISTRY_ACCESS");
            }

            if (enable)
            {
                var appPath = Assembly.GetExecutingAssembly().Location;
                if (appPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    appPath = appPath[..^4] + ".exe";
                }

                key.SetValue(appName, $"\"{appPath}\"");
            }
            else if (key.GetValue(appName) != null)
            {
                key.DeleteValue(appName);
            }

            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            return OperationResult.Failure($"Ошибка применения автозапуска: {ex.Message}", "REGISTRY_ERROR");
        }
    }
}
