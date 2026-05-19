using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Data.Activity;

public interface IPendingActivityStore
{
    Task<OperationResult<IReadOnlyList<PendingActivitySegment>>> LoadAsync();
    Task<OperationResult> SaveAsync(IEnumerable<PendingActivitySegment> segments);
    Task<OperationResult> AddAsync(PendingActivitySegment segment);
    Task<OperationResult> AddRangeAsync(IEnumerable<PendingActivitySegment> segments);
    Task<OperationResult<IReadOnlyList<PendingActivitySegment>>> GetPendingAsync();
    Task<OperationResult> RegisterSyncAttemptAsync(IEnumerable<long> ids, DateTimeOffset attemptedAt);
    Task<OperationResult> MarkAsSyncedAsync(IEnumerable<long> ids);
    Task<OperationResult> RemoveSyncedAsync();
    Task<OperationResult> ClearAsync();
}
