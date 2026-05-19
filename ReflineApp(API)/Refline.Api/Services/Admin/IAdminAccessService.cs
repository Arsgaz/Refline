using Refline.Api.Entities;

namespace Refline.Api.Services.Admin;

public interface IAdminAccessService
{
    Task<AdminAccessContextResult> ResolveAccessContextAsync(HttpContext httpContext, CancellationToken cancellationToken);

    bool CanViewCompanyUsers(AdminAccessContext context, long companyId);

    Task<bool> CanViewUserAsync(AdminAccessContext context, long targetUserId, CancellationToken cancellationToken);
}

public sealed record AdminAccessContext(
    long UserId,
    long CompanyId,
    Enums.UserRole Role);

public sealed record AdminAccessContextResult(
    AdminAccessContext? Context,
    string? ErrorMessage)
{
    public bool IsSuccess => Context is not null;

    public static AdminAccessContextResult Success(AdminAccessContext context)
    {
        return new AdminAccessContextResult(context, null);
    }

    public static AdminAccessContextResult Failure(string errorMessage)
    {
        return new AdminAccessContextResult(null, errorMessage);
    }
}
