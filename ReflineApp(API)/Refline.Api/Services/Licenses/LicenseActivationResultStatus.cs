namespace Refline.Api.Services.Licenses;

public enum LicenseActivationResultStatus
{
    Success,
    UserNotFound,
    UserInactive,
    LicenseNotFound,
    LicenseInactive,
    LicenseExpired,
    CompanyMismatch,
    ActivationRevoked,
    DeviceAssignedToAnotherUser,
    DeviceLimitReached
}
