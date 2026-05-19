namespace Refline.Admin.Models;

public sealed class CreatedUserCredentials
{
    public long UserId { get; init; }

    public string Login { get; init; } = string.Empty;

    public string TemporaryPassword { get; init; } = string.Empty;

    public UserRole Role { get; init; }

    public bool MustChangePassword { get; init; }
}
