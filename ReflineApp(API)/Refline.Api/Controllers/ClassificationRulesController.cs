using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Refline.Api.Contracts.ClassificationRules;
using Refline.Api.Services.Auth;
using Refline.Api.Services.ClassificationRules;

namespace Refline.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/classification-rules")]
public sealed class ClassificationRulesController(
    IRequestUserContextService requestUserContextService,
    ClassificationRuleReadService classificationRuleReadService,
    ILogger<ClassificationRulesController> logger) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<IReadOnlyList<EmployeeClassificationRuleDto>>> GetMyCompanyClassificationRules(
        CancellationToken cancellationToken)
    {
        var requestUserResult = await requestUserContextService.ResolveAsync(HttpContext, cancellationToken);
        if (!requestUserResult.IsSuccess)
        {
            logger.LogWarning(
                "Rejected classification rules request: {Reason}",
                requestUserResult.ErrorMessage);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = requestUserResult.ErrorMessage });
        }

        var requestUser = requestUserResult.Context!;
        logger.LogInformation(
            "Classification rules requested for current user {UserId} in company {CompanyId}.",
            requestUser.UserId,
            requestUser.CompanyId);

        var rules = await classificationRuleReadService.GetActiveRulesForCompanyAsync(
            requestUser.CompanyId,
            cancellationToken);

        return Ok(rules);
    }
}
