namespace Refline.Admin.Business.Identity;

public sealed class ApiTokenSet
{
    public string AccessToken { get; init; } = string.Empty;

    public DateTimeOffset AccessTokenExpiresAt { get; init; }

    public string RefreshToken { get; init; } = string.Empty;

    public DateTimeOffset RefreshTokenExpiresAt { get; init; }
}
