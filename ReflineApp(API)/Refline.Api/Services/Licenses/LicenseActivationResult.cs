using Refline.Api.Contracts.Licenses;

namespace Refline.Api.Services.Licenses;

public sealed class LicenseActivationResult
{
    private LicenseActivationResult(
        LicenseActivationResultStatus status,
        ActivateLicenseResponse? response,
        string? errorMessage)
    {
        Status = status;
        Response = response;
        ErrorMessage = errorMessage;
    }

    public LicenseActivationResultStatus Status { get; }

    public ActivateLicenseResponse? Response { get; }

    public string? ErrorMessage { get; }

    public static LicenseActivationResult Success(ActivateLicenseResponse response)
        => new(LicenseActivationResultStatus.Success, response, null);

    public static LicenseActivationResult Failure(
        LicenseActivationResultStatus status,
        string errorMessage)
        => new(status, null, errorMessage);
}
