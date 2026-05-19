using Refline.Api.Enums;

namespace Refline.Api.Contracts.InternalCompanies;

public sealed class ProvisionCompanyRequestDto
{
    public string CompanyName { get; set; } = string.Empty;

    public string AdminFullName { get; set; } = string.Empty;

    public string AdminLogin { get; set; } = string.Empty;

    public LicenseType LicenseType { get; set; }
}
