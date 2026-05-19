using Refline.Admin.Data.Infrastructure;

namespace Refline.Admin.Business.Identity;

public interface ICurrentSessionStateStore
{
    Task<OperationResult<AdminSessionState?>> LoadAsync();
    Task<OperationResult> SaveAsync(AdminSessionState state);
    Task<OperationResult> ClearAsync();
}
