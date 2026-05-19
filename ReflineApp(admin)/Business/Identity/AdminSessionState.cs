using Refline.Admin.Models;

namespace Refline.Admin.Business.Identity;

public sealed class AdminSessionState
{
    public long UserId { get; set; }
    public long CompanyId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool MustChangePassword { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public DateTimeOffset AccessTokenExpiresAt { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset RefreshTokenExpiresAt { get; set; }

    public AdminUser ToUser()
    {
        return new AdminUser
        {
            Id = UserId,
            CompanyId = CompanyId,
            FullName = FullName,
            Login = Login,
            Role = Role,
            MustChangePassword = MustChangePassword
        };
    }

    public static AdminSessionState From(AdminUser user, ApiTokenSet tokens)
    {
        return new AdminSessionState
        {
            UserId = user.Id,
            CompanyId = user.CompanyId,
            FullName = user.FullName,
            Login = user.Login,
            Role = user.Role,
            MustChangePassword = user.MustChangePassword,
            AccessToken = tokens.AccessToken,
            AccessTokenExpiresAt = tokens.AccessTokenExpiresAt,
            RefreshToken = tokens.RefreshToken,
            RefreshTokenExpiresAt = tokens.RefreshTokenExpiresAt
        };
    }
}
