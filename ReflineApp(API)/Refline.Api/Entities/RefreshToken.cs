namespace Refline.Api.Entities;

public sealed class RefreshToken
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public User User { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? RevokedAt { get; set; }

    public long? ReplacedByTokenId { get; set; }

    public RefreshToken? ReplacedByToken { get; set; }

    public long? ReplacedTokenId { get; set; }

    public RefreshToken? ReplacedToken { get; set; }

    public bool IsRevoked => RevokedAt.HasValue;
}
