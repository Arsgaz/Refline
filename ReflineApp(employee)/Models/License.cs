namespace Refline.Models;

public class License
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string LicenseKey { get; set; } = string.Empty;
    public int MaxDevices { get; set; }
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddYears(1);
    public bool IsActive { get; set; } = true;
}
