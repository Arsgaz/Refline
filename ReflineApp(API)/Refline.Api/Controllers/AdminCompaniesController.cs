using Microsoft.AspNetCore.Mvc;
using Refline.Api.Contracts.Admin;
using Refline.Api.Services.Admin;

namespace Refline.Api.Controllers;

[ApiController]
[Route("api/admin/companies")]
public sealed class AdminCompaniesController(
    AdminAnalyticsService adminAnalyticsService,
    ILogger<AdminCompaniesController> logger) : ControllerBase
{
    [HttpGet("{companyId:long}/users")]
    public async Task<ActionResult<IReadOnlyList<CompanyUserListItemDto>>> GetCompanyUsers(
        long companyId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Admin users list requested for company {CompanyId}.", companyId);

        var companyExists = await adminAnalyticsService.CompanyExistsAsync(companyId, cancellationToken);
        if (!companyExists)
        {
            return NotFound(new { message = $"Company with id {companyId} was not found." });
        }

        var users = await adminAnalyticsService.GetCompanyUsersAsync(companyId, cancellationToken);
        return Ok(users);
    }
}
