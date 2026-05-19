using Refline.Api.Enums;

namespace Refline.Api.Contracts.InternalCompanies;

public sealed class ProvisionCompanyResponseDto
{
    public long CompanyId { get; set; }

    public string CompanyName { get; set; } = string.Empty;

    public long AdminUserId { get; set; }

    public string AdminLogin { get; set; } = string.Empty;

    public string TemporaryPassword { get; set; } = string.Empty;

    public long LicenseId { get; set; }

    public string LicenseKey { get; set; } = string.Empty;

    public LicenseType LicenseType { get; set; }
}
