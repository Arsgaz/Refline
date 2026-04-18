namespace Refline.Admin.Models;

public sealed class ResetPasswordResult
{
    public long UserId { get; init; }

    public string Login { get; init; } = string.Empty;

    public string TemporaryPassword { get; init; } = string.Empty;

    public bool MustChangePassword { get; init; }
}
