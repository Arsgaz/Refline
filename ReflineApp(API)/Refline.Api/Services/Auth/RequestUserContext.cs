using Refline.Api.Enums;

namespace Refline.Api.Services.Auth;

public sealed record RequestUserContext(
    long UserId,
    long CompanyId,
    UserRole Role,
    string Login);
