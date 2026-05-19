using Refline.Data.Infrastructure;

namespace Refline.Services.ActivitySync;

public interface IActivitySyncService
{
    Task<OperationResult<int>> TrySyncPendingAsync(CancellationToken cancellationToken = default);
}
