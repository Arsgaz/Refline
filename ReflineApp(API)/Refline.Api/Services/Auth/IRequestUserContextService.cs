namespace Refline.Api.Services.Auth;

public interface IRequestUserContextService
{
    Task<RequestUserContextResult> ResolveAsync(HttpContext httpContext, CancellationToken cancellationToken);
}
