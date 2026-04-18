using Refline.Admin.Data.Infrastructure;
using Refline.Admin.Models;

namespace Refline.Admin.Business.Identity;

public sealed class CurrentSessionContext
{
    private readonly ICurrentSessionStateStore _sessionStateStore;

    public CurrentSessionContext(ICurrentSessionStateStore sessionStateStore)
    {
        _sessionStateStore = sessionStateStore;
    }

    public AdminUser? CurrentUser { get; private set; }

    public AdminSessionState? CurrentSession { get; private set; }

    public bool IsAuthenticated => CurrentUser is not null;

    public long CompanyId => CurrentUser?.CompanyId ?? 0;

    public UserRole? Role => CurrentUser?.Role;

    public async Task<OperationResult> RestoreAsync()
    {
        var loadResult = await _sessionStateStore.LoadAsync();
        if (!loadResult.IsSuccess)
        {
            CurrentUser = null;
            CurrentSession = null;
            return OperationResult.Failure(loadResult.Message, loadResult.ErrorCode);
        }

        CurrentSession = loadResult.Value;
        CurrentUser = CurrentSession?.ToUser();
        return OperationResult.Success();
    }

    public async Task<OperationResult> SetSessionAsync(AdminUser user, ApiTokenSet tokens)
    {
        var state = AdminSessionState.From(user, tokens);
        var saveResult = await _sessionStateStore.SaveAsync(state);
        if (!saveResult.IsSuccess)
        {
            CurrentUser = null;
            CurrentSession = null;
            return OperationResult.Failure(saveResult.Message, saveResult.ErrorCode);
        }

        CurrentUser = user;
        CurrentSession = state;
        return OperationResult.Success();
    }

    public async Task<OperationResult> UpdateTokensAsync(ApiTokenSet tokens)
    {
        if (CurrentUser == null)
        {
            return OperationResult.Failure("Сессия администратора не найдена.", "ADMIN_SESSION_MISSING");
        }

        return await SetSessionAsync(CurrentUser, tokens);
    }

    public async Task<OperationResult> ClearAsync()
    {
        CurrentUser = null;
        CurrentSession = null;
        var clearResult = await _sessionStateStore.ClearAsync();
        return clearResult.IsSuccess
            ? OperationResult.Success()
            : OperationResult.Failure(clearResult.Message, clearResult.ErrorCode);
    }
}
