using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Business.Identity;

public interface ICurrentUserSessionStore
{
    User? GetCurrentUser();
    Task<OperationResult> SetCurrentUserAsync(User user);
    Task<OperationResult> RestoreAsync();
    Task<OperationResult> ClearAsync();
}
