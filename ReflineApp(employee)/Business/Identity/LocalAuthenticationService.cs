using Refline.Data.Identity;
using Refline.Data.Infrastructure;
using Refline.Models;
using Refline.Utils;

namespace Refline.Business.Identity;

public class LocalAuthenticationService : IAuthenticationService
{
    private readonly IUserStore _userStore;
    private readonly ICurrentUserContext _currentUserContext;

    public LocalAuthenticationService(IUserStore userStore, ICurrentUserContext currentUserContext)
    {
        _userStore = userStore;
        _currentUserContext = currentUserContext;
    }

    public Task<OperationResult<User?>> GetUserByLoginAsync(string login)
    {
        return _userStore.GetByLoginAsync(login);
    }

    public async Task<OperationResult<bool>> ValidateCredentialsAsync(string login, string password)
    {
        var userResult = await _userStore.GetByLoginAsync(login);
        if (!userResult.IsSuccess)
        {
            return OperationResult<bool>.Failure(userResult.Message, userResult.ErrorCode);
        }

        var user = userResult.Value;
        if (user == null || !user.IsActive)
        {
            _currentUserContext.Clear();
            return OperationResult<bool>.Success(false, "Пользователь не найден или неактивен.");
        }

        var isValid = PasswordHashHelper.Verify(password, user.PasswordHash);
        if (isValid)
        {
            _currentUserContext.SetCurrentUser(user.Id);
        }
        else
        {
            _currentUserContext.Clear();
        }

        return OperationResult<bool>.Success(isValid, isValid ? "OK" : "Неверный логин или пароль.");
    }

    public async Task<OperationResult<User?>> GetCurrentUserAsync()
    {
        var currentUserId = _currentUserContext.GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return OperationResult<User?>.Success(null);
        }

        var userResult = await _userStore.GetByIdAsync(currentUserId.Value);
        if (!userResult.IsSuccess)
        {
            return OperationResult<User?>.Failure(userResult.Message, userResult.ErrorCode);
        }

        return OperationResult<User?>.Success(userResult.Value);
    }

    public Task<OperationResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        return Task.FromResult(OperationResult.Failure("Локальная смена пароля не поддерживается."));
    }
}
