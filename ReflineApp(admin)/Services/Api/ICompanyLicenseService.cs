using Refline.Admin.Data.Infrastructure;
using Refline.Admin.Models;

namespace Refline.Admin.Services.Api;

public interface ICompanyLicenseService
{
    Task<OperationResult<CompanyLicense?>> GetCompanyLicenseAsync(long companyId, CancellationToken cancellationToken = default);
}
