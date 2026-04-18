using Microsoft.EntityFrameworkCore;
using Refline.Api.Contracts.Admin;
using Refline.Api.Data;
using Refline.Api.Enums;

namespace Refline.Api.Services.Admin;

public sealed class AdminCompanyLicenseService(ReflineDbContext dbContext)
{
    public Task<CompanyLicenseDto?> GetActiveCompanyLicenseAsync(long companyId, CancellationToken cancellationToken)
    {
        return dbContext.Licenses
            .AsNoTracking()
            .Where(license => license.CompanyId == companyId && license.IsActive)
            .OrderByDescending(license => license.IssuedAt)
            .ThenByDescending(license => license.Id)
            .Select(license => new CompanyLicenseDto
            {
                CompanyId = license.CompanyId,
                LicenseId = license.Id,
                LicenseKey = license.LicenseKey,
                LicenseType = license.LicenseType,
                IsActive = license.IsActive,
                IssuedAt = license.IssuedAt,
                ExpiresAt = license.ExpiresAt,
                MaxDevices = license.MaxDevices,
                ActivatedDevicesCount = license.DeviceActivations.Count(activation => !activation.IsRevoked),
                IsLifetime = license.LicenseType == LicenseType.Basic || license.ExpiresAt >= DateTimeOffset.MaxValue.AddDays(-1)
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
