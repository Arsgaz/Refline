using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Business.Identity;

public interface ILicenseActivationService
{
    Task<OperationResult<License?>> ValidateLicenseKeyAsync(string key);
    Task<OperationResult<DeviceActivation>> ActivateAsync(Guid userId, string licenseKey);
    Task<OperationResult<LocalActivationState>> GetLocalActivationStateAsync();
    Task<OperationResult<bool>> IsActivatedAsync();
}
