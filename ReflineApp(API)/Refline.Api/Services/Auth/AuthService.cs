using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Refline.Api.Contracts.Auth;
using Refline.Api.Data;
using Refline.Api.Entities;
using Refline.Api.Security;

namespace Refline.Api.Services.Auth;

public sealed class AuthService(
    ReflineDbContext dbContext,
    IRequestUserContextService requestUserContextService,
    JwtTokenFactory jwtTokenFactory,
    IOptions<JwtOptions> jwtOptions)
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<AuthResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var login = (request.Login ?? string.Empty).Trim();

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(currentUser => currentUser.Login == login, cancellationToken);

        if (user is null)
        {
            return AuthResult<LoginResponse>.InvalidCredentials("Invalid login or password.");
        }

        if (!user.IsActive)
        {
            return AuthResult<LoginResponse>.InactiveUser("User account is inactive.");
        }

        if (!PasswordHashHelper.Verify(request.Password, user.PasswordHash))
        {
            return AuthResult<LoginResponse>.InvalidCredentials("Invalid login or password.");
        }

        var response = await CreateLoginResponseAsync(user, cancellationToken);
        return AuthResult<LoginResponse>.Success(response);
    }

    public async Task<AuthResult<RefreshTokenResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var refreshToken = (request.RefreshToken ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return AuthResult<RefreshTokenResponse>.ValidationFailed("RefreshToken is required.");
        }

        var tokenHash = RefreshTokenFactory.ComputeHash(refreshToken);
        var existingToken = await dbContext.RefreshTokens
            .Include(token => token.User)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (existingToken is null || existingToken.User is null)
        {
            return AuthResult<RefreshTokenResponse>.InvalidCredentials("Refresh token is invalid.");
        }

        if (existingToken.IsRevoked)
        {
            return AuthResult<RefreshTokenResponse>.InvalidCredentials("Refresh token is revoked.");
        }

        if (existingToken.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            existingToken.RevokedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return AuthResult<RefreshTokenResponse>.TokenExpired("Refresh token has expired.");
        }

        if (!existingToken.User.IsActive)
        {
            existingToken.RevokedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return AuthResult<RefreshTokenResponse>.InactiveUser("User account is inactive.");
        }

        var rotatedToken = await RotateRefreshTokenAsync(existingToken, cancellationToken);
        var accessToken = jwtTokenFactory.CreateAccessToken(existingToken.User);

        return AuthResult<RefreshTokenResponse>.Success(new RefreshTokenResponse
        {
            AccessToken = accessToken.Token,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            RefreshToken = rotatedToken.PlainTextToken,
            RefreshTokenExpiresAt = rotatedToken.Entity.ExpiresAt
        });
    }

    public async Task<AuthResult<object>> RevokeRefreshTokenAsync(RevokeRefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var refreshToken = (request.RefreshToken ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return AuthResult<object>.ValidationFailed("RefreshToken is required.");
        }

        var tokenHash = RefreshTokenFactory.ComputeHash(refreshToken);
        var existingToken = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (existingToken is null)
        {
            return AuthResult<object>.Success(new object());
        }

        if (!existingToken.IsRevoked)
        {
            existingToken.RevokedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return AuthResult<object>.Success(new object());
    }

    public async Task<AuthResult<object>> ChangePasswordAsync(
        HttpContext httpContext,
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            return AuthResult<object>.ValidationFailed("CurrentPassword is required.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return AuthResult<object>.ValidationFailed("NewPassword is required.");
        }

        if (!PasswordPolicy.IsValid(request.NewPassword, out var passwordValidationError))
        {
            return AuthResult<object>.ValidationFailed(passwordValidationError!);
        }

        if (string.Equals(request.CurrentPassword, request.NewPassword, StringComparison.Ordinal))
        {
            return AuthResult<object>.ValidationFailed("New password must be different from the current password.");
        }

        var requestUserResult = await requestUserContextService.ResolveAsync(httpContext, cancellationToken);
        if (!requestUserResult.IsSuccess)
        {
            return AuthResult<object>.Forbidden(requestUserResult.ErrorMessage!);
        }

        var user = await dbContext.Users
            .SingleOrDefaultAsync(currentUser => currentUser.Id == requestUserResult.Context!.UserId, cancellationToken);

        if (user is null)
        {
            return AuthResult<object>.UserNotFound("User not found.");
        }

        if (!user.IsActive)
        {
            return AuthResult<object>.InactiveUser("User account is inactive.");
        }

        if (!PasswordHashHelper.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return AuthResult<object>.InvalidCredentials("Current password is incorrect.");
        }

        user.PasswordHash = PasswordHashHelper.ComputeHash(request.NewPassword);
        user.MustChangePassword = false;

        await dbContext.SaveChangesAsync(cancellationToken);

        return AuthResult<object>.Success(new object());
    }

    private async Task<LoginResponse> CreateLoginResponseAsync(User user, CancellationToken cancellationToken)
    {
        var accessToken = jwtTokenFactory.CreateAccessToken(user);
        var refreshToken = await CreateRefreshTokenAsync(user.Id, cancellationToken);

        return new LoginResponse
        {
            AccessToken = accessToken.Token,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            RefreshToken = refreshToken.PlainTextToken,
            RefreshTokenExpiresAt = refreshToken.Entity.ExpiresAt,
            UserId = user.Id,
            CompanyId = user.CompanyId,
            FullName = user.FullName,
            Login = user.Login,
            Role = user.Role,
            MustChangePassword = user.MustChangePassword
        };
    }

    private async Task<(RefreshToken Entity, string PlainTextToken)> CreateRefreshTokenAsync(long userId, CancellationToken cancellationToken)
    {
        var plainTextToken = RefreshTokenFactory.CreateToken();
        var entity = RefreshTokenFactory.CreateEntity(
            userId,
            plainTextToken,
            DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenLifetimeDays));

        dbContext.RefreshTokens.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (entity, plainTextToken);
    }

    private async Task<(RefreshToken Entity, string PlainTextToken)> RotateRefreshTokenAsync(RefreshToken existingToken, CancellationToken cancellationToken)
    {
        var plainTextToken = RefreshTokenFactory.CreateToken();
        var replacementToken = RefreshTokenFactory.CreateEntity(
            existingToken.UserId,
            plainTextToken,
            DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenLifetimeDays));

        existingToken.RevokedAt = DateTimeOffset.UtcNow;
        existingToken.ReplacedByToken = replacementToken;

        dbContext.RefreshTokens.Add(replacementToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (replacementToken, plainTextToken);
    }

}
