using Microsoft.AspNetCore.Mvc;
using Refline.Api.Contracts.Admin;
using Refline.Api.Services.Admin;

namespace Refline.Api.Controllers;

[ApiController]
[Route("api/admin/companies")]
public sealed class AdminCompaniesController(
    IAdminAccessService adminAccessService,
    AdminAnalyticsService adminAnalyticsService,
    ILogger<AdminCompaniesController> logger) : ControllerBase
{
    [HttpGet("{companyId:long}/users")]
    public async Task<ActionResult<IReadOnlyList<CompanyUserListItemDto>>> GetCompanyUsers(
        long companyId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Admin users list requested for company {CompanyId}.", companyId);

        var accessContextResult = await adminAccessService.ResolveAccessContextAsync(HttpContext, cancellationToken);
        if (!accessContextResult.IsSuccess)
        {
            logger.LogWarning(
                "Rejected admin users list request for company {CompanyId}: {Reason}",
                companyId,
                accessContextResult.ErrorMessage);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = accessContextResult.ErrorMessage });
        }

        var accessContext = accessContextResult.Context!;
        var companyExists = await adminAnalyticsService.CompanyExistsAsync(companyId, cancellationToken);
        if (!companyExists)
        {
            return NotFound(new { message = $"Company with id {companyId} was not found." });
        }

        if (!adminAccessService.CanViewCompanyUsers(accessContext, companyId))
        {
            logger.LogWarning(
                "Rejected admin users list request: requesting user {RequestingUserId} cannot access company {CompanyId}.",
                accessContext.UserId,
                companyId);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Access to the requested company is forbidden." });
        }

        long? managerFilter = accessContext.Role == Enums.UserRole.Manager
            ? accessContext.UserId
            : null;

        var users = await adminAnalyticsService.GetCompanyUsersAsync(companyId, managerFilter, cancellationToken);
        return Ok(users);
    }
}
