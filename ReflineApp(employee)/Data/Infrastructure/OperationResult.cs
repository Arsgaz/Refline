namespace Refline.Data.Infrastructure;

public class OperationResult
{
    public bool IsSuccess { get; }
    public string Message { get; }
    public string? ErrorCode { get; }

    protected OperationResult(bool isSuccess, string message, string? errorCode = null)
    {
        IsSuccess = isSuccess;
        Message = message;
        ErrorCode = errorCode;
    }

    public static OperationResult Success(string message = "OK") => new(true, message);

    public static OperationResult Failure(string message, string? errorCode = null) => new(false, message, errorCode);
}

public sealed class OperationResult<T> : OperationResult
{
    public T? Value { get; }

    private OperationResult(bool isSuccess, T? value, string message, string? errorCode = null)
        : base(isSuccess, message, errorCode)
    {
        Value = value;
    }

    public new static OperationResult<T> Success(T value, string message = "OK")
        => new(true, value, message);

    public new static OperationResult<T> Failure(string message, string? errorCode = null)
        => new(false, default, message, errorCode);
}