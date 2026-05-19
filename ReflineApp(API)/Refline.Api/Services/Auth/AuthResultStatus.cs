namespace Refline.Api.Services.Auth;

public enum AuthResultStatus
{
    Success,
    InvalidCredentials,
    InactiveUser,
    UserNotFound,
    ValidationFailed,
    Forbidden,
    TokenExpired
}
