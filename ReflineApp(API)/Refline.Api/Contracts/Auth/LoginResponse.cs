using Refline.Api.Enums;

namespace Refline.Api.Contracts.Auth;

public sealed class LoginResponse
{
    public long UserId { get; set; }

    public long CompanyId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Login { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public bool MustChangePassword { get; set; }
}
