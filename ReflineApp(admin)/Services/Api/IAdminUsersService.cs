using Refline.Admin.Data.Infrastructure;
using Refline.Admin.Models;

namespace Refline.Admin.Services.Api;

public interface IAdminUsersService
{
    Task<OperationResult<IReadOnlyList<CompanyUserListItem>>> GetCompanyUsersAsync(long companyId, CancellationToken cancellationToken = default);
}
