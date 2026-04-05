using Microsoft.AspNetCore.Mvc;
using Refline.Api.Contracts.Admin;
using Refline.Api.Services.Admin;

namespace Refline.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
public sealed class AdminUsersController(
    IAdminAccessService adminAccessService,
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

        var accessContextResult = await adminAccessService.ResolveAccessContextAsync(HttpContext, cancellationToken);
        if (!accessContextResult.IsSuccess)
        {
            logger.LogWarning(
                "Rejected admin user summary request for user {UserId}: {Reason}",
                userId,
                accessContextResult.ErrorMessage);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = accessContextResult.ErrorMessage });
        }

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

        var accessContext = accessContextResult.Context!;
        var canViewUser = await adminAccessService.CanViewUserAsync(accessContext, userId, cancellationToken);
        if (!canViewUser)
        {
            logger.LogWarning(
                "Rejected admin user summary request: requesting user {RequestingUserId} cannot access target user {TargetUserId}.",
                accessContext.UserId,
                userId);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Access to the requested user is forbidden." });
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

        var accessContextResult = await adminAccessService.ResolveAccessContextAsync(HttpContext, cancellationToken);
        if (!accessContextResult.IsSuccess)
        {
            logger.LogWarning(
                "Rejected admin user activity breakdown request for user {UserId}: {Reason}",
                userId,
                accessContextResult.ErrorMessage);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = accessContextResult.ErrorMessage });
        }

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

        var accessContext = accessContextResult.Context!;
        var canViewUser = await adminAccessService.CanViewUserAsync(accessContext, userId, cancellationToken);
        if (!canViewUser)
        {
            logger.LogWarning(
                "Rejected admin user activity breakdown request: requesting user {RequestingUserId} cannot access target user {TargetUserId}.",
                accessContext.UserId,
                userId);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Access to the requested user is forbidden." });
        }

        var breakdown = await adminAnalyticsService.GetUserActivityBreakdownAsync(userId, from.Value, to.Value, cancellationToken);
        return Ok(breakdown);
    }
}
