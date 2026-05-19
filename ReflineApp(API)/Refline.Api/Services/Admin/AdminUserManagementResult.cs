namespace Refline.Api.Services.Admin;

public enum AdminUserManagementErrorType
{
    Validation = 1,
    Forbidden = 2,
    NotFound = 3,
    Conflict = 4
}

public sealed class AdminUserManagementResult<T>
{
    private AdminUserManagementResult(bool isSuccess, T? value, AdminUserManagementErrorType? errorType, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorType = errorType;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }

    public T? Value { get; }

    public AdminUserManagementErrorType? ErrorType { get; }

    public string? ErrorMessage { get; }

    public static AdminUserManagementResult<T> Success(T value)
    {
        return new AdminUserManagementResult<T>(true, value, null, null);
    }

    public static AdminUserManagementResult<T> Failure(AdminUserManagementErrorType errorType, string errorMessage)
    {
        return new AdminUserManagementResult<T>(false, default, errorType, errorMessage);
    }
}
