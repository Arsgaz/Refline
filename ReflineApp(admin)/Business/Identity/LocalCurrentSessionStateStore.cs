using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Refline.Admin.Data.Infrastructure;

namespace Refline.Admin.Business.Identity;

public sealed class LocalCurrentSessionStateStore : ICurrentSessionStateStore
{
    private static readonly object FileSync = new();
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public LocalCurrentSessionStateStore(string filePath = "admin_current_session.json")
    {
        _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
    }

    public Task<OperationResult<AdminSessionState?>> LoadAsync()
    {
        try
        {
            lock (FileSync)
            {
                if (!File.Exists(_filePath))
                {
                    return Task.FromResult(OperationResult<AdminSessionState?>.Success(null));
                }

                var json = File.ReadAllText(_filePath);
                var state = JsonSerializer.Deserialize<AdminSessionState>(json, _jsonOptions);
                return Task.FromResult(OperationResult<AdminSessionState?>.Success(state));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult<AdminSessionState?>.Failure(
                $"Ошибка чтения локальной админской сессии: {ex.Message}",
                "ADMIN_SESSION_READ_ERROR"));
        }
    }

    public Task<OperationResult> SaveAsync(AdminSessionState state)
    {
        try
        {
            lock (FileSync)
            {
                var json = JsonSerializer.Serialize(state, _jsonOptions);
                File.WriteAllText(_filePath, json);
                return Task.FromResult(OperationResult.Success());
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult.Failure(
                $"Ошибка сохранения локальной админской сессии: {ex.Message}",
                "ADMIN_SESSION_SAVE_ERROR"));
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
                $"Ошибка очистки локальной админской сессии: {ex.Message}",
                "ADMIN_SESSION_CLEAR_ERROR"));
        }
    }
}
