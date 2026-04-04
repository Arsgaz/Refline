namespace Refline.Models;

public class DeviceActivation
{
    public Guid Id { get; set; }
    public Guid LicenseId { get; set; }
    public Guid UserId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string MachineName { get; set; } = Environment.MachineName;
    public DateTime ActivatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; }
}
