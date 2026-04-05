using Microsoft.EntityFrameworkCore;
using Refline.Api.Data;
using Refline.Api.Entities;
using Refline.Api.Enums;

namespace Refline.Api.Services.Admin;

public sealed class AdminAccessService(ReflineDbContext dbContext) : IAdminAccessService
{
    public async Task<AdminAccessContextResult> ResolveAccessContextAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        if (!httpContext.Request.Headers.TryGetValue(AdminRequestHeaders.RequestingUserId, out var headerValues))
        {
            return AdminAccessContextResult.Failure(
                $"Missing required header '{AdminRequestHeaders.RequestingUserId}'.");
        }

        if (!long.TryParse(headerValues.ToString(), out var requestingUserId) || requestingUserId <= 0)
        {
            return AdminAccessContextResult.Failure(
                $"Header '{AdminRequestHeaders.RequestingUserId}' must contain a valid positive user id.");
        }

        var requestingUser = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == requestingUserId && user.IsActive)
            .Select(user => new
            {
                user.Id,
                user.CompanyId,
                user.Role
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (requestingUser is null)
        {
            return AdminAccessContextResult.Failure("Requesting user was not found or is inactive.");
        }

        if (requestingUser.Role == UserRole.Employee)
        {
            return AdminAccessContextResult.Failure("Employee role is not allowed to access admin endpoints.");
        }

        return AdminAccessContextResult.Success(new AdminAccessContext(
            requestingUser.Id,
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
            query = query.Where(user => user.ManagerId == context.UserId);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
