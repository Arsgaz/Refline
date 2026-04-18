using Microsoft.AspNetCore.Mvc;
using Refline.Api.Contracts.InternalCompanies;
using Refline.Api.Services.Internal;
using Refline.Api.Services.InternalCompanies;

namespace Refline.Api.Controllers;

[ApiController]
[Route("api/internal/companies")]
public sealed class InternalCompaniesController(
    InternalApiAuthorizationService internalApiAuthorizationService,
    CompanyProvisioningService companyProvisioningService,
    ILogger<InternalCompaniesController> logger) : ControllerBase
{
    [HttpPost("provision")]
    public async Task<ActionResult<ProvisionCompanyResponseDto>> Provision(
        [FromBody] ProvisionCompanyRequestDto request,
        CancellationToken cancellationToken)
    {
        var authorizationResult = internalApiAuthorizationService.Authorize(HttpContext);
        if (!authorizationResult.IsAuthorized)
        {
            logger.LogWarning(
                "Rejected internal company provisioning request for company name {CompanyName}: {Reason}",
                request.CompanyName,
                authorizationResult.ErrorMessage);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = authorizationResult.ErrorMessage });
        }

        var result = await companyProvisioningService.ProvisionAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status201Created, result.Response);
        }

        return result.ErrorType switch
        {
            CompanyProvisioningErrorType.Validation => BadRequest(new { message = result.ErrorMessage }),
            CompanyProvisioningErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { message = result.ErrorMessage }),
            CompanyProvisioningErrorType.NotFound => NotFound(new { message = result.ErrorMessage }),
            CompanyProvisioningErrorType.Conflict => Conflict(new { message = result.ErrorMessage }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = "Unexpected company provisioning error." })
        };
    }
}
