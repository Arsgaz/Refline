using Refline.Admin.Data.Infrastructure;
using Refline.Admin.Models;

namespace Refline.Admin.Services.Api;

public interface IAdminUserAnalyticsService
{
    Task<OperationResult<EmployeeAnalyticsSnapshot>> GetEmployeeAnalyticsAsync(
        long userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);
}
