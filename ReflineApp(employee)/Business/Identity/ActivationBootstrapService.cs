using Refline.Data.Identity;
using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Business.Identity;

public class ActivationBootstrapService : IActivationBootstrapService
{
    private readonly ILocalActivationStateStore _activationStateStore;
    private readonly IUserStore _userStore;
    private readonly ICurrentUserContext _currentUserContext;

    public ActivationBootstrapService(
        ILocalActivationStateStore activationStateStore,
        IUserStore userStore,
        ICurrentUserContext currentUserContext)
    {
        _activationStateStore = activationStateStore;
        _userStore = userStore;
        _currentUserContext = currentUserContext;
    }

    public async Task<OperationResult<LocalActivationState>> BootstrapAsync()
    {
        var stateResult = await _activationStateStore.LoadAsync();
        if (!stateResult.IsSuccess || stateResult.Value == null)
        {
            _currentUserContext.Clear();
            return OperationResult<LocalActivationState>.Success(LocalActivationState.Empty(), "Локальное состояние активации не задано.");
        }

        var state = stateResult.Value;
        if (!state.IsActivated || !state.CurrentUserId.HasValue)
        {
            _currentUserContext.Clear();
            return OperationResult<LocalActivationState>.Success(state);
        }

        var userResult = await _userStore.GetByIdAsync(state.CurrentUserId.Value);
        if (!userResult.IsSuccess || userResult.Value == null || !userResult.Value.IsActive)
        {
            _currentUserContext.Clear();
            return OperationResult<LocalActivationState>.Success(LocalActivationState.Empty(), "Текущий пользователь не найден или неактивен.");
        }

        _currentUserContext.SetCurrentUser(userResult.Value.Id);
        return OperationResult<LocalActivationState>.Success(state);
    }
}
