using Refline.Api.Enums;

namespace Refline.Api.Contracts.Admin;

public sealed class CreateAdminUserRequestDto
{
    public string FullName { get; set; } = string.Empty;

    public string Login { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public long? ManagerId { get; set; }
}
