using System.ComponentModel.DataAnnotations;

namespace Refline.Api.Contracts.Licenses;

public sealed class ActivateLicenseRequest
{
    [Range(1, long.MaxValue)]
    public long UserId { get; set; }

    [Required]
    [MaxLength(128)]
    public string LicenseKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string DeviceId { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string MachineName { get; set; } = string.Empty;
}
