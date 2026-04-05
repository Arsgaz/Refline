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
            Role = user.Role
        });
    }
}
