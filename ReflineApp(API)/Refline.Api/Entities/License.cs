namespace Refline.Api.Entities;

public sealed class License
{
    public long Id { get; set; }

    public long CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public string LicenseKey { get; set; } = string.Empty;

    public int MaxDevices { get; set; }

    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<DeviceActivation> DeviceActivations { get; set; } = new List<DeviceActivation>();
}
