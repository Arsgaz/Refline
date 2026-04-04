using Refline.Data.Infrastructure;
using Refline.Data.Settings;
using Refline.Models;
using System.IO;

namespace Refline.Business.Settings;

public class SettingsBusinessServer : ISettingsBusinessServer
{
    private readonly ISettingsDataService _settingsDataService;
    private readonly SettingsValidationService _validationService;
    private readonly IAutoStartRegistryService _autoStartRegistryService;

    public SettingsBusinessServer(
        ISettingsDataService settingsDataService,
        SettingsValidationService validationService,
        IAutoStartRegistryService autoStartRegistryService)
    {
        _settingsDataService = settingsDataService;
        _validationService = validationService;
        _autoStartRegistryService = autoStartRegistryService;
    }

    public OperationResult<AppSettings> LoadSettings()
    {
        var result = _settingsDataService.Load();
        if (!result.IsSuccess || result.Value == null)
        {
            return OperationResult<AppSettings>.Failure(result.Message, result.ErrorCode);
        }

        return OperationResult<AppSettings>.Success(result.Value);
    }

    public OperationResult SaveSettings(AppSettings settings)
    {
        var validation = _validationService.Validate(settings);
        if (!validation.IsSuccess)
        {
            return OperationResult.Failure(validation.Message, validation.ErrorCode);
        }

        try
        {
            Directory.CreateDirectory(settings.ReportsPath);
        }
        catch (Exception ex)
        {
            return OperationResult.Failure($"Не удалось создать папку отчётов: {ex.Message}", "REPORTS_PATH_CREATE_ERROR");
        }

        var saveResult = _settingsDataService.Save(settings);
        if (!saveResult.IsSuccess)
        {
            return OperationResult.Failure(saveResult.Message, saveResult.ErrorCode);
        }

        var autoStartResult = _autoStartRegistryService.ApplyAutoStart("ReflineTracker", settings.AutoStartWindows);
        if (!autoStartResult.IsSuccess)
        {
            return OperationResult.Failure(autoStartResult.Message, autoStartResult.ErrorCode);
        }

        return OperationResult.Success("Настройки сохранены.");
    }

    public OperationResult<bool> IsBackgroundTrackingAllowed()
    {
        var result = _settingsDataService.Load();
        if (!result.IsSuccess || result.Value == null)
        {
            return OperationResult<bool>.Failure(result.Message, result.ErrorCode);
        }

        return OperationResult<bool>.Success(result.Value.AllowBackgroundTracking);
    }
}
