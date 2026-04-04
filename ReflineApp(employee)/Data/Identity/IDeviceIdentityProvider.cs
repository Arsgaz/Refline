using Refline.Data.Infrastructure;

namespace Refline.Data.Identity;

public interface IDeviceIdentityProvider
{
    Task<OperationResult<string>> GetOrCreateDeviceIdAsync();
}
