using System.IO;
using Refline.Data.Infrastructure;

namespace Refline.Data.Identity;

public class LocalDeviceIdentityProvider : IDeviceIdentityProvider
{
    private static readonly object FileSync = new();
    private readonly string _filePath;

    public LocalDeviceIdentityProvider(string filePath = "device_id.txt")
    {
        _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
    }

    public Task<OperationResult<string>> GetOrCreateDeviceIdAsync()
    {
        try
        {
            lock (FileSync)
            {
                if (File.Exists(_filePath))
                {
                    var existingId = File.ReadAllText(_filePath).Trim();
                    if (Guid.TryParse(existingId, out var parsedDeviceId))
                    {
                        return Task.FromResult(OperationResult<string>.Success(parsedDeviceId.ToString("D")));
                    }
                }

                var newDeviceId = Guid.NewGuid().ToString("D");
                File.WriteAllText(_filePath, newDeviceId);
                return Task.FromResult(OperationResult<string>.Success(newDeviceId));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult<string>.Failure(
                $"Ошибка получения DeviceId: {ex.Message}",
                "DEVICE_ID_ERROR"));
        }
    }
}
