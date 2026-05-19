namespace Refline.Api.Services.Licenses;

public sealed class CurrentDeviceActivationStatusRequest
{
    public long UserId { get; set; }

    public string LicenseKey { get; set; } = string.Empty;

    public string DeviceId { get; set; } = string.Empty;
}
