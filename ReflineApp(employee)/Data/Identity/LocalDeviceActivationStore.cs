using System.IO;
using System.Text.Json;
using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Data.Identity;

public class LocalDeviceActivationStore : IDeviceActivationStore
{
    private static readonly object FileSync = new();
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public LocalDeviceActivationStore(string filePath = "device_activations.json")
    {
        _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
    }

    public Task<OperationResult<DeviceActivation?>> GetByLicenseAndDeviceAsync(Guid licenseId, string deviceId)
    {
        try
        {
            lock (FileSync)
            {
                var normalizedDeviceId = (deviceId ?? string.Empty).Trim();
                var activation = ReadAllUnsafe()
                    .FirstOrDefault(item =>
                        item.LicenseId == licenseId &&
                        string.Equals(item.DeviceId, normalizedDeviceId, StringComparison.OrdinalIgnoreCase));

                return Task.FromResult(OperationResult<DeviceActivation?>.Success(activation));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult<DeviceActivation?>.Failure(
                $"Ошибка чтения активации устройства: {ex.Message}",
                "DEVICE_ACTIVATION_READ_ERROR"));
        }
    }

    public Task<OperationResult<int>> CountActiveByLicenseAsync(Guid licenseId)
    {
        try
        {
            lock (FileSync)
            {
                var count = ReadAllUnsafe().Count(item => item.LicenseId == licenseId && !item.IsRevoked);
                return Task.FromResult(OperationResult<int>.Success(count));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult<int>.Failure(
                $"Ошибка подсчёта активаций: {ex.Message}",
                "DEVICE_ACTIVATION_COUNT_ERROR"));
        }
    }

    public Task<OperationResult<IReadOnlyList<DeviceActivation>>> GetAllAsync()
    {
        try
        {
            lock (FileSync)
            {
                IReadOnlyList<DeviceActivation> activations = ReadAllUnsafe();
                return Task.FromResult(OperationResult<IReadOnlyList<DeviceActivation>>.Success(activations));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult<IReadOnlyList<DeviceActivation>>.Failure(
                $"Ошибка чтения активаций устройств: {ex.Message}",
                "DEVICE_ACTIVATION_LIST_ERROR"));
        }
    }

    public Task<OperationResult> SaveAsync(DeviceActivation activation)
    {
        try
        {
            lock (FileSync)
            {
                var all = ReadAllUnsafe();
                var existing = all.FirstOrDefault(item => item.Id == activation.Id);

                if (existing == null)
                {
                    if (activation.Id == Guid.Empty)
                    {
                        activation.Id = Guid.NewGuid();
                    }

                    all.Add(activation);
                }
                else
                {
                    existing.LicenseId = activation.LicenseId;
                    existing.UserId = activation.UserId;
                    existing.DeviceId = activation.DeviceId;
                    existing.MachineName = activation.MachineName;
                    existing.ActivatedAt = activation.ActivatedAt;
                    existing.LastSeenAt = activation.LastSeenAt;
                    existing.IsRevoked = activation.IsRevoked;
                }

                WriteAllUnsafe(all);
                return Task.FromResult(OperationResult.Success());
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult.Failure(
                $"Ошибка сохранения активации устройства: {ex.Message}",
                "DEVICE_ACTIVATION_SAVE_ERROR"));
        }
    }

    private List<DeviceActivation> ReadAllUnsafe()
    {
        if (!File.Exists(_filePath))
        {
            WriteAllUnsafe(new List<DeviceActivation>());
            return new List<DeviceActivation>();
        }

        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<List<DeviceActivation>>(json) ?? new List<DeviceActivation>();
    }

    private void WriteAllUnsafe(List<DeviceActivation> activations)
    {
        var json = JsonSerializer.Serialize(activations, _jsonOptions);
        File.WriteAllText(_filePath, json);
    }
}
