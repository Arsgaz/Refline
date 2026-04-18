using Microsoft.AspNetCore.Mvc;
using Refline.Api.Contracts.Auth;
using Refline.Api.Services.Auth;

namespace Refline.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService authService) : ControllerBase
{
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

    [HttpPost("change-password")]
    public async Task<ActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.ChangePasswordAsync(request, cancellationToken);

        return result.Status switch
        {
            AuthResultStatus.Success => Ok(new { message = "Password changed successfully." }),
            AuthResultStatus.ValidationFailed => BadRequest(new { message = result.ErrorMessage }),
            AuthResultStatus.UserNotFound => NotFound(new { message = result.ErrorMessage }),
            AuthResultStatus.InactiveUser => StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = result.ErrorMessage }),
            _ => Unauthorized(new { message = result.ErrorMessage })
        };
    }
}
