using Refline.Api.Enums;

namespace Refline.Api.Contracts.Admin;

public sealed record CreateAdminUserResponseDto(
    long UserId,
    string Login,
    string TemporaryPassword,
    UserRole Role,
    bool MustChangePassword);
