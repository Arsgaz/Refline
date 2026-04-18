using Microsoft.AspNetCore.Mvc;
using Refline.Api.Contracts.Common;
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
    [ProducesResponseType(typeof(ProvisionCompanyResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status500InternalServerError)]
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
            return StatusCode(StatusCodes.Status403Forbidden, ErrorResponse(authorizationResult.ErrorMessage));
        }

        var result = await companyProvisioningService.ProvisionAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status201Created, result.Response);
        }

        return result.ErrorType switch
        {
            CompanyProvisioningErrorType.Validation => BadRequest(ErrorResponse(result.ErrorMessage)),
            CompanyProvisioningErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, ErrorResponse(result.ErrorMessage)),
            CompanyProvisioningErrorType.NotFound => NotFound(ErrorResponse(result.ErrorMessage)),
            CompanyProvisioningErrorType.Conflict => Conflict(ErrorResponse(result.ErrorMessage)),
            _ => StatusCode(StatusCodes.Status500InternalServerError, ErrorResponse("Unexpected company provisioning error."))
        };
    }

    private static ApiErrorResponseDto ErrorResponse(string? message)
    {
        return new ApiErrorResponseDto
        {
            Message = message ?? string.Empty
        };
    }
}
