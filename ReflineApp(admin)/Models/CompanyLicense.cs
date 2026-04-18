namespace Refline.Admin.Models;

public sealed class CompanyLicense
{
    public long CompanyId { get; init; }

    public long LicenseId { get; init; }

    public string LicenseKey { get; init; } = string.Empty;

    public LicenseType LicenseType { get; init; }

    public bool IsActive { get; init; }

    public DateTimeOffset IssuedAt { get; init; }

    public DateTimeOffset ExpiresAt { get; init; }

    public int MaxDevices { get; init; }

    public int ActivatedDevicesCount { get; init; }

    public bool IsLifetime { get; init; }
}
