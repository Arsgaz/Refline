using Refline.Api.Enums;

namespace Refline.Api.Contracts.Admin;

public sealed class CompanyLicenseDto
{
    public long CompanyId { get; set; }

    public long LicenseId { get; set; }

    public string LicenseKey { get; set; } = string.Empty;

    public LicenseType LicenseType { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset IssuedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public int MaxDevices { get; set; }

    public int ActivatedDevicesCount { get; set; }

    public bool IsLifetime { get; set; }
}
