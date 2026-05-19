using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Business.Identity;

public interface ICurrentUserSessionStore
{
    User? GetCurrentUser();
    CurrentUserSessionState? GetCurrentSession();
    Task<OperationResult> SetCurrentUserAsync(User user, ApiTokenSet tokens);
    Task<OperationResult> UpdateTokensAsync(ApiTokenSet tokens);
    Task<OperationResult> RestoreAsync();
    Task<OperationResult> ClearAsync();
}
