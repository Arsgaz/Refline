namespace Refline.Api.Services.Admin;

public enum ActivityClassificationRuleManagementErrorType
{
    Validation = 1,
    Forbidden = 2,
    NotFound = 3
}

public sealed class ActivityClassificationRuleManagementResult<T>
{
    private ActivityClassificationRuleManagementResult(
        bool isSuccess,
        T? value,
        ActivityClassificationRuleManagementErrorType? errorType,
        string? errorMessage)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorType = errorType;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }

    public T? Value { get; }

    public ActivityClassificationRuleManagementErrorType? ErrorType { get; }

    public string? ErrorMessage { get; }

    public static ActivityClassificationRuleManagementResult<T> Success(T value)
    {
        return new ActivityClassificationRuleManagementResult<T>(true, value, null, null);
    }

    public static ActivityClassificationRuleManagementResult<T> Failure(
        ActivityClassificationRuleManagementErrorType errorType,
        string errorMessage)
    {
        return new ActivityClassificationRuleManagementResult<T>(false, default, errorType, errorMessage);
    }
}
