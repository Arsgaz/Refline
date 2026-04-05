namespace Refline.Admin.Models;

public sealed class AdminUserCreateRequest
{
    public string FullName { get; init; } = string.Empty;

    public string Login { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public UserRole Role { get; init; }

    public long? ManagerId { get; init; }
}
