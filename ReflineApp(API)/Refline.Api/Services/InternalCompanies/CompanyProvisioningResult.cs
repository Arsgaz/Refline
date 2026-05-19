using Refline.Api.Contracts.InternalCompanies;

namespace Refline.Api.Services.InternalCompanies;

public enum CompanyProvisioningErrorType
{
    Validation = 1,
    Forbidden = 2,
    NotFound = 3,
    Conflict = 4
}

public sealed class CompanyProvisioningResult
{
    private CompanyProvisioningResult(
        bool isSuccess,
        ProvisionCompanyResponseDto? response,
        CompanyProvisioningErrorType? errorType,
        string? errorMessage)
    {
        IsSuccess = isSuccess;
        Response = response;
        ErrorType = errorType;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }

    public ProvisionCompanyResponseDto? Response { get; }

    public CompanyProvisioningErrorType? ErrorType { get; }

    public string? ErrorMessage { get; }

    public static CompanyProvisioningResult Success(ProvisionCompanyResponseDto response)
    {
        return new CompanyProvisioningResult(true, response, null, null);
    }

    public static CompanyProvisioningResult Failure(
        CompanyProvisioningErrorType errorType,
        string errorMessage)
    {
        return new CompanyProvisioningResult(false, null, errorType, errorMessage);
    }
}
