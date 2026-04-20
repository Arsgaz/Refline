using Refline.Admin.Data.Infrastructure;
using Refline.Admin.Models;

namespace Refline.Admin.Business.Identity;

public interface IAuthenticationService
{
    Task<OperationResult<AdminUser>> LoginAsync(string login, string password, CancellationToken cancellationToken = default);

    Task<OperationResult> LogoutAsync(CancellationToken cancellationToken = default);

    Task<OperationResult> ChangePasswordAsync(
        long userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);
}
