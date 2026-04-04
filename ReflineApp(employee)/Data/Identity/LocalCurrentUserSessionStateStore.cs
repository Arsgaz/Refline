using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Data.Identity;

public sealed class LocalCurrentUserSessionStateStore : ICurrentUserSessionStateStore
{
    private static readonly object FileSync = new();
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public LocalCurrentUserSessionStateStore(string filePath = "current_user_session.json")
    {
        _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
    }

    public Task<OperationResult<CurrentUserSessionState?>> LoadAsync()
    {
        try
        {
            lock (FileSync)
            {
                if (!File.Exists(_filePath))
                {
                    return Task.FromResult(OperationResult<CurrentUserSessionState?>.Success(null));
                }

                var json = File.ReadAllText(_filePath);
                var state = JsonSerializer.Deserialize<CurrentUserSessionState>(json, _jsonOptions);
                return Task.FromResult(OperationResult<CurrentUserSessionState?>.Success(state));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult<CurrentUserSessionState?>.Failure(
                $"Ошибка чтения локальной сессии пользователя: {ex.Message}",
                "CURRENT_USER_SESSION_READ_ERROR"));
        }
    }

    public Task<OperationResult> SaveAsync(CurrentUserSessionState state)
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
                $"Ошибка сохранения локальной сессии пользователя: {ex.Message}",
                "CURRENT_USER_SESSION_SAVE_ERROR"));
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
                $"Ошибка очистки локальной сессии пользователя: {ex.Message}",
                "CURRENT_USER_SESSION_CLEAR_ERROR"));
        }
    }
}
