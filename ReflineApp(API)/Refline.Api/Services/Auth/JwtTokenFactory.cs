using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Refline.Api.Entities;

namespace Refline.Api.Services.Auth;

public sealed class JwtTokenFactory(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _options = options.Value;

    public JwtAccessTokenResult CreateAccessToken(User user)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.AccessTokenLifetimeMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtClaimNames.UserId, user.Id.ToString()),
            new(JwtClaimNames.CompanyId, user.CompanyId.ToString()),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(JwtClaimNames.Login, user.Login),
            new(ClaimTypes.Name, user.Login),
            new(JwtRegisteredClaimNames.UniqueName, user.Login)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new JwtAccessTokenResult(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt);
    }
}

public sealed record JwtAccessTokenResult(string Token, DateTimeOffset ExpiresAt);
