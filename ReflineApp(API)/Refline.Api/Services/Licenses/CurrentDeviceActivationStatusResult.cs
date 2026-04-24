using Refline.Api.Contracts.Licenses;

namespace Refline.Api.Services.Licenses;

public sealed class CurrentDeviceActivationStatusResult
{
    private CurrentDeviceActivationStatusResult(
        CurrentDeviceActivationStatusResultStatus status,
        CurrentDeviceActivationStatusResponse? response,
        string? errorMessage)
    {
        Status = status;
        Response = response;
        ErrorMessage = errorMessage;
    }

    public CurrentDeviceActivationStatusResultStatus Status { get; }

    public CurrentDeviceActivationStatusResponse? Response { get; }

    public string? ErrorMessage { get; }

    public static CurrentDeviceActivationStatusResult Success(CurrentDeviceActivationStatusResponse response)
    {
        return new CurrentDeviceActivationStatusResult(CurrentDeviceActivationStatusResultStatus.Success, response, null);
    }

    public static CurrentDeviceActivationStatusResult Failure(
        CurrentDeviceActivationStatusResultStatus status,
        string errorMessage)
    {
        return new CurrentDeviceActivationStatusResult(status, null, errorMessage);
    }
}
