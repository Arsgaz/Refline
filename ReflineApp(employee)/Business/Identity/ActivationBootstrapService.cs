using Refline.Data.Identity;
using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Business.Identity;

public class ActivationBootstrapService : IActivationBootstrapService
{
    private readonly ILocalActivationStateStore _activationStateStore;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ICurrentUserSessionStore _currentUserSessionStore;

    public ActivationBootstrapService(
        ILocalActivationStateStore activationStateStore,
        ICurrentUserContext currentUserContext,
        ICurrentUserSessionStore currentUserSessionStore)
    {
        _activationStateStore = activationStateStore;
        _currentUserContext = currentUserContext;
        _currentUserSessionStore = currentUserSessionStore;
    }

    public async Task<OperationResult<LocalActivationState>> BootstrapAsync()
    {
        var stateResult = await _activationStateStore.LoadAsync();
        if (!stateResult.IsSuccess || stateResult.Value == null)
        {
            _currentUserContext.Clear();
            await _currentUserSessionStore.ClearAsync();
            return OperationResult<LocalActivationState>.Success(LocalActivationState.Empty(), "Локальное состояние активации не задано.");
        }

        var state = stateResult.Value;
        if (!state.IsActivated || !state.CurrentUserId.HasValue)
        {
            _currentUserContext.Clear();
            await _currentUserSessionStore.ClearAsync();
            return OperationResult<LocalActivationState>.Success(state);
        }

        _currentUserContext.SetCurrentUser(state.CurrentUserId.Value);

        var restoreSessionResult = await _currentUserSessionStore.RestoreAsync();
        if (!restoreSessionResult.IsSuccess)
        {
            await _currentUserSessionStore.ClearAsync();
            return OperationResult<LocalActivationState>.Success(state, restoreSessionResult.Message);
        }

        var sessionUser = _currentUserSessionStore.GetCurrentUser();
        if (sessionUser == null || sessionUser.Id != state.CurrentUserId.Value)
        {
            await _currentUserSessionStore.ClearAsync();
        }

        return OperationResult<LocalActivationState>.Success(state);
    }
}
