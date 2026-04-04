namespace Refline.Models;

public sealed class CurrentUserSessionState
{
    public Guid UserId { get; set; }
    public Guid CompanyId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Employee;

    public static CurrentUserSessionState? FromUser(User? user)
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
            Role = user.Role
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
            IsActive = true
        };
    }
}
