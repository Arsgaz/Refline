namespace Refline.Admin.Models;

public sealed class LicenseDeviceActivation
{
    public long ActivationId { get; init; }

    public long LicenseId { get; init; }

    public long UserId { get; init; }

    public string UserFullName { get; init; } = string.Empty;

    public string UserLogin { get; init; } = string.Empty;

    public string DeviceId { get; init; } = string.Empty;

    public string MachineName { get; init; } = string.Empty;

    public DateTimeOffset ActivatedAt { get; init; }

    public DateTimeOffset LastSeenAt { get; init; }

    public bool IsRevoked { get; init; }

    public bool CanRevoke => !IsRevoked;

    public string StatusDisplay => IsRevoked ? "Отозвано" : "Активно";

    public string ActivatedAtDisplay => ActivatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");

    public string LastSeenAtDisplay => LastSeenAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
}
