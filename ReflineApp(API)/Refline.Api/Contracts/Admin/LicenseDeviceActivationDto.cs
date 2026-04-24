namespace Refline.Api.Contracts.Admin;

public sealed class LicenseDeviceActivationDto
{
    public long ActivationId { get; set; }

    public long LicenseId { get; set; }

    public long UserId { get; set; }

    public string UserFullName { get; set; } = string.Empty;

    public string UserLogin { get; set; } = string.Empty;

    public string DeviceId { get; set; } = string.Empty;

    public string MachineName { get; set; } = string.Empty;

    public DateTimeOffset ActivatedAt { get; set; }

    public DateTimeOffset LastSeenAt { get; set; }

    public bool IsRevoked { get; set; }
}
