using Refline.Api.Enums;

namespace Refline.Api.Contracts.Admin;

public sealed record AdminManagedUserDto(
    long Id,
    long CompanyId,
    string FullName,
    string Login,
    UserRole Role,
    long? ManagerId,
    bool IsActive,
    DateTimeOffset CreatedAt);
