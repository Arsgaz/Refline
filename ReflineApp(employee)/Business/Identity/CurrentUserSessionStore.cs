using Refline.Data.Identity;
using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Business.Identity;

public sealed class CurrentUserSessionStore(ICurrentUserSessionStateStore sessionStateStore) : ICurrentUserSessionStore
{
    private User? _currentUser;
    private CurrentUserSessionState? _currentSession;

    public User? GetCurrentUser()
    {
        return _currentUser;
    }

    public CurrentUserSessionState? GetCurrentSession()
    {
        return _currentSession;
    }

    public async Task<OperationResult> SetCurrentUserAsync(User user, ApiTokenSet tokens)
    {
        var state = CurrentUserSessionState.FromUser(user, tokens);
        if (state == null)
        {
            _currentUser = null;
            _currentSession = null;
            return OperationResult.Failure("Не удалось сохранить пустую пользовательскую сессию.", "CURRENT_USER_SESSION_EMPTY");
        }

        var saveResult = await sessionStateStore.SaveAsync(state);
        if (!saveResult.IsSuccess)
        {
            _currentUser = null;
            _currentSession = null;
            return OperationResult.Failure(saveResult.Message, saveResult.ErrorCode);
        }

        _currentUser = user;
        _currentSession = state;
        return OperationResult.Success();
    }

    public async Task<OperationResult> UpdateTokensAsync(ApiTokenSet tokens)
    {
        if (_currentUser == null)
        {
            return OperationResult.Failure("Не удалось обновить токены без активной пользовательской сессии.", "CURRENT_USER_SESSION_EMPTY");
        }

        var state = CurrentUserSessionState.FromUser(_currentUser, tokens);
        if (state == null)
        {
            return OperationResult.Failure("Не удалось обновить токены пользовательской сессии.", "CURRENT_USER_SESSION_EMPTY");
        }

        var saveResult = await sessionStateStore.SaveAsync(state);
        if (!saveResult.IsSuccess)
        {
            return OperationResult.Failure(saveResult.Message, saveResult.ErrorCode);
        }

        _currentSession = state;
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

        _currentSession = loadResult.Value;
        _currentUser = loadResult.Value?.ToUser();
        return OperationResult.Success();
    }

    public async Task<OperationResult> ClearAsync()
    {
        _currentUser = null;
        _currentSession = null;
        var clearResult = await sessionStateStore.ClearAsync();
        return clearResult.IsSuccess
            ? OperationResult.Success()
            : OperationResult.Failure(clearResult.Message, clearResult.ErrorCode);
    }
}
