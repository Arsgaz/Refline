using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Refline.Api.Contracts.Auth;
using Refline.Api.Services.Auth;

namespace Refline.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);

        return result.Status switch
        {
            AuthResultStatus.Success => Ok(result.Response),
            AuthResultStatus.InactiveUser => StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = result.ErrorMessage }),
            _ => Unauthorized(new { message = result.ErrorMessage })
        };
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<RefreshTokenResponse>> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.RefreshAsync(request, cancellationToken);

        return result.Status switch
        {
            AuthResultStatus.Success => Ok(result.Response),
            AuthResultStatus.ValidationFailed => BadRequest(new { message = result.ErrorMessage }),
            AuthResultStatus.InactiveUser => StatusCode(StatusCodes.Status403Forbidden, new { message = result.ErrorMessage }),
            AuthResultStatus.TokenExpired => Unauthorized(new { message = result.ErrorMessage }),
            _ => Unauthorized(new { message = result.ErrorMessage })
        };
    }

    [AllowAnonymous]
    [HttpPost("revoke")]
    public async Task<ActionResult> Revoke(
        [FromBody] RevokeRefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.RevokeRefreshTokenAsync(request, cancellationToken);

        return result.Status switch
        {
            AuthResultStatus.Success => Ok(new { message = "Refresh token revoked." }),
            AuthResultStatus.ValidationFailed => BadRequest(new { message = result.ErrorMessage }),
            _ => Unauthorized(new { message = result.ErrorMessage })
        };
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.ChangePasswordAsync(HttpContext, request, cancellationToken);

        return result.Status switch
        {
            AuthResultStatus.Success => Ok(new { message = "Password changed successfully." }),
            AuthResultStatus.ValidationFailed => BadRequest(new { message = result.ErrorMessage }),
            AuthResultStatus.UserNotFound => NotFound(new { message = result.ErrorMessage }),
            AuthResultStatus.Forbidden => StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = result.ErrorMessage }),
            AuthResultStatus.InactiveUser => StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = result.ErrorMessage }),
            _ => Unauthorized(new { message = result.ErrorMessage })
        };
    }
}
