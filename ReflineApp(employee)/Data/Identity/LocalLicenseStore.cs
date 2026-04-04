using System.IO;
using System.Text.Json;
using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Data.Identity;

public class LocalLicenseStore : ILicenseStore
{
    private static readonly object FileSync = new();
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public LocalLicenseStore(string filePath = "licenses.json")
    {
        _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
    }

    public Task<OperationResult<License?>> GetByKeyAsync(string licenseKey)
    {
        try
        {
            lock (FileSync)
            {
                var normalizedKey = (licenseKey ?? string.Empty).Trim();
                var license = ReadAllUnsafe()
                    .FirstOrDefault(item => string.Equals(item.LicenseKey, normalizedKey, StringComparison.OrdinalIgnoreCase));

                return Task.FromResult(OperationResult<License?>.Success(license));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult<License?>.Failure(
                $"Ошибка чтения лицензии: {ex.Message}",
                "LICENSE_READ_ERROR"));
        }
    }

    public Task<OperationResult<License?>> GetByIdAsync(Guid licenseId)
    {
        try
        {
            lock (FileSync)
            {
                var license = ReadAllUnsafe().FirstOrDefault(item => item.Id == licenseId);
                return Task.FromResult(OperationResult<License?>.Success(license));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult<License?>.Failure(
                $"Ошибка поиска лицензии: {ex.Message}",
                "LICENSE_LOOKUP_ERROR"));
        }
    }

    public Task<OperationResult<IReadOnlyList<License>>> GetAllAsync()
    {
        try
        {
            lock (FileSync)
            {
                IReadOnlyList<License> licenses = ReadAllUnsafe();
                return Task.FromResult(OperationResult<IReadOnlyList<License>>.Success(licenses));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult<IReadOnlyList<License>>.Failure(
                $"Ошибка чтения лицензий: {ex.Message}",
                "LICENSE_LIST_ERROR"));
        }
    }

    public Task<OperationResult> SaveAllAsync(IEnumerable<License> licenses)
    {
        try
        {
            lock (FileSync)
            {
                WriteAllUnsafe(licenses.ToList());
                return Task.FromResult(OperationResult.Success());
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult.Failure(
                $"Ошибка сохранения лицензий: {ex.Message}",
                "LICENSE_SAVE_ERROR"));
        }
    }

    private List<License> ReadAllUnsafe()
    {
        if (!File.Exists(_filePath))
        {
            var seedLicenses = IdentitySeedData.CreateLicenses();
            WriteAllUnsafe(seedLicenses);
            return seedLicenses;
        }

        var json = File.ReadAllText(_filePath);
        var licenses = JsonSerializer.Deserialize<List<License>>(json) ?? new List<License>();

        if (licenses.Count == 0)
        {
            licenses = IdentitySeedData.CreateLicenses();
            WriteAllUnsafe(licenses);
        }

        return licenses;
    }

    private void WriteAllUnsafe(List<License> licenses)
    {
        var json = JsonSerializer.Serialize(licenses, _jsonOptions);
        File.WriteAllText(_filePath, json);
    }
}
