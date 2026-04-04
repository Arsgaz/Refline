using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Data.Identity;

public interface IDeviceActivationStore
{
    Task<OperationResult<DeviceActivation?>> GetByLicenseAndDeviceAsync(Guid licenseId, string deviceId);
    Task<OperationResult<int>> CountActiveByLicenseAsync(Guid licenseId);
    Task<OperationResult<IReadOnlyList<DeviceActivation>>> GetAllAsync();
    Task<OperationResult> SaveAsync(DeviceActivation activation);
}
