namespace Refline.Models;

public class LocalActivationState
{
    public Guid? CurrentUserId { get; set; }
    public string CurrentLicenseKey { get; set; } = string.Empty;
    public string CurrentDeviceId { get; set; } = string.Empty;
    public bool IsActivated { get; set; }
    public DateTime? LastValidatedAt { get; set; }

    public static LocalActivationState Empty()
    {
        return new LocalActivationState
        {
            CurrentUserId = null,
            CurrentLicenseKey = string.Empty,
            CurrentDeviceId = string.Empty,
            IsActivated = false,
            LastValidatedAt = null
        };
    }
}
