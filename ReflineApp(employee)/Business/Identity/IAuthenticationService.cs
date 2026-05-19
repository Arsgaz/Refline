using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Business.Identity;

public interface IAuthenticationService
{
    Task<OperationResult<User?>> GetUserByLoginAsync(string login);
    Task<OperationResult<bool>> ValidateCredentialsAsync(string login, string password);
    Task<OperationResult<User?>> GetCurrentUserAsync();
    Task<OperationResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
}
