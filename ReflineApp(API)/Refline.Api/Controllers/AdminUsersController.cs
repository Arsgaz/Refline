using Microsoft.AspNetCore.Mvc;
using Refline.Api.Contracts.Admin;
using Refline.Api.Services.Admin;

namespace Refline.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
public sealed class AdminUsersController(
    AdminAnalyticsService adminAnalyticsService,
    ILogger<AdminUsersController> logger) : ControllerBase
{
    [HttpGet("{userId:long}/summary")]
    public async Task<ActionResult<UserSummaryDto>> GetUserSummary(
        long userId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Admin user summary requested for user {UserId} from {From} to {To}.",
            userId,
            from,
            to);

        if (from is null || to is null)
        {
            return BadRequest(new { message = "Both 'from' and 'to' query parameters are required." });
        }

        var validationError = AdminAnalyticsPeriodValidator.Validate(from.Value, to.Value);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var userExists = await adminAnalyticsService.UserExistsAsync(userId, cancellationToken);
        if (!userExists)
        {
            return NotFound(new { message = $"User with id {userId} was not found." });
        }

        var summary = await adminAnalyticsService.GetUserSummaryAsync(userId, from.Value, to.Value, cancellationToken);
        return Ok(summary);
    }

    [HttpGet("{userId:long}/activity-breakdown")]
    public async Task<ActionResult<UserActivityBreakdownDto>> GetUserActivityBreakdown(
        long userId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Admin user activity breakdown requested for user {UserId} from {From} to {To}.",
            userId,
            from,
            to);

        if (from is null || to is null)
        {
            return BadRequest(new { message = "Both 'from' and 'to' query parameters are required." });
        }

        var validationError = AdminAnalyticsPeriodValidator.Validate(from.Value, to.Value);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var userExists = await adminAnalyticsService.UserExistsAsync(userId, cancellationToken);
        if (!userExists)
        {
            return NotFound(new { message = $"User with id {userId} was not found." });
        }

        var breakdown = await adminAnalyticsService.GetUserActivityBreakdownAsync(userId, from.Value, to.Value, cancellationToken);
        return Ok(breakdown);
    }
}
