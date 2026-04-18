namespace Refline.Api.Services.Internal;

public sealed class InternalApiAuthorizationResult
{
    private InternalApiAuthorizationResult(bool isAuthorized, string? errorMessage)
    {
        IsAuthorized = isAuthorized;
        ErrorMessage = errorMessage;
    }

    public bool IsAuthorized { get; }

    public string? ErrorMessage { get; }

    public static InternalApiAuthorizationResult Success()
    {
        return new InternalApiAuthorizationResult(true, null);
    }

    public static InternalApiAuthorizationResult Failure(string errorMessage)
    {
        return new InternalApiAuthorizationResult(false, errorMessage);
    }
}
