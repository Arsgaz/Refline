using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Data.Activity;

public sealed class LocalActivityClassificationRuleStore : IActivityClassificationRuleStore
{
    private static readonly object FileSync = new();
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public LocalActivityClassificationRuleStore(string filePath = "activity_classification_rules.json")
    {
        _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
    }

    public Task<OperationResult<ActivityClassificationRulesCache?>> LoadAsync()
    {
        try
        {
            lock (FileSync)
            {
                if (!File.Exists(_filePath))
                {
                    return Task.FromResult(OperationResult<ActivityClassificationRulesCache?>.Success(null));
                }

                var json = File.ReadAllText(_filePath);
                var cache = JsonSerializer.Deserialize<ActivityClassificationRulesCache>(json, _jsonOptions);
                return Task.FromResult(OperationResult<ActivityClassificationRulesCache?>.Success(cache));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult<ActivityClassificationRulesCache?>.Failure(
                $"Ошибка чтения локального кеша classification rules: {ex.Message}",
                "CLASSIFICATION_RULES_CACHE_READ_ERROR"));
        }
    }

    public Task<OperationResult> SaveAsync(ActivityClassificationRulesCache cache)
    {
        try
        {
            lock (FileSync)
            {
                var json = JsonSerializer.Serialize(cache, _jsonOptions);
                File.WriteAllText(_filePath, json);
                return Task.FromResult(OperationResult.Success());
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult.Failure(
                $"Ошибка сохранения локального кеша classification rules: {ex.Message}",
                "CLASSIFICATION_RULES_CACHE_SAVE_ERROR"));
        }
    }

    public Task<OperationResult> ClearAsync()
    {
        try
        {
            lock (FileSync)
            {
                if (File.Exists(_filePath))
                {
                    File.Delete(_filePath);
                }

                return Task.FromResult(OperationResult.Success());
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult.Failure(
                $"Ошибка очистки локального кеша classification rules: {ex.Message}",
                "CLASSIFICATION_RULES_CACHE_CLEAR_ERROR"));
        }
    }
}
