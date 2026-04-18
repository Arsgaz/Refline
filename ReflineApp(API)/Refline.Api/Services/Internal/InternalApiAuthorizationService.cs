using Microsoft.Extensions.Options;

namespace Refline.Api.Services.Internal;

public sealed class InternalApiAuthorizationService(IOptions<InternalApiOptions> options)
{
    private readonly InternalApiOptions internalApiOptions = options.Value;

    public InternalApiAuthorizationResult Authorize(HttpContext httpContext)
    {
        if (string.IsNullOrWhiteSpace(internalApiOptions.ApiKey))
        {
            return InternalApiAuthorizationResult.Failure("Internal API key is not configured.");
        }

        if (!httpContext.Request.Headers.TryGetValue(InternalApiHeaders.ApiKey, out var headerValues))
        {
            return InternalApiAuthorizationResult.Failure(
                $"Missing required header '{InternalApiHeaders.ApiKey}'.");
        }

        if (!string.Equals(headerValues.ToString(), internalApiOptions.ApiKey, StringComparison.Ordinal))
        {
            return InternalApiAuthorizationResult.Failure("Invalid internal API key.");
        }

        return InternalApiAuthorizationResult.Success();
    }
}
