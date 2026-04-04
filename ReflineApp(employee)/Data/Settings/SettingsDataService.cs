using System.Text.Json;
using Refline.Data.Infrastructure;
using Refline.Models;
using System.IO;

namespace Refline.Data.Settings;

public class SettingsDataService : ISettingsDataService
{
    private static readonly object FileSync = new();
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public SettingsDataService(string filePath = "settings.json")
    {
        _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
    }

    public OperationResult<AppSettings> Load()
    {
        try
        {
            lock (FileSync)
            {
                if (!File.Exists(_filePath))
                {
                    return OperationResult<AppSettings>.Success(new AppSettings());
                }

                var json = File.ReadAllText(_filePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                return OperationResult<AppSettings>.Success(settings);
            }
        }
        catch (Exception ex)
        {
            return OperationResult<AppSettings>.Failure(
                $"Ошибка чтения настроек: {ex.Message}",
                "SETTINGS_READ_ERROR");
        }
    }

    public OperationResult Save(AppSettings settings)
    {
        try
        {
            lock (FileSync)
            {
                var json = JsonSerializer.Serialize(settings, _jsonOptions);
                File.WriteAllText(_filePath, json);
                return OperationResult.Success();
            }
        }
        catch (Exception ex)
        {
            return OperationResult.Failure(
                $"Ошибка сохранения настроек: {ex.Message}",
                "SETTINGS_SAVE_ERROR");
        }
    }
}
