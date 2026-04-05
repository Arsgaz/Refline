using Microsoft.EntityFrameworkCore;
using Refline.Api.Data;
using Refline.Api.Entities;
using Refline.Api.Enums;
using Refline.Api.Services.Auth;

namespace Refline.Api.Services.Admin;

public sealed class AdminAccessService(
    ReflineDbContext dbContext,
    IRequestUserContextService requestUserContextService) : IAdminAccessService
{
    public async Task<AdminAccessContextResult> ResolveAccessContextAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        var requestUserResult = await requestUserContextService.ResolveAsync(httpContext, cancellationToken);
        if (!requestUserResult.IsSuccess)
        {
            return AdminAccessContextResult.Failure(requestUserResult.ErrorMessage!);
        }

        var requestingUser = requestUserResult.Context!;

        if (requestingUser.Role == UserRole.Employee)
        {
            return AdminAccessContextResult.Failure("Employee role is not allowed to access admin endpoints.");
        }

        return AdminAccessContextResult.Success(new AdminAccessContext(
            requestingUser.UserId,
            requestingUser.CompanyId,
            requestingUser.Role));
    }

    public bool CanViewCompanyUsers(AdminAccessContext context, long companyId)
    {
        return context.CompanyId == companyId;
    }

    public async Task<bool> CanViewUserAsync(AdminAccessContext context, long targetUserId, CancellationToken cancellationToken)
    {
        IQueryable<User> query = dbContext.Users
            .AsNoTracking()
            .Where(user =>
                user.Id == targetUserId &&
                user.CompanyId == context.CompanyId);

        if (context.Role == UserRole.Manager)
        {
            query = query.Where(user => user.Id == context.UserId || user.ManagerId == context.UserId);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
