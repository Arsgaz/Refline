using Refline.Data.Identity;
using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Business.Identity;

public sealed class CurrentUserSessionStore(ICurrentUserSessionStateStore sessionStateStore) : ICurrentUserSessionStore
{
    private User? _currentUser;

    public User? GetCurrentUser()
    {
        return _currentUser;
    }

    public async Task<OperationResult> SetCurrentUserAsync(User user)
    {
        var state = CurrentUserSessionState.FromUser(user);
        if (state == null)
        {
            _currentUser = null;
            return OperationResult.Failure("Не удалось сохранить пустую пользовательскую сессию.", "CURRENT_USER_SESSION_EMPTY");
        }

        var saveResult = await sessionStateStore.SaveAsync(state);
        if (!saveResult.IsSuccess)
        {
            _currentUser = null;
            return OperationResult.Failure(saveResult.Message, saveResult.ErrorCode);
        }

        _currentUser = user;
        return OperationResult.Success();
    }

    public async Task<OperationResult> RestoreAsync()
    {
        var loadResult = await sessionStateStore.LoadAsync();
        if (!loadResult.IsSuccess)
        {
            _currentUser = null;
            return OperationResult.Failure(loadResult.Message, loadResult.ErrorCode);
        }

        _currentUser = loadResult.Value?.ToUser();
        return OperationResult.Success();
    }

    public async Task<OperationResult> ClearAsync()
    {
        _currentUser = null;
        var clearResult = await sessionStateStore.ClearAsync();
        return clearResult.IsSuccess
            ? OperationResult.Success()
            : OperationResult.Failure(clearResult.Message, clearResult.ErrorCode);
    }
}
