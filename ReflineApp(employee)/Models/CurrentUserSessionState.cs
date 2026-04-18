namespace Refline.Models;

public sealed class CurrentUserSessionState
{
    public Guid UserId { get; set; }
    public Guid CompanyId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Employee;
    public bool MustChangePassword { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public DateTimeOffset AccessTokenExpiresAt { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset RefreshTokenExpiresAt { get; set; }

    public static CurrentUserSessionState? FromUser(User? user, Business.Identity.ApiTokenSet? tokens = null)
    {
        if (user == null)
        {
            return null;
        }

        return new CurrentUserSessionState
        {
            UserId = user.Id,
            CompanyId = user.CompanyId,
            FullName = user.FullName,
            Login = user.Login,
            Role = user.Role,
            MustChangePassword = user.MustChangePassword,
            AccessToken = tokens?.AccessToken ?? string.Empty,
            AccessTokenExpiresAt = tokens?.AccessTokenExpiresAt ?? DateTimeOffset.MinValue,
            RefreshToken = tokens?.RefreshToken ?? string.Empty,
            RefreshTokenExpiresAt = tokens?.RefreshTokenExpiresAt ?? DateTimeOffset.MinValue
        };
    }

    public User ToUser()
    {
        return new User
        {
            Id = UserId,
            CompanyId = CompanyId,
            FullName = FullName,
            Login = Login,
            Role = Role,
            MustChangePassword = MustChangePassword,
            IsActive = true
        };
    }
}
