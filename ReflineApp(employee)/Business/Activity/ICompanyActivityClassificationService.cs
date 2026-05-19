using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Business.Activity;

public interface ICompanyActivityClassificationService
{
    Task<OperationResult> RestoreCachedRulesAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<OperationResult<int>> RefreshRulesAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<OperationResult> ClearCacheAsync(CancellationToken cancellationToken = default);

    ActivityCategory? TryClassify(string appName, string? windowTitle);

    ActivityClassificationDecision? TryClassifyDetailed(string appName, string? windowTitle);
}
