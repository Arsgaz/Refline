using Refline.Admin.Data.Infrastructure;
using Refline.Admin.Models;

namespace Refline.Admin.Services.Api;

public interface IAdminUsersService
{
    Task<OperationResult<IReadOnlyList<CompanyUserListItem>>> GetCompanyUsersAsync(long companyId, CancellationToken cancellationToken = default);

    Task<OperationResult<CompanyUserListItem>> CreateUserAsync(AdminUserCreateRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult<CompanyUserListItem>> UpdateUserAsync(long userId, AdminUserUpdateRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult> DeactivateUserAsync(long userId, CancellationToken cancellationToken = default);

    Task<OperationResult> ActivateUserAsync(long userId, CancellationToken cancellationToken = default);
}
