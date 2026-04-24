namespace Refline.Business.Identity;

public sealed class CurrentActivationValidationResult
{
    public CurrentActivationValidationStatus Status { get; init; }

    public string Message { get; init; } = string.Empty;
}
