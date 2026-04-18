using System.Security.Cryptography;
using System.Text;
using Refline.Api.Entities;

namespace Refline.Api.Services.Auth;

public static class RefreshTokenFactory
{
    public static string CreateToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public static string ComputeHash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    public static RefreshToken CreateEntity(long userId, string plainTextToken, DateTimeOffset expiresAt)
    {
        return new RefreshToken
        {
            UserId = userId,
            TokenHash = ComputeHash(plainTextToken),
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
