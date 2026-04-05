namespace Refline.Admin.Data.Infrastructure;

public class OperationResult
{
    protected OperationResult(bool isSuccess, string message, string? errorCode = null)
    {
        IsSuccess = isSuccess;
        Message = message;
        ErrorCode = errorCode;
    }

    public bool IsSuccess { get; }

    public string Message { get; }

    public string? ErrorCode { get; }

    public static OperationResult Success(string message = "")
    {
        return new OperationResult(true, message);
    }

    public static OperationResult Failure(string message, string? errorCode = null)
    {
        return new OperationResult(false, message, errorCode);
    }
}

public sealed class OperationResult<T> : OperationResult
{
    private OperationResult(bool isSuccess, T? value, string message, string? errorCode = null)
        : base(isSuccess, message, errorCode)
    {
        Value = value;
    }

    public T? Value { get; }

    public static OperationResult<T> Success(T? value, string message = "")
    {
        return new OperationResult<T>(true, value, message);
    }

    public static new OperationResult<T> Failure(string message, string? errorCode = null)
    {
        return new OperationResult<T>(false, default, message, errorCode);
    }
}
