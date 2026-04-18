using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Refline.Api.Contracts.Licenses;
using Refline.Api.Services.Auth;
using Refline.Api.Services.Licenses;

namespace Refline.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/licenses")]
public sealed class LicensesController(
    LicenseActivationService licenseActivationService,
    IRequestUserContextService requestUserContextService) : ControllerBase
{
    [HttpPost("activate")]
    public async Task<ActionResult<ActivateLicenseResponse>> Activate(
        [FromBody] ActivateLicenseRequest request,
        CancellationToken cancellationToken)
    {
        var requestUserResult = await requestUserContextService.ResolveAsync(HttpContext, cancellationToken);
        if (!requestUserResult.IsSuccess)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = requestUserResult.ErrorMessage });
        }

        if (request.UserId > 0 && request.UserId != requestUserResult.Context!.UserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "License activation user does not match current token." });
        }

        request.UserId = requestUserResult.Context!.UserId;
        var result = await licenseActivationService.ActivateAsync(request, cancellationToken);

        return result.Status switch
        {
            LicenseActivationResultStatus.Success => Ok(result.Response),
            LicenseActivationResultStatus.UserNotFound => NotFound(new { message = result.ErrorMessage }),
            LicenseActivationResultStatus.UserInactive => StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = result.ErrorMessage }),
            LicenseActivationResultStatus.LicenseNotFound => NotFound(new { message = result.ErrorMessage }),
            LicenseActivationResultStatus.LicenseInactive => BadRequest(new { message = result.ErrorMessage }),
            LicenseActivationResultStatus.LicenseExpired => BadRequest(new { message = result.ErrorMessage }),
            LicenseActivationResultStatus.CompanyMismatch => BadRequest(new { message = result.ErrorMessage }),
            LicenseActivationResultStatus.ActivationRevoked => Conflict(new { message = result.ErrorMessage }),
            LicenseActivationResultStatus.DeviceAssignedToAnotherUser => Conflict(new { message = result.ErrorMessage }),
            LicenseActivationResultStatus.DeviceLimitReached => Conflict(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }
}
