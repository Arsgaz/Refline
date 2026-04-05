using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Data.Activity;

public interface IActivityClassificationRuleStore
{
    Task<OperationResult<ActivityClassificationRulesCache?>> LoadAsync();

    Task<OperationResult> SaveAsync(ActivityClassificationRulesCache cache);

    Task<OperationResult> ClearAsync();
}
