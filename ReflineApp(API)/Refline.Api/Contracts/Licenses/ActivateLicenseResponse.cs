namespace Refline.Api.Contracts.Licenses;

public sealed class ActivateLicenseResponse
{
    public long ActivationId { get; set; }

    public long LicenseId { get; set; }

    public long UserId { get; set; }

    public string DeviceId { get; set; } = string.Empty;

    public string MachineName { get; set; } = string.Empty;

    public DateTimeOffset ActivatedAt { get; set; }

    public DateTimeOffset LastSeenAt { get; set; }

    public bool IsRevoked { get; set; }
}
