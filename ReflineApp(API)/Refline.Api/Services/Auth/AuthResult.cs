using Refline.Api.Contracts.Auth;

namespace Refline.Api.Services.Auth;

public sealed class AuthResult
{
    private AuthResult(AuthResultStatus status, LoginResponse? response, string? errorMessage)
    {
        Status = status;
        Response = response;
        ErrorMessage = errorMessage;
    }

    public AuthResultStatus Status { get; }

    public LoginResponse? Response { get; }

    public string? ErrorMessage { get; }

    public static AuthResult Success(LoginResponse response)
        => new(AuthResultStatus.Success, response, null);

    public static AuthResult InvalidCredentials(string errorMessage)
        => new(AuthResultStatus.InvalidCredentials, null, errorMessage);

    public static AuthResult InactiveUser(string errorMessage)
        => new(AuthResultStatus.InactiveUser, null, errorMessage);

    public static AuthResult UserNotFound(string errorMessage)
        => new(AuthResultStatus.UserNotFound, null, errorMessage);

    public static AuthResult ValidationFailed(string errorMessage)
        => new(AuthResultStatus.ValidationFailed, null, errorMessage);
}
