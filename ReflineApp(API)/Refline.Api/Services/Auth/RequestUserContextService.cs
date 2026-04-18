using System.Security.Claims;

namespace Refline.Api.Services.Auth;

public sealed class RequestUserContextService : IRequestUserContextService
{
    public async Task<RequestUserContextResult> ResolveAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        var principal = httpContext.User;
        if (principal.Identity?.IsAuthenticated != true)
        {
            return RequestUserContextResult.Failure("Authenticated user context is missing.");
        }

        if (!TryReadLongClaim(principal, JwtClaimNames.UserIdCandidates, out var userId) || userId <= 0)
        {
            return RequestUserContextResult.Failure("JWT claim 'userId' is missing or invalid.");
        }

        if (!TryReadLongClaim(principal, JwtClaimNames.CompanyIdCandidates, out var companyId) || companyId <= 0)
        {
            return RequestUserContextResult.Failure("JWT claim 'companyId' is missing or invalid.");
        }

        var roleValue = principal.FindFirstValue(ClaimTypes.Role);
        if (!Enum.TryParse<Enums.UserRole>(roleValue, ignoreCase: true, out var role))
        {
            return RequestUserContextResult.Failure("JWT role claim is missing or invalid.");
        }

        var login = FindFirstValue(principal, JwtClaimNames.LoginCandidates);
        if (string.IsNullOrWhiteSpace(login))
        {
            return RequestUserContextResult.Failure("JWT claim 'login' is missing or invalid.");
        }

        return RequestUserContextResult.Success(new RequestUserContext(userId, companyId, role, login));
    }

    private static bool TryReadLongClaim(ClaimsPrincipal principal, IEnumerable<string> claimTypes, out long value)
    {
        var rawValue = FindFirstValue(principal, claimTypes);
        return long.TryParse(rawValue, out value);
    }

    private static string? FindFirstValue(ClaimsPrincipal principal, IEnumerable<string> claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = principal.FindFirstValue(claimType);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
