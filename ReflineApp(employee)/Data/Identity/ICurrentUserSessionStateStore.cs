using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Data.Identity;

public interface ICurrentUserSessionStateStore
{
    Task<OperationResult<CurrentUserSessionState?>> LoadAsync();
    Task<OperationResult> SaveAsync(CurrentUserSessionState state);
    Task<OperationResult> ClearAsync();
}
