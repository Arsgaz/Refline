namespace Refline.Api.Services.Auth;

public sealed class RequestUserContextResult
{
    private RequestUserContextResult(bool isSuccess, RequestUserContext? context, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Context = context;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }

    public RequestUserContext? Context { get; }

    public string? ErrorMessage { get; }

    public static RequestUserContextResult Success(RequestUserContext context) => new(true, context, null);

    public static RequestUserContextResult Failure(string errorMessage) => new(false, null, errorMessage);
}
