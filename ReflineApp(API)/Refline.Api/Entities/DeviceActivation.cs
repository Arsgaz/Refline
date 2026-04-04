namespace Refline.Api.Entities;

public sealed class DeviceActivation
{
    public long Id { get; set; }

    public long LicenseId { get; set; }

    public License License { get; set; } = null!;

    public long UserId { get; set; }

    public User User { get; set; } = null!;

    public string DeviceId { get; set; } = string.Empty;

    public string MachineName { get; set; } = string.Empty;

    public DateTimeOffset ActivatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;

    public bool IsRevoked { get; set; }
}
