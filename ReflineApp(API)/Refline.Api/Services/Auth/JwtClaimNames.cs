using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Refline.Api.Services.Auth;

public static class JwtClaimNames
{
    public const string UserId = "userId";
    public const string CompanyId = "companyId";
    public const string Login = "login";

    public static readonly string[] UserIdCandidates =
    [
        UserId,
        ClaimTypes.NameIdentifier,
        JwtRegisteredClaimNames.Sub
    ];

    public static readonly string[] CompanyIdCandidates = [CompanyId];

    public static readonly string[] LoginCandidates =
    [
        Login,
        ClaimTypes.Name,
        JwtRegisteredClaimNames.UniqueName
    ];
}
