namespace Refline.Admin.Models;

public sealed class AdminUser
{
    public long Id { get; init; }

    public long CompanyId { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string Login { get; init; } = string.Empty;

    public UserRole Role { get; init; }

    public bool MustChangePassword { get; set; }
}
