namespace Refline.Business.Identity;

public enum CurrentActivationValidationStatus
{
    Valid = 0,
    NotActivated = 1,
    Revoked = 2,
    Unavailable = 3
}
