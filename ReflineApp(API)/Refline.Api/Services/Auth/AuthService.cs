using Microsoft.EntityFrameworkCore;
using Refline.Api.Contracts.Auth;
using Refline.Api.Data;
using Refline.Api.Security;

namespace Refline.Api.Services.Auth;

public sealed class AuthService(ReflineDbContext dbContext)
{
    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var login = (request.Login ?? string.Empty).Trim();

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(currentUser => currentUser.Login == login, cancellationToken);

        if (user is null)
        {
            return AuthResult.InvalidCredentials("Invalid login or password.");
        }

        if (!user.IsActive)
        {
            return AuthResult.InactiveUser("User account is inactive.");
        }

        if (!PasswordHashHelper.Verify(request.Password, user.PasswordHash))
        {
            return AuthResult.InvalidCredentials("Invalid login or password.");
        }

        return AuthResult.Success(new LoginResponse
        {
            UserId = user.Id,
            CompanyId = user.CompanyId,
            FullName = user.FullName,
            Login = user.Login,
            Role = user.Role,
            MustChangePassword = user.MustChangePassword
        });
    }

    public async Task<AuthResult> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (request.UserId <= 0)
        {
            return AuthResult.ValidationFailed("UserId must be a valid positive value.");
        }

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            return AuthResult.ValidationFailed("CurrentPassword is required.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return AuthResult.ValidationFailed("NewPassword is required.");
        }

        if (string.Equals(request.CurrentPassword, request.NewPassword, StringComparison.Ordinal))
        {
            return AuthResult.ValidationFailed("New password must be different from the current password.");
        }

        var user = await dbContext.Users
            .SingleOrDefaultAsync(currentUser => currentUser.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return AuthResult.UserNotFound("User not found.");
        }

        if (!user.IsActive)
        {
            return AuthResult.InactiveUser("User account is inactive.");
        }

        if (!PasswordHashHelper.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return AuthResult.InvalidCredentials("Current password is incorrect.");
        }

        user.PasswordHash = PasswordHashHelper.ComputeHash(request.NewPassword);
        user.MustChangePassword = false;

        await dbContext.SaveChangesAsync(cancellationToken);

        return AuthResult.Success(new LoginResponse
        {
            UserId = user.Id,
            CompanyId = user.CompanyId,
            FullName = user.FullName,
            Login = user.Login,
            Role = user.Role,
            MustChangePassword = user.MustChangePassword
        });
    }
}
