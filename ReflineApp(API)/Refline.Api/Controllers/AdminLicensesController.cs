using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Refline.Api.Contracts.Admin;
using Refline.Api.Enums;
using Refline.Api.Services.Admin;
using Refline.Api.Services.Licenses;

namespace Refline.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/licenses")]
public sealed class AdminLicensesController(
    IAdminAccessService adminAccessService,
    LicenseDeviceManagementService licenseDeviceManagementService,
    ILogger<AdminLicensesController> logger) : ControllerBase
{
    [HttpGet("devices")]
    public async Task<ActionResult<IReadOnlyList<LicenseDeviceActivationDto>>> GetLicenseDevices(
        CancellationToken cancellationToken)
    {
        var accessContextResult = await adminAccessService.ResolveAccessContextAsync(HttpContext, cancellationToken);
        if (!accessContextResult.IsSuccess)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = accessContextResult.ErrorMessage });
        }

        var accessContext = accessContextResult.Context!;
        if (accessContext.Role != UserRole.Admin)
        {
            logger.LogWarning(
                "Rejected license devices request: requesting user {RequestingUserId} is not Admin.",
                accessContext.UserId);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Only Admin can view license devices." });
        }

        var devices = await licenseDeviceManagementService.GetCompanyLicenseDevicesAsync(
            accessContext.CompanyId,
            cancellationToken);

        return Ok(devices);
    }

    [HttpPost("devices/{activationId:long}/revoke")]
    public async Task<IActionResult> RevokeDevice(
        long activationId,
        CancellationToken cancellationToken)
    {
        var accessContextResult = await adminAccessService.ResolveAccessContextAsync(HttpContext, cancellationToken);
        if (!accessContextResult.IsSuccess)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = accessContextResult.ErrorMessage });
        }

        var accessContext = accessContextResult.Context!;
        if (accessContext.Role != UserRole.Admin)
        {
            logger.LogWarning(
                "Rejected device revoke request: requesting user {RequestingUserId} is not Admin.",
                accessContext.UserId);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Only Admin can revoke license devices." });
        }

        var revoked = await licenseDeviceManagementService.RevokeDeviceActivationAsync(
            accessContext.CompanyId,
            activationId,
            cancellationToken);

        if (!revoked)
        {
            return NotFound(new { message = "Device activation was not found." });
        }

        return Ok(new { message = "Device activation revoked." });
    }
}
