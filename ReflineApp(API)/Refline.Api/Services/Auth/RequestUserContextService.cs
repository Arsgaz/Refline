using Microsoft.EntityFrameworkCore;
using Refline.Api.Data;
using Refline.Api.Services.Admin;

namespace Refline.Api.Services.Auth;

public sealed class RequestUserContextService(ReflineDbContext dbContext) : IRequestUserContextService
{
    public async Task<RequestUserContextResult> ResolveAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        if (!httpContext.Request.Headers.TryGetValue(AdminRequestHeaders.RequestingUserId, out var headerValues))
        {
            return RequestUserContextResult.Failure(
                $"Missing required header '{AdminRequestHeaders.RequestingUserId}'.");
        }

        if (!long.TryParse(headerValues.ToString(), out var requestingUserId) || requestingUserId <= 0)
        {
            return RequestUserContextResult.Failure(
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
            return RequestUserContextResult.Failure("Requesting user was not found or is inactive.");
        }

        return RequestUserContextResult.Success(new RequestUserContext(
            requestingUser.Id,
            requestingUser.CompanyId,
            requestingUser.Role));
    }
}
