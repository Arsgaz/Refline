namespace Refline.Api.Services.Licenses;

public enum CurrentDeviceActivationStatusResultStatus
{
    Success = 0,
    UserNotFound = 1,
    UserInactive = 2,
    LicenseNotFound = 3,
    CompanyMismatch = 4,
    ActivationNotFound = 5
}
