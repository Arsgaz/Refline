using Microsoft.AspNetCore.Mvc;
using Refline.Api.Contracts.Admin;
using Refline.Api.Services.Admin;

namespace Refline.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
public sealed class AdminUsersController(
    IAdminAccessService adminAccessService,
    AdminAnalyticsService adminAnalyticsService,
    AdminUserManagementService adminUserManagementService,
    ILogger<AdminUsersController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreateAdminUserResponseDto>> CreateUser(
        [FromBody] CreateAdminUserRequestDto request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Admin user create requested for login {Login}.", request.Login);

        var accessContextResult = await adminAccessService.ResolveAccessContextAsync(HttpContext, cancellationToken);
        if (!accessContextResult.IsSuccess)
        {
            logger.LogWarning(
                "Rejected admin user create request for login {Login}: {Reason}",
                request.Login,
                accessContextResult.ErrorMessage);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = accessContextResult.ErrorMessage });
        }

        var result = await adminUserManagementService.CreateUserAsync(accessContextResult.Context!, request, cancellationToken);
        return ToActionResult(result, StatusCodes.Status201Created);
    }

    [HttpPost("{userId:long}/reset-password")]
    public async Task<ActionResult<ResetPasswordResponseDto>> ResetPassword(
        long userId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Admin password reset requested for user {UserId}.", userId);

        var accessContextResult = await adminAccessService.ResolveAccessContextAsync(HttpContext, cancellationToken);
        if (!accessContextResult.IsSuccess)
        {
            logger.LogWarning(
                "Rejected admin password reset request for user {UserId}: {Reason}",
                userId,
                accessContextResult.ErrorMessage);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = accessContextResult.ErrorMessage });
        }

        var result = await adminUserManagementService.ResetPasswordAsync(accessContextResult.Context!, userId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut("{userId:long}")]
    public async Task<ActionResult<AdminManagedUserDto>> UpdateUser(
        long userId,
        [FromBody] UpdateAdminUserRequestDto request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Admin user update requested for user {UserId}.", userId);

        var accessContextResult = await adminAccessService.ResolveAccessContextAsync(HttpContext, cancellationToken);
        if (!accessContextResult.IsSuccess)
        {
            logger.LogWarning(
                "Rejected admin user update request for user {UserId}: {Reason}",
                userId,
                accessContextResult.ErrorMessage);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = accessContextResult.ErrorMessage });
        }

        var result = await adminUserManagementService.UpdateUserAsync(accessContextResult.Context!, userId, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("{userId:long}/deactivate")]
    public async Task<ActionResult<AdminManagedUserDto>> DeactivateUser(
        long userId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Admin user deactivate requested for user {UserId}.", userId);

        var accessContextResult = await adminAccessService.ResolveAccessContextAsync(HttpContext, cancellationToken);
        if (!accessContextResult.IsSuccess)
        {
            logger.LogWarning(
                "Rejected admin user deactivate request for user {UserId}: {Reason}",
                userId,
                accessContextResult.ErrorMessage);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = accessContextResult.ErrorMessage });
        }

        var result = await adminUserManagementService.DeactivateUserAsync(accessContextResult.Context!, userId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("{userId:long}/activate")]
    public async Task<ActionResult<AdminManagedUserDto>> ActivateUser(
        long userId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Admin user activate requested for user {UserId}.", userId);

        var accessContextResult = await adminAccessService.ResolveAccessContextAsync(HttpContext, cancellationToken);
        if (!accessContextResult.IsSuccess)
        {
            logger.LogWarning(
                "Rejected admin user activate request for user {UserId}: {Reason}",
                userId,
                accessContextResult.ErrorMessage);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = accessContextResult.ErrorMessage });
        }

        var result = await adminUserManagementService.ActivateUserAsync(accessContextResult.Context!, userId, cancellationToken);
        return ToActionResult(result);
    }

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

    private ActionResult<AdminManagedUserDto> ToActionResult(
        AdminUserManagementResult<AdminManagedUserDto> result,
        int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            return StatusCode(successStatusCode, result.Value);
        }

        return result.ErrorType switch
        {
            AdminUserManagementErrorType.Validation => BadRequest(new { message = result.ErrorMessage }),
            AdminUserManagementErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { message = result.ErrorMessage }),
            AdminUserManagementErrorType.NotFound => NotFound(new { message = result.ErrorMessage }),
            AdminUserManagementErrorType.Conflict => Conflict(new { message = result.ErrorMessage }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = "Unexpected user management error." })
        };
    }

    private ActionResult<CreateAdminUserResponseDto> ToActionResult(
        AdminUserManagementResult<CreateAdminUserResponseDto> result,
        int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            return StatusCode(successStatusCode, result.Value);
        }

        return result.ErrorType switch
        {
            AdminUserManagementErrorType.Validation => BadRequest(new { message = result.ErrorMessage }),
            AdminUserManagementErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { message = result.ErrorMessage }),
            AdminUserManagementErrorType.NotFound => NotFound(new { message = result.ErrorMessage }),
            AdminUserManagementErrorType.Conflict => Conflict(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage ?? "Request failed." })
        };
    }

    private ActionResult<ResetPasswordResponseDto> ToActionResult(
        AdminUserManagementResult<ResetPasswordResponseDto> result,
        int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            return StatusCode(successStatusCode, result.Value);
        }

        return result.ErrorType switch
        {
            AdminUserManagementErrorType.Validation => BadRequest(new { message = result.ErrorMessage }),
            AdminUserManagementErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { message = result.ErrorMessage }),
            AdminUserManagementErrorType.NotFound => NotFound(new { message = result.ErrorMessage }),
            AdminUserManagementErrorType.Conflict => Conflict(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage ?? "Request failed." })
        };
    }
}
