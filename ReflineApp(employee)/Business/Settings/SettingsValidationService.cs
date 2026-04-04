using Refline.Data.Infrastructure;
using Refline.Models;
using System.IO;

namespace Refline.Business.Settings;

public class SettingsValidationService
{
    public OperationResult Validate(AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ReportsPath))
        {
            return OperationResult.Failure("Путь для отчётов обязателен.", "REPORTS_PATH_REQUIRED");
        }

        try
        {
            _ = Path.GetFullPath(settings.ReportsPath);
            return OperationResult.Success();
        }
        catch (Exception)
        {
            return OperationResult.Failure("Путь для отчётов указан некорректно.", "REPORTS_PATH_INVALID");
        }
    }
}
