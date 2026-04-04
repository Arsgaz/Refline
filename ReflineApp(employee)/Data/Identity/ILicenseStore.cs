using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Data.Identity;

public interface ILicenseStore
{
    Task<OperationResult<License?>> GetByKeyAsync(string licenseKey);
    Task<OperationResult<License?>> GetByIdAsync(Guid licenseId);
    Task<OperationResult<IReadOnlyList<License>>> GetAllAsync();
    Task<OperationResult> SaveAllAsync(IEnumerable<License> licenses);
}
