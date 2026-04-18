namespace Refline.Api.Services.Auth;

public sealed class AuthResult<T>
{
    private AuthResult(AuthResultStatus status, T? response, string? errorMessage)
    {
        Status = status;
        Response = response;
        ErrorMessage = errorMessage;
    }

    public AuthResultStatus Status { get; }

    public T? Response { get; }

    public string? ErrorMessage { get; }

    public static AuthResult<T> Success(T response)
        => new(AuthResultStatus.Success, response, null);

    public static AuthResult<T> InvalidCredentials(string errorMessage)
        => new(AuthResultStatus.InvalidCredentials, default, errorMessage);

    public static AuthResult<T> InactiveUser(string errorMessage)
        => new(AuthResultStatus.InactiveUser, default, errorMessage);

    public static AuthResult<T> UserNotFound(string errorMessage)
        => new(AuthResultStatus.UserNotFound, default, errorMessage);

    public static AuthResult<T> ValidationFailed(string errorMessage)
        => new(AuthResultStatus.ValidationFailed, default, errorMessage);

    public static AuthResult<T> Forbidden(string errorMessage)
        => new(AuthResultStatus.Forbidden, default, errorMessage);

    public static AuthResult<T> TokenExpired(string errorMessage)
        => new(AuthResultStatus.TokenExpired, default, errorMessage);
}
