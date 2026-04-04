using System.IO;
using System.Text.Json;
using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Data.Identity;

public class LocalActivationStateStore : ILocalActivationStateStore
{
    private static readonly object FileSync = new();
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public LocalActivationStateStore(string filePath = "local_activation_state.json")
    {
        _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
    }

    public Task<OperationResult<LocalActivationState>> LoadAsync()
    {
        try
        {
            lock (FileSync)
            {
                if (!File.Exists(_filePath))
                {
                    return Task.FromResult(OperationResult<LocalActivationState>.Success(LocalActivationState.Empty()));
                }

                var json = File.ReadAllText(_filePath);
                var state = JsonSerializer.Deserialize<LocalActivationState>(json) ?? LocalActivationState.Empty();
                return Task.FromResult(OperationResult<LocalActivationState>.Success(state));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult<LocalActivationState>.Failure(
                $"Ошибка чтения локального состояния активации: {ex.Message}",
                "ACTIVATION_STATE_READ_ERROR"));
        }
    }

    public Task<OperationResult> SaveAsync(LocalActivationState state)
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
                $"Ошибка сохранения локального состояния активации: {ex.Message}",
                "ACTIVATION_STATE_SAVE_ERROR"));
        }
    }

    public Task<OperationResult> ClearAsync()
    {
        return SaveAsync(LocalActivationState.Empty());
    }
}
