using Refline.Admin.Data.Infrastructure;
using Refline.Admin.Models;

namespace Refline.Admin.Services.Api;

public interface ITeamDashboardService
{
    Task<OperationResult<TeamDashboardSnapshot>> GetDashboardAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);
}
