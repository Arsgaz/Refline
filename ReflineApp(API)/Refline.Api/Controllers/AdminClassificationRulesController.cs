using Microsoft.AspNetCore.Mvc;
using Refline.Api.Contracts.Admin;
using Refline.Api.Services.Admin;

namespace Refline.Api.Controllers;

[ApiController]
[Route("api/admin/classification-rules")]
public sealed class AdminClassificationRulesController(
    IAdminAccessService adminAccessService,
    AdminClassificationRuleManagementService classificationRuleManagementService,
    ILogger<AdminClassificationRulesController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ActivityClassificationRuleDto>> CreateRule(
        [FromBody] CreateActivityClassificationRuleRequestDto request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Admin classification rule create requested for company {CompanyId}.",
            request.CompanyId);

        var accessContextResult = await adminAccessService.ResolveAccessContextAsync(HttpContext, cancellationToken);
        if (!accessContextResult.IsSuccess)
        {
            logger.LogWarning(
                "Rejected admin classification rule create request for company {CompanyId}: {Reason}",
                request.CompanyId,
                accessContextResult.ErrorMessage);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = accessContextResult.ErrorMessage });
        }

        var result = await classificationRuleManagementService.CreateRuleAsync(
            accessContextResult.Context!,
            request,
            cancellationToken);

        return ToActionResult(result, StatusCodes.Status201Created);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ActivityClassificationRuleDto>> UpdateRule(
        long id,
        [FromBody] UpdateActivityClassificationRuleRequestDto request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Admin classification rule update requested for rule {RuleId}.", id);

        var accessContextResult = await adminAccessService.ResolveAccessContextAsync(HttpContext, cancellationToken);
        if (!accessContextResult.IsSuccess)
        {
            logger.LogWarning(
                "Rejected admin classification rule update request for rule {RuleId}: {Reason}",
                id,
                accessContextResult.ErrorMessage);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = accessContextResult.ErrorMessage });
        }

        var result = await classificationRuleManagementService.UpdateRuleAsync(
            accessContextResult.Context!,
            id,
            request,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("{id:long}/toggle")]
    public async Task<ActionResult<ActivityClassificationRuleDto>> ToggleRule(
        long id,
        [FromBody] ToggleActivityClassificationRuleRequestDto request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Admin classification rule toggle requested for rule {RuleId}.", id);

        var accessContextResult = await adminAccessService.ResolveAccessContextAsync(HttpContext, cancellationToken);
        if (!accessContextResult.IsSuccess)
        {
            logger.LogWarning(
                "Rejected admin classification rule toggle request for rule {RuleId}: {Reason}",
                id,
                accessContextResult.ErrorMessage);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = accessContextResult.ErrorMessage });
        }

        var result = await classificationRuleManagementService.ToggleRuleAsync(
            accessContextResult.Context!,
            id,
            request,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteRule(
        long id,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Admin classification rule delete requested for rule {RuleId}.", id);

        var accessContextResult = await adminAccessService.ResolveAccessContextAsync(HttpContext, cancellationToken);
        if (!accessContextResult.IsSuccess)
        {
            logger.LogWarning(
                "Rejected admin classification rule delete request for rule {RuleId}: {Reason}",
                id,
                accessContextResult.ErrorMessage);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = accessContextResult.ErrorMessage });
        }

        var result = await classificationRuleManagementService.DeleteRuleAsync(
            accessContextResult.Context!,
            id,
            cancellationToken);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        return result.ErrorType switch
        {
            ActivityClassificationRuleManagementErrorType.Validation => BadRequest(new { message = result.ErrorMessage }),
            ActivityClassificationRuleManagementErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { message = result.ErrorMessage }),
            ActivityClassificationRuleManagementErrorType.NotFound => NotFound(new { message = result.ErrorMessage }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = "Unexpected classification rule delete error." })
        };
    }

    private ActionResult<ActivityClassificationRuleDto> ToActionResult(
        ActivityClassificationRuleManagementResult<ActivityClassificationRuleDto> result,
        int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            return StatusCode(successStatusCode, result.Value);
        }

        return result.ErrorType switch
        {
            ActivityClassificationRuleManagementErrorType.Validation => BadRequest(new { message = result.ErrorMessage }),
            ActivityClassificationRuleManagementErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { message = result.ErrorMessage }),
            ActivityClassificationRuleManagementErrorType.NotFound => NotFound(new { message = result.ErrorMessage }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = "Unexpected classification rule management error." })
        };
    }
}
